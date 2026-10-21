# 0.1.22 — Line tiêu ngữ không xuất hiện ở cột phải

Phản hồi 0.1.21: người dùng xác nhận Line 1 và 3 ổn. Log xác nhận Agency đã bù và kiểm XPS đạt; khi thêm Motto không có nét mới (chỉ thấy các nét đã có, hoặc không thấy nét nào khi bắt đầu từ tài liệu trống). Không có dữ liệu để dùng phép bù theo nét mới của 0.1.21.

DOCX riêng đặt tiêu ngữ trong ô bên phải. Nếu Word áp dụng gốc ô như trường hợp Agency, điểm bắt đầu theo trang bị cộng thêm gốc cột phải sẽ vượt chiều rộng trang. Đây là giải thích phù hợp với dữ liệu hiện có; chưa có XPS candidate riêng của lỗi Motto để khẳng định mọi chi tiết của Word renderer.

Bổ sung một cách tạo candidate thứ hai chỉ cho một Line Motto mới và không hiển thị:
- Chỉ dùng khi đoạn tham chiếu đã xác minh nằm trong bảng. Đọc khung cắt hình chữ nhật chung của toàn bộ glyph tham chiếu từ XPS nền, quy đổi đủ ancestor transforms sang tọa độ trang. Không dùng lề cố định hoặc gốc cột trái.
- Khung phải hẹp hơn trang, chứa vị trí đích; vị trí nếu bị cộng thêm gốc khung phải có điểm đầu ngoài trang. Không có nét mới nào và mọi nét nền phải còn nguyên, ghép một-một. Không áp dụng cho Line đã có, nhiều vai trò hoặc khung không rõ.
- Candidate thứ hai trừ gốc ô đo được, giữ đích trang ban đầu trong metadata như 0.1.21. Vẫn bắt buộc kiểm chữ, dấu DOCX và nét XPS đúng đích. Sai/thiếu sẽ rollback; không thử lần ba.

Giữ phép bù nét thực đo của 0.1.21 cho Line 1 và 3. Khi thêm Line mới, kiểm cả các nét đã có còn đúng vị trí, bên cạnh kiểm Line mới. Bài Office check bổ sung thứ tự Agency → Subject → Motto trên mẫu compatibilityMode=15 và hoàn tác đủ ba bước.

Có kiểm thử Windows WPF cho đo khung qua ancestor transforms, không có clip, clip toàn trang/không phải chữ nhật, glyph khác khung và việc dùng gốc cột phải. Kiểm thử Core bao phủ nét cũ bị mất, nét thừa, khung không hợp lệ, điểm chưa ra ngoài trang và trường hợp mơ hồ. Không đưa tài liệu riêng lên GitHub.

Build và các kiểm thử không phải bằng chứng đã chạy đạt Line Motto trên Office 2021 của người dùng. Cần xác nhận thực tế bản 0.1.22 với tài liệu đã có Line 1 và 3; không chấp nhận chỉ dựa vào marker hoặc số Shape.
