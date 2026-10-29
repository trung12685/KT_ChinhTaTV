# Bằng chứng bản thử Windows0.1.77

Đối chiếu ngày29/10/2026 (UTC). Đây là một artifact thử tích lũy đợt20–32, chưa tạo release/tag mới. Microsoft Word runtime: **NOT_RUN**; quyền sửa Exactly/AtLeast giữ nguyên.

| Mục | Giá trị |
| --- | --- |
| Nguồn đóng gói | `ac6f50cc2751835667971204dd4828d973c9408a` |
| CI Windows | [Run40940520669 — Success](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40940520669) |
| Job | [123682390903](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40940520669/job/123682390903) |
| Artifact | [ChinhTaViet-0.1.77-preview-ac6f50cc](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40940520669/artifacts/12927431090) |
| Artifact ID | `12927431090` |
| Kích thước hiển thị trên GitHub | 69,3MB |
| Digest artifact do GitHub công bố | `7af2a9d67fb5b38bab697f09ae63180d7871c3e7c8b14b984ceb8b552232d197` |
| ZIP ứng dụng bên trong | `ChinhTaViet-0.1.77-preview-ac6f50cc-win-x64.zip` |
| Lưu artifact | 30ngày từ lần chạy; cần đăng nhập GitHub để tải |

Digest ở bảng là của **artifact bên ngoài**. SHA256 của ZIP ứng dụng nằm trong `SHA256.txt` đi cùng ZIP; không dùng hai giá trị thay nhau. `build-checks.json` đi kèm ghi số kiểm Windows thực, commit và runURL. Trang công khai đã xác nhận mọi bước và digest artifact; số Core/WPF và SHA của ZIP bên trong không đọc được khi chưa tải artifact, nên không suy số từ runner khác.

Các bước Windows đã thành công: Core/OpenXML; WPF và build ứng dụng; publish tự chứa Windowsx64; kiểm metadata bằng payload giả; chạy EXE đã publish với nhóm không hợp lệ và nhận mã2; đối chiếu báo cáo gói; tạo ZIP/giải nén sạch rồi đối chiếu toàn tập file và từngSHA256; kiểm ba thay đổi âm trên bản sao riêng cùng bốn control dương; tải đúng một artifact. CI không gọi Word ở smoke mã2.

Kiểm cục bộ Linux trên cùng phần mã ứng dụng: **5343** kiểm Core/OpenXML, tăng36 so với mốc5307; build Desktop Release không warning/error. Harness metadata có23 kiểm qua với `python -X warn_default_encoding -W error::EncodingWarning`. Những số này tách riêng khỏi số Windows trong gói và khỏi kết quả Word thực.

Để thử, làm theo [PREVIEW-0.1.77.md](PREVIEW-0.1.77.md), giải nén đầy đủ rồi chạy `KIEM-TRA-WORD.cmd`. Giữ `office-groups.txt` của đúng phiên cùng moduleID Desktop/Core để đối chiếu. Tài liệu trên nhánh phát triển sau gói có thể được cập nhật; nguồn của EXE trong artifact vẫn là commit trong bảng.
