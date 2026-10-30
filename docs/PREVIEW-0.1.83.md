# Chính tả Việt 0.1.83 — sửa trích yếu theo NĐ30

Bản thử tiếp từ 0.1.82, sửa cảnh báo nhầm công văn với văn bản có tên loại. Căn cứ và giới hạn: [FORMAT-BATCH-81.md](FORMAT-BATCH-81.md). Giữ sửa thủ công mặc định; ô sửa đoạn vẫn chưa phải soạn thảo toàn trang hoặc Word nhúng.

## Thử trên Word

Giải nén toàn bộ vào thư mục mới và chạy ChinhTa.Desktop.exe trên Windows có Word desktop đã kích hoạt. Mở bản sao tài liệu của bạn, kiểm cảnh báo:

- Công văn V/v dưới số/ký hiệu trong ô trái: cỡ 12 hoặc 13, đứng thường không bị yêu cầu sửa thành đậm/14 hoặc thêm đường kẻ trích yếu.
- Quyết định/Báo cáo/Kế hoạch: tên loại và trích yếu ngoài bảng, căn giữa, đứng đậm cỡ 13 hoặc 14 vẫn hợp lệ; giữ yêu cầu nét dưới trích yếu.
- Không dùng bảng không tự thành lỗi. Nếu mốc không rõ, báo chưa xác định loại; chưa suy thành thiếu nét.

Chạy **Kiểm tra → Kiểm tra từng nhóm Word… → Trích yếu công văn và văn bản có tên loại**, hoặc:

```powershell
.\ChinhTa.Desktop.exe --word-check document-subject
```

Nhóm này độc lập, không đợi Subject/Line của bài tổng hợp. Có tổng 17 nhóm; KIEM-TRA-WORD.cmd cũ không tự chọn bài mới. Gửi toàn thư mục OfficeGroups của lượt chạy, office-groups.txt, diagnostics.log và build-checks.json nếu thất bại.

## Giới hạn

Cỡ 12–13/13–14 là khoảng NĐ30, không ép mọi dòng về 13/14. Chỉ đề xuất sửa phông khi role và chữ đã khớp, giữ từng cỡ hợp lệ. Căn dọc, khoảng 6 pt dưới số/ký hiệu và nét dư vẫn duyệt riêng; phần mềm chưa tự sửa các mục này. C10 quan sát phông không có nghĩa đã xác nhận toàn thể thức.

Mẫu ngoài bảng chỉ nhận khi đủ quốc hiệu/tiêu ngữ, số và V/v/greeting. Khối nhiều đoạn/đối tượng hoặc dấu hiệu mâu thuẫn có thể chưa nhận diện. Không tự xóa đường kẻ có sẵn của người dùng. Sửa thủ công trực tiếp từng chữ trên trang vẫn chưa triển khai; [hướng dẫn ô sửa đoạn](PREVIEW-0.1.82.md) còn áp dụng, riêng số nhóm hiện là 17.

**Microsoft Word thực tế: NOT_RUN.** Bản thử được kiểm Core/OpenXML/WPF và gói Windows; cần chạy nhóm mới trên máy có Word trước khi kết luận tương thích. Không thay main, tag hoặc release cũ.
