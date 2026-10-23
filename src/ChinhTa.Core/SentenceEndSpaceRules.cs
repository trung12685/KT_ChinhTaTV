using System.Globalization;
using System.Text.RegularExpressions;

namespace ChinhTa.Core;

// Delete ASCII spaces only, immediately before a single final .?! in a narrow
// prose context. Do not rewrite words, punctuation, trailing spaces or terminators.
public static class SentenceEndSpaceRules
{
    private static readonly Regex DateWords=new(@"\bngày\b[^\r\n\a]*\btháng\b[^\r\n\a]*\bnăm\b",
        RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.NonBacktracking);
    private static readonly Regex Word=new(@"\p{L}[\p{L}\p{M}]*",RegexOptions.CultureInvariant|RegexOptions.NonBacktracking);
    public static IReadOnlyList<Issue> Check(Paragraph paragraph)
    {
        string text=paragraph.Text.TrimEnd('\r','\n','\a');
        if(AdministrativeHeaderRules.IsCandidate(text) || DateWords.IsMatch(text) ||
            text.IndexOfAny(['\t','\r','\n','\a','\v','\f','@','/','\\','_','=','`','<','>','[',']','{','}','+','*','^','|'])>=0)return Array.Empty<Issue>();
        int mark=text.Length-1;
        while(mark>=0 && text[mark]==' ')mark--;
        if(mark<0 || text[mark] is not ('.' or '?' or '!'))return Array.Empty<Issue>();
        int start=mark;
        while(start>0 && text[start-1]==' ')start--;
        if(start==mark)return Array.Empty<Issue>();
        int wordStart=start;
        while(wordStart>0 && (char.IsLetter(text[wordStart-1]) || Mark(text[wordStart-1])))wordStart--;
        string last=text[wordStart..start];
        if(last.Length==0 || !char.IsLetter(last[0]) || last.Count(char.IsLetter)<2 ||
            last.Any(c=>char.IsLetter(c) && !char.IsLower(c)) || last.All(c=>"ivxlcdm".Contains(c)) ||
            wordStart>0 && text[wordStart-1]!=' ' || !Word.IsMatch(text[..wordStart]))return Array.Empty<Issue>();
        int previous=wordStart-1;
        while(previous>=0 && text[previous]==' ')previous--;
        // Avoid spaced dotted tokens and a single-word segment after punctuation.
        if(previous>=0 && text[previous] is '.' or '?' or '!')return Array.Empty<Issue>();
        if(!paragraph.TextEditable)
            return [new(paragraph.Start,text.Length,text,text,
                "Có khoảng trắng trước dấu kết câu trong vùng chưa cho phép sửa chữ. Kiểm tra thủ công.",
                false,FixKind.SentenceEndSpace,CanFix:false,Title:"Dấu kết câu trong vùng có cấu trúc")];
        return [new(paragraph.Start+start,mark-start,text[start..mark],"",
            "Đề xuất xóa dấu cách trước dấu kết câu cuối đoạn; giữ chữ, dấu câu và khoảng trắng sau. Cần duyệt cả câu.",
            false,FixKind.SentenceEndSpace,Title:"Khoảng trắng trước dấu kết câu cuối đoạn")];
    }
    private static bool Mark(char c)=>char.GetUnicodeCategory(c) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark;
}
