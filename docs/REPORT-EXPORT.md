# Xem và xuất tiến độ, chức năng

Từ 0.1.41, chọn **Trợ giúp → Tiến độ và chức năng…**. Mở được khi chưa mở DOCX, không cần Word cho riêng thao tác xem/xuất báo cáo.

Báo cáo gồm phiên bản/thời điểm, số nhóm chức năng tính từ danh mục, loại lỗi có thể đề xuất sửa, các mục chỉ cảnh báo, lịch sử phần đã làm, bằng chứng kiểm thử đi kèm gói, giới hạn và việc tiếp theo. Các số nhóm không phải số lỗi trong tài liệu đang mở; báo cáo chưa thống kê lịch sử bấm sửa của từng tài liệu.

Bấm **Xuất báo cáo TXT…**, chọn thư mục/tên `.txt`. Tệp đọc đúng tiếng Việt trong Notepad hoặc Word. Muốn thay báo cáo cũ thì chọn cùng tên và xác nhận thay tệp theo hộp thoại lưu. Có thể chọn/sao chép nội dung trong cửa sổ. Lỗi lưu không thay tài liệu hoặc xóa danh sách lỗi chính; xử lý quyền/khóa hay chọn nơi lưu khác rồi thử lại.

Gói Windows kèm `build-checks.json` để báo đúng số kiểm thử đã đạt của đúng phiên bản, cùng báo cáo TXT mẫu tạo ở CI. Giữ các tệp đi kèm khi giải nén. Nếu thiếu hoặc sai dữ liệu kiểm thử, báo cáo vẫn xem/xuất được nhưng ghi rõ chưa có thông tin phù hợp; không tự suy PASS. Đây là thông tin kiểm thử tự động đi kèm gói, không thay xác nhận Word/Office 2021 hoặc kết quả kiểm tài liệu thực.

Thứ tự tiếp theo: hoàn thiện thể thức/độ ổn định; lỗi chữ và lặp cơ bản; ngữ cảnh/thư viện sau cùng. Các chức năng mới phải có mẫu và kiểm thử trước khi ghi vào danh mục. [Danh sách lỗi chi tiết hiện có](SUPPORTED-FIXES-0.1.46.md) và [kịch bản Windows/Office](WINDOWS-VALIDATION.md).

Từ .42 danh mục có 14 nhóm đề xuất sửa (thêm C14 về khoảng trắng quanh dấu phẩy/chấm phẩy), 8 nhóm thủ công và 1 minh họa. Đọc điều kiện của từng nhóm; không coi tổng nhóm là số lỗi trong văn bản.

Từ .43 có 15 nhóm đề xuất sửa (thêm C15 về khoảng trắng sát trong ngoặc tròn), 8 nhóm thủ công và 1 minh họa. Ô ngoặc chỉ chứa dấu cách được giữ cả khi sửa tất cả; dấu câu chưa chọn vẫn được quét sau sửa mép ngoặc. Mỗi nhóm vẫn có điều kiện, chưa phải kiểm đầy đủ chính tả/dấu câu.

Từ .44 có 16 nhóm đề xuất sửa (C16: dấu cách trước dấu .?! cuối đoạn có điều kiện), 8 nhóm thủ công và 1 minh họa. Không coi danh mục là đã kiểm toàn bộ chính tả/dấu câu; kết quả thực tế vẫn cần kiểm trên Word.

## Ví dụ lỗi từ .45

Mỗi mục C01–C16, M01–M08 và D01 có **Ví dụ** và kết quả/hướng xử lý. Báo cáo giữ khoảng trắng thật, ví dụ:

```text
C01 — Khoảng trắng thừa
  Ví dụ: “hôm       nay là thứ 2”
  Kết quả minh họa khi duyệt sửa: hôm nay là thứ 2
```

Phân biệt ba trạng thái phát triển: đã hỗ trợ đề xuất sửa (cần duyệt/đủ điều kiện); chưa tự sửa (chỉ cảnh báo và hướng thủ công); dự kiến (P01–P05 có ví dụ, chưa triển khai sửa tự động). D01 là minh họa riêng. Các hướng dự kiến gồm vị trí dọc số/ngày, thể thức vùng còn thiếu, dấu giữa câu, dữ liệu chính tả và ngữ cảnh sau cùng; không cộng vào 16 nhóm có thể đề xuất sửa.

Ví dụ phông/vị trí nêu cả kiểu hoặc số đo. [TAB] chỉ là ký hiệu tab. Các ví dụ là mẫu giả để thử, không lấy từ tài liệu đang mở và không có nghĩa chương trình đã sửa các lỗi đó trong tài liệu của bạn. Cửa sổ và TXT dùng cùng nội dung; số kiểm thử vẫn lấy từ dữ liệu đúng gói, không suy từ ví dụ. Các mục mới sau này cần có ví dụ/hướng xử lý tương ứng.

Từ .46 có 17 nhóm đề xuất sửa, thêm C17: `Ghi chú : nội dung` → `Ghi chú: nội dung` theo điều kiện. Mỗi mục mới tiếp tục có ví dụ và kết quả/hướng xử lý; 8 nhóm thủ công, 1 minh họa và 5 hướng dự kiến giữ trạng thái riêng.
