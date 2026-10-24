# Chính tả Việt 0.1.60 — bản thử gom 5 bước

Đợt này gom B01–B05 sau .59 theo nhịp 5–10 bước đã thống nhất, giữ kiểm thử từng bước trước khi tạo một gói Windows chung. Phần chính tả đầy đủ/ngữ nghĩa/thư viện vẫn chưa triển khai hoàn chỉnh. Bản này là prerelease; main và mốc ổn định đã chốt vẫn .27.

## Nội dung mới

| Bước | Nội dung và ví dụ | Giới hạn |
|---|---|---|
| B01 | Hoa/thường viết tắt: `TTR → TTr`, `QYĐ → QyĐ`, `CTR → CTr` | Chỉ prefix đã khớp danh mục và một tên loại tương ứng duy nhất ở đầu; giữ số/mã cơ quan. BCĐX là bổ sung người dùng. |
| B02 | `KẾ HOẠCH` nhưng `Số: 01/BC-ABC` thì cảnh báo tên loại/ký hiệu không khớp | Người dùng đối chiếu bên ghi sai; không tự đổi tên/mã. Bản sao/Công văn/unknown hoặc vùng chưa đủ không bị suy diễn. |
| B03 | `Ghi chú:nội dung` hoặc `Ghi chú :nội dung` → `Ghi chú: nội dung` | Chọn riêng trước/sau hoặc cả hai; giữ từ/khoảng chưa chọn. Loại giờ/tỉ lệ/protocol/mã/header/ngày và biên chưa rõ. |
| B04 | `Đã hoàn thành.   ` → `Đã hoàn thành.` | Chỉ ASCII spaces sau dấu .?! ở cuối đoạn đủ rõ; giữ dấu kết đoạn/ô. Không thu hẹp ô số/ngày trống hoặc đoạn chưa kết câu. |
| B05 | `Theo mẫu (nội dung)đã duyệt.` → `Theo mẫu (nội dung) đã duyệt.` | Hai từ trước, ít nhất hai từ trong và hai từ sau đủ rõ; từ ngay sau thường. Chọn riêng khoảng trong/ngoài; không suy diễn hàm/công thức/ngoặc lồng/ô trống/vùng XML chưa chắc. |

Danh mục hiện **25 nhóm đề xuất sửa, 8 nhóm thủ công và 1 quy tắc minh họa**. Báo cáo TXT trong Trợ giúp và kèm gói có ví dụ từng nhóm, phần đã làm/chưa sửa/dự kiến và bằng chứng kiểm thử đúng commit. C15/C17/C25 là quy tắc ứng dụng cần duyệt, không gán căn cứ pháp lý. Tooltip ngắn và các nhóm checkbox hiện có được giữ.

Sửa nhiều nhóm vẫn theo chữ/phông → đo lại → căn giữa → đo lại → Line, chỉ theo mục đã chọn. Kiểm nguyên văn/phông và nét XPS thật, một Undo/checkpoint và rollback cả lượt; không báo thành công từ số Shape hay lịch sử bấm sửa. Dữ liệu người dùng không đưa vào kho/gói, chỉ có mẫu giả công khai.

## Kiểm thử và cách thử trên Office 2021

Kết quả tự động thực tế được ghi trong `build-checks.json`, TXT kèm gói và thông tin CI của bản phát hành; không dùng số kiểm dự kiến. Pipeline chạy Core/OpenXML, WPF/build ứng dụng, publish Windows x64 và kiểm metadata báo cáo trong gói trước khi tạo ZIP/SHA-256.

**Chưa có bằng chứng chạy đạt toàn bộ bài Word trên Office 2021 cho .60.** Các bài Word mới đã được viết và biên dịch; CI không có Word. Lỗi cảnh báo phông cơ quan/trích yếu sau sửa nhóm trên .56 vẫn chờ bằng chứng runtime phù hợp, không coi việc thêm các nhóm mới là đã khép lỗi đó.

Trên máy có Word: thử sửa riêng từng mép, chọn chung các nhóm mới/cũ, quét lại, một Undo, lưu bản sao và mở lại; chạy **Kiểm tra → Kiểm tra Word tự động…**. Với ba Line, thử riêng 1, 1+3 và cả ba cùng nhóm chữ, giữ kiểm nét thực tế. Khi báo lỗi, gửi tên cảnh báo, thao tác, phiên bản và TXT/log đúng lần kiểm.

[Danh sách đủ C01–C25 và giới hạn](SUPPORTED-FIXES-0.1.60.md) · [Nhật ký B01–B05](BATCH-AFTER-0.1.59.md) · [Chu kỳ phát triển](RELEASE-CADENCE.md).
