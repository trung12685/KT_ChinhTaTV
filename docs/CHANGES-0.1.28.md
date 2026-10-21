# 0.1.28 — Số, ký hiệu và địa danh/ngày tháng (bản thử)

Phát triển từ `main` đã chốt 0.1.27 (`da6cf3b0999dbfb60d16ab1eae9733b0f1f20c81`). Đây là bản thử tính năng mới; 0.1.27 vẫn là mốc ổn định đã được người dùng xác nhận thao tác sửa nhóm ba đường trên Office 2021.

## Phần bổ sung

Chế độ Nhà nước: kiểm số/ký hiệu và địa danh/ngày tháng trong phần đầu trang 1 đã nhận diện chắc chắn cặp quốc hiệu/tiêu ngữ. Chỉ đề xuất sửa các đoạn chữ nhỏ trong trường rõ ràng:

- `số  :  3 / BC – ABC` → `Số: 03/BC-ABC`.
- `Địa danh  Ngày3  Tháng1  Năm2026` → `Địa danh, ngày 03 tháng 01 năm 2026`.
- Thêm 0 cho số văn bản/ngày nhỏ hơn 10 và tháng 1, 2; không bắt tháng 3–9 phải thêm 0.
- Giữ khoảng trống và dấu chấm/gạch dưới của trường chưa điền. Không lấy ngày máy tính, điền số văn bản hoặc đổi mã cơ quan/địa danh.
- Ngày không hợp lệ, năm thiếu, dòng trùng/mơ hồ, mã chữ thường hoặc mẫu không hỗ trợ được cảnh báo thủ công. Khi quan sát được định dạng, cảnh báo phông/cỡ/kiểu chữ; bản này chưa tự sửa định dạng hay vị trí hai dòng.
- Trường Word, content control, hyperlink, tab bố cục hoặc dòng chưa đối chiếu chắc chắn với XML được giữ thủ công. Không viết đè cấu trúc thông qua quy tắc khoảng trắng.

Căn cứ: NĐ 30/2020/NĐ-CP, Phụ lục I, Phần I.II.3.a–c và 4.b–c, trang PDF 22–23 trong bản nguồn đã lưu dấu kiểm ở `FORMAT-RULES.md`. Đã đọc các trang nguồn, chưa đối chiếu độc lập mọi sửa đổi pháp lý sau đó. Không xác minh mã viết tắt được cấp hoặc tên địa danh chính thức; đơn vị công an/quân đội có ngoại lệ cần đối chiếu riêng.

## Giới hạn và điều kiện giữ lại

Nhận diện tối đa 40 đoạn đầu ở trang 1, cặp tiêu đề phải nằm trong 16 đoạn đầu và duy nhất; dừng tại tên loại văn bản hoặc nội dung thường. Không dò toàn bộ phần thân như ngày ban hành, không báo thiếu trường chưa nhận diện và không hỗ trợ mọi mẫu công văn. Chưa tự căn giữa hai dòng, kiểm toàn bộ thể thức hoặc chính tả tiếng Việt.

Giữ kiểm marker DOCX, nét XPS thực tế, dòng tham chiếu, checkpoint/rollback và Undo của 0.1.27. Không kết luận đường đạt từ số Shape hoặc XML đơn thuần. Chỉ thay từng span chữ của gợi ý mới qua giao dịch sửa đang có.

## Kiểm thử và dùng thử

Bổ sung test quy tắc, biên phạm vi, năm nhuận/ngày sai, trường trống, không chồng sửa khoảng trắng, văn bản Word có cấu trúc và hai DOCX giả công khai. Workflow Windows chạy toàn bộ Core/OpenXML, WPF rồi đóng gói; xem kết quả theo đúng commit trong Actions. CI không cài Word và không chứng minh Office 2021 đã đạt.

Bài **Kiểm tra → Kiểm tra Word tự động…** giữ các bước Line, bổ sung sửa riêng số, Undo, sửa nhóm số/ngày, lưu/mở lại và Undo cả lượt trên mẫu giả. Các bước này còn chờ chạy trên máy có Word.

Giải nén vào thư mục mới, giữ `samples`. Chọn Nhà nước rồi thử `SoKyHieuNgayThang.docx` và `TruongSoNgayDeTrong.docx`: sửa riêng, sửa nhóm, quét lại, Hoàn tác một lượt, lưu bản sao/mở lại; kiểm khoảng trống còn nguyên. Thử lại ba đường trên bản sao tài liệu đã dùng với 0.1.27. Nếu lỗi, gửi log đúng 0.1.28 và `office-check.txt`; không coi việc có gói tải là bằng chứng chạy đạt.
