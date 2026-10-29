# Đợt thể thức 18 — chuyển PAGE đơn thuần từ cuối trang lên đầu trang

Ngày 29/10/2026 (UTC+7), mã 0.1.76, cụm thứ tư sau gói 0.1.75. Mở một phạm vi hẹp của C33; không cho phép thay footer nhiều thành phần bằng một số trang mới.

Một section dọc thông thường, số Ả Rập bắt đầu từ 1, tài liệu từ 1 đến 9 trang và footer chỉ chứa trường PAGE đã nhận diện chắc mới được lập kế hoạch di chuyển. Cần biết phông/cỡ/kiểu/màu nguồn, code field và vai trò default/first. Run có trang trí tạo thêm nét hoặc nội dung chưa hỗ trợ, field khóa/lạ, chữ nhãn, bảng/hình, số bắt đầu lại, chẵn/lẻ hoặc nhiều section giữ thủ công. Footer tự viết không trở thành PAGE chỉ vì chứa một chữ số.

Kế hoạch gắn với khóa cấu trúc nguồn hiện tại. Candidate chỉ gỡ đúng tham chiếu footer PAGE được duyệt, giữ các part footer và quan hệ của chúng; tạo/sửa PAGE giữa lề trên, đánh từ 1 và ẩn trang đầu. Không kích hoạt footer first tùy ý đã bị ẩn. Cỡ đang hợp lệ và phần footer không chọn vẫn được giữ theo phạm vi C33.

Trước sửa, XPS phải có đúng một glyph số ứng với từng trang và footer nguồn đang hoạt động, nằm trọn vùng lề dưới, đúng phông/cỡ/kiểu/màu đã đọc và hiện đủ. Snapshot bảo toàn chỉ loại đúng glyph đã ghép này; không bỏ mọi chữ số cuối trang. Số thiếu, trùng, ẩn, cắt, khác kiểu hoặc không ghép duy nhất làm lượt sửa thất bại. Sau sửa, hậu kiểm yêu cầu PAGE đầu trang đúng và không còn số PAGE đã di chuyển ở cuối trang, giữ nội dung, hình, nét và footer khác. Word bỏ hoặc đổi part/quan hệ cần giữ cũng phải phục hồi. Chưa có kết quả Word thực để khẳng định các part không còn tham chiếu này luôn được Word giữ sau lưu.

Đường sửa tiếp tục dùng checkpoint, hậu kiểm sau lưu, rollback cả lượt và một Hoàn tác. Kiểm Core/OpenXML xét field đơn/phức, vai trò first/default, nguồn bất biến, candidate và part không chọn. Kiểm WPF xét nguồn số cuối trang và số dư sau di chuyển. Bài Office **Chuyển PAGE cuối trang** dùng mẫu riêng để thử sửa, lỗi cuối lượt, Undo và lưu–mở; chạy độc lập, không chờ Subject của bài toàn bộ. CI biên dịch bài Office nhưng không chạy Word; chưa có PASS Word thực cho phạm vi mới.
