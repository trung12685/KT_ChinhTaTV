# Đợt 33–36 — giữ phiên làm việc và giảm sửa nhầm

Ngày 30/10/2026 (UTC+7), tiếp tục từ `05ce3a195d3878d9da931b614ce24e394cda5b06` trên nhánh `feature/isolated-spacing-proof-batch22`. Mã phát triển chuyển sang 0.1.78 để phân biệt với artifact 0.1.77 đã tạo. Tích lũy mã và kiểm tra; chưa xuất ZIP mới. Giữ bộ đọc thử nghiệm tắt, khoảng ký tay 80 pt và quyền sửa Exactly/AtLeast hiện có.

## 33. Mở tài liệu mới qua bản chuẩn bị

Trước đây `Open` đóng Word, xóa Undo, đổi Source/Changed rồi mới sao chép và mở tài liệu mới. Tệp không đọc được làm mất phiên cũ trong giao diện; DOCX Word từ chối còn có thể thay bản recovery cũ trước khi mở thành công. Giao diện đã hỏi bỏ thay đổi trước khi mở, nhưng chuyển phiên thất bại vẫn cần giữ khả năng phục hồi.

Luồng mới kiểm đường dẫn thuộc sở hữu phiên, chuẩn bị bản sao và xác minh bytes trước khi đóng. Trạng thái nguồn/Changed/Undo và quan sát của phiên cũ chỉ được thay sau khi mở thành công. Nếu mở bản mới thất bại, thử phục hồi bản trước; lỗi phục hồi giữ backup và checkpoint không được coi đã chắc chắn. Lỗi dọn tệp tạm được ghi riêng. Không ghi vào tài liệu nguồn hoặc nhận bản working/Undo của chính phiên làm nguồn mới.

Nếu lỗi đóng khiến phiên cũ không còn kết nối nhưng checkpoint vẫn đã xác minh, **Kiểm tra → Quét lại** cho phép thử mở lại chính bản được giữ. Chưa xác minh checkpoint thì không tự mở. Word mở bản làm việc ở chế độ chỉ đọc bị từ chối; không suy có quyền ghi chỉ từ tham số yêu cầu khi mở. Nguồn/Changed/Undo của bản trước giữ qua lần thử lại.

## 34. Phân biệt lỗi thao tác với kết quả rollback

`AggregateException` có thể chứa nhiều lỗi thao tác mà bản cũ vẫn đã phục hồi đúng. Suy mọi aggregate là mất phục hồi làm sai trạng thái checkpoint, chặn reconnect hoặc giữ thư mục không cần thiết. `CheckpointSwap` nay ghi kết quả của đúng lần chuyển tệp; lỗi chưa phân loại không tự thành kết quả đã xác minh. Lỗi phục hồi thật giữ riêng đường rollback và cả hai nguyên nhân. Kết quả của giao dịch ngoài không suy từ trạng thái của exception do giao dịch bên trong để lại.

Các luồng Undo và sửa đường kẻ dùng kết quả phục hồi rõ ràng. Việc dọn backup là bước riêng, không được làm mất lỗi thao tác gốc hoặc biến lượt đã xác minh thành lượt chưa áp dụng.

Lỗi xóa backup bước do file bị khóa hoặc thiếu quyền được trả thành cảnh báo giữ file; mọi backup còn lại vẫn được thử dọn, lượt sửa đã kiểm vẫn có một Undo. Nếu rollback thật thất bại thì giữ toàn bộ backup và cả hai lỗi. Khi trạng thái bản làm việc chưa chắc, Changed được bật để không bỏ qua cảnh báo chưa lưu lúc mở tài liệu khác hoặc đóng ứng dụng.

## 35. Giữ chuỗi kỹ thuật còn bỏ sót

Ba ví dụ đã tái hiện trước sửa: `print("Báo  cáo")` bị gợi ý gộp khoảng; `Console.WriteLine("kế hoạch kế hoạch");` bị gợi ý xóa cụm lặp; đường dẫn `/tài liệu tài liệu` và email có tên miền Unicode tổ hợp chưa được giữ đúng.

Nhận diện được bổ sung cho lời gọi có `(` sát tên hàm và đối số đầu là chuỗi, tệp ngay thư mục gốc POSIX có kết thúc rõ, cùng combining mark hợp lệ trong tên miền email. Gợi ý chuyển thủ công cho cả đoạn, vẫn hiện cảnh báo và giữ nguyên nguồn. Văn xuôi như `ngày/đêm`, `/ ta /` hoặc `Cần duyệt ("...")` tiếp tục dùng luật cũ. Đây là guard có giới hạn, chưa phải parser mọi ngôn ngữ lập trình/tên tệp và chưa có từ điển ngữ nghĩa.

Đường dẫn chỉ có một segment còn cần đầu đoạn, quote/ngoặc hoặc từ dẫn rõ như `Mở`, `tệp`, `file`, `đường dẫn`. Không coi dấu `/` sau khoảng trắng bất kỳ là đường dẫn: dòng mẫu `Số:  /BC-ABCD  nội dung.` tiếp tục giữ ô số trống và báo đúng khoảng thừa ngoài trường. Hồi quy này đã phát hiện và sửa trong đợt, trước khi chốt mã.

## 36. Đóng độc lập và bỏ trang xem trước lỗi thời

Trước đây XPS bị khóa hoặc đóng lỗi có thể bỏ qua `session.Dispose()` khi cửa sổ đóng. Một lỗi release document cũng có thể bỏ qua Quit application. Luồng mới thử đóng và giải phóng từng tài nguyên đã sở hữu độc lập, giữ mọi nguyên nhân; không đụng Word hoặc tài liệu của phiên khác.

Khi quét lỗi, preview trước đó được tách khỏi viewer cùng danh sách lỗi/coverage. Lỗi dọn preview được ghi riêng, không thay lỗi thao tác chính. Handle/path chưa dọn được giữ để thử lại. Kiểm WPF dùng viewer thật và file bị khóa không cho xóa, bên cạnh các tổ hợp lỗi callback.

## Kiểm chứng và phần tiếp theo

Core/OpenXML trên Linux: **5469 kiểm qua**, tăng **126** từ mốc 5343. Build DesktopChecks Release qua, 0 warning/error; WPF runtime chờ CI Windows trên đúng mã. Harness riêng của cleanup kết nối có 7 kiểm managed-fake qua; không cộng số này vào Core. Kiểm Core dùng tệp thật và callback giả. Microsoft Word thực vẫn NOT_RUN. Chưa dùng những kiểm này để xác nhận mọi DOCX, Office 2021 hoặc mở quyền tự sửa Exactly/AtLeast.

Bản tiếp theo trên máy có Word cần kiểm thêm: mở tài liệu A đã sửa, thử tài liệu B hỏng/không đọc được, quét và lưu lại A; mở B hợp lệ rồi đảm bảo Undo cũ không còn tác động; kiểm đóng khi preview bị khóa; chạy lại ba nhóm trong launcher của đúng gói sau khi tích lũy đủ và tạo bản thử mới.
