# 0.1.53 — Sửa nhiều nhóm, tự đo lại, một Hoàn tác

Trước đây chọn lẫn lỗi chữ/thứ tự/dấu đơn với Line bị chặn “Chọn riêng một nhóm”; Sửa tất cả chỉ chạy nhóm đầu rồi yêu cầu bấm tiếp. Từ .53, một lần chọn/duyệt có thể chạy qua chữ/phông → đo lại → căn giữa → đo lại → đường kẻ theo đúng các mục đã chọn.

Sau sửa chữ, không phát lại vị trí hoặc số đo cũ: nhận diện mốc theo vai trò duy nhất và nội dung dự kiến, dùng XML/range và XPS mới. Không nhận thêm lỗi ngoài lựa chọn, không mở quyền sửa mục chỉ cảnh báo. Mục đã đạt sau bước trước phải được xác minh thật mới bỏ qua.

Một checkpoint ngoài bảo vệ cả lượt; giao dịch chữ/căn giữa/Line cũ vẫn giữ hậu kiểm riêng. Hậu kiểm cuối bảo vệ toàn bộ chữ dự kiến, phông đã chọn, vị trí số/ngày và các nét đã chọn. Thành công có một Undo; lỗi ở bất kỳ bước/đo/hậu kiểm phục hồi cả các nhóm trước, không để Undo trung gian. Nếu rollback lỗi, giữ checkpoint và nguyên nhân, không báo thành công. Lỗi quét danh sách sau lượt đã cam kết vẫn phân biệt với lỗi sửa như trước.

Tooltip ngắn .52 giữ nguyên. Báo cáo TXT/nhật ký thêm mốc .53; danh mục vẫn 21 nhóm đề xuất, 8 thủ công, 1 minh họa cùng ví dụ.

## Kiểm thử và giới hạn

Core kiểm phân bước, prefix Word khác chỉ số chuỗi, cập nhật mốc, scope, manual/duplicate/mất chứng cứ, rollback khi các bước/commit lỗi và giữ hai nguyên nhân khi phục hồi lỗi. WPF dùng glyph đổi kích thước/CP mô phỏng và nét thật: Line phải dùng chiều dài mới; Shape có nhưng nét sai phải rollback. Windows chạy đầy đủ Core/OpenXML, WPF và kiểm báo cáo trong gói trước phát hành; số kiểm thực nằm trong `build-checks.json` và báo cáo TXT.

Đã bổ sung kịch bản Word giả cho đúng tình huống chọn hẹp/toàn bộ các nhóm, lỗi chèn sau từng bước, Undo cũ, lưu/mở và giữ nguồn. CI không có Word; chưa xác nhận runtime Office 2021 cho bản .53. Cần kiểm theo [hướng dẫn sửa nhiều nhóm](MULTI-GROUP-REPAIR.md). Mục Line bị khóa do chưa đủ bằng chứng vẫn không được tự chọn theo sau chữ/phông; không coi bấm Sửa tất cả là đã xử lý mọi loại lỗi.

Bản thử trên nhánh riêng từ .52, chưa nhập main; tag/gói stable .27 giữ nguyên. Xem [danh mục sửa](SUPPORTED-FIXES-0.1.53.md), [nhật ký phát triển](DEVELOPMENT.md) và [danh sách lỗi](ISSUE-LIST.md).
