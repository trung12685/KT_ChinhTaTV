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
    private IReadOnlyList<IssueChoice> choices = [];
    public MainWindow()
    {
        InitializeComponent();
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
            pending = displayed = null;
            anchors.Clear();
            highlighter.Clear();
            choices = [];
            Issues.ItemsSource = null;
            UpdateCheckedCount();
            string message = WordConnectionPolicy.IsDisconnected(ex)
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
        Run(() => { pending = displayed = null; anchors.Clear(); ClosePreview(); Issues.ItemsSource = null; session.Open(dialog.FileName); Scan(); });
    }
    private void ScanClick(object sender, RoutedEventArgs e) { if (session.IsOpen) Run(Scan); }
    private void Scan()
    {
        pending = displayed = null;
        var timer = Stopwatch.StartNew();
        var found = session.Check();
        double checkSeconds = timer.Elapsed.TotalSeconds;
        ClosePreview();
        anchors.Clear();
        xpsPath = session.ExportPreview(found, anchors);
        xps = new XpsDocument(xpsPath, FileAccess.Read);
        Preview.Document = xps.GetFixedDocumentSequence();
        choices = found.Select(issue => new IssueChoice(issue)).ToArray();
        Issues.ItemsSource = choices;
        UpdateCheckedCount();
        Status.Text = $"{Path.GetFileName(session.Source)} — {found.Count} cảnh báo. " +
            $"Quét: {checkSeconds:F1}s; dựng trang: {timer.Elapsed.TotalSeconds - checkSeconds:F1}s. " +
            "Chọn hoặc rê chuột lên lỗi để đánh dấu riêng, không xuất lại Word." +
            (session.Recovered ? " Đã khôi phục kết nối Word; lịch sử hoàn tác trước khôi phục không còn." : "");
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
        bool marked = highlighter.Show(document, anchor);
        displayed = issue;
        Status.Text = $"Trang {anchor.Page} — {issue.Message} Gợi ý: {issue.Replacement}. " +
            (marked ? $"Đánh dấu: {timer.Elapsed.TotalMilliseconds:F0} ms." :
            "Chưa ánh xạ chắc chắn được vị trí trên trang này; không đánh dấu để tránh nhầm.");
    }
    private void Queue(Issue issue) { pending = issue; hover.Stop(); hover.Start(); }
    private void IssueHover(object sender, MouseEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IssueChoice choice })
        { Issues.SelectedItem = choice; Queue(choice.Issue); }
    }
    private void IssueSelected(object sender, SelectionChangedEventArgs e)
    { if (Issues.SelectedItem is IssueChoice choice) Queue(choice.Issue); }
    private void FixClick(object sender, RoutedEventArgs e)
    {
        if (Issues.SelectedItem is IssueChoice choice) ConfirmFix([choice.Issue]);
    }
    private void FixCheckedClick(object sender, RoutedEventArgs e) =>
        ConfirmFix(choices.Where(x => x.IsChecked).Select(x => x.Issue).ToArray());
    private void FixAllClick(object sender, RoutedEventArgs e) =>
        ConfirmFix(choices.Select(x => x.Issue).ToArray());
    private void ConfirmFix(IReadOnlyList<Issue> selected)
    {
        hover.Stop();
        if (selected.Count == 0)
        {
            MessageBox.Show(this, "Chưa chọn lỗi nào. Tích ô bên cạnh các lỗi cần sửa.", "Chọn lỗi");
            return;
        }
        string examples = string.Join("\n", selected.Take(8).Select(x =>
            $"• {x.Original.Replace("\r", " ")} → {x.Replacement}"));
        string more = selected.Count > 8 ? $"\n… và {selected.Count - 8} lỗi khác." : "";
        string caution = selected.Any(x => !x.BatchSafe)
            ? "\nCó gợi ý cần bạn duyệt (ngữ cảnh, dàn trang hoặc thể thức); có thể cảnh báo nhầm." : "";
        if (MessageBox.Show(this, $"Áp dụng {selected.Count} gợi ý sửa?\n\n{examples}{more}{caution}\n\n" +
            "Hoàn tác một lần để bỏ cả lượt sửa. Tài liệu gốc được giữ nguyên.", "Duyệt lượt sửa",
            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        Run(() => { session.FixMany(selected); Scan(); Status.Text = $"Đã áp dụng {selected.Count} gợi ý. " + Status.Text; });
    }
    private void UpdateCheckedCount()
    {
        if (CheckedCount is not null) CheckedCount.Text = $"Đã tích: {choices.Count(x => x.IsChecked)}/{choices.Count} lỗi";
    }
    private void CheckChanged(object sender, RoutedEventArgs e) =>
        Dispatcher.BeginInvoke(new Action(UpdateCheckedCount));
    private void CheckAllClick(object sender, RoutedEventArgs e) => SetChecked(true);
    private void UncheckAllClick(object sender, RoutedEventArgs e) => SetChecked(false);
    private void SetChecked(bool value)
    {
        foreach (var choice in choices) choice.IsChecked = value;
        UpdateCheckedCount();
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
