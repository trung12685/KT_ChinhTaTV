# Đợt 45–47 — giữ định dạng riêng C14 và dẫn chiếu mục

Ngày 30/10/2026 (UTC+7), tiếp tục từ `257635fa15b0730af8399b188cb14a35f9879898`, nhánh `feature/isolated-spacing-proof-batch22`. Mã phát triển chuyển sang 0.1.79 để phân biệt với gói 0.1.78 đã gửi. Chưa xuất ZIP mới; giữ main, gói/tag cũ, bộ đọc thử nghiệm OFF, khoảng ký tay 80 pt và quyền sửa Exactly/AtLeast.

## 45. Hậu kiểm khi chỉ sửa dấu phẩy/chấm phẩy

Đã tái hiện bằng Core: riêng C14 `  ,` → `, ` không tạo probe style trước đây. Lượt sửa có thể có chữ đúng nhưng mô hình vẫn commit khi dấu phẩy 14 pt đậm/gạch chân bị đổi thành thường. Lượt chọn chung body/style đã có proof nên không mắc khoảng trống này; đây chưa phải xác nhận Word thật làm mất định dạng.

Thêm lượt thứ hai trong `CasePreservationPlan.Regions`: giữ owner body/title/signature cũ trước; C14 chưa được phủ mới nhận probe toàn đoạn từ đúng một vùng XML/native đã chứng minh. Chỉ nhận một dấu phẩy/chấm phẩy không đổi giữa ASCII spaces. Nhiều C14 trong cùng đoạn dùng một probe; sửa chữ khác đã duyệt trong đoạn vẫn được chiếu. Cần đủ style glyph và paragraph spacing đã đọc; không tự áp Times New Roman hoặc giãn dòng mới.

Ghép phông/heading/paragraph/spacing chưa có owner được hỗ trợ bị từ chối trước sửa; không mở chung `Compatible` cho mọi role. Vùng cắt qua đoạn, nguồn thiếu/mơ hồ hoặc có control nội bộ không đủ proof mới. Probe không tạo finding/approval/write phụ; writer Word giữ nguyên. Đường hậu kiểm sau lưu, sau Line/page và rollback dùng lại logic có sẵn.

Fixture `technical-text` có dấu phẩy riêng 14 pt đậm/gạch chân giữa khoảng 13 pt, so cả glyph/style/thuộc tính đoạn qua sửa riêng/chung, lỗi muộn, Undo và lưu/mở. Kiểm schema/mô hình của fixture chạy trong Windows runner; kịch bản Word thực vẫn NOT_RUN. Nếu Word làm mất thuộc tính và guard phục hồi thì phải báo FAIL/chưa sửa được, không nới điều kiện hoặc chấp nhận chữ đúng để ghi PASS. Chưa đổi sang writer khác khi chưa có bằng chứng Word.

## 46. Dẫn chiếu không mất bảo vệ vì Unicode hoặc đệm số

Chuỗi 1/2/4 có `Xem mục 4.` được giữ thủ công trước đây, nhưng `Xem mục 4.` (NFD), `mục số 4`, tab/NBSP hoặc `khoản (4)` bị bỏ sót và còn đề xuất 4 → 3. Ngoài ra, nhãn 01/02/04 có dẫn chiếu `mục 4` hoặc `mục 3` không khớp chuỗi 04/03 theo cách so cũ.

Nhận biến thể NFC/NFD của tập cue hiện có, khoảng ngang Unicode/tab, từ “số” và một nhãn trong ngoặc. Giữ grammar danh sách/khoảng cũ. Khóa so sánh numeric bỏ 0 đầu từng thành phần cho cả nhãn hiện tại/dự kiến và tiền tố mục con, không sửa văn bản hoặc CP. Không suy số Ả Rập tương đương La Mã, không tự sửa dẫn chiếu, không thêm cue “điều” ngoài tập trước đây trong đợt này.

Hồi quy có dẫn chiếu đến số cũ/số dự kiến/mục con, nhiều thành phần và độ rộng 0 khác nhau, lựa chọn cũ với cùng nhãn/CP, mọi profile và đối chứng nhóm không dẫn chiếu vẫn sửa đúng. Đây là guard có giới hạn cho dẫn chiếu gõ tay trong phạm vi đọc, không phải parser mọi dẫn chiếu pháp lý hoặc mọi story Word.

## 47. Giữ placeholder của ô số và ngày

Hai lỗi nối nhau đã tái hiện: C01 gộp khoảng của `Số:    ...    /BC-ABC`; trong header Nhà nước đã nhận diện, `CheckNumber` còn đề xuất sửa đè khoảng trước `/` dù chính parser đã coi số là chưa được điền. Nay C01 giữ span placeholder/padding tới slash theo nhãn NFC/NFD; header chỉ xóa khoảng sau `/` khi số chưa cấp. Số đã điền vẫn chuẩn hóa như cũ; không đoán số được cấp.

Ngày slash có nhãn rõ như `Ngày sinh:    /    /…` hoặc năm `____` được capture placeholder thật và chỉ miễn padding của slot blank/placeholder. Không dùng dấu `.` đầu slot làm lookahead giả khiến năm chưa phân tích vẫn được coi đã nhận diện. Trường đã có số và khoảng ngoài slot giữ kiểm cũ; nhãn sai/nội dung không thuộc dạng vẫn ngoài ngoại lệ.

Fixture Word sửa chữ bổ sung cả hai loại; không tạo group Word mới hoặc chọn tài liệu cá nhân. Guard ở `AdministrativeHeaderRules` và C01 được kiểm cùng nhau để một đường sửa khác không thu hẹp lại ô đã bảo vệ.

## Kiểm chứng

Core/OpenXML trên Linux: **6149 kiểm qua**, tăng **374** từ mốc 5775. Build DesktopChecks Release: 0 warning/error. Các harness nguồn riêng đã qua 86 kiểm pure C14, 185 kiểm dẫn chiếu và 103 kiểm placeholder cùng hồi quy liên quan; số này đã nằm trong runner Core, không cộng thêm lần nữa. WPF có kiểm production session/helper với managed ranges và mô hình fixture DOCX. CI Windows của mã cuối đợt đang chờ.

Word thực vẫn **NOT_RUN**. Cần kiểm riêng dấu câu 14 pt có kiểu khác khoảng bên cạnh, tổ hợp nhiều nhóm, Undo/lưu-mở/nguồn bất biến trước khi kết luận Word đã giữ định dạng. Bản 0.1.78 đã đóng gói giữ nguyên tại commit/URL của gói; mã ở đây chưa có ZIP 0.1.79.
