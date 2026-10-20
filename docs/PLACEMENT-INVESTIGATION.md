# Điều tra vị trí Line — 0.1.17

Đã so sánh DOCX gốc và bản sau sửa người dùng cung cấp. Bản gốc không có Line; bản sau sửa có hai v:line và hai pict rỗng, thiếu Line trích yếu. Không lưu tài liệu riêng tư vào Git/gói tải.

Hai Line chứa tọa độ trang trong from/to, nhưng thiếu margin-left/top của hộp VML. Sửa encoder thành hộp đặt theo trang với đầu mút cục bộ. Đồng thời bỏ vỏ pict rỗng khi thay Line có marker ứng dụng.

Đoạn đầu trích yếu có dấu cách cuối. Bộ nhận diện trước dùng điều kiện đo chữ an toàn làm điều kiện căn giữa cấu trúc nên dừng trước đoạn cuối. Hai điều kiện đã được tách; vẫn giữ kiểm tra an toàn khi đo đoạn cuối.

Bổ sung phân trang trước khi đọc metadata, cảnh báo từng vai trò chưa nhận diện, và kiểm nét XPS sau mở candidate. Không báo thành công khi chỉ đúng marker mà nét không hiện hoặc lệch vị trí; rollback nếu kiểm thất bại.

139 kiểm tra portable/SDK đạt với cặp tài liệu thực tế, gồm giữ nguyên chữ, part khác và file nguồn, sửa hai Line cũ/thêm Line thứ ba, dọn khung rỗng và kiểm tọa độ khai báo. Build Windows thành công. Chưa chạy Word/Office 2021 tại cloud Linux; kiểm tra XML không chứng minh khả năng dựng trang thực tế. Bước kiểm XPS trên máy Windows là điều kiện chấp nhận sửa.
