# Bằng chứng bản thử Windows 0.1.80

Đối chiếu ngày 30/10/2026 (UTC+7). Gom 12 cụm đợt 57–68 sau 0.1.79 thành một artifact preview từ nguồn `8e9cb3b19eac0cfe300fb206b27065219ba6c3a9`. Main, tag/release và các gói cũ giữ nguyên.

| Mục | Giá trị |
| --- | --- |
| Nguồn code và đóng gói | `8e9cb3b19eac0cfe300fb206b27065219ba6c3a9` |
| Nhánh preview riêng | `preview/windows-0.1.80-8e9cb3b1` |
| CI trước đóng gói | [40967635856 — Success](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40967635856) |
| Workflow preview | [40967945181 — Success](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40967945181) |
| Job preview | [123777113950](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40967945181/job/123777113950) |
| Artifact | [ChinhTaViet-0.1.80-preview-8e9cb3b1](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40967945181/artifacts/12938519861) |
| Artifact ID | `12938519861` |
| Kích thước GitHub hiển thị | 69,4 MB |
| Digest artifact ngoài | `730e063b48a5c7300f466cd9c75d20e667334f8f33eac20be689aaf42c67ac0b` |
| ZIP ứng dụng bên trong | `ChinhTaViet-0.1.80-preview-8e9cb3b1-win-x64.zip` |
| Lưu artifact | 30 ngày từ lần chạy; cần đăng nhập GitHub để tải |
| Microsoft Word thực | **NOT_RUN** |

Các bước preview Windows đều Success: Core/OpenXML, thực thi WPF/build, ghi số kiểm thật, publish Windows x64 tự chứa, hồi quy metadata, EXE từ chối nhóm sai bằng mã 2, đối chiếu báo cáo trước/sau giải nén ZIP, kiểm toàn tập file/từng SHA256 và thử phát hiện thay đổi trên bản sao riêng. Smoke EXE mã 2 không gọi Word. Workflow tải đúng một artifact, không tạo release/tag.

Core/OpenXML cục bộ Linux của nguồn này: **8362 kiểm qua**, tăng 1341 từ 7021 của gói 0.1.79. Riêng đợt 65–68 tăng 276 từ 8086. Build DesktopChecks Release 0 warning/error. Tổng Core/WPF Windows lấy từ `build-checks.json` trong gói, không suy từ Linux hoặc cộng harness riêng. Trang công khai xác nhận trạng thái bước/artifact/digest; chưa tải artifact nên không khẳng định đã đọc tổng Windows hoặc SHA256 ZIP bên trong.

Digest artifact ngoài khác SHA256 ZIP ứng dụng trong `SHA256.txt`. Metadata ghi nguồn/run và số kiểm đúng gói; tài liệu nhánh phát triển có thể cập nhật sau đóng gói mà không đổi EXE. Các dòng “chưa ZIP” trong nhật ký cũ mô tả trạng thái trước khi tạo gói.

Đọc [hướng dẫn 0.1.80](PREVIEW-0.1.80.md), giải nén toàn bộ gói vào thư mục mới. Nhóm `technical-text` có mẫu bookmark cùng/qua đoạn bên cạnh văn xuôi được sửa, cộng các mẫu ngày/ô chọn/literal và dấu câu có style riêng. Có 13 nhóm Word, trạng thái thực vẫn NOT_RUN. Quyền sửa Exactly/AtLeast giữ nguyên; bộ đọc thử nghiệm OFF và khoảng ký tay 80 pt.
