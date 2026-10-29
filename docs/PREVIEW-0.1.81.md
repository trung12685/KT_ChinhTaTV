# Chính tả Việt 0.1.81 — bản thử tích lũy để kiểm Word

Gom 9 cụm đợt 69–77 sau 0.1.80: bài Word dấu câu/bookmark chạy riêng; giữ mảng JSON và dẫn chiếu La Mã/dấu nháy; chặn đích lưu bị dọn khi đóng phiên; kiểm đích trước Save để giữ trạng thái duyệt khi chọn nhầm; đồng nhất loại trừ ngày tháng NFC/NFD; đối chiếu thêm chế độ vẽ XPS và xuất báo cáo phạm vi kiểm TXT. Các gói/main/tag cũ giữ nguyên. Word thực **NOT_RUN** tại thời điểm chuẩn bị gói.

[Tải artifact đã kiểm](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40973832589/artifacts/12941004409) · [Nguồn, CI và digest](PREVIEW-0.1.81-EVIDENCE.md). Artifact `ChinhTaViet-0.1.81-preview-bd1ec9ea`, khoảng 69,4 MB, lưu 30 ngày từ lần chạy ngày 30/10/2026 (UTC+7); cần đăng nhập GitHub để tải.

Cần **Windows x64 và Microsoft Word desktop đã cài, mở và kích hoạt bình thường**. Gói có .NET 8 runtime; không cần SDK để chạy EXE. Kiểm Core/OpenXML, WPF, EXE và ZIP trong CI không chạy Microsoft Word.

## Tải và mở

1. Tải artifact `ChinhTaViet-0.1.81-preview-bd1ec9ea` từ liên kết trên; workflow **Windows preview artifact** đã thành công. Cần đăng nhập GitHub.
2. Giải nén artifact rồi giải nén **toàn bộ** ZIP `ChinhTaViet-0.1.81-preview-<mã commit>-win-x64.zip` bên trong vào thư mục mới có quyền ghi. Giữ `samples`, DLL và tài liệu cạnh EXE.
3. Mở `ChinhTa.Desktop.exe`, kiểm phiên bản 0.1.81. Dùng DOCX thử, duyệt đề xuất/bản xem trước, lưu tên mới ở ngoài thư mục làm việc riêng của phiên.
4. Giữ `build-checks.json`, `preview-manifest.json`, TXT tiến độ và `SHA256.txt`. Commit/run phải thuộc đúng gói. Digest artifact ngoài khác SHA256 của ZIP ứng dụng bên trong.

## Kiểm ba nhóm bằng mẫu giả

Chạy `KIEM-TRA-WORD.cmd` cạnh EXE. Lệnh giữ ba nhóm bản sao cô lập, hiệu chuẩn Single/1,5/khoảng đoạn và sửa chữ. Không cần chọn tài liệu cá nhân.

PowerShell tương đương tại thư mục đã giải nén:

```powershell
$p = Start-Process -FilePath .\ChinhTa.Desktop.exe -ArgumentList '--word-check','isolated-spacing,spacing-calibration,technical-text' -Wait -PassThru
$p.ExitCode
```

Mã 0 chỉ khi mọi nhóm đã chọn PASS; mã 1 khi FAIL/NOT_RUN hoặc kết quả chưa hoàn tất; mã 2 khi lựa chọn lệnh không hợp lệ. Báo cáo ở `%LOCALAPPDATA%\ChinhTaViet\OfficeGroups-...\office-groups.txt`. Giữ đúng lượt cùng phiên bản/MVID Desktop/Core, phiên bản/build Word, từng nhóm/bước và lỗi. Có thể chọn trong **Kiểm tra → Kiểm tra từng nhóm Word…**.

- Bản sao phải khớp nguồn trước hiệu chuẩn về trang/ngắt dòng, phông/glyph/baseline, ảnh, nét và chế độ vẽ liên quan.
- Hiệu chuẩn phải giữ phần chưa chọn; không lấy cỡ chữ làm Single hoặc lấy khoảng hai hộp mực so trực tiếp với 6 pt.
- Sửa chữ phải giữ dữ liệu đường dẫn/literal/JSON, các ô biểu mẫu và cả hai dạng Unicode của chú thích ngày tháng. Sửa riêng/chung, lỗi muộn, một Undo và lưu/mở phải giữ nguồn.

## Chạy riêng dấu câu và bookmark

Gói này có **15 nhóm Word**. Hai nhóm sau chạy trực tiếp trong phiên/thư mục riêng, nên lỗi trước trong `technical-text` không ngăn chúng bắt đầu:

```powershell
$p = Start-Process -FilePath .\ChinhTa.Desktop.exe -ArgumentList '--word-check','retained-marks,annotated-text' -Wait -PassThru
$p.ExitCode
```

`retained-marks` kiểm dấu câu có run 14 pt đậm/gạch chân riêng qua sửa riêng/chung, rollback, Undo và lưu/mở. Nếu Word làm mất style và guard phục hồi thì ghi FAIL/chưa sửa được; chỉ đúng chữ chưa đủ PASS.

`annotated-text` kiểm bookmark cùng đoạn/qua đoạn giữ thủ công, trong khi hai đoạn văn xuôi độc lập vẫn sửa được. Tên, phạm vi chữ, kiểu phần chưa chọn và hash nguồn phải được giữ. Không chứng nhận mọi annotation.

## Chạy riêng dẫn chiếu

```powershell
$p = Start-Process -FilePath .\ChinhTa.Desktop.exe -ArgumentList '--word-check','outline-references' -Wait -PassThru
$p.ExitCode
```

Các mẫu số đơn/nhiều cấp, khoảng La Mã và dấu nháy/ngoặc phải giữ nhóm được dẫn chiếu thủ công; nhánh độc lập vẫn được duyệt. Bài này chỉ quét, không đổi chữ, dirty, Undo hoặc nguồn; PASS không thay bài sửa mục/phối hợp Line hoặc bài Word toàn bộ.

## Lưu bản sao và báo cáo phạm vi

Chọn nhầm nguồn, bản làm việc/Undo, thư mục riêng sẽ bị dọn, parent chưa tồn tại hoặc đuôi khác `.docx` thì ứng dụng từ chối trước Save và giữ danh sách lỗi/bản xem trước để chọn lại nơi lưu. Lỗi Save hoặc lỗi phát hiện sau bước kiểm ban đầu vẫn cần Quét lại theo thông báo; không coi mọi lỗi lưu là vô hại.

Mở **Kiểm tra → Phạm vi kiểm tra tài liệu… → Lưu báo cáo TXT…** để giữ snapshot lần quét đang xem. Xuất TXT không gọi Word Save, không sửa tài liệu hoặc Undo. Nếu lưu báo cáo lỗi, bảng vẫn còn để chọn nơi khác/thử lại. Báo cáo không tự quét cập nhật và không xuất nội dung đoạn/CP; số cảnh báo bằng 0 không chứng minh đã kiểm hết tài liệu.

Nếu Word Save lỗi hoặc chưa xác nhận lưu xong, giữ đường phục hồi và nhật ký. Thời hạn chờ lưu không cưỡng bức hủy một COM call đang treo. Các bài mô phỏng lỗi không phải hướng dẫn tạo lỗi trên tài liệu cần giữ.

## Phạm vi

**Quyền sửa Exactly/AtLeast giữ nguyên.** Bộ đọc thử nghiệm OFF, khoảng ký tay 80 pt. Nhiều section/trang ngang/cột, field/control/hình/style đặc biệt và danh sách tự động còn giới hạn. Không chấp nhận/từ chối revision để mở quyền sửa.

Mảng JSON nhúng trong văn xuôi hoặc nông sai cú pháp chưa được parser mới nhận; prefix đạt ngân sách độ sâu thì giữ phần còn lại thủ công. Dẫn chiếu chỉ nhận grammar đã ghi. Các cờ vẽ XPS được đối chiếu không chứng minh mọi backend/hardware vẽ giống nhau. Chưa có từ điển/ngữ nghĩa đầy đủ; 33 nhóm đề xuất có điều kiện, 8 thủ công và 1 minh họa không phải chứng nhận toàn tài liệu.
