# 0.1.37 — Giữ vùng có cấu trúc khi sửa nhóm (bản thử)

Tiếp nối 0.1.36, commit `4a748a2e73620f12b72088ced24d113d317cc9f4`. Rà soát phát hiện một lỗ hổng từ mã: trích yếu có liên kết/field/control được giữ thủ công về phông nhưng vẫn có thể có gợi ý sửa khoảng trắng hoặc chữ minh họa. Chưa có báo cáo tài liệu thật bị hỏng vì trường hợp này.

## Đã xử lý

- Các quy tắc sửa chữ chung tuân theo trạng thái cho phép sửa của đoạn. Dòng chưa được phép chỉ có thông báo thủ công cho gợi ý chữ phát hiện được, neo ở đầu đoạn thật; không dùng vị trí ký tự đoán trong field/đối tượng. Quốc hiệu/tiêu ngữ cũng không được sửa chữ/phông khi đoạn chưa được phép.
- Áp dụng kết quả đối chiếu XML/range cho toàn bộ 40 đoạn đầu Nhà nước, thay vì chỉ số/ngày và vùng tên loại/trích yếu. Hyperlink, control, field đơn, drawing/đối tượng, tab thật… giữ thủ công. Field phức tạp hoặc sai khớp làm phần từ điểm đó chưa được chứng minh; không suy ra vị trí ký tự từ chữ hiển thị.
- Vùng chữ thuần khác vẫn sửa được. Định dạng trộn đơn thuần không cấm sửa chữ nếu nguyên văn/vị trí đã đủ chứng cứ. Các khoảng để trống sau Số:/ngày vẫn theo ngoại lệ hẹp cũ, không tạo lỗi khoảng trắng mới ở đó.
- Trước lượt chữ/phông, quét lại và ghép **mọi** lựa chọn với gợi ý hiện tại: vị trí, độ dài, nguyên văn, thay thế, loại và payload định dạng. Một mục đã thành thủ công, mất/trùng hoặc thay đổi làm cả nhóm bị từ chối trước checkpoint/ghi. Không chỉ so chữ nhìn thấy, không bỏ qua riêng mục lỗi để báo thành công một phần.

## Giới hạn và phần giữ nguyên

Đây là bảo vệ hẹp cho dữ liệu cấu trúc đã đối chiếu, chưa là hỗ trợ biên tập mọi field/control hoặc xác minh chữ thuần toàn tài liệu. Ngoài 40 đoạn đầu, profile Đảng/cá nhân và header/footer/textbox chưa mở rộng đối chiếu XML trong lượt này. Quy tắc Core tôn trọng cờ an toàn ở mọi profile khi có cung cấp cờ đó; không đồng nghĩa Word đã quan sát đủ mọi vùng.

Các đoạn có Line neo trong đó có thể còn cảnh báo chữ/phông thủ công; không xóa cảnh báo thật để danh sách trống. Không thêm tự sửa ngôn ngữ; quy tắc “con đo” vẫn chỉ minh họa và cần người dùng chọn. Cảnh báo phông do kiểu trộn vẫn độc lập với sửa chữ thuần.

Giữ giao dịch chữ/phông, hậu kiểm toàn story/định dạng sau lưu, rollback cả lượt, một Undo khi commit. Giữ trình tự chữ/phông → căn giữa → Line, đối chiếu marker từ DOCX và nét XPS thực tế, rollback Line và lưu bản sao có kiểm byte. Không thay thuật toán đường kẻ hoặc nhận diện hình học.

## Kiểm thử và cách thử

Core kiểm lỗ hổng sửa vòng, XML hyperlink/control/field/đối tượng, vùng thường bên cạnh, kiểu trộn, ô trống, heading thủ công và từ chối nhóm cũ trước bất kỳ ghi/checkpoint nào. Windows CI chạy toàn bộ Core/OpenXML + WPF, đóng gói Windows x64; CI không có Word.

Giải nén thư mục mới, giữ `samples`, chạy **Kiểm tra → Kiểm tra Word tự động…**. Hai mẫu giả trong thư mục `structured-hyperlink` và `structured-sdt` có trích yếu chứa “con đo” + hai khoảng trắng và đoạn thường bên cạnh. Bài kiểm lấy nhóm gợi ý từ bản chữ thuần rồi mở bản cùng nguyên văn nhưng có cấu trúc: phải từ chối toàn bộ nhóm, giữ hash checkpoint/Changed/Undo. Sau đó sửa riêng vùng thường, lưu/mở lại và một Undo; giữ nguyên chữ và định danh liên kết/control, tệp nguồn không đổi. Cảnh báo thủ công đúng vùng vẫn hiện, các lỗi sửa được đã hết phải biến mất.

**Chưa có bằng chứng 0.1.37 chạy đạt trên Office 2021.** Giữ main, mốc ổn định 0.1.27 và mọi tag/gói cũ. Chỉ chốt sau kết quả dùng thử đúng phiên bản.
