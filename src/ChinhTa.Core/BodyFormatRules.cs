using System.Text.RegularExpressions;

namespace ChinhTa.Core;

// First body-format scope: ordinary, unnumbered prose in a confirmed named State
// document, after Kính gửi and before a signature/recipient boundary. This does
// not infer a body from page number, font, or every paragraph after the title.
public static class BodyFormatRules
{
    public const string Citation="NĐ30, PL I, Phần I.I.4 và II.6.e (PDF 21, 25): Times New Roman, màu đen, nội dung cỡ 13–14, đứng. Bước này chỉ sửa phông/màu/cỡ đoạn văn xuôi thường; chưa kiểm toàn bộ nội dung hoặc căn/giãn đoạn.";
    private static string Clean(string text)=>text.TrimEnd('\r','\a','\n');
    private static bool Stop(string text)=>RecipientContext.Label(text) || Regex.IsMatch(text.Trim(),
        @"^(?:(?:TM|KT|TL|TUQ|Q)\s*\.|(?:GIÁM ĐỐC|PHÓ GIÁM ĐỐC|TRƯỞNG PHÒNG|PHÓ TRƯỞNG PHÒNG|CHỦ TỊCH|PHÓ CHỦ TỊCH|BỘ TRƯỞNG|THỨ TRƯỞNG|THỦ TRƯỞNG|HIỆU TRƯỞNG|CHÁNH VĂN PHÒNG)$)",RegexOptions.CultureInvariant|RegexOptions.IgnoreCase);
    private static bool Prose(Paragraph row)
    {
        string text=Clean(row.Text).Trim(' ');
        if(!row.TextEditable || row.InTable || row.Outline is not {Scope:OutlineScope.PlainBody,HeadingLevel:null,Alignment:0 or 3} ||
            text.Length==0 || text.Length>4000 || text.Any(char.IsControl) || !char.IsUpper(text[0]) ||
            text[^1] is not ('.' or '?' or '!' or ';') || text==text.ToUpperInvariant() ||
            Regex.Matches(text,@"\p{L}[\p{L}\p{M}]*").Count<6)return false;
        return !Regex.IsMatch(text,@"^(?:Kính gửi|Căn cứ|Nơi nhận|Lưu\s*:|Phần\b|Chương\b|Mục\b|Tiểu mục\b|Điều\s+\d|[\p{L}][.)]|[IVXLCDM]+[.)])",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
    }
    public static IReadOnlyList<Paragraph> ObservationTargets(IReadOnlyList<Paragraph> paragraphs,IReadOnlyList<LayoutParagraph> layout,DocumentProfile profile)
    {
        if(profile!=DocumentProfile.State || paragraphs.Select(p=>p.Start).Distinct().Count()!=paragraphs.Count)return [];
        var titleRows=DocumentHeadingRules.ObservationTargets(paragraphs,layout,profile);
        if(titleRows.Count<2 || !ShapeLineRules.IsDocumentType(titleRows[0].Text))return [];
        var title=titleRows[0];
        if(titleRows.Any(p=>!layout.Any(l=>l.Start==p.Start && Clean(l.Text)==Clean(p.Text) && !l.InTable && (l.UnindentedCenter??l.Centered))))return [];
        var headers=paragraphs.Take(16).Where(p=>p.Page==1 && HeadingRules.IsCandidate(p.Text,profile)).ToArray();
        if(headers.Length!=2 || headers.Any(p=>p.Start>=title.Start) ||
            headers.Count(p=>Clean(p.Text).TrimStart().StartsWith("Độc lập",StringComparison.OrdinalIgnoreCase))!=1)return [];
        int headerEnd=headers.Max(p=>p.Start);
        if(paragraphs.Any(p=>p.Start>headerEnd && p.Start<title.Start && !p.InTable && !string.IsNullOrWhiteSpace(Clean(p.Text)) &&
            !AdministrativeHeaderRules.IsCandidate(p.Text)))return [];
        var following=paragraphs.Where(p=>p.Start>titleRows[^1].Start && !string.IsNullOrWhiteSpace(Clean(p.Text))).OrderBy(p=>p.Start).ToArray();
        if(following.Length<3 || !Regex.IsMatch(Clean(following[0].Text).TrimStart(),@"^Kính gửi\s*:",RegexOptions.CultureInvariant|RegexOptions.IgnoreCase) ||
            !following[0].TextEditable || following[0].Outline is not {Scope:OutlineScope.PlainBody,HeadingLevel:null})return [];
        int end=Array.FindIndex(following,1,p=>Stop(Clean(p.Text)));
        if(end<2)return [];
        return following.Skip(1).Take(end-1).Where(Prose).ToArray();
    }
    public static IReadOnlyList<Issue> Check(IReadOnlyList<Paragraph> paragraphs,IReadOnlyList<LayoutParagraph> layout,DocumentProfile profile)
    {
        var result=new List<Issue>();
        foreach(var row in ObservationTargets(paragraphs,layout,profile))
        {
            // Mixed emphasis can be deliberate. Do not erase it or call it a
            // defect; later scopes can distinguish ordinary text and emphasis.
            var fonts=row.FormatEvidence?.Fonts;
            if(fonts is null || fonts.Count==0 || fonts.Any(f=>!f.Known() || f.Bold!=0 || f.Italic!=0))continue;
            var assessment=new FormatPolicy(13,14,false,false).Assess(row,allowSelectiveSizeRepair:true);
            if(assessment.State!=FormatState.Repairable)continue;
            string text=Clean(row.Text);
            result.Add(new(row.Start,text.Length,text,text,
                "Đoạn văn xuôi thường: Times New Roman, đen, cỡ 13–14; chỉ sửa cỡ sai về 13, giữ từng cỡ đúng, chữ, gạch chân và bố cục đoạn.",
                false,FixKind.BodyFormat,Citation:Citation,Title:"Phông/cỡ chữ nội dung chưa đúng",
                CharacterFormat:new(assessment.Size,false,false,ComponentRole.Body,assessment.PreserveSize,assessment.RepairInvalidSizes)));
        }
        return result;
    }
}
