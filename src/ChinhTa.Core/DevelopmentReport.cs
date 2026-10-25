using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ChinhTa.Core;

public enum CapabilityMode { Reviewable,Manual,Demonstration }
public sealed record ReportExample(string Before,string Outcome);
public sealed record ReportCapability(string Id,string Name,CapabilityMode Mode,string Scope,string Detail,ReportExample Example);
public sealed record ReportPlannedWork(string Id,string Name,ReportExample Example);
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
            "Gộp dấu cách thường liên tiếp giữa chữ; giữ ô số/ngày trống theo mẫu nhận diện và ngoặc tròn chỉ chứa dấu cách. Không tự xóa tab hoặc thụt đầu dòng.",
            new ReportExample("hôm       nay là thứ 2","hôm nay là thứ 2")),
        new("C02","Quốc hiệu",CapabilityMode.Reviewable,"Nhà nước, trang đầu",
            "Chuẩn hóa chữ/khoảng trắng của dòng nhận diện; Times New Roman, đen, đậm, đứng, cỡ 12–13. Khi chữ đã đúng, sửa riêng cỡ sai và giữ mỗi cỡ đúng, kể cả qua Line đã xác minh; cỡ trộn kèm thay chữ vẫn có giới hạn. Không tự thêm dòng bị thiếu hoặc đoán dòng sai chữ chưa nhận ra.",
            new ReportExample("Quốc hiệu đúng chữ nhưng có ký tự cỡ 10, 12 và 13.","Chỉ đưa ký tự cỡ 10 về 12, giữ từng cỡ 12/13 và chữ HÒA/HOÀ; sửa Times New Roman, đen, đậm, đứng khi đủ bằng chứng. Chữ chưa đúng vẫn cần qua guard thay chữ riêng.")),
        new("C03","Tiêu ngữ",CapabilityMode.Reviewable,"Nhà nước, trang đầu",
            "Chuẩn hóa dòng nhận diện; Times New Roman, đen, đậm, đứng, cỡ 13–14. Khi chữ đã đúng, sửa riêng cỡ ngoài khoảng về 13, giữ từng cỡ đúng; đường kẻ được đo lại và kiểm nét riêng.",
            new ReportExample("Tiêu ngữ đúng chữ nhưng có ký tự cỡ 12, 13 và 14.","Chỉ đưa 12 về 13, giữ từng cỡ 13/14 và dấu -/– được chấp nhận; sửa Times New Roman, đen, đậm, đứng và đo lại đường kẻ khi chọn kèm.")),
        new("C04","Tiêu đề Đảng",CapabilityMode.Reviewable,"Đảng, trang đầu",
            "Chuẩn hóa dòng ĐẢNG CỘNG SẢN VIỆT NAM đã nhận diện và phông đen, đậm, đứng, cỡ 15. Chưa tự đặt vị trí toàn trang.",
            new ReportExample("đảng cộng sản việt nam","ĐẢNG CỘNG SẢN VIỆT NAM; phông Times New Roman, đen, đậm, đứng, cỡ 15 trong chế độ Đảng.")),
        new("C05","Cách ghi số/ký hiệu",CapabilityMode.Reviewable,"Đầu văn bản Nhà nước nhận diện được",
            "Nhãn Số:, dấu /, dấu nối, khoảng trắng; thêm 0 cho số 1–9. Chuẩn hóa hoa/thường của viết tắt đã khớp danh mục và tên loại duy nhất ở đầu văn bản, ví dụ TTR → TTr, CTR → CTr, QYĐ → QyĐ; BCĐX do người dùng bổ sung. Giữ số và mã cơ quan, không suy đoán loại văn bản hoặc viết tắt chưa biết.",
            new ReportExample("số  :  3 / bc – ABC","Số: 03/BC-ABC")),
        new("C06","Cách ghi địa danh/ngày tháng",CapabilityMode.Reviewable,"Đầu Nhà nước, mẫu rõ và ngày lịch hợp lệ",
            "Dấu phẩy, nhãn ngày/tháng/năm và khoảng trắng; thêm 0 trước ngày 1–9, tháng 1–2. Giữ trường chưa điền, không chọn ngày hay địa danh thay người dùng.",
            new ReportExample("Hà Nội  Ngày3  Tháng1  Năm2026","Hà Nội, ngày 03 tháng 01 năm 2026")),
        new("C07","Phông dòng số/ký hiệu",CapabilityMode.Reviewable,"Dòng được nhận diện và đủ bằng chứng chữ thuần",
            "Times New Roman, đen, cỡ 13, đứng; giữ chữ, ô trống, đậm/gạch chân và căn đoạn. Đậm trộn không tự thành lỗi khi chính sách giữ đậm; đọc theo dữ liệu chữ đã đối chiếu.",
            new ReportExample("Dòng Số: 01/BC-ABC dùng Arial, đỏ, nghiêng, cỡ 12.","Giữ nguyên chữ; đề xuất Times New Roman, đen, đứng, cỡ 13 khi dòng đủ điều kiện sửa phông.")),
        new("C08","Phông địa danh/ngày tháng",CapabilityMode.Reviewable,"Dòng được nhận diện và định dạng đủ rõ",
            "Times New Roman, đen, nghiêng, cỡ 13–14; giữ từng cỡ hợp lệ, đậm/gạch chân và chữ. Cỡ 13/14 cùng hợp lệ không tự thành lỗi; nếu trộn cỡ sai và đúng thì chỉ đưa cỡ sai về 13. Căn giữa là chức năng riêng.",
            new ReportExample("Dòng Hà Nội, ngày 03 tháng 3 năm 2026 dùng Arial, đỏ, đứng, cỡ 12.","Giữ nguyên chữ; đề xuất Times New Roman, đen, nghiêng, cỡ 13; nếu cỡ 13–14 đã hợp lệ thì giữ cỡ.")),
        new("C09","Phông tên loại văn bản",CapabilityMode.Reviewable,"Đầu Nhà nước, khối tên loại/trích yếu chắc chắn",
            "Times New Roman, đen, đứng, đậm, cỡ 13–14. Danh mục có 27 tên hành chính theo PL III.I và Báo cáo đề xuất/BCĐX do người dùng bổ sung; vẫn cần khối/trích yếu đủ chắc, chưa áp cho Công văn/V/v hoặc thể thức bản sao. Viết hoa tên loại có điều kiện theo C24 riêng.",
            new ReportExample("Tên loại BÁO CÁO dùng Arial, đỏ, nghiêng, cỡ 12.","Giữ nguyên chữ; đề xuất Times New Roman, đen, đứng, đậm, cỡ 13 khi khối tên loại được nhận diện chắc chắn.")),
        new("C10","Phông trích yếu",CapabilityMode.Reviewable,"Khối dưới tên loại, có ranh giới rõ trong vùng đầu trang đã đối chiếu",
            "Times New Roman, đen, đứng, đậm, cỡ 13–14; giữ nguyên văn, từng cỡ hợp lệ, gạch chân/căn đoạn và ngắt dòng. Màu đen tự động/đen RGB, khác kiểu ở dấu cách/ngắt dòng hoặc các cỡ cùng hợp lệ không tự thành lỗi. Sai phông/màu có thể sửa mà giữ cỡ; trộn cỡ sai/đúng đã đọc chắc thì chỉ sửa cỡ ngoài 13–14 về 13, giữ từng cỡ đúng; không đọc chắc vẫn thủ công. Chưa tự viết thường trích yếu toàn chữ hoa.",
            new ReportExample("Trích yếu có chữ cỡ 12, 13 và 14; phông Arial đỏ.","Chỉ đưa chữ cỡ 12 về 13, giữ từng cỡ 13/14 và nguyên văn; sửa Times New Roman, đen, đứng, đậm khi đủ bằng chứng. Hậu kiểm từng cỡ sau lưu/Line, không ép cả dòng về 13.")),
        new("C11","Căn giữa số/ngày",CapabilityMode.Reviewable,"Mốc chữ và cùng khung ô được xác minh trên XPS",
            "Căn giữa và bỏ ba thụt lề ngang; giữ chữ/khoảng cách dọc. Chưa tự làm hai dòng cùng hàng hoặc dịch vị trí tuyệt đối.",
            new ReportExample("Dòng Số: 01/BC-ABC có tâm chữ lệch tâm ô 10 pt.","Nếu ô/mốc đo được và đủ điều kiện: căn giữa trong ô, bỏ thụt lề ngang; giữ chữ và vị trí dọc, kiểm lại trên XPS.")),
        new("C12","Ba đường kẻ Nhà nước",CapabilityMode.Reviewable,"Cơ quan, tiêu ngữ, trích yếu nhận diện và đo được",
            "Tạo Line thiếu hoặc đặt lại Line của ứng dụng. Nếu cần sửa chữ/phông liên quan, chọn kèm đúng các mục đó, đo lại rồi kẻ cùng lượt; nét đang đạt có thể chỉ cần đo lại, không ghi dư. Đường cơ quan/trích yếu chấp nhận 1/3–1/2 dòng tham chiếu, tự tạo 3/7; tiêu ngữ theo bề rộng chữ. Kiểm nét XPS thật, không chỉ đếm Shape.",
            new ReportExample("Dòng cơ quan rộng 210 pt nhưng đường kẻ chỉ dài 50 pt.","Độ dài hợp lệ 70–105 pt; nếu tự sửa, đích 90 pt (3/7), căn giữa và kiểm vị trí nét thật trên XPS.")),
        new("C13","Từ/cụm từ lặp liên tiếp",CapabilityMode.Reviewable,"Cùng đoạn, dấu cách thường; chỉ chấp nhận khác chữ hoa đầu tiên tại đầu đoạn",
            "Cụm 2–8 tiếng: đề xuất xóa bản lặp, giữ nguyên lần đầu sau duyệt. Cho phép duy nhất chữ đầu đoạn hoa, bản lặp bắt đầu thường; các chữ khác vẫn khớp chính xác sau đối chiếu Unicode, không đổi chữ của lần đầu. Không gộp hoa/thường giữa câu hoặc sau nhãn mục. Một tiếng như đều đều/nhà nhà, toàn chữ hoa hoặc dòng số/ngày giữ thủ công. Chưa suy nghĩa câu.",
            new ReportExample("Kế hoạch kế hoạch đã duyệt.","Kế hoạch đã duyệt.")),
        new("C14","Khoảng trắng quanh dấu phẩy/chấm phẩy",CapabilityMode.Reviewable,"Giữa các từ hoặc sau ngoặc tròn đơn đã đối chiếu; chỉ dấu cách ASCII",
            "Đề xuất bỏ khoảng trắng trước và giữ một khoảng trắng sau dấu phẩy/chấm phẩy. Nhánh sau ngoặc cần hai từ trước rõ, ít nhất hai từ chữ trong ngoặc và từ phải ít nhất hai chữ cái có chữ thường; ngoặc cân bằng, không lồng. Giữ chữ/ngoặc và khoảng trong chưa chọn; không vượt số/mã, dấu hiệu kỹ thuật, hàm nhận diện được, tab/NBSP, ô trống, header/ngày hoặc XML chưa chắc. Cần duyệt. Chưa mở rộng sang mọi dấu câu.",
            new ReportExample("Theo mẫu (nội dung) ,báo cáo.","Theo mẫu (nội dung), báo cáo.")),
        new("C15","Khoảng trắng quanh ngoặc tròn",CapabilityMode.Reviewable,"Ngoặc cân bằng, không lồng, phần chữ đơn giản trong cùng đoạn",
            "Đề xuất xóa dấu cách thường sát trong ngoặc. Thêm dấu cách còn thiếu sau ngoặc đóng khi hai từ trước, ít nhất hai từ trong và hai từ sau đều rõ, từ ngay sau chữ thường. Chọn riêng hoặc chung, giữ chữ/kiểu và khoảng chưa chọn. Bỏ ô trống, số/mã/công thức/hàm, toàn hoa, tab/NBSP, ngoặc lồng/thiếu và XML chưa chắc. Cần duyệt; chưa thêm khoảng trước ngoặc mở hoặc kiểm mọi loại ngoặc/nháy.",
            new ReportExample("Theo mẫu ( nội dung )đã duyệt.","Theo mẫu (nội dung) đã duyệt.")),
        new("C16","Khoảng trắng trước dấu kết câu cuối đoạn",CapabilityMode.Reviewable,"Một dấu .?! cuối đoạn, từ cuối ≥2 chữ cái thường và có từ trước",
            "Đề xuất xóa dấu cách ASCII trước dấu kết câu; giữ chữ, dấu câu, khoảng trắng sau. Bỏ qua số/ký hiệu, dấu hiệu kỹ thuật/header/ngày, chuỗi dấu và ranh giới chưa rõ. Cần duyệt; chưa kiểm dấu giữa câu hoặc tự thêm dấu/khoảng sau.",
            new ReportExample("Đã hoàn thành .","Đã hoàn thành.")),
        new("C17","Khoảng trắng quanh dấu hai chấm",CapabilityMode.Reviewable,"Hai từ kề nhau bên trái; từ cuối chữ thường; từ bên phải rõ, cùng đoạn chữ thuần",
            "Đề xuất xóa dấu cách ASCII trước hoặc thêm một dấu cách còn thiếu sau dấu hai chấm; chọn riêng từng phía hoặc cả hai. Giữ chữ/dấu câu và khoảng chưa chọn. Bỏ qua số/giờ/tỉ lệ số hoặc La Mã, mã/đường dẫn/protocol, header/ngày, từ bên phải toàn hoa và vùng chưa rõ. Cần duyệt; chưa sửa mọi dấu hai chấm.",
            new ReportExample("Ghi chú :nội dung","Ghi chú: nội dung")),
        new("C18","Khoảng trắng trước dấu kết câu giữa đoạn",CapabilityMode.Reviewable,"Một dấu .?! giữa đoạn; hai từ trái rõ, từ phải viết hoa đầu; khoảng sau được xét riêng",
            "Đề xuất xóa dấu cách ASCII trước dấu; giữ chữ, dấu câu và khoảng sau. Nhận cả câu cuối chỉ một từ viết hoa đầu/còn lại thường, theo sau là một dấu .?! cuối đoạn (có thể còn ASCII spaces chưa chọn). Bỏ qua số, viết tắt/đuôi tệp phổ biến, dấu hiệu kỹ thuật/header/ngày, chuỗi dấu và vùng chưa cho phép sửa. Cần duyệt; chưa kiểm mọi viết tắt hoặc thêm dấu/khoảng sau.",
            new ReportExample("Đã hoàn thành . Xong!","Đã hoàn thành. Xong!")),
        new("C19","Thiếu khoảng trắng sau dấu kết câu",CapabilityMode.Reviewable,"Một dấu .?! giữa đoạn; hai từ trái rõ và từ phải viết hoa đầu",
            "Đề xuất thêm một dấu cách ASCII sau dấu; giữ chữ, dấu câu và khoảng trước chưa chọn. Nhận cả câu cuối một từ có dấu kết câu đơn .?!; không mở rộng qua dấu câu nằm giữa đoạn, chuỗi dấu hoặc viết tắt đã biết. Có thể chọn cùng C18 để sửa hai phía một lượt. Bỏ qua số, viết tắt/đuôi tệp phổ biến, dấu hiệu kỹ thuật/header/ngày và vùng chưa cho phép sửa. Cần duyệt; chưa sửa mọi dấu câu hoặc tự viết hoa.",
            new ReportExample("Đã hoàn thành.Xong!","Đã hoàn thành. Xong!")),
        new("C20","Thứ tự mục gõ tay",CapabilityMode.Reviewable,"Đầu đoạn thân đã xác minh XML; cùng mục cha/cấp, có mốc 1/a/I hoặc nhãn số nhiều cấp rõ",
            "Khi đủ điều kiện, xếp Bắt buộc sửa theo yêu cầu người dùng, vẫn duyệt thứ tự/dẫn chiếu. Đề xuất đổi nhãn 1,2,4 thành 1,2,3; tương tự a), b), d) hoặc I., II., IV. Giữ dấu phân cách, tiêu đề và nội dung. B04 hỗ trợ nhãn số 2–4 cấp khi có mục cha trực tiếp duy nhất, ví dụ 1.1., 1.2., 1.4. → 1.3.; chỉ thay phần số cuối, giữ tiền tố/tab. Không tự đổi tiền tố mục cha còn nhãn con phụ thuộc; dẫn chiếu đến số cũ/số dự kiến hoặc hậu duệ giữ cả nhóm để duyệt. Tách các mục cha; danh sách tự động/bảng/cấp chưa rõ, bắt đầu lại, chữ cái hoặc dẫn chiếu chưa chắc giữ thủ công. Cần duyệt, không suy đoán nội dung bị thiếu.",
            new ReportExample("1. Tình hình\n2. Kết quả công tác\n4. Đề xuất","1. Tình hình\n2. Kết quả công tác\n3. Đề xuất")),
        new("C21","Dấu đầu dòng đứng một mình",CapabilityMode.Reviewable,"Dấu -, –/— hoặc + gõ tay trong mục cha xác định trọn vẹn",
            "Khi đủ điều kiện, xếp Bắt buộc sửa theo yêu cầu người dùng, vẫn cần duyệt. Nếu cùng cấp trong mục cha chỉ có một dấu, đề xuất bỏ dấu và dấu cách phân cách; giữ nội dung/thụt đầu dòng. Dấu + đếm riêng dưới từng dấu - hoặc mục cha khác. Có từ hai dấu cùng cấp thì giữ; mục cha chưa rõ/cấu trúc chưa hỗ trợ giữ thủ công. Đây là lựa chọn trình bày theo yêu cầu người dùng.",
            new ReportExample("1. Nội dung chính\n- Nội dung riêng\n2. Mục khác","1. Nội dung chính\nNội dung riêng\n2. Mục khác")),
        new("C22","Tên cơ quan chủ quản và ban hành",CapabilityMode.Reviewable,"Nhà nước, khối trái đầu trang có mốc Số và cặp quốc hiệu/tiêu ngữ duy nhất",
            "Theo NĐ30 PL I.I.4 và II.2.b: Times New Roman, đen, in hoa, cỡ 12–13, đứng; cơ quan ban hành đậm, cơ quan chủ quản đứng. Chỉ nhận một hoặc hai đoạn tên trong ô đã xác minh; không đoán tên chính thức hoặc sửa khối nhiều dòng/cấp chưa rõ.",
            new ReportExample("cơ quan chủ quản dùng Arial đỏ cỡ 14.","CƠ QUAN CHỦ QUẢN; Times New Roman, đen, cỡ 12–13, đứng, không đậm; giữ nội dung, căn đoạn và cỡ đang hợp lệ.")),
        new("C23","Nơi nhận tại ô 9b",CapabilityMode.Reviewable,"Nhà nước, nhãn Nơi nhận và dòng Lưu cùng vùng XML đã nhận diện",
            "NĐ30 PL I.I.4 và II.9.d: nhãn cỡ 12 nghiêng đậm; danh sách cỡ 11 đứng, có gạch đầu dòng, cuối dòng ;, dòng Lưu kết thúc bằng chấm. Không áp quy tắc dấu đơn/thứ tự mục thân bài cho danh sách này. Chưa đo đủ vị trí lề/hàng, không đoán đơn vị nhận hoặc số bản lưu.",
            new ReportExample("Nơi nhận: cỡ 14 đứng; một dòng ghi Đơn vị mẫu.","Nhãn cỡ 12 nghiêng đậm; danh sách cỡ 11 đứng; sửa dòng thành - Đơn vị mẫu; khi đã xác minh có dòng Lưu cuối khối.")),
        new("C24","Viết hoa tên loại văn bản",CapabilityMode.Reviewable,"Nhà nước, tên loại thuộc danh mục hành chính/bổ sung đã nhận diện chắc trong khối đầu trang",
            "Theo NĐ30 PL I.II.5.b: tên loại in hoa. 27 tên theo PL III.I và Báo cáo đề xuất/BCĐX theo yêu cầu người dùng; không nhận tên bản sao theo mẫu văn bản gốc. Chỉ đổi hoa/thường tên chính xác, có proof chữ thuần và dữ liệu định dạng đủ chắc; chấp nhận cỡ hợp lệ khác nhau và đối chiếu từng ký tự sau viết hoa. Ví dụ Báo cáo có chữ cỡ13/14 và gạch chân → BÁO CÁO, giữ từng cỡ13/14 và gạch chân. Giữ phông/màu/kiểu/căn đoạn chưa chọn sửa; nếu chọn kèm phông/cỡ thì chỉ đổi thuộc tính đã duyệt. Giữ khoảng trắng ngoài tên, trích yếu nhiều đoạn, phông và căn đoạn. Không suy đoán tên loại từ nội dung, không tự viết thường trích yếu hoặc áp cho Công văn/V/v, bảng/field/drawing/vùng chưa rõ. Có thể chọn chung sửa phông, một Undo/rollback.",
            new ReportExample("Báo cáo","BÁO CÁO")),
        new("C25","Khoảng trắng cuối đoạn sau dấu câu",CapabilityMode.Reviewable,"Cả ba chế độ, đoạn chữ thuần có kết thúc rõ bằng .?! hoặc ;",
            "Xóa dấu cách ASCII sau dấu kết câu hoặc chấm phẩy cuối đoạn, giữ dấu kết đoạn/ô, dấu câu, chữ và khoảng trước chưa chọn. Cần hai từ rõ bên trái; bỏ qua số/ngày, mã/đường dẫn/viết tắt, khoảng không ngắt, tab, ô trống, đoạn chưa kết câu và vùng XML chưa chắc. Có thể sửa chung các nhóm chữ/phông đủ điều kiện; cần duyệt.",
            new ReportExample("Đã hoàn thành.   ","Đã hoàn thành.")),
        new("C26","Phông/cỡ chữ nội dung",CapabilityMode.Reviewable,"Nhà nước, văn xuôi thường trong văn bản có tên loại, sau Kính gửi hoặc hai đoạn mở đầu có proof bố cục/chữ thường, trước chữ ký/Nơi nhận",
            "NĐ30 PL I.I.4 và II.6.e: Times New Roman, đen, cỡ 13–14, đứng. Chỉ đoạn chữ thuần đã đối chiếu XML, không danh sách/mục/bảng/field; từ .64 giữ đậm/nghiêng từng glyph đã đọc chắc, chỉ sửa phông/màu/cỡ; cỡ sai về13 và giữ từng cỡ13/14 đúng. Giữ chữ, đậm/nghiêng, gạch chân, căn/thụt và spacing; đối chiếu từng ký tự sau lưu/Line, một Undo và rollback. C26 không tự sửa căn/giãn đoạn (C27/C28/C29 có phạm vi riêng) hoặc toàn bộ thân bài; vùng chưa đủ bằng chứng bỏ qua, không kết luận đúng toàn bộ.",
            new ReportExample("Đoạn nội dung thông thường có chữ Arial đỏ cỡ12 và chữ cỡ14.","Sau duyệt: Times New Roman đen, cỡ12 về13, giữ chữ cỡ14 và nguyên văn/gạch chân/căn đoạn.")),
        new("C27","Căn/thụt đầu dòng nội dung",CapabilityMode.Reviewable,"Nhà nước, văn xuôi thường trong phạm vi C26, không thụt cả đoạn/danh sách/bảng; có thể có từ nhấn mạnh xen chữ thường đứng",
            "NĐ30 PL I.II.6.e: căn đều hai lề, đầu dòng lùi1cm hoặc1,27cm. Giữ mức đang đúng; sai đề xuất1,27cm. Chỉ sửa thuộc tính đã duyệt; đoạn có từ nhấn mạnh xen chữ thường đứng vẫn giữ từng đậm/nghiêng; giữ nguyên văn/phông/cỡ/gạch chân, khoảng đoạn và giãn dòng; đối chiếu sau lưu/Line, sửa chung chữ/phông, một Undo/rollback. Cần XML và quan sát đoạn/phông chắc; không sửa khoảng trắng thủ công đầu dòng, thụt cả đoạn, hanging, bảng/mục/chữ ký hoặc toàn bộ thân bài.",
            new ReportExample("Đoạn văn xuôi thường căn trái, dòng đầu thụt0cm.","Sau duyệt: căn đều, dòng đầu1,27cm; giữ phông/cỡ, khoảng đoạn và giãn dòng chưa chọn.")),
        new("C28","Khoảng cách giữa các đoạn",CapabilityMode.Reviewable,"Nhà nước, hai đoạn văn xuôi thường liền nhau cùng trang, XML/ngữ cảnh/spacing chắc",
            "NĐ30 PL I.II.6.e: giữa các đoạn tối thiểu6pt. Xét cả sau đoạn trước và trước đoạn sau; chỉ khi tổng hai khoảng còn dưới6pt mới đề xuất sau đoạn trước6pt, giữ trước đoạn sau. Không kết luận sai từ After đơn lẻ; tổng đủ6pt nhưng có thể bị gộp giữ ngoài phạm vi chờ đo. Bỏ qua khác trang, đoạn trống/mục/bảng xen giữa, contextual/auto spacing, keep/page break/grid hoặc vùng chưa chắc. Đoạn có từ nhấn mạnh xen chữ thường đứng vẫn giữ từng kiểu. Sửa riêng/chung giữ phông/cỡ/căn/thụt/giãn dòng chưa chọn, kiểm lại cặp đoạn và XML sau lưu/Line, một Undo/rollback.",
            new ReportExample("Hai đoạn văn xuôi liền nhau cùng trang: sau đoạn trước0pt, trước đoạn sau0pt.","Sau duyệt: sau đoạn trước6pt; giữ trước đoạn sau, giãn dòng và các phần chưa chọn.")),
        new("C29","Giãn dòng nội dung",CapabilityMode.Reviewable,"Nhà nước, văn xuôi thường có Word Double hoặc Multiple ngoài1–1,5 dòng chắc chắn",
            "NĐ30 PL I.II.6.e: dòng đơn đến1,5lines. Nhận Single/1,5 và Multiple1–1,5 đang đúng. Double hoặc Multiple ngoài khoảng đề xuất Single; chỉ ghi LineSpacingRule, không gán pt biến chế độ thành Exactly. Exactly/AtLeast chưa có số đo tương đương, grid/ngữ cảnh chưa chắc giữ ngoài phạm vi, không báo lỗi chỉ từ tên chế độ. Đoạn có từ nhấn mạnh xen chữ thường đứng vẫn giữ từng kiểu. Giữ khoảng đoạn, căn/thụt và phông/cỡ chưa chọn; hậu kiểm XML/thuộc tính/glyph sau lưu/Line, sửa riêng/chung, một Undo/rollback.",
            new ReportExample("Đoạn văn xuôi thường giãn dòng Double (2 dòng).","Sau duyệt: Single (dòng đơn); giữ khoảng đoạn, căn/thụt, chữ/phông/cỡ chưa chọn.")),
        new("M01","Khổ giấy và lề",CapabilityMode.Manual,"Nhà nước/Đảng",
            "Cảnh báo A4/lề, trang ngang/lề đối xứng và ngoại lệ; chưa tự đổi thiết lập trang.",
            new ReportExample("Văn bản Nhà nước trên A4 có lề trái 20 mm.","Cảnh báo đối chiếu quy định và ngoại lệ; chưa tự đổi khổ giấy/lề.")),
        new("M02","Khác hàng hoặc vị trí dọc số/ngày",CapabilityMode.Manual,"Khi đo được mốc XPS",
            "Chưa tự dịch theo chiều dọc hoặc làm số và ngày cùng hàng.",
            new ReportExample("Dòng Số: 01/BC-ABC cao hơn dòng địa danh/ngày 12 pt.","Cảnh báo khi đo được mốc; chưa tự dịch dọc để làm hai dòng cùng hàng.")),
        new("M03","Cách viết trích yếu hoặc tên loại chưa đủ chứng cứ",CapabilityMode.Manual,"Khối đã nhận diện",
            "Tên loại chỉ chưa viết hoa nhưng đủ chứng cứ được đề xuất theo C24. Tên loại chưa rõ hoặc trích yếu toàn chữ hoa giữ thủ công để bảo toàn tên riêng/mã; phần phông có gợi ý riêng đủ điều kiện.",
            new ReportExample("Trích yếu viết toàn chữ hoa: KẾT QUẢ CÔNG TÁC UNESCO.","Cảnh báo cách viết trích yếu; giữ tên riêng/mã UNESCO, chưa tự chuyển sang chữ thường. Phần phông có thể có gợi ý riêng nếu đủ điều kiện.")),
        new("M04","Giá trị số/ngày và mã/tên chính thức",CapabilityMode.Manual,"Đầu văn bản",
            "Số bằng 0, ngày sai lịch, năm chưa rõ, mẫu/trùng mốc, mã cơ quan hoặc địa danh cần đối chiếu; không tự đoán giá trị mới. Khi cả tên loại và viết tắt đều nhận diện chắc mà không khớp nhau, cảnh báo đúng phần viết tắt; đối chiếu tên loại/ký hiệu đã đăng ký để sửa bên sai. BCĐX là bổ sung người dùng, Công văn/bản sao/mã lạ vẫn ngoài đối chiếu này.",
            new ReportExample("KẾ HOẠCH\nSố: 01/BC-ABC","Tên loại KẾ HOẠCH tương ứng KH nhưng ký hiệu dùng BC; đối chiếu tên loại và ký hiệu đã đăng ký, sửa bên ghi sai rồi quét lại. Ví dụ ngày sai lịch: ngày 31 tháng 02 năm 2026 vẫn cần kiểm tra thủ công.")),
        new("M05","Thống nhất cỡ quốc hiệu/tiêu ngữ",CapabilityMode.Manual,"Nhà nước",
            "Cảnh báo cặp cỡ minh họa chưa thống nhất; chưa tự chọn cặp mới cho mọi tài liệu.",
            new ReportExample("Quốc hiệu cỡ 12 nhưng tiêu ngữ cỡ 14.","Cảnh báo đối chiếu cặp cỡ theo chú thích mẫu; chưa tự ép chọn một cặp mới.")),
        new("M06","Tab và bố cục đầu đoạn",CapabilityMode.Manual,"Cảnh báo riêng trong chế độ cá nhân",
            "Tab liên tiếp, tab/khoảng trắng đầu đoạn căn giữa; không tự xóa tab hoặc thụt lề.",
            new ReportExample("[TAB][TAB]Nội dung","Trong chế độ cá nhân: cảnh báo tab liên tiếp; không tự xóa vì có thể dùng để dàn bố cục.")),
        new("M07","Vùng cấu trúc hoặc kiểu chưa rõ",CapabilityMode.Manual,"Trong phạm vi đã đối chiếu",
            "Field/control/liên kết/đối tượng hoặc thuộc tính chưa đọc chắc giữ thủ công. Khác biệt định dạng hợp lệ không tự thành lỗi; đủ dữ liệu có thể sửa phông/màu/kiểu mà giữ cỡ, không mở sửa chữ qua đối tượng. Đối chiếu XML phần thân ở cả ba chế độ, kể cả sau đoạn 40; vùng chưa chứng minh giữ thủ công. Đo tọa độ/phông đặc thù đầu Nhà nước vẫn có phạm vi riêng. Thứ tự mục/dấu đầu dòng cần bằng chứng thân ngoài bảng; danh sách tự động hoặc kiểu/cấp chưa rõ giữ thủ công.",
            new ReportExample("Nội dung  báo cáo nằm trong field/control/liên kết chưa được phép sửa chữ.","Giữ thủ công để bảo vệ cấu trúc; không chuyển thành sửa được chỉ vì thấy khoảng trắng thừa.")),
        new("M08","Nét/Shape chưa ghép chắc chắn",CapabilityMode.Manual,"Đường kẻ",
            "Line không có dấu của ứng dụng, nhóm Shape, border, nét chồng hoặc thiếu mốc/số đo; không tạo thêm để làm mất cảnh báo.",
            new ReportExample("Đã có Line dưới cơ quan nhưng Line không mang dấu nhận diện của ứng dụng.","Giữ cảnh báo thủ công nếu không ghép chắc chắn; không tạo thêm Line chồng để làm mất cảnh báo.")),
        new("D01","con đo → con đò",CapabilityMode.Demonstration,"Quy tắc minh họa cũ",
            "Có thể cảnh báo nhầm, phải đọc ngữ cảnh. Chưa phải bộ kiểm chính tả tiếng Việt đầy đủ; chưa mở rộng suy đoán ngữ cảnh.",
            new ReportExample("Em ngồi trên con đo.","Nếu nói phương tiện qua sông thì có thể là con đò; đây chỉ là quy tắc minh họa, phải đọc ngữ cảnh, chưa phải bộ kiểm chính tả đầy đủ."))
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
        new("0.1.41","Thêm xem/xuất báo cáo tiến độ, danh mục loại lỗi, kết quả kiểm thử đi kèm gói và việc tiếp theo, không cần mở Word."),
        new("0.1.42","Thêm gợi ý khoảng trắng quanh dấu phẩy/chấm phẩy có giới hạn, gộp vùng khoảng trắng để sửa nhóm không chồng lấn; cập nhật danh mục và mẫu kiểm."),
        new("0.1.43","Thêm khoảng trắng sát trong ngoặc tròn có điều kiện; giữ đề xuất dấu phẩy/chấm phẩy khi sửa ngoặc, kiểm nhóm/rollback và cập nhật báo cáo."),
        new("0.1.44","Thêm gợi ý khoảng trắng trước dấu kết câu cuối đoạn có điều kiện; sửa nhóm cùng ngoặc/dấu phẩy/cụm lặp, rollback và cập nhật báo cáo."),
        new("0.1.45","Bổ sung ví dụ lỗi và kết quả/hướng xử lý cho từng nhóm đã hỗ trợ, chỉ cảnh báo, minh họa và các việc dự kiến trong báo cáo xem/xuất TXT."),
        new("0.1.46","Thêm gợi ý khoảng trắng trước dấu hai chấm trong nhãn/câu chữ đủ điều kiện, kiểm sửa nhóm/rollback và cập nhật ví dụ C17 trong báo cáo."),
        new("0.1.47","Mở rộng đối chiếu XML cho vùng sửa chữ toàn thân và ba chế độ; chặn vùng chưa chứng minh, giữ phép đo đầu trang và kiểm giao dịch hiện có."),
        new("0.1.48","Thêm gợi ý bỏ dấu cách trước dấu kết câu giữa đoạn có ranh giới rõ; sửa chung lỗi chữ, kiểm rollback và bổ sung ví dụ C18."),
        new("0.1.49","Thêm gợi ý thiếu khoảng sau dấu kết câu giữa đoạn; chọn riêng trước/sau hoặc sửa hai phía một lượt, giữ chữ/kiểu, kiểm giao dịch và ví dụ C19."),
        new("0.1.50","Thu gọn danh sách lỗi một dòng, gom nhóm có checkbox từng mục/nhóm/tất cả, chia thể thức cần sửa/gợi ý cần duyệt/cảnh báo thủ công; chi tiết ở tooltip và khung mở rộng."),
        new("0.1.51","Thêm ngữ cảnh cấu trúc cho thứ tự mục gõ tay và dấu đầu dòng đơn trong từng mục cha; bằng chứng XML, sửa nhóm/rollback và ví dụ C20/C21."),
        new("0.1.52","Rút tooltip còn Lỗi — Căn cứ — Cách xử lý; nêu đúng vị trí và nội dung quy định, phân biệt cấu hình người dùng; giữ số đo/ngữ cảnh đầy đủ trong Chi tiết lỗi."),
        new("0.1.53","Chọn nhiều nhóm một lượt: sửa chữ/phông, đo lại, căn giữa và Line theo đúng mục đã chọn; một Undo, hậu kiểm nét/phông/vị trí và rollback toàn bộ nếu bước sau lỗi."),
        new("0.1.54","Nhận diện trích yếu nhiều đoạn/ngắt dòng và Kính gửi căn giữa; tên cơ quan C22, nơi nhận 9b C23 theo NĐ30; tách thông tin phạm vi XML khỏi lỗi dòng đầu, giữ sửa nhóm/rollback."),
        new("0.1.55","Hậu kiểm phông trong lượt nhiều nhóm dùng toàn thân đã xác minh XML, gồm nơi nhận sau đoạn 40; không mở rộng phạm vi đo Line/căn giữa."),
        new("0.1.56","Sửa cảnh báo phông giả ở cơ quan/trích yếu sau chèn Line: proof định dạng riêng cho Line nổi của ứng dụng đã xác minh, không mở sửa chữ qua đối tượng. C20/C21 đủ điều kiện chuyển Bắt buộc sửa theo yêu cầu người dùng; giữ duyệt, kiểm nét XPS, một Undo/rollback."),
        new("0.1.57","Thêm cửa sổ tiến độ độc lập khi sửa: bước hiện tại, số mục hậu kiểm tạm thời, thời gian chờ, đo lại/quét lại/phục hồi; chưa hoàn tất trước quét lại. Mở proof định dạng cho Line trong nhánh tương thích/run gộp, đọc từng ký tự Word khi phông vùng bị trộn; giữ kiểm nét và rollback, chưa xác nhận runtime Office 2021."),
        new("0.1.58","Thêm C24 viết hoa chính xác tên loại văn bản đã nhận diện chắc theo NĐ30 II.5.b; giữ trích yếu, khoảng ngoài tên, phông/căn đoạn, chặn trường/đối tượng/vùng chưa rõ; sửa chung phông, quét lại và một Undo/rollback. Lỗi cơ quan/trích yếu trên .56 vẫn chờ bằng chứng runtime .57 hoặc mới hơn."),
        new("0.1.59","Danh mục đủ 27 tên hành chính/3 bản sao theo NĐ30 PL III.I trang 40; thêm Báo cáo đề xuất/BCĐX theo người dùng, tách nguồn. Dùng chung nhận diện/C24/phông/mốc trích yếu, không áp thể thức gốc cho bản sao; sửa cảnh báo nhầm chữ thường ở QyĐ/CTr/TTr/TrS, giữ mã cơ quan. Có cửa sổ tra cứu và danh mục đầy đủ trong TXT."),
        new("0.1.60","Gom 5 bước sau .59: B01 hoa/thường viết tắt theo tên loại chắc chắn; B02 cảnh báo tên loại/ký hiệu mâu thuẫn, không đoán bên sai; B03 thêm khoảng sau hai chấm; B04 C25 bỏ dấu cách cuối đoạn có điều kiện, giữ kết đoạn/ô; B05 C15 thêm khoảng sau ngoặc đóng trong cụm chữ rõ. Chọn riêng/chung, giữ khoảng chưa chọn, XPS, một Undo/rollback; tổng25/8/1. Bản thử, chưa xác nhận toàn bộ bài Word trên Office 2021."),
        new("0.1.61","Gom B01–B05 sau .60. B01–B03: cụm lặp khác chữ hoa đầu đoạn, dấu câu sau ngoặc, câu cuối một từ. B04: luồng kiểm định dạng chung, gắn dữ liệu vào text, phân biệt đúng/sai/chưa chắc; aggregate → từ → ký tự, giữ cỡ hợp lệ và kiểm từng cỡ sau lưu/Line. B05: chọn đường kẻ tích kèm đúng lỗi cần sửa trước, duyệt rõ; sửa chữ/phông → đo lại → Line, chỉ ghi nếu cần; bỏ đọc CP và đo mốc lặp trong cùng bản xem trước. Giữ XPS, quét mới, một Undo/rollback; tổng25/8/1. Bản thử, chưa xác nhận bài Word Office 2021 hoặc khép phản hồi .56 chỉ bằng CI."),
        new("0.1.62 — đợt tích lũy","B01 sau .61: sửa riêng cỡ sai trong cơ quan 12–13, tên loại/trích yếu/ngày 13–14 đã nhận diện chắc; giữ mỗi cỡ đúng, chữ/kiểu và sửa chung chữ → đo lại → Line. Chụp cỡ nguồn, chuẩn hóa đúng glyph sai, chiếu qua sửa chữ và hậu kiểm từng cỡ sau lưu/Line; sai thì rollback một lượt. B02: mở sửa cỡ hỗn hợp quốc hiệu/tiêu ngữ khi giữ nguyên chữ; dùng cùng writer Range ký tự, bảo toàn cỡ, quyền định dạng qua Line và hậu kiểm sau lưu/Line. Giữ guard khi đồng thời cần thay chữ, Đảng vẫn cỡ15 riêng. B03: viết hoa tên loại cỡ hỗn hợp; đối chiếu phông/cỡ/màu/đậm/nghiêng/gạch chân từng ký tự và căn/thụt đoạn sau lưu/Line, giữ phần chưa chọn và chuẩn hóa đúng phần phông/cỡ đã duyệt. Dùng Range.Case, không thay text cả vùng. B04: mục số gõ tay2–4 cấp, chỉ sửa số cuối dưới mục cha rõ; giữ tiền tố/tab, chặn đổi cha còn tiền tố con hoặc dẫn chiếu đến số cũ/số dự kiến, quét lại và rollback. B05: mở C25 cho khoảng cuối sau chấm phẩy trong câu chữ rõ, giữ dấu/ô trống/phần chưa chọn, hồi quy sửa chung và rollback. Đủ5 hạng mục; kết quả/gói theo đúng commit tại PR/release, không suy có bản tải từ version. Chưa xác nhận runtime Office 2021."),
        new("0.1.63 — đợt tích lũy","B01 sau .62: kiểm/sửa phông, màu và cỡ đoạn văn xuôi thường có ranh giới xác minh, giữ mỗi cỡ đúng và đối chiếu kiểu ký tự/căn đoạn sau lưu/Line. B02: C27 căn đều/thụt đầu dòng văn xuôi chắc, giữ1cm/1,27cm đang đúng và sửa sai về1,27cm, giữ phông/cỡ/khoảng đoạn/giãn dòng sau lưu/Line; sửa riêng/chung và rollback. B03: C28 khoảng giữa hai đoạn liền nhau cùng trang, chỉ kết luận khi cả hai khoảng đặt chưa thể đạt6pt, sau đoạn trước6pt và giữ trước đoạn sau; B04: C29 Double/Multiple ngoài1–1,5 vềSingle, Exactly/AtLeast chờ đo. Tách đề xuất, ghép cùng C26/C27/chữ/Line theo snapshot bất biến, kiểm ngữ cảnh và đoạn kế sau lưu/Line, giữ phần chưa chọn, một Undo/rollback. B05: văn bản có tên loại không Kính gửi, cần hai đoạn mở đầu văn xuôi có XML căn đều/thụt1cm hoặc1,27cm và dữ liệu kiểu chắc; không dùng chữ nhấn mạnh/không rõ làm mốc. Hai mốc ngắn sau xóa lặp vẫn giữ vai trò, không làm mất lỗi chưa chọn phía sau. Chưa mở toàn bộ thân bài hoặc mọi cách giãn đoạn; đủ5 bước, gói/kết quả theo commit tại PR/release, chưa xác nhận Word/Office 2021."),
        new("0.1.64 — đang phát triển","B01 sau .63: C26 sửa phông/màu/cỡ đoạn có nhấn mạnh, giữ đậm/nghiêng/gạch chân theo từng glyph; cỡ đúng và cả cỡ ở khoảng trắng không bị ép lại. Source snapshot đoạn và kiểu/cỡ bắt buộc, hậu kiểm sau lưu/Line; sửa riêng/chung chữ, một Undo/rollback. Dùng proof kiểu đầy đủ thay lượt đọc cỡ riêng trùng, không bỏ hậu kiểm. B02 mở C27–C29 cho đoạn văn thường có từ nhấn mạnh xen chữ thường đứng, giữ từng kiểu/cỡ/gạch chân; kiểm sửa riêng từng thuộc tính/chung bốn nhóm với chữ/Line, snapshot đoạn kế, stale/drift/quét lại/rollback/một Undo/lưu-mở. Đoạn toàn đậm/nghiêng hoặc không chắc chưa sửa bố cục; hai mốc không Kính gửi vẫn không nhấn mạnh. Hai bước, chưa đóng gói, chưa có Word runtime Office2021.")
    });
    public static IReadOnlyList<string> NextSteps { get; }=Array.AsReadOnly(new[]
    {
        "1. Thể thức và độ ổn định: kiểm tra tổng thể trên Office 2021; xử lý lỗi thực, hoàn thiện nhận diện và phần thể thức còn thiếu theo nguồn. Giữ sửa riêng/nhóm, quét, Undo, lưu/mở, nét thật và rollback.",
        "2. Lỗi chữ cơ bản: mở rộng các quy tắc có thể kiểm chứng về ký tự, khoảng trắng, dấu câu và từ/cụm lặp; bổ sung mẫu, ngoại lệ và kiểm thử. Chưa có từ điển thì không kết luận đã kiểm đầy đủ chính tả tiếng Việt.",
        "3. Sau cùng: ngữ cảnh, ngữ nghĩa và thư viện phục vụ các trường hợp khó; chưa tự suy đoán hoặc viết lại câu trong giai đoạn hiện nay."
    });
    public static IReadOnlyList<ReportPlannedWork> PlannedWork { get; }=Array.AsReadOnly(new ReportPlannedWork[]
    {
        new("P01","Vị trí dọc số/ngày",new ReportExample("Dòng số và dòng ngày lệch hàng 12 pt.","Hoàn thiện nhận diện, căn cứ, phép đo và hậu kiểm trước khi cho phép dịch dọc; hiện chưa có nút tự sửa phần này.")),
        new("P02","Thể thức các vùng còn thiếu",new ReportExample("Đoạn thân bài hoặc phần chữ ký dùng Arial màu đỏ.","Tiếp tục xác minh quy định và nhận diện từng vùng trước khi bổ sung sửa phông/kiểu. Chưa tự áp phông cho toàn bộ thân bài/chữ ký; nơi nhận 9b có phạm vi C23 riêng, văn xuôi thường đủ điều kiện có C26–C29; giãn Exactly/AtLeast, khoảng đoạn bị gộp/auto/grid còn cần đo thêm.")),
        new("P03","Dấu câu ngoài phạm vi hiện tại",new ReportExample("Đã hoàn thành.tiếp tục báo cáo","Nghiên cứu ranh giới câu, chữ hoa/thường, viết tắt và ngoại lệ; C18/C19 cần từ sau viết hoa đầu đủ rõ, chưa sửa ví dụ này hoặc tự viết hoa.")),
        new("P04","Chính tả qua dữ liệu kiểm chứng",new ReportExample("Tôi xẽ gửi báo cáo.","Cần dữ liệu từ và mẫu ngoại lệ trước khi đề xuất xẽ → sẽ; hiện chưa có quy tắc sửa ví dụ này. Chưa coi chương trình là bộ kiểm chính tả đầy đủ.")),
        new("P05","Ngữ cảnh và từ dễ nhầm — làm sau cùng",new ReportExample("Anh ấy giành thời gian đọc sách.","Nghiên cứu ngữ cảnh giành/dành và thư viện sau phần thể thức/lỗi chữ cơ bản; chưa có gợi ý tự động cho ví dụ này."))
    });
    public static int Count(CapabilityMode mode)=>Capabilities.Count(c=>c.Mode==mode);
    public static string Build(string version,DateTimeOffset created,ReportEvidence? evidence=null)
    {
        var text=new StringBuilder();
        void Line(string value="")=>text.AppendLine(value);
        Line("CHÍNH TẢ VIỆT — TIẾN ĐỘ VÀ CHỨC NĂNG");
        Line("Phiên bản: "+version);Line("Tạo lúc: "+created.ToString("yyyy-MM-dd HH:mm zzz",System.Globalization.CultureInfo.InvariantCulture));
        Line("Báo cáo năng lực chương trình; không phải số lỗi đã sửa trong tài liệu đang mở.");
        Line("Trạng thái đã hỗ trợ/chưa tự sửa/dự kiến là tiến độ phát triển, không phải lịch sử sửa tài liệu.");
        Line("Ví dụ dùng dữ liệu giả để minh họa điều kiện; [TAB] là ký hiệu tab. Dấu cách trong ví dụ được giữ nguyên.");
        Line("Các dòng của ví dụ thứ tự mục/dấu đầu dòng thể hiện các đoạn riêng, không ghép chung một đoạn.");Line();
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
            Line(mode switch {CapabilityMode.Reviewable=>"ĐÃ HỖ TRỢ ĐỀ XUẤT SỬA — CẦN DUYỆT",CapabilityMode.Manual=>"CHƯA TỰ SỬA — CHỈ CẢNH BÁO",_=>"QUY TẮC MINH HỌA"});
            foreach(var item in Capabilities.Where(c=>c.Mode==mode))
            {
                Line(item.Id+" — "+item.Name);Line("  Áp dụng: "+item.Scope);Line("  "+item.Detail);
                Line("  Ví dụ: “"+item.Example.Before+"”");
                Line((mode==CapabilityMode.Reviewable?"  Kết quả minh họa khi duyệt sửa: ":mode==CapabilityMode.Manual?"  Hiện tại: ":"  Cần duyệt ngữ cảnh: ")+item.Example.Outcome);
            }
            Line();
        }
        Line("DANH MỤC TÊN LOẠI VÀ CHỮ VIẾT TẮT");
        Line(DocumentTypeCatalog.Citation);
        foreach(var source in Enum.GetValues<DocumentTypeSource>())
        {
            Line(source switch {DocumentTypeSource.DecreeAdministrative=>"27 TÊN LOẠI HÀNH CHÍNH THEO NGHỊ ĐỊNH",DocumentTypeSource.DecreeCopy=>"3 LOẠI BẢN SAO THEO NGHỊ ĐỊNH",_=>"BỔ SUNG THEO YÊU CẦU NGƯỜI DÙNG"});
            foreach(var item in DocumentTypeCatalog.Entries.Where(e=>e.Source==source))Line("  "+item.Name+" — "+item.Abbreviation);
        }
        Line(DocumentTypeCatalog.UserNote);
        Line("Danh mục đầy đủ không có nghĩa đã kiểm mọi mẫu đặc thù. Bản sao có thể thức riêng; chưa tự đổi chữ viết tắt theo tên loại hoặc mã cơ quan. Giữ đúng QyĐ, CTr, TTr, TrS.");Line();
        Line("CHU KỲ PHÁT TRIỂN");
        Line("Gom khoảng 5–10 bước có kiểm thử rồi mới đóng gói Windows; từng bước vẫn cập nhật code và nhật ký. Nội dung đợt sau .59 được ghi tại docs/BATCH-AFTER-0.1.59.md.");Line();
        Line("CÁC NỘI DUNG ĐÃ LÀM");
        foreach(var item in Milestones)Line(item.Version+": "+item.Detail);
        Line();Line("GIỚI HẠN CÒN LẠI");
        Line("Chưa kiểm/sửa đầy đủ tên/mã cơ quan chính thức, toàn bộ thân bài, chữ ký và mọi biến thể nơi nhận, số trang và mọi mẫu riêng. Header/footer/textbox, bảng gộp/lồng, field/control mọi trang còn giới hạn. Track Changes hoặc tài liệu bảo vệ chưa được hỗ trợ.");
        Line("Phần chính tả tiếng Việt đầy đủ chưa hoàn thành. Danh mục có điều kiện không phải kết luận pháp lý hoặc kết quả kiểm tra tài liệu cụ thể.");
        Line();Line("CÁC BƯỚC TIẾP THEO — THEO ƯU TIÊN ĐÃ THỐNG NHẤT");
        foreach(string item in NextSteps)Line(item);
        Line();Line("CÁC VIỆC DỰ KIẾN — CHƯA TRIỂN KHAI SỬA TỰ ĐỘNG");
        Line("Đây là hướng phát triển, không phải danh sách chức năng hiện có hoặc cam kết thời điểm hoàn thành.");
        foreach(var item in PlannedWork)
        {
            Line(item.Id+" — "+item.Name);Line("  Ví dụ: “"+item.Example.Before+"”");
            Line("  Chuẩn bị/nghiên cứu: "+item.Example.Outcome);
        }
        Line();Line("CÁCH KIỂM TRA TỔNG THỂ");
        Line("Với từng nhóm: sửa riêng → sửa nhóm → quét lại → một Undo → lưu bản sao/mở lại. Từ .53 có thể chọn nhiều nhóm cùng lúc: chương trình tự sửa chữ/phông, đo lại, căn giữa và Line theo mục đã chọn; một Undo bỏ cả lượt. Mục chỉ cảnh báo vẫn giữ thủ công. Ghi phiên bản, tên cảnh báo, thao tác và kết quả thực tế khi báo lỗi.");
        return text.ToString();
    }
}
