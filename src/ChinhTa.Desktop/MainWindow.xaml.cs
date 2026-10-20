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
    private readonly DispatcherTimer hover = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private XpsDocument? xps;
    private string? xpsPath;
    private Issue? pending;
    private Issue? displayed;
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
        Run(() => { ClosePreview(); Issues.ItemsSource = null; session.Open(dialog.FileName); Scan(); });
    }
    private void ScanClick(object sender, RoutedEventArgs e) { if (session.IsOpen) Run(Scan); }
    private void Scan()
    {
        pending = displayed = null;
        var found = session.Check();
        Issues.ItemsSource = found;
        ShowPreview(null);
        Status.Text = $"{Path.GetFileName(session.Source)} — {found.Count} cảnh báo. Bản thử dùng quy tắc, chưa tích hợp AI.";
    }
    private void ClosePreview()
    {
        Preview.Document = null;
        xps?.Close(); xps = null;
        if (xpsPath is not null) { File.Delete(xpsPath); xpsPath = null; }
    }
    private void ShowPreview(Issue? issue)
    {
        if (issue is not null && issue == displayed) return;
        ClosePreview();
        xpsPath = session.ExportPreview(issue);
        xps = new XpsDocument(xpsPath, FileAccess.Read);
        Preview.Document = xps.GetFixedDocumentSequence();
        displayed = issue;
        if (issue is not null) Preview.GoToPage(session.Page(issue));
    }
    private void Queue(Issue issue) { pending = issue; hover.Stop(); hover.Start(); }
    private void IssueHover(object sender, MouseEventArgs e)
    { if (sender is FrameworkElement { DataContext: Issue issue }) Queue(issue); }
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
