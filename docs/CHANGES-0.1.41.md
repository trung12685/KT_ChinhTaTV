# 0.1.41 — Xem và xuất tiến độ, chức năng (bản thử)

Tiếp nối 0.1.40, commit `366b53b29d02c1c14dd2dd5fa0db2593089918a0`. Theo yêu cầu người dùng, thêm báo cáo ngay trong chương trình: nội dung đã làm, danh sách loại lỗi hiện xử lý được/cần duyệt và các bước phát triển tiếp theo.

## Cách dùng

Vào **Trợ giúp → Tiến độ và chức năng…**. Không cần mở DOCX hoặc khởi động Word. Cửa sổ có nội dung chỉ đọc, có thể chọn/sao chép và bấm **Xuất báo cáo TXT…** để chọn nơi lưu; tệp UTF-8 có dấu nhận diện để đọc đúng tiếng Việt. Báo cáo mẫu cùng phiên bản cũng có trong ZIP và trang tải GitHub.

## Nội dung báo cáo

- Phiên bản, thời điểm tạo và tổng số nhóm tính từ danh mục: 13 nhóm có đề xuất sửa sau khi duyệt, 8 nhóm chỉ cảnh báo và 1 quy tắc minh họa. Đây là số nhóm chức năng, không phải số lỗi đã sửa trong tài liệu của bạn.
- C01–C13: phạm vi, điều kiện và giới hạn; các mục thủ công và quy tắc con-đo minh họa được tách rõ. Chưa có bộ chính tả tiếng Việt đầy đủ.
- Các mốc đã làm từ chuỗi sửa ba Line đến số/ngày, phông, vị trí, lưu/rollback, lặp cơ học và báo cáo.
- Kết quả kiểm thử phù hợp phiên bản từ `build-checks.json` đi kèm gói. Workflow lấy số thực tế từ log kiểm thử đã đạt; không nhập số dự kiến vào báo cáo. Thiếu/sai phiên bản/sai định dạng thì báo chưa có thông tin, không tự ghi PASS.
- Giới hạn còn lại và thứ tự đã thống nhất: thể thức/độ ổn định → lỗi chữ/lặp cơ bản → sau cùng ngữ cảnh và thư viện. Có bảng cách kiểm riêng/nhóm/quét/Undo/lưu-mở.

CI không có Word. Báo cáo giữ rõ mốc người dùng xác nhận ba Line ở .27, không chuyển thành xác nhận mọi chức năng .28–.41 đã chạy đạt trên Office 2021.

## Giữ tài liệu và thao tác hiện tại

Xem/xuất báo cáo không gọi Word, không quét tài liệu, không đưa chữ/tên tệp tài liệu đang mở vào nội dung báo cáo, không đổi Changed, checkpoint hoặc stack Undo. Lỗi xuất được xử lý trong cửa sổ báo cáo; không xóa danh sách lỗi/lựa chọn của cửa sổ chính.

Xuất TXT dùng cơ chế stage/kiểm byte/thay tên đã có, chặn ghi vào nguồn/bản làm việc/Undo kể cả alias được xác minh. Đích lỗi/khóa giữ tệp cũ và trạng thái tài liệu. Chỉ dọn tệp nguồn báo cáo do lượt xuất tạo; lỗi dọn I/O/quyền ghi log riêng. Không thay thuật toán sửa chữ/phông/lặp, nhận diện/nét XPS hoặc rollback cũ.

## Kiểm thử và trạng thái

Core kiểm danh mục, số nhóm, dữ liệu kiểm thử đúng/sai/thiếu phiên bản, số thực tế được render và thứ tự ưu tiên. WPF kiểm cửa sổ chỉ đọc, tổng nhóm, xuất UTF-8 trước khi mở Word, thay báo cáo, đích khóa, alias checkpoint, đuôi tệp, không thêm Undo và dọn stage. Một bước CI riêng đọc thông tin trong thư mục Windows đã publish và tạo báo cáo mẫu bằng đúng renderer của ứng dụng trước khi đóng ZIP.

Office check thêm xuất khi Word có thay đổi chưa lưu: giữ toàn chữ, Changed, checkpoint, nguồn và lượt Undo trước đó. Kịch bản này có trong chương trình nhưng **chưa có bằng chứng 0.1.41 chạy đạt trên Office 2021**. Giữ main, stable .27 và mọi tag/gói cũ.
