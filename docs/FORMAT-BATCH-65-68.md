# Đợt 65–68 — lịch sử định dạng, ngày sinh và phục hồi Word

Ngày 30/10/2026 (UTC+7), tiếp tục từ `43b31b183625dbe07771f18f967bf48cb9242842` trên nhánh `feature/isolated-spacing-proof-batch22`, mã 0.1.80. Đợt này cùng 57–64 được gom vào một bản thử Windows riêng sau kiểm chứng. Main, tag và gói cũ giữ nguyên; không đổi quyền Exactly/AtLeast, bộ đọc thử nghiệm OFF hoặc khoảng ký tay 80 pt.

## 65. Không cấp quyền qua lịch sử thay đổi thuộc tính

Tái hiện với XML hợp lệ: `rPrChange`/`pPrChange` vẫn có thể cấp quyền C01 và sửa phông khi chữ hiện tại khớp, dù writer/hậu kiểm không chứng minh giữ cây thuộc tính lịch sử. Các lịch sử bảng `tblPrChange`, `trPrChange`, `tcPrChange`, `tblPrExChange`, `tblGridChange` cũng từng cho vùng chữ/ô đã đối chiếu đi qua.

Thêm chính xác bảy tên Word này vào ranh giới revision chưa hỗ trợ đã dùng cho ins/del/move. Giữ prefix đã chứng minh trước ranh giới; không đoán CP hoặc cho phép sửa phần sau chỉ vì chữ khớp. Hai đường XML string/snapshot dùng cùng chính sách. Không tự chấp nhận/từ chối revision hoặc xóa metadata để mở quyền. Đây là khoảng trống quyền sửa tái hiện trong mã, chưa phải mất lịch sử được quan sát trên Word thật.

## 66. Giữ ô mẫu “Sinh ngày … tháng … năm …”

Mẫu `Sinh ngày        tháng        năm        ` từng có hai C01 làm hẹp ô. Thêm riêng nhãn `Sinh ngày` ở đầu dòng vào grammar ngày bằng chữ; giữ NFC/NFD, source offset và khoảng ngang ASCII/NBSP/NNBSP đã hỗ trợ. Chỉ bảo vệ slot trống/placeholder; khoảng trong nhãn, trường đã điền và chú thích độc lập vẫn được duyệt sửa.

Kiểm âm phát hiện grammar cũ có thể backtrack trong `...x` để coi dấu chấm đầu là kết thúc trường năm. Nay dấu chấm kết thúc chỉ hợp lệ trước khoảng ngang, kết đoạn hoặc hết chuỗi; không coi hậu tố chưa parse là năm trống. Không mở mọi câu chứa “sinh ngày”, tiền tố địa danh hoặc nhãn sai. Bài Word sửa chữ thêm hai mẫu chưa điền/điền một phần.

## 67. Giải phóng các đối tượng đã lấy khi getter Word lỗi

Ở đường đọc section, mã cũ lấy Section → PageSetup → Range trước khi vào try/finally. Nếu getter sau lỗi thì Section/PageSetup đã lấy chưa được giải phóng; Release đầu tiên lỗi cũng bỏ qua các đối tượng sau. Harness gọi đúng reader đã tái hiện những đường này bằng đối tượng managed.

Đăng ký mỗi acquisition thành công ngay trước khi hỏi đối tượng con tiếp theo, rồi giải phóng theo thứ tự ngược và thử tất cả. Áp dụng cho reader section dùng chung lúc quét/hậu kiểm trang, cặp Font/ParagraphFormat và cặp point đo BodyFlow. Hai acquisition dùng chung identity vẫn được cân bằng hai lần, không FinalRelease hoặc deduplicate.

Nếu thao tác gốc lỗi, giữ đúng exception/HRESULT và gắn lỗi cleanup riêng để không che nguyên nhân hoặc làm sai phân loại mất kết nối. Nếu chỉ Release lỗi sau đọc thành công, trả lỗi cleanup có wrapper riêng để không tự replay phép đọc. Không tuyên bố đã cải tổ mọi acquisition trong ứng dụng hoặc quan sát rò tiến trình Word thật.

## 68. Thông báo đúng khi còn reference nhưng Word đã mất kết nối

Tái hiện luồng SaveCopy ghi dở rồi lỗi, sau đó Check gặp RPC disconnect. Document reference vẫn khác null nhưng checkpoint chưa đủ điều kiện tự mở lại. UI cũ chỉ nhìn IsOpen nên tiếp tục hướng dẫn Quét lại.

Thêm tín hiệu từ chối phục hồi riêng, giữ RPC gốc trong InnerException. UI nhận đúng trạng thái mất kết nối dù vẫn sở hữu reference, đưa hướng dẫn giữ dữ liệu/nhật ký thay cho hứa quét lại sẽ mở được. Trường hợp sửa hoặc Undo đã xong nhưng quét sau lỗi vẫn giữ sự thật về thao tác đã hoàn tất. Lỗi thông thường trên phiên còn dùng được vẫn cho quét lại; không tự Save/Close/reopen hoặc lặp thao tác ghi.

## Mẫu Word cho vùng đánh dấu

Nhóm `technical-text` có thêm mẫu bookmark cùng đoạn và qua đoạn. Chữ có lỗi trong bookmark phải giữ thủ công, hai đoạn văn xuôi bên cạnh vẫn sửa riêng/chung được. Bài kiểm gồm lỗi cuối lượt/rollback, một Undo, SaveCopy và mở lại; đối chiếu tên/phạm vi chữ, kiểu phần chưa chọn và hash nguồn. Cho phép Word đổi ID nhất quán nhưng không chấp nhận thiếu/đảo/trùng mốc hoặc đổi tên/chữ. Fixture có kiểm schema, part không liên quan và mô hình quyền sửa trước khi chạy Word. Không thêm nhóm Word thứ 14 và không suy PASS Word từ kiểm fixture.

## Kiểm chứng

Core/OpenXML cục bộ: **8362 kiểm qua**, tăng **276** từ 8086 (87 lịch sử định dạng, 183 ngày sinh và 6 giữ nguyên nhân phục hồi). Build DesktopChecks Release: **0 warning/error**. Harness riêng qua 120 kiểm ownership và 17 kiểm fixture bookmark; các số này thuộc runner tương ứng sau tích hợp, không cộng lặp vào Core.

[Windows CI 40967635856](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40967635856) của nguồn `8e9cb3b19eac0cfe300fb206b27065219ba6c3a9` đã **Success**; [job 123776065561](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40967635856/job/123776065561) qua Core/OpenXML, thực thi WPF/build và ghi evidence. Tổng Windows lấy từ `check-evidence.json` của đúng lượt; không suy từ Linux.

Nhánh preview riêng dùng nguyên commit này; [workflow đóng gói 40967945181](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40967945181) đã **Success**, gồm publish, EXE, metadata và ZIP/tamper checks. [Tải 0.1.80](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40967945181/artifacts/12938519861) · [bằng chứng nguồn/digest](PREVIEW-0.1.80-EVIDENCE.md). Tài liệu cập nhật sau không đổi EXE đã khóa.

Word thực vẫn **NOT_RUN**. Danh mục 33 nhóm đề xuất có điều kiện, 8 thủ công, 1 minh họa và 13 nhóm Word giữ nguyên.
