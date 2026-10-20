# Điều tra vị trí Line — 0.1.17

Đã so sánh DOCX gốc và bản sau sửa người dùng cung cấp. Bản gốc không có Line; bản sau sửa có hai v:line và hai pict rỗng, thiếu Line trích yếu. Không lưu tài liệu riêng tư vào Git/gói tải.

Hai Line chứa tọa độ trang trong from/to, nhưng thiếu margin-left/top của hộp VML. Sửa encoder thành hộp đặt theo trang với đầu mút cục bộ. Đồng thời bỏ vỏ pict rỗng khi thay Line có marker ứng dụng.

Đoạn đầu trích yếu có dấu cách cuối. Bộ nhận diện trước dùng điều kiện đo chữ an toàn làm điều kiện căn giữa cấu trúc nên dừng trước đoạn cuối. Hai điều kiện đã được tách; vẫn giữ kiểm tra an toàn khi đo đoạn cuối.

Bổ sung phân trang trước khi đọc metadata, cảnh báo từng vai trò chưa nhận diện, và kiểm nét XPS sau mở candidate. Không báo thành công khi chỉ đúng marker mà nét không hiện hoặc lệch vị trí; rollback nếu kiểm thất bại.

139 kiểm tra portable/SDK đạt với cặp tài liệu thực tế, gồm giữ nguyên chữ, part khác và file nguồn, sửa hai Line cũ/thêm Line thứ ba, dọn khung rỗng và kiểm tọa độ khai báo. Build Windows thành công. Chưa chạy Word/Office 2021 tại cloud Linux; kiểm tra XML không chứng minh khả năng dựng trang thực tế. Bước kiểm XPS trên máy Windows là điều kiện chấp nhận sửa.

## Phản hồi 0.1.17 và điều chỉnh 0.1.18

Nhật ký mới: kiểm nét Agency thất bại (145,17/92,68/70,90pt; RenderedCount=1). Rollback hoạt động thay vì chấp nhận thành công. Điều này bác bỏ việc coi đổi style VML là đã giải quyết hiển thị. Không có tọa độ nét thực tế trong nhật ký cũ nên chưa kết luận nguyên nhân duy nhất.

0.1.18 chuyển encoder sang DrawingML page-relative và giữ kiểm nét. Nhật ký bổ sung số đo thực tế/native; bài kiểm tra Office trên mẫu giả giữ DOCX candidate/XPS khi thất bại để có bằng chứng phân biệt writer/renderer/reader. Tài liệu thật không tự được đưa vào bundle đó. Chặn số trang preview ngoài phạm vi, tương ứng ArgumentOutOfRangeException trong log 0.1.16. Cần chạy Office check thực tế trước thử lại tài liệu, chưa khẳng định vị trí đã đúng trên Word.
