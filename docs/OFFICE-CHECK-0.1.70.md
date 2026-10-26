# Kiểm thử Chính tả Việt 0.1.70 trên Office 2021

CI kiểm Core/OpenXML/WPF, biên dịch và đóng gói ứng dụng. CI không chạy Microsoft Word. Bài Word mới đã có trong ứng dụng nhưng chưa có xác nhận chạy đạt trên Office2021; kết quả máy người dùng cần gắn đúng phiên bản/build-checks.json.

## Mở bản mới

1. Giải nén ZIP vào thư mục mới, mở ChinhTa.Desktop.exe; kiểm tiêu đề0.1.70. Không chạy exe từ trong ZIP.
2. Kiểm tra → Thử bộ đọc mới (đang đánh giá) mặc định bỏ tích. Công việc tăng tốc đã dừng; bản này tập trung thể thức.
3. Trợ giúp → Tiến độ và chức năng… có33 nhóm đề xuất,8 thủ công,1 minh họa; nút xuất TXT gồm ví dụ và việc tiếp theo. TXT/build-checks.json đi kèm ghi commit và số kiểm tự động của đúng gói.

## Kiểm tra Word tự động

Chọn **Kiểm tra → Kiểm tra Word tự động…**. Ứng dụng tạo phiên Word riêng và dùng mẫu giả trong samples, không dùng tài liệu riêng làm fixture. Để chạy xong rồi xem thông báo ĐẠT/CHƯA ĐẠT và thư mục kết quả tự mở.

Các bài thêm trong đợt0.1.70 gồm khối ký, khổ/lề, PAGE và lựa chọn chung bảy mục: khổ/lề, PAGE, màu phông cơ quan, khoảng trắng và ba Line. Kiểm số thực/nét XPS, giữ chữ/bảng/kiểu/footer, rollback khi chèn lỗi có chủ đích, một Hoàn tác và lưu/mở. Một lượt ĐẠT chỉ chứng minh các mẫu đó trên máy đó, không xác nhận mọi tài liệu.

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
