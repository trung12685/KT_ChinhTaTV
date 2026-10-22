namespace ChinhTa.Core;

public sealed record CheckpointCleanup(string RetainedPath,Exception Error);

// Replace the working checkpoint only after closing Word; restore it if reopening fails.
public static class CheckpointSwap
{
    public const string CleanupDataKey="ChinhTa.CheckpointCleanup";
    public static CheckpointCleanup? Apply(string current,string candidate,Action close,Action reopen,Action<string>? deleteRollback=null)
    {
        if (Path.GetFullPath(current) == Path.GetFullPath(candidate)) throw new InvalidOperationException("Separate candidate required");
        string rollback = Path.Combine(Path.GetDirectoryName(current)!, Guid.NewGuid() + ".rollback.docx");
        File.Copy(current, rollback);
        bool closed = false, preserveRollback = false;
        Exception? operationFailure=null;
        CheckpointCleanup? cleanup=null;
        try
        {
            close(); closed = true;
            File.Copy(candidate, current, true);
            reopen();
        }
        catch (Exception failure)
        {
            operationFailure=failure;
            if (closed)
            {
                try { close(); File.Copy(rollback, current, true); reopen(); }
                catch (Exception restoreFailure)
                {
                    preserveRollback = true;
                    throw new AggregateException("Không mở lại được bản làm việc. Bản trước lượt sửa: " + rollback,
                        failure, restoreFailure);
                }
            }
            throw;
        }
        finally
        {
            if(!preserveRollback)
                try
                {
                    if(deleteRollback is null)File.Delete(rollback);else deleteRollback(rollback);
                }
                catch(Exception error) when(error is IOException or UnauthorizedAccessException)
                {
                    // Cleanup cannot undo a verified reopen or replace the actual failure.
                    // The caller logs this separately; the retained backup stays available.
                    cleanup=new(rollback,error);
                    if(operationFailure is not null)operationFailure.Data[CleanupDataKey]=cleanup;
                }
        }
        return cleanup;
    }
}
