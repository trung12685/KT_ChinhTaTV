using System.Globalization;
using System.Text.RegularExpressions;

namespace ChinhTa.Core;

// Only delete edge ASCII spaces in balanced, nonnested prose parentheses.
// Keep the delimiters and all words/runs; do not rewrite the whole phrase.
public static class ParenthesisSpaceRules
{
    // A date annotation can be outside the strict header parser. Keep such rows
    // out of this generic edit rule without broadening administrative recognition.
    private static readonly Regex DateWords=new(@"\bngày\b[^\r\n\a]*\btháng\b[^\r\n\a]*\bnăm\b",
        RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.NonBacktracking);
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
            if(pair.Open>0 && text[pair.Open-1]!=' ' || !OutsideBoundary(text,pair.Close+1))continue;
            int first=pair.Open+1,last=pair.Close;
            while(first<last && text[first]==' ')first++;
            while(last>first && text[last-1]==' ')last--;
            if(!Prose(text,first,last) || first==pair.Open+1 && last==pair.Close)continue;
            if(!paragraph.TextEditable)
            {
                string whole=text.TrimEnd('\r','\n','\a');
                return [new(paragraph.Start,whole.Length,whole,whole,
                    "Có khoảng trắng trong ngoặc cần duyệt ở vùng chưa cho phép sửa chữ. Kiểm tra thủ công.",
                    false,FixKind.ParenthesisSpace,CanFix:false,Title:"Khoảng trắng trong ngoặc ở vùng có cấu trúc")];
            }
            if(first>pair.Open+1)Add(pair.Open+1,first,"Khoảng trắng sau dấu mở ngoặc");
            if(last<pair.Close)Add(last,pair.Close,"Khoảng trắng trước dấu đóng ngoặc");
        }
        return result;
        void Add(int start,int end,string title)=>result.Add(new(paragraph.Start+start,end-start,text[start..end],"",
            "Đề xuất xóa dấu cách sát bên trong ngoặc tròn; giữ dấu ngoặc, chữ và khoảng cách bên ngoài. Cần duyệt cả câu.",
            false,FixKind.ParenthesisSpace,Title:title));
    }
    private static bool OutsideBoundary(string text,int position)=>position==text.Length || text[position] is ' ' or '\r' or '\n' or '\a' or ',' or ';' or '.' or ':' or '!' or '?';
    private static bool Mark(char c)=>char.GetUnicodeCategory(c) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark;
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
