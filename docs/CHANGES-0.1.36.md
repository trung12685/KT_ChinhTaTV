# 0.1.36 — Định dạng tên loại và trích yếu (bản thử)

Tiếp nối tag 0.1.35, commit `bd5cf140dfcbb8553917eabfafe2dc1537bb87e3`. Thêm kiểm/sửa định dạng tên loại và trích yếu của văn bản Nhà nước có tên loại rõ ràng. Giữ main, mốc ổn định 0.1.27 và mọi gói/tag cũ.

## Phần bổ sung

- Tên loại như “BÁO CÁO” và 1–4 đoạn trích yếu ngay dưới: kiểm Times New Roman, màu đen, cỡ 13–14, chữ đứng, đậm. Giữ cỡ đang hợp lệ, ngoài khoảng đề xuất cỡ 13. Không thay nguyên văn, gạch chân hoặc căn đoạn.
- Chỉ cho sửa khi khối ở đầu trang 1 sau cặp quốc hiệu–tiêu ngữ duy nhất, có tên loại duy nhất được nhận diện, cùng vùng XML/range đã đối chiếu, căn giữa không thụt lề và có ranh giới trước nội dung thường. Phạm vi 4 đoạn là chính sách an toàn của bản thử.
- Thiếu mốc, trùng tên loại, nhầm sang Kính gửi/Căn cứ/Điều/Chương, chưa rõ điểm dừng hoặc kiểu trộn giữ thủ công. Field/control/drawing/tab chưa đủ bằng chứng để tự sửa. Có Line neo trong cùng đoạn cũng có thể khiến định dạng đoạn đó được giữ thủ công.
- Tên loại chưa viết hoa và trích yếu toàn chữ hoa chỉ cảnh báo để duyệt, không tự đổi tên riêng/mã hoặc nội dung. Công văn/V/v và mẫu chưa nhận diện chưa áp dụng phần mới.

Căn cứ: NĐ30 Phụ lục I Phần I.I.4 (PDF 21) và II.5.b (PDF 24) trong nguồn đã ghi tại FORMAT-RULES.md. Đã đối chiếu lại hai trang nguồn; chưa xác minh mọi sửa đổi pháp lý sau nguồn hoặc mọi mẫu/ngoại lệ. Vị trí tuyệt đối và đường kẻ vẫn là các kiểm tra riêng, không suy đạt từ phông đúng.

## Giữ an toàn

Ghép lại gợi ý với mốc hiện tại ngay trước ghi. Áp dụng định dạng ký tự trước các lỗi chữ được chọn trong cùng lượt, kiểm toàn bộ chữ và cả trạng thái đậm thật sau lưu. Lỗi giữa lượt/lưu/hậu kiểm khôi phục cả lượt; chỉ một Undo khi thành công. Kiểu đậm của số/ngày cũ vẫn được giữ như trước.

Sau sửa quét lại thật để bỏ mục đã hết lỗi, giữ mục còn thật. Tiếp tục theo thứ tự **chữ/phông → căn giữa số/ngày → Shape Line** vì đổi cỡ/phông có thể đổi số đo dòng. Giữ kiểm marker/nét XPS/tham chiếu và rollback Line, lưu bản sao có kiểm byte, ô số/ngày để trống và các chức năng cũ.

## Kiểm thử và cách thử

Core kiểm khối hợp lệ, trích yếu nhiều đoạn, ranh giới thiếu/nhầm, cỡ hợp lệ, vùng trộn/cấu trúc, cách viết thủ công, payload sai, gộp span và hậu kiểm đậm thất bại. Windows CI chạy toàn bộ Core/OpenXML + WPF và đóng gói; không có Word trong CI.

Giải nén vào thư mục mới, giữ `samples`, chạy **Kiểm tra → Kiểm tra Word tự động…**. Bài mới tạo `document-heading-input.docx`, kiểm sửa riêng, lỗi cố ý giữa nhóm, sửa nhóm + một lỗi khoảng trắng, giữ cỡ 14/13,5/gạch chân/căn đoạn, một Undo và lưu/mở lại. Kết quả `document-heading-fixed.docx`; mẫu trộn `document-heading-mixed.docx` phải còn đúng mục thủ công. Xem `office-check.txt` đúng phiên bản.

**Chưa có bằng chứng 0.1.36 chạy đạt trên Office 2021.** Phản hồi chưa phát hiện thêm lỗi của bản trước được ghi nhận, không chuyển thành xác nhận bài Word mới. Chưa nhập main hoặc chốt ổn định trước kết quả dùng thử.
