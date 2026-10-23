using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Xps.Packaging;
using ChinhTa.Core;
using Microsoft.Win32;

namespace ChinhTa.Desktop;
public partial class MainWindow : Window
{
    private readonly WordSession session = new();
    private readonly DispatcherTimer hover = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private XpsDocument? xps;
    private string? xpsPath;
    private Issue? pending;
    private Issue? displayed;
    private readonly Dictionary<Issue, PreviewAnchor> anchors = new();
    private readonly PreviewHighlighter highlighter = new();
    private IReadOnlyList<IssueChoice> choices=>Review.Choices;
    public MainWindow()
    {
        InitializeComponent();
        Review.ActiveIssueChanged+=Queue;
        ProfilePicker.SelectedIndex = 0;
        hover.Tick += (_, _) => { hover.Stop(); if (pending is not null) Run(() => ShowPreview(pending)); };
    }
    private void ProfileChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProfilePicker.SelectedIndex < 0) return;
        session.Profile = (DocumentProfile)ProfilePicker.SelectedIndex;
        if (ProfileInfo is not null) ProfileInfo.Text = DocumentProfiles.Description(session.Profile);
        if (session.IsOpen) Run(Scan);
    }
    private void Run(Action action)
    {
        hover.Stop();
        Mouse.OverrideCursor = Cursors.Wait;
        try { action(); }
        catch (Exception ex)
        {
            Diagnostics.Write("UI.Operation", ex);
            if(ex is ScanAfterEditException && ex.InnerException is Exception cause)
                Diagnostics.Write("UI.ScanAfterEdit.Cause",cause);
            pending = displayed = null;
            anchors.Clear();
            highlighter.Clear();
            Review.LoadIssues([]);
            Status.Text=ex is ScanAfterEditException?ex.Message:
                "Thao tác chưa hoàn tất. Danh sách lỗi cũ đã được xóa; hãy Quét lại trước khi sửa tiếp.";
            string message = ex is ScanAfterEditException?ex.Message:WordConnectionPolicy.IsDisconnected(ex)
                ? "Ứng dụng mất kết nối với Microsoft Word. Hãy chọn Kiểm tra → Quét lại để thử khôi phục bản lưu gần nhất. " +
                  "Không cần đóng các cửa sổ Word khác. Nếu lỗi xảy ra ngay khi mở, hãy mở lại file."
                : ex.Message;
            MessageBox.Show(this, message + "\n\nBản làm việc: " + session.RecoveryPath +
                "\nNhật ký: " + Diagnostics.LogPath, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally { Mouse.OverrideCursor = null; }
    }
    private bool CanDiscard() => !session.Changed || MessageBox.Show(this,
        "Có thay đổi chưa lưu bản sao. Bạn muốn bỏ các thay đổi này?", "Xác nhận",
        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    private void OpenClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Word (*.docx)|*.docx" };
        if (dialog.ShowDialog(this) != true || !CanDiscard()) return;
        Run(() => { pending = displayed = null; anchors.Clear(); ClosePreview(); Review.LoadIssues([]); session.Open(dialog.FileName); Scan(); });
    }
    private void ScanClick(object sender, RoutedEventArgs e) { if (session.IsOpen) Run(Scan); }
    private void Scan()
    {
        hover.Stop();pending = displayed = null;
        Review.LoadIssues([]);
        var timer = Stopwatch.StartNew();
        var found = session.Check().ToList();
        double checkSeconds = timer.Elapsed.TotalSeconds;
        ClosePreview();
        anchors.Clear();
        var positionProbes=session.AdministrativePositionTargets.SelectMany(t=>t.References.Select(r=>r.Probe).Append(t.Probe));
        xpsPath = session.ExportPreview(found.Concat(session.LineTargets.Select(t => t.Probe)).Concat(positionProbes).Distinct().ToArray(), anchors);
        xps = new XpsDocument(xpsPath, FileAccess.Read);
        Preview.Document = xps.GetFixedDocumentSequence();
        if (Preview.Document is System.Windows.Documents.FixedDocumentSequence sequence)
        {
            var policy = LinePolicy.Decree30;
            foreach (var entry in LineInspection.Inspect(sequence, highlighter, session.LineTargets, session.LineShapes,
                anchors, found, policy, session.HasGroupedShapes, session.VerifiedLineEdits))
            { found.Add(entry.Issue); anchors[entry.Issue] = entry.Anchor; }
            foreach(var entry in AdministrativePositionInspection.Inspect(sequence,highlighter,session.AdministrativePositionTargets,anchors))
            {found.Add(entry.Issue);anchors[entry.Issue]=entry.Anchor;}
        }
        Review.LoadIssues(found,anchors);
        Status.Text = $"{Path.GetFileName(session.Source)} — {found.Count} cảnh báo. " +
            $"Quét: {checkSeconds:F1}s; dựng trang: {timer.Elapsed.TotalSeconds - checkSeconds:F1}s. " +
            "Chọn hoặc rê chuột lên lỗi để đánh dấu riêng, không xuất lại Word." +
            (session.Recovered ? " Đã khôi phục kết nối Word từ bản lưu gần nhất." : "");
    }
    private void ClosePreview()
    {
        highlighter.Reset();
        Preview.Document = null;
        xps?.Close(); xps = null;
        if (xpsPath is not null) { File.Delete(xpsPath); xpsPath = null; }
    }
    private void ShowPreview(Issue? issue)
    {
        if (issue is null || issue == displayed || !anchors.TryGetValue(issue, out var anchor)) return;
        if (Preview.Document is not System.Windows.Documents.FixedDocumentSequence document) return;
        var timer = Stopwatch.StartNew();
        Preview.GoToPage(anchor.Page);
        bool pageWarning = issue.Kind == FixKind.PageLayout;
        if (pageWarning) highlighter.Clear();
        bool marked = !pageWarning && highlighter.Show(document, anchor);
        if (issue.LineFix is ShapeLineFix lineFix) highlighter.ShowLineSuggestion(document,anchor.Page,lineFix);
        displayed = issue;
        Status.Text = $"Trang {anchor.Page} — {IssueReviewCatalog.Describe(issue).Label}. " +
            (pageWarning ? "Cảnh báo thiết lập trang, không đánh dấu ký tự. " : marked ? $"Đánh dấu: {timer.Elapsed.TotalMilliseconds:F0} ms." :
            "Chưa ánh xạ chắc chắn được vị trí trên trang này; không đánh dấu để tránh nhầm.") + (issue.LineFix is not null ? " Vạch cam là vị trí đề xuất, chưa ghi vào Word." : "");
    }
    private void Queue(Issue issue) { pending = issue; hover.Stop(); hover.Start(); }
    private void FixClick(object sender, RoutedEventArgs e)
    {
        if (Review.SelectedIssue is not Issue issue) return;
        if (!issue.CanFix)
        {
            MessageBox.Show(this, "Mục này chỉ cảnh báo để kiểm tra thủ công.\n\n" + issue.Message + "\n" + issue.Citation,
                "Kiểm tra thể thức");
            return;
        }
        ConfirmFix([issue]);
    }
    private void FixCheckedClick(object sender, RoutedEventArgs e) =>
        ConfirmFix(choices.Where(x => x.IsChecked).Select(x => x.Issue).ToArray());
    private void FixAllClick(object sender, RoutedEventArgs e)
    {
        var all = choices.Where(x => x.Issue.CanFix).Select(x => x.Issue).ToArray();
        bool hasText=all.Any(x=>x.LineFix is null && x.CenterFix is null);
        bool hasCenter=all.Any(x=>x.CenterFix is not null);
        if((hasText && (hasCenter || all.Any(x=>x.LineFix is not null))) || (hasCenter && all.Any(x=>x.LineFix is not null)))
        {
            MessageBox.Show(this, "Sửa theo thứ tự: chữ/phông → căn giữa số/ngày → Shape Line. Lượt này sửa nhóm đầu còn lỗi; sau quét lại, bấm Sửa tất cả để tiếp tục theo số đo mới.", "Cập nhật số đo");
            all=all.Where(x=>hasText?x.LineFix is null && x.CenterFix is null:x.CenterFix is not null).ToArray();
        }
        ConfirmFix(all);
    }
    private void ConfirmFix(IReadOnlyList<Issue> selected)
    {
        hover.Stop();
        if (selected.Count == 0)
        {
            MessageBox.Show(this, "Chưa chọn lỗi nào. Tích ô bên cạnh các lỗi cần sửa.", "Chọn lỗi");
            return;
        }
        int Stage(Issue i)=>i.LineFix is not null?2:i.CenterFix is not null?1:0;
        if(selected.Select(Stage).Distinct().Count()>1)
        {
            MessageBox.Show(this, "Chọn riêng một nhóm: chữ/phông, căn giữa số/ngày hoặc Shape Line. Sửa theo thứ tự này và quét lại để dùng số đo mới.", "Sửa theo từng lượt");
            return;
        }
        string examples = string.Join("\n", selected.Take(8).Select(x =>
            $"• {x.Original.Replace("\r", " ")} → {x.Suggestion}"));
        string more = selected.Count > 8 ? $"\n… và {selected.Count - 8} lỗi khác." : "";
        int manual = choices.Count(x => !x.Issue.CanFix);
        string manualNote = manual > 0 ? $"\n{manual} cảnh báo thủ công sẽ không bị sửa." : "";
        string caution = selected.Any(x => !x.BatchSafe)
            ? "\nCó gợi ý cần bạn duyệt (ngữ cảnh, dàn trang hoặc thể thức); có thể cảnh báo nhầm." : "";
        if (MessageBox.Show(this, $"Áp dụng {selected.Count} gợi ý sửa?\n\n{examples}{more}{caution}{manualNote}\n\n" +
            "Hoàn tác một lần để bỏ cả lượt sửa. Tài liệu gốc được giữ nguyên.", "Duyệt lượt sửa",
            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        Run(() => { EditThenScan.Run(()=>session.FixMany(selected),Scan); Status.Text = $"Đã áp dụng {selected.Count} gợi ý. " + Status.Text; });
    }
    private void OfficeCheckClick(object sender, RoutedEventArgs e) => Run(() =>
    {
        Status.Text = "Đang kiểm tra Word bằng tài liệu mẫu riêng: mở, ghi Line, xuất trang, lưu và hoàn tác…";
        var result = OfficeIntegrationCheck.Run();
        Status.Text = result.Passed ? "Kiểm tra Word tự động: ĐẠT." : "Kiểm tra Word tự động: CHƯA ĐẠT; xem office-check.txt.";
        MessageBox.Show(this, (result.Passed ? "Đã đạt các bước kiểm tra Word trên mẫu riêng." :
            "Kiểm tra Word chưa đạt ở bước: " + result.Stage) +
            "\n\nKết quả và file mẫu đã sửa lưu tại:\n" + result.Folder +
            "\n\nBài kiểm tra đo và kiểm nét XPS trên mẫu; không xác nhận mọi tài liệu.",
            "Kiểm tra Word", MessageBoxButton.OK, result.Passed ? MessageBoxImage.Information : MessageBoxImage.Warning);
        Process.Start(new ProcessStartInfo { FileName = result.Folder, UseShellExecute = true });
    });
    private void ProgressReportClick(object sender,RoutedEventArgs e)
    {
        hover.Stop();
        // This independent report must not scan Word or clear the document issue list.
        var report=new ProgressReportWindow(session.SaveProgressReport) {Owner=this};
        report.ShowDialog();
    }
    private void DiagnosticsFolderClick(object sender, RoutedEventArgs e)
    {
        string folder = Path.GetDirectoryName(Diagnostics.LogPath)!;
        Directory.CreateDirectory(folder);
        Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
    }
    private void UndoClick(object sender, RoutedEventArgs e)
    { if (session.IsOpen) Run(() => { session.Undo(); Scan(); }); }
    private void SaveClick(object sender, RoutedEventArgs e)
    {
        if (!session.IsOpen) return;
        var dialog = new SaveFileDialog { Filter = "Word (*.docx)|*.docx", FileName = Path.GetFileNameWithoutExtension(session.Source) + "_da-sua.docx" };
        if (dialog.ShowDialog(this) == true) Run(() => { session.SaveCopy(dialog.FileName); Status.Text = "Đã lưu bản sao: " + dialog.FileName; });
    }
    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!CanDiscard()) { e.Cancel = true; return; }
        hover.Stop();
        Run(() => { ClosePreview(); session.Dispose(); });
    }
}

