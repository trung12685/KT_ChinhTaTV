# Thử Windows 0.1.85

Bản gợi ý chính tả tiếng Việt duyệt tay. Cần Windows x64 và Microsoft Word. Giải nén toàn bộ gói vào thư mục mới, chạy ChinhTa.Desktop.exe. Không cần Python/PySide. Sửa thủ công vẫn là mặc định.

1. Vào **Chính tả → Nạp dữ liệu chính tả từ ZIP…**, chọn `2026-10-24.zip` đã gửi. Đợi báo nguồn hongocduc/tudientv/wiktionary, 78.239 mục từ, mã `C82CE0AD7C2A`. Dữ liệu lưu trên máy cho lần sau; lỗi nạp giữ bộ trước. Có thể dùng lại bộ mẫu nhỏ từ cùng menu.
2. Mở bản DOCX thử có đoạn chữ thuần: `Giải quyết dứt điễm vụ việc. Kết quả thật sáng lạng.` Khi chưa nạp ZIP, hai nhóm lỗi mẫu này cũng hoạt động. Xem nhóm **Chính tả tiếng Việt — cần duyệt**, gạch vàng và chi tiết nguồn.
3. Với từ đơn, bấm chữ trên trang. Với cả cụm `sáng lạng`, chọn cảnh báo và bấm **Ô sửa từ/cụm hoặc đoạn đang chọn…**. Chọn `điểm` hoặc `xán lạn` chỉ điền bản nháp; đọc lại rồi bấm **Ghi chữ tôi sửa** / Enter. Có thể tự gõ cách khác hoặc Hủy. Không sửa chính tả hàng loạt.
4. Kiểm chữ ngoài vùng chọn và kiểu chữ còn nguyên, gợi ý chỗ đã sửa hết, cảnh báo chưa sửa còn. Hoàn tác một lần, lưu bản sao riêng, mở lại trong Word để so sánh. Tệp gốc được giữ nguyên.
5. Chạy **Kiểm tra → Kiểm tra từng nhóm Word…**, chọn **Duyệt gợi ý chính tả và sửa đúng từ/cụm**. Hoặc `ChinhTa.Desktop.exe --word-check vietnamese-spelling`. Bài dùng mẫu riêng của dự án, không cần ZIP. Giữ TXT/diagnostics khi lỗi.

Chưa nạp dữ liệu lớn chỉ có vài nhóm mẫu, không phải kiểm chính tả đầy đủ. Gợi ý ngữ cảnh là để tham khảo, chưa bảo đảm mọi câu sai/đúng. Bản thử sửa từ/cụm trong đoạn thuần ngoài bảng, không phải Word nhúng toàn trang. Vùng nhiều kiểu/field/đối tượng hoặc mốc không chắc cần sửa từng vùng hoặc trong Word rồi quét lại.

Core/OpenXML và WPF được kiểm trên Windows CI; Word thực **NOT_RUN** trước khi người dùng chạy. Kết quả build đúng gói nằm trong build-checks.json. Hướng dẫn nguồn/phạm vi/đánh giá tại FORMAT-BATCH-83.md. Dữ liệu lớn không nằm trong ZIP ứng dụng.
