# Đợt thể thức 19 — sửa trang khi đầu/cuối trang thực sự trống

Ngày 29/10/2026 (UTC+7), mã 0.1.76, cụm thứ năm sau gói 0.1.75. Mở thêm điều kiện C32 của một section dọc thông thường; không mở trang ngang hoặc nhiều section.

Có header/footer reference không còn là lý do duy nhất để chặn sửa khổ giấy/lề. Mỗi tham chiếu default/first/even phải thuộc đúng section cuối duy nhất, không trùng vai trò, có quan hệ nội bộ hợp lệ và trỏ đúng part đầu/cuối trang. Part phải thực sự trống theo cấu trúc được hỗ trợ. Kiểm cả style trực tiếp, style kế thừa và docDefaults; chuỗi style thiếu/vòng/trùng, dấu cách có nội dung, field, hình, đánh số, border/shading hoặc trang trí run chưa hỗ trợ giữ thủ công. Trống trong chữ đọc được chưa chứng minh là không có nội dung vẽ.

Đoạn trống vẫn có thể chiếm chiều cao lớn. Sau Word phân trang candidate, hậu kiểm toàn trang XPS yêu cầu chữ/hình được quan sát nằm trong hình chữ nhật nội dung của khổ và bốn lề đã duyệt, với dung sai một point. Kiểm cả alignment box và nét mực chữ, không bỏ dải lề trên. Khổ XPS phải khớp; thiếu bằng chứng, clip/mask/paint chưa rõ hoặc tràn bất kỳ lề nào làm lượt sửa phục hồi. Không lấy riêng cỡ chữ của đoạn trống để đoán nó không ảnh hưởng bố cục.

Candidate vẫn chỉ đổi pgSz/pgMar được duyệt, giữ byte của mọi part khác và nguồn đầu vào. XML sau Word/lưu được đối chiếu lại: thêm chữ/field/trang trí vào part trống không được coi đã bảo toàn. Hậu kiểm áp dụng trước khi sửa các nhóm chọn kèm và sau cả lượt; lỗi phải phục hồi toàn bộ, giữ một Hoàn tác cho lượt thành công.

Kiểm Core/OpenXML gồm các tham chiếu và style hợp lệ/lạ, nguồn bất biến, giới hạn thay đổi candidate và hậu kiểm sau Word mô phỏng. Kiểm WPF gồm đủ bốn lề, dung sai, khổ trang và paint/clip chưa rõ. Bài Office **Khổ/lề với đầu–cuối trang trống** dùng mẫu riêng và chạy độc lập. Các kiểm này không thay kết quả Word thực. Trang ngang, nhiều section, lề đối xứng/gáy/cột, header/footer có nội dung và các bố cục đặc biệt khác vẫn nằm ngoài phạm vi tự sửa C32.
