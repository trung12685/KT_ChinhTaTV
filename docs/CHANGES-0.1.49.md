# 0.1.49 — Thiếu khoảng trắng sau dấu kết câu

Bổ sung C19: `Đã hoàn thành.Tiếp tục báo cáo` → `Đã hoàn thành. Tiếp tục báo cáo` sau duyệt. Thêm đúng một dấu cách ASCII sau một dấu `.?!` trong ngữ cảnh rõ; giữ chữ, dấu câu và khoảng trước chưa chọn.

## Điều kiện và giới hạn

- Dùng các giới hạn ngữ cảnh của C18: hai từ trái ≥2 chữ cái, từ cuối thường, biên rõ; từ phải hoa đầu và còn lại thường, ≥2 chữ cái, biên sau space/hết đoạn.
- Bỏ qua số/La Mã, viết tắt và đuôi tệp phổ biến, header/ngày, URL/email/đường dẫn/ký hiệu kỹ thuật, tab/control/NBSP, chuỗi dấu và vùng XML chưa cho phép sửa. Danh sách viết tắt hữu hạn, cần đọc lại câu.
- Không thêm dấu câu, tự viết hoa hoặc xử lý mọi ranh giới/ngoặc/nháy. `Đã hoàn thành.tiếp tục báo cáo` vẫn ngoài phạm vi, đưa vào ví dụ P03 cần nghiên cứu.

## Sửa riêng và sửa nhóm

Từ .49, C18 xét được phần khoảng trước ngay cả khi chưa có khoảng sau. C18/C19 độc lập: chọn C19 trên `hoàn thành  .Tiếp tục` giữ nguyên khoảng trước và C18 chưa chọn; chọn C18 giữ C19 chưa chọn; chọn cả hai sửa đúng hai phía một lượt. Hai span kề nhau, không chồng.

C19 dùng range dấu câu hiện có dài 1 với replacement cùng dấu + space; không đoán anchor chèn dài 0 hoặc ghi lại chữ kế bên. C01/C13–C19 và phông đủ điều kiện vẫn có thể sửa chung; nhóm căn giữa/Line tách như hiện tại. Giữ kiểm văn bản cuối, rollback cả lượt và một Undo; không đổi WordSession/CP mapping, checkpoint, lưu bản sao hoặc kiểm nét XPS thực tế.

## Kiểm thử và báo cáo

Core kiểm span chèn chính xác, Unicode/offset, nhiều dấu, ngoại lệ, cả ba chế độ, manual/stale, chọn riêng giữ mục chưa chọn, sửa nhóm chữ/phông và lỗi ở nhiều điểm chèn/xóa. Bài Word giả thêm ba C19 + chín mục khác: kiểm kiểu từ ngay hai phía dấu, quét lại, rollback, Undo, lưu/mở và hash nguồn. Kịch bản đã lập trình, chưa có kết quả Word thực tế.

Báo cáo Trợ giúp/TXT hiện có 19 nhóm đề xuất, 8 thủ công, 1 minh họa; thêm ví dụ C19, mốc .49 và giới hạn. P01–P05 tiếp tục là dự kiến riêng. Xem [danh mục hiện tại](SUPPORTED-FIXES-0.1.49.md), [hướng dẫn xuất TXT](REPORT-EXPORT.md) và [bài kiểm Windows](WINDOWS-VALIDATION.md).

Gói Windows phát hành sau kiểm Core/OpenXML, WPF và báo cáo đóng gói; số đếm thực tế lấy từ `build-checks.json`/TXT đi kèm. **CI không có Word; chưa xác nhận đầy đủ Office 2021 cho .49.** Main/stable .27 và tag/asset cũ giữ nguyên.
