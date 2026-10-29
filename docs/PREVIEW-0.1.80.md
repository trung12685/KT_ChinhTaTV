# Chính tả Việt 0.1.80 — bản thử tích lũy để kiểm Word

Gom các đợt 57–68 sau 0.1.79: giữ trạng thái phục hồi khi lưu lỗi, chờ Word lưu nền xong, chặn sửa qua lịch sử thay đổi/vùng đánh dấu chưa có bằng chứng bảo toàn, giữ ô biểu mẫu, sửa vòng đời cửa sổ tiến độ và hướng dẫn phục hồi khi mất kết nối. Các gói, main và tag cũ giữ nguyên. Word thật **NOT_RUN** tại thời điểm chuẩn bị gói.

Cần **Windows x64 và Microsoft Word desktop đã cài, mở và kích hoạt bình thường**. Gói có .NET 8 runtime; không cần SDK để chạy EXE. Core/OpenXML, WPF và kiểm EXE/ZIP trong CI không chạy Microsoft Word.

## Tải và mở

1. Tải artifact `ChinhTaViet-0.1.80-preview-<mã commit>` từ workflow **Windows preview artifact** đã thành công; cần đăng nhập GitHub. Liên kết và commit của gói được ghi trong tài liệu bằng chứng sau khi workflow hoàn tất.
2. Giải nén artifact rồi giải nén **toàn bộ** ZIP `ChinhTaViet-0.1.80-preview-<mã commit>-win-x64.zip` bên trong vào một thư mục mới có quyền ghi. Giữ `samples`, DLL và tài liệu cạnh EXE.
3. Mở `ChinhTa.Desktop.exe`, kiểm phiên bản 0.1.80. Bắt đầu với DOCX thử, duyệt các đề xuất/bản xem trước và lưu tên mới.
4. Giữ `build-checks.json`, `preview-manifest.json`, TXT tiến độ và `SHA256.txt`. Commit/run phải thuộc đúng gói. Digest artifact ngoài khác SHA256 của ZIP ứng dụng bên trong.

## Kiểm ba nhóm bằng mẫu giả

Chạy `KIEM-TRA-WORD.cmd` cạnh EXE. Lệnh chọn ba nhóm bản sao cô lập, hiệu chuẩn Single/1,5/khoảng đoạn và sửa chữ. Không cần chọn tài liệu cá nhân.

PowerShell tương đương tại thư mục đã giải nén:

```powershell
$p = Start-Process -FilePath .\ChinhTa.Desktop.exe -ArgumentList '--word-check','isolated-spacing,spacing-calibration,technical-text' -Wait -PassThru
$p.ExitCode
```

Mã 0 chỉ khi tất cả nhóm đã chọn PASS; mã 1 khi FAIL/NOT_RUN hoặc kết quả chưa hoàn tất; mã 2 khi lựa chọn lệnh không hợp lệ. Báo cáo ở `%LOCALAPPDATA%\ChinhTaViet\OfficeGroups-...\office-groups.txt`. Giữ báo cáo đúng lượt, cùng MVID Desktop/Core, phiên bản/build Word, từng nhóm/bước và lỗi. Có thể chọn trong **Kiểm tra → Kiểm tra từng nhóm Word…**.

- Bản sao phải khớp nguồn trước hiệu chuẩn: trang/ngắt dòng, glyph/phông/baseline, ảnh và nét.
- Hiệu chuẩn phải giữ phần chưa chọn. Không lấy cỡ chữ làm Single hoặc lấy khoảng hai hộp mực so trực tiếp với 6 pt.
- Sửa chữ phải giữ dữ liệu đường dẫn/literal và các ô số/ngày/ô chọn nhận diện được. Sửa riêng/chung, lỗi muộn, một Undo, lưu/mở phải giữ nguồn.
- Dấu câu có run 14 pt đậm/gạch chân riêng phải còn thuộc tính chưa duyệt đổi. Nếu Word làm mất style và guard phục hồi thì ghi FAIL/chưa sửa được; đúng chữ riêng lẻ chưa đủ PASS.
- Bài vùng đánh dấu kiểm chữ có lỗi trong bookmark cùng đoạn/qua đoạn được giữ thủ công, trong khi đoạn văn xuôi độc lập vẫn sửa được. Sau lưu/mở, tên và phạm vi chữ của bookmark phải còn được đối chiếu. Mẫu này không chứng nhận hỗ trợ mọi annotation.

## Kiểm dẫn chiếu độc lập

Có 13 nhóm Word. Nhóm sau chỉ quét mẫu số nhiều cấp/dẫn chiếu, không cần Subject/Line trước đạt:

```powershell
$p = Start-Process -FilePath .\ChinhTa.Desktop.exe -ArgumentList '--word-check','outline-references' -Wait -PassThru
$p.ExitCode
```

Nhóm 1.4 phải giữ thủ công khi nhãn cũ/dự kiến nằm trong khoảng hoặc danh sách; nhóm 2.4 độc lập vẫn được duyệt. Quét không đổi nguồn, nội dung, dirty hoặc Undo. PASS nhóm này không thay bài sửa mục/phối hợp Line hoặc bài Word toàn bộ.

## Lưu và phục hồi

Nếu Save lỗi hoặc Word chưa xác nhận lưu xong, bản tạm chưa được coi là checkpoint tin cậy. Giữ đường phục hồi và nhật ký; việc còn file không chứng minh nội dung đã lưu đủ. Ứng dụng chờ trạng thái lưu nền trong giới hạn, nhưng không thể cưỡng bức hủy một COM call đang treo.

Sau Hoàn tác, nếu quét lại lỗi, đọc thông báo về thao tác đã hoàn tất trước khi Undo thêm. Khi Word mất kết nối và checkpoint chưa đủ điều kiện, giao diện hướng dẫn giữ dữ liệu/đường dẫn để kiểm tra; không tiếp tục hướng dẫn Quét lại như thể chắc chắn mở lại được. Các bài mô phỏng lỗi trong CI không phải hướng dẫn tạo lỗi trên tài liệu cần giữ.

## Phạm vi

**Quyền sửa Exactly/AtLeast giữ nguyên.** Bộ đọc thử nghiệm OFF, khoảng ký tay 80 pt. Nhiều section/trang ngang/cột, field/control/hình/style đặc biệt và danh sách tự động còn giới hạn. Không chấp nhận/từ chối revision để mở quyền sửa. Bảo vệ bookmark/comment/permission là giới hạn quyền sửa chữ trong phạm vi đã đối chiếu, chưa phải writer bảo toàn mọi cấu trúc đánh dấu.

Chưa có từ điển/ngữ nghĩa đầy đủ. Danh mục 33 nhóm đề xuất có điều kiện, 8 thủ công và 1 minh họa không phải chứng nhận toàn tài liệu.
