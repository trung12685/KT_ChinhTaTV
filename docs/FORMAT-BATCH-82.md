# Đợt 82 — bấm từ và sửa tại trang (0.1.84)

## Nghiên cứu và lựa chọn

Microsoft xác nhận [DocumentViewer chỉ hỗ trợ xem](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/documents-in-wpf). [Glyphs/GlyphRun](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/introduction-to-the-glyphrun-object-and-glyphs-element) cho phép đọc chữ Unicode và ánh xạ glyph/cluster; đó là cơ sở chọn đúng từ trên trang. Chuyển toàn DOCX sang RichTextBox/FlowDocument không chứng minh được giữ phân trang, field, bảng và đối tượng của Word.

Bản này dùng ô nhập neo tại từ trên XPS, Word vẫn dựng trang và lưu gói đầy đủ. Không chuyển cả tài liệu sang một định dạng soạn thảo khác, không thêm thư viện thương mại hoặc nhúng cửa sổ Office bằng SetParent. Khi cần trải nghiệm Word đầy đủ (con trỏ liên tục, kéo chọn, bảng/phông, Enter tạo đoạn, Undo liên tục), hướng có hỗ trợ của Microsoft là [Word task-pane add-in](https://learn.microsoft.com/en-us/office/dev/add-ins/reference/overview/word-add-ins-reference-overview): chuyển phần kiểm/cảnh báo vào Word, người dùng soạn trực tiếp bằng Word. Phương án đó cần dự án và đường cài đặt riêng; chưa triển khai trong bản WPF này.

## Luồng thực hiện

- Chế độ thủ công mặc định. Bấm từ thật trên trang, kể cả chưa có cảnh báo, mở ô chỉ chứa từ đó ngay tại vị trí chữ. Người dùng gõ lại, Enter/nút Ghi mới ghi; Esc/nút Đóng có nháp thì hỏi bỏ. Không ghi khi chỉ chọn, hover, mất focus hoặc nhập chưa xong. Enter trong composition IME không ghi.
- Hit-test vùng glyph/cluster LTR hiển thị đầy đủ; không chọn gần đúng khi bấm khoảng trống, chữ bị cắt/ẩn/xoay hoặc có hai vị trí chồng nhau. Ranh giới từ dùng chữ/số/dấu kết hợp Unicode; tối đa 128 ký tự từ, 256 ký tự thay thế.
- Ghép ngược bằng ngữ cảnh trang về đúng một đoạn native cùng trang; xác nhận nguyên văn, biên CP tuyến tính, nguồn của XPS còn trùng và chứng minh XML-native. Không lấy lần xuất hiện đầu khi chữ trùng. Không giả CanFix, không tự điền gợi ý máy.
- Ghi theo **vùng chọn rõ ràng** thay vì minimal-diff có thể trượt sang run khác trong chuỗi lặp. Phần thay nằm trong một nút chữ đã chứng minh. Giữ chữ ngoài chọn, kiểu vùng đó, các part/XML/nhị phân khác. Dùng đường manual đã có: checkpoint, kiểm toàn gói và story sau mở/lưu, một Undo, rollback hoặc giữ bản phục hồi khi chưa chắc.
- Lỗi ghi/quét không làm mất bản nháp: ô nhập được chuyển về giữa cửa sổ nếu XPS phải đóng. Nếu đã ghi nhưng quét lỗi thì báo đã ghi, không ghi lại. Nếu thất bại thì giữ nháp có thể sao chép, chặn gửi lại nháp cũ. Đổi chế độ/mở/quét/đóng khi còn nháp phải duyệt bỏ trước.

## Giới hạn

Đây là sửa từng từ tại trang, **chưa phải trình Word nhúng hoặc soạn thảo đầy đủ**. Trang chỉ reflow sau khi ghi, không sau mỗi phím. Chưa kéo chọn nhiều từ/nhiều đoạn, chèn đoạn mới hoặc đổi phông/bảng. Chỉ đoạn thuần ngoài bảng, không các vùng đặc biệt mà manual writer đã chặn. Từ đi qua nhiều vùng chữ/định dạng, đoạn qua trang, văn bản RTL/sideways hoặc ánh xạ mơ hồ có thể chưa sửa được. Dùng ô sửa đoạn cũ hoặc Word cho vùng đó; không bỏ giới hạn để giả thành sửa được mọi nơi.

Bộ kiểm lỗi vẫn trong phạm vi catalog hiện có, chưa kiểm toàn bộ chính tả/ngữ nghĩa tiếng Việt. Không tự tạo màu/gạch chân lên tài liệu Word. Quy tắc NĐ30 của 0.1.83 giữ nguyên; bộ đọc thử nghiệm OFF, tối ưu dừng.

## Xác minh

Core kiểm chọn vị trí lặp, ghép ngữ cảnh, Unicode, giới hạn/biên và giữ kiểu run. WPF kiểm hit glyph thật, khoảng trống, clip/opacity/xoay/chồng chữ, visual tree DocumentViewer, popup chỉ chứa từ, ghi một lần, giữ nháp khi lỗi và đã ghi/chưa quét. Nhóm Word độc lập `inline-word` dùng XPS do Word xuất, click glyph, ghép native/XML, cố ý lỗi sau lưu, thay một từ, chặn nháp cũ, Undo, lưu/mở, hash nguồn.

**Word thực tế NOT_RUN** trong môi trường này; CI không có Word. Cần kiểm máy Word thực và IME/DPI/zoom trước khi xem là đủ ổn định.
