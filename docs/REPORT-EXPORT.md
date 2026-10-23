# Xem và xuất tiến độ, chức năng

Từ 0.1.41, chọn **Trợ giúp → Tiến độ và chức năng…**. Mở được khi chưa mở DOCX, không cần Word cho riêng thao tác xem/xuất báo cáo.

Báo cáo gồm phiên bản/thời điểm, số nhóm chức năng tính từ danh mục, loại lỗi có thể đề xuất sửa, các mục chỉ cảnh báo, lịch sử phần đã làm, bằng chứng kiểm thử đi kèm gói, giới hạn và việc tiếp theo. Các số nhóm không phải số lỗi trong tài liệu đang mở; báo cáo chưa thống kê lịch sử bấm sửa của từng tài liệu.

Bấm **Xuất báo cáo TXT…**, chọn thư mục/tên `.txt`. Tệp đọc đúng tiếng Việt trong Notepad hoặc Word. Muốn thay báo cáo cũ thì chọn cùng tên và xác nhận thay tệp theo hộp thoại lưu. Có thể chọn/sao chép nội dung trong cửa sổ. Lỗi lưu không thay tài liệu hoặc xóa danh sách lỗi chính; xử lý quyền/khóa hay chọn nơi lưu khác rồi thử lại.

Gói Windows kèm `build-checks.json` để báo đúng số kiểm thử đã đạt của đúng phiên bản, cùng báo cáo TXT mẫu tạo ở CI. Giữ các tệp đi kèm khi giải nén. Nếu thiếu hoặc sai dữ liệu kiểm thử, báo cáo vẫn xem/xuất được nhưng ghi rõ chưa có thông tin phù hợp; không tự suy PASS. Đây là thông tin kiểm thử tự động đi kèm gói, không thay xác nhận Word/Office 2021 hoặc kết quả kiểm tài liệu thực.

Thứ tự tiếp theo: hoàn thiện thể thức/độ ổn định; lỗi chữ và lặp cơ bản; ngữ cảnh/thư viện sau cùng. Các chức năng mới phải có mẫu và kiểm thử trước khi ghi vào danh mục. [Danh sách lỗi chi tiết hiện có](SUPPORTED-FIXES-0.1.44.md) và [kịch bản Windows/Office](WINDOWS-VALIDATION.md).

Từ .42 danh mục có 14 nhóm đề xuất sửa (thêm C14 về khoảng trắng quanh dấu phẩy/chấm phẩy), 8 nhóm thủ công và 1 minh họa. Đọc điều kiện của từng nhóm; không coi tổng nhóm là số lỗi trong văn bản.

Từ .43 có 15 nhóm đề xuất sửa (thêm C15 về khoảng trắng sát trong ngoặc tròn), 8 nhóm thủ công và 1 minh họa. Ô ngoặc chỉ chứa dấu cách được giữ cả khi sửa tất cả; dấu câu chưa chọn vẫn được quét sau sửa mép ngoặc. Mỗi nhóm vẫn có điều kiện, chưa phải kiểm đầy đủ chính tả/dấu câu.

Từ .44 có 16 nhóm đề xuất sửa (C16: dấu cách trước dấu .?! cuối đoạn có điều kiện), 8 nhóm thủ công và 1 minh họa. Không coi danh mục là đã kiểm toàn bộ chính tả/dấu câu; kết quả thực tế vẫn cần kiểm trên Word.
