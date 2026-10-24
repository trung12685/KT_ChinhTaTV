# C26 — phông/cỡ chữ đoạn văn xuôi thường

B03/B04 đã có [C28/C29](BODY-SPACING.md) với duyệt riêng cho khoảng đoạn/giãn dòng đủ bằng chứng. Các giới hạn dưới đây là phạm vi riêng của C26/C27 lúc bổ sung; không mở toàn bộ thân bài hoặc mọi kiểu giãn dòng.

B02 sau .62 đã có [C27 căn/thụt dòng đầu](BODY-PARAGRAPH.md) với phạm vi/duyệt riêng. Tài liệu C26 này chỉ mô tả phông/màu/cỡ; các giới hạn về căn/thụt dưới đây thuộc C26.

Bổ sung từ đợt B01 sau .62, code .63 đang phát triển; **chưa phát hành Windows .63**.

## Căn cứ và kết quả

Nghị định 30/2020/NĐ-CP, Phụ lục I, phần I, mục I.4: Times New Roman, Unicode, màu đen. Phần II, mục 6, điểm e (PDF trang 25): nội dung chữ in thường, đứng, cỡ 13–14, căn đều hai lề; đầu dòng lùi 1 cm hoặc 1,27 cm; khoảng đoạn tối thiểu 6 pt; giãn dòng đơn đến 1,5 lines. Đã đối chiếu trang nguồn, không gọi nhầm thành điểm b.

Bước này chỉ kiểm/sửa **phông, màu, cỡ** trong phạm vi dưới. Ví dụ đoạn `Nội dung công tác đã được kiểm tra đầy đủ.` có chữ Arial đỏ cỡ 12 và chữ cỡ 14: sau duyệt, phông Times New Roman màu đen, cỡ 12 về 13, cỡ 14 giữ nguyên. Đoạn đã đúng không xuất hiện lỗi C26.

## Nhận diện và loại trừ

- Chỉ chế độ Nhà nước, văn bản có tên loại/trích yếu căn giữa xác định trong phần đầu, hai mốc quốc hiệu/tiêu ngữ trước tên loại, mốc Kính gửi ở sau trích yếu và mốc kết thúc chữ ký/Nơi nhận. Chưa mở Công văn, tài liệu Đảng/cá nhân hoặc tài liệu không có các mốc này.
- Chỉ văn xuôi thường đủ dài và có dấu kết thúc, không đậm/nghiêng, không tiêu đề/mục gõ tay hoặc Word auto-list. Chữ nhấn mạnh chưa bị coi là lỗi để xóa kiểu. Đoạn trích dẫn, kết thúc chưa rõ, tab/ngắt dòng thủ công, bảng, field/control/link/object, heading hoặc XML không chắc đều ngoài phạm vi.
- Mỗi đoạn cần proof chữ và style/định dạng thực tế. Căn đoạn từ XML có kế thừa style; chỉ căn trái/căn đều làm ứng viên. Căn khác/không biết giữ ngoài phạm vi, không kết luận là sai hay đúng toàn bộ.
- Không giới hạn thân bài ở 40 đoạn; phần hình học đầu trang vẫn giữ giới hạn cũ. Không đọc phông mọi đoạn tiêu đề/bảng trước khi xác định vai trò.

## Sửa, hậu kiểm và giao diện

C26 ở **Bắt buộc sửa**, checkbox từng mục/nhóm/tất cả, người dùng vẫn duyệt. Tooltip ba dòng: Lỗi / Căn cứ / Cách xử lý. Không tự chọn thêm nhóm chữ không liên quan.

Sửa phông/màu trên vùng đã xác minh; nếu cỡ đúng trộn cỡ sai, chỉ đổi glyph cỡ sai về 13. Hậu kiểm từng ký tự còn lại sau sửa chữ, giữ cỡ đúng/gạch chân, phông/màu/kiểu đã duyệt, căn và thụt đoạn. Dùng cùng phép chiếu glyph đã có cho viết hoa tên loại, không dùng số Shape làm bằng chứng.

Sửa riêng C26 giữ khoảng trắng/cụm lặp chưa chọn. Chọn chung thì chữ/phông → đo lại XPS → căn/Line nếu cần → hậu kiểm → quét mới. Lỗi đã đạt phải biến mất khỏi danh sách. Một Undo và rollback toàn lượt khi thất bại; nguồn DOCX giữ nguyên, chỉ lưu bản sao.

Chưa sửa căn, thụt, giãn dòng/khoảng đoạn, nhấn mạnh hoặc toàn bộ thân bài/chữ ký. Không có C26 không chứng minh toàn văn bản đúng NĐ30.

## Kiểm chứng

Core kiểm mẫu đúng/sai, cỡ hỗn hợp, ngoại lệ, ranh giới, body sau đoạn 40, kế thừa căn XML, bảng không có cờ COM và auto-list, stale selection, sửa chung, kiểu/gạch chân/căn đoạn và rollback. Desktop kiểm nhóm/checkbox/tooltip và loại bỏ dòng cũ khi tạo danh sách mới. Fixture Word `OfficeBodyFormatCheck` tạo mẫu công khai giả lập, kiểm sửa riêng/chung chữ + ba Line, giữ cỡ/gạch chân/căn đoạn, rollback, một Undo, lưu/mở và hash nguồn.

CI Windows không cài Word; fixture Word được biên dịch, **không coi là đã chạy Word/Office 2021**. Phản hồi phông cũ chưa khép bằng CI. Kết quả theo commit tại PR/Actions.
