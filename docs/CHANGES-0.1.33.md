# 0.1.33 — Căn giữa số và ngày tháng có kiểm XPS (bản thử)

Tiếp nối tag 0.1.32, commit `08e711de522b69e7c3298a7c5f3fbf418ed2ba7a`. Mở sửa căn giữa trong phạm vi hẹp đã xác minh; giữ mốc ổn định 0.1.27 và mọi tag/gói cũ.

## Phần bổ sung

- Cho sửa căn giữa dòng số/ký hiệu và địa danh/ngày tháng trong đầu Nhà nước đã nhận diện chắc chắn. Tên cơ quan hoặc cả quốc hiệu–tiêu ngữ phải đã căn giữa không thụt lề trong ô bảng đơn có lề cân bằng, và glyph thực tế cùng tâm với khung ô trên XPS.
- Chỉ bật sửa khi dòng và toàn bộ mốc đo được trọn vẹn một dòng, cùng một khung ô. Thiếu clip/anchor, bảng phức tạp, chữ xuống dòng/cắt/xoay, mốc chưa rõ hoặc không cùng tâm vẫn duyệt thủ công.
- Thao tác đặt căn giữa và bỏ ba thụt lề ngang của đúng dòng được chọn. Giữ nguyên chữ, ô trống để điền số/ngày, phông và khoảng cách dọc. Cảnh báo khác hàng vẫn độc lập, chưa tự dịch chuyển lên/xuống.

Căn cứ: NĐ30 PL I Phần I.II.3.c và 4.c, PDF 23 trong nguồn đã ghi tại FORMAT-RULES.md. Ngưỡng lệch tâm 3 pt và dung sai giữ hình học 0,5 pt là chính sách đo của bản thử, không phải số bắt buộc trong quy định. Chưa kiểm mọi mẫu/ngoại lệ hoặc sửa đổi pháp lý sau nguồn đã ghi.

## Kiểm kết quả và phục hồi

Đo lại trước ghi để tránh dùng số đo cũ. Sau lưu, dựng XPS mới và kiểm tâm chữ thực tế, toàn bộ chữ không đổi, baseline/kích thước dòng, vị trí mốc và khung ô được giữ. Không báo đạt chỉ vì thuộc tính COM đã nhận giá trị hoặc danh sách không còn gợi ý. Thiếu bằng chứng sau sửa cũng phải khôi phục cả lượt. Lượt thành công có một Undo cho cả nhóm; lượt thất bại không thêm Undo. Nếu phục hồi lỗi, giữ checkpoint và cả hai nguyên nhân để phân tích.

Sửa theo thứ tự **chữ/phông → căn giữa số/ngày → Shape Line**. Sửa tất cả xử lý nhóm đầu còn lỗi, quét lại rồi bấm tiếp để dùng số đo mới. Có thể tích sửa chung hai dòng căn giữa; chọn lẫn các nhóm bị chặn trước ghi. Giữ kiểm marker, nét XPS, tham chiếu cuối và rollback ba Line; không thay thuật toán Line.

## Kiểm thử và dùng thử

Core kiểm điều kiện bật sửa, mốc/khung thiếu hoặc xung đột, kết quả giả chưa dịch chuyển, thay đổi baseline/mốc/khung, payload không hợp lệ và rollback khi hậu kiểm không đạt. WPF dùng glyph/clip thật để kiểm tâm, đề xuất và mục đã sửa biến mất sau quét. Workflow Windows chạy đầy đủ Core/OpenXML + WPF rồi đóng gói; CI không có Word.

Giải nén vào thư mục mới, giữ `samples`, chạy **Kiểm tra → Kiểm tra Word tự động…**. Bài mới kiểm sửa riêng/nhóm hai dòng, lỗi có chủ ý sau bước đầu và phục hồi, một Undo, lưu/mở lại. Mẫu cố ý lệch dọc phải giữ cảnh báo khác hàng sau khi tâm đã đúng. Kết quả lưu `office-check.txt` và `position-*.xps`; mẫu giả đã sửa là `position-centered.docx`.

**Chưa có bằng chứng chạy đạt 0.1.33 trên Office 2021.** Kịch bản có trong chương trình không phải xác nhận đã đạt. Thử gói mới trên bản sao tài liệu; ghi phiên bản và gửi kết quả/log nếu có lỗi. Chưa nhập main hoặc chốt ổn định trước bằng chứng dùng thử.
