# Đợt 73–77 — dẫn chiếu, lưu, Unicode, đối chiếu XPS và báo cáo

Ngày 30/10/2026 (UTC+7), tiếp tục từ `352e500e5c8709470be7a393605ebc99dd12a676` trên nhánh `feature/isolated-spacing-proof-batch22`. Mã 0.1.81 tích lũy cùng đợt 69–72, dự kiến đóng gói sau kiểm chứng đúng commit. Bản 0.1.80 và main/tag/gói cũ giữ nguyên; quyền tự sửa Exactly/AtLeast, bộ đọc thử nghiệm OFF, khoảng ký tay 80 pt và danh mục 33/8/1 giữ nguyên.

## 73. Dẫn chiếu kết thúc bằng dấu nháy hoặc ngoặc

Tái hiện trước sửa: nhóm 1/2/4 vẫn có quyền đổi 4 → 3 dù có `Xem “mục 4”.`, do ranh giới dẫn chiếu chưa nhận dấu đóng nháy/ngoặc. Bổ sung suffix dấu đóng rồi yêu cầu ranh giới thật phía sau. Áp dụng cùng grammar số đơn, số nhiều cấp và khoảng La Mã đã có; giữ source spelling/offset.

Không nhận prefix `4”suffix` hoặc `4'000` thành một tham chiếu mục 4; không tự ghép cặp ngoặc hoặc nhận dấu mở chen giữa cue và nhãn. Thêm 3 mẫu số và 2 mẫu La Mã vào nhóm Word `outline-references`; các mẫu cũ giữ nguyên. Kiểm mô hình/fixture không thay Word thực.

## 74. Chọn nhầm đích lưu vẫn giữ danh sách lỗi và bản xem trước

Tái hiện trước sửa: chọn trùng file nguồn vẫn gọi Word Save trước khi từ chối đích; nếu Save lỗi, bản làm việc đã chuyển sang trạng thái chưa chắc dù đầu vào đích vốn không hợp lệ. UI dùng đường xử lý lỗi chung nên xóa danh sách lỗi/preview và yêu cầu Quét lại cả khi lỗi mới ở bước kiểm đích chỉ đọc.

Thêm preflight chuẩn hóa đường dẫn, yêu cầu DOCX, kiểm nguồn/bản làm việc/Undo/thư mục riêng/đích là thư mục/parent thiếu trước Save. UI chạy bước này trước thao tác có thể đổi Word; nếu bị từ chối, giữ lựa chọn, mapping, preview và phạm vi để chọn nơi lưu khác. Khi đã vào Save hoặc phát hiện đích đổi sau preflight, vẫn dùng đường lỗi thật và vô hiệu danh sách cũ; không phân loại từ nội dung thông báo lỗi. Writer tiếp tục kiểm lại trước công bố.

## 75. Cùng ngày tháng không khác quyền sửa vì NFC/NFD

Tái hiện `Ninh Bình ( bản nháp ), ngày 03 tháng 10 năm 2026`: NFC không có gợi ý sửa, nhưng bản NFD lại có hai C15 xóa khoảng trong ngoặc. Dùng helper boolean chung nhận nhãn NFC/NFD và dạng trộn, thay bốn regex cũ trong luật ngoặc/dấu kết câu/hai chấm/ngữ cảnh dùng chung. Không chuẩn hóa lại nội dung hoặc dùng offset từ chuỗi đã đổi.

Giữ ngoại lệ ở cùng phạm vi ngày tháng đã có, không tự parse ngày lịch hoặc mở sửa header. Văn xuôi thông thường và gợi ý dấu phẩy độc lập giữ hành vi cũ; lựa chọn NFD cũ mất quyền thì bị từ chối. Mẫu Word sửa chữ thêm cùng chú thích ngày ở cả NFC/NFD, chưa chạy Word thật.

## 76. Đối chiếu chế độ vẽ của glyph và nét XPS

Chữ, geometry, màu và baseline giống nhau chưa chứa toàn bộ điều kiện vẽ. Bổ sung snapshot chế độ vẽ liên quan của glyph/nét, bao gồm nguồn kế thừa và override; đối chiếu trước chấp nhận bản sao/trial. Không gộp false/true bằng OR và không dùng các cờ thuộc loại hình khác để tạo drift.

Bài WPF có control không đổi, khác cờ local/ancestor/style, RenderTargetBitmap và probe comparator cũ chỉ bỏ riêng bằng chứng mới. Các ca EdgeMode của nét bắt buộc có pixel khác để tái hiện khoảng trống proof; các ca chữ kiểm trạng thái vẽ thực sự đổi, vẫn ghi nhận pixel nhưng không ép mọi backend phải repaint khác. Kết quả cần Windows thực thi; không suy từ Linux compile hoặc kết luận mọi backend/hardware vẽ giống nhau. Không đổi quyền tự sửa Exactly/AtLeast hoặc mở quyền PAGE.

## 77. Xuất báo cáo phạm vi kiểm thành TXT

Trong cửa sổ phạm vi kiểm của lần quét, thêm nút lưu TXT qua đường xuất báo cáo đã kiểm byte và bảo vệ file của phiên. Nội dung giữ nguyên snapshot đang xem; không gọi Word Save/Close hoặc thay dirty/checkpoint/Undo. Lỗi lưu giữ bảng và báo lỗi trong chính cửa sổ báo cáo; có thể chọn nơi lưu khác hoặc thử lại.

Báo cáo vẫn phân biệt nhóm chưa nhận diện/thiếu dữ liệu/một phần/đã quan sát và số cảnh báo. Không có cảnh báo không thành chứng nhận đã kiểm hết; không xuất nội dung đoạn hoặc CP nguồn. Xuất báo cáo không tự quét để cập nhật snapshot.

## Kiểm chứng

Core/OpenXML cục bộ: **8976 kiểm qua**, tăng **295** từ 8681 (157 dẫn chiếu, 138 Unicode). Build DesktopChecks Release: **0 warning/error**. Harness riêng qua 80 kiểm preflight mới cùng 143 hồi quy lưu; fixture dẫn chiếu 123 kiểm, tăng 33. Fixture sửa chữ với cả hai dạng Unicode của chú thích ngày qua 11 kiểm schema/part/selection/expected-story. Đây là các số thuộc runner tương ứng sau tích hợp, không cộng lặp vào Core. Các bài WPF mới gồm 19 kiểm giữ UI khi chọn nhầm đích, 15 xuất báo cáo và 80 chế độ vẽ; cần Windows thực thi.

[Windows CI 40973438980](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40973438980) của nguồn `bd1ec9ea0bbb9bc6664756ac92ccf5af25918398` đã **Success**; [job 123795580629](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40973438980/job/123795580629) qua Core/OpenXML, thực thi WPF/build, ghi evidence và tải log. Các bài WPF mới đã được thực thi trên Windows, gồm bốn ca pixel của nét và các ca trạng thái vẽ chữ. Tổng Windows lấy từ `check-evidence.json` của đúng lượt, không suy từ Linux.

Nhánh preview riêng dùng nguyên commit này; [workflow đóng gói 40973832589](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40973832589) đã **Success**, gồm publish, EXE, metadata, ZIP và tamper checks. [Tải 0.1.81](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40973832589/artifacts/12941004409) · [bằng chứng nguồn/digest](PREVIEW-0.1.81-EVIDENCE.md). Tài liệu cập nhật sau không đổi EXE đã khóa.

Word thực vẫn **NOT_RUN**. Gói 0.1.81 có 15 nhóm Word độc lập; bản EXE 0.1.80 vẫn 13.
