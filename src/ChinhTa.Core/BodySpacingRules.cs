namespace ChinhTa.Core;

public sealed record BodySpacingContext(bool Contextual=false,bool Grid=false,bool PageBreakBefore=false,bool KeepNext=false);
public sealed record BodyFlow(int StartPage,int EndPage)
{
    public bool Known=>StartPage>0 && EndPage>=StartPage;
}
public sealed record BodyGapNeighbor(int Start,string Text,ObservedParagraphStyle Style,BodySpacingContext Context,BodyFlow Flow);
public sealed record BodySpacingFix(ObservedParagraphStyle Before,BodySpacingContext Context,BodyFlow Flow,BodyGapNeighbor? Next=null,
    float? After=null,int? Rule=null,float? Line=null)
{
    public bool IsGap=>After is not null;
    public bool Valid=>BodySpacingRules.Ordinary(Before) && Flow.Known && !Context.Grid && !Context.PageBreakBefore && !Context.KeepNext &&
        (IsGap?After==6 && Rule is null && Line is null && !Context.Contextual && Next is { } next &&
            next.Start>=0 && next.Text.Length>0 && !next.Text.Any(char.IsControl) && next.Flow.Known && Flow.EndPage==next.Flow.StartPage &&
            !next.Context.Grid && !next.Context.Contextual && !next.Context.PageBreakBefore && !next.Context.KeepNext &&
            BodySpacingRules.Ordinary(next.Style) && Before.Spacing!.BeforeAuto==0 && Before.Spacing.AfterAuto==0 &&
            next.Style.Spacing!.BeforeAuto==0 && next.Style.Spacing.AfterAuto==0 && Before.Spacing.After+next.Style.Spacing.Before<5.99f:
            After is null && Next is null && Rule==0 && Line is null && BodySpacingRules.LineTooFar(Before.Spacing!));
    public ObservedParagraphStyle Apply(ObservedParagraphStyle current)=>current with {Spacing=current.Spacing! with
        {After=After??current.Spacing.After,Rule=Rule??current.Spacing.Rule,Line=Line??current.Spacing.Line}};
    public string Suggestion=>IsGap?"Đặt khoảng sau đoạn 6 pt; giữ khoảng trước đoạn sau và giãn dòng chưa chọn.":
        "Đặt giãn dòng đơn; giữ khoảng trước/sau đoạn và căn/thụt chưa chọn.";
}

public static class BodySpacingRules
{
    public const string GapCitation="NĐ30, PL I, Phần I.II.6.e (PDF 25): khoảng cách giữa các đoạn văn tối thiểu 6 pt.";
    public const string LineCitation="NĐ30, PL I, Phần I.II.6.e (PDF 25): khoảng cách giữa các dòng tối thiểu dòng đơn, tối đa 1,5 lines.";
    public static bool Ordinary(ObservedParagraphStyle? style)=>style is {Known:true,Spacing:{Known:true}} && style.Alignment is 0 or 3 &&
        Math.Abs(style.Left)<.05f && Math.Abs(style.Right)<.05f && style.First is >=0 and <=144;
    public static bool LineTooFar(ObservedParagraphSpacing spacing)=>spacing.Known &&
        (spacing.Rule==2 || spacing.Rule==5 && (spacing.Line<11.99f || spacing.Line>18.01f));
    private static bool Eligible(Paragraph row)=>Ordinary(row.ParagraphStyle) && row.Outline is {SpacingContext:not null} &&
        row.Outline.Alignment==row.ParagraphStyle!.Alignment && row.Flow is {Known:true} && row.FormatObserved &&
        row.Text.TrimEnd('\r','\n','\a')==row.Text.TrimEnd('\r','\n','\a').Trim(' ') &&
        row.FormatEvidence is {Fonts.Count:>0} e && e.Text==row.Text.TrimEnd('\r','\n','\a') && e.Fonts.All(f=>f.Known() && f.Bold==0 && f.Italic==0);
    public static IReadOnlyList<Issue> Check(IReadOnlyList<Paragraph> paragraphs,IReadOnlyList<LayoutParagraph> layout,DocumentProfile profile)
    {
        var candidates=BodyFormatRules.RepairTargets(paragraphs,layout,profile).Where(Eligible).ToDictionary(p=>p.Start);
        var result=new List<Issue>();
        foreach(var row in candidates.Values)
        {
            var fix=new BodySpacingFix(row.ParagraphStyle!,row.Outline!.SpacingContext!,row.Flow!,Rule:0);
            if(fix.Valid)result.Add(Issue(row,fix,FixKind.BodyLineSpacing,"Giãn dòng nội dung chưa đúng",LineCitation));
        }
        // A blank/heading/table/unproven row remains a boundary. Never skip it to
        // invent a pair. Sum is an upper bound even when Word collapses spacing;
        // if sum could reach 6pt, leave the ambiguous case for later measurement.
        for(int i=0;i+1<paragraphs.Count;i++)
        {
            var row=paragraphs[i];var next=paragraphs[i+1];
            if(!candidates.ContainsKey(row.Start) || !candidates.ContainsKey(next.Start))continue;
            var neighbor=new BodyGapNeighbor(next.Start,next.Text.TrimEnd('\r','\n','\a'),next.ParagraphStyle!,next.Outline!.SpacingContext!,next.Flow!);
            var fix=new BodySpacingFix(row.ParagraphStyle!,row.Outline!.SpacingContext!,row.Flow!,neighbor,After:6);
            if(fix.Valid)result.Add(Issue(row,fix,FixKind.BodyGap,"Khoảng cách giữa hai đoạn thiếu",GapCitation));
        }
        return result;
    }
    private static Issue Issue(Paragraph row,BodySpacingFix fix,FixKind kind,string title,string citation)
    {
        string text=row.Text.TrimEnd('\r','\n','\a');
        string observed=fix.IsGap?$"Sau đoạn {fix.Before.Spacing!.After:0.##} pt; trước đoạn kế {fix.Next!.Style.Spacing!.Before:0.##} pt. ":
            fix.Before.Spacing!.Rule==2?"Đang giãn dòng đôi. ":$"Đang giãn Multiple {fix.Before.Spacing.Line/12:0.##} dòng. ";
        return new(row.Start,text.Length,text,text,observed+fix.Suggestion,false,kind,Citation:citation,Title:title,SpacingFix:fix);
    }
}

// Merge independent full-paragraph patches against one immutable source. Each
// writer receives the expected state after earlier approved patches, never an
// arbitrary newly-read state that could hide an unselected drift.
public static class BodyStylePatchPlan
{
    private static int Owner(Issue i)=>i.Kind switch
    {FixKind.BodyFormat=>1,FixKind.BodyParagraph=>2,FixKind.BodyGap=>3,FixKind.BodyLineSpacing=>4,_=>0};
    public static bool Disjoint(Issue a,Issue b)=>Owner(a)>0 && Owner(b)>0 && Owner(a)!=Owner(b) &&
        a.Start==b.Start && a.Length==b.Length && a.Original==b.Original;
    public static ObservedParagraphStyle Apply(Issue item,ObservedParagraphStyle current)=>item.ParagraphFix is { } p?
        current with {Alignment=p.Alignment??current.Alignment,First=p.First??current.First}:item.SpacingFix is { } s?s.Apply(current):current;
    public static ObservedParagraphStyle Before(Issue item,IReadOnlyList<Issue> selection)
    {
        var plan=BatchPlan.Create(selection);var baseline=item.ParagraphFix?.Before??item.SpacingFix?.Before??throw new InvalidOperationException("Thiếu snapshot đoạn.");
        var current=baseline;bool found=false;
        foreach(var patch in plan.Where(p=>p.Start==item.Start && p.Length==item.Length && p.Original==item.Original))
        {
            var source=patch.ParagraphFix?.Before??patch.SpacingFix?.Before;
            if(source is not null && (!baseline.Matches(source) || !source.Matches(baseline)))
                throw new InvalidOperationException("Các đề xuất đoạn không cùng snapshot; hãy quét lại.");
            if(patch==item){found=true;break;}
            current=Apply(patch,current);
        }
        if(!found)throw new InvalidOperationException("Mục sửa đoạn không thuộc lượt đã duyệt.");
        return current;
    }
}
