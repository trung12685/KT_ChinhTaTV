# Kiểm thử Chính tả Việt 0.1.72 trên Office 2021

CI kiểm Core/OpenXML/WPF, biên dịch và đóng gói ứng dụng. CI không chạy Microsoft Word. Bài Word mới đã có trong ứng dụng nhưng chưa có xác nhận chạy đạt trên Office2021; kết quả máy người dùng cần gắn đúng phiên bản/build-checks.json.

Phản hồi 0.1.71 đã qua khóa checkpoint; dừng ở Subject vì không có đề xuất sửa. Gói DOCX thực có nhánh DrawingML/VML tương thích; bản 0.1.72 sửa inventory và thay cặp khi cập nhật, vẫn kiểm nét XPS thực tế. Các lỗi COM cũ trong diagnostics.log nhiều ngày không được gán cho lần chạy mới nếu phiên bản/thời gian khác.

## Mở bản mới

1. Giải nén ZIP vào thư mục mới, mở ChinhTa.Desktop.exe; kiểm tiêu đề0.1.72. Không chạy exe từ trong ZIP.
2. Kiểm tra → Thử bộ đọc mới (đang đánh giá) mặc định bỏ tích. Công việc tăng tốc đã dừng; bản này tập trung thể thức.
3. Trợ giúp → Tiến độ và chức năng… có33 nhóm đề xuất,8 thủ công,1 minh họa; nút xuất TXT gồm ví dụ và việc tiếp theo. TXT/build-checks.json đi kèm ghi commit và số kiểm tự động của đúng gói.

## Kiểm tra Word tự động

Chọn **Kiểm tra → Kiểm tra Word tự động…**. Bài kiểm dùng đúng chế độ đã chọn trên menu. Mặc định bỏ tích thử bộ đọc mới: chạy các bài chức năng bằng bộ đọc cũ, log ghi MODE Legacy và NOT_RUN cho thử nghiệm; một kết quả ĐẠT chỉ áp dụng bộ đọc cũ. Các kiểm nét XPS/chữ/bảng/phông/lưu-mở/Undo/rollback vẫn phải đạt đầy đủ.

Chỉ khi chủ động tích **Thử bộ đọc mới (đang đánh giá)**, bài kiểm mới chạy chế độ thử nghiệm và bắt buộc so sánh hai cách đọc sau các bài chức năng. Không coi bỏ qua là thử nghiệm đã đạt. Khác biệt vẫn FAIL; file comparison-xx-legacy.json và comparison-xx-experimental.json trong read-pipeline cùng đường dẫn JSON khác đầu tiên giúp chẩn đoán, không tự làm tròn hoặc bỏ kiểm để vượt bài.

 Ứng dụng tạo phiên Word riêng và dùng mẫu giả trong samples, không dùng tài liệu riêng làm fixture. Để chạy xong rồi xem thông báo ĐẠT/CHƯA ĐẠT và thư mục kết quả tự mở.

Các bài thể thức tích lũy từ0.1.70 gồm khối ký, khổ/lề, PAGE và lựa chọn chung bảy mục: khổ/lề, PAGE, màu phông cơ quan, khoảng trắng và ba Line. Kiểm số thực/nét XPS, giữ chữ/bảng/kiểu/footer, rollback khi chèn lỗi có chủ đích, một Hoàn tác và lưu/mở. Một lượt ĐẠT chỉ chứng minh các mẫu đó trên máy đó, không xác nhận mọi tài liệu.

Nếu CHƯA ĐẠT, giữ **office-check.txt**, **diagnostics.log**, **build-checks.json** cùng các DOCX/XPS mẫu ở thư mục kết quả. Ghi tên bước thất bại, phiên bản Word và phiên bản ứng dụng. Không cần sửa mẫu để làm bài kiểm vượt qua.

## Đối chiếu trên bản sao tài liệu thực

| Nhóm | Thử và kết quả cần thấy |
|---|---|
| Ba đường kẻ | Chọn chung hoặc từng đường. Đường1/3 dài khoảng3/7 dòng mốc khi tự sửa; tiêu ngữ theo chiều dài dòng. Kiểm vị trí nét trên Preview; quét lại không còn lỗi đã đạt. Không tạo chồng đường không nhận diện chắc. |
| Khối ký C31 | Chức vụ/họ tên ở bảng cuối có Nơi nhận/Lưu đối diện: Arial đỏ12 → Times New Roman đen13, đứng đậm. Giữ cỡ13/14 đúng, họ tên/cấp bậc, ngắt dòng, gạch chân và ảnh/chữ ký ở ngoài vùng ghi. |
| Khổ/lề C32 | Một section dọc đơn giản, bảng vừa vùng mới, không ảnh/Shape/header/footer: giấy Letter và lề sai → A4/lề hợp lệ; giữ mỗi lề đang đúng. Bố cục đặc biệt giữ thủ công. |
| Số trang C33 | Từ hai trang, một section, header trống/PAGE-only: trang1 không số, trang2 hiện2 rồi3... Times New Roman đen13–14, đứng, căn giữa lề trên. Giữ footer cả trang đầu; một trang không bị báo thiếu số. Header/logo riêng hoặc nhiều section chưa tự sửa. |
| Nhiều nhóm | Chọn trang/PAGE/phông/chữ/Line khi từng mục có đủ điều kiện. Tiến độ giữ tổng; chỉ mục đã chọn được sửa. Sau quét, lỗi đã đạt biến mất, mục chưa chọn vẫn tích được. |
| Bảo toàn | So Preview với Word, kiểm đầy đủ nội dung các ô bảng, ảnh, footer và khối trích yếu có Enter/ngắt dòng. Một Hoàn tác phải khôi phục cả lượt; lưu bản sao/mở lại giữ kết quả. Tệp nguồn không đổi. |

Nếu bản thực có khác biệt, ghi nhóm đã tích và nguyên thông báo lỗi, giữ log và checkpoint được chỉ ra. Chưa xác minh thì không coi lỗi đã sửa thành công.

## Quay lại mốc lưu

Giữ thư mục/bản ZIP0.1.67 đã chốt và tài liệu nguồn trước sửa. [Tải lại0.1.67](https://github.com/trung12685/KT_ChinhTaTV/releases/download/v0.1.67/ChinhTaViet-0.1.67-win-x64.zip). Không ghi đè file nguồn khi thử; các bản lưu mới dùng tên riêng.


Bước “Đọc mã băm checkpoint khi Word đang mở” là bắt buộc ở cả bộ đọc cũ và thử nghiệm: đọc băm trước/sau quét, so chữ/Changed/Undo. Tắt phép so sánh tùy chọn không được làm mất phép tái kiểm lỗi khóa file; khóa còn tồn tại hoặc bằng chứng thay đổi vẫn RESULT FAIL.


Trước từng bước Agency/Subject/Motto, thư mục kết quả chứa `modern-before-<role>.docx`, `.xps` và `.json`. Đây là checkpoint đã lưu và XPS đo ở bước đó, không phải kết quả PASS. Nếu thiếu đề xuất hoặc chưa đo được, bài kiểm vẫn FAIL và ghi rõ lý do; không tự bỏ qua hoặc thêm Line mù. Gửi ZIP cả thư mục cùng diagnostics.log mới để giữ đúng cặp bằng chứng. Bộ đọc cũ tiếp tục mặc định; việc sửa tương thích Line không xác nhận tốc độ/bộ đọc thử nghiệm.
