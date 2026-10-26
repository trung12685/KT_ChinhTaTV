# Chính tả Việt 0.1.70 — tích lũy thể thức văn bản, chưa phát hành

Thay đổi trong mã, chưa tạo gói Windows cho riêng bước này. Tiếp nối 0.1.69 (`7f16a0b599a0e627203941b87af3f790c2c45c73`). Mốc .67 và mọi tag/gói cũ giữ nguyên.

## Kết quả người dùng cung cấp

| Bản/chế độ | Quét | Dựng trang | Cộng hai bước | Tổng mở báo cáo |
|---|---:|---:|---:|---:|
| .68 | 3,5 s | 1,4 s | 4,9 s | Chưa có |
| .69 bật thử nghiệm | 5,0 s | 1,8 s | 6,8 s | 12,9 s |
| .69 tắt thử nghiệm | 2,1 s | 1,2 s | 3,3 s | 6,1 s |

Số đo báo cáo của file đang so cho thấy tắt thử nghiệm tốt hơn rõ rệt: quét/dựng trang giảm khoảng51,5% so với bật. Chưa có số lượt lặp, điều kiện lạnh/ấm hoặc log tách chặng, nên chưa gán nguyên nhân cho một tính năng riêng và không coi tổng mở là phép so khởi động Word có kiểm soát. Các số đã làm tròn.

## Đã sửa

- WordSession mới mặc định UseOptimizedReading=false; checkbox giao diện cũng mặc định bỏ tích.
- Đổi nhãn thành **Kiểm tra → Thử bộ đọc mới (đang đánh giá)**, không gọi là “nhanh” hoặc “tối ưu”. Trạng thái dùng cùng cách gọi trung tính.
- Giữ bộ đọc mới ở chế độ tự bật để nghiên cứu; không xóa source thử hoặc thay các tag đã có.
- Giữ nguyên bảng, Preview Word/XPS, luật/chức năng, editor, kiểm nét thực tế, checkpoint, hậu kiểm, Undo và rollback. Không cắt phép kiểm để lấy số nhanh hơn.
- Thêm kiểm WPF xác nhận backend và checkbox cùng tắt khi khởi động, không cần khởi động Word để kiểm cấu hình này. Chạy bộ kiểm hồi quy trên Windows đúng commit; kết quả theo CI, không phải chứng nhận Office2021.

## Cách dùng ngay

Người dùng đang có .69 chỉ cần **tiếp tục để bỏ tích tùy chọn thử nghiệm**. Không cần tải bản mới để đạt trạng thái này. Khởi động lại .69 vẫn có thể bật sẵn theo thiết kế cũ, nên kiểm tra lại checkbox; mã .70 sửa mặc định cho lần khởi động mới. Chưa phát hành .70 cho riêng thay đổi này; tạo gói theo chu kỳ đã thống nhất hoặc khi có yêu cầu cần bản tải.

Mốc quay lại: [0.1.67](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.67). Ghi chú lịch sử trước khi người dùng yêu cầu dừng tối ưu: hướng nghiên cứu khi đó dựa trên stage timings/counters thực tế: đọc đoạn, đọc gộp/fallback, đọc định dạng, phân trang, gắn vị trí và xuất XPS. Chưa thêm một thuật toán tăng tốc khác dựa vào số getter mô hình. Danh mục giữ nguyên30 đề xuất/8 thủ công/1 minh họa; các giới hạn Office2021 còn mở giữ nguyên.

## Đợt thể thức 01 — khối ký (26/10/2026)

Theo yêu cầu mới, dừng tối ưu tốc độ đọc. Giữ bộ đọc cũ mặc định và chuyển sang thể thức trước, chữ cơ bản/khoảng trắng/dấu câu/lặp sau, ngữ cảnh và thư viện cuối cùng.

Thêm **C31 — Phông/cỡ chữ khối ký**: nhận diện quyền hạn/chức vụ và họ tên trong bảng kết thúc, có Nơi nhận/Lưu phía trái. Kiểm Times New Roman, đen, cỡ13–14, đứng, đậm theo NĐ30 PL I.I.4 và II.7.đ, PDF21,26–27. Chỉ thuộc tính sai mới được đề xuất sửa. Không ép đồng loạt về13; giữ từng cỡ13/14 đúng. Bảo toàn nguyên văn, cấp bậc, chữ hoa/thường, ngắt dòng mềm, gạch chân và căn/thụt/khoảng đoạn. Ảnh/chữ ký/con dấu ở đoạn riêng nằm ngoài vùng ghi.

Nhóm **Chữ ký** trong Bắt buộc sửa có chọn riêng/chọn nhóm/chọn chung các nhóm khác. Tooltip ba dòng ngắn; danh sách dựng mới sau sửa, không giữ lỗi đã hết dưới dạng checkbox vô hiệu. Unknown không bị gọi là lỗi. Bảng lồng/gộp/control, trường Word trong mốc tên, danh sách tự động, họ tên mẫu trống, chức vụ lạ hoặc có nội dung/phụ lục sau bảng chưa mở sửa.

Bổ sung kiểm nhận diện, từng loại sai thật, kiểu/cỡ trộn hợp lệ, stale payload, giữ cỡ/gạch chân/bố cục, chọn nhiều nhóm và rollback. Luồng Word hiện có vẫn phải quét lại trước ghi, checkpoint, hậu kiểm sau lưu/Line/XPS và một Undo. Bài Word bổ sung mẫu giả có ảnh PNG, sửa riêng/chung ba Line, lỗi giữa lượt, Undo, lưu-mở và hash nguồn/ảnh. CI chỉ biên dịch bài này; không phải bằng chứng Office2021 chạy đạt.

Báo cáo trong ứng dụng/TXT cập nhật31 nhóm đề xuất/8 thủ công/1 minh họa, kèm ví dụ C31 và giới hạn. Đây là một nhóm tính năng có nhiều loại thuộc tính được sửa, không tính mỗi test/tài liệu là một tính năng. Tiếp tục tích lũy trước khi tạo Windows theo nhịp5–10 bước đã thống nhất. Chi tiết: [FORMAT-BATCH-01.md](FORMAT-BATCH-01.md).
