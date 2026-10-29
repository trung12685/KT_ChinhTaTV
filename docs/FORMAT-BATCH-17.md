# Đợt thể thức 17 — bảo toàn bitmap khi sửa PAGE

Ngày 29/10/2026 (UTC+7), mã 0.1.76, cụm thứ ba sau gói 0.1.75. Mở thêm bằng chứng bảo toàn ảnh trong C33, tiếp hậu kiểm paint của đợt 16.

Bản xem trước có thể đổi tên hoặc mã hóa lại tài nguyên ảnh khi Word xuất XPS. So URI hoặc byte PNG không đủ để kết luận ảnh thay đổi. Snapshot mới đối chiếu kích thước và hàm băm pixel đã giải mã về Pbgra32, cùng khung hiển thị, opacity và ánh xạ ảnh. Giữ được một ảnh toàn vẹn vẽ bằng Image hoặc ImageBrush hình chữ nhật khi bằng chứng tương đương; không cần giữ cùng tên tài nguyên.

Phạm vi là bitmap đã giải mã, kích thước hữu hạn trong giới hạn kiểm, ánh xạ toàn ảnh cùng trục dương, không cắt và không tile. Clip/mask/effect, profile màu hoặc cách trải ảnh chưa chứng minh được giữ thủ công/phục hồi. Không dùng khung ảnh và hàm băm nguồn để bỏ qua thay đổi opacity, dịch/chuyển/cắt hoặc pixel. Thuộc tính render ảnh cũng phải khớp bằng chứng đã biết. Không mở quyền sửa nội dung ảnh.

Đây là bảo toàn hình ảnh thông thường trong lượt sửa số trang, không phải mở rộng nhận diện ảnh chữ ký. Yêu cầu chừa khoảng ký tay khoảng 80 pt giữ nguyên; không suy thiếu ảnh chữ ký là lỗi.

Kiểm WPF dựng Image/ImageBrush và chạy Capture/Preserve trước–sau: ảnh tương đương, thay pixel/alpha/khung và các ánh xạ chưa hỗ trợ. Kết quả kiểm thực tế của đúng commit nằm ở CI và build-checks.json khi có gói. Linux chỉ biên dịch được phần WPF; CI Windows chạy các mẫu WPF tổng hợp, không chạy Microsoft Word. Cần thêm kết quả xuất XPS/lưu–mở trên Word thực trước khi xác nhận tương thích đầy đủ.

Chỉ nhận nguồn pixel 8 bit thông thường đã liệt kê trong mã (Pbgra32/Bgra32/Bgr32/Bgr24/Rgb24/Gray8). Nguồn độ sâu cao, float hoặc palette chưa đối chiếu giữ thủ công vì chuyển về Pbgra32 có thể làm mất sai khác. Ảnh thân bài nằm hoàn toàn trong lề trên vẫn được đưa vào đối chiếu bảo toàn, không bị loại cùng vùng PAGE.
