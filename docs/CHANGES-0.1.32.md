# 0.1.32 — Kiểm vị trí số và ngày tháng (bản thử)

Tiếp nối tag 0.1.31, commit `7bf086637622f4a1657edb4e66dc79e3b9e92838`. Bổ sung cảnh báo vị trí dựa trên chữ được dựng thật trong XPS; chưa tự dịch chuyển hoặc căn lại đoạn. Giữ mốc ổn định 0.1.27 và mọi gói/tag cũ.

## Phần bổ sung

Trong phần đầu Nhà nước có cặp quốc hiệu/tiêu ngữ và vùng bảng đơn được đối chiếu chắc chắn:

- Kiểm số/ký hiệu nằm dưới, cùng tâm với toàn khối chữ tên cơ quan phía trên trong ô.
- Kiểm địa danh/ngày tháng nằm dưới và cùng tâm với quốc hiệu–tiêu ngữ. Nếu hai mốc phía trên không cùng tâm, báo chưa đủ điều kiện kết luận thay vì chọn một mốc tùy ý.
- Kiểm số/ký hiệu và ngày tháng cùng hàng bằng đường cơ sở glyph thật; không so đáy hộp chữ để tránh cảnh báo giả khi cỡ 13 và 14 khác nhau.

Căn cứ: NĐ30 Phụ lục I Phần I.II.3.c, 4.c, PDF 23 trong nguồn đã ghi ở FORMAT-RULES.md. Ngưỡng phát hiện lệch tâm/hàng 3 pt và dung sai đo nhỏ 0,5 pt là chính sách của bản thử, không phải thông số bắt buộc nêu trong NĐ30. Chưa kiểm mọi ngoại lệ, mẫu, tên cơ quan chính thức hoặc sửa đổi pháp lý sau bản nguồn.

## Giới hạn và giữ an toàn

Tất cả mục vị trí chỉ cảnh báo để người dùng duyệt, không tích sửa tự động. Thiếu mốc, khung bảng chưa ghép chắc chắn, chữ bị cắt/xoay/xuống dòng hoặc không ánh xạ được sẽ ghi rõ chưa kiểm được; không coi không đo được là đã đạt. Trường Word/control/tab thật, ứng viên trùng, profile khác hoặc trang sau không được đoán thành vùng đủ điều kiện. Đầu văn bản ngoài bảng còn giới hạn. Cảnh báo có thể còn sau sửa phông vì yêu cầu vị trí khác với định dạng ký tự.

Dùng chung XPS đã dựng cho danh sách, không xuất Word thêm lần nữa khi rê chuột. Đo advances/baseline theo transform thật, không suy ra từ số Shape hoặc cờ căn giữa. Không lấy dòng cuối của đoạn xuống dòng làm đại diện cả dòng.

Giữ các sửa chữ/phông/ô trống, giao dịch phục hồi và một Undo của 0.1.28–0.1.31. Giữ kiểm marker/nét XPS, tham chiếu và rollback Line. Bản này không tự đặt lại vị trí chữ hoặc đường kẻ.

## Kiểm thử và cách thử

Core kiểm nhận diện vùng, mốc thiếu/trùng/khác ô, vị trí lệch, dung sai, baseline khác cỡ và dữ liệu không hợp lệ. WPF kiểm trên glyph dựng thật, transform, lệch tâm/hàng, mất anchor, clip/xoay/xuống dòng; các bài Line cũ vẫn chạy. Workflow Windows chạy toàn bộ Core/OpenXML + WPF rồi publish; CI không có Word.

Giải nén vào thư mục mới và giữ `samples`. Chạy **Kiểm tra → Kiểm tra Word tự động…**: bài mới kiểm mẫu giả chuẩn và mẫu cố ý lệch tâm/khác hàng, lưu `position-base.xps`, `position-shifted.xps` cùng `office-check.txt`. Kịch bản có trong chương trình nhưng chưa có xác nhận Office 2021. Nếu mẫu chuẩn chưa đủ mốc/không đo được, bài kiểm phải báo chưa đạt để phân tích.

Có thể mở bản sao `position-shifted-input.docx` trong thư mục kết quả để xem các cảnh báo vị trí. Gói gồm các sửa trước; chỉ cần thử gói mới. Ghi rõ phiên bản và gửi log/kết quả nếu còn lỗi. Chưa nhập main hoặc chốt ổn định trước bằng chứng dùng thử.
