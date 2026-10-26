using System.Text.RegularExpressions;

namespace ChinhTa.Core;

public enum IssueReviewSection { RequiredFormat, ReviewSuggestion, ManualWarning }
public sealed record IssueReviewInfo(IssueReviewSection Section,string FamilyKey,string Family,int Order,string Label);

// Presentation only: classifications never grant CanFix or replace current-plan checks.
public static class IssueReviewCatalog
{
    public static string SectionTitle(IssueReviewSection section)=>section switch
    {
        IssueReviewSection.RequiredFormat=>"BẮT BUỘC SỬA",
        IssueReviewSection.ReviewSuggestion=>"ĐỀ XUẤT SỬA — CẦN DUYỆT",
        _=>"CHƯA TỰ SỬA — CHỈ CẢNH BÁO"
    };
    public static string SectionDetail(IssueReviewSection section)=>section switch
    {
        IssueReviewSection.RequiredFormat=>"Yêu cầu thể thức hoặc thứ tự/dấu đầu dòng theo lựa chọn người dùng, đã đủ điều kiện sửa. Bạn vẫn duyệt trước khi áp dụng; mục này không chặn lưu tài liệu.",
        IssueReviewSection.ReviewSuggestion=>"Đã hỗ trợ đề xuất sửa — cần duyệt. Gợi ý chữ, dấu câu, bố cục hoặc số đo cần xem lại theo ngữ cảnh.",
        _=>"Chưa tự sửa — chỉ cảnh báo. Không tích để sửa tự động; xem chi tiết và đối chiếu trong Word."
    };
    public static IssueReviewInfo Describe(Issue issue)
    {
        bool space=issue.Kind==FixKind.Text && issue.Original.Length>0 && issue.Original.All(c=>c==' ') && issue.Replacement==" ";
        var (key,family,order,label)=issue.Kind switch
        {
            FixKind.Text when space=>("spacing","Khoảng trắng thừa",10,"Khoảng trắng thừa"),
            FixKind.ParagraphEndSpace=>("spacing","Khoảng trắng thừa",10,issue.CanFix?"Thừa khoảng trắng cuối đoạn":issue.Title??"Kiểm tra khoảng trắng cuối đoạn"),
            FixKind.Text=>("spelling","Gợi ý chính tả",90,"Gợi ý chính tả: "+Brief(issue.Original,20)+" → "+Brief(issue.Replacement,20)),
            FixKind.NationalHeading=>("national","Quốc hiệu",20,"Quốc hiệu chưa đúng thể thức"),
            FixKind.StateMotto=>("motto","Tiêu ngữ",21,"Tiêu ngữ chưa đúng thể thức"),
            FixKind.PartyHeading=>("party","Tiêu đề Đảng",22,"Tiêu đề Đảng chưa đúng thể thức"),
            FixKind.AdministrativeNumber=>("number","Số, ký hiệu",30,issue.Title??"Cách ghi số, ký hiệu"),
            FixKind.AdministrativeDate=>("date","Địa danh, ngày tháng",31,issue.Title??"Cách ghi địa danh, ngày tháng"),
            FixKind.AdministrativeFormat=>("admin-font","Phông số và ngày tháng",32,issue.Title??"Phông số/ngày tháng chưa đúng"),
            FixKind.DocumentHeadingFormat or FixKind.DocumentTypeText=>("document-font","Tên loại và trích yếu",33,issue.Title??"Tên loại/trích yếu chưa đúng"),
            FixKind.BodyGap=>("body-gap","Khoảng cách giữa các đoạn",37,issue.Title??"Khoảng cách đoạn thiếu"),
            FixKind.BodyLineSpacing=>("body-lines","Giãn dòng nội dung",38,issue.Title??"Giãn dòng nội dung chưa đúng"),
            FixKind.BodyParagraph=>("body-paragraph","Căn/thụt đầu dòng nội dung",36,issue.Title??"Căn/thụt đầu dòng chưa đúng"),
            FixKind.BodyFormat=>("body-font","Phông/cỡ chữ nội dung",35,issue.Title??"Phông/cỡ chữ nội dung chưa đúng"),
            FixKind.AdministrativePosition=>("position","Vị trí số và ngày tháng",34,issue.Title??"Kiểm tra vị trí số/ngày tháng"),
            FixKind.ShapeLine=>("lines","Đường kẻ",40,issue.Title??"Kiểm tra đường kẻ"),
            FixKind.AgencyFormat or FixKind.AgencyText=>("agency-font","Tên cơ quan",23,issue.Title??"Tên cơ quan chưa đúng thể thức"),
            FixKind.RecipientFormat or FixKind.RecipientText=>("recipients","Nơi nhận",47,issue.Title??"Nơi nhận chưa đúng thể thức"),
            FixKind.SignatureFormat=>("signature-font","Chữ ký",48,issue.Title??"Định dạng khối ký chưa đúng"),
            FixKind.OutlineOrder=>("outline-order","Thứ tự các mục",45,issue.Title??"Kiểm tra thứ tự mục"),
            FixKind.OutlineMarker=>("outline-marker","Dấu đầu dòng đứng một mình",46,issue.Title??"Kiểm tra dấu đầu dòng"),
            FixKind.RepeatedText=>("repeat","Từ, cụm từ lặp",50,"Lặp từ/cụm từ: "+Brief(issue.Original.Trim(),34)),
            FixKind.PunctuationSpace=>("comma","Dấu phẩy, chấm phẩy",60,issue.Title??"Khoảng trắng quanh dấu câu"),
            FixKind.ParenthesisSpace=>("parenthesis","Ngoặc tròn",61,!issue.CanFix?issue.Title??"Kiểm tra khoảng trắng quanh ngoặc":
                issue.AddsSpaceBeforeOpeningParenthesis?"Thiếu khoảng trắng trước dấu mở ngoặc":issue.AddsSpaceAfterClosingParenthesis?"Thiếu khoảng trắng sau dấu đóng ngoặc":"Khoảng trắng sát trong ngoặc tròn"),
            FixKind.SentenceEndSpace=>("sentence-end","Dấu kết câu cuối đoạn",62,"Thừa khoảng trước dấu kết câu cuối đoạn"),
            FixKind.ColonSpace=>("colon","Dấu hai chấm",63,!issue.CanFix?issue.Title??"Kiểm tra khoảng trắng quanh dấu hai chấm":
                issue.AddsSpaceAfterColon?"Thiếu khoảng trắng sau dấu hai chấm":"Thừa khoảng trước dấu hai chấm"),
            FixKind.SentenceInnerSpace=>("sentence-before","Khoảng trước dấu kết câu",64,"Thừa khoảng trước dấu kết câu"),
            FixKind.SentenceAfterSpace=>("sentence-after","Khoảng sau dấu kết câu",65,"Thiếu khoảng sau dấu kết câu"),
            FixKind.QuoteSpace=>("quotes","Ngoặc kép",66,issue.Title??"Khoảng trắng quanh ngoặc kép"),
            FixKind.PageNumber=>("page-number","Số trang",71,issue.Title??"Số trang chưa đúng thể thức"),
            FixKind.PageLayout=>("page","Khổ giấy và lề",70,issue.Title??"Kiểm tra khổ giấy/lề"),
            _=>("notice:"+(issue.Title??"other"),Brief(issue.Title??"Kiểm tra thủ công",60),80,issue.Title??"Kiểm tra thủ công")
        };
        bool cited=!string.IsNullOrWhiteSpace(issue.Citation);
        // Geometry/centering based on user tolerances remains reviewable. Only a
        // verified missing Line joins the required-format section here.
        bool required=issue.Kind is FixKind.OutlineOrder or FixKind.OutlineMarker || cited && (issue.Kind==FixKind.PageNumber && issue.NumberFix is not null || issue.Kind==FixKind.PageLayout && issue.PageFix is not null ||
            issue.Kind is FixKind.AdministrativeNumber or FixKind.AdministrativeDate or FixKind.AdministrativeFormat or FixKind.SignatureFormat or FixKind.BodyFormat or FixKind.BodyParagraph or FixKind.BodyGap or FixKind.BodyLineSpacing or FixKind.DocumentHeadingFormat or FixKind.DocumentTypeText or FixKind.AgencyFormat or FixKind.AgencyText or FixKind.RecipientFormat or FixKind.RecipientText ||
            issue.Kind is FixKind.NationalHeading or FixKind.StateMotto or FixKind.PartyHeading &&
                (issue.Format is {Alignment:null} || issue.Original!=issue.Replacement) ||
            issue.Kind==FixKind.ShapeLine && issue.LineFix is {ShapeId:null});
        var section=!issue.CanFix?IssueReviewSection.ManualWarning:required?IssueReviewSection.RequiredFormat:IssueReviewSection.ReviewSuggestion;
        return new(section,key,family,order,Brief(label,88));
    }
    public static string Brief(string value,int length)
    {
        string clean=Regex.Replace(value,@"[\s\p{Cc}]+"," ").Trim();
        return clean.Length>length?clean[..Math.Max(0,length-1)]+"…":clean;
    }
}
