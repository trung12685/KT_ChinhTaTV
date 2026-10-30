# Thử Windows 0.1.86 — có sẵn bộ chính tả

Giải nén toàn bộ ZIP ứng dụng vào thư mục mới rồi chạy `ChinhTa.Desktop.exe`. Giữ thư mục `spelling` cạnh chương trình. Không cần Python, Internet hoặc nạp từ điển trong lần mở đầu.

Máy chưa nạp riêng sẽ hiện **Bộ kèm ứng dụng v1 — Viet74K + UD Vietnamese-VTB**, 72.535 mục từ. Máy đã nạp `2026-10-24.zip` tiếp tục ưu tiên bộ riêng. Nếu muốn dùng bộ chung, chọn **Chính tả → Dùng bộ dữ liệu kèm ứng dụng**. Chỉ cần nạp ZIP khi chủ động dùng bộ dữ liệu riêng; nâng cấp không yêu cầu nạp lại.

Thử đoạn chữ thuần: `Cần sử lý hồ sơ, bổ xung nhân lực và giải quyết dứt điễm vụ việc.` Chọn gợi ý vàng, mở ô sửa từ/cụm rồi tự gõ hoặc chọn ứng viên. Chỉ khi bấm Ghi / Enter mới thay chữ; Hoàn tác trả lại lượt sửa. Kiểm và lưu bản sao, mở lại trong Word.

Vào **Chính tả → Nguồn và giấy phép dữ liệu…** để xem bộ đang dùng và nguồn bộ kèm. Nếu báo bộ kèm thiếu/hỏng, giải nén lại toàn bộ gói; dữ liệu riêng đã lưu được giữ khi nạp không thành công.

Bộ ngữ cảnh chung còn nhỏ, gợi ý thận trọng và có bỏ sót; bộ riêng của bạn có thể khác kết quả. Chưa phải bảo đảm chính tả toàn diện. Sửa trực tiếp vẫn giới hạn đoạn thuần ngoài bảng/field/đối tượng. Sửa thủ công là mặc định; không sửa chính tả hàng loạt.

Core/WPF, bộ dữ liệu trong thư mục publish và sau giải nén ZIP được kiểm trên Windows CI. Microsoft Word thực **NOT_RUN**. Chạy nhóm `vietnamese-spelling` trong Kiểm tra từng nhóm Word để xác nhận ghi và Undo trên máy bạn. Chi tiết chất lượng/nguồn tại FORMAT-BATCH-84.md.
