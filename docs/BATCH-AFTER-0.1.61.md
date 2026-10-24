# Đợt phát triển sau 0.1.61

Mốc v0.1.61 `1a53d2c6f304adc913c139fb4e4f3d3b03d2db46`, đã phát hành ZIP/TXT/SHA-256: 2435 Core/OpenXML +144 WPF +1 kiểm metadata gói. Gói .61, main và stable .27 giữ nguyên. Đợt mới code **0.1.62**, chưa tạo tag/gói; gom khoảng5–10 bước hữu ích rồi đóng gói theo nhịp người dùng yêu cầu. Tính năng cùng tests/docs là một bước, không đếm theo số commit.

## B01 — Sửa cỡ hỗn hợp mà giữ phần đã đúng

Hoàn thiện cả luồng nhận diện → đề xuất → duyệt/sửa chung → đo lại → hậu kiểm. Trước đây vùng có cỡ sai trộn cỡ đúng khác đích phải giữ thủ công; nay CharacterFormat của cơ quan/tên loại/trích yếu/ngày cho sửa riêng các ký tự sai cỡ. Ví dụ cơ quan cỡ10/12/13 →12/12/13; trích yếu12/13/14 →13/13/14. Giữ từng cỡ đúng, nguyên văn/ngắt dòng/gạch chân và các thuộc tính không yêu cầu thay. Một mục vùng nên sửa hoa/thường, khoảng trắng hoặc lặp cắt qua ranh giới cỡ vẫn cùng lượt.

Không set Size toàn vùng khi cần bảo toàn; dùng đúng Range ký tự Word, kiểm toàn text/biên trước khi ghi. Bản cỡ kỳ vọng chuẩn hóa đúng phần sai rồi chiếu qua sửa chữ; kiểm chính xác cỡ sau lưu/Line, không chỉ kiểm khoảng hợp lệ. Cỡ đúng bị ép hoặc ký tự sai không về đúng đích đều rollback toàn lượt. Lựa chọn Line tích kèm mục phông, đo lại từ XPS mới; không bỏ proof, nguyên văn, một Undo hoặc kiểm nét thật.

Giữ thủ công khi chưa đọc chắc, mất quyền XML hoặc ngoài nhận diện. Quốc hiệu/tiêu ngữ/Đảng chưa mở nhánh này; C24 tên loại có cỡ hỗn hợp vẫn có giới hạn riêng. Không tăng nhóm loại lỗi:25 đề xuất/8 thủ công/1 minh họa. Báo cáo xem/xuất TXT có ví dụ cỡ sai/đúng và ghi rõ đang phát triển .62, chưa gói.

Core/Desktop kiểm policy/role, cỡ đúng/ngoài khoảng, sai payload, Unicode/CP thực, sửa chung chữ, stale, đọc/ghi COM lỗi, hậu kiểm drift/rollback, checkbox phụ thuộc và glyph/nét mới. Fixture Word mở rộng mẫu đúng/sai cỡ hỗn hợp và lưu/mở/Undo/rollback/băm nguồn, CI chỉ biên dịch. Kết quả Windows đúng commit ghi tại PR và workflow checks; không lấy số kiểm cục bộ thay cho CI. Chưa xác nhận Office2021 hoặc khép phản hồi .56.

[Thiết kế và cách thử](SELECTIVE-SIZE-REPAIR.md). Bước B01 đã hoàn thành kiểm bắt buộc: commit triển khai `3a9d2267b6bf060c60e4e5f261bd665b55313c0d` đạt **2471 Core/OpenXML +163 WPF**, build ứng dụng thành công, không có warning/error biên dịch trong log. [CI đúng commit](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40074445118) · [PR38 và kết quả head mới nhất](https://github.com/trung12685/KT_ChinhTaTV/pull/38). Runner cục bộ chọn lọc đạt2436 lượt assertion, không thay cho tổng CI hoặc Word PASS. Khi chốt B01 có **1 bước hoàn tất**, chưa đóng gói/tag .62. Các commit chốt tài liệu không tính thêm bước.

## B02 — Quốc hiệu/tiêu ngữ giữ nguyên chữ, chỉ sửa cỡ sai

Nối B01 sang HeadingFormat thay vì mở một đường ghi khác. Trong quốc hiệu đã đúng chữ, cỡ10/12/13 →12/12/13; tiêu ngữ12/13/14 →13/13/14. Giữ nguyên HÒA/HOÀ, dấu -/– được chấp nhận, mỗi cỡ đúng và toàn text. Có thể sửa căn giữa cục bộ trong ô theo lựa chọn đã có; không suy ra vị trí toàn trang. Chỉ opt-in khi chữ khớp đúng; cỡ trộn kèm chữ cần thay vẫn thủ công, Đảng cỡ15 cố định giữ route riêng. Khi nội dung đúng, proof định dạng qua Line của ứng dụng đã xác minh vẫn cho sửa font, không mở TextEditable.

BatchPlan chặn cờ thiếu khoảng/nhầm vai trò/thay chữ; hai loại format gọi cùng writer Range ký tự thực. Bản cỡ kỳ vọng chỉ chuẩn hóa phần sai; hậu kiểm trước/sau lưu và sau Line giữ từng cỡ đúng. Line trong ô tiêu ngữ tích kèm đúng hai mục quốc hiệu/tiêu ngữ, đo mới trước ghi/đo lại, không dùng số Shape làm bằng chứng. Bất kỳ lỗi đọc/ghi/nét/cỡ đều vào rollback cả lượt và một Undo cũ.

Core kiểm các biến thể chữ/dấu, cỡ sai/đúng, quyền XML, stale, payload giả và drift; thêm trường hợp dùng drawing thật của DocxLineEditor. Desktop kiểm chung writer, COM read/write failures và glyph/nét tiêu ngữ sau đổi cỡ. OfficeHeadingSizeCheck dùng mẫu giả đã có ba Line, giữ drawing trong run gộp; sửa riêng/nhóm, lỗi sau nhóm phông, một Undo, lưu/mở và băm nguồn. Đọc cỡ ở paragraph hiện tại đã proof, không đoán CP từ text trước mở lại. Bài Word mới chỉ biên dịch trong CI, chưa xác nhận Office2021 hoặc khép phản hồi .56.

B01–B02 là **2 hạng mục** trong code .62, vẫn25 đề xuất/8 thủ công/1 minh họa. Báo cáo TXT và giao diện có ví dụ C02/C03 cập nhật. Kết quả kiểm đúng commit và trạng thái hoàn thành ghi tại [PR38](https://github.com/trung12685/KT_ChinhTaTV/pull/38)/Windows checks; runner cục bộ chọn lọc không thay CI/Word. Chưa tạo gói/tag .62; gom thêm bước theo chu kỳ người dùng yêu cầu.

## Việc tiếp theo

Tiếp tục các vùng thể thức còn thiếu khi có nhận diện/căn cứ rõ, mở rộng lỗi chữ có ngoại lệ và giảm thao tác Word trùng khi có bằng chứng. Phần từ điển/chính tả đầy đủ và ngữ nghĩa vẫn theo giai đoạn sau; không coi chức năng cỡ chữ là hoàn thành các phần này. Chỉ tạo Windows sau đủ đợt và kiểm bắt buộc đạt.
