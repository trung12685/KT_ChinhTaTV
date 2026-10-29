# Tiếp tục sau bàn giao — 29/10/2026

- Nguồn bàn giao: `5977e8a54e8e9a190f333dd3c1607bad6dd6d65f`, từ mã phát triển `6180757eebdbf59b82b6435bb351cba68c712103` (0.1.77). Đã đọc lại head GitHub trước khi làm; nhánh phát triển chưa thay đổi tại thời điểm bắt đầu. ZIP người dùng khớp 483/483 tệp tree Git, không có tệp thừa.
- Nhánh làm việc mới: [`feature/isolated-spacing-proof-batch22`](https://github.com/trung12685/KT_ChinhTaTV/tree/feature/isolated-spacing-proof-batch22), đã đẩy lên GitHub. Commit triển khai: `705ece02fff0436873c5fd74fa8463aea953e0ba`. Không sửa main, merge PR61, ghi đè tag hoặc xuất ZIP Windows.
- Hoàn thành nền tảng [đợt 22](FORMAT-BATCH-22.md): full Flat OPC → DOCX, kiểm toàn vẹn/quan hệ, proof toàn trang/glyph/baseline/paint/ảnh/nét, phiên Word diagnostic riêng chỉ đọc và nhóm kiểm Exactly/AtLeast. Chưa mở quyền sửa tự động mới; giữ danh mục 33/8/1 và các ưu tiên trong USER-PREFERENCES.md.

## Bằng chứng chạy trên máy hiện tại

Debian Linux x64, .NET SDK 8.0.427. Các gói NuGet khôi phục bằng kết nối proxy hiện có.

| Kiểm tra | Kết quả |
| --- | --- |
| Baseline Core/OpenXML trước sửa | 4.927 assertion đạt, exit 0 |
| Core/OpenXML sau sửa | **4.988 assertion đạt**, exit 0; tăng 61 assertion |
| Build Release solution | Đạt, 0 cảnh báo, 0 lỗi |
| Build Release DesktopChecks và Desktop | Đạt, 0 cảnh báo, 0 lỗi |
| Chạy WPF checks | Máy Linux không chạy; **đã đạt trên Windows CI** của commit triển khai |
| Microsoft Word thực | Chưa chạy: cần Windows cùng Word desktop |
| Windows CI của nhánh mới | **SUCCESS**, [run40909945567](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40909945567), [job123574876003](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40909945567/job/123574876003) |
| Gói Windows/tag/release mới | Không tạo |

4.927 baseline Linux không mâu thuẫn 4.929 Windows trong bàn giao: `VerifiedSaveCopyChecks` và `CheckpointCleanupChecks` có nhánh kiểm hardlink/khóa tệp riêng cho Windows; chênh số assertion tổng là hai. Không cộng số kiểm WPF cũ vào kết quả hiện tại. Không coi build net8.0-windows trên Linux là WPF/Word PASS.

Đã xác minh trang run liên kết đúng commit `705ece0`; từng bước **Core and OpenXML regression checks**, **WPF regression checks and application build**, **Record check evidence** đều `success`. Các bước gọi runner thực và giữ exit code. Đường tải log chi tiết GitHub trả HTTP404 trong phiên này, nên không ghi số assertion Windows chưa đọc được. Kết quả Linux 4.988 ở trên là số đã đọc trực tiếp, không phải số suy từ CI. Bằng chứng tổng hợp: [batch22-evidence.json](handoff-evidence/batch22-evidence.json).

Các mẫu DOCX cũ có lỗi thứ tự thuộc tính schema đã tồn tại; test mới so chính xác danh sách lỗi trước/sau và hash nguồn, không sửa fixture cũ hoặc xóa lỗi để vượt kiểm. Fixture gói mới độc lập được xác minh không có lỗi OpenXML.

Log local được lưu dưới `artifacts/batch22/` (ignored, không phát hành): `core-checks.log`, `desktop-build.log`, `local-evidence.json`. Đây là kết quả trên máy hiện tại, không phải metadata của một gói Windows đã phát hành.

## Bước tiếp theo

Chạy nhóm **Đối chiếu bản sao cô lập Exactly/AtLeast** trên Word thật; WPF đã đạt trong CI Windows, không thay kiểm Word này. Chỉ khi clone khớp nguồn mới triển khai hiệu chuẩn Single/1,5 và khoảng đoạn trên clone; vẫn cần ngăn reflow, chứng minh context, giữ phần chưa chọn, hậu kiểm và rollback/Undo/lưu-mở trước mở quyền C28/C29. Không dùng cỡ font hoặc khoảng hộp mực làm đường tắt. Mã 0.1.77 hiện tích lũy đợt20–22, chưa đủ nhịp 5–10 cụm để đóng gói mới.
