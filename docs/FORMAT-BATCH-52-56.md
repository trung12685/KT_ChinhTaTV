# Đợt 52–56 — phần còn thiếu của bảo toàn và phục hồi

Ngày 30/10/2026 (UTC+7), tiếp tục từ `59a406da10ff3bc27413ceb54042d50f1b91c3df`, nhánh `feature/isolated-spacing-proof-batch22`. Mã 0.1.79 tích lũy đợt 45–56 sau 0.1.78; chuẩn bị một gói preview riêng sau khi kiểm tra. Main, tag/gói cũ, bộ đọc thử nghiệm OFF, khoảng ký tay 80 pt và quyền sửa Exactly/AtLeast giữ nguyên.

## 52. Dẫn chiếu lặp cùng cue

Tái hiện: 1/2/9 có “Xem từ mục 4 đến mục 10.” còn cho 9 → 3 vì cue thứ hai làm mất quan hệ khoảng. Nay `OutlineRules` nhận cùng cue lặp trong chuỗi danh sách/khoảng, kể cả `số`, ngoặc, NFC/NFD, hoa/thường và khoảng ngang Unicode. Chỉ cùng loại cue mới nối khoảng; không ghép “mục” với “khoản” thành cùng phạm vi. Dấu phẩy/`và` vẫn là danh sách.

Giữ chữ/CP nguồn, cơ chế so số bằng chuỗi và kiểm lựa chọn hiện tại. Mẫu Word dùng 1.3 đến mục 1.5 để chặn nhãn giữa 1.4, hoặc 1.0 tới mục 1.3 để bảo vệ nhãn dự kiến 1.2; nhóm 2.4 độc lập vẫn được duyệt. Chưa phải parser mọi dẫn chiếu pháp lý.

## 53. Hậu kiểm dấu gốc khi thêm khoảng

Tái hiện bằng Core: những gợi ý thật như `:` → `: `, `.` → `. `, `(` → ` (` hoặc `”` → `” ` không tạo probe trong lượt chỉ sửa dấu câu. Nếu writer làm mất cỡ/đậm/gạch chân của dấu gốc, mô hình chưa có hậu kiểm riêng để chặn.

Thêm trigger hẹp cho dấu hai chấm, `.?!`, ngoặc tròn và nháy thẳng/cong khi đúng payload giữ một dấu, chèn một ASCII space đúng phía. Dùng sentinel/probe `PunctuationSpace` toàn đoạn đã chứng minh, giữ owner body/title/signature cũ, dùng nguyên reader native/prefix và hậu kiểm sau lưu/rollback. Thiếu style glyph, spacing hoặc tổ hợp phông chưa hỗ trợ bị chặn trước sửa. Xóa riêng spaces vẫn theo đường cũ; không mở mọi phép thay dấu hoặc thay writer `Range.Text`.

Mẫu Word sửa chữ thêm bốn đoạn có dấu 14 pt đậm/gạch chân riêng giữa chữ 13 pt; kiểm sửa riêng/chung, lỗi cuối lượt, một Undo, lưu/mở và nguồn bất biến. Fixture đúng schema và giữ các part khác được kiểm riêng; trường hợp Word làm mất style phải FAIL/rollback, không chấp nhận chỉ đúng chữ.

Sửa hai kiểm kịch bản cũ vốn mặc định dirty phải false sau Undo: giờ so trạng thái ngay trước fault với sau rollback, đúng chính sách Undo có thể giữ dirty=true. Không đổi chính sách Undo sản xuất để làm bài kiểm đạt.

## 54. Không tin checkpoint sau Save lỗi

Tái hiện bằng code sản xuất và fake Save ghi dở rồi ném lỗi: `snapshotCurrent` còn true, `Changed` còn false; Dispose xóa thư mục chứa bản làm việc và Undo. Nay đánh dấu chưa xác minh trước Save, chỉ xác nhận lại khi Save hoàn tất. Lỗi giữ nguyên nguyên nhân và đánh dấu chưa lưu.

Kiểm đường SaveCopy thật xác nhận không công bố đích, không tiêu thụ Undo hoặc sửa nguồn; byte lưu dở được giữ nhưng không được mở phục hồi như bản đã xác minh. Dispose giữ thư mục này để xử lý tiếp. Save thành công không tự đổi dirty; chỉ bản sao hoàn chỉnh đã công bố mới xóa dirty. Không tự lặp lại một lệnh lưu lỗi.

## 55. Hoàn tác và vị trí xem trước không gây hiểu nhầm

Sau Undo thành công mà Check/XPS quét lại lỗi, UI trước đây báo chung thao tác chưa hoàn tất. Thêm `EditThenScan.RunUndo`: giữ lỗi Undo gốc, chỉ bọc lỗi quét sau lệnh đã xong và hướng dẫn Quét lại; không tự pop thêm Undo. Thông báo vẫn đúng khi lệnh không có gì để hoàn tác.

Chọn một cảnh báo thiếu anchor trước đây có thể giữ highlight/status của cảnh báo trước. Nay xóa dấu cũ cùng pending/displayed và báo chưa định vị chắc; không tự đoán trang hoặc tạo anchor. Chọn lại mục có mapping vẫn hoạt động; guard hover cùng mục không tạo dấu lặp. Kiểm WPF dùng MainWindow/preview thật với dữ liệu giả, không Word COM.

## 56. Kiểm dẫn chiếu Word độc lập

Nhóm thứ 13 `outline-references` dùng các mẫu số nhiều cấp/dẫn chiếu hiện có, phiên Word và thư mục riêng theo runner. Kiểm giữ 1.4 thủ công nhưng vẫn cho duyệt 2.4; quét không đổi chữ, nguồn, dirty hoặc Undo. Không cần bước Subject/Line trước đó đạt; bài Word toàn bộ vẫn giữ kết quả riêng. Nhóm mới chỉ kiểm bảo vệ dẫn chiếu, không chứng nhận sửa mục/phối hợp Line đã đạt.

## Kiểm chứng

Core/OpenXML cục bộ: **7021 kiểm qua**, tăng **318** từ 6703. Build DesktopChecks Release 0 warning/error. Harness riêng: 94 kiểm cue lặp, 212 giữ dấu gốc, 12 Undo/quét và 92 checkpoint/Save; các số nằm trong runner tương ứng, không cộng lại. Hai lớp fixture cuối qua 64 kiểm nguồn (19 dấu câu + 45 dẫn chiếu), giữ schema/part và mô hình lựa chọn; không cộng harness vào tổng runner.

[Windows CI](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40959817177) của commit `3e8f268ad935a9cfb24a12427ec36224a7848e02` đã **Success**: [job 123749340881](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40959817177/job/123749340881) qua Core/OpenXML, WPF/build, ghi evidence và tải log. [Gói preview](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40960198832/artifacts/12935094001) trên đúng commit cũng đã qua build/EXE/metadata/ZIP/toàn vẹn; xem [bằng chứng gói](PREVIEW-0.1.79-EVIDENCE.md). Tổng Windows nằm trong evidence của đúng lượt, không suy từ Linux.

Microsoft Word thực vẫn **NOT_RUN**. Kịch bản/biên dịch/Core/WPF không phải bằng chứng Word. Không mở thêm tự sửa Exactly/AtLeast, parser tổng quát hoặc từ điển/ngữ nghĩa.
