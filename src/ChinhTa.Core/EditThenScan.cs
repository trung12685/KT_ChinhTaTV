namespace ChinhTa.Core;

public sealed class ScanAfterEditException(Exception cause):InvalidOperationException(
    "Lượt sửa đã được áp dụng trên bản làm việc, nhưng chưa quét lại được danh sách lỗi. " +
    "Hãy Quét lại trước khi sửa tiếp, hoặc Hoàn tác để bỏ lượt vừa sửa.",cause);

public static class EditThenScan
{
    public static void Run(Action apply,Action scan)
    {
        apply(); // An edit failure keeps its original error and rollback result.
        try{scan();}
        catch(Exception failure){throw new ScanAfterEditException(failure);}
        // Never replay edits or Undo a committed action because its review failed.
    }
}
