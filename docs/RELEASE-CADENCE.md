# Chu kỳ phát triển và tạo bản Windows

Yêu cầu người dùng từ 24/10/2026 (UTC+7): tiếp tục bổ sung code và gom khoảng **5–10 bước phát triển** mới tạo bản tải Windows. Không bỏ qua kiểm thử hoặc bảo toàn dữ liệu để tăng tốc.

## Mỗi bước

1. Thực hiện một nội dung có ích, giới hạn rõ; kiểm tra hồi quy phù hợp trước khi chốt code.
2. Cập nhật nhật ký đợt: đã làm, ví dụ, giới hạn, kết quả đúng commit và phần tiếp theo. Một tính năng cùng test/docs vẫn là một bước.
3. Push nhánh phát triển để `windows-checks.yml` chạy Core/OpenXML và WPF, đồng thời build ứng dụng. Workflow chỉ lưu log/kết quả, quyền contents:read; không publish, tạo ZIP, tag hoặc GitHub Release. Các ảnh/TXT tạm do bài kiểm tạo không phải gói phát hành.
4. Chưa hoàn tất kiểm thử thì ghi trạng thái đang kiểm; không đánh dấu bước hoàn tất theo số lượng commit. Chưa có bằng chứng Word thực tế thì giữ mục Office 2021 đang chờ.

## Khi đủ khoảng 5–10 bước

Đối chiếu nhật ký và lỗi còn mở, chuẩn bị version tiếp theo cùng `docs/CHANGES-<version>.md`, `docs/SUPPORTED-FIXES-<version>.md` và báo cáo TXT có ví dụ. Hoàn thành các kiểm bắt buộc trước khi chủ động gọi `windows-package.yml` hoặc tạo tag `v<version>` tại đúng commit. Manual dispatch cần workflow đã có trên default branch của GitHub; tag trigger dùng workflow tại tag. Không chạy packaging chỉ để kiểm một bước bình thường.

Workflow đóng gói đối chiếu version/tag và tài liệu, chạy lại Core/OpenXML/WPF, publish Windows self-contained, kiểm metadata báo cáo trong gói, tạo ZIP/SHA-256 và prerelease mới. Không sửa hoặc phát hành lại tag/asset cũ. Phiên bản .60 trong code đang phát triển không phải bằng chứng đã có bản tải .60; chưa có tài liệu phát hành .60 thì guard đóng gói sẽ chặn.

Nếu có lỗi nghiêm trọng cần bản sửa sớm thì nêu lý do và kết quả kiểm thực tế; mốc 5–10 là nhịp làm việc, không phải lý do trì hoãn xử lý lỗi. Giữ kiểm nét XPS, đối chiếu nguyên văn/định dạng, checkpoint, một Undo và rollback. CI không cài Word và không chứng minh Office 2021 đã đạt.

Nhật ký hiện tại: [Đợt sau 0.1.59](BATCH-AFTER-0.1.59.md). Mốc ổn định đã chốt vẫn .27; gói thử gần nhất .59.
