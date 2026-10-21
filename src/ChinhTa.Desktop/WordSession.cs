using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Xps.Packaging;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

// All calls stay on the WPF STA thread. Only this private Word instance is closed.
public sealed class WordSession : IDisposable
{
    private dynamic? app;
    private dynamic? document;
    private readonly string folder = Path.Combine(Path.GetTempPath(), "ChinhTa-" + Guid.NewGuid());
    private bool snapshotCurrent = true;
    private readonly Stack<string> undoCheckpoints = new();
    private IReadOnlyList<WordParagraphSnapshot> lastLayout = [];
    private readonly Dictionary<LineRole,Issue> verifiedLineEdits = new();
    public IReadOnlyList<Issue> VerifiedLineEdits => verifiedLineEdits.Values.ToArray();
    public IReadOnlyList<LineTarget> LineTargets { get; private set; } = [];
    public IReadOnlyList<WordLineShape> LineShapes { get; private set; } = [];
    public bool HasGroupedShapes { get; private set; }
    public DocumentProfile Profile { get; set; } = DocumentProfile.State;
    public bool Recovered { get; private set; }
    public string RecoveryPath => Path.Combine(folder, "working.docx");
    public string Source { get; private set; } = "";
    public bool Changed { get; private set; }
    public bool IsOpen => document is not null;
    public string OfficeDescription => app is null ? "Word chưa mở" :
        $"Word {app.Version}; Build {app.Build}; App64={Environment.Is64BitProcess}; OS64={Environment.Is64BitOperatingSystem}";
    internal string? LineCheckArtifactFolder { get; init; }
    public WordSession() => Directory.CreateDirectory(folder);
    private void CaptureCheckArtifact(string path, string name)
    {
        // Enabled only by the isolated Office check, which uses the public synthetic sample.
        if (LineCheckArtifactFolder is null || !File.Exists(path)) return;
        try { File.Copy(path, Path.Combine(LineCheckArtifactFolder, name), true); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        { Diagnostics.Write("OfficeCheck.Capture", error); }
    }
    // Balance this acquisition only; child and parent COM wrappers can share identity.
    private static void Release(object? value)
    {
        if (value is not null && Marshal.IsComObject(value)) Marshal.ReleaseComObject(value);
    }
    public void Open(string path)
    {
        // Do not reuse an RCW from a Word process that has already exited.
        CloseConnection();
        undoCheckpoints.Clear();verifiedLineEdits.Clear();
        lastLayout = [];
        Source = Path.GetFullPath(path);
        Changed = false;
        Recovered = false;
        File.Copy(path, RecoveryPath, true);
        snapshotCurrent = true;
        ReadWithRecovery("Word.Open", () => { Connect(); return true; });
    }
    private void Connect()
    {
        app ??= Activator.CreateInstance(Type.GetTypeFromProgID("Word.Application")
            ?? throw new InvalidOperationException("Cần cài Microsoft Word desktop trên Windows."));
        app!.Visible = false;
        app.DisplayAlerts = 0;
        app.ScreenUpdating = false;
        app.AutomationSecurity = 3; // Never enable macros in documents opened by this instance.
        var working = RecoveryPath;
        dynamic docs = app.Documents;
        try { document = docs.Open(working, ReadOnly: false, AddToRecentFiles: false, Visible: false); }
        finally { Release(docs); }
        if ((bool)document!.TrackRevisions || (int)document.ProtectionType != -1)
        {
            DisposeDocument();
            throw new InvalidOperationException("Bản thử chưa hỗ trợ Track Changes hoặc tài liệu được bảo vệ.");
        }
    }
    private void Reconnect()
    {
        if (!snapshotCurrent)
            throw new InvalidOperationException("Word mất kết nối trước khi lưu được thay đổi cuối. " +
                "Không tự mở lại để tránh bỏ thay đổi âm thầm. Bản lưu gần nhất: " + RecoveryPath);
        Diagnostics.Write("Word.Reconnect", null);
        CloseConnection();
        Connect();
        Recovered = true;
    }
    private T ReadWithRecovery<T>(string operation, Func<T> read) => WordConnectionPolicy.ReadWithRecovery(() =>
    {
        try { return read(); }
        catch (Exception error) { Diagnostics.Write(operation, error); throw; }
    }, Reconnect);
    public IReadOnlyList<Issue> Check() => ReadWithRecovery("Word.Check", CheckCore);
    private IReadOnlyList<Issue> CheckCore()
    {
        // Page information after reopening may be stale until Word has paginated.
        document!.Repaginate();
        var paragraphs = new List<Paragraph>();
        IReadOnlyList<LayoutParagraph> headerLayout = [];
        var liveLayout = new List<WordParagraphSnapshot>();
        bool lineLayoutUnavailable = false;
        WordXmlMappingException? mappingWarning = null;
        dynamic ps = document!.Paragraphs;
        try
        {
            int count = (int)ps.Count;
            for (int i = 1; i <= count; i++)
            {
                dynamic p = ps[i]; dynamic r = p.Range;
                try
                {
                    string text = (string)r.Text;
                    int start = (int)r.Start;
                    if (Profile == DocumentProfile.State && i <= 40)
                    {
                        liveLayout.Add(ReadLayout(r, start, text));
                    }
                    if (HeadingRules.IsCandidate(text, Profile) ||
                        Profile == DocumentProfile.Personal && System.Text.RegularExpressions.Regex.IsMatch(text, @"^[ \t]+\S"))
                    {
                        dynamic f = r.Font; dynamic format = r.ParagraphFormat;
                        try { paragraphs.Add(new(start, text, (int)f.Bold == -1,
                            (float)f.Size, (int)format.Alignment, (string?)f.Name ?? "",
                            (int)f.Italic != 0, (int)f.Color, (bool)r.Information[12], (int)r.Information[3])); }
                        finally { Release(format); Release(f); }
                    }
                    else paragraphs.Add(new(start, text));
                }
                finally { Release(r); Release(p); }
            }
        }
        finally { Release(ps); }
        if (liveLayout.Count > 0)
        {
            dynamic content = document!.Content;
            try
            {
                // Current in-memory document, including unsaved edits; no per-paragraph Tables/Cells calls.
                string xml = (string)content.WordOpenXML;
                headerLayout = WordXmlTableMap.Read(xml, liveLayout, warning =>
                {
                    mappingWarning = warning;
                    Diagnostics.Write("Word.XmlTableMap." + warning.DiagnosticCode, warning);
                });
                lineLayoutUnavailable = headerLayout.Count == 0;
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.Xml.XmlException ||
                ex is COMException ce && ce.HResult == unchecked((int)0x800A1735))
            {
                Diagnostics.Write("Word.XmlTableMap.Unavailable", ex);
                lineLayoutUnavailable = true;
            }
            finally { if (Marshal.IsComObject(content)) Marshal.ReleaseComObject(content); }
        }
        lastLayout = liveLayout.ToArray();
        LineTargets = lineLayoutUnavailable ? [] : ShapeLineRules.Identify(headerLayout, Profile);
        LineShapes = ReadLineShapes();
        var issues = Checker.Check(paragraphs, Profile).ToList();
        if ((lineLayoutUnavailable || mappingWarning is not null && LineTargets.Count < 3) && paragraphs.FirstOrDefault(p => p.Text.Length > 0) is Paragraph first)
            issues.Add(new(first.Start, first.Text.Length, first.Text, first.Text,
                "Chưa đối chiếu đủ các mốc đường kẻ. " +
                (mappingWarning is null ? "Không đọc được cấu trúc XML của tài liệu." : "Mã kiểm tra: " + mappingWarning.DiagnosticCode + ".") +
                " Các vùng đã xác minh vẫn được kiểm tra; vùng chưa xác minh cần kiểm tra thủ công.",
                false, FixKind.ShapeLine, CanFix: false, Title: "Chưa kiểm tra được đường kẻ"));
        if (Profile == DocumentProfile.State && paragraphs.FirstOrDefault(p => p.Text.Length > 0) is Paragraph reference)
        {
            foreach (var role in ShapeLineRules.UnidentifiedRoles(LineTargets))
            {
                string label = role switch {LineRole.Agency => "cơ quan ban hành", LineRole.Motto => "tiêu ngữ", _ => "trích yếu"};
                issues.Add(new(reference.Start, reference.Text.Length, reference.Text, reference.Text,
                    "Chưa nhận diện được mốc " + label + "; chưa kết luận đường kẻ vùng này đúng hoặc thiếu. Cần kiểm tra lại nhận diện.",
                    false, FixKind.ShapeLine, CanFix:false, Title:"Chưa kiểm tra đường kẻ dưới " + label));
            }
        }
        if (Profile != DocumentProfile.Personal)
        {
            dynamic sections = document.Sections;
            try
            {
                int count = (int)sections.Count;
                for (int i = 1; i <= count; i++)
                {
                    dynamic section = sections[i]; dynamic setup = section.PageSetup; dynamic range = section.Range;
                    try
                    {
                        string text = (string)range.Text;
                        if (text.Length == 0) continue;
                        const double mmPerPoint = 25.4 / 72;
                        var layout = new SectionLayout(i, (int)range.Start, text[..1],
                            (double)setup.PageWidth * mmPerPoint, (double)setup.PageHeight * mmPerPoint,
                            (double)setup.TopMargin * mmPerPoint, (double)setup.BottomMargin * mmPerPoint,
                            (double)setup.LeftMargin * mmPerPoint, (double)setup.RightMargin * mmPerPoint,
                            (int)setup.MirrorMargins != 0);
                        issues.AddRange(PageRules.Check(layout, Profile));
                    }
                    finally { Release(range); Release(setup); Release(section); }
                }
            }
            finally { Release(sections); }
        }
        return issues.OrderBy(x => x.Start).ToArray();
    }
    private static WordParagraphSnapshot ReadLayout(dynamic range, int start, string text)
    {
        int page = (int)range.Information[3];
        bool inTable = (bool)range.Information[12];
        dynamic format = range.ParagraphFormat;
        bool centered, paragraphCentered;
        try { paragraphCentered = (int)format.Alignment == 1;
            centered = paragraphCentered && Math.Abs((float)format.LeftIndent) < .1 &&
            Math.Abs((float)format.RightIndent) < .1 && Math.Abs((float)format.FirstLineIndent) < .1 && !text.Contains('\t') &&
            text.TrimEnd('\r', '\a', '\n') == text.TrimEnd('\r', '\a', '\n').Trim(); }
        finally { Release(format); }
        return new(start, (int)range.End, text, page, inTable, centered, paragraphCentered);
    }
    private IReadOnlyList<WordLineShape> ReadLineShapes()
    {
        // Use the saved package and verified live paragraph offsets, without any Shape COM getter.
        var inventory = DocxLineInventory.Read(RecoveryPath, lastLayout);
        HasGroupedShapes = inventory.HasUnmappedShapes;
        return inventory.Lines.Select(line => new WordLineShape(line.Id, line.AnchorStart,
            line.Page, line.Geometry, line.AppOwned)).ToArray();
    }
    private void CloseDocumentOnly()
    {
        if (document is null) return;
        document.Close(0);
        Release(document);
        document = null;
    }
    private string ReadStoryText()
    {
        dynamic content = document!.Content;
        try { return (string)content.Text; }
        finally { Release(content); }
    }
    private void VerifyPackageLines(IReadOnlyList<Issue> plan)
    {
        var metadata = DocxLineMetadata.Read(RecoveryPath);
        DocxLineMetadata.Verify(metadata, plan.Select(item => item.LineFix!.Role));
        // Package identity is only the first gate. VerifyRenderedLines must still pass.
        // Office 2021 can fail even on Shape.ID / Shape.Type; never enumerate Shapes here.
    }
    internal void VerifyLineResult(IReadOnlyList<Issue> plan, IReadOnlyList<LineGeometry>? preserve = null, bool verifyReference = false)
    {
        VerifyPackageLines(plan);
        VerifyRenderedLines(plan, preserve, verifyReference);
    }
    private void VerifyRenderedLines(IReadOnlyList<Issue> plan, IReadOnlyList<LineGeometry>? preserve, bool verifyReference)
    {
        document!.Repaginate();
        string path = Path.Combine(folder, Guid.NewGuid() + ".verify.xps");
        try
        {
            var anchors=new Dictionary<Issue,PreviewAnchor>();
            path=ExportPreviewCore(plan,anchors);
            using var xps = new XpsDocument(path, FileAccess.Read);
            var sequence = xps.GetFixedDocumentSequence();
            var preview=new PreviewHighlighter();
            var rendered = preview.HorizontalLines(sequence, 1);
            if(preserve is not null && preserve.Any(line=>RenderedLineVerifier.Find(line,rendered) is null))
                throw new InvalidOperationException("Nét đã có thay đổi sau khi thêm Line; phải khôi phục bản trước.");
            foreach (var item in plan)
            {
                var fix = item.LineFix!;
                Issue? rebased=null;
                if(verifyReference)
                {
                    var target=LineTargets.Single(t=>t.Start==item.Start && t.Role==fix.Role);
                    var reference=ReferenceMeasurement.Read(sequence,preview,anchors[item]);
                    if(reference is null || !target.Centered)
                        throw new InvalidOperationException("Chưa đo được dòng tham chiếu sau khi Word mở candidate; phải khôi phục bản trước.");
                    var assessed=ShapeLineRules.Evaluate(target,reference,new(fix.Left,fix.Top,fix.Width),fix.ShapeId,LinePolicy.Decree30);
                    if(assessed.Errors.Length>0)rebased=item with {LineFix=assessed.Fix};
                }
                if (RenderedLineVerifier.Find(new(fix.Left, fix.Top, fix.Width), rendered) is not null && rebased is null) continue;
                var failure = new RenderedLineMismatchException(rendered) {
                    RebasedPlan=rebased is null ? null : plan.Select(i=>i.LineFix!.Role==fix.Role?rebased!:i).ToArray()};
                failure.Data["ChinhTa.LineVerification"] = FormattableString.Invariant(
                    $"Role={fix.Role}; ExpectedLeft={fix.Left:F2}; ExpectedTop={fix.Top:F2}; ExpectedWidth={fix.Width:F2}; RenderedCount={rendered.Count}") +
                    "; Rendered=" + string.Join("|", rendered.Take(40).Select(line => FormattableString.Invariant(
                        $"L={line.Left:F2},Y={line.Y:F2},W={line.Width:F2}")));
                if(rebased?.LineFix is ShapeLineFix updated)
                    failure.Data["ChinhTa.LineVerification"] += FormattableString.Invariant(
                        $"; ReferenceUpdatedLeft={updated.Left:F2}; ReferenceUpdatedTop={updated.Top:F2}; ReferenceUpdatedWidth={updated.Width:F2}");
                throw failure;
            }
        }
        catch
        {
            CaptureCheckArtifact(path, "failed-line-preview.xps");
            throw;
        }
        finally
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (IOException error) { Diagnostics.Write("Word.LineVerify.Cleanup",error); }
        }
    }
    private (IReadOnlyList<LineGeometry> Lines, LineAnchorFrame? MottoFrame) ReadPlacementBaseline(IReadOnlyList<Issue> plan)
    {
        var anchors=new Dictionary<Issue,PreviewAnchor>();
        string path=ExportPreview(plan,anchors);
        try
        {
            using var xps=new XpsDocument(path,FileAccess.Read);
            var sequence=xps.GetFixedDocumentSequence();
            var preview=new PreviewHighlighter();
            LineAnchorFrame? frame=null;
            if(plan.Count==1 && plan[0].LineFix?.Role==LineRole.Motto &&
                lastLayout.Any(p=>p.Start==plan[0].Start && p.InTable) && anchors.TryGetValue(plan[0],out var anchor))
                frame=preview.TableFrame(sequence,anchor);
            return (preview.HorizontalLines(sequence,1),frame);
        }
        finally {if(File.Exists(path))File.Delete(path);}
    }
    private IReadOnlyList<Issue> ApplyLinePackage(IReadOnlyList<Issue> plan, string before, bool recordUndo = true, bool verifyReference = true)
    {
        string candidate = Path.Combine(folder, Guid.NewGuid() + ".candidate.docx");
        bool previousChanged = Changed;
        IReadOnlyList<Issue> acceptancePlan=plan;
        try
        {
            string expectedText = ReadStoryText();
            // Word owns RecoveryPath with a write handle. ZIP's FileShare.Read conflicts with that handle.
            // The checkpoint copy was made by File.Copy and is not open in Word.
            var encodedPlan=DocxLineEditor.Write(before, candidate, lastLayout, LineTargets, plan);
            snapshotCurrent = false;
            var baseline = ReadPlacementBaseline(plan);
            void ApplyCandidate()
            {
                int openAttempts = 0;
                CheckpointSwap.Apply(RecoveryPath, candidate, CloseDocumentOnly, () =>
                {
                    bool verifyLines = ++openAttempts == 1;
                    Connect();
                    if (ReadStoryText() != expectedText)
                        throw new InvalidOperationException("Nội dung chữ thay đổi sau khi mở candidate; phải khôi phục bản trước.");
                    if (verifyLines) VerifyLineResult(acceptancePlan, plan.All(i=>i.LineFix!.ShapeId is null) ? baseline.Lines : null,verifyReference);
                });
            }
            try { ApplyCandidate(); }
            catch (RenderedLineMismatchException mismatch)
            {
                // CheckpointSwap has already restored and reopened the original working document.
                var corrected = LinePlacementFeedback.Correct(plan, baseline.Lines, mismatch.Rendered);
                bool frameCorrection=corrected is null;
                corrected ??= LinePlacementFeedback.CorrectHiddenMotto(plan,baseline.Lines,mismatch.Rendered,baseline.MottoFrame);
                if (corrected is null) throw;
                acceptancePlan=mismatch.RebasedPlan ?? plan;
                // Feedback is in page coordinates; retain any existing DOCX encoding translation.
                corrected=LinePlacementFeedback.Rebase(corrected,plan,encodedPlan);
                corrected=LinePlacementFeedback.Rebase(corrected,plan,acceptancePlan);
                if(frameCorrection && baseline.MottoFrame is LineAnchorFrame frame)
                    mismatch.Data["ChinhTa.LineVerification"] += FormattableString.Invariant(
                        $"; CellLeft={frame.Left:F2}; CellTop={frame.Top:F2}; CellWidth={frame.Width:F2}; PageWidth={frame.PageWidth:F2}");
                Diagnostics.Write(frameCorrection ? "Word.LinePackage.TableFrameCorrection" : "Word.LinePackage.MeasuredCorrection", mismatch);
                File.Delete(candidate);
                DocxLineEditor.Write(before, candidate, lastLayout, LineTargets, corrected, acceptancePlan);
                // Exactly one retry. Use the original intended plan for the full verification.
                ApplyCandidate();
                Diagnostics.Write(frameCorrection ? "Word.LinePackage.TableFrameCorrection.Verified" : "Word.LinePackage.MeasuredCorrection.Verified", null);
            }
            snapshotCurrent = true;
            if(recordUndo)
            {
                undoCheckpoints.Push(before);
                foreach(var item in acceptancePlan)verifiedLineEdits[item.LineFix!.Role]=item;
            }
            Changed = true;
            return acceptancePlan;
        }
        catch (Exception error)
        {
            CaptureCheckArtifact(candidate, "failed-line-candidate.docx");
            Diagnostics.Write("Word.LinePackage", error);
            // A failed candidate open is rolled back by CheckpointSwap. Keep backups if rollback fails.
            snapshotCurrent = error is not AggregateException;
            Changed = previousChanged;
            throw new InvalidOperationException("Chưa áp dụng được nhóm đường kẻ. " +
                "Bản trước lượt sửa được giữ tại: " + before, error);
        }
        finally { if (File.Exists(candidate)) File.Delete(candidate); }
    }
    public void Fix(Issue issue) => FixMany([issue]);
    public void FixMany(IEnumerable<Issue> issues) => FixManyCore(issues,true);
    internal void FixLineProbe(IEnumerable<Issue> issues) => FixManyCore(issues,false);
    private void FixManyCore(IEnumerable<Issue> issues,bool verifyReference)
    {
        var plan = BatchPlan.Create(issues);
        if (plan.Count == 0) return;
        if (plan.Any(i => i.LineFix is not null) && plan.Any(i => i.LineFix is null))
            throw new InvalidOperationException("Sửa chữ/định dạng trước, quét lại rồi sửa nhóm Shape Line để dùng số đo mới.");
        // Validate every original before making any changes, not halfway through the batch.
        BatchPlan.Validate(plan, issue =>
        {
            dynamic range = document!.Range(issue.Start, issue.Start + issue.Length);
            try { return (string)range.Text; } finally { Release(range); }
        });
        Checkpoint();
        string before = Path.Combine(folder, Guid.NewGuid() + ".undo.docx");
        File.Copy(RecoveryPath, before);
        if (plan.All(i => i.LineFix is not null))
        {
            if(plan.Count==1)ApplyLinePackage(plan,before,verifyReference:verifyReference);
            else
            {
                bool previousChanged=Changed;
                try
                {
                    var accepted=new List<Issue>();
                    AtomicLineBatch.Apply(RecoveryPath,before,plan,
                        (item,step)=>accepted.AddRange(ApplyLinePackage([item],step,recordUndo:false,verifyReference:verifyReference)),
                        ()=>CheckpointSwap.Apply(RecoveryPath,before,CloseDocumentOnly,Connect),
                        ()=>VerifyLineResult(accepted,verifyReference:verifyReference));
                    undoCheckpoints.Push(before);
                    foreach(var item in accepted)verifiedLineEdits[item.LineFix!.Role]=item;
                    Changed=true;snapshotCurrent=true;
                }
                catch(Exception error)
                {
                    Changed=previousChanged;snapshotCurrent=error is not AggregateException;
                    Diagnostics.Write("Word.LineBatch",error);
                    throw new InvalidOperationException("Chưa áp dụng được cả nhóm đường kẻ. Bản trước cả lượt sửa: "+before,error);
                }
            }
            return;
        }
        verifiedLineEdits.Clear();
        undoCheckpoints.Push(before);
        dynamic undo = app!.UndoRecord;
        try
        {
            undo.StartCustomRecord($"Sửa {plan.Count} lỗi");
            snapshotCurrent = false;
            Changed = true;
            try
            {
                foreach (var issue in plan)
                {
                    dynamic r = document!.Range(issue.Start, issue.Start + issue.Length);
                    try
                    {
                        if ((string)r.Text != issue.Replacement) r.Text = issue.Replacement;
                        if (issue.Format is HeadingFormat heading)
                        {
                            dynamic f = r.Font; dynamic format = r.ParagraphFormat;
                            try
                            {
                                f.Bold = -1; f.Italic = 0; f.Color = 0;
                                f.Size = heading.Size; f.Name = "Times New Roman";
                                if (heading.Alignment is int alignment) format.Alignment = alignment;
                            }
                            finally { Release(f); Release(format); }
                        }
                    }
                    finally { Release(r); }
                }
            }
            finally { undo.EndCustomRecord(); }
        }
        catch (Exception error)
        {
            Diagnostics.Write("Word.FixMany", error);
            throw new InvalidOperationException("Lượt sửa bị gián đoạn; có thể mới áp dụng một phần. " +
                "Không tự thử lại. Nếu Word vẫn kết nối, dùng Hoàn tác để bỏ lượt sửa hoặc lưu bản sao để kiểm tra. " +
                "Bản checkpoint trước lượt sửa: " + RecoveryPath, error);
        }
        finally { Release(undo); }
        // One checkpoint, one UI rescan/export for the entire successful batch.
        Checkpoint();
    }
    private void Checkpoint()
    {
        document!.Save();
        snapshotCurrent = true;
    }
    public void Undo()
    {
        if (undoCheckpoints.Count == 0) return;
        string before = undoCheckpoints.Peek();
        verifiedLineEdits.Clear();
        snapshotCurrent = false;
        try
        {
            CheckpointSwap.Apply(RecoveryPath, before, CloseDocumentOnly, Connect);
            undoCheckpoints.Pop();
            Changed = true;
            snapshotCurrent = true;
        }
        catch (Exception error)
        {
            Diagnostics.Write("Word.UndoCheckpoint", error);
            snapshotCurrent = error is not AggregateException;
            throw;
        }
    }
    public string ExportPreview(IReadOnlyList<Issue> issues, IDictionary<Issue, PreviewAnchor> anchors)
        => ReadWithRecovery("Word.ExportPreview", () => ExportPreviewCore(issues, anchors));
    private string ExportPreviewCore(IReadOnlyList<Issue> issues, IDictionary<Issue, PreviewAnchor> anchors)
    {
        anchors.Clear();
        // Export the unmodified document once. Selection highlighting belongs exclusively to WPF.
        // CheckCore has already paginated before collecting page metadata.
        // Read the story once; do not navigate Word's invisible window with Document.GoTo.
        // Each issue retains its absolute source offset plus the physical page number.
        dynamic content = document!.Content;
        int contentStart;
        string text;
        try { contentStart = (int)content.Start; text = (string)content.Text; }
        finally { Release(content); }
        foreach (var issue in issues)
        {
            dynamic point = document.Range(issue.Start, issue.Start);
            int page;
            try { page = (int)point.Information[3]; } finally { Release(point); }
            anchors[issue] = new(page, text, issue.Start - contentStart, issue.Length);
        }
        string output = Path.Combine(folder, Guid.NewGuid() + ".xps");
        document.ExportAsFixedFormat(output, 18, OpenAfterExport: false);
        return output;
    }
    public void SaveCopy(string path)
    {
        if (string.Equals(Path.GetFullPath(path), Source, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Hãy chọn tên khác để giữ nguyên tài liệu gốc.");
        Checkpoint();
        File.Copy(RecoveryPath, path, true);
        Changed = false;
    }
    private void CloseConnection()
    {
        // A dead COM server can throw from Close/Quit too. Still release both handles.
        dynamic? oldDocument = document; document = null;
        dynamic? oldApp = app; app = null;
        try
        {
            if (oldDocument is not null)
                try { oldDocument.Close(0); }
                catch (COMException error) { Diagnostics.Write("Word.Close", error); }
        }
        finally
        {
            Release(oldDocument);
            try
            {
                if (oldApp is not null)
                    try { oldApp.Quit(0); }
                    catch (COMException error) { Diagnostics.Write("Word.Quit", error); }
            }
            finally { Release(oldApp); }
        }
    }
    private void DisposeDocument() => CloseConnection();
    public void Dispose()
    {
        CloseConnection();
        // Keep the last checkpoint if a mutation could not be saved successfully.
        if (!snapshotCurrent) return;
        try { Directory.Delete(folder, true); } catch (IOException) { }
    }
}

