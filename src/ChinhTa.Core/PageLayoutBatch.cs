namespace ChinhTa.Core;

public sealed record PageSelectionReference(Issue Issue,string Prefix);
public static class PageLayoutBatch
{
    public static IReadOnlyList<Issue> Prepare(IEnumerable<Issue> selection)
    {
        var all=selection.Distinct().ToArray();var numbers=all.Where(i=>i.NumberFix is not null).ToArray();
        if(numbers.Length>1)throw new InvalidOperationException("Chỉ có một đề xuất PAGE trong section đã xác minh.");
        BatchPlan.Create(numbers);
        var items=all.Where(i=>i.NumberFix is null).ToArray();var pages=items.Where(i=>i.PageFix is not null).ToArray();
        if(pages.Length>1)throw new InvalidOperationException("Chỉ có một đề xuất cho section dọc đã xác minh.");
        BatchPlan.Create(pages);StagedRepairBatch.Prepare(items.Where(i=>i.PageFix is null));return all;
    }
    public static IReadOnlyList<Issue> Rebind(IReadOnlyList<PageSelectionReference> selected,IReadOnlyList<Issue> current,Func<Issue,string> prefix)
    {
        var result=new List<Issue>();
        foreach(var item in selected)
        {
            var old=item.Issue;
            var matches=current.Where(i=>i.CanFix && i.PageFix is null && i.Kind==old.Kind && i.Original==old.Original && i.Replacement==old.Replacement &&
                i.Length==old.Length && (old.LineFix is { } line?i.LineFix?.Role==line.Role:
                old.CenterFix is { } center?i.CenterFix?.Role==center.Role:
                i.LineFix is null && i.CenterFix is null && i.CharacterFormat==old.CharacterFormat && i.Format==old.Format &&
                    i.OutlineContext==old.OutlineContext && i.ParagraphFix==old.ParagraphFix && i.SpacingFix==old.SpacingFix) && prefix(i)==item.Prefix).ToArray();
            if(matches.Length!=1)throw new InvalidOperationException("Mục đã chọn không còn khớp sau đổi trang; phải phục hồi và quét lại.");
            result.Add(matches[0]);
        }
        StagedRepairBatch.Prepare(result);return result;
    }
    public static void Apply(Action page,Action remaining,Action verify,Action commit,Action restore)
    {
        try{page();remaining();verify();commit();}
        catch(Exception failure)
        {
            try{restore();}catch(Exception restoration){throw new AggregateException("Chưa phục hồi được lượt sửa thiết lập trang; giữ checkpoint.",failure,restoration);}
            throw;
        }
    }
}
