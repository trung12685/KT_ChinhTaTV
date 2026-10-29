# Đợt 27–29 — bảo toàn dữ liệu chữ và xử lý lỗi đo

Ngày30/10/2026 (UTC+7). Tiếp tục mã0.1.77 từ `f3898b5e78ee196d30a2b7bb4fa78135c1df3fb5` trên nhánh `feature/isolated-spacing-proof-batch22`. Thêm các phần có thể làm độc lập trong khi phép hiệu chuẩn cần được kiểm chứng trên Word thật. Không đổi quyền sửa Exactly/AtLeast, bộ đọc mặc định, khoảng ký tay80pt, main hoặc tag/gói đã phát hành.

## 27. Không sửa nhầm tên tệp, đường dẫn và dữ liệu mã

Rà soát phát hiện `RepeatedTextRules` có thể đề xuất xóa cụm lặp trong `C:\tài liệu tài liệu\bao-cao.docx`, còn quy tắc gộp dấu cách có thể làm đổi `C:\Báo  cáo\mau.docx`. XML cho phép sửa chữ không chứng minh những ký tự đó là lỗi văn xuôi.

`TechnicalTextContext` nhận diện các dạng rõ: đường dẫn drive/UNC/relative/rooted, tên tệp với đuôi đã biết, URI/URL/email và code/literal có marker. Có hỗ trợ đường dẫn Unix chứa khoảng trắng và dấu Unicode tổ hợp. Không coi mọi dấu `/` là kỹ thuật: `và/hoặc`, `ngày/đêm`, văn xuôi `/ ta /` không bị chặn chỉ vì có slash.

Sau khi thực sự tìm thấy gợi ý chữ, Checker chuyển cả đoạn có dạng kỹ thuật sang một cảnh báo thủ công, giữ nguyên văn và không tích sửa. Vùng pha trộn văn xuôi với dữ liệu kỹ thuật vẫn bảo thủ theo cả đoạn; chưa có parser chứng minh riêng quyền từng token. Đoạn kỹ thuật không có gợi ý không bị thêm cảnh báo vô cớ. `RepeatedTextRules` trực tiếp cũng giữ gate này. Các đề xuất thể thức/phông độc lập không được tự chuyển thành sửa chữ.

Kiểm cả ba chế độ, vị trí UTF16/emoji/dấu tổ hợp, khoảng trắng có ý nghĩa, ví dụ chính tả minh họa nằm trong tên tệp, bảo toàn toàn dòng, văn xuôi vẫn sửa được và lựa chọn cũ mất quyền khi context thành kỹ thuật. Đây là giảm báo/sửa nhầm, không phải từ điển chính tả hoặc parser mọi kiểu đường dẫn.

## 28. Lỗi đo được phân loại, không biến lỗi toàn vẹn thành thiếu bằng chứng

`SpacingDiagnosticException` chỉ chứa thông điệp an toàn theo loại: thiếu/cũ bản xem trước, ngoài phạm vi, lỗi thao tác, lỗi toàn vẹn, đóng/khôi phục phiên chưa hoàn tất, hoặc nguồn đổi. Giao diện đưa hướng xử lý tương ứng thay vì luôn bảo quét lại. Không hiện raw exception/inner hoặc chữ/FlatXML của tài liệu. Đường phục hồi chỉ nhận thư mục tạm riêng `ChinhTa-Isolated-<GUID>` hợp lệ.

`SpacingDiagnosticGuard.Optional` chỉ chấp nhận **typed UnsupportedEvidence** thành kết quả khoảng đoạn chưa đủ bằng chứng. Lỗi COM, I/O, plain InvalidOperation, integrity, cleanup hoặc nguồn đổi phải truyền tiếp và không hiển thị kết quả đạt. Tạo trial rồi audit thất bại và VerifyFile không giữ payload đều là lỗi toàn vẹn. Nguồn ngoài phạm vi passive hoặc style mặc định trùng là giới hạn đo, không hướng người dùng sửa Word.

Nếu cleanup lỗi, mọi lỗi cũ được giữ, folder phục hồi vẫn thuộc phiên riêng, và lỗi đọc nguồn sau cùng không làm mất hướng dẫn cleanup. Nếu xác nhận XML nguồn đổi, kết quả bị hủy và cache bị xóa. Báo cáo cũng bác các trạng thái dưới/trong/trên dòng hay đạt mốc khoảng đoạn nếu trạng thái trái các vector/hiệu ứng thực đưa vào.

## 29. Nhóm kiểm Word riêng cho sửa chữ và giữ dữ liệu kỹ thuật

Menu **Kiểm tra từng nhóm Word… → Sửa chữ và giữ đường dẫn/tên tệp**, tổng12nhóm. Fixture riêng có một đoạn văn xuôi ba gợi ý và các dòng đường dẫn/URL/tên tệp có khoảng/lặp/`con đo` phải giữ. Kiểm chọn riêng/chung, quét lại, lỗi sau ghi, một Undo, lưu/mở lại, hash nguồn và đậm/gạch chân theo CP thực của gợi ý. Không lấy chỉ số chuỗi toàn story làm Word CP.

Kiểm WPF/Core xác minh schema, mọi part ngoài main và seed không đổi, Select All không tích cảnh báo kỹ thuật, kết quả toàn story đúng và stale selection bị chặn. Kịch bản thực thi Word đã viết và biên dịch; **Word runtime chưa chạy ở môi trường Linux**. CI Windows chạy WPF không được coi là Word PASS.

## Kiểm chứng và việc tiếp theo

Chạy runner chuẩn `dotnet run --project tests/ChinhTa.Checks -c Release` và trên Windows `dotnet run --project tests/ChinhTa.DesktopChecks -c Release`. Bằng chứng cuối đợt ghi theo commit triển khai, không cộng harness phụ vào số runner chính.

Kết quả trên commit triển khai `918c9e41ba129e2bdda02e8a6fe1ae01e41cd54c`: **5.307 assertion Core/OpenXML đạt trên Linux**, tăng164 so với5.143; build DesktopChecks/Desktop Release không cảnh báo hoặc lỗi. **Windows CI SUCCESS**, [run40921878984](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40921878984), [job123617017609](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40921878984/job/123617017609). Core/OpenXML, WPF/build và ghi evidence đều đạt. Không suy số assertion Windows từ Linux và không coi CI là Word thực. [Bằng chứng](handoff-evidence/batch27-29-evidence.json).

Cần tiếp tục xác minh hai nhóm clone/hiệu chuẩn và nhóm sửa chữ trên máy Windows có Word, xử lý sai khác thật nếu có; sau đó mới thiết kế writer Exactly/AtLeast với hậu kiểm, rollback/Undo/lưu-mở. Trong lúc chưa có bằng chứng Word, vẫn có thể xử lý các lỗi thể thức độc lập, mở rộng tập ngoại lệ lỗi chữ cơ bản và giảm báo nhầm. Không tự suy `xẽ→sẽ` hoặc `giành/dành` khi chưa có dữ liệu.
