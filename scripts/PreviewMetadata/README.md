# Metadata của gói preview Windows

Workflow `windows-preview.yml` chỉ đóng gói khi đẩy nhánh `preview/windows-*` hoặc chạy thủ công. Nhánh phát triển thông thường không tạo gói. Workflow tạo một artifact preview, giữ nguyên release/tag hiện có.

Sau khi kiểm Core/WPF thực trên Windows, workflow lưu `build-checks.json` chứa version, số kiểm, commit và URL run đúng nguồn. Thư mục publish phải là bản Windows x64 tự chứa runtime, có mẫu giả và launcher `KIEM-TRA-WORD.cmd`.

```powershell
dotnet run --project scripts/PreviewMetadata -c Release -- prepare artifacts/win-x64 0.1.77 docs/PREVIEW-0.1.77.md
python scripts/PreviewMetadata/checks.py
# Chạy --verify-report-package bằng DesktopChecks, sau đó chép TXT đã xuất vào gói.
dotnet run --project scripts/PreviewMetadata -c Release -- manifest artifacts/win-x64 0.1.77
# Sau khi ZIP được giải nén vào thư mục sạch:
dotnet run --project scripts/PreviewMetadata -c Release -- verify artifacts/preview-extracted 0.1.77
pwsh -NoProfile -File scripts/PreviewMetadata/VerifyTamper.ps1 -PackageFolder artifacts/preview-extracted -Version 0.1.77
```

`prepare` tạo checklist từ danh mục thực, báo cáo TXT và manifest preview với Word runtime `NOT_RUN`. `manifest` băm mọi file sau khi đã có đủ tài liệu; `verify` kiểm lại toàn bộ tập file, SHA256, checklist và toàn văn báo cáo gắn code/evidence. Link/reparse point, file thừa/thiếu/đổi và metadata cũ đều bị chặn. Công cụ không sửa mã hoặc tài liệu nguồn, không mở Word và không ghi đè file đã tạo.

`checks.py` dùng payload giả trong thư mục tạm để kiểm tamper, thiếu/thừa file, evidence/report/checklist cũ, runtime không tự chứa và đường manifest không an toàn. Số ca này được báo riêng, không cộng vào Core/WPF; fixture symlink ghi `SKIP` nếu runner không được phép tạo link. Nó không chạy ứng dụng Windows hoặc Word.

`VerifyTamper.ps1` kiểm trên bản sao tạm của gói thật đã giải nén: đổi một byte DOCX, thêm file ngoài manifest và chèn đường dẫn thoát thư mục vào manifest đều phải bị verifier từ chối. Sau phục hồi, cả bản sao và gói nguồn phải vẫn qua kiểm tra. ZIP/dist thật không được sửa; kết quả ba probe và bốn control ghi riêng trong summary CI.
