# 0.1.56 — Sửa cảnh báo định dạng sau khi chèn Line

## Vấn đề và thay đổi

Sau khi sửa cả ba đường kẻ, lượt quét .55 có thể báo “Kiểm tra định dạng cơ quan ban hành” và “Tên loại/trích yếu” dù chữ đúng. Line mới được neo trong đoạn cơ quan/trích yếu; bộ chứng minh chữ thuần từ chối đoạn có drawing, làm mất điều kiện kiểm phông. Đây là giới hạn của bộ kiểm, không chứng minh chữ sai.

.56 tách quyền sửa định dạng khỏi quyền thay chữ: khi XML hiện tại khớp với đoạn Word, drawing là Line nổi của ứng dụng có ID/vai trò duy nhất, cấu trúc Line hợp lệ, không chứa chữ và nằm trong run riêng, đoạn có thể được đọc/đề xuất sửa phông. Bản sao XML dùng để chứng minh bỏ riêng run Line; tài liệu thật, run và đối tượng không bị xóa. Thay chữ thông thường qua đối tượng vẫn bị chặn. Line lạ, inline, dấu nhận diện trùng/sai, nội dung nhúng hoặc lệch XML giữ thủ công. Chữ thật sự sai phông/cỡ/màu vẫn có đề xuất; định dạng trộn/chưa đọc được vẫn cảnh báo.

Khối trích yếu được phép xuống nhiều đoạn/ngắt dòng như .54, giữ nguyên chỗ xuống dòng và căn đoạn. Cơ quan chủ quản và ban hành vẫn theo hai vai trò riêng, TNR đen, cỡ 12–13, đứng; ban hành đậm. Không áp phông cho cả tài liệu.

C20 thứ tự mục và C21 dấu đầu dòng đơn **đủ điều kiện sửa** chuyển sang **Bắt buộc sửa** theo yêu cầu người dùng. Đây là lựa chọn phân nhóm; căn cứ tooltip vẫn ghi quy tắc người dùng, không biến thành điều khoản của Nghị định. Mục chưa rõ vẫn chỉ cảnh báo, mọi lượt sửa vẫn cần chọn/duyệt.

## Kiểm chứng và giới hạn

Kiểm hồi quy dùng ba drawing được tạo thật bằng DocxLineEditor: quét lại không phát sinh cảnh báo ở mẫu đúng, mẫu sai phông vẫn có hai đề xuất, không mở sửa chữ qua Line, các đối tượng/ID/vai trò sai bị chặn. Mẫu này tái hiện cảnh báo khi dùng lại bộ map/guard của .55. Có kiểm nhóm chọn chung Line + C20/C21.

File người dùng được kiểm XML riêng trên bản sao: trước và sau chèn ba Line không sinh hai cảnh báo trên; giữ nguyên nội dung/định dạng XML và hash file gốc. Vị trí CP/trang của bài đọc XML là giả lập, **không phải bằng chứng Word/Office 2021 hoặc vị trí nét XPS**. Không đưa file người dùng vào mã nguồn hay release.

Bài Kiểm tra Word được bổ sung quét/lưu/mở sau cả ba Line trên mẫu đúng; sửa phông cơ quan/trích yếu/nơi nhận sau đoạn 40 cùng ba Line, lỗi sau giai đoạn Line phải phục hồi cả lượt, một Undo và lưu/mở. Windows CI biên dịch bài này nhưng không có Word để chạy; kết quả Core/OpenXML, WPF và gói TXT thật nằm trong build-checks.json/TXT.

Giữ phép đo/nét XPS thật, vị trí/số đo mới giữa các giai đoạn, hậu kiểm phông và một Undo/rollback. Không công nhận thành công từ số Shape hoặc ẩn cảnh báo theo lịch sử sửa. Giữ stable .27 và main; .56 là bản thử, chưa xác nhận runtime Office 2021.

## Cách thử trên Office 2021

1. Mở file kiểm tra ở chế độ Nhà nước. Ba Line thiếu, lỗi 5 → 3 và dấu đơn đủ điều kiện đều ở Bắt buộc sửa.
2. Tích tất cả rồi Sửa các lỗi đã tích. Quét lại: các mục đã đạt biến mất; cơ quan và khối trích yếu đúng không phát sinh cảnh báo phông mới.
3. Lưu bản sao/mở lại, thử một Hoàn tác; chữ và chỗ xuống dòng phải nguyên vẹn. Thử đổi một vùng đã nhận diện sang phông/cỡ sai: vẫn phải báo và đưa đề xuất phù hợp.
4. Có thể chạy Kiểm tra Word tự động để lấy bằng chứng đúng phiên bản; nếu còn cảnh báo, giữ diagnostics.log và file bản sao đã sửa để đối chiếu XML/live Word.

Danh mục không thêm loại lỗi: 23 nhóm có thể đề xuất, 8 thủ công, 1 minh họa. Xem [danh mục .56](SUPPORTED-FIXES-0.1.56.md), [danh sách lỗi](ISSUE-LIST.md) và [lượt nhiều nhóm](MULTI-GROUP-REPAIR.md).
