# Chính tả Việt 0.1.76 — phạm vi quét và hậu kiểm khổ/lề/số trang

Bản thử tích lũy năm cụm đợt 15–19 sau 0.1.75. Ngày 29/10/2026 (UTC+7). Giữ danh mục 33 nhóm đề xuất có điều kiện / 8 thủ công / 1 minh họa, bộ đọc thử nghiệm tắt mặc định. Công việc tối ưu tốc độ dừng.

- Thêm **Kiểm tra → Phạm vi kiểm tra tài liệu…** và nút **Phạm vi đã kiểm…**. Hiện đủ 33 nhóm, số vùng đã quan sát và lý do chưa kiểm; số cảnh báo tách riêng. Quét không có lỗi không được trình bày thành đã kiểm toàn tài liệu.
- Hậu kiểm PAGE yêu cầu chữ số thực sự hiện đủ, đen đục, đúng phông/cỡ/đứng và cùng trục trang. Số ẩn, mờ, cắt hoặc mask chưa rõ không đạt. Giữ paint/độ hiển thị chữ, nét và footer trước–sau.
- Bảo toàn bitmap trong phạm vi đã chứng minh bằng pixel giải mã, ánh xạ toàn ảnh và độ hiển thị; chấp nhận tài nguyên XPS đổi tên/mã hóa nhưng giữ cùng ảnh. Ảnh bị cắt/tile hoặc màu/ánh xạ chưa rõ vẫn giữ thủ công/phục hồi. Đây là bảo toàn ảnh thông thường khi sửa PAGE, không mở rộng nhận diện chữ ký.
- Cho chuyển footer chỉ chứa PAGE đã chứng minh lên giữa lề trên trong một section dọc, số Ả Rập từ 1, từ 1 đến 9 trang. Chỉ bỏ glyph nguồn đã ghép duy nhất trong hậu kiểm trước–sau; giữ part/quan hệ footer, phần không chọn và một Undo. Footer nhiều thành phần hoặc field/bố cục chưa rõ vẫn thủ công.
- Cho sửa khổ/lề khi các header/footer reference trỏ tới part thực sự trống đã kiểm cấu trúc/style. Candidate giữ các part khác; sau Word phải chứng minh nội dung nằm trong bốn lề đã duyệt. Tràn lề, thay nội dung đầu/cuối trang hoặc thiếu bằng chứng phải phục hồi.

Khoảng ký tay khoảng 80 pt theo nhu cầu in rồi ký giữ nguyên; không cần ảnh chữ ký và không coi 80 pt là quy định NĐ30. Chưa mở tự sửa Exactly/AtLeast, khoảng đoạn gộp chưa đo được, nhiều section, header phức tạp, dịch dọc số/ngày hoặc khối ký chưa rõ. Xem [phạm vi](SUPPORTED-FIXES-0.1.76.md) và [các phần còn thiếu](REMAINING-WORK.md).

Thêm hai bài độc lập **Chuyển PAGE cuối trang** và **Khổ/lề với đầu–cuối trang trống** trong menu kiểm từng nhóm Word, tổng tám nhóm. Không cần chờ Subject của bài toàn bộ.

Số kiểm, commit và run thực của gói nằm trong build-checks.json/TXT và ghi chú release. **CI chạy Core/OpenXML, WPF và biên dịch ứng dụng; không chạy Microsoft Word. Chưa xác nhận bài Word thực đã PASS phiên bản này.** Gói chỉ được công bố sau kiểm đúng commit; giữ main và tag/gói cũ.

Giải nén vào thư mục mới, chạy ChinhTa.Desktop.exe, dùng bộ đọc cũ và bắt đầu bằng kiểm từng nhóm Word. [Hướng dẫn và các file cần giữ](OFFICE-CHECK-0.1.76.md).
