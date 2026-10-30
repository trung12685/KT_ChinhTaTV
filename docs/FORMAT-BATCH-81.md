# Đợt 81 — trích yếu theo loại văn bản, 0.1.83

Căn cứ: [bản ký Nghị định 30/2020/NĐ-CP](https://datafiles.chinhphu.vn/cpp/files/vbpq/2020/03/30.signed.pdf), Phụ lục I, Phần I, mục II.5.b (trang PDF 24); sơ đồ ô 5a/5b và bảng cỡ/kiểu chữ tại mục IV–V (trang PDF 30–32). Đã xem trực tiếp trang bản ký, không suy quy tắc từ mẫu cũ.

- Công văn: trích yếu sau V/v, dưới số/ký hiệu bên trái, cỡ 12–13, đứng thường; cách dòng 6 pt. Không có yêu cầu kẻ ngang trích yếu.
- Văn bản có tên loại: tên loại in hoa, trích yếu ngay dưới; cả hai đứng đậm cỡ 13–14, căn giữa chiều ngang văn bản. Có đường kẻ dưới trích yếu dài 1/3–1/2 dòng chữ.
- 13 và 14 là lựa chọn hợp lệ theo từng khoảng, không phải hai cỡ duy nhất. Bảng Word không phải yêu cầu của Nghị định.

## Thay đổi

1. Nhận diện hẹp công văn bằng quốc hiệu/tiêu ngữ, dòng số duy nhất và V/v cùng ô bên trái dưới số. Không cần căn giữa sẵn để nhận diện lỗi căn; không suy từ font, thiếu tên loại hoặc V/v trong thân bài. Chấp nhận mẫu ngoài bảng nếu khối đầu và Kính gửi đủ ranh giới. Ký hiệu tên loại, mốc trùng, khác ô, quá trang/phạm vi hoặc native/XML lệch không cho phép sửa phông.
2. Quan sát và sửa phông V/v theo vai trò riêng 12–13 thường; giữ từng cỡ đúng, chữ, gạch chân và căn đoạn. Payload được kiểm lại trước ghi và dùng hậu kiểm định dạng/transaction hiện có. Không ép công văn sang phông đậm cỡ 14. Vị trí chưa căn giữa được hướng dẫn thủ công đúng vùng, không tự dịch sang dưới bảng.
3. Công văn chỉ cần hai mốc cơ quan/tiêu ngữ; loại có tên cần thêm mốc trích yếu. Bỏ cảnh báo mốc trích yếu cho công văn đã nhận diện. Chưa nhận diện loại thì báo chưa xác định, không báo thiếu nét. C10/C09/C24 và C12 phản ánh phạm vi tương ứng; hai nét công văn không được chứng nhận thay ba nét loại có tên.
4. Thêm nhóm Word độc lập `document-subject`: công văn 12/13, Quyết định/Báo cáo/Kế hoạch 14 ngoài bảng, sửa riêng phông công văn, Undo, lưu/mở, hash nguồn. Kiểm schema và đường nối CLI không cần Word.

## Giới hạn còn lại

Chưa đọc mọi biến thể công văn, nhiều đoạn trích yếu hoặc nhiều khối đầu; khi mơ hồ vẫn yêu cầu người dùng duyệt. Chưa tự đo/sửa khoảng dọc 6 pt và vị trí tuyệt đối; C10 chỉ chứng minh phông, không chứng nhận vị trí dọc. Không tự xóa nét dư vì có thể là đối tượng của người dùng. Chưa nâng thành trình sửa trực tiếp trên trang; giữ chế độ thủ công mặc định và ô sửa đoạn của 0.1.82. Bộ đọc thử nghiệm OFF, tối ưu dừng. Word thực tế **NOT_RUN**; CI chỉ Core/OpenXML/WPF/gói, không xác nhận Word.
