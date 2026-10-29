# Đợt 57–60 — độ tin cậy phục hồi, lịch sử XML và chuỗi lệnh

Ngày 30/10/2026 (UTC+7), tiếp tục từ `0ff96d0c8a81390dc155bd9bc15e8a3c0bff5557`, nhánh `feature/isolated-spacing-proof-batch22`. Mã phát triển chuyển sang **0.1.80** để phân biệt với gói 0.1.79 đã tạo. Không xuất ZIP mới trong đợt này; giữ main, nhánh/gói/tag cũ, bộ đọc thử nghiệm OFF, khoảng ký tay 80 pt và quyền sửa Exactly/AtLeast.

## 57. Undo không biến byte lưu dở thành đã xác minh

Tái hiện bằng code sản xuất: Save ghi dở rồi ném lỗi làm checkpoint chưa xác minh như chính sách đợt 54. Nếu tiếp tục Undo và Close lỗi, `CheckpointSwap` ghi nhận byte hiện tại chưa bị thay; Undo trước đây chỉ dùng kết quả đó để đặt lại `snapshotCurrent=true`. Vì thế `CanRecover` có thể true trên chính byte lưu dở và Dispose xóa thư mục phục hồi.

Nay khi Undo thất bại, kết quả Unchanged/Restored chỉ giữ độ tin cậy nếu đầu vào trước Undo đã tin cậy. Không coi phục hồi byte đầu vào là xác minh ngữ nghĩa tài liệu. Giữ source, dirty state và stack Undo, không mở hoặc xóa bản chưa chắc. Undo thành công vẫn có thể mở candidate Undo đã được xác minh, kể cả bản hiện tại trước đó chưa chắc.

Kiểm gọi SaveCopy, Undo, Connect và Dispose thật qua managed documents: Close lỗi, mở candidate lỗi nhưng trả lại byte đầu vào thành công, cả hai trạng thái tin cậy/dirty và đường thành công. Không dùng Word COM hoặc ghi vào tài liệu cá nhân.

## 58. Không cấp quyền qua lịch sử tracked move

Tái hiện: fixture DOCX đúng schema có `moveTo`/`moveFrom` bao run chữ hoặc mốc move range còn được cấp C01/lặp chữ vì nguyên văn khớp. TrackRevisions có thể đang OFF trong khi lịch sử cũ vẫn tồn tại.

Thêm wrapper và bốn mốc RangeStart/End vào ranh giới `XML-CONTENT` đang dùng cho insert/delete/field chưa hỗ trợ. Quyền chữ/phông/bố cục chỉ thuộc phần đã chứng minh trước ranh giới; không suy rằng CP sau cấu trúc chưa hỗ trợ vẫn khớp. Kiểm cả XML string và snapshot, mọi profile, stale selection và phần prefix vẫn được phép sửa. Các thuộc tính proofErr chính tả thông thường không bị coi là revision.

Mốc hợp lệ trực tiếp dưới body/tc cũng phải chặn trước paragraph bị ảnh hưởng. Cặp được đối chiếu bằng family/id nguyên văn trong cùng thân, không trộn story textbox. Một RangeEnd bên ngoài không có start trước đó, kể cả nằm sau paragraph cuối, làm toàn thân chưa đủ bằng chứng; không thể cấp toàn quyền chỉ vì không còn paragraph sau mốc. Có start trong paragraph thì giữ prefix trước start đó. Wrapper ancestor sai schema vẫn bị chặn.

Đây là guard theo nguồn XML thân hiện tại, không phải parser mọi revision/story hoặc hỗ trợ sửa Track Changes. Không tự chấp nhận/từ chối hoặc xóa lịch sử để làm mất cảnh báo.

## 59. Khoảng/từ lặp trong đối số lệnh có nháy

Tái hiện: `echo "Báo  cáo"`, `printf '%s' 'kế hoạch kế hoạch'`, `Write-Output "Báo  cáo"`, `tool --label "Báo  cáo"`, `tool --message='kế hoạch kế hoạch'` và `cmd /c echo "Báo  cáo"` còn có đề xuất làm đổi dữ liệu.

Nhận giá trị có nháy thẳng sau tùy chọn dài `--name` phân cách bằng space/tab hoặc `=`, và tập lệnh đầu dòng hẹp echo/printf/Write-Output/Write-Host với cmd /c tùy chọn. Theo cơ chế cũ, gợi ý chữ trong cả dòng kỹ thuật giữ thủ công; dòng không có gợi ý không tự sinh cảnh báo. Văn xuôi có dấu gạch/nháy, từ echo ở giữa câu hoặc tùy chọn không rõ không tự bị che.

Giữ nguồn NFC/NFD/emoji và CP, quyền XML vẫn độc lập. Lựa chọn cũ bị chặn khi cùng span trở thành dữ liệu lệnh. Chưa phân tích mọi shell/short option. Bài Word sửa chữ thêm bốn dòng lệnh để đối chiếu qua sửa riêng/chung, lỗi muộn, Undo và lưu/mở; vẫn cần Word thực.

## 60. Phản hồi khi không đủ điều kiện mở lại

Tình huống lỗi lưu rồi lỗi Undo có thể để phiên đã đóng, bản tạm chưa chắc và `CanRecover=false`. UI cũ vẫn hướng dẫn Quét lại trong lỗi chung; nhấn Quét lại lại không làm gì.

Tách phản hồi theo trạng thái thật: khi không đủ điều kiện mở lại tự động, nói rõ cần giữ dữ liệu phục hồi/nhật ký, giữ nguyên nguyên nhân lỗi. Nếu sửa/Undo đã xong nhưng quét lỗi, giữ thông tin thao tác đã hoàn tất và không gợi ý phát lại lệnh. Nhánh Quét lại bị chặn hiện phản hồi rõ và không gọi Connect hoặc nâng quyền bản tạm. Phiên còn mở/bản đã xác minh có thể mở lại giữ hướng dẫn cũ.

## Kiểm chứng

Core/OpenXML cục bộ: **7674 kiểm qua**, tăng **653** so với mốc 7021 của 0.1.79. Build DesktopChecks Release: **0 warning/error**. Các harness nguồn cô lập đã qua 59 kiểm Undo trust, 551 chuỗi lệnh và 102 XML lịch sử di chuyển; các số này nằm trong runner tương ứng sau tích hợp, không cộng lặp vào tổng. Phản hồi UI có 13 kiểm managed/WPF, chưa thực thi WPF trên Linux. Fixture sửa chữ thực qua 11 kiểm schema/part/mô hình nguồn với bốn dòng lệnh mới.

[Windows CI](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40962906287) của commit `5ec846b554a373e411d07e7cb216d5045fb13213` đã **Success**: [job 123760002134](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40962906287/job/123760002134) qua Core/OpenXML, WPF/build, ghi evidence và tải log. Tổng Windows nằm trong `check-evidence.json` của CI, không suy từ Linux. Workflow này không đóng gói ứng dụng hoặc chạy Microsoft Word.

Microsoft Word thực vẫn **NOT_RUN**, tổng 13 nhóm Word giữ nguyên. Không xuất ZIP 0.1.80 hoặc thay artifact 0.1.79.
