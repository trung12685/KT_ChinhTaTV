using System.Globalization;

namespace ChinhTa.Core;

public sealed record IssueTooltipInfo(string Error,string Basis,string Action)
{
    public string Text=>$"Lỗi: {Error}\nCăn cứ: {Basis}\nCách xử lý: {Action}";
}

// Display summaries only: retain the original evidence in Detail; never change CanFix.
public static class IssueTooltipCatalog
{
    private const string Decree="Nghị định 30/2020/NĐ-CP, Phụ lục I, Phần I, ";
    private static string Brief(string value,int length=100)=>IssueReviewCatalog.Brief(value,length);
    private static string Number(double value)=>value.ToString("0.#",CultureInfo.InvariantCulture);
    private static bool Spacing(Issue issue)=>issue.Kind==FixKind.Text && issue.Original.Length>0 &&
        issue.Original.All(c=>c==' ') && issue.Replacement==" ";
    private static LineRole? Role(Issue issue)=>issue.LineFix?.Role ?? (issue.Title switch
    {
        string title when title.Contains("cơ quan ban hành",StringComparison.Ordinal)=>LineRole.Agency,
        string title when title.Contains("tiêu ngữ",StringComparison.Ordinal)=>LineRole.Motto,
        string title when title.Contains("trích yếu",StringComparison.Ordinal)=>LineRole.Subject,
        _=>null
    });
    public static IssueTooltipInfo Describe(Issue issue)=>new(IssueReviewCatalog.Describe(issue).Label,Basis(issue)+(issue.Citation?.Contains(DocumentTypeCatalog.UserNote,StringComparison.Ordinal)==true?" BCĐX là bổ sung theo yêu cầu người dùng.":""),Action(issue));
    private static string Basis(Issue issue)
    {
        if(issue.Kind==FixKind.OutlineOrder)
            return "Theo yêu cầu người dùng: các mục cùng cấp trong cùng mục cha đánh thứ tự liên tiếp; cần duyệt dẫn chiếu.";
        if(issue.Kind==FixKind.OutlineMarker)
            return "Theo yêu cầu người dùng: bỏ dấu - hoặc + nếu chỉ có một dấu cùng cấp trong cùng mục cha.";
        if(Spacing(issue))return "Quy tắc ứng dụng: giữa các từ dùng một dấu cách; giữ ô số/ngày để trống theo mẫu.";
        if(issue.AddsSpaceBeforeOpeningQuote || issue.AddsSpaceBeforeOpeningParenthesis)return "Quy tắc ứng dụng: có một dấu cách giữa từ và dấu mở ngoặc trong lời văn rõ.";
        if(issue.AddsSpaceAfterClosingQuote)return "Quy tắc ứng dụng: có một dấu cách giữa ngoặc kép đóng và từ tiếp theo trong câu chữ rõ.";
        if(issue.Kind==FixKind.QuoteSpace)return "Quy tắc ứng dụng: không cách sát trong ngoặc kép; có khoảng sau dấu đóng khi tiếp tục lời văn rõ.";
        if(issue.Kind==FixKind.RepeatedText)return "Quy tắc ứng dụng: phát hiện từ/cụm từ giống nhau liền nhau; cần duyệt ngữ cảnh.";
        if(issue.Kind is FixKind.PunctuationSpace or FixKind.ParenthesisSpace or FixKind.SentenceEndSpace or FixKind.ParagraphEndSpace or
            FixKind.ColonSpace or FixKind.SentenceInnerSpace or FixKind.SentenceAfterSpace)
            return issue.Kind switch
            {
                FixKind.ParagraphEndSpace=>"Quy tắc ứng dụng: bỏ dấu cách thừa sau dấu kết câu/chấm phẩy ở cuối đoạn chữ thuần.",
                FixKind.ParenthesisSpace when issue.AddsSpaceAfterClosingParenthesis=>"Quy tắc ứng dụng: có một dấu cách giữa ngoặc đóng và từ tiếp theo trong câu chữ rõ.",
                FixKind.ParenthesisSpace=>"Quy tắc ứng dụng: không có dấu cách sát bên trong ngoặc tròn chứa chữ.",
                FixKind.SentenceAfterSpace=>"Quy tắc ứng dụng: có một dấu cách giữa dấu kết câu và câu tiếp theo.",
                _=>"Quy tắc ứng dụng: không có dấu cách trước dấu câu; có khoảng cách sau dấu câu khi tiếp tục viết."
            };
        string citation=issue.Citation??"";
        bool state=citation.StartsWith("NĐ30",StringComparison.Ordinal) || citation.StartsWith("NĐ 30/2020/NĐ-CP",StringComparison.Ordinal);
        bool party=citation.StartsWith("05-HD/VPTW",StringComparison.Ordinal);
        if(state)
        {
            if(issue.Kind==FixKind.BodyGap)
                return Decree+"mục II.6, điểm e: khoảng cách giữa các đoạn văn tối thiểu 6 pt.";
            if(issue.Kind==FixKind.BodyLineSpacing)
                return Decree+"mục II.6, điểm e: giãn dòng từ dòng đơn đến 1,5 lines.";
            if(issue.Kind==FixKind.BodyParagraph)
                return Decree+"mục II.6, điểm e: nội dung căn đều hai lề; đầu dòng lùi 1 cm hoặc 1,27 cm.";
            if(issue.Kind==FixKind.BodyFormat)
                return Decree+"mục I.4 và II.6, điểm e: Times New Roman, đen, cỡ 13–14; kiểu đứng của phần lời văn xét riêng.";
            if(issue.Kind is FixKind.AgencyFormat or FixKind.AgencyText)
                return Decree+"mục I.4 và II.2, điểm b: Times New Roman, đen, in hoa, cỡ 12–13, đứng; tên cơ quan ban hành đậm, tên cơ quan chủ quản đứng.";
            if(issue.Kind==FixKind.SignatureFormat)
                return Decree+"mục I.4 và II.7, điểm đ: Times New Roman, đen, cỡ 13–14, đứng, đậm cho quyền hạn/chức vụ và họ tên người ký.";
            if(issue.Kind is FixKind.RecipientFormat or FixKind.RecipientText)
                return Decree+"mục I.4 và II.9, điểm d, ô 9b: Nơi nhận: cỡ 12, nghiêng, đậm; danh sách cỡ 11, đứng, có gạch đầu dòng, cuối dòng ;, dòng Lưu kết thúc bằng dấu chấm.";
            if(issue.Kind==FixKind.ShapeLine && Role(issue) is LineRole role)
                return Decree+(role switch
                {
                    LineRole.Agency=>"mục II.2, điểm b: dưới tên cơ quan có đường kẻ ngang, nét liền, dài 1/3–1/2 dòng chữ, đặt cân đối.",
                    LineRole.Motto=>"mục II.1, điểm b: dưới tiêu ngữ có đường kẻ ngang, nét liền, dài bằng dòng chữ.",
                    _=>"mục II.5, điểm b: dưới trích yếu có đường kẻ ngang, nét liền, dài 1/3–1/2 dòng chữ, đặt cân đối."
                });
            if(issue.Kind is FixKind.NationalHeading or FixKind.StateMotto)
            {
                bool motto=issue.Kind==FixKind.StateMotto;
                return Decree+"mục I.4 và II.1, điểm "+(motto?"b":"a")+": Times New Roman, đen; "+
                    (motto?"tiêu ngữ cỡ 13–14, đứng, đậm, căn giữa dưới quốc hiệu.":"quốc hiệu in hoa, cỡ 12–13, đứng, đậm.")+
                    (!motto && issue.Format?.Alignment is not null?" Căn giữa trong ô theo yêu cầu người dùng.":"");
            }
            if(issue.Kind==FixKind.AdministrativeNumber)
            {
                if(issue.Title==AdministrativeHeaderRules.TypeMismatchTitle)
                    return Decree+"mục II.3, điểm b và Phụ lục III.I: ký hiệu gồm viết tắt tên loại và cơ quan/chức danh ban hành.";
                if(issue.Original!=issue.Replacement && DocumentTypeCatalog.FindAbbreviation(issue.Original)?.Abbreviation==issue.Replacement)
                    return DocumentTypeCatalog.FindAbbreviation(issue.Original)?.Source==DocumentTypeSource.UserSupplement
                        ?"Danh mục người dùng bổ sung: giữ cách viết chữ viết tắt đã thống nhất."
                        :"Nghị định 30/2020/NĐ-CP, Phụ lục III, phần I: giữ đúng cách viết chữ hoa/thường của chữ viết tắt tên loại trong bảng.";
                return Decree+"mục II.3, điểm a–c: ghi Số:, số dưới 10 thêm 0; dùng / giữa số và ký hiệu, - giữa nhóm viết tắt, không cách chữ.";
            }
            if(issue.Kind==FixKind.AdministrativeDate)
                return Decree+"mục II.4, điểm b–c: sau địa danh có dấu phẩy; ghi đủ ngày, tháng, năm; ngày dưới 10 và tháng 1, 2 thêm 0.";
            if(issue.Kind==FixKind.AdministrativeFormat)
                return Decree+"mục I.4 và "+(issue.CharacterFormat?.Italic==true?"II.4, điểm c: Times New Roman, đen, cỡ 13–14, nghiêng.":
                    "II.3, điểm c: Times New Roman, đen, cỡ 13, đứng.");
            if(issue.Kind==FixKind.DocumentTypeText)
                return Decree+"mục II.5, điểm b: tên loại văn bản được trình bày bằng chữ in hoa.";
            if(issue.Kind==FixKind.DocumentHeadingFormat)
                return issue.CharacterFormat?.Role==ComponentRole.LetterSubject || citation==OfficialLetterRules.Citation?
                    Decree+"mục I.4 và II.5.b: trích yếu công văn sau V/v, cỡ 12–13, đứng, căn giữa dưới số/ký hiệu, cách 6 pt; không yêu cầu đường kẻ trích yếu.":
                    Decree+"mục I.4 và II.5, điểm b: Times New Roman, đen, cỡ 13–14, đứng, đậm; tên loại in hoa, trích yếu ngay dưới, căn giữa.";
            if(issue.Kind==FixKind.AdministrativePosition)
                return Decree+"mục II.3.c và II.4.c: số/ký hiệu căn giữa dưới tên cơ quan; địa danh/ngày tháng cùng dòng số, căn giữa dưới quốc hiệu/tiêu ngữ.";
            if(issue.Kind==FixKind.PageNumber)
                return Decree+"mục I.4 và I.7: Times New Roman đen, số Ả Rập từ 1, cỡ 13–14, đứng, giữa lề trên; ẩn trang đầu.";
            if(issue.Kind==FixKind.PageLayout)
                return Decree+"mục I.1–3: A4 210 × 297 mm; lề trên/dưới 20–25, trái 30–35, phải 15–20 mm; trang ngang có ngoại lệ bảng, biểu.";
            if(citation.Contains("chú thích 1",StringComparison.Ordinal))
                return "Nghị định 30/2020/NĐ-CP, Phụ lục I, Phần I, mục V, chú thích 1: cỡ chữ tăng/giảm thống nhất; mẫu quốc hiệu/tiêu ngữ 12/13 hoặc 13/14.";
            if(citation.Contains("II.1",StringComparison.Ordinal))
                return Decree+"mục I.4 và II.1: Times New Roman, đen, đứng, đậm; quốc hiệu in hoa cỡ 12–13, tiêu ngữ cỡ 13–14.";
        }
        if(party)
        {
            if(issue.Kind==FixKind.PageNumber)
                return Decree+"mục I.4 và I.7: Times New Roman đen, số Ả Rập từ 1, cỡ 13–14, đứng, giữa lề trên; ẩn trang đầu.";
            if(issue.Kind==FixKind.PageLayout)
                return "Hướng dẫn 05-HD/VPTW, Phần II.IV.3–4: thường dùng A4; lề trên/dưới/trái/phải 20/20/30/15 mm; có ngoại lệ giấy, trang ngang, hai mặt.";
            if(issue.Kind==FixKind.PartyHeading || citation.Contains("II.I.1",StringComparison.Ordinal))
                return "Hướng dẫn 05-HD/VPTW (27/5/2026), Phần II.I.1, II.IV.1 và Phụ lục 2, mục 1: ĐẢNG CỘNG SẢN VIỆT NAM; Times New Roman, đen, cỡ 15, đứng, đậm.";
        }
        return string.IsNullOrWhiteSpace(citation)?"Quy tắc kiểm tra của ứng dụng; mục này chưa có căn cứ pháp lý riêng.":
            Brief(citation,180)+" Nội dung cần đối chiếu trong Chi tiết lỗi.";
    }
    private static string Action(Issue issue)
    {
        if(!issue.CanFix)
            return issue.Kind==FixKind.AdministrativeNumber && issue.Title==AdministrativeHeaderRules.TypeMismatchTitle
                ?"Đối chiếu tên loại và ký hiệu đã đăng ký; sửa bên ghi sai trong Word rồi quét lại."
                :issue.Kind==FixKind.ShapeLine && issue.Message.Contains("Sửa nội dung/định dạng",StringComparison.Ordinal)
                ?"Sửa chữ/phông trong vùng trước, rồi quét lại đường kẻ.":"Đối chiếu trong Chi tiết lỗi và chỉnh thủ công trong Word.";
        if(issue.LineFix is ShapeLineFix line)
        {
            if(issue.Prerequisites is {Count:>0} required)return $"Sửa kèm {required.Count} mục liên quan, đo lại rồi kẻ/đặt lại đường ngang.";
            return (line.ShapeId is null?"Kẻ":"Đặt lại")+" đường ngang dài "+Number(line.Width)+" pt, căn theo dòng chữ; "+
                (line.Role==LineRole.Motto?"dài bằng tiêu ngữ.":"mức tự sửa theo cấu hình (mặc định 3/7).");
        }
        if(issue.NumberFix is not null)return PageNumberFix.Action;
        if(issue.PageFix is not null)return issue.PageFix.Suggestion;
        if(issue.CenterFix is not null)return "Căn giữa trong ô đã xác minh, bỏ thụt lề ngang; giữ chữ và khoảng cách dọc.";
        if(issue.IsStyleOnly)return issue.Suggestion;
        if(issue.Format?.RepairInvalidSizes==true)return issue.Suggestion;
        if(issue.Format is HeadingFormat format)
            return (issue.Original==issue.Replacement?"Đặt":"Sửa chữ và đặt")+" Times New Roman, đen, đậm, đứng, cỡ "+Number(format.Size)+
                (format.MaximumSize is float maximum?"–"+Number(maximum)+" (giữ cỡ hợp lệ)":"")+
                (format.Alignment is null?".":"; căn giữa trong ô.");
        if(Spacing(issue))return "Gộp phần đã đánh dấu thành một dấu cách.";
        return issue.Kind switch
        {
            FixKind.DocumentTypeText=>"Viết hoa tên loại thành “"+Brief(issue.Replacement,40)+"”; giữ trích yếu, phông và căn đoạn.",
            FixKind.RepeatedText=>"Bỏ phần lặp đã đánh dấu, giữ lần đầu; cần duyệt cả câu.",
            FixKind.OutlineOrder=>$"Đổi {Brief(issue.Original,20)} → {Brief(issue.Replacement,20)}; giữ nội dung, cần duyệt dẫn chiếu.",
            FixKind.OutlineMarker=>"Bỏ dấu đầu dòng đơn và dấu cách phân cách; giữ nội dung và thụt đầu dòng.",
            FixKind.QuoteSpace when issue.AddsSpaceBeforeOpeningQuote=>"Thêm một dấu cách trước ngoặc kép mở; giữ chữ, dấu ngoặc và phần chưa chọn.",
            FixKind.ParenthesisSpace when issue.AddsSpaceBeforeOpeningParenthesis=>"Thêm một dấu cách trước ngoặc tròn mở; giữ chữ, dấu ngoặc và phần chưa chọn.",
            FixKind.QuoteSpace when issue.AddsSpaceAfterClosingQuote=>"Thêm một dấu cách sau ngoặc kép đóng; giữ chữ, dấu ngoặc và mép trong chưa chọn.",
            FixKind.QuoteSpace=>"Bỏ dấu cách sát trong ngoặc kép đã chọn; giữ chữ, dấu ngoặc và khoảng cách chưa chọn.",
            FixKind.PunctuationSpace=>"Bỏ khoảng trước, chuẩn hóa khoảng sau dấu đã chọn; cần duyệt cả câu.",
            FixKind.ParenthesisSpace when issue.AddsSpaceAfterClosingParenthesis=>"Thêm một dấu cách sau ngoặc đóng; giữ chữ, ngoặc và khoảng trong chưa chọn.",
            FixKind.ParenthesisSpace=>"Bỏ dấu cách sát trong ngoặc đã chọn; giữ chữ và ngoặc.",
            FixKind.SentenceAfterSpace=>"Thêm một dấu cách sau dấu kết câu đã chọn.",
            FixKind.ColonSpace when issue.AddsSpaceAfterColon=>"Thêm một dấu cách sau dấu hai chấm; giữ chữ, dấu câu và khoảng trước chưa chọn.",
            FixKind.ParagraphEndSpace=>"Bỏ dấu cách cuối đoạn; giữ chữ, dấu câu, khoảng trước chưa chọn và dấu kết đoạn/ô.",
            FixKind.SentenceEndSpace or FixKind.SentenceInnerSpace or FixKind.ColonSpace=>"Bỏ dấu cách trước dấu câu đã chọn; giữ chữ và dấu câu.",
            _=>issue.Replacement.Length==0?"Xóa phần đã đánh dấu; cần duyệt ngữ cảnh.":"Thay phần đã đánh dấu bằng “"+Brief(issue.Replacement,80)+"”; cần duyệt ngữ cảnh."
        };
    }
}
