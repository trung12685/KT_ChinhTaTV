namespace ChinhTa.Core;

// Callers supply Word's live range text, not guessed string offsets or replayed mutations.
public static class AtomicTextBatch
{
    public static string ExpectedStory(IReadOnlyList<Issue> selection,string story,Func<Issue,string> readPrefix)
    {
        var plan=TextPlan(selection);
        var mapped=plan.Select(item=>item with
        {
            Start=WordPreviewAnchor.Create(1,story,readPrefix(item),item.Original).Offset
        }).ToArray();
        var stringPlan=BatchPlan.Create(mapped);
        if(stringPlan.Count!=plan.Count || !mapped.Select(i=>i.Start).SequenceEqual(stringPlan.Select(i=>i.Start)))
            throw new InvalidOperationException("Không đối chiếu được thứ tự các vùng chữ đã chọn.");
        foreach(var item in stringPlan.Where(i=>!i.IsStyleOnly))
            story=story.Remove(item.Start,item.Length).Insert(item.Start,item.Replacement);
        return story;
    }
    private static IReadOnlyList<Issue> TextPlan(IEnumerable<Issue> selection)
    {
        var plan=BatchPlan.Create(selection);
        if(plan.Any(i=>i.PageFix is not null || i.LineFix is not null || i.Kind==FixKind.ShapeLine))
            throw new InvalidOperationException("Giao dịch chữ không nhận thao tác Shape Line.");
        return plan;
    }
    public static void Apply(IReadOnlyList<Issue> selection,Func<Issue,string> read,
        Func<Issue,string> applyStep,Action commitAndVerify,Action restoreOriginal)
    {
        var plan=TextPlan(selection);
        if(plan.Count==0)return;
        // Stale/manual/overlapping selections fail before any write or rollback.
        BatchPlan.Validate(plan,read);
        try
        {
            foreach(var item in plan)
            {
                if(read(item)!=item.Original)
                    throw new InvalidOperationException("Nội dung thay đổi trong lượt sửa; phải khôi phục cả lượt.");
                if(applyStep(item)!=item.Replacement)
                    throw new InvalidOperationException("Chữ sau sửa không khớp gợi ý đã chọn; phải khôi phục cả lượt.");
            }
            commitAndVerify(); // Save and final verification are part of the same transaction.
        }
        catch(Exception failure)
        {
            try{restoreOriginal();}
            catch(Exception restoreFailure)
            {
                throw new AggregateException("Chưa khôi phục được lượt sửa chữ; giữ checkpoint để phục hồi.",failure,restoreFailure);
            }
            throw;
        }
    }
}
