# 0.1.55 — Cơ quan, trích yếu, nơi nhận và hậu kiểm nhiều nhóm

Bao gồm toàn bộ [sửa nhận diện và C22/C23 của .54](CHANGES-0.1.54.md): trích yếu nhiều đoạn/ngắt dòng, Kính gửi căn giữa, tên cơ quan đúng vai trò, nơi nhận 9b không bị nhầm thành mục thân bài, thông tin phạm vi XML tách khỏi lỗi dòng đầu.

Bổ sung hậu kiểm phông khi nơi nhận nằm sau đoạn 40 được sửa cùng đường kẻ: dùng toàn bộ các đoạn đã khớp XML/range và đủ điều kiện sửa phông. Trước đây hậu kiểm nhiều nhóm chỉ tìm trong danh sách dùng cho hình học đầu trang, nên một đoạn recipient dài có thể không được tìm lại dù có bằng chứng chữ. Không đoán chỉ số chuỗi từ CP, không mở qua field/control/link hoặc vùng chưa có proof; mất bằng chứng sau mở lại vẫn phục hồi cả lượt.

Không đổi phạm vi đo Line/căn giữa, ngưỡng hình học, điều kiện kiểm nét XPS, một Undo/rollback hoặc nội dung tài liệu gốc. Không thêm loại lỗi so với .54: 23 đề xuất/8 thủ công/1 minh họa, có ví dụ C22/C23 và nhật ký tiếp tục phát triển.

Core thêm mẫu recipient sau hơn 40 đoạn; bài Word giả thêm sửa chung recipient dài và Line, quét và một Undo. Windows CI chạy Core/OpenXML, WPF và kiểm báo cáo trong gói, số thực nằm trong build-checks.json/TXT. CI không có Word; bài Word mới chỉ được biên dịch, chưa có xác nhận runtime Office 2021.

Kiểm lại ba thành phần trong file người dùng: đúng trong phạm vi đã kiểm thì không có cảnh báo lỗi, sai phông/cỡ/kiểu/dấu có đề xuất riêng. Thử thêm văn bản dài, chọn font recipient cùng Line, một Hoàn tác, lưu/mở; giữ ngắt dòng và các lỗi chưa chọn. Khi thật sự thiếu proof, thông tin phạm vi vẫn hiển thị riêng và không quy lỗi cho dòng đầu.

.54 là mốc kiểm nội bộ đã CI đạt; không ghi đè tag/gói. Gói gửi người dùng là .55. Main và stable .27 giữ nguyên. [Danh mục và ví dụ](SUPPORTED-FIXES-0.1.55.md).
