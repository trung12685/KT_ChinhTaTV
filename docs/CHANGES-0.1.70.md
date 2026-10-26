# Chính tả Việt 0.1.70 — bản thử tích lũy thể thức văn bản

Gom năm đợt sau0.1.69 theo chu kỳ đã thống nhất. Mã tính năng đã qua kiểm Windows tại `bd583da42e26f3671bf381606a25b3d42f508c5b`; commit đóng gói bổ sung tài liệu/báo cáo và được chạy lại đầy đủ trong pipeline. Tổng **33 nhóm đề xuất/8 thủ công/1 minh họa**; chưa phải bộ kiểm chính tả/thể thức đầy đủ. Main và mốc quay lại0.1.67 giữ nguyên.

## Nội dung mới

| Đợt | Kết quả | Phạm vi chính |
|---|---|---|
| B01 | Bộ đọc cũ mặc định; checkbox thử bộ đọc mới bỏ tích khi mở ứng dụng | Dừng tối ưu tốc độ theo yêu cầu. Giữ đầy đủ dữ liệu bảng và Preview Word/XPS. |
| B02 — C31 | Sửa Times New Roman, đen, cỡ13–14, đứng/đậm cho chức vụ và họ tên khối ký | Bảng kết thúc có Nơi nhận/Lưu đối diện và mốc chắc. Giữ cỡ đúng, tên/cấp bậc, chữ hoa/thường, ngắt dòng/gạch chân/bố cục và ảnh ngoài vùng ghi. |
| B03 — C32 | Đổi giấy sai vềA4; chỉ lề sai về khoảng quy định, giữ lề đúng | Một section dọc đơn giản, bảng vừa vùng mới; chưa có ảnh/Shape/header/footer, field hoặc bố cục đặc biệt. |
| B04 — C33 | PAGE sốẢRập từ1, ẩn trang đầu, đen13–14 đứng, giữa lề trên | Một section dọc, header trống/PAGE-only. Giữ cỡ đúng/đậm/gạch chân, thân/bảng/ảnh và footer cả trang đầu. Một trang không bị báo thiếu số. |
| B05 | Sửa chung các nhóm có hậu kiểm cuối và một tổng tiến độ | Sửa lỗi fingerprint khi footer chứa toàn dấu cách; kiểm điều kiện trang từ full Document XML. Kiểm lại chữ/phông/cỡ/căn-khoảng đoạn/tâm/nét sau bước lưu-mở cuối; sai thì rollback cả lượt. |

NĐ30/2020/NĐ-CP, PL I.I.1–4/I.7 và II.7.đ là căn cứ các phần mới. Cỡ đang đúng được giữ theo từng ký tự; không ép toàn vùng về cỡ tối thiểu. Header có logo/nội dung riêng, nhiều section/liên kết, field khóa/không rõ, footer chứa PAGE chưa tự di chuyển; không ghi đè vùng chưa chứng minh.

## Chọn và kiểm nhiều nhóm

Khi đủ điều kiện: thiết lập trang trước → quét/đo lại → chữ/phông/căn-khoảng đoạn → căn giữa → Line → PAGE → hậu kiểm toàn lựa chọn. Không dùng CP/toạ độ cũ sau đổi trang/chữ. Chỉ sửa mục đã chọn; quét lại bỏ mục đã đạt. Một Hoàn tác cho cả lượt; COM/lưu/hậu kiểm lỗi phải phục hồi cả lượt. Không báo thành công chỉ từ số Shape, marker DOCX hoặc PAGE cache.

Đã thêm mẫu bảy mục: khổ/lề, PAGE, phông cơ quan, khoảng trắng và ba Line; có kiểm nguồn/kiểu/bảng, tiến độ, lỗi ba giai đoạn, Undo và lưu-mở. Các bài Word có trong ứng dụng; **CI chỉ biên dịch, không chạy Word/Office2021**. Chưa có xác nhận Office2021 cho toàn bộ phần mới. Số kiểm thực tế/commit/run của đúng gói do pipeline ghi vào release, build-checks.json và TXT.

## Cách dùng và các bước tiếp

Giải nén vào thư mục mới, chạy ChinhTa.Desktop.exe và kiểm phiên bản0.1.70. Thử bộ đọc mới mặc định tắt. Chạy Kiểm tra → Kiểm tra Word tự động…, sau đó thử bản sao tài liệu thật, một Hoàn tác và lưu/mở; xem [hướng dẫn kiểm Office](OFFICE-CHECK-0.1.70.md). Bản tải gồm .NET tự chứa, samples giả, danh mục hỗ trợ, hướng dẫn, nhật ký bốn đợt, build-checks.json, TXT có ví dụ, SHA256.txt và ảnh giao diện minh họa.

Tiếp tục kiểm các đợt thể thức trên Word/Office2021 và xử lý lỗi thực. Sau đó gom lỗi chữ cơ bản/khoảng trắng/dấu câu/từ-cụm lặp; ngữ cảnh và thư viện từ làm sau cùng. [Danh sách đầy đủ33/8/1 và ví dụ](SUPPORTED-FIXES-0.1.70.md). Giữ [bản0.1.67 để quay lại](https://github.com/trung12685/KT_ChinhTaTV/releases/download/v0.1.67/ChinhTaViet-0.1.67-win-x64.zip).
