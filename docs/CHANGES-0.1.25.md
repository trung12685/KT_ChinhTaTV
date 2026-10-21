# 0.1.25 — hoàn tất nhóm Line trước khi trả kết quả

Phản hồi mới đã xác nhận trên 0.1.24: chọn sửa ba đường thì cả ba được vẽ nhưng danh sách còn đường số 2 và 3; chọn sửa tiếp hai mục đó thì hết. Chưa có nội dung cảnh báo/log mới để xác nhận nguyên nhân cụ thể của lần chạy này. 0.1.24 vẫn chưa đạt tình huống sửa nhóm trên Office 2021 của người dùng.

Qua rà soát phát hiện một sai khác có thể kiểm chứng: candidate cho phép nét lệch tối đa 1 pt so với tọa độ đích, nhưng trước đây lại đánh giá thể thức tại tọa độ đích. Danh sách lỗi đánh giá chính nét thực tế. Ví dụ nét trích yếu thực tế cao hơn đề xuất 0.75 pt vẫn khớp tọa độ trong dung sai 1 pt, song khoảng cách dưới chữ còn 5.25 pt, ngoài ngưỡng 5.5 pt. Nay candidate và danh sách cùng đánh giá nét thật, giữ nguyên dung sai thể thức.

Sau khi tạo/đặt lại các Line đã chọn, chương trình lưu, lấy lại cấu trúc và quét XPS bằng cùng bộ kiểm dùng trong danh sách, khi checkpoint gốc vẫn còn hiệu lực. Chỉ các cảnh báo tự sửa được của đúng Line đã chọn và đã có dấu nhận diện mới được đặt lại thêm một lượt. Sau đó bắt buộc quét lại, kiểm marker, nét thực tế, dòng tham chiếu và nội dung chữ; nếu chưa đạt thì rollback toàn bộ. Không ẩn cảnh báo, không lặp vô hạn, không tự sửa mục chưa chọn. Một lần Hoàn tác bỏ cả lượt kể cả phần đặt lại.

Khi sửa Line đã có, loại riêng nét cũ cần thay khỏi baseline ghép phản hồi; mọi nét khác vẫn phải được giữ nguyên. Điều này cũng tránh nhầm nét cập nhật dưới 1 pt thành nét baseline không đổi. Với Line cũ không hiển thị, giữ tất cả nét đang thấy để không làm mất nét khác. Phần bù tọa độ, đo đủ glyph cuối, tỷ lệ tự tạo 3/7, khoảng hợp lệ 1/3–1/2 và ngoại lệ số/ngày trống được giữ lại.

Kiểm thử thêm: nét thực tế lệch 0.75 pt gây cảnh báo nhất quán; nhóm ba Line còn hai cảnh báo được đặt lại và xác minh trong cùng giao dịch; cảnh báo dai dẳng rollback sau đúng một lượt thêm; không sửa mục ngoài lựa chọn, mục thủ công, mục chưa có nhận diện hoặc trùng; giữ checkpoint Hoàn tác. Bài Office check thêm quét lặp, lưu rồi mở lại nhóm đã sửa, không dựa vào lịch sử sửa.

CI chạy Core/OpenXML và WPF trên Windows rồi đóng gói win-x64. CI không có Word desktop; bản này vẫn cần kiểm chứng Office 2021 thực tế. Log mới bổ sung mỗi role, số cảnh báo còn lại và số đo tham chiếu/nét, không ghi nội dung tài liệu. Tệp riêng người dùng không được đưa lên GitHub.
