# Đợt30–32 — một bản thử Windows để xác minh Word

Tiếp tục mã0.1.77 sau `9efbdd9e715c65d6ecd2553a2b42061b4a25ce9b`, nhánh `feature/isolated-spacing-proof-batch22`. Mục tiêu đưa các thay đổi tích lũy thành một gói có thể chạy trên máy Windows có Word, đồng thời sửa các lỗi thực đã tìm thấy. Không đổi main, tag/gói phát hành cũ, bộ đọc mặc định,80pt ký tay hoặc quyền sửa Exactly/AtLeast.

## 30. Phân loại đúng dữ liệu hỏng và giữ tên tệp thông dụng

Nếu chỉ `[Content_Types].xml` trong DOCX bị thay thành XML hỏng, `VerifyFile` trước đây có thể để lọt XmlException ra nhóm lỗi thao tác. Nay lỗi đó là typedIntegrity và không được optional-gap fallback nuốt. Kiểm bằng ZIP thực giữ mọi part khác, phá riêng manifest và xác minh loại lỗi/không fallback.

Danh sách đuôi tên tệp kỹ thuật bổ sung Word macro/template,RTF,OpenDocument cùng các biến thể Excel/PowerPoint thông dụng. Gợi ý gộp khoảng hoặc xóa cụm lặp trong tên chỉ có đuôi mà không có drive/path vẫn thủ công, kể cả chữ hoa và dấu tổ hợp. Văn xuôi thường vẫn dùng luật cũ; đây chưa phải parser mọi tên tệp.

## 31. Lệnh kiểm Word và điều kiện đóng phiên

`--word-check isolated-spacing,spacing-calibration,technical-text` chỉ chạy các nhóm mẫu giả được chọn. Parser kiểm đủ trước khi gọi Word; không nhận tên tài liệu, không mở mặc định tất cả nhóm, không tự merge kết quả thiếu. Mã0 cần đúng toàn bộ coverage và mọi nhóm PASS; mã1 là lỗi/NOT_RUN/kết quả thiếu hoặc không ghi được; mã2 là lệnh không hợp lệ. Khởi động thường vẫn mở GUI, chế độ lệnh không tạo MainWindow.

Rà soát cũng phát hiện `WordSession.Dispose` thường có thể bỏ qua COM lỗiClose/Quit để phục vụ reconnect, nên bài focused trước đây chưa đủ chắc để suy đóng sạch. `DisposeCheckSession` mới tách riêng cho phiên kiểm: detach ownedhandles, thử mọiClose/Release/Quit, giữ mọi lỗi hiện tại/lịch sử/checkpoint chưa chắc, không xóa folder phục hồi khi chưa xác nhận sạch. Lỗi cleanup chuyển nhóm thànhFAIL hoặcNOT_RUN trong trường hợp chưa mở được, không thể PASS. NormalDispose của tài liệu người dùng giữ hành vi cũ.

Báo cáo mẫu ghi moduleID Desktop/Core, phiên bản Word/build trong từng nhóm và đường báo cáo thuộc LocalAppData. Có kiểm bằng tài nguyên giả cho lỗi Close/Quit/Release, lỗi kép, lỗi xóa, lịch sử lỗi, checkpoint chưa chắc, detach/doublecall và guard không tác động phiên thường. Đây là kiểm logic và WPF; chưa phải Word-runtime PASS.

## 32. Một artifact preview có thể tái lập

Workflow `windows-preview.yml` chỉ chạy trên nhánh `preview/windows-*` hoặc dispatch; push nhánh feature bình thường không xuất gói. Quyền contents:read, không tạo release hoặc tag. Gói self-containedWindowsx64, có mẫu, tài liệu từ danh mục code thực, `build-checks.json`, TXT tiến độ, hướng dẫn [PREVIEW-0.1.77.md](PREVIEW-0.1.77.md) và launcher [KIEM-TRA-WORD.cmd](KIEM-TRA-WORD.cmd).

Chạy Core/OpenXML và WPF thật, kiểm số dương/đúngrunner/exitcode; metadata gắn sourcecommit và runURL. Kiểm EXEWindowsAMD64 và WindowsDesktop runtime đi kèm. EXE đã publish phải thoát mã2 trong20giây với nhóm không hợp lệ; không gọi Word ở smoke này. SauZIP, giải nén vào thư mục sạch rồi kiểm lại metadata/checklist/report cùng tập file và từngSHA256. Công bố số kiểm, SHA ZIP và artifactidentity trong summary của chính run. Chỉ một artifact có ZIP và chứng cứ; thời hạn lưu30ngày.

`scripts/PreviewMetadata` có kiểm giả lập dương/âm cho metadata, code-report hiện tại, file thiếu/thừa/đổi, traversal/reparse và manifest; workflow mới là bằng chứng góiWindows thực. Các con số harness không cộng vào runnerCore/WPF.

## Điều kiện tiếp theo

Người kiểm trên máy Windows có Word chạy launcher hoặc chọn ba nhóm trong menu, giữ đúng `office-groups.txt`. Chỉ sau khi clone/hiệu chuẩn/giữ nguồn/cleanup thực được xác minh mới thiết kế writer Exactly/AtLeast/khoảng đoạn gộp. Gói preview chưa biến CI thành kết quả Word, chưa chứng nhận mọi văn bản hoặc chính tả/ngữ nghĩa tiếng Việt hoàn chỉnh.
