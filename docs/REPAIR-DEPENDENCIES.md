# Chọn nhiều nhóm có mục cần sửa trước

## Hành vi

Lượt sửa nhiều nhóm đã có bộ điều phối nhưng giao diện cũ khóa Line khi vùng tham chiếu còn lỗi chữ/phông; người dùng không chọn được Line dù bộ điều phối có thể đo lại sau sửa. B05 nối nhận diện, checkbox, duyệt và giao dịch thành một luồng.

- Line đo được và có thể sửa được nêu đúng các lỗi chữ/phông cần sửa trước trong vùng tham chiếu. Chọn Line sẽ tích kèm các mục này ở nhóm tương ứng; Summary và tooltip báo ngắn gọn. Bỏ tích mục cần sửa trước sẽ bỏ tích Line phụ thuộc. Bỏ tích Line không tự bỏ mục chữ/phông có thể sửa độc lập.
- “Sửa lỗi đang xem” trên Line cũng liệt kê đầy đủ các mục cần kèm trong hộp duyệt. Không âm thầm sửa các lỗi ngoài danh sách đã duyệt. Select all và chọn nhóm dùng cùng quan hệ phụ thuộc.
- Nét của ứng dụng đang đúng nhưng có lỗi phông/chữ liên quan được ghi là **Đo lại đường kẻ… sau sửa chữ/phông**, không ghi thiếu. Sau sửa chữ/phông, nếu nét vẫn đúng thì tính đã đạt và không ghi lại; nếu sai thì dùng số đo mới.
- Line lạ, nhóm Shape, ghép chưa chắc, mốc chưa đo được hoặc chưa căn giữa vẫn thủ công. Không dùng mục cần sửa trước để mở quyền sửa đường lạ hoặc chữ qua đối tượng.

## Giao dịch và danh sách

`RepairDependencies` khớp đúng mục hiện có bằng span/chữ/loại/thông số định dạng, loại trùng và không kéo theo lỗi không liên quan. `StagedRepairBatch.Prepare` chặn thiếu mục cần sửa trước, vòng/phụ thuộc lồng hoặc mục thủ công. Sửa chữ/phông trước, đọc lại XML/bố cục/XPS, rồi căn giữa và Line. Geometry còn phụ thuộc chưa giải quyết không được ghi từ số đo cũ; phát sinh lỗi mới chưa duyệt thì rollback cả lượt.

Sau sửa, MainWindow vẫn quét và dựng danh sách từ dữ liệu hiện tại, không giữ các hàng cũ bằng lịch sử sửa hay đếm Shape. Progress chỉ hoàn tất sau quét lại; nếu sửa đã commit nhưng bước dựng danh sách lỗi thất bại thì thông báo riêng, không lặp lại sửa hoặc tự Undo giao dịch đã kiểm. Có một Undo cho cả lượt nhiều nhóm; lỗi sửa/hậu kiểm bên trong vẫn rollback.

Tên cơ quan chưa viết hoa được nhận làm mốc khi có một dòng Số rõ và một/hai tên ở cùng ô đúng vùng; không lấy cơ quan chủ quản thay cơ quan ban hành chỉ vì tên ban hành đang chữ thường. Không có cấu trúc số rõ thì chỉ giữ nhánh uppercase hẹp, không đoán từ dòng thường.

## Giảm thao tác lặp

- Một lượt xuất preview đọc trang và prefix Word một lần cho mỗi CP khác nhau. Mỗi lỗi vẫn đối chiếu riêng text/length. CP khác chỉ số UTF-16 được giữ; không dùng GoTo hoặc suy chỉ số. Cache chỉ nằm trong lượt xuất, không qua sửa/Undo/mở file hoặc lần quét khác.
- Căn giữa/số-ngày dùng một bộ bằng chứng vừa đo trên cùng XPS để kiểm và phân loại, không đo cùng dòng/tham chiếu lần nữa. Thiếu anchor hoặc khác trang vẫn không cho sửa.
- Không bỏ lần đo lại cần thiết sau sửa phông hoặc chèn Line, không dùng metadata để thay nét thực tế.

## Kiểm thử

Core kiểm closure đúng mục, trùng/mất/đổi/thủ công/vòng, backend thiếu lựa chọn, thứ tự chữ/phông → đo mới → Line, geometry cũ 10pt không thay số đo mới 100pt, Line đã đạt không ghi lại và phát sinh phụ thuộc mới rollback. Kiểm 1200 anchor/600 CP chỉ 600 lượt đọc vị trí bằng provider giả; không phải số đo thời gian Office.

Desktop kiểm checkbox theo lỗi/nhóm/all, phụ thuộc chung, bỏ tích, dòng cũ sau quét, nét thật còn thiếu/đang đúng/ngoài vùng/đường lạ và dùng bằng chứng centering có anchor. Bài Word component/format dùng đúng inspection và model chọn Line của ứng dụng, không gạt cảnh báo phông trong test để tạo Line giả. Có sửa riêng/chung ba Line, rollback, một Undo, giữ cỡ/gạch chân, lưu/mở và băm nguồn. CI chỉ biên dịch bài Word; vẫn cần máy có Office 2021 chạy thực tế.
