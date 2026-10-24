# Đợt bổ sung sau 0.1.59 — B01–B05 trong bản thử 0.1.60

Nền: `03bf183746d37f0ca543f386faf8045f2ee9a090` (.59). Nhánh: `feature/format-batch-after-0.1.59`. Phiên bản của đợt là .60; chỉ tạo gói thử sau khi các kiểm bắt buộc đạt. Tích lũy khoảng 5–10 bước có kiểm thử theo [chu kỳ mới](RELEASE-CADENCE.md); không gộp số test/commit thành số tính năng.

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

Bài mẫu OfficeComponentCheck dùng Báo cáo đề xuất nay ghi BCĐX tương ứng để không trộn mâu thuẫn tên loại vào bài phông/Line; bài này chỉ biên dịch trong CI, chưa xác nhận runtime Word. Core mới có 117 assertion: 28 tên/nguồn, phạm vi, không báo nhầm, offset, tooltip, phối hợp sửa số và quét lại, cấm kế hoạch manual và chặn đề xuất case cũ khi đổi tên loại. Local runner tổng 1806 assertion đạt. Windows CI B02 đã đạt **1883 Core/OpenXML + 113 WPF** trên `7e996ba19dd0249bb9dec584612deaf73cf847f8`, [run 40042437109](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40042437109); packaging không chạy.

## B03 — Thiếu khoảng trắng sau dấu hai chấm

Mở rộng nhóm C17 thành **Khoảng trắng quanh dấu hai chấm**, giữ tổng 24 nhóm đề xuất / 8 thủ công / 1 minh họa. Đây là quy tắc ứng dụng về dấu câu, không gán thành một điều/khoản của NĐ30. Cùng điều kiện hai từ rõ ở trái, từ cuối chữ thường, từ phải ít nhất hai chữ có chữ thường, không phải mẫu kỹ thuật/header/ngày. Chỉ thêm một dấu cách ASCII còn thiếu sau `:`; dấu hai chấm làm mốc nguồn có độ dài 1, giữ hai từ ngoài vùng ghi.

| Trước | Khi duyệt sửa |
|---|---|
| `Ghi chú:nội dung` | `Ghi chú: nội dung` |
| `Ghi chú :nội dung` — chọn cả hai phía | `Ghi chú: nội dung` |
| `Ghi chú  :nội dung` — chỉ chọn thiếu sau | `Ghi chú  : nội dung`; lỗi trước còn để chọn |
| `Ghi chú  :nội dung` — chỉ chọn thừa trước | `Ghi chú:nội dung`; lỗi sau còn để chọn |
| `12:30`, `1:2`, `http:host`, `Ghi chú:NỘI DUNG` | Không áp quy tắc thiếu sau này |

Giao diện dùng cùng nhóm Dấu hai chấm, nhãn thiếu sau/thừa trước và tooltip tương ứng, không đổi quyền sửa. Khi chọn cả hai phía, hai span liền nhau không ghi chồng; ghép cùng khoảng trắng/ngoặc/cụm lặp/dấu câu và phông qua giao dịch cũ. Giữ khoảng chưa chọn, nguyên văn, CP Word, checkbox theo nhóm, quét mới, checkpoint, một Undo và rollback. XML/field/control chưa đủ bằng chứng giữ thủ công; nếu từ sau đã đổi thành số dù `:` còn nguyên thì CurrentTextPlan vẫn chặn gợi ý cũ.

Core thêm 75 assertion, chuyển một trường hợp thiếu sau khỏi danh sách loại trừ cũ; local runner tổng **1880 assertion đạt**. Có Unicode tổ hợp/emoji, offset, giờ/tỉ lệ/protocol/đường dẫn/mã, chọn riêng/cả hai phía, cùng nhiều lỗi, phông, mất quyền/context và lỗi lưu phục hồi cả lượt. OfficeIntegrationCheck bổ sung hai lượt sửa riêng trước/sau, giữ kiểu đậm/gạch chân, quét/Undo/rollback/lưu-mở trên mẫu giả; CI chỉ biên dịch bài Word, chưa chạy runtime. TXT cập nhật C17 và ví dụ cả hai phía. Windows CI B03 đã đạt **1957 Core/OpenXML + 113 WPF** trên `ef45c4a1e12c8f305f2e298aaee9e5a43ded4819`, [run 40043399042](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40043399042); không đóng gói.

## B04 — C25: Khoảng trắng cuối đoạn sau dấu kết câu

Thêm nhóm C25, tổng **25 nhóm đề xuất / 8 thủ công / 1 minh họa**. Chỉ xóa dấu cách ASCII sau một dấu `.`, `?`, `!` ở cuối đoạn có chữ thuần và hai từ bên trái đủ rõ; từ cuối ít nhất hai chữ, chữ thường, không phải viết tắt/La Mã đã loại trừ. Giữ dấu câu, phần chữ, khoảng trước chưa chọn và toàn bộ dấu kết đoạn/ô. Đây là quy tắc ứng dụng cần duyệt, không gán căn cứ pháp lý.

| Trước (`·` chỉ minh họa dấu cách trong bảng này) | Khi duyệt |
|---|---|
| `Đã hoàn thành.···` | `Đã hoàn thành.` |
| `Đã hoàn thành··.···` — chỉ chọn cuối đoạn | `Đã hoàn thành··.`; lỗi trước dấu câu còn để chọn |
| `Đã hoàn thành··.···` — chọn cả hai | `Đã hoàn thành.` |
| `Đã hoàn thành···` chưa có dấu kết câu | Giữ ngoài phạm vi C25 |
| `Số:········/BC-ABC`, ô trống, ngày chưa điền | Giữ nguyên; không dùng C25 để thu hẹp ô trống |

Không áp cho số/giờ, từ viết tắt, mã, đường dẫn, chuỗi dấu/ba chấm, từ cuối toàn hoa/tên riêng, tab/NBSP/ngắt dòng trong đoạn, khoảng trống sau đoạn chưa kết câu hoặc XML/control chưa đủ. Không xóa CR/LF/kết ô. FixKind nối cuối enum để giữ giá trị cũ; BatchPlan chỉ nhận payload xóa ASCII spaces và cho ghép phông đã xác minh, CurrentTextPlan phải còn cùng gợi ý từ quét mới. Gom C25 cạnh C01 trong nhóm Khoảng trắng thừa, tooltip ba dòng chỉ rõ giữ dấu kết đoạn/ô.

C25 kết hợp hai phía hai chấm/ngoặc/lặp/dấu câu/phông qua giao dịch cũ, hậu quét và rollback toàn lượt. Core kiểm Unicode/CP, chọn riêng/hai phía, blank/date/technical, mất quyền/context, payload giả, lưu thất bại và giữ nguyên dấu kết đoạn. Local runner tổng **1960 assertion đạt**; Windows CI B04 đã đạt **2037 Core/OpenXML + 113 WPF** trên `9e5449df1655acb789b01726767a71de126e0922`, [run 40044420287](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40044420287). Báo cáo TXT có C25/ví dụ dấu cách thật, đếm25/8/1 và lịch sử đủ B01–B04.

OfficeParagraphEndCheck thêm mẫu giả có kiểu đậm/gạch chân: sửa riêng và nhóm trước/sau dấu câu, quét lại, một Undo, rollback, lưu/mở và kiểm nguồn còn nguyên. Bài được gọi từ Kiểm tra Word tự động; CI chỉ biên dịch, chưa có Word runtime xác nhận. Không phát hành Windows trong bước này.

## B05 — C15: Thiếu khoảng trắng sau ngoặc đóng

Mở rộng C15, giữ tổng25/8/1. Mốc `)` → `) ` có độ dài nguồn1, không viết lại từ. Chỉ sau ngoặc cân bằng/không lồng và có khoảng trước ngoặc mở sẵn; cần hai từ trước rõ, ít nhất hai từ trong (mỗi từ ≥2 chữ), hai từ sau rõ, từ ngay sau toàn chữ thường. Không thêm khoảng trước ngoặc mở; giữ các trường hợp hàm/công thức/mã, từ ngắn/toàn hoa, ngoặc thiếu/lồng/ô trống, tab/NBSP/header/ngày hoặc XML chưa đủ. Danh sách hàm loại trừ chỉ là guard cục bộ, không phải thư viện ngữ nghĩa.

| Trước | Khi duyệt |
|---|---|
| `Theo mẫu (nội dung)đã duyệt.` | `Theo mẫu (nội dung) đã duyệt.` |
| `Theo mẫu ( nội dung )đã duyệt.` — chọn riêng ngoài | `Theo mẫu ( nội dung ) đã duyệt.`; giữ lỗi trong chưa chọn |
| Cùng ví dụ — chỉ chọn trong | `Theo mẫu (nội dung)đã duyệt.`; thiếu sau vẫn còn để chọn |
| Cùng ví dụ — chọn cả nhóm | `Theo mẫu (nội dung) đã duyệt.` |
| `fn(nội dung)đã duyệt`, `Tính sin (alpha beta)gamma delta`, ngoặc trống | Không dùng quy tắc thiếu sau để thay chữ/ký hiệu |

Cùng Family parenthesis và FixKind hiện có; property nhận diện sau ngoặc chỉ dùng nhãn/Suggestion/tooltip, không cấp quyền sửa. Nguồn quy tắc ứng dụng, không gán NĐ30. CurrentTextPlan chặn khi quyền hoặc từ sau thay đổi dù dấu `)` giữ nguyên; ghép phông/cụm lặp/hai chấm/dấu câu/C25 qua giao dịch cũ. Local runner tổng **2028 assertion đạt**; CI Windows thực tế gắn đúng commit ở PR/checks.

OfficeParenthesisAfterCheck dùng mẫu giả giữ đậm/gạch chân: sửa riêng trong/ngoài, nhóm, quét lại, một Undo, lỗi giữa lượt/rollback và lưu/mở/giữ nguồn. Bài được gọi trong Kiểm tra Word tự động; CI chỉ biên dịch, chưa có xác nhận runtime Word cho bản .60.

## Chốt đợt 5 bước

B01–B05 là **5 bước tính năng**, không tính test/docs/commit thành bước riêng. Chuẩn bị CHANGES/SUPPORTED-FIXES .60 và TXT có ví dụ; gói Windows chỉ tạo sau khi Core/OpenXML, WPF/build, publish và kiểm metadata gói đạt. Workflow package nhận manual/tag hoặc nhánh release/windows-0.1.60 được tạo chủ động từ commit đã kiểm; push feature vẫn chỉ chạy tests. Không ghi đè tag/gói cũ, không nhập main, stable vẫn .27.

Bằng chứng CI/commit/tag và checksum gói nằm ở PR36, release .60 cùng build-checks.json/TXT/SHA256.txt. Chưa coi bản tải/CI là Office2021 PASS; phản hồi phông sau nhóm .56 vẫn chưa khép. Đợt tiếp theo tiếp tục theo ưu tiên thể thức → lỗi chữ/lặp → ngữ nghĩa/thư viện sau cùng.

## Nội dung tiếp theo

- Hoàn thiện kiểm thể thức từng vùng theo NĐ30, chỉ đề xuất sửa khi nhận diện và bằng chứng đủ chắc.
- Tiếp tục các lỗi chữ cơ học, lặp từ/cụm từ trong phạm vi an toàn; chưa triển khai suy đoán ngữ nghĩa hoặc thư viện ngữ cảnh.
- Lỗi cảnh báo phông sau sửa nhiều nhóm trên .56 vẫn chờ bằng chứng runtime đúng phiên bản; CI không khép mục này.
- Khi đủ khoảng 5–10 bước, tổng hợp danh sách, ví dụ đã sửa/chưa sửa/dự kiến, chạy kiểm bắt buộc và tạo một gói Windows mới.
