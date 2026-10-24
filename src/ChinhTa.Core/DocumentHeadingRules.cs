using System.Text.RegularExpressions;

namespace ChinhTa.Core;

// Named State documents only. Type capitalization is limited to exact supported names in a proven front-title role; subject content is never inferred.
public static class DocumentHeadingRules
{
    public const string Citation="NĐ30, PL I, Phần I.I.4 và II.5.b (PDF 21, 24): tên loại/trích yếu cỡ 13–14, đứng, đậm; tên loại in hoa. Chưa kiểm mọi mẫu và ngoại lệ.";
    private static string Clean(string text)=>text.TrimEnd('\r','\a','\n');
    private static bool BodyMarker(string text)=>Regex.IsMatch(text.TrimStart(),
        @"^(?:Kính gửi|Căn cứ|Nơi nhận|Điều\s+[0-9]|Chương\s+[IVXLCDM0-9]+(?:[ .:]|$)|[IVXLCDM0-9]+[.)])",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
    private static Issue Manual(Paragraph p,string message,string? title=null,string? citation=null)
    {
        string text=Clean(p.Text);
        return new(p.Start,text.Length,text,text,message,false,FixKind.DocumentHeadingFormat,CanFix:false,
            Citation:citation??Citation,Title:title??"Kiểm tra tên loại/trích yếu");
    }
    // Read-only candidates; Check below remains the authority for role/repair
    // eligibility. This avoids reading every centered body paragraph over COM.
    public static IReadOnlyList<Paragraph> ObservationTargets(IReadOnlyList<Paragraph> paragraphs,IReadOnlyList<LayoutParagraph> layout,DocumentProfile profile)
    {
        if(profile!=DocumentProfile.State)return [];
        var subjects=ShapeLineRules.Identify(layout,profile).Where(t=>t.Role==LineRole.Subject).ToArray();
        if(subjects.Length!=1)return [];
        var target=subjects[0];
        return paragraphs.Take(40).Where(p=>p.Page==1 && !p.InTable && p.Start>=target.RegionStart && p.Start<=target.Start &&
            !string.IsNullOrWhiteSpace(Clean(p.Text))).ToArray();
    }
    public static IReadOnlyList<Issue> Check(IReadOnlyList<Paragraph> paragraphs,IReadOnlyList<LayoutParagraph> layout,DocumentProfile profile)
    {
        if(profile!=DocumentProfile.State)return [];
        var first=paragraphs.Take(40).Where(p=>p.Page==1).ToArray();
        var headings=layout.Where(p=>p.Page==1 && HeadingRules.IsCandidate(p.Text,profile)).ToArray();
        var sourceHeadings=first.Select((p,i)=>(p,i)).Where(x=>HeadingRules.IsCandidate(x.p.Text,profile)).ToArray();
        if(sourceHeadings.Length!=2 || sourceHeadings.Any(x=>x.i>=16) ||
            headings.Any(h=>!sourceHeadings.Any(x=>x.p.Start==h.Start && Clean(x.p.Text)==Clean(h.Text))) || headings.Length!=2 || headings.Count(p=>Clean(p.Text).TrimStart().StartsWith("Độc lập",StringComparison.OrdinalIgnoreCase))!=1)return [];
        int headerEnd=headings.Max(p=>p.Start);
        var types=first.Where(p=>!p.InTable && p.Start>headerEnd && ShapeLineRules.IsDocumentType(p.Text)).ToArray();
        if(types.Length==0)return [];
        var type=types[0];
        string citation=DocumentTypeCatalog.WithSource(Citation,DocumentTypeCatalog.IsUserHeading(type.Text));
        Issue Notice(Paragraph row,string message,string? label=null)=>Manual(row,message,label,citation);
        // A quoted/body heading after ordinary prose is not the front document title.
        if(first.Any(p=>p.Start>headerEnd && p.Start<type.Start && !p.InTable && !string.IsNullOrWhiteSpace(Clean(p.Text)) &&
            !AdministrativeHeaderRules.IsCandidate(p.Text)))return [];
        var targets=ShapeLineRules.Identify(layout,profile).Where(t=>t.Role==LineRole.Subject && t.RegionStart==type.Start).ToArray();
        var sourceType=layout.Where(p=>p.Start==type.Start && Clean(p.Text)==Clean(type.Text)).ToArray();
        if(types.Length!=1 || targets.Length!=1 || sourceType.Length!=1 || !(sourceType[0].UnindentedCenter??sourceType[0].Centered))
            return [Notice(type,"Chưa xác định chắc khối tên loại/trích yếu hoặc chưa xác minh căn giữa không thụt lề. Giữ thủ công.")];
        var subject=layout.Where(p=>p.Page==1 && p.Start>type.Start && p.Start<=targets[0].Start).ToArray();
        var following=layout.Where(p=>p.Page==1 && p.Start>targets[0].Start && !string.IsNullOrWhiteSpace(Clean(p.Text))).FirstOrDefault();
        if(subject.Length<1 || subject.Any(p=>p.InTable || !(p.UnindentedCenter??p.Centered) || string.IsNullOrWhiteSpace(Clean(p.Text)) || BodyMarker(p.Text)) ||
            following is null || following.InTable || (following.ParagraphCentered??following.Centered) && !BodyMarker(following.Text))
            return [Notice(type,"Chưa phân định chắc trích yếu với nội dung trong vùng đã nhận diện. Giữ thủ công; không tự định dạng nội dung.")];
        var rows=sourceType.Concat(subject).ToArray();
        if(rows.Any(row=>first.Count(p=>p.Start==row.Start && Clean(p.Text)==Clean(row.Text))!=1))
            return [Notice(type,"Mốc chữ/range của tên loại hoặc trích yếu chưa khớp đủ; cần quét lại, không tự sửa.")];
        var result=new List<Issue>();
        foreach(var row in rows)
        {
            var p=first.Single(p=>p.Start==row.Start && Clean(p.Text)==Clean(row.Text));
            bool isTitle=p.Start==type.Start;string text=Clean(p.Text),label=isTitle?"tên loại":"trích yếu";
            var assessment=new FormatPolicy(13,14,false,true).Assess(p);
            if(assessment.State is FormatState.Unknown or FormatState.NeedsReview || text.IndexOfAny(['\t','\f','\r','\n','\a'])>=0)
            {
                result.Add(Notice(p,assessment.State==FormatState.NeedsReview?
                    "Có định dạng sai nhưng cỡ hợp lệ và không hợp lệ đang trộn; cần duyệt từng vùng để giữ cỡ đúng.":
                    "Chưa xác minh đủ chữ/định dạng của "+label+". Có thể có trường, đối tượng nhúng hoặc thuộc tính chưa đọc được; giữ thủ công.","Kiểm tra định dạng "+label));
                continue;
            }
            if(assessment.State==FormatState.Repairable)
            {
                result.Add(new(p.Start,text.Length,text,text,"Times New Roman, màu đen, cỡ 13–14, chữ đứng, đậm; giữ cỡ hợp lệ, nguyên văn, gạch chân và căn đoạn. Vị trí/nét kẻ kiểm riêng.",
                    false,FixKind.DocumentHeadingFormat,Citation:citation,Title:"Định dạng "+label,CharacterFormat:new(assessment.Size,false,true,PreserveSize:assessment.PreserveSize)));
            }
            if(isTitle && text!=text.ToUpperInvariant())
            {
                string name=text.Trim(' ');int offset=text.Length-text.TrimStart(' ').Length;
                if(p.TextEditable && !assessment.PreserveSize && name==name.Trim() && ShapeLineRules.IsDocumentType(name) && name.ToUpperInvariant().Length==name.Length)
                    result.Add(new(p.Start+offset,name.Length,name,name.ToUpperInvariant(),
                        "Tên loại văn bản được trình bày bằng chữ in hoa; chỉ đổi hoa/thường của tên đã nhận diện, giữ trích yếu, khoảng trắng ngoài tên, phông và căn đoạn.",
                        false,FixKind.DocumentTypeText,Citation:citation,Title:"Tên loại văn bản chưa viết hoa"));
                else result.Add(Notice(p,"Tên loại cần viết hoa nhưng chưa đủ bằng chứng để thay chữ an toàn; duyệt thủ công.","Kiểm tra cách viết tên loại"));
            }
            if(!isTitle && text.Any(char.IsLetter) && text==text.ToUpperInvariant())
                result.Add(Notice(p,"Trích yếu đang toàn chữ hoa; cần duyệt cách viết, giữ tên riêng/mã viết tắt, chưa tự đổi chữ.","Kiểm tra cách viết trích yếu"));
        }
        return result;
    }
}
