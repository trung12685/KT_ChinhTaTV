using System.IO;
using System.Windows.Xps.Packaging;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

public sealed partial class WordSession
{
    private string ReadPageBodyXml()
    {dynamic content=document!.Content;try{return (string)content.WordOpenXML;}finally{Release(content);}}
    private SectionLayout ReadCurrentPageLayout()
    {
        dynamic sections=document!.Sections;
        try
        {
            if((int)sections.Count!=1)throw new InvalidOperationException("Số section thay đổi; chưa cho sửa trang.");
            dynamic section=sections[1];dynamic setup=section.PageSetup;dynamic range=section.Range;
            try
            {
                string text=(string)range.Text;if(text.Length==0)throw new InvalidOperationException("Section không có mốc chữ.");
                const double mm=25.4/72;
                return new(1,(int)range.Start,text[..1],(double)setup.PageWidth*mm,(double)setup.PageHeight*mm,
                    (double)setup.TopMargin*mm,(double)setup.BottomMargin*mm,(double)setup.LeftMargin*mm,(double)setup.RightMargin*mm,(int)setup.MirrorMargins!=0);
            }
            finally{Release(range);Release(setup);Release(section);}
        }
        finally{Release(sections);}
    }
    private void VerifyPagePreview(SectionLayout expected)
    {
        string path=ExportPreviewCore([],new Dictionary<Issue,PreviewAnchor>());
        try
        {
            using var xps=new XpsDocument(path,FileAccess.Read);var paginator=xps.GetFixedDocumentSequence().DocumentPaginator;
            paginator.ComputePageCount();
            if(paginator.PageCount<1)throw new InvalidOperationException("Chưa dựng được trang để hậu kiểm.");
            for(int i=0;i<paginator.PageCount;i++)
            {
                using var page=paginator.GetPage(i);
                if(Math.Abs(page.Size.Width-expected.WidthMm*96/25.4)>1 || Math.Abs(page.Size.Height-expected.HeightMm*96/25.4)>1)
                    throw new InvalidOperationException("Khổ trang XPS chưa khớp thiết lập đã duyệt; phải phục hồi.");
            }
        }
        finally{if(File.Exists(path))File.Delete(path);}
    }
    private RepairBatchResult FixPageSelection(IReadOnlyList<Issue> selection,int? failAfterStage=null)
    {
        var selected=PageLayoutBatch.Prepare(selection);var pageIssue=selected.Single(i=>i.PageFix is not null);var fix=pageIssue.PageFix!;
        var remaining=selected.Where(i=>i.PageFix is null).ToArray();var current=CheckCore();
        if(current.Count(i=>i.CanFix && i.Kind==FixKind.PageLayout && i.Start==pageIssue.Start && i.Original==pageIssue.Original && i.PageFix==fix)!=1)
            throw new InvalidOperationException("Gợi ý trang hoặc điều kiện sửa đã thay đổi; hãy quét lại.");
        BatchPlan.Validate(selected,ReadIssueText);
        var references=remaining.Select(i=>new PageSelectionReference(i,ReadPrefix(i))).ToArray();
        string original=ReadStoryText();
        var text=remaining.Where(i=>StagedRepairBatch.Stage(i)==RepairStage.Text).ToArray();
        string expected=AtomicTextBatch.ExpectedStory(text,original,ReadPrefix);
        var geometry=remaining.Where(i=>i.LineFix is not null || i.CenterFix is not null).Select(i=>new DeferredRepair(i,
            StagedRepairBatch.ExpectedReference(i,text,original,ReadPrefix))).ToArray();
        var geometryPrefixes=geometry.ToDictionary(r=>r.Requested,r=>AtomicTextBatch.ExpectedStory(
            text.Where(t=>(long)t.Start+t.Length<=r.Requested.Start).ToArray(),ReadPrefix(r.Requested),ReadPrefix));
        Checkpoint();string before=Path.Combine(folder,Guid.NewGuid()+".page.undo.docx"),candidate=Path.Combine(folder,Guid.NewGuid()+".page.candidate.docx");
        File.Copy(RecoveryPath,before);string xmlBefore=ReadPageBodyXml();
        int depth=undoCheckpoints.Count;bool previousChanged=Changed,restored=false,committed=false;
        var oldLines=verifiedLineEdits.ToArray();var previousProgress=repairProgress;repairProgress=new(selected.Count,ProgressReporter);
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
                    DocxPageLayoutEditor.VerifyBody(xmlBefore,ReadPageBodyXml(),fix);repairProgress?.Confirm(1);verifiedLineEdits.Clear();
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
                    if(ReadStoryText()!=expected || !PageLayoutFix.Matches(fix.After,ReadCurrentPageLayout()))throw new InvalidOperationException("Hậu kiểm cả lượt sửa trang/chữ chưa đạt.");
                    if(remaining.Length==0)DocxPageLayoutEditor.VerifyBody(xmlBefore,ReadPageBodyXml(),fix);
                    if(geometry.Length>0)
                    {
                        var final=ReadStagedReview(expected);
                        foreach(var request in geometry)
                        {
                            if(StagedGeometryResolver.Resolve(request,final) is not null)throw new InvalidOperationException("Còn mục hình học đã chọn chưa đạt sau cả lượt sửa trang.");
                            var probe=request.Requested.LineFix is { } line?final.Lines.Single(t=>t.Role==line.Role).Probe:
                                final.Centers.Single(e=>e.Target.Role==request.Requested.CenterFix!.Role).Target.Probe;
                            if(ReadPrefix(probe)!=geometryPrefixes[request.Requested])throw new InvalidOperationException("Mốc hình học sau sửa trang chưa khớp ngữ cảnh đã duyệt.");
                        }
                    }
                    VerifyPagePreview(fix.After);
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
            repairProgress=previousProgress;if(File.Exists(candidate))File.Delete(candidate);
            if(!committed && !restored)snapshotCurrent=false;
        }
    }
}
