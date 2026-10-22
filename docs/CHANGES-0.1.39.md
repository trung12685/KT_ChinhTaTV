# 0.1.39 — Giữ đúng kết quả khi dọn checkpoint lỗi (bản thử)

Tiếp nối 0.1.38, commit `b85d8d304e28e393f4fc1a7eb3f9279e05caa084`. Rà soát phục hồi phát hiện `CheckpointSwap` xóa backup trong finally mà chưa tách lỗi dọn: xóa bị khóa/từ chối có thể làm một lần mở lại thành công bị báo thất bại, hoặc che mất lỗi gốc. Với Hoàn tác, stack chưa được pop dù bản làm việc đã trở về checkpoint; lần tiếp theo có thể lặp lại cùng lượt. Chưa có báo cáo Office thực xác nhận sự cố này.

## Đã xử lý

- Khi đóng, chép và mở lại candidate thành công, lỗi I/O/quyền lúc xóa backup chỉ tạo kết quả dọn chưa xong. Giữ backup và ghi mã `Word.CheckpointCleanup.Retained`; không tự chép/mở/ghi lại thao tác đã hoàn tất.
- Hoàn tác thành công vẫn tiêu thụ đúng một lượt và giữ Changed như hành vi cũ. Lần sau đi về lượt trước, không lặp checkpoint vừa dùng chỉ vì chưa dọn được backup.
- Nếu thao tác thực thất bại, giữ đúng ngoại lệ gốc và kết quả phục hồi. Lỗi dọn được giữ riêng trong dữ liệu ngoại lệ và ghi log, không thay loại/nguyên nhân lỗi gốc. Lỗi nét XPS vẫn tới cơ chế hiệu chỉnh giới hạn hiện có.
- Phục hồi cũng thất bại vẫn ném AggregateException chứa cả hai nguyên nhân và giữ rollback backup; không dọn hoặc báo thành công.

Phạm vi sửa chỉ cleanup của backup do CheckpointSwap tạo, không phải thay mọi luồng dọn tệp tạm. Không bỏ qua lỗi đóng Word, chép candidate, mở lại hoặc hậu kiểm. Giữ nguyên xác minh chữ/phông, marker DOCX/nét XPS thật, thứ tự nhóm sửa, rollback và lưu bản sao có kiểm byte. Backup dư nằm trong thư mục tạm phiên làm việc; dọn cả thư mục vẫn theo cơ chế cũ khi đóng phiên. Nhật ký không ghi chữ, tên hoặc đường dẫn tài liệu.

## Kiểm thử và cách thử

Core kiểm thành công bình thường; lỗi xóa I/O/quyền sau mở lại; lỗi đóng/mở/chép đồng thời lỗi cleanup; giữ nguyên RenderedLineMismatchException; rollback thực thất bại; hai lượt Hoàn tác. Kiểm Windows giữ handle không cho xóa file rollback thật. Windows CI chạy toàn bộ Core/OpenXML + WPF và đóng gói; không có Word trong CI.

Giải nén vào thư mục mới, giữ `samples`, chạy **Kiểm tra → Kiểm tra Word tự động…**. Cuối bài dùng mẫu giả `heading-proof-State/heading-input.docx`, sửa quốc hiệu và tiêu ngữ thành hai lượt độc lập. Lần Hoàn tác đầu cố ý gây lỗi dọn backup: phải về đúng trạng thái sau lượt thứ nhất, giữ backup và Changed; lần Hoàn tác tiếp theo về nguyên bản; hết stack thì không thay đổi. Kiểm chữ và cảnh báo thật cùng hash nguồn. Không gây khóa/xóa tệp thật của người dùng.

**Chưa có bằng chứng 0.1.39 chạy đạt trên Office 2021.** Người dùng đang kiểm tra tổng thể; chưa ghi nhận là mọi lỗi của .38 đã đạt. Giữ main, mốc ổn định 0.1.27 và mọi tag/gói cũ; chưa chốt ổn định trước kết quả dùng thử đúng phiên bản.
