namespace ChinhTa.Core;

public sealed record StagedGeometryReview(IReadOnlyList<LineTarget> Lines,IReadOnlyList<Issue> LineIssues,
        IReadOnlyList<AdministrativeCenterEvidence> Centers,IReadOnlyList<Issue> CenterIssues);

// Input comes from the same fresh XPS inspectors as the user-facing review list.
public static class StagedGeometryResolver
{
    public static bool PreservesCenters(IReadOnlyList<AdministrativeCenterEvidence> before,IReadOnlyList<AdministrativeCenterEvidence> after)
    {
        if(before.Count==0)return after.Count==0;
        if(before.Count!=after.Count || before.Select(e=>e.Target.Role).Distinct().Count()!=before.Count ||
            after.Select(e=>e.Target.Role).Distinct().Count()!=after.Count)return false;
        var rebound=new List<AdministrativeCenterEvidence>();
        foreach(var previous in before)
        {
            var current=after.SingleOrDefault(e=>e.Target.Role==previous.Target.Role);
            if(current is null || current.Target.Row.Text!=previous.Target.Row.Text ||
                current.Target.ReferencesCentered!=previous.Target.ReferencesCentered ||
                !current.Target.References.Select(r=>r.Text).SequenceEqual(previous.Target.References.Select(r=>r.Text)))return false;
            // Save/reopen can change Word CPs. Rebind the same role/text while
            // retaining every actual observation of geometry and missing evidence.
            rebound.Add(current with {Target=previous.Target});
        }
        return AdministrativeCentering.VerifyRestored(before,rebound);
    }
    public static Issue? Resolve(DeferredRepair request,StagedGeometryReview review)
    {
        if(request.Requested.LineFix is ShapeLineFix line)
        {
            var targets=review.Lines.Where(t=>t.Role==line.Role).ToArray();
            if(targets.Length!=1 || targets[0].Text!=request.ExpectedText)
                throw new InvalidOperationException("Không nhận diện lại được đúng mốc đường kẻ đã chọn; phải khôi phục cả lượt.");
            var target=targets[0];
            var warnings=review.LineIssues.Where(i=>i.Start==target.Start && i.Original==target.Text).ToArray();
            // Inspect emits a notice for every unmeasured/blocked target. Absence here
            // follows a positive real-stroke assessment of this uniquely bound target.
            if(warnings.Length==0)return null;
            if(warnings.Length!=1 || !warnings[0].CanFix || warnings[0].LineFix?.Role!=line.Role)
                throw new InvalidOperationException("Đường kẻ đã chọn chưa đủ bằng chứng đo lại để sửa an toàn; phải khôi phục cả lượt.");
            return warnings[0];
        }
        if(request.Requested.CenterFix is AdministrativeCenterFix center)
        {
            var evidence=review.Centers.Where(e=>e.Target.Role==center.Role).ToArray();
            if(evidence.Length!=1 || evidence[0].Target.Row.Text!=request.ExpectedText || !AdministrativeCentering.Proven(evidence[0]))
                throw new InvalidOperationException("Chưa xác minh lại được đúng dòng căn giữa đã chọn; phải khôi phục cả lượt.");
            if(!AdministrativeCentering.CanOffer(evidence[0]))return null;
            var warnings=review.CenterIssues.Where(i=>i.CanFix && i.CenterFix?.Role==center.Role &&
                i.Start==evidence[0].Target.Row.Start && i.Original==request.ExpectedText).ToArray();
            if(warnings.Length!=1)throw new InvalidOperationException("Chưa có đề xuất căn giữa mới khớp dòng đã chọn.");
            return warnings[0];
        }
        throw new InvalidOperationException("Nhóm sửa không hợp lệ.");
    }
}
