namespace ChinhTa.Core;

// UTF-16 sizes of actual visible Word characters; whitespace/new glyphs are null.
// A repair may change text, so project retained source glyphs before verifying.
public sealed class FontSizeEvidence
{
    public string Text {get;}
    public IReadOnlyList<float?> Sizes {get;}
    public FontSizeEvidence(string text,IEnumerable<float?> sizes)
    {
        var values=sizes.ToArray();
        if(values.Length!=text.Length || values.Any(s=>s is float f && (!float.IsFinite(f) || f<=0 || f>1638)))
            throw new InvalidOperationException("Chưa đọc chắc cỡ chữ của các ký tự cần giữ.");
        Text=text;Sizes=Array.AsReadOnly(values);
    }
    // Every visible source glyph must be known before a selective size repair.
    // The expected result retains each already legal size, rather than accepting
    // any post-save size in the allowed interval.
    public FontSizeEvidence CorrectInvalid(float minimum,float maximum)
    {
        if(!float.IsFinite(minimum) || !float.IsFinite(maximum) || minimum<=0 || maximum<minimum || maximum>1638 ||
            Sizes.Where((_,i)=>!char.IsWhiteSpace(Text[i])).Any(s=>s is null))
            throw new InvalidOperationException("Chưa đọc đủ cỡ chữ để sửa riêng phần sai.");
        return new(Text,Sizes.Select(s=>s is float size && (size<minimum || size>maximum)?minimum:s));
    }
    public FontSizeEvidence Project(IReadOnlyList<Issue> edits,Func<Issue,string> readPrefix)
    {
        var projected=RetainedGlyphProjection.Project(Text,Sizes,edits,readPrefix,(float?)null);
        return new(projected.Text,projected.Values);
    }
    public bool Matches(FontSizeEvidence actual)=>Text==actual.Text && Sizes.Count==actual.Sizes.Count &&
        Sizes.Select((size,i)=>size is not float expected || actual.Sizes[i] is float found && Math.Abs(expected-found)<.01f).All(ok=>ok);
}
