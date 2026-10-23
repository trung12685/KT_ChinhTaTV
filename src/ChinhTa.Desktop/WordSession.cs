using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Xps.Packaging;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

// All calls stay on the WPF STA thread. Only this private Word instance is closed.
public sealed partial class WordSession : IDisposable
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
    public IReadOnlyList<AdministrativePositionTarget> AdministrativePositionTargets { get; private set; } = [];
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
        lastLayout = [];AdministrativePositionTargets=[];
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
        var editableHeaderStarts=new HashSet<int>();
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
                    WordParagraphSnapshot? snapshot=null;
                    if (Profile == DocumentProfile.State && i <= 40)
                    {
                        snapshot=ReadLayout(r,start,text);
                        liveLayout.Add(snapshot);
                    }
                    bool documentHeading=Profile==DocumentProfile.State && i<=40 && !HeadingRules.IsCandidate(text,Profile) && snapshot is {InTable:false} &&
                        (ShapeLineRules.IsDocumentType(text) || snapshot.ParagraphCentered==true);
                    if (documentHeading || HeadingRules.IsCandidate(text, Profile) ||
                        Profile == DocumentProfile.State && i<=40 && AdministrativeHeaderRules.IsCandidate(text) ||
                        Profile == DocumentProfile.Personal && System.Text.RegularExpressions.Regex.IsMatch(text, @"^[ \t]+\S"))
                    {
                        bool administrative=Profile==DocumentProfile.State && i<=40 && AdministrativeHeaderRules.IsCandidate(text);
                        // The paragraph/cell terminator can carry a different font. Observe
                        // the same visible text span that a new character-format fix changes.
                        bool observeVisible=administrative || documentHeading || HeadingRules.IsCandidate(text,Profile);
                        dynamic observed=observeVisible?document!.Range(start,start+text.TrimEnd('\r','\a','\n').Length):r;
                        dynamic? f=null,format=null;
                        try
                        {
                            f=observed.Font;format=r.ParagraphFormat;
                            int bold=(int)f.Bold,italic=(int)f.Italic,color=(int)f.Color;
                            float size=(float)f.Size;string fontName=(string?)f.Name??"";
                            paragraphs.Add(new(start, text, bold == -1,
                            size, (int)format.Alignment, fontName,
                            italic != 0, color, snapshot?.InTable??(bool)r.Information[12], snapshot?.Page??(int)r.Information[3],
                            FormatObserved:true,FormatMixed:italic is not (0 or -1) || size<=0 || size>=9999999 || color is 9999999 or -9999999 ||
                                string.IsNullOrWhiteSpace(fontName),BoldMixed:bold is not (0 or -1))); }
                        finally { Release(format); Release(f); if(observeVisible)Release(observed); }
                    }
                    else paragraphs.Add(new(start, text,Page:snapshot?.Page??0,InTable:snapshot?.InTable??false));
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
                },onTextEditable:(start,editable)=>{if(editable)editableHeaderStarts.Add(start);});
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
        for(int i=0;i<paragraphs.Count;i++)
            if(Profile==DocumentProfile.State && (i<40 || AdministrativeHeaderRules.IsCandidate(paragraphs[i].Text)))
                paragraphs[i]=paragraphs[i] with {TextEditable=editableHeaderStarts.Contains(paragraphs[i].Start)};
        LineTargets = lineLayoutUnavailable ? [] : ShapeLineRules.Identify(headerLayout, Profile);
        AdministrativePositionTargets=AdministrativePositionRules.Identify(paragraphs,headerLayout,Profile);
        LineShapes = ReadLineShapes();
        var issues = Checker.Check(paragraphs, Profile).ToList();
        issues.AddRange(DocumentHeadingRules.Check(paragraphs,headerLayout,Profile));
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
    internal string ReadCheckText() => ReadStoryText(); // Isolated Office check; never logged.
    internal (int Bold,int Underline,int Alignment) ReadCheckDecorations(Issue issue)
    {
        if(LineCheckArtifactFolder is null)throw new InvalidOperationException("Chỉ dùng trong phiên kiểm Word riêng.");
        dynamic range=document!.Range(issue.Start,issue.Start+issue.Length);
        try
        {
            dynamic font=range.Font;dynamic format=range.ParagraphFormat;
            try{return ((int)font.Bold,(int)font.Underline,(int)format.Alignment);}
            finally{Release(format);Release(font);}
        }
        finally{Release(range);}
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
                var actual=RenderedLineVerifier.Find(new(fix.Left,fix.Top,fix.Width),rendered);
                Issue? rebased=null;
                if(verifyReference)
                {
                    var target=LineTargets.Single(t=>t.Start==item.Start && t.Role==fix.Role);
                    if(!anchors.TryGetValue(item,out var anchor))
                        throw new InvalidOperationException("Không đối chiếu được mốc Word trong bản xem trước; phải khôi phục bản trước.");
                    var reference=ReferenceMeasurement.Read(sequence,preview,anchor);
                    if(reference is null || !target.Centered)
                        throw new InvalidOperationException("Chưa đo được dòng tham chiếu sau khi Word mở candidate; phải khôi phục bản trước.");
                    // Apply exactly the scanner's policy to the measured stroke, not its intended coordinates.
                    var assessed=RenderedLineVerifier.Assess(target,reference,fix,rendered,LinePolicy.Decree30);
                    if(assessed.Errors.Length>0)rebased=item with {LineFix=assessed.Fix};
                }
                if (actual is not null && rebased is null) continue;
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
    private (IReadOnlyList<LineGeometry> Preserve, LineAnchorFrame? MottoFrame) ReadPlacementBaseline(IReadOnlyList<Issue> plan,string checkpoint)
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
            var baseline=preview.HorizontalLines(sequence,1);
            var replaced=new List<LineGeometry>();
            // Remove only the uniquely identified stroke that this candidate replaces.
            // Otherwise a sub-point update can be mistaken for an unchanged baseline stroke.
            var inventory=DocxLineInventory.Read(checkpoint,lastLayout);
            foreach(var item in plan.Where(i=>i.LineFix!.ShapeId is not null))
            {
                var owned=inventory.Lines.SingleOrDefault(l=>l.Id==item.LineFix!.ShapeId && l.AppOwned);
                if(owned?.Geometry is not LineGeometry geometry)
                    throw new InvalidOperationException("Không xác định được Line cũ cần đặt lại.");
                replaced.Add(geometry);
            }
            return (RenderedLineVerifier.RetainOtherStrokes(baseline,replaced),frame);
        }
        finally {if(File.Exists(path))File.Delete(path);}
    }
    private IReadOnlyList<Issue> ApplyLinePackage(IReadOnlyList<Issue> plan, string before, bool verifyReference = true)
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
            var baseline = ReadPlacementBaseline(plan,before);
            void ApplyCandidate()
            {
                int openAttempts = 0;
                SwapCheckpoint(RecoveryPath, candidate, CloseDocumentOnly, () =>
                {
                    bool verifyLines = ++openAttempts == 1;
                    Connect();
                    if (ReadStoryText() != expectedText)
                        throw new InvalidOperationException("Nội dung chữ thay đổi sau khi mở candidate; phải khôi phục bản trước.");
                    if(verifyReference)
                    {
                        // Refresh on both candidate open and restoration. Neither file is assumed
                        // to retain the CPs observed before the preceding open/save cycle.
                        CheckCore();
                        LogReferenceBindings(acceptancePlan,verifyLines?"Candidate":"Restored");
                        if(verifyLines)
                            acceptancePlan=LineReferenceBinding.Rebind(acceptancePlan,LineTargets,expectedText,ReadStoryText());
                    }
                    if (verifyLines) VerifyLineResult(acceptancePlan,baseline.Preserve,verifyReference);
                });
            }
            try { ApplyCandidate(); }
            catch (RenderedLineMismatchException mismatch)
            {
                // CheckpointSwap has already restored and reopened the original working document.
                var corrected = LinePlacementFeedback.Correct(plan, baseline.Preserve, mismatch.Rendered);
                bool frameCorrection=corrected is null;
                corrected ??= LinePlacementFeedback.CorrectHiddenMotto(plan,baseline.Preserve,mismatch.Rendered,baseline.MottoFrame);
                if (corrected is null) throw;
                acceptancePlan=mismatch.RebasedPlan ?? plan;
                // Feedback is in page coordinates; retain any existing DOCX encoding translation.
                corrected=LinePlacementFeedback.Rebase(corrected,plan,encodedPlan);
                corrected=LinePlacementFeedback.Rebase(corrected,plan,acceptancePlan);
                if(verifyReference)
                {
                    // CheckpointSwap has restored the source: encode using its current CPs,
                    // while keeping the independently measured encoded and intended geometry.
                    string restoredStory=ReadStoryText();
                    corrected=LineReferenceBinding.Rebind(corrected,LineTargets,expectedText,restoredStory);
                    acceptancePlan=LineReferenceBinding.Rebind(acceptancePlan,LineTargets,expectedText,restoredStory);
                }
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
    private string ReadIssueText(Issue issue)
    {
        dynamic range=document!.Range(issue.Start,issue.Start+issue.Length);
        try{return (string)range.Text;}
        finally{Release(range);}
    }
    private LineBatchReview ReadRemainingLines(IReadOnlyList<Issue> requested,IDictionary<LineRole,Issue> accepted,string expectedStory)
    {
        // Exercise the same save, refresh and XPS inspection that follows the UI operation,
        // while the original checkpoint can still restore the entire user action.
        Checkpoint();
        var otherIssues=CheckCore();
        LogReferenceBindings(requested,"AfterSave");
        string currentStory=ReadStoryText();
        Diagnostics.Write("Word.LineBatch.StoryBinding",null,FormattableString.Invariant(
            $"PreviousLength={expectedStory.Length}; CurrentLength={currentStory.Length}; Equal={(expectedStory==currentStory?1:0)}"));
        var bound=LineReferenceBinding.Rebind(requested,LineTargets,expectedStory,currentStory);
        foreach(var item in bound)
        {
            var role=item.LineFix!.Role;
            accepted[role]=accepted[role] with {Start=item.Start,Length=item.Length};
        }
        var anchors=new Dictionary<Issue,PreviewAnchor>();
        string path=ExportPreviewCore(LineTargets.Select(t=>t.Probe).ToArray(),anchors);
        try
        {
            using var xps=new XpsDocument(path,FileAccess.Read);
            var sequence=xps.GetFixedDocumentSequence();
            var preview=new PreviewHighlighter();
            var residual=LineInspection.Inspect(sequence,preview,LineTargets,LineShapes,anchors,otherIssues,
                LinePolicy.Decree30,HasGroupedShapes,accepted.Values.ToArray()).Where(entry=>bound.Any(i=>
                    entry.Issue.Start==i.Start && entry.Issue.Original==i.Original)).Select(entry=>entry.Issue).ToArray();
            foreach(var item in bound)
            {
                var fix=item.LineFix!;
                var target=LineTargets.Single(t=>t.Start==item.Start && t.Role==fix.Role);
                var reference=anchors.TryGetValue(target.Probe,out var anchor)?ReferenceMeasurement.Read(sequence,preview,anchor):null;
                var expected=accepted[fix.Role].LineFix!;
                var actual=RenderedLineVerifier.Find(new(expected.Left,expected.Top,expected.Width),preview.HorizontalLines(sequence,1));
                Diagnostics.Write("Word.LineBatch.Rescan",null,FormattableString.Invariant(
                    $"Role={fix.Role}; Residual={residual.Count(i=>i.Start==item.Start)}; ReferenceLeft={reference?.Left:F2}; ReferenceBottom={reference?.Bottom:F2}; ReferenceWidth={reference?.Width:F2}; ActualLeft={actual?.Left:F2}; ActualTop={actual?.Y:F2}; ActualWidth={actual?.Width:F2}"));
            }
            return new(bound,residual);
        }
        finally{if(File.Exists(path))File.Delete(path);}
    }
    private void LogReferenceBindings(IReadOnlyList<Issue> requested,string stage)
    {
        foreach(var item in requested)
        {
            var role=item.LineFix!.Role;
            var candidates=LineTargets.Where(t=>t.Role==role).ToArray();
            var matching=candidates.Where(t=>t.Text==item.Original).ToArray();
            Diagnostics.Write("Word.LineBatch.ReferenceBinding."+stage,null,FormattableString.Invariant(
                $"Role={role}; PreviousStart={item.Start}; PreviousLength={item.Length}; RoleCount={candidates.Length}; TextMatchCount={matching.Length}; CurrentStart={(matching.Length==1?matching[0].Start:-1)}; Centered={(matching.Length==1&&matching[0].Centered?1:0)}"));
        }
    }
    internal void FixTextFailureProbe(IEnumerable<Issue> issues,int failAfter)
    {
        if(LineCheckArtifactFolder is null || failAfter<1)throw new InvalidOperationException("Chỉ dùng trong phiên Kiểm tra Word riêng.");
        FixManyCore(issues,true,failAfter);
    }
    internal void FixHeadingDriftProbe(IEnumerable<Issue> issues)
    {
        if(LineCheckArtifactFolder is null)throw new InvalidOperationException("Chỉ dùng trong phiên Kiểm tra Word riêng.");
        var selected=issues.ToArray();
        if(!selected.Any(i=>i.Format is not null) || selected.Any(i=>i.LineFix is not null || i.CenterFix is not null))
            throw new InvalidOperationException("Mẫu lỗi hậu kiểm cần gợi ý định dạng tiêu đề.");
        FixManyCore(selected,true,driftHeading:true);
    }
    private void FixManyCore(IEnumerable<Issue> issues,bool verifyReference,int? failTextAfter=null,bool driftHeading=false)
    {
        var plan = BatchPlan.Create(issues);
        if (plan.Count == 0) return;
        if (plan.Any(i => i.LineFix is not null) && plan.Any(i => i.LineFix is null))
            throw new InvalidOperationException("Sửa chữ/định dạng trước, quét lại rồi sửa nhóm Shape Line để dùng số đo mới.");
        if(failTextAfter is not null && plan.Any(i=>i.LineFix is not null))
            throw new InvalidOperationException("Mẫu lỗi chữ không nhận Shape Line.");
        if(plan.Any(i=>i.CenterFix is not null))
        {
            if(plan.Any(i=>i.CenterFix is null))
                throw new InvalidOperationException("Sửa chữ/phông trước, quét lại rồi chọn riêng nhóm căn giữa; sửa Line sau cùng.");
            FixPositionBatch(plan,failTextAfter);
            return;
        }
        if(plan.All(i=>i.LineFix is null))
            CurrentTextPlan.Validate(plan,CheckCore());
        // Validate every original before making any changes, not halfway through the batch.
        BatchPlan.Validate(plan,ReadIssueText);
        Checkpoint();
        string before = Path.Combine(folder, Guid.NewGuid() + ".undo.docx");
        File.Copy(RecoveryPath, before);
        if (plan.All(i => i.LineFix is not null))
        {
            bool previousChanged=Changed;
            string expectedText=ReadStoryText();
            try
            {
                var accepted=new Dictionary<LineRole,Issue>();
                AtomicLineBatch.Apply(RecoveryPath,before,plan,
                    (item,step)=>
                    {
                        Issue current=item;
                        if(verifyReference)
                        {
                            CheckCore();
                            LogReferenceBindings([item],"BeforeEdit");
                            current=LineReferenceBinding.Rebind([item],LineTargets,expectedText,ReadStoryText()).Single();
                            BatchPlan.Validate([current],ReadIssueText);
                        }
                        foreach(var completed in ApplyLinePackage([current],step,verifyReference:verifyReference))
                            accepted[completed.LineFix!.Role]=completed;
                    },
                    ()=>SwapCheckpoint(RecoveryPath,before,CloseDocumentOnly,Connect),
                    ()=>
                    {
                        if(verifyReference && ReadRemainingLines(plan,accepted,expectedText).Repairs.Count>0)
                            throw new InvalidOperationException("Còn cảnh báo ở Line vừa sửa sau lượt đo lại; phải khôi phục cả lượt.");
                        VerifyLineResult(accepted.Values.ToArray(),verifyReference:verifyReference);
                        if(ReadStoryText()!=expectedText)
                            throw new InvalidOperationException("Nội dung chữ thay đổi sau lượt sửa Line; phải khôi phục bản trước.");
                    },
                    verifyReference ? ()=>ReadRemainingLines(plan,accepted,expectedText) : null);
                undoCheckpoints.Push(before);
                foreach(var item in accepted.Values)verifiedLineEdits[item.LineFix!.Role]=item;
                Changed=true;snapshotCurrent=true;
            }
            catch(Exception error)
            {
                Changed=previousChanged;snapshotCurrent=error is not AggregateException;
                Diagnostics.Write("Word.LineBatch",error);
                throw new InvalidOperationException("Chưa áp dụng được cả lượt sửa đường kẻ. Bản trước lượt sửa: "+before,error);
            }
            return;
        }
        var formatRanges=new List<(Issue Issue,object Range,string Expected)>();
        try
        {
            dynamic textContent=document!.Content;
            int storyStart;string originalStory;
            try{storyStart=(int)textContent.Start;originalStory=(string)textContent.Text;}
            finally{Release(textContent);}
            string expectedStory=AtomicTextBatch.ExpectedStory(plan,originalStory,item=>
            {
                dynamic prefix=document!.Range(storyStart,item.Start);
                try{return (string?)prefix.Text??"";}finally{Release(prefix);}
            });
            foreach(var formatIssue in plan.Where(i=>i.CharacterFormat is not null))
            {
                var rowEdits=plan.Where(i=>i.CharacterFormat is null && i.Start>=formatIssue.Start &&
                    (long)i.Start+i.Length<=(long)formatIssue.Start+formatIssue.Length).ToArray();
                string expectedRow=AtomicTextBatch.ExpectedStory(rowEdits,formatIssue.Original,item=>
                {
                    dynamic prefix=document!.Range(formatIssue.Start,item.Start);
                    try{return (string?)prefix.Text??"";}finally{Release(prefix);}
                });
                dynamic target=document!.Range(formatIssue.Start,formatIssue.Start+formatIssue.Length);
                try
                {
                    if((string)target.Text!=formatIssue.Original)throw new InvalidOperationException("Dòng định dạng đã thay đổi; hãy quét lại.");
                    formatRanges.Add((formatIssue,(object)target,expectedRow));target=null!;
                }
                finally{Release(target);}
            }
            bool textPreviousChanged=Changed,started=false,restored=false;
            int applied=0;
            try
            {
                AtomicTextBatch.Apply(plan,ReadIssueText,item=>
                {
                    started=true;snapshotCurrent=false;Changed=true;
                    verifiedLineEdits.Clear();
                    dynamic range=document!.Range(item.Start,item.Start+item.Length);
                    try
                    {
                        if((string)range.Text!=item.Replacement)range.Text=item.Replacement;
                        if(item.CharacterFormat is CharacterFormat character)
                        {
                            dynamic font=range.Font;
                            try{font.Name="Times New Roman";font.Size=character.Size;font.Italic=character.Italic?-1:0;font.Color=0;
                                if(character.Bold is bool bold)font.Bold=bold?-1:0;}
                            finally{Release(font);}
                        }
                        if(item.Format is HeadingFormat heading)
                        {
                            dynamic font=range.Font;dynamic format=range.ParagraphFormat;
                            try
                            {
                                font.Bold=-1;font.Italic=0;font.Color=0;
                                font.Size=heading.Size;font.Name="Times New Roman";
                                if(heading.Alignment is int alignment)format.Alignment=alignment;
                            }
                            finally{Release(format);Release(font);}
                        }
                        if(item.Format is not null)
                        {
                            // Track the actual replacement range, after a full heading text
                            // replacement has changed its length. Later lower CP edits may move it.
                            dynamic tracked=range.Duplicate;
                            try{formatRanges.Add((item,(object)tracked,item.Replacement));tracked=null!;}
                            finally{Release(tracked);}
                        }
                        string actual=(string?)range.Text??"";
                        if(++applied==failTextAfter)throw new OfficeTextProbeException();
                        return actual;
                    }
                    finally{Release(range);}
                },()=>
                {
                    Checkpoint();
                    if(driftHeading)
                    {
                        // Only the separate synthetic Office check can request this drift.
                        // Do not throw here: the normal post-save proof must detect it.
                        dynamic changedRange=formatRanges.First(s=>s.Issue.Format is not null).Range;
                        dynamic changedFont=changedRange.Font;
                        try{changedFont.Italic=-1;snapshotCurrent=false;}
                        finally{Release(changedFont);}
                    }
                    if(ReadStoryText()!=expectedStory)
                        throw new InvalidOperationException("Chữ sau lưu không đúng phạm vi đã chọn; phải khôi phục cả lượt.");
                    foreach(var styled in formatRanges)
                    {
                        dynamic target=styled.Range;dynamic font=target.Font;dynamic? paragraphFormat=null;
                        try
                        {
                            int? alignment=null;
                            if(styled.Issue.Format?.Alignment is not null)
                            {paragraphFormat=target.ParagraphFormat;alignment=(int)paragraphFormat.Alignment;}
                            string name=(string?)font.Name??"";float size=(float)font.Size;
                            int bold=(int)font.Bold,italic=(int)font.Italic,color=(int)font.Color;
                            bool matches=styled.Issue.CharacterFormat is CharacterFormat character
                                ?character.Matches(name,size,italic,color,bold)
                                :styled.Issue.Format is HeadingFormat heading && heading.Matches(name,size,bold,italic,color,alignment);
                            if((string)target.Text!=styled.Expected || !matches)
                                throw new InvalidOperationException("Định dạng sau lưu chưa khớp gợi ý hoặc vùng chữ bị thay đổi; phải khôi phục cả lượt.");
                        }
                        finally{Release(paragraphFormat);Release(font);}
                    }
                },()=>
                {
                    // Close this private Word instance, including a disconnected COM server.
                    foreach(var styled in formatRanges)Release(styled.Range);
                    formatRanges.Clear();
                    SwapCheckpoint(RecoveryPath,before,CloseConnection,Connect);
                    if(ReadStoryText()!=originalStory)
                        throw new InvalidOperationException("Chưa xác minh được chữ sau phục hồi checkpoint.");
                    restored=true;
                });
                undoCheckpoints.Push(before); // Only a fully committed action enters the Undo stack.
                Changed=true;snapshotCurrent=true;
            }
            catch(Exception error)
            {
                Changed=restored || !started?textPreviousChanged:true;
                snapshotCurrent=restored || !started;
                Diagnostics.Write(restored?"Word.TextBatch.RolledBack":"Word.TextBatch.Failed",error,
                    $"Selected={plan.Count}; Applied={applied}; Restored={(restored?1:0)}");
                if(error is AggregateException aggregate && aggregate.InnerExceptions.Count==2)
                {
                    Diagnostics.Write("Word.TextBatch.FailureCause",aggregate.InnerExceptions[0]);
                    Diagnostics.Write("Word.TextBatch.RestoreCause",aggregate.InnerExceptions[1]);
                }
                throw new InvalidOperationException(restored
                    ? "Lượt sửa chưa hoàn tất; đã khôi phục bản trước lượt sửa. Hãy quét lại trước khi sửa tiếp. Checkpoint: "+before
                    : "Lượt sửa chưa hoàn tất; giữ bản trước để phục hồi: "+before,error);
            }
        }
        finally{foreach(var styled in formatRanges)Release(styled.Range);}
    }

    private void Checkpoint()
    {
        document!.Save();
        snapshotCurrent = true;
    }
    public void Undo() => UndoCore();
    private CheckpointCleanup? UndoCore(Action<string>? deleteRollback=null)
    {
        if (undoCheckpoints.Count == 0) return null;
        string before = undoCheckpoints.Peek();
        verifiedLineEdits.Clear();
        snapshotCurrent = false;
        try
        {
            var cleanup=SwapCheckpoint(RecoveryPath,before,CloseDocumentOnly,Connect,deleteRollback);
            undoCheckpoints.Pop();
            Changed = true;
            snapshotCurrent = true;
            return cleanup;
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
        // Word CPs and Range.Text string indices are different coordinate systems.
        // Read the actual prefix at each distinct CP; never navigate with Document.GoTo.
        dynamic content = document!.Content;
        int contentStart;
        string text;
        try { contentStart = (int)content.Start; text = (string)content.Text; }
        finally { Release(content); }
        var prefixes=new Dictionary<int,string>();
        foreach (var issue in issues)
        {
            dynamic point = document.Range(issue.Start, issue.Start);
            int page;
            try { page = (int)point.Information[3]; } finally { Release(point); }
            if(!prefixes.TryGetValue(issue.Start,out string? prefix))
            {
                dynamic range=document.Range(contentStart,issue.Start);
                try{prefix=Convert.ToString((object?)range.Text)??"";}
                finally{Release(range);}
                prefixes.Add(issue.Start,prefix);
            }
            if(prefix.Length!=issue.Start-contentStart)
                Diagnostics.Write("Word.Preview.CpMapping",null,FormattableString.Invariant(
                    $"Kind={issue.Kind}; CP={issue.Start}; StoryStart={contentStart}; StringOffset={prefix.Length}; Length={issue.Length}"));
            try{anchors[issue]=WordPreviewAnchor.Create(page,text,prefix,issue.Original);}
            catch(InvalidOperationException)
            {
                // A missing anchor becomes a manual Line notice in the UI and blocks candidate acceptance.
                Diagnostics.Write("Word.Preview.Unmapped",null,FormattableString.Invariant(
                    $"Kind={issue.Kind}; CP={issue.Start}; StoryLength={text.Length}; PrefixLength={prefix.Length}; Length={issue.Length}"));
            }
        }
        string output = Path.Combine(folder, Guid.NewGuid() + ".xps");
        document.ExportAsFixedFormat(output, 18, OpenAfterExport: false);
        return output;
    }
    public void SaveCopy(string path) => SaveCopyCore(path);
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


// Deliberate failure used only by the isolated Office check; never enabled by UI commands.
internal sealed class OfficeTextProbeException() : InvalidOperationException("OFFICE-CHECK-INJECTED-TEXT-FAILURE") { }
