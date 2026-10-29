# Đợt 22 — nền tảng bản sao Word cô lập

Tiếp tục mã 0.1.77 từ commit bàn giao `5977e8a54e8e9a190f333dd3c1607bad6dd6d65f`, có cha là mã phát triển `6180757eebdbf59b82b6435bb351cba68c712103`. Đã đối chiếu cả 483 tệp ZIP người dùng với tree Git: khớp toàn bộ. Main và các tag/gói cũ được giữ nguyên. Đây là một đợt nền tảng, chưa xuất gói Windows.

## Phạm vi đã triển khai

- `IsolatedWordPackage` đọc **toàn bộ Document.WordOpenXML**, không dùng Range XML hoặc `PageNumberPackage` làm clone. Tạo DOCX mới giữ mọi part XML/nhị phân, ID/type/target quan hệ, content type và namespace kế thừa kể cả prefix chỉ xuất hiện trong giá trị `mc:Ignorable`. XML được tuần tự hóa lại; không tuyên bố byte XML bằng file DOCX gốc. Payload nhị phân giữ nguyên byte. Kiểm lại chính xác mọi payload sau ghi, không cho Word ghi lại candidate.
- Kiểm tên part, tên trùng kể cả khác hoa/thường, quan hệ nội bộ thiếu đích, ID trùng, tham chiếu `r:*` thiếu quan hệ, payload lỗi, DTD và giới hạn 4.096 part/64 Mi ký tự XML/128 MiB payload. Tạo file tạm riêng rồi công bố bằng thao tác không ghi đè; lỗi không được làm mất file nguồn hoặc candidate có sẵn.
- Bảo toàn khác quyền mở: gói vẫn có thể giữ quan hệ external, nhưng diagnostic từ chối mở chúng. Macro/ActiveX/OLE, chữ ký gói, embedding, altChunk và field ngoài PAGE/NUMPAGES đơn giản chưa được chấp nhận hiệu chuẩn. Không âm thầm xóa part hoặc liên kết để vượt kiểm.
- `IsolatedSpacingPreview` lấy toàn bộ trang và glyph theo thứ tự vẽ: kích thước trang, chữ/ngắt dòng, SHA-256 dữ liệu phông, cỡ, glyph index/advance/offset/cluster/caret, baseline thật và transform. Kèm proof paint/clip/opacity/bitmap có sẵn; so thêm hình học nét và thứ tự vẽ. Tọa độ/khung phải khớp trong 0,01 đơn vị XPS (1/96 inch), chặt hơn proof bảo toàn PAGE. Container có nền chưa được chứng minh, glyph xoay/lật/hướng chữ ngoài phạm vi vẫn từ chối. Sai khác chỉ dẫn tới chưa đủ bằng chứng, không suy tài liệu sai spacing.
- `IsolatedWordCopy` mở một phiên Word mới riêng, chỉ đọc, không dùng Word của người dùng, không save hoặc thêm checkpoint. Tắt macro và các tùy chọn cập nhật liên kết/field khi in trước mở; chụp và khôi phục các tùy chọn đã thay trước khi Quit. Không có field updater tổng quát; PAGE/NUMPAGES do Word kết xuất vẫn phải vượt đối chiếu. Đóng riêng document/app đã tạo kể cả khi thất bại. Lỗi đóng/release/xóa tạm không thể thành PASS; giữ thư mục bản sao khi đóng chưa được xác nhận và báo đường phục hồi. Không kill tiến trình Word khác.
- `VerifyCheckIsolatedCopy` chỉ là đường diagnostic, không tự chạy khi quét/sửa. Đọc full package nguồn trước và sau; nguồn in-memory thay đổi thì bác kết quả. Không Save nguồn để lấy XML. Xóa XPS diagnostic sau dùng; không ghi chữ tài liệu/font/XML vào báo cáo tiến độ.
- Menu **Kiểm tra → Kiểm tra từng nhóm Word… → Đối chiếu bản sao cô lập Exactly/AtLeast**: hai mẫu, mỗi mẫu có hai đoạn ba dòng và Before/After, chạy so clone hai lần, kiểm nguồn/Undo/trạng thái. Tổng danh sách có 10 nhóm độc lập. Báo cáo chỉ nêu part/trang/glyph run và trạng thái, không gọi đó là hiệu chuẩn hoặc sửa spacing.

## Kiểm chứng và giới hạn

Lệnh Core/OpenXML:

```bash
dotnet run --project tests/ChinhTa.Checks -c Release
dotnet build tests/ChinhTa.DesktopChecks -c Release
```

Các ca Core mới kiểm DOCX fixture mở được theo schema, toàn vẹn ảnh/quan hệ/content type/namespace, bảo toàn nguồn và không ghi đè, thay một byte ảnh, lỗi quan hệ, field/link không hỗ trợ, bốn mẫu DOCX có sẵn không thêm hoặc che lỗi schema gốc, và bằng chứng baseline bất biến. Các ca WPF mới có cả phông thay thế, dòng mất/lệch, đổi màu/opacity/clip, ảnh đổi/dịch, hình nét khác cùng bounding box và thứ tự vẽ đổi, cùng schema mẫu Office. **Biên dịch các ca WPF không phải chạy chúng.** Số kiểm Linux cuối đợt và trạng thái build nằm trong [CONTINUATION-2026-10-29.md](CONTINUATION-2026-10-29.md).

Windows vẫn cần chạy:

```powershell
dotnet run --project tests/ChinhTa.DesktopChecks -c Release
dotnet run --project src/ChinhTa.Desktop
```

Sau đó chọn riêng nhóm clone trong menu trên máy có Word desktop. Cần xác nhận Word thực cho Exactly/AtLeast, gói có theme/numbering/ảnh/font riêng, giữ nguồn và cleanup khi COM lỗi. Trình ghi không tự sửa lỗi hoặc bỏ part để bản sao khớp; Word có thể chuẩn hóa font subset/XML/field nên một số bản sao hợp lệ vẫn bị từ chối bảo thủ. Không gọi CI hoặc cross-compile là Word thực PASS.

## Công việc tiếp theo

Chưa có hiệu chuẩn Single/1,5 trên bản sao, chưa loại Before/After để đo khoảng đoạn gộp, chưa có writer/hậu kiểm mới cho Exactly/AtLeast. Phải chứng minh baseline clone trên Word trước, rồi giới hạn đoạn cùng trang/context rõ, đối chiếu cả vector dòng và reflow, sau đó mới thiết kế quyền sửa cùng rollback/Undo/lưu-mở/phần chưa chọn. Không lấy cỡ font làm Single, không so khoảng hai hộp mực trực tiếp với 6 pt. Số nhóm đề xuất/thủ công/minh họa giữ 33/8/1; bộ đọc thử nghiệm vẫn OFF; chỗ ký tay vẫn 80 pt; không mở lại tối ưu tốc độ.
