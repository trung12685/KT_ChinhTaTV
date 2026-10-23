# 0.1.45 — Ví dụ lỗi trong báo cáo tiến độ (bản thử)

Tiếp nối 0.1.44, commit `009b8c806a6b17897ab70b75551fde7d435ff0be`. Theo yêu cầu người dùng, bổ sung ví dụ cho các nhóm đã hỗ trợ sửa, chưa tự sửa và các việc dự kiến vào báo cáo xem/xuất TXT.

## Nội dung báo cáo

Mỗi mục trong 25 mục hiện có đều có ví dụ và kết quả/hướng xử lý:

- 16 nhóm C01–C16 đã hỗ trợ đề xuất sửa có điều kiện: nêu ví dụ trước sửa và kết quả minh họa sau khi duyệt.
- 8 mục M01–M08 chưa tự sửa: nêu tình huống và cách xử lý hiện tại/thủ công.
- D01 giữ riêng là quy tắc minh họa cần đọc ngữ cảnh.
- 5 hướng P01–P05 dự kiến có ví dụ: vị trí dọc số/ngày, các vùng thể thức còn thiếu, dấu giữa câu, dữ liệu chính tả và ngữ cảnh làm sau cùng. Ghi rõ chưa triển khai sửa tự động, chưa phải chức năng có thể bấm sửa hoặc cam kết thời điểm hoàn thành.

Ví dụ C01 giữ đúng khoảng trắng để dễ thử:

```text
C01 — Khoảng trắng thừa
  Ví dụ: “hôm       nay là thứ 2”
  Kết quả minh họa khi duyệt sửa: hôm nay là thứ 2
```

Các mục phông/vị trí dùng mô tả cụ thể, ví dụ dòng Arial đỏ/cỡ 12 hoặc Line dài 50 pt dưới chữ rộng 210 pt; kết quả ghi rõ điều kiện, định dạng/số đo và hậu kiểm cần có. Tab được viết bằng ký hiệu [TAB] để không làm mất cách đọc ví dụ.

Trạng thái đã hỗ trợ/chưa tự sửa/dự kiến là tiến độ phát triển chương trình, không phải số lỗi hay lịch sử bấm sửa trong tài liệu đang mở. Dữ liệu ví dụ đều là mẫu giả; báo cáo không đọc nội dung DOCX. Cửa sổ Trợ giúp và TXT dùng chung dữ liệu/renderer nên có cùng ví dụ, dấu cách và giới hạn.

## Kiểm tra và phát hành

Đối chiếu các ví dụ trước/sau sửa chữ với quy tắc thực thi hiện tại, gồm C01, C05, C06, C13–C16; các ví dụ dự kiến không được nhận là chức năng sửa chữ hiện có. Kiểm danh mục có đủ ví dụ, mốc lịch sử duy nhất, trạng thái rõ; WPF kiểm hiển thị ví dụ và xuất nguyên văn UTF-8. CI kiểm báo cáo mẫu bằng dữ liệu của gói đã publish trước khi đóng ZIP.

Danh mục khả năng sửa vẫn 16 nhóm/8 thủ công/1 minh họa, theo [phạm vi .44](SUPPORTED-FIXES-0.1.44.md). Bản này bổ sung báo cáo và tài liệu; các quy tắc sửa Word, giao dịch/rollback, Undo, kiểm nét XPS, lưu bản sao và checkpoint giữ nguyên.

**Chưa có bằng chứng 0.1.45 chạy đạt toàn bộ bài Word trên Office 2021.** CI không có Word. Giữ main, stable .27 và mọi tag/gói cũ; bản mới tiếp tục thử nghiệm.
