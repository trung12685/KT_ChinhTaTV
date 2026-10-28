# Chính tả Việt 0.1.75 — khối ký, khoảng đoạn và kiểm Word theo nhóm

Bản thử tích lũy năm cụm đợt 10–14 sau 0.1.74. Ngày 28/10/2026 (UTC+7). Giữ danh mục 33 nhóm đề xuất có điều kiện / 8 thủ công / 1 minh họa, bộ đọc thử nghiệm tắt mặc định. Không tiếp tục tối ưu tốc độ.

- Khối ký trong bảng cuối tài liệu dài tiếp tục được quan sát khi XML và range Word đã chứng minh đủ vai trò; không dùng cờ bảng chưa quan sát sau đoạn 40 để bỏ cả nhóm.
- Bảo toàn ảnh inline đã có: đối chiếu đúng ký tự đối tượng Word, giữ đoạn ảnh không được sửa chữ/phông; ảnh không trở thành Line. Không tiếp tục mở rộng tính năng ảnh chữ ký trong giai đoạn này.
- Thêm **Kiểm tra → Chừa chỗ ký tay (80 pt)**. Tính cả khoảng trắng sẵn, chỉ bổ sung thiếu; giữ vùng từ 77 pt trở lên. Chỉ đổi khoảng trước tên, đo lại XPS và lưu/mở lại, giữ trang/chữ/phông/phần gói khác. Một Hoàn tác, phục hồi nếu không đủ chỗ hoặc hậu kiểm thất bại. 80 pt là lựa chọn người dùng, không phải quy định NĐ30. Phạm vi hiện tại là khối ký Nhà nước trong bảng cuối rõ, không ảnh/đối tượng ở vùng ký.
- Chặn quyền sửa khoảng đoạn/giãn dòng khi XML còn nguồn đơn vị dòng kế thừa hoặc bố cục đặc biệt chưa hỗ trợ. Hiển thị phần spacing chưa kiểm, kể cả Exactly/AtLeast; không kết luận sai từ tên chế độ hoặc khoảng trắng giữa hai hộp mực chữ.
- Thêm **Kiểm tra → Kiểm tra từng nhóm Word…**. Sáu nhóm dùng phiên Word và mẫu riêng, bộ đọc cũ; lỗi nhóm trước không ngăn nhóm sau. Ghi PASS/FAIL/NOT_RUN từng nhóm ngay khi xong. Kết quả chỉ áp dụng các nhóm đã chọn, không thay kết quả toàn bài hoặc xác nhận bộ đọc thử nghiệm.

Đã phân tích giới hạn và thứ tự hoàn thiện trong [REMAINING-WORK.md](REMAINING-WORK.md), chi tiết cụm ở FORMAT-BATCH-10.md đến FORMAT-BATCH-14.md. Chưa mở tự sửa Exactly/AtLeast, nhiều section, bố cục trang đặc biệt, dịch dọc số/ngày hoặc khối ký chưa rõ. Chưa phải bộ kiểm chính tả tiếng Việt đầy đủ.

Số kiểm, commit và run thực của gói nằm trong build-checks.json/TXT và phần ghi chú release. **CI không chạy Microsoft Word; chưa xác nhận toàn bài Word thực đã PASS phiên bản này.** Bài kiểm nhóm được bổ sung để lấy kết quả cụ thể thay vì chỉ dừng ở Subject. Giữ main, checkpoint và tag/gói cũ.

Giải nén vào thư mục mới, mở ChinhTa.Desktop.exe; trước hết chạy Kiểm tra từng nhóm Word, sau đó bài Kiểm tra Word tự động. [Hướng dẫn và các file cần giữ](OFFICE-CHECK-0.1.75.md).
