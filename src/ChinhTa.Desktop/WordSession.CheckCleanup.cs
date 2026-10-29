using System.IO;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

public sealed partial class WordSession
{
    // Focused checks must retain errors suppressed by the normal reconnect/close
    // path. A later successful Quit cannot retroactively prove that close safe.
    private readonly List<Exception> checkCleanupFailures=new();

    internal static Exception? CompleteCheckCleanup(object? ownedDocument,object? ownedApp,
        Action<object?> release,Action deleteTemporaryDirectory,IReadOnlyList<Exception> historicalFailures,bool snapshotCurrent)
    {
        ArgumentNullException.ThrowIfNull(release);ArgumentNullException.ThrowIfNull(deleteTemporaryDirectory);
        ArgumentNullException.ThrowIfNull(historicalFailures);
        var actions=new List<Action>();
        // IsolatedSessionCleanup intentionally allows deletion after an ordinary
        // operation failure. Here the earlier failures specifically concern
        // cleanup, so make their uncertainty a failing cleanup action instead.
        var previous=historicalFailures.ToArray();
        if(previous.Length>0)actions.Add(()=>throw new AggregateException("Lần đóng phiên kiểm trước chưa được xác nhận.",previous));
        if(!snapshotCurrent)actions.Add(()=>throw new InvalidOperationException("Bản checkpoint hiện tại chưa được xác minh; giữ bản phục hồi phiên kiểm."));
        if(ownedDocument is not null)
        {
            actions.Add(()=>{dynamic copy=ownedDocument;copy.Close(0);});
            actions.Add(()=>release(ownedDocument));
        }
        if(ownedApp is not null)
        {
            actions.Add(()=>{dynamic application=ownedApp;application.Quit(0);});
            actions.Add(()=>release(ownedApp));
        }
        return IsolatedSessionCleanup.Complete(null,actions,deleteTemporaryDirectory);
    }

    internal void DisposeCheckSession()
    {
        if(LineCheckArtifactFolder is null)
            throw new InvalidOperationException("Chỉ dùng đóng nghiêm ngặt cho phiên kiểm Word riêng.");
        object? ownedDocument=document;document=null;
        object? ownedApp=app;app=null;
        var failure=CompleteCheckCleanup(ownedDocument,ownedApp,Release,
            ()=>{if(Directory.Exists(folder))Directory.Delete(folder,true);},checkCleanupFailures,snapshotCurrent);
        if(failure is null)return;
        checkCleanupFailures.Add(failure); // Sticky even after both RCWs detached.
        throw new InvalidOperationException("Chưa đóng và xác minh đầy đủ phiên kiểm Word; giữ thư mục phục hồi: "+folder,failure);
    }
}
