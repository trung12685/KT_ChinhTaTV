# 0.1.47 — Đối chiếu vùng sửa chữ toàn thân tài liệu (bản thử)

Tiếp nối 0.1.46, commit `54dafb525513106261916139d6bab23b73039116`. Ưu tiên củng cố việc sửa nhóm trước khi tiếp tục thêm quy tắc lỗi chữ.

## Phần được mở rộng

Trước đây, đường Word mới đối chiếu XML sâu để cho phép sửa chữ ở 40 đoạn đầu chế độ Nhà nước. Các đoạn khác/hai chế độ còn lại có thể dùng mặc định cũ. Bản này thu Start/End/Text từ range Word thật cho toàn phần thân và áp bằng chứng XML cho mọi đoạn ở cả Nhà nước, Đảng, cá nhân.

- Đoạn chữ thuần đã khớp XML vẫn có thể nhận gợi ý sửa theo các quy tắc hiện có, kể cả sau đoạn 40.
- Các dạng đã nhận diện như field đơn, control, hyperlink, drawing/object/pict/smartTag, tab/ngắt dòng trong run hoặc toán được giữ thủ công. Nội dung nhìn giống chữ không tự biến thành vùng được phép ghi.
- Field phức tạp, revision, footnote reference hoặc lệch chữ/XML dừng đối chiếu tại ranh giới chưa chắc. Các đoạn phía sau có thể phải kiểm tra thủ công, dù trông như chữ thuần. Phần trước đã xác minh vẫn được xét.
- Không đọc/đối chiếu được XML thì không có mặc định cho phép sửa chữ. Có cảnh báo riêng về phạm vi đối chiếu; đây không phải lỗi “đã sửa nhưng còn hiện”. Kế hoạch cũ có một mục nay thành thủ công bị từ chối cả lượt trước checkpoint/ghi.

Ví dụ: sau 45 đoạn đệm, `Ghi chú : nội dung` nằm trong hyperlink giữ thủ công; đoạn chữ thuần kế tiếp có khoảng trắng/cụm lặp/ngoặc vẫn sửa được khi đối chiếu khớp. Nếu thay hyperlink bằng field phức tạp, phần sau ranh giới đó chưa được tự sửa.

## Phạm vi và cách thực hiện

Chỉ áp cho phần thân chính mà Word đang liệt kê; chưa bổ sung sửa header/footer/textbox hoặc tự sửa cấu trúc field/control. Đối chiếu hiện tại nhận diện những dạng đã hỗ trợ, không chứng minh mọi cấu trúc XML/tài liệu đều có thể biên tập. Track Changes hoặc bảo vệ tài liệu vẫn chưa hỗ trợ.

Lấy XML của bản đang mở, gồm thay đổi chưa lưu, một lần mỗi lượt quét. Phép kiểm chữ dùng chế độ không dựng geometry; ngoài phạm vi đo cũ chỉ lấy End thật của range, không thêm đọc căn lề/page/table cho mọi đoạn. Header geometry vẫn dùng danh sách 40 đoạn đầu Nhà nước như trước, với đối chiếu riêng từ cùng XML; không mở rộng căn giữa/Line vào thân tài liệu. Hai lượt phân tích XML thuần phục vụ hai việc khác nhau; chưa có số đo thời gian Word thực tế cho bản này.

Giữ kế hoạch hiện tại, hậu kiểm toàn story/kiểu, rollback cả lượt, một Undo, nét XPS thật, lưu bản sao và checkpoint. Danh mục vẫn 17 nhóm sửa/8 thủ công/1 minh họa; báo cáo cập nhật M07, ví dụ, mốc .47 và giới hạn, không tạo thêm nhóm lỗi chỉ để tăng số đếm.

## Kiểm thử và cách thử

Core kiểm 65 đoạn và trường hợp 1.200 đoạn giả, các vùng có cấu trúc sau đoạn 40 ở ba chế độ, chữ thuần kế bên, dừng đúng ranh giới phức tạp/lệch XML, XML thiếu/hỏng, CP trùng/chồng/không hợp lệ, mặc định chặn khi không có bằng chứng và kế hoạch cũ. Các bài XML/Line hiện có tiếp tục dùng mặc định geometry cũ.

Bài **Kiểm tra → Kiểm tra Word tự động…** thêm `body-proof-plain.docx`, `body-proof-link.docx`, `body-proof-complex.docx`. Kiểm nhận diện và chặn kế hoạch cũ ở cả ba chế độ sau 45 đoạn đệm. Trên mẫu hyperlink, sửa nhóm chữ thuần phía sau: lỗi cố ý giữa lượt phải rollback, rồi sửa/quét/một Undo/lưu/mở `body-proof-fixed.docx`, giữ kiểu phần chữ và quan hệ hyperlink trong DOCX. Field phức tạp phải chặn phần sau và báo giới hạn ở ba chế độ; hash mọi nguồn giả giữ nguyên.

**Chưa có bằng chứng 0.1.47 chạy đạt toàn bộ bài Word trên Office 2021.** CI không có Word; Core/OpenXML, WPF và gói được kiểm trước phát hành. Giữ main, stable .27 và mọi tag/gói cũ; tiếp tục là bản thử.
