# Xem và xuất tiến độ, chức năng

Đợt sau .64, code .65 chưa đóng gói: B01 mở C14 cho dấu câu sau ngoặc kép cong, giữ quote/chữ/kiểu và mép trong chưa chọn; ví dụ `Theo mẫu “nội dung” ,báo cáo.` → `Theo mẫu “nội dung”, báo cáo.`. TXT thêm phạm vi/ví dụ/giới hạn ở C14 và mốc .65; tổng30/8/1 giữ nguyên. Các nội dung .64 bên dưới là lịch sử phát hành trước, không lấy số kiểm cũ làm bằng chứng cho code mới.

Mốc .64 gom B01–B05 sau .63, tổng30 đề xuất/8 thủ công/1 minh họa. TXT ghi giữ nhấn mạnh/phông/cỡ và căn/spacing, quét chung một lượt nhận diện, C30 mép ngoặc kép và câu có một dấu .?!, đầy đủ ví dụ/ngoại lệ và việc tiếp theo. Metadata gói lấy từ pipeline đúng commit; không dùng tổng kiểm cũ hoặc coi gói thành Word runtime PASS. Các trạng thái chưa đóng gói bên dưới là lịch sử từng bước.

Đợt sau .63, code .64 chưa đóng gói; B04 bổ sung C30 khoảng trắng trong ngoặc kép cong, ví dụ “ nội dung báo cáo ” → “nội dung báo cáo”. Tổng hiện30 đề xuất/8 thủ công/1 minh họa. C30 ghi rõ giữ dấu/chữ/kiểu và phía chưa chọn, ô trống/unknown ngoài phạm vi, không coi là điều khoản riêng của NĐ30. Các tổng29 ở phần lịch sử B01–B03 dưới đây không phải tổng code B04.

B01–B02: C26 giữ đậm/nghiêng/gạch chân khi sửa phông/màu/cỡ, tránh ép size cả vùng; unknown kiểu/paragraph snapshot không cấp sửa. B02 mở C27–C29 cho đoạn văn thường có từ nhấn mạnh xen chữ thường đứng; giữ kiểu/cỡ từng glyph, chưa sửa bố cục đoạn toàn đậm/nghiêng hoặc không chắc. No-greeting anchors không mở rộng. TXT có ví dụ và giới hạn từng nhóm, tổng vẫn29/8/1; các mốc .63 là lịch sử phát hành trước, không lấy số cũ cho CI mới hoặc coi là Word pass.

B03 ghi trong mốc .64: nhận diện thân bài một lần cho C26–C29 trong mỗi lần quét thay ba lượt; giữ kết quả/thứ tự/giới hạn từng loại, quét mới và hậu kiểm. Không tăng số nhóm lỗi, không coi giảm CPU phân tích là bằng chứng tốc độ Word hoặc Office2021 đạt. Khi chốt B03 có ba bước, chưa đóng gói.

Mốc .63 gom đủ5 bước sau .62, tổng29 đề xuất/8 thủ công/1 minh họa. B05 mở C26–C29 cho văn bản có tên loại không Kính gửi bằng hai mốc văn xuôi XML/layout/kiểu chắc; chưa mở toàn thân bài. TXT giữ ví dụ các lỗi hiện có, giới hạn từng phạm vi, lịch sử B01–B05 và việc còn lại. Các dòng chưa đóng gói bên dưới là lịch sử lúc code chưa đủ5 bước; chỉ metadata/pipeline đúng commit chứng minh gói và số kiểm mới. CI không có Word/Office2021.

B03/B04 sau .62: C28 khoảng giữa hai đoạn và C29 giãn dòng, tổng hiện tại29 đề xuất/8 thủ công/1 minh họa. TXT có ví dụ0+0pt → After6pt, Double → Single; nói rõ không kết luận sai từ After đơn lẻ, Exactly/AtLeast/contextual/auto/grid chưa đo và phạm vi văn xuôi chắc. Sửa riêng/chung giữ phần chưa chọn, hậu kiểm đoạn kế/glyph/thuộc tính sau lưu/Line. Các tổng27/26 ở dưới là lịch sử B02/B01, không phải tổng code mới. .63 chưa có gói, CI không có Word.

B02 sau .62 bổ sung C27, tổng hiện tại27 đề xuất/8 thủ công/1 minh họa. Ví dụ căn trái/dòng đầu0cm → căn đều/1,27cm; giữ1cm/1,27cm đúng và các thuộc tính chưa chọn. Chưa sửa giãn dòng/khoảng đoạn hoặc toàn thân bài; có snapshot và hậu kiểm sau lưu/Line, một Undo/rollback. Đoạn bên dưới về B01 là lịch sử phạm vi khi có26 nhóm. .63 chưa có gói tải, không coi code/CI là Word pass.

Đợt sau .62 (code .63 chưa đóng gói): thêm C26 phông/cỡ văn xuôi thường có ranh giới và proof chắc, tổng26 đề xuất/8 thủ công/1 minh họa. TXT có ví dụ Arial đỏ cỡ12 xen14 → Times New Roman đen,12 về13 giữ14; ghi rõ không áp toàn bộ thân bài, chưa sửa căn/giãn đoạn và chưa xác nhận Word runtime. Lịch sử .62 là mốc trước, không lấy tổng25 thay cho code mới. Kết quả build vẫn lấy metadata đúng phiên bản/commit, không hard-code.

Bản .62 gom năm hạng mục sau .61: sửa riêng cỡ sai, quốc hiệu/tiêu ngữ, viết hoa giữ kiểu, mục số nhiều cấp và khoảng cuối sau chấm phẩy. TXT có ví dụ/giới hạn và số kiểm từ build-checks.json đúng phiên bản/commit. [Nội dung .62](CHANGES-0.1.62.md) · [Danh sách chức năng](SUPPORTED-FIXES-0.1.62.md). CI không chạy Word.

Bản .61 gom năm hạng mục sau .60, trọng tâm kiểm định dạng và chọn/sửa nhiều nhóm có mục cần sửa trước. TXT có ví dụ sửa phông giữ từng cỡ hợp lệ, C12 chọn kèm rồi đo lại, ba mở rộng lỗi chữ và giới hạn Word runtime. Số kiểm thực tế lấy từ build-checks.json đúng phiên bản/commit; không lấy số dự kiến hoặc lời gọi giả để khẳng định tốc độ Office. Xem [nội dung .61](CHANGES-0.1.61.md).

Từ 0.1.41, chọn **Trợ giúp → Tiến độ và chức năng…**. Mở được khi chưa mở DOCX, không cần Word cho riêng thao tác xem/xuất báo cáo.

Báo cáo gồm phiên bản/thời điểm, số nhóm chức năng tính từ danh mục, loại lỗi có thể đề xuất sửa, các mục chỉ cảnh báo, lịch sử phần đã làm, bằng chứng kiểm thử đi kèm gói, giới hạn và việc tiếp theo. Các số nhóm không phải số lỗi trong tài liệu đang mở; báo cáo chưa thống kê lịch sử bấm sửa của từng tài liệu.

Bấm **Xuất báo cáo TXT…**, chọn thư mục/tên `.txt`. Tệp đọc đúng tiếng Việt trong Notepad hoặc Word. Muốn thay báo cáo cũ thì chọn cùng tên và xác nhận thay tệp theo hộp thoại lưu. Có thể chọn/sao chép nội dung trong cửa sổ. Lỗi lưu không thay tài liệu hoặc xóa danh sách lỗi chính; xử lý quyền/khóa hay chọn nơi lưu khác rồi thử lại.

Gói Windows kèm `build-checks.json` để báo đúng số kiểm thử đã đạt của đúng phiên bản, cùng báo cáo TXT mẫu tạo ở CI. Giữ các tệp đi kèm khi giải nén. Nếu thiếu hoặc sai dữ liệu kiểm thử, báo cáo vẫn xem/xuất được nhưng ghi rõ chưa có thông tin phù hợp; không tự suy PASS. Đây là thông tin kiểm thử tự động đi kèm gói, không thay xác nhận Word/Office 2021 hoặc kết quả kiểm tài liệu thực.

Thứ tự tiếp theo: hoàn thiện thể thức/độ ổn định; lỗi chữ và lặp cơ bản; ngữ cảnh/thư viện sau cùng. Các chức năng mới phải có mẫu và kiểm thử trước khi ghi vào danh mục. [Danh sách lỗi chi tiết hiện có](SUPPORTED-FIXES-0.1.47.md) và [kịch bản Windows/Office](WINDOWS-VALIDATION.md).

Từ .42 danh mục có 14 nhóm đề xuất sửa (thêm C14 về khoảng trắng quanh dấu phẩy/chấm phẩy), 8 nhóm thủ công và 1 minh họa. Đọc điều kiện của từng nhóm; không coi tổng nhóm là số lỗi trong văn bản.

Từ .43 có 15 nhóm đề xuất sửa (thêm C15 về khoảng trắng sát trong ngoặc tròn), 8 nhóm thủ công và 1 minh họa. Ô ngoặc chỉ chứa dấu cách được giữ cả khi sửa tất cả; dấu câu chưa chọn vẫn được quét sau sửa mép ngoặc. Mỗi nhóm vẫn có điều kiện, chưa phải kiểm đầy đủ chính tả/dấu câu.

Từ .44 có 16 nhóm đề xuất sửa (C16: dấu cách trước dấu .?! cuối đoạn có điều kiện), 8 nhóm thủ công và 1 minh họa. Không coi danh mục là đã kiểm toàn bộ chính tả/dấu câu; kết quả thực tế vẫn cần kiểm trên Word.

## Ví dụ lỗi từ .45

Mỗi mục C01–C16, M01–M08 và D01 có **Ví dụ** và kết quả/hướng xử lý. Báo cáo giữ khoảng trắng thật, ví dụ:

```text
C01 — Khoảng trắng thừa
  Ví dụ: “hôm       nay là thứ 2”
  Kết quả minh họa khi duyệt sửa: hôm nay là thứ 2
```

Phân biệt ba trạng thái phát triển: đã hỗ trợ đề xuất sửa (cần duyệt/đủ điều kiện); chưa tự sửa (chỉ cảnh báo và hướng thủ công); dự kiến (P01–P05 có ví dụ, chưa triển khai sửa tự động). D01 là minh họa riêng. Các hướng dự kiến gồm vị trí dọc số/ngày, thể thức vùng còn thiếu, dấu giữa câu, dữ liệu chính tả và ngữ cảnh sau cùng; không cộng vào 16 nhóm có thể đề xuất sửa.

Ví dụ phông/vị trí nêu cả kiểu hoặc số đo. [TAB] chỉ là ký hiệu tab. Các ví dụ là mẫu giả để thử, không lấy từ tài liệu đang mở và không có nghĩa chương trình đã sửa các lỗi đó trong tài liệu của bạn. Cửa sổ và TXT dùng cùng nội dung; số kiểm thử vẫn lấy từ dữ liệu đúng gói, không suy từ ví dụ. Các mục mới sau này cần có ví dụ/hướng xử lý tương ứng.

Từ .46 có 17 nhóm đề xuất sửa, thêm C17: `Ghi chú : nội dung` → `Ghi chú: nội dung` theo điều kiện. Mỗi mục mới tiếp tục có ví dụ và kết quả/hướng xử lý; 8 nhóm thủ công, 1 minh họa và 5 hướng dự kiến giữ trạng thái riêng.

Từ .47, M07 ghi đối chiếu XML vùng sửa chữ toàn thân ở ba chế độ. Field phức tạp/lệch XML có thể làm các đoạn phía sau phải kiểm tra thủ công; header/footer/textbox và đo tọa độ có phạm vi riêng. Danh mục vẫn 17 nhóm đề xuất/8 thủ công/1 minh họa; ví dụ và trạng thái dự kiến giữ rõ như .45.

Từ .48 có thêm C18 với ví dụ `Đã hoàn thành . Tiếp tục báo cáo` → `Đã hoàn thành. Tiếp tục báo cáo`; tổng 18 nhóm có thể đề xuất, 8 thủ công, 1 minh họa. P03 còn nghiên cứu thiếu khoảng sau dấu (`Đã hoàn thành.Tiếp tục báo cáo`), chưa chuyển thành khả năng đã hỗ trợ. Giới hạn/ngoại lệ C18 ghi riêng trong báo cáo và [nội dung .48](CHANGES-0.1.48.md).

Từ .49 có C19 `Đã hoàn thành.Tiếp tục báo cáo` → `Đã hoàn thành. Tiếp tục báo cáo`; tổng 19 đề xuất/8 thủ công/1 minh họa. C18/C19 có thể chọn riêng hoặc sửa hai phía một lượt, ví dụ và giới hạn trong báo cáo. P03 chuyển sang `Đã hoàn thành.tiếp tục báo cáo`: ranh giới chữ hoa/thường chưa rõ vẫn là việc dự kiến, không tự viết hoa. Các ghi chú .48 ở trên là lịch sử theo phiên bản. [Nội dung .49](CHANGES-0.1.49.md).

Từ .50, danh sách lỗi của tài liệu được thu gọn và chia ba phần, có tích theo nhóm/phần/tất cả; xem [cách dùng](ISSUE-LIST.md). Đây là phân loại các cảnh báo cụ thể đang xem, khác ba chế độ năng lực trong báo cáo TXT. Báo cáo giữ 19 nhóm đề xuất/8 thủ công/1 minh họa, thêm mốc .50; không biến số checkbox hoặc số nhóm trên màn hình thành số loại lỗi đã hỗ trợ.

Từ .51 có C20 thứ tự mục gõ tay và C21 dấu đầu dòng đơn theo mục cha, tổng21 đề xuất/8 thủ công/1 minh họa. Ví dụ C20/C21 có nhiều dòng tương ứng các đoạn riêng; không ghép thành một đoạn để thử. Báo cáo ghi giới hạn danh sách tự động/cấp/dẫn chiếu/tab và mốc .51. Các hướng P01–P05 vẫn là việc dự kiến, không coi ngữ cảnh cấu trúc là thư viện ngữ nghĩa đầy đủ. [Cách thử](OUTLINE-CONTEXT.md).

Từ .52, lịch sử có mốc tooltip ngắn ba phần và căn cứ có nội dung; giữ 21/8/1 cùng ví dụ C01–C21. Đây là cập nhật trình bày, không phải loại lỗi mới.

Từ .53, báo cáo ghi mốc sửa nhiều nhóm có đo lại, một Undo/rollback cả lượt; giữ 21/8/1 và ví dụ. Không dùng số loại lỗi làm số lỗi đã sửa trong tài liệu.

Từ .54 có C22 tên cơ quan và C23 nơi nhận, với ví dụ, tổng 23/8/1. Mốc .54 ghi nhận diện trích yếu và tách thông tin phạm vi XML.

Mốc .55 hoàn thiện hậu kiểm phông nơi nhận sau đoạn 40 trong lượt nhiều nhóm.

Từ .58 thêm C24 viết hoa tên loại đủ điều kiện, ví dụ `Báo cáo` → `BÁO CÁO`; tổng 24/8/1. M03 giữ trích yếu toàn hoa/vùng chưa rõ, không tự viết thường tên riêng hoặc mã. Nhật ký ghi rõ lỗi cơ quan/trích yếu .56 còn chờ bằng chứng runtime .57 hoặc mới hơn.

Từ .59, TXT có đủ 27 tên hành chính/3 bản sao theo PL III.I trang40, thêm Báo cáo đề xuất — BCĐX theo người dùng; ba nhóm nguồn riêng. Có cửa sổ tra cứu tương ứng trong Trợ giúp. Đây là dữ liệu tham chiếu, không tăng số nhóm sửa24/8/1 hoặc chứng minh mọi mẫu đã được kiểm.

## Bổ sung chưa phát hành — B03 sau .59

C17 đổi thành Khoảng trắng quanh dấu hai chấm, ví dụ `Ghi chú :nội dung` → `Ghi chú: nội dung`: sửa riêng thừa trước/thiếu sau hoặc cả hai phía. Đây là quy tắc ứng dụng có duyệt và có giới hạn về chữ/giờ/mã/protocol/vùng XML, không phải kiểm mọi dấu hai chấm. Báo cáo đang trong code chuẩn bị .60, chưa có trong ZIP .59. Đợt có 3 bước, [nhật ký](BATCH-AFTER-0.1.59.md); tổng nhóm vẫn 24/8/1.

## Bổ sung chưa phát hành — B04/C25

Báo cáo thêm C25 Khoảng trắng cuối đoạn sau dấu kết câu, ví dụ dấu cách thật `Đã hoàn thành.   ` → `Đã hoàn thành.`. Tổng hiện25 nhóm đề xuất/8 thủ công/1 minh họa, lịch sử ghi đủ B01–B04. Chỉ áp cho đoạn chữ thuần có kết thúc rõ, giữ dấu kết đoạn/ô và khoảng trước chưa chọn; xem [phạm vi và ngoại lệ](BATCH-AFTER-0.1.59.md). Code chuẩn bị .60, chưa có trong ZIP .59; chưa đóng gói trong bước4.

## Bản thử .60 — chốt B01–B05

C15 thêm thiếu khoảng sau ngoặc đóng trong cụm chữ rõ: `Theo mẫu ( nội dung )đã duyệt.` → `Theo mẫu (nội dung) đã duyệt.`; chọn riêng trong/ngoài hoặc cả nhóm. Báo cáo có lịch sử đủ5 bước, vẫn25/8/1. [Danh sách đủ/ví dụ](SUPPORTED-FIXES-0.1.60.md). Gói lấy số kiểm thực tế từ JSON theo đúng commit; các bài Word mới chưa được CI chạy runtime. Xem ghi chú .60 và nhật ký đợt để tiếp tục.
