using System.IO;
using System.Windows.Xps.Packaging;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

public sealed partial class WordSession
{
    private string ReadPageBodyXml()
        =>PageNumberDocumentReader.ReadXml((object)document!);
    private SectionLayout ReadCurrentPageLayout()
        =>WordOwnedObjects.Read(own=>
        {
            dynamic sections=own((object)document!.Sections);
            if((int)sections.Count!=1)throw new InvalidOperationException("Số section thay đổi; chưa cho sửa trang.");
            return ReadOwnedSectionLayout((object)sections[1],1,Release)
                ??throw new InvalidOperationException("Section không có mốc chữ.");
        },Release);
    private void VerifyPagePreview(SectionLayout expected,bool checkBody=false)
    {
        if(checkBody)
        {
            var current=XDocument.Parse(ReadPageBodyXml(),LoadOptions.PreserveWhitespace);
            if(!EmptyHeaderFooterScope.HasReferences(current) || !EmptyHeaderFooterScope.Proven(current))
                throw new InvalidOperationException("Đầu/cuối trang không còn được chứng minh trống sau lượt sửa; phải phục hồi.");
        }
        string path=ExportPreviewCore([],new Dictionary<Issue,PreviewAnchor>());
        try
        {
            using var xps=new XpsDocument(path,FileAccess.Read);var sequence=xps.GetFixedDocumentSequence();var paginator=sequence.DocumentPaginator;
            paginator.ComputePageCount();
            if(paginator.PageCount<1)throw new InvalidOperationException("Chưa dựng được trang để hậu kiểm.");
            for(int i=0;i<paginator.PageCount;i++)
            {
                using var page=paginator.GetPage(i);
                if(Math.Abs(page.Size.Width-expected.WidthMm*96/25.4)>1 || Math.Abs(page.Size.Height-expected.HeightMm*96/25.4)>1)
                    throw new InvalidOperationException("Khổ trang XPS chưa khớp thiết lập đã duyệt; phải phục hồi.");
            }
            if(checkBody)PageLayoutBodyPreviewVerifier.Verify(sequence,expected);
        }
        finally{DeleteStagedBackup(path);}
    }
    private RepairBatchResult FixPageSelection(IReadOnlyList<Issue> selection,int? failAfterStage=null)
    {
        var selected=PageLayoutBatch.Prepare(selection);var pageIssue=selected.Single(i=>i.PageFix is not null);var fix=pageIssue.PageFix!;
        var remaining=selected.Where(i=>i.PageFix is null).ToArray();var current=CheckCore();
        if(current.Count(i=>i.CanFix && i.Kind==FixKind.PageLayout && i.Start==pageIssue.Start && i.Original==pageIssue.Original && i.PageFix==fix)!=1)
            throw new InvalidOperationException("Gợi ý trang hoặc điều kiện sửa đã thay đổi; hãy quét lại.");
        BatchPlan.Validate(selected,ReadIssueText);
        var references=remaining.Select(i=>new PageSelectionReference(i,ReadPrefix(i))).ToArray();
        string original=ReadStoryText();var postconditions=CaptureSelectionPostconditions(selected,original);
        Checkpoint();string before=Path.Combine(folder,Guid.NewGuid()+".page.undo.docx"),candidate=Path.Combine(folder,Guid.NewGuid()+".page.candidate.docx");
        File.Copy(RecoveryPath,before);string xmlBefore=ReadPageBodyXml();
        bool checkEmptyHeaderFooterBody=EmptyHeaderFooterScope.HasReferences(XDocument.Parse(xmlBefore));
        int depth=undoCheckpoints.Count;bool previousChanged=Changed,restored=false,committed=false;
        var oldLines=verifiedLineEdits.ToArray();var previousProgress=repairProgress;repairProgress=RepairProgressTracker.ForSelection(repairProgress,selected.Count,ProgressReporter);
        RepairBatchResult otherResult=new(0,0,0);
        try
        {
            PageLayoutBatch.Apply(
                ()=>
                {
                    Progress(RepairActivity.Preparing,"Đối chiếu gói DOCX và tạo bản đổi trang; giữ nguyên chữ/bảng/phần khác.");
                    DocxPageLayoutEditor.Write(before,candidate,fix);snapshotCurrent=false;
                    Progress(RepairActivity.Editing,"Đang áp dụng khổ giấy/lề đã duyệt và phân trang lại.");
                    SwapCheckpoint(RecoveryPath,candidate,CloseDocumentOnly,Connect);CheckCore();
                    if(ReadStoryText()!=original || !PageLayoutFix.Matches(fix.After,ReadCurrentPageLayout()))throw new InvalidOperationException("Chữ hoặc thiết lập sau đổi trang chưa đúng.");
                    DocxPageLayoutEditor.VerifyBody(xmlBefore,ReadPageBodyXml(),fix);
                    if(checkEmptyHeaderFooterBody)VerifyPagePreview(fix.After,true);
                    repairProgress?.Confirm(1);verifiedLineEdits.Clear();
                    if(failAfterStage==1)throw new OfficeStagedProbeException();
                },
                ()=>
                {
                    if(remaining.Length==0)return;
                    Progress(RepairActivity.Measuring,"Đang quét/đo lại các nhóm đã chọn theo khổ trang mới.");
                    var found=CheckCore();var review=ReadStagedReview(original);
                    var available=found.Concat(review.LineIssues).Concat(review.CenterIssues).Distinct().ToArray();
                    var satisfied=references.Where(r=>(r.Issue.LineFix is not null || r.Issue.CenterFix is not null) &&
                        StagedGeometryResolver.Resolve(new(r.Issue,r.Issue.Original),review) is null).ToArray();
                    foreach(var r in satisfied)
                    {
                        var probe=r.Issue.LineFix is { } line?review.Lines.Single(t=>t.Role==line.Role).Probe:
                            review.Centers.Single(e=>e.Target.Role==r.Issue.CenterFix!.Role).Target.Probe;
                        if(ReadPrefix(probe)!=r.Prefix)throw new InvalidOperationException("Mốc đã đạt sau đổi trang chưa khớp ngữ cảnh nguồn.");
                    }
                    repairProgress?.Confirm(satisfied.Length);
                    var rebound=PageLayoutBatch.Rebind(references.Except(satisfied).ToArray(),available,ReadPrefix);
                    if(rebound.Count==0)otherResult=new(remaining.Length,0,satisfied.Length);
                    else if(rebound.Select(StagedRepairBatch.Stage).Distinct().Count()>1)
                    {var result=FixStaged(rebound);otherResult=result with {Selected=remaining.Length,AlreadySatisfied=result.AlreadySatisfied+satisfied.Length};}
                    else{FixManyCore(rebound,true);repairProgress?.Confirm(rebound.Count);otherResult=new(remaining.Length,rebound.Count,satisfied.Length);}
                    if(failAfterStage is 2 or 3)throw new OfficeStagedProbeException();
                },
                ()=>
                {
                    Progress(RepairActivity.Verifying,"Kiểm cả lượt: thiết lập trang, chữ/bảng và trang XPS; các Line dùng số đo mới.");
                    Checkpoint();CheckCore();
                    if(ReadStoryText()!=postconditions.Expected || !PageLayoutFix.Matches(fix.After,ReadCurrentPageLayout()))throw new InvalidOperationException("Hậu kiểm cả lượt sửa trang/chữ chưa đạt.");
                    if(remaining.Length==0)DocxPageLayoutEditor.VerifyBody(xmlBefore,ReadPageBodyXml(),fix);
                    VerifySelectionPostconditions(postconditions);
                    VerifyPagePreview(fix.After,checkEmptyHeaderFooterBody);
                },
                ()=>
                {
                    while(undoCheckpoints.Count>depth)DeleteStagedBackup(undoCheckpoints.Pop());
                    undoCheckpoints.Push(before);Changed=true;snapshotCurrent=true;committed=true;
                },
                ()=>
                {
                    Progress(RepairActivity.Restoring,"Chưa đạt hậu kiểm; đang phục hồi cả lượt khổ giấy/lề và các nhóm đã chọn.");
                    SwapCheckpoint(RecoveryPath,before,CloseDocumentOnly,Connect);CheckCore();
                    if(ReadStoryText()!=original || !PageLayoutFix.Matches(fix.Before,ReadCurrentPageLayout()))throw new InvalidOperationException("Chưa xác minh được bản trang trước lượt sửa.");
                    restored=true;
                });
            return new(selected.Count,1+otherResult.Applied,otherResult.AlreadySatisfied);
        }
        catch(Exception failure)
        {
            while(undoCheckpoints.Count>depth){string path=undoCheckpoints.Pop();if(restored)DeleteStagedBackup(path);}
            verifiedLineEdits.Clear();if(restored)foreach(var entry in oldLines)verifiedLineEdits[entry.Key]=entry.Value;
            Changed=restored?previousChanged:true;snapshotCurrent=restored;
            Diagnostics.Write("Word.PageBatch",failure,$"Selected={selected.Count}; Restored={(restored?1:0)}");
            throw new InvalidOperationException(restored?"Lượt sửa trang chưa hoàn tất; đã phục hồi cả lượt. Checkpoint: "+before:
                "Lượt sửa trang chưa hoàn tất; giữ bản trước để phục hồi: "+before,failure);
        }
        finally
        {
            repairProgress=previousProgress;DeleteStagedBackup(candidate);
            if(!committed && !restored)snapshotCurrent=false;
        }
    }
}
