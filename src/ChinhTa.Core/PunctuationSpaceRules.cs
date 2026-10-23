using System.Globalization;

namespace ChinhTa.Core;

// Deliberately narrow: ASCII spaces around comma/semicolon between two words.
// No semantic inference, number punctuation, general token splitting or tabs.
public static class PunctuationSpaceRules
{
    public static IReadOnlyList<Issue> Check(Paragraph paragraph)
    {
        string text=paragraph.Text;
        if(AdministrativeHeaderRules.IsCandidate(text) || text.IndexOfAny(['\t','@','/','\\','_','=','`','<','>','[',']','{','}'])>=0)
            return Array.Empty<Issue>();
        var result=new List<Issue>();
        for(int mark=0;mark<text.Length;mark++)
        {
            if(text[mark] is not (',' or ';'))continue;
            int start=mark,end=mark+1;
            while(start>0 && text[start-1]==' ')start--;
            while(end<text.Length && text[end]==' ')end++;
            int left=start,right=end;
            while(left>0 && WordChar(text[left-1]))left--;
            while(right<text.Length && WordChar(text[right]))right++;
            if(!Word(text,left,start) || !Word(text,end,right) ||
                !LeftBoundary(text,left) || !RightBoundary(text,right) ||
                Upper(text,left,start) && Upper(text,end,right))continue;
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
    }
    private static bool WordChar(char c)=>char.IsLetter(c) || char.GetUnicodeCategory(c) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark;
    private static bool Word(string text,int start,int end)=>start<end && char.IsLetter(text[start]) &&
        text[start..end].Count(char.IsLetter)>=2;
    private static bool Upper(string text,int start,int end)=>text[start..end].All(c=>!char.IsLetter(c) || char.IsUpper(c));
    private static bool LeftBoundary(string text,int start)=>start==0 || text[start-1]==' ' ||
        text[start-1]=='(' && (start==1 || text[start-2]==' ');
    private static bool RightBoundary(string text,int end)
    {
        if(end==text.Length || text[end] is ' ' or '\r' or '\n' or '\a' or ',' or ';')return true;
        if(text[end]==')')return end+1==text.Length || text[end+1] is ' ' or '\r' or '\n' or '\a' or ',' or ';' or '.' or ':' or '!' or '?';
        // A sentence-ending dot is allowed; a domain/file extension is not.
        return text[end] is '.' or '!' or '?' && (end+1==text.Length || text[end+1] is ' ' or '\r' or '\n' or '\a');
    }
}
