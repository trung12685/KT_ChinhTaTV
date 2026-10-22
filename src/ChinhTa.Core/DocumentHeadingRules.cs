using System.Text.RegularExpressions;

namespace ChinhTa.Core;

// Named State documents only. The content and capitalization are never inferred or rewritten.
public static class DocumentHeadingRules
{
    public const string Citation="NĐ30, PL I, Phần I.I.4 và II.5.b (PDF 21, 24): tên loại/trích yếu cỡ 13–14, đứng, đậm; tên loại in hoa. Chưa kiểm mọi mẫu và ngoại lệ.";
    private static string Clean(string text)=>text.TrimEnd('\r','\a','\n');
    private static bool BodyMarker(string text)=>Regex.IsMatch(text.TrimStart(),
        @"^(?:Kính gửi|Căn cứ|Nơi nhận|Điều\s+[0-9]|Chương\s+[IVXLCDM0-9]+(?:[ .:]|$)|[IVXLCDM0-9]+[.)])",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
    private static Issue Manual(Paragraph p,string message,string? title=null)
    {
        string text=Clean(p.Text);
        return new(p.Start,text.Length,text,text,message,false,FixKind.DocumentHeadingFormat,CanFix:false,
            Citation:Citation,Title:title??"Kiểm tra tên loại/trích yếu");
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
        // A quoted/body heading after ordinary prose is not the front document title.
        if(first.Any(p=>p.Start>headerEnd && p.Start<type.Start && !p.InTable && !string.IsNullOrWhiteSpace(Clean(p.Text)) &&
            !AdministrativeHeaderRules.IsCandidate(p.Text)))return [];
        var targets=ShapeLineRules.Identify(layout,profile).Where(t=>t.Role==LineRole.Subject && t.RegionStart==type.Start).ToArray();
        var sourceType=layout.Where(p=>p.Start==type.Start && Clean(p.Text)==Clean(type.Text)).ToArray();
        if(types.Length!=1 || targets.Length!=1 || sourceType.Length!=1 || !sourceType[0].Centered)
            return [Manual(type,"Chưa xác định chắc khối tên loại/trích yếu hoặc chưa xác minh căn giữa không thụt lề. Giữ thủ công.")];
        var subject=layout.Where(p=>p.Page==1 && p.Start>type.Start && p.Start<=targets[0].Start).ToArray();
        var following=layout.Where(p=>p.Page==1 && p.Start>targets[0].Start && !string.IsNullOrWhiteSpace(Clean(p.Text))).FirstOrDefault();
        if(subject.Length is <1 or >4 || subject.Any(p=>p.InTable || !p.Centered || string.IsNullOrWhiteSpace(Clean(p.Text)) || BodyMarker(p.Text)) ||
            following is null || following.InTable || (following.ParagraphCentered??following.Centered))
            return [Manual(type,"Chưa phân định chắc trích yếu với nội dung hoặc khối vượt phạm vi bản thử (tối đa 4 đoạn). Giữ thủ công; không tự định dạng nội dung.")];
        var rows=sourceType.Concat(subject).ToArray();
        if(rows.Any(row=>first.Count(p=>p.Start==row.Start && Clean(p.Text)==Clean(row.Text))!=1))
            return [Manual(type,"Mốc chữ/range của tên loại hoặc trích yếu chưa khớp đủ; cần quét lại, không tự sửa.")];
        var result=new List<Issue>();
        foreach(var row in rows)
        {
            var p=first.Single(p=>p.Start==row.Start && Clean(p.Text)==Clean(row.Text));
            bool isTitle=p.Start==type.Start;string text=Clean(p.Text),label=isTitle?"tên loại":"trích yếu";
            if(!p.FormatObserved || !p.TextEditable || text.IndexOfAny(['\t','\v','\f','\r','\n','\a'])>=0 || p.FormatMixed || p.BoldMixed)
            {
                result.Add(Manual(p,"Chưa xác minh đủ chữ thuần/định dạng đồng nhất của "+label+". Có thể có trường, đối tượng nhúng hoặc kiểu chữ trộn; giữ thủ công.","Kiểm tra định dạng "+label));
                continue;
            }
            if(!float.IsFinite(p.Size) || p.Size<=0 || p.Size>1638 || string.IsNullOrWhiteSpace(p.Font))
            {result.Add(Manual(p,"Chưa đọc được định dạng hợp lệ; giữ thủ công.","Kiểm tra định dạng "+label));continue;}
            if(!p.Font.Equals("Times New Roman",StringComparison.OrdinalIgnoreCase) || p.Size<13 || p.Size>14 || p.Italic || !p.Bold || p.Color is not (0 or -16777216))
            {
                float size=p.Size>=13 && p.Size<=14?p.Size:13;
                result.Add(new(p.Start,text.Length,text,text,"Times New Roman, màu đen, cỡ 13–14, chữ đứng, đậm; giữ cỡ hợp lệ, nguyên văn, gạch chân và căn đoạn. Vị trí/nét kẻ kiểm riêng.",
                    false,FixKind.DocumentHeadingFormat,Citation:Citation,Title:"Định dạng "+label,CharacterFormat:new(size,false,true)));
            }
            if(isTitle && text!=text.ToUpperInvariant() || !isTitle && text.Any(char.IsLetter) && text==text.ToUpperInvariant())
                result.Add(Manual(p,isTitle?"Tên loại cần viết hoa; duyệt thủ công, chưa tự đổi nội dung.":
                    "Trích yếu đang toàn chữ hoa; cần duyệt cách viết, giữ tên riêng/mã viết tắt, chưa tự đổi chữ.","Kiểm tra cách viết "+label));
        }
        return result;
    }
}
