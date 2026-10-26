# Đợt thể thức03 — số trang và PAGE

Ngày26/10/2026. Mã .70 đang tích lũy, sau C32 tại f2b1ff04. Chưa đóng gói Windows; mốc .67 và gói .69 giữ nguyên. Không tiếp tục tối ưu đọc. Thể thức trước; chữ cơ bản/khoảng trắng/dấu câu/lặp sau; ngữ cảnh và thư viện cuối cùng.

## Quy định và C33

NĐ30/2020/NĐ-CP, Phụ lụcI, PhầnI.I.4 và I.7, PDF21 do người dùng cung cấp: Times New Roman đen; sốẢRập từ1; cỡ13–14, đứng; căn giữa theo chiều ngang trong lề trên, không hiển thị số trang đầu.

| Tình huống trong phạm vi | Hành vi |
|---|---|
| Một trang, không header | Không báo thiếu số: trang đầu vốn phải ẩn |
| Từ hai trang, header trống | Đề xuất PAGE, ẩn trang1; trang2 hiện2, rồi3... |
| PAGE hiện trang đầu | Bật chế độ trang đầu riêng; chỉ bỏ tham chiếu header PAGE-only của trang1, giữ phần cũ |
| PAGE sai số bắt đầu hoặc La Mã | Chuẩn hoá sốẢRập từ1 |
| PAGE căn lệch; Arial/đỏ/nghiêng/cỡ10 hoặc16 | Căn giữa; Times New Roman đen, đứng; chỉ cỡ ngoài khoảng về13 |
| Cỡ13/13,5/14 đang đúng | Giữ cỡ đang đúng; đậm/gạch chân không bị xoá |
| Header riêng/logo/nhiều section/PAGE ở footer | Không ghi đè hay tự di chuyển; lỗi nhận diện chắc giữ thủ công, phần chưa nhận diện không tự coi là sai |

Ví dụ: tài liệu hai trang chưa số, footer có ghi chú. Sau duyệt: trang1 không số, trang2 hiện2 bằng PAGE trong lề trên; ghi chú footer vẫn hiện ở cả hai trang.

## Điều kiện và giữ dữ liệu

Một section dọc, lề trên20–25mm, không odd-even/gáy/đối xứng, cấu hình đọc chắc từ WordOpenXML hiện tại. Không dùng file đã lưu cũ làm fallback. Dùng Document.WordOpenXML cho đầy đủ story/quan hệ header-footer; Range.WordOpenXML chỉ chứa phần cần để biểu diễn range, không dùng để kết luận footer vắng mặt. Nguồn API: [Document.WordOpenXML](https://learn.microsoft.com/office/vba/api/word.document.wordopenxml), [Range.WordOpenXML](https://learn.microsoft.com/office/vba/api/word.range.wordopenxml). Header phải trống thông thường hoặc chỉ một PAGE đơn/phức, font/cỡ/kiểu giải được từ thuộc tính/kiểu kế thừa chắc; không field khoá hay nội dung lạ. Header có literal số hoặc tên cơ quan không bị gọi là thiếu PAGE chỉ vì chương trình chưa nhận ra kiểu trình bày đó. Một số bố trí/kiểu phức tạp còn ngoài phạm vi, không coi đã kiểm toàn bộ thể thức.

Bật “khác trang đầu” ảnh hưởng cả footer. Nếu footer trang1 trước đây dùng footer thường, chương trình thêm first-reference tới chính footer đó để giữ hiển thị. Nếu đã có footer riêng tiềm ẩn khác nội dung, không tự bật rồi thay nội dung đang xem. Không sửa/xoá part footer. Header trang đầu có nội dung riêng chưa đủ điều kiện tự thay.

Candidate mới chỉ sửa header PAGE và cờ section/quan hệ/ContentTypes cần thiết. Khoảng header mục tiêu8mm là cấu hình ứng dụng để dành chỗ trong lề trên, không gán8mm thành quy định NĐ30. Chỉ PAGE được dựng lại, dùng cached số để lưu gói nhưng cached số không phải bằng chứng đã render đúng. Giữ main story/chữ/bảng/ảnh/kiểu, footer và mọi phần ZIP không đổi; kiểm XML và từng byte phần khác. Không đọc/diễn giải dữ liệu phụ không liên quan.

## Một lượt và hậu kiểm

- Cho chọn chung số trang với nhóm đang có. Các nhóm chữ/thể thức/Line chọn kèm chạy trước bằng luồng giao dịch cũ; sau đó đối chiếu lại fingerprint cấu hình PAGE và số trang hiện tại. Không áp một header đã thay đổi ngoài phần được duyệt.
- Nếu các sửa đã chọn làm tài liệu còn một trang, chỉ công nhận đã đạt khi cùng cấu hình nguồn và đúng phạm vi; không tự thêm số khi không cần.
- Hậu kiểm field/header/section/footer sau mở và sau lưu; dựng XPS đầy đủ. Trang1 phải ẩn; mỗi trang sau phải hiện đúng số thứ tự, phông/màu/cỡ/đứng và căn giữa. Dùng kích thước chữ sau toàn bộ transform của XPS, kể cả canvas theo đơn vị point của Word.
- So nội dung và vị trí các lá visual ngoài lề trên trước-sau: chữ, hình/bảng/footer không được biến mất hoặc dịch chuyển. Không công nhận đạt chỉ từ số Shape, số part hoặc tồn tại codePAGE.
- Lỗi ở bất kỳ giai đoạn nào phục hồi cả lượt gồm các nhóm đã sửa trước, main story và cấu hình header/footer. Chỉ thành công toàn bộ mới tạo một Hoàn tác; giữ checkpoint nếu chưa xác minh phục hồi.

## Kiểm thử và phần chưa chứng minh

Core kiểm một/trên hai trang, PAGE đúng không cảnh báo, cỡ hợp lệ/ngoài khoảng, font/kiểu kế thừa, namespace và start01=1, quan hệ Internal, header tuỳ biến/field khoá/ngoại lệ, candidate/source, footer trang1, parts/tamper/fingerprint và chặn writer sai story. WPF kiểm schema của PAGE/header/section, chọn nhiều nhóm/tooltip, số thực/ẩn trang đầu, sai thứ tự, text dịch chuyển/hình mất và canvas scale. Có mẫu Word hai trang với footer ghi chú, sửa riêng/chung khoảng trắng/ba Line, fault sau nhóm/sau PAGE, Undo/lưu-mở/hash nguồn.

CI chỉ biên dịch bài Word; chưa có bằng chứng Office2021 chạy đạt phần mới. Kết quả cụ thể theo đúng commit trong PR/CI, không suy từ số phiên bản. Danh mục/TXT nguồn33 nhóm đề xuất/8 thủ công/1 minh hoạ, có ví dụ C33 và giới hạn; chưa phát hành .70 cho riêng bước này.

## Tiếp theo

Rà đồng bộ thể thức đầu–thân–cuối và các tổ hợp sửa có số trang, bảng, ảnh, nhiều nhóm. Tiếp tục mở phạm vi header/footer có nội dung riêng hoặc nhiều section chỉ sau khi có bằng chứng và cơ chế bảo toàn liên kết; chưa tự sửa các bố trí đó. Khi đủ các bước tích luỹ đã thống nhất mới đóng gói. Chữ cơ bản/dấu câu/lặp và ngữ cảnh/thư viện vẫn theo thứ tự ưu tiên đã chốt.
