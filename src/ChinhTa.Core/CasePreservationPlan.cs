namespace ChinhTa.Core;

public static class CasePreservationPlan
{
    private static bool IsBody(Issue i)=>i.Kind is FixKind.BodyFormat or FixKind.BodyParagraph or FixKind.BodyGap or FixKind.BodyLineSpacing;
    private static bool Compatible(Issue probe,Issue patch)=>IsBody(probe)?
        patch.Kind==FixKind.BodyFormat && patch.CharacterFormat?.Role==ComponentRole.Body || patch.Kind==FixKind.BodyParagraph && patch.ParagraphFix is not null ||
        patch.Kind is FixKind.BodyGap or FixKind.BodyLineSpacing && patch.SpacingFix is not null:
        patch.Kind==FixKind.DocumentHeadingFormat && patch.CharacterFormat is not null;
    private static bool Overlaps(Issue a,Issue b)=>a.Start<(long)b.Start+b.Length && b.Start<(long)a.Start+a.Length;
    private static bool Inside(Issue item,Issue row)=>item.Start>=row.Start && (long)item.Start+item.Length<=(long)row.Start+row.Length;
    // Tracking probes are full proven paragraphs, including layout spaces. They
    // never enter the edit selection or create an extra write/approval item.
    public static IReadOnlyList<Issue> Regions(IReadOnlyList<Issue> selection,IReadOnlyList<WordParagraphSnapshot> proven)
    {
        var cases=selection.Where(i=>i.Kind is FixKind.DocumentTypeText or FixKind.BodyFormat or FixKind.BodyParagraph or FixKind.BodyGap or FixKind.BodyLineSpacing).ToArray();
        if(cases.Length==0)return [];
        BatchPlan.Create(selection);
        var result=new Dictionary<int,Issue>();
        foreach(var item in cases)
        {
            var rows=proven.Where(p=>p.Start<=item.Start && (long)item.Start+item.Length<=p.End).ToArray();
            if(rows.Length!=1)throw new InvalidOperationException("Chưa có một đoạn đã đối chiếu XML duy nhất để giữ định dạng vùng chữ.");
            var row=rows[0];string text=row.Text.TrimEnd('\r','\a','\n');
            var probe=new Issue(row.Start,text.Length,text,text,"",false,item.Kind);
            if(!Inside(item,probe) || text.IndexOfAny(['\r','\n','\a','\t','\v','\f'])>=0)
                throw new InvalidOperationException("Vùng giữ định dạng chưa khớp đoạn chữ thuần.");
            foreach(var patch in selection.Where(i=>(i.Format is not null || i.IsStyleOnly) && Overlaps(i,probe)))
                if(!Compatible(item,patch) || patch.Format is not null ||
                    patch.Start!=probe.Start || patch.Length!=probe.Length || patch.Original!=probe.Original)
                    throw new InvalidOperationException("Định dạng chọn kèm chưa phủ đúng đoạn đã chọn để đối chiếu bảo toàn.");
            result[row.Start]=probe;
        }
        return result.Values.OrderBy(i=>i.Start).ToArray();
    }
    public static CharacterStyleEvidence Expected(Issue probe,IReadOnlyList<Issue> selection,CharacterStyleEvidence source,Func<Issue,string> readPrefix)
    {
        BatchPlan.Create(selection);
        if(source.Text!=probe.Original || !source.Complete)
            throw new InvalidOperationException("Chưa đọc đủ định dạng ký tự của vùng chữ trước sửa.");
        var baseline=source.Paragraph;
        foreach(var format in selection.Where(i=>i.IsStyleOnly && Overlaps(i,probe)))
        {
            if(!Compatible(probe,format) || format.Start!=probe.Start || format.Length!=probe.Length || format.Original!=probe.Original)
                throw new InvalidOperationException("Định dạng vùng chữ chọn kèm đã thay đổi.");
            if(IsBody(probe) && (
                source.Paragraph is not {Alignment:0 or 3} || format.CharacterFormat?.PreserveEmphasis!=true &&
                !BodyParagraphRules.KnownProseFonts(source.Styles.Where(s=>s is not null).Select(s=>s!.Font).ToArray())))
                throw new InvalidOperationException("Đoạn nội dung chưa có chữ thường đứng hoặc căn đoạn chưa phù hợp phạm vi sửa; hãy quét lại.");
            source=format.ParagraphFix is { } paragraph?source.WithParagraph(paragraph,baseline):
                format.SpacingFix is { } spacing?source.WithSpacing(spacing,baseline):source.WithFormat(format.CharacterFormat!);
        }
        return source.Project(selection.Where(i=>!i.IsStyleOnly && i.Format is null && Inside(i,probe)).ToArray(),readPrefix);
    }
}
