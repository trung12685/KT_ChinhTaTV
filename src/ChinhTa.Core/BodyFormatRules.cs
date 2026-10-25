using System.Text.RegularExpressions;

namespace ChinhTa.Core;

// First body-format scope: ordinary, unnumbered prose in a confirmed named State
// document, bounded by a proven opening and signature/recipient boundary. This does
// not infer a body from page number, font, or every paragraph after the title.
public static class BodyFormatRules
{
    public const string Citation="NĐ30, PL I, Phần I.I.4 và II.6.e (PDF 21, 25): Times New Roman, màu đen, nội dung cỡ 13–14, đứng. Bước này chỉ sửa phông/màu/cỡ đoạn văn xuôi thường; giữ đậm/nghiêng để duyệt riêng; chưa kiểm toàn bộ nội dung hoặc căn/giãn đoạn.";
    private static string Clean(string text)=>text.TrimEnd('\r','\a','\n');
    private static bool Stop(string text)=>RecipientContext.Label(text) || Regex.IsMatch(text.Trim(),
        @"^(?:(?:TM|KT|TL|TUQ|Q)\s*\.|(?:GIÁM ĐỐC|PHÓ GIÁM ĐỐC|TRƯỞNG PHÒNG|PHÓ TRƯỞNG PHÒNG|CHỦ TỊCH|PHÓ CHỦ TỊCH|BỘ TRƯỞNG|THỨ TRƯỞNG|THỦ TRƯỞNG|HIỆU TRƯỞNG|CHÁNH VĂN PHÒNG)$)",RegexOptions.CultureInvariant|RegexOptions.IgnoreCase);
    private static bool Prose(Paragraph row,int minimumWords=6)
    {
        string text=Clean(row.Text).Trim(' ');
        if(!row.TextEditable || row.InTable || row.Outline is not {Scope:OutlineScope.PlainBody,HeadingLevel:null,Alignment:0 or 3} ||
            text.Length==0 || text.Length>4000 || text.Any(char.IsControl) || !char.IsUpper(text[0]) ||
            text[^1] is not ('.' or '?' or '!' or ';') || text==text.ToUpperInvariant() ||
            Regex.Matches(text,@"\p{L}[\p{L}\p{M}]*").Count<minimumWords)return false;
        return !Regex.IsMatch(text,@"^(?:Kính gửi|Căn cứ|Nơi nhận|Lưu\s*:|Phần\b|Chương\b|Mục\b|Tiểu mục\b|Điều\s+\d|[\p{L}][.)]|[IVXLCDM]+[.)])",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
    }
    public static IReadOnlyList<Paragraph> ObservationTargets(IReadOnlyList<Paragraph> paragraphs,IReadOnlyList<LayoutParagraph> layout,DocumentProfile profile)=>Identify(paragraphs,layout,profile,false);
    public static IReadOnlyList<Paragraph> RepairTargets(IReadOnlyList<Paragraph> paragraphs,IReadOnlyList<LayoutParagraph> layout,DocumentProfile profile)=>Identify(paragraphs,layout,profile,true);
    private static IReadOnlyList<Paragraph> Identify(IReadOnlyList<Paragraph> paragraphs,IReadOnlyList<LayoutParagraph> layout,DocumentProfile profile,bool requireOpeningFonts)
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
        if(following.Length<3)return [];
        bool greeting=Regex.IsMatch(Clean(following[0].Text).TrimStart(),@"^Kính gửi\s*:",RegexOptions.CultureInvariant|RegexOptions.IgnoreCase);
        int start=greeting?1:0;
        if(greeting)
        {
            if(!following[0].TextEditable || following[0].Outline is not {Scope:OutlineScope.PlainBody,HeadingLevel:null})return [];
        }
        else
        {
            // Without Kính gửi, two ordinary opening paragraphs must already
            // have justified, non-inset 1cm/1.27cm first-line layout proven by
            // XML and the bounded front-page layout. Font defects are allowed,
            // but unknown or emphasized anchors never grant later repairs.
            foreach(var opening in following.Take(2))
            {
                if(opening.Page!=1 || !Prose(opening,2) || opening.Outline is not {Alignment:3,BodyIndent:{Left:0,Right:0,Hanging:0} indent} ||
                    !BodyParagraphRules.ValidFirst(indent.First/20f) ||
                    !layout.Any(p=>p.Start==opening.Start && Clean(p.Text)==Clean(opening.Text) && p.Page==1 && !p.InTable && !(p.ParagraphCentered??p.Centered)))return [];
                if(requireOpeningFonts && (!opening.FormatObserved || opening.FormatEvidence is not {Fonts.Count:>0} evidence ||
                    evidence.Text!=Clean(opening.Text) || evidence.Fonts.Any(f=>!f.Known() || f.Bold!=0 || f.Italic!=0) ||
                    opening.ParagraphStyle is not {Known:true,Alignment:3} observedLayout || Math.Abs(observedLayout.Left)>=.05f ||
                    Math.Abs(observedLayout.Right)>=.05f || Math.Abs(observedLayout.First-indent.First/20f)>=.05f))return [];
            }
        }
        int end=Array.FindIndex(following,start,p=>Stop(Clean(p.Text)));
        if(end<2)return [];
        return following.Skip(start).Take(end-start).Where((p,i)=>Prose(p,!greeting && i<2?2:6)).ToArray();
    }
    public static IReadOnlyList<Issue> Check(IReadOnlyList<Paragraph> paragraphs,IReadOnlyList<LayoutParagraph> layout,DocumentProfile profile)
        =>CheckTargets(RepairTargets(paragraphs,layout,profile));
    internal static IReadOnlyList<Issue> CheckTargets(IReadOnlyList<Paragraph> targets)
    {
        var result=new List<Issue>();
        foreach(var row in targets)
        {
            // Emphasis is retained per glyph; it never becomes a font/color/size defect.
            var fonts=row.FormatEvidence?.Fonts;
            if(fonts is null || fonts.Count==0 || fonts.Any(f=>!f.Known()) || row.ParagraphStyle is not {Known:true,Spacing:{Known:true}})continue;
            var assessment=new FormatPolicy(13,14,false,null,PreserveEmphasis:true).Assess(row,allowSelectiveSizeRepair:true);
            if(assessment.State!=FormatState.Repairable)continue;
            string text=Clean(row.Text);
            result.Add(new(row.Start,text.Length,text,text,
                "Đoạn văn xuôi thường: Times New Roman, đen, cỡ 13–14; chỉ sửa cỡ sai về 13, giữ từng cỡ đúng, chữ, đậm/nghiêng, gạch chân và bố cục đoạn.",
                false,FixKind.BodyFormat,Citation:Citation,Title:"Phông/cỡ chữ nội dung chưa đúng",
                CharacterFormat:new(13,false,null,ComponentRole.Body,PreserveSize:true,RepairInvalidSizes:fonts.Any(f=>f.Size<13 || f.Size>14),PreserveEmphasis:true)));
        }
        return result;
    }
}
