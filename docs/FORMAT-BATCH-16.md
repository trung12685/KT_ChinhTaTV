# Đợt thể thức 16 — hậu kiểm số trang hiển thị thật

Ngày 29/10/2026 (UTC+7), mã phát triển 0.1.76, cụm thứ hai sau 0.1.75. Tiếp phần C33 trong REMAINING-WORK.md trước khi mở rộng header/footer/nhiều section.

Mã 0.1.75 hậu kiểm số trang theo chữ/phông/tọa độ, chưa xét visibility/opacity của ancestor, clip/mask và trục chữ. Vì vậy chữ đúng “2” nhưng bị ẩn vẫn có thể được tính là số trang đạt. Dùng chung proof paint/clip đã có để yêu cầu số thực sự hiện đủ, đen đục, đúng phông/cỡ/đứng, cùng trục dương của trang; kiểm cả alignment box và ink box. Nét mực bị cắt, ancestor/mask mờ hoặc cách vẽ chưa rõ không thành PASS. Số sai vị trí trong vùng lề trên không được bỏ khỏi kiểm chỉ vì nằm xa dải tâm dự kiến; trang đầu vẫn phải không hiện số.

Snapshot bảo toàn nội dung ngoài lề trên giữ thêm màu/độ hiển thị/font của glyph và paint/stroke của Path, bên cạnh chữ/số trang/vị trí. Thân/footer bị ẩn, mờ, đổi màu hoặc cắt thì hậu kiểm thất bại. Nội dung không đổi có transparency đã biết được giữ, không ép toàn thân đen đục. Mask/effect/leaf chưa hỗ trợ chuyển thủ công/phục hồi; chưa mở hỗ trợ ảnh/gradient phức tạp hoặc mọi cách rasterize của Word. Thay đổi nhắm vào hậu kiểm, không tự sửa trực tiếp paint của tài liệu.

Các lỗi này đi qua ranh giới rollback cả lượt PAGE đã có; không công bố một Hoàn tác thành công khi hậu kiểm chưa đạt. Không nới scope writer/header/footer/section. C29 Exactly/AtLeast, dịch dọc số/ngày và các phần khác trong báo cáo vẫn chưa tự sửa.

Kiểm WPF số ẩn/mờ/clip/mask/xoay/lật/ngoài trang và sai vị trí; positive scaling 4/3, mask đục/clip đủ; thay đổi paint ở chữ/hình/footer và rollback. Mẫu tổng hợp không thay bằng bằng chứng Word thực; CI chỉ chạy WPF và biên dịch bài Office. Chưa ZIP 0.1.76; giữ main và tag/gói cũ.
