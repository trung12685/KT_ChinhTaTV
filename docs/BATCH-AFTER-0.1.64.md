# Đợt phát triển sau 0.1.64

Nền `f5971a5a8754df45bf68e2ba845ed4b73893eac4`, tag v0.1.64:3357 Core/OpenXML +244 WPF +1 kiểm metadata gói, chưa Word runtime. Code0.1.65 đang phát triển, chưa đóng gói. Gom khoảng5–10 bước; main/stable .27 và gói .64 giữ nguyên.

## B01 — C14 dấu câu ngay sau ngoặc kép

Ví dụ `Theo mẫu “ nội dung ” ,báo cáo.` → `Theo mẫu “nội dung”, báo cáo.` khi chọn cả C14 và C30. Chọn riêng dấu câu ngoài giữ mép trong; chọn riêng C30 giữ dấu câu ngoài còn có thể tích sau quét. Không viết lại lời trích dẫn hoặc đổi dấu ngoặc, giữ kiểu/cỡ và phần chưa chọn.

Nhận cặp ngoặc kép cong cân bằng/không lồng, hai từ trước rõ, ít nhất hai từ chữ bên trong và từ phải ít nhất hai chữ cái có chữ thường. Bỏ allcaps/numeric/blank/mã/hàm/header/ngày/tab/NBSP/unknown, mixed/unbalanced/nested quote hoặc nội dung có dấu kết câu. C14 vẫn cùng nhóm comma, nguồn quy tắc ứng dụng, không thêm ID hay điều khoản NĐ30. Tổng30/8/1 giữ nguyên.

Parser mỗi loại ngoặc một lần khi cần, cache failure chỉ trong lần gọi để không quét lặp; nhánh ngoặc tròn giữ guard/hành vi cũ. Không thêm lượt COM/XPS hoặc giữ bằng chứng qua sửa. Batch/glyph/style/text/native/XML/XPS và rollback/một Undo hiện có giữ nguyên. Core kiểm trong/ngoài/chung chữ/phông/bốn nhóm thân bài, retained glyph, CP/NFD/emoji/ba profile, stale/rollback và hồi quy ngoặc tròn. Fixture Word mới kiểm riêng trong/ngoài/chung chữ/baLine, kiểu/cỡ/nguồn/Undo/lưu-mở; CI chỉ biên dịch, không chứng minh Office2021 chạy đạt.

README/TXT/nhật ký ghi rõ phạm vi và ví dụ. Một bước .65 chưa đóng gói; kết quả kiểm theo đúng commit tại PR/Actions. [Chi tiết](QUOTED-PUNCTUATION.md).

## B02 — C30 thiếu khoảng sau ngoặc kép đóng

Ví dụ `Theo mẫu “ nội dung ”đã duyệt.` → `Theo mẫu “nội dung” đã duyệt.` khi chọn cả hai mép trong và khoảng sau. Chọn riêng `”` → `” ` giữ hai mép trong; chọn riêng mép trong giữ gap ngoài còn tích được sau quét. Quote gốc và chữ giữ kiểu/cỡ/gạch chân; khoảng mới không được giả lập là glyph cũ.

Cặp cong cân bằng/không lồng và XML chắc; thêm sau chỉ khi hai từ trước, ít nhất hai từ chữ trong và hai từ sau rõ, từ ngay sau thường. C15/C30 dùng chung gate ngữ cảnh, caller vẫn có policy/cặp riêng; không thêm cho mã/hàm/date/header/uppercase/một từ/câu có dấu kết bên trong/mixed/unknown. Payload chỉ đúng1ASCII qua quote đóng; không thêm khoảng qua CP trống hoặc xóa/đổi quote.

Core kiểm âm/dương, từng phía/chung, Unicode/CP/ba profile, giữ quote/glyph/cỡ/nhấn mạnh qua4 nhóm thân bài, stale/forged/rollback. Desktop kiểm ba checkbox cùng nhóm, tri-state/all và quét mới giữ chỉ lỗi chưa chọn. Fixture Word có quote kiểu riêng, sửa ngoài/trong/chung chữ/baLine, phần chưa chọn/Undo/rollback/lưu-mở/nguồn; CI chỉ biên dịch. TXT cập nhật C30 và ví dụ, tổng30/8/1; hai bước .65 chưa đóng gói. [Chi tiết](QUOTE-AFTER-SPACING.md).

## Tiếp theo

Kiểm tổng thể trên Word/Office2021, xử lý lỗi thực và mở từng phạm vi thể thức/lỗi chữ có chứng cứ. Dấu sau trích dẫn kết bằng .?!, các kiểu quote khác hoặc ngữ cảnh chưa rõ cần nghiên cứu riêng; chưa tự suy nghĩa hoặc đổi loại dấu.
