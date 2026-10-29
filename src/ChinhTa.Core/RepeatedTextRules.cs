using System.Text;
using System.Text.RegularExpressions;

namespace ChinhTa.Core;

// Mechanical repetition only. No dictionary or inference about the author's meaning.
public static class RepeatedTextRules
{
    private sealed record Token(int Start,int End,string Key);
    private static readonly Regex Words=new(@"(?<![\p{L}\p{M}\p{N}_])\p{L}[\p{L}\p{M}]*(?![\p{L}\p{M}\p{N}_])",RegexOptions.CultureInvariant);
    private static bool Spaces(string text,int start,int end)=>end>start && text.AsSpan(start,end-start).IndexOfAnyExcept(' ')<0;
    public static IReadOnlyList<Issue> Check(Paragraph paragraph)
    {
        string text=paragraph.Text;
        bool technical=TechnicalTextContext.IsTechnical(text);
        var tokens=Words.Matches(text).Select(m=>new Token(m.Index,m.Index+m.Length,m.Value.Normalize(NormalizationForm.FormC))).ToArray();
        bool paragraphInitial=tokens.Length>0 && text.AsSpan(0,tokens[0].Start).IndexOfAnyExcept(' ')<0;
        bool SameUnit(int first,int next,int size)
        {
            for(int k=0;k<size;k++)
                if(tokens[first+k].Key!=tokens[next+k].Key && !(k==0 && first==0 && size>=2 && paragraphInitial &&
                    InitialCapitalOnly(tokens[first].Key,tokens[next].Key)))return false;
            return true;
        }
        var found=new List<Issue>();
        for(int runStart=0;runStart<tokens.Length;)
        {
            int runEnd=runStart+1;
            while(runEnd<tokens.Length && Spaces(text,tokens[runEnd-1].End,tokens[runEnd].Start))runEnd++;
            int i=runStart;
            while(i<runEnd)
            {
                int unit=0;
                for(int size=1;size<=8 && i+2*size<=runEnd;size++)
                    if(SameUnit(i,i+size,size)){unit=size;break;}
                if(unit==0){i++;continue;}
                int end=i+2*unit;
                while(end+unit<=runEnd && SameUnit(i,end,unit))end+=unit;
                if(!paragraph.TextEditable || technical)
                {
                    string whole=text.TrimEnd('\r','\a','\n');
                    return [new(paragraph.Start,whole.Length,whole,whole,
                        technical && paragraph.TextEditable?
                            "Có từ/cụm từ lặp trong đoạn có đường dẫn, địa chỉ hoặc biểu thức kỹ thuật. Giữ nguyên dữ liệu, kiểm tra thủ công; không tự xóa phần lặp.":
                            "Có từ/cụm từ lặp nhưng vùng chưa được phép sửa chữ; kiểm tra thủ công để giữ cấu trúc.",false,
                        FixKind.RepeatedText,CanFix:false,Title:technical && paragraph.TextEditable?"Kiểm tra lặp trong đoạn kỹ thuật":"Kiểm tra lặp trong vùng có cấu trúc")];
                }
                int firstEnd=tokens[i+unit-1].End,lastEnd=tokens[end-1].End;
                string phrase=text[tokens[i].Start..firstEnd];
                bool canFix=unit>=2 && phrase.Any(char.IsLower) && !AdministrativeHeaderRules.IsCandidate(text);
                int start=canFix?firstEnd:tokens[i].Start;
                string original=text[start..lastEnd];
                found.Add(new(paragraph.Start+start,original.Length,original,canFix?"":original,
                    canFix?(tokens[i].Key!=tokens[i+unit].Key?
                        "Cụm chữ lặp ở đầu đoạn, chỉ khác chữ hoa đầu tiên. Đề xuất xóa phần lặp, giữ nguyên chữ hoa và kiểu của lần đầu; cần duyệt cả câu, có thể là lặp có chủ ý.":
                        "Cụm chữ giống nhau lặp liền nhau. Đề xuất chỉ xóa phần lặp, giữ lần đầu; phải đọc cả câu trước khi duyệt, có thể là lặp có chủ ý."):
                        "Một tiếng, chuỗi toàn chữ hoa hoặc dòng số/ngày đang lặp. Có thể là từ láy, cách nhấn mạnh hoặc mã/tên; chỉ cảnh báo, không tự xóa.",
                    false,FixKind.RepeatedText,CanFix:canFix,Title:(canFix?"Cụm từ lặp: ":"Lặp cần duyệt: ")+phrase));
                i=end;
            }
            runStart=runEnd;
        }
        return found;
    }
    // One initial capital at the actual paragraph start is presentation, not a
    // license to fold names/codes throughout a sentence or to remove single words.
    private static bool InitialCapitalOnly(string first,string next)=>
        first.Count(char.IsLetter)>=2 && char.IsUpper(first[0]) &&
        first.Skip(1).All(c=>!char.IsLetter(c) || char.IsLower(c)) &&
        next==char.ToLowerInvariant(first[0])+first[1..];
}
