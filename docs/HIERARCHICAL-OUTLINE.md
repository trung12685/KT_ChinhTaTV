# Số mục gõ tay nhiều cấp — B04 trong code .62

## Sửa được khi đủ bằng chứng

Mỗi dòng là một paragraph Word riêng, ở thân bài đã đối chiếu XML:

```text
1. Nội dung chính
1.1. Tình hình
1.2. Kết quả
1.4. Đề xuất
```

Đề xuất `1.4 → 1.3`, nhưng chỉ ghi lại ký tự `4` thành `3`. Giữ `1.`, dấu cuối, nội dung và định dạng ngoài span. Tương tự2–4 thành phần, ví dụ dưới cha `1.1.`, chuỗi `1.1.1., 1.1.4.` →`1.1.1., 1.1.2.`. Mỗi thành phần1–999, không0 đầu; chỉ nhận nhãn có dấu `.` hoặc `)` trước khoảng phân cách. Không áp sửa chữ lên danh sách Word sinh tự động.

Phải có cha số trực tiếp duy nhất đang mở, ví dụ `1.` cho `1.1.`; `1.` không đủ thay cho cha thiếu `1.1.` của `1.1.1.`. Cùng cha/cấp có mốc đầu1, không restart/trộn delimiter, leading whitespace/indent cùng kiểu. Dấu tab chỉ được giữ qua proof LabelThroughTabs hiện có; không mở TextEditable cho cả đoạn có tab. Các mục cha khác có bộ đếm riêng. C21 đếm dash/plus trong từng cha nhiều cấp như trước.

Checkbox nhóm chọn các mục sửa được ở nhiều cha; mục thủ công không bị tích kèm. Chọn riêng chỉ thay số đã chọn, quét mới giữ các mục chưa chọn. Dòng lỗi hiển thị đầy đủ `1.4 → 1.3` để phân biệt cha, còn highlight chỉ phần số cuối. Tooltip giữ Lỗi/Căn cứ/Cách xử lý; căn cứ là quy tắc ứng dụng theo yêu cầu người dùng, không bịa một điều khoản NĐ30.

## Không tự đổi tiền tố hoặc dẫn chiếu

```text
1. Nội dung đầu
3. Nội dung sau
3.1. Nhánh con
5. Nội dung cuối
```

Đổi `3 → 2` cần đổi cả `3.1`; hiện chưa có thao tác cascade được duyệt. Cả nhóm sửa thứ tự giữ thủ công để không đổi riêng `5 → 3` rồi tạo trùng với mục3 chưa sửa. Nhãn con hoặc dấu hiệu nhãn con chưa đọc chắc đều chặn đổi tiền tố cha. Cũng chặn khi nhiều cha đang mở cùng tiền tố, cha thiếu/nhảy cấp, số0 đầu hoặc sâu quá4 cấp.

`Xem mục 1.4` hoặc `Xem mục 1.2` đều làm nhóm `1.1., 1.4.` cần đối chiếu: một bên là nhãn cũ, một bên là nhãn dự kiến. Ref hậu duệ như `1.4.1` phụ thuộc `1.4`; danh sách/range đơn giản như `1.1, 1.4`, `1.1–1.4`, `1.1 đến 1.4` được đọc cùng. Chỉ số tham chiếu được dựng một lần, lấy các ancestor cần cho nhãn tối đa4 cấp. REF/PAGEREF/NOTEREF và XML chưa chắc tiếp tục chặn theo quy tắc cũ. Chưa phân tích mọi câu dẫn chiếu tự do hoặc tự cập nhật dẫn chiếu.

Một giá trị như `1.5 triệu đồng` không tự bị coi là lỗi đánh số; nếu khiến phạm vi đếm chưa chắc thì các sửa thứ tự/dấu đơn liên quan giữ thận trọng. Không suy ra nội dung bị thiếu chỉ vì số không liên tiếp.

## Giao dịch và kiểm chứng

Không thêm FixKind hoặc ghi bằng Word numPr. Dùng Issue/BatchPlan/CurrentTextPlan/AtomicTextBatch hiện có, kiểm lại toàn ngữ cảnh ngay trước ghi. Thêm con/dẫn chiếu hoặc đổi cha/tiền tố làm gợi ý cũ mất hiệu lực; không áp gợi ý cũ chỉ vì số cuối và vị trí vẫn giống. Chọn chung với chữ/phông/căn/Line vẫn đi qua các bước và XPS mới; một Undo, kiểm nguyên văn/nét và rollback cả lượt được giữ.

Core kiểm marker span, nhiều cấp/cha/tab/Unicode, ngoại lệ, ref cũ/mới/danh sách/range, stale, chọn riêng/chung, rollback và1200 đoạn giả. Desktop kiểm cùng nhóm RequiredFormat/manual, checkbox/totals và nhãn gọn. OfficeHierarchicalOutlineCheck tạo mẫu giả có tiền tố đậm/gạch chân và tab, sửa riêng/chung chữ + Line, lỗi sau Line, một Undo/lưu-mở/băm nguồn. CI chỉ biên dịch fixture Word; chưa xác nhận Office2021 và không coi số Shape là bằng chứng nét đạt. .62 đang phát triển, chưa có gói tải mới.
