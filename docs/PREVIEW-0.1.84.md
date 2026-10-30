# Chính tả Việt 0.1.84 — sửa từng từ ngay trên trang

Bản thử giữ sửa thủ công mặc định và quy tắc trích yếu NĐ30 của 0.1.83. Chưa phải trình Word nhúng; nghiên cứu và giới hạn: [FORMAT-BATCH-82.md](FORMAT-BATCH-82.md).

## Cách thử

1. Giải nén toàn bộ gói Windows vào thư mục mới, chạy ChinhTa.Desktop.exe trên Windows có Word desktop đã kích hoạt, mở bản sao DOCX.
2. Giữ chế độ **Sửa thủ công**, bấm vào một từ trên trang. Không cần từ đó đã bị cảnh báo. Ô nhập mở ngay tại chữ và chọn sẵn từ đó; dùng phím mũi tên/chuột để sửa từng ký tự trong từ nếu cần.
3. Gõ chữ đúng rồi bấm **Ghi chữ tôi sửa** hoặc Enter sau khi hoàn tất nhập tiếng Việt. Chỉ từ đã chọn được thay, không gửi cả đoạn cho máy tự sửa. Có thể thay bằng cụm ngắn hoặc xóa từ. Không ghi khi chỉ bấm, hover hoặc chuyển focus.
4. Trang dựng lại bằng Word và cảnh báo được quét lại. Một Hoàn tác khôi phục một lần ghi từ. Lưu bản sao, đóng/mở lại để kiểm chữ và định dạng ngoài chỗ sửa.
5. Esc/nút Hủy đóng ô; nếu có bản nháp sẽ hỏi bỏ. Khi ghi hoặc quét lỗi, bản nháp được giữ; nếu XPS bị đóng ô nhập chuyển về giữa cửa sổ. Sao chép nháp nếu cần. Không tự gửi lại khi chưa biết kết quả.

Nếu bấm khoảng trống hoặc không đủ bằng chứng glyph thì không chọn đoán từ gần đó. Nếu chưa ghép được một vị trí Word duy nhất hoặc vùng đặc biệt thì thanh trạng thái giải thích. Nút **Ô sửa đoạn đang chọn…** vẫn dành cho luồng cũ.

## Phạm vi hiện tại

Đoạn thuần ngoài bảng và một vùng chữ đã chứng minh; chưa sửa bảng/field/bookmark/comment/lịch sử/đối tượng hoặc từ qua nhiều vùng định dạng. Từ trên phần tiếp theo của một đoạn qua trang có thể chưa ghép được. Chưa chọn nhiều đoạn, Enter tạo đoạn, kéo chọn tự do, đổi phông hoặc reflow theo từng phím như Word. Giữ giới hạn bảo toàn DOCX, không xuất lại toàn tài liệu từ RichTextBox.

Thử thêm chữ Việt NFC/NFD, UniKey/IME, màn hình 100/125/150%, zoom trang và cuộn. Không có kiểm từ vựng/ngữ nghĩa mới; người dùng có thể tự sửa từ chưa bị phát hiện. Microsoft Word thực tế **NOT_RUN** trong CI.

## Kiểm Word riêng

**Kiểm tra → Kiểm tra từng nhóm Word… → Bấm chữ trên trang và sửa từng từ**, hoặc:

```powershell
.\ChinhTa.Desktop.exe --word-check inline-word
```

Có tổng 18 nhóm; bài này độc lập, không cần bài Subject/Line trước đó. KIEM-TRA-WORD.cmd cũ không tự chọn nhóm mới. Gửi toàn thư mục OfficeGroups, office-groups.txt, diagnostics.log và build-checks.json nếu lỗi. Không kết luận Word PASS từ kết quả Core/WPF/gói.
