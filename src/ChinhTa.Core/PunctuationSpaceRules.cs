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
        Dictionary<char,Dictionary<int,int>>? delimiters=null;
        for(int mark=0;mark<text.Length;mark++)
        {
            if(text[mark] is not (',' or ';'))continue;
            int start=mark,end=mark+1;
            while(start>0 && text[start-1]==' ')start--;
            while(end<text.Length && text[end]==' ')end++;
            int left=start,right=end;
            while(left>0 && WordChar(text[left-1]))left--;
            while(right<text.Length && WordChar(text[right]))right++;
            bool wordLeft=Word(text,left,start) && LeftBoundary(text,left);
            bool delimitedLeft=!wordLeft && start>0 && (text[start-1] is ')' or '”') && DelimitedLeft(start-1,text[start-1]);
            if(!Word(text,end,right) || !RightBoundary(text,right) || !wordLeft && !delimitedLeft ||
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
        bool DelimitedLeft(int close,char closing)
        {
            // Parse each delimiter kind once per paragraph, only on demand.
            // Cache failure as an empty map; no repeated scans for later marks.
            delimiters??=new();
            if(!delimiters.TryGetValue(closing,out var pairs))
            {
                pairs=new();delimiters.Add(closing,pairs);
                if(!SentenceSpaceContext.Supported(text.TrimEnd('\r','\n','\a')) ||
                    closing=='”' && text.IndexOfAny(['"','«','»','„','‟','‘','’'])>=0)return false;
                char openingMark=closing==')'?'(':'“';int open=-1;
                for(int i=0;i<text.Length;i++)
                    if(text[i]==openingMark)
                    {
                        if(open>=0){pairs.Clear();return false;}
                        open=i;
                    }
                    else if(text[i]==closing)
                    {
                        if(open<0){pairs.Clear();return false;}
                        pairs.Add(i,open);open=-1;
                    }
                if(open>=0)pairs.Clear();
            }
            if(!pairs.TryGetValue(close,out int opening))return false;
            int leftEnd=opening;
            while(leftEnd>0 && text[leftEnd-1]==' ')leftEnd--;
            if(leftEnd==opening || !SentenceSpaceContext.ClearEnding(text,leftEnd))return false;
            int wordStart=leftEnd;
            while(wordStart>0 && WordChar(text[wordStart-1]))wordStart--;
            if(ParenthesisSpaceRules.IsFunctionName(text[wordStart..leftEnd]))return false;
            string inside=text[(opening+1)..close];
            var words=inside.Split(' ',StringSplitOptions.RemoveEmptyEntries);
            return words.Length>=2 && inside.Any(char.IsLower) &&
                words.All(word=>char.IsLetter(word[0]) && word.Count(char.IsLetter)>=2 && word.All(WordChar));
        }
    }
    private static bool WordChar(char c)=>char.IsLetter(c) || char.GetUnicodeCategory(c) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark;
    private static bool Word(string text,int start,int end)=>start<end && char.IsLetter(text[start]) &&
        text[start..end].Count(char.IsLetter)>=2;
    private static bool Upper(string text,int start,int end)=>text[start..end].All(c=>!char.IsLetter(c) || char.IsUpper(c));
    private static bool LeftBoundary(string text,int start)=>start==0 || text[start-1]==' ' ||
        text[start-1] is '(' or '“' && (start==1 || text[start-2]==' ');
    private static bool RightBoundary(string text,int end)
    {
        if(end==text.Length || text[end] is ' ' or '\r' or '\n' or '\a' or ',' or ';')return true;
        if(text[end] is ')' or '”')return end+1==text.Length || text[end+1] is ' ' or '\r' or '\n' or '\a' or ',' or ';' or '.' or ':' or '!' or '?';
        // A sentence-ending dot is allowed; a domain/file extension is not.
        return text[end] is '.' or '!' or '?' && (end+1==text.Length || text[end+1] is ' ' or '\r' or '\n' or '\a');
    }
}
