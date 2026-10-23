# Danh mục tên loại và chữ viết tắt

Đối chiếu Nghị định 30/2020/NĐ-CP, Phụ lục III, phần I, trang PDF 40. Giữ đúng chữ hoa/thường của chữ viết tắt, đặc biệt **QyĐ, CTr, TTr, TrS**. Không tự thêm chữ viết tắt CV cho công văn vì bảng này không có mục đó.

## 27 tên loại văn bản hành chính theo Nghị định

| STT | Tên trong bảng | Chữ viết tắt |
| --- | --- | --- |
| 1 | Nghị quyết (cá biệt) | NQ |
| 2 | Quyết định (cá biệt) | QĐ |
| 3 | Chỉ thị | CT |
| 4 | Quy chế | QC |
| 5 | Quy định | QyĐ |
| 6 | Thông cáo | TC |
| 7 | Thông báo | TB |
| 8 | Hướng dẫn | HD |
| 9 | Chương trình | CTr |
| 10 | Kế hoạch | KH |
| 11 | Phương án | PA |
| 12 | Đề án | ĐA |
| 13 | Dự án | DA |
| 14 | Báo cáo | BC |
| 15 | Biên bản | BB |
| 16 | Tờ trình | TTr |
| 17 | Hợp đồng | HĐ |
| 18 | Công điện | CĐ |
| 19 | Bản ghi nhớ | BGN |
| 20 | Bản thỏa thuận | BTT |
| 21 | Giấy uỷ quyền | GUQ |
| 22 | Giấy mời | GM |
| 23 | Giấy giới thiệu | GGT |
| 24 | Giấy nghỉ phép | GNP |
| 25 | Phiếu gửi | PG |
| 26 | Phiếu chuyển | PC |
| 27 | Phiếu báo | PB |

Hai mục “Nghị quyết (cá biệt)”, “Quyết định (cá biệt)” dùng tên đầu văn bản NGHỊ QUYẾT, QUYẾT ĐỊNH; phần cá biệt là phân loại trong danh mục. “Giấy uỷ quyền” giữ theo bảng; nhận diện cả cách đặt dấu “Giấy ủy quyền” và không tự đổi cách đặt dấu.

## 3 loại bản sao theo cùng bảng

| Tên | Chữ viết tắt |
| --- | --- |
| Bản sao y | SY |
| Bản trích sao | TrS |
| Bản sao lục | SL |

Danh mục bản sao để tra cứu; chưa áp dụng tự động các quy tắc phông/trích yếu/Line của văn bản gốc cho phần thể thức bản sao.

## Bổ sung theo yêu cầu người dùng

| Tên | Chữ viết tắt | Nguồn |
| --- | --- | --- |
| Báo cáo đề xuất | BCĐX | Người dùng bổ sung, không nằm trong bảng Phụ lục III |

## Phạm vi ứng dụng từ .59

Có thể xem trong **Trợ giúp → Danh mục tên loại văn bản…** hoặc báo cáo TXT. Một nguồn dữ liệu dùng cho tên loại/viết tắt/nguồn, nhận diện tên loại, kiểm phông, C24 và mốc trích yếu/Line. Có 28 tên hành chính/bổ sung có thể tham gia nhận diện; vẫn phải qua guard khối đầu/trích yếu/Word/XML hiện có. Danh mục đầy đủ không có nghĩa đã hỗ trợ mọi mẫu đặc thù như hợp đồng, biên bản, giấy/phiếu có bố cục khác hoặc bảng.

Ở dòng Số, không báo “chữ thường” chỉ do chữ viết tắt tên loại đúng bảng như TTr/CTr/QyĐ/TrS. Phần mã cơ quan có chữ thường vẫn giữ cảnh báo. Chưa tự đổi chữ viết tắt theo tên loại hoặc đoán mã cơ quan; chuẩn hóa nhãn, số, dấu nối và khoảng trắng hiện có giữ nguyên. BCĐX là cấu hình bổ sung của người dùng, không coi là mục mới của Nghị định. Lỗi cũ trên Office2021 vẫn chờ bằng chứng runtime phù hợp.

## Bổ sung chưa phát hành — B01 sau .59

C05 có thể đề xuất sửa **hoa/thường** phần viết tắt đã khớp danh mục trong dòng Số nhận diện chắc: `TTR-ABC → TTr-ABC`, `QYĐ-ABC → QyĐ-ABC`. Chỉ sửa prefix, giữ số và mã cơ quan; không đổi BC thành KH theo tên loại, không suy đoán CV hoặc viết tắt thiếu dấu. BCĐX là bổ sung người dùng. Đây là code đang phát triển, chưa nằm trong ZIP .59. Xem [nhật ký](BATCH-AFTER-0.1.59.md).
