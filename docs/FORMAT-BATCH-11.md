# Đợt thể thức11 — ảnh inline giữa chức vụ và họ tên

Ngày28/10/2026 (UTC+7). Tiếp đợt10 tại96759d89, mã phát triển0.1.75; cụm thứ hai kể từ gói0.1.74. Chưa đóng ZIP mới; danh mục33/8/1 và bộ đọc cũ mặc định giữ nguyên.

## Lỗi tái hiện

Word biểu diễn ảnh inline trong main story bằng ký tự đối tượng U+0001. Phép TextOf trước đây bỏ drawing, nên một đoạn chỉ có ảnh được đọc thành chuỗi rỗng. Ca mẫu độc lập cung cấp range Word có ký tự đối tượng, đặt ảnh giữa chức vụ và họ tên: mã đợt10 dừng tại XML-TEXT.P7.XML0.Word1, trả0 mốc ký. Ảnh sau họ tên vẫn làm đối chiếu phần tiếp theo dừng, có thể kéo theo inventory Line thiếu chắc chắn ở mẫu ngắn.

Đây là tái hiện bằng dữ liệu Word giả và DOCX sinh có schema, chưa phải xác nhận chạy Word thực. Không kết luận mọi mã XML cũ của người dùng có cùng nguyên nhân.

## Cụm sửa

- So sánh một ảnh nhúng trực tiếp dạng w:r/w:drawing/wp:inline/a:graphicData/pic:pic với một ký tự U+0001 trong **text Word thực đã đọc**. Namespace/cấu trúc một ảnh, blip có embed và không có link, không chữ/story/math/nhánh tương thích phải rõ. Không tính hay đoán Start/End từ XML.
- Ký tự thiếu/thừa/khác ở Word vẫn dừng đúng ranh giới đối chiếu. Floating, VML, ảnh liên kết ngoài, hình có chữ, wrapper/nhánh chưa hỗ trợ không được dùng để bắc qua đoạn trước họ tên.
- C31 có thể bỏ qua một đoạn chỉ chứa một ảnh đã nhận diện khi xác định các dòng chức vụ/họ tên. Đoạn ảnh có đánh số hoặc style chưa chứng minh vẫn đóng nhóm. Chức vụ/họ tên vẫn cần vai trò và quyền sửa hiện tại cho đủ nhóm; ảnh không phải mốc tên/chức vụ.
- Đoạn chứa ảnh luôn không được phép sửa chữ/phông. Nếu ảnh nằm giữa chữ trong một đoạn, cả đoạn đó giữ thủ công; chỉ các đoạn độc lập phía sau khớp đủ mới tiếp tục có quyền. Không xóa ký tự ảnh hoặc dòng ảnh để làm phép đối chiếu vượt qua.
- Cùng phép đối chiếu được dùng ở inventory Line: ảnh đã khớp không tự tạo lỗi mapping, không biến ảnh thành Line, kể cả khi preset geometry của picture mang tên line. Nét XPS/ownership/clip và kiểm bảo toàn vẫn độc lập, không suy từ token ảnh.

## Kiểm chứng và giới hạn

Ca Core gồm ảnh trước/sau tên trong mẫu ngắn/dài, XML cũ/chia sẻ, tên/chức vụ vẫn nhận đủ và chỉ hai dòng chữ có đề xuất; giữ XML/CP/object token. Kiểm ký tự Word sai/thiếu/thừa, URI/namespace sai, anchor, r:link, blip thiếu embed, chữ/đối tượng lạ và nhiều inline. Có ảnh ở giữa chữ, đoạn sau độc lập, image paragraph có numPr/style lạ và hai ảnh trong cùng đoạn không được dùng làm ngoại lệ khối ký. Có ca picture mang preset line vẫn không được đếm thành Word Line.

Kiểm Windows gọi observer thật với range giả trên4DOCX mẫu ngắn/dài × ảnh trước/sau họ tên; kiểm schema, ảnh và quyền của đoạn ảnh, hai vai trò, prefix inventory Line và các đoạn sau40. Bài Word thực cũng mở rộng cùng4mẫu: sửa riêng/chung ba Line, giữ cỡ13–14/tên/cấp bậc/gạch chân/căn-khoảng đoạn/ảnh, rollback, một Undo và lưu/mở. Byte ảnh, liên kết hiển thị và kích thước trong bản lưu phải giữ đúng.

CI chỉ biên dịch bài Word thực, không chạy Microsoft Word. Chưa xác nhận toàn bài0.1.74/0.1.75 đã đạt trên máy người dùng. Chưa hỗ trợ mọi loại hình, ảnh qua nhánh tương thích hoặc ảnh chung dòng ký. Không đổi bộ ghi DOCX, nguồn/ảnh, geometry, Undo, profile, bộ đọc thử nghiệm hay tối ưu tốc độ. Giữ gói0.1.74/main/tag cũ; tích lũy5–10 cụm trước gói kế tiếp.
