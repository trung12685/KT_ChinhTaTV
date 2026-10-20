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
    public string Source { get; private set; } = "";
    public bool Changed { get; private set; }
    public bool IsOpen => document is not null;
    public WordSession() => Directory.CreateDirectory(folder);
    private static void Release(object? value)
    {
        if (value is not null && Marshal.IsComObject(value)) Marshal.FinalReleaseComObject(value);
    }
    public void Open(string path)
    {
        DisposeDocument();
        app ??= Activator.CreateInstance(Type.GetTypeFromProgID("Word.Application")
            ?? throw new InvalidOperationException("Cần cài Microsoft Word desktop trên Windows."));
        app!.Visible = false;
        app.DisplayAlerts = 0;
        app.AutomationSecurity = 3; // Never enable macros in documents opened by this instance.
        var working = Path.Combine(folder, "working.docx");
        File.Copy(path, working, true);
        dynamic docs = app.Documents;
        try { document = docs.Open(working, ReadOnly: false, AddToRecentFiles: false, Visible: false); }
        finally { Release(docs); }
        if ((bool)document!.TrackRevisions || (int)document.ProtectionType != -1)
        {
            DisposeDocument();
            throw new InvalidOperationException("Bản thử chưa hỗ trợ Track Changes hoặc tài liệu được bảo vệ.");
        }
        Source = Path.GetFullPath(path);
        Changed = false;
    }
    public IReadOnlyList<Issue> Check()
    {
        var paragraphs = new List<Paragraph>();
        dynamic ps = document!.Paragraphs;
        try
        {
            for (int i = 1; i <= (int)ps.Count; i++)
            {
                dynamic p = ps[i]; dynamic r = p.Range; dynamic f = r.Font;
                dynamic format = r.ParagraphFormat;
                try { paragraphs.Add(new((int)r.Start, (string)r.Text, (int)f.Bold == -1,
                    (float)f.Size, (int)format.Alignment, (string?)f.Name ?? "")); }
                finally { Release(format); Release(f); Release(r); Release(p); }
            }
        }
        finally { Release(ps); }
        return Checker.Check(paragraphs);
    }
    public void Fix(Issue issue)
    {
        dynamic r = document!.Range(issue.Start, issue.Start + issue.Length);
        dynamic undo = app!.UndoRecord;
        try
        {
            if ((string)r.Text != issue.Original) throw new InvalidOperationException("Vị trí lỗi đã thay đổi. Hãy quét lại.");
            undo.StartCustomRecord("Sửa lỗi chính tả");
            try
            {
                if ((string)r.Text != issue.Replacement) r.Text = issue.Replacement;
                if (issue.Kind == FixKind.NationalHeading)
                {
                    dynamic f = r.Font; dynamic format = r.ParagraphFormat;
                    try { f.Bold = -1; f.Size = 13; f.Name = "Times New Roman"; format.Alignment = 1; }
                    finally { Release(f); Release(format); }
                }
                Changed = true;
            }
            finally { undo.EndCustomRecord(); }
        }
        finally { Release(undo); Release(r); }
    }
    public void Undo() { if ((bool)document!.Undo()) Changed = true; }
    public string ExportPreview(Issue? issue)
    {
        // Highlight a separate throwaway document, never the editable working document.
        document!.Save();
        string copy = Path.Combine(folder, "preview.docx");
        File.Copy(Path.Combine(folder, "working.docx"), copy, true);
        dynamic docs = app!.Documents;
        dynamic? preview = null;
        try
        {
            preview = docs.Open(copy, ReadOnly: false, AddToRecentFiles: false, Visible: false);
            if (issue is not null)
            {
                dynamic r = preview.Range(issue.Start, issue.Start + issue.Length);
                try { r.HighlightColorIndex = 7; } finally { Release(r); }
            }
            string output = Path.Combine(folder, Guid.NewGuid() + ".xps");
            preview.ExportAsFixedFormat(output, 18, OpenAfterExport: false);
            return output;
        }
        finally
        {
            if (preview is not null) { try { preview.Close(0); } finally { Release(preview); } }
            Release(docs);
        }
    }
    public int Page(Issue issue)
    {
        dynamic r = document!.Range(issue.Start, issue.Start);
        try { return (int)r.Information[3]; } finally { Release(r); }
    }
    public void SaveCopy(string path)
    {
        if (string.Equals(Path.GetFullPath(path), Source, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Hãy chọn tên khác để giữ nguyên tài liệu gốc.");
        document!.Save();
        File.Copy(Path.Combine(folder, "working.docx"), path, true);
        Changed = false;
    }
    private void DisposeDocument()
    {
        if (document is null) return;
        try { document.Close(0); } finally { Release(document); document = null; }
    }
    public void Dispose()
    {
        try { DisposeDocument(); }
        finally
        {
            if (app is not null) { try { app.Quit(0); } finally { Release(app); app = null; } }
            try { Directory.Delete(folder, true); } catch (IOException) { }
        }
    }
}
