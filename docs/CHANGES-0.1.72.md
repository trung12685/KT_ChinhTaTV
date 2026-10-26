# Chính tả Việt 0.1.72 — đường kẻ sau khi Word lưu nhánh tương thích

Phản hồi bản 0.1.71, Word 16.0 Build 16.0.20326: lỗi khóa checkpoint đã qua; ghi/lưu/mở/Undo ba Line và sửa Agency đạt. Bài kiểm dừng trước khi sửa Subject với `Sequence contains no matching element`. Gói kết quả có ba cặp DrawingML/VML do Word lưu, mỗi cặp chỉ hiện một nét trong XPS. Mã cũ đánh dấu mọi VML là đối tượng chưa ghép; một Line đã lưu có thể chặn tạo Line tiếp theo. Bộ ghi cũ cũng đếm hai nhánh cùng marker thành hai đối tượng khi sửa lại.

## Thay đổi

- Nhận diện một AlternateContent gồm một Choice WPS và một Fallback VML Line là một đối tượng khi marker/identity trùng, không có chữ/ảnh/nhóm lạ, hai đầu mút khớp tọa độ DrawingML đã ghi. Dùng đúng namespace, không phụ thuộc tên prefix. Không suy vị trí hiển thị từ VML.
- Khi cập nhật Line có cặp hợp lệ, thay cả cặp đúng một lần; không để VML cũ hoặc Choice rỗng. Giữ nguyên run chữ nếu Word ghép đối tượng vào đó, giữ cặp khác và mọi part khác. Giữ phép bù tọa độ đã đo và metadata ý định riêng.
- Nhánh không chứng minh được, VML đứng riêng, ảnh/chữ lẫn hoặc marker trùng vẫn từ chối/kiểm thủ công. Các kiểm nét XPS, dung sai, giữ nét khác, rollback và một Hoàn tác giữ nguyên.
- Bài kiểm tuần tự đối chiếu đúng mốc/chữ/role, báo rõ thiếu đề xuất thay cho lỗi Single chung chung. Ghi `modern-before-Agency/Subject/Motto.docx/.xps/.json` với mốc, tham chiếu đo, inventory và cảnh báo; chụp checkpoint bằng handle chỉ đọc chia sẻ, không gọi SaveCopy hoặc đóng Word để lấy bằng chứng.

## Kiểm chứng và phạm vi

Đã tái hiện `HasUnmappedShapes=true` trên chính DOCX được gửi trước sửa; sau sửa nhận ba cặp, cập nhật cả ba không trùng, giữ nguồn từng byte/chữ/các part ngoài document.xml. Chỉ dùng file người dùng trong kiểm cục bộ, không đưa vào repository hoặc gói phát hành. Có regression sinh mẫu tương đương cho tạo riêng tuần tự, đặt lại, nhánh độc lập, run ghép chữ, bù tọa độ và từ chối các nhánh lạ.

Kết quả Core/OpenXML, WPF và metadata của đúng commit được ghi trong build-checks.json, log và TXT của gói. **CI không chạy Word; chưa xác nhận toàn bộ bài Kiểm tra Word trên Office thực đã đạt 0.1.72.** Không coi chỉ có marker/số lượng Line là đúng nét, không bỏ kiểm để vượt bài.

Giải nén vào thư mục mới, kiểm tiêu đề 0.1.72; để bộ đọc thử nghiệm tắt và chạy Kiểm tra Word tự động. Nếu chưa đạt, gửi ZIP thư mục kết quả và diagnostics.log mới. Không cần chỉnh mẫu hoặc cài lại Word.

Danh mục giữ 33 nhóm đề xuất có điều kiện / 8 thủ công / 1 minh họa. Đây là bản vá lỗi Office đang chặn bài kiểm; không mở thêm tính năng hoặc tiếp tục tối ưu tốc độ. Giữ nguyên main, các tag/gói cũ và mốc quay lại 0.1.67.
