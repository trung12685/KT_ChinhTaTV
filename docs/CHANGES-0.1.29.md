# 0.1.29 — Giữ ô trống có ghi chú, phân biệt tab (bản thử)

Tiếp nối 0.1.28 (`5f3fdcbd2f4148ab4eaa0b172d0fb80f9d1debfb`). Mốc ổn định 0.1.27 và các tag/gói đã phát hành giữ nguyên. Phạm vi số/ký hiệu, ngày tháng và căn cứ NĐ30 của [0.1.28](CHANGES-0.1.28.md) tiếp tục áp dụng; bản này sửa hai lỗi biên đã tái hiện trong quá trình rà soát.

## Đã sửa

1. Với dòng đầu văn bản như `Địa danh, ngày        tháng 10 năm 2026 (dự thảo)  ghi chú`, 0.1.28 cảnh báo thủ công về mẫu có ghi chú nhưng quy tắc khoảng trắng vẫn đề xuất thu hẹp ô ngày trống. Nay giữ đúng các vùng ngày/tháng/năm chưa điền, kể cả có ghi chú; vẫn báo hai dấu cách ở `)  ghi chú`. Chỉ áp dụng ngoại lệ mở rộng trong đầu Nhà nước đã nhận diện, không miễn khoảng trắng cho mọi đoạn thân bài. Dòng có ghi chú vẫn cần duyệt thủ công, không được coi là đã đạt thể thức.
2. `w:pPr/w:tabs/w:tab` chỉ là điểm dừng tab trong định dạng, không phải ký tự tab. Nay nó không ngăn sửa chữ thuần đã đối chiếu với Word XML. Tab thật trong run, trường Word, control, hyperlink và dòng chưa khớp chắc chắn tiếp tục giữ thủ công.

Không thay các điều kiện kiểm marker DOCX, nét XPS thật, dòng tham chiếu, giới hạn bù vị trí, checkpoint/rollback và Undo Line. Không tự điền số/ngày, chọn mã cơ quan hoặc đổi địa danh.

## Kiểm thử và giới hạn bằng chứng

Đã thêm test có đối chiếu trước/sau: test giữ ô trống có ghi chú thất bại trên mã 0.1.28 và qua sau sửa. Kiểm riêng/ngày/tháng/năm trống, khoảng trắng trong ghi chú, ngày đã điền, thân bài ngoài phạm vi, điểm dừng tab và tab thật. Kiểm quét lại sau từng gợi ý và từng nhóm số/ngày với vị trí đoạn dịch theo độ dài chữ mới; không giữ CP cũ để giả lập kết quả.

Workflow Windows chạy toàn bộ Core/OpenXML, WPF và publish trước prerelease. Xem kết quả theo commit ở Actions/PR; CI không có Word. Office check bổ sung một mẫu giả tự tạo từ file đi kèm, sửa nhóm số và khoảng trắng ở ghi chú, kiểm nguyên văn/ô trống, lưu/mở và Undo. Việc thêm kịch bản chưa chứng minh đã chạy đạt trên Office 2021.

## Cách dùng thử

Giải nén vào thư mục mới, giữ `samples`. Chạy **Kiểm tra → Kiểm tra Word tự động…**, gửi `office-check.txt` nếu chưa đạt. Bài kiểm tự tạo `annotated-header-input.docx` trong thư mục kết quả; có thể mở bản sao mẫu này để thử giao diện: chỉ chọn các gợi ý được phép, sửa rồi kiểm ô ngày trống còn nguyên, khoảng trắng thừa trong ghi chú được xử lý và một Undo phục hồi cả lượt. Cảnh báo mẫu ngày có ghi chú vẫn còn để người dùng duyệt là đúng hành vi.

Thử lại sửa riêng/nhóm số và ngày trên hai mẫu 0.1.28, sau đó ba đường trên bản sao tài liệu cũ. Chưa nhập main hoặc coi 0.1.29 là bản ổn định trước khi có bằng chứng dùng thử. Không dùng số Shape, số cảnh báo giảm hoặc hết cảnh báo đơn thuần làm kết luận Word đã đạt.
