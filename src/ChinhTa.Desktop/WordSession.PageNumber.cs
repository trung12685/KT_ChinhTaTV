using System.IO;
using System.Windows.Xps.Packaging;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

public sealed partial class WordSession
{
    private (PageNumberPackage Package,int Pages) CurrentNumberPackage()=>
        (PageNumberDocumentReader.ReadCurrent((object)document!),(int)document!.ComputeStatistics(2));
    private PageNumberVisualSnapshot NumberPreview(SectionLayout layout,bool verify)
    {
        string path=ExportPreviewCore([],new Dictionary<Issue,PreviewAnchor>());
        try
        {
            using var xps=new XpsDocument(path,FileAccess.Read);var sequence=xps.GetFixedDocumentSequence();
            if(verify)PageNumberPreviewVerifier.VerifyNumbers(sequence,layout.LeftMm,layout.RightMm,layout.TopMm);
            return PageNumberPreviewVerifier.Capture(sequence,layout.TopMm);
        }
        finally{if(File.Exists(path))File.Delete(path);}
    }
    private RepairBatchResult FixNumberSelection(IReadOnlyList<Issue> selection,int? failAfterStage=null)
    {
        var selected=PageLayoutBatch.Prepare(selection);var item=selected.Single(i=>i.NumberFix is not null);var fix=item.NumberFix!;
        var remaining=selected.Where(i=>i.NumberFix is null).ToArray();var current=CheckCore();
        if(current.Count(i=>i.CanFix && i.Kind==FixKind.PageNumber && i.NumberFix==fix && i.Start==item.Start && i.Original==item.Original)!=1)
            throw new InvalidOperationException("Gợi ý PAGE đã đổi; hãy quét lại.");
        BatchPlan.Validate(selected,ReadIssueText);Checkpoint();
        string before=Path.Combine(folder,Guid.NewGuid()+".number.undo.docx"),candidate=Path.Combine(folder,Guid.NewGuid()+".number.candidate.docx");File.Copy(RecoveryPath,before);
        var rollbackNumbers=CurrentNumberPackage();string rollbackKey=PageNumberRules.ConfigurationKey(rollbackNumbers.Package,rollbackNumbers.Pages);
        string original=ReadStoryText();int depth=undoCheckpoints.Count;bool previousChanged=Changed,restored=false,committed=false,numberSatisfied=false;
        var previousLines=verifiedLineEdits.ToArray();var previousProgress=repairProgress;repairProgress=new(selected.Count,ProgressReporter);
        RepairBatchResult otherResult=new(0,0,0);
        try
        {
            PageLayoutBatch.Apply(
                ()=>
                {
                    if(remaining.Length==0)return;
                    Progress(RepairActivity.Editing,"Đang sửa các nhóm đã chọn trước khi đối chiếu PAGE theo phân trang mới.");
                    if(remaining.Any(i=>i.PageFix is not null))
                    {otherResult=FixPageSelection(remaining);repairProgress?.Confirm(otherResult.Selected);}
                    else if(remaining.Select(StagedRepairBatch.Stage).Distinct().Count()>1)otherResult=FixStaged(remaining);
                    else{FixManyCore(remaining,true);repairProgress?.Confirm(remaining.Length);otherResult=new(remaining.Length,remaining.Length,0);}
                    if(failAfterStage==1)throw new OfficeStagedProbeException();
                },
                ()=>
                {
                    Progress(RepairActivity.Measuring,"Đang đối chiếu header/footer và số trang sau các nhóm sửa.");
                    var findings=CheckCore();var live=CurrentNumberPackage();
                    var matching=findings.Where(i=>i.CanFix && i.NumberFix==fix).ToArray();
                    if(matching.Length==0 && PageNumberRules.Satisfied(live.Package,live.Pages,fix))
                    {numberSatisfied=true;repairProgress?.Confirm(1);return;}
                    if(matching.Length!=1)throw new InvalidOperationException("Cấu hình PAGE không còn khớp phần đã duyệt; phải phục hồi cả lượt.");
                    string textBefore=ReadStoryText();var layout=ReadCurrentPageLayout();var visualBefore=NumberPreview(layout,false);
                    Checkpoint();string source=Path.Combine(folder,Guid.NewGuid()+".number.source.docx");File.Copy(RecoveryPath,source);
                    try
                    {
                        DocxPageNumberEditor.Write(source,candidate,fix,live.Pages);snapshotCurrent=false;
                        Progress(RepairActivity.Editing,"Đang đánh số tự động ở lề trên, ẩn trang đầu và giữ footer cũ.");
                        SwapCheckpoint(RecoveryPath,candidate,CloseDocumentOnly,Connect);CheckCore();
                        var after=CurrentNumberPackage();
                        if(ReadStoryText()!=textBefore || !PageNumberRules.ProvenStandard(after.Package,after.Pages))throw new InvalidOperationException("Header PAGE hoặc phần thân chưa đạt sau mở lại.");
                        DocxPageNumberEditor.VerifyLive(live.Package,after.Package);
                        PageNumberPreviewVerifier.Preserve(visualBefore,NumberPreview(layout,true));
                        Checkpoint();CheckCore();after=CurrentNumberPackage();
                        if(!PageNumberRules.ProvenStandard(after.Package,after.Pages))throw new InvalidOperationException("PAGE chưa đạt sau lưu.");
                        DocxPageNumberEditor.VerifyLive(live.Package,after.Package);
                        PageNumberPreviewVerifier.Preserve(visualBefore,NumberPreview(layout,true));repairProgress?.Confirm(1);
                    }
                    finally{if(File.Exists(source))File.Delete(source);}
                    if(failAfterStage is 2 or 3)throw new OfficeStagedProbeException();
                },
                ()=>
                {
                    Progress(RepairActivity.Verifying,"Kiểm lại cả lượt: PAGE thực trên XPS, nội dung và kết quả các nhóm đã chọn.");
                    var live=CurrentNumberPackage();
                    if(numberSatisfied?!PageNumberRules.Satisfied(live.Package,live.Pages,fix):!PageNumberRules.ProvenStandard(live.Package,live.Pages))
                        throw new InvalidOperationException("PAGE chưa đạt ở bước hoàn tất.");
                },
                ()=>
                {
                    while(undoCheckpoints.Count>depth)DeleteStagedBackup(undoCheckpoints.Pop());
                    undoCheckpoints.Push(before);Changed=true;snapshotCurrent=true;committed=true;
                },
                ()=>
                {
                    Progress(RepairActivity.Restoring,"Chưa đạt PAGE/XPS; đang phục hồi cả lượt và header/footer trước sửa.");
                    SwapCheckpoint(RecoveryPath,before,CloseDocumentOnly,Connect);CheckCore();
                    var restoredNumbers=CurrentNumberPackage();
                    if(ReadStoryText()!=original || PageNumberRules.ConfigurationKey(restoredNumbers.Package,restoredNumbers.Pages)!=rollbackKey)
                        throw new InvalidOperationException("Chưa xác minh phần thân/header/footer sau phục hồi PAGE.");restored=true;
                });
            return new(selected.Count,otherResult.Applied+(numberSatisfied?0:1),otherResult.AlreadySatisfied+(numberSatisfied?1:0));
        }
        catch(Exception failure)
        {
            while(undoCheckpoints.Count>depth){string path=undoCheckpoints.Pop();if(restored)DeleteStagedBackup(path);}
            verifiedLineEdits.Clear();if(restored)foreach(var pair in previousLines)verifiedLineEdits[pair.Key]=pair.Value;
            Changed=restored?previousChanged:true;snapshotCurrent=restored;
            Diagnostics.Write("Word.PageNumberBatch",failure,$"Restored={(restored?1:0)}; Selected={selected.Count}");
            throw new InvalidOperationException(restored?"Sửa PAGE chưa hoàn tất; đã phục hồi cả lượt. Checkpoint: "+before:
                "Sửa PAGE chưa hoàn tất; giữ checkpoint để phục hồi: "+before,failure);
        }
        finally
        {repairProgress=previousProgress;if(File.Exists(candidate))File.Delete(candidate);if(!committed && !restored)snapshotCurrent=false;}
    }
}
