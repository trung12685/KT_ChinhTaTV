# Đợt bổ sung sau 0.1.59 — chưa phát hành Windows

Nền: `03bf183746d37f0ca543f386faf8045f2ee9a090` (.59). Nhánh: `feature/format-batch-after-0.1.59`. Version trong code chuẩn bị .60, chưa có gói tải .60. Tích lũy khoảng 5–10 bước có kiểm thử theo [chu kỳ mới](RELEASE-CADENCE.md); không gộp số test/commit thành số tính năng.

## B01 — Hoa/thường của viết tắt tên loại

Mở rộng C05, không thêm nhóm đếm mới. Chỉ nhận prefix trước dấu nối đầu tiên trong dòng Số Nhà nước đã qua parser và đối chiếu XML/chữ thuần. So khớp danh mục bằng OrdinalIgnoreCase, không sửa ký tự khác ngoài hoa/thường. Bắt buộc một tên loại tương ứng duy nhất, chữ thuần, trước phần nội dung trong 40 đoạn đầu; ví dụ TTR chỉ được đề xuất thành TTr khi nhận diện TỜ TRÌNH. Thiếu, trùng hoặc không khớp tên loại thì giữ thủ công, tránh đổi mã cơ quan trong công văn.

| Trước | Khi duyệt sửa |
|---|---|
| `Số: 01/TTR-ABC` | `Số: 01/TTr-ABC` |
| `Số: 01/CTR-ABC` | `Số: 01/CTr-ABC` |
| `Số: 01/QYĐ-ABC` | `Số: 01/QyĐ-ABC` |
| `Số: 01/bcđx-ABC` | `Số: 01/BCĐX-ABC` — nguồn người dùng |
| `Số: 03/ttr-Abc-XYZ` | `Số: 03/TTr-Abc-XYZ`; mã cơ quan `Abc` vẫn cảnh báo thủ công |

Giữ nguyên số đã cấp, ô số chưa điền và mọi nhóm mã cơ quan; đúng QyĐ/CTr/TTr/TrS thì không báo. Không thay viết tắt theo tên loại văn bản, không đoán CV/mã lạ, không thêm dấu tiếng Việt cho BCDX, không sửa dòng ở thân bài/trang 2, field/control/tab/vùng chưa chứng minh. Bản sao vẫn có trong bảng tra cứu nhưng chưa được cấp quyền sửa mã theo nhận diện tên loại văn bản gốc. BCĐX giữ citation/tooltip nguồn người dùng, không gán vào Nghị định.

Hồi quy bổ sung: đủ 31 mục danh mục, 28 tên hành chính/bổ sung được phép; 3 bản sao không sửa mã; đúng offset, giữ agency suffix, quét lại không còn lỗi case đã sửa, kết hợp nhãn/0/dấu nối, chặn stale/protected/page/tab/unknown và rollback toàn lượt khi lưu thất bại. Kiểm cục bộ: 1689 lượt assertion đạt qua runner Core/proof/COM giả tập trung; không phải WPF hoặc Word runtime. Windows Core/OpenXML/WPF sẽ có kết quả gắn đúng commit trong checks/PR; khi chưa có CI đạt, giữ trạng thái đang kiểm. YAML đã được kiểm cú pháp và tách trigger/quyền; chưa chạy đường phát hành trong bước này.

Thay đổi quy trình đi kèm: mỗi push chỉ chạy Windows checks; packaging tách riêng và bỏ version/tag/ZIP cố định .59. Báo cáo TXT cập nhật C05, ví dụ và chu kỳ tích lũy. Không phát hành Windows trong B01.

## Nội dung tiếp theo

- Hoàn thiện kiểm thể thức từng vùng theo NĐ30, chỉ đề xuất sửa khi nhận diện và bằng chứng đủ chắc.
- Tiếp tục các lỗi chữ cơ học, lặp từ/cụm từ trong phạm vi an toàn; chưa triển khai suy đoán ngữ nghĩa hoặc thư viện ngữ cảnh.
- Lỗi cảnh báo phông sau sửa nhiều nhóm trên .56 vẫn chờ bằng chứng runtime đúng phiên bản; CI không khép mục này.
- Khi đủ khoảng 5–10 bước, tổng hợp danh sách, ví dụ đã sửa/chưa sửa/dự kiến, chạy kiểm bắt buộc và tạo một gói Windows mới.
