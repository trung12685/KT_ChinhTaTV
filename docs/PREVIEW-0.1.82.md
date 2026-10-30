# Chính tả Việt 0.1.82 — bản thử sửa thủ công mặc định

Theo yêu cầu ngày30/10/2026 (UTC+7): người dùng tự sửa là luồng chính; máy chỉ áp dụng gợi ý khi chủ động đổi chế độ. Đây là bản thử giao diện và đường ghi nội dung thủ công, chưa phải trình Word nhúng hoặc trình sửa toàn trang WYSIWYG. Không đổi main/tag/gói cũ.

## Cách thử

1. Giải nén toàn bộ gói vào thư mục mới; chạy ChinhTa.Desktop.exe trên Windows có Word desktop đã kích hoạt.
2. Chế độ mặc định là **Sửa thủ công**. Mở DOCX bản sao, quét và xem các dấu gạch chân trên bản xem trước: đỏ cho lỗi thuộc nhóm thể thức đã nhận diện, vàng cho đề xuất/cảnh báo cần xem xét. Nhóm và mô tả vẫn hiện trong danh sách, không chỉ phân biệt bằng màu. Vùng chưa ghép chắc không được đánh dấu đoán.
3. Bấm một lỗi hoặc dấu gạch chân rồi chọn **Tự sửa nội dung đang chọn…**. Ô sửa chứa nguyên văn đoạn hiện tại, chọn sẵn vị trí lỗi; không tự điền gợi ý của máy. Sửa một từ/vùng trong một lần rồi bấm **Áp dụng nội dung tôi sửa**. Chưa bấm thì Word không đổi.
4. Sau áp dụng, phần mềm dựng lại trang và quét lại. Có thể sửa câu theo ý người dùng; phần mềm không chứng nhận câu mới đúng ngữ nghĩa. Một Hoàn tác bỏ một lần áp dụng. Lưu bản sao, đóng/mở lại và kiểm phần chưa chọn.
5. Muốn máy sửa theo luật đã có, chủ động chuyển sang **Máy sửa theo gợi ý đã duyệt**. Khi đó mới hiện các nút máy sửa một mục/các mục đã tích/tất cả đủ điều kiện; vẫn có bước duyệt trước ghi. Chuyển về thủ công xóa các ô tích tự động.

## Phạm vi sửa trong bản thử

Chỉ sửa chữ của một đoạn thuần trực tiếp ở thân tài liệu, ngoài bảng/textbox/control, trong tài liệu một section đã đối chiếu Word/XML. Đoạn và nội dung mới tối đa16.000ký tự BMP, không ngắt đoạn/tab/ký tự điều khiển. Giữ phần không đổi; phần thay phải nằm trong một nút chữ XML để không đoán kiểu khi vượt nhiều vùng định dạng. Chữ mới mang kiểu của vùng chứa chỗ sửa. Nếu sửa nhiều nơi vượt các run, tách thành các lần áp dụng rõ ràng.

Chưa sửa phông/lề/bảng/Line trực tiếp trong ô này. Lịch sử Track Changes, vùng bookmark/comment chọn sửa, tài liệu bảo vệ, field không hỗ trợ, macro/OLE/chữ ký gói hoặc liên kết ngoài có thể bị chặn. Ảnh và phần chưa chọn phải được giữ nguyên; không bỏ chúng để mở quyền. Các nút máy sửa cũ có phạm vi riêng.

Dấu màu chỉ là lớp xem trước, không ghi màu/gạch chân vào Word hoặc file XPS. Chế độ thủ công không tự bật Word spell-check, không thêm từ điển/ngữ nghĩa, không mở quyền Exactly/AtLeast. Bộ đọc thử nghiệm OFF, tối ưu tốc độ dừng, khoảng ký tay80pt giữ nguyên.

## Bảo toàn và lỗi

Khi áp dụng: đối chiếu lại toàn nguồn, giữ checkpoint, tạo DOCX đầy đủ từ WordOpenXML, chỉ đổi nút chữ đã duyệt; kiểm chữ toàn main story và mọi part XML/nhị phân sau mở/lưu. Cho phép thay đổi cache proofing/RSID và metadata lưu/thống kê đã liệt kê trong mã, không bỏ qua chữ/phông/quan hệ hoặc dữ liệu XML tùy ý. Thiếu/thay part hoặc đổi phần chưa chọn phải phục hồi; chưa xác nhận phục hồi thì giữ checkpoint và báo lỗi.

Bản nháp chưa áp dụng được giữ khi thao tác lỗi; không tự gửi lại. Sửa đã thành công nhưng quét lại lỗi phải được báo là đã áp dụng/chưa quét, không viết lần hai. Đóng ô sửa có bản nháp hỏi bỏ nháp; đóng tài liệu đã sửa vẫn hỏi thay đổi chưa lưu.

## Kiểm Word riêng

Chọn **Kiểm tra → Kiểm tra từng nhóm Word… → Sửa nội dung thủ công**, hoặc:

```powershell
.\ChinhTa.Desktop.exe --word-check manual-text
```

Tổng16nhóm. Bài mới dùng mẫu riêng có kiểu chữ và đoạn cần giữ, cố ý lỗi sau lưu để thử phục hồi, sửa nội dung người dùng nhập, một Undo, lưu/mở và hash nguồn. Kiểm này không đợi Subject hoặc Line. `KIEM-TRA-WORD.cmd` cũ vẫn chạy ba nhóm clone/spacing/technical-text; muốn bài thủ công hãy chọn nhóm mới hoặc dùng lệnh trên.

Giữ office-groups.txt cùng toàn thư mục OfficeGroups của lượt chạy, diagnostics.log và build-checks.json. CI kiểm Core/OpenXML, UI/WPF và gói; không chạy Microsoft Word. **Chưa có Word thực PASS cho bản thử thủ công này.** Các thay đổi chuẩn hóa XML của Word có thể khiến hậu kiểm từ chối bảo thủ; gửi lại mẫu/log để xử lý, không nới proof chỉ để PASS.
