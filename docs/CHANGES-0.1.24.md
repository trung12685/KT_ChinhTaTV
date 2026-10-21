# 0.1.24 — phép đo ổn định và sửa lại Line đã có

Log 0.1.23: sửa nhóm đầu đã kiểm đạt các nét Agency/Motto. Sau quét lại, người dùng còn cảnh báo Motto/Subject; khi sửa tiếp hai nét thì Motto không hiện, nhóm bị rollback. Không coi nét đã vẽ hoặc số Shape là bằng chứng đủ.

Hai lỗi được xử lý:

1. Đo điểm cuối bằng caret hit testing. GetDistanceFromCaretCharacterHit mô tả điểm dừng con trỏ; nếu điểm dừng cuối bị tắt, nó có thể trả cạnh trước ký tự cuối. Chênh lệch tiêu ngữ trong log là 175.94 - 169.71 = 6.23 pt. Trong XPS mẫu người dùng đã gửi, độ tiến glyph c cuối là 6.231621 pt; tổng advances cho đúng 175.941887 pt, còn thiếu glyph c cho 169.710266 pt. WPF regression tạo đúng trường hợp caret cuối bị tắt và kiểm phép đo không đổi khi đưa trang vào DocumentViewer. Thay điểm con trỏ bằng tổng AdvanceWidths theo ClusterMap; không sửa hoặc giả lập số đo để khớp nét đã vẽ. Nếu biên chọn nằm giữa shaping cluster hoặc glyph không hỗ trợ, không tự kết luận vị trí.

2. Khi sửa Line đã bù tọa độ, encoder cũ ghi lại tọa độ trang và đánh mất phần bù. Nhánh cứu nét mất của Motto chỉ áp dụng cho nét mới nên không sửa được lần tiếp theo. Encoder nay giữ chênh lệch đã lưu giữa tọa độ đích và tọa độ mã hóa của chính Line đó; metadata tiếp tục lưu đích mới. Phản hồi vị trí vẫn tính từ nét XPS và cộng đúng tọa độ mã hóa thực tế, tránh bù hai lần. Mọi candidate vẫn phải qua kiểm chữ, marker, tham chiếu và nét thực tế; metadata không tự chứng minh thành công.

Giữ sửa nhóm nguyên tử, kiểm cuối nhóm, một Undo, rollback, tỷ lệ tự tạo 3/7 (chấp nhận 1/3–1/2) và ngoại lệ trường số/ngày trống. Không ẩn cảnh báo chỉ vì đã bấm sửa.

Kiểm thử thêm advances/cluster, số đo đúng của mẫu XPS, caret stop bật/tắt và DocumentViewer, cập nhật Motto đã bù mà không đổi hai nét khác. Bài Office check mở rộng tình huống ba nét đã có, trong đó Motto/Subject cố ý lệch tâm, sửa chung hai nét rồi kiểm quét lại và lưu/mở.

CI không có Word desktop; kết quả Core/OpenXML/WPF và build không thay thế xác nhận trên Office 2021 với tài liệu riêng. Tài liệu riêng và font/XPS đính kèm không được đưa lên GitHub.
