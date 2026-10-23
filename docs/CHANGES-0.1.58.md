# 0.1.58 — Viết hoa tên loại văn bản đã nhận diện

Ngày 24/10/2026 (UTC+7). Tiếp tục phần thể thức; bổ sung C24, không mở rộng thư viện/ngữ nghĩa.

## Hành vi mới

Trước đây tên loại `Báo cáo` đã được nhận diện nhưng chỉ cảnh báo cần viết hoa. Nay nếu khối đầu trang Nhà nước có cặp quốc hiệu/tiêu ngữ, tên loại duy nhất thuộc 13 tên hỗ trợ, trích yếu có ranh giới/căn giữa đã xác minh, chữ thuần và định dạng đồng nhất, chương trình đề xuất `Báo cáo` → `BÁO CÁO`. Tương tự Thông báo, Kế hoạch, Tờ trình, Quyết định, Nghị quyết, Hướng dẫn, Chỉ thị, Chương trình, Phương án, Đề án, Quy chế, Quy định.

Căn cứ Nghị định 30/2020/NĐ-CP, Phụ lục I, Phần I, mục II.5, điểm b (PDF trang 24): tên loại văn bản trình bày bằng chữ in hoa. Nguồn được đối chiếu trực tiếp; không đổi căn cứ thành Thông tư. Chỉ đổi hoa/thường tên chính xác, giữ khoảng ngoài tên, nội dung/ngắt đoạn trích yếu, phông/căn đoạn. C24 ở Bắt buộc sửa khi CanFix và vẫn cần chọn/duyệt; không thay tên loại dựa nội dung hoặc tự chọn loại văn bản.

Chọn riêng C24 giữ nguyên gợi ý phông chưa chọn. Chọn cả C24 và phông dùng một giao dịch: định dạng trước, đổi chữ sau, hậu kiểm đúng chữ/phông. Một Hoàn tác bỏ cả lượt; lỗi hậu kiểm phục hồi toàn bộ. Vùng cũ đổi vai trò hoặc mất proof trước ghi phải bị chặn. Payload C24 chỉ nhận tên đã biết và phép viết hoa cùng độ dài, không cho thay nội dung tùy ý hoặc đưa định dạng vào nhánh thay chữ.

## Giới hạn

Chỉ đầu Nhà nước trong phạm vi nhận diện hiện có; không áp cho Công văn/V/v, tên chưa hỗ trợ, tiêu đề trong thân bài, nhiều ứng viên, khối/trích yếu chưa rõ, field/control/link/drawing hoặc phông trộn/chưa đọc. Chỉ đủ quyền sửa định dạng qua Line không đủ để thay chữ. Giữ trích yếu toàn hoa ở phần thủ công để bảo toàn tên riêng/mã; chưa tự viết thường trích yếu. Chế độ Đảng/cá nhân giữ nguyên.

Không thay kiểm nét Line/XPS, popup tiến độ .57, rollback/Undo hoặc các ngưỡng hình học. Người dùng đã xác nhận lỗi cơ quan/trích yếu lặp lại trên **0.1.56**; đang chờ log/DOCX sau sửa và kết quả **0.1.57 hoặc mới hơn**. C24 là phát triển riêng, không được coi là đã khép lỗi đó. Chưa có xác nhận runtime Office 2021 cho .58.

## Kiểm chứng và thử tiếp

Core kiểm 13 tên với chữ thường/hoa thường xen kẽ, giữ khoảng ngoài tên, clearing sau quét, chặn vùng không đủ chứng cứ, không đụng tên riêng trích yếu, stale plan, payload sai, ghép phông và rollback. Bài Kiểm tra Word trên mẫu giả có thêm tên `Báo cáo`, sửa chung cơ quan/trích yếu/nơi nhận và ba Line; kiểm chữ, phông, một Undo, rollback và lưu/mở. CI Windows chỉ biên dịch bài Word, không có Word để chạy. Số kiểm thực tế trong build-checks.json/TXT.

1. Ở mẫu Nhà nước được nhận diện rõ, đổi tên loại thành `Báo cáo`: phải có một mục viết hoa tên loại; tên đã in hoa thì không có mục C24.
2. Sửa riêng C24, giữ trích yếu, phông và lỗi chưa chọn. Thử tên loại vừa sai hoa/thường vừa sai phông, chọn cả hai, quét lại không còn các mục đã đạt.
3. Một Hoàn tác phục hồi cả lượt; lưu bản sao/mở lại giữ kết quả. Không áp C24 cho cụm “báo cáo” trong câu thân bài hoặc field/control.

Báo cáo TXT thêm ví dụ **C24 — Viết hoa tên loại văn bản: Báo cáo → BÁO CÁO**; tổng 24 nhóm đề xuất, 8 thủ công, 1 minh họa. M03 tiếp tục giữ các trường hợp không đủ chứng cứ và trích yếu toàn hoa. [Danh mục .58](SUPPORTED-FIXES-0.1.58.md), [báo cáo TXT](REPORT-EXPORT.md), [nhật ký](DEVELOPMENT.md). Giữ main/stable .27.
