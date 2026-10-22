# 0.1.35 — Lưu bản sao có kiểm tra (bản thử)

Tiếp nối tag 0.1.34, commit `235085a6473fe5092f837f12b7af9085380cc0c3`. Củng cố bước Lưu bản sao; giữ main, mốc ổn định 0.1.27 và mọi gói/tag cũ. Không mở thêm quy tắc thể thức hoặc thay thuật toán sửa ba Line.

## Nội dung đã xử lý

Trước đây Lưu bản sao ghi đè trực tiếp tệp đích bằng File.Copy. Việc sao chép lỗi giữa chừng có thể để bản đích cũ bị ghi dở. Đây là rủi ro tìm qua rà soát mã; chưa có báo cáo tài liệu thực bị hỏng.

Nay chương trình lưu checkpoint, ghi một tệp tạm mới trong cùng thư mục đích, đẩy dữ liệu xuống tệp rồi kiểm SHA-256 với checkpoint trước và sau copy. Chỉ khi đủ dữ liệu mới thay tên thành bản sao đích. Không fallback sang ghi đè trực tiếp khi thay tên lỗi. Lỗi trước công bố giữ tệp đích cũ và thay đổi chưa lưu ở bản làm việc; có thể xử lý khóa/quyền rồi lưu lại. Nếu dọn stage cũng lỗi thì giữ vị trí stage cùng nguyên nhân để phân tích.

Chặn chọn tài liệu gốc, bản làm việc và các bản Hoàn tác làm đích; đối chiếu cả đường dẫn liên kết và danh tính tệp trên Windows, thay vì chỉ so chuỗi tên. Kiểm lại trước công bố. Chỉ bỏ trạng thái chưa lưu khi lưu thành công; lưu thất bại không làm mất Undo hoặc tự sửa lại tài liệu. Thao tác lưu không thêm một Undo mới.

Giữ nguyên các sửa chữ/phông/ô trống, căn giữa có kiểm cả dòng không chọn, danh sách sau quét, kiểm nét XPS và rollback Line. Bản sao có byte đúng không đồng nghĩa văn bản đạt toàn bộ thể thức.

## Kiểm thử và cách thử

Core kiểm tạo/thay bản sao, lỗi copy một phần, bản thiếu byte hoặc sai byte cùng độ dài, checkpoint thay đổi khi copy, đường dẫn bị bảo vệ, đích không hợp lệ và dọn stage. Windows kiểm hard link và tệp đang khóa không cho rename; Linux cục bộ kiểm symlink tệp/thư mục. Workflow chạy đầy đủ Core/OpenXML và WPF rồi đóng gói Windows.

Giải nén gói vào thư mục mới, giữ `samples`, chạy **Kiểm tra → Kiểm tra Word tự động…**. Bài mới tạo mẫu đích có sẵn, cố ý ngắt copy sau 16 byte và thử đích bị khóa. Tệp gốc/đích cũ, trạng thái chưa lưu và Undo phải được giữ. Sau bỏ khóa, bản sao `save-copy-existing.docx` phải bằng checkpoint; các bước lưu/mở lại, một Undo chữ/phông và toàn bộ bài Line/vị trí cũ vẫn chạy. Xem `office-check.txt` đúng phiên bản.

Thử thực tế trên bản sao tài liệu: lưu tên mới, sửa tiếp rồi thay bản sao đã có, mở lại kiểm chữ/phông. Không dùng chính tài liệu gốc làm đích.

Đây không phải sao lưu lịch sử hoặc bảo đảm mất điện trên mọi thiết bị, hệ thống tệp mạng/đồng bộ. Khi thay tệp, không cam kết giữ mọi ACL, luồng dữ liệu phụ hoặc metadata đặc biệt của bản đích cũ; ghi chú kỹ thuật tại DEVELOPMENT.md. Đích cần cho phép xác minh danh tính và đổi tên; không đủ quyền thì báo lỗi.

**Chưa có bằng chứng 0.1.35 chạy đạt trên Office 2021.** CI không có Word; các kịch bản trong chương trình không thay xác nhận thực tế. Chưa nhập main hoặc chốt ổn định.
