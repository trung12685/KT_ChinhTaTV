using System.Globalization;
using System.Text.RegularExpressions;

namespace ChinhTa.Core;

// Only delete ASCII spaces before a single .?! between two clear prose spans.
// Require two adjacent left words and a separated, title-case right word.
public static class SentenceInnerSpaceRules
{
    private static readonly Regex DateWords=new(@"\bngày\b[^\r\n\a]*\btháng\b[^\r\n\a]*\bnăm\b",
        RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.NonBacktracking);
    private static readonly HashSet<string> Abbreviations=new(StringComparer.OrdinalIgnoreCase)
        {"tp","tx","tt","tr","ts","ths","pgs","gs","dr","mr","mrs","ms","etc","vs","st","no","tel","fax","tnhh","http","https","ftp","file","mailto","data","urn"};
    private static readonly HashSet<string> Suffixes=new(StringComparer.OrdinalIgnoreCase)
        {"com","net","org","vn","gov","edu","io","dev","doc","docx","xls","xlsx","ppt","pptx","pdf","txt","exe","zip","rar","csv","xml","json","dll","log","jpg","png"};
    public static IReadOnlyList<Issue> Check(Paragraph paragraph)
    {
        string text=paragraph.Text.TrimEnd('\r','\n','\a');
        if(AdministrativeHeaderRules.IsCandidate(text) || DateWords.IsMatch(text) ||
            text.IndexOfAny(['\t','\r','\n','\a','\v','\f','\u00a0','@','/','\\','_','=','`','<','>','[',']','{','}','+','*','^','|'])>=0)
            return Array.Empty<Issue>();
        var result=new List<Issue>();
        for(int mark=0;mark+1<text.Length;mark++)
        {
            if(text[mark] is not ('.' or '?' or '!') || text[mark+1]!=' ')continue;
            int start=mark;
            while(start>0 && text[start-1]==' ')start--;
            if(start==mark)continue;
            int lastStart=start;
            while(lastStart>0 && WordChar(text[lastStart-1]))lastStart--;
            if(!Word(text,lastStart,start) || text[lastStart..start].Any(c=>char.IsLetter(c) && !char.IsLower(c)) ||
                text[lastStart..start].All(c=>"ivxlcdm".Contains(c)) || Abbreviations.Contains(text[lastStart..start]) ||
                lastStart==0 || text[lastStart-1]!=' ')continue;
            int previousEnd=lastStart;
            while(previousEnd>0 && text[previousEnd-1]==' ')previousEnd--;
            int previousStart=previousEnd;
            while(previousStart>0 && WordChar(text[previousStart-1]))previousStart--;
            if(!Word(text,previousStart,previousEnd) || previousStart>0 && text[previousStart-1] is not (' ' or ',' or ';'))continue;
            int rightStart=mark+1;
            while(rightStart<text.Length && text[rightStart]==' ')rightStart++;
            int rightEnd=rightStart;
            while(rightEnd<text.Length && WordChar(text[rightEnd]))rightEnd++;
            if(!Word(text,rightStart,rightEnd) || !char.IsUpper(text[rightStart]) ||
                !text[(rightStart+1)..rightEnd].Any(char.IsLower) ||
                text[(rightStart+1)..rightEnd].Any(c=>char.IsLetter(c) && !char.IsLower(c)) ||
                Suffixes.Contains(text[rightStart..rightEnd]) ||
                rightEnd<text.Length && text[rightEnd]!=' ')continue;
            if(!paragraph.TextEditable)
                return [new(paragraph.Start,text.Length,text,text,
                    "Có khoảng trắng trước dấu kết câu giữa đoạn trong vùng chưa cho phép sửa chữ. Kiểm tra thủ công.",
                    false,FixKind.SentenceInnerSpace,CanFix:false,Title:"Dấu kết câu giữa đoạn trong vùng có cấu trúc")];
            result.Add(new(paragraph.Start+start,mark-start,text[start..mark],"",
                "Đề xuất xóa dấu cách trước dấu kết câu giữa đoạn; giữ chữ, dấu câu và khoảng trắng sau. Cần duyệt cả câu.",
                false,FixKind.SentenceInnerSpace,Title:"Khoảng trắng trước dấu kết câu giữa đoạn"));
        }
        return result;
    }
    private static bool WordChar(char c)=>char.IsLetter(c) || char.GetUnicodeCategory(c) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark;
    private static bool Word(string text,int start,int end)=>start<end && char.IsLetter(text[start]) && text[start..end].Count(char.IsLetter)>=2;
}
