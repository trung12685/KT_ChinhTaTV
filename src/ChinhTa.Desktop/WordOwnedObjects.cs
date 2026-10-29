using System.Runtime.ExceptionServices;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

// Register each successful acquisition before asking Word for the next child.
// Balance acquisitions, rather than distinct RCW identities, in reverse order.
internal static class WordOwnedObjects
{
    private static readonly object CleanupDataKey=new();
    internal static Exception? CleanupFailure(Exception failure)=>failure.Data[CleanupDataKey] as Exception;

    internal static T Read<T>(Func<Func<object,object>,T> read,Action<object?> release)
    {
        ArgumentNullException.ThrowIfNull(read);ArgumentNullException.ThrowIfNull(release);
        var acquired=new List<object>();
        object Own(object value){acquired.Add(value);return value;}
        T result=default!;Exception? failure=null;
        try{result=read(Own);}
        catch(Exception error){failure=error;}
        var cleanup=IndependentCleanup.Complete(null,acquired.AsEnumerable().Reverse()
            .Select(value=>(Action)(()=>release(value))).ToArray());
        if(failure is not null)
        {
            if(cleanup is not null)
            {
                // Recovery classifies the operation's original HRESULT. An
                // independent Release fault must not replace that cause.
                var previous=CleanupFailure(failure);
                failure.Data[CleanupDataKey]=previous is null?cleanup:new AggregateException(previous,cleanup);
                try{Diagnostics.Write("Word.OwnedObjects.CleanupFailed",cleanup);}
                catch(Exception){/* Diagnostics cannot replace the primary Word failure. */}
            }
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
        // A completed read must not be replayed solely because releasing an
        // acquired RCW returned a disconnected HRESULT.
        if(cleanup is not null)throw new WordObjectCleanupException(cleanup);
        return result;
    }
}

// Kept separate from missing/unsupported measurement errors caught by readers.
internal sealed class WordObjectCleanupException(Exception cause)
    :Exception("Chưa giải phóng đầy đủ các đối tượng Word đã đọc.",cause);
