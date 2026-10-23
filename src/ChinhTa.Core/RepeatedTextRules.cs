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
        var tokens=Words.Matches(text).Select(m=>new Token(m.Index,m.Index+m.Length,m.Value.Normalize(NormalizationForm.FormC))).ToArray();
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
                    if(Enumerable.Range(0,size).All(k=>tokens[i+k].Key==tokens[i+size+k].Key)){unit=size;break;}
                if(unit==0){i++;continue;}
                int end=i+2*unit;
                while(end+unit<=runEnd && Enumerable.Range(0,unit).All(k=>tokens[i+k].Key==tokens[end+k].Key))end+=unit;
                if(!paragraph.TextEditable)
                {
                    string whole=text.TrimEnd('\r','\a','\n');
                    return [new(paragraph.Start,whole.Length,whole,whole,
                        "Có từ/cụm từ lặp nhưng vùng chưa được phép sửa chữ; kiểm tra thủ công để giữ cấu trúc.",false,
                        FixKind.RepeatedText,CanFix:false,Title:"Kiểm tra lặp trong vùng có cấu trúc")];
                }
                int firstEnd=tokens[i+unit-1].End,lastEnd=tokens[end-1].End;
                string phrase=text[tokens[i].Start..firstEnd];
                bool canFix=unit>=2 && phrase.Any(char.IsLower) && !AdministrativeHeaderRules.IsCandidate(text);
                int start=canFix?firstEnd:tokens[i].Start;
                string original=text[start..lastEnd];
                found.Add(new(paragraph.Start+start,original.Length,original,canFix?"":original,
                    canFix?"Cụm chữ giống nhau lặp liền nhau. Đề xuất chỉ xóa phần lặp, giữ lần đầu; phải đọc cả câu trước khi duyệt, có thể là lặp có chủ ý.":
                        "Một tiếng, chuỗi toàn chữ hoa hoặc dòng số/ngày đang lặp. Có thể là từ láy, cách nhấn mạnh hoặc mã/tên; chỉ cảnh báo, không tự xóa.",
                    false,FixKind.RepeatedText,CanFix:canFix,Title:(canFix?"Cụm từ lặp: ":"Lặp cần duyệt: ")+phrase));
                i=end;
            }
            runStart=runEnd;
        }
        return found;
    }
}
