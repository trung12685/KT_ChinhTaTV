namespace ChinhTa.Core;

public sealed record BodySpacingProof(Issue Source,string Text,string Prefix,string? NextText,ObservedParagraphStyle? NextStyle);
public static class BodySpacingVerification
{
    public static IReadOnlyList<BodySpacingProof> Build(IReadOnlyList<Issue> plan,string story,Func<Issue,string> readPrefix)
    {
        BatchPlan.Create(plan);
        return plan.Where(i=>i.SpacingFix is not null).Select(item=>new BodySpacingProof(item,
            StagedRepairBatch.ExpectedReference(item,plan,story,readPrefix),
            AtomicTextBatch.ExpectedStory(plan.Where(i=>(long)i.Start+i.Length<=item.Start).ToArray(),readPrefix(item),readPrefix),
            item.SpacingFix!.Next is { } next?StagedRepairBatch.ExpectedReference(new(next.Start,next.Text.Length,next.Text,next.Text,"",false),plan,story,readPrefix):null,
            item.SpacingFix.Next is { } neighbor?plan.Where(p=>p.Start==neighbor.Start && p.Length==neighbor.Text.Length).Aggregate(neighbor.Style,(style,p)=>BodyStylePatchPlan.Apply(p,style)):null)).ToArray();
    }
    public static void Verify(IReadOnlyList<BodySpacingProof> proofs,IReadOnlyList<Paragraph> current,Func<Paragraph,string> readPrefix)
    {
        var texts=proofs.Select(p=>p.Text).ToHashSet();
        var bindings=current.Select((p,i)=>(p,i,Text:p.Text.TrimEnd('\r','\n','\a'))).Where(x=>texts.Contains(x.Text))
            .ToLookup(x=>(x.Text,Prefix:readPrefix(x.p)));
        foreach(var proof in proofs)
        {
            var fix=proof.Source.SpacingFix!;
            var rows=bindings[(proof.Text,proof.Prefix)].ToArray();
            if(rows.Length!=1 || rows[0].p.Flow is not {Known:true} || rows[0].p.Outline?.SpacingContext!=fix.Context ||
                rows[0].p.ParagraphStyle?.Spacing is not {Known:true} spacing ||
                (fix.IsGap?Math.Abs(spacing.After-6)>=.01f:spacing.Rule!=0))
                throw new InvalidOperationException("Chưa xác minh lại đúng vùng/khoảng đoạn/giãn dòng sau sửa; phải phục hồi.");
            if(fix.Next is not { } source)continue;
            int index=rows[0].i+1;
            if(index>=current.Count || current[index].Text.TrimEnd('\r','\n','\a')!=proof.NextText ||
                current[index].Flow is not {Known:true} || current[index].Outline?.SpacingContext!=source.Context ||
                current[index].ParagraphStyle is not {Spacing:{Known:true}} style || proof.NextStyle is null || !proof.NextStyle.Matches(style) ||
                style.Spacing is not { } next ||
                next.BeforeAuto!=source.Style.Spacing!.BeforeAuto || next.AfterAuto!=source.Style.Spacing.AfterAuto ||
                Math.Abs(next.Before-source.Style.Spacing.Before)>=.01f)
                throw new InvalidOperationException("Đoạn liền sau hoặc khoảng trước chưa chọn đã đổi; phải phục hồi.");
            // Approved font/spacing edits may repaginate the pair. The selected
            // 6pt setting and unchanged neighbor/context must still be proven;
            // a new page boundary is never used as a substitute for that setting.
        }
    }
}
