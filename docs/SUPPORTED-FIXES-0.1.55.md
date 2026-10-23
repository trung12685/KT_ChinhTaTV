# Danh sách lỗi có thể sửa — bản thử 0.1.55

Danh sách cho bản thử 0.1.55, ngày 23/10/2026. Các nhóm C01–C17 kế thừa .46; .47 mở rộng đối chiếu XML vùng sửa chữ cho toàn thân ở cả ba chế độ; .48 bổ sung C18 trước dấu kết câu giữa đoạn; .49 thêm C19 thiếu khoảng sau và cho chọn riêng hai phía. Đây là khả năng đã lập trình, không phải xác nhận mọi chức năng đã chạy đạt trên Office 2021.

“Sửa được” nghĩa là chương trình đưa gợi ý có thể chọn và chỉ áp dụng sau khi bạn duyệt. Phải chọn đúng chế độ Nhà nước/Đảng/cá nhân. Cùng một loại lỗi có thể chỉ được cảnh báo thủ công nếu dòng chưa được nhận diện hoặc chưa đủ bằng chứng để sửa. Không có gợi ý không đồng nghĩa văn bản đã đúng toàn bộ thể thức/chính tả.

## 1. Các nhóm có thể áp dụng sửa

| Mã | Lỗi/nhóm | Chương trình có thể sửa | Điều kiện và giới hạn |
| --- | --- | --- | --- |
| C01 | Khoảng trắng thừa | Gộp từ hai dấu cách thường liên tiếp giữa chữ thành một dấu cách, ví dụ `Đây  là văn bản` → `Đây là văn bản` | Cả ba chế độ. Không phải quy tắc xóa mọi loại khoảng trắng: không tự xóa thụt đầu dòng/tab hoặc gộp khoảng để trống số/ngày thuộc mẫu được nhận diện. Khoảng cách dùng dàn trang vẫn cần bạn duyệt. |
| C02 | Quốc hiệu | Chuẩn hóa viết hoa/khoảng trắng trong dòng đã nhận ra; Times New Roman, đậm, đứng, đen, cỡ 12–13 | Nhà nước, trang đầu. Giữ cỡ đang hợp lệ; ngoài khoảng đề xuất 12. Nhận cả cách viết HÒA/HOÀ hiện có. Trong ô bảng có thể căn giữa đoạn; đây không phải phép xác nhận tọa độ toàn trang. Không tự thêm quốc hiệu bị thiếu hoặc đoán lại dòng sai/mất chữ chưa nhận ra. |
| C03 | Tiêu ngữ | Chuẩn hóa chữ/khoảng trắng của dòng đã nhận ra; Times New Roman, đậm, đứng, đen, cỡ 13–14 | Nhà nước, trang đầu. Giữ cỡ hợp lệ; ngoài khoảng đề xuất 13. Giữ dấu gạch `-`/`–` theo mẫu nhận diện; không có bộ sửa mọi dấu câu. Có thể căn giữa trong ô; đường kẻ kiểm riêng. |
| C04 | Tiêu đề Đảng | Chuẩn hóa dòng nhận diện thành `ĐẢNG CỘNG SẢN VIỆT NAM`; Times New Roman, đậm, đứng, đen, cỡ 15 | Chế độ Đảng, trang đầu. Không tự chèn tiêu đề bị thiếu hoặc sửa toàn bộ các thành phần văn bản Đảng. Không tự dịch tiêu đề vào vị trí trên trang. |
| C05 | Cách ghi số/ký hiệu | Chuẩn hóa nhãn `Số:`, khoảng trắng quanh `/`, dấu nối trong mã; thêm 0 cho số thứ tự 1–9 | Đầu văn bản Nhà nước được nhận diện. Ví dụ `số  :  3 / BC – ABC` → `Số: 03/BC-ABC`. Không tự cấp số, đoán mã, đổi mã chữ thường thành chữ hoa hoặc phân tích mọi mẫu số có năm/cấu trúc riêng. |
| C06 | Cách ghi địa danh, ngày tháng | Chuẩn hóa dấu phẩy sau địa danh, từ ngày/tháng/năm và khoảng trắng; thêm 0 trước ngày 1–9 và tháng 1–2 | Đầu Nhà nước, mẫu phân tích rõ và giá trị lịch hợp lệ. Ví dụ `Địa danh  Ngày3  Tháng1  Năm2026` → `Địa danh, ngày 03 tháng 01 năm 2026`. Không tự chọn địa danh, điền ngày trống hoặc đổi ngày sai lịch. Không thêm 0 cho mọi tháng: tháng 3 được giữ/chuẩn hóa thành `tháng 3`. |
| C07 | Phông dòng số/ký hiệu | Times New Roman, đen, cỡ 13, chữ đứng | Dòng được nhận diện, định dạng đọc được/đồng nhất và đủ bằng chứng chữ thuần. Giữ chữ, ô trống, đậm/gạch chân và căn đoạn. Không áp kiểu này cho cả tài liệu. |
| C08 | Phông dòng địa danh/ngày tháng | Times New Roman, đen, cỡ 13–14, chữ nghiêng | Cùng điều kiện nhận diện như C07. Giữ cỡ trong 13–14; ngoài khoảng đề xuất 13. Giữ đậm/gạch chân và nội dung. Việc căn giữa là C11 riêng. |
| C09 | Phông tên loại văn bản | Times New Roman, đen, cỡ 13–14, đứng, đậm | Trang 1/40 đoạn đầu Nhà nước, sau cặp quốc hiệu–tiêu ngữ nhận diện duy nhất; tên loại duy nhất trong danh sách, nằm ngoài bảng và căn giữa đúng khối. Giữ cỡ hợp lệ; ngoài khoảng đề xuất 13. Chưa tự đổi tên loại sang chữ hoa. |
| C10 | Phông trích yếu | Times New Roman, đen, cỡ 13–14, đứng, đậm | Khối có ranh giới rõ ngay dưới tên loại, căn giữa và có điểm dừng rõ trước nội dung. Giữ cỡ hợp lệ, nguyên văn, gạch chân/căn đoạn. Không tự viết thường trích yếu toàn chữ hoa. |
| C11 | Lệch tâm dòng số/ngày | Căn giữa dòng trong ô và đưa ba thụt lề ngang về 0 | Chỉ khi xác minh được mốc chữ và cùng khung ô trên XPS; dòng không bị cắt/xoay/xuống dòng, khối tham chiếu đã căn giữa. Giữ chữ và khoảng cách dọc. Chưa tự làm số và ngày cùng hàng hoặc sửa vị trí tuyệt đối toàn trang. |
| C12 | Ba đường kẻ Nhà nước | Tạo Line bị thiếu; đặt lại Line của ứng dụng bị sai độ dài/tâm/khoảng cách hoặc chưa hiện đúng nét | Dưới cơ quan, tiêu ngữ, trích yếu khi nhận diện và đo được mốc/nét XPS. Đường cơ quan và trích yếu chấp nhận 1/3–1/2 bề rộng dòng tham chiếu, đích tự sửa 3/7. Đường tiêu ngữ theo bề rộng tiêu ngữ với dung sai đo. Line có sẵn không mang dấu nhận diện ứng dụng, border, nhóm Shape hoặc ghép mốc chưa chắc giữ thủ công. |
| C13 | Từ/cụm từ lặp liên tiếp | Cụm 2–8 tiếng giống nhau: đề xuất chỉ xóa phần lặp, giữ lần đầu; có thể sửa riêng/nhóm sau khi duyệt | Cả ba chế độ theo cờ cho phép sửa hiện có. Chỉ qua dấu cách thường trong cùng đoạn, đúng chữ hoa/thường. Một tiếng lặp như “đều đều”, chuỗi toàn chữ hoa và dòng ứng viên số/ngày giữ thủ công. Không suy nghĩa hoặc tự xóa mọi trường hợp lặp có chủ ý. |
| C14 | Khoảng trắng quanh dấu phẩy/chấm phẩy | `Nội dung  ,  báo cáo;công tác` → `Nội dung, báo cáo; công tác` sau khi duyệt | Chỉ giữa hai từ ≥2 chữ cái và dấu cách ASCII. Bỏ qua dòng số/ngày, tab, dấu hiệu mã/đường dẫn/địa chỉ, cặp toàn chữ hoa, số và đuôi tên miền/tệp. Chưa xử lý mọi dấu câu hoặc suy nghĩa. Phạm vi chi tiết tại [nội dung .42](CHANGES-0.1.42.md). |
| C15 | Khoảng trắng sát trong ngoặc tròn | `( nội dung )` → `(nội dung)` sau khi duyệt; hai mép chọn được riêng, không ghi lại chữ | Ngoặc cân bằng/không lồng, chữ đơn giản và dấu cách thường. Bỏ qua phần trống/số/toàn hoa/ký hiệu kỹ thuật, tab, NBSP/ngắt dòng, số/ngày và ranh giới chưa rõ. Chưa sửa ngoặc thiếu/lồng/ngoặc vuông/nháy. [Phạm vi .43](CHANGES-0.1.43.md). |
| C16 | Khoảng trắng trước dấu kết câu cuối đoạn | `Đã hoàn thành .` → `Đã hoàn thành.`; tương tự `?` và `!` sau duyệt | Chỉ ASCII space trước một dấu cuối đoạn, từ cuối ≥2 chữ cái thường và có từ trước. Bỏ qua số/viết hoa/dấu hiệu kỹ thuật/header/ngày/chuỗi dấu và ranh giới chưa rõ. Giữ khoảng sau/dấu đoạn; chưa xử lý dấu giữa câu hoặc thêm dấu/khoảng sau. [Phạm vi .44](CHANGES-0.1.44.md). |
| C17 | Khoảng trắng trước dấu hai chấm | `Ghi chú : nội dung` → `Ghi chú: nội dung` sau duyệt; chỉ xóa space trước dấu | Hai từ kề nhau phía trái (từ cuối thường), từ phải đã được ngăn bằng space và đủ rõ. Bỏ qua số/giờ/tỉ lệ số hoặc La Mã, dấu kỹ thuật/giao thức/header/ngày và ranh giới chưa rõ. Giữ khoảng sau để chọn riêng. [Phạm vi .46](CHANGES-0.1.46.md). |

| C18 | Khoảng trắng trước dấu kết câu giữa đoạn | `Đã hoàn thành . Tiếp tục báo cáo` → `Đã hoàn thành. Tiếp tục báo cáo` sau duyệt | Chỉ xóa ASCII space trước một dấu .?!; hai từ trái rõ (từ cuối thường), từ phải viết hoa đầu; khoảng sau được xét riêng qua C19. Giữ khoảng sau. Bỏ qua số, viết tắt/đuôi tệp phổ biến, header/ngày, dấu hiệu kỹ thuật/control, chuỗi dấu và vùng chưa chứng minh. C18 không tự thêm khoảng sau; chọn C19 nếu đủ điều kiện. Không kiểm hết mọi viết tắt. [Phạm vi .48](CHANGES-0.1.48.md). |

| C19 | Thiếu khoảng trắng sau dấu kết câu | `Đã hoàn thành.Tiếp tục báo cáo` → `Đã hoàn thành. Tiếp tục báo cáo` | Thêm một ASCII space sau dấu .?!, hai từ trái rõ, từ cuối thường, từ phải hoa đầu/còn lại thường và biên space/hết đoạn. Giữ khoảng trước chưa chọn, chọn chung C18 sửa cả hai phía một lượt. Bỏ qua số, viết tắt/đuôi tệp phổ biến, kỹ thuật/control/header/ngày và vùng chưa chứng minh. Không tự viết hoa hoặc xử lý mọi dấu câu. [Phạm vi .49](CHANGES-0.1.49.md). |

| C20 | Thứ tự mục gõ tay theo mục cha | `1., 2., 4.` → `1., 2., 3.`; tương tự `a), b), d)` → `a), b), c)` và `I., II., IV.` → `I., II., III.` | Đầu các đoạn riêng, cùng cấp/cách ghi và mốc đầu rõ; chỉ đổi nhãn. XML/kiểu/thụt lề phải xác minh. Tab bố cục chỉ cho phép thay nhãn khi có bằng chứng hẹp, giữ tab. Bảng/danh sách tự động/cấp, bắt đầu lại hoặc dẫn chiếu chưa chắc giữ thủ công. Không đoán nội dung bị thiếu. |
| C21 | Dấu đầu dòng đứng một mình | Trong mục `1.`, chỉ có `- Nội dung` thì đề xuất bỏ `- `; một `+` dưới một dấu `-` xét tương tự | Có mục cha đã xác định trọn vẹn, đếm theo cha/cùng cấp. Giữ nếu có ≥2 dấu cùng cấp; không gộp dấu ở các cha khác nhau. Giữ chữ/thụt lề/tab trước dấu, chỉ bỏ dấu và separator ASCII; separator tab hoặc biên chưa rõ giữ thủ công. Lựa chọn trình bày của người dùng, cần duyệt. |

C09–C10 hiện dùng 13 tên loại: **BÁO CÁO, THÔNG BÁO, KẾ HOẠCH, TỜ TRÌNH, QUYẾT ĐỊNH, NGHỊ QUYẾT, HƯỚNG DẪN, CHỈ THỊ, CHƯƠNG TRÌNH, PHƯƠNG ÁN, ĐỀ ÁN, QUY CHẾ, QUY ĐỊNH**. Chưa áp phần định dạng tên loại/trích yếu này cho Công văn/V/v hoặc mọi mẫu đặc thù. Không đồng nghĩa đã hỗ trợ mọi thể thức của 13 loại trên.

### Thứ tự phát triển và gợi ý ngôn ngữ minh họa

Ưu tiên thể thức, lỗi chữ/lặp trước; ngữ cảnh khó và thư viện thực hiện sau cùng theo thống nhất của người dùng. C13 là đối chiếu chữ lặp, không phải suy đoán ý nghĩa câu.

`con đo` → gợi ý `con đò` là quy tắc minh họa, có thể cảnh báo nhầm và phải đọc ngữ cảnh trước khi chọn. **Chưa có bộ từ điển chính tả tiếng Việt đầy đủ**, kiểm lỗi dấu/âm đầu/vần phổ quát, ngữ pháp hoặc viết lại câu. Không dùng số lượng cảnh báo còn lại để đánh giá chất lượng chính tả toàn văn bản.

## 2. Những mục hiện chỉ cảnh báo hoặc chưa tự sửa

| Mục | Hành vi hiện tại |
| --- | --- |
| Khổ giấy, lề; trang ngang/lề đối xứng và ngoại lệ mẫu | Đưa cảnh báo để bạn kiểm tra; chưa tự đổi thiết lập trang. Chế độ cá nhân không áp kiểm giấy/lề Nhà nước/Đảng. |
| Số và ngày chưa cùng hàng, dòng chưa nằm dưới khối tham chiếu | Cảnh báo vị trí khi đo được; chưa tự dịch theo chiều dọc. |
| Tên loại chưa viết hoa, trích yếu toàn chữ hoa | Cảnh báo để duyệt; phần phông có thể có gợi ý riêng nếu đủ điều kiện, nhưng không tự đổi nội dung/chữ hoa. |
| Số văn bản bằng 0, ngày sai lịch, năm chưa đủ rõ, mẫu số/ngày chưa phân tích được hoặc nhiều ứng viên | Giữ thủ công, không đoán giá trị thay thế. Một lỗi an toàn khác trong dòng vẫn có thể có gợi ý riêng; cần xem từng mục. |
| Mã cơ quan hoặc tên địa danh cần xác minh | Không xác nhận mã/tên chính thức. Chữ thường trong mã/đầu địa danh có thể được cảnh báo, chưa tự đổi tên/mã. |
| Quốc hiệu và tiêu ngữ có cỡ chưa thống nhất theo cặp minh họa | Cảnh báo để bạn chọn; không tự ép mọi trường hợp về một cặp cỡ mới. |
| Tab liên tiếp, khoảng trắng/tab đầu đoạn căn giữa trong chế độ cá nhân | Cảnh báo thủ công; không tự xóa tab hoặc thụt đầu dòng. |
| Trộn phông/kiểu ở vùng chưa đủ chứng cứ, field/control/liên kết/đối tượng nhúng trong vùng đã đối chiếu | Có thể giữ thủ công. Riêng trộn kiểu không mặc nhiên cấm mọi sửa chữ thuần đã chứng minh; xem khả năng chọn của từng gợi ý. |
| Nét có sẵn chưa ghép được, Line không phải của ứng dụng, đường chồng hoặc chưa đo được XPS | Không tự tạo thêm hoặc báo đạt chỉ từ số Shape; cần kiểm tra trong Word. |
| Tên cơ quan, thân bài, chữ ký, nơi nhận, số trang, mọi mẫu riêng | Chưa kiểm/sửa đầy đủ. C12 dưới tên cơ quan không phải chức năng sửa phông/nội dung tên cơ quan. |

Từ .47, đối chiếu XML vùng sửa chữ áp dụng cho **toàn phần thân ở cả ba chế độ**, kể cả sau đoạn 40. Vùng có cấu trúc đã nhận ra giữ thủ công; field phức tạp hoặc lệch XML có thể chặn cả các đoạn phía sau. Đây không phải chứng minh mọi field/control đều sửa được. Phông/toạ độ đặc thù đầu Nhà nước vẫn có phạm vi riêng 40 đoạn; header/footer/textbox, bảng gộp/lồng và nhận diện phức tạp còn giới hạn. Tài liệu bật Track Changes hoặc được bảo vệ chưa được hỗ trợ mở để sửa.

## 3. Các ví dụ phải giữ nguyên

- `Số:        /BC-ABC`: không thu hẹp phần để điền số theo mẫu nhận diện.
- `Địa danh, ngày        tháng 10 năm 2026`: không thu hẹp phần để điền ngày.
- `Địa danh, ngày 31 tháng 02 năm 2026`: báo giá trị sai, không tự đổi sang ngày khác.
- Tên riêng, mã được cấp, chữ ngoài phạm vi đã chọn: không tự chọn nội dung mới.
- Khoảng trắng đầu đoạn hoặc tab dùng bố cục: C01 không phải lệnh xóa thụt lề.

Ngoại lệ ô trống là mẫu hẹp; không bảo đảm miễn mọi chuỗi khoảng trắng trong mọi mẫu tự tạo. Phần khác có nhiều dấu cách vẫn có thể được cảnh báo.

## 4. Danh sách dùng khi kiểm tra tổng thể

Với mỗi nhóm C01–C21 đang có gợi ý sửa, ghi kết quả của từng thao tác sau:

| Việc kiểm | Kết quả cần thấy |
| --- | --- |
| Sửa riêng lỗi đang xem | Đúng chữ/kiểu đã chọn thay đổi; kiểm chữ/kiểu không chọn giữ nguyên. Đổi phông/cỡ có thể làm dàn trang thay đổi, cần quét đo lại. |
| Sửa các lỗi đã tích trong cùng nhóm | Sửa đủ một lượt; không cần bấm lần hai để hết cùng lỗi đã sửa. |
| Sửa tất cả | Từ .53, một lượt dùng các gợi ý sửa được đang có: chữ/phông → đo lại → căn giữa → đo lại → Line. Một Undo bỏ cả lượt, lỗi bước sau phục hồi tất cả. Mục thủ công/mới xuất hiện không tự được thêm. |
| Quét lại | Bỏ mục đã thực sự hết; giữ cảnh báo thủ công hoặc lỗi khác còn thật. Không tự coi mọi mục còn lại là lỗi danh sách. |
| Hoàn tác một lần | Phục hồi cả lượt sửa thành công; nếu có hai lượt, Hoàn tác tiếp về đúng lượt trước. |
| Lưu bản sao rồi mở lại | Chữ, phông, vị trí/nét đã sửa giữ đúng; tệp gốc không đổi. |
| Gặp lỗi giữa sửa/lưu/hậu kiểm | Không báo thành công một phần; phục hồi cả lượt nếu có thể, giữ lỗi/hiện vật nếu phục hồi không đạt. Lỗi dọn backup riêng xem ghi chú .39. |

Nếu chọn lẫn chữ/phông, căn giữa và Line, chương trình yêu cầu tách nhóm trước ghi. Đổi phông/cỡ có thể thay số đo đường kẻ, nên phải dùng kết quả quét mới. Một cảnh báo Line bị chặn do còn gợi ý chữ/phông trong vùng có thể trở thành sửa được sau khi giải quyết phần chữ/phông.

Mẫu ghi lỗi ngắn để gửi lại:

```text
Phiên bản:
Chế độ: Nhà nước / Đảng / cá nhân
Nhóm hoặc tên cảnh báo:
Thao tác: sửa riêng / tích nhóm / sửa tất cả / quét / Undo / lưu-mở
Kết quả mong đợi:
Kết quả thực tế:
Có trở về đúng sau Undo không:
```

## 5. Bằng chứng và tài liệu liên quan

C13 được bổ sung ở [0.1.40](CHANGES-0.1.40.md), C14 ở [0.1.42](CHANGES-0.1.42.md); kết quả Core/CI không thay bài Word thực tế. Chưa có xác nhận toàn bộ bài Office 2021 cho .43.

- Bản thử [0.1.39](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.39): [Windows CI](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/39873266352) đạt 561 Core/OpenXML + 45 WPF; CI không có Word. Chưa có xác nhận bài Word đúng phiên bản cho toàn bộ chức năng .39.
- Mốc 0.1.27 có xác nhận người dùng sửa cả ba Line trên Office 2021; không mở rộng xác nhận đó sang mọi chức năng .28–.39.
- [Kịch bản Windows/Office](WINDOWS-VALIDATION.md), [căn cứ và phạm vi thể thức](FORMAT-RULES.md), [ghi chú .39](CHANGES-0.1.39.md), [hướng dẫn phát triển](DEVELOPMENT.md).
- Đã đối chiếu các nhóm với `Checker`, `HeadingRules`, `AdministrativeHeaderRules`, `DocumentHeadingRules`, `AdministrativePositionRules`/`AdministrativeCentering`, `ShapeLineRules`, `LineInspection`, `PageRules` và luồng nút sửa của `MainWindow` tại .39; C13 đối chiếu thêm `RepeatedTextRules` của .40. Ví dụ chữ/số/ngày trong danh sách đã được chạy qua Core của .39; không thay thế việc chạy Word/XPS thật.

## Bổ sung cách thử C14

Sửa riêng một dấu câu phải giữ lỗi không chọn; sửa chung dấu câu, khoảng trắng và cụm lặp phải hết đúng các mục sau quét, một Undo phục hồi cả nhóm. Lưu bản sao/mở lại, kiểm chữ và kiểu quanh dấu câu. Các mẫu số `1,25`, mã `BC,AB`, email, URL/đường dẫn, tab/NBSP và dòng số/ngày không nhận gợi ý C14. Gợi ý khoảng trắng cũ trùng span C14 phải được gộp để không ghi chồng.

Báo cáo trong Trợ giúp hiện liệt kê C01–C21, M01–M08, D01; xem [hướng dẫn xuất TXT](REPORT-EXPORT.md). C14 chưa phải bộ kiểm dấu câu hoàn chỉnh. Cần ghi riêng phiên bản và kết quả Word thực tế; chưa có xác nhận toàn bộ bài Office 2021 cho .42.

## Bổ sung cách thử C15

Dùng `( nội dung )`: chọn từng mép hoặc cả hai; chữ/kiểu và dấu ngoặc phải giữ nguyên. Chọn cùng khoảng trắng ngoài, cụm lặp và phẩy/chấm phẩy; một lượt hết đúng mục sau quét, một Undo phục hồi cả nhóm, lưu/mở giữ chữ/kiểu và nguồn. Sửa riêng ngoặc trong `( công,tác )` thì mục dấu phẩy chưa chọn vẫn còn sau quét. `(    )` phải giữ nguyên cả khi sửa tất cả lỗi khoảng trắng cũ; các lỗi độc lập khác vẫn sửa được. Không nhận C15 với ngoặc thiếu/lồng, phần số/toàn hoa, tab/NBSP, công thức có dấu kỹ thuật hoặc dòng số/ngày. Phạm vi bảo vệ XML vẫn theo giới hạn hiện có; chưa có xác nhận Word/Office 2021 cho .43.

## Bổ sung cách thử C16

Thử `Đã hoàn thành .`, `Nội dung đã duyệt ?`, `Xin cảm ơn !`: sửa riêng giữ các lỗi không chọn; sửa chung ngoặc/phẩy/cụm lặp/khoảng trắng phải hết đúng mục một lần, một Undo trả cả nhóm, lưu/mở giữ chữ/kiểu/nguồn. Kiểm số, viết tắt hoa/chấm liền, URL/email/đường dẫn, La Mã, dấu lửng/chuỗi dấu, tab/NBSP, dòng số/ngày và dấu giữa câu không nhận C16. Khoảng sau dấu và dấu đoạn/ô phải nguyên vẹn. C16 chưa phải bộ kiểm dấu câu đầy đủ; chưa có xác nhận Word/Office 2021 cho .44.

## Bổ sung cách thử C17 và báo cáo có ví dụ

Dùng `Ghi chú  :  nội dung`: chọn riêng C17 chỉ bỏ khoảng trước dấu, gợi ý khoảng sau vẫn còn; chọn cả hai phải ra `Ghi chú: nội dung` một lần. Sửa chung ngoặc/cụm lặp/phẩy/chấm phẩy/dấu kết câu/phông đủ điều kiện, quét hết đúng mục, một Undo trả cả nhóm, lưu/mở giữ chữ/kiểu và nguồn. Mẫu giờ `12 : 30`, tỉ lệ `1 : 2`, giao thức `http : host`, địa chỉ/đường dẫn/tab/NBSP/dòng số/ngày không nhận C17; nhãn ngắn/thiếu khoảng sau còn giới hạn. Đọc [nội dung .46](CHANGES-0.1.46.md).

Báo cáo Trợ giúp/TXT hiện có ví dụ và kết quả/hướng xử lý cho C01–C21/M01–M08/D01; P01–P05 riêng là dự kiến chưa triển khai. Đây là tiến độ chương trình, không phải lịch sử sửa tài liệu. Xem [cách xuất báo cáo](REPORT-EXPORT.md). Chưa có xác nhận toàn bộ bài Word/Office 2021 cho .46.

## Bổ sung cách thử .47 — toàn thân và ba chế độ

Sau hơn 40 đoạn, đặt một dòng có lỗi trong hyperlink/control và một đoạn chữ thuần bên cạnh. Ở Nhà nước, Đảng, cá nhân: vùng có cấu trúc giữ thủ công, vùng thuần đã khớp vẫn sửa được; lựa chọn cũ không được vượt qua gate mới. Field phức tạp/lệch XML phải chặn phần chưa chứng minh và báo phạm vi chưa đối chiếu. Sửa nhóm/rollback/Undo/lưu-mở giữ chữ/kiểu và quan hệ hyperlink. Các phép đo Line/căn giữa không mở rộng sang các đoạn chỉ được đối chiếu chữ. Đọc [nội dung .47](CHANGES-0.1.47.md); chưa có xác nhận runtime Office 2021.

## Bổ sung cách thử C18

Dùng `Đã hoàn thành  .  Tiếp tục báo cáo`: sửa riêng C18 chỉ bỏ khoảng trước dấu, giữ khoảng sau chưa chọn; sửa chung C01 ra một khoảng sau. Thử cả `?`, `!`, nhiều câu cùng đoạn và ghép nhóm C13–C17; một lượt hết đúng các mục, một Undo trả cả nhóm, lỗi giữa lượt phục hồi cả lượt, lưu/mở giữ chữ/kiểu/nguồn. Kiểm số, viết tắt hoa hoặc phổ biến (`tp`, `dr`…), đuôi tệp phổ biến (`Docx`…), URL/email/đường dẫn/tab/NBSP, chuỗi dấu, dòng số/ngày, chữ sau dấu viết thường hoặc không rõ không nhận C18. Không có cảnh báo không chứng minh mọi dấu câu đúng. Báo cáo 18 nhóm có thể đề xuất, 8 thủ công, 1 minh họa; P01–P05 vẫn là việc dự kiến riêng. Chưa có xác nhận runtime Office 2021 cho .48.

## Bổ sung cách thử C19 và hai phía dấu câu

Dùng `Đã hoàn thành  .Tiếp tục báo cáo`: chọn C19 giữ nguyên hai space trước dấu; C18 chưa chọn phải còn. Chọn C18 giữ thiếu khoảng sau, C19 vẫn còn. Chọn cả hai ra `Đã hoàn thành. Tiếp tục báo cáo` một lượt. Thử .?!, nhiều câu và nhóm C01/C13–C19; quét hết đúng mục, một Undo trả cả lượt, lỗi giữa chèn/xóa phục hồi toàn bộ, lưu/mở giữ chữ/kiểu/hash nguồn. Kiểm kiểu từ ngay hai phía dấu câu, không chỉ từ ở xa. Không nhận C19 cho số, viết tắt/đuôi tệp phổ biến, URL/email/control/NBSP/dòng số-ngày hoặc chữ sau viết thường/toàn hoa/chưa rõ. Báo cáo hiện 19 đề xuất, 8 thủ công, 1 minh họa; ví dụ P03 chuyển sang `Đã hoàn thành.tiếp tục báo cáo`, vẫn chưa sửa. Chưa có xác nhận runtime Office 2021 cho .49.

## Danh sách lỗi gọn từ .50

Mỗi lỗi một dòng, checkbox và số trang, tên ngắn; mô tả/gợi ý/căn cứ trong tooltip hoặc mở **Chi tiết lỗi** sau khi chọn. Ba phần: **Bắt buộc sửa** (thể thức có căn cứ và được phép đề xuất), **Đề xuất sửa — cần duyệt**, **Chưa tự sửa — chỉ cảnh báo**. Checkbox ở từng nhóm/phần/tất cả dùng chung trạng thái; cảnh báo thủ công luôn bị loại khỏi lựa chọn sửa, kể cả nhóm đang thu gọn. Vị trí đánh dấu và ô tích độc lập.

Phân loại giao diện không mở quyền sửa hoặc kết luận pháp lý cho mọi trường hợp. Chỉ xếp các loại thể thức được hỗ trợ có căn cứ, với phân loại thận trọng khi có lựa chọn căn trong ô; đặt lại Line theo dung sai/căn giữa vẫn cần duyệt. Lỗi thể thức bị chặn/không đủ bằng chứng luôn ở phần thủ công. Không bắt buộc phải sửa trước khi lưu. Các gợi ý cùng loại nằm cạnh nhau, trong nhóm sắp theo vị trí văn bản; lượt ghi vẫn do giao dịch cũ sắp lại theo CP. Từ .53, sửa lỗi đã tích có thể qua nhiều giai đoạn tự động; chỉ dùng các mục đã chọn và số đo mới, giữ một Undo/rollback cho cả lượt. Xem [hướng dẫn danh sách](ISSUE-LIST.md).

Tên **Thiếu đường kẻ dưới cơ quan ban hành/tiêu ngữ/trích yếu** chỉ được tạo từ nhánh thiếu đã xác định; Line có sẵn sai số đo hoặc chưa thấy nét dùng tên khác. Không đổi kiểm nét XPS/rollback hoặc xóa cảnh báo dựa vào tên/nhóm. Đây là cập nhật giao diện, danh mục vẫn C01–C21/M01–M08/D01. Chưa có xác nhận toàn bộ bài Word trên Office 2021 cho .50.

## Ngữ cảnh cấu trúc từ .51

C20/C21 thuộc **Đề xuất sửa — cần duyệt**; các giới hạn ở [hướng dẫn cấu trúc](OUTLINE-CONTEXT.md). Chọn riêng một nhãn/dấu không đổi mục chưa chọn. Có thể sửa chung lỗi chữ/phông đủ điều kiện; một Undo phục hồi cả lượt, lỗi giữa lượt rollback, quét lại dùng ngữ cảnh mới. Thêm dấu cùng cấp hoặc đổi mục cha phải làm lựa chọn đơn cũ hết hiệu lực trước ghi. Tự động Word/field/control/liên kết/bookmark/kiểu không rõ không được biến thành mục gõ tay. Phần bảo vệ này không thay quyền sửa chữ chung .47.

Báo cáo hiện21 nhóm đề xuất,8 thủ công,1 minh họa; ví dụ C20/C21 gồm nhiều dòng tương ứng các đoạn riêng. Chưa có kết quả Word/Office2021 cho .51. Giao diện một dòng của .50 được giữ; ảnh mẫu .51 có thêm hai nhóm mới và nền sáng kín.

Bản .52 chỉ đổi tooltip: ba phần ngắn và căn cứ có nội dung; không thêm loại lỗi. Xem [cách dùng](ISSUE-LIST.md).

Bản .53 không thêm loại lỗi; thêm lượt sửa nhiều nhóm. Xem [phạm vi/cách thử](MULTI-GROUP-REPAIR.md).


## Bổ sung .54

| Mã | Phạm vi mới | Sửa có duyệt |
| --- | --- | --- |
| C22 | Cơ quan chủ quản/ban hành đã nhận diện chắc trong ô trái đầu văn bản | TNR đen, in hoa, cỡ 12–13, đứng; cơ quan ban hành đậm. Không đoán tên chính thức/cấp chưa rõ. |
| C23 | Nhãn Nơi nhận và danh sách cùng vùng XML, có Lưu cuối | Nhãn 12 nghiêng đậm; danh sách 11 đứng; sửa dash và dấu cuối dòng. Không áp quy tắc dấu đơn thân bài; chưa đo đủ vị trí lề/hàng. |

Trích yếu có thể nhiều đoạn hoặc textWrapping break; giữ nguyên ngắt, không coi Kính gửi căn giữa là trích yếu. Phạm vi XML chưa đối chiếu hiển thị riêng, không quy lỗi cho dòng đầu. Tổng hiện tại 23 đề xuất, 8 thủ công, 1 minh họa. Xem [ghi chú .54](CHANGES-0.1.55.md).

Từ .55, hậu kiểm phông recipient ở sau đoạn 40 trong lượt cùng Line dùng đúng proof toàn thân; hình học không mở rộng. Xem [nội dung .55](CHANGES-0.1.55.md).
