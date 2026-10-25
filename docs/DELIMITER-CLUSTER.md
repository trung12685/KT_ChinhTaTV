# Cụm ngoặc, dấu câu và khoảng trắng — .65

B03–B05 sau .64 được triển khai chung một lượt, tiếp nối C14/C30 của B01–B02. Không tăng ID chức năng để đếm mỗi trường hợp nhỏ. C14/C15/C30 cùng xử lý các vùng được chứng minh, mỗi lỗi vẫn có checkbox riêng và checkbox nhóm.

| Trường hợp | Trước → sau khi chọn đủ nhóm |
| --- | --- |
| Thiếu trước/trong/sau, từ lặp, dấu câu | `Theo mẫu( nội dung nội dung )đã  duyệt;hoàn thành.` → `Theo mẫu (nội dung) đã duyệt; hoàn thành.` |
| Quote thẳng | `Theo mẫu" kế hoạch "cần làm.` → `Theo mẫu "kế hoạch" cần làm.` |
| Quote cong và dấu câu ngoài | `Theo mẫu“ nội dung ” ,báo cáo.` → `Theo mẫu “nội dung”, báo cáo.` |
| Câu trong ngoặc tròn | `( Đã hoàn thành. )` → `(Đã hoàn thành.)` |
| Dấu phẩy trước dấu kết | `Theo mẫu ( nội dung,báo cáo. ) đã ghi.` → `Theo mẫu (nội dung, báo cáo.) đã ghi.` |

Giữ loại dấu; không đổi quote thẳng thành cong. Chỉ xóa ASCII spaces ở mép hoặc thêm đúng1ASCII qua dấu mở/đóng đã chứng minh. Khoảng trước dấu mở cần hai từ trái rõ, ít nhất hai từ chữ trong và biên sau rõ; khoảng sau giữ gate hai từ trái/trong/phải, từ ngay sau thường. Xóa mép câu có .?! cần một dấu gắn cuối và các từ trước rõ. Không mở chuỗi .../?!, nhiều câu, khoảng trước dấu, viết tắt/mã hoặc cấu trúc chưa chắc.

Hai họ quote không được trộn trong cùng nhánh; cặp lồng/thiếu/thừa không cấp sửa. Số/mã/hàm nhận diện được, header/ngày, control/tab/NBSP và XML chưa chắc giữ guard. Ô trống `"    "`, `“    ”`, `(    )` giữ width khi FixAll. Ngoài phạm vi không đồng nghĩa văn bản đúng toàn bộ. Quy tắc ứng dụng có duyệt, không gán điều khoản NĐ30 mới.

Parser cặp, ngữ cảnh và prose dùng chung; cache chỉ trong lần gọi, không giữ quyền sửa qua thay đổi tài liệu. C14 vẫn phát hiện dấu câu sau ngoặc dính từ và bên trong trước dấu kết khi có ranh giới chắc. Chọn riêng trước/trong/sau giữ các lỗi chưa chọn còn hành động sau quét; chọn đủ nhóm xử lý các tổ hợp đã hỗ trợ một lượt. Không thêm COM/XPS để phát hiện. Bốn nhóm thân bài/chữ/phông/Line vẫn qua proof hiện có, quét mới, nét thực, một Undo/rollback, giữ nguồn.

Core kiểm72 tổ hợp ba loại dấu và từng cạnh đã đúng/sai, partial-then-rescan, Unicode/CP/ba profile, ngoại lệ, retained glyph/kiểu/cỡ khi ghép phông/đoạn, payload/stale/rollback. WPF model kiểm nhóm và từng cạnh. Fixture Word tổng hợp nhiều dạng có font/kiểu/cỡ từng ký tự khác nhau, lấy CP native đã đối chiếu XML, sửa riêng/chung chữ/baLine, rollback/mộtUndo/lưu-mở/hash nguồn. CI chỉ biên dịch fixture; Word/Office2021 cần chạy thực tế.
