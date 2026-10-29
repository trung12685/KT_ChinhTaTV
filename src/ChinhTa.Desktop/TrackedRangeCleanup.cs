using ChinhTa.Core;

namespace ChinhTa.Desktop;

internal sealed record TrackedRangeRestoration(bool Restored,Exception? CleanupFailure,Exception? RestoreFailure)
{
    // A resource-release fault is kept separately from the verified checkpoint
    // outcome. Only an actual restoration fault fails the restore callback.
    internal Exception? Failure=>RestoreFailure is null?null:CleanupFailure is null?RestoreFailure:
        new AggregateException("Chưa phục hồi được nhóm chữ; giữ cả lỗi phục hồi và giải phóng range.",RestoreFailure,CleanupFailure);
}

internal static class TrackedRangeCleanup
{
    internal static Exception? Drain<T>(ICollection<T> tracked,Func<T,object?> range,Action<object?> release)
    {
        ArgumentNullException.ThrowIfNull(tracked);ArgumentNullException.ThrowIfNull(range);ArgumentNullException.ThrowIfNull(release);
        var owned=tracked.ToArray();
        // Transfer ownership before a release can fail or run a callback. An
        // outer finally must not release a partially processed collection twice.
        tracked.Clear();
        return IndependentCleanup.Complete(null,owned.Select(item=>(Action)(()=>release(range(item)))).ToArray());
    }

    internal static TrackedRangeRestoration Restore<T>(ICollection<T> tracked,Func<T,object?> range,
        Action<object?> release,Action restore)
    {
        ArgumentNullException.ThrowIfNull(restore);
        var cleanup=Drain(tracked,range,release);
        bool restored=false;
        var failure=IndependentCleanup.Complete(null,[()=>{restore();restored=true;}]);
        return new(restored,cleanup,failure);
    }
}
