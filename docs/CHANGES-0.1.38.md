# 0.1.38 — Hậu kiểm định dạng tiêu đề (bản thử)

Tiếp nối 0.1.37, commit `45b8c40639be8b6bcb8568bde3265af5b8ac2b70`. Rà soát mã tìm thấy: lượt sửa quốc hiệu/tiêu ngữ/tiêu đề Đảng có kiểm toàn bộ chữ sau lưu nhưng chưa kiểm lại đủ các thuộc tính định dạng đã yêu cầu. Một setter trả về bình thường không chứng minh định dạng đã đạt. Đây là lỗ hổng tái hiện bằng kiểm thử mô phỏng, chưa phải báo cáo hỏng tài liệu thật trên Office.

## Đã xử lý

- Sau lưu, đọc lại phông Times New Roman, đúng cỡ đã đề xuất, đậm, đứng, đen/Automatic từ range chữ thật của từng tiêu đề đã sửa. Nếu yêu cầu căn giữa trong ô thì kiểm cả Alignment thật. Giá trị trộn/chưa đọc được hoặc khác mục tiêu không được coi là đạt.
- Theo dõi range sau thay chữ tiêu đề, vì sửa khoảng trắng/viết hoa có thể thay độ dài. Range tiếp tục dịch theo các sửa chữ ở vị trí trước nó trong cùng nhóm. Đối chiếu nguyên văn range và toàn story; không lấy số cảnh báo còn lại làm bằng chứng đạt.
- Quốc hiệu/tiêu ngữ/tiêu đề Đảng được quan sát trên chữ hiển thị, bỏ dấu kết thúc đoạn/ô. Dấu đoạn có phông khác không còn tự gây cảnh báo phông cho phần chữ đã đúng. Không thay phông của dấu đoạn.
- Sai chữ hoặc định dạng sau lưu làm rollback cả nhóm, kể cả các lỗi khoảng trắng khác đã sửa. Chỉ thêm một Undo sau commit đầy đủ; nhóm thất bại không thêm Undo hoặc tự ghi lại.

Giữ căn cứ, phạm vi nhận diện và các cỡ hợp lệ cũ: không thêm quy tắc thể thức mới. Khi không yêu cầu căn đoạn thì không áp đặt căn giữa mới. Số/ngày vẫn giữ kiểu đậm như trước; tên loại/trích yếu tiếp tục hậu kiểm ký tự hiện có. Kiểm tra cấu trúc đầu Nhà nước của .37, ngoại lệ ô số/ngày trống, bảo vệ lưu bản sao, marker DOCX, nét XPS thật và rollback Line không đổi.

## Kiểm thử và cách thử

Core tái hiện story-only chấp nhận chữ đúng nhưng đậm chưa được áp dụng; hậu kiểm mới phải phục hồi cả nhóm chữ/phông. Kiểm các thuộc tính sai/trộn/mất dữ liệu, cả ba loại tiêu đề, mục tiêu cỡ riêng, căn giữa có/không yêu cầu và nhóm thay chữ độ dài khác cùng khoảng trắng ở đoạn khác. Windows CI chạy toàn bộ Core/OpenXML + WPF và đóng gói; CI không có Word.

Giải nén vào thư mục mới, giữ `samples`, chạy **Kiểm tra → Kiểm tra Word tự động…**. Cuối bài có thư mục `heading-proof-State` và `heading-proof-Party`: chữ tiêu đề viết thường/thừa khoảng trắng, Arial đỏ/nghiêng/chưa đậm/cỡ 16; dấu đoạn cố ý Arial cỡ 8. Bài kiểm gây đổi nghiêng sau Save rồi để hậu kiểm bình thường phát hiện và rollback; phải giữ chữ/checkpoint/Changed, không tạo Undo. Sau đó sửa nhóm tiêu đề + khoảng trắng ở đoạn khác, quét mới phải hết các mục đã sửa, lưu/mở và một Undo phục hồi cả nhóm. Tệp gốc mẫu không đổi; `heading-fixed.docx` giữ kết quả.

**Chưa có bằng chứng 0.1.38 chạy đạt trên Office 2021.** Giữ main, mốc ổn định 0.1.27 và mọi tag/gói cũ. Chưa chốt ổn định trước kết quả dùng thử đúng phiên bản.
