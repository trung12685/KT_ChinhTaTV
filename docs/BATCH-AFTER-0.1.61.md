# Đợt phát triển sau 0.1.61

Mốc v0.1.61 `1a53d2c6f304adc913c139fb4e4f3d3b03d2db46`, đã phát hành ZIP/TXT/SHA-256: 2435 Core/OpenXML +144 WPF +1 kiểm metadata gói. Gói .61, main và stable .27 giữ nguyên. Đợt mới code **0.1.62**, chưa tạo tag/gói; gom khoảng5–10 bước hữu ích rồi đóng gói theo nhịp người dùng yêu cầu. Tính năng cùng tests/docs là một bước, không đếm theo số commit.

## B01 — Sửa cỡ hỗn hợp mà giữ phần đã đúng

Hoàn thiện cả luồng nhận diện → đề xuất → duyệt/sửa chung → đo lại → hậu kiểm. Trước đây vùng có cỡ sai trộn cỡ đúng khác đích phải giữ thủ công; nay CharacterFormat của cơ quan/tên loại/trích yếu/ngày cho sửa riêng các ký tự sai cỡ. Ví dụ cơ quan cỡ10/12/13 →12/12/13; trích yếu12/13/14 →13/13/14. Giữ từng cỡ đúng, nguyên văn/ngắt dòng/gạch chân và các thuộc tính không yêu cầu thay. Một mục vùng nên sửa hoa/thường, khoảng trắng hoặc lặp cắt qua ranh giới cỡ vẫn cùng lượt.

Không set Size toàn vùng khi cần bảo toàn; dùng đúng Range ký tự Word, kiểm toàn text/biên trước khi ghi. Bản cỡ kỳ vọng chuẩn hóa đúng phần sai rồi chiếu qua sửa chữ; kiểm chính xác cỡ sau lưu/Line, không chỉ kiểm khoảng hợp lệ. Cỡ đúng bị ép hoặc ký tự sai không về đúng đích đều rollback toàn lượt. Lựa chọn Line tích kèm mục phông, đo lại từ XPS mới; không bỏ proof, nguyên văn, một Undo hoặc kiểm nét thật.

Giữ thủ công khi chưa đọc chắc, mất quyền XML hoặc ngoài nhận diện. Quốc hiệu/tiêu ngữ/Đảng chưa mở nhánh này; C24 tên loại có cỡ hỗn hợp vẫn có giới hạn riêng. Không tăng nhóm loại lỗi:25 đề xuất/8 thủ công/1 minh họa. Báo cáo xem/xuất TXT có ví dụ cỡ sai/đúng và ghi rõ đang phát triển .62, chưa gói.

Core/Desktop kiểm policy/role, cỡ đúng/ngoài khoảng, sai payload, Unicode/CP thực, sửa chung chữ, stale, đọc/ghi COM lỗi, hậu kiểm drift/rollback, checkbox phụ thuộc và glyph/nét mới. Fixture Word mở rộng mẫu đúng/sai cỡ hỗn hợp và lưu/mở/Undo/rollback/băm nguồn, CI chỉ biên dịch. Kết quả Windows đúng commit ghi tại PR và workflow checks; không lấy số kiểm cục bộ thay cho CI. Chưa xác nhận Office2021 hoặc khép phản hồi .56.

[Thiết kế và cách thử](SELECTIVE-SIZE-REPAIR.md). Kết quả kiểm đang được hoàn tất trước khi chốt bước.

## Việc tiếp theo

Tiếp tục các vùng thể thức còn thiếu khi có nhận diện/căn cứ rõ, mở rộng lỗi chữ có ngoại lệ và giảm thao tác Word trùng khi có bằng chứng. Phần từ điển/chính tả đầy đủ và ngữ nghĩa vẫn theo giai đoạn sau; không coi chức năng cỡ chữ là hoàn thành các phần này. Chỉ tạo Windows sau đủ đợt và kiểm bắt buộc đạt.
