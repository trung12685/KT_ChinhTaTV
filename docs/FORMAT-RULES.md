# Quy tắc thể thức — bản 0.1.15

## Nguồn đã đối chiếu

Dùng đúng hai PDF do người dùng cung cấp. Đây là kiểm tra theo các phiên bản đính kèm, chưa phải xác nhận độc lập rằng không có văn bản sửa đổi/thay thế khác. Không kiểm định chữ ký số PDF. Với NĐ30 dạng scan, OCR chỉ hỗ trợ tra cứu; các giá trị được đưa vào mã đã đối chiếu ảnh trang PDF. Số trang dưới đây là số trang vật lý trong PDF.

| Nguồn | Dung lượng trang | SHA256 |
|---|---:|---|
| `30.signed.pdf` — Nghị định 30/2020/NĐ-CP ngày 05/3/2020 | 76 | `be2514460c953141923579e4a0ddd0249c611643a59514976790dc1d90589178` |
| `HD_05_VPTW_e1550.pdf` — Hướng dẫn 05-HD/VPTW ngày 27/5/2026 | 36 | `53bb4bc543cd1f19e0f60c861364ceab9c425bc54325c04be6a3ac99108650c5` |

HD05 Phần III (PDF trang 29) nêu thay thế Hướng dẫn 36-HD/VPTW ngày 04/3/2018. Bộ Đảng này không lấy thông số của HD36 thay cho HD05.

Các bảng ban đầu dưới đây mô tả phạm vi lúc lập bộ quy tắc. Thay đổi Line và số/ngày được ghi tại các mục bổ sung theo phiên bản ở cuối tài liệu.

## Những quy tắc đã triển khai

| Thành phần | Nhà nước — NĐ30 | Đảng — HD05 | Phạm vi ứng dụng |
|---|---|---|---|
| Khổ giấy | A4 210 × 297 mm; PL I, Phần I.I.1, PDF 21 | A4; giấy tờ hành chính có thể A4/A5 hoặc mẫu in sẵn; Phần II.IV.3, PDF 28 | Đọc từng section; ngoài A4 chỉ cảnh báo, không kết luận mọi trường hợp đều sai |
| Lề trang dọc | Trên/dưới 20–25, trái 30–35, phải 15–20 mm; PL I, Phần I.I.3, PDF 21 | Trên/dưới 20, trái 30, phải 15 mm; Phần II.IV.4, PDF 28 | Chỉ cảnh báo, dung sai đọc Word 0,25 mm; không tự sửa |
| Hai mặt/trang ngang | Trang ngang có điều kiện cho bảng/biểu, PL I, Phần I.I.2 | Mặt sau hai mặt trái 15/phải 30 mm; có ngoại lệ bảng/biểu trang ngang, II.IV.4.2–4.3 | Không tự đổi hướng trang; lề đối xứng Nhà nước yêu cầu xem thủ công, Đảng kiểm tra lề trong/ngoài; section đảo lề riêng mặt sau được nhắc kiểm tra ngoại lệ |
| Quốc hiệu | In hoa, đứng, đậm, cỡ 12–13; PL I, Phần I.II.1.a, PDF 21 | Không áp dụng quốc hiệu Nhà nước | Dòng khớp nội dung trên trang đầu; có thể sửa chữ/font/cỡ/đậm/nghiêng/màu |
| Tiêu ngữ | “Độc lập - Tự do - Hạnh phúc”, đứng, đậm, cỡ 13–14, căn giữa dưới quốc hiệu; PL I, Phần I.II.1.b, PDF 21 | Không dùng tiêu ngữ Nhà nước | Sửa định dạng dòng nhận diện được; chỉ kiểm tra căn giữa cục bộ khi ở trong ô bảng |
| Tiêu đề Đảng | Không áp quy tắc Đảng | “ĐẢNG CỘNG SẢN VIỆT NAM”, in hoa, đứng, đậm, cỡ 15; II.I.1 và PL2 mục 1, PDF 2, 31 | Dòng khớp nội dung trên trang đầu, sửa được; chưa kiểm tra tọa độ góc phải/đường kẻ |
| Phông/màu | Times New Roman, Unicode, màu đen; PL I, Phần I.I.4 | Times New Roman, Unicode, màu đen; II.IV.1 | Hiện chỉ kiểm font/màu các dòng tiêu đề nhận diện được; DOCX lưu Unicode không đồng nghĩa đã kiểm hết lỗi font/mã tiếng Việt |
| Thống nhất cỡ chữ | Chú thích 1, PL I, Phần I.V, PDF 32 minh họa quốc hiệu/tiêu ngữ 12/13 hoặc 13/14 | Bộ thông số riêng theo PL2 | Khi chỉ có một cặp trên trang đầu và cỡ không thống nhất, cảnh báo thủ công; không tự chọn cặp |

Căn giữa **quốc hiệu trong ô bảng** là lựa chọn trình bày theo yêu cầu người dùng từ bản thử, được ghi riêng trong căn cứ của cảnh báo; không quy nó thành câu chữ trực tiếp của NĐ30. Căn giữa toàn trang không tương đương với việc đặt thành phần ở vùng trên bên phải, nên không ép căn giữa mọi dòng tiêu đề ngoài bảng.

Quốc hiệu chấp nhận cả `HÒA` và `HOÀ` (mẫu bảng PDF trang 32 dùng HOÀ), không tự đổi vị trí dấu khi chỉ vì khác cách đặt dấu. Nhận diện có chuẩn hóa khoảng trắng/gạch nối để gom lỗi toàn thành phần, tránh chồng lấn với lỗi khoảng trắng nhỏ. Không tự nhận diện mọi câu sai chính tả trong tiêu đề và không suy ra một dòng vắng trong nội dung chính là thành phần bị thiếu (nó có thể nằm ở đầu trang/textbox chưa quét).

## Đã nghiên cứu nhưng chưa tự kiểm tra trong bản này

| Nhóm | Căn cứ và khác biệt đáng chú ý |
|---|---|
| Nội dung chính Nhà nước | NĐ30 PL I, Phần I.II.6.e, PDF 25: chữ thường, đứng, cỡ 13–14, căn đều; đầu dòng 1 cm hoặc 1,27 cm; khoảng đoạn tối thiểu 6pt; giãn dòng đơn đến 1,5 lines |
| Nội dung chính Đảng | HD05 II.I.6.2.2, PDF 12 và PL2, PDF 32: cỡ 14–15, căn đều; đầu dòng khoảng 10 mm; khoảng đoạn tối thiểu 6pt; giãn dòng Exactly 18–22pt |
| Tên cơ quan, số/ký hiệu, địa danh/ngày tháng, tên loại/trích yếu, nơi nhận, chức vụ/chữ ký | Phải phân loại đúng vai trò đoạn; không dùng một font/cỡ/kiểu chữ chung cho mọi thành phần. Ví dụ HD05 “Nơi nhận” cỡ 14 đứng/gạch chân, danh sách nơi nhận cỡ 12; khác NĐ30. Chưa tự kiểm các thành phần này |
| Đường kẻ và vị trí | Độ dài đường kẻ, vùng đặt tiêu đề/quốc hiệu, quan hệ căn giữa tương đối cần hình học trang và đối chiếu mẫu. Chưa tự kiểm |
| Số trang | NĐ30 PL I.I.7: cỡ 13–14, giữa lề trên, không hiện trang đầu; HD05 II.IV.5 và PL2 mục15: giữa phía trên, cách mép trên 10 mm, cỡ 14 theo bảng mẫu, không hiện trang đầu. Chưa quét header/footer và trường PAGE |
| Mẫu hành chính | NĐ30 PL III, PDF 40–57, gồm bảng viết tắt và mẫu nghị quyết cá biệt, quyết định trực tiếp/gián tiếp, văn bản có tên loại, công văn, công điện, giấy mời, giấy giới thiệu, biên bản, giấy nghỉ phép, phụ lục và bản sao. Chưa phải chức năng tạo mẫu hoặc kiểm mọi mẫu |
| Viết hoa | NĐ30 PL II, PDF 37–39: danh từ riêng/tên tổ chức/chức danh… cần phân tích ngữ cảnh. Chưa triển khai bộ kiểm viết hoa đầy đủ |
| Bản sao, dấu/chữ ký số và quy trình văn thư | Không thể kết luận đầy đủ chỉ từ file DOCX hoặc suy luận từ một vài đoạn văn. Ngoài phạm vi bộ kiểm trình bày ban đầu |

## Chế độ cá nhân

Không áp quốc hiệu/tiêu ngữ, khổ giấy hay lề hành chính. Giữ các quy tắc ngôn ngữ minh họa hiện có, cảnh báo khoảng trắng; thêm gợi ý thủ công cho tab liên tiếp và đoạn căn giữa có tab/khoảng trắng đầu dòng. Không tự xóa tab/căn giữa toàn văn bản vì có thể phá bố cục. Đây chưa phải bộ từ điển/AI chính tả hoàn chỉnh.

## Giới hạn và cơ chế sửa

- Không tuyên bố “đạt NĐ30/HD05” khi không có cảnh báo; chỉ báo kết quả các mục đã kiểm.
- Dòng nhận diện tiêu đề có thể nằm trong bảng. Chưa quét header/footer/textbox, chưa tự kiểm thiếu tiêu đề, phạm vi văn bản chuyên ngành/mẫu in sẵn hoặc cơ quan có hướng dẫn riêng.
- Sửa font/cỡ/chữ trên dòng nhận diện giữ nguyên cỡ đang nằm trong khoảng hợp lệ. Nếu cỡ sai, chọn cận dưới hợp lệ; các quan hệ với thành phần khác vẫn có thể cần cảnh báo thủ công.
- Lề, khổ giấy, ngoại lệ trang ngang/hai mặt, tính thống nhất cỡ và gợi ý tab là **chỉ cảnh báo**; ô tích bị vô hiệu và Sửa tất cả bỏ qua. Lượt sửa vẫn có xác nhận, kiểm tra chồng lấn, undo nhóm và checkpoint như bản trước.
- Chưa kiểm thử Word COM/WPF trên Windows với các quy tắc mới; cần chạy checklist Office 2021 trong WINDOWS-VALIDATION.md.

## Bản 0.1.7 — tương đương dấu trong tiêu ngữ

Theo yêu cầu người dùng, chấp nhận cả hyphen `-` và en dash `–` trong tiêu ngữ, kể cả hai dấu phân cách khác loại do Word AutoCorrect. Không coi riêng khác biệt này là lỗi. Khi sửa font/cỡ/kiểu chữ, giữ nguyên toàn bộ nội dung nếu không có lỗi nội dung khác; khi sửa hoa thường/khoảng trắng trong tiêu ngữ, giữ đúng từng dấu phân cách ban đầu. Đây là chính sách tương thích ứng dụng được người dùng xác định, không diễn giải thêm một quy định pháp lý mới.

## Bổ sung 0.1.8 — ba Shape Line

Đã bổ sung bộ kiểm/thêm/đặt lại ba Line ở mức thử nghiệm; mục “Đường kẻ và vị trí” nêu chưa kiểm bên trên mô tả phạm vi trước 0.1.8. Phạm vi/định nghĩa đo/ngoại lệ được ghi trong SHAPE-LINES.md. Từ bản 0.1.9 chỉ dùng khoảng NĐ30 1/3–1/2; đã bỏ tùy chọn 4/7 theo yêu cầu cập nhật. Chưa thể tuyên bố kiểm toàn bộ đường kẻ hay mọi vị trí.

Từ 0.1.15, thao tác ghi ba Shape Line dùng candidate DOCX và snapshot/rollback, không dùng COM Shape setters; xem SHAPE-LINES.md. Chỉ cập nhật Line có marker của ứng dụng, Line khác kiểm/chỉnh thủ công.


## Bổ sung 0.1.28: số/ký hiệu, địa danh/ngày tháng

Đối chiếu trực tiếp trang PDF 22–23 của nguồn NĐ30 đã ghi ở trên: Phụ lục I, Phần I.II.3.a–c, số nhỏ hơn 10 có 0 phía trước; nhãn Số có hai chấm; giữa số và ký hiệu dùng /, giữa nhóm viết tắt dùng -, không có khoảng trắng. Mã viết tắt thuộc quy định của cơ quan nên không tự đoán hoặc đổi mã. Phần I.II.4.b–c: địa danh có dấu phẩy, ghi đủ ngày/tháng/năm; ngày nhỏ hơn 10, tháng 1 và 2 có 0 phía trước. Không suy rộng thành bắt tháng 3–9 phải thêm 0. Địa danh chính thức và ngoại lệ công an/quân đội chưa được xác minh tự động.

Bản thử chỉ sửa cách viết trong vùng đầu đã nhận diện, cảnh báo thủ công phông/cỡ/kiểu chữ khi đo được; chưa tự căn dòng hoặc kiểm đủ vị trí. Trường bỏ trống phải giữ nguyên. Ngày không hợp lệ không được tự thay bằng ngày khác. Chi tiết phạm vi và kiểm thử: [CHANGES-0.1.28.md](CHANGES-0.1.28.md). Chưa đối chiếu độc lập mọi sửa đổi pháp lý sau bản nguồn.

## Bổ sung 0.1.31: sửa định dạng số/ngày

Đọc lại trực tiếp NĐ30 PL I Phần I.II.3.c và 4.c trên PDF 23: số/ký hiệu cỡ 13 đứng; địa danh/ngày tháng cỡ 13–14 nghiêng; phông/màu theo PL I.I.4. Bản thử cho sửa phông/cỡ/nghiêng/màu khi quan sát rõ trong dòng chữ thuần đã nhận diện. Giữ cỡ ngày hợp lệ, đậm/gạch chân/căn đoạn và nguyên văn. Đây chưa phải kiểm đủ tọa độ/căn giữa tương đối hoặc kiểu đậm/gạch chân của thành phần; các phần đó vẫn ngoài phạm vi tự sửa mới. Mẫu và ngoại lệ chưa rõ giữ thủ công. Xem [CHANGES-0.1.31.md](CHANGES-0.1.31.md).

## Bổ sung 0.1.32: cảnh báo vị trí số/ngày

NĐ30 PL I, Phần I.II.3.c và 4.c, PDF 23: số/ký hiệu căn giữa dưới tên cơ quan; địa danh/ngày tháng cùng dòng với số/ký hiệu, đặt dưới và căn giữa với quốc hiệu/tiêu ngữ. Bản thử chỉ đánh giá các quan hệ này trong vùng bảng đã nhận diện bằng khung glyph/baseline XPS và đưa cảnh báo thủ công. Ngưỡng phát hiện 3 pt và dung sai nhỏ 0,5 pt là chính sách đo ứng dụng, không phải số ghi trong điều khoản. Chưa kiểm đủ vị trí trên trang hoặc mọi mẫu, chưa tự dịch đoạn/căn giữa. Thiếu mốc hoặc chữ cắt/xoay/xuống dòng phải báo chưa đo được. Xem [CHANGES-0.1.32.md](CHANGES-0.1.32.md).


## Bản thử 0.1.33 — sửa căn giữa có điều kiện

Tiếp nối căn cứ vị trí số/ngày ở bản .32 (NĐ30 PL I Phần I.II.3.c, 4.c, PDF 23). Chỉ tự căn giữa khi có cả bằng chứng range/XML và glyph/khung ô XPS đồng nhất; bỏ thụt lề ngang của đúng dòng được chọn, không sửa chữ/ô trống, khoảng cách dọc hoặc tự làm hai dòng cùng hàng. Tham chiếu phía trên phải đã căn giữa không thụt lề trong ô lề cân bằng, và được đo thật cùng tâm với khung. Sau lưu phải đo đạt lại, nếu không phục hồi cả lượt. Dung sai tâm 3 pt và giữ hình học 0,5 pt là chính sách kiểm đo của ứng dụng, không phải số bắt buộc nêu trong NĐ30. Chưa có xác nhận runtime Office 2021 cho tính năng này.


## Bản thử 0.1.36 — tên loại/trích yếu của văn bản có tên loại

Đối chiếu lại nguồn NĐ30 đã ghi: PDF 21 (PL I Phần I.I.4) quy định Times New Roman, Unicode TCVN 6909:2001, màu đen; PDF 24 (I.II.5.b) quy định tên loại in hoa, cỡ 13–14, đứng, đậm; trích yếu ngay dưới, chữ in thường, cỡ 13–14, đứng, đậm và căn giữa. Công văn sau “V/v” có cách trình bày khác; chưa áp dụng phần mới cho công văn.

Tự sửa chỉ định dạng ký tự của dòng đã xác minh: giữ cỡ trong 13–14; ngoài khoảng đề xuất 13. Không tự viết hoa tên loại hoặc viết thường trích yếu để tránh đổi tên riêng/mã. Chỉ dùng khối ở đầu trang 1, sau cặp quốc hiệu/tiêu ngữ duy nhất và trước nội dung thường; XML/range sống phải khớp. Tối đa 4 đoạn trích yếu là phạm vi an toàn của bản thử, không phải giới hạn số đoạn theo NĐ30. Chưa kiểm toàn bộ vị trí tuyệt đối, mẫu đặc thù hoặc sửa đổi pháp lý sau nguồn đã ghi. Đường kẻ vẫn đo riêng trên XPS, không suy đạt từ định dạng chữ.
