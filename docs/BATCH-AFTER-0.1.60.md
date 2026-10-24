# Đợt phát triển sau 0.1.60

Tiếp tục ứng dụng hiện tại; prompt dành cho Claude chỉ là tài liệu nghiên cứu, không thay mục tiêu phát triển. Mốc đầu đợt là tag v0.1.60, commit `bd86e13f005fe41d3b9d068a317dd8a8795753ab`, gói đã phát hành với 2105 Core/OpenXML + 113 WPF và 1 kiểm metadata gói. CI không có Word.

Code mới dùng version **0.1.61**, chưa tạo gói/tag/release. Gom khoảng 5–10 bước có kiểm thử rồi chuẩn bị một gói; giữ nguyên bản .60 và main/stable .27.

## B01 — Cụm từ lặp chỉ khác chữ hoa đầu đoạn

| Trước | Sau khi duyệt |
|---|---|
| `Kế hoạch kế hoạch đã duyệt.` | `Kế hoạch đã duyệt.` |
| `Báo cáo báo cáo báo cáo.` | `Báo cáo.` |
| `  Kế  hoạch kế hoạch` | `  Kế  hoạch` nếu chỉ chọn lặp; khoảng chưa chọn giữ nguyên |

Mở rộng C13, không tăng số nhóm: vẫn 25 đề xuất/8 thủ công/1 minh họa. So sánh cụm 2–8 tiếng, cách nhau bằng ASCII space; đối chiếu Unicode NFC nhưng luôn giữ nguyên byte/ký tự của lần đầu, xóa đúng phần lặp theo CP nguồn. Chỉ cho phép ký tự đầu từ đầu tiên của đoạn viết hoa, bản lặp bắt đầu thường; phần còn lại vẫn khớp chính xác. Cho phép khoảng ASCII đầu đoạn nhưng giữ nguyên nếu chưa chọn lỗi khoảng trắng.

Không gộp hoa/thường giữa câu hoặc sau nhãn mục/tab/emoji; không xóa từ đơn như `Đều đều`, mã toàn hoa, hoặc khi chữ khác bên trong cụm không khớp. Không vượt dấu câu, ngắt đoạn/tab/NBSP. Bằng chứng XML vẫn bắt buộc; gợi ý cũ mất hiệu lực nếu quyền sửa hoặc ngữ cảnh đầu đoạn thay đổi. Đây là quy tắc ứng dụng cần duyệt, có thể lặp có chủ ý; không gán căn cứ pháp lý hoặc suy nghĩa.

Kiểm sửa riêng/chung cùng khoảng trắng, hai chấm, dấu kết câu/cuối đoạn và định dạng ký tự; giữ phần chưa chọn. Hậu kiểm nguyên văn, một lượt lưu và rollback toàn bộ khi lỗi. Bài Word có nguồn giả nhiều run, giữ kiểu đậm/gạch chân lần đầu, chọn riêng hai cụm, quét lại, một Undo, rollback, lưu/mở và băm nguồn; cần máy có Word để chạy thật, CI chỉ biên dịch.

Kiểm cục bộ đạt **2082 lượt assertion**, gồm 54 lượt bổ sung so với mốc .60 và các hồi quy cũ; đây là runner chọn lọc, không phải toàn bộ CI. Kết quả Windows đúng commit được ghi trong PR và Windows checks. Lỗi cảnh báo phông cơ quan/trích yếu sau sửa nhóm trên .56 vẫn chờ bằng chứng runtime để khép lỗi. Không dùng tính năng C13 mới để tuyên bố lỗi đó đã hết.

## B02 — Dấu phẩy/chấm phẩy sau ngoặc tròn

| Trước | Sau khi duyệt |
|---|---|
| `Theo mẫu (nội dung) ,báo cáo.` | `Theo mẫu (nội dung), báo cáo.` |
| `Theo mẫu (nội dung);  đã duyệt.` | `Theo mẫu (nội dung); đã duyệt.` |
| `Theo mẫu ( nội dung ) ,báo cáo.` | `Theo mẫu ( nội dung ), báo cáo.` nếu chỉ chọn dấu câu; khoảng trong chưa chọn giữ nguyên |

Mở rộng C14, giữ cùng FixKind/nhóm/tooltip và tổng 25/8/1. Nhánh cũ giữa hai từ giữ nguyên điều kiện; nhánh mới sau ngoặc chỉ nhận câu có hai từ trước rõ, từ cuối thường và cách ngoặc mở bằng ASCII space, ít nhất hai từ chữ trong ngoặc (mỗi từ ≥2 chữ cái, có chữ thường) và từ bên phải ≥2 chữ cái có chữ thường. Cả đoạn phải có ngoặc cân bằng, không lồng; parse cặp một lần khi gặp ứng viên sau `)`. Chỉ thay span dấu phẩy/chấm phẩy và các ASCII spaces sát bên, không thay ngoặc/chữ/CP khác.

Bỏ qua số/mã, tên hàm đã nhận diện, dấu hiệu kỹ thuật/header/ngày, tab/NBSP/control, ô trống, ngoặc lồng/thiếu, từ phải toàn hoa/một chữ/đuôi tên miền và XML chưa chắc. Gợi ý là quy tắc ứng dụng có duyệt, không phải căn cứ pháp lý. Không tuyên bố phân biệt được mọi hàm hay mọi ngữ cảnh ngôn ngữ.

Kiểm mới có NFC/NFD/emoji và CP thật, ba chế độ, XML/manual, stale khi quyền sửa hoặc ngữ cảnh đổi dù span dấu câu không đổi; chọn riêng ngoài/trong, ghép B01/cụm lặp/khoảng trắng/hai chấm/dấu kết câu/cuối đoạn/phông và rollback khi lỗi lưu. Bài Word giả có hai dấu câu ngoài/hai mép trong, giữ run đậm/gạch chân, chọn riêng/chung, quét, một Undo, rollback, lưu/mở/băm nguồn; CI chỉ biên dịch, chưa chạy Word thực tế.

Kiểm cục bộ đạt **2159 lượt assertion**, thêm 77 so với B01; đây là runner chọn lọc, không thay số kiểm CI. Hiện đủ **2 bước** trong code .61, chưa tạo gói Windows .61. Kết quả Windows đúng commit được lưu tại PR/CI; bản tải .60 giữ nguyên. Lỗi phông cơ quan/trích yếu .56 chưa khép.

## B03 — Khoảng trắng khi câu cuối chỉ có một từ

| Trước | Sau khi duyệt |
|---|---|
| `Đã hoàn thành.Xong!` | `Đã hoàn thành. Xong!` |
| `Đã hoàn thành . Xong!` | `Đã hoàn thành. Xong!` |
| `Đã hoàn thành  .Xong!` | `Đã hoàn thành. Xong!` nếu chọn cả hai phía |

Mở rộng biên phải dùng chung C18/C19: từ viết hoa đầu/còn lại thường, ≥2 chữ cái, có thể theo ngay một dấu .?! ở cuối đoạn; sau dấu cuối chỉ cho phép ASCII spaces và giữ nguyên nếu không chọn phép sửa tương ứng. Hai từ trái và guard chữ thuần/XML/header/date/kỹ thuật vẫn giữ. Không mở qua dấu phẩy/chấm phẩy/nháy/ngoặc, dấu câu còn nội dung phía sau, chuỗi dấu/ellipsis, số/allcaps hoặc viết tắt/đuôi tệp đã biết. Không tự viết hoa, thêm dấu hay đổi chữ; chưa kiểm mọi viết tắt hoặc suy nghĩa.

Chọn riêng khoảng trước, sau hoặc cả hai; quét mới giữ lỗi chưa chọn. Dùng span xóa ASCII và dấu câu neo dài một ký tự, không đoán vị trí chèn rỗng. C13/C14/C15 và các quy tắc chỉ dùng ClearEnding không đổi phạm vi; Clear chỉ được C18/C19 gọi.

Kiểm có ma trận ba dấu ở biên × ba dấu cuối, Unicode tổ hợp/emoji/CP, giữ kết đoạn và khoảng cuối chưa chọn, ngoại lệ viết tắt/đuôi tệp/chuỗi dấu, các chế độ, XML/manual/stale, ghép B01/B02/các lỗi chữ/phông qua nhiều đoạn và rollback lỗi lưu. Bài Word giả mới giữ Xong đậm/gạch chân qua chọn riêng hai phía, sửa chung, quét, một Undo, rollback/lưu-mở/băm nguồn; CI chỉ biên dịch.

Kiểm cục bộ đạt **2267 lượt assertion**, thêm 108 so với B02; đây là runner chọn lọc, không thay số kiểm Windows CI. Hiện **3 bước** trong code .61, chưa tạo gói Windows .61. Kết quả đúng commit tại PR/CI; gói .60 giữ nguyên. Chưa xác nhận Office 2021 hoặc khép lỗi phông .56.

## B04 — Sửa chung luồng kiểm định dạng và cảnh báo giả

Theo phản hồi người dùng, chuyển trọng tâm từ các lỗi chữ nhỏ sang một hạng mục hệ thống: nhận diện → đọc phông → đánh giá theo vai trò → sửa → hậu kiểm, gồm quốc hiệu/tiêu ngữ/Đảng, số/ngày, tên loại/trích yếu, cơ quan và nơi nhận. Tiếp nối B03 `e92ec19e048d6805e5cc3fee9eedaee786f10337` (2344 Core/OpenXML +113 WPF).

Hai lỗi chung tái hiện được: mọi quan sát khác nhau bị coi là trộn không an toàn dù đều hợp lệ; fallback đọc sâu chỉ mở ở một số vùng có Line, và hậu kiểm trước/sau Line dùng hai cách đọc khác nhau. Luồng mới gắn dữ liệu với text hiện tại, phân biệt đúng/sai thật/chưa chắc/sai cỡ hỗn hợp cần duyệt. Đen RGB/đen tự động, các cỡ cùng hợp lệ và đậm không bị ràng buộc không tự thành lỗi; kiểu của dấu cách/ngắt dòng không thay thế kiểu chữ nhìn thấy.

Đọc sau proof XML, dedupe mỗi vùng; aggregate rõ dùng nhanh, chưa rõ đi qua từ rồi các ký tự cần thiết; không ghép được toàn text thì fail closed/fallback có kiểm. Không tăng lượt kết xuất XPS. Mẫu giả 2400 ký tự: 601 lượt Font thay 1801 nếu đọc từng glyph; không phải số đo Word thực tế. Sửa phông/màu/kiểu giữ từng cỡ đang hợp lệ bằng cách không set Size, chụp/chiếu cỡ qua sửa chữ và hậu kiểm cỡ còn lại trước/sau lưu/Line. Cỡ 14 bị ép về 13 vẫn rollback dù cả hai cùng hợp lệ. Đổi hoa dùng Word.Case để giữ run khi chỉ đổi hoa/thường; kiểm nguyên văn vẫn bắt buộc.

| Ví dụ | Kết quả/hướng xử lý |
|---|---|
| Trích yếu TNR đen, đứng/đậm, có chữ 13 và chữ 14 | Không báo lỗi chỉ vì cỡ khác nhau |
| Cùng trích yếu nhưng Arial đỏ, cỡ 13/14 đều đúng | Sửa phông/màu, giữ từng cỡ 13/14, ngắt dòng và gạch chân |
| Một chữ cỡ 12 sai trộn chữ cỡ 14 đúng | Duyệt từng vùng khi chưa thể sửa cỡ mà giữ phần đúng; không ép toàn vùng về 13 |
| Một glyph chưa đọc được phông | Giữ thủ công, không suy từ glyph đầu rằng cả dòng đúng |

[Thiết kế và giới hạn](FORMAT-ASSESSMENT.md). Kiểm Core/Desktop có vai trò, scalar undefined, dữ liệu stale/quyền XML, Word thật/giả theo nguồn, Word ranges vượt biên, COM lỗi/mất kết nối, số lời gọi, một glyph sai, giả payload và hậu kiểm cỡ. OfficeFormatSystemCheck tạo mẫu giả đúng/sai, nhiều run/cỡ, dấu cách/ngắt dòng khác kiểu, sửa riêng/chung ba Line, XPS, quét/Undo/rollback/lưu-mở/băm nguồn; CI chỉ biên dịch.

Hiện **4 hạng mục** trong code .61, chưa tạo gói. Chưa khẳng định đã khép phản hồi thực tế .56 trên Office 2021: các nguyên nhân trong code đã sửa và có hồi quy, vẫn cần chạy bài Word/tài liệu thực tế. Gói .60/main/stable .27 giữ nguyên.

## Tiếp theo

Tiếp tục thể thức/lỗi chữ có điều kiện rõ, ngoại lệ và hồi quy; rà nhận diện và hiệu năng khi có số đo. Chính tả qua từ điển và ngữ nghĩa làm sau. Chỉ chốt gói đợt mới khi đủ bước và các kiểm bắt buộc đạt.
