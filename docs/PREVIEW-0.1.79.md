# Chính tả Việt 0.1.79 — bản thử tích lũy để kiểm Word

Gom đợt 45–56 sau 0.1.78: hậu kiểm dấu câu có định dạng riêng, bảo vệ dẫn chiếu/literal/đệm biểu mẫu, giữ dữ liệu phục hồi sau Save lỗi, thông báo Hoàn tác/quét và xóa highlight cũ khi không định vị được cảnh báo. Chỉ có artifact sau khi workflow tạo gói thành công; các bản 0.1.77/0.1.78 và release/tag cũ giữ nguyên. Word thật **NOT_RUN** tại thời điểm chuẩn bị gói.

Cần **Windows x64 và Microsoft Word desktop đã cài, mở và kích hoạt bình thường**. Gói có .NET 8 runtime; không cần SDK để chạy EXE. Các kiểm Core/OpenXML, WPF, EXE và ZIP trong CI không chạy Microsoft Word.

## Tải và mở

1. Trên GitHub, tải artifact `ChinhTaViet-0.1.79-preview-<mã commit>` của workflow **Windows preview artifact** đã thành công; cần đăng nhập GitHub.
2. Giải nén artifact, rồi giải nén **toàn bộ** ZIP `ChinhTaViet-0.1.79-preview-<mã commit>-win-x64.zip` bên trong vào thư mục mới có quyền ghi. Giữ `samples`, DLL và tài liệu cạnh EXE.
3. Mở `ChinhTa.Desktop.exe`, kiểm phiên bản 0.1.79. Bắt đầu bằng DOCX thử, duyệt đề xuất/bản xem trước, lưu tên mới.
4. Giữ `build-checks.json`, `preview-manifest.json`, TXT tiến độ và `SHA256.txt`. Commit/run của metadata phải thuộc đúng gói. Digest artifact ngoài và SHA256 ZIP ứng dụng là hai giá trị khác nhau.

## Kiểm ba nhóm bằng mẫu giả

Chạy `KIEM-TRA-WORD.cmd` cạnh EXE. Lệnh chọn ba nhóm bản sao cô lập, hiệu chuẩn Single/1,5/khoảng đoạn và sửa chữ. Không cần chọn tài liệu cá nhân.

PowerShell tương đương, mở tại thư mục đã giải nén:

```powershell
$p = Start-Process -FilePath .\ChinhTa.Desktop.exe -ArgumentList '--word-check','isolated-spacing,spacing-calibration,technical-text' -Wait -PassThru
$p.ExitCode
```

Mã 0 chỉ khi mọi nhóm đã chọn PASS; mã 1 khi FAIL/NOT_RUN hoặc kết quả chưa hoàn tất; mã 2 khi lựa chọn lệnh không hợp lệ. Báo cáo ở `%LOCALAPPDATA%\ChinhTaViet\OfficeGroups-...\office-groups.txt`. Giữ báo cáo đúng lần chạy cùng MVID Desktop/Core, phiên bản/build Word, từng nhóm/bước và lỗi. Chọn trong UI tại **Kiểm tra → Kiểm tra từng nhóm Word…** cũng được.

- Bản sao phải khớp nguồn trước hiệu chuẩn: trang/ngắt dòng, glyph/phông/baseline, ảnh và nét.
- Hiệu chuẩn phải giữ phần chưa chọn. Không lấy cỡ chữ làm Single hoặc lấy khoảng hai hộp mực so trực tiếp 6 pt.
- Nhóm sửa chữ giữ nguyên đường dẫn/tên tệp/literal, ô số/ngày/ô chọn và NBSP/NNBSP; sửa riêng/chung, lỗi muộn, một Undo và lưu/mở phải giữ nguồn. Dấu phẩy, hai chấm, kết câu, đóng ngoặc/nháy có run 14 pt đậm/gạch chân riêng phải còn nguyên thuộc tính chưa duyệt đổi. Nếu Word làm mất style và guard phục hồi, ghi nhận FAIL/chưa sửa được; không coi chỉ đúng chữ là PASS.

## Kiểm dẫn chiếu độc lập

Có 13 nhóm Word. Để kiểm riêng các mẫu dẫn chiếu không cần Subject/Line trước đạt:

```powershell
$p = Start-Process -FilePath .\ChinhTa.Desktop.exe -ArgumentList '--word-check','outline-references' -Wait -PassThru
$p.ExitCode
```

Nhóm này chỉ quét mẫu số nhiều cấp/dẫn chiếu: nhóm 1.4 phải giữ thủ công khi nhãn cũ/dự kiến nằm trong khoảng hoặc danh sách, nhóm 2.4 độc lập vẫn được duyệt. Quét không đổi nguồn, nội dung, dirty hoặc Undo. PASS nhóm này không thay bài sửa mục/phối hợp Line hoặc bài Word toàn bộ.

## Thử luồng phục hồi và giao diện

1. Trên tài liệu thử A, sửa một lỗi rồi thử mở B là DOCX lỗi/Word từ chối. A và Undo phải được giữ; nếu được hướng dẫn thì chọn **Kiểm tra → Quét lại** để mở lại bản đã giữ.
2. Sau một Undo, nếu quét lỗi, lệnh Undo đã hoàn tất; dùng Quét lại để xem trạng thái hiện tại. Chỉ Undo thêm khi muốn bỏ lượt sửa trước đó.
3. Khi một cảnh báo chưa định vị được, dấu của cảnh báo trước phải biến mất và UI báo chưa ánh xạ chắc. Không coi vị trí đang xem là vị trí cảnh báo mới.
4. Nếu Word Save lỗi, bản tạm chưa được coi là checkpoint chắc chắn; giữ đường phục hồi và nhật ký. Không dùng việc còn file làm bằng chứng đã lưu/khôi phục thành công. Kiểm Save lỗi bằng fake trong CI không phải hướng dẫn tạo lỗi trên tài liệu cần giữ.

## Phạm vi

Hậu kiểm dấu câu chỉ mở với payload giữ đúng dấu và ASCII space đã xác minh trong đoạn thuần đủ style/spacing; không đổi writer hoặc bảo đảm mọi Word giữ style. Dẫn chiếu chỉ nhận grammar đã ghi, không phải parser mọi văn bản pháp lý. C01 chỉ bảo vệ ô/placeholder nhận diện rõ; phần đã điền và khoảng ngoài ô vẫn cần duyệt.

**Quyền sửa Exactly/AtLeast vẫn giữ nguyên.** Bộ đọc thử nghiệm OFF, khoảng ký tay 80 pt. Nhiều section/trang ngang/cột, field/control/hình/style đặc biệt và danh sách tự động còn giới hạn. Chưa có từ điển/ngữ nghĩa đầy đủ; 33 nhóm đề xuất có điều kiện, 8 thủ công và 1 minh họa không phải chứng nhận toàn tài liệu.
