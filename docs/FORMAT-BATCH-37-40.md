# Đợt 37–40 — giữ biểu mẫu và hoàn thiện lượt sửa chữ

Ngày 30/10/2026 (UTC+7), tiếp tục từ `51914f6` trên nhánh `feature/isolated-spacing-proof-batch22`. Tích lũy mã 0.1.78 sau bản thử 0.1.77, chưa xuất ZIP mới. Giữ bộ đọc thử nghiệm tắt, khoảng ký tay 80 pt và quyền sửa Exactly/AtLeast hiện có. Danh mục vẫn 33 nhóm đề xuất có điều kiện, 8 thủ công, 1 minh họa.

## 37. Lỗi release range không bỏ qua phục hồi

Luồng sửa chữ giữ các range để đối chiếu phông/cỡ sau lưu. Trước đây vòng release trong callback phục hồi và `finally` dừng ở lỗi đầu; lỗi một RCW có thể bỏ qua các range còn lại, chưa gọi SwapCheckpoint hoặc thay lỗi thao tác bằng lỗi cleanup.

`TrackedRangeCleanup` chụp và chuyển ownership trước khi release, thử mọi range và vẫn thử phục hồi checkpoint. Kết quả phục hồi chữ được tách khỏi lỗi release. Nếu phục hồi thật thất bại, giữ cả lỗi thao tác/khôi phục/release. Nếu checkpoint đã phục hồi, giữ kết quả đó cùng cảnh báo cleanup; cleanup cuối không làm mất kết quả commit hoặc Undo. Lỗi range của phiên kiểm riêng được lưu vào lịch sử, nên phiên đó chưa thể PASS hoặc xóa bằng chứng chỉ vì checkpoint đã đúng.

Có kiểm helper và `AtomicTextBatch` với nhiều range, lỗi đầu/đôi, lỗi selector, ownership bị callback đổi, rollback đúng/sai và nguyên nhân lỗi giữ nguyên. Kiểm trạng thái phiên dùng đối tượng managed, không khởi động Word.

## 38. Dọn tệp phụ không che kết quả sửa

Các nhánh đo/đối chiếu Line, sửa nhiều nhóm, khổ/lề, PAGE và chừa chỗ ký tay có `finally` xóa XPS/candidate/source phụ. Trước đây file bị khóa hoặc thiếu quyền có thể thay lỗi gốc; sau commit còn có thể báo thất bại dù thay đổi và Undo đã được ghi.

Những tệp phụ này dùng cùng cleanup giữ file và ghi diagnostics cho IOException/UnauthorizedAccessException. Không thay lỗi đo/sửa, không bỏ trạng thái phục hồi hoặc chạy lại lượt đã commit. Bước cần thật sự xóa candidate trước khi tái tạo vẫn phải thành công; không bỏ qua lỗi đó. Đóng tài nguyên, kiểm chữ/hình học và cleanup nghiêm ngặt của phiên Word kiểm vẫn giữ nguyên.

Kiểm Windows dùng file thật mở với chế độ không cho xóa: file khóa được giữ nguyên, lỗi chính/kết quả trả về còn đúng, file khác vẫn được dọn và sau mở khóa có thể xóa lại.

## 39. Giữ ô chọn và trường ngày chưa điền

Hai báo nhầm đã tái hiện: `Tích chọn [        ] nếu đồng ý.` bị gợi ý gộp tám dấu cách thành một; `Ngày sinh:    /    /2026` bị thu hẹp các trường ngày/tháng trống.

C01 nay giữ chính xác phần chỉ chứa ASCII spaces trong `[...]`. Với nhãn đầu dòng `Ngày:`, `Ngày sinh:` hoặc `Sinh ngày:`, mẫu ba phần chia bằng slash nhận các trường toàn space/tab là chỗ chưa điền. Nhãn “ngày” NFC/NFD dùng nguyên chỉ số nguồn. Chỉ giữ trường rỗng; nhãn có khoảng thừa, trường đã có số và khoảng bên ngoài vẫn được kiểm. Slash trong văn xuôi hoặc nhãn không rõ không được miễn mọi khoảng trắng. Đây chưa phải bộ nhận mọi loại biểu mẫu/ngày hợp lệ.

Hồi quy có mọi profile, phần điền/để trống xen nhau, nhiều ô, offset sau emoji/dấu tổ hợp, trường số/ngày cũ, khoảng ngoài ô và lựa chọn đã cũ. Mẫu Word của nhóm `technical-text` bổ sung ô chọn/ngày, kiểm giữ nguyên qua sửa văn xuôi, lỗi muộn/rollback, một Undo và lưu/mở.

## 40. Khoảng cuối sau ngoặc văn xuôi rõ

C25 nhận thêm `Đã duyệt “kế hoạch”.   ` hoặc `Theo mẫu (nội dung);   `. Chỉ xóa ASCII spaces sau một dấu `.?!;` cuối đoạn. Phía trước phải có đúng một cặp quote/ngoặc đơn cân bằng, tiền tố văn xuôi rõ và ít nhất hai từ thường bên trong; loại số, viết tắt, mã, chữ hoa hoặc cặp lồng/thiếu/trộn.

Giữ mọi khoảng bên trong ngoặc, trước dấu câu chưa chọn và dấu kết đoạn/ô. Các mục mép ngoặc có thể chọn riêng hoặc gộp khi vẫn đủ điều kiện; thất bại ở lưu cuối phục hồi toàn lượt. Vùng XML chưa cho sửa vẫn là cảnh báo thủ công, context đổi phải đối chiếu lại. Không mở rộng helper dùng chung của các luật khác để tránh nới quyền ngoài C25.

Hai đoạn mới được thêm vào fixture Word sửa chữ hiện có. Kiểm schema và mô hình chọn sửa/xâu kết quả chạy trong WPF runner; kịch bản Word thực vẫn cần chạy riêng, chưa có PASS.

## Kiểm chứng

Core/OpenXML trên Linux: **5629 kiểm qua**, tăng **160** từ mốc 5469. Build DesktopChecks Release: 0 warning/error. Harness range cleanup có 11 kiểm managed qua, không cộng vào số Core.

CI Windows của mã triển khai `a3b17dee8f8f1dcc76ea500960616c6f098b63ce`: [run 40946225091 — Success](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40946225091), [job 123702175926](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40946225091/job/123702175926). Core/OpenXML, WPF/build, ghi bằng chứng và lưu log đều thành công. Runner chính đã đăng ký các lớp kiểm mới, gồm schema/mô hình lựa chọn của fixture Word mở rộng và file-sharing lock trên Windows. Số kiểm Windows thực nằm trong `check-evidence.json`/log của run; không suy từ số Linux. Workflow chỉ lưu kết quả kiểm, không tạo ZIP ứng dụng. Cập nhật tài liệu sau commit trên không đổi code đã kiểm.

Word runtime vẫn **NOT_RUN**. Gói 0.1.77 giữ nguyên; các thay đổi ở đây thuộc mã phát triển 0.1.78. Chưa dùng kết quả Core/WPF để mở quyền sửa Exactly/AtLeast hoặc chứng nhận mọi DOCX.
