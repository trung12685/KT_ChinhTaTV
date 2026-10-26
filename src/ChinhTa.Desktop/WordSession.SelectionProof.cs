using ChinhTa.Core;

namespace ChinhTa.Desktop;

public sealed partial class WordSession
{
    private sealed record GeometryPostcondition(DeferredRepair Request,string Prefix);
    private sealed record SelectionPostconditions(string Expected,IReadOnlyList<SelectedFormatProof> Formats,
        IReadOnlyList<BodySpacingProof> Spacing,IReadOnlyList<GeometryPostcondition> Geometry,PageLayoutFix? Page);

    private List<SelectedFormatProof> CaptureStagedFormats(IReadOnlyList<Issue> text,string original)
    {
        var formats=text.Where(i=>i.Format is not null || i.CharacterFormat is not null).Select(i=>new SelectedFormatProof(i,
            StagedRepairBatch.ExpectedReference(i,text,original,ReadPrefix),
            AtomicTextBatch.ExpectedStory(text.Where(t=>(long)t.Start+t.Length<=i.Start).ToArray(),ReadPrefix(i),ReadPrefix),ExpectedSizesFor(i,text))).ToList();
        foreach(var probe in CaseTrackingRegions(text))
        {
            var styles=ExpectedCaseStyles(probe,text);
            formats.Add(new(probe,styles.Text,
                AtomicTextBatch.ExpectedStory(text.Where(t=>(long)t.Start+t.Length<=probe.Start).ToArray(),ReadPrefix(probe),ReadPrefix),null,styles));
        }
        return formats;
    }
    // Capture approvals before any stage writes. Main-story probes remain
    // distinct from page/PAGE anchors even when they share a character position.
    private SelectionPostconditions CaptureSelectionPostconditions(IReadOnlyList<Issue> selected,string original)
    {
        var body=selected.Where(i=>i.NumberFix is null && i.PageFix is null).ToArray();
        var text=body.Where(i=>StagedRepairBatch.Stage(i)==RepairStage.Text).ToArray();
        var geometry=body.Where(i=>i.LineFix is not null || i.CenterFix is not null).Select(i=>new GeometryPostcondition(
            new(i,StagedRepairBatch.ExpectedReference(i,text,original,ReadPrefix)),
            AtomicTextBatch.ExpectedStory(text.Where(t=>(long)t.Start+t.Length<=i.Start).ToArray(),ReadPrefix(i),ReadPrefix))).ToArray();
        return new(AtomicTextBatch.ExpectedStory(text,original,ReadPrefix),CaptureStagedFormats(text,original),
            BodySpacingVerification.Build(text,original,ReadPrefix),geometry,selected.SingleOrDefault(i=>i.PageFix is not null)?.PageFix);
    }
    private void VerifySelectionPostconditions(SelectionPostconditions proof)
    {
        if(ReadStoryText()!=proof.Expected)throw new InvalidOperationException("Nội dung sau cả lượt không khớp các mục đã chọn; phải phục hồi.");
        if(proof.Geometry.Count>0)
        {
            var current=ReadStagedReview(proof.Expected);
            foreach(var entry in proof.Geometry)
            {
                var request=entry.Request;
                if(StagedGeometryResolver.Resolve(request,current) is not null)
                    throw new InvalidOperationException("Còn mục hình học đã chọn chưa đạt sau cả lượt; phải phục hồi.");
                var probe=request.Requested.LineFix is { } line?current.Lines.Single(t=>t.Role==line.Role).Probe:
                    current.Centers.Single(e=>e.Target.Role==request.Requested.CenterFix!.Role).Target.Probe;
                if(ReadPrefix(probe)!=entry.Prefix)throw new InvalidOperationException("Mốc hình học sau cả lượt chưa khớp ngữ cảnh đã duyệt.");
            }
        }
        else if(proof.Formats.Count>0 || proof.Spacing.Count>0)CheckCore();
        VerifyStagedFormats(proof.Formats);VerifyBodySpacing(proof.Spacing,proof.Expected);
        if(proof.Page is { } page && !PageLayoutFix.Matches(page.After,ReadCurrentPageLayout()))
            throw new InvalidOperationException("Thiết lập trang thay đổi sau nhóm sửa tiếp theo; phải phục hồi.");
    }
}
