namespace ChinhTa.Core;

public sealed record ObservedParagraphStyle(int Alignment,float Left,float Right,float First,ObservedParagraphSpacing? Spacing=null)
{
    public bool Known=>Alignment is >=0 and <=9 && new[]{Left,Right,First}.All(v=>float.IsFinite(v) && Math.Abs(v)<9999999) && (Spacing is null || Spacing.Known);
    public bool Matches(ObservedParagraphStyle actual)=>Known && actual.Known && Alignment==actual.Alignment &&
        Math.Abs(Left-actual.Left)<.01f && Math.Abs(Right-actual.Right)<.01f && Math.Abs(First-actual.First)<.01f && (Spacing is null || actual.Spacing is { } spacing && Spacing.Matches(spacing));
}

public sealed record ObservedCharacterStyle(ObservedFont Font,int Underline)
{
    public bool Known=>Font.Known() && Underline is not (9999999 or -9999999);
    public bool Matches(ObservedCharacterStyle actual)=>Known && actual.Known &&
        Font.Name.Equals(actual.Font.Name,StringComparison.OrdinalIgnoreCase) && Math.Abs(Font.Size-actual.Font.Size)<.01f &&
        Font.Bold==actual.Font.Bold && Font.Italic==actual.Font.Italic && Underline==actual.Underline &&
        (Font.Color==actual.Font.Color || Font.Color is 0 or -16777216 && actual.Font.Color is 0 or -16777216);
}

// Bound to an exact, XML-proven paragraph. Whitespace and newly inserted glyphs
// have no retained source style; every other observed source glyph must be known.
public sealed class CharacterStyleEvidence
{
    public string Text {get;}
    public IReadOnlyList<ObservedCharacterStyle?> Styles {get;}
    public ObservedParagraphStyle? Paragraph {get;}
    public CharacterStyleEvidence(string text,IEnumerable<ObservedCharacterStyle?> styles,ObservedParagraphStyle? paragraph=null)
    {
        var values=styles.ToArray();
        if(values.Length!=text.Length || values.Any(s=>s is not null && !s.Known) || paragraph is not null && !paragraph.Known)
            throw new InvalidOperationException("Chưa đọc chắc định dạng ký tự cần giữ khi sửa chữ/phông.");
        Text=text;Styles=Array.AsReadOnly(values);Paragraph=paragraph;
    }
    public bool Complete=>Styles.Select((s,i)=>char.IsWhiteSpace(Text[i]) || s is not null).All(x=>x);
    public CharacterStyleEvidence WithFormat(CharacterFormat format)
    {
        if(!format.Valid || !Complete)throw new InvalidOperationException("Chưa có định dạng đã duyệt để đối chiếu sau sửa.");
        return new(Text,Styles.Select(s=>s is null?null:s with {Font=s.Font with
        {
            Name="Times New Roman",Color=0,Italic=format.PreserveEmphasis?s.Font.Italic:format.Italic?-1:0,Bold=format.PreserveEmphasis?s.Font.Bold:format.Bold is bool bold?bold?-1:0:s.Font.Bold,
            Size=!format.PreserveSize?format.Size:format.RepairInvalidSizes &&
                (s.Font.Size<format.SizeRange.Minimum || s.Font.Size>format.SizeRange.Maximum)?format.Size:s.Font.Size
        }}),Paragraph);
    }
    public CharacterStyleEvidence WithParagraph(BodyParagraphFix fix,ObservedParagraphStyle? baseline=null)
    {
        if(!fix.Valid || Paragraph is null || !fix.Before.Matches(baseline??Paragraph))
            throw new InvalidOperationException("Căn/thụt hoặc khoảng đoạn đã đổi; hãy quét lại trước khi sửa.");
        return new(Text,Styles,Paragraph with {Alignment=fix.Alignment??Paragraph.Alignment,First=fix.First??Paragraph.First});
    }
    public CharacterStyleEvidence WithSpacing(BodySpacingFix fix,ObservedParagraphStyle? baseline=null)
    {
        if(!fix.Valid || Paragraph is null || !fix.Before.Matches(baseline??Paragraph))
            throw new InvalidOperationException("Khoảng đoạn/giãn dòng đã đổi; hãy quét lại.");
        return new(Text,Styles,fix.Apply(Paragraph));
    }
    public CharacterStyleEvidence Project(IReadOnlyList<Issue> edits,Func<Issue,string> readPrefix)
    {
        var projected=RetainedGlyphProjection.Project(Text,Styles,edits,readPrefix,(ObservedCharacterStyle?)null);
        return new(projected.Text,projected.Values,Paragraph);
    }
    public bool Matches(CharacterStyleEvidence actual)=>Text==actual.Text && actual.Complete && Styles.Count==actual.Styles.Count &&
        (Paragraph is null || actual.Paragraph is { } observed && Paragraph.Matches(observed)) &&
        Styles.Select((s,i)=>s is null || actual.Styles[i] is { } found && s.Matches(found)).All(x=>x);
}
