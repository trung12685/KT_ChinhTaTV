# Chính tả Việt0.1.77 — bản thử tích lũy để kiểm Word

Đây là bản thử trước phát hành, gom các thay đổi sau0.1.76. Bản0.1.76 và các tag/gói cũ giữ nguyên. Bản thử có sẵn .NET8 runtime, cần Windowsx64 và Microsoft Word desktop đã cài/khởi động/kích hoạt được bình thường. Không cần cài SDK để dùng EXE. Chưa có kết quả Word thực được ghi nhận cho mã mới.

[Tải artifact đã kiểm](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40940520669/artifacts/12927431090) · [Nguồn, CI và digest artifact](PREVIEW-0.1.77-EVIDENCE.md). Tên artifact: `ChinhTaViet-0.1.77-preview-ac6f50cc`; cần đăng nhập GitHub. Artifact được lưu30ngày từ lần chạy29/10/2026 (UTC).

## Bắt đầu

1. Tải artifact `ChinhTaViet-0.1.77-preview-<mã commit>` từ workflow Windows preview, giải nén artifact rồi giải nén **toàn bộ** ZIP bên trong vào một thư mục mới có quyền ghi. Giữ `samples`, DLL và các tệp đi kèm cạnh EXE; không ghép vào thư mục của bản cũ.
2. Mở `ChinhTa.Desktop.exe` để dùng giao diện. Mở WordDOCX, chọn chế độ phù hợp, xem từng gợi ý, kiểm bản xem trước và lưu tên mới. Không dùng tài liệu quan trọng làm lần thử đầu.
3. `build-checks.json`, `preview-manifest.json`, TXT tiến độ và SHA256 ghi bằng chứng của **đúng mã đóng gói**. CI tự động chạy Core/OpenXML, WPF và metadata; CI không chạy Microsoft Word.

## Kiểm Word bằng mẫu đi kèm

Đóng gói này ưu tiên xác minh ba nhóm độc lập: clone trung thực, hiệu chuẩn Single/1,5/khoảng đoạn, và sửa chữ giữ tên tệp/đường dẫn. Có hai cách:

- Chạy `KIEM-TRA-WORD.cmd`. Lệnh chỉ chọn ba nhóm trên và các mẫu giả đi kèm; không nhận đường dẫn tài liệu cá nhân. Cửa sổ giữ mã thoát và vị trí báo cáo; kết quả từng nhóm nằm trong `office-groups.txt`. Mã0 chỉ khi đủ cả ba nhóm PASS; mã1 nghĩa là có FAIL/NOT_RUN hoặc phiên kiểm/báo cáo chưa hoàn tất; mã2 là lệnh không hợp lệ.
- Trong ứng dụng chọn **Kiểm tra → Kiểm tra từng nhóm Word…**, chọn **Đối chiếu bản sao cô lập Exactly/AtLeast**, **Đo Single/1,5 và khoảng đoạn trên bản sao**, **Sửa chữ và giữ đường dẫn/tên tệp**, rồi chạy. Đây là kết quả các nhóm đã chọn, không thay bài Word toàn bộ.

Lệnh tương đương khi cần chạy trong script; mở PowerShell tại thư mục vừa giải nén:

```powershell
$p = Start-Process -FilePath .\ChinhTa.Desktop.exe -ArgumentList '--word-check','isolated-spacing,spacing-calibration,technical-text' -Wait -PassThru
$p.ExitCode
```

Báo cáo được giữ ở `%LOCALAPPDATA%\ChinhTaViet\OfficeGroups-...\office-groups.txt`. Mỗi nhóm có mẫu/phiên Word riêng; lỗi một nhóm không làm nhóm sau thành PASS hoặc mất kết quả. Nếu mở phiên không được thì NOT_RUN; lỗi kiểm/đóng là FAIL. Giữ file báo cáo của đúng phiên bản. Báo cáo dùng mẫu giả; các tài liệu riêng bạn mở trong giao diện không được lệnh này chọn để kiểm.

## Những điểm cần xác minh

- Clone nguồn khớp toàn XPS trước hiệu chuẩn, không reflow/đổi phông/ảnh/nét; phép đo hai lượt cùng kết quả.
- Trial dòng đơn/1,5 và trial khoảng0/6pt giữ phần chưa chọn; không dùng cỡ phông làm Single hoặc khoảng hộp mực làm6pt.
- Sửa văn xuôi riêng/chung vẫn giữ đường dẫn, tên tệp, URL, phông/đậm/gạch chân; lỗi muộn phục hồi cả lượt; một Undo và lưu/mở lại đúng; nguồn mẫu không đổi.
- Nếu có lỗi, xem loại lỗi và bước đầu tiên trong báo cáo. Gói hiện tại chưa được coi đạt Word chỉ vì có kịch bản kiểm hoặc số kiểm CI.

## Phạm vi và giới hạn

Đợt20–32 gồm PAGE nhiều chữ số, proof toàn cụm số, bản sao fullFlatOPC, đo thử theo yêu cầu, bảo toàn chữ trong đường dẫn/mã, hướng xử lý lỗi đo và đóng phiên kiểm nghiêm ngặt. Báo cáo ghi module Desktop/Core để đối chiếu các bản cùng version. Ngoài các phép đo, luồng sửa cũ vẫn có checkpoint/rollback/Undo và lưu bản sao. Danh mục33nhóm có đề xuất/8thủcông/1minh họa không phải chứng nhận toàn bộ văn bản đã đúng.

**Chưa mở quyền tự sửa Exactly/AtLeast hoặc khoảng đoạn gộp dựa vào phép đo mới.** Vùng nhiều section/trang ngang/cột, auto/grid/style chưa chứng minh, field/control/hình phức tạp vẫn có giới hạn và có thể phải làm thủ công. Bộ đọc thử nghiệmOFF, khoảng ký tay80pt theo lựa chọn người dùng, chưa thêm từ điển/ngữ cảnh chính tả.

Phiên bản Word/font/môi trường khác có thể làm clone không khớp. Khi thiếu bằng chứng, giữ cảnh báo thủ công; tránh tự nới guard để có kết quả đạt. Tiếp tục dùng các gói cũ riêng nếu cần đối chiếu.
