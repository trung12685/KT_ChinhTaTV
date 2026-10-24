# Đợt phát triển sau 0.1.62

Mốc nền: `68865ca574b3c32f170b378080e6663287d036cd`, tag `v0.1.62`. Bản nền đạt 2882 Core/OpenXML + 206 WPF + 1 kiểm metadata gói; chưa xác nhận Word runtime. Main và stable .27 giữ nguyên.

Phiên bản code: **0.1.63**, đủ5 bước B01–B05; chỉ có gói khi kiểm bắt buộc và pipeline phát hành đúng commit đạt. Các ghi chú chưa đóng gói ở B01–B04 là lịch sử từng mốc. Sau khoảng 5–10 hạng mục hoàn chỉnh mới chuẩn bị phát hành theo yêu cầu người dùng. [Windows gần nhất .62](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.62).

## B01 — phông, màu, cỡ đoạn văn xuôi thường

- C26 mới: Times New Roman, đen, cỡ 13–14 theo NĐ30 PL I.I.4 và II.6.e. Ví dụ: đoạn văn xuôi thường có Arial đỏ cỡ 12 xen chữ cỡ 14 → Times New Roman đen, chỉ cỡ 12 về 13, giữ từng cỡ 14.
- Cần văn bản Nhà nước có tên loại/trích yếu xác định, cặp quốc hiệu/tiêu ngữ trang đầu, mốc Kính gửi và chữ ký/Nơi nhận phía sau. Không suy vai trò từ cỡ chữ hoặc vị trí sau tiêu đề đơn thuần.
- Chỉ đoạn chữ thuần đã đối chiếu XML, văn xuôi có kết thúc rõ, không danh sách/heading/bảng/tab/field, không đậm/nghiêng. XML cung cấp căn đoạn kể cả sau đoạn 40; chỉ quan sát phông các ứng viên. Chưa đủ bằng chứng thì bỏ qua phạm vi C26, không tạo cảnh báo định dạng giả.
- Dùng writer phông/cỡ hiện có; giữ cỡ đúng, nguyên văn và gạch chân. Mở theo dõi kiểu ký tự/căn-thụt đoạn cho C26; kiểm sau lưu và sau các nhóm/Line. Chọn riêng giữ lỗi chữ/phông chưa chọn; chọn chung khoảng trắng/cụm lặp đo lại trước Line, giữ nét XPS thật và rollback/một Undo.
- Thêm Core/GUI regression và fixture Word tổng hợp để kiểm sửa riêng/chung chữ + ba Line, Undo, lưu/mở và hash nguồn. Fixture Word chỉ được biên dịch trong CI; muốn có runtime pass cần chạy trên máy có Word đúng bản.

Tổng danh mục: **26 nhóm đề xuất, 8 thủ công, 1 minh họa**; không phải số lỗi của tài liệu. Kết quả kiểm tự động theo đúng commit tại PR/Windows checks, không dùng số của bản nền thay cho lượt mới.

## B02 — căn đều và thụt dòng đầu

- C27 mới theo II.6.e: văn xuôi thường căn trái, đầu dòng0cm → căn đều, đầu dòng1,27cm. Giữ mức1cm/1,27cm đang đúng, chỉ đổi thuộc tính thực sự sai.
- Tái dùng ranh giới/proof C26; giữ ngoài phạm vi căn giữa/phải, thụt cả đoạn/hanging, khoảng trắng đầu/cuối, nhấn mạnh/danh sách/bảng và vùng chưa chắc. Quan sát căn/thụt và khoảng đoạn thực tế một lần trên ứng viên; không đọc được thì không mở quyền sửa căn/thụt.
- Payload căn/thụt riêng, không ghi phông/chữ. Snapshot trước sửa gồm giãn dòng/khoảng đoạn/auto spacing; stale so cả payload. Hậu kiểm giữ glyph/cỡ/gạch chân và các thuộc tính đoạn chưa chọn sau lưu/Line. Sửa riêng giữ C26/chữ chưa chọn, sửa chung không chồng lấn, một Undo/rollback.
- Thêm Core và Desktop writer/GUI checks; fixture Word sau C26 kiểm riêng/chung phông/chữ/ba Line, khôi phục, lưu-mở và nguồn. CI không có Word, chỉ biên dịch fixture; kết quả tự động theo đúng commit tại PR39/Actions.

Tổng code sau B02: **27 đề xuất/8 thủ công/1 minh họa**. Hoàn thành2 hạng mục sau .62, chưa đóng gói .63. [Phạm vi C27](BODY-PARAGRAPH.md).

## B03 — C28 khoảng cách giữa hai đoạn

Hai đoạn văn xuôi liền nhau cùng trang, After trước0pt + Before sau0pt → sau đoạn trước6pt. Xét cả hai khoảng và chỉ kết luận khi tổng chưa thể đạt6pt, không chỉ thấy After<6 là báo lỗi. Giữ ngoài phạm vi tổng có thể đủ nhưng gộp chưa rõ, auto/contextual/grid, page/keep, khoảng trống/mục/bảng xen giữa. Chụp XML/ngữ cảnh/trang vật lý và đoạn kế, hậu kiểm cặp sau lưu/Line; chỉ ghi After, giữ phần chưa chọn.

## B04 — C29 giãn dòng

Double hoặc Multiple ngoài1–1,5 → Single; Single/1,5/Multiple trong khoảng giữ đúng. Exactly/AtLeast chưa có phép đo tương đương giữ ngoài phạm vi. Writer chỉ đổi rule, không ghi pt làm chuyển sang Exactly; giữ Before/After/căn/thụt/glyph chưa chọn. Ghép C26/C27/C28/C29 từ snapshot bất biến theo thứ tự checkbox nào cũng đúng, sau chữ đo lại Line; một Undo/rollback.

Core/desktop kiểm riêng/chung, biên trang/đoạn/context/style, chế độ giãn dòng, stale/neighbor, writer/drift/COM/checkbox. Fixture Word kiểm riêng/chung chữ/phông/căn/ba Line, khôi phục/lưu-mở/nguồn; CI chỉ biên dịch fixture. Kết quả mới theo đúng commit tại PR39/Actions, không lấy tổng B02 làm bằng chứng. [Phạm vi C28/C29](BODY-SPACING.md).

Sau B04: **4 hạng mục**, code .63 **29 đề xuất/8 thủ công/1 minh họa**, chưa đóng gói Windows .63. Bản tải .62, main/stable giữ nguyên. Chưa có Word runtime hoặc bằng chứng khép phản hồi Office2021 cũ.

## B05 — mở đầu không Kính gửi / chuẩn bị .63

Tiếp nối B04 `9445ce25e7c45805cdb7040c7b267f09f5bb234a` (3080 Core/OpenXML +232 WPF). Mở C26–C29 cho văn bản có tên loại/trích yếu nhưng không Kính gửi khi hai mốc mở đầu văn xuôi có XML căn đều, lề trái/phải0, không hanging, firstLine1cm hoặc1,27cm và nằm trong vùng trang đầu đã đối chiếu. Font/kiểu và căn/thụt Word thực tế phải rõ/khớp XML; quan sát tạm không cấp quyền sửa. Hai mốc ngắn sau xóa lặp vẫn giữ vai trò để không che lỗi chưa chọn phía sau; vẫn cần chữ ký/Nơi nhận đóng khối. Không mở Công văn không tên loại/nhấn mạnh/mục/bảng/field/kiểu chưa rõ hoặc toàn thân bài.

Thêm Core về mốc đúng/sai/stale/kế thừa/bidi/proof tạm, fixture Word ghép bốn nhóm/chữ/ba Line, giữ1cm/1,27cm, cỡ/gạch chân/spacing, rollback/Undo/lưu-mở/nguồn. CI chỉ biên dịch fixture, không Word runtime. [Chi tiết](BODY-OPENING.md).

Đủ **5 hạng mục**, tổng29/8/1. Chuẩn bị release notes/checklist .63, đưa cả5 bước vào TXT và gói. Kết quả thực tế theo commit tại PR39/Actions/release; không lấy số B04 làm kết quả mới hoặc coi version là đã có bản tải. Main/stable .27 và .62 giữ nguyên.

## Hạng mục tiếp theo

1. Mở rộng nhận diện thân bài từng trường hợp có bằng chứng, giữ các phần nhấn mạnh và cấu trúc riêng.
2. Kiểm tổng thể C27 căn/thụt trên Word thực tế và mở phạm vi từng vùng khi có bằng chứng, không tự áp toàn văn bản.
3. Đo các ngoại lệ khoảng đoạn cộng/gộp/contextual/auto và giãn Exactly/AtLeast/grid; C28/C29 đã có phạm vi chắc riêng, không tự kết luận các phần chưa đo.
4. Kiểm tổng thể nhiều nhóm, danh sách mới sau sửa, tiến độ, Undo/lưu-mở và Office 2021.
5. Tiếp tục lỗi chữ cơ bản có mẫu/ngoại lệ; từ điển/ngữ nghĩa sau cùng.

Chi tiết: [phạm vi C26](BODY-FORMAT.md). Báo cáo TXT trong chương trình có ví dụ, việc đã hỗ trợ và việc còn lại; code version không đồng nghĩa đã có bản tải.
