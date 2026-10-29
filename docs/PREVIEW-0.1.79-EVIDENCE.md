# Bằng chứng bản thử Windows 0.1.79

Đối chiếu ngày 30/10/2026 (UTC+7). Đã gom 12 cụm đợt 45–56 sau 0.1.78 thành một artifact preview trên nguồn `3e8f268ad935a9cfb24a12427ec36224a7848e02`. Main, tag/release và các gói cũ giữ nguyên.

| Mục | Giá trị |
| --- | --- |
| Nguồn code và đóng gói | `3e8f268ad935a9cfb24a12427ec36224a7848e02` |
| Nhánh preview riêng | `preview/windows-0.1.79-3e8f268a` |
| CI trước đóng gói | [40959817177 — Success](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40959817177) |
| Workflow preview | [40960198832 — Success](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40960198832) |
| Job preview | [123750662328](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40960198832/job/123750662328) |
| Artifact | [ChinhTaViet-0.1.79-preview-3e8f268a](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40960198832/artifacts/12935094001) |
| Artifact ID | `12935094001` |
| Kích thước GitHub hiển thị | 69,4 MB |
| Digest artifact ngoài | `1ac0c9076d136049e4f368ed79461ee61b4ddc97d6d57af93cb366da84662a6c` |
| ZIP ứng dụng bên trong | `ChinhTaViet-0.1.79-preview-3e8f268a-win-x64.zip` |
| Lưu artifact | 30 ngày từ lần chạy; cần đăng nhập GitHub để tải |
| Microsoft Word thực | **NOT_RUN** |

Các bước preview Windows đều Success: Core/OpenXML, WPF/build, ghi số kiểm thật, publish Windows x64 tự chứa, hồi quy metadata, EXE từ chối nhóm sai bằng mã 2, đối chiếu báo cáo trước/sau giải nén ZIP, kiểm toàn tập file/từng SHA256 và thử phát hiện thay đổi trên bản sao riêng. Workflow tải đúng một artifact, không tạo release/tag. Smoke EXE mã 2 không gọi Word.

Core/OpenXML cục bộ Linux của mã này: **7021 kiểm qua**, tăng 1246 so với mốc 5775 của gói 0.1.78. Build DesktopChecks Release 0 warning/error. Tổng Core/WPF Windows lấy từ `build-checks.json` trong gói; không suy số Windows từ runner Linux hoặc cộng harness riêng vào tổng khác. Trang công khai xác nhận trạng thái bước/artifact/digest; chưa tải artifact nên không khẳng định đã đọc các tổng Windows hoặc SHA256 ZIP bên trong.

Digest artifact ngoài và SHA256 ZIP ứng dụng bên trong là hai giá trị khác nhau. SHA256 của ZIP nằm trong `SHA256.txt` đi kèm. Metadata ghi commit/run và số kiểm đúng nguồn; tài liệu nhánh phát triển có thể được cập nhật sau đóng gói nhưng không đổi EXE đã tạo. Các dòng “chưa ZIP” trong nhật ký cũ mô tả trạng thái trước khi tạo gói này.

Đọc [hướng dẫn 0.1.79](PREVIEW-0.1.79.md), giải nén toàn bộ gói vào thư mục mới. Mẫu sửa chữ thêm dấu câu có run 14 pt đậm/gạch chân; nhóm Word độc lập `outline-references` kiểm dẫn chiếu không cần Subject/Line trước đạt. Có tổng 13 nhóm Word, trạng thái thực vẫn NOT_RUN. Quyền sửa Exactly/AtLeast giữ nguyên; bộ đọc thử nghiệm OFF và khoảng ký tay 80 pt.
