# Chính tả Việt 0.1.78 — bản thử tích lũy để kiểm Word

Bản thử này gom đợt 33–44 sau 0.1.77: giữ phiên đang sửa khi mở tệp mới lỗi, hoàn thiện phục hồi/Undo, giữ ô chọn và ngày chưa điền, giảm báo nhầm khoảng cuối đoạn, siết hậu kiểm định dạng. Bản 0.1.77 và các release/tag cũ giữ nguyên. Chưa có kết quả chạy Microsoft Word thật được ghi nhận cho mã mới.

Cần **Windows x64 và Microsoft Word desktop đã cài, mở và kích hoạt được bình thường**. Gói có sẵn .NET 8 runtime; không cần cài SDK để chạy EXE. Core/OpenXML, WPF và kiểm gói trong CI không thay kết quả Word thực.

## Tải và mở ứng dụng

1. Tải artifact `ChinhTaViet-0.1.78-preview-<mã commit>` của workflow **Windows preview artifact** tại kho GitHub; cần đăng nhập GitHub. Artifact chỉ có sau khi workflow tạo gói thành công.
2. Giải nén artifact, rồi giải nén **toàn bộ** ZIP `ChinhTaViet-0.1.78-preview-<mã commit>-win-x64.zip` bên trong vào thư mục mới có quyền ghi. Giữ `samples`, DLL, tài liệu và các tệp đi kèm cạnh EXE; không chép đè vào thư mục bản cũ.
3. Mở `ChinhTa.Desktop.exe`, kiểm phiên bản 0.1.78 trên cửa sổ. Mở một DOCX thử, chọn chế độ phù hợp, xem từng đề xuất và bản xem trước rồi lưu bằng tên mới.
4. Giữ `build-checks.json`, `preview-manifest.json`, TXT tiến độ và `SHA256.txt` đi cùng gói. Commit/run và số kiểm trong đó phải thuộc đúng gói đang dùng. SHA256 của ZIP ứng dụng khác digest của artifact bên ngoài.

## Chạy ba nhóm kiểm Word bằng mẫu đi kèm

Chạy `KIEM-TRA-WORD.cmd` trong thư mục vừa giải nén. Lệnh chọn rõ ba nhóm: đối chiếu bản sao cô lập, hiệu chuẩn giãn dòng/khoảng đoạn và sửa chữ giữ đường dẫn/biểu mẫu. Lệnh dùng mẫu giả, không nhận đường dẫn tài liệu cá nhân.

Cửa sổ CMD hiển thị mã thoát và vị trí chung của báo cáo; kết quả từng nhóm nằm trong `office-groups.txt`:

| Mã | Ý nghĩa |
| --- | --- |
| 0 | Đủ cả ba nhóm đã chọn và mọi nhóm PASS. |
| 1 | Có FAIL/NOT_RUN, kết quả thiếu hoặc phiên kiểm/báo cáo chưa hoàn tất. |
| 2 | Lệnh hoặc lựa chọn nhóm không hợp lệ. |

Cách chạy tương đương bằng PowerShell, mở tại thư mục vừa giải nén:

```powershell
$p = Start-Process -FilePath .\ChinhTa.Desktop.exe -ArgumentList '--word-check','isolated-spacing,spacing-calibration,technical-text' -Wait -PassThru
$p.ExitCode
```

Trong giao diện cũng có thể chọn **Kiểm tra → Kiểm tra từng nhóm Word…**, rồi chọn **Đối chiếu bản sao cô lập Exactly/AtLeast**, **Đo Single/1,5 và khoảng đoạn trên bản sao**, **Sửa chữ và giữ đường dẫn/tên tệp**. Đây là kết quả của các nhóm đã chọn, không thay bài Word toàn bộ hoặc xác nhận mọi tài liệu đã đúng.

Báo cáo ở `%LOCALAPPDATA%\ChinhTaViet\OfficeGroups-...\office-groups.txt`. Giữ nguyên báo cáo của đúng lần chạy, gồm phiên bản, mã module Desktop/Core (MVID), từng nhóm/bước và phiên bản Word/build nếu Word đã mở. Đối chiếu với commit trong `build-checks.json` để phân biệt các bản cùng version. Mỗi nhóm có mẫu và phiên Word riêng; một nhóm lỗi không làm nhóm khác thành PASS. Không mở được phiên là NOT_RUN; lỗi kiểm hoặc đóng phiên là FAIL.

## Đối chiếu kết quả trên mẫu

- Nhóm bản sao: XPS nguồn và bản sao phải khớp trước hiệu chuẩn; giữ ngắt dòng, chữ/phông, ảnh và nét. Không lấy kết quả đo từ bản sao chưa chứng minh được nguồn.
- Nhóm hiệu chuẩn: đối chiếu Single/1,5 và khoảng 0/6 pt trong cùng ngữ cảnh; giữ phần chưa chọn. Thiếu bằng chứng vẫn là chưa xác minh, không coi cỡ chữ hoặc khoảng giữa hai hộp mực là mốc 6 pt.
- Nhóm sửa chữ: sửa riêng/chung, lỗi muộn, một Undo và lưu/mở lại phải giữ đường dẫn, tên tệp, kiểu chữ và nguồn mẫu. Mẫu 0.1.78 có thêm ô trống, ô đánh dấu, trường ngày chưa điền và khoảng cuối sau ngoặc. Giữ nguyên độ rộng các ô này qua cả lượt sửa.
- Nếu có lỗi, giữ báo cáo và xem bước đầu tiên chưa đạt. Sự tồn tại của kịch bản hoặc số kiểm CI không phải kết quả PASS trên Word.

## Thử thêm luồng mở lại và định dạng hỗn hợp

Dùng các tệp thử riêng để đối chiếu, lưu bản sao cần giữ trước khi thử chuyển tài liệu:

1. Mở tài liệu A, sửa một lỗi rõ và ghi nhận nội dung trước/sau cùng khả năng Undo. Thử mở B là tệp thử không đọc được hoặc DOCX mà Word từ chối. Khi mở B thất bại, nguồn/Undo và bản làm việc của A phải còn được giữ. Quét lại A; nếu chương trình hướng dẫn **Kiểm tra → Quét lại** để mở phiên đã giữ thì dùng chính lệnh đó. Nếu báo chưa xác minh phục hồi, giữ đường bản phục hồi và nhật ký, không ghi nhận lượt này đã đạt.
2. Trở lại A, thử một Undo và lưu tên mới rồi mở lại để so nội dung. Sau đó mở B hợp lệ: phiên mới không được mang Undo của A sang. Tệp A nguồn phải giữ nguyên; đối chiếu lại bản nguồn hoặc SHA256 đã ghi trước khi thử.
3. Trong đoạn được ứng dụng nhận diện đủ điều kiện sửa, tạo dấu phẩy có cỡ 14 pt, đậm/gạch chân giữa chữ thường 13 pt, kèm khoảng trắng cần sửa quanh dấu đó. Chọn cùng một đề xuất giãn dòng/định dạng được phép; sau sửa, Undo và lưu/mở lại, so các thuộc tính của dấu phẩy mà mình chưa duyệt đổi. Nếu không có đề xuất cho phép thì ghi nhận chưa đủ điều kiện, giữ thủ công.

Bản này tăng bằng chứng giữ style/cỡ cho dấu câu còn giữ trong **lượt sửa chung đã có hậu kiểm body/case/style**. Chưa có fingerprint cho mọi lượt chỉ sửa dấu câu và chưa chứng minh Word thực luôn giữ định dạng khi thay `Range.Text`. Không dùng một lần sửa dấu câu đơn thuần để kết luận đã kiểm toàn bộ định dạng; trường hợp mất thuộc tính trong phạm vi đã chứng minh phải bị từ chối/phục hồi.

## Phạm vi cần nhớ

C01 giữ ô vuông trống hoặc có đúng một dấu `X`, `x`, `✓`, `✔`; giữ các trường trống/placeholder của mẫu ngày nhận diện rõ, kể cả nhãn NFC/NFD. Phần đã điền và khoảng thừa ngoài ô vẫn có thể được đề xuất sửa. Đây chưa phải bộ nhận mọi biểu mẫu hoặc xác minh giá trị ngày.

C25 hỗ trợ khoảng ASCII cuối đoạn sau một dấu `.?!;` khi phía trước là một cặp quote/ngoặc đơn cân bằng, tiền tố rõ và ít nhất hai từ thường bên trong. Giữ khoảng trong ngoặc, trước dấu câu chưa chọn và dấu kết đoạn/ô. Cặp lồng/thiếu, mã, số, viết tắt hoặc chữ hoa chưa đủ điều kiện vẫn ngoài phạm vi.

**Chưa mở thêm quyền tự sửa Exactly/AtLeast hoặc khoảng đoạn gộp từ phép đo mới.** Bộ đọc thử nghiệm vẫn OFF; khoảng ký tay 80 pt giữ theo lựa chọn người dùng. Vùng nhiều section/trang ngang/cột, auto/grid/style, field/control/hình phức tạp vẫn có giới hạn và có thể cần thủ công. Chưa thêm từ điển/ngữ cảnh để xác nhận đầy đủ chính tả tiếng Việt. Danh mục 33 nhóm đề xuất có điều kiện, 8 thủ công và 1 minh họa không phải chứng nhận toàn bộ tài liệu.
