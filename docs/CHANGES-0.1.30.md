# 0.1.30 — Khôi phục lượt sửa chữ khi thất bại (bản thử)

Tiếp nối tag 0.1.29, commit `593e7313db781eb1c3233e44987a8633b3d953bc`. Gom phần an toàn của sửa chữ/số/ngày, lưu, kiểm kết quả và lịch sử Hoàn tác trong một đợt. Không mở thêm nhóm thể thức mới. Mốc ổn định 0.1.27 và mọi tag/gói cũ giữ nguyên.

## Vấn đề đã xử lý

Trước đây nhánh sửa chữ có thể dừng sau vài thay đổi và yêu cầu tự Hoàn tác; bước lưu cuối nằm ngoài xử lý lỗi. Checkpoint đã được đẩy vào lịch sử ngay cả khi lượt sửa thất bại. Đây là vấn đề tìm thấy qua rà soát mã và kiểm thử gây lỗi, chưa phải kết luận đã tái hiện trên máy Office của người dùng.

Nay giao dịch:

1. Kiểm nguyên văn của toàn bộ lựa chọn trước ghi; từ chối vùng chồng nhau, gợi ý thủ công và gộp chữ với Line.
2. Dựng chữ dự kiến từ tiền tố thật của Word, không dùng CP làm chỉ số chuỗi. Ghi theo thứ tự vị trí giảm dần, đọc lại range sau mỗi bước.
3. Lưu và đối chiếu toàn bộ chữ với kết quả dự kiến, gồm chữ ngoài vùng sửa. Chỉ khi đạt mới báo hoàn tất và thêm một checkpoint vào Hoàn tác.
4. Khi lỗi giữa lượt, lỗi lưu hoặc hậu kiểm không đạt, phục hồi DOCX trước lượt sửa và kiểm lại chữ. Lượt thất bại không chiếm mất lượt Hoàn tác thành công trước đó.
5. Nếu phục hồi cũng lỗi, giữ checkpoint/thư mục làm việc và cả hai lỗi để phục hồi; không tự thử ghi lại, không báo đã khôi phục khi chưa xác minh được.

Undo của ứng dụng tiếp tục dùng checkpoint; nhánh chữ bỏ phụ thuộc Word UndoRecord. Các bài Office cũ sửa riêng/nhóm, lưu/mở và Undo vẫn được giữ. Giao dịch áp dụng cả gợi ý định dạng tiêu đề; kiểm sau lưu ở bản này xác minh chữ, chưa tự chứng minh mọi thuộc tính định dạng.

## Những phần giữ nguyên

Giữ các sửa số/ngày và ô trống từ 0.1.28–0.1.29. Không tự điền giá trị, chọn mã cơ quan hoặc địa danh. Giữ đường kẻ, kiểm marker DOCX, nét XPS thực tế, dòng tham chiếu, giới hạn bù vị trí và rollback Line. Không kết luận thành công chỉ từ số Shape, số cảnh báo hoặc XML đã ghi.

## Kiểm thử và cách thử

Core có kiểm gây lỗi sau từng bước, đọc lại sai, nguồn thay sau preflight, lỗi lưu, thay đổi ngoài phạm vi khi hậu kiểm, rollback lỗi; có kiểm ghép giao dịch với thao tác đổi checkpoint trên tệp giả. Kiểm các vùng không an toàn không được ghi, không phát lại thao tác và không mất hai nguyên nhân khi phục hồi thất bại.

Workflow Windows chạy toàn bộ Core/OpenXML, WPF và publish trước khi phát hành. Xem CI/PR theo đúng commit. CI không có Word và không thay thế xác nhận Office 2021.

Giải nén gói mới, giữ `samples`, chạy **Kiểm tra → Kiểm tra Word tự động…**. Bài này dùng phiên Word riêng với mẫu giả, cố ý gây lỗi sau bước đầu và bước cuối của nhóm số/ngày, kiểm phục hồi, giữ Undo của lượt trước và sửa tiếp thành công. Kết quả PASS chỉ có ý nghĩa khi chạy thật; các bước mới hiện còn chờ máy có Word. Trong log có thể xuất hiện `Word.TextBatch.RolledBack` do lỗi có chủ đích của bài kiểm, cần xem kết quả `office-check.txt` và đúng bước.

Khi thử giao diện, dùng bản sao tài liệu, kiểm sửa riêng/nhóm số/ngày, một Undo cho cả lượt, lưu/mở lại và sửa ba Line như trước. Nếu lượt sửa lỗi nhưng đã phục hồi, quét lại trước khi sửa tiếp. Chưa nhập main hoặc đánh dấu ổn định trước bằng chứng dùng thử. Chi tiết tiếp tục phát triển ở [DEVELOPMENT.md](DEVELOPMENT.md).
