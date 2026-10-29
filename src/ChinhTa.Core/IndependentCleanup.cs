namespace ChinhTa.Core;

// Every resource still needs an attempt when another cleanup has failed. Keep
// the operation error first and preserve the original exception identities.
public static class IndependentCleanup
{
    public static Exception? Complete(Exception? primary,IReadOnlyList<Action> actions)
    {
        ArgumentNullException.ThrowIfNull(actions);
        var errors=new List<Exception>();
        if(primary is not null)errors.Add(primary);
        // Closing a resource can mutate the caller's collection of owned state.
        foreach(var action in actions.ToArray())
        {
            try{action();}
            catch(Exception failure){errors.Add(failure);}
        }
        return errors.Count switch
        {
            0=>null,
            1=>errors[0],
            _=>new AggregateException("Chưa hoàn tất thao tác hoặc dọn dẹp; giữ đầy đủ các lỗi để kiểm tra.",errors)
        };
    }
}
