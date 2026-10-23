# Sửa nhiều nhóm trong một lượt — từ 0.1.53

Có thể tích đồng thời **Thiếu đường kẻ**, **Dấu đầu dòng đứng một mình**, **Thứ tự mục** rồi bấm **Sửa các lỗi đã tích**. **Sửa tất cả** cũng chạy qua các nhóm sửa được đang có, sau một lần duyệt.

Ví dụ: đã chọn đường kẻ dưới trích yếu, dấu `-` đơn dưới mục 1 và thứ tự `1, 2, 5`. Chương trình bỏ dấu đơn, đổi `5` thành `3`, nhận diện lại mốc trích yếu trên bản mới rồi đo/kẻ đường. Không dùng vị trí hoặc chiều dài lấy trước khi sửa chữ.

## Các bước tự động

1. Kiểm tra lựa chọn còn đúng, không có mục thủ công hoặc vùng chữ chồng lấn chưa hỗ trợ; tạo bản phục hồi cho cả lượt.
2. Sửa chữ/phông, gồm thứ tự mục và dấu đơn đã chọn. Các loại này có thể chạy chung bước chữ.
3. Đọc lại XML/range và xuất XPS mới, nhận diện đúng các dòng căn giữa đã chọn; sửa theo số đo mới khi còn cần.
4. Đo lại đường kẻ theo dòng chữ mới, chỉ sửa đúng vai trò đã chọn. Chưa đo/ghép được chắc chắn thì phục hồi cả lượt.
5. Hậu kiểm chữ, phông đã chọn, các tâm và nét thật; giữ vị trí số/ngày quan sát được qua bước Line. Chỉ sau đó công nhận thành công và tạo **một Hoàn tác** cho cả lượt.

Bước không có mục được chọn sẽ được bỏ qua. Một mục đã đạt sau bước chữ/phông chỉ được bỏ qua sau khi đo/xác minh thật; thanh trạng thái tách số đã áp dụng và số đã đạt sau đo lại.

## Phạm vi lựa chọn và bảo vệ

- Chỉ xử lý các mục đã chọn; không tự thêm lỗi mới xuất hiện sau quét. Sửa tất cả lấy các mục sửa được của danh sách tại lúc duyệt.
- Mục chưa đủ điều kiện/checkbox bị vô hiệu vẫn thủ công. Ví dụ cần sửa chữ/phông trước mới có thể đo Line thì cảnh báo Line chưa được phép chọn không tự được biến thành gợi ý sửa trong lượt này.
- Các xung đột giữa lỗi chữ trong cùng bước vẫn bị chặn. Nếu sửa chữ cắt qua ranh giới mốc mà không chứng minh được vùng mới, từ chối trước ghi.
- Một lỗi ở bước căn giữa, Line, đo lại hoặc hậu kiểm sẽ phục hồi cả các nhóm trước đó, giữ lượt sửa và Undo có trước. Nếu phục hồi gặp lỗi, giữ checkpoint và báo rõ; không báo thành công một phần.
- Lượt đã thành công nhưng quét danh sách cuối gặp lỗi vẫn dùng thông báo riêng của EditThenScan; không tự sửa lại hoặc tự Undo một lượt đã được xác minh và cam kết.

## Cách thử trên Office 2021

1. Chọn một lỗi thứ tự, một dấu đơn và một đường kẻ. Sau sửa, các mục này phải hết; đường kẻ và lỗi chữ chưa chọn vẫn còn.
2. Chọn cả ba đường kẻ và các lỗi mục; sau một lần sửa, kiểm nét nằm đúng chỗ và danh sách đã cập nhật. Một Hoàn tác phải trả lại cả lượt.
3. Thử thêm phông/số-ngày/căn giữa nếu tài liệu có gợi ý đủ điều kiện. Sau sửa và lưu/mở, chữ, phông và vị trí phải giữ đúng.
4. Chạy **Kiểm tra Word**: mẫu giả có chọn hẹp/toàn bộ, lỗi chèn sau từng bước, giữ Undo cũ, hậu kiểm nét, lưu/mở và hash nguồn. Kết quả thực ghi vào `office-check.txt`; gửi phiên bản và log khi có lỗi.

Windows CI kiểm Core/OpenXML và WPF với glyph/nét thật; không có Word. Bộ kiểm Word nêu trên mới được lập trình/biên dịch, chưa là bằng chứng runtime Office 2021. Bản .53 vẫn là bản thử, stable .27 giữ nguyên.
