# Chính tả Việt 0.1.70 — tích lũy thể thức văn bản, chưa phát hành

Thay đổi trong mã, chưa tạo gói Windows cho riêng bước này. Tiếp nối 0.1.69 (`7f16a0b599a0e627203941b87af3f790c2c45c73`). Mốc .67 và mọi tag/gói cũ giữ nguyên.

## Kết quả người dùng cung cấp

| Bản/chế độ | Quét | Dựng trang | Cộng hai bước | Tổng mở báo cáo |
|---|---:|---:|---:|---:|
| .68 | 3,5 s | 1,4 s | 4,9 s | Chưa có |
| .69 bật thử nghiệm | 5,0 s | 1,8 s | 6,8 s | 12,9 s |
| .69 tắt thử nghiệm | 2,1 s | 1,2 s | 3,3 s | 6,1 s |

Số đo báo cáo của file đang so cho thấy tắt thử nghiệm tốt hơn rõ rệt: quét/dựng trang giảm khoảng51,5% so với bật. Chưa có số lượt lặp, điều kiện lạnh/ấm hoặc log tách chặng, nên chưa gán nguyên nhân cho một tính năng riêng và không coi tổng mở là phép so khởi động Word có kiểm soát. Các số đã làm tròn.

## Đã sửa

- WordSession mới mặc định UseOptimizedReading=false; checkbox giao diện cũng mặc định bỏ tích.
- Đổi nhãn thành **Kiểm tra → Thử bộ đọc mới (đang đánh giá)**, không gọi là “nhanh” hoặc “tối ưu”. Trạng thái dùng cùng cách gọi trung tính.
- Giữ bộ đọc mới ở chế độ tự bật để nghiên cứu; không xóa source thử hoặc thay các tag đã có.
- Giữ nguyên bảng, Preview Word/XPS, luật/chức năng, editor, kiểm nét thực tế, checkpoint, hậu kiểm, Undo và rollback. Không cắt phép kiểm để lấy số nhanh hơn.
- Thêm kiểm WPF xác nhận backend và checkbox cùng tắt khi khởi động, không cần khởi động Word để kiểm cấu hình này. Chạy bộ kiểm hồi quy trên Windows đúng commit; kết quả theo CI, không phải chứng nhận Office2021.

## Cách dùng ngay

Người dùng đang có .69 chỉ cần **tiếp tục để bỏ tích tùy chọn thử nghiệm**. Không cần tải bản mới để đạt trạng thái này. Khởi động lại .69 vẫn có thể bật sẵn theo thiết kế cũ, nên kiểm tra lại checkbox; mã .70 sửa mặc định cho lần khởi động mới. Chưa phát hành .70 cho riêng thay đổi này; tạo gói theo chu kỳ đã thống nhất hoặc khi có yêu cầu cần bản tải.

Mốc quay lại: [0.1.67](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.67). Ghi chú lịch sử trước khi người dùng yêu cầu dừng tối ưu: hướng nghiên cứu khi đó dựa trên stage timings/counters thực tế: đọc đoạn, đọc gộp/fallback, đọc định dạng, phân trang, gắn vị trí và xuất XPS. Chưa thêm một thuật toán tăng tốc khác dựa vào số getter mô hình. Danh mục giữ nguyên30 đề xuất/8 thủ công/1 minh họa; các giới hạn Office2021 còn mở giữ nguyên.

## Đợt thể thức 01 — khối ký (26/10/2026)

Theo yêu cầu mới, dừng tối ưu tốc độ đọc. Giữ bộ đọc cũ mặc định và chuyển sang thể thức trước, chữ cơ bản/khoảng trắng/dấu câu/lặp sau, ngữ cảnh và thư viện cuối cùng.

Thêm **C31 — Phông/cỡ chữ khối ký**: nhận diện quyền hạn/chức vụ và họ tên trong bảng kết thúc, có Nơi nhận/Lưu phía trái. Kiểm Times New Roman, đen, cỡ13–14, đứng, đậm theo NĐ30 PL I.I.4 và II.7.đ, PDF21,26–27. Chỉ thuộc tính sai mới được đề xuất sửa. Không ép đồng loạt về13; giữ từng cỡ13/14 đúng. Bảo toàn nguyên văn, cấp bậc, chữ hoa/thường, ngắt dòng mềm, gạch chân và căn/thụt/khoảng đoạn. Ảnh/chữ ký/con dấu ở đoạn riêng nằm ngoài vùng ghi.

Nhóm **Chữ ký** trong Bắt buộc sửa có chọn riêng/chọn nhóm/chọn chung các nhóm khác. Tooltip ba dòng ngắn; danh sách dựng mới sau sửa, không giữ lỗi đã hết dưới dạng checkbox vô hiệu. Unknown không bị gọi là lỗi. Bảng lồng/gộp/control, trường Word trong mốc tên, danh sách tự động, họ tên mẫu trống, chức vụ lạ hoặc có nội dung/phụ lục sau bảng chưa mở sửa.

Bổ sung kiểm nhận diện, từng loại sai thật, kiểu/cỡ trộn hợp lệ, stale payload, giữ cỡ/gạch chân/bố cục, chọn nhiều nhóm và rollback. Luồng Word hiện có vẫn phải quét lại trước ghi, checkpoint, hậu kiểm sau lưu/Line/XPS và một Undo. Bài Word bổ sung mẫu giả có ảnh PNG, sửa riêng/chung ba Line, lỗi giữa lượt, Undo, lưu-mở và hash nguồn/ảnh. CI chỉ biên dịch bài này; không phải bằng chứng Office2021 chạy đạt.

Báo cáo trong ứng dụng/TXT cập nhật31 nhóm đề xuất/8 thủ công/1 minh họa, kèm ví dụ C31 và giới hạn. Đây là một nhóm tính năng có nhiều loại thuộc tính được sửa, không tính mỗi test/tài liệu là một tính năng. Tiếp tục tích lũy trước khi tạo Windows theo nhịp5–10 bước đã thống nhất. Chi tiết: [FORMAT-BATCH-01.md](FORMAT-BATCH-01.md).

## Đợt thể thức02 — sửa khổ giấy/lề (26/10/2026)

Thêm **C32 — Khổ giấy và lề trang** theo NĐ30, PL I, Phần I.I.1–3 (PDF21). Với một section dọc thông thường đã xác minh, đặt giấy sai vềA4; chỉ lề ngoài khoảng trên/dưới20–25, trái30–35, phải15–20mm về biên gần nhất. Giữ nguyên mỗi lề hợp lệ. Ví dụ Letter, lề10/22/20/15 → A4,20/22/30/15mm.

Bản candidate chỉ sửa thuộc tính pgSz/pgMar, đối chiếu toàn bộ chữ/bảng/kiểu và mọi phần ZIP khác trước khi mở vào Word. Bảng phải vừa vùng mới. Ảnh/Shape, header/footer, nhiều section, bảng gộp/lồng/nổi, tab/field/control, trang ngang, cột, lề gáy/đối xứng/đóng sách hoặc dữ liệu chưa biết giữ thủ công; không tự đổi hướng trang.

Sửa thiết lập trang trước, sau đó quét/đo lại các mục đã chọn qua chữ/prefix Word thật. Không dùng lại CP hoặc toạ độ cũ. Chỉ nhóm người dùng đã chọn được áp dụng; mục hình học đã đạt sau đổi trang phải có bằng chứng xác nhận. Hậu kiểm lại chữ, kích thước trang XPS, mọi mục hình học đã chọn; lỗi ở bất kỳ giai đoạn nào phục hồi cả lượt; một Hoàn tác cho cả lượt thành công. Có progress và chọn nhóm/all, không giữ dòng lỗi đã hết.

Báo cáo nguồn cập nhật32 đề xuất/8 thủ công/1 minh họa, thêm ví dụ C32, giữ M01 cho ngoại lệ. Kiểm Core cho policy/guard/candidate/proof/tamper/rebind/transaction; WPF cho chọn nhóm/mẫu thật/thuộc tính section OpenXML. Có kịch bản Word giả sửa riêng/chung chữ và ba Line, lỗi sau trang/sau nhóm, Undo, lưu-mở, kiểu chữ và hash nguồn. CI chỉ biên dịch bài Word, chưa chứng minh Office2021 chạy đạt. Chưa có gói Windows mới.

Số trang: đã đối chiếu PL I.I.7 — sốẢRập từ1,13–14pt, đứng, giữa lề trên, ẩn trang đầu. Chưa triển khai sửa số trang trong đợt này: header/footer có field, liên kết và nhiều section cần một phạm vi riêng để bảo toàn dữ liệu. Xem [FORMAT-BATCH-02.md](FORMAT-BATCH-02.md).

## Đợt thể thức03 — số trang/PAGE (26/10/2026)

Thêm **C33 — Số trang và trường PAGE**, theo NĐ30 PL I.I.4 và I.7 (PDF21). Khi tài liệu Nhà nước có từ hai trang, một section dọc, header trống hoặc chỉ PAGE đã nhận diện chắc: đề xuất tạo/sửa PAGE dùng Times New Roman đen,13–14pt, đứng, căn giữa lề trên; sốẢRập từ1, ẩn trang đầu. Một trang không có header không bị báo thiếu. Giữ cỡ13/13,5/14 đúng, đậm/gạch chân; không đổi chữ/bảng/ảnh trong thân.

Khi bật khác trang đầu, tái sử dụng footer trước đây đang hiển thị ở trang1; không làm mất ghi chú footer. Header/logo/nội dung riêng, nhiều section/liên kết, odd-even, field khoá/kiểu chưa chắc hoặc PAGE ở footer chưa tự đổi. Phần chưa nhận diện không bị kết luận sai chỉ vì chưa tìm được PAGE.

Candidate chỉ thay header PAGE, cờ số/ẩn trang đầu/khoảng header và quan hệ/ContentTypes cần thiết; đối chiếu XML và từng byte phần không đổi. Các nhóm chọn kèm được sửa trước rồi đọc lại cấu hình/PAGE theo phân trang hiện tại; một Undo/rollback cả lượt. Hậu kiểm cả số thực trên từng trangXPS: trang1 không có số, trang2 hiện2, tiếp tục3...; đúng phông/màu/cỡ/đứng/vị trí. Đối chiếu vị trí và nội dung các lá hình ảnh/chữ/bảng/footer ngoài lề trên trước-sau. Không báo đạt từ codePAGE hoặc số part/Shape.

Có kiểm Core, WPF/schema, lỗi giữa lượt, footer trang đầu, bảo vệ dữ liệu, danh sách nhóm/tooltip/TXT và ví dụ. Kịch bản Word giả thêm sửa riêng/chung khoảng trắng/ba Line, lỗi sau nhóm/sau PAGE, lưu-mở/Undo/hash nguồn. CI biên dịch bài Word nhưng không chạy Office; chưa có bằng chứng Office2021 đạt C33. Tổng nguồn33 đề xuất/8 thủ công/1 minh họa; .70 chưa đóng gói. Chi tiết [FORMAT-BATCH-03.md](FORMAT-BATCH-03.md).
