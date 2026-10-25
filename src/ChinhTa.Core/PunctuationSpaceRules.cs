using System.Globalization;

namespace ChinhTa.Core;

// ASCII spaces around comma/semicolon between words, or after a verified simple
// prose parenthesis/curly quotation. Delimited paths have a stricter context gate.
// No semantic inference, number punctuation, general token splitting or tabs.
public static class PunctuationSpaceRules
{
    public static IReadOnlyList<Issue> Check(Paragraph paragraph)
    {
        string text=paragraph.Text;
        if(AdministrativeHeaderRules.IsCandidate(text) || text.IndexOfAny(['\t','@','/','\\','_','=','`','<','>','[',']','{','}'])>=0)
            return Array.Empty<Issue>();
        var result=new List<Issue>();
        Dictionary<char,ProseDelimiterIndex?>? delimiters=null;bool? supported=null;
        for(int mark=0;mark<text.Length;mark++)
        {
            if(text[mark] is not (',' or ';'))continue;
            int start=mark,end=mark+1;
            while(start>0 && text[start-1]==' ')start--;
            while(end<text.Length && text[end]==' ')end++;
            int left=start,right=end;
            while(left>0 && WordChar(text[left-1]))left--;
            while(right<text.Length && WordChar(text[right]))right++;
            bool wordLeft=Word(text,left,start) && LeftBoundary(left);
            bool delimitedLeft=!wordLeft && start>0 && (text[start-1] is ')' or '”' or '"') && DelimitedLeft(start-1,text[start-1]);
            if(!Word(text,end,right) || !RightBoundary(right) || !wordLeft && !delimitedLeft ||
                wordLeft && Upper(text,left,start) && Upper(text,end,right) ||
                delimitedLeft && Upper(text,end,right))continue;
            string original=text[start..end],replacement=text[mark]+" ";
            if(original==replacement)continue;
            if(!paragraph.TextEditable)
            {
                string whole=text.TrimEnd('\r','\n','\a');
                return [new(paragraph.Start,whole.Length,whole,whole,
                    "Có khoảng trắng quanh dấu câu cần duyệt trong vùng chưa cho phép sửa chữ. Kiểm tra thủ công.",
                    false,FixKind.PunctuationSpace,CanFix:false,Title:"Dấu câu trong vùng có cấu trúc")];
            }
            result.Add(new(paragraph.Start+start,end-start,original,replacement,
                "Đề xuất bỏ dấu cách trước dấu câu và giữ một dấu cách sau; cần duyệt cả câu.",
                false,FixKind.PunctuationSpace,Title:text[mark]==','?"Khoảng trắng quanh dấu phẩy":"Khoảng trắng quanh dấu chấm phẩy"));
        }
        return result;
        bool Supported()
        {
            if(supported is null)supported=SentenceSpaceContext.Supported(text.TrimEnd('\r','\n','\a'));
            return supported.Value;
        }
        ProseDelimiterIndex? Pairs(char kind)
        {
            char family=kind is '(' or ')'?'p':'q';delimiters??=new();
            if(!delimiters.TryGetValue(family,out var pairs))
            {
                var rows=family=='p'?ProseDelimiterContext.Parentheses(text):ProseDelimiterContext.Quotes(text);
                pairs=rows is null?null:new ProseDelimiterIndex(rows);
                delimiters.Add(family,pairs);
            }
            return pairs;
        }
        bool DelimitedLeft(int close,char closing)
        {
            if(!Supported())return false;
            var pair=Pairs(closing)?.Close.GetValueOrDefault(close);
            return pair is not null && ProseDelimiterContext.Prefix(text,pair.Open) &&
                ProseDelimiterContext.SimpleWords(text,pair.Open+1,pair.Close);
        }
        bool LeftBoundary(int start)
        {
            if(start==0 || text[start-1]==' ')return true;
            char opening=text[start-1];if(opening is not ('(' or '“' or '"'))return false;
            var pair=Pairs(opening)?.Open.GetValueOrDefault(start-1);
            return pair is not null && (pair.Open==0 || text[pair.Open-1]==' ' || ProseDelimiterContext.ClearBefore(text,pair,Supported()));
        }
        bool RightBoundary(int end)
        {
            if(end==text.Length || text[end] is ' ' or '\r' or '\n' or '\a' or ',' or ';')return true;
            if(text[end] is ')' or '”' or '"')return ClosingBoundary(end);
            return text[end] is '.' or '!' or '?' && (end+1==text.Length || text[end+1] is ' ' or '\r' or '\n' or '\a' ||
                text[end+1] is ')' or '”' or '"' && ClosingBoundary(end+1));
        }
        bool ClosingBoundary(int close)
        {
            char c=text[close];
            if(c!=')' && Pairs(c)?.Close.ContainsKey(close)!=true)return false;
            return close+1==text.Length || text[close+1] is ' ' or '\r' or '\n' or '\a' or ',' or ';' or '.' or ':' or '!' or '?';
        }
    }
    private static bool WordChar(char c)=>char.IsLetter(c) || char.GetUnicodeCategory(c) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark;
    private static bool Word(string text,int start,int end)=>start<end && char.IsLetter(text[start]) &&
        text[start..end].Count(char.IsLetter)>=2;
    private static bool Upper(string text,int start,int end)=>text[start..end].All(c=>!char.IsLetter(c) || char.IsUpper(c));
}
