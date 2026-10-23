# 0.1.46 — Khoảng trắng trước dấu hai chấm (bản thử)

Tiếp nối 0.1.45, commit `23d7185427df369c90817a87d1118df4c947ea31`. Tiếp tục lỗi chữ cơ bản và giữ ví dụ trong báo cáo theo yêu cầu người dùng.

## Gợi ý mới C17

`Ghi chú : nội dung` → đề xuất `Ghi chú: nội dung`. Tương tự với `Kính gửi  : cơ quan.` hoặc `Về việc : Nội dung.` khi đủ điều kiện.

Chỉ xóa dấu cách ASCII trước dấu hai chấm. Giữ nguyên chữ, dấu hai chấm và khoảng cách sau; không ghi lại cả nhãn/câu. Nếu sau dấu còn hai dấu cách, đó vẫn là gợi ý khoảng trắng độc lập: chọn riêng C17 giữ phần chưa chọn, chọn chung mới sửa cả hai. Gợi ý khoảng trắng cũ trùng span trước dấu được gộp để tránh ghi chồng.

Phạm vi hẹp: hai từ kề nhau bên trái đều ít nhất hai chữ cái, từ cuối toàn chữ thường; bên phải đã có dấu cách và từ ít nhất hai chữ cái, có chữ thường/ranh giới rõ. Bỏ qua đoạn có tab/ngắt dòng hoặc dấu kỹ thuật, dòng ứng viên số/ngày và cụm ngày…tháng…năm; token có chấm/đuôi tên miền/tệp; các giao thức http/https/ftp/file/mailto/tel/data/urn. Số/giờ/tỉ lệ số, cặp La Mã, mã hoa, chữ một ký tự và dấu :: không nhận C17. Đây là các dấu hiệu bảo thủ, chưa nhận biết mọi mã/biểu thức viết bằng chữ. Một số nhãn ngắn như `Lưu ý :` hoặc thiếu dấu cách sau dấu chưa thuộc phạm vi; luôn cần duyệt cả câu.

## Gộp sửa và bảo vệ

Có thể sửa nhóm cùng khoảng trắng độc lập, ngoặc, cụm lặp, phẩy/chấm phẩy, dấu kết câu và phông cả dòng đủ điều kiện. Giữ đối chiếu kế hoạch hiện tại/nguyên văn/Word CP, áp phông trước và chữ từ cuối về đầu, hậu kiểm toàn story, rollback cả nhóm khi lỗi và một Undo sau thành công; không retry ghi. Quét mới bỏ đúng mục đã sửa, không ẩn theo lịch sử bấm.

TextEditable và giới hạn XML sâu 40 đoạn đầu Nhà nước giữ nguyên; chưa chứng minh mọi vùng/trang/profile. Các ngoại lệ trên áp riêng cho C17, không đổi phạm vi mọi quy tắc cũ. Ô số/ngày và ngoặc chỉ chứa dấu cách tiếp tục được giữ. Không thay thuật toán Line/kiểm nét XPS, lưu bản sao hoặc checkpoint.

Báo cáo Trợ giúp/TXT thêm C17 với ví dụ trước/sau: 17 nhóm có đề xuất, 8 nhóm thủ công, 1 minh họa; các hướng dự kiến vẫn ghi chưa triển khai. Gói kèm báo cáo TXT mẫu, dữ liệu kiểm thử đúng phiên bản và danh mục hiện tại.

## Kiểm thử và cách thử

Core kiểm sửa riêng/nhóm, ngoại lệ giờ/tỉ lệ/giao thức/mã/địa chỉ/header/control, Unicode, giữ khoảng sau chưa chọn, gộp phông, TextEditable/kế hoạch cũ và rollback sau khi xóa khoảng trước dấu hai chấm cuối cùng trong nhóm. Ví dụ C17 của báo cáo được đối chiếu với quy tắc thực thi như các ví dụ chữ khác.

Bài **Kiểm tra → Kiểm tra Word tự động…** thêm `colon-input.docx` và bản lưu `colon-fixed.docx`: hai C17 và bảy lỗi độc lập. Kiểm sửa riêng giữ khoảng sau/dữ liệu chưa chọn, lỗi cố ý giữa nhóm, giữ đậm/gạch chân, quét lại hết mục đã sửa, một Undo, lưu/mở và hash nguồn. Các dòng giờ/tỉ lệ/giao thức/địa chỉ/số để trống phải giữ nguyên. Bài Word đã lập trình, chưa xác nhận chạy đạt.

**Chưa có bằng chứng 0.1.46 chạy đạt toàn bộ bài Word trên Office 2021.** CI không có Word; chạy Core/OpenXML, WPF và kiểm gói trước phát hành. Giữ main, stable .27 và mọi tag/gói cũ; đây tiếp tục là bản thử.
