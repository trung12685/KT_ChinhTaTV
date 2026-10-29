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
    private DocumentCoverageSnapshot? lastCoverage;
    private readonly Dictionary<Issue, PreviewAnchor> anchors = new();
    private readonly PreviewHighlighter highlighter = new();
    private IReadOnlyList<IssueChoice> choices=>Review.Choices;
    public MainWindow()
    {
        InitializeComponent();
        string version=typeof(MainWindow).Assembly.GetName().Version?.ToString(3)??"chưa rõ";
        Title="Chính tả Việt — Bản thử nghiệm "+version;BuildVersion.Text="Bản "+version;
        Diagnostics.Write("UI.Start",null,$"DesktopModule={typeof(MainWindow).Module.ModuleVersionId}; CoreModule={typeof(Checker).Module.ModuleVersionId}");
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
            var cleanupFailure=InvalidateReview();
            if(cleanupFailure is not null)Diagnostics.Write("UI.Operation.PreviewCleanup",cleanupFailure);
            var feedback=DescribeOperationFailure(session,ex);
            Status.Text=feedback.Status;
            string cleanupNotice=cleanupFailure is null?"":"\n\nChưa dọn hết bản xem trước. Các lỗi dọn dẹp và lỗi thao tác ban đầu đều đã được giữ trong nhật ký.";
            MessageBox.Show(this, feedback.Message + cleanupNotice + "\n\nBản làm việc: " + session.RecoveryPath +
                "\nNhật ký: " + Diagnostics.LogPath, "Không thể thực hiện", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally { Mouse.OverrideCursor = null; }
    }
    internal const string UnavailableRecoveryNotice="Phiên tài liệu đã đóng và chưa có bản làm việc đủ điều kiện mở lại tự động. " +
        "Quét lại hiện chưa thể chạy. Hãy giữ các tệp trong thư mục bản làm việc và nhật ký để phục hồi thủ công.";
    internal sealed record OperationFailureFeedback(string Status,string Message);
    internal static OperationFailureFeedback DescribeOperationFailure(WordSession current,Exception error)
    {
        ArgumentNullException.ThrowIfNull(current);ArgumentNullException.ThrowIfNull(error);
        if(!current.IsOpen && !current.CanRecover && current.Source.Length>0)
        {
            string completed=error is ScanAfterEditException reviewed?reviewed.CompletedActionNotice:
                "Thao tác chưa hoàn tất. Danh sách lỗi cũ đã được xóa.";
            string primary=error is ScanAfterEditException afterReview?afterReview.InnerException?.Message??afterReview.Message:error.Message;
            return new(completed+" "+UnavailableRecoveryNotice,
                (error is ScanAfterEditException?completed+"\n\nNguyên nhân quét lại: ":"")+primary+"\n\n"+UnavailableRecoveryNotice);
        }
        string status=error is ScanAfterEditException?error.Message:
            "Thao tác chưa hoàn tất. Danh sách lỗi cũ đã được xóa; hãy Quét lại trước khi sửa tiếp.";
        string message=error is ScanAfterEditException?error.Message:WordConnectionPolicy.IsDisconnected(error)?
            "Ứng dụng mất kết nối với Microsoft Word. Hãy chọn Kiểm tra → Quét lại để thử khôi phục bản lưu gần nhất. " +
            "Không cần đóng các cửa sổ Word khác. Nếu lỗi xảy ra ngay khi mở, hãy mở lại file.":error.Message;
        if(error.Data["ChinhTa.RecoveryInstruction"] is string recoveryInstruction)message+="\n\n"+recoveryInstruction;
        return new(status,message);
    }
    private bool CanDiscard() => !session.Changed || MessageBox.Show(this,
        "Có thay đổi chưa lưu bản sao. Bạn muốn bỏ các thay đổi này?", "Xác nhận",
        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    private void OpenClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Word (*.docx)|*.docx" };
        if (dialog.ShowDialog(this) != true || !CanDiscard()) return;
        Run(() =>
        {
            using var timing=new WordOperationTiming("Word.Perf.OpenUI");var total=Stopwatch.StartNew();
            pending=displayed=null;anchors.Clear();ClosePreview();Review.LoadIssues([]);lastCoverage=null;
            session.Open(dialog.FileName);double openSeconds=total.Elapsed.TotalSeconds;timing.Mark("OpenWordDocument");
            Scan();timing.Mark("ScanAndPreview");timing.Count("Completed",1);
            Status.Text+=$" Mở Word/tệp: {openSeconds:F1}s; tổng mở: {total.Elapsed.TotalSeconds:F1}s.";
        });
    }
    private void ScanClick(object sender, RoutedEventArgs e)
    {
        if(!session.IsOpen && !session.CanRecover)
        {
            if(session.Source.Length>0)
            {
                hover.Stop();var failure=InvalidateReview();
                if(failure is not null)Diagnostics.Write("UI.ScanUnavailable.PreviewCleanup",failure);
                Status.Text=UnavailableRecoveryNotice+"\nBản làm việc: "+session.RecoveryPath+"\nNhật ký: "+Diagnostics.LogPath;
            }
            return;
        }
        Run(()=>{if(!session.IsOpen)session.ReopenPreservedDocument();Scan();});
    }
    private void CoverageClick(object sender,RoutedEventArgs e)
    {
        hover.Stop();
        if(!session.IsOpen || lastCoverage is null)
        {MessageBox.Show(this,"Mở tài liệu và quét thành công để xem phạm vi của lần kiểm hiện tại.","Phạm vi kiểm tra");return;}
        new DocumentCoverageWindow(lastCoverage){Owner=this}.ShowDialog();
    }
    private void SpacingMeasurementClick(object sender,RoutedEventArgs e)
    {
        hover.Stop();
        if(!session.IsOpen){MessageBox.Show(this,"Hãy mở và quét tài liệu trước khi đo.","Đo giãn dòng");return;}
        new SpacingCalibrationWindow(session.SpacingDiagnosticTargets(),session.MeasureSpacing){Owner=this}.ShowDialog();
    }
    private void ReadingModeClick(object sender,RoutedEventArgs e)
    {
        session.UseOptimizedReading=((MenuItem)sender).IsChecked;
        if(session.IsOpen)Run(Scan);
    }
    private void Scan()
    {
        using var timing=new WordOperationTiming("Word.Perf.Scan");
        timing.Count("Optimized",session.UseOptimizedReading?1:0);
        highlighter.UsePreparedText=session.UseOptimizedReading;
        hover.Stop();pending = displayed = null;
        Review.LoadIssues([]);
        lastCoverage=null;
        CoverageInfo.Visibility=Visibility.Collapsed;
        var timer = Stopwatch.StartNew();
        var found = session.Check().ToList();
        timing.Mark("Check");
        double checkSeconds = timer.Elapsed.TotalSeconds;
        ClosePreview();
        anchors.Clear();
        var positionProbes=session.AdministrativePositionTargets.SelectMany(t=>t.References.Select(r=>r.Probe).Append(t.Probe));
        xpsPath = session.ExportPreview(found.Concat(session.LineTargets.Select(t => t.Probe)).Concat(positionProbes).Distinct().ToArray(), anchors);
        xps = new XpsDocument(xpsPath, FileAccess.Read);
        Preview.Document = xps.GetFixedDocumentSequence();
        timing.Mark("BuildPreview");
        if (Preview.Document is System.Windows.Documents.FixedDocumentSequence sequence)
        {
            int lineObserved=0,positionObserved=0;
            var policy = LinePolicy.Decree30;
            foreach (var entry in LineInspection.Inspect(sequence, highlighter, session.LineTargets, session.LineShapes,
                anchors, found, policy, session.HasGroupedShapes, session.VerifiedLineEdits,(_,known)=>{if(known)lineObserved++;}))
            { found.Add(entry.Issue); anchors[entry.Issue] = entry.Anchor; }
            foreach(var entry in AdministrativePositionInspection.Inspect(sequence,highlighter,session.AdministrativePositionTargets,anchors,
                (_,known)=>{if(known)positionObserved++;}))
            {found.Add(entry.Issue);anchors[entry.Issue]=entry.Anchor;}
            var coverage=session.Coverage;
            if(coverage is not null)
            {
                CoverageObservation Geometry(int total,int known,string reason)=>new(known==total?CoverageState.Observed:CoverageState.Partial,total,known,reason);
                if(session.LineTargets.Count>0)coverage=coverage.WithExternalEvidence("C12",Geometry(session.LineTargets.Count,lineObserved,
                    "Chỉ các mốc và nét đã đo/ghép chắc trên bản xem trước; vùng thiếu mốc hoặc nét chưa rõ vẫn cần kiểm thủ công."));
                if(session.AdministrativePositionTargets.Count>0)coverage=coverage.WithExternalEvidence("C11",Geometry(session.AdministrativePositionTargets.Count,positionObserved,
                    "Chỉ các dòng số/ngày và khung đặt đã xác minh; chưa tự sửa vị trí dọc."));
                if(session.NumberCoveragePreviewLayout is { } layout)
                {
                    try
                    {
                        PageNumberPreviewVerifier.VerifyNumbers(sequence,layout.LeftMm,layout.RightMm,layout.TopMm);
                        coverage=coverage.WithExternalEvidence("C33",new(CoverageState.Observed,1,1,
                            "Đã đối chiếu thiết lập và số hiển thị trên bản xem trước trong cấu trúc số trang được hỗ trợ."));
                    }
                    catch(Exception error) when(error is InvalidOperationException or ArgumentException or NotSupportedException)
                    {
                        Diagnostics.Write("Word.Coverage.PageNumberPreview",error);
                        coverage=coverage.WithExternalEvidence("C33",new(CoverageState.Partial,1,0,
                            "Đã đọc thiết lập nhưng chưa xác minh đủ số thực sự hiển thị trên bản xem trước; cần kiểm tra thủ công."));
                    }
                }
                lastCoverage=coverage.WithFindings(found);
            }
        }
        lastCoverage??=session.Coverage?.WithFindings(found);
        timing.Mark("InspectGeometry");
        timing.Count("PreparedPreviewSources",highlighter.PreparedSources);
        Review.LoadIssues(found,anchors);
        timing.Mark("ShowIssues");timing.Count("Completed",1);
        CoverageInfo.Text=(lastCoverage?.Summary??"")+(session.CoverageNotice is { } notice?"\n"+notice:"");
        CoverageInfo.Visibility=CoverageInfo.Text.Length==0?Visibility.Collapsed:Visibility.Visible;
        Status.Text = $"{Path.GetFileName(session.Source)} — {found.Count} cảnh báo. " +
            $"Quét: {checkSeconds:F1}s; dựng trang: {timer.Elapsed.TotalSeconds - checkSeconds:F1}s. " +
            (session.UseOptimizedReading?"Bộ đọc mới đang thử nghiệm. ":"Đọc theo cách cũ. ")+
            "Chọn hoặc rê chuột lên lỗi để đánh dấu riêng, không xuất lại Word." +
            (session.Recovered ? " Đã khôi phục kết nối Word từ bản lưu gần nhất." : "");
    }
    internal Exception? InvalidateReview()=>IndependentCleanup.Complete(null,
        [()=>{pending=displayed=null;anchors.Clear();},
         ()=>Review.LoadIssues([]),
         ()=>{lastCoverage=null;CoverageInfo.Visibility=Visibility.Collapsed;},
         ClosePreview]);
    private void ClosePreview()
    {
        var ownedXps=xps;string? ownedPath=xpsPath;
        var failure=IndependentCleanup.Complete(null,
            [()=>Preview.Document=null,
             highlighter.Reset,
             ()=>{ownedXps?.Close();if(ReferenceEquals(xps,ownedXps))xps=null;},
             ()=>{if(ownedPath is not null)File.Delete(ownedPath);if(xpsPath==ownedPath)xpsPath=null;}]);
        if(failure is not null)
        {
            // A failed close/delete retains its handle/path for a later attempt.
            // Never let that failure skip invalidation of the visible document.
            Diagnostics.Write("UI.PreviewCleanup",failure);
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
    private void ShowPreview(Issue? issue)
    {
        if (issue is null || issue == displayed) return;
        if(!anchors.TryGetValue(issue,out var anchor))
        {
            highlighter.Clear();pending=displayed=null;
            Status.Text=IssueReviewCatalog.Describe(issue).Label+". Chưa ánh xạ chắc chắn được vị trí trên bản xem trước; không đánh dấu để tránh nhầm.";
            return;
        }
        if (Preview.Document is not System.Windows.Documents.FixedDocumentSequence document) return;
        var timer = Stopwatch.StartNew();
        Preview.GoToPage(anchor.Page);
        bool pageWarning = issue.Kind is FixKind.PageLayout or FixKind.PageNumber;
        if (pageWarning) highlighter.Clear();
        bool marked = !pageWarning && highlighter.Show(document, anchor);
        if (issue.LineFix is ShapeLineFix lineFix) highlighter.ShowLineSuggestion(document,anchor.Page,lineFix);
        displayed = issue;
        Status.Text = $"Trang {anchor.Page} — {IssueReviewCatalog.Describe(issue).Label}. " +
            (pageWarning ? "Thiết lập trang; xem thông số trong Chi tiết lỗi. " : marked ? $"Đánh dấu: {timer.Elapsed.TotalMilliseconds:F0} ms." :
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
        int originalCount=selected.Count;
        try{selected=RepairDependencies.Expand(selected,choices.Select(c=>c.Issue).ToArray());}
        catch(InvalidOperationException error)
        {MessageBox.Show(this,error.Message,"Quét lại trước khi sửa",MessageBoxButton.OK,MessageBoxImage.Information);return;}
        bool staged=selected.Select(StagedRepairBatch.Stage).Distinct().Count()>1;
        string stageNote=staged?"\nChương trình tự sửa chữ/phông, đo lại, căn giữa rồi sửa đường kẻ theo các mục đã chọn. Không cần tích chọn lại.":"";
        if(selected.Count>originalCount)stageNote+=$"\nKèm {selected.Count-originalCount} mục cần sửa trước đường kẻ; danh sách dưới đây gồm cả các mục này.";
        string examples = string.Join("\n", selected.Take(8).Select(x =>
            $"• {x.Original.Replace("\r", " ")} → {x.Suggestion}"));
        string more = selected.Count > 8 ? $"\n… và {selected.Count - 8} lỗi khác." : "";
        int manual = choices.Count(x => !x.Issue.CanFix);
        string manualNote = manual > 0 ? $"\n{manual} cảnh báo thủ công sẽ không bị sửa." : "";
        string caution = selected.Any(x => !x.BatchSafe)
            ? "\nCó gợi ý cần bạn duyệt (ngữ cảnh, dàn trang hoặc thể thức); có thể cảnh báo nhầm." : "";
        if (MessageBox.Show(this, $"Áp dụng {selected.Count} gợi ý sửa?\n\n{examples}{more}{caution}{manualNote}{stageNote}\n\n" +
            "Hoàn tác một lần để bỏ cả lượt sửa. Tài liệu gốc được giữ nguyên.", "Duyệt lượt sửa",
            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        Run(() =>
        {
            using var progress=new RepairProgressDialog(new System.Windows.Interop.WindowInteropHelper(this).Handle,
                typeof(MainWindow).Assembly.GetName().Version?.ToString(3)??"chưa rõ",selected.Count);
            var previous=session.ProgressReporter;bool enabled=IsEnabled;IsEnabled=false;
            try
            {
                session.ProgressReporter=progress.Report;RepairBatchResult? result=null;
                EditThenScan.Run(()=>result=session.FixSelection(selected),()=>
                {
                    progress.Report(new(RepairActivity.Rescanning,selected.Count,selected.Count,"Đã sửa; đang quét lại và dựng trang để cập nhật danh sách lỗi."));
                    Scan();
                });
                progress.Report(new(RepairActivity.Completed,selected.Count,selected.Count,"Đã hoàn tất hậu kiểm và quét lại."));
                Status.Text=$"Đã áp dụng {result!.Applied} gợi ý. "+
                    (result.AlreadySatisfied>0?$"{result.AlreadySatisfied} mục đã đạt sau đo lại. ":"")+Status.Text;
            }
            finally{session.ProgressReporter=previous;IsEnabled=enabled;}
        });
    }
    private void SigningSpaceClick(object sender,RoutedEventArgs e)=>Run(()=>
    {
        using var progress=new RepairProgressDialog(new System.Windows.Interop.WindowInteropHelper(this).Handle,
            typeof(MainWindow).Assembly.GetName().Version?.ToString(3)??"chưa rõ",1);
        var previous=session.ProgressReporter;bool enabled=IsEnabled;IsEnabled=false;
        try
        {
            session.ProgressReporter=progress.Report;SignatureSpaceResult? result=null;
            EditThenScan.Run(()=>result=session.ReserveHandSigningSpace(),Scan);
            progress.Report(new(RepairActivity.Completed,1,1,"Đã kiểm xong khoảng trống ký tay và quét lại."));
            Status.Text=result!.Changed?$"Đã chừa chỗ ký tay, đo được {result.Gap:0.#} pt. Hoàn tác một lần để trở lại.":
                $"Vùng ký đã có khoảng trống {result.Gap:0.#} pt; giữ nguyên.";
        }
        finally{session.ProgressReporter=previous;IsEnabled=enabled;}
    });
    private void OfficeCheckClick(object sender, RoutedEventArgs e) => Run(() =>
    {
        Status.Text = "Đang kiểm tra Word bằng tài liệu mẫu riêng: mở, ghi Line, xuất trang, lưu và hoàn tác…";
        var result = OfficeIntegrationCheck.Run(session.UseOptimizedReading);
        Status.Text = result.Passed ? "Kiểm tra Word tự động: ĐẠT trên mẫu; "+result.ReaderScope : "Kiểm tra Word tự động: CHƯA ĐẠT; xem office-check.txt.";
        MessageBox.Show(this, (result.Passed ? "Đã đạt các bước kiểm tra Word trên mẫu riêng." :
            "Kiểm tra Word chưa đạt ở bước: " + result.Stage) +
            "\n\n"+result.ReaderScope+"\n\nKết quả và file mẫu đã sửa lưu tại:\n" + result.Folder +
            "\n\nBài kiểm tra đo và kiểm nét XPS trên mẫu; không xác nhận mọi tài liệu.",
            "Kiểm tra Word", MessageBoxButton.OK, result.Passed ? MessageBoxImage.Information : MessageBoxImage.Warning);
        Process.Start(new ProcessStartInfo { FileName = result.Folder, UseShellExecute = true });
    });
    private void OfficeGroupsClick(object sender,RoutedEventArgs e)
    {
        hover.Stop();var selected=OfficeFocusedCheck.Select(this);if(selected is null)return;
        Run(()=>
        {
            Status.Text="Đang kiểm các nhóm Word trên mẫu riêng bằng bộ đọc cũ…";
            var result=OfficeFocusedCheck.Run(selected);
            Status.Text=$"Kiểm các nhóm Word đã chọn: {result.Report.PassedCount} đạt, {result.Report.FailedCount} lỗi, {result.Report.NotRunCount} chưa chạy.";
            MessageBox.Show(this,result.Summary+"\n\nKết quả và mẫu từng nhóm:\n"+result.Folder,
                "Kiểm tra từng nhóm Word",MessageBoxButton.OK,result.Report.Passed?MessageBoxImage.Information:MessageBoxImage.Warning);
            Process.Start(new ProcessStartInfo{FileName=result.Folder,UseShellExecute=true});
        });
    }
    private void ProgressReportClick(object sender,RoutedEventArgs e)
    {
        hover.Stop();
        // This independent report must not scan Word or clear the document issue list.
        var report=new ProgressReportWindow(session.SaveProgressReport) {Owner=this};
        report.ShowDialog();
    }
    private void DocumentTypesClick(object sender,RoutedEventArgs e)
    {
        hover.Stop();new DocumentTypeCatalogWindow{Owner=this}.ShowDialog();
    }
    private void DiagnosticsFolderClick(object sender, RoutedEventArgs e)
    {
        string folder = Path.GetDirectoryName(Diagnostics.LogPath)!;
        Directory.CreateDirectory(folder);
        Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
    }
    private void UndoClick(object sender, RoutedEventArgs e)
    { if (session.IsOpen) Run(() => EditThenScan.RunUndo(session.Undo,Scan)); }
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
        Run(() =>
        {
            var failure=CloseOwnedResources(ClosePreview,session.Dispose);
            if(failure is not null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        });
    }
    internal static Exception? CloseOwnedResources(Action closePreview,Action closeSession)=>
        IndependentCleanup.Complete(null,[closePreview,closeSession]);
}

