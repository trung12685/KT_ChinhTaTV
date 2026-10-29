# Đợt 69–72 — kiểm Word độc lập, dữ liệu literal, dẫn chiếu và đích lưu

Ngày 30/10/2026 (UTC+7), tiếp tục từ `86300283f56cb9ce9dfaa5ce7caf83db78de672a` trên nhánh `feature/isolated-spacing-proof-batch22`. Mã phát triển **0.1.81**, chưa tạo ZIP mới. Bản thử 0.1.80 từ nguồn `8e9cb3b19eac0cfe300fb206b27065219ba6c3a9` và các mốc cũ giữ nguyên.

## 69. Chạy riêng bài dấu câu và bookmark trên Word

Nhóm `technical-text` chứa nhiều bài nối tiếp; nếu bài đầu lỗi, các bài sau chưa được chạy. Bổ sung hai lựa chọn độc lập `retained-marks` và `annotated-text`, trỏ trực tiếp đến runner mẫu hiện có, mỗi nhóm dùng phiên Word/thư mục riêng qua runner chung. Lỗi hoặc NOT_RUN của nhóm trước vẫn được ghi, không chặn nhóm sau và không được đổi thành PASS nhờ nhóm sau đạt.

Mã mới có **15 nhóm Word**, giữ thứ tự 13 nhóm cũ và chuỗi `technical-text` để tương thích. Chọn nhóm bằng ID tường minh, không mở token `all`; danh sách trong cửa sổ chọn có thanh cuộn và giới hạn chiều cao theo vùng làm việc. Lệnh kiểm ba nhóm trong gói cũ giữ nguyên. Hai ID mới chỉ có trong mã phát triển 0.1.81, **chưa có trong EXE 0.1.80**.

Sau khi build đúng mã mới, có thể chọn riêng:

```powershell
$p = Start-Process -FilePath .\ChinhTa.Desktop.exe -ArgumentList '--word-check','retained-marks,annotated-text' -Wait -PassThru
$p.ExitCode
```

Kết quả vẫn theo PASS/FAIL/NOT_RUN, phiên bản/MVID, từng nhóm/bước và lỗi thực tế. Hồi quy định tuyến/lệnh bằng đối tượng giả không thay kiểm Word thật.

## 70. Giữ chuỗi dữ liệu trong mảng JSON

Tái hiện trước sửa: `["Báo  cáo", "kế hoạch kế hoạch"]` được cấp C01/C13 làm thay giá trị; `["con đo", "Nội dung  báo cáo"]` còn có gợi ý thay chữ minh họa trong chuỗi dữ liệu. Nay đọc token cho đoạn có dạng mảng JSON đầy đủ, giữ nguyên giá trị, khoảng trắng, NFC/NFD và source offset; chỉ cảnh báo thủ công nếu có ứng viên sửa. Mảng hợp lệ không có ứng viên không tự sinh cảnh báo.

Không cấp ngoại lệ mới cho mảng nông sai cú pháp, ngoặc vuông văn xuôi hoặc mảng nhúng trong câu. Parser có ngân sách độ sâu 64; khi prefix JSON đã parse tới mức đó, giữ phần còn lại thủ công, không dùng lỗi giới hạn để mở quyền sửa và không tuyên bố đã parse toàn phần sau. Các đoạn văn xuôi độc lập trước/sau vẫn được sửa; kế hoạch cũ bị từ chối nếu cùng vị trí/khoảng chữ chuyển thành dữ liệu JSON. Hai dòng JSON được thêm vào mẫu Word sửa chữ, chưa chạy Word thật.

## 71. Giữ mục nằm trong khoảng dẫn chiếu La Mã

Tái hiện trước sửa: nhóm I/II/IX có `Xem từ mục IV đến mục X.` vẫn được đổi IX → III vì chỉ hai đầu mút được giữ. Nay khoảng explicit theo cùng cue bảo vệ cả nhãn cũ và nhãn dự kiến nằm giữa; canonical 1–3999, cho đầu mút đảo chiều và các dạng chữ/khoảng ngang NFC/NFD đã hỗ trợ. Không đổi source spelling/CP hoặc chuyển giá trị La Mã sang nhóm số Ả Rập/chữ cái.

Danh sách dùng dấu phẩy/“và” vẫn chỉ giữ các nhãn được nêu; không tự biến thành khoảng. Nhãn không canonical, mixed family/cue, ngắt đoạn hoặc phần dẫn chiếu chưa nhận diện vẫn ngoài mở rộng này. Thêm 5 fixture La Mã vào nhóm Word `outline-references`, giữ 9 fixture số cũ: IX giữ thủ công, nhánh số con 9 → 3 vẫn được duyệt; quét không đổi chữ/dirty/Undo/hash nguồn. Kiểm schema/mô hình chưa thay kết quả Word.

## 72. Không công bố bản sao vào thư mục sẽ bị dọn khi đóng phiên

Harness gọi SaveCopy/SaveProgressReport thực tế với đối tượng Word giả đã tái hiện: lưu sang tên mới dưới thư mục riêng của phiên được coi thành công, nhưng Dispose bình thường xóa cả thư mục và bản sao cũng mất. Thêm guard từ chối đích ở trong thư mục riêng trước Save/tạo stage; khi từ chối giữ Source, bytes, dirty/checkpoint/Undo và kết nối.

Đối chiếu cả đường dẫn entry được thay và parent đã resolve, không chỉ target của symlink tệp cuối. Trên Windows, mở handle thư mục hiện có gần nhất để nhận đúng alias dấu chấm/khoảng cuối, extended path, short name và reparse; lỗi không xác minh được thì không coi đích là độc lập. Các phần thư mục còn thiếu chỉ được nối sau parent đã chứng minh.

VerifiedSaveCopy nhận danh sách thư mục cần giữ ngoài phạm vi dọn, kiểm trước tạo stage và lần nữa trước File.Move. Các lượt staging nội bộ hợp lệ không truyền guard này. Đích độc lập, kể cả thư mục tạm khác và tên sibling chung prefix, vẫn được lưu và tồn tại sau Dispose. Không tuyên bố chống mọi thay đổi filesystem cạnh tranh giữa lần kiểm cuối và rename. Symlink/8.3 được ghi rõ nếu runner không có khả năng tạo alias.

## Kiểm chứng và phần còn chờ

Core/OpenXML cục bộ: **8681 kiểm qua**, tăng **319** từ 8362: 181 JSON, 106 La Mã và 32 đích lưu trên Linux. Windows có thêm nhánh kiểm alias riêng; không suy tổng từ Linux. Harness riêng qua 88 kiểm SaveCopy/report/Dispose, 18 kiểm nhóm Word độc lập và 90 kiểm fixture dẫn chiếu (45 mới); số thuộc runner tương ứng sau tích hợp, không cộng lặp vào Core. Fixture sửa chữ thực tế với hai dòng JSON đã qua 11 kiểm schema/part/selection/expected-story; mỗi dòng kỹ thuật có ứng viên lỗi thuộc phạm vi để đối chiếu đúng một cảnh báo thủ công.

Build DesktopChecks Release: **0 warning/error**. Windows CI được cập nhật sau khi hoàn tất. Word thực vẫn **NOT_RUN**; quyền Exactly/AtLeast, bộ đọc thử nghiệm OFF, khoảng ký tay 80 pt và danh mục 33/8/1 giữ nguyên. Không xuất ZIP mới trong đợt này.
