# Chính tả Việt 0.1.61 — bản thử sau năm hạng mục

Trọng tâm đợt này là kiểm định dạng và sửa nhiều nhóm thành một luồng. Gom B01–B05 sau .60, kiểm từng bước rồi tạo một gói Windows chung. Không nâng main/mốc ổn định .27; không ghi đè bản .60.

## Hai hạng mục chính

**B04 — Định dạng theo hệ thống:** đọc sau nhận diện và proof XML, mỗi vùng một lần; whole-range rõ dùng nhanh, chưa rõ đọc Words rồi ký tự cần thiết. Phân biệt đúng/sai/chưa đọc chắc; không báo lỗi chỉ vì cỡ 13/14 cùng hợp lệ, đen tự động/đen RGB hoặc kiểu khác ở dấu cách/ngắt dòng. Sửa phông/màu giữ từng cỡ hợp lệ, kiểm từng cỡ ký tự còn lại sau lưu/các nhóm/Line; cỡ đúng bị ép về cỡ đúng khác vẫn rollback. Một glyph sai thật không bị bỏ qua. [Thiết kế/giới hạn](https://github.com/trung12685/KT_ChinhTaTV/blob/v0.1.61/docs/FORMAT-ASSESSMENT.md).

**B05 — Chọn/sửa nhiều nhóm:** Line có lỗi chữ/phông cần sửa trước được chọn kèm đúng các mục đó và duyệt rõ. Không cần chia thành lượt thủ công khi đã có đủ bằng chứng. Nét đang đạt có thể chỉ cần đo lại sau sửa phông; không ghi thêm nếu số đo mới vẫn đạt. Nhận diện mốc cơ quan theo cấu trúc Số/tên ngay cả khi còn lỗi viết hoa. Backend chặn thiếu/phát sinh phụ thuộc chưa duyệt; giữ đo lại, nét XPS thật, một Undo và rollback. Giảm đọc trang/prefix Word ở cùng CP và đo lại trùng cùng mốc trong một XPS. [Cách chọn và bảo toàn](https://github.com/trung12685/KT_ChinhTaTV/blob/v0.1.61/docs/REPAIR-DEPENDENCIES.md).

## Ba bổ sung lỗi chữ đầu đợt

| Bước | Ví dụ | Giới hạn |
|---|---|---|
| B01/C13 | `Kế hoạch kế hoạch đã duyệt.` → `Kế hoạch đã duyệt.` | Chỉ khác chữ hoa đầu đoạn; không gộp hoa/thường giữa câu, từ đơn hoặc mã. |
| B02/C14 | `Theo mẫu (nội dung) ,báo cáo.` → `Theo mẫu (nội dung), báo cáo.` | Ngoặc đơn cân bằng, câu chữ rõ; không suy diễn hàm/mã/ngoặc lồng. |
| B03/C18–C19 | `Đã hoàn thành .Xong!` → `Đã hoàn thành. Xong!` | Câu cuối một từ có dấu kết câu đơn; chọn riêng trước/sau, loại viết tắt/đuôi tệp/chuỗi dấu. |

Tổng vẫn **25 nhóm đề xuất sửa, 8 nhóm thủ công và 1 minh họa**. TXT có ví dụ và giới hạn; chưa có từ điển/ngữ nghĩa tiếng Việt đầy đủ.

## Bằng chứng và kiểm trên Office 2021

Pipeline kiểm Core/OpenXML và WPF/build, ghi số thực tế đúng commit vào `build-checks.json` và TXT, publish Windows x64 self-contained, kiểm metadata báo cáo rồi ZIP/SHA-256. Có số lời gọi bằng đối tượng giả để kiểm tối ưu; **chưa có đo tốc độ Word thực tế**, không bỏ hậu kiểm để tăng tốc.

**Chưa xác nhận toàn bộ bài Word .61 trên Office 2021; chưa khép phản hồi phông cơ quan/trích yếu .56 chỉ bằng CI.** Hai nguyên nhân trong code đã được tái hiện/sửa và có hồi quy, nhưng CI chỉ biên dịch fixture Word. Không coi đây là bản ổn định thay .27.

Trên máy có Word: chạy **Kiểm tra → Kiểm tra Word tự động…**. Với Bao cao.docx, thử ba Line cùng thứ tự mục/dấu đơn, font cơ quan/trích yếu, Enter/ngắt dòng; quét lại, một Undo, lưu/mở. Thử cố ý đổi một glyph sang Arial đỏ để chắc lỗi thật vẫn bị phát hiện; thử chọn Line và xem các mục cần kèm. Nếu có lỗi, giữ TXT/log đúng phiên bản và tên cảnh báo/thao tác.

[Danh mục đủ](https://github.com/trung12685/KT_ChinhTaTV/blob/v0.1.61/docs/SUPPORTED-FIXES-0.1.61.md) · [Nhật ký đợt](https://github.com/trung12685/KT_ChinhTaTV/blob/v0.1.61/docs/BATCH-AFTER-0.1.60.md).
