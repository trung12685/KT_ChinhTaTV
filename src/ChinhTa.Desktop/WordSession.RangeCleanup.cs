namespace ChinhTa.Desktop;

public sealed partial class WordSession
{
    internal void RecordTrackedRangeCleanup(Exception? failure)
    {
        if(failure is null)return;
        // Focused checks cannot certify clean ownership after an RCW release
        // failure, even if the document checkpoint was independently restored.
        if(LineCheckArtifactFolder is not null)checkCleanupFailures.Add(failure);
        try
        {
            if(failure is AggregateException aggregate)
                foreach(var cause in aggregate.Flatten().InnerExceptions)Diagnostics.Write("Word.TextRanges.CleanupRetained",cause);
            else Diagnostics.Write("Word.TextRanges.CleanupRetained",failure);
        }
        catch(Exception){/* Diagnostics cannot replace a commit or restoration outcome. */}
    }
}
