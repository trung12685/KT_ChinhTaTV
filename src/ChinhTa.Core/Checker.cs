using System.Text.RegularExpressions;

namespace ChinhTa.Core;

public record Paragraph(int Start, string Text, bool Bold = false, float Size = 13,
    int Alignment = 1, string Font = "Times New Roman", bool Italic = false,
    int Color = 0, bool InTable = false, int Page = 1, bool FormatObserved = false, bool FormatMixed = false, bool TextEditable = true, bool BoldMixed = false, OutlineParagraphInfo? Outline = null, bool FormatEditable = false,FormatEvidence? FormatEvidence=null,ObservedParagraphStyle? ParagraphStyle=null,BodyFlow? Flow=null);
public enum FixKind { Text, NationalHeading, StateMotto, PartyHeading, Advisory, PageLayout, ShapeLine, AdministrativeNumber, AdministrativeDate, AdministrativeFormat, AdministrativePosition, DocumentHeadingFormat, RepeatedText, PunctuationSpace, ParenthesisSpace, SentenceEndSpace, ColonSpace, SentenceInnerSpace, SentenceAfterSpace, OutlineOrder, OutlineMarker, AgencyFormat, AgencyText, RecipientFormat, RecipientText, DocumentTypeText, ParagraphEndSpace, BodyFormat, BodyParagraph, BodyGap, BodyLineSpacing, QuoteSpace, SignatureFormat, PageNumber }
public record HeadingFormat(float Size, int? Alignment = null,float? MaximumSize=null,bool RepairInvalidSizes=false)
{
    public bool Matches(string font,float size,int bold,int italic,int color,int? alignment)=>
        font.Equals("Times New Roman",StringComparison.OrdinalIgnoreCase) && (!RepairInvalidSizes || MaximumSize is not null) && float.IsFinite(Size) && Size>0 &&
        float.IsFinite(size) && (MaximumSize is float maximum?size>=Size && size<=maximum:Math.Abs(size-Size)<.01f) && bold==-1 && italic==0 &&
        color is 0 or -16777216 && (Alignment is null || alignment==Alignment);
}
public enum ComponentRole { AgencyParent, Agency, RecipientLabel, RecipientItem, Body, SignatureAuthority, SignatureName }
public record CharacterFormat(float Size,bool Italic,bool? Bold=null,ComponentRole? Role=null,bool PreserveSize=false,bool RepairInvalidSizes=false,bool PreserveEmphasis=false)
{
    public (float Minimum,float Maximum) SizeRange=>Role switch
    {
        ComponentRole.Agency or ComponentRole.AgencyParent=>(12,13),
        ComponentRole.Body or ComponentRole.SignatureAuthority or ComponentRole.SignatureName=>(13,14),
        ComponentRole.RecipientLabel=>(12,12),ComponentRole.RecipientItem=>(11,11),
        _=>Bold==true || Italic?(13,14):(13,13)
    };
    public bool Valid => (!PreserveEmphasis || Role==ComponentRole.Body && Bold is null && !Italic) && (!RepairInvalidSizes || PreserveSize) && float.IsFinite(Size) && (!PreserveSize || SizeRange.Minimum<SizeRange.Maximum && Size==SizeRange.Minimum) && (Role is not null ? Role switch
    {
        ComponentRole.Body=>Size>=13 && Size<=14 && !Italic && (PreserveEmphasis?Bold is null:Bold==false),
        ComponentRole.SignatureAuthority or ComponentRole.SignatureName=>Size>=13 && Size<=14 && !Italic && Bold==true,
        ComponentRole.AgencyParent=>Size>=12 && Size<=13 && !Italic && Bold==false,
        ComponentRole.Agency=>Size>=12 && Size<=13 && !Italic && Bold==true,
        ComponentRole.RecipientLabel=>Size==12 && Italic && Bold==true,
        ComponentRole.RecipientItem=>Size==11 && !Italic && Bold is null,
        _=>false
    } : Bold is null?(Italic?Size>=13 && Size<=14:Math.Abs(Size-13)<.01f):Bold==true && !Italic && Size>=13 && Size<=14);
    public bool Matches(string font,float size,int italic,int color,int? bold=null)=>
        font.Equals("Times New Roman",StringComparison.OrdinalIgnoreCase) && float.IsFinite(size) &&
        (PreserveSize?size>=SizeRange.Minimum && size<=SizeRange.Maximum:Math.Abs(size-Size)<.01f) && color is 0 or -16777216 &&
        (PreserveEmphasis?italic is 0 or -1 && bold is 0 or -1:italic==(Italic?-1:0) && (Bold is null || bold==(Bold==true?-1:0)));
}
public sealed record OutlineEditContext(int? ParentStart,int GroupStart,string Label,string Delimiter);
public record Issue(int Start, int Length, string Original, string Replacement,
    string Message, bool BatchSafe, FixKind Kind = FixKind.Text,
    bool CanFix = true, string? Citation = null, HeadingFormat? Format = null, string? Title = null, ShapeLineFix? LineFix = null,
    CharacterFormat? CharacterFormat = null, AdministrativeCenterFix? CenterFix = null,IReadOnlyList<Issue>? Prerequisites=null,OutlineEditContext? OutlineContext=null,BodyParagraphFix? ParagraphFix=null,BodySpacingFix? SpacingFix=null,PageLayoutFix? PageFix=null,PageNumberFix? NumberFix=null)
{
    public bool IsStyleOnly=>CharacterFormat is not null || ParagraphFix is not null || SpacingFix is not null;
    public string DisplayText => Title ?? Original;
    public bool AddsSpaceAfterColon => Kind==FixKind.ColonSpace && Original==":" && Replacement==": ";
    public bool AddsSpaceBeforeOpeningParenthesis=>Kind==FixKind.ParenthesisSpace && Original=="(" && Replacement==" (";
    public bool AddsSpaceBeforeOpeningQuote=>Kind==FixKind.QuoteSpace && Original is "“" or "\"" && Replacement==" "+Original;
    public bool AddsSpaceAfterClosingQuote => Kind==FixKind.QuoteSpace && Original is "”" or "\"" && Replacement==Original+" ";
    public bool AddsSpaceAfterClosingParenthesis => Kind==FixKind.ParenthesisSpace && Original==")" && Replacement==") ";
    public string Suggestion => NumberFix is not null?NumberFix.Suggestion:PageFix is not null?PageFix.Suggestion:!CanFix ? "Kiểm tra thủ công; không tự sửa."
        : LineFix is not null ? Prerequisites is {Count:>0} required?$"Sửa kèm {required.Count} mục liên quan, đo lại rồi tạo/đặt lại đường kẻ.":"Tạo/đặt lại Shape Line theo số đo và cấu hình đã chọn."
        : CenterFix is not null ? "Căn giữa trong ô đã xác minh, bỏ thụt lề ngang; giữ nguyên chữ và khoảng cách dọc."
        : Format is {RepairInvalidSizes:true,MaximumSize:float headingMaximum} selectiveHeading ? $"Times New Roman, đen, đứng, đậm; chỉ đưa cỡ ngoài {selectiveHeading.Size:0.#}–{headingMaximum:0.#} về {selectiveHeading.Size:0.#}; giữ cỡ đúng và chữ."+(selectiveHeading.Alignment is null?"":" Căn giữa trong ô.")
        : SpacingFix is not null ? SpacingFix.Suggestion
        : ParagraphFix is not null ? ParagraphFix.Suggestion
        : CharacterFormat is {PreserveEmphasis:true} retained ? "Times New Roman, đen; "+
            (retained.RepairInvalidSizes?$"chỉ đưa cỡ ngoài 13–14 về {retained.Size:0.#}, giữ cỡ đúng":retained.PreserveSize?"giữ từng cỡ 13–14 đang đúng":$"cỡ {retained.Size:0.#}")+
            "; giữ chữ, đậm/nghiêng và gạch chân."
        : CharacterFormat?.RepairInvalidSizes==true ? $"Times New Roman, đen, {(CharacterFormat.Italic?"nghiêng":"đứng")}{(CharacterFormat.Bold==true?", đậm":CharacterFormat.Bold==false?", không đậm":"")}; chỉ đưa cỡ ngoài {CharacterFormat.SizeRange.Minimum:0.#}–{CharacterFormat.SizeRange.Maximum:0.#} về {CharacterFormat.Size:0.#}; giữ từng cỡ đúng và chữ."
        : CharacterFormat is not null ? $"Times New Roman, màu đen, cỡ {(CharacterFormat.PreserveSize?$"{CharacterFormat.SizeRange.Minimum:0.#}–{CharacterFormat.SizeRange.Maximum:0.#} (giữ cỡ hợp lệ)":$"{CharacterFormat.Size:0.#}")}, chữ {(CharacterFormat.Italic?"nghiêng":"đứng")}{(CharacterFormat.Bold==true?", đậm":CharacterFormat.Bold==false?", không đậm":"")}; giữ nguyên chữ."
        : Kind==FixKind.DocumentTypeText ? "Viết hoa tên loại thành “"+Replacement+"”; giữ nội dung trích yếu, phông và căn đoạn."
        : Kind==FixKind.OutlineOrder ? $"Đổi ký hiệu mục {Original} → {Replacement}; giữ nội dung, cần duyệt thứ tự và dẫn chiếu."
        : Kind==FixKind.OutlineMarker ? "Bỏ dấu đầu dòng và dấu cách phân cách; giữ nội dung và thụt đầu dòng, cần duyệt mục cha."
        : AddsSpaceBeforeOpeningQuote ? "Thêm một dấu cách trước ngoặc kép mở; giữ chữ, dấu ngoặc và phần chưa chọn."
        : AddsSpaceBeforeOpeningParenthesis ? "Thêm một dấu cách trước ngoặc tròn mở; giữ chữ, dấu ngoặc và phần chưa chọn."
        : AddsSpaceAfterClosingQuote ? "Thêm một dấu cách sau ngoặc kép đóng; giữ chữ, dấu ngoặc và khoảng trong chưa chọn, cần duyệt cả câu."
        : Kind==FixKind.QuoteSpace ? "Xóa dấu cách sát trong ngoặc kép; giữ chữ, dấu ngoặc và khoảng cách chưa chọn, cần duyệt cả câu."
        : Kind==FixKind.SentenceAfterSpace ? "Thêm một dấu cách sau dấu kết câu; giữ chữ, dấu câu và khoảng trước, cần duyệt cả câu."
        : Kind==FixKind.SentenceInnerSpace ? "Xóa dấu cách trước dấu kết câu giữa đoạn; giữ chữ và dấu câu, cần duyệt cả câu."
        : AddsSpaceAfterColon ? "Thêm một dấu cách sau dấu hai chấm; giữ chữ, dấu câu và khoảng trước, cần duyệt cả câu."
        : Kind==FixKind.ColonSpace ? "Xóa dấu cách trước dấu hai chấm; giữ chữ và dấu câu, cần duyệt cả câu."
        : Kind==FixKind.SentenceEndSpace ? "Xóa dấu cách trước dấu kết câu cuối đoạn; giữ chữ và dấu câu, cần duyệt cả câu."
        : Kind==FixKind.ParagraphEndSpace ? "Xóa dấu cách cuối đoạn sau dấu kết câu/chấm phẩy; giữ chữ, dấu câu và dấu kết đoạn/ô, cần duyệt cả câu."
        : AddsSpaceAfterClosingParenthesis ? "Thêm một dấu cách sau dấu đóng ngoặc; giữ chữ, ngoặc và khoảng trong chưa chọn, cần duyệt cả câu."
        : Kind==FixKind.ParenthesisSpace ? "Xóa dấu cách sát bên trong ngoặc; giữ chữ và dấu ngoặc, cần duyệt cả câu."
        : Kind==FixKind.RepeatedText ? "Xóa phần lặp đã đánh dấu, giữ lần đầu; cần duyệt cả câu."
        : Format is null ? Replacement
        : Original == Replacement ? "Sửa định dạng theo mô tả." : Replacement + " — sửa định dạng theo mô tả.";
}

public static class Checker
{
    public const string Heading = "CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM";
    public static IReadOnlyList<Issue> Check(IEnumerable<Paragraph> paragraphs, DocumentProfile profile = DocumentProfile.State)
    {
        var result = new List<Issue>();
        var items = paragraphs.ToArray();
        var outline=OutlineRules.Check(items);
        var outlineOwners=outline.Where(f=>f.Issue.CanFix).ToLookup(f=>f.ParagraphStart);
        var administrative=AdministrativeHeaderRules.Check(items,profile);
        var administrativeStarts=AdministrativeHeaderRules.Identify(items,profile).Select(p=>p.Start).ToHashSet();
        var protectedAdministrative=items.Where(p=>administrativeStarts.Contains(p.Start) && (!p.TextEditable || p.Text.Contains('\t')))
            .Select(p=>p.Start).ToHashSet();
        foreach (var p in items)
        {
            var textIssues=new List<Issue>();
            if (!(p.Page == 1 && HeadingRules.IsCandidate(p.Text, profile)))
            foreach (Match m in Regex.Matches(p.Text, @"(?<=\S) {2,}(?=\S)"))
                if(!protectedAdministrative.Contains(p.Start) && !SpacingRules.IsBlankField(p.Text,m.Index,m.Length,administrativeStarts.Contains(p.Start)) && !administrative.Any(i=>i.CanFix && i.CharacterFormat is null &&
                    i.Start<p.Start+m.Index+m.Length && (long)i.Start+i.Length>p.Start+m.Index)) textIssues.Add(new(p.Start + m.Index, m.Length, m.Value, " ",
                    "Khoảng trắng liên tiếp; kiểm tra trước nếu dùng để dàn trang.", false));
            foreach (Match m in Regex.Matches(p.Text, @"\bcon\s+(đo)\b", RegexOptions.IgnoreCase))
            {
                var word = m.Groups[1];
                textIssues.Add(new(p.Start + word.Index, word.Length, word.Value,
                    char.IsUpper(word.Value[0]) ? "Đò" : "đò",
                    "Có thể là ‘con đò’. Đây là quy tắc minh họa, cần người dùng xác nhận.", false));
            }
            var punctuation=PunctuationSpaceRules.Check(p).Concat(ParenthesisSpaceRules.Check(p)).Concat(QuoteSpaceRules.Check(p)).Concat(SentenceEndSpaceRules.Check(p)).Concat(ParagraphEndSpaceRules.Check(p)).Concat(ColonSpaceRules.Check(p)).Concat(SentenceInnerSpaceRules.Check(p)).Concat(SentenceAfterSpaceRules.Check(p)).ToArray();
            // A punctuation span owns its adjacent spaces; do not offer an overlapping
            // generic spacing write for the same characters in a multi-selection.
            textIssues.RemoveAll(candidate=>punctuation.Any(r=>r.CanFix &&
                candidate.Start<(long)r.Start+r.Length && (long)candidate.Start+candidate.Length>r.Start));
            textIssues.AddRange(punctuation);
            var repetitions=RepeatedTextRules.Check(p);
            // One deletion may contain old spacing/demo candidates. Do not propose
            // overlapping writes; independently editable text in the retained copy stays.
            textIssues.RemoveAll(candidate=>repetitions.Any(r=>r.CanFix &&
                candidate.Start<(long)r.Start+r.Length && (long)candidate.Start+candidate.Length>r.Start));
            textIssues.AddRange(repetitions);
            textIssues.RemoveAll(candidate=>outlineOwners[p.Start].Any(f=>
                candidate.Start<(long)f.Issue.Start+f.Issue.Length && (long)candidate.Start+candidate.Length>f.Issue.Start));
            if(p.TextEditable && !protectedAdministrative.Contains(p.Start)) result.AddRange(textIssues);
            else if(textIssues.Count>0)
            {
                // A Word field/object may occupy CPs absent from visible text. Anchor a
                // manual notice to the real paragraph start, never a guessed substring.
                string text=p.Text.TrimEnd('\r','\a','\n');
                result.Add(new(p.Start,text.Length,text,text,
                    "Có gợi ý sửa chữ/khoảng trắng trong vùng có cấu trúc hoặc chưa đối chiếu được XML. Kiểm tra thủ công để giữ trường, liên kết và đối tượng.",
                    false,FixKind.Advisory,CanFix:false,Title:"Kiểm tra chữ trong vùng chưa cho phép sửa"));
            }
            if (profile == DocumentProfile.Personal)
            {
                foreach (Match tabs in Regex.Matches(p.Text, @"\t{2,}"))
                    result.Add(new(p.Start + tabs.Index, tabs.Length, tabs.Value, tabs.Value,
                        "Nhiều tab liên tiếp; có thể dùng để dàn bảng. Kiểm tra thủ công, không tự xóa tab.",
                        false, FixKind.Advisory, CanFix: false, Title: "Tab liên tiếp"));
                if (p.Alignment == 1 && Regex.IsMatch(p.Text, @"^[ \t]+\S"))
                    result.Add(new(p.Start, p.Text.TrimEnd('\r', '\a').Length,
                        p.Text.TrimEnd('\r', '\a'), p.Text.TrimEnd('\r', '\a'),
                        "Đoạn căn giữa có tab/khoảng trắng đầu dòng; kiểm tra xem có làm lệch bố cục không.",
                        false, FixKind.Advisory, CanFix: false, Title: "Kiểm tra đoạn căn giữa"));
            }
            result.AddRange(HeadingRules.Check(p, profile));
        }
        result.AddRange(HeadingRules.CheckConsistency(items, profile));
        result.AddRange(administrative);
        result.AddRange(outline.Select(f=>f.Issue));
        return result.OrderBy(x => x.Start).ToArray();
    }
}

