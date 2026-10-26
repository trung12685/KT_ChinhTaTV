# Chính tả Việt0.1.71 — sửa đọc file Word đang mở trong bài kiểm

Log người dùng gửi ghi0.1.69, Word16.0 Build16.0.20326, dừng ở “So sánh hai cách đọc: bảng, định dạng và nét XPS”. Nguyên nhân cụ thể là IOException0x80070020 tại File.ReadAllBytes(working checkpoint) trong OfficeReadPipelineCheck.Compare, trước khi so sánh kết quả. Cùng đoạn mã vẫn còn ở0.1.70. Word đang giữ handle ghi; File.ReadAllBytes chỉ chia sẻ đọc nên xung đột quyền chia sẻ trên Windows. Đây chưa phải bằng chứng hai bộ đọc trả kết quả khác nhau.

## Đã sửa

- Tính SHA256 của checkpoint đang mở bằng handle chỉ đọc, chia sẻ ReadWrite/Delete tương thích handle Word. Vẫn băm mọi byte và so trước/sau; thay đổi thật, khóa độc quyền hoặc file mất vẫn thất bại. Không đóng/lưu/sửa tài liệu để lấy mã băm. Thêm kiểm Windows tái hiện chính xác0x80070020 bằng handle ghi kiểu Word, rồi kiểm cách mới đọc được và không làm đổi dữ liệu/trạng thái.

- Kiểm tra Word dùng đúng bộ đọc đang chọn. Mặc định bộ đọc cũ; chạy đầy đủ các bài chức năng, ghi MODE Legacy/NOT_RUN và thông báo rõ chưa kiểm bộ đọc thử nghiệm. Không coi thử nghiệm chưa chạy là PASS của thử nghiệm.
- Nếu chủ động bật thử nghiệm, chạy các bài chức năng bằng chế độ đó rồi bắt buộc so sánh hai bộ đọc. Sai khác hoặc ngoại lệ vẫn làm lượt kiểm FAIL.
- Log ghi thêm nguyên thông báo ngoại lệ, mẫu/reader/pha lỗi. Khi có hai snapshot, lưu JSON và chỉ rõ trường khác đầu tiên; giữ phép so sánh nghiêm ngặt, kể cả khác biệt toạ độ rất nhỏ. Không làm tròn/nới điều kiện để vượt bài.
- Giữ nguyên các luật sửa văn bản33/8/1, bảng/ảnh/Preview, kiểm nét thực tế XPS, checkpoint, rollback và một Undo. Không tiếp tục tối ưu tốc độ hoặc coi lỗi khóa file là lỗi sai khác của bộ đọc. Sửa cơ chế đọc mã băm; vẫn cần chạy lại Word thật để xác nhận toàn bộ.

## Cách kiểm

Giải nén vào thư mục mới, kiểm tiêu đề0.1.71. Để bỏ tích **Kiểm tra → Thử bộ đọc mới (đang đánh giá)**, rồi chạy **Kiểm tra Word tự động…**. Nếu lỗi, giữ office-check.txt và diagnostics.log; bản mới ghi Message và chi tiết pha lỗi. Nếu cố ý thử bộ đọc mới, kèm thư mục read-pipeline có comparison-xx-legacy.json và comparison-xx-experimental.json.

Các bài chức năng vẫn phải đạt; kết quả ĐẠT chỉ áp dụng mẫu và bộ đọc được ghi trong báo cáo. **Chưa xác nhận Office2021 chạy đạt0.1.71.** Kết quả Core/WPF/metadata của đúng commit được pipeline ghi tại release, build-checks.json và TXT; CI không chạy Word.

[Hướng dẫn kiểm](https://github.com/trung12685/KT_ChinhTaTV/blob/v0.1.71/docs/OFFICE-CHECK-0.1.71.md) · [Danh sách33 nhóm và ví dụ](https://github.com/trung12685/KT_ChinhTaTV/blob/v0.1.71/docs/SUPPORTED-FIXES-0.1.71.md). Giữ nguyên bản0.1.70 và mốc0.1.67. Tiếp tục ưu tiên thể thức, sau đó chữ cơ bản/khoảng trắng/dấu câu/lặp; ngữ cảnh/thư viện sau cùng.


Bước “Đọc mã băm checkpoint khi Word đang mở” là bắt buộc ở cả bộ đọc cũ và thử nghiệm: đọc băm trước/sau quét, so chữ/Changed/Undo. Tắt phép so sánh tùy chọn không được làm mất phép tái kiểm lỗi khóa file; khóa còn tồn tại hoặc bằng chứng thay đổi vẫn RESULT FAIL.
