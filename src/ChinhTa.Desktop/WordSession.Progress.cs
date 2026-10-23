using ChinhTa.Core;

namespace ChinhTa.Desktop;
public sealed partial class WordSession
{
    public Action<RepairProgressUpdate>? ProgressReporter{get;set;}
    private RepairProgressTracker? repairProgress;
    private void Progress(RepairActivity activity,string message)=>repairProgress?.Show(activity,message);
    private static string StageLabel(RepairStage stage)=>stage switch
    {RepairStage.Text=>"chữ, phông và thứ tự mục",RepairStage.Center=>"căn giữa số/ngày",_=>"đường kẻ"};
}
