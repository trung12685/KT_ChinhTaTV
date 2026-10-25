# C14 — dấu phẩy/chấm phẩy sau ngoặc kép cong

B01 sau .64, code .65 chưa đóng gói. Quy tắc ứng dụng về khoảng trắng có duyệt, cùng nhóm Dấu phẩy, chấm phẩy; không tạo thêm loại lỗi hoặc gán căn cứ pháp lý mới.

Ví dụ `Theo mẫu “nội dung” ,báo cáo.` → `Theo mẫu “nội dung”, báo cáo.`. Tương tự `Theo mẫu “kế hoạch” ;đã duyệt.` → `Theo mẫu “kế hoạch”; đã duyệt.`. Chỉ sửa phần dấu phẩy/chấm phẩy và ASCII spaces sát hai phía của dấu; không đưa dấu quote hoặc từ vào span thay thế.

Cần cặp “…” cân bằng, không lồng; hai từ trái rõ với từ cuối thường; ít nhất hai từ chữ bên trong, mỗi từ từ hai chữ cái, có chữ thường; từ phải từ hai chữ cái và có chữ thường. Giữ gate chữ thuần/XML hiện hành. Bỏ số/mã/hàm/đường dẫn/header/ngày, allcaps/ô trống, tab/NBSP/control, mixed/nested/unbalanced quote. Nhánh này chưa nhận câu trích dẫn có dấu kết bên trong, dấu ngoặc thẳng/đơn/kiểu khác hoặc tự quyết định vị trí dấu câu trong/ngoài trích dẫn.

Chọn riêng C14 ở `Theo mẫu “ nội dung ” ,báo cáo.` được `Theo mẫu “ nội dung ”, báo cáo.`; C30 mép trong vẫn còn. Chọn riêng C30 được `Theo mẫu “nội dung” ,báo cáo.`; dấu câu ngoài vẫn còn có thể tích. Chọn cả hai cùng lỗi chữ/phông đủ điều kiện được `Theo mẫu “nội dung”, báo cáo.` trong một lượt, giữ kiểu/cỡ/gạch chân của quote và nội dung.

Hai loại ngoặc tròn/kép dùng parser theo từng loại, một lần khi cần trong một lần gọi; cặp lỗi được giữ rỗng trong map cục bộ để không quét lặp. Không cache qua lần quét/sửa. Policy ngoặc tròn và các guard cũ giữ nguyên. Không thêm đọc COM hoặc xuất XPS cho phát hiện; sửa chung chữ/phông/căn/spacing/Line vẫn qua quét mới, proof nguồn/glyph, nét thực, một Undo và rollback toàn lượt khi lỗi.

Core kiểm ví dụ dương/âm, nhiều cặp tròn/kép, Unicode tổ hợp/emoji/CP và ba profile, lỗi chưa chọn còn hành động, payload/text/snapshot stale, bốn nhóm thân bài và retained glyph, lỗi lưu/rollback. Fixture Word mới kiểm riêng trong/ngoài/chung chữ/baLine, giữ kiểu/cỡ/nguồn, một Undo và lưu/mở; CI chỉ biên dịch fixture. Chưa có xác nhận runtime Office2021 cho phần mới hoặc khép phản hồi phông cũ.
