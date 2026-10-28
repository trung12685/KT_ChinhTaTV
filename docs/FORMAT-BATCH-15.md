# Đợt thể thức 15 — phạm vi của lần quét

Ngày 29/10/2026 (UTC+7), mã phát triển 0.1.76, cụm thứ nhất sau gói 0.1.75. Xử lý phần “Thông báo phạm vi” trong REMAINING-WORK.md.

Thêm **Kiểm tra → Phạm vi kiểm tra tài liệu…** và nút **Phạm vi đã kiểm…**. Sổ có đủ 33 nhóm có đề xuất, kể cả chưa nhận diện vùng. Trạng thái gồm không áp dụng chế độ, chưa nhận diện vùng, thiếu dữ liệu, mới quan sát một phần, đã quan sát trong phạm vi hỗ trợ và chờ bản xem trước. Số vùng nhận diện/quan sát và số cảnh báo sửa được/thủ công tách riêng. Không có cảnh báo không đồng nghĩa đã kiểm đầy đủ; “đã quan sát” chỉ áp dụng phạm vi đã công bố. 8 nhóm thủ công và 1 minh họa vẫn cần duyệt riêng.

Snapshot dùng cùng dữ liệu quét hiện tại; mất XML/native/format hoặc nguồn spacing chưa hỗ trợ không thành quan sát đầy đủ. C11/C12 lấy callback từ phép đo mốc/nét thực; thiếu mốc, clip hoặc ghép nét chưa rõ vẫn một phần. C32 dùng native section và toàn gói có settings, không bỏ lề đối xứng/đóng sách. C33 chỉ ghi đã quan sát sau hậu kiểm số hiển thị nếu cấu trúc đủ điều kiện; thiết lập đúng nhưng chưa đo vẫn chờ/một phần.

Xóa snapshot cũ khi mở tài liệu, bắt đầu quét hoặc có lỗi. Chỉ công bố dữ liệu sau lượt hiện tại thành công. Cửa sổ chỉ đọc; sao chép báo cáo gồm catalog/số đếm/lý do, không xuất chữ hay CP/đường dẫn tài liệu. Không thêm thao tác Word, Undo hoặc quyền sửa từ sổ phạm vi. Khi chỉ có cảnh báo hoặc chưa đọc đủ, trạng thái không bị nâng lên do danh sách lỗi rỗng.

Kiểm Core cho nhóm không có mốc/không có cảnh báo, nguồn thiếu, chế độ khác, spacing chưa hỗ trợ, hình học chưa đo, số đếm/định danh sai, snapshot bất biến và kết quả trang. WPF kiểm bảng chỉ đọc, nội dung báo cáo và callback thật của Line/vị trí hành chính với glyph ẩn/clip/mất anchor/nét chồng/khung sai. Kết quả đúng commit ở CI/PR; CI không chạy Word. Chưa đóng ZIP 0.1.76; giữ gói 0.1.75, bộ đọc cũ và dừng tốc độ.
