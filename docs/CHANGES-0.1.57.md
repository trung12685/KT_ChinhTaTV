# 0.1.57 — Tiến độ sửa và kiểm phông sau Word

## Phản hồi và trạng thái bằng chứng

Người dùng vẫn gặp lại hai cảnh báo cơ quan/trích yếu sau sửa, đồng thời mô tả hai lỗi mục còn ở phần đề xuất. Chưa có số phiên bản đang chạy, diagnostics.log hoặc DOCX đã qua sửa của lượt này; chưa thể xác định chính xác nhánh lỗi thực tế. Bản .56 đã chuyển C20/C21 CanFix sang Bắt buộc sửa, .57 giữ nguyên phân loại đó. Hiển thị phiên bản lấy từ assembly đang chạy ngay cạnh Loại văn bản và trên cửa sổ tiến độ; log khởi động có mã module Desktop/Core để đối chiếu, không có nội dung/đường dẫn tài liệu.

Bản trước kiểm XML do DocxLineEditor tạo nhưng chưa đủ bằng chứng Word runtime. .57 bổ sung các dạng còn thiếu và tiến độ; không tuyên bố đã hết lỗi Office 2021.

## Tiến độ khi sửa

Sau duyệt lượt sửa có cửa sổ ghi bước đang chạy, số mục đã hậu kiểm trong nhóm, tổng mục đã chọn, thời gian chờ. Phân biệt sửa chữ/phông/thứ tự mục, đo lại, căn giữa, đặt từng đường kẻ, kiểm nét XPS, hậu kiểm cả lượt, quét lại và phục hồi. Số mục là kết quả tạm thời: vẫn có thể rollback nếu bước sau thất bại. Thanh tiến độ chưa đạt 100% hoặc trạng thái xanh trước khi hậu kiểm và quét lại hoàn tất. Rollback đặt lại bộ đếm tạm thời về 0; thất bại vẫn hiện thông báo cũ kèm bản phục hồi/log.

Cửa sổ có dispatcher STA riêng chỉ để hiển thị/tính thời gian; Word, COM range và thao tác tài liệu giữ nguyên luồng cũ. Không truyền COM hay WPF preview sang luồng khác, không bơm thông điệp trên luồng sửa để tránh thao tác lồng nhau. Không có hủy giữa giao dịch hoặc tự dừng Word theo đồng hồ; thời gian chờ không phải phần trăm hoặc thời gian dự đoán. Lỗi/đóng bộ quan sát tiến độ không được làm đổi kết quả sửa/rollback. Giao diện chính bị vô hiệu hóa trong lượt sửa.

## Phông cơ quan và trích yếu

Proof định dạng nay hỗ trợ Line nổi hợp lệ do ứng dụng tạo khi Word bọc trong AlternateContent/Choice wps với fallback một Line VML không chứa chữ/hình/đối tượng, hoặc gộp Line vào cùng run với chữ. Chỉ bỏ container đối tượng trong bản sao dùng chứng minh; giữ toàn bộ chữ/run/đối tượng thật. Marker/ID/vai trò phải hợp lệ và XML khớp live Word. TextEditable qua drawing vẫn false. Nhánh tương thích chứa textbox, ảnh, object, hình không phải Line, marker lạ/trùng/sai hoặc XML lệch vẫn bị chặn.

Khi COM Font của vùng đã đủ proof trả về dữ liệu trộn/không xác định, đọc phông trên từng Range ký tự do Word cung cấp và đối chiếu toàn bộ chuỗi. Không đoán CP của từng ký tự. Chỉ bỏ kiểu của phần phân cách/trống khi kiểm chữ hiển thị; một chữ khác phông/cỡ/màu vẫn được quan sát, chữ chưa đọc được vẫn giữ thủ công. Vùng có font tổng thể rõ dùng đường đọc nhanh. Hậu kiểm phông cuối lượt nhiều nhóm dùng cùng cách quan sát, vẫn bắt buộc đạt phông và khớp chữ/prefix hiện tại.

Giữ tên cơ quan đúng hai vai trò của NĐ30 và trích yếu được xuống nhiều đoạn/ngắt dòng khi đúng định dạng/căn giữa. Không ẩn cảnh báo vì đã bấm sửa. Nếu còn manual, log ghi cờ chứng minh/đã đọc/phông trộn, cỡ/đậm/nghiêng/màu và loại issue, không ghi chữ tài liệu.

## Kiểm thử và thử tiếp

Core thêm mẫu nhánh tương thích/run gộp từ drawing thật của editor, nhánh nguy hiểm vẫn bị chặn, và quan sát tiến độ không thay giao dịch/rollback. Kiểm phông dùng đối tượng giống COM để kiểm vùng tổng hợp trộn, từng ký tự đúng/sai, ký tự chưa đọc hoặc mất chuỗi; không phải Word thật. WPF mở cửa sổ tiến độ thực trên STA riêng, chặn luồng gọi để xác minh heartbeat tiếp tục; thử reset khi phục hồi, không hoàn tất sớm và chụp ảnh dữ liệu giả. CI Windows biên dịch bài Kiểm tra Word nhưng không có Word/Office 2021 để chạy. Số thực được ghi trong build-checks.json/TXT.

1. Mở bản .57 và kiểm số bản ngay cạnh Loại văn bản. Ở Bao cao.docx, C20/C21 sửa được phải ở Bắt buộc sửa.
2. Tích ba Line + hai lỗi mục rồi sửa. Cửa sổ phải hiện bước và thời gian; khi Word bận vẫn cập nhật, quét lại là bước cuối.
3. Kiểm lại cơ quan/trích yếu, vị trí nét thật, một Hoàn tác và lưu/mở bản sao. Không xóa hoặc gộp chỗ xuống dòng của trích yếu.
4. Nếu còn hai cảnh báo, giữ diagnostics.log của .57 và bản sao DOCX **sau sửa** để phân biệt mất proof XML với Font trộn; file ban đầu chưa thể hiện cấu trúc do Word viết lại. Không công bố tài liệu này lên kho/release.

23 nhóm đề xuất/8 thủ công/1 minh họa giữ nguyên. [Danh mục .57](SUPPORTED-FIXES-0.1.57.md), [phân nhóm và tiến độ](ISSUE-LIST.md), [nhật ký phát triển](DEVELOPMENT.md). Main/stable .27 giữ nguyên.
