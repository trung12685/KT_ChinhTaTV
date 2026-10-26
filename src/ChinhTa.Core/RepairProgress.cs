namespace ChinhTa.Core;

public enum RepairActivity { Preparing,Editing,Measuring,Verifying,Rescanning,Restoring,Completed }
public sealed record RepairProgressUpdate(RepairActivity Activity,int Verified,int Total,string Message)
{
    // Leave room for final verification/rescan; stage counts cannot advertise 100%.
    public double Fraction=>Activity==RepairActivity.Completed?1:Math.Clamp(Verified,0,Total)/(Total+1d);
    public string CountText=>$"Đã hậu kiểm {Verified}/{Total} mục";
}
// Observation only. A failed/closed progress view must never affect document edits,
// verification or rollback. Intermediate counts remain provisional for the batch.
public sealed class RepairProgressTracker(int total,Action<RepairProgressUpdate>? report)
{
    // Nested page/PAGE stages belong to the same user selection. Share its
    // denominator and provisional confirmations instead of restarting the UI.
    public static RepairProgressTracker ForSelection(RepairProgressTracker? current,int total,Action<RepairProgressUpdate>? report)=>current??new(total,report);
    public int Verified{get;private set;}
    public void Show(RepairActivity activity,string message)
    {
        if(activity==RepairActivity.Restoring)Verified=0;
        try{report?.Invoke(new(activity,Verified,total,message));}catch(Exception){/* UI observers cannot change transaction outcome. */}
    }
    public void Confirm(int count)
    {
        Verified=(int)Math.Clamp((long)Verified+Math.Max(count,0),0,total);
        Show(RepairActivity.Verifying,"Đã kiểm xong nhóm; còn hậu kiểm cả lượt và quét lại.");
    }
}
