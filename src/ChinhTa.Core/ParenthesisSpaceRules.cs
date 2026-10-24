using System.Globalization;
using System.Text.RegularExpressions;

namespace ChinhTa.Core;

// Independent inner-edge deletions and a conservative missing gap after ')'.
// Keep delimiters and words/runs; every edit has a positive source span.
public static class ParenthesisSpaceRules
{
    // A date annotation can be outside the strict header parser. Keep such rows
    // out of this generic edit rule without broadening administrative recognition.
    private static readonly Regex DateWords=new(@"\bngày\b[^\r\n\a]*\btháng\b[^\r\n\a]*\bnăm\b",
        RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.NonBacktracking);
    private static readonly HashSet<string> Functions=new(StringComparer.OrdinalIgnoreCase)
        {"fn","func","function","sin","cos","tan","cot","log","ln","exp","sqrt","abs","max","min"};
    internal static bool IsFunctionName(string value)=>Functions.Contains(value);
    public static IReadOnlyList<Issue> Check(Paragraph paragraph)
    {
        string text=paragraph.Text;
        if(AdministrativeHeaderRules.IsCandidate(text) || DateWords.IsMatch(text) || text.IndexOfAny(['\t','@','/','\\','_','=','`','<','>','[',']','{','}'])>=0)
            return Array.Empty<Issue>();
        var pairs=new List<(int Open,int Close)>();
        int open=-1;
        for(int i=0;i<text.Length;i++)
        {
            if(text[i]=='(')
            {
                if(open>=0)return Array.Empty<Issue>();
                open=i;
            }
            else if(text[i]==')')
            {
                if(open<0)return Array.Empty<Issue>();
                pairs.Add((open,i));open=-1;
            }
        }
        if(open>=0)return Array.Empty<Issue>();
        var result=new List<Issue>();
        foreach(var pair in pairs)
        {
            if(pair.Open>0 && text[pair.Open-1]!=' ')continue;
            int first=pair.Open+1,last=pair.Close;
            while(first<last && text[first]==' ')first++;
            while(last>first && text[last-1]==' ')last--;
            bool missingAfter=!OutsideBoundary(text,pair.Close+1) && ClearAfter(text,pair.Open,pair.Close,first,last);
            if(!OutsideBoundary(text,pair.Close+1) && !missingAfter || !Prose(text,first,last) ||
                !missingAfter && first==pair.Open+1 && last==pair.Close)continue;
            if(!paragraph.TextEditable)
            {
                string whole=text.TrimEnd('\r','\n','\a');
                return [new(paragraph.Start,whole.Length,whole,whole,
                    "Có khoảng trắng quanh ngoặc cần duyệt ở vùng chưa cho phép sửa chữ. Kiểm tra thủ công.",
                    false,FixKind.ParenthesisSpace,CanFix:false,Title:"Khoảng trắng quanh ngoặc ở vùng có cấu trúc")];
            }
            if(first>pair.Open+1)Add(pair.Open+1,first,"Khoảng trắng sau dấu mở ngoặc");
            if(last<pair.Close)Add(last,pair.Close,"Khoảng trắng trước dấu đóng ngoặc");
            if(missingAfter)result.Add(new(paragraph.Start+pair.Close,1,")",") ",
                "Đề xuất thêm một dấu cách ASCII sau dấu đóng ngoặc; giữ chữ, ngoặc, khoảng trong ngoặc và phần chưa chọn. Cần duyệt cả câu.",
                false,FixKind.ParenthesisSpace,Title:"Thiếu khoảng trắng sau dấu đóng ngoặc"));
        }
        return result;
        void Add(int start,int end,string title)=>result.Add(new(paragraph.Start+start,end-start,text[start..end],"",
            "Đề xuất xóa dấu cách sát bên trong ngoặc tròn; giữ dấu ngoặc, chữ và khoảng cách bên ngoài. Cần duyệt cả câu.",
            false,FixKind.ParenthesisSpace,Title:title));
    }
    private static bool OutsideBoundary(string text,int position)=>position==text.Length || text[position] is ' ' or '\r' or '\n' or '\a' or ',' or ';' or '.' or ':' or '!' or '?';
    private static bool Mark(char c)=>char.GetUnicodeCategory(c) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark;
    private static bool Letters(string value)=>value.Count(char.IsLetter)>=2 && value.All(c=>char.IsLetter(c)||Mark(c));
    private static bool ClearAfter(string text,int open,int close,int first,int last)
    {
        if(!SentenceSpaceContext.Supported(text.TrimEnd('\r','\n','\a')))return false;
        int leftEnd=open;
        while(leftEnd>0 && text[leftEnd-1]==' ')leftEnd--;
        if(!SentenceSpaceContext.ClearEnding(text,leftEnd))return false;
        int wordStart=leftEnd;
        while(wordStart>0 && (char.IsLetter(text[wordStart-1]) || Mark(text[wordStart-1])))wordStart--;
        if(IsFunctionName(text[wordStart..leftEnd]))return false;
        var inside=text[first..last].Split(' ',StringSplitOptions.RemoveEmptyEntries);
        if(inside.Length<2 || inside.Any(word=>!Letters(word)))return false;
        int start=close+1,end=start;
        while(end<text.Length && (char.IsLetter(text[end]) || Mark(text[end])))end++;
        if(!Letters(text[start..end]) || text[start..end].Any(c=>char.IsLetter(c) && !char.IsLower(c)) ||
            end==text.Length || text[end]!=' ')return false;
        while(end<text.Length && text[end]==' ')end++;
        start=end;
        while(end<text.Length && (char.IsLetter(text[end]) || Mark(text[end])))end++;
        if(!Letters(text[start..end]) || !text[start..end].Any(char.IsLower))return false;
        return end==text.Length || text[end] is ' ' or '\r' or '\n' or '\a' or ',' or ';' ||
            text[end] is '.' or '?' or '!' && (end+1==text.Length || text[end+1] is ' ' or '\r' or '\n' or '\a');
    }
    private static bool Prose(string text,int start,int end)
    {
        if(start==end || !char.IsLetter(text[start]) || !(char.IsLetter(text[end-1]) || Mark(text[end-1])))return false;
        bool longWord=false,lower=false;int letters=0;
        for(int i=start;i<end;i++)
        {
            char c=text[i];
            if(char.IsLetter(c)){letters++;longWord|=letters>=2;lower|=char.IsLower(c);}
            else if(Mark(c))continue;
            else if(c is ' ' or ',' or ';' or '-' or '–' or '—')letters=0;
            else return false;
        }
        return longWord && lower;
    }
}
