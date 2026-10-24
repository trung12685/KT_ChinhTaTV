using System.Globalization;
using System.Text.RegularExpressions;

namespace ChinhTa.Core;

// Independent ASCII-space edits on each side of a prose colon. A missing gap
// uses the colon as a positive source anchor and never rewrites either word.
public static class ColonSpaceRules
{
    private static readonly Regex DateWords=new(@"\bngày\b[^\r\n\a]*\btháng\b[^\r\n\a]*\bnăm\b",
        RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.NonBacktracking);
    private static readonly HashSet<string> Protocols=new(StringComparer.OrdinalIgnoreCase)
        {"http","https","ftp","file","mailto","tel","data","urn"};
    public static IReadOnlyList<Issue> Check(Paragraph paragraph)
    {
        string text=paragraph.Text.TrimEnd('\r','\n','\a');
        if(AdministrativeHeaderRules.IsCandidate(text) || DateWords.IsMatch(text) ||
            text.IndexOfAny(['\t','\r','\n','\a','\v','\f','@','/','\\','_','=','`','<','>','[',']','{','}','+','*','^','|'])>=0)return Array.Empty<Issue>();
        var result=new List<Issue>();
        for(int mark=0;mark<text.Length;mark++)
        {
            if(text[mark]!=':' || mark+1==text.Length)continue;
            int start=mark,rightStart=mark+1;
            while(start>0 && text[start-1]==' ')start--;
            bool missingAfter=text[mark+1]!=' ';
            if(start==mark && !missingAfter)continue;
            int leftStart=start;
            while(leftStart>0 && WordChar(text[leftStart-1]))leftStart--;
            if(!Word(text,leftStart,start) || text[leftStart..start].Any(c=>char.IsLetter(c) && !char.IsLower(c)) ||
                Protocols.Contains(text[leftStart..start]) || leftStart==0 || text[leftStart-1]!=' ')continue;
            int previousEnd=leftStart;
            while(previousEnd>0 && text[previousEnd-1]==' ')previousEnd--;
            int previousStart=previousEnd;
            while(previousStart>0 && WordChar(text[previousStart-1]))previousStart--;
            if(!Word(text,previousStart,previousEnd) || previousStart>0 && text[previousStart-1]!=' ')continue;
            while(rightStart<text.Length && text[rightStart]==' ')rightStart++;
            int rightEnd=rightStart;
            while(rightEnd<text.Length && WordChar(text[rightEnd]))rightEnd++;
            if(!Word(text,rightStart,rightEnd) || !text[rightStart..rightEnd].Any(char.IsLower) || !RightBoundary(text,rightEnd) ||
                Roman(text[leftStart..start]) && Roman(text[rightStart..rightEnd]))continue;
            if(!paragraph.TextEditable)
                return [new(paragraph.Start,text.Length,text,text,
                    "Có khoảng trắng quanh dấu hai chấm cần duyệt trong vùng chưa cho phép sửa chữ. Kiểm tra thủ công.",
                    false,FixKind.ColonSpace,CanFix:false,Title:"Dấu hai chấm trong vùng có cấu trúc")];
            if(start<mark)
                result.Add(new(paragraph.Start+start,mark-start,text[start..mark],"",
                    "Đề xuất xóa dấu cách trước dấu hai chấm; giữ chữ, dấu hai chấm và khoảng cách sau. Cần duyệt cả câu.",
                    false,FixKind.ColonSpace,Title:"Khoảng trắng trước dấu hai chấm"));
            if(missingAfter)
                result.Add(new(paragraph.Start+mark,1,":",": ",
                    "Đề xuất thêm một dấu cách ASCII sau dấu hai chấm; giữ chữ, dấu câu và khoảng trước chưa chọn. Cần duyệt cả câu.",
                    false,FixKind.ColonSpace,Title:"Thiếu khoảng trắng sau dấu hai chấm"));
        }
        return result;
    }
    private static bool WordChar(char c)=>char.IsLetter(c) || char.GetUnicodeCategory(c) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark;
    private static bool Word(string text,int start,int end)=>start<end && char.IsLetter(text[start]) && text[start..end].Count(char.IsLetter)>=2;
    private static bool Roman(string word)=>word.All(c=>"ivxlcdm".Contains(c));
    private static bool RightBoundary(string text,int end)=>end==text.Length || text[end] is ' ' or ',' or ';' ||
        text[end] is '.' or '?' or '!' && (end+1==text.Length || text[end+1]==' ');
}
