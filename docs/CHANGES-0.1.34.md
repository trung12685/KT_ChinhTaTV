# 0.1.34 — Siết hậu kiểm và cập nhật danh sách lỗi (bản thử)

Tiếp nối tag 0.1.33, commit `455ac094977634c982397017a6982e6dff4ecffb`. Đợt này củng cố sửa riêng/nhóm số-ngày và cách báo lỗi quét lại, chưa mở thêm quy tắc thể thức. Giữ main, mốc ổn định 0.1.27 và mọi gói/tag cũ.

## Nội dung đã xử lý

- Khi căn giữa riêng một dòng, hậu kiểm đối chiếu cả dòng số/ngày không chọn. Trước đây phép kiểm chỉ được gọi cho dòng đã chọn; test mô phỏng đã chứng minh số đúng có thể che việc ngày bị dịch. Đây là khoảng trống tìm qua rà soát/test, chưa có báo cáo tái hiện trên Office 2021.
- Hậu kiểm mới giữ vai trò, CP/nguyên văn, mốc và số đo của dòng không chọn. Mất mục, mất anchor/số đo đã có, đổi baseline/khung hoặc dịch chữ ngoài ý muốn đều từ chối và phục hồi cả lượt. Dòng chọn vẫn phải đo tâm thực tế trên XPS sau lưu. Dung sai giữ hình học 0,5 pt là chính sách đo của ứng dụng.
- Sau phục hồi, đo lại cả hai dòng và kiểm chữ gốc trước khi báo đã khôi phục. Nếu chưa xác minh được, giữ checkpoint và nguyên nhân để phân tích. Một Undo chỉ thêm khi lượt sửa thực sự thành công.
- Nếu sửa đã commit nhưng quét lại thất bại, thông báo rõ thay đổi vẫn ở bản làm việc; xóa danh sách/lựa chọn cũ để tránh dùng lại vị trí cũ. Có thể Quét lại hoặc Hoàn tác. Không tự ghi lại hay tự bỏ lượt đã commit chỉ vì lần quét lỗi. Nguyên nhân quét được giữ trong log riêng, không ghi chữ tài liệu.

Sau quét, chỉ bỏ mục thực sự đã hết lỗi. Lỗi chưa chọn và cảnh báo khác hàng còn thật vẫn hiện, mục thủ công không được tích sửa. Chưa đo được vẫn là chưa đo được; không truyền xác nhận đạt từ dòng đã sửa sang dòng khác.

Giữ sửa chữ/phông/ô trống, thứ tự chữ/phông → căn giữa → Shape Line, kiểm marker/nét XPS/tham chiếu và rollback Line của các bản trước. Chưa tự sửa khác hàng hoặc mở rộng sang bảng phức tạp.

## Kiểm thử và cách thử

Core kiểm lỗ hổng cũ, tác động lên dòng không chọn, mất mốc, kiểm phục hồi và tình huống sửa đã commit trước khi giả lập lỗi quét XPS. WPF kiểm glyph/clip thật, danh sách sau sửa riêng/cả hai, mất anchor và giữ cảnh báo khác hàng thủ công. Workflow Windows chạy toàn bộ Core/OpenXML + WPF rồi đóng gói; không có Word trong CI.

Giải nén gói vào thư mục mới, giữ `samples`, chạy **Kiểm tra → Kiểm tra Word tự động…**. Bài mới cố ý dịch ngày không chọn sau khi sửa số, yêu cầu hậu kiểm từ chối và phục hồi, không thêm Undo. Xem `office-check.txt`, `position-unselected-drift.xps`, `position-drift-before.xps` và `position-drift-restored.xps` cùng các hiện vật cũ. Bài kiểm phải chạy hết và báo đạt, không bỏ điều kiện khi thiếu số đo. Nhánh lỗi quét sau commit được giả lập trong test Core; chưa cố ý gây lỗi xuất XPS trên Office thật.

**Chưa có bằng chứng 0.1.34 chạy đạt trên Office 2021.** Kịch bản trong chương trình và CI đạt không thay xác nhận thực tế. Thử trên bản sao tài liệu, giữ phiên bản/log đúng gói khi báo lỗi; chưa nhập main hoặc chốt ổn định.
