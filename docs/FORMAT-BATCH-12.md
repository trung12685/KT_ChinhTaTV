# Đợt thể thức12 — chừa chỗ ký tay khoảng80pt

Ngày28/10/2026 (UTC+7). Theo yêu cầu người dùng: tài liệu thường in rồi mới ký, ưu tiên chừa khoảng trống ký tay80pt; không ưu tiên phát triển thêm nhận diện ảnh chữ ký. Tiếp từ đợt11/c5c08d18, mã0.1.75, cụm thứ ba sau gói0.1.74. Chưa ZIP mới.

## Lệnh sử dụng

Mở tài liệu rồi chọn **Kiểm tra → Chừa chỗ ký tay (80 pt)**. Đây là lệnh chủ động cho khối ký Nhà nước đã chứng minh trong bảng cuối: Nơi nhận/Lưu ở trái, chức vụ/họ tên rõ ở phải; vùng giữa chỉ có các đoạn trống. Không yêu cầu ảnh chữ ký. Có ảnh/đối tượng, tên/chức vụ chưa rõ, bảng gộp/lồng, chiều cao cố định hoặc định dạng đặc biệt thì giữ nguyên và báo phạm vi chưa hỗ trợ.

80pt là thiết lập theo yêu cầu người dùng, không phải một quy định bắt buộc của NĐ30. Lệnh không nằm trong nút Sửa tất cả và không trộn với lượt chọn lỗi chữ/phông; mỗi lần thực hiện thành công có một Hoàn tác. Các kiểm phông khối ký hiện có giữ riêng.

## Cách đo và sửa

Đo trên XPS khoảng giữa vùng chữ của dòng chức vụ cuối và vùng chữ họ tên. Tính cả khoảng của Enter đang có, không cộng mù80pt hoặc đếm số dòng trắng. Vùng đã từ77pt trở lên được giữ nguyên, không tạo Undo rỗng. Vùng thiếu được điều chỉnh khoảng trước họ tên đến80±3pt, tối đa12 lần đo, ghi theo độ phân giải twip của Word.

Các lần đo cần chữ hiện đủ, đúng một dòng tên, chức vụ cuối đã đo rõ; không dùng chữ bị ẩn/cắt. Hai mốc phải ở cùng trang ban đầu và giữ số trang. Khoảng quá lớn trong lần thử được thu hẹp bằng phản hồi số đo; Word gộp spacing hoặc bỏ qua thuộc tính không được tự coi là thành công. Không đạt, thiếu mốc hoặc không đủ chỗ trên trang này thì phục hồi.

Chỉ ghi SpaceBefore và tắt SpaceBeforeAuto của đoạn tên. Giữ chữ, Enter, phông/cỡ/đậm/nghiêng/gạch chân, căn/thụt/giãn dòng, khoảng sau tên và các đoạn khác. Đọc đủ style ký tự trước/sau; kiểm toàn gói WordOpenXML chỉ cho đổi thuộc tính trước tên, giữ nội dung/phần khác kể cả footer và dữ liệu ảnh. Chỉ bỏ qua revision IDs/hint proofing/phân trang và core/app metadata tự cập nhật; không bỏ qua chữ hay style. Lưu checkpoint, đóng/mở lại phiên Word riêng, đo lại và hậu kiểm trước khi công bố một Undo. Lỗi sau bước cuối cũng phải phục hồi cả lượt.

## Kiểm chứng và giới hạn

Core kiểm nhận diện vùng trống ngắn/dài/ngắt mềm, mốc sai/profile khác/đối tượng/cấu trúc đặc biệt; phản hồi spacing cộng và bị gộp, làm tròn twip, vùng đủ, thiếu chỗ/đổi trang/thêm trang, mất glyph, lỗi xác minh muộn và lỗi phục hồi. XML proof kiểm phần được chọn, thay chữ/font/khoảng khác/footer/ảnh.

Windows kiểm writer chỉ ghi đúng thuộc tính, nguồn cũ/setter sai/COM lỗi, XPS qua transform theo point, chữ ẩn/bị clip và mẫu DOCX không ảnh có0/2Enter sẵn. Bài Office mới dùng hai mẫu có chỗ bố trí dự kiến đủ, thử lỗi hậu kiểm có chủ đích, giữ chữ/phông, một Undo, chạy lại không sửa và lưu/mở. CI chỉ biên dịch bài Office, chưa chạy Word thực.

Phạm vi bảo thủ: Word tự chuẩn hóa cấu trúc/phần khác ngoài thuộc tính cho phép hoặc khoảng chỉ đạt khi đổi trang sẽ khiến lượt sửa phục hồi. Chưa xác nhận toàn bài Word thực0.1.74/75 đã đạt. Giữ bộ đọc cũ, dừng tối ưu tốc độ; không phát triển thêm ảnh chữ ký ở giai đoạn tiếp theo. Danh mục vẫn33nhóm/8thủ công/1minh họa; lệnh80pt là tiện ích chủ động của C31. Tích lũy5–10cụm trước gói mới; gói/main/tag cũ giữ nguyên.
