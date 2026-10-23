# 0.1.48 — Khoảng trắng trước dấu kết câu giữa đoạn

Bổ sung C18: `Đã hoàn thành . Tiếp tục báo cáo` → `Đã hoàn thành. Tiếp tục báo cáo` sau khi duyệt. Chỉ xóa dấu cách ASCII ngay trước một dấu `.?!` giữa đoạn; giữ chữ, dấu câu và khoảng trắng sau để có thể chọn riêng C01.

## Điều kiện và giới hạn

- Hai từ kề nhau phía trái, mỗi từ ≥2 chữ cái; từ cuối thường, không La Mã hoặc viết tắt trong danh sách loại trừ. Biên trước cặp space/phẩy/chấm phẩy cho phép sửa chung C14 mà không phải quét sửa lần hai.
- Phía phải đã có dấu cách ASCII và từ ≥2 chữ cái: chữ đầu hoa, phần chữ còn lại thường, biên sau space/hết đoạn. Chưa xử lý mọi trường hợp tên riêng/toàn hoa/ngoặc/nháy.
- Bỏ qua dòng số/ngày, số, viết tắt/đuôi tệp phổ biến, dấu hiệu giao thức/URL/email/đường dẫn/mã, tab/control/NBSP, chuỗi dấu và ranh giới chưa rõ. Danh sách viết tắt hữu hạn; cần đọc lại ngữ cảnh.
- Vùng chưa đối chiếu XML hoặc có cấu trúc giữ thủ công theo .47. Không thêm dấu kết câu, khoảng sau hoặc viết hoa chữ. Không coi là kiểm chính tả/ngữ pháp đầy đủ.

## Sửa nhóm và kiểm chứng

Sửa riêng chỉ bỏ span đã chọn; các mục khác vẫn còn. C18 gộp span chồng C01 trước dấu và có thể sửa chung C01/C13–C17, phông đủ điều kiện. Chữ/phông vẫn phải tách nhóm căn giữa/Line như hiện tại. Giữ hậu kiểm nguyên văn, phục hồi cả lượt và một Undo; không đổi phép đo/nét XPS, đường kẻ hoặc lưu bản sao.

Core có trường hợp Unicode/offset, nhiều câu, ngoại lệ, manual/stale, full-row formatting, sửa riêng/nhóm và lỗi giữa lượt. Kịch bản Word dùng tài liệu giả có ba C18 + chín lỗi độc lập; kiểm kiểu chữ giữ lại, rollback, Undo, quét và lưu/mở/hash nguồn. Kịch bản đã bổ sung vào mã; chưa chạy Word thực tế.

Báo cáo Trợ giúp/TXT có 18 nhóm đề xuất, 8 thủ công, 1 minh họa, ví dụ C18 và mốc .48. P03 còn nghiên cứu thiếu khoảng sau dấu như `Đã hoàn thành.Tiếp tục báo cáo`; không ghi là đã sửa được. Danh sách việc tiếp theo vẫn ưu tiên thể thức/ổn định, lỗi chữ cơ bản, rồi thư viện/ngữ cảnh.

Gói Windows chỉ phát hành sau khi Core/OpenXML, WPF và kiểm báo cáo đóng gói đạt; số kiểm thực tế lấy trong `build-checks.json`/báo cáo TXT đi kèm. **CI không có Word; chưa có xác nhận đầy đủ Office 2021 cho .48.** Không thay main/stable .27 hoặc asset/tag cũ. Xem [danh mục hiện tại](SUPPORTED-FIXES-0.1.48.md) và [bài kiểm Windows/Office](WINDOWS-VALIDATION.md).
