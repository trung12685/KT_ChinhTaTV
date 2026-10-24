# Đợt phát triển sau 0.1.62

Mốc nền: `68865ca574b3c32f170b378080e6663287d036cd`, tag `v0.1.62`. Bản nền đạt 2882 Core/OpenXML + 206 WPF + 1 kiểm metadata gói; chưa xác nhận Word runtime. Main và stable .27 giữ nguyên.

Phiên bản code: **0.1.63 đang phát triển**, chưa có gói Windows .63. Sau khoảng 5–10 hạng mục hoàn chỉnh mới chuẩn bị phát hành theo yêu cầu người dùng. [Windows gần nhất .62](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.62).

## B01 — phông, màu, cỡ đoạn văn xuôi thường

- C26 mới: Times New Roman, đen, cỡ 13–14 theo NĐ30 PL I.I.4 và II.6.e. Ví dụ: đoạn văn xuôi thường có Arial đỏ cỡ 12 xen chữ cỡ 14 → Times New Roman đen, chỉ cỡ 12 về 13, giữ từng cỡ 14.
- Cần văn bản Nhà nước có tên loại/trích yếu xác định, cặp quốc hiệu/tiêu ngữ trang đầu, mốc Kính gửi và chữ ký/Nơi nhận phía sau. Không suy vai trò từ cỡ chữ hoặc vị trí sau tiêu đề đơn thuần.
- Chỉ đoạn chữ thuần đã đối chiếu XML, văn xuôi có kết thúc rõ, không danh sách/heading/bảng/tab/field, không đậm/nghiêng. XML cung cấp căn đoạn kể cả sau đoạn 40; chỉ quan sát phông các ứng viên. Chưa đủ bằng chứng thì bỏ qua phạm vi C26, không tạo cảnh báo định dạng giả.
- Dùng writer phông/cỡ hiện có; giữ cỡ đúng, nguyên văn và gạch chân. Mở theo dõi kiểu ký tự/căn-thụt đoạn cho C26; kiểm sau lưu và sau các nhóm/Line. Chọn riêng giữ lỗi chữ/phông chưa chọn; chọn chung khoảng trắng/cụm lặp đo lại trước Line, giữ nét XPS thật và rollback/một Undo.
- Thêm Core/GUI regression và fixture Word tổng hợp để kiểm sửa riêng/chung chữ + ba Line, Undo, lưu/mở và hash nguồn. Fixture Word chỉ được biên dịch trong CI; muốn có runtime pass cần chạy trên máy có Word đúng bản.

Tổng danh mục: **26 nhóm đề xuất, 8 thủ công, 1 minh họa**; không phải số lỗi của tài liệu. Kết quả kiểm tự động theo đúng commit tại PR/Windows checks, không dùng số của bản nền thay cho lượt mới.

## Hạng mục tiếp theo

1. Mở rộng nhận diện thân bài từng trường hợp có bằng chứng, giữ các phần nhấn mạnh và cấu trúc riêng.
2. Kiểm và đề xuất căn đều/thụt đầu dòng theo II.6.e, phân biệt mục/danh sách/bảng/chữ ký.
3. Kiểm khoảng đoạn tối thiểu 6 pt và giãn dòng đơn đến 1,5 lines; giữ ngoại lệ và chưa tự đổi khi chưa nhận diện đủ.
4. Kiểm tổng thể nhiều nhóm, danh sách mới sau sửa, tiến độ, Undo/lưu-mở và Office 2021.
5. Tiếp tục lỗi chữ cơ bản có mẫu/ngoại lệ; từ điển/ngữ nghĩa sau cùng.

Chi tiết: [phạm vi C26](BODY-FORMAT.md). Báo cáo TXT trong chương trình có ví dụ, việc đã hỗ trợ và việc còn lại; code version không đồng nghĩa đã có bản tải.
