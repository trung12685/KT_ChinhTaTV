# Đợt78–80 — người dùng tự sửa là luồng mặc định

Ngày30/10/2026 (UTC+7), tiếp từ nhánh Pro feature/isolated-spacing-proof-batch22, commit85fab4c4b17a9ed9bd8498fd06315aaf3c54296b. Mã phát triển0.1.82. Người dùng yêu cầu thử chế độ sửa thủ công mặc định; bản preview riêng được chuẩn bị cho yêu cầu này, không phải thêm một luật tự sửa hay cập nhật tag/release cũ.

## 78. Chế độ thủ công và bản nháp

Khởi động ở Sửa thủ công; ẩn checklist và các nút máy sửa, không chuyển mục đang sửa chỉ vì rê chuột. Người dùng chọn lỗi rồi mở ô sửa đoạn nguyên văn, tự nhập và áp dụng. Không điền sẵn câu thay thế của máy, không submit bằng Enter, không cắt bản nháp quá dài. Có trả lại bản gốc trong ô và xác nhận bỏ nháp khi đóng. Máy sửa chỉ hiện sau đổi chế độ tường minh; quay về thủ công bỏ các ô tích cũ.

## 79. Lớp đánh dấu chỉ đọc

Gạch chân đỏ/vàng các cảnh báo có anchor và proof hiển thị trọn, bấm dấu chọn đúng lỗi. Không đoán vị trí của cảnh báo chưa ghép, không đánh dấu chữ đại diện như lỗi trang/PAGE. Tách dấu thường trực khỏi highlight mục đang chọn; dọn khi mở/quét/lỗi/đổi chế độ. Không sửa Word.Font.Underline hoặc highlightColor, không ghi lớp dấu vào XPS đĩa.

## 80. Đường ghi nội dung người dùng

ManualTextTarget gắn toàn WordOpenXML và paragraph native đã ghép, độc lập CanFix/CurrentTextPlan của luật tự động. Chỉ đoạn chữ thuần ngoài bảng và một vùng text XML cho mỗi lần thay. Candidate đầy đủ giữ mọi part XML/binary/quan hệ, chỉ sửa nút chữ cùng xml:space; không thay run format. So cấu trúc XML chuẩn hóa bằng XNode.DeepEquals, không dùng chuỗi nối chưa escape để so văn bản người dùng. Giữ whitespace dữ liệu XML tùy ý.

Đối chiếu sau mở/lưu: mọi main-story text ngoài thay đổi đã duyệt, parts, binary và thuộc tính chữ/bố cục. Chỉ bỏ cache proofing, RSID, marker phân trang và các giá trị metadata lưu/thống kê rõ ràng; không coi mọi thay đổi namespace/part là hợp lệ. Nếu Word chuẩn hóa thêm chưa hiểu thì phục hồi bảo thủ. Không lấy cỡ chữ quan sát giả để chứng minh format; giữ XML run/style/theme làm nguồn trước–sau.

Lượt thành công có một checkpoint Undo; lỗi muộn phục hồi toàn bộ, lỗi phục hồi giữ nguyên cả hai nguyên nhân. Bản nháp cũ, paragraph/control không chắc và nội dung cấu trúc đều bị từ chối. Sau commit quét lại lỗi giữ phân biệt đã sửa/chưa quét, không phát lại mutation. Bổ sung nhóm Word riêng manual-text; CI chỉ biên dịch bài Word, không xác nhận Word thực.

Phạm vi và hướng thử ở PREVIEW-0.1.82.md. Chưa nhúng Word toàn trang hoặc sửa bảng/phông/lề trực tiếp, chưa mở Exactly/AtLeast, readerOFF và chỗ ký80pt giữ nguyên.
