# Đợt20 — PAGE cuối trang nhiều chữ số

Mã phát triển0.1.77, chưa có ZIP mới. Mở phạm vi C33 từ1–9 lên1–10.000trang, giữ một section dọc và các điều kiện nguồn PAGE-only cũ.

Ghép các glyph số liền nhau theo dòng nền, chiều cao và khoảng ngang; không chọn chuỗi con trong một cụm dài hơn. Cụm thiếu/thừa, khác kiểu, chồng, lệch dòng hoặc không duy nhất phải thất bại. Chỉ loại các glyph đã ghép đúng số nguồn ra khỏi snapshot trước sửa; sau sửa kiểm số dư bằng cùng phép ghép, giữ toàn bộ nội dung khác. Mỗi vùng cuối trang giới hạn128glyph số để giữ phạm vi hữu hạn.

Bài WPF đối chiếu tài liệu12/101trang với số nguyên cụm và tách từng chữ số; kiểm mất/ẩn/lệch/chồng/thừa/sai màu/đậm. Core kiểm các mốc9/10/99/100/999/1000/10000 và chặn10001. Thêm nhóm Word độc lập “Chuyển PAGE từ10trang”, dùng mẫu bổ sung10ngắt trang; tổng9nhóm. Các bài Office tiếp tục có rollback muộn, mộtUndo, lưu/mở và nguồn bất biến.

Giữ điều kiện bảo toàn part/quan hệ footer gốc; Word tự bỏ chúng thì phục hồi. CI không chạy Microsoft Word. Chưa mở nhiều section, footer có nhãn/trang trí hoặc PAGE bắt đầu lại; không thay công việc Exactly/AtLeast còn thiếu. Bộ đọc thử nghiệm OFF, tối ưu tốc độ dừng; khoảng ký tay80pt giữ nguyên.
