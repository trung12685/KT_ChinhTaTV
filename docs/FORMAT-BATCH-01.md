# Đợt thể thức 01 — khối ký

Ngày26/10/2026. Mã tích lũy0.1.70, tiếp nối policy bộ đọc cũ của commit118484da. Chưa phát hành Windows. Theo yêu cầu người dùng, dừng tối ưu đọc; giữ bảng và Preview Word/XPS. Thứ tự phát triển: thể thức → chữ cơ bản/khoảng trắng/dấu câu/từ-cụm lặp → ngữ cảnh và thư viện sau cùng.

## Phần đã có và đợt này

| Nhóm thể thức | Trạng thái mã |
|---|---|
| Quốc hiệu/tiêu ngữ, tên cơ quan | Có kiểm/sửa trong vùng xác minh; giữ cỡ hợp lệ |
| Số/ký hiệu, địa danh/ngày tháng | Có kiểm/sửa hình thức, bảo vệ ô trống; giá trị chưa rõ không đoán |
| Tên loại và trích yếu nhiều đoạn/ngắt dòng | Có nhận diện, phông/cỡ/kiểu; không coi Enter trong trích yếu là lỗi |
| Ba đường ngang | Có sửa chung nhiều nhóm, đo lại và kiểm nét XPS; không xác nhận bằng số Shape |
| Văn xuôi thường | Có phông/cỡ, căn/thụt, khoảng đoạn/giãn dòng trong phạm vi riêng C26–C29 |
| Nơi nhận9b | Có phông nhãn/danh sách và dấu; không coi các gạch nơi nhận là lỗi thứ tự |
| Quyền hạn/chức vụ và họ tên người ký | **Mới C31**: phông/màu/cỡ/đứng/đậm trong khối chắc chắn |
| Khổ giấy/lề, các bố trí chưa nhận diện, số trang | Còn phần chỉ cảnh báo/chưa hỗ trợ; không coi bảng này là đã kiểm toàn bộ NĐ30 |

## C31 nhận và sửa gì

Khối nằm trong bảng cuối phần thân, một hàng/hai ô, không gộp hoặc lồng. Ô trái có nhãn Nơi nhận và dòng kết Lưu. Ô phải có chức vụ được nhận diện, khoảng dành ký/ảnh, rồi họ tên có thể kèm cấp bậc. Chỉ các mốc đã đối chiếu với đoạn Word thật mới mở sửa. Tên người không lấy từ từ điển, không xác minh danh tính/thẩm quyền.

Nguồn: bản Nghị định30/2020/NĐ-CP do người dùng cung cấp, Phụ lụcI, PhầnI.I.4 và II.7.đ, ô7a–7b; PDF21 và26–27. Quyền hạn/chức vụ và họ tên dùng Times New Roman, đen, cỡ13–14, đứng, đậm. Đợt này xử lý các thuộc tính chữ đó; yêu cầu in hoa/thường và vị trí khối chưa tự đổi.

| Lỗi thực | Xử lý sau khi người dùng chọn |
|---|---|
| Arial hoặc phông khác | Times New Roman |
| Màu đỏ hoặc màu khác | Đen |
| Cỡ12 xen13/14 | Chỉ12 về13; giữ từng cỡ13/14 đúng |
| Cỡ15 hoặc cỡ ngoài khoảng | Đưa phần sai về13; không ép toàn khối |
| Nghiêng | Đứng |
| Không đậm | Đậm |

Ví dụ: `TRƯỞNG PHÒNG` hoặc `Đại tá Nguyễn Văn An` dùng Arial đỏ cỡ12 nghiêng, không đậm → sửa các thuộc tính sai trên. Nội dung/chữ hoa-thường/cấp bậc giữ nguyên. Có thể chỉ sửa chức vụ, chỉ sửa họ tên, chọn cả nhóm Chữ ký hoặc chọn cùng nhóm khác.

Dữ liệu font chưa đọc chắc không thành cảnh báo sai. Cỡ13/14 trộn, hai mã màu đen tương đương đều hợp lệ. Tên mẫu “Người ký”, “Họ và tên”, field/control trong mốc, danh sách tự động/kiểu chưa rõ, bảng gộp/lồng, chức vụ chưa nhận diện hoặc phụ lục/nội dung sau bảng chưa cấp quyền sửa. Không đổi bố cục để ép nhận diện.

## Giữ dữ liệu và kết quả

Vùng ghi chỉ bao gồm chữ đã xác minh, không gồm dấu kết đoạn/ô hoặc ảnh. Giữ ngắt dòng mềm, gạch chân, mỗi cỡ đúng và căn/thụt/khoảng đoạn. Dùng quan sát glyph và thuộc tính đoạn trước/sau, không coi việc gán Font thành công là toàn bộ thao tác đã đạt. Vẫn quét lại trước ghi, checkpoint, hậu kiểm sau lưu/Line/XPS và một Undo/rollback cho cả lượt. Danh sách dựng lại từ lần quét mới, lỗi đã hết bị xóa khỏi bảng, lỗi chưa sửa còn chọn được.

Kiểm tự động gồm sai từng thuộc tính, đúng/trộn hợp lệ, mốc/CP thiếu, vùng cấu trúc, stale/forged payload, chọn nhiều vai trò/nhóm, bảo toàn kiểu/cỡ/bố cục và rollback. WPF kiểm chọn nhóm/all, tri-state và xóa kết quả cũ. Kịch bản Word dùng dữ liệu giả/PNG kiểm sửa riêng/chung ba Line, lỗi sau ghi, Undo, lưu-mở và nội dung/ảnh/nguồn nguyên vẹn. CI biên dịch nhưng không chạy Office: **chưa có bằng chứng Office2021 chạy đạt C31**.

## Các đợt tiếp theo

1. Tiếp tục thể thức theo nhóm bố cục: đối chiếu trang/lề/số trang và các vùng còn thiếu; tách điều kiện sửa chắc khỏi ngoại lệ trước khi mở tự sửa.
2. Rà đồng bộ các thành phần đầu–thân–cuối, sửa riêng/chung, danh sách sau sửa, lưu/mở và Undo trên bộ mẫu có bảng/ảnh/ngắt dòng. Sửa lỗi thực phát hiện trước khi mở rộng phạm vi.
3. Sau thể thức, gom các nhóm chữ cơ bản, khoảng trắng, dấu câu và từ/cụm lặp theo tình huống; giữ ô số/ngày trống và tên riêng/mã.
4. Ngữ cảnh/ý nghĩa và thư viện từ để sau cùng. Không tự viết lại câu để thay cho quy tắc chưa có.

Báo cáo trong chương trình/TXT cập nhật31 nhóm đề xuất,8 thủ công,1 minh họa cùng ví dụ/giới hạn. Đây là số loại hỗ trợ, không phải số lỗi đã sửa trên một tài liệu. Tích lũy theo nhịp5–10 bước có ích rồi mới đóng gói, trừ khi người dùng cần bản tải sớm hơn; giữ nguyên mốc .67 và gói .69.
