# C30 — thêm khoảng sau ngoặc kép đóng

B02 sau .64, code .65 chưa đóng gói. Ví dụ `Theo mẫu “nội dung”đã duyệt.` → `Theo mẫu “nội dung” đã duyệt.`. Cùng nhóm Ngoặc kép với hai mép trong, không thêm ID hoặc điều khoản pháp lý. Tooltip ghi quy tắc ứng dụng và hành động thêm khoảng.

Chỉ nhận cặp “…” cân bằng/không lồng, chữ thuần có proof XML; hai từ trước rõ, ít nhất hai từ chữ trong cặp và hai từ sau rõ, từ ngay sau quote viết thường. Mỗi từ kiểm có ít nhất hai chữ cái; hỗ trợ Unicode tổ hợp. Gate ngữ cảnh ClearAfterDelimitedText dùng chung với C15, nhưng mỗi caller vẫn chứng minh cặp/permission riêng. Không thêm sau cho câu có dấu kết trong quote, từ/số/mã/hàm/viết tắt hoặc ranh giới chưa rõ, mixed/nested/unbalanced quote, tab/NBSP/control, header/ngày hoặc XML chưa chắc.

Mốc sửa có length1, `Original=”`, `Replacement=” `, đúng một dấu cách ASCII; không dùng CP trống hoặc đổi/xóa dấu quote. BatchPlan chỉ nhận payload này hay xóa spaces ở mép trong, không kèm ghi phông/thuộc tính khác trái phép. Retained-glyph projection giữ đúng quote gốc và từng kiểu/cỡ/gạch chân; khoảng mới không có chứng cứ giả về glyph nguồn.

Ví dụ `Theo mẫu “ nội dung ”đã duyệt.` có ba mục trong cùng nhóm: xóa sau dấu mở, xóa trước dấu đóng, thêm sau dấu đóng. Chọn riêng khoảng ngoài giữ mép trong; chọn mép trong giữ khoảng ngoài còn có thể tích. Chọn cả nhóm hoặc cùng chữ/phông/căn/spacing/Line qua luồng hiện có cho `Theo mẫu “nội dung” đã duyệt.` một lượt. Không đổi nội dung trích dẫn hoặc tự viết hoa từ sau.

Không thêm lượt đọc COM/XPS hoặc giảm quét/hậu kiểm. Một Undo cho cả lượt; lỗi ghi/lưu/hậu kiểm phải rollback toàn lượt và giữ nguồn. Core kiểm riêng/chung/ba profile/Unicode/CP, hai loại ngoặc, ngoại lệ, payload/stale và proof glyph với bốn nhóm thân bài; Desktop kiểm từng mục/nhóm/all/tri-state/quét mới. Fixture Word có quote kiểu riêng, sửa riêng trong/ngoài, chung chữ/baLine, giữ kiểu/cỡ/phần chưa chọn, Undo/rollback/lưu-mở/hash nguồn. CI chỉ biên dịch fixture, chưa xác nhận runtime Office2021.
