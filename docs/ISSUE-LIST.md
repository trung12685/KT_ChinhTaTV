# Danh sách lỗi từ 0.1.50

Mỗi mục chỉ một dòng: **checkbox · tên lỗi · trang**. Bỏ câu “Chọn để sửa hàng loạt”. Từ .52, rê chuột xem ba phần ngắn **Lỗi — Căn cứ — Cách xử lý**. Căn cứ gồm vị trí và nội dung quy định; cách xử lý nêu thay đổi/chiều dài đề xuất. Chọn bằng chuột/bàn phím rồi mở **Chi tiết lỗi** để xem đầy đủ số đo, ngữ cảnh và mô tả. Câu dài hiển thị dấu …, không bị mất nội dung trong chi tiết.

## Ba phần

- **Bắt buộc sửa**: yêu cầu thể thức thuộc phạm vi đã hỗ trợ, có căn cứ và đủ điều kiện đưa gợi ý. Bạn vẫn duyệt trước khi ghi; không chặn lưu nếu chưa sửa.
- **Đề xuất sửa — cần duyệt**: lỗi chữ, dấu câu, bố cục hoặc số đo cần xem lại theo ngữ cảnh. Không đồng nghĩa tự động chắc chắn đúng.
- **Chưa tự sửa — chỉ cảnh báo**: vùng/chức năng chưa có điều kiện sửa. Checkbox bị vô hiệu hóa và không được chọn qua nhóm/tất cả.

Phân loại thận trọng: geometry Line/căn giữa theo cấu hình người dùng ở phần cần duyệt; thiếu Line đã xác định có căn cứ có thể ở thể thức. Mục bị chặn do field/XML/nét chưa chắc luôn thủ công. Một nhóm trên màn hình không phải một loại lỗi mới trong danh mục năng lực của chương trình.

## Chọn và xem

Các lỗi cùng loại nằm cạnh nhau; trong nhóm sắp theo vị trí. Checkbox từng dòng, đầu nhóm, đầu phần và **Tất cả** cùng cập nhật. Dấu gạch ngang trong checkbox nghĩa là mới chọn một phần; bấm để đổi trạng thái. Bấm mũi tên thu gọn/mở nhóm; thao tác Tất cả vẫn áp dụng các mục sửa được trong nhóm đang thu gọn.

Chọn/rê chuột một dòng để đánh dấu vị trí không tự tích lỗi đó. **Sửa lỗi đang xem** dùng dòng đang chọn; **Sửa các lỗi đã tích** dùng các ô đã tích. Thứ tự/giao dịch ghi vẫn do engine cũ kiểm: chữ/phông → căn giữa số/ngày → đường kẻ. Chọn lẫn các giai đoạn vẫn cần tách lượt; Sửa tất cả giữ cách xử lý nhóm đầu như trước.

Sau sửa/Undo/quét lại, danh sách được dựng từ kết quả mới và bỏ ô tích/chi tiết cũ. Nếu quét lỗi, danh sách cũ bị xóa để tránh sửa lại theo vị trí cũ. Không tự ẩn cảnh báo chỉ vì đã bấm sửa.

## Tên đường kẻ

- **Thiếu đường kẻ dưới cơ quan ban hành** (hoặc tiêu ngữ/trích yếu): chỉ khi nhánh kiểm xác định thiếu.
- **Đường kẻ dưới … chưa đúng**: đã có Line nhưng số đo chưa đạt.
- **Đường kẻ dưới … chưa hiển thị đúng**: Shape có nhưng chưa khớp nét thật.
- Trường hợp chưa xác minh vẫn là kiểm tra thủ công; không gán thiếu chỉ dựa số Shape.

Chi tiết vẫn có số đo/căn cứ và phép đo thực tế; kiểm XPS, rollback, Undo và lưu bản sao giữ nguyên. Ảnh xem trước trên release dùng dữ liệu giả trong Windows CI; chưa là xác nhận Word/Office2021.


## Tooltip từ 0.1.52

Ví dụ (chiều dài cụ thể phụ thuộc số đo của từng dòng):

> Lỗi: Thiếu đường kẻ dưới cơ quan ban hành
>
> Căn cứ: Nghị định 30/2020/NĐ-CP, Phụ lục I, Phần I, mục II.2, điểm b: dưới tên cơ quan có đường kẻ ngang, nét liền, dài 1/3–1/2 dòng chữ, đặt cân đối.
>
> Cách xử lý: Kẻ đường ngang dài 95.2 pt, căn theo dòng chữ; mức tự sửa theo cấu hình (mặc định 3/7).

Nguồn Nhà nước là Nghị định, nguồn Đảng là Hướng dẫn; không thay tên thành Thông tư. Các quy tắc khoảng trắng/lặp từ ghi quy tắc ứng dụng, thứ tự mục/dấu đầu dòng ghi theo yêu cầu người dùng. Mục chưa đủ điều kiện ghi hướng xử lý thủ công; số đo/cấu hình không thành câu chữ của quy định. Tooltip không làm mục thủ công trở thành sửa được.
