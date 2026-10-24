# B05 — thân bài văn bản có tên loại không có Kính gửi

Mở phạm vi C26–C29 cho một số văn bản Nhà nước có tên loại, không thêm nhóm lỗi mới. Tổng vẫn29 đề xuất/8 thủ công/1 minh họa; không phải xác nhận tất cả thể thức/chính tả.

## Bằng chứng cần có

Khối đầu có cặp quốc hiệu/tiêu ngữ và tên loại/trích yếu căn giữa xác định. Ngay sau trích yếu là hai đoạn văn xuôi mở đầu, cùng nằm trong phần hình học trang đầu đã đối chiếu, không ở bảng, không căn giữa. XML phải chứng minh căn đều, lề đoạn trái/phải0, không hanging, dòng đầu1cm hoặc1,27cm; đọc đúng từng thuộc tính kế thừa style, không dùng tổng indent thay cho firstLine.

Trước sửa, cả hai đoạn còn phải có chữ thuần, dữ liệu phông/kiểu gắn đúng text, không đậm/nghiêng và căn/thụt Word thực tế khớp XML. Phông/màu/cỡ có thể sai để C26 sửa; không cần các thuộc tính ấy đã đúng mới nhận diện. Cần chữ ký hoặc Nơi nhận đóng phần nội dung như nhánh Kính gửi cũ.

Quan sát phông là bước đọc tạm thời; nếu mốc mở đầu chưa đọc chắc, bị nhấn mạnh hoặc sai căn/thụt thực tế thì không cấp quyền sửa các đoạn phía sau. Căn cứ pháp lý C26–C29 không đổi; đây là nhận diện ứng dụng, không thêm quy định pháp luật.

## Ví dụ

BÁO CÁO / hai dòng trích yếu căn giữa / hai đoạn mở đầu văn xuôi căn đều, thụt1cm và1,27cm / phần nội dung / Nơi nhận. Không có Kính gửi vẫn có thể đề xuất Times New Roman đen/cỡ13–14, căn/thụt đoạn sau, khoảng đoạn6pt và giãn dòng trong phạm vi C26–C29.

Hai mốc có thể chỉ còn một cụm văn xuôi ngắn sau xóa lặp; layout và kiểu vẫn có bằng chứng thì vai trò không mất, các lỗi chưa chọn phía sau không tự biến mất. Không coi tiêu đề/mục, Căn cứ, đoạn chưa kết thúc, bảng/field/auto-list, chữ toàn hoa hoặc font/indent/bidi chưa chắc là mốc mở đầu. Một đoạn mở đầu hoặc mốc sang trang sau chưa đủ cho nhánh này. Không áp Công văn không có tên loại, Đảng/cá nhân hay toàn thân bài/chữ ký.

## Giữ an toàn và kiểm chứng

Sửa riêng/chung bốn nhóm thân bài, chữ và ba Line dùng cùng snapshot, đo XPS mới, hậu kiểm glyph/phông/cỡ/căn/thụt/spacing/đoạn kế, một Undo/rollback và giữ file nguồn. Core kiểm mốc đúng/sai, quan sát tạm/không cấp quyền, style kế thừa, mất mốc làm selection cũ vô hiệu và không mất lỗi phía sau khi mốc ngắn đi. Fixture Word tổng hợp kiểm không Kính gửi, giữ1cm/1,27cm và cỡ, rút gọn cụm lặp, bốn nhóm + ba Line, rollback, Undo/lưu-mở/hash nguồn.

CI biên dịch fixture nhưng không chạy Word. Chưa có runtime Office2021 hoặc bằng chứng khép lỗi phông cũ. [Nhật ký](BATCH-AFTER-0.1.62.md), [C26](BODY-FORMAT.md), [C27](BODY-PARAGRAPH.md), [C28/C29](BODY-SPACING.md).
