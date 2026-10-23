# Danh sách lỗi từ 0.1.50

Mỗi mục chỉ một dòng: **checkbox · tên lỗi · trang**. Bỏ câu “Chọn để sửa hàng loạt”. Từ .52, rê chuột xem ba phần ngắn **Lỗi — Căn cứ — Cách xử lý**. Căn cứ gồm vị trí và nội dung quy định; cách xử lý nêu thay đổi/chiều dài đề xuất. Chọn bằng chuột/bàn phím rồi mở **Chi tiết lỗi** để xem đầy đủ số đo, ngữ cảnh và mô tả. Câu dài hiển thị dấu …, không bị mất nội dung trong chi tiết.

## Ba phần

- **Bắt buộc sửa**: yêu cầu thể thức có căn cứ, hoặc C20/C21 theo lựa chọn người dùng từ .56, đã đủ điều kiện đưa gợi ý. Bạn vẫn duyệt trước khi ghi; không chặn lưu nếu chưa sửa.
- **Đề xuất sửa — cần duyệt**: lỗi chữ, dấu câu, bố cục hoặc số đo cần xem lại theo ngữ cảnh. Không đồng nghĩa tự động chắc chắn đúng.
- **Chưa tự sửa — chỉ cảnh báo**: vùng/chức năng chưa có điều kiện sửa. Checkbox bị vô hiệu hóa và không được chọn qua nhóm/tất cả.

Phân loại thận trọng: geometry Line/căn giữa theo cấu hình người dùng ở phần cần duyệt; thiếu Line đã xác định có căn cứ có thể ở thể thức. Mục bị chặn do field/XML/nét chưa chắc luôn thủ công. C20/C21 chỉ chuyển phần khi CanFix; mục cấu trúc chưa rõ vẫn thủ công. Việc chuyển phần không tạo căn cứ pháp lý mới. Một nhóm trên màn hình không phải một loại lỗi mới trong danh mục năng lực của chương trình.

## Chọn và xem

Các lỗi cùng loại nằm cạnh nhau; trong nhóm sắp theo vị trí. Checkbox từng dòng, đầu nhóm, đầu phần và **Tất cả** cùng cập nhật. Dấu gạch ngang trong checkbox nghĩa là mới chọn một phần; bấm để đổi trạng thái. Bấm mũi tên thu gọn/mở nhóm; thao tác Tất cả vẫn áp dụng các mục sửa được trong nhóm đang thu gọn.

Chọn/rê chuột một dòng để đánh dấu vị trí không tự tích lỗi đó. **Sửa lỗi đang xem** dùng dòng đang chọn; **Sửa các lỗi đã tích** dùng các ô đã tích. Từ .53 có thể chọn nhiều nhóm khác nhau cùng lúc. Chương trình tự chạy chữ/phông → đo lại → căn giữa số/ngày → đo lại → đường kẻ, theo đúng mục đã chọn. **Sửa tất cả** dùng các gợi ý sửa được đang có trong danh sách; mục thủ công hoặc mới phát hiện sau đó không tự được thêm vào lượt. Một Undo bỏ cả lượt; bước sau lỗi thì phục hồi tất cả. Xem [sửa nhiều nhóm](MULTI-GROUP-REPAIR.md).

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

Từ .56, đoạn có Line nổi do ứng dụng tạo vẫn được kiểm/sửa phông nếu chữ đã khớp XML, Line có dấu nhận diện/cấu trúc hợp lệ và không chứa chữ. Quyền sửa chữ thông thường qua đối tượng vẫn đóng; hình/Line lạ, dấu nhận diện trùng/sai hoặc vùng chưa khớp vẫn thủ công. Không ẩn cảnh báo dựa lịch sử đã bấm sửa.

## Tiến độ từ .57

Sau duyệt sửa xuất hiện cửa sổ có bước hiện tại, số mục đã hậu kiểm/tổng mục chọn và thời gian chờ. Bộ đếm tạm thời không bảo đảm cả lượt đạt; thanh chưa hoàn tất trước hậu kiểm/quét lại. Khi phục hồi, số đếm tạm thời về 0. Cửa sổ vẫn cập nhật khi Word đang xử lý, không dự đoán thời gian còn lại hoặc tự hủy giao dịch. Phiên bản lấy từ chương trình đang chạy nằm cạnh Loại văn bản và trên cửa sổ này. Xem [ghi chú .57](CHANGES-0.1.57.md).
