using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ChinhTa.Core;

public enum CapabilityMode { Reviewable,Manual,Demonstration }
public sealed record ReportCapability(string Id,string Name,CapabilityMode Mode,string Scope,string Detail);
public sealed record ReportMilestone(string Version,string Detail);
public sealed record ReportEvidence(string Version,int CoreChecks,int WpfChecks,string Commit,string RunUrl)
{
    public bool Matches(string version)=>Version==version && CoreChecks>0 && WpfChecks>0 &&
        Regex.IsMatch(Commit??"",@"\A[0-9a-f]{40}\z",RegexOptions.CultureInvariant) &&
        Regex.IsMatch(RunUrl??"",@"\Ahttps://github\.com/trung12685/KT_ChinhTaTV/actions/runs/[0-9]+\z",RegexOptions.CultureInvariant);
    public static ReportEvidence? Parse(string json,string version)
    {
        try
        {
            var value=JsonSerializer.Deserialize<ReportEvidence>(json);
            return value is not null && value.Matches(version)?value:null;
        }
        catch(JsonException){return null;}
    }
}

// A versioned capability/roadmap report. It never consumes an open document or
// claims that a feature count is a count of repaired errors in a user's file.
public static class DevelopmentReport
{
    public static IReadOnlyList<ReportCapability> Capabilities { get; }=Array.AsReadOnly(new ReportCapability[]
    {
        new("C01","Khoảng trắng thừa",CapabilityMode.Reviewable,"Cả ba chế độ",
            "Gộp dấu cách thường liên tiếp giữa chữ; giữ ô số/ngày trống theo mẫu nhận diện. Không tự xóa tab hoặc thụt đầu dòng."),
        new("C02","Quốc hiệu",CapabilityMode.Reviewable,"Nhà nước, trang đầu",
            "Chuẩn hóa chữ/khoảng trắng của dòng nhận diện; Times New Roman, đen, đậm, đứng, cỡ 12–13. Không tự thêm dòng bị thiếu hoặc đoán dòng sai chữ chưa nhận ra."),
        new("C03","Tiêu ngữ",CapabilityMode.Reviewable,"Nhà nước, trang đầu",
            "Chuẩn hóa dòng nhận diện; Times New Roman, đen, đậm, đứng, cỡ 13–14. Đường kẻ được kiểm riêng."),
        new("C04","Tiêu đề Đảng",CapabilityMode.Reviewable,"Đảng, trang đầu",
            "Chuẩn hóa dòng ĐẢNG CỘNG SẢN VIỆT NAM đã nhận diện và phông đen, đậm, đứng, cỡ 15. Chưa tự đặt vị trí toàn trang."),
        new("C05","Cách ghi số/ký hiệu",CapabilityMode.Reviewable,"Đầu văn bản Nhà nước nhận diện được",
            "Nhãn Số:, dấu /, dấu nối, khoảng trắng; thêm 0 cho số 1–9. Không tự cấp số, đoán mã hoặc đổi tên/mã chính thức."),
        new("C06","Cách ghi địa danh/ngày tháng",CapabilityMode.Reviewable,"Đầu Nhà nước, mẫu rõ và ngày lịch hợp lệ",
            "Dấu phẩy, nhãn ngày/tháng/năm và khoảng trắng; thêm 0 trước ngày 1–9, tháng 1–2. Giữ trường chưa điền, không chọn ngày hay địa danh thay người dùng."),
        new("C07","Phông dòng số/ký hiệu",CapabilityMode.Reviewable,"Dòng được nhận diện và đủ bằng chứng chữ thuần",
            "Times New Roman, đen, cỡ 13, đứng; giữ chữ, ô trống, đậm/gạch chân và căn đoạn."),
        new("C08","Phông địa danh/ngày tháng",CapabilityMode.Reviewable,"Dòng được nhận diện và định dạng đủ rõ",
            "Times New Roman, đen, nghiêng, cỡ 13–14; giữ cỡ hợp lệ, đậm/gạch chân và chữ. Căn giữa là chức năng riêng."),
        new("C09","Phông tên loại văn bản",CapabilityMode.Reviewable,"Đầu Nhà nước, khối tên loại/trích yếu chắc chắn",
            "Times New Roman, đen, đứng, đậm, cỡ 13–14. Hiện dùng 13 tên loại; chưa áp cho Công văn/V/v. Không tự đổi nội dung sang chữ hoa."),
        new("C10","Phông trích yếu",CapabilityMode.Reviewable,"Khối 1–4 đoạn dưới tên loại, có ranh giới rõ",
            "Times New Roman, đen, đứng, đậm, cỡ 13–14; giữ nguyên văn, cỡ hợp lệ, gạch chân/căn đoạn. Chưa tự viết thường trích yếu toàn chữ hoa."),
        new("C11","Căn giữa số/ngày",CapabilityMode.Reviewable,"Mốc chữ và cùng khung ô được xác minh trên XPS",
            "Căn giữa và bỏ ba thụt lề ngang; giữ chữ/khoảng cách dọc. Chưa tự làm hai dòng cùng hàng hoặc dịch vị trí tuyệt đối."),
        new("C12","Ba đường kẻ Nhà nước",CapabilityMode.Reviewable,"Cơ quan, tiêu ngữ, trích yếu nhận diện và đo được",
            "Tạo Line thiếu hoặc đặt lại Line của ứng dụng. Đường cơ quan/trích yếu chấp nhận 1/3–1/2 dòng tham chiếu, tự tạo 3/7; tiêu ngữ theo bề rộng chữ. Kiểm nét XPS thật, không chỉ đếm Shape."),
        new("C13","Từ/cụm từ lặp liên tiếp",CapabilityMode.Reviewable,"Cùng đoạn, dấu cách thường, đúng chữ hoa/thường",
            "Cụm 2–8 tiếng: đề xuất xóa bản lặp, giữ lần đầu sau khi duyệt. Một tiếng như đều đều/nhà nhà, toàn chữ hoa hoặc dòng số/ngày giữ thủ công. Chưa suy nghĩa câu."),
        new("M01","Khổ giấy và lề",CapabilityMode.Manual,"Nhà nước/Đảng",
            "Cảnh báo A4/lề, trang ngang/lề đối xứng và ngoại lệ; chưa tự đổi thiết lập trang."),
        new("M02","Khác hàng hoặc vị trí dọc số/ngày",CapabilityMode.Manual,"Khi đo được mốc XPS",
            "Chưa tự dịch theo chiều dọc hoặc làm số và ngày cùng hàng."),
        new("M03","Cách viết hoa tên loại/trích yếu",CapabilityMode.Manual,"Khối đã nhận diện",
            "Tên loại chưa viết hoa/trích yếu toàn chữ hoa cần duyệt. Phần phông có thể có gợi ý riêng đủ điều kiện."),
        new("M04","Giá trị số/ngày và mã/tên chính thức",CapabilityMode.Manual,"Đầu văn bản",
            "Số bằng 0, ngày sai lịch, năm chưa rõ, mẫu/trùng mốc, mã cơ quan hoặc địa danh cần đối chiếu; không tự đoán giá trị mới."),
        new("M05","Thống nhất cỡ quốc hiệu/tiêu ngữ",CapabilityMode.Manual,"Nhà nước",
            "Cảnh báo cặp cỡ minh họa chưa thống nhất; chưa tự chọn cặp mới cho mọi tài liệu."),
        new("M06","Tab và bố cục đầu đoạn",CapabilityMode.Manual,"Cảnh báo riêng trong chế độ cá nhân",
            "Tab liên tiếp, tab/khoảng trắng đầu đoạn căn giữa; không tự xóa tab hoặc thụt lề."),
        new("M07","Vùng cấu trúc hoặc kiểu chưa rõ",CapabilityMode.Manual,"Trong phạm vi đã đối chiếu",
            "Field/control/liên kết/đối tượng hoặc kiểu trộn có thể giữ thủ công. Bảo vệ XML sâu hiện chỉ 40 đoạn đầu Nhà nước, chưa phủ mọi trang/profile."),
        new("M08","Nét/Shape chưa ghép chắc chắn",CapabilityMode.Manual,"Đường kẻ",
            "Line không có dấu của ứng dụng, nhóm Shape, border, nét chồng hoặc thiếu mốc/số đo; không tạo thêm để làm mất cảnh báo."),
        new("D01","con đo → con đò",CapabilityMode.Demonstration,"Quy tắc minh họa cũ",
            "Có thể cảnh báo nhầm, phải đọc ngữ cảnh. Chưa phải bộ kiểm chính tả tiếng Việt đầy đủ; chưa mở rộng suy đoán ngữ cảnh.")
    });
    public static IReadOnlyList<ReportMilestone> Milestones { get; }=Array.AsReadOnly(new ReportMilestone[]
    {
        new("0.1.18–0.1.27","Sửa chuỗi lỗi ba Line Office 2021: dấu nhận diện DOCX, nét XPS, sửa nhóm, hậu kiểm/rollback và một Undo. Người dùng xác nhận sửa ba Line cùng lúc ở .27; không coi là xác nhận mọi kịch bản."),
        new("0.1.28–0.1.31","Số/ký hiệu, ngày tháng, giữ trường trống, giao dịch chữ/phông và định dạng số/ngày."),
        new("0.1.32–0.1.34","Đo/cảnh báo vị trí, căn giữa có điều kiện, kiểm cả dòng không chọn; phân biệt sửa đã xong với quét lại lỗi."),
        new("0.1.35","Lưu bản sao qua tệp tạm có kiểm byte và bảo vệ nguồn/bản làm việc/Undo."),
        new("0.1.36–0.1.38","Định dạng tên loại/trích yếu; chặn sửa vòng vùng cấu trúc; kiểm lại phông và căn đoạn tiêu đề sau lưu."),
        new("0.1.39","Tách lỗi dọn backup khỏi kết quả checkpoint/Hoàn tác, giữ nguyên nguyên nhân lỗi thực."),
        new("0.1.40","Phát hiện lặp cơ học, đề xuất xóa phần lặp có duyệt, giữ kiểu lần đầu; từ láy/mã/header chưa rõ giữ thủ công."),
        new("0.1.41","Thêm xem/xuất báo cáo tiến độ, danh mục loại lỗi, kết quả kiểm thử đi kèm gói và việc tiếp theo, không cần mở Word.")
    });
    public static IReadOnlyList<string> NextSteps { get; }=Array.AsReadOnly(new[]
    {
        "1. Thể thức và độ ổn định: kiểm tra tổng thể trên Office 2021; xử lý lỗi thực, hoàn thiện nhận diện và phần thể thức còn thiếu theo nguồn. Giữ sửa riêng/nhóm, quét, Undo, lưu/mở, nét thật và rollback.",
        "2. Lỗi chữ cơ bản: mở rộng các quy tắc có thể kiểm chứng về ký tự, khoảng trắng, dấu câu và từ/cụm lặp; bổ sung mẫu, ngoại lệ và kiểm thử. Chưa có từ điển thì không kết luận đã kiểm đầy đủ chính tả tiếng Việt.",
        "3. Sau cùng: ngữ cảnh, ngữ nghĩa và thư viện phục vụ các trường hợp khó; chưa tự suy đoán hoặc viết lại câu trong giai đoạn hiện nay."
    });
    public static int Count(CapabilityMode mode)=>Capabilities.Count(c=>c.Mode==mode);
    public static string Build(string version,DateTimeOffset created,ReportEvidence? evidence=null)
    {
        var text=new StringBuilder();
        void Line(string value="")=>text.AppendLine(value);
        Line("CHÍNH TẢ VIỆT — TIẾN ĐỘ VÀ CHỨC NĂNG");
        Line("Phiên bản: "+version);Line("Tạo lúc: "+created.ToString("yyyy-MM-dd HH:mm zzz",System.Globalization.CultureInfo.InvariantCulture));
        Line("Báo cáo năng lực chương trình; không phải số lỗi đã sửa trong tài liệu đang mở.");Line();
        Line($"TỔNG QUAN: {Count(CapabilityMode.Reviewable)} nhóm có thể đề xuất sửa; {Count(CapabilityMode.Manual)} nhóm chỉ cảnh báo; {Count(CapabilityMode.Demonstration)} quy tắc minh họa.");
        Line("Sửa được khi nhận diện đủ chắc chắn và người dùng duyệt. Không có cảnh báo không đồng nghĩa đúng toàn bộ thể thức/chính tả.");Line();
        Line("KẾT QUẢ KIỂM THỬ ĐI KÈM GÓI");
        if(evidence is not null && evidence.Matches(version))
        {
            Line($"Windows tự động: {evidence.CoreChecks} Core/OpenXML và {evidence.WpfChecks} WPF đạt.");
            Line("Nguồn kết quả: "+evidence.RunUrl);Line("Mã nguồn gói: "+evidence.Commit);
        }
        else Line("Chưa có thông tin kiểm thử đi kèm phù hợp phiên bản; không suy đoán số kiểm tra hoặc trạng thái ĐẠT.");
        Line("CI không có Word; chưa có xác nhận bài Word đúng phiên bản cho toàn bộ chức năng hiện tại trên Office 2021.");
        Line("Mốc người dùng đã xác nhận: sửa ba Line cùng lúc trên .27. Những chức năng sau đó tiếp tục thử nghiệm.");Line();
        foreach(var mode in Enum.GetValues<CapabilityMode>())
        {
            Line(mode switch {CapabilityMode.Reviewable=>"LOẠI LỖI CÓ THỂ ĐỀ XUẤT SỬA",CapabilityMode.Manual=>"CÁC MỤC CHỈ CẢNH BÁO",_=>"QUY TẮC MINH HỌA"});
            foreach(var item in Capabilities.Where(c=>c.Mode==mode))
            {Line(item.Id+" — "+item.Name);Line("  Áp dụng: "+item.Scope);Line("  "+item.Detail);}
            Line();
        }
        Line("CÁC NỘI DUNG ĐÃ LÀM");
        foreach(var item in Milestones)Line(item.Version+": "+item.Detail);
        Line();Line("GIỚI HẠN CÒN LẠI");
        Line("Chưa kiểm/sửa đầy đủ tên cơ quan, thân bài, chữ ký/nơi nhận, số trang và mọi mẫu riêng. Header/footer/textbox, bảng gộp/lồng, field/control mọi trang còn giới hạn. Track Changes hoặc tài liệu bảo vệ chưa được hỗ trợ.");
        Line("Phần chính tả tiếng Việt đầy đủ chưa hoàn thành. Danh mục có điều kiện không phải kết luận pháp lý hoặc kết quả kiểm tra tài liệu cụ thể.");
        Line();Line("CÁC BƯỚC TIẾP THEO — THEO ƯU TIÊN ĐÃ THỐNG NHẤT");
        foreach(string item in NextSteps)Line(item);
        Line();Line("CÁCH KIỂM TRA TỔNG THỂ");
        Line("Với từng nhóm: sửa riêng → sửa nhóm → quét lại → một Undo → lưu bản sao/mở lại. Chữ/phông trước, căn giữa sau, Line cuối cùng theo số đo mới. Ghi phiên bản, tên cảnh báo, thao tác và kết quả thực tế khi báo lỗi.");
        return text.ToString();
    }
}
