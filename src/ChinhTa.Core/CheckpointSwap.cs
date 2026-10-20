namespace ChinhTa.Core;

// Replace the working checkpoint only after closing Word; restore it if reopening fails.
public static class CheckpointSwap
{
    public static void Apply(string current, string candidate, Action close, Action reopen)
    {
        if (Path.GetFullPath(current) == Path.GetFullPath(candidate)) throw new InvalidOperationException("Separate candidate required");
        string rollback = Path.Combine(Path.GetDirectoryName(current)!, Guid.NewGuid() + ".rollback.docx");
        File.Copy(current, rollback);
        bool closed = false, preserveRollback = false;
        try
        {
            close(); closed = true;
            File.Copy(candidate, current, true);
            reopen();
        }
        catch (Exception failure)
        {
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
        finally { if (!preserveRollback) File.Delete(rollback); }
    }
}
