using System.IO;
using System.Threading;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class RepairProgressStartupChecks
{
    private static readonly TimeSpan StartupTimeout=TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan TeardownTimeout=TimeSpan.FromSeconds(5);
    private static bool Exited(Thread? worker)=>worker is not null && worker.Join(TeardownTimeout);
    public static void Run(Action<bool,string> assert)
    {
        NormalOwnership(assert); // Warm WPF before the short gated timeout cases.
        LateFactory(assert);
        TimeoutDuringShow(assert);
        FactoryFailure(assert);
        ShowFailure(assert);
    }
    private static void LateFactory(Action<bool,string> assert)
    {
        using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
        Thread? worker=null;int created=0,shown=0,closed=0;Exception? failure=null;
        try
        {
            using var dialog=new RepairProgressDialog(IntPtr.Zero,()=>
            {
                worker=Thread.CurrentThread;entered.Set();
                if(!release.Wait(TeardownTimeout))throw new TimeoutException("Synthetic late progress factory was not released");
                Interlocked.Increment(ref created);var view=new RepairProgressWindow("late factory",1);
                view.SourceInitialized+=(_,_)=>Interlocked.Increment(ref shown);
                view.Closed+=(_,_)=>Interlocked.Increment(ref closed);
                return view;
            },StartupTimeout);
        }
        catch(Exception error){failure=error;}
        finally{release.Set();}
        bool exited=Exited(worker);
        assert(entered.IsSet && failure is InvalidOperationException &&
            failure.Message.Contains("chưa bắt đầu sửa",StringComparison.Ordinal),
            "A gated progress factory times out before returning ownership, without beginning a document repair");
        assert(exited && created==1 && shown==0 && closed==1,
            "A window created after the constructor timed out is closed on its owning STA without ever being shown or leaving a dispatcher thread running");
    }
    private static void TimeoutDuringShow(Action<bool,string> assert)
    {
        using var showing=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
        Thread? worker=null;int shown=0,closed=0;Exception? failure=null;
        try
        {
            using var dialog=new RepairProgressDialog(IntPtr.Zero,()=>
            {
                worker=Thread.CurrentThread;var view=new RepairProgressWindow("show race",1);
                view.SourceInitialized+=(_,_)=>
                {
                    Interlocked.Increment(ref shown);showing.Set();
                    if(!release.Wait(TeardownTimeout))throw new TimeoutException("Synthetic progress Show was not released");
                };
                view.Closed+=(_,_)=>Interlocked.Increment(ref closed);
                return view;
            },StartupTimeout);
        }
        catch(Exception error){failure=error;}
        finally{release.Set();}
        bool exited=Exited(worker);
        assert(showing.IsSet && shown==1 && failure is InvalidOperationException,
            "The progress constructor can time out deterministically while the published view is inside Show");
        assert(exited && closed==1,
            "Cancellation racing publication and Show drains the owned view and dispatcher after Show returns instead of leaving a noncloseable orphan");
    }
    private static void FactoryFailure(Action<bool,string> assert)
    {
        Thread? worker=null;var primary=new IOException("Synthetic progress factory failed");Exception? failure=null;
        try
        {
            using var dialog=new RepairProgressDialog(IntPtr.Zero,()=>
            {worker=Thread.CurrentThread;throw primary;},TeardownTimeout);
        }
        catch(Exception error){failure=error;}
        bool exited=Exited(worker);
        assert(ReferenceEquals(failure,primary),
            "A startup factory error keeps its original cause instead of being replaced by timeout or aggregate wait errors");
        assert(exited,"A failed progress factory shuts down its owned STA dispatcher even before a view was acquired");
    }
    private static void ShowFailure(Action<bool,string> assert)
    {
        Thread? worker=null;int closed=0;Exception? failure=null;
        try
        {
            using var dialog=new RepairProgressDialog(IntPtr.Zero,()=>
            {
                worker=Thread.CurrentThread;var view=new RepairProgressWindow("closed before Show",1);
                view.Closed+=(_,_)=>Interlocked.Increment(ref closed);view.Finish();return view;
            },TeardownTimeout);
        }
        catch(Exception error){failure=error;}
        bool exited=Exited(worker);
        assert(failure is InvalidOperationException && failure is not AggregateException && closed==1,
            "A real WPF Show failure from a factory-returned closed view remains the startup error and cleanup is idempotent");
        assert(exited,"A failed Show cannot leave the progress STA dispatcher running");
    }
    private static void NormalOwnership(Action<bool,string> assert)
    {
        Thread? worker=null;int shown=0,closed=0;RepairProgressDialog? dialog=null;
        try
        {
            dialog=new RepairProgressDialog(IntPtr.Zero,()=>
            {
                worker=Thread.CurrentThread;var view=new RepairProgressWindow("normal ownership",3);
                view.SourceInitialized+=(_,_)=>Interlocked.Increment(ref shown);
                view.Closed+=(_,_)=>Interlocked.Increment(ref closed);return view;
            },TeardownTimeout);
            dialog.Report(new(RepairActivity.Verifying,2,3,"Kiểm tra mẫu tổng hợp."));
            var snapshot=dialog.Snapshot();
            assert(shown==1 && snapshot.Update.Verified==2 && snapshot.Update.Activity==RepairActivity.Verifying,
                "A successfully owned progress view still receives reports on its separate dispatcher");
        }
        finally{dialog?.Dispose();dialog?.Dispose();}
        bool exited=Exited(worker);
        assert(exited && closed==1,"Repeated dialog disposal closes the successfully owned view once and lets its dispatcher thread exit");
        dialog!.Report(new(RepairActivity.Completed,3,3,"Đã đóng mẫu tổng hợp."));
        assert(closed==1 && worker is {IsAlive:false},
            "A late report after disposal cannot restart a closed progress view or its dispatcher");
    }
}
