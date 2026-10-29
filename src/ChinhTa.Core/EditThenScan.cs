namespace ChinhTa.Core;

public sealed class ScanAfterEditException:InvalidOperationException
{
    public ScanAfterEditException(Exception cause):this(cause,false){}
    internal ScanAfterEditException(Exception cause,bool afterUndo):base(afterUndo?
        "Lệnh Hoàn tác đã hoàn tất, nhưng chưa quét lại được danh sách lỗi. " +
        "Hãy Quét lại để cập nhật; chỉ Hoàn tác thêm nếu bạn muốn bỏ lượt sửa trước đó.":
        "Lượt sửa đã được áp dụng trên bản làm việc, nhưng chưa quét lại được danh sách lỗi. " +
        "Hãy Quét lại trước khi sửa tiếp, hoặc Hoàn tác để bỏ lượt vừa sửa.",cause){}
}

public static class EditThenScan
{
    public static void Run(Action apply,Action scan)
        =>RunCore(apply,scan,false);
    public static void RunUndo(Action undo,Action scan)
        =>RunCore(undo,scan,true);
    private static void RunCore(Action apply,Action scan,bool afterUndo)
    {
        apply(); // An edit failure keeps its original error and rollback result.
        try{scan();}
        catch(Exception failure){throw new ScanAfterEditException(failure,afterUndo);}
        // Never replay edits or Undo a committed action because its review failed.
    }
}
