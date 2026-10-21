# Chốt mốc phát triển 0.1.27 — 21/10/2026

## Mã nguồn và gói đã được thử

- Dự án: `trung12685/KT_ChinhTaTV`.
- Xuất phát từ `main` 0.1.18, commit `a2fa5a90b4de1bb24edfbaaccac20f13ec044fe9`.
- Mã ứng dụng 0.1.27 đã kiểm thử và đóng gói: `1ec3092ccd68ed0acd8878b2737664116a6b11fe`.
- Tag bất biến: `v0.1.27`. Các commit chốt tài liệu sau đó không thay mã ứng dụng hoặc gói tải này.
- [Release 0.1.27](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.27) · [ZIP Windows x64](https://github.com/trung12685/KT_ChinhTaTV/releases/download/v0.1.27/ChinhTaViet-0.1.27-win-x64.zip).
- ZIP: 72.274.460 byte. SHA-256: `6b9607c6aecb6497ff2c294b5019e6fa2162f428da350a41b02e759562403b09`.
- [CI của đúng mã đóng gói](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/39570349615).

Người dùng đã xác nhận sau 0.1.27 rằng thao tác chọn sửa cả ba đường trên Office 2021 đã chạy được, và yêu cầu chốt bản này. Mốc ổn định ở đây là phần sửa ba đường đã được xác nhận, không phải công bố ứng dụng hoàn thiện mọi quy định/chính tả hoặc tương thích mọi cấu hình Office.

## Các nội dung đã xử lý

| Nhóm | Kết quả hiện tại |
|---|---|
| COM Shape bị lỗi trên Office 2021 | Bỏ đường đọc AlternativeText/ID/Type từ Shape COM; đọc marker, ID gói và loại Line từ DOCX. Không dùng số Shape để kết luận thành công. |
| Ghi Line | Ghi DrawingML vào candidate từ checkpoint riêng, không mở ZIP trên working DOCX đang bị Word giữ handle ghi. Đối chiếu Word/XML theo range thật; giữ chữ, định dạng và các part ngoài phần chỉnh. |
| Nét lệch khi neo trong bảng | Bù tọa độ theo nét XPS ghép chắc chắn; Motto mới bị ẩn chỉ có nhánh bù hẹp dựa trên khung ô đo được. Kiểm lại nét sau bù; giữ phần bù khi đặt lại Line đã có. |
| Sửa cả ba hoặc nhiều đường | Các bước được gom trong một giao dịch, thứ tự Agency → Subject → Motto. Lưu, lấy lại cấu trúc, quét lại và kiểm cuối. Có tối đa một lượt đặt lại các cảnh báo đủ điều kiện của đúng nhóm đã chọn. Chưa đạt thì rollback cả lượt; một lần Hoàn tác cho cả lượt. |
| Đánh giá nét thật | Đánh giá độ dài/tâm/khoảng cách trên nét XPS thực đo, thay vì chỉ kiểm tọa độ đề xuất. Giữ mọi nét khác khi thay Line đã nhận diện; trường hợp ghép mơ hồ phải dừng. |
| Đường sửa rồi còn trong danh sách | Quét lại từ trạng thái mới; lịch sử sửa chỉ hỗ trợ ghép mốc và luôn phải kiểm lại nét cùng tham chiếu hiện tại. Không ẩn lỗi chỉ vì đã bấm sửa. |
| Độ dài đường 1 và 3 | Khoảng hợp lệ 1/3–1/2 dòng tham chiếu; tự tạo/đặt lại nhắm 3/7. Tiêu ngữ bằng dòng tiêu ngữ. Không ép đường hợp lệ về đúng 3/7. |
| Đo dòng chữ | Dùng AdvanceWidths/ClusterMap để lấy đủ glyph cuối, không lấy điểm dừng con trỏ làm biên chữ. Không đoán biên nằm giữa shaping cluster hoặc glyph không hỗ trợ. |
| Word CP và chỉ số chuỗi | Đọc tiền tố Range.Text thật để ánh xạ CP sang chuỗi xem trước; kiểm tiền tố và đoạn chữ khớp chính xác. Không coi phép trừ CP là chỉ số chuỗi. |
| Mốc thay đổi sau lưu/mở/rollback | Lấy bố cục Word/XML mới và ghép lại bằng vai trò, nguyên văn duy nhất, trạng thái đủ điều kiện, toàn bộ story không đổi. Candidate hiệu chỉnh dùng CP của checkpoint đang khôi phục; phần bù và đích hình học được giữ riêng. |
| Trường số/ngày để trống | Không báo khoảng trắng có chủ ý sau `Số:` trước phần ký hiệu hoặc giữa `ngày` và `tháng` của mẫu ngày chưa điền. Chỗ khác, số/ngày đã điền hoặc khoảng trắng ở trường khác vẫn được kiểm. |
| Chẩn đoán | Có phiên bản, mã thao tác, role, số mốc khớp, CP trước/sau, độ dài/trạng thái story và số đo nét/tham chiếu. Không ghi nội dung chữ hoặc đưa tài liệu, log, XPS và font riêng lên kho. |

Các nhánh bù và lượt đặt lại đều có giới hạn, không lặp cho đến khi vô tình đạt. Marker/metadata chỉ xác định danh tính và đích, không chứng minh nét thật đã đúng.

## Bằng chứng và phần còn phải kiểm

| Hạng mục | Trạng thái được ghi nhận |
|---|---|
| 223 kiểm thử Core/OpenXML | Đạt trong CI Windows của commit đóng gói. |
| 20 kiểm thử WPF | Đạt trong cùng CI: đo glyph, frame, caret, Word CP/chỉ số chuỗi, scanner và nét thực tế. |
| Publish Windows x64 self-contained | Đạt; ZIP và SHA-256 đã có trên GitHub. |
| Chọn sửa ba đường cùng lúc trên Office 2021 | Người dùng xác nhận đã chạy được trên tài liệu/máy đã thử với 0.1.27. |
| Lưu/mở lại và một lần Hoàn tác trên máy người dùng với 0.1.27 | Chưa có xác nhận riêng trong hội thoại. Có logic và kịch bản kiểm thử; không đánh dấu là đã thử đạt thực tế. |
| Toàn bộ menu kiểm tra Word tự động với 0.1.27 | Chưa có báo cáo PASS riêng của phiên bản này; CI không có Word desktop. |
| Tương thích mọi tài liệu/Office hoặc đạt toàn bộ NĐ30/HD05 | Chưa xác nhận; phạm vi kiểm còn giới hạn. |

## Lịch sử cần nhớ khi sửa tiếp

- 0.1.18 chưa có bằng chứng đạt Office 2021; không dùng lại như mốc đã ổn định.
- 0.1.19–0.1.20 thay đọc Shape COM bằng DOCX; một bài Office check mẫu ở 0.1.20 từng đạt, không chứng minh tài liệu riêng đã đúng vị trí.
- 0.1.21–0.1.22 bổ sung bù nét theo XPS và trường hợp Motto ở ô phải.
- 0.1.23 xử lý giao dịch nhóm, tỷ lệ 3/7, quét lại và ngoại lệ khoảng trắng trường trống; vẫn có phản hồi lỗi nhóm.
- 0.1.24 sửa phép đo glyph/phần bù nhưng người dùng vẫn phải sửa lần hai ở đường 2/3.
- 0.1.25 phát hiện được lỗi cuối lượt và rollback; log dừng ở guard ghép mốc sau lưu tại ReadRemainingLines dòng 402. Log đó chưa phân biệt trường nào khác, không được kết luận riêng CP đã được chứng minh là nguyên nhân duy nhất.
- 0.1.26–0.1.27 hoàn thiện ánh xạ CP/chuỗi và ghép mốc theo bản đang mở. 0.1.27 là bản người dùng xác nhận sửa nhóm chạy được.

Ghi chép chi tiết được giữ ở [HISTORY-THROUGH-0.1.27.md](HISTORY-THROUGH-0.1.27.md) và các CHANGES/OFFICE2021 theo phiên bản; nhận xét cũ thuộc thời điểm từng bản. Trạng thái hiện tại ưu tiên biên bản này.

## Điểm bắt đầu cho lần phát triển sau

Đọc [DEVELOPMENT.md](DEVELOPMENT.md). Giữ 0.1.27 làm mốc quay lại; thay đổi mới dùng phiên bản mới. Ưu tiên tiếp theo đã đề xuất là số/ký hiệu văn bản và địa danh/ngày tháng theo NĐ30, nhưng chưa triển khai và cần thống nhất phạm vi trước khi thêm quy tắc tự sửa.
