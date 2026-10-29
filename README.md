# Chính tả Việt

Đang phát triển0.1.77: [PAGE cuối trang nhiều chữ số](docs/FORMAT-BATCH-20.md). Chưa có ZIP0.1.77; bản tải gần nhất vẫn0.1.76.

Mã nguồn mới có [đo thử giãn dòng/khoảng đoạn trên bản sao](docs/FORMAT-BATCH-23-26.md), [bảo toàn đường dẫn/tên tệp và xử lý lỗi đo](docs/FORMAT-BATCH-27-29.md). Quyền tự sửa Exactly/AtLeast vẫn cần bằng chứng Word thực; các kết quả Core/WPF không thay kiểm Word.

[Bản thử Windows0.1.77 và cách kiểm Word](docs/PREVIEW-0.1.77.md) được tạo thành artifact riêng bởi workflow **Windows preview artifact**; không thay các release bên dưới. [Đợt30–32](docs/FORMAT-BATCH-30-32.md) bổ sung lệnh kiểm theo nhóm và điều kiện đóng phiên nghiêm ngặt.

## Bản thử tích lũy 0.1.76 — phạm vi kiểm và hậu kiểm khổ/lề/số trang

Năm cụm đợt 15–19 sau 0.1.75: sổ phạm vi lần quét cho 33 nhóm, số PAGE được vẽ thật, bảo toàn bitmap, chuyển footer PAGE-only có điều kiện và sửa khổ/lề khi đầu–cuối trang thực sự trống. Thêm hai bài kiểm Word riêng, tổng tám nhóm; giữ khoảng ký tay 80 pt, bộ đọc thử nghiệm tắt mặc định và dừng tối ưu tốc độ.

[Thay đổi](docs/CHANGES-0.1.76.md) · [Phạm vi](docs/SUPPORTED-FIXES-0.1.76.md) · [Kiểm Word](docs/OFFICE-CHECK-0.1.76.md) · [Phần còn thiếu](docs/REMAINING-WORK.md).

[Tải Windows thử 0.1.76](https://github.com/trung12685/KT_ChinhTaTV/releases/download/v0.1.76/ChinhTaViet-0.1.76-win-x64.zip). Chỉ dùng khi asset đã xuất hiện tại release; số kiểm/commit thật nằm trong build-checks.json và TXT đi kèm. CI không chạy Microsoft Word, chưa xác nhận bài Word thực đã PASS. Giữ nguyên các bản tải cũ bên dưới.

## Bản thử tích lũy 0.1.75 — thể thức và kiểm Word theo nhóm

Năm cụm đợt 10–14: khối ký cuối tài liệu dài, bảo toàn ảnh inline đã có, **chừa chỗ ký tay 80 pt**, chứng minh nguồn khoảng đoạn/giãn dòng và thông báo phần chưa kiểm; thêm **Kiểm tra → Kiểm tra từng nhóm Word…** để lỗi nhóm trước không che kết quả nhóm sau. [Thay đổi](docs/CHANGES-0.1.75.md) · [Cách kiểm Word](docs/OFFICE-CHECK-0.1.75.md) · [Phân tích các phần còn thiếu](docs/REMAINING-WORK.md).

[Tải bản Windows thử 0.1.75](https://github.com/trung12685/KT_ChinhTaTV/releases/download/v0.1.75/ChinhTaViet-0.1.75-win-x64.zip). Số kiểm và commit của gói nằm trong build-checks.json/TXT; CI không chạy Word thực. Chỉ dùng kết quả release khi gói đã xuất hiện ở đường dẫn trên. Bộ đọc thử nghiệm tắt mặc định, dừng tối ưu tốc độ; danh mục 33/8/1. Các mục phát triển bên dưới là nhật ký trước khi đóng gói.

## Đang phát triển0.1.75 — chừa chỗ ký tay80pt

Theo yêu cầu người dùng, ưu tiên chừa chỗ để in rồi ký tay; dừng mở rộng ảnh chữ ký. Lệnh **Kiểm tra → Chừa chỗ ký tay (80 pt)** đo khoảng hiện có trên XPS, bổ sung phần thiếu khi bố cục đủ chỗ; giữ vùng đã đủ, Enter/chữ/phông và có một Hoàn tác. [Phạm vi và kiểm chứng](docs/FORMAT-BATCH-12.md). Đây là thiết lập người dùng, không phải quy định80pt của NĐ30.0.1.75 đang tích lũy ba cụm, chưa ZIP mới; bản tải gần nhất0.1.74, CI không chạy Word thực.

## Đang phát triển0.1.75 — ảnh inline trong khối ký

[Đợt thể thức11](docs/FORMAT-BATCH-11.md): đối chiếu ký tự đối tượng của ảnh nhúng rõ cấu trúc với range Word hiện tại, giữ đoạn ảnh không sửa; khối ký có ảnh nằm giữa chức vụ/họ tên tiếp tục được nhận diện khi đủ chứng minh. Kiểm mẫu ngắn/dài, hai đường XML, schema/ảnh, quyền, inventory Line và Undo/lưu-mở. Đã tích lũy hai cụm sau0.1.74; chưa ZIP mới, CI không chạy Word thực.

## Đang phát triển0.1.75 — khối ký ở cuối tài liệu dài

[Đợt thể thức10](docs/FORMAT-BATCH-10.md): khối ký đã được XML hiện tại/range Word chứng minh tiếp tục được quan sát phông sau đoạn40. Không dùng cờ bảng chưa quan sát để loại cả nhóm; giữ điều kiện đủ chức vụ/họ tên/quyền sửa và mọi kiểm bảo toàn. Thêm hồi quy từ quan sát native và mẫu Office sau60 đoạn. Chưa đóng ZIP0.1.75; bản tải gần nhất vẫn0.1.74. CI không chạy Word thực.

## Bản thử tích lũy0.1.74 — năm cụm kiểm thể thức

[Thay đổi0.1.74](docs/CHANGES-0.1.74.md): nét duy nhất và đủ số lần; paint/clip thực; mốc chữ hiển thị đầy đủ; dòng cuối nhiều cỡ chữ theo baseline; bằng chứng Word/XML cùng lần đọc trong bài kiểm mẫu. Xem [cách kiểm Office](docs/OFFICE-CHECK-0.1.74.md). Số kiểm/commit/run đúng gói nằm trong build-checks.json/TXT; CI không chạy Word. Bộ đọc cũ mặc định, dừng tối ưu tốc độ; danh mục33/8/1, main và gói cũ giữ nguyên. Các mục bên dưới là nhật ký từng cụm trước đóng gói.

## Đợt thể thức08 — đo đủ dòng cuối khi nhiều cỡ chữ

[Phạm vi và kiểm chứng](docs/FORMAT-BATCH-08.md): gom theo đường chân chữ, giữ toàn bộ các phần cùng dòng dù khác cỡ chữ và chọn đúng hàng cuối khi xuống dòng rõ. Hàng chồng, chữ nâng/hạ hoặc thứ tự không rõ chuyển thủ công; dòng số/ngày vẫn cần đúng một hàng. Mã0.1.74 đã tích lũy bốn cụm, chưa phát hành ZIP; gói gần nhất vẫn0.1.73. CI không chạy Word thực.

## Đợt thể thức07 — mốc chữ phải hiển thị đầy đủ

[Phạm vi và kiểm chứng](docs/FORMAT-BATCH-07.md): chữ ẩn, bị cắt, ngoài trang hoặc xoay/skew/mirror không được dùng để tự đặt đường kẻ/căn dòng. Chữ và nét dùng chung proof paint/clip; kiểm thêm phần mực nhô ngoài advance box. Bounding highlight để xem vị trí giữ riêng; nếu một phần mốc không xác minh được thì không dùng phần còn lại làm mốc giả. Mã0.1.74 đã tích lũy ba cụm, chưa phát hành ZIP; bản tải gần nhất vẫn0.1.73 và CI chưa xác nhận Word thực.

## Đợt thể thức06 — không lấy nét ẩn làm bằng chứng

[Chi tiết và phạm vi](docs/FORMAT-BATCH-06.md): loại nét chắc chắn không vẽ do ẩn/trong suốt/vùng cắt/fill rỗng; chỉ dùng phần được vẽ trọn vẹn. Clip một phần hoặc cách tô chưa chứng minh được chuyển thủ công và chặn hậu kiểm, tránh tự thêm nét chồng. Kiểm cả fill/stroke, transform và clip ancestor/layout. Mã 0.1.74 đang tích lũy hai đợt, chưa có ZIP mới; bản tải gần nhất vẫn0.1.73. CI không chạy Word thực.

## Đang phát triển sau 0.1.73 — hậu kiểm đủ nét

[Đợt thể thức 05](docs/FORMAT-BATCH-05.md): không công nhận nét chồng bằng nét đầu tiên; giữ nét theo từng lần xuất hiện, không dùng chung border cũ cho Line mới; dừng bù vị trí nếu mất nét nền; quét/lịch sử sửa báo thủ công khi nét chồng. Mã 0.1.74 chưa đóng gói, danh mục 33/8/1 và bộ đọc cũ giữ nguyên. Gói tải gần nhất vẫn 0.1.73, chưa có xác nhận toàn bài Word thực qua bản đó.

## 0.1.73 — nhãn Line đã bù vị trí sau lưu Word

[Windows x64](https://github.com/trung12685/KT_ChinhTaTV/releases/download/v0.1.73/ChinhTaViet-0.1.73-win-x64.zip) · [Thay đổi](docs/CHANGES-0.1.73.md) · [Hướng dẫn kiểm](docs/OFFICE-CHECK-0.1.73.md).

Phản hồi 0.1.72 vẫn dừng Subject: checkpoint cho thấy Word ghép Title ý định và Description marker vào alt VML sau bù vị trí; phép nhận cặp trước chỉ chấp nhận Description đơn. Bổ sung đối chiếu toàn bộ nhãn ghép với Title/marker cùng đối tượng; giữ kiểm endpoint, nét XPS, rollback/Undo và bộ đọc cũ. Đã tái hiện và kiểm candidate trên checkpoint đúng bước thất bại, giữ chữ/bảng/định dạng/part khác. Chưa xác nhận toàn bài Office thực đạt 0.1.73; kết quả CI theo đúng commit trong gói. Danh mục 33/8/1 giữ nguyên.

## 0.1.72 — sửa nhận diện Line sau khi Word lưu

[Windows x64](https://github.com/trung12685/KT_ChinhTaTV/releases/download/v0.1.72/ChinhTaViet-0.1.72-win-x64.zip) · [Thay đổi](docs/CHANGES-0.1.72.md) · [Hướng dẫn kiểm](docs/OFFICE-CHECK-0.1.72.md).

Phản hồi 0.1.71 xác nhận khóa checkpoint và Agency đạt, nhưng dừng trước Subject. DOCX kèm có cặp DrawingML/VML do Word lưu; mã cũ coi VML là đối tượng chưa ghép. Bản vá nhận một cặp hợp lệ là một Line, cập nhật cả cặp một lần và giữ nhánh/chữ/ảnh không rõ ở chế độ thủ công. Kiểm nét XPS/rollback/Undo giữ nguyên. Bài kiểm lưu DOCX/XPS/JSON trước từng role và báo nguyên nhân thiếu đề xuất. Chưa xác nhận Office thực chạy đạt toàn bộ 0.1.72; CI chỉ kiểm mã/gói. Giữ danh mục 33/8/1, bộ đọc cũ mặc định và các bản cũ.

## 0.1.71 — sửa khóa file trong Kiểm tra Word

[Windows x64](https://github.com/trung12685/KT_ChinhTaTV/releases/download/v0.1.71/ChinhTaViet-0.1.71-win-x64.zip) · [Hướng dẫn kiểm](docs/OFFICE-CHECK-0.1.71.md) · [Thay đổi](docs/CHANGES-0.1.71.md).

Log phản hồi ghi0.1.69: File.ReadAllBytes bị Word giữ file, lỗi0x80070020 trước so sánh; cùng mã còn ở0.1.70. Bản sửa dùng handle chỉ đọc chia sẻ tương thích Word, giữ kiểm mã băm trước/sau và lỗi thật. Đồng thời bài Kiểm tra Word không còn tự ép bật thử nghiệm đang tắt. Mặc định kiểm các chức năng bằng bộ đọc cũ, ghi rõ thử nghiệm chưa chạy; bật thử nghiệm thì so sánh vẫn bắt buộc và sai khác vẫn FAIL. Log thêm thông báo ngoại lệ, pha lỗi, hai snapshot và trường JSON khác nhau. Chưa xác nhận Office2021 chạy đạt bản sửa; giữ nguyên luật33/8/1, kiểm nét/bảng/Preview/Undo/rollback và bộ đọc. Bản0.1.70 và0.1.67 giữ nguyên.

## 0.1.70 — bản thử tích lũy thể thức văn bản

[Tải Windows x64](https://github.com/trung12685/KT_ChinhTaTV/releases/download/v0.1.70/ChinhTaViet-0.1.70-win-x64.zip) · [Release](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.70) · [33 nhóm hỗ trợ và ví dụ](docs/SUPPORTED-FIXES-0.1.70.md) · [Cách kiểm trên Office2021](docs/OFFICE-CHECK-0.1.70.md).

Gom năm đợt sau0.1.69: bộ đọc cũ mặc định, phông/cỡ khối ký C31, khổ giấy/lề C32, số trang PAGE C33 và hậu kiểm phối hợp. Giữ bảng, ảnh, Preview Word/XPS; sửa chung nhiều nhóm có tiến độ một tổng, một Hoàn tác và rollback cả lượt. [Thay đổi, phạm vi và các bước tiếp](docs/CHANGES-0.1.70.md). Chưa có bằng chứng Office2021 chạy đạt các phần mới; kết quả tự động/commit của đúng gói có trong build-checks.json và TXT đi kèm. Main và mốc quay lại0.1.67 giữ nguyên. Không tiếp tục tối ưu tốc độ đọc.

Các ghi chú từng bước trước đóng gói: [C31 khối ký](docs/FORMAT-BATCH-01.md), [C32 trang](docs/FORMAT-BATCH-02.md), [C33 PAGE](docs/FORMAT-BATCH-03.md), [hậu kiểm phối hợp](docs/FORMAT-BATCH-04.md). Số nhóm33/8/1 là danh mục có điều kiện, không phải kết quả kiểm toàn bộ một tài liệu.

Ứng dụng C# / WPF chạy trên Windows với Microsoft Word desktop, làm việc offline trên bản sao DOCX và xem trước bằng XPS. Không gửi nội dung tài liệu tới dịch vụ AI.

**[Danh sách nhóm sửa và bảng kiểm mốc lưu 0.1.67](docs/SUPPORTED-FIXES-0.1.67.md)** — gồm điều kiện, ví dụ, ngoại lệ ô trống và các mục chỉ cảnh báo.

Ưu tiên đã thống nhất: **thể thức → lỗi chữ và lặp từ/cụm từ → ngữ cảnh và thư viện sau cùng**. Phần chính tả tiếng Việt đầy đủ còn cần xây dựng; không coi quy tắc minh họa hiện tại là bộ kiểm hoàn chỉnh.

## Mã .70 — bộ đọc cũ làm mặc định

Phản hồi .69 trên file đang so: bật thử nghiệm quét5,0s/dựng1,8s; tắt quét2,1s/dựng1,2s. Đặt backend và checkbox mặc định tắt, đổi nhãn thành “Thử bộ đọc mới (đang đánh giá)”. Giữ nguyên bảng/Preview và mọi luật/hậu kiểm. Người dùng đang có .69 có thể tiếp tục để bỏ tích, chưa cần tải bản mới. Mã .70 chưa đóng gói cho riêng bước này.

[Kết quả thử](docs/PERFORMANCE-FEEDBACK.md) · [Thay đổi](docs/CHANGES-0.1.70.md).

## Thử nghiệm đọc 0.1.69 — giảm trao đổi Word/COM

Duyệt đoạn tuần tự có kiểm đủ/thứ tự, dùng chung phông/thuộc tính đoạn khi Word xác nhận đồng nhất, chuẩn bị chữ một lần cho cùng XPS và giữ Word riêng qua các lần mở. Có fallback và bật/tắt; giữ bảng, full Preview, nét XPS, hậu kiểm/Undo/rollback. Hiện thời gian mở ngoài Quét/dựng trang. Phản hồi .68 tăng tốc ít được giữ làm căn cứ đo tiếp, không tuyên bố tốc độ toàn bộ từ số getter giả lập. Mốc .67 và .68 giữ nguyên.

[Trang phát hành .69](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.69) · [Thay đổi và cách so](docs/CHANGES-0.1.69.md) · [Danh mục giữ nguyên](docs/SUPPORTED-FIXES-0.1.69.md).

## Thử nghiệm đọc 0.1.68 — dùng chung XML và đọc định dạng theo run

Giữ bảng, Preview Word/XPS và luật/hậu kiểm từ .67. Thử snapshot XML hiện tại dùng chung; vùng định dạng trộn đọc theo run khi mọi CP/chữ/phông Word khớp, còn lại dùng Words/Characters. Có tùy chọn bật/tắt trong menu Kiểm tra và log thời gian từng chặng. Không suy ra tốc độ toàn bộ từ số đo XML. Mốc tải/source/tag .67 giữ riêng để quay lại. Chưa có xác nhận Office2021 mới.

[Trang phát hành .68](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.68) · [Phạm vi/phép đo](docs/PERFORMANCE-EXPERIMENT-0.1.68.md) · [Thay đổi](docs/CHANGES-0.1.68.md).

## Mốc lưu 0.1.67 — trước thử nghiệm tăng tốc

Giữ nguyên chức năng/thuật toán từ 0.1.65 (`56891d501a32f183ba6154e9a5829b2cd65c34e4`), bảng, Preview Word/XPS, hậu kiểm và rollback. Chỉ cập nhật phiên bản, lịch sử báo cáo và tài liệu; chưa áp dụng tối ưu đọc DOCX. Giữ ZIP/source/tag 0.1.67 riêng để quay lại nếu thử nghiệm sau không đạt. Số kiểm đúng commit do pipeline ghi; chưa có xác nhận runtime Word/Office2021 mới.

[Trang phát hành .67](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.67) · [Nội dung và cách quay lại](docs/CHANGES-0.1.67.md) · [Danh sách chức năng/ví dụ](docs/SUPPORTED-FIXES-0.1.67.md).

## Bản thử tích lũy 0.1.65 — sửa cùng cụm ngoặc và dấu câu

Gom B01–B05 sau .64, trong đó ba hạng mục cuối được xử lý chung một lượt để tăng tiến độ. C14/C15/C30 phối hợp: dấu câu sau ngoặc, thiếu khoảng trước/sau, thừa khoảng trong, ngoặc kép thẳng và câu có dấu kết trong ngoặc tròn. Ví dụ:

- `Theo mẫu( nội dung nội dung )đã  duyệt;hoàn thành.` → `Theo mẫu (nội dung) đã duyệt; hoàn thành.`
- `Theo mẫu" kế hoạch "cần làm.` → `Theo mẫu "kế hoạch" cần làm.`
- `( Đã hoàn thành. )` → `(Đã hoàn thành.)`.

Giữ loại dấu, nội dung và kiểu/cỡ chưa chọn; ô trống/mã/unknown không được tự xử lý theo nhánh mới. Kiểm72 tổ hợp trước–trong–sau, sửa riêng/chung nhiều nhóm, quét mới, Undo/rollback và lưu/mở; không thay kiểm nét XPS bằng số Shape. Tổng30 đề xuất/8 thủ công/1 minh họa. Chỉ có bản tải khi pipeline đúng commit đạt; chưa xác nhận runtime Word/Office2021.

[Trang phát hành .65](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.65) · [Thay đổi/cách thử](docs/CHANGES-0.1.65.md) · [Danh sách](docs/SUPPORTED-FIXES-0.1.65.md) · [Nhật ký](docs/BATCH-AFTER-0.1.64.md). Main/stable .27 và gói .64 giữ nguyên.

## Bản thử tích lũy 0.1.64 — năm hạng mục sau .63

- B01: sửa phông/màu/cỡ thân bài giữ từng đậm/nghiêng/gạch chân và cỡ13/14 đúng, không ép size cả vùng.
- B02: căn/thụt/khoảng đoạn/giãn dòng cho đoạn thường có từ nhấn mạnh xen chữ thường đứng; chọn riêng giữ lỗi phông/chữ chưa chọn.
- B03: nhận diện chung thân bài C26–C29 một lần thay ba lượt trùng trong mỗi lần quét, giữ đầy đủ quét mới và hậu kiểm.
- B04: C30 `“ nội dung báo cáo ”` → `“nội dung báo cáo”`, giữ mẫu trống `“    ”`.
- B05: C30 `“ Đã hoàn thành. ”` → `“Đã hoàn thành.”`, giữ dấu kết câu và kiểu/cỡ.

Tổng30 đề xuất/8 thủ công/1 minh họa. Giữ các guard XML/vùng chưa chắc, chọn riêng/chung chữ/phông/Line, nét XPS thực, một Undo và rollback. Đủ5 bước để đóng gói theo chu kỳ5–10 bước; chỉ có bản tải khi pipeline đúng commit đạt. CI không chạy Word/Office2021, chưa khép phản hồi phông cũ hoặc chứng minh tốc độ toàn phiên Word.

[Trang phát hành .64](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.64) · [Thay đổi/cách thử](docs/CHANGES-0.1.64.md) · [Danh sách chức năng](docs/SUPPORTED-FIXES-0.1.64.md) · [Nhật ký](docs/BATCH-AFTER-0.1.63.md). Main/stable .27 và gói .63 giữ nguyên.

## Bản thử tích lũy 0.1.63 — năm hạng mục thân bài

- B01/C26: Arial đỏ12 xen14 → Times New Roman đen13/14; giữ từng cỡ đang đúng, glyph/gạch chân và bố cục chưa chọn.
- B02/C27: căn trái/đầu dòng0cm → căn đều/1,27cm; giữ mức1cm hoặc1,27cm đang đúng.
- B03/C28: hai đoạn liền nhau cùng trang có After0 + Before0 → sau đoạn trước6pt; xét cả hai phía, không báo thiếu chỉ từ After<6.
- B04/C29: Double/Multiple ngoài1–1,5 → Single; không đoán Exactly/AtLeast/grid hoặc khoảng đoạn gộp/auto/contextual chưa đo.
- B05: văn bản có tên loại không Kính gửi vẫn nhận thân bài khi hai đoạn mở đầu có XML căn đều/thụt1cm hoặc1,27cm, kiểu và căn/thụt Word thực tế chắc, có chữ ký/Nơi nhận. Mốc ngắn đi sau sửa lặp không làm mất lỗi chưa chọn phía sau.

Đủ **5 bước**, tổng29 đề xuất/8 thủ công/1 minh họa; chưa phải bộ kiểm đầy đủ NĐ30/chính tả. Sửa riêng/chung giữ phần chưa chọn, đo XPS mới trước Line, hậu kiểm glyph/thuộc tính/ngữ cảnh/đoạn kế, một Undo/rollback và giữ nguồn.

[Gói .63](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.63) có ZIP/TXT/SHA-256 khi pipeline phát hành đúng commit đạt. [Nội dung/cách thử](docs/CHANGES-0.1.63.md) · [Danh sách chức năng](docs/SUPPORTED-FIXES-0.1.63.md) · [Nhật ký](docs/BATCH-AFTER-0.1.62.md) · [Không Kính gửi](docs/BODY-OPENING.md). CI không chạy Word, chưa xác nhận runtime Office2021 hoặc khép phản hồi phông cũ. Gói .62 và main/stable .27 giữ nguyên.

## Bản thử tích lũy 0.1.62 — năm hạng mục sau .61

- B01–B02: sửa riêng cỡ sai trong các vùng đã nhận diện, gồm quốc hiệu/tiêu ngữ giữ nguyên chữ; giữ từng cỡ đúng và đo lại trước Line.
- B03: `Báo cáo` cỡ13/14 →`BÁO CÁO`, giữ từng cỡ/gạch chân và các thuộc tính chưa chọn. Hậu kiểm định dạng sau lưu/Line, sai thì rollback toàn lượt.
- B04: dưới mục cha rõ, `1.1., 1.2., 1.4.` →`1.1., 1.2., 1.3.`; chỉ sửa số cuối, giữ prefix/tab, chặn cascade/dẫn chiếu/gợi ý cũ khi đổi tiền tố.
- B05: `Nội dung đã duyệt;   ` →`Nội dung đã duyệt;`; giữ dấu, đoạn/ô và trường số/ngày trống.

Đủ **5 hạng mục**, tổng25 đề xuất/8 thủ công/1 minh họa. [Gói .62](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.62) có ZIP/TXT/SHA-256 khi pipeline phát hành đúng commit đạt; [bản gần nhất .61](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.61) giữ nguyên. CI không có Word; chưa khép phản hồi Office2021 cũ bằng kiểm code. [Nội dung/cách thử](docs/CHANGES-0.1.62.md) · [Danh sách chức năng](docs/SUPPORTED-FIXES-0.1.62.md) · [Nhật ký](docs/BATCH-AFTER-0.1.61.md). Main/stable .27 giữ nguyên.

## Bản thử 0.1.61 — kiểm định dạng và sửa nhiều nhóm

B04 sửa chung luồng kiểm phông/cỡ/màu/kiểu cho quốc hiệu/tiêu ngữ, số/ngày, tên loại/trích yếu, cơ quan và nơi nhận. Các cỡ cùng hợp lệ, hai mã màu đen hoặc kiểu khác ở dấu cách/ngắt dòng không tự thành lỗi. Đọc sau proof XML, mỗi vùng một lần; aggregate rõ dùng ngay, chưa rõ thì đọc từ rồi ký tự cần thiết. Sửa phông/màu giữ từng cỡ hợp lệ và hậu kiểm cỡ ký tự còn lại sau sửa chữ/Line; chưa đọc chắc thì giữ thủ công. Không mở sửa chữ qua đối tượng hoặc bỏ kiểm nét XPS/Undo/rollback.

B05 nối luồng chọn/sửa nhiều nhóm: chọn đường kẻ tích kèm đúng lỗi chữ/phông cần sửa trước, duyệt rõ, sửa rồi đo lại và chỉ ghi Line khi cần. Bỏ đọc trang/prefix trùng CP và đo trùng mốc trong cùng XPS; không tái dùng qua sửa. B01–B03 trước đó thêm cụm lặp khác chữ hoa đầu đoạn, dấu câu sau ngoặc và câu cuối một từ. Đủ **5 hạng mục**, tổng25/8/1, chuẩn bị gói .61 sau các kiểm bắt buộc; cảnh báo cũ Office 2021 chưa khép chỉ bằng CI.

[Windows .61](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.61) · [Nội dung/cách thử](docs/CHANGES-0.1.61.md) · [Nhật ký](docs/BATCH-AFTER-0.1.60.md) · [Chọn nhiều nhóm](docs/REPAIR-DEPENDENCIES.md) · [Luồng định dạng](docs/FORMAT-ASSESSMENT.md). Link tải có gói khi pipeline phát hành đạt. Main/stable .27 giữ nguyên.

## Bản thử 0.1.60 — gom B01–B05

Gom **5 bước** sau .59: chuẩn hóa hoa/thường viết tắt theo tên loại chắc chắn; cảnh báo tên loại/ký hiệu mâu thuẫn để đối chiếu; sửa thiếu khoảng sau hai chấm; C25 bỏ dấu cách cuối đoạn có điều kiện; C15 thêm khoảng sau ngoặc đóng trong cụm chữ rõ. Ví dụ `Ghi chú:nội dung` → `Ghi chú: nội dung`, `Theo mẫu (nội dung)đã duyệt.` → `Theo mẫu (nội dung) đã duyệt.`. Chọn riêng/chung, giữ từ/khoảng chưa chọn, dấu kết đoạn/ô, kiểm nét XPS và một Undo/rollback. Tổng **25/8/1**, TXT có ví dụ và lịch sử đợt. **Chưa xác nhận toàn bộ bài Word .60 trên Office 2021; lỗi phông sau nhóm .56 vẫn chờ bằng chứng.**

[Tải Windows .60](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.60) · [Nội dung/cách thử](docs/CHANGES-0.1.60.md) · [Nhật ký B01–B05](docs/BATCH-AFTER-0.1.59.md) · [Chu kỳ 5–10 bước](docs/RELEASE-CADENCE.md). Gói .60 đã phát hành từ commit bd86e13, đạt 2105 Core/OpenXML + 113 WPF và kiểm metadata gói; chưa xác nhận bài Word trên Office 2021. Main/stable vẫn .27.

## Bản thử 0.1.59

Cập nhật [đầy đủ tên loại/viết tắt](docs/DOCUMENT-TYPES.md) theo NĐ30 PL III.I trang 40: 27 tên hành chính, 3 loại bản sao; thêm **Báo cáo đề xuất — BCĐX** theo yêu cầu người dùng, tách nguồn. Giữ đúng QyĐ/CTr/TTr/TrS, không báo chữ thường chỉ vì các viết tắt đúng bảng. Có cửa sổ **Trợ giúp → Danh mục tên loại văn bản…** và danh mục trong TXT; dùng chung cho nhận diện/C24/phông/mốc trích yếu nhưng vẫn qua guard. Bản sao chưa áp thể thức văn bản gốc, không tự đổi viết tắt theo tên loại hoặc mã cơ quan. [Tải Windows](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.59) · [Nội dung/cách thử](docs/CHANGES-0.1.59.md). Giữ 24/8/1, popup/XPS/Undo/rollback, stable .27; chưa Office2021 PASS, lỗi cũ .56 chờ bằng chứng mới.

## Bản thử 0.1.58

Thêm C24: tên loại đã nhận diện chắc như `Báo cáo` → `BÁO CÁO`, theo NĐ30 PL I.II.5.b. Giữ trích yếu/ngắt đoạn, phông/căn đoạn và khoảng ngoài tên; có thể sửa chung phông sau duyệt. Không đổi tên loại chưa rõ, trường/đối tượng hoặc tự viết thường trích yếu. [Tải Windows](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.58) · [Phạm vi và cách thử](docs/CHANGES-0.1.58.md). Báo cáo 24/8/1 có ví dụ C24. Giữ tiến độ .57, XPS, một Undo/rollback; lỗi cơ quan/trích yếu đã được người dùng xác nhận trên .56, đang chờ bằng chứng runtime .57 hoặc mới hơn. Chưa xác nhận Office 2021 cho .58; stable .27 giữ nguyên.

## Bản thử 0.1.57

Có cửa sổ tiến độ khi sửa: đang sửa nhóm nào, số mục hậu kiểm tạm thời, thời gian đã chờ, lúc đo lại/quét lại/phục hồi. Cửa sổ vẫn cập nhật khi Word bận; giữ COM trên luồng STA cũ. Kiểm phông hỗ trợ thêm Line do Word bọc nhánh tương thích/gộp run; nếu phông vùng bị trộn, đọc các ký tự Word đã xác minh. Giữ C20/C21 đủ điều kiện ở Bắt buộc sửa. [Tải Windows](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.57) · [Nội dung, giới hạn và cách thử](docs/CHANGES-0.1.57.md). Chưa có log đúng phiên bản của phản hồi lặp lại hoặc xác nhận runtime Office 2021; không kết luận đã hết lỗi thực tế chỉ từ kiểm XML/CI. Giữ stable .27, kiểm nét XPS và rollback.

## Bản thử 0.1.56

Sửa cảnh báo phông giả tại cơ quan/trích yếu sau chèn cả ba Line: chứng minh định dạng riêng cho Line nổi của ứng dụng đã xác minh, giữ chặn sửa chữ qua đối tượng. C20/C21 đủ điều kiện chuyển sang **Bắt buộc sửa** theo yêu cầu người dùng, vẫn duyệt trước khi ghi. [Tải Windows](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.56) · [Nội dung và cách thử](docs/CHANGES-0.1.56.md). Giữ kiểm nét XPS, một Undo/rollback, danh mục 23/8/1; chưa xác nhận runtime Office 2021, stable .27 giữ nguyên.

## Bản thử 0.1.55

Bao gồm toàn bộ sửa nhận diện/cơ quan/nơi nhận của .54; hoàn thiện hậu kiểm phông khi nơi nhận ở sau đoạn 40 được chọn cùng Line. Chỉ dùng toàn thân đã có bằng chứng XML/range, giữ giới hạn đo hình học. [Tải Windows](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.55) · [Nội dung .54](docs/CHANGES-0.1.54.md) · [Bổ sung .55](docs/CHANGES-0.1.55.md). Giữ 23/8/1; chưa có xác nhận runtime Office 2021, stable .27 giữ nguyên.

## Bản thử 0.1.54

Sửa nhận diện trích yếu nhiều đoạn/ngắt dòng, dấu cách cuối đoạn và Kính gửi căn giữa. Thêm kiểm/sửa tên cơ quan C22, nơi nhận 9b C23 theo Nghị định 30/2020/NĐ-CP: không nhầm danh sách nơi nhận với mục thân bài. Thông tin chưa đối chiếu XML tách khỏi lỗi dòng đầu. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.54) · [Nội dung và cách thử](docs/CHANGES-0.1.54.md). Danh mục 23 đề xuất/8 thủ công/1 minh họa. Giữ sửa nhiều nhóm/Undo/rollback, chưa xác nhận runtime Office 2021; stable .27 giữ nguyên.

## Bản thử 0.1.53

Chọn nhiều nhóm rồi bấm **Sửa các lỗi đã tích** hoặc **Sửa tất cả**: chương trình tự sửa chữ/phông (gồm thứ tự mục, dấu đầu dòng), đo lại, căn giữa và sửa Line theo các mục đã chọn. Không dùng vị trí/số đo cũ, không nhận thêm lỗi chưa chọn. Một Undo bỏ cả lượt; bước sau không xác minh được thì phục hồi tất cả. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.53) · [Cách thử](docs/MULTI-GROUP-REPAIR.md) · [Nội dung .53](docs/CHANGES-0.1.53.md). Giữ tooltip .52 và danh mục 21/8/1; chưa xác nhận runtime Office 2021, stable vẫn .27.

## Bản thử 0.1.52

Tooltip ngắn theo ba phần **Lỗi — Căn cứ — Cách xử lý**; căn cứ có vị trí và nội dung quy định áp dụng. Phân biệt Nghị định 30/2020/NĐ-CP, Hướng dẫn 05-HD/VPTW, quy tắc ứng dụng và lựa chọn người dùng; đường kẻ hiện chiều dài đề xuất thật, mức 3/7 theo cấu hình. Ngữ cảnh/số đo đầy đủ vẫn ở **Chi tiết lỗi**. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.52) · [Nội dung .52](docs/CHANGES-0.1.52.md) · [Cách dùng](docs/ISSUE-LIST.md). Giữ danh mục 21 đề xuất/8 thủ công/1 minh họa; chưa xác nhận đầy đủ Office 2021, stable vẫn .27.

## Bản thử 0.1.51

Thêm ngữ cảnh cấu trúc: gợi ý sửa thứ tự mục gõ tay `1,2,4 → 1,2,3`, tương tự chữ cái/La Mã; bỏ một dấu `-` hoặc `+` đơn trong mục cha đã xác định trọn vẹn theo yêu cầu người dùng. Dấu + đếm riêng dưới từng dấu -; có từ hai dấu cùng cấp thì giữ. Có bằng chứng XML cho thân/kiểu/thụt lề; nhãn mục có tab bố cục được xét riêng và giữ nguyên tab, không mở sửa chữ chung. Danh sách tự động/bảng/cấp hoặc dẫn chiếu chưa chắc giữ thủ công. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.51) · [Phạm vi và ví dụ](docs/OUTLINE-CONTEXT.md) · [Nội dung .51](docs/CHANGES-0.1.51.md). Báo cáo thêm C20/C21; chưa xác nhận đầy đủ Office 2021, stable vẫn .27.

## Bản thử 0.1.50

Thu gọn danh sách lỗi còn một dòng mỗi mục, giữ checkbox và số trang; mô tả/số đo/căn cứ chuyển sang tooltip và **Chi tiết lỗi**. Gom theo ba phần và theo loại, có checkbox từng lỗi/nhóm/phần/tất cả, trạng thái chọn một phần và thu gọn nhóm. Chỉ ghi **Thiếu đường kẻ…** ở nhánh đã xác định thiếu; Line có sẵn hoặc chưa chắc có tên riêng. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.50) · [Cách dùng danh sách mới](docs/ISSUE-LIST.md) · [Nội dung .50](docs/CHANGES-0.1.50.md). Giữ điều kiện sửa, lượt chữ/phông → căn giữa → Line, rollback/Undo và hậu kiểm XPS. Chưa xác nhận đầy đủ Office 2021; stable vẫn .27.

## Bản thử 0.1.49

Thêm C19: `Đã hoàn thành.Tiếp tục báo cáo` → `Đã hoàn thành. Tiếp tục báo cáo` sau duyệt, trong phạm vi ranh giới chữ rõ. Có thể chọn riêng C18/C19 hoặc sửa cả hai phía dấu câu trong một lượt (`hoàn thành .Tiếp tục`). Giữ chữ, dấu câu, khoảng chưa chọn và các giao dịch hiện có. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.49) · [Phạm vi và cách thử](docs/CHANGES-0.1.49.md). Báo cáo cập nhật C19/ví dụ/giới hạn; chưa xác nhận đầy đủ trên Office 2021, stable vẫn .27.

## Bản thử 0.1.48

Thêm C18: bỏ dấu cách trước một dấu kết câu giữa đoạn có ranh giới rõ, ví dụ `Đã hoàn thành . Tiếp tục báo cáo` → `Đã hoàn thành. Tiếp tục báo cáo`. Giữ chữ, dấu câu và khoảng sau; sửa riêng hoặc chung lỗi chữ cũ, báo cáo TXT có ví dụ. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.48) · [Phạm vi và cách thử](docs/CHANGES-0.1.48.md). Số/viết tắt nhận diện được/ký hiệu kỹ thuật/vùng XML chưa chắc vẫn loại trừ; chưa xác nhận toàn bộ bài Word trên Office 2021, stable vẫn .27.

## Bản thử 0.1.47

Mở rộng đối chiếu XML vùng sửa chữ cho toàn thân và cả ba chế độ, thay giới hạn cũ ở 40 đoạn đầu Nhà nước. Giữ thủ công các vùng cấu trúc/chưa chứng minh, chặn kế hoạch cũ; chữ thuần đã khớp vẫn xét sửa. Báo cáo cập nhật M07, ví dụ và giới hạn. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.47) · [Nội dung và cách thử](docs/CHANGES-0.1.47.md). Field phức tạp/lệch XML có thể chặn phần sau; chưa xác nhận toàn bộ bài Word trên Office 2021, stable vẫn .27.

## Bản thử 0.1.46

Thêm gợi ý xóa dấu cách trước dấu hai chấm như `Ghi chú : nội dung` → `Ghi chú: nội dung`, giữ chữ/dấu câu và khoảng sau. Có thể sửa chung các lỗi chữ cũ; báo cáo cập nhật C17 và ví dụ trước/sau. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.46) · [Nội dung và cách thử](docs/CHANGES-0.1.46.md). Chưa xác nhận toàn bộ bài Word trên Office 2021; stable vẫn .27.

## Bản thử 0.1.45

Báo cáo Trợ giúp/TXT có ví dụ trước/sau hoặc hướng xử lý cho đủ C01–C16, M01–M08, D01; thêm 5 hướng dự kiến có ví dụ và trạng thái chưa triển khai. Giữ nguyên dấu cách trong ví dụ C01 theo yêu cầu người dùng. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.45) · [Nội dung .45](docs/CHANGES-0.1.45.md) · [Cách xem/xuất báo cáo](docs/REPORT-EXPORT.md). Chưa xác nhận toàn bộ bài Word trên Office 2021; stable vẫn .27.

## Bản thử 0.1.44

Thêm gợi ý bỏ dấu cách trước một dấu .?! cuối đoạn trong phạm vi chữ rõ ràng; giữ chữ/dấu câu và khoảng sau, có thể sửa chung cùng ngoặc/phẩy/cụm lặp. Báo cáo Trợ giúp cập nhật C16, lịch sử và giới hạn. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.44) · [Nội dung và cách thử](docs/CHANGES-0.1.44.md). Chưa xác nhận bài Word trên Office 2021; stable vẫn .27.

## Bản thử 0.1.43

Thêm gợi ý xóa khoảng trắng sát trong ngoặc tròn có điều kiện; giữ ô ngoặc trống khi sửa tất cả và giữ cảnh báo dấu phẩy/chấm phẩy chưa chọn sau sửa ngoặc. Báo cáo trong Trợ giúp cập nhật C15, lịch sử và giới hạn. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.43) · [Nội dung và cách thử](docs/CHANGES-0.1.43.md). Chưa xác nhận bài Word trên Office 2021; stable vẫn .27.

## Bản thử 0.1.42

Thêm gợi ý khoảng trắng quanh dấu phẩy/chấm phẩy trong phạm vi chữ thông thường; gộp vùng khoảng trắng để sửa nhóm cùng cụm lặp không ghi chồng. Báo cáo trong Trợ giúp cập nhật C14, việc đã làm và giới hạn. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.42) · [Nội dung và cách thử](docs/CHANGES-0.1.42.md). Chưa xác nhận bài Word trên Office 2021; stable vẫn .27.

## Bản thử 0.1.41

Thêm **Trợ giúp → Tiến độ và chức năng…** để xem/xuất TXT: phần đã làm, số nhóm và danh sách lỗi có điều kiện, kết quả kiểm thử kèm đúng gói, giới hạn và việc tiếp theo. Không cần mở DOCX/Word cho báo cáo; không đổi tài liệu hoặc Undo. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.41) · [Cách dùng báo cáo](docs/REPORT-EXPORT.md) · [Nội dung .41](docs/CHANGES-0.1.41.md). Chưa xác nhận toàn bộ bài Office 2021; stable vẫn .27.

## Bản thử 0.1.40

Phát hiện lặp liên tiếp trong cùng đoạn. Cụm 2–8 tiếng giống nhau có thể đề xuất xóa bản lặp sau khi duyệt, giữ chữ/kiểu của lần đầu; một tiếng lặp, chuỗi toàn chữ hoa và dòng số/ngày giữ thủ công. Không suy đoán nghĩa câu. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.40) · [Nội dung và cách thử](docs/CHANGES-0.1.40.md). Chưa xác nhận runtime Office 2021; mốc ổn định vẫn 0.1.27.

## Bản thử 0.1.39

Tách lỗi dọn backup khỏi kết quả thay checkpoint: phục hồi/Hoàn tác đã mở lại đúng bản vẫn được ghi nhận khi tệp dự phòng chưa xóa được; giữ backup và ghi nhật ký. Lỗi thực khi đóng/chép/mở và nguyên nhân kiểm nét vẫn giữ nguyên. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.39) · [Nội dung và cách thử](docs/CHANGES-0.1.39.md). Chưa xác nhận runtime Office 2021; mốc ổn định vẫn 0.1.27.

## Bản thử 0.1.38

Bổ sung hậu kiểm phông/cỡ/đậm/đứng/màu và căn đoạn được yêu cầu sau lưu quốc hiệu, tiêu ngữ, tiêu đề Đảng; sai định dạng cũng phục hồi cả lượt. Đọc phông trên phần chữ, bỏ dấu kết thúc đoạn để tránh giữ cảnh báo sai khi dấu đoạn có kiểu khác. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.38) · [Nội dung và cách thử](docs/CHANGES-0.1.38.md). Chưa xác nhận runtime Office 2021; mốc ổn định vẫn 0.1.27.

## Bản thử 0.1.37

Chặn gợi ý sửa chữ/khoảng trắng trong vùng đã xác định có cấu trúc hoặc chưa đối chiếu được ở 40 đoạn đầu Nhà nước; tránh sửa vòng qua cảnh báo thủ công. Trước ghi nhóm chữ/phông, quét lại và đối chiếu toàn bộ lựa chọn, giữ rollback và kiểm nét XPS như trước. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.37) · [Nội dung và cách thử](docs/CHANGES-0.1.37.md). Chưa xác nhận runtime Office 2021; mốc ổn định vẫn 0.1.27.

## Bản thử 0.1.36

Bổ sung kiểm/sửa phông Times New Roman, màu đen, cỡ 13–14, đứng và đậm cho tên loại/trích yếu đã nhận diện chắc chắn ở đầu văn bản Nhà nước. Giữ chữ, cỡ đang hợp lệ, gạch chân và căn đoạn; cách viết hoa/thường và vùng chưa chắc vẫn thủ công. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.36) · [Phạm vi và cách thử](docs/CHANGES-0.1.36.md). Chưa xác nhận runtime Office 2021 cho chức năng mới; mốc ổn định vẫn 0.1.27.

## Bản thử 0.1.35

Lưu bản sao qua tệp tạm cùng thư mục, kiểm SHA-256 rồi thay tên để tránh công bố bản bị thiếu khi sao chép lỗi. Giữ tệp đích cũ và trạng thái chưa lưu nếu chưa thay thành công; chặn ghi đè gốc/bản làm việc/Undo, kể cả alias được xác minh. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.35) · [Phạm vi và cách thử](docs/CHANGES-0.1.35.md). Chưa xác nhận Office 2021; mốc ổn định vẫn 0.1.27.

## Bản thử 0.1.34

Siết hậu kiểm căn giữa: kiểm cả dòng số/ngày không chọn để phát hiện dịch chuyển ngoài ý muốn; sau rollback đo lại vị trí gốc. Nếu sửa đã xong nhưng quét lại thất bại, thông báo rõ kết quả và bỏ danh sách cũ để tránh sửa lại nhầm. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.34) · [Phạm vi và cách thử](docs/CHANGES-0.1.34.md). Chưa xác nhận Office 2021; 0.1.27 vẫn là mốc ổn định.

## Bản thử 0.1.33

Bổ sung căn giữa số/ký hiệu và địa danh/ngày tháng khi mốc chữ và cùng khung ô bảng được xác minh trên XPS. Giữ chữ, ô trống và khoảng cách dọc; đo lại sau lưu, rollback cả lượt nếu không đạt, một Undo cho nhóm. Khác hàng hoặc thiếu bằng chứng vẫn thủ công. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.33) · [Phạm vi và cách thử](docs/CHANGES-0.1.33.md). Chưa xác nhận Office 2021.

## Bản thử 0.1.32

Bổ sung cảnh báo vị trí số/ký hiệu và địa danh/ngày tháng theo khung chữ và đường cơ sở đo trên XPS: tâm so với khối cơ quan/quốc hiệu–tiêu ngữ, nằm dưới và cùng hàng. Chỉ cảnh báo để duyệt, chưa tự dịch chuyển. Thiếu mốc, chữ bị cắt/xoay/xuống dòng được ghi rõ chưa kiểm được. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.32) · [Phạm vi và cách thử](docs/CHANGES-0.1.32.md). Chưa xác nhận Office 2021.

## Bản thử 0.1.31

Bổ sung sửa Times New Roman, màu đen, cỡ và kiểu đứng/nghiêng cho dòng số/ký hiệu và địa danh/ngày tháng đủ điều kiện. Có thể sửa chung với cách ghi số/ngày trong một lượt; giữ chữ, ô trống, đậm/gạch chân và căn đoạn. Định dạng trộn hoặc mẫu chưa rõ vẫn thủ công; vị trí tương đối chưa tự sửa. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.31) · [Chi tiết và cách thử](docs/CHANGES-0.1.31.md). Chưa xác nhận Office 2021.

## Bản thử 0.1.30

Sửa nhóm chữ/số/ngày có giao dịch phục hồi: kiểm nguyên văn, lưu và đối chiếu toàn bộ chữ trong phạm vi đã chọn trước khi báo hoàn tất. Lỗi giữa lượt hoặc khi lưu sẽ phục hồi checkpoint và giữ lượt Hoàn tác trước đó; nếu phục hồi lỗi thì giữ hiện vật. [Tải bản thử 0.1.30](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.30) · [Chi tiết và cách thử](docs/CHANGES-0.1.30.md). Chưa xác nhận runtime Office 2021.

## Bản thử 0.1.29

Rà soát tiếp 0.1.28: giữ ô ngày/tháng/năm chưa điền khi cuối dòng có ghi chú; phân biệt điểm dừng tab trong định dạng với ký tự tab thật. Vẫn kiểm khoảng trắng thừa trong ghi chú và nội dung thường. [Tải bản thử 0.1.29](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.29) · [Chi tiết và cách thử](docs/CHANGES-0.1.29.md). Chưa xác nhận runtime Office 2021; 0.1.27 vẫn là mốc ổn định.

## Bản thử 0.1.28

Bổ sung kiểm số/ký hiệu và địa danh/ngày tháng đã nhận diện ở đầu văn bản Nhà nước; sửa hẹp nhãn, dấu phân cách và cách ghi số, giữ trường chưa điền. Chưa có xác nhận chạy đạt trên Office 2021. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.28) · [Phạm vi, căn cứ và cách thử](docs/CHANGES-0.1.28.md).

## Mốc ổn định 0.1.27 — 21/10/2026

Đã chốt phần sửa nhóm ba đường kẻ Nhà nước. Người dùng xác nhận thao tác sửa cả ba đường cùng lúc đã chạy được trên Office 2021 sau các bản thử 0.1.18–0.1.27. Đây là mốc phát triển của chức năng đã xác nhận; ứng dụng chưa kiểm đủ toàn bộ thể thức và chính tả tiếng Việt.

**[Tải Windows x64 0.1.27](https://github.com/trung12685/KT_ChinhTaTV/releases/download/v0.1.27/ChinhTaViet-0.1.27-win-x64.zip)** — giải nén cả gói vào thư mục mới, giữ thư mục `samples` đi kèm. Gói có sẵn .NET runtime; máy chạy vẫn cần Word desktop đã kích hoạt. SDK chỉ cần khi phát triển từ mã nguồn.

- [Biên bản chốt bản, nội dung đã xử lý, bằng chứng và giới hạn](docs/STATUS-0.1.27.md).
- [Hướng dẫn tiếp tục phát triển: kiến trúc, điều kiện an toàn, kiểm thử và việc tiếp theo](docs/DEVELOPMENT.md).
- [Kịch bản kiểm tra Windows/Office](docs/WINDOWS-VALIDATION.md).
- [Lịch sử các bản thử](docs/HISTORY-THROUGH-0.1.27.md), [chi tiết thay đổi cuối](docs/CHANGES-0.1.27.md), [căn cứ thể thức](docs/FORMAT-RULES.md).

## Những phần đã có

- Mở DOCX, chọn/rê chuột để đánh dấu riêng lỗi trong XPS; sửa lỗi đang xem, các lỗi đã tích hoặc tất cả gợi ý được phép; Hoàn tác và Lưu bản sao.
- Ba chế độ Nhà nước, Đảng, cá nhân; kiểm quốc hiệu/tiêu ngữ hoặc tiêu đề Đảng đã nhận diện, một số định dạng và cảnh báo giấy/lề theo từng chế độ.
- Nhà nước: nhận diện đường dưới cơ quan, tiêu ngữ và trích yếu; đọc marker từ DOCX, kiểm nét XPS thực tế và đối chiếu dòng chữ. Sửa nhóm có checkpoint, kiểm cuối lượt và rollback; một lần Hoàn tác cho cả lượt.
- Đường cơ quan/trích yếu chấp nhận dài 1/3–1/2 dòng tham chiếu; đích tự tạo 3/7. Đường tiêu ngữ bằng dòng tiêu ngữ, theo dung sai hiện có.
- Bản thử 0.1.28: số/ký hiệu, dấu phẩy sau địa danh, cách ghi ngày/tháng/năm; trường không rõ giữ thủ công. Không tự chọn mã, địa danh hay giá trị ngày/số.
- Khoảng trắng liên tiếp được kiểm tra, có ngoại lệ hẹp cho trường số văn bản/ngày còn để trống. Giữ các dấu gạch tiêu ngữ Word đã tự đổi.
- Phần ngôn ngữ hiện vẫn là quy tắc minh họa và khoảng trắng; chưa có từ điển chính tả tiếng Việt hoàn chỉnh. Hết cảnh báo không đồng nghĩa đạt toàn bộ NĐ30/HD05.

Chưa kiểm đầy đủ số/ký hiệu, địa danh/ngày tháng, nội dung, chữ ký/nơi nhận, số trang hoặc mọi mẫu và ngoại lệ. Header/footer/textbox, bảng gộp/lồng, Shape lạ và vùng chưa ghép chắc chắn còn giới hạn; một số trường hợp chỉ cảnh báo thủ công. Tài liệu bảo vệ hoặc bật Track Changes chưa được hỗ trợ.

## Chạy từ mã nguồn và kiểm thử

Cần .NET 8 SDK. WPF và Word COM phải chạy trên Windows.

```powershell
dotnet run --project tests/ChinhTa.Checks -c Release
dotnet run --project tests/ChinhTa.DesktopChecks -c Release
dotnet run --project src/ChinhTa.Desktop
```

Mốc 0.1.27 đạt **223 kiểm thử Core/OpenXML và 20 kiểm thử WPF** trong [CI Windows](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/39570349615). CI không có Word desktop. Xác nhận của người dùng trên Office 2021 áp dụng cho thao tác sửa ba đường đã báo đạt; lưu/mở lại, Hoàn tác trên máy người dùng và toàn bộ bài Office check chưa có xác nhận riêng được ghi nhận.

Menu **Kiểm tra → Kiểm tra Word tự động…** chạy một phiên Word riêng với mẫu giả trong `samples`, kiểm ghi Line, nét XPS, quét lại, lưu/mở và Hoàn tác. Kịch bản này có trong ứng dụng; sự tồn tại của kịch bản không phải bằng chứng đã chạy đạt trên mọi máy.

## Đóng gói bản tiếp theo

```powershell
dotnet publish src/ChinhTa.Desktop -c Release -r win-x64 --self-contained true -o artifacts/win-x64
```

Gói phân phối cần kèm `samples` và ghi chú phiên bản. Giữ nguyên tag/file tải của 0.1.27; tính năng mới dùng phiên bản mới. Xem quy trình trong [DEVELOPMENT.md](docs/DEVELOPMENT.md).
