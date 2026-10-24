namespace ChinhTa.Core;

// Dependencies are explicit selected text/font repairs, not permission to write
// any extra current finding. Geometry must be measured again after they finish.
public static class RepairDependencies
{
    public static bool Same(Issue a,Issue b)=>a.Start==b.Start && a.Length==b.Length && a.Original==b.Original &&
        a.Replacement==b.Replacement && a.Kind==b.Kind && a.OutlineContext==b.OutlineContext && a.Format==b.Format && a.CharacterFormat==b.CharacterFormat && a.ParagraphFix==b.ParagraphFix &&
        a.LineFix==b.LineFix && a.CenterFix==b.CenterFix;
    private static bool Leaf(Issue issue)=>issue.CanFix && StagedRepairBatch.Stage(issue)==RepairStage.Text &&
        issue.Prerequisites is not {Count:>0};
    public static IReadOnlyList<Issue> Expand(IEnumerable<Issue> selected,IReadOnlyList<Issue> available)
    {
        var result=new List<Issue>();
        foreach(var seed in selected)
        {
            var matches=available.Where(i=>i.CanFix && Same(i,seed)).ToArray();
            if(matches.Length!=1)throw new InvalidOperationException("Mục đã chọn thay đổi; hãy quét lại.");
            var current=matches[0];
            if(!result.Any(i=>Same(i,current)))result.Add(current);
            if(current.Prerequisites is not {Count:>0} required)continue;
            if(current.LineFix is null)throw new InvalidOperationException("Mục phụ thuộc chưa đúng vai trò đường kẻ.");
            foreach(var dependency in required)
            {
                var candidates=available.Where(i=>Leaf(i) && Same(i,dependency)).ToArray();
                if(!Leaf(dependency) || candidates.Length!=1)
                    throw new InvalidOperationException("Lỗi cần sửa trước đường kẻ đã thay đổi hoặc chỉ được duyệt thủ công; hãy quét lại.");
                if(!result.Any(i=>Same(i,candidates[0])))result.Add(candidates[0]);
            }
        }
        StagedRepairBatch.Prepare(result);
        return result;
    }
    public static void Validate(IReadOnlyList<Issue> selected)
    {
        foreach(var issue in selected)
        {
            if(issue.Prerequisites is not {Count:>0} required)continue;
            if(issue.LineFix is null || required.Any(d=>!Leaf(d) || selected.Count(i=>Leaf(i) && Same(i,d))!=1))
                throw new InvalidOperationException("Cần chọn đủ các lỗi chữ/phông liên quan trước khi sửa đường kẻ. Hãy quét lại và tích các mục được chỉ ra.");
        }
    }
}
