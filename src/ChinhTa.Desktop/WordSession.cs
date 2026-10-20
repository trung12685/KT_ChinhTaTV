using System.IO;
using System.Runtime.InteropServices;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

// All calls stay on the WPF STA thread. Only this private Word instance is closed.
public sealed class WordSession : IDisposable
{
    private dynamic? app;
    private dynamic? document;
    private readonly string folder = Path.Combine(Path.GetTempPath(), "ChinhTa-" + Guid.NewGuid());
    private bool snapshotCurrent = true;
    public IReadOnlyList<LineTarget> LineTargets { get; private set; } = [];
    public IReadOnlyList<WordLineShape> LineShapes { get; private set; } = [];
    public bool HasGroupedShapes { get; private set; }
    public DocumentProfile Profile { get; set; } = DocumentProfile.State;
    public bool Recovered { get; private set; }
    public string RecoveryPath => Path.Combine(folder, "working.docx");
    public string Source { get; private set; } = "";
    public bool Changed { get; private set; }
    public bool IsOpen => document is not null;
    public WordSession() => Directory.CreateDirectory(folder);
    // Balance this acquisition only; child and parent COM wrappers can share identity.
    private static void Release(object? value)
    {
        if (value is not null && Marshal.IsComObject(value)) Marshal.ReleaseComObject(value);
    }
    public void Open(string path)
    {
        // Do not reuse an RCW from a Word process that has already exited.
        CloseConnection();
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
        LineTargets = lineLayoutUnavailable ? [] : ShapeLineRules.Identify(headerLayout, Profile);
        LineShapes = ReadLineShapes();
        var issues = Checker.Check(paragraphs, Profile).ToList();
        if ((lineLayoutUnavailable || mappingWarning is not null && LineTargets.Count < 3) && paragraphs.FirstOrDefault(p => p.Text.Length > 0) is Paragraph first)
            issues.Add(new(first.Start, first.Text.Length, first.Text, first.Text,
                "Chưa đối chiếu đủ các mốc đường kẻ. " +
                (mappingWarning is null ? "Không đọc được cấu trúc XML của tài liệu." : "Mã kiểm tra: " + mappingWarning.DiagnosticCode + ".") +
                " Các vùng đã xác minh vẫn được kiểm tra; vùng chưa xác minh cần kiểm tra thủ công.",
                false, FixKind.ShapeLine, CanFix: false, Title: "Chưa kiểm tra được đường kẻ"));
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
        bool centered;
        try { centered = (int)format.Alignment == 1 && Math.Abs((float)format.LeftIndent) < .1 &&
            Math.Abs((float)format.RightIndent) < .1 && Math.Abs((float)format.FirstLineIndent) < .1 && !text.Contains('\t') &&
            text.TrimEnd('\r', '\a', '\n') == text.TrimEnd('\r', '\a', '\n').Trim(); }
        finally { Release(format); }
        return new(start, (int)range.End, text, page, inTable, centered);
    }
    private IReadOnlyList<WordLineShape> ReadLineShapes()
    {
        HasGroupedShapes = false;
        if (LineTargets.Count == 0) return [];
        var result = new List<WordLineShape>();
        dynamic shapes = document!.Shapes;
        try
        {
            int count = (int)shapes.Count;
            for (int i = 1; i <= count; i++)
            {
                dynamic shape = shapes[i];
                try
                {
                    int type = (int)shape.Type;
                    if (type is 6 or 20) HasGroupedShapes = true;
                    if (type != 9) continue; // Office msoLine
                    LineGeometry? geometry = null;
                    if (((string?)shape.AlternativeText ?? "").StartsWith("ChinhTaViet.ShapeLine.", StringComparison.Ordinal) &&
                        (int)shape.RelativeHorizontalPosition == 1 && (int)shape.RelativeVerticalPosition == 1 &&
                        Math.Abs((float)shape.Height) <= .5f && Math.Abs((float)shape.Rotation) < .01f && (int)shape.Visible != 0)
                    {
                        dynamic line = shape.Line;
                        try
                        {
                            if ((int)line.Visible != 0) geometry = new((float)shape.Left, (float)shape.Top, (float)shape.Width);
                        }
                        finally { Release(line); }
                    }
                    dynamic anchor = shape.Anchor;
                    try { result.Add(new((int)shape.ID, (int)anchor.Start, (int)anchor.Information[3], geometry)); }
                    finally { Release(anchor); }
                }
                finally { Release(shape); }
            }
        }
        finally { Release(shapes); }
        return result;
    }
    private dynamic FindLine(int id)
    {
        dynamic shapes = document!.Shapes;
        try
        {
            int count = (int)shapes.Count;
            for (int i = 1; i <= count; i++)
            {
                dynamic shape = shapes[i];
                if ((int)shape.ID == id && (int)shape.Type == 9) return shape;
                Release(shape);
            }
        }
        finally { Release(shapes); }
        throw new InvalidOperationException("Shape Line đã thay đổi hoặc bị xóa. Hãy quét lại.");
    }
    private void ApplyLine(ShapeLineFix fix, dynamic anchor)
    {
        // Keep the owning collection alive until all Shape operations and cleanup finish.
        dynamic shapes = document!.Shapes;
        try
        {
            object? existing = fix.ShapeId is int id ? (object)FindLine(id) : null;
            WordLineWriter.Apply(existing, () => (object)shapes.AddLine((float)fix.Left, (float)fix.Top,
                (float)(fix.Left + fix.Width), (float)fix.Top, Anchor: anchor), fix, Release, Diagnostics.Write);
        }
        finally { Release(shapes); }
    }
    public void Fix(Issue issue) => FixMany([issue]);
    public void FixMany(IEnumerable<Issue> issues)
    {
        var plan = BatchPlan.Create(issues);
        if (plan.Count == 0) return;
        if (plan.Any(i => i.LineFix is not null) && plan.Any(i => i.LineFix is null))
            throw new InvalidOperationException("Sửa chữ/định dạng trước, quét lại rồi sửa nhóm Shape Line để dùng số đo mới.");
        foreach (var item in plan)
            if (item.LineFix?.ShapeId is int id) { dynamic existing = FindLine(id); Release(existing); }
        // Validate every original before making any changes, not halfway through the batch.
        BatchPlan.Validate(plan, issue =>
        {
            dynamic range = document!.Range(issue.Start, issue.Start + issue.Length);
            try { return (string)range.Text; } finally { Release(range); }
        });
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
                        if (issue.LineFix is ShapeLineFix lineFix) { ApplyLine(lineFix, r); continue; }
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
        snapshotCurrent = false;
        if ((bool)document!.Undo()) Changed = true;
        Checkpoint();
    }
    public string ExportPreview(IReadOnlyList<Issue> issues, IDictionary<Issue, PreviewAnchor> anchors)
        => ReadWithRecovery("Word.ExportPreview", () => ExportPreviewCore(issues, anchors));
    private string ExportPreviewCore(IReadOnlyList<Issue> issues, IDictionary<Issue, PreviewAnchor> anchors)
    {
        anchors.Clear();
        // Export the unmodified document once. Selection highlighting belongs exclusively to WPF.
        document!.Repaginate();
        // Read the story once; do not navigate Word's invisible window with Document.GoTo.
        // Each issue retains its absolute source offset plus the physical page number.
        dynamic content = document.Content;
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
