# Danh sách lỗi có thể sửa — bản thử 0.1.60

25 nhóm có thể đề xuất sửa, 8 nhóm thủ công, 1 minh họa. Đây là năng lực chương trình, không phải số lỗi đã sửa trong file. Chỉ áp dụng sau duyệt khi đủ bằng chứng; chọn đúng chế độ. Không có cảnh báo không chứng minh văn bản đúng toàn bộ. Chưa có từ điển chính tả tiếng Việt đầy đủ, xác nhận runtime Office 2021 cho toàn bộ .60 hoặc bằng chứng khép lỗi phông sau nhóm trên .56.

C20/C21 đủ điều kiện ở **Bắt buộc sửa** từ .56 theo yêu cầu người dùng; không phải căn cứ pháp lý mới. Code C22/C10 có proof định dạng riêng khi thêm Line nổi của ứng dụng đủ bằng chứng; phản hồi thực tế trên .56 vẫn chờ đối chiếu runtime mới. Line lạ/cấu trúc chưa rõ vẫn thủ công; không mở thay chữ qua đối tượng.

## 1. Các nhóm có thể áp dụng sửa

| Mã | Lỗi/nhóm | Chương trình có thể sửa | Điều kiện và giới hạn |
| --- | --- | --- | --- |
| C01 | Khoảng trắng thừa | Gộp từ hai dấu cách thường liên tiếp giữa chữ thành một dấu cách, ví dụ `Đây  là văn bản` → `Đây là văn bản` | Cả ba chế độ. Không phải quy tắc xóa mọi loại khoảng trắng: không tự xóa thụt đầu dòng/tab hoặc gộp khoảng để trống số/ngày thuộc mẫu được nhận diện. Khoảng cách dùng dàn trang vẫn cần bạn duyệt. |
| C02 | Quốc hiệu | Chuẩn hóa viết hoa/khoảng trắng trong dòng đã nhận ra; Times New Roman, đậm, đứng, đen, cỡ 12–13 | Nhà nước, trang đầu. Giữ cỡ đang hợp lệ; ngoài khoảng đề xuất 12. Nhận cả cách viết HÒA/HOÀ hiện có. Trong ô bảng có thể căn giữa đoạn; đây không phải phép xác nhận tọa độ toàn trang. Không tự thêm quốc hiệu bị thiếu hoặc đoán lại dòng sai/mất chữ chưa nhận ra. |
| C03 | Tiêu ngữ | Chuẩn hóa chữ/khoảng trắng của dòng đã nhận ra; Times New Roman, đậm, đứng, đen, cỡ 13–14 | Nhà nước, trang đầu. Giữ cỡ hợp lệ; ngoài khoảng đề xuất 13. Giữ dấu gạch `-`/`–` theo mẫu nhận diện; không có bộ sửa mọi dấu câu. Có thể căn giữa trong ô; đường kẻ kiểm riêng. |
| C04 | Tiêu đề Đảng | Chuẩn hóa dòng nhận diện thành `ĐẢNG CỘNG SẢN VIỆT NAM`; Times New Roman, đậm, đứng, đen, cỡ 15 | Chế độ Đảng, trang đầu. Không tự chèn tiêu đề bị thiếu hoặc sửa toàn bộ các thành phần văn bản Đảng. Không tự dịch tiêu đề vào vị trí trên trang. |
| C05 | Cách ghi số/ký hiệu | Chuẩn hóa nhãn `Số:`, khoảng trắng quanh `/`, dấu nối trong mã; thêm 0 cho số thứ tự 1–9 | Đầu văn bản Nhà nước được nhận diện. Ví dụ `số  :  3 / BC – ABC` → `Số: 03/BC-ABC`. Thêm chuẩn hóa hoa/thường prefix đã khớp danh mục và tên loại tương ứng duy nhất, ví dụ TTR → TTr. Giữ số và mã cơ quan; không tự cấp số/đoán mã hoặc đổi loại văn bản, không phân tích mọi mẫu có năm/cấu trúc riêng. |
| C06 | Cách ghi địa danh, ngày tháng | Chuẩn hóa dấu phẩy sau địa danh, từ ngày/tháng/năm và khoảng trắng; thêm 0 trước ngày 1–9 và tháng 1–2 | Đầu Nhà nước, mẫu phân tích rõ và giá trị lịch hợp lệ. Ví dụ `Địa danh  Ngày3  Tháng1  Năm2026` → `Địa danh, ngày 03 tháng 01 năm 2026`. Không tự chọn địa danh, điền ngày trống hoặc đổi ngày sai lịch. Không thêm 0 cho mọi tháng: tháng 3 được giữ/chuẩn hóa thành `tháng 3`. |
| C07 | Phông dòng số/ký hiệu | Times New Roman, đen, cỡ 13, chữ đứng | Dòng được nhận diện, định dạng đọc được/đồng nhất và đủ bằng chứng chữ thuần. Giữ chữ, ô trống, đậm/gạch chân và căn đoạn. Không áp kiểu này cho cả tài liệu. |
| C08 | Phông dòng địa danh/ngày tháng | Times New Roman, đen, cỡ 13–14, chữ nghiêng | Cùng điều kiện nhận diện như C07. Giữ cỡ trong 13–14; ngoài khoảng đề xuất 13. Giữ đậm/gạch chân và nội dung. Việc căn giữa là C11 riêng. |
| C09 | Phông tên loại văn bản | Times New Roman, đen, cỡ 13–14, đứng, đậm | Trang 1/40 đoạn đầu Nhà nước, sau cặp quốc hiệu–tiêu ngữ nhận diện duy nhất; tên loại duy nhất trong danh sách, nằm ngoài bảng và căn giữa đúng khối. Giữ cỡ hợp lệ; ngoài khoảng đề xuất 13. Viết hoa tên loại đủ điều kiện theo C24 riêng. |
| C10 | Phông trích yếu | Times New Roman, đen, cỡ 13–14, đứng, đậm | Khối có ranh giới rõ ngay dưới tên loại, căn giữa và có điểm dừng rõ trước nội dung. Giữ cỡ hợp lệ, nguyên văn, gạch chân/căn đoạn. Không tự viết thường trích yếu toàn chữ hoa. |
| C11 | Lệch tâm dòng số/ngày | Căn giữa dòng trong ô và đưa ba thụt lề ngang về 0 | Chỉ khi xác minh được mốc chữ và cùng khung ô trên XPS; dòng không bị cắt/xoay/xuống dòng, khối tham chiếu đã căn giữa. Giữ chữ và khoảng cách dọc. Chưa tự làm số và ngày cùng hàng hoặc sửa vị trí tuyệt đối toàn trang. |
| C12 | Ba đường kẻ Nhà nước | Tạo Line bị thiếu; đặt lại Line của ứng dụng bị sai độ dài/tâm/khoảng cách hoặc chưa hiện đúng nét | Dưới cơ quan, tiêu ngữ, trích yếu khi nhận diện và đo được mốc/nét XPS. Đường cơ quan và trích yếu chấp nhận 1/3–1/2 bề rộng dòng tham chiếu, đích tự sửa 3/7. Đường tiêu ngữ theo bề rộng tiêu ngữ với dung sai đo. Line có sẵn không mang dấu nhận diện ứng dụng, border, nhóm Shape hoặc ghép mốc chưa chắc giữ thủ công. |
| C13 | Từ/cụm từ lặp liên tiếp | Cụm 2–8 tiếng giống nhau: đề xuất chỉ xóa phần lặp, giữ lần đầu; có thể sửa riêng/nhóm sau khi duyệt | Cả ba chế độ theo cờ cho phép sửa hiện có. Chỉ qua dấu cách thường trong cùng đoạn, đúng chữ hoa/thường. Một tiếng lặp như “đều đều”, chuỗi toàn chữ hoa và dòng ứng viên số/ngày giữ thủ công. Không suy nghĩa hoặc tự xóa mọi trường hợp lặp có chủ ý. |
| C14 | Khoảng trắng quanh dấu phẩy/chấm phẩy | `Nội dung  ,  báo cáo;công tác` → `Nội dung, báo cáo; công tác` sau khi duyệt | Chỉ giữa hai từ ≥2 chữ cái và dấu cách ASCII. Bỏ qua dòng số/ngày, tab, dấu hiệu mã/đường dẫn/địa chỉ, cặp toàn chữ hoa, số và đuôi tên miền/tệp. Chưa xử lý mọi dấu câu hoặc suy nghĩa. Phạm vi chi tiết tại [nội dung .42](CHANGES-0.1.42.md). |
| C15 | Khoảng trắng quanh ngoặc tròn | `Theo mẫu ( nội dung )đã duyệt.` → `Theo mẫu (nội dung) đã duyệt.`; chọn riêng khoảng trong/thiếu sau | Ngoặc cân bằng, không lồng. Thêm sau chỉ khi hai từ trước, ít nhất hai từ trong và hai từ sau đủ rõ, từ ngay sau thường. Không thêm khoảng trước ngoặc mở, không suy diễn hàm/công thức/mã/tab/NBSP/ô trống hoặc XML chưa rõ. Giữ chữ/kiểu/phần chưa chọn. |
| C16 | Khoảng trắng trước dấu kết câu cuối đoạn | `Đã hoàn thành .` → `Đã hoàn thành.`; tương tự `?` và `!` sau duyệt | Chỉ ASCII space trước một dấu cuối đoạn, từ cuối ≥2 chữ cái thường và có từ trước. Bỏ qua số/viết hoa/dấu hiệu kỹ thuật/header/ngày/chuỗi dấu và ranh giới chưa rõ. Giữ khoảng sau/dấu đoạn; chưa xử lý dấu giữa câu hoặc thêm dấu/khoảng sau. [Phạm vi .44](CHANGES-0.1.44.md). |
| C17 | Khoảng trắng quanh dấu hai chấm | `Ghi chú :nội dung` → `Ghi chú: nội dung`; chọn riêng hoặc cả hai phía | Hai từ trái rõ, từ cuối thường, từ phải có chữ thường. Chỉ xóa ASCII space trước hoặc thêm sau bằng mốc `:`; giữ phần chưa chọn. Loại giờ/tỉ lệ/protocol/mã/đường dẫn/header/ngày và vùng chưa chắc. |
| C18 | Khoảng trắng trước dấu kết câu giữa đoạn | `Đã hoàn thành . Tiếp tục báo cáo` → `Đã hoàn thành. Tiếp tục báo cáo` sau duyệt | Chỉ xóa ASCII space trước một dấu .?!; hai từ trái rõ (từ cuối thường), từ phải viết hoa đầu; khoảng sau được xét riêng qua C19. Giữ khoảng sau. Bỏ qua số, viết tắt/đuôi tệp phổ biến, header/ngày, dấu hiệu kỹ thuật/control, chuỗi dấu và vùng chưa chứng minh. C18 không tự thêm khoảng sau; chọn C19 nếu đủ điều kiện. Không kiểm hết mọi viết tắt. [Phạm vi .48](CHANGES-0.1.48.md). |
| C19 | Thiếu khoảng trắng sau dấu kết câu | `Đã hoàn thành.Tiếp tục báo cáo` → `Đã hoàn thành. Tiếp tục báo cáo` | Thêm một ASCII space sau dấu .?!, hai từ trái rõ, từ cuối thường, từ phải hoa đầu/còn lại thường và biên space/hết đoạn. Giữ khoảng trước chưa chọn, chọn chung C18 sửa cả hai phía một lượt. Bỏ qua số, viết tắt/đuôi tệp phổ biến, kỹ thuật/control/header/ngày và vùng chưa chứng minh. Không tự viết hoa hoặc xử lý mọi dấu câu. [Phạm vi .49](CHANGES-0.1.49.md). |
| C20 | Thứ tự mục gõ tay theo mục cha | `1., 2., 4.` → `1., 2., 3.`; tương tự `a), b), d)` → `a), b), c)` và `I., II., IV.` → `I., II., III.` | Đầu các đoạn riêng, cùng cấp/cách ghi và mốc đầu rõ; chỉ đổi nhãn. XML/kiểu/thụt lề phải xác minh. Tab bố cục chỉ cho phép thay nhãn khi có bằng chứng hẹp, giữ tab. Bảng/danh sách tự động/cấp, bắt đầu lại hoặc dẫn chiếu chưa chắc giữ thủ công. Không đoán nội dung bị thiếu. |
| C21 | Dấu đầu dòng đứng một mình | Trong mục `1.`, chỉ có `- Nội dung` thì đề xuất bỏ `- `; một `+` dưới một dấu `-` xét tương tự | Có mục cha đã xác định trọn vẹn, đếm theo cha/cùng cấp. Giữ nếu có ≥2 dấu cùng cấp; không gộp dấu ở các cha khác nhau. Giữ chữ/thụt lề/tab trước dấu, chỉ bỏ dấu và separator ASCII; separator tab hoặc biên chưa rõ giữ thủ công. Lựa chọn trình bày của người dùng, cần duyệt. |
| C22 | Tên cơ quan chủ quản/ban hành | TNR đen, in hoa, cỡ 12–13, đứng; ban hành đậm | Khối trái đầu trang có mốc Số và cặp quốc hiệu/tiêu ngữ duy nhất; không đoán tên chính thức. Phông đúng không báo lỗi. |
| C23 | Nơi nhận 9b | Nhãn 12 nghiêng đậm; danh sách 11 đứng; dấu đầu dòng và dấu cuối ; hoặc . cho dòng Lưu | Có nhãn và Lưu trong vùng đã xác minh. Không áp quy tắc dấu đơn/thứ tự mục thân bài cho danh sách này. |
| C24 | Viết hoa tên loại văn bản | `Báo cáo` → `BÁO CÁO` | Chỉ tên trong danh mục hành chính/bổ sung ở khối đầu Nhà nước đã xác minh, chữ thuần/định dạng đồng nhất; giữ trích yếu, phông/căn đoạn và khoảng ngoài tên; sửa chung phông sau duyệt, một Undo/rollback. |
| C25 | Khoảng trắng cuối đoạn sau dấu kết câu | `Đã hoàn thành.   ` → `Đã hoàn thành.` | Chỉ xóa ASCII spaces sau một dấu .?! cuối đoạn chữ thuần với hai từ trái rõ; giữ CR/LF/kết ô, chữ/dấu câu và khoảng trước chưa chọn. Không áp mẫu số/ngày, đoạn chưa kết câu, mã/viết tắt/technical/NBSP/control hoặc XML chưa chắc. |

C09/C10/C24 dùng chung [danh mục](DOCUMENT-TYPES.md): 27 tên hành chính theo PL III.I và Báo cáo đề xuất/BCĐX theo người dùng; bản sao để tra cứu riêng. Chỉ nhận diện/sửa khi các guard khối đầu/trích yếu và Word/XML đã đủ; chưa hỗ trợ mọi mẫu đặc thù. Không áp tên loại/phông/Line của văn bản gốc cho bản sao hoặc Công văn/V/v.

### Thứ tự phát triển và gợi ý ngôn ngữ minh họa

Ưu tiên thể thức, lỗi chữ/lặp trước; ngữ cảnh khó và thư viện thực hiện sau cùng theo thống nhất của người dùng. C13 là đối chiếu chữ lặp, không phải suy đoán ý nghĩa câu.

`con đo` → gợi ý `con đò` là quy tắc minh họa, có thể cảnh báo nhầm và phải đọc ngữ cảnh trước khi chọn. **Chưa có bộ từ điển chính tả tiếng Việt đầy đủ**, kiểm lỗi dấu/âm đầu/vần phổ quát, ngữ pháp hoặc viết lại câu. Không dùng số lượng cảnh báo còn lại để đánh giá chất lượng chính tả toàn văn bản.

## 2. Những mục hiện chỉ cảnh báo hoặc chưa tự sửa

| Mục | Hành vi hiện tại |
| --- | --- |
| Khổ giấy, lề; trang ngang/lề đối xứng và ngoại lệ mẫu | Đưa cảnh báo để bạn kiểm tra; chưa tự đổi thiết lập trang. Chế độ cá nhân không áp kiểm giấy/lề Nhà nước/Đảng. |
| Số và ngày chưa cùng hàng, dòng chưa nằm dưới khối tham chiếu | Cảnh báo vị trí khi đo được; chưa tự dịch theo chiều dọc. |
| Tên loại chưa rõ, trích yếu toàn chữ hoa | Giữ thủ công; tên loại thuộc danh mục hành chính/bổ sung đủ điều kiện có C24 riêng. Không tự viết thường trích yếu. |
| Số văn bản bằng 0, ngày sai lịch, năm chưa đủ rõ, mẫu số/ngày chưa phân tích được hoặc nhiều ứng viên | Giữ thủ công, không đoán giá trị thay thế. Một lỗi an toàn khác trong dòng vẫn có thể có gợi ý riêng; cần xem từng mục. |
| Tên loại và ký hiệu đã biết nhưng không khớp | Cảnh báo đúng prefix; đối chiếu tên loại/ký hiệu đã đăng ký để sửa bên sai. Không tự đổi tên hay mã. BCĐX là bổ sung người dùng; unknown/bản sao/Công văn không bị suy diễn. |
| Mã cơ quan hoặc tên địa danh cần xác minh | Không xác nhận mã/tên chính thức. Chữ thường trong mã/đầu địa danh có thể được cảnh báo, chưa tự đổi tên/mã. |
| Quốc hiệu và tiêu ngữ có cỡ chưa thống nhất theo cặp minh họa | Cảnh báo để bạn chọn; không tự ép mọi trường hợp về một cặp cỡ mới. |
| Tab liên tiếp, khoảng trắng/tab đầu đoạn căn giữa trong chế độ cá nhân | Cảnh báo thủ công; không tự xóa tab hoặc thụt đầu dòng. |
| Trộn phông/kiểu ở vùng chưa đủ chứng cứ, field/control/liên kết/đối tượng nhúng trong vùng đã đối chiếu | Có thể giữ thủ công. Riêng trộn kiểu không mặc nhiên cấm mọi sửa chữ thuần đã chứng minh; xem khả năng chọn của từng gợi ý. |
| Nét có sẵn chưa ghép được, Line không phải của ứng dụng, đường chồng hoặc chưa đo được XPS | Không tự tạo thêm hoặc báo đạt chỉ từ số Shape; cần kiểm tra trong Word. |
| Thân bài, chữ ký, số trang, mọi mẫu riêng | Chưa kiểm/sửa đầy đủ. Tên cơ quan và nơi nhận chỉ theo phạm vi hẹp C22/C23. |

Từ .47, đối chiếu XML vùng sửa chữ áp dụng cho **toàn phần thân ở cả ba chế độ**, kể cả sau đoạn 40. Vùng có cấu trúc đã nhận ra giữ thủ công; field phức tạp hoặc lệch XML có thể chặn cả các đoạn phía sau. Đây không phải chứng minh mọi field/control đều sửa được. Phông/toạ độ đặc thù đầu Nhà nước vẫn có phạm vi riêng 40 đoạn; header/footer/textbox, bảng gộp/lồng và nhận diện phức tạp còn giới hạn. Tài liệu bật Track Changes hoặc được bảo vệ chưa được hỗ trợ mở để sửa.


## Cách thử và tiến độ

Có thể chọn chung các nhóm sửa được: chữ/phông → đo lại → căn giữa → đo lại → Line. Chỉ dùng mục đã chọn và số đo mới; một Undo bỏ cả lượt, không xác minh được thì rollback cả lượt. Không báo đạt từ số Shape. Lưu bản sao/mở lại phải giữ chữ và ngắt dòng.

[Sửa và thử .56](CHANGES-0.1.57.md), [cách chọn nhóm](ISSUE-LIST.md), [sửa nhiều nhóm](MULTI-GROUP-REPAIR.md), [báo cáo TXT có ví dụ trước/sau và việc tiếp theo](REPORT-EXPORT.md), [nhật ký phát triển](DEVELOPMENT.md). Báo cáo Trợ giúp/TXT liệt kê đủ C01–C25/M01–M08/D01 và 5 việc dự kiến riêng; không phải lịch sử sửa tài liệu. Bằng chứng CI đúng phiên bản trong build-checks.json và TXT đi kèm gói; CI không có Word.

Dùng Bao cao.docx ở Nhà nước: chọn cả ba Line + thứ tự mục + dấu đơn, quét lại không còn mục đã đạt hoặc cảnh báo giả cơ quan/trích yếu; thử Undo/lưu/mở và đổi thật một vùng sang phông/cỡ sai để kiểm không bỏ sót. Đọc kịch bản đầy đủ ở ghi chú .56. Giữ main/stable .27 cho đến khi có bằng chứng phù hợp.

Từ .57 có cửa sổ tiến độ sửa và thời gian chờ; số hậu kiểm nhóm còn tạm thời, chưa là thành công cả lượt. Chỉ hoàn tất sau quét lại. Mở rộng đọc định dạng cho Line trong nhánh tương thích/run gộp do Word và phông vùng trộn, không mở thay chữ qua drawing; vẫn chưa có xác nhận runtime Office 2021 cho phản hồi đang điều tra. Xem ghi chú .57.

Bản .58 thêm C24 và ví dụ; [phạm vi/cách thử .58](CHANGES-0.1.58.md). Lỗi cơ quan/trích yếu đã xác nhận trên .56 chưa khép, đang chờ runtime .57 hoặc mới hơn.

Từ .59 có cửa sổ tra cứu tên loại/viết tắt và danh mục trong TXT; giữ QyĐ/CTr/TTr/TrS đúng hoa-thường, không tự đổi mã cơ quan hoặc viết tắt theo tên loại. Xem [ghi chú .59](CHANGES-0.1.59.md).

## Đợt .60 gồm 5 bước sau .59

B01 đối chiếu hoa/thường viết tắt với tên loại; B02 cảnh báo mâu thuẫn tên loại/ký hiệu; B03 thiếu khoảng sau hai chấm; B04 C25 khoảng cuối đoạn; B05 thiếu khoảng sau ngoặc đóng. Xem [nhật ký và ví dụ](BATCH-AFTER-0.1.59.md), [ghi chú .60](CHANGES-0.1.60.md). C15/C17/C25 là quy tắc ứng dụng có duyệt, không tự gán căn cứ NĐ30. Kiểm nét XPS, đối chiếu chữ/phông, một Undo và rollback vẫn giữ.

Checklist người dùng trên bản .60: thử từng lỗi, chỉ sửa một mép, chọn chung nhiều nhóm, quét lại, Undo cả lượt, lưu/mở. Chạy Kiểm tra Word tự động trên máy có Office 2021, giữ TXT/log đúng phiên bản nếu lỗi. Các bài mới có trong code nhưng CI chỉ biên dịch; chưa có xác nhận runtime Office 2021 cho toàn bộ đợt hoặc khép lỗi phông sau sửa nhóm .56. Stable/main vẫn .27.
