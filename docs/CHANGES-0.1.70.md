# Chính tả Việt 0.1.70 — bộ đọc cũ làm mặc định

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

Mốc quay lại: [0.1.67](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.67). Bước nghiên cứu tiếp chỉ dựa trên stage timings/counters thực tế: đọc đoạn, đọc gộp/fallback, đọc định dạng, phân trang, gắn vị trí và xuất XPS. Chưa thêm một thuật toán tăng tốc khác dựa vào số getter mô hình. Danh mục giữ nguyên30 đề xuất/8 thủ công/1 minh họa; các giới hạn Office2021 còn mở giữ nguyên.
