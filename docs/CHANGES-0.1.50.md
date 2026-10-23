# 0.1.50 — Danh sách lỗi gọn và chọn theo nhóm

Theo yêu cầu người dùng: mỗi lỗi một dòng cao28px, giữ checkbox nhưng bỏ câu “Chọn để sửa hàng loạt”. Tên lỗi ngắn và số trang; mô tả/gợi ý/số đo/căn cứ xem qua tooltip hoặc chọn rồi mở **Chi tiết lỗi**. Có thể dùng bàn phím để chọn và đọc chi tiết.

## Nhóm và trạng thái chọn

Ba phần: **Bắt buộc sửa**, **Đề xuất sửa — cần duyệt**, **Chưa tự sửa — chỉ cảnh báo**. Nhóm cùng loại nằm cạnh nhau; trong nhóm theo vị trí. Checkbox theo từng mục/nhóm/phần/tất cả, hỗ trợ trạng thái chọn một phần; thu gọn/mở nhóm không làm mất lựa chọn, Tất cả áp dụng cả nhóm đang thu gọn. Cảnh báo thủ công không tích được. Chọn đang xem/hover độc lập với checkbox.

“Bắt buộc sửa” chỉ dùng cho các yêu cầu thể thức có căn cứ trong phạm vi được hỗ trợ và còn được phép đề xuất; phân loại thận trọng khi chỉ có dữ kiện giao diện hoặc lựa chọn căn trong ô. Geometry Line/dung sai người dùng và căn giữa vẫn cần duyệt. Mục chưa chắc/bị chặn luôn thủ công. Nhãn này không tự cho phép ghi hoặc chặn lưu.

Tên **Thiếu đường kẻ dưới cơ quan ban hành/tiêu ngữ/trích yếu** lấy từ nhánh đã xác định thiếu. Line có sẵn sai số đo/chưa khớp nét có tên riêng, không gọi là thiếu. Giữ toàn bộ mô tả/độ dài/tâm/khoảng cách trong chi tiết.

## Giữ luồng sửa hiện có

Sắp xếp chỉ thay trình bày; truyền lại đúng Issue/anchor gốc. Giữ lượt chữ/phông → căn giữa → Line, duyệt trước khi áp dụng, xác minh hiện hành, hậu kiểm nét XPS, rollback toàn lượt, một Undo và lưu bản sao. Không đổi WordSession/transaction/checkpoint/save hoặc mở quyền sửa qua phân loại. Scan/lỗi thay model mới và bỏ checked/selection/details cũ; không tự ẩn mục theo lịch sử nút sửa.

Danh mục vẫn C01–C19/M01–M08/D01 (19 đề xuất,8 thủ công,1 minh họa), không thêm quy tắc ngôn ngữ. Báo cáo TXT thêm mốc .50; xem [cách dùng](ISSUE-LIST.md), [danh mục](SUPPORTED-FIXES-0.1.50.md) và [bài kiểm](WINDOWS-VALIDATION.md).

Core/model kiểm phân loại, partial selection/manual và1200mục. WPF kiểm panel thật/grouping/binding/row28px/chọn không tự tích/tooltip/details/clear-rescan, cùng tên Line thiếu và các hồi quy hiện có. Windows CI render `issue-list-preview.png` từ dữ liệu giả, đính kèm release để xem bố cục; không dùng tài liệu riêng.

Gói chỉ phát hành sau kiểm Core/OpenXML, WPF và báo cáo đóng gói; số kiểm thực tế có trong JSON/TXT kèm gói. **CI không có Word; chưa xác nhận đầy đủ Office2021 cho .50.** Giữ main/stable .27 và tag/asset cũ.
