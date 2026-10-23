using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class RepairProgressDesktopChecks
{
    public static void Run(Action<bool,string> assert)
    {
        int ownerThread=Environment.CurrentManagedThreadId;
        using(var dialog=new RepairProgressDialog(IntPtr.Zero,"test",5))
        {
            dialog.Report(new(RepairActivity.Editing,2,5,"Đang sửa đường kẻ dưới trích yếu."));
            // Simulate a blocking Word call on its original owner STA. The view
            // must keep painting/timing without pumping or reentering that STA.
            Thread.Sleep(800);
            var snapshot=dialog.Snapshot();
            assert(snapshot.Heartbeats>=1 && snapshot.Update.Verified==2 && Environment.CurrentManagedThreadId==ownerThread,
                "The actual progress window keeps its own heartbeat while the original STA is blocked and COM ownership stays unchanged");
            dialog.Report(new(RepairActivity.Restoring,0,5,"Đang phục hồi cả lượt sửa."));
            snapshot=dialog.Snapshot();
            assert(snapshot.Update.Activity==RepairActivity.Restoring && snapshot.Update.Verified==0,
                "The live progress view displays rollback and clears provisional completion");
        }
        var view=new RepairProgressWindow("0.1.57",5);
        try
        {
            view.Show();
            view.Dispatcher.Invoke(()=>{},System.Windows.Threading.DispatcherPriority.ContextIdle);
            view.Apply(new(RepairActivity.Verifying,5,5,"Kiểm lại cả lượt: chữ, phông, tâm dòng và nét XPS."));
            assert(view.Current.Fraction<1,"Five of five provisional items cannot show complete before the final rescan");
            view.Apply(new(RepairActivity.Rescanning,5,5,"Đã sửa; đang quét lại và dựng trang để cập nhật danh sách lỗi."));
            view.Measure(new Size(560,400));view.Arrange(new Rect(0,0,560,view.DesiredSize.Height));view.UpdateLayout();
            var picture=new RenderTargetBitmap(560,Math.Max(1,(int)Math.Ceiling(view.ActualHeight)),96,96,PixelFormats.Pbgra32);picture.Render(view);
            string folder=Path.Combine(Environment.CurrentDirectory,"artifacts");Directory.CreateDirectory(folder);
            var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(picture));using(var output=File.Create(Path.Combine(folder,"repair-progress-preview.png")))png.Save(output);
            assert(new FileInfo(Path.Combine(folder,"repair-progress-preview.png")).Length>1000,"A real WPF progress preview is captured from synthetic data without Word or private documents");
            view.Apply(new(RepairActivity.Completed,5,5,"Đã hoàn tất hậu kiểm và quét lại."));
            assert(view.Current.Fraction==1,"Completion is a distinct state emitted after verification and rescan");
        }
        finally{view.Dispose();}
    }
}
