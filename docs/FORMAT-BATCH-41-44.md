# Đợt 41–44 — giữ bằng chứng định dạng và biểu mẫu

Ngày 30/10/2026 (UTC+7), tiếp tục từ `4429ec2` trên nhánh `feature/isolated-spacing-proof-batch22`. Tích lũy mã 0.1.78, chưa xuất ZIP mới. Giữ bộ đọc thử nghiệm tắt, khoảng ký tay 80 pt và quyền sửa Exactly/AtLeast hiện có. Danh mục vẫn 33 nhóm đề xuất có điều kiện, 8 thủ công, 1 minh họa.

## 41. Dấu câu không đổi vẫn phải giữ bằng chứng style/cỡ

Đã tái hiện bằng Core: đề xuất `  ,` → `, ` làm dấu phẩy cũ dịch khỏi hai mép chung. Phép chiếu trước chỉ giữ prefix/suffix/case, nên dấu đó bị thành glyph không có ràng buộc nguồn. Trong lượt sửa chung giãn dòng/định dạng, hậu kiểm có thể chấp nhận dấu phẩy 14 pt, đậm, gạch chân thành 13 pt thường dù không duyệt đổi các thuộc tính ấy.

Phép chiếu nay giữ đúng một dấu câu không đổi nếu hai phía chỉ có ASCII spaces và cùng một glyph duy nhất. Bằng chứng style và cỡ đều giữ nguồn. Không đoán LCS hoặc ánh xạ nhiều dấu, tab, thay dấu hoặc nội dung phức tạp; chữ mới vẫn không nhận style nguồn giả. Kiểm thực hiện qua `PunctuationSpaceRules`, `CasePreservationPlan`, `AtomicTextBatch` và `StagedRepairBatch`, gồm hậu kiểm muộn buộc phục hồi cả lượt.

Đây là siết các fingerprint đang có của lượt sửa chung body/case/style. Chưa thêm fingerprint cho mọi lượt chỉ sửa dấu câu, chưa chứng minh Word thực sẽ giữ dấu đó khi `Range.Text` thay đổi. Nếu mất style trong phạm vi đã có proof, phải từ chối/rollback; không thêm writer mới khi chưa có bằng chứng Word.

## 42. Đọc cỡ chữ theo CP thực

`ReadSizes` trước có thể nhận các child range ngoài parent hoặc chồng/gap nếu chữ nối lại vẫn đúng. Cỡ đó chưa đủ chứng minh thuộc vùng được duyệt. Nay kiểm Start/End từng child, thứ tự và coverage liên tục trong parent, chuỗi con đúng nguồn, rồi đọc lại Start/End/Text của parent sau mọi lần đọc font.

Không lấy độ dài UTF-16 làm độ dài Word CP. Kiểm dương có emoji một CP và dấu tổ hợp nhiều CP; kiểm âm có ngoài biên/chồng/gap, thiếu đuôi, dữ liệu rỗng/đổi, cỡ không hợp lệ và COM mất kết nối. Fakes cũ bổ sung biên theo API Word thật; không nới guard vì dữ liệu giả. Đây là sửa tính đúng, không bật tối ưu đọc.

## 43. Ô đã đánh dấu và mẫu ngày NFC/NFD

Giữ đệm của `[    X    ]`, `[    x    ]`, `[    ✓    ]`, `[    ✔    ]`, chỉ đúng một dấu được hỗ trợ giữa ASCII spaces. Dấu `?`, nhiều dấu, chữ trong ngoặc hoặc cặp chưa cân bằng không được suy là checkbox.

Nhận nhãn `Số`, `ngày`, `tháng`, `năm` dạng NFC/NFD bằng mẫu trên chuỗi gốc, không chuẩn hóa làm đổi offset. Mẫu ngày bằng chữ có thể còn trống/placeholder như `Ngày 03 tháng        năm 2026` hoặc `ngày        tháng        năm        `. Chỉ giữ vùng blank/placeholder được nhận diện; trường đã điền và khoảng ngoài ô vẫn có thể sửa. Không suy giá trị ngày hoặc coi số bất kỳ là ngày hợp lệ.

Dòng địa danh/ngày kèm chú thích vẫn cần điều kiện header đã nhận diện như trước; không mở ngoại lệ này cho đoạn thân tùy ý. Hồi quy giữ các mẫu số/ngày cũ, nhãn sai, văn xuôi có từ “ngày”, nhiều ô, chọn cũ với cùng bytes/CP, và mọi profile. Fixture nhóm Word `technical-text` bổ sung ô đã đánh dấu, tháng chưa điền và nhãn số NFD để kiểm qua sửa riêng/chung, Undo, lỗi muộn và lưu/mở; chưa chạy Word thật.

## 44. Giữ lỗi chính khi giải phóng tài nguyên hậu kiểm

`SelectedFormatVerification` trước release paragraph format rồi range trong cùng `finally`; lỗi đầu bỏ qua range và có thể thay lỗi quan sát/định dạng gốc. Nay thử cả hai độc lập, giữ nguyên lỗi duy nhất hoặc tổng hợp lỗi quan sát trước các lỗi cleanup theo thứ tự. Proof chưa sạch vẫn chặn commit và đi qua rollback hiện có.

Kiểm dùng đúng verifier/reader với managed ranges: đọc phông đúng/sai kết hợp mọi tổ hợp lỗi release, đúng danh tính/thứ tự lỗi, và giao dịch trang thật của Core phải phục hồi thay vì commit. Các kiểm này không tạo phiên Word.

## Kiểm chứng

Core/OpenXML trên Linux: **5775 kiểm qua**, tăng **146** từ mốc 5629. Build DesktopChecks Release: 0 warning/error. Harness reader managed có 95 kiểm qua, gồm boundary mới và hồi quy reader/format/cleanup; không cộng vào số Core.

CI Windows của mã triển khai `9bd2918dc4443ffcc3041707bcd38817cdfc47fe`: [run 40949079333 — Success](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40949079333), [job 123712070425](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40949079333/job/123712070425). Core/OpenXML, WPF/build, ghi bằng chứng và lưu log đều thành công, gồm fixture Word bổ sung và toàn bộ lớp kiểm mới đã đăng ký. Số Windows thực nằm trong `check-evidence.json`/log của run; không suy từ số Linux. Workflow này chỉ lưu kết quả kiểm, không tạo ZIP ứng dụng. Cập nhật tài liệu sau commit trên không thay code đã kiểm.

Word runtime vẫn **NOT_RUN**. Các thay đổi thuộc mã phát triển 0.1.78; gói 0.1.77 và main giữ nguyên. Chưa dùng kiểm Core/WPF để mở quyền tự sửa Exactly/AtLeast hoặc chứng nhận mọi DOCX.
