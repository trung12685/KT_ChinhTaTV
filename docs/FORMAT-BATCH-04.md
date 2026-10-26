# Đợt thể thức04 — phối hợp và hậu kiểm cả lượt

Ngày26/10/2026. Tiếp nối C33 tại ce5b46b1, mã0.1.70. Đây là một đợt sửa phối hợp, không thêm mã loại lỗi mới; danh mục vẫn33 đề xuất/8 thủ công/1 minh họa. Chưa đóng gói Windows; mốc0.1.67 và gói0.1.69 giữ nguyên. Công việc tốc độ đọc tiếp tục dừng.

## Các lỗi và khoảng trống đã xử lý

| Tình huống | Trước | Sau |
|---|---|---|
| Footer có run chỉ gồm ba dấu cách, không xml:space | Parse Flat OPC bỏ run text trắng, khác fingerprint DOCX; PAGE bị từ chối | Giữ whitespace khi đọc Flat OPC như khi đọc file; duyệt và sửa PAGE vẫn giữ nguyên footer |
| Thiết lập đóng sách có trong settings nhưng không có trong XML của body range | Điều kiện sửa trang có thể cho chọn rồi writer từ chối khi thấy settings thật | Kiểm trang bằng Document.WordOpenXML đầy đủ, cùng snapshot với PAGE; bố cục ngoài phạm vi được chặn ngay lúc quét |
| PAGE chạy sau nhóm chữ/phông/Line và mở lại DOCX | Các nhóm trước đã hậu kiểm riêng, nhưng cuối wrapper PAGE chủ yếu kiểm PAGE và visual body | Chụp kỳ vọng toàn lựa chọn trước ghi, kiểm lại các nhóm sau bước cuối: chữ, font, từng cỡ hợp lệ cần giữ, gạch chân/bố cục đã theo dõi, khoảng đoạn/giãn dòng, tâm và nét Line thật |
| Chọn cả PAGE, khổ/lề và nhóm nội dung | Wrapper trang mở tracker mới với tổng nhỏ hơn | Một tracker/tổng/đếm xác nhận cho cả lựa chọn; không cộng lặp số mục của nhóm con |

Ví dụ kiểm tổng thể: bảy mục gồm khổ/lề, PAGE, màu phông cơ quan, khoảng trắng thừa và ba đường kẻ. Đổi trang trước, quét/đo lại nhóm chữ và Line, PAGE sau; hậu kiểm tất cả rồi mới công nhận một lượt thành công. Không ghi đè vùng chưa nhận diện hoặc áp thêm lỗi chưa chọn.

## Bảo toàn và kiểm thử

Không đổi quy định pháp lý hay mở thêm phạm vi trang/header phức tạp. Bộ đọc body đang mặc định giữ nguyên; chỉ dữ liệu chứng minh điều kiện trang cần gói đầy đủ. Không lấy file đã lưu cũ thay snapshot Word hiện tại. Checkpoint, một Undo và rollback toàn lượt vẫn bắt buộc; hậu kiểm font/hình học muộn thất bại không được commit.

Bài footer trước sửa đã thất bại đúng chỗ fingerprint Flat OPC/DOCX không khớp, sau sửa vượt qua candidate và kiểm giữ footer trang đầu. Kiểm tiến độ lồng giữ tổng7, xác nhận1→6→7, rollback về0. Kiểm đọc Word hiện tại không chạm Content range, nhìn thấy settings đóng sách. Kiểm format sau mở lại dùng CP mới và đúng text/prefix; từ chối mất gạch chân, đổi màu, đổi một cỡ13 thành14 dù cả hai trong khoảng hợp lệ, đổi nghiêng/căn đoạn, mốc thiếu/trùng/lệch ngữ cảnh; late failure phục hồi thay vì commit. Mẫu DOCX tổng thể được kiểm điều kiện và candidate trang→PAGE giữ toàn bộ thân/bảng.

Kịch bản Word mới kiểm bảy mục sửa chung, XPS/nét thật, nguyên chữ/kiểu và nguồn, tiến độ, lỗi sau nhóm trước/sau PAGE/sau hậu kiểm cuối, một Undo, lưu/mở. CI chỉ biên dịch kịch bản này; chưa có kết quả chạy Office2021. Số kiểm và commit cuối xem bằng chứng CI của PR, không suy từ phiên bản.

## Bước tiếp

Đã tích lũy năm đợt có ích ở0.1.70: policy bộ đọc, C31 khối ký, C32 trang, C33 PAGE, hậu kiểm phối hợp. Chuẩn bị gói Windows cùng danh sách33 nhóm, ví dụ, giới hạn và kịch bản kiểm Office thực. Sau xác nhận các đợt thể thức, chuyển sang nhóm lỗi chữ cơ bản/khoảng trắng/dấu câu/từ-cụm lặp; ngữ cảnh và thư viện tiếp tục sau cùng.
