using System.IO;
using System.Windows.Xps.Packaging;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

public sealed partial class WordSession
{
    private IReadOnlyList<AdministrativeCenterEvidence> ReadCenteringEvidence()
    {
        CheckCore();
        var probes=AdministrativePositionTargets.SelectMany(t=>t.References.Select(r=>r.Probe).Append(t.Probe)).Distinct().ToArray();
        var anchors=new Dictionary<Issue,PreviewAnchor>();
        string path=ExportPreviewCore(probes,anchors);
        using var input=new XpsDocument(path,FileAccess.Read);
        var sequence=input.GetFixedDocumentSequence();var preview=new PreviewHighlighter();
        return AdministrativePositionTargets.Select(t=>AdministrativePositionInspection.MeasureCentering(sequence,preview,t,anchors)).ToArray();
    }
    private void FixPositionBatch(IReadOnlyList<Issue> plan,int? failAfter)
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
                Checkpoint();
                var after=ReadCenteringEvidence();
                if(ReadStoryText()!=original || plan.Where((item,i)=>!AdministrativeCentering.Verify(beforeEvidence[i],Match(item,after))).Any())
                    throw new InvalidOperationException("Tâm chữ trên XPS sau sửa chưa đạt hoặc bố cục khác bị thay đổi; phải khôi phục cả lượt.");
            },()=>
            {
                CheckpointSwap.Apply(RecoveryPath,before,CloseConnection,Connect);
                if(ReadStoryText()!=original)throw new InvalidOperationException("Chưa xác minh chữ sau phục hồi căn giữa.");
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
