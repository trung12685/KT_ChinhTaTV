# Kiểm thử 0.1.76 trên máy có Word

CI chạy Core/OpenXML, WPF và build ứng dụng. Các kết quả đó không xác nhận Microsoft Word đã chạy đạt. Giữ kết quả của đúng phiên bản 0.1.76 và đúng thư mục giải nén mới.

1. Mở ChinhTa.Desktop.exe, kiểm phiên bản và giữ menu **Thử bộ đọc mới** bỏ tích.
2. Chọn **Kiểm tra → Kiểm tra từng nhóm Word…**. Danh sách có tám nhóm, gồm hai nhóm mới **Chuyển PAGE cuối trang** và **Khổ/lề với đầu–cuối trang trống**. Mỗi nhóm dùng mẫu giả, phiên Word và thư mục riêng. Lỗi một nhóm không ngăn nhóm tiếp; PASS chỉ áp dụng nhóm đã chọn, không thay kết quả toàn bài.
3. Ưu tiên hai nhóm mới, sau đó nhóm **Khổ giấy và lề** và **Số trang** hiện có. Hai mẫu mới chạy độc lập, không chờ bài Subject. Giữ từng PASS/FAIL/NOT_RUN và bước thất bại; không đổi mẫu để làm mất lỗi.
4. Lưu cả thư mục **OfficeGroups-…**, gồm **office-groups.txt** và thư mục mẫu từng nhóm, cùng diagnostics.log và build-checks.json.
5. Chạy tiếp **Kiểm tra → Kiểm tra Word tự động…** để kiểm toàn luồng và đường kẻ. Bài toàn bộ vẫn dừng ở lỗi đầu tiên. Giữ **OfficeCheck-…/office-check.txt** và các DOCX/JSON/XPS chẩn đoán. Bài theo nhóm và bài toàn bộ có kết quả riêng.

Chỉ khi chủ động bật bộ đọc thử nghiệm, bài toàn bộ mới bắt buộc so hai cách đọc. Kiểm theo nhóm luôn dùng bộ đọc cũ và không xác nhận bộ đọc thử nghiệm.

## Kiểm phạm vi mới trên bản sao tài liệu

- Mở **Phạm vi kiểm tra tài liệu…** sau quét. Xem nhóm chưa nhận diện/thiếu dữ liệu/chờ bản xem trước và số cảnh báo riêng. Mở tài liệu khác hoặc quét lỗi phải xóa dữ liệu cũ; trạng thái đã quan sát không phải chứng nhận toàn bộ tài liệu.
- Với số trang, thử bản sao có footer chỉ PAGE trong phạm vi một section dọc, số Ả Rập từ 1 và 1–9 trang. Duyệt đề xuất chuyển PAGE; xem trang đầu không hiện số, trang sau liên tiếp ở giữa lề trên. Kiểm nội dung/footer khác/ảnh giữ nguyên; thử một Hoàn tác, lưu bản sao và mở lại. Footer có nhãn, trang trí, field lạ, nhiều section hoặc chưa đủ bằng chứng phải giữ thủ công/phục hồi.
- Với ảnh thân bài khi sửa PAGE, kiểm ảnh và các phần khác trước–sau, cả độ rõ/độ trong suốt/vị trí. Ảnh cắt/tile hoặc ánh xạ chưa hỗ trợ có thể khiến lượt sửa phục hồi; không lấy việc nhìn thấy ảnh làm bằng chứng thuật toán đã kiểm được mọi loại ảnh.
- Với khổ/lề, thử bản sao có header/footer thực sự trống. Sau duyệt phải giữ chữ/bảng/kiểu, nội dung nằm trong bốn lề và đúng khổ XPS. Part đầu/cuối trang có chữ/field/trang trí hoặc bố cục đặc biệt phải giữ thủ công; không tự xóa thành phần để mở quyền sửa.

Riêng chuyển PAGE, writer giữ part footer và quan hệ dù đã gỡ tham chiếu được duyệt. Nếu Word tự bỏ các part/quan hệ này khi lưu, hậu kiểm sẽ phục hồi và nhóm vẫn FAIL; chưa có kết quả Word thực để khẳng định mọi phiên bản Word giữ được chúng.

Các lượt thành công phải kiểm được một Hoàn tác, lưu bản sao/mở lại và nguồn đầu vào không đổi. Bài Office có nhánh cố tình gây lỗi để kiểm rollback; kết quả thực tế cần được giữ cùng thư mục mẫu.

Lệnh **Kiểm tra → Chừa chỗ ký tay (80 pt)** tiếp tục tính khoảng có sẵn và chỉ bổ sung thiếu khi đủ chỗ trên cùng trang, trong khối ký đã nhận diện. Không cần ảnh chữ ký. Khoảng 80 pt là lựa chọn sử dụng, không phải quy định NĐ30. Exactly/AtLeast và các bố cục còn giới hạn được ghi trong [REMAINING-WORK.md](REMAINING-WORK.md).
