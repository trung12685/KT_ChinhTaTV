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
    public MainWindow()
    {
        InitializeComponent();
        hover.Tick += (_, _) => { hover.Stop(); if (pending is not null) Run(() => ShowPreview(pending)); };
    }
    private void Run(Action action)
    {
        hover.Stop();
        Mouse.OverrideCursor = Cursors.Wait;
        try { action(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning); }
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
        Issues.ItemsSource = found;
        Status.Text = $"{Path.GetFileName(session.Source)} — {found.Count} cảnh báo. " +
            $"Quét: {checkSeconds:F1}s; dựng trang: {timer.Elapsed.TotalSeconds - checkSeconds:F1}s. " +
            "Chọn hoặc rê chuột lên lỗi để đánh dấu riêng, không xuất lại Word.";
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
        if (sender is FrameworkElement { DataContext: Issue issue })
        { Issues.SelectedItem = issue; Queue(issue); }
    }
    private void IssueSelected(object sender, SelectionChangedEventArgs e)
    { if (Issues.SelectedItem is Issue issue) Queue(issue); }
    private void FixClick(object sender, RoutedEventArgs e)
    {
        if (Issues.SelectedItem is not Issue issue) return;
        if (MessageBox.Show(this, $"{issue.Message}\n\nThay bằng: {issue.Replacement}?", "Duyệt sửa",
            MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        Run(() => { session.Fix(issue); Scan(); });
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
