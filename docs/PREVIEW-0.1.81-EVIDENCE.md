# Bằng chứng bản thử Windows 0.1.81

Đối chiếu ngày 30/10/2026 (UTC+7). Gom 9 cụm đợt 69–77 sau 0.1.80 thành một artifact preview từ nguồn `bd1ec9ea0bbb9bc6664756ac92ccf5af25918398`. Main, tag/release và các gói cũ giữ nguyên.

| Mục | Giá trị |
| --- | --- |
| Nguồn code và đóng gói | `bd1ec9ea0bbb9bc6664756ac92ccf5af25918398` |
| Nhánh preview riêng | `preview/windows-0.1.81-bd1ec9ea` |
| CI trước đóng gói | [40973438980 — Success](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40973438980) |
| Workflow preview | [40973832589 — Success](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40973832589) |
| Job preview | [123796880816](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40973832589/job/123796880816) |
| Artifact | [ChinhTaViet-0.1.81-preview-bd1ec9ea](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40973832589/artifacts/12941004409) |
| Artifact ID | `12941004409` |
| Kích thước GitHub hiển thị | 69,4 MB |
| Digest artifact ngoài | `4412d8f138ce2b662eb42fbc067725c3cfe0817b183dc9fd58d5271410d88d5f` |
| ZIP ứng dụng bên trong | `ChinhTaViet-0.1.81-preview-bd1ec9ea-win-x64.zip` |
| Lưu artifact | 30 ngày từ lần chạy; cần đăng nhập GitHub để tải |
| Microsoft Word thực | **NOT_RUN** |

Các bước preview Windows đều Success: Core/OpenXML, thực thi WPF/build, ghi số kiểm thật, publish Windows x64 tự chứa, hồi quy metadata, EXE từ chối nhóm sai bằng mã 2, đối chiếu báo cáo trước/sau giải nén ZIP, kiểm toàn tập file/từng SHA256 và thử phát hiện thay đổi trên bản sao riêng. Smoke EXE mã 2 không gọi Word. Workflow tải đúng một artifact, không tạo release/tag.

Core/OpenXML cục bộ Linux: **8976 kiểm qua**, tăng 614 từ 8362 của gói 0.1.80. Riêng đợt 73–77 tăng 295 từ 8681. Build DesktopChecks Release 0 warning/error. Tổng Core/WPF Windows lấy từ `build-checks.json` trong gói, không suy từ Linux hoặc cộng harness riêng. Trang công khai xác nhận trạng thái bước/artifact/digest; chưa tải artifact nên không khẳng định đã đọc tổng Windows hoặc SHA256 ZIP bên trong.

Bài WPF rendering giữ bốn ca EdgeMode bắt buộc khác pixel để tái hiện khoảng trống proof; các ca glyph bảo vệ trạng thái hiệu lực, ghi pixel theo backend, không ép mọi máy vẽ khác nhau. Đây là kiểm WPF, không phải bằng chứng Word. UI lưu, báo cáo phạm vi TXT và các fixture nguồn đều được runner Windows thực thi.

Digest artifact ngoài khác SHA256 ZIP ứng dụng trong `SHA256.txt`. Metadata ghi nguồn/run và số kiểm đúng gói. Tài liệu nhánh phát triển có thể cập nhật sau đóng gói mà không đổi EXE; các dòng “chưa ZIP” trong nhật ký cũ mô tả trạng thái trước khi tạo gói.

Đọc [hướng dẫn 0.1.81](PREVIEW-0.1.81.md), giải nén toàn bộ gói vào thư mục mới. Có 15 nhóm Word, gồm `retained-marks` và `annotated-text` chạy riêng, không cần `technical-text` trước đạt. Nhóm `outline-references` thêm dẫn chiếu có dấu nháy/ngoặc. Word thực vẫn NOT_RUN; quyền sửa Exactly/AtLeast giữ nguyên, bộ đọc thử nghiệm OFF và khoảng ký tay 80 pt.
