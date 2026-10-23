using System.IO;
using System.Windows.Xps.Packaging;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

public sealed partial class WordSession
{
    private IReadOnlyList<AdministrativeCenterEvidence> ReadCenteringEvidence(string? artifact=null)
    {
        CheckCore();
        var probes=AdministrativePositionTargets.SelectMany(t=>t.References.Select(r=>r.Probe).Append(t.Probe)).Distinct().ToArray();
        var anchors=new Dictionary<Issue,PreviewAnchor>();
        string path=ExportPreviewCore(probes,anchors);
        if(artifact is not null)CaptureCheckArtifact(path,artifact);
        using var input=new XpsDocument(path,FileAccess.Read);
        var sequence=input.GetFixedDocumentSequence();var preview=new PreviewHighlighter();
        return AdministrativePositionTargets.Select(t=>AdministrativePositionInspection.MeasureCentering(sequence,preview,t,anchors)).ToArray();
    }
    internal void FixPositionDriftProbe(IEnumerable<Issue> issues)
    {
        var plan=BatchPlan.Create(issues);
        if(LineCheckArtifactFolder is null || plan.Count!=1 || plan[0].CenterFix is null)
            throw new InvalidOperationException("Chỉ dùng trong phiên kiểm Word riêng với một dòng căn giữa.");
        FixPositionBatch(plan,null,true);
    }
    private void FixPositionBatch(IReadOnlyList<Issue> plan,int? failAfter,bool driftUnselected=false)
    {
        BatchPlan.Validate(plan,ReadIssueText);
        Checkpoint();
        string original=ReadStoryText();
        var measured=ReadCenteringEvidence();
        AdministrativeCenterEvidence Match(Issue issue,IReadOnlyList<AdministrativeCenterEvidence> evidence)
        {
            var matches=evidence.Where(e=>e.Target.Role==issue.CenterFix!.Role && e.Target.Row.Start==issue.Start &&
                e.Target.Row.Text==issue.Original).ToArray();
            if(matches.Length!=1)throw new InvalidOperationException("Mốc căn giữa đã thay đổi; hãy quét lại.");
            return matches[0];
        }
        var beforeEvidence=plan.Select(item=>Match(item,measured)).ToArray();
        if(beforeEvidence.Any(e=>!AdministrativeCentering.CanOffer(e)) || ReadStoryText()!=original)
            throw new InvalidOperationException("Chưa đủ số đo mới để căn giữa an toàn. Hãy quét lại hoặc duyệt thủ công.");
        string before=Path.Combine(folder,Guid.NewGuid()+".undo.docx");File.Copy(RecoveryPath,before);
        bool previousChanged=Changed,started=false,restored=false;int applied=0;
        try
        {
            AtomicTextBatch.Apply(plan,ReadIssueText,item=>
            {
                Progress(RepairActivity.Editing,$"Đang căn giữa dòng {applied+1}/{plan.Count} trong nhóm.");
                started=true;Changed=true;snapshotCurrent=false;verifiedLineEdits.Clear();
                dynamic range=document!.Range(item.Start,item.Start+item.Length);
                try
                {
                    dynamic format=range.ParagraphFormat;
                    try {format.Alignment=1;format.LeftIndent=0;format.RightIndent=0;format.FirstLineIndent=0;}
                    finally{Release(format);}
                    if(++applied==failAfter)throw new OfficeTextProbeException();
                    return (string)range.Text;
                }
                finally{Release(range);}
            },()=>
            {
                if(driftUnselected)
                {
                    var other=measured.Single(e=>plan.All(i=>i.CenterFix!.Role!=e.Target.Role)).Target.Row;
                    dynamic range=document!.Range(other.Start,other.Start+other.Text.Length);
                    try
                    {
                        dynamic format=range.ParagraphFormat;
                        try{format.LeftIndent=(float)format.LeftIndent+12;}
                        finally{Release(format);}
                    }
                    finally{Release(range);}
                }
                Checkpoint();
                Progress(RepairActivity.Verifying,"Đang kiểm tâm dòng và bố cục trên XPS.");
                var after=ReadCenteringEvidence(driftUnselected?"position-unselected-drift.xps":null);
                if(ReadStoryText()!=original || !AdministrativeCentering.VerifyBatch(measured,after,plan.Select(i=>i.CenterFix!.Role).ToArray()))
                    throw new PositionVerificationException();
            },()=>
            {
                Progress(RepairActivity.Restoring,"Đang phục hồi cả nhóm căn giữa.");
                SwapCheckpoint(RecoveryPath,before,CloseConnection,Connect);
                var restoredEvidence=ReadCenteringEvidence();
                if(ReadStoryText()!=original || !AdministrativeCentering.VerifyRestored(measured,restoredEvidence))
                    throw new InvalidOperationException("Chưa xác minh đủ chữ và vị trí số/ngày sau phục hồi căn giữa.");
                restored=true;
            });
            undoCheckpoints.Push(before);Changed=true;snapshotCurrent=true;
        }
        catch(Exception error)
        {
            Changed=restored || !started?previousChanged:true;snapshotCurrent=restored || !started;
            Diagnostics.Write(restored?"Word.PositionBatch.RolledBack":"Word.PositionBatch.Failed",error,
                $"Selected={plan.Count}; Applied={applied}; Restored={(restored?1:0)}");
            if(error is AggregateException aggregate && aggregate.InnerExceptions.Count==2)
            {
                Diagnostics.Write("Word.PositionBatch.FailureCause",aggregate.InnerExceptions[0]);
                Diagnostics.Write("Word.PositionBatch.RestoreCause",aggregate.InnerExceptions[1]);
            }
            throw new InvalidOperationException(restored
                ? "Lượt căn giữa chưa hoàn tất; đã khôi phục bản trước. Hãy quét lại. Checkpoint: "+before
                : "Lượt căn giữa chưa hoàn tất; giữ bản trước để phục hồi: "+before,error);
        }
    }
}

internal sealed class PositionVerificationException():InvalidOperationException(
    "Tâm chữ trên XPS sau sửa chưa đạt, mất số đo hoặc bố cục khác bị thay đổi; phải khôi phục cả lượt.");
