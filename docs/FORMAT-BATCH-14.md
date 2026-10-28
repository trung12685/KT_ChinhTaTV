# Đợt thể thức 14 — kiểm các nhóm Word độc lập

Ngày 28/10/2026 (UTC+7), mã phát triển 0.1.75, cụm thứ năm sau gói 0.1.74. Bài toàn bộ dừng khi lỗi đầu tiên; lỗi Subject trước đây khiến các bài nội dung/khối ký/trang phía sau không có cơ hội chạy. Giữ nguyên kết quả và hành vi bài toàn bộ; thêm **Kiểm tra → Kiểm tra từng nhóm Word…**.

Có thể chọn sáu nhóm: chỗ ký tay 80 pt, phông khối ký, khổ giấy/lề, số trang, khoảng đoạn/giãn dòng và phối hợp, phông/căn nội dung và phối hợp. Mỗi nhóm mở phiên Word riêng, nguồn mẫu giả và thư mục riêng, luôn dùng bộ đọc cũ. Mẫu khoảng đoạn được tạo trực tiếp; không đợi bài phông/Line phía trước vượt qua. Những bước phối hợp với Line trong chính nhóm vẫn bắt buộc và có thể khiến nhóm FAIL.

Kết quả trong `OfficeGroups-<id>/office-groups.txt`: PASS, FAIL hoặc NOT_RUN cho từng nhóm. Không mở được môi trường/mẫu/phiên là NOT_RUN; lỗi kiểm hoặc đóng phiên là FAIL. Ghi kết quả và mọi nhánh lỗi trước khi mở nhóm sau. Không ghi được báo cáo thì dừng với lỗi I/O; không gán lỗi đó thành kết quả giả của nhóm. Chỉ khi tất cả nhóm đã chọn đạt và mỗi nhóm xác nhận bước kiểm thực mới có SELECTED_PASS. Kết quả này không thay cho bài Word toàn bộ, không xác nhận bộ đọc thử nghiệm hoặc mọi tài liệu.

Kiểm Core cho tổng hợp, mở/đóng/lỗi kép, tiếp tục sau nhóm lỗi, không chọn nhóm, mã trùng, ghi kết quả theo thứ tự và I/O thất bại. WPF kiểm catalog/summary/phạm vi, phiên mặc định, schema mẫu modern và nguồn nội dung thuần không cần chạy Word. CI không chạy Microsoft Word; kết quả runtime cần nhận từ đúng gói trên máy có Office.

Xem [phần còn thiếu](REMAINING-WORK.md) và [cách kiểm bản 0.1.75](OFFICE-CHECK-0.1.75.md). Giữ danh mục 33/8/1, một Undo/rollback của các thao tác tài liệu, main và các tag/gói cũ; không mở thêm luật tự sửa chỉ để đạt bài kiểm.
