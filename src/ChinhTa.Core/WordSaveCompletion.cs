namespace ChinhTa.Core;

// Save can return while this private Word instance still has background work.
// https://learn.microsoft.com/office/vba/api/word.application.backgroundsavingstatus
// Observe completion; never change Options.BackgroundSave or replay Save.
public static class WordSaveCompletion
{
    public static readonly TimeSpan MaximumWait=TimeSpan.FromSeconds(30);
    public static readonly TimeSpan PollInterval=TimeSpan.FromMilliseconds(50);

    public static void Wait(Func<int> pending,Func<bool> saved)
    {
        var clock=System.Diagnostics.Stopwatch.StartNew();
        Wait(pending,saved,()=>clock.Elapsed,System.Threading.Thread.Sleep,MaximumWait,PollInterval);
    }

    public static void Wait(Func<int> pending,Func<bool> saved,Func<TimeSpan> elapsed,
        Action<TimeSpan> pause,TimeSpan timeout,TimeSpan interval)
    {
        ArgumentNullException.ThrowIfNull(pending);ArgumentNullException.ThrowIfNull(saved);
        ArgumentNullException.ThrowIfNull(elapsed);ArgumentNullException.ThrowIfNull(pause);
        if(timeout<=TimeSpan.Zero)throw new ArgumentOutOfRangeException(nameof(timeout));
        if(interval<=TimeSpan.Zero)throw new ArgumentOutOfRangeException(nameof(interval));
        TimeSpan previous=TimeSpan.Zero;
        TimeSpan ObserveTime()
        {
            var now=elapsed();
            if(now<previous || now<TimeSpan.Zero)
                throw new InvalidOperationException("Đồng hồ chờ lưu không hợp lệ; chưa xác nhận checkpoint.");
            previous=now;
            if(now>=timeout)throw new TimeoutException("Word chưa hoàn tất lưu nền trong thời gian chờ; giữ bản làm việc để kiểm tra.");
            return now;
        }
        int ObservePending()
        {
            int count=pending();
            if(count<0)throw new InvalidOperationException("Word trả về số tác vụ lưu nền không hợp lệ; chưa xác nhận checkpoint.");
            return count;
        }
        while(true)
        {
            ObserveTime();
            if(ObservePending()==0)
            {
                if(!saved())throw new InvalidOperationException("Word chưa xác nhận tài liệu đã được lưu; chưa xác nhận checkpoint.");
                // The Saved getter is another COM observation. Do not accept
                // an idle-to-busy transition between those observations.
                int afterSaved=ObservePending();
                ObserveTime();
                if(afterSaved==0)return;
            }
            var remaining=timeout-ObserveTime();
            pause(interval<remaining?interval:remaining);
        }
    }
}
