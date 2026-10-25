# Chính tả Việt 0.1.65 — sửa cùng cụm ngoặc, dấu câu và khoảng trắng

Gom năm hạng mục sau .64, tổng **30 đề xuất/8 thủ công/1 minh họa**. Ba hạng mục cuối được triển khai chung một lượt theo phản hồi tiến độ; không cắt kiểm tra và không tăng số nhóm giả. Bản thử, chưa xác nhận runtime Word/Office2021 hoặc toàn bộ chính tả/thể thức. Main/stable .27 và gói .64 giữ nguyên.

| Bước | Nội dung |
| --- | --- |
| B01 | C14 sửa dấu phẩy/chấm phẩy sau ngoặc kép, chọn chung với mép trong |
| B02 | C30 thêm khoảng sau dấu đóng trong ngữ cảnh chữ rõ, giữ mép trong chưa chọn |
| B03 | Thêm khoảng trước dấu mở tròn/kép khi đủ ngữ cảnh; dấu câu ngoài xử lý cùng lượt |
| B04 | Hỗ trợ quote thẳng cho trước/trong/sau/C14 và ô trống, giữ nguyên loại dấu |
| B05 | Giữ một dấu .?! cuối câu trong ngoặc tròn khi xóa mép; giữ dấu câu trong đoạn còn phát hiện sau sửa |

Ví dụ chọn đủ nhóm:

- `Theo mẫu( nội dung nội dung )đã  duyệt;hoàn thành.` → `Theo mẫu (nội dung) đã duyệt; hoàn thành.`
- `Theo mẫu" kế hoạch "cần làm.` → `Theo mẫu "kế hoạch" cần làm.`
- `( Đã hoàn thành. )` → `(Đã hoàn thành.)`.

Giữ chữ, loại dấu, phần chưa chọn và kiểu/cỡ theo luồng hiện có. Ô trống giữ độ rộng; mixed/nested/unbalanced quote, mã/hàm/header/ngày/unknown không được ép thành sửa được. Chọn riêng trước/trong/sau vẫn còn các lỗi chưa chọn có thể tích; chọn nhóm/all kết hợp các lỗi đủ bằng chứng trong một lượt. Sửa chữ/phông/đoạn → quét/đo lại → Line → hậu kiểm; một Undo hoặc rollback cả lượt khi thất bại. Không coi số Shape hoặc marker DOCX là nét đúng.

## Cách thử

1. Giải nén vào thư mục mới, xác nhận0.1.65. Mở bản sao DOCX, chọn đúng chế độ và quét.
2. Thử các ví dụ trên; chọn một cạnh, kiểm lỗi chưa chọn còn hành động; sau đó thử chọn nhóm và nhiều nhóm khác nhau. Giữ nguyên quote/dấu kết, chữ đậm/nghiêng/gạch chân và cỡ đúng.
3. Thử kèm ba Line và các nhóm thể thức thân bài đủ điều kiện; xem tiến độ, kiểm nét thực. Một Undo phải bỏ cả lượt; lưu bản sao/mở lại và giữ nguồn.
4. Mẫu `"    "`, `“    ”`, `(    )`, số/ngày trống, mã/hàm và vùng XML chưa chắc cần được bảo vệ. Không có cảnh báo không chứng minh văn bản đúng toàn bộ.
5. Chạy Kiểm tra Word tự động trên máy Office2021; nếu lỗi, giữ office-check.txt, diagnostics.log và bản xuất đúng phiên bản. Kiểm bằng mắt cơ quan/trích yếu và lỗi đã sửa không còn trong danh sách.

Kiểm Core/OpenXML/WPF và metadata gói trong CI không cài/chạy Word; fixture chỉ biên dịch. Tổng kiểm, commit và run thực tế do pipeline bổ sung tại release/build-checks.json/TXT. ZIP Windows x64 tự chứa .NET, TXT chức năng/ví dụ/việc tiếp theo, SHA256.txt, metadata và bốn ảnh đi kèm.

[Danh sách chức năng](https://github.com/trung12685/KT_ChinhTaTV/blob/v0.1.65/docs/SUPPORTED-FIXES-0.1.65.md) · [Nhật ký](https://github.com/trung12685/KT_ChinhTaTV/blob/v0.1.65/docs/BATCH-AFTER-0.1.64.md) · [Cụm sửa](https://github.com/trung12685/KT_ChinhTaTV/blob/v0.1.65/docs/DELIMITER-CLUSTER.md).
