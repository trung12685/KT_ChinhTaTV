# Đợt thể thức08 — xác định đầy đủ dòng tham chiếu cuối

Ngày 27/10/2026 (UTC+7). Tiếp đợt07 tại969e6124; mã phát triển0.1.74, cụm thứ tư sau gói0.1.73. Danh mục33/8/1, bộ đọc cũ mặc định; chưa đóng gói ZIP mới.

## Lỗi tái hiện

Commit2212e5ba chỉ thêm ca WPF vào mã đợt07. CI40437018462 chạy4234 kiểm Core/OpenXML rồi thất bại đúng ca: dòng có hai cỡ chữ cùng baseline bị đo với Left=210 thay vì100; hai dòng cách nhau2pt vẫn được nhận thành tham chiếu. Phép đo trước lấy đáy hộp lớn nhất rồi gom các hộp có đáy chênh không quá3DIP, nên cỡ chữ khác nhau làm mất phần đầu hoặc gộp hai dòng sát nhau.

## Cụm sửa

- Gom các glyph run theo đường chân chữ đã biến đổi sang tọa độ trang, dùng dung sai nửa point vốn có ở dòng hành chính. Toàn nhóm phải nằm trong dung sai; không nối dây chuyền qua từng run gần nhau.
- Giữ toàn bộ chiều rộng và đáy lớn nhất của các run cùng baseline dù cỡ chữ khác nhau. Dòng thực sự xuống hàng lấy toàn bộ hàng cuối đã chứng minh, không lấy một phần có hộp cao nhất.
- Các hàng phải tiến xuống trang theo thứ tự vùng chữ và không chồng vùng đo quá nửa point. Chữ nâng/hạ, hàng quá sát/chồng, đảo thứ tự hoặc quay lại baseline trước không đủ bằng chứng để tự đặt Line. Chuyển thủ công, không đoán phần còn lại.
- Dòng số/ngày và tham chiếu Line dùng chung phép gom hàng; dòng hành chính vẫn bắt buộc đúng một hàng. Bounding highlight để xem vẫn hoạt động. Hậu kiểm mất mốc mới phải phục hồi cả checkpoint; lịch sử sửa không che được lỗi.

Không thay chỉ mục chữ/CP, bộ ghi DOCX, cách bù tọa độ, luật chiều dài/khoảng cách/tâm Line, bảng/chữ/phông, giấy/lề/PAGE, Undo hoặc bộ đọc Word. Các phép chứng minh chữ hiển thị đầy đủ từ đợt07 vẫn bắt buộc.

## Kiểm chứng và giới hạn

Các ca WPF gồm hai cỡ chữ ở cả hai thứ tự, nhiều run cùng baseline, trôi baseline nhỏ, hai/ba hàng cách nhau rõ, chọn từ hàng sau, dòng cuối có nhiều cỡ chữ; ca nâng/hạ/chồng/đảo/quay lại và chuỗi độ lệch nhỏ. Kiểm cả đề xuất Line dùng đúng chiều rộng dòng cuối, chuyển thủ công, lịch sử sửa và rollback sau mở candidate. Toàn bộ kiểm Core/OpenXML/WPF trước đó tiếp tục bắt buộc; số kiểm đúng commit xem CI/PR.

Đây là điều kiện bảo thủ: bố cục có các hộp chữ chồng nhau hoặc XPS xuất thứ tự hàng không rõ có thể chuyển thủ công, kể cả chữ vẫn nhìn thấy. Chưa nhận diện ngữ nghĩa superscript/subscript hay dựng lại thứ tự đọc. CI không chạy Word thực và không xác nhận toàn bộ Office đã PASS. Tiếp tục tích lũy theo nhịp5–10 cụm trước đóng gói; bản tải gần nhất vẫn0.1.73.
