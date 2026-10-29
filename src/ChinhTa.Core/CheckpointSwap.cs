namespace ChinhTa.Core;

public sealed record CheckpointCleanup(string RetainedPath,Exception Error);

public sealed class CheckpointRestoreException(string rollbackPath,Exception operationFailure,Exception restoreFailure)
    : AggregateException("Không mở lại được bản làm việc. Bản trước lượt sửa: "+rollbackPath,operationFailure,restoreFailure)
{
    public string RollbackPath { get; }=rollbackPath;
}

// Replace the working checkpoint only after closing Word; restore it if reopening fails.
public static class CheckpointSwap
{
    public const string CleanupDataKey="ChinhTa.CheckpointCleanup";
    private static readonly object OutcomeDataKey=new();
    private enum RecoveryOutcome { Unchanged,Restored,Unverified }

    // Only an outcome recorded by this boundary establishes that its current
    // checkpoint is safe. Exception shape (including nested AggregateException)
    // cannot distinguish an operation error from a failed restoration.
    public static bool CurrentIsVerified(Exception failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return failure.Data[OutcomeDataKey] is RecoveryOutcome.Unchanged or RecoveryOutcome.Restored;
    }

    public static CheckpointCleanup? Apply(string current,string candidate,Action close,Action reopen,Action<string>? deleteRollback=null)
    {
        string rollback="";
        bool closed = false, backupCopied=false, preserveRollback = false;
        Exception? operationFailure=null;
        CheckpointCleanup? cleanup=null;
        try
        {
            if (Path.GetFullPath(current) == Path.GetFullPath(candidate)) throw new InvalidOperationException("Separate candidate required");
            rollback = Path.Combine(Path.GetDirectoryName(current)!, Guid.NewGuid() + ".rollback.docx");
            File.Copy(current, rollback);backupCopied=true;
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
                    var recoveryFailure=new CheckpointRestoreException(rollback,failure,restoreFailure);
                    recoveryFailure.Data[OutcomeDataKey]=RecoveryOutcome.Unverified;
                    throw recoveryFailure;
                }
            }
            // Overwrite any outcome carried by an inner operation: this outer
            // swap has independently preserved/restored its own current file.
            failure.Data[OutcomeDataKey]=closed?RecoveryOutcome.Restored:
                backupCopied?RecoveryOutcome.Unchanged:RecoveryOutcome.Unverified;
            throw;
        }
        finally
        {
            if(backupCopied && !preserveRollback)
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
