# Chính tả Việt0.1.74 — năm cụm kiểm thể thức và bằng chứng Office

Gói thử tích lũy đợt05–09 sau0.1.73. Ưu tiên thể thức; bộ đọc thử nghiệm tắt mặc định, chưa tiếp tục tối ưu tốc độ. Danh mục33 nhóm đề xuất có điều kiện /8 thủ công /1 minh họa giữ nguyên.

- Kiểm đúng một nét và đủ số lần xuất hiện: hai nét chồng không được coi là một Line đạt; một nét không đồng thời chứng minh hai yêu cầu giữ/tạo. Mất nét nền phải dừng bù tọa độ.
- Chỉ dùng nét thực sự được vẽ: xét fill/stroke/opacity và clip hình học, ancestor/layout/trang. Nét ẩn/rỗng bị loại; phần nét bị cắt hoặc cách vẽ chưa rõ chuyển thủ công.
- Mốc chữ phải hiển thị đầy đủ, kể cả phần chữ nghiêng nhô ngoài ô đo. Một phần không chứng minh được không biến phần còn lại thành mốc ngắn hơn.
- Đo dòng cuối theo đường chân chữ, giữ đủ chiều rộng khi cùng dòng có nhiều cỡ chữ. Hàng chồng/chữ nâng-hạ/thứ tự chưa rõ giữ thủ công; dòng số/ngày vẫn cần đúng một hàng.
- Bài Kiểm tra Word ghi XML hiện tại và các range Word của cùng lần đọc khi có lỗi đối chiếu. File mapping-evidence-legacy-*.json hoặc experimental có vòng tối đa4 file mỗi bộ đọc; không ghi nội dung này trong phiên tài liệu thông thường, không gọi Save/close hoặc đổi quyền sửa.

Cụm sửa nằm trong FORMAT-BATCH-05.md đến FORMAT-BATCH-09.md đi kèm. Giữ nhánh tương thích DrawingML/VML, chữ/bảng/ảnh, nét XPS, chọn nhiều nhóm, một Hoàn tác và rollback cả lượt. Các điều kiện bảo thủ có thể yêu cầu xem thủ công với clip/mask/bố cục chưa rõ; chưa thêm kiểm tương phản màu hoặc suy đoán superscript/subscript.

Số kiểm tự động và commit/run của đúng gói ở build-checks.json và TXT tiến độ. **CI không chạy Microsoft Word. Chưa xác nhận toàn bài Kiểm tra Word trên Office thực đã đạt0.1.74; không gán PASS từ build/CI.** Phản hồi Office gần nhất là0.1.72 dừng tại Subject; bản0.1.73 đã sửa nhãn VML của Line đã bù nhưng chưa nhận lại kết quả toàn bài.

Giải nén0.1.74 vào thư mục mới, mở ChinhTa.Desktop.exe và để “Thử bộ đọc mới” bỏ tích; chạy Kiểm tra → Kiểm tra Word tự động. Nếu chưa đạt, gửi ZIP thư mục OfficeCheck mới cùng diagnostics.log. Trong ZIP giữ cả mapping-evidence-*.json nếu có, office-check.txt, modern-before-*.docx/.json/.xps và build-checks.json. Không sửa mẫu để vượt bài kiểm. Các file cũ của người dùng không nằm trong source hoặc gói này; main và tag/gói cũ giữ nguyên.
