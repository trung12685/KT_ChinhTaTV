# Chính tả Việt 0.1.67 — mốc lưu trước thử nghiệm tăng tốc

Đóng gói theo yêu cầu người dùng để giữ một bản có thể quay lại trước khi thử phương án tối ưu đọc DOCX. Nguồn chức năng là bản 0.1.65, commit `56891d501a32f183ba6154e9a5829b2cd65c34e4`. Chọn số 0.1.67 làm tên mốc; không có bản chức năng 0.1.66 trung gian trong đợt này.

## Nội dung được giữ nguyên

- Toàn bộ thuật toán kiểm/sửa và danh mục **30 đề xuất sửa, 8 nhóm thủ công, 1 quy tắc minh họa** của 0.1.65.
- Nội dung trong bảng, bản xem trước Word/XPS và cách đọc/quan sát hiện tại. Không thay bằng preview chữ thuần hay bộ đọc Python.
- Sửa riêng, theo nhóm và nhiều nhóm; quét/đo lại theo thứ tự phụ thuộc, xác minh nét kẻ thực tế, checkpoint, một Undo, rollback cả lượt và lưu bản sao.
- Các điều kiện bảo vệ nguồn, định dạng, ô số/ngày trống, vùng XML chưa chứng minh và phần chưa chọn.

Chỉ đổi số phiên bản, thêm mốc trong báo cáo Trợ giúp/TXT và tài liệu/metadata đóng gói. **Chưa áp dụng tối ưu DOCX/COM, cache, chạy nền hay thuật toán mới.** Các giới hạn và lỗi còn mở của bản nền vẫn được ghi nhận; không gọi đây là chứng nhận đã hết mọi lỗi hoặc đã đạt Office2021.

## Những chức năng hiện có

Danh sách đầy đủ kèm ví dụ tại [SUPPORTED-FIXES-0.1.67.md](SUPPORTED-FIXES-0.1.67.md). Năm hạng mục gần nhất của .65 gồm phối hợp dấu câu sau ngoặc, thiếu khoảng trước/sau, thừa khoảng trong, ngoặc kép thẳng và câu có dấu kết trong ngoặc tròn. Ví dụ:

- `Theo mẫu( nội dung nội dung )đã  duyệt;hoàn thành.` → `Theo mẫu (nội dung) đã duyệt; hoàn thành.`
- `Theo mẫu" kế hoạch "cần làm.` → `Theo mẫu "kế hoạch" cần làm.`
- `( Đã hoàn thành. )` → `(Đã hoàn thành.)`.

Lịch sử chức năng gốc: [CHANGES-0.1.65.md](CHANGES-0.1.65.md), [BATCH-AFTER-0.1.64.md](BATCH-AFTER-0.1.64.md).

## Giữ và quay lại mốc này

1. Tải và giữ ZIP `ChinhTaViet-0.1.67-win-x64.zip`, `SHA256.txt`, TXT tiến độ và mã nguồn tại tag `v0.1.67`. Giải nén vào thư mục riêng, xác nhận tiêu đề 0.1.67.
2. Các thử nghiệm hiệu năng sau phải dùng nhánh và phiên bản riêng, không ghi đè tag hoặc asset này. Nếu chưa hiệu quả, chạy lại bản trong thư mục 0.1.67; mã nguồn có thể lấy lại đúng tag.
3. So sánh trên các bản sao giống nhau của DOCX. Việc quay lại phiên bản ứng dụng không tự hoàn tác những tài liệu đã lưu từ phiên bản thử nghiệm; giữ bản DOCX gốc riêng.
4. Trước khi kết luận hiệu quả, đối chiếu đủ dữ liệu trong bảng, Preview, danh sách lỗi, kết quả sửa, hậu kiểm và hoàn tác; tách thời gian hoàn tất thật khỏi thời gian hiện kết quả đầu tiên.

## Kiểm chứng

Tệp `BASELINE-0.1.67.json` ghi commit nền và SHA-256 các tệp source/test không thay đổi. `DevelopmentReport.cs` chỉ thêm lịch sử mốc; project chỉ đổi version. Windows pipeline chạy Core/OpenXML, WPF, publish self-contained và kiểm metadata báo cáo trong gói, rồi bổ sung số kiểm/commit/run thực tế tại release, TXT và `build-checks.json`.

CI không chạy Microsoft Word. Bài Word/Office2021, đặc biệt phản hồi định dạng cơ quan/trích yếu trước đây, chưa có bằng chứng mới để khép lại. Mốc này dùng để lưu và đối chiếu, không phải tuyên bố runtime đã đạt. Main, mốc .27 và các gói/tag cũ giữ nguyên.
