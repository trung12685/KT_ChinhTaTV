# Đợt bổ sung sau 0.1.59 — chưa phát hành Windows

Nền: `03bf183746d37f0ca543f386faf8045f2ee9a090` (.59). Nhánh: `feature/format-batch-after-0.1.59`. Version trong code chuẩn bị .60, chưa có gói tải .60. Tích lũy khoảng 5–10 bước có kiểm thử theo [chu kỳ mới](RELEASE-CADENCE.md); không gộp số test/commit thành số tính năng.

## B01 — Hoa/thường của viết tắt tên loại

Mở rộng C05, không thêm nhóm đếm mới. Chỉ nhận prefix trước dấu nối đầu tiên trong dòng Số Nhà nước đã qua parser và đối chiếu XML/chữ thuần. So khớp danh mục bằng OrdinalIgnoreCase, không sửa ký tự khác ngoài hoa/thường. Bắt buộc một tên loại tương ứng duy nhất, chữ thuần, trước phần nội dung trong 40 đoạn đầu; ví dụ TTR chỉ được đề xuất thành TTr khi nhận diện TỜ TRÌNH. Thiếu, trùng hoặc không khớp tên loại thì không sửa viết tắt, tránh đổi mã cơ quan trong công văn. Mã toàn hoa chưa đối chiếu được tên loại không sinh cảnh báo case mới; chữ thường chưa đủ điều kiện vẫn qua cảnh báo mã cũ.

| Trước | Khi duyệt sửa |
|---|---|
| `Số: 01/TTR-ABC` | `Số: 01/TTr-ABC` |
| `Số: 01/CTR-ABC` | `Số: 01/CTr-ABC` |
| `Số: 01/QYĐ-ABC` | `Số: 01/QyĐ-ABC` |
| `Số: 01/bcđx-ABC` | `Số: 01/BCĐX-ABC` — nguồn người dùng |
| `Số: 03/ttr-Abc-XYZ` | `Số: 03/TTr-Abc-XYZ`; mã cơ quan `Abc` vẫn cảnh báo thủ công |

Giữ nguyên số đã cấp, ô số chưa điền và mọi nhóm mã cơ quan; đúng QyĐ/CTr/TTr/TrS thì không báo. Không thay viết tắt theo tên loại văn bản, không đoán CV/mã lạ, không thêm dấu tiếng Việt cho BCDX, không sửa dòng ở thân bài/trang 2, field/control/tab/vùng chưa chứng minh. Bản sao vẫn có trong bảng tra cứu nhưng chưa được cấp quyền sửa mã theo nhận diện tên loại văn bản gốc. BCĐX giữ citation/tooltip nguồn người dùng, không gán vào Nghị định.

Hồi quy bổ sung: đủ 31 mục danh mục, 28 tên hành chính/bổ sung được phép; 3 bản sao không sửa mã; đúng offset, giữ agency suffix, quét lại không còn lỗi case đã sửa, kết hợp nhãn/0/dấu nối, chặn stale/protected/page/tab/unknown và rollback toàn lượt khi lưu thất bại. Kiểm cục bộ: 1689 lượt assertion đạt qua runner Core/proof/COM giả tập trung; không phải WPF hoặc Word runtime. Windows CI B01 đã đạt **1766 Core/OpenXML + 113 WPF** tại commit `030d599daf8b8db0aeeac0da0ee2295b8c193263`, [run 40040754991](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40040754991); không có package/Word runtime. YAML đã được kiểm cú pháp và tách trigger/quyền; chưa chạy đường phát hành trong bước này.

Thay đổi quy trình đi kèm: mỗi push chỉ chạy Windows checks; packaging tách riêng và bỏ version/tag/ZIP cố định .59. Báo cáo TXT cập nhật C05, ví dụ và chu kỳ tích lũy. Không phát hành Windows trong B01.

## B02 — Tên loại không khớp ký hiệu

Mở rộng cảnh báo M04, giữ 24 nhóm đề xuất / 8 nhóm thủ công / 1 minh họa. Căn cứ NĐ30 Phụ lục I, phần I, mục II.3.b (PDF trang 22) về cấu tạo ký hiệu và bảng PL III.I (PDF trang 40); đã đối chiếu bản ký. Khi có một tên loại chữ thuần duy nhất ở đầu trước nội dung và prefix là một viết tắt khác đã biết, báo **Tên loại không khớp ký hiệu** đúng phần prefix. Tooltip ba dòng ghi căn cứ và hướng đối chiếu tên loại/ký hiệu đã đăng ký.

| Tình huống | Kết quả |
|---|---|
| `KẾ HOẠCH`, `Số: 01/BC-ABC` | Cảnh báo: KH tương ứng KẾ HOẠCH, ký hiệu đang dùng BC; người dùng đối chiếu và sửa bên sai |
| `KẾ HOẠCH`, `Số: 01/KH-ABC` | Không có cảnh báo mâu thuẫn |
| `BÁO CÁO ĐỀ XUẤT`, `Số: 01/BC-ABC` | Cảnh báo, nêu BCĐX do người dùng bổ sung |
| `Công văn` hoặc `V/v…`, `Số: 01/TTR-ABC` | Không áp kiểm tên loại của văn bản có tên loại, không đổi/báo nhầm mã cơ quan |
| `KẾ HOẠCH`, `số  :  3 / BC – ABC` | Các sửa đã hỗ trợ cho ra `Số: 03/BC-ABC`; cảnh báo mâu thuẫn còn đúng cho đến khi người dùng đối chiếu |

Đây là cảnh báo **không tích sửa tự động**: chưa biết tên loại hay ký hiệu đã ghi sai; không tự đổi BC thành KH, không đoán tên loại theo nội dung. Không đoán mã lạ/thiếu dấu, không áp bảng tên loại gốc cho bản sao. Chặn khi tên loại/số trùng, tên loại sau nội dung, trang 2, field/control/XML chưa đủ, tab và mẫu số chưa hỗ trợ. Giữ nguyên số, cơ quan, nội dung, khối nhiều đoạn, XPS và giao dịch đang có. Sau khi sửa đúng một bên trong Word và quét mới thì cảnh báo hết; sửa khoảng trắng/số/dấu nối đơn thuần không được làm ẩn mâu thuẫn.

Bài mẫu OfficeComponentCheck dùng Báo cáo đề xuất nay ghi BCĐX tương ứng để không trộn mâu thuẫn tên loại vào bài phông/Line; bài này chỉ biên dịch trong CI, chưa xác nhận runtime Word. Core mới có 117 assertion: 28 tên/nguồn, phạm vi, không báo nhầm, offset, tooltip, phối hợp sửa số và quét lại, cấm kế hoạch manual và chặn đề xuất case cũ khi đổi tên loại. Local runner tổng 1806 assertion đạt. Windows CI gắn đúng commit sẽ được ghi trong PR/checks; packaging không chạy.

Tiến độ đợt: **2 bước tính năng (B01–B02)**. Còn tích lũy đến khoảng 5–10 bước mới chuẩn bị bản tải Windows.

## Nội dung tiếp theo

- Hoàn thiện kiểm thể thức từng vùng theo NĐ30, chỉ đề xuất sửa khi nhận diện và bằng chứng đủ chắc.
- Tiếp tục các lỗi chữ cơ học, lặp từ/cụm từ trong phạm vi an toàn; chưa triển khai suy đoán ngữ nghĩa hoặc thư viện ngữ cảnh.
- Lỗi cảnh báo phông sau sửa nhiều nhóm trên .56 vẫn chờ bằng chứng runtime đúng phiên bản; CI không khép mục này.
- Khi đủ khoảng 5–10 bước, tổng hợp danh sách, ví dụ đã sửa/chưa sửa/dự kiến, chạy kiểm bắt buộc và tạo một gói Windows mới.
