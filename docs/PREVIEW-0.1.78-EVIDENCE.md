# Bằng chứng bản thử Windows 0.1.78

Đối chiếu ngày 30/10/2026 (UTC+7). Đã gom 12 cụm đợt 33–44 thành một artifact preview. Không tạo release/tag mới; giữ main, bản thử 0.1.77 và các gói cũ. Microsoft Word thực: **NOT_RUN**; quyền sửa Exactly/AtLeast giữ nguyên.

| Mục | Giá trị |
| --- | --- |
| Nguồn đóng gói | `de1ea9652a1cbebdf4fabe6e8e3d1da9e07689a3` |
| Nguồn code triển khai cuối | `9bd2918dc4443ffcc3041707bcd38817cdfc47fe` |
| CI preview | [Run 40950303966 — Success](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40950303966) |
| Job | [123716344741](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40950303966/job/123716344741) |
| Artifact | [ChinhTaViet-0.1.78-preview-de1ea965](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40950303966/artifacts/12930333908) |
| Artifact ID | `12930333908` |
| Kích thước GitHub hiển thị | 69,4 MB |
| Digest artifact do GitHub công bố | `7f04ce3de5634bf8ba1bbc38a60c8bae6cd3351e5615adfb3cc057c91a4b129f` |
| ZIP ứng dụng bên trong | `ChinhTaViet-0.1.78-preview-de1ea965-win-x64.zip` |
| Lưu artifact | 30 ngày từ lần chạy; cần đăng nhập GitHub để tải |

Digest trên là của **artifact bên ngoài**. SHA256 của ZIP ứng dụng nằm trong `SHA256.txt` đi cùng ZIP; không dùng hai giá trị thay nhau. `build-checks.json` ghi số Core/WPF Windows thực, đúng commit và runURL. Trang công khai xác nhận trạng thái từng bước, artifact và digest; số kiểm Windows/SHA ZIP bên trong không đọc được khi chưa tải artifact, nên không suy từ runner khác.

Các bước Windows đã thành công: Core/OpenXML; WPF/build; publish tự chứa Windows x64; metadata regression; EXE đã publish từ chối nhóm sai với mã 2; đối chiếu báo cáo/metadata trước ZIP; giải nén ZIP sạch và kiểm toàn tập file/từng SHA256; ba probe thay đổi âm trên bản sao cùng bốn control dương; tải đúng một artifact. Smoke mã 2 không gọi Word. Probe không sửa ZIP đã xuất hoặc thư mục gói nguồn.

Kiểm cục bộ Linux của code mới: **5775** Core/OpenXML qua, tăng 432 so với mốc 5343 của bản thử 0.1.77; build DesktopChecks Release 0 warning/error. Metadata helper có 23 kiểm giả lập qua với encoding warning thành lỗi. Những số này tách riêng khỏi số Windows trong gói và khỏi kết quả Word thực.

Đọc [PREVIEW-0.1.78.md](PREVIEW-0.1.78.md), giải nén đầy đủ rồi mở EXE hoặc chạy `KIEM-TRA-WORD.cmd`. Giữ `office-groups.txt` của đúng lần chạy và MVID Desktop/Core. Fixture sửa chữ đã có ô trống/đánh dấu, ngày Unicode và khoảng cuối sau ngoặc; việc có kịch bản không thay kết quả Word.

Tài liệu trên nhánh phát triển có thể được bổ sung sau khi tạo gói; EXE trong artifact vẫn thuộc nguồn trong bảng. Các dòng “chưa đóng gói” ở nhật ký cũ ghi trạng thái trước khi workflow này hoàn tất.
