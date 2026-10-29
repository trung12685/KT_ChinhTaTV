using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

public sealed class RepairProgressWindow:Window,IDisposable
{
    private readonly TextBlock step=new(){FontSize=17,TextWrapping=TextWrapping.Wrap,Margin=new(0,16,0,12)};
    private readonly TextBlock count=new(){Margin=new(0,8,0,0)};
    private readonly TextBlock elapsed=new(){Margin=new(0,12,0,0),Foreground=Brushes.DimGray};
    private readonly ProgressBar bar=new(){Height=16,Minimum=0,Maximum=1,IsIndeterminate=true};
    private readonly Stopwatch clock=Stopwatch.StartNew();
    private readonly DispatcherTimer heartbeat=new(){Interval=TimeSpan.FromMilliseconds(250)};
    private bool allowClose;
    private bool closed;
    public int Heartbeats{get;private set;}
    public RepairProgressUpdate Current{get;private set;}
    public RepairProgressWindow(string version,int total)
    {
        Title="Đang sửa văn bản · "+version;Width=560;SizeToContent=SizeToContent.Height;
        ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.CenterScreen;ShowInTaskbar=false;
        var content=new StackPanel{Margin=new(24)};
        content.Children.Add(new TextBlock{Text="ĐANG SỬA VĂN BẢN",FontWeight=FontWeights.Bold,FontSize=20});
        content.Children.Add(step);content.Children.Add(bar);content.Children.Add(count);content.Children.Add(elapsed);
        content.Children.Add(new TextBlock{Text="Chỉ hoàn tất khi hậu kiểm và quét lại xong. Nếu bước sửa thất bại, chương trình sẽ thử phục hồi cả lượt.",
            TextWrapping=TextWrapping.Wrap,Margin=new(0,14,0,0),Foreground=Brushes.DimGray});
        Content=content;Background=new SolidColorBrush(Color.FromRgb(241,245,249));
        Current=new(RepairActivity.Preparing,0,total,"Đang chuẩn bị lượt sửa…");Apply(Current);
        heartbeat.Tick+=(_,_)=>{Heartbeats++;elapsed.Text=$"Đã chờ {clock.Elapsed.TotalSeconds:F0} giây · Word có thể cần thời gian cho bước hiện tại.";};
        heartbeat.Start();Closing+=ClosingProgress;Closed+=(_,_)=>{closed=true;heartbeat.Stop();};
    }
    private void ClosingProgress(object? sender,CancelEventArgs e){if(!allowClose)e.Cancel=true;}
    public void Apply(RepairProgressUpdate update)
    {
        Current=update;step.Text=update.Message;count.Text=update.CountText;
        bar.IsIndeterminate=update.Verified==0 && update.Activity!=RepairActivity.Completed;
        bar.Value=update.Fraction;
        count.Foreground=update.Activity==RepairActivity.Completed?Brushes.DarkGreen:Brushes.Black;
    }
    internal void Finish(){if(closed)return;allowClose=true;heartbeat.Stop();Close();}
    public void Dispose()=>Finish();
}

// Word and all its COM ranges stay on their original STA. This small view owns a
// separate STA dispatcher so elapsed time/painting continue during a blocking Word
// call. No document, COM object or main-window WPF control crosses the boundary.
public sealed class RepairProgressDialog:IDisposable
{
    private readonly Thread thread;
    private readonly TaskCompletionSource<RepairProgressWindow> ready=new(TaskCreationOptions.RunContinuationsAsynchronously);
    private RepairProgressWindow? window;
    private int disposed;
    public RepairProgressDialog(IntPtr owner,string version,int total):
        this(owner,()=>new RepairProgressWindow(version,total),TimeSpan.FromSeconds(8)){}
    internal RepairProgressDialog(IntPtr owner,Func<RepairProgressWindow> createWindow,TimeSpan startupTimeout)
    {
        ArgumentNullException.ThrowIfNull(createWindow);
        if(startupTimeout<=TimeSpan.Zero)throw new ArgumentOutOfRangeException(nameof(startupTimeout));
        thread=new Thread(()=>RunDisplay(owner,createWindow)){IsBackground=true,Name="Repair progress display"};
        try
        {
            thread.SetApartmentState(ApartmentState.STA);thread.Start();
            using var timeoutCancellation=new CancellationTokenSource();
            var timeout=Task.Delay(startupTimeout,timeoutCancellation.Token);
            if(Task.WhenAny(ready.Task,timeout).GetAwaiter().GetResult()!=ready.Task)
                throw new InvalidOperationException("Chưa mở được cửa sổ tiến độ; chưa bắt đầu sửa.");
            timeoutCancellation.Cancel();
            ready.Task.GetAwaiter().GetResult();
        }
        catch{RequestClose();throw;}
    }
    private void RunDisplay(IntPtr owner,Func<RepairProgressWindow> createWindow)
    {
        Dispatcher? dispatcher=null;RepairProgressWindow? view=null;
        try
        {
            var ownedDispatcher=Dispatcher.CurrentDispatcher;dispatcher=ownedDispatcher;
            if(Volatile.Read(ref disposed)!=0)return;
            view=createWindow()??throw new InvalidOperationException("Chưa tạo được cửa sổ tiến độ; chưa bắt đầu sửa.");
            Interlocked.Exchange(ref window,view);
            view.Closed+=(_,_)=>
            {if(!ownedDispatcher.HasShutdownStarted)ownedDispatcher.BeginInvokeShutdown(DispatcherPriority.Background);};
            if(Volatile.Read(ref disposed)!=0)return;
            if(owner!=IntPtr.Zero)new WindowInteropHelper(view){Owner=owner};
            if(Volatile.Read(ref disposed)!=0)return;
            view.Show();
            if(Volatile.Read(ref disposed)!=0 || !ready.TrySetResult(view))return;
            Dispatcher.Run();
        }
        catch(Exception error)
        {
            if(!ready.TrySetException(error))Diagnostics.Write("UI.ProgressDisplay",error);
        }
        finally
        {
            Interlocked.CompareExchange(ref window,null,view);
            var cleanup=IndependentCleanup.Complete(null,
                [()=>view?.Finish(),()=>{if(dispatcher is not null && !dispatcher.HasShutdownFinished)dispatcher.InvokeShutdown();}]);
            if(cleanup is not null)Diagnostics.Write("UI.ProgressDisplay.Cleanup",cleanup);
            ready.TrySetCanceled();
        }
    }
    public void Report(RepairProgressUpdate update)
    {
        var view=Volatile.Read(ref window);if(Volatile.Read(ref disposed)!=0 || view is null || view.Dispatcher.HasShutdownStarted)return;
        try{view.Dispatcher.InvokeAsync(()=>view.Apply(update),DispatcherPriority.Background);}catch(InvalidOperationException){ }
    }
    public (RepairProgressUpdate Update,int Heartbeats) Snapshot()
    {
        var view=Volatile.Read(ref window)??throw new InvalidOperationException("Chưa có cửa sổ tiến độ.");
        return view.Dispatcher.Invoke(()=>(view.Current,view.Heartbeats),DispatcherPriority.ContextIdle);
    }
    private void RequestClose()
    {
        if(Interlocked.Exchange(ref disposed,1)!=0)return;
        ready.TrySetCanceled();var view=Interlocked.Exchange(ref window,null);
        if(view is not null && !view.Dispatcher.HasShutdownStarted)
            try{view.Dispatcher.InvokeAsync(view.Finish,DispatcherPriority.Send);}catch(InvalidOperationException){ }
        // Do not synchronously wait across owner/UI dispatchers while Word is busy.
    }
    public void Dispose()=>RequestClose();
}
