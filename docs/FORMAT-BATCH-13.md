# Đợt thể thức 13 — nguồn khoảng đoạn và phạm vi kiểm

Ngày 28/10/2026 (UTC+7), mã phát triển 0.1.75, tiếp đợt 12. Tái hiện trên mã trước: XML có `beforeLines=1`, trong khi số point native thông thường, vẫn tạo đề xuất C28/C29. Nguồn đơn vị dòng có thể thắng nguồn point trong Word; đọc SpaceBefore/After đơn lẻ chưa chứng minh đủ quyền ghi.

Đọc nguồn paragraph/style/basedOn/default để ghi riêng dấu hiệu đơn vị dòng. Giá trị bằng 0 không tự chặn; nonzero hoặc sai cú pháp chặn, kể cả direct zero nhưng style trước còn nonzero. Ghi dấu hiệu bố cục đặc biệt: frame, border, section trong đoạn, bidi/hướng chữ hoặc căn dọc chữ chưa hỗ trợ; spacing/flag trùng, sai cú pháp hoặc nội dung lạ không trở thành mặc định thông thường. Bằng chứng PlainBody/chữ/phông giữ độc lập; chỉ quyền C28/C29 bị thu hẹp. Đoạn sau độc lập vẫn được kiểm.

Context tham gia payload lựa chọn, đối chiếu trước sửa và hậu kiểm cuối. Đổi context không được tiếp tục dùng đề xuất cũ hoặc nhận After=6 là đủ. Không đổi cách đánh giá bảo thủ tổng khoảng dưới 6, không áp số đo khoảng giữa hộp mực chữ thành quy tắc 6 pt.

Thông báo phạm vi nêu số đoạn nội dung đã nhận diện còn phần spacing cần kiểm thủ công: đơn vị dòng, bố cục đặc biệt, grid, auto/contextual, ngắt/giữ trang, Exactly/AtLeast hoặc thiếu quan sát. Đây không phải lỗi tài liệu, không tạo đề xuất sửa giả và không xác nhận những nhóm chưa nhận diện. Exactly/AtLeast chưa được tự sửa.

Kiểm hồi quy gồm direct/kế thừa/default/ghi đè0, malformed/duplicate, ranh giới đoạn, quyền độc lập, payload cũ và hậu kiểm context. Kết quả đúng commit được ghi ở CI/PR; CI không chạy Word. Xem [phân tích phần còn thiếu](REMAINING-WORK.md). Giữ bộ đọc cũ, không tiếp tục tốc độ hoặc ảnh chữ ký.
