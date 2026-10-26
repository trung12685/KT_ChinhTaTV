# Chính tả Việt 0.1.73 — nhãn tương thích của Line đã bù tọa độ

Phản hồi 0.1.72 lúc 01:08 ngày 27/10/2026 (UTC+7), Word 16.0 Build 16.0.20326: khóa checkpoint, ghi/lưu/mở/Undo ba Line và sửa Agency đạt; vẫn dừng trước Subject. Nhờ bằng chứng mới, `modern-before-Subject.json` cho biết mốc/đo tham chiếu đầy đủ, nhưng Line cơ quan bị đánh dấu AppOwned=false/HasGroupedShapes=true.

Checkpoint cho thấy sau bù tọa độ, Word lưu VML alt dưới dạng `Title: <ý định tọa độ> - Description: <marker vai trò>`. Bản 0.1.72 chỉ nhận dạng alt bằng Description đơn. XPS cùng bước chỉ có một nét cơ quan ở vị trí đã sửa (154,45;103 pt), không phải nét lệch của lần thử trước. Đây là trường hợp nhãn lưu lại chưa được bao phủ trong bản vá trước.

## Sửa và kiểm

- Giữ dạng Description đơn; thêm đúng dạng nhãn ghép Word đã ghi, đối chiếu toàn bộ với Title ý định hợp lệ và Description của cùng đối tượng. Không dò substring, đoán role hoặc bỏ nhánh lạ.
- Giữ mọi kiểm namespace, identity, cấu trúc không chữ/ảnh, đầu mút VML theo tọa độ mã hóa DrawingML. Title chỉ là ý định trang; vẫn kiểm nét XPS thực, không thay tọa độ mã hóa bằng Title để nhận cặp.
- Khi sửa lại, bộ ghi hiện có thay cả cặp một lần, giữ các nét/phần khác và bù tọa độ. Không đổi bộ đọc, dung sai XPS, checkpoint, rollback hay Hoàn tác.
- Regression mô phỏng lưu Line đã bù vị trí với nhãn ghép, thêm Subject/Motto, cập nhật và loại nhãn sai Title/role, nhãn có prefix/suffix lạ, thiếu Title/Title không hợp lệ hoặc endpoint dùng nhầm tọa độ ý định. Có số phân số và đầu mút VML làm tròn tương đương phản hồi thực.

Đã tái hiện AppOwned=false/HasGroupedShapes=true trên chính `modern-before-Subject.docx` bằng mã 0.1.72. Mã mới nhận một Line có ý định đúng, thử cập nhật Agency/thêm hai Line thiếu trên candidate; nguồn, chữ, bảng, định dạng và các part khác giữ nguyên. File người dùng chỉ kiểm cục bộ, không đưa vào repository/gói. Candidate dùng tọa độ kiểm, không phải bản sửa tài liệu để sử dụng.

Kết quả Core/OpenXML, WPF và metadata của đúng commit nằm trong build-checks.json, log và TXT đi kèm. **CI không chạy Word; chưa xác nhận toàn bộ Kiểm tra Word trên Office thực đạt 0.1.73.** Bản vá sửa điều kiện nhận cặp, không bỏ qua Subject để lấy PASS.

Giải nén vào thư mục mới; kiểm tiêu đề 0.1.73, để bộ đọc thử nghiệm tắt rồi chạy Kiểm tra Word tự động. Nếu chưa đạt, gửi ZIP thư mục kết quả mới và diagnostics.log. Giữ danh mục 33 nhóm đề xuất có điều kiện / 8 thủ công / 1 minh họa, ưu tiên thể thức, giữ main và các tag/gói cũ.
