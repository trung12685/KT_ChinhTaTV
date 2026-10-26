# Đợt thể thức02 — khổ giấy và lề

Ngày26/10/2026, mã .70 đang tích lũy, tiếp nối C31 tại e7e29c47. Chưa có gói Windows mới. Tối ưu tốc độ đọc đã dừng theo yêu cầu người dùng; vẫn giữ bộ đọc cũ mặc định, bảng và full Preview Word/XPS.

## Đã bổ sung C32

Nguồn đã đối chiếu: Nghị định30/2020/NĐ-CP, Phụ lụcI, PhầnI.I.1–3, PDF21: A4(210×297mm), trình bày dọc; ngoại lệ ngang cho bảng/biểu không làm phụ lục riêng. Lề trên/dưới20–25, trái30–35, phải15–20mm.

Một mục C32 gộp các thông số trang đang sai và thể hiện rõ thông số thay thế. Nếu giấy sai thì đặtA4; lề ngoài khoảng về biên gần nhất. Mỗi lề đang đúng được giữ nguyên, kể cả giá trị giữa khoảng.

| Trước | Sau khi chọn sửa |
|---|---|
| Letter215,9×279,4mm; lề10/22/20/15mm | A4; lề20/22/30/15mm |
| A4; lề25/20/35/20mm | Không báo lỗi, không đề xuất đổi |
| A4; trên30mm, ba lề khác đúng | Chỉ trên về25mm |
| Giấy/lề sai nhưng có ảnh hoặc Shape đã đặt | Cảnh báo để duyệt thủ công, chưa tự đổi trang |

Phạm vi tự sửa: chế độ Nhà nước, một section dọc, thiết lập Word khớp XML, không cột/khung trang đặc biệt. Bảng chữ phải có grid/độ rộng đã biết và vừa vùng mới. Ảnh/Shape, header/footer, tab/field/control, Track Changes, nhiều section, trang ngang, bảng gộp/lồng/nổi, lề gáy/đối xứng/đóng sách, grid văn bản hoặc dữ liệu chưa chắc chưa mở sửa. Không bỏ thành phần hoặc đổi hướng để ép nhận diện. Chế độ Đảng/cá nhân chưa mở thêm tự sửa trang.

## Giữ dữ liệu và sửa cùng nhiều nhóm

1. Quét lại, đối chiếu đề xuất trang hiện tại và toàn bộ nội dung gốc đã chọn, tạo checkpoint trước lượt.
2. Tạo candidate DOCX mới. Chỉ sửa các thuộc tính pgSz/pgMar đã duyệt, bỏ mã giấy máy in cũ khi đổi giấy. Giữ mọi chữ, ô bảng, phông/kiểu, thiết lập chưa chọn và từng byte các phần ZIP khác; kiểm trước khi mở vào Word.
3. Mở candidate, xác minh lại nội dung/kiểu/bảng và thông số trang. Bản nguồn người dùng không bị ghi đè.
4. Với các nhóm chọn kèm, quét và đo XPS lại. Ghép đúng mục cũ qua chữ và prefix đọc từ Word thật; lấy CP/toạ độ mới. Không áp thêm lỗi chưa được chọn. Mục hình học đã tự đạt nhờ đổi trang phải có bằng chứng đúng mốc, rồi vẫn kiểm cuối.
5. Sửa các nhóm đó bằng giao dịch đang có; hậu kiểm chữ, khổ từng trang XPS và mọi Line/căn giữa đã chọn. Có thông báo tiến độ. Chỉ sau khi đạt toàn bộ mới tạo một mục Hoàn tác.
6. Nếu bất kỳ bước nào lỗi, phục hồi cả lượt gồm trang và các nhóm sau. Nếu phục hồi chưa xác minh được, giữ checkpoint và báo rõ, không báo sửa thành công.

Đổi giấy/lề có thể làm văn bản phân trang hoặc xuống dòng lại theo Word; chương trình giữ nguyên dữ liệu, dùng bản render Word đầy đủ. Không lấy số Shape hoặc số file tạo được làm bằng chứng đạt.

## Kiểm thử và giới hạn

Core: các biên/giá trị chưa biết, ngoại lệ, bảng vừa/không vừa vùng mới, source/candidate riêng, từng phần ZIP, sửa lén chữ/kiểu/bảng/lề đúng, stale/forged payload, rebind CP/toạ độ theo ngữ cảnh và lỗi ở từng giai đoạn. WPF: chọn nhóm/all, không tích cảnh báo thủ công, loại dòng đã sửa, mẫu DOCX và schema của thuộc tính section. Có bài Word giả sửa riêng/chung khoảng trắng và ba Line, lỗi sau trang/sau nhóm, Hoàn tác/lưu-mở/kiểu chữ/nguồn; CI biên dịch bài này, chưa phải bằng chứng Office2021 chạy đạt.

Danh mục nguồn32 nhóm đề xuất/8 thủ công/1 minh họa; báo cáo trong ứng dụng/TXT có ví dụ và giới hạn C32, M01 giữ các bố cục ngoài phạm vi. Tích lũy theo nhịp5–10 bước có ích; sau C31 và C32, .70 chưa đóng gói. Mốc .67 và gói .69 nguyên vẹn.

## Phần tiếp theo

Đã đọc PL I.I.7 về số trang: sốẢRập từ1, cỡ13–14, đứng, giữa lề trên, không hiển thị trang đầu. **Chưa triển khai sửa số trang trong đợt này**. Header/footer có field, nhiều section và liên kết cần một nhóm nhận diện/ghi/hậu kiểm riêng để bảo toàn nội dung. Sau đó rà đồng bộ thể thức đầu–thân–cuối. Lỗi chữ cơ bản/khoảng trắng/dấu câu/lặp ở bước sau; ngữ cảnh và thư viện từ cuối cùng.
