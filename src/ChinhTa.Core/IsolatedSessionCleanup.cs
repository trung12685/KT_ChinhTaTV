namespace ChinhTa.Core;

// Closing one COM wrapper can fail while the remaining wrappers and application
// still need releasing. Every action is independent; no cleanup error may hide
// the operation failure or turn an uncertain close into successful disposal.
public static class IsolatedSessionCleanup
{
    public static Exception? Complete(Exception? primary,IReadOnlyList<Action> closeAndRestoreActions,Action deleteTemporaryDirectory)
    {
        ArgumentNullException.ThrowIfNull(closeAndRestoreActions);
        ArgumentNullException.ThrowIfNull(deleteTemporaryDirectory);
        var errors=new List<Exception>();
        if(primary is not null)errors.Add(primary);
        bool closed=true;
        // Callbacks may release state used by the caller's callback collection.
        // Capture ownership once so later mutations cannot skip a resource.
        var actions=closeAndRestoreActions.ToArray();
        foreach(var action in actions)
        {
            try{action();}
            catch(Exception error){closed=false;errors.Add(error);}
        }
        // A failed operation does not by itself leave an open document. A failed
        // close/release/option restore does: retain the disposable recovery copy.
        if(closed)
            try{deleteTemporaryDirectory();}
            catch(Exception error){errors.Add(error);}
        return errors.Count switch
        {
            0=>null,
            1=>errors[0],
            _=>new AggregateException("Phiên Word cô lập chưa hoàn tất thao tác hoặc dọn dẹp; giữ đầy đủ các lỗi để kiểm tra.",errors)
        };
    }
}
