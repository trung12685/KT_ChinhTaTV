using System.Text.RegularExpressions;

namespace ChinhTa.Core;

public sealed record InlineWordBinding(WordParagraphSnapshot Paragraph,int Offset,string Word);
public static class InlineWordLocator
{
    public static readonly Regex Words=new(@"[\p{L}\p{N}][\p{L}\p{M}\p{N}]*",RegexOptions.CultureInvariant|RegexOptions.NonBacktracking);
    public static InlineWordBinding? Locate(PreviewAnchor hit,IReadOnlyList<WordParagraphSnapshot> paragraphs)
    {
        if(hit.Page<1 || hit.Offset<0 || hit.Length is <1 or >128 || hit.Offset>hit.PageText.Length-hit.Length)return null;
        string word=hit.PageText.Substring(hit.Offset,hit.Length);
        if(!Words.Matches(word).Cast<Match>().Any(m=>m.Index==0 && m.Length==word.Length))return null;
        var matches=new List<InlineWordBinding>();
        foreach(var row in paragraphs.Where(p=>p.Page==hit.Page))
        {
            string text=row.Text.TrimEnd('\r','\a','\n');var span=PreviewLocator.Locate(hit,text);
            if(span is null || span.Length!=word.Length || text.Substring(span.Start,span.Length)!=word)continue;
            // Both ends must be a complete native word, not the middle of a
            // longer token that happened to align after whitespace normalization.
            if(!Words.Matches(text).Cast<Match>().Any(m=>m.Index==span.Start && m.Length==span.Length))continue;
            matches.Add(new(row,span.Start,word));
        }
        if(matches.Count!=1)return null;
        var candidate=matches[0];
        return (long)candidate.Paragraph.End-candidate.Paragraph.Start==candidate.Paragraph.Text.Length?candidate:null;
    }
}
public sealed class InlineWordDraft
{
    public ManualTextTarget Target {get;}
    public int Offset {get;}
    public string Original {get;}
    public InlineWordDraft(ManualTextTarget target,int offset,string original)
    {
        if(offset<0 || original.Length is <1 or >128 || offset>target.Original.Length-original.Length || target.Original.Substring(offset,original.Length)!=original)
            throw new InvalidOperationException("Chữ đã bấm chưa khớp đoạn hiện tại; hãy Quét lại.");
        Target=target;Offset=offset;Original=original;
    }
    public ManualTextChange Change(string replacement)
    {
        if(replacement.Length>256 || replacement.Any(c=>char.IsControl(c) || char.IsSurrogate(c) || char.GetUnicodeCategory(c)==System.Globalization.UnicodeCategory.Format))
            throw new InvalidOperationException("Ô sửa tại chữ nhận tối đa 256 ký tự; không chèn ngắt đoạn, tab hoặc ký tự điều khiển.");
        return Target.EditRange(Offset,Original.Length,replacement);
    }
}
