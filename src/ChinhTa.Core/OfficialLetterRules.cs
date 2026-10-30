using System.Text.RegularExpressions;

namespace ChinhTa.Core;

public sealed record OfficialLetterSubject(LayoutParagraph Number,LayoutParagraph Subject);

// A V/v in ordinary prose is not a document type. Bind the first-page header,
// number and subject before choosing the separate official-letter policy.
public static class OfficialLetterRules
{
    public const string Citation="NĐ30, PL I, Phần I.I.4 và II.5.b (PDF 21, 24): trích yếu công văn sau V/v, Times New Roman đen, cỡ 12–13, đứng, không đậm; căn giữa dưới số/ký hiệu, cách 6 pt. Không yêu cầu đường kẻ trích yếu; bảng Word không phải điều kiện bắt buộc.";
    private static string Clean(string text)=>text.TrimEnd('\r','\a','\n');
    public static bool IsSubject(string text)=>Regex.IsMatch(Clean(text).Trim(),@"^V/v\s+\S",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
    public static OfficialLetterSubject? Identify(IReadOnlyList<LayoutParagraph> layout,DocumentProfile profile)
    {
        if(profile!=DocumentProfile.State || layout.Select(p=>p.Start).Distinct().Count()!=layout.Count)return null;
        var front=layout.Where(p=>p.Page==1).Take(40).ToArray();
        var headings=front.Select((p,i)=>(p,i)).Where(x=>HeadingRules.IsCandidate(x.p.Text,profile)).ToArray();
        if(headings.Length!=2 || headings.Any(x=>x.i>=16) || headings.Count(x=>Clean(x.p.Text).TrimStart().StartsWith("Độc lập",StringComparison.OrdinalIgnoreCase))!=1)return null;
        var candidates=front.Where(p=>IsSubject(p.Text)).ToArray();
        var numbers=front.Where(p=>AdministrativeHeaderRules.IsNumber(p.Text)).ToArray();
        if(candidates.Length!=1 || numbers.Length!=1 || front.Any(p=>!p.InTable && ShapeLineRules.IsDocumentType(p.Text)))return null;
        var subject=candidates[0];var number=numbers[0];
        var code=Regex.Match(Clean(number.Text),@"/\s*(?<type>[\p{L}\p{M}]+)\s*-");
        if(code.Success && DocumentTypeCatalog.FindAbbreviation(code.Groups["type"].Value) is not null)return null;
        if(subject.Start<=number.Start || subject.InTable!=number.InTable)return null;
        var between=front.Where(p=>p.Start>number.Start && p.Start<subject.Start).ToArray();
        if(subject.InTable)
        {
            var motto=headings.Single(x=>Clean(x.p.Text).TrimStart().StartsWith("Độc lập",StringComparison.OrdinalIgnoreCase)).p;
            if(subject.TableStart<0 || subject.TableStart!=number.TableStart || subject.CellStart!=number.CellStart || subject.CellEnd!=number.CellEnd ||
                subject.CellStart>number.Start || subject.CellEnd<=subject.Start || !motto.InTable || motto.TableStart!=subject.TableStart ||
                subject.CellStart>=motto.CellStart || subject.CellEnd>motto.CellStart+1 || between.Any(p=>!string.IsNullOrWhiteSpace(Clean(p.Text))))return null;
            if(front.Any(p=>p.Start>subject.Start && p.InTable && p.TableStart==subject.TableStart && p.CellStart==subject.CellStart && !string.IsNullOrWhiteSpace(Clean(p.Text))))return null;
        }
        else
        {
            // Untabled layouts are accepted only with an intact header and a
            // following greeting. No table is fabricated or demanded by NĐ30.
            if(headings.Any(x=>x.p.InTable || x.p.Start>subject.Start) || between.Any(p=>!string.IsNullOrWhiteSpace(Clean(p.Text)) && !HeadingRules.IsCandidate(p.Text,profile) && !AdministrativeHeaderRules.IsCandidate(p.Text)))return null;
            var following=front.FirstOrDefault(p=>p.Start>subject.Start && !string.IsNullOrWhiteSpace(Clean(p.Text)));
            if(following is null || following.InTable || !Regex.IsMatch(Clean(following.Text).TrimStart(),@"^Kính gửi\s*:",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant))return null;
        }
        return new(number,subject);
    }
    public static IReadOnlyList<Paragraph> ObservationTargets(IReadOnlyList<Paragraph> paragraphs,IReadOnlyList<LayoutParagraph> layout,DocumentProfile profile)
    {
        var target=Identify(layout,profile);if(target is null)return [];
        var required=layout.Where(p=>p.Start==target.Number.Start || p.Page==1 && HeadingRules.IsCandidate(p.Text,profile)).ToArray();
        if(required.Any(l=>paragraphs.Count(p=>p.Page==1 && p.Start==l.Start && p.InTable==l.InTable && Clean(p.Text)==Clean(l.Text))!=1))return [];
        var matches=paragraphs.Where(p=>p.Page==1 && p.Start==target.Subject.Start && p.InTable==target.Subject.InTable && Clean(p.Text)==Clean(target.Subject.Text)).ToArray();
        return matches.Length==1?matches:[];
    }
    public static IReadOnlyList<Issue> Check(IReadOnlyList<Paragraph> paragraphs,IReadOnlyList<LayoutParagraph> layout,DocumentProfile profile)
    {
        var target=Identify(layout,profile);var rows=ObservationTargets(paragraphs,layout,profile);
        if(target is null || rows.Count!=1)return [];
        var row=rows[0];string text=Clean(row.Text);var result=new List<Issue>();
        var assessment=new FormatPolicy(12,13,false,false).Assess(row,true);
        if(assessment.State==FormatState.Repairable && !text.Any(char.IsControl))
            result.Add(new(row.Start,text.Length,text,text,"Trích yếu công văn: Times New Roman, đen, cỡ 12–13, đứng, không đậm; giữ từng cỡ hợp lệ, chữ và căn đoạn. Không thêm đường kẻ dưới trích yếu.",false,
                FixKind.DocumentHeadingFormat,Citation:Citation,Title:"Định dạng trích yếu công văn",CharacterFormat:new(assessment.Size,false,false,ComponentRole.LetterSubject,assessment.PreserveSize,assessment.RepairInvalidSizes)));
        else if(assessment.State!=FormatState.Correct)
            Notice("Chưa đủ bằng chứng để sửa định dạng trích yếu công văn; không kết luận chữ hiện tại sai.","Kiểm tra định dạng trích yếu công văn");
        if(!(target.Subject.UnindentedCenter??target.Subject.Centered))
            Notice("Trích yếu công văn cần căn giữa dưới số/ký hiệu trong vùng đầu bên trái; cần duyệt vị trí. Không áp dụng vị trí trích yếu dưới tên loại ở giữa trang.","Vị trí trích yếu công văn");
        void Notice(string message,string title)=>result.Add(new(row.Start,text.Length,text,text,message,false,FixKind.Advisory,CanFix:false,Citation:Citation,Title:title));
        return result;
    }
}
