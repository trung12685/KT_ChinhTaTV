# 0.1.52 — Tooltip ngắn: Lỗi, Căn cứ, Cách xử lý

Tiếp nối 0.1.51, mỗi tooltip lỗi chỉ còn ba phần theo yêu cầu người dùng:

> Lỗi: Thiếu đường kẻ dưới cơ quan ban hành
>
> Căn cứ: Nghị định 30/2020/NĐ-CP, Phụ lục I, Phần I, mục II.2, điểm b: dưới tên cơ quan có đường kẻ ngang, nét liền, dài 1/3–1/2 dòng chữ, đặt cân đối.
>
> Cách xử lý: Kẻ đường ngang dài 95.2 pt, căn theo dòng chữ; mức tự sửa theo cấu hình (mặc định 3/7).

Chiều dài lấy từ đề xuất thật của từng lỗi. Căn cứ nêu vị trí và tóm tắt đúng nội dung quy định áp dụng; không gán khoản/điều khi quy định nằm trong phụ lục. Nguồn Nhà nước là Nghị định 30/2020/NĐ-CP, nguồn Đảng là Hướng dẫn 05-HD/VPTW ngày 27/5/2026. Phần cấu hình, quy tắc ứng dụng và yêu cầu người dùng ghi rõ nguồn riêng, không biến thành quy định pháp luật.

Lỗi thủ công vẫn hướng dẫn đối chiếu/chỉnh trong Word; lỗi Line bị chặn bởi chữ/phông nhắc sửa phần đó rồi quét lại. Nguồn chưa có bản tóm tắt giữ tham chiếu và yêu cầu đối chiếu nội dung trong Chi tiết lỗi. Số đo, ngữ cảnh và chẩn đoán dài vẫn đầy đủ ở **Chi tiết lỗi**; tên lỗi một dòng, checkbox từng mục/nhóm/phần/tất cả giữ nguyên.

Không đổi engine phát hiện/sửa, điều kiện CanFix, giao dịch, quét lại, hậu kiểm XPS, rollback, Undo hoặc lưu bản sao. Giữ 21 loại đề xuất, 8 loại thủ công, 1 minh họa cùng ví dụ TXT; báo cáo có thêm mốc .52.

## Kiểm chứng và cách thử

Windows CI chạy bộ Core/OpenXML, WPF và kiểm báo cáo trong gói trước khi đóng ZIP. `build-checks.json` và TXT đi kèm ghi số kiểm thực và commit; ảnh `issue-tooltip-preview.png` mở tooltip thật bằng dữ liệu giả. Không dùng tài liệu riêng cho ảnh hay đưa vào gói.

Thử rê chuột vào lỗi thiếu đường kẻ, lỗi đã có đường nhưng lệch, quốc hiệu/tiêu ngữ, số/ngày, khoảng trắng, từ lặp, thứ tự mục và cảnh báo thủ công. Tooltip phải gọn ba phần; mở Chi tiết lỗi vẫn thấy nguyên mô tả/số đo/ngữ cảnh. Thử chọn nhóm, sửa, quét lại và Hoàn tác như trước. Bản này chưa được xác nhận đầy đủ trên Word/Office 2021; CI không có Word, không dùng kết quả WPF thay cho kiểm Word thực tế.

Bản thử riêng, chưa nhập main; stable 0.1.27 cùng tag/gói đã chốt giữ nguyên. Xem [cách dùng danh sách](ISSUE-LIST.md), [danh mục sửa](SUPPORTED-FIXES-0.1.52.md) và [nhật ký phát triển](DEVELOPMENT.md).
