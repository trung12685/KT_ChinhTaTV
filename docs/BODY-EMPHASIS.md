# C26 — sửa phông/cỡ và giữ nhấn mạnh theo từng ký tự

B01 sau .63, code .64 chưa đóng gói. Phạm vi pháp lý C26 không đổi: NĐ30 Phụ lục I.I.4 và II.6.e cho phông/màu/cỡ lời văn. Đậm/nghiêng trong một câu có thể là nhấn mạnh chủ ý; bước này không kết luận kiểu đó sai hoặc xóa nó.

Chỉ vùng thân bài đã nhận diện và chữ thuần có proof XML, font/kiểu từng glyph và snapshot căn/thụt/spacing chắc. Không mở bảng/field/list/tiêu đề hoặc vùng chưa xác minh. Nhánh không Kính gửi vẫn cần hai mốc mở đầu không nhấn mạnh; đoạn nhấn mạnh ở phía sau có thể được C26 kiểm riêng.

Ví dụ Arial đỏ cỡ12 ở chữ đậm xen cỡ14 ở chữ nghiêng → TNR đen,12 về13 giữ14, chữ đậm/nghiêng/gạch chân giữ từng ký tự. Đậm/nghiêng với font/màu/cỡ đã đúng không thành lỗi C26. B01 chưa sửa bố cục; B02 tiếp nối mở C27–C29 cho đoạn có từ nhấn mạnh xen chữ thường đứng, xem [phạm vi bố cục](BODY-EMPHASIS-LAYOUT.md). Đoạn toàn đậm/nghiêng vẫn không sửa bố cục.

Payload PreserveEmphasis chỉ hợp lệ cho Body C26 và không yêu cầu Bold/Italic mới. Writer chỉ Name/Color, sau đó size của glyph sai; không ghi Size cả vùng hay Bold/Italic tổng hợp. Không ép lại cỡ đúng ở chữ hoặc khoảng trắng. Hậu kiểm kiểu/cỡ từng glyph đã giữ, căn/thụt và toàn bộ spacing có chụp; scalar font/range count không thay proof ấy. Proof kiểu đầy đủ đã chứa cỡ nên tránh đọc thêm cỡ riêng bị trùng.

Chọn riêng giữ khoảng trắng/cụm lặp chưa chọn; chọn chung sửa chữ/phông → đo XPS mới → Line → hậu kiểm và danh sách mới. Không đạt hoặc COM lỗi thì rollback cả lượt; một Undo, giữ nguồn, lưu bản sao/mở lại. Không báo đạt chỉ từ số Shape hay hết cảnh báo.

Core kiểm giữ style và cỡ, unknown/stale/forged, phạm vi no-greeting và loại trừ bố cục; Desktop kiểm setter chỉ Name/Color/size sai, không chạm Bold/Italic/Size tổng. Fixture Word trộn Arial đỏ12/13/14, đậm/nghiêng/gạch chân khác nhau, sửa riêng/chung chữ/ba Line, Undo/rollback/lưu-mở/hash nguồn. CI chỉ biên dịch fixture, không chạy Word/Office2021. Cần kiểm thực tế; phản hồi phông cũ chưa khép bằng CI.
