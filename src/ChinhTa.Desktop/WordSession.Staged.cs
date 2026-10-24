using System.IO;
using System.Windows.Xps.Packaging;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

public sealed partial class WordSession
{
    private sealed record StagedFormat(Issue Issue,string Text,string Prefix,FontSizeEvidence? Sizes);
    public RepairBatchResult FixSelection(IEnumerable<Issue> selection)
    {
        var plan=StagedRepairBatch.Prepare(selection);
        repairProgress=new(plan.Count,ProgressReporter);
        try
        {
            Progress(RepairActivity.Preparing,"Đối chiếu lại các mục đã chọn và tạo bản phục hồi.");
            if(plan.Select(StagedRepairBatch.Stage).Distinct().Count()>1)return FixStaged(plan);
            Progress(RepairActivity.Editing,"Đang sửa "+(plan.Count>0?StageLabel(StagedRepairBatch.Stage(plan[0])):"các mục đã chọn")+".");
            FixManyCore(plan,true);repairProgress.Confirm(plan.Count);
            return new(plan.Count,plan.Count,0);
        }
        finally{repairProgress=null;}
    }
    internal void FixStagedFailureProbe(IEnumerable<Issue> selection,int afterStage)
    {
        if(LineCheckArtifactFolder is null || afterStage is <1 or >3)
            throw new InvalidOperationException("Chỉ dùng trong phiên kiểm Word riêng.");
        var plan=StagedRepairBatch.Prepare(selection);
        if(plan.Select(StagedRepairBatch.Stage).Distinct().Count()<2)
            throw new InvalidOperationException("Mẫu kiểm cần nhiều nhóm sửa.");
        FixStaged(plan,afterStage);
    }
    internal int CheckUndoCount=>undoCheckpoints.Count;
    private string ReadPrefix(Issue issue)
    {
        dynamic content=document!.Content;int start;
        try{start=(int)content.Start;}finally{Release(content);}
        dynamic range=document!.Range(start,issue.Start);
        try{return (string?)range.Text??"";}finally{Release(range);}
    }
    private StagedGeometryReview ReadStagedReview(string expectedStory)
    {
        var found=CheckCore();
        var lines=LineTargets.ToArray();var centers=AdministrativePositionTargets.ToArray();
        var probes=lines.Select(t=>t.Probe).Concat(centers.SelectMany(t=>t.References.Select(r=>r.Probe).Append(t.Probe))).Distinct().ToArray();
        var anchors=new Dictionary<Issue,PreviewAnchor>();
        string path=ExportPreviewCore(probes,anchors);
        try
        {
            using var input=new XpsDocument(path,FileAccess.Read);
            var sequence=input.GetFixedDocumentSequence();var preview=new PreviewHighlighter();
            var lineIssues=LineInspection.Inspect(sequence,preview,lines,LineShapes,anchors,found,
                LinePolicy.Decree30,HasGroupedShapes,VerifiedLineEdits).Select(e=>e.Issue).ToArray();
            var evidence=centers.Select(t=>AdministrativePositionInspection.MeasureCentering(sequence,preview,t,anchors)).ToArray();
            var centerIssues=AdministrativePositionInspection.Inspect(sequence,preview,centers,anchors).Select(e=>e.Issue).ToArray();
            if(ReadStoryText()!=expectedStory)throw new InvalidOperationException("Nội dung thay đổi giữa các bước sửa; phải khôi phục cả lượt.");
            return new(lines,lineIssues,evidence,centerIssues);
        }
        finally{if(File.Exists(path))File.Delete(path);}
    }
    private void VerifyStagedFormats(IReadOnlyList<StagedFormat> formats)
    {
        foreach(var proof in formats)
        {
            var matches=lastFormatLayout.Where(p=>p.Text.TrimEnd('\r','\a','\n')==proof.Text &&
                ReadPrefix(proof.Issue with {Start=p.Start})==proof.Prefix).ToArray();
            if(matches.Length!=1)throw new InvalidOperationException("Chưa đối chiếu lại được vùng phông sau các nhóm sửa.");
            dynamic range=document!.Range(matches[0].Start,matches[0].Start+proof.Text.Length);
            dynamic? paragraph=null;
            try
            {
                int? alignment=null;
                if(proof.Issue.Format?.Alignment is not null){paragraph=range.ParagraphFormat;alignment=(int)paragraph.Alignment;}
                bool correct=VisibleFormatReader.Matches((object)range,proof.Text,proof.Issue.CharacterFormat,proof.Issue.Format,alignment);
                if((string)range.Text!=proof.Text || !correct)
                    throw new InvalidOperationException("Phông sau các nhóm sửa không còn khớp đề xuất; phải khôi phục cả lượt.");
                if(proof.Sizes is not null && !proof.Sizes.Matches(VisibleFormatReader.ReadSizes((object)range,proof.Text)))
                    throw new InvalidOperationException("Cỡ chữ đã đổi sau các nhóm sửa; phải khôi phục cả lượt.");
            }
            finally{Release(paragraph);Release(range);}
        }
    }
    private RepairBatchResult FixStaged(IReadOnlyList<Issue> selected,int? failAfterStage=null)
    {
        var text=selected.Where(i=>StagedRepairBatch.Stage(i)==RepairStage.Text).ToArray();
        var deferred=selected.Where(i=>StagedRepairBatch.Stage(i)!=RepairStage.Text).ToArray();
        var current=CheckCore();CurrentTextPlan.Validate(text,current);
        BatchPlan.Validate(selected,ReadIssueText);
        foreach(var item in deferred)
        {
            bool bound=item.LineFix is ShapeLineFix line?
                LineTargets.Count(t=>t.Role==line.Role)==1 && LineTargets.Any(t=>t.Role==line.Role && t.Start==item.Start && t.Text==item.Original):
                AdministrativePositionTargets.Count(t=>t.Role==item.CenterFix!.Role)==1 &&
                    AdministrativePositionTargets.Any(t=>t.Role==item.CenterFix!.Role && t.Row.Start==item.Start && t.Row.Text==item.Original);
            if(!bound)throw new InvalidOperationException("Mốc nhóm đã chọn thay đổi; hãy quét lại trước khi sửa.");
        }
        Checkpoint();
        string original=ReadStoryText(),expected=AtomicTextBatch.ExpectedStory(text,original,ReadPrefix);
        var requests=selected.Select(i=>new DeferredRepair(i,StagedRepairBatch.Stage(i)==RepairStage.Text?i.Replacement:
            StagedRepairBatch.ExpectedReference(i,text,original,ReadPrefix))).ToArray();
        var formats=text.Where(i=>i.Format is not null || i.CharacterFormat is not null).Select(i=>new StagedFormat(i,
            StagedRepairBatch.ExpectedReference(i,text,original,ReadPrefix),
            AtomicTextBatch.ExpectedStory(text.Where(t=>(long)t.Start+t.Length<=i.Start).ToArray(),ReadPrefix(i),ReadPrefix),ExpectedSizesFor(i,text))).ToArray();
        string before=Path.Combine(folder,Guid.NewGuid()+".multi.undo.docx");File.Copy(RecoveryPath,before);
        int depth=undoCheckpoints.Count,completed=0;
        bool previousChanged=Changed,restored=false,committed=false;
        var previousLines=verifiedLineEdits.ToArray();StagedGeometryReview? review=null;
        IReadOnlyList<AdministrativeCenterEvidence>? beforeLines=null;
        void TrimInterimUndo(bool delete)
        {
            while(undoCheckpoints.Count>depth)
            {
                string path=undoCheckpoints.Pop();
                if(delete)DeleteStagedBackup(path);
            }
        }
        try
        {
            var result=StagedRepairBatch.Apply(requests,
                stage=>
                {
                    Progress(RepairActivity.Measuring,"Đo lại trước nhóm "+StageLabel(stage)+".");
                    review=ReadStagedReview(expected);
                    if(stage==RepairStage.Line)beforeLines=review.Centers;
                },
                request=>
                {
                    var resolved=StagedGeometryResolver.Resolve(request,review??throw new InvalidOperationException("Chưa đo lại nhóm sửa."));
                    if(resolved is null)repairProgress?.Confirm(1);
                    return resolved;
                },
                plan=>
                {
                    Progress(RepairActivity.Editing,"Đang sửa "+StageLabel(StagedRepairBatch.Stage(plan[0]))+".");
                    FixManyCore(plan,true);repairProgress?.Confirm(plan.Count);
                    Diagnostics.Write("Word.StagedBatch.StageVerified",null,$"Stage={StagedRepairBatch.Stage(plan[0])}; Selected={plan.Count}");
                    completed++;
                    if((int)StagedRepairBatch.Stage(plan[0])+1==failAfterStage)throw new OfficeStagedProbeException();
                },
                ()=>
                {
                    Progress(RepairActivity.Verifying,"Kiểm lại cả lượt: chữ, phông, tâm dòng và nét XPS.");
                    // One final measurement checks the selected centers again after Line
                    // insertion/reopen, and every selected Line against its current text.
                    review=ReadStagedReview(expected);
                    if(beforeLines is not null && !StagedGeometryResolver.PreservesCenters(beforeLines,review.Centers))
                        throw new InvalidOperationException("Vị trí số/ngày bị thay đổi khi ghi Line; phải khôi phục cả lượt.");
                    if(requests.Where(r=>StagedRepairBatch.Stage(r.Requested)!=RepairStage.Text).Any(r=>StagedGeometryResolver.Resolve(r,review) is not null))
                        throw new InvalidOperationException("Còn mục đã chọn chưa đạt sau hậu kiểm; phải khôi phục cả lượt.");
                    VerifyStagedFormats(formats);
                },
                ()=>
                {
                    TrimInterimUndo(true);undoCheckpoints.Push(before);
                    Changed=true;snapshotCurrent=true;committed=true;
                },
                ()=>
                {
                    Progress(RepairActivity.Restoring,"Chưa đạt hậu kiểm; đang phục hồi cả lượt sửa.");
                    SwapCheckpoint(RecoveryPath,before,CloseConnection,Connect);
                    if(ReadStoryText()!=original)throw new InvalidOperationException("Chưa xác minh được chữ sau phục hồi nhiều nhóm.");
                    restored=true;
                });
            Diagnostics.Write("Word.StagedBatch.Verified",null,$"Selected={result.Selected}; Applied={result.Applied}; AlreadySatisfied={result.AlreadySatisfied}");
            return result;
        }
        catch(Exception failure)
        {
            TrimInterimUndo(restored);verifiedLineEdits.Clear();
            if(restored)foreach(var item in previousLines)verifiedLineEdits[item.Key]=item.Value;
            Changed=restored?previousChanged:true;snapshotCurrent=restored;
            Diagnostics.Write(restored?"Word.StagedBatch.RolledBack":"Word.StagedBatch.Failed",failure,
                $"Selected={selected.Count}; CompletedStages={completed}; Restored={(restored?1:0)}");
            throw new InvalidOperationException(restored?
                "Lượt sửa nhiều nhóm chưa hoàn tất; đã phục hồi cả lượt. Hãy quét lại. Checkpoint: "+before:
                "Lượt sửa nhiều nhóm chưa hoàn tất; giữ bản trước để phục hồi: "+before,failure);
        }
        finally{if(!committed && !restored)snapshotCurrent=false;}
    }
    private static void DeleteStagedBackup(string path)
    {
        try{File.Delete(path);}
        catch(Exception error) when(error is IOException or UnauthorizedAccessException)
        {Diagnostics.Write("Word.StagedBatch.CleanupRetained",error);}
    }
}

internal sealed class OfficeStagedProbeException():InvalidOperationException("OFFICE-CHECK-INJECTED-STAGED-FAILURE");
