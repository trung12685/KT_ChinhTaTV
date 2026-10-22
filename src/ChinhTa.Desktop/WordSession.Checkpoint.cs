using System.IO;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

public sealed partial class WordSession
{
    private CheckpointCleanup? SwapCheckpoint(string current,string candidate,Action close,Action reopen,Action<string>? deleteRollback=null)
    {
        try
        {
            var cleanup=CheckpointSwap.Apply(current,candidate,close,reopen,deleteRollback);
            if(cleanup is not null)Diagnostics.Write("Word.CheckpointCleanup.Retained",cleanup.Error);
            return cleanup;
        }
        catch(Exception failure)
        {
            if(failure.Data[CheckpointSwap.CleanupDataKey] is CheckpointCleanup cleanup)
                Diagnostics.Write("Word.CheckpointCleanup.Retained",cleanup.Error);
            throw;
        }
    }
    internal bool UndoCleanupProbe()
    {
        if(LineCheckArtifactFolder is null)throw new InvalidOperationException("Chỉ dùng trong phiên kiểm Word riêng.");
        var cleanup=UndoCore(_=>throw new IOException("OFFICE-CHECK-INJECTED-CHECKPOINT-CLEANUP"));
        return cleanup is not null && File.Exists(cleanup.RetainedPath);
    }
}
