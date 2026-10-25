namespace ChinhTa.Core;

public sealed record ObservedParagraphSpacing(float Before,float After,int Rule,float Line,int BeforeAuto,int AfterAuto)
{
    public bool Known=>new[]{Before,After,Line}.All(v=>float.IsFinite(v) && v>=0 && v<9999999) &&
        Rule is >=0 and <=5 && BeforeAuto is 0 or -1 && AfterAuto is 0 or -1;
    // Single/1.5/double are defined by their rule; their reported point height
    // may follow a selected font change. AtLeast/Exactly/Multiple retain the value.
    public bool Matches(ObservedParagraphSpacing actual)=>Known && actual.Known && Rule==actual.Rule && BeforeAuto==actual.BeforeAuto &&
        AfterAuto==actual.AfterAuto && Math.Abs(Before-actual.Before)<.01f && Math.Abs(After-actual.After)<.01f && (Rule<=2 || Math.Abs(Line-actual.Line)<.01f);
}
public sealed record BodyParagraphFix(ObservedParagraphStyle Before,int? Alignment=null,float? First=null)
{
    public bool Valid=>Before.Known && Before.Spacing is {Known:true} && Before.Alignment is 0 or 3 &&
        Math.Abs(Before.Left)<.05f && Math.Abs(Before.Right)<.05f && Before.First is >=0 and <=144 &&
        (Alignment is null || Alignment==3 && Before.Alignment!=3) &&
        (First is null || First==36 && !BodyParagraphRules.ValidFirst(Before.First)) && (Alignment is not null || First is not null);
    public ObservedParagraphStyle Expected=>Before with {Alignment=Alignment??Before.Alignment,First=First??Before.First};
    public string Suggestion=>(Alignment is null?"":"Căn đều hai lề. ")+(First is null?"":"Thụt dòng đầu 1,27 cm. ")+
        "Giữ chữ, đậm/nghiêng/gạch chân, phông/cỡ, khoảng đoạn và giãn dòng chưa chọn.";
}
public static class BodyParagraphRules
{
    public const string Citation="NĐ30, PL I, Phần I.II.6.e (PDF 25): nội dung căn đều hai lề, đầu dòng lùi 1 cm hoặc 1,27 cm. Bước này chưa sửa giãn dòng/khoảng đoạn.";
    // Fonts are observed only on visible glyphs. Require some plain prose;
    // an entirely bold/italic paragraph remains outside layout repair scope.
    public static bool KnownProseFonts(IReadOnlyList<ObservedFont> fonts)=>fonts.Count>0 &&
        fonts.All(f=>f.Known()) && fonts.Any(f=>f.Bold==0 && f.Italic==0);
    public static bool ValidFirst(float value)=>float.IsFinite(value) && (Math.Abs(value-72/2.54f)<.05f || Math.Abs(value-36)<.05f);
    public static IReadOnlyList<Issue> Check(IReadOnlyList<Paragraph> paragraphs,IReadOnlyList<LayoutParagraph> layout,DocumentProfile profile)
        =>CheckTargets(BodyFormatRules.RepairTargets(paragraphs,layout,profile));
    internal static IReadOnlyList<Issue> CheckTargets(IReadOnlyList<Paragraph> targets)
    {
        var result=new List<Issue>();
        foreach(var row in targets)
        {
            string text=row.Text.TrimEnd('\r','\n','\a');var style=row.ParagraphStyle;
            if(style is null || !style.Known || style.Spacing is not {Known:true} || style.Alignment is not (0 or 3) ||
                row.Outline?.Alignment!=style.Alignment || text!=text.Trim(' ') || Math.Abs(style.Left)>=.05f || Math.Abs(style.Right)>=.05f ||
                style.First<0 || style.First>144 || !row.FormatObserved || row.FormatEvidence is not { } evidence || evidence.Text!=text ||
                !KnownProseFonts(evidence.Fonts))continue;
            var fix=new BodyParagraphFix(style,style.Alignment==3?null:3,ValidFirst(style.First)?null:36);
            if(!fix.Valid)continue;
            result.Add(new(row.Start,text.Length,text,text,fix.Suggestion,false,FixKind.BodyParagraph,Citation:Citation,
                Title:fix.Alignment is null?"Thụt đầu dòng nội dung chưa đúng":fix.First is null?"Nội dung chưa căn đều":"Căn/thụt đầu dòng nội dung chưa đúng",ParagraphFix:fix));
        }
        return result;
    }
}
