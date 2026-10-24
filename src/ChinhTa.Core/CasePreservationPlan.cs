namespace ChinhTa.Core;

public static class CasePreservationPlan
{
    private static bool Overlaps(Issue a,Issue b)=>a.Start<(long)b.Start+b.Length && b.Start<(long)a.Start+a.Length;
    private static bool Inside(Issue item,Issue row)=>item.Start>=row.Start && (long)item.Start+item.Length<=(long)row.Start+row.Length;
    // Tracking probes are full proven paragraphs, including layout spaces. They
    // never enter the edit selection or create an extra write/approval item.
    public static IReadOnlyList<Issue> Regions(IReadOnlyList<Issue> selection,IReadOnlyList<WordParagraphSnapshot> proven)
    {
        var cases=selection.Where(i=>i.Kind==FixKind.DocumentTypeText).ToArray();
        if(cases.Length==0)return [];
        BatchPlan.Create(selection);
        var result=new Dictionary<int,Issue>();
        foreach(var item in cases)
        {
            var rows=proven.Where(p=>p.Start<=item.Start && (long)item.Start+item.Length<=p.End).ToArray();
            if(rows.Length!=1)throw new InvalidOperationException("Chưa có một đoạn đã đối chiếu XML duy nhất để giữ định dạng tên loại.");
            var row=rows[0];string text=row.Text.TrimEnd('\r','\a','\n');
            var probe=new Issue(row.Start,text.Length,text,text,"",false,FixKind.DocumentTypeText);
            if(!Inside(item,probe) || text.IndexOfAny(['\r','\n','\a','\t','\v','\f'])>=0)
                throw new InvalidOperationException("Vùng giữ định dạng tên loại chưa khớp đoạn chữ thuần.");
            foreach(var patch in selection.Where(i=>(i.Format is not null || i.CharacterFormat is not null) && Overlaps(i,probe)))
                if(patch.Kind!=FixKind.DocumentHeadingFormat || patch.CharacterFormat is null || patch.Format is not null ||
                    patch.Start!=probe.Start || patch.Length!=probe.Length || patch.Original!=probe.Original)
                    throw new InvalidOperationException("Định dạng chọn kèm chưa phủ đúng đoạn tên loại để đối chiếu bảo toàn.");
            result[row.Start]=probe;
        }
        return result.Values.OrderBy(i=>i.Start).ToArray();
    }
    public static CharacterStyleEvidence Expected(Issue probe,IReadOnlyList<Issue> selection,CharacterStyleEvidence source,Func<Issue,string> readPrefix)
    {
        BatchPlan.Create(selection);
        if(source.Text!=probe.Original || !source.Complete)
            throw new InvalidOperationException("Chưa đọc đủ định dạng ký tự của tên loại trước sửa.");
        foreach(var format in selection.Where(i=>i.CharacterFormat is not null && Overlaps(i,probe)))
        {
            if(format.Kind!=FixKind.DocumentHeadingFormat || format.Start!=probe.Start || format.Length!=probe.Length || format.Original!=probe.Original)
                throw new InvalidOperationException("Định dạng tên loại chọn kèm đã thay đổi.");
            source=source.WithFormat(format.CharacterFormat!);
        }
        return source.Project(selection.Where(i=>i.CharacterFormat is null && i.Format is null && Inside(i,probe)).ToArray(),readPrefix);
    }
}
