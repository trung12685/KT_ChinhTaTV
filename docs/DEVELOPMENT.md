# Tiếp tục phát triển Chính tả Việt

## .70 — dừng bật mặc định thử nghiệm đọc

Phản hồi26/10/2026: .69 bật5,0s+1,8s, tắt2,1s+1,2s; tổng báo cáo12,9s/6,1s, chưa kiểm soát lạnh/ấm. Đặt backend/checkbox cùng false, đổi nhãn trung tính; giữ legacy reader, bảng/Preview, editor/hậu kiểm/rollback. Thêm kiểm cấu hình WPF, chạy CI đúng commit. Chưa tạo gói cho riêng bước này; đang có .69 dùng tùy chọn tắt được ngay. Ghi feedback có phạm vi, không kết luận nguyên nhân riêng hoặc Office2021 PASS. [Kết quả](PERFORMANCE-FEEDBACK.md), [thay đổi](CHANGES-0.1.70.md).

## Thử nghiệm .69 — sau phản hồi .68 cải thiện ít

Ngày26/10/2026: chuyển trọng tâm sang trao đổi Word/COM và chuẩn bị vị trí Preview. Năm hạng mục: private Word warm-open; enumeration có fallback; batch phông; batch thuộc tính đoạn có chứng cứ; prepared preview text có ràng buộc phiên XPS. Hiện thêm thời gian mở/tổng và counters để xác định phần chậm thực tế. Giữ full bảng/Preview, quyền sửa và toàn bộ editor/hậu kiểm/rollback. Bộ thử so evidence/getters, CP/lỗi/mixed, lookup Unicode/khoảng trắng/mơ hồ và reset XPS. Source .67/.68/main giữ nguyên; không tuyên bố giảm thời gian Office từ mô hình hoặc XML. [Chi tiết và cách đo](CHANGES-0.1.69.md).

## Thử nghiệm đọc 0.1.68 từ mốc .67

Theo yêu cầu người dùng ngày 26/10/2026: thử snapshot XML chung, tra vùng theo chỉ mục và đọc format theo run Word đã đối chiếu, có fallback toàn vùng. Thêm bật/tắt để đối chiếu và thời gian/counters từng chặng. Giữ bảng, Preview XPS và toàn bộ hậu kiểm/rollback. Không lấy bộ đọc Python bỏ bảng; không suy đoán style hoặc dùng cache qua sửa. Chạy hồi quy/benchmark XML và thêm fixture so hai chế độ trên Word; CI không chứng minh Office2021. Bản thử tách nhánh/version, mốc .67 và main giữ nguyên. [Thiết kế và phép thử](PERFORMANCE-EXPERIMENT-0.1.68.md).

## Mốc lưu 0.1.67 — trước thử nghiệm hiệu năng

Yêu cầu người dùng ngày 25/10/2026: đóng gói bản hiện tại và giữ lại trước khi thử tối ưu. Nguồn chức năng 0.1.65 tại `56891d501a32f183ba6154e9a5829b2cd65c34e4`; giữ thuật toán, bảng và Preview Word/XPS. Không triển khai tối ưu trong mốc này. Chỉ version/report-history/docs/metadata đóng gói thay đổi; ghi SHA-256 nguồn giữ nguyên trong BASELINE-0.1.67.json. Phiên bản thử tiếp theo phải tách nhánh/version, không di chuyển tag .67 hoặc ghi đè gói. Đánh giá tốc độ phải kèm đủ dữ liệu/Preview, kết quả sửa và rollback. Giới hạn/điểm Office2021 còn mở tiếp tục giữ. [Hướng dẫn mốc và quay lại](CHANGES-0.1.67.md).

## Sau .64, B03–B05 — gom cụm, chốt .65

Tiếp nối B02 `17002f9045ae5919e691ed0e0f8cca49ef10a3f4` đạt3480 Core/OpenXML +249 WPF. Theo phản hồi chia quá nhỏ, triển khai ba hạng mục chung một lượt: thiếu khoảng trước dấu mở, quote thẳng trước/trong/sau/C14/ô trống, câu có .?! trong ngoặc tròn. Parser/prose/prefix chung giữ từng gate; exact anchor payload, phép chiếu giữ dấu, C14 giữ lỗi nội bộ sau sửa mép. Test72 tổ hợp/ba profile/Unicode/CP/unknown, sửa riêng/chung/chữ/phông/bốn nhóm thân bài/stale/glyph/rollback; fixture tổng hợp Word/native CP có nhiều dạng trong cùng DOCX, giữ kiểu/cỡ và nguồn, riêng/chung/baLine/mộtUndo/lưu-mở. CI chỉ biên dịch Word. Đủ5 bước, tổng30/8/1; release notes/catalog/TXT và package sau đúng commit đạt, không suy Word PASS từ CI. [Thay đổi](CHANGES-0.1.65.md), [cụm sửa](DELIMITER-CLUSTER.md), [nhật ký](BATCH-AFTER-0.1.64.md).

## Sau .64, B02 — C30 thiếu khoảng sau ngoặc kép đóng

Tiếp nối B01 `55f1bfb31ac626a3124b06d4aad0a6f979ade9f2` đạt3420 Core/OpenXML +244 WPF. C30 thêm `”` → `” ` khi hai từ trước, ít nhất hai từ trong và hai từ sau rõ, từ ngay sau thường; giữ mép trong và quote gốc/kiểu/cỡ. C15/C30 dùng chung gate ClearAfterDelimitedText, caller vẫn chứng minh đúng loại cặp và permission; không mở câu có dấu cuối trong quote/mã/unknown. AddsSpaceAfterClosingQuote phân biệt action với xóa mép; BatchPlan chỉ nhận đúng payload thêm1ASCII qua quote hoặc xóa spaces, SharesFormat giữ ghép phông. Nhóm quotes/tooltip3 dòng, chọn từng mép/nhóm/all; TXT đổi tên C30 quanh ngoặc, thêm ví dụ/giới hạn, tổng30/8/1 giữ nguyên. Core/Desktop kiểm riêng/chung/bốn nhóm thân bài/Unicode/CP, giữ quote/glyph/kiểu/cỡ/unknown/stale/forged/rollback và modelquét mới; fixture Word kiểm một phía/chung chữ/baLine, phần chưa chọn, nguồn/Undo/lưu-mở, CI chỉ biên dịch. Hai bước .65 chưa đóng gói. [Chi tiết](QUOTE-AFTER-SPACING.md), [nhật ký](BATCH-AFTER-0.1.64.md).

## Sau .64, B01 — C14 dấu câu ngay sau ngoặc kép

Nền `f5971a5a8754df45bf68e2ba845ed4b73893eac4`, gói .64 đạt3357 Core/OpenXML +244 WPF +1 kiểm metadata, chưa Word runtime. C14 nhận dấu phẩy/chấm phẩy sau cặp “…” chứa ít nhất hai từ chữ, có hai từ trái và từ phải rõ; chỉ chuẩn hóa span dấu/space ngoài, giữ quote/nội dung/mép trong. Hai loại ngoặc tròn/kép dùng parser theo từng kind một lần khi cần, giữ guard cũ cho ngoặc tròn và unknown, thêm chặn mixed quote ở nhánh kép. Không thêm COM hoặc cache xuyên sửa, không đổi batch/XPS/Undo/rollback. Core kiểm dương/âm/CP/Unicode/ba profile, riêng/chung C30/chữ/phông/bốn nhóm thân bài, stale/glyph/rollback; fixture Word kiểm trong/ngoài/chung chữ/baLine, nguồn/Undo/lưu-mở, CI chỉ biên dịch. C14 và TXT có ví dụ/giới hạn, tổng30/8/1 giữ nguyên. Một bước code .65 chưa đóng gói, main/stable và gói .64 giữ nguyên; số kiểm mới theo đúng commit tại PR/Actions. [Chi tiết](QUOTED-PUNCTUATION.md), [nhật ký](BATCH-AFTER-0.1.64.md).

## Sau .63, B05 — giữ dấu kết trong câu trích dẫn, chốt .64

Tiếp nối B04 `eab1f8c3039c0b38ed4184146a79885485492d1a` đạt3313 Core/OpenXML +244 WPF. C30 mở đúng một dấu .?! gắn cuối lời văn rõ, giữ dấu/quote/glyph/kiểu/cỡ, đồng nhất điều kiện nếu một mép đã đúng. Không mở .../?!, nhiều câu, khoảng trước dấu/viết tắt/header/mã/unknown. Core thêm trường hợp mỗi mép/Unicode/CP/dấu/kiểu/sửa chung/stale; fixture C30 có hai biến thể cụm/câu, terminal có kiểu riêng, CI chỉ biên dịch Word. Đủ5 bước, tổng30/8/1; chuẩn bị gói .64, ghi chú và checklist từ catalog hiện hành, TXT ghi đủ tiến độ/ví dụ/giới hạn. Bằng chứng phát hành/CI đúng commit tại PR/release; chưa xác nhận runtime Office2021 hoặc khép phản hồi phông cũ. Main/stable và gói cũ giữ nguyên. [Thay đổi và cách thử](CHANGES-0.1.64.md), [danh sách](SUPPORTED-FIXES-0.1.64.md), [nhật ký](BATCH-AFTER-0.1.63.md).

## Sau .63, B04 — C30 khoảng trắng sát trong ngoặc kép

Tiếp nối B03 `0d3a5040df1b5fd59e559d3d661fbdab87021874` đạt3240 Core/OpenXML +239 WPF. QuoteSpaceRules đọc cặp “…” cân bằng/không lồng, lời văn ít nhất hai từ có chữ thường; chỉ xóa ASCII spaces ngay hai mép trong, giữ dấu/chữ/Unicode/kiểu và phía chưa chọn. Supported context chặn mã/ngày/header/tab/NBSP/control/mixed quote/unknownXML; cặp rỗng giữ nguyên width bằng guard chung SpacingRules cho FixAll. C14 thêm ranh giới ngoặc kép để dấu phẩy/chấm phẩy trong cặp vẫn được quét sau xóa mép. Enum QuoteSpace nối cuối, payload chỉ xóa space, SharesFormat nhận chọn kèm phông, tooltip3 dòng nêu quy tắc ứng dụng, nhóm checkbox quotes riêng. TXT thêm C30/ví dụ và giới hạn, tổng30/8/1. Core kiểm ba profile/NFD/emoji/CP, loại trừ, riêng/chung cả4 nhóm thân bài, giữ kiểu/glyph, stale/rollback; Desktop kiểm nhóm/chọnall/quét mới/manual; fixture Word kiểm một mép/chung chữ/baLine, nguồn/Undo/rollback/lưu-mở, CI chỉ biên dịch. Bốn bước .64 chưa đóng gói; số kiểm mới theo đúng commit tại PR/Actions. [Thiết kế](QUOTE-SPACING.md), [nhật ký](BATCH-AFTER-0.1.63.md).

## Sau .63, B03 — một lần nhận diện thân bài trong mỗi lần quét

Tiếp nối B02 `da2a0c8fb13df754fad29c07a6fdc8f9083f49e6` đạt3174 Core/OpenXML +239 WPF. BodyRuleScan.Check tạo snapshot đoạn và RepairTargets một lần, gọi các builder nội bộ C26/C27/C28–C29; CheckCore dùng chung entry này. Giữ API kiểm riêng, payload/nhóm/thứ tự, nguồn quy tắc, adjacency gốc và mọi unknown/guard. Không cache qua sửa/lưu/repagination, không lấy observation cấp write, không bỏ đọc native/XML/XPS hay hậu kiểm/rollback. Core kiểm bằng Issue equality và bộ đếm layout,500 đoạn, ba profile, fresh-scan/stale/source/boundary/no-greeting. So sánh cục bộ thêm với bản B02; Windows build/tests theo đúng commit tại PR/Actions. Ba bước code .64, tổng29/8/1, chưa đóng gói. Chưa đo end-to-end trên Office2021. [Thiết kế](BODY-RULE-SCAN.md), [nhật ký](BATCH-AFTER-0.1.63.md).

## Sau .63, B02 — bố cục đoạn có từ nhấn mạnh

Tiếp nối B01 `f3d14b5e9634a5dc1c0681fd8e4eb79428c75ea9` đạt3138 Core/OpenXML +239 WPF. C27–C29 nhận đoạn văn thường có một phần chữ thường đứng và các từ nhấn mạnh, khi font/text/XML/paragraph/spacing đã chắc. Không đổi mốc nhận thân bài, loại trừ đoạn toàn đậm/nghiêng/unknown và no-greeting anchors vẫn plain. Writer đoạn/spacing không chạm Font; proof glyph giữ từng bold/italic/underline/cỡ, fingerprint source và đoạn kế, kiểm riêng/chung4 nhóm/chữ/Line, quét mới không giữ mục đã sửa. Core thêm trường hợp mixed, không giảm guard snapshot/context/flow; fixture Word kiểm riêng từng thuộc tính, chung cả4 nhóm/chữ/baLine, rollback/mộtUndo/lưu-mở/nguồn, CI chỉ biên dịch. Hai bước .64 chưa đóng gói, tổng29/8/1. [Chi tiết](BODY-EMPHASIS-LAYOUT.md), [nhật ký](BATCH-AFTER-0.1.63.md).

## Sau .63, B01 — C26 giữ nhấn mạnh

Nền `e693191ff6caf93b0ca28c9532e0df3a7d622f4d` đạt3116 Core +232 WPF +1 metadata gói, chưa Word runtime. C26 có PreserveEmphasis chỉ cho Body, giữ bold/italic/underline và cỡ từng glyph, tránh Size cả vùng và giữ spacing/căn/thụt source. Writer ký tự tách riêng, kiểm text/valid trước setter; proof style đầy đủ thay lượt đọc size riêng bị trùng, không giảm hậu kiểm. Không mở C27–C29 hoặc no-greeting anchors cho nhấn mạnh. Core/Desktop/fixture Word kiểm riêng/chung chữ/Line, stale/drift/COM/Undo/lưu-mở/nguồn, fixture chỉ biên dịch trong CI. Code .64,1 bước, tổng29/8/1, chưa đóng gói. [Phạm vi](BODY-EMPHASIS.md), [nhật ký](BATCH-AFTER-0.1.63.md); số kiểm mới theo đúng commit tại PR/Actions.

## B05 / chuẩn bị .63 — thân bài không Kính gửi

Nền B04 `9445ce25e7c45805cdb7040c7b267f09f5bb234a` đạt3080 Core +232 WPF, chưa Word runtime. Hai đoạn mở đầu văn xuôi có XML từng indent/căn đều1cm hoặc1,27cm, font/kiểu và layout Word khớp, mốc tên loại/trích yếu và chữ ký/Nơi nhận; quan sát tạm tách quyền sửa. Mốc ngắn đi sau xóa lặp vẫn giữ lỗi chưa chọn phía sau. Core/fixture Word kiểm riêng/chung4 nhóm/chữ/Line, nguồn/Undo/rollback/lưu-mở; fixture chỉ biên dịch trong CI. Đủ5 bước .63, tổng29/8/1; release notes/checklist/TXT/gói theo đúng commit và các kiểm bắt buộc, chưa xác nhận Office2021. [Phạm vi](BODY-OPENING.md), [nhật ký](BATCH-AFTER-0.1.62.md).

## Sau .62, B03/B04 — khoảng đoạn/giãn dòng

Nền B02 `86e16d52421ec772616f2545fbe55e06a72851e8` đạt3007 Core +220 WPF, chưa Word runtime. C28 chỉ cặp văn xuôi liền nhau cùng trang với tổng After+Before chưa thể đạt6pt; C29 Double/Multiple ngoài1–1,5 vềSingle, Exactly/AtLeast/grid và khoảng gộp/auto/contextual còn phải đo. Payload/checkbox riêng; writer chỉ After hoặc rule, không Font/Before/pt/căn/thụt. Ghép snapshot bất biến với C26/C27/chữ trước Line, đối chiếu glyph/thuộc tính/ngữ cảnh/đoạn kế sau lưu/Line; một Undo/rollback. Flow dựa trang đầu/cuối Range hiện tại, XML on/off kế thừa và docGrid; tra ngữ cảnh chung một lần, prefix ràng buộc cuối không đọc lặp cùng đoạn. Core/desktop/fixture Word kiểm riêng/chung/Line/khôi phục/lưu-mở/nguồn, CI chỉ biên dịch fixture.4 bước, tổng29/8/1; .63 chưa đóng gói, không khép phản hồi Office2021. [Chi tiết](BODY-SPACING.md), [nhật ký](BATCH-AFTER-0.1.62.md); bằng chứng kiểm mới theo đúng head tại PR/Actions.

## Sau .62, B02 — căn đều và dòng đầu văn xuôi

Nền B01 `4462ea0670705fc978c05861317e3dbd1d8ffecd` đạt2950 Core +210 WPF, không Word runtime. C27 riêng theo II.6.e, giữ1cm/1,27cm và sửa sai về1,27cm; chỉ ghi căn/thụt đã duyệt. Snapshot chặn stale và hậu kiểm glyph/phông/cỡ/gạch chân, căn/thụt và giãn dòng/khoảng đoạn/auto spacing. Ghép cùng C26 + chữ trước đo lại Line, một Undo/rollback. Phạm vi thường, không nhấn mạnh/mục/bảng/field/căn giữa/phải/hanging/thụt cả đoạn. Test Core, Desktop writer/nhóm và fixture Word riêng/chung/Line/khôi phục/lưu-mở/nguồn; CI chỉ biên dịch fixture. Tổng27/8/1;2 bước, .63 chưa đóng gói, .62 tải cũ giữ nguyên. [Chi tiết](BODY-PARAGRAPH.md), [nhật ký](BATCH-AFTER-0.1.62.md); kết quả mới theo đúng commit tại PR/Actions.

## Sau .62, B01 — phông/cỡ văn xuôi thường

Nền `68865ca574b3c32f170b378080e6663287d036cd` (.62: 2882 Core +206 WPF +1 package; không Word runtime). Code .63 đang phát triển, chưa đóng gói. C26 mới, tổng26/8/1: cần ranh giới tên loại/trích yếu → Kính gửi → chữ ký/Nơi nhận, XML chữ/style/căn đoạn và phông thực tế. Chỉ văn xuôi thường không nhấn mạnh/danh sách/bảng/field; sửa phông/màu/cỡ sai, giữ13/14/gạch chân/căn-thụt. Mở tracking kiểu đã có cho C26, đối chiếu sau lưu/Line. Test Core/GUI và fixture Word sửa riêng/chung/rollback/Undo/lưu-mở/nguồn. Căn cứ đúng II.6.e đã đọc trang nguồn; chưa mở căn/giãn đoạn. [Chi tiết](BODY-FORMAT.md), [kế hoạch đợt](BATCH-AFTER-0.1.62.md). Kết quả tự động theo đúng commit tại PR/CI; fixture Word chỉ biên dịch trong CI, không khép phản hồi Office2021.

## B05 / chốt đợt .62 — khoảng cuối sau chấm phẩy

Tiếp nối B04 `f2ec57528c8339b29046ba0e314d8abe5b2bd7f3`, 2844 Core/OpenXML +206 WPF. C25 thêm dấu cuối ; qua cùng Supported/ClearEnding, chỉ xóa ASCII spaces cuối, giữ dấu/CR/LF/cell-end và trước dấu chưa chọn; không mở mã, tab/NBSP, ô trống hoặc XML chưa chắc. Giữ enum, nhóm spacing, backend/giao dịch/XPS cũ. Core kiểm ba profile, Unicode, loại trừ, sửa riêng/chung và stale/rollback; OfficeParagraphEndCheck mở rộng mẫu giả semicolon, chọn riêng/chung chữ và lưu/mở/Undo/nguồn, CI chỉ biên dịch Word. Chuẩn bị CHANGES/SUPPORTED .62, báo cáo và tài liệu đủ B01–B05; đóng gói chỉ sau kiểm đúng commit, không khép Office2021 .56 bằng CI. [Nhật ký](BATCH-AFTER-0.1.61.md).

## B04 sau .61 — số mục gõ tay nhiều cấp và dẫn chiếu

Tiếp nối B03 `068b4dc4d48dbd05827c7eb42c0f0a4a2e44ae3d`, 2744 Core/OpenXML +201 WPF. OutlineRules có NumberPath2–4 cấp, cha số trực tiếp/duy nhất và sibling cùng kiểu, bắt đầu1; chỉ thay đuôi số của nhãn. Dùng cùng enum/payload/giao dịch cũ, giữ prefix/tab/terminator và proof Word/XML; không đổi Word numPr. NumberedDependents bảo vệ cha khi tiền tố con/hint còn phụ thuộc; nhóm cần cascade giữ thủ công toàn bộ. Index dẫn chiếu cũ/mới/hậu duệ và danh sách/range một lần; tối đa4 ancestor prefixes cho path được hỗ trợ, không cấp phát prefix bậc hai cho ref sâu. Số thập phân chưa chắc không tự thành lỗi; chỉ giữ thận trọng khi thiếu chứng cứ cho sửa. CurrentTextPlan vẫn quét cả cấu trúc trước ghi. [Thiết kế](HIERARCHICAL-OUTLINE.md) · [Nhật ký](BATCH-AFTER-0.1.61.md). Core có nhiều cấp, thiếu/ambiguous/padded/auto/field/tab/reference, stale, chọn riêng/chung và rollback, 1200 dòng giả; Desktop kiểm nhóm/mục cha thủ công. OfficeHierarchicalOutlineCheck có mẫu giả số nhiều cấp + dấu đơn/chữ + Line, giữ prefix/kiểu/tab, một Undo/lưu-mở/nguồn; CI chỉ biên dịch Word. Bốn bước,25/8/1, .62 chưa gói; kết quả đúng commit theo PR38, chưa Word2021 PASS.

## B03 sau .61 — viết hoa tên loại cỡ hỗn hợp, đối chiếu định dạng

Tiếp nối B02 `3e10f46bf8fe431c55973e722d2a7a96d607f222`, 2602 Core/OpenXML +180 WPF. Bỏ chặn C24 chỉ vì PreserveSize khi assessment đã đủ chắc. CasePreservationPlan dựng probe toàn paragraph đã proof từ Word CP thật, không thêm mục sửa; CharacterStyleEvidence giữ phông/cỡ/màu/đậm/nghiêng/gạch chân và căn/thụt đoạn. Chọn kèm CharacterFormat thì chỉ chuẩn hóa thuộc tính theo payload đã duyệt; chọn riêng giữ cả lỗi phông chưa chọn. RetainedGlyphProjection dùng chung với FontSizeEvidence, chiếu qua chữ theo prefix Word thật. WordSession chụp trước ghi, theo dõi Range trong lượt chữ và bind lại paragraph/prefix sau Line; sai chữ/thuộc tính/cỡ thì rollback cùng một Undo. [Thiết kế](CASE-PRESERVATION.md) · [Nhật ký](BATCH-AFTER-0.1.61.md). Core có danh mục/ngoại lệ/projection/stale/phân chọn/drift; Desktop có Word range giả, CP/UTF16, COM lỗi, checkbox và glyph/nét thật dù cỡ bị sai. OfficeCaseStyleCheck tạo ba mẫu giả chọn riêng chữ/phông, kết hợp Line, lỗi sau Line/Undo/lưu-mở/băm nguồn; CI chỉ biên dịch. Ba bước,25/8/1, code .62 chưa gói, chưa Word/Office2021 PASS.

## B02 sau .61 — quốc hiệu/tiêu ngữ cỡ sai trộn cỡ đúng

Tiếp nối B01 đã chốt `3ebcfa965afd6d007f2d38f7200ea7b3bcd40014`, 2471 Core/OpenXML +163 WPF. HeadingFormat.RepairInvalidSizes chỉ hợp lệ với MaximumSize và đúng cặp vai trò quốc hiệu12–13, tiêu ngữ13–14; Original==Replacement. HeadingRules chỉ opt-in khi chữ đã khớp, giữ HÒA/HOÀ và dấu nối được chấp nhận. XML FormatEditable qua Line vẫn riêng với TextEditable; thiếu proof/glyph chưa chắc không mở sửa. Hai loại format gọi chung writer Range ký tự và proof chuẩn hóa cỡ trước lưu/Line; không bỏ nguyên văn, quét mới, một Undo hoặc rollback. Core có role/ngoại lệ/real-owned-Line XML/payload/stale/drift; Desktop có writer lỗi COM và đo glyph/nét tiêu ngữ; fixture Office mới có Line sẵn và CP sau mở lại. [Nhật ký](BATCH-AFTER-0.1.61.md) · [Thiết kế](SELECTIVE-SIZE-REPAIR.md). Tổng25/8/1, hai bước trong code .62 chưa gói; kết quả đúng commit tại PR38, chưa xác nhận runtime Office2021.

## B01 sau .61 — sửa cỡ hỗn hợp, giữ phần đúng

Mốc nguồn v0.1.61 `1a53d2c6f304adc913c139fb4e4f3d3b03d2db46`, CI 2435 Core/OpenXML +144 WPF, gói thêm1 kiểm metadata. Đợt mới code .62, chưa gói, một bước chức năng. CharacterFormat.RepairInvalidSizes chỉ hợp lệ khi PreserveSize và vai trò có khoảng cỡ; FormatPolicy chỉ opt-in ở cơ quan/tên loại/trích yếu/ngày. Giữ nguyên quốc hiệu/tiêu ngữ/Đảng và quyền XML. Không cắt thành nhiều Issue gây chồng với text edit. Chụp cỡ nguồn → chỉ chuẩn hóa ngoài khoảng → chiếu qua sửa chữ → hậu kiểm exact glyph sau lưu/Line. Setter dùng Range ký tự Word thực, đối chiếu toàn text/biên trước khi ghi Size, bỏ ký tự trắng; lỗi vào rollback toàn lượt cũ. [Nhật ký](BATCH-AFTER-0.1.61.md) · [Thiết kế/giới hạn](SELECTIVE-SIZE-REPAIR.md). Core/Desktop có guard, CP/UTF16, lỗi COM, hậu kiểm và lựa chọn; bài Word mở rộng mẫu hỗn hợp, sửa riêng/chung ba Line, một Undo/lưu-mở/băm nguồn, CI chỉ biên dịch. Chưa khép phản hồi .56 trên Office2021.

## B05 / chuẩn bị mốc .61 — sửa nhiều nhóm có mục cần trước

Tiếp nối B04 `52fd60993737f106617889abdc80fa54de2f0ad9`, 2411 Core/OpenXML +127 WPF. Thêm RepairDependencies gắn đúng lỗi cần sửa trước vào Line, nối model checkbox/nhóm/all/single-fix → duyệt rõ → backend. Dùng geometry mới sau chữ/phông; Line hiện đã đạt chỉ đo lại/không ghi nếu vẫn đạt, không nhãn thiếu. Thiếu/phát sinh phụ thuộc chưa duyệt thì dừng/rollback. Nhận mốc cơ quan thường theo cấu trúc số/tên rõ, không đoán tên chính thức hay lấy chủ quản thay ban hành.

PreviewAnchorBatch đọc trang/prefix một lần mỗi CP mỗi export, riêng original/length vẫn kiểm; không cache qua sửa. AdministrativePositionInspection dùng chung dữ liệu vừa đo trên XPS thay đo lặp. Không bỏ lần đo sau phông/Line, không dùng số Shape thay nét. Tests Word component/format không gạt blocker mà dùng đúng dependency inspection/model của UI. Thêm Core/Desktop hồi quy lựa chọn/permission/stale/vòng/geometry mới/rollback/CP và trường hợp nét đang đạt cần đo lại.

Đủ5 hạng mục sau .60, chuẩn bị CHANGES/SUPPORTED .61 và báo cáo có ví dụ, ghi actual CI trong build-checks/TXT trước ZIP/SHA-256. [Nhật ký](BATCH-AFTER-0.1.60.md) · [Thiết kế/chọn nhóm](REPAIR-DEPENDENCIES.md). Giữ main/stable .27 và gói .60; chưa khép runtime Office2021 .56 chỉ bằng kiểm code/CI.

## B04 sau .60 — hạng mục kiểm định dạng theo hệ thống

Người dùng yêu cầu tăng phạm vi thay vì tiếp tục từng lỗi chữ nhỏ. Tiếp nối B03 `e92ec19e048d6805e5cc3fee9eedaee786f10337` đạt 2344 Core/OpenXML +113 WPF. Thêm FormatEvidence gắn text và FormatPolicy dùng chung: phân biệt hợp lệ/sai thật/chưa chắc/sai cỡ hỗn hợp; chấp nhận các cỡ hợp lệ/đen tương đương và bỏ ràng buộc đậm khi vai trò giữ đậm.

WordSession đọc phông sau proof và nhận diện, dedupe vùng; VisibleFormatReader dùng aggregate → Words → Characters từ chưa rõ, chỉ chấp nhận phủ đúng text. Hậu kiểm chữ/phông và sau Line dùng cùng Matches; font repair có thể giữ các cỡ hợp lệ, thêm size fingerprint và chiếu qua sửa chữ để phát hiện cỡ bị ép dù vẫn trong khoảng hợp lệ. Case-only dùng Range.Case, giữ run nhưng vẫn xác minh chữ/Undo/rollback. Không nới quyền thay chữ qua đối tượng, XPS vẫn giữ.

Phạm vi gồm các tiêu đề nhận diện, số/ngày, tên loại/trích yếu, cơ quan/nơi nhận. Kiểm nhiều vai trò và ghép cấu trúc ba Line thật; Desktop kiểm fallback/mất kết nối, phủ text và số lời gọi; fixture Word mới có mẫu đúng/sai, size13/14, whitespace/break styles, sửa riêng/chung ba Line/rollback/một Undo/lưu-mở/giữ nguồn. [Thiết kế](FORMAT-ASSESSMENT.md) · [Nhật ký](BATCH-AFTER-0.1.60.md). Bốn hạng mục, tổng25/8/1, code .61 chưa gói; chưa có bằng chứng runtime khép phản hồi .56, không coi CI là Office PASS.

## B03 sau .60 — câu cuối chỉ một từ có dấu kết câu

Tiếp nối B02 `2b2fb58007215008c1f8718ad103610884497d28`, đã đạt 2236 Core/OpenXML +113 WPF. Clear dùng chung C18/C19 nhận từ hoa đầu/còn lại thường tiếp ngay một dấu .?! cuối đoạn, đuôi chỉ ASCII spaces được giữ; loại thêm viết tắt đã biết ở nhánh mới. Không đổi ClearEnding hoặc guard của các nhóm khác, không thêm enum/nhóm. Ví dụ `Đã hoàn thành .Xong!` sửa riêng hoặc chung thành `Đã hoàn thành. Xong!`, giữ phần chưa chọn.

Kiểm ma trận .?! × .?!, các biên/viết tắt/đuôi tệp/control/Unicode/emoji/CP/XML/stale, ghép lỗi qua nhiều đoạn và phông, rollback khi lỗi lưu; TXT có ví dụ C18/C19 tương ứng. Bài Word giả mới giữ kiểu và một Undo/rollback/lưu-mở/băm nguồn, CI chỉ biên dịch. Đủ 3 bước, code .61, tổng25/8/1, chưa ZIP/tag/release. [Nhật ký](BATCH-AFTER-0.1.60.md). Lỗi cơ quan/trích yếu .56 vẫn chờ runtime, không khép chỉ bằng CI.

## B02 sau .60 — dấu phẩy/chấm phẩy sau ngoặc tròn

Tiếp nối B01 `1908f0387051d96664915be17d456bf0918d0a79`, nhánh feature/format-batch-after-0.1.60; B01 đạt 2159 Core/OpenXML + 113 WPF. C14 nhận `Theo mẫu (nội dung) ,báo cáo.` và giữ `Theo mẫu (nội dung), báo cáo.`. Không sửa chữ/ngoặc; giữ nhóm comma, nguồn quy tắc ứng dụng và phần chưa chọn. Nhánh mới có hai từ trước rõ, ≥2 từ chữ trong, từ phải có chữ thường, cân bằng/không lồng và guard kỹ thuật/header/date/blank/control/XML. Tên hàm dùng chung bộ loại trừ hiện có với C15, không đổi hành vi C15; parse cặp một lần cho nhánh mới, không tăng lượt COM/kết xuất.

Hồi quy NFC/NFD/emoji/CP, mọi chế độ, XML/manual/stale, chọn riêng trong/ngoài, nhóm B01/chữ/phông/hai chấm/dấu câu/cuối đoạn và rollback. Bài Word giả giữ run/đậm/gạch chân, quét/Undo/lưu-mở/băm nguồn và rollback; CI chỉ biên dịch. Mới 2 bước, code .61, tổng25/8/1, chưa ZIP/tag/release. [Nhật ký/ví dụ](BATCH-AFTER-0.1.60.md); lỗi phông .56 và Office 2021 vẫn chờ bằng chứng runtime.

## B01 sau .60 — cụm lặp chỉ khác chữ hoa đầu đoạn

Mốc đầu đợt mới: tag v0.1.60, commit `bd86e13f005fe41d3b9d068a317dd8a8795753ab`, đã có gói với 2105 Core/OpenXML + 113 WPF + 1 kiểm metadata; CI không có Word. Code mới version .61, chỉ một bước, chưa ZIP/tag/release.

C13 nhận `Kế hoạch kế hoạch` và giữ `Kế hoạch`, chỉ khi duy nhất ký tự đầu của từ đầu đoạn khác hoa/thường. Cụm 2–8 tiếng, đối chiếu NFC nhưng giữ nguyên UTF-16/kiểu của lần đầu, chỉ xóa đuôi. Không gộp giữa câu/sau nhãn, từ đơn, mã/allcaps hoặc vượt dấu câu/tab/NBSP. Vẫn duyệt và phải có quyền sửa chữ; quét mới chặn lựa chọn cũ khi first-word/context hoặc XML thay đổi. SameUnit dùng vòng lặp thoát sớm thay Range/All trong so sánh cụm; không tuyên bố tăng tốc Word khi chưa đo.

Kiểm hồi quy cũ và mới gồm Unicode tổ hợp, offset, giữ phần chưa chọn, các chế độ, XML, stale, tooltip/report, sửa chung khoảng trắng/hai chấm/dấu kết câu/cuối đoạn/phông và rollback. Bài Word giả mới có giữ đậm/gạch chân, sửa riêng/chung, quét, Undo/rollback/lưu-mở/băm nguồn; CI chỉ biên dịch. [Nhật ký hiện tại](BATCH-AFTER-0.1.60.md). Lỗi phông cơ quan/trích yếu .56 vẫn chờ bằng chứng runtime, main/stable .27 giữ nguyên.

## B05 / mốc bản thử .60 — gom đủ 5 bước

Tiếp nối B04 `9e5449df1655acb789b01726767a71de126e0922`, nhánh feature/format-batch-after-0.1.59. C15 thêm `)` → `) ` khi các từ trước/trong/sau đủ rõ, giữ guard hàm/công thức/ngoặc thiếu-lồng/ô trống/header/date/control/XML. Các mép độc lập, giữ phần chưa chọn; cùng FixKind/Family parenthesis, property UI không cấp CanFix. Core có Unicode/CP, guard, một mép/cả nhóm, stale khi đổi quyền/context, ghép phông/cụm lặp/hai chấm/C25 và rollback lỗi lưu. Local2028 assertion. OfficeParenthesisAfterCheck giả giữ kiểu, quét/Undo/rollback/lưu-mở; CI chỉ biên dịch.

Đợt đủ5 bước B01–B05, tổng25/8/1. .60 có CHANGES và bảng C01–C25, báo cáo ví dụ, ghi chú Office2021 chờ; lịch sử TXT ghi đủ5 bước. Chuẩn bị một gói Windows mới sau kiểm bắt buộc; workflow package thêm nhánh release/windows-<version> được tạo chủ động khi connector không dispatch/tag được. Guard nhánh/version và tag có sẵn ngăn ghi đè; nhánh feature vẫn chỉ tests, không packaging mỗi bước. Bằng chứng theo đúng commit ở PR36/release, không lấy CI làm Word PASS hoặc khép lỗi .56. Main/stable .27 giữ nguyên. [Nhật ký đợt](BATCH-AFTER-0.1.59.md) · [Ghi chú .60](CHANGES-0.1.60.md).

## B04 — C25 khoảng trắng cuối đoạn

Tiếp nối B03 `ef45c4a1e12c8f305f2e298aaee9e5a43ded4819`, cùng nhánh đợt. ParagraphEndSpaceRules chỉ xóa ASCII gap sau một dấu kết câu cuối đoạn có chữ đủ rõ, giữ CR/LF/kết ô và phần chưa chọn. SentenceSpaceContext.ClearEnding tách nguyên điều kiện trái đang dùng cho Clear, không đổi guard cũ; số/ngày/mã/technical/NBSP/control/không kết câu vẫn ngoài phạm vi. FixKind.ParagraphEndSpace append cuối enum; BatchPlan chặn payload không phải xóa space thuần/giả định dạng, cho chia sẻ span phông. CurrentTextPlan vẫn phải quét mới trước ghi. C25 cùng Family spacing cạnh C01, ReviewSuggestion, nguồn quy tắc ứng dụng.

TXT thêm C25/ví dụ giữ literal trailing spaces trong dấu nháy; tổng25/8/1, lịch sử .60 nay ghi đủ B01–B04. Local1960 assertion, có chọn riêng phần trước/sau và giữ dấu kết đoạn, tổ hợp/emoji, kiểu chữ, stale/context/payload và rollback sau lỗi lưu. OfficeParagraphEndCheck được gọi ở cuối bài Word tự động, có mẫu giả sửa/quét/Undo/rollback/lưu-mở/giữ nguồn, chỉ biên dịch trong CI; không đánh dấu Word runtime đạt. Hiện4 bước, chưa đóng gói .60. [Nhật ký/checklist](BATCH-AFTER-0.1.59.md).

## B03 — Khoảng sau dấu hai chấm

Tiếp nối B02 `7e996ba19dd0249bb9dec584612deaf73cf847f8` trên cùng nhánh đợt. ColonSpaceRules nhận thiếu sau bằng span `:` → `: `, chỉ khi qua cùng bộ nhận diện chữ rõ của C17; bỏ điều kiện cũ buộc phải có dấu cách sau mới xét phía trước. Hai lỗi độc lập cho hai phía, span dương/liền nhau không chồng; không viết lại từ. Không thêm FixKind hoặc nới BatchPlan. Issue.AddsSpaceAfterColon chỉ phân biệt nhãn/Suggestion/tooltip, không cấp CanFix; cùng Family colon/ReviewSuggestion. Giờ, tỉ lệ, chữ hoa/mã, protocol, đường dẫn, tab/control/header/ngày và vùng chưa đủ giữ guard cũ.

C17 đổi tên/phạm vi/ví dụ TXT, số nhóm vẫn24/8/1. Core 75 assertion mới và cập nhật một negative cũ, local1880. OfficeIntegrationCheck mẫu hai chấm có cả thiếu sau/thừa trước; sửa riêng từng phía giữ phần chưa chọn và kiểu chữ, vẫn quét/Undo/rollback/lưu-mở; chỉ được biên dịch trong CI, chưa có Word runtime. Nhật ký đợt có ví dụ và trạng thái chính xác; hiện3 bước, chưa đóng gói .60, không nhập main. [Chi tiết](BATCH-AFTER-0.1.59.md).

## B02 — Đối chiếu tên loại và ký hiệu

Trên nhánh đợt sau .59, tiếp nối B01 `030d599daf8b8db0aeeac0da0ee2295b8c193263`. AdministrativeHeaderRules thêm cảnh báo TypeMismatchTitle thuộc AdministrativeNumber, CanFix=false; chỉ đối chiếu namedType chữ thuần duy nhất trước nội dung và prefix đã có trong danh mục NamedHeading, bỏ bản sao/unknown. Căn cứ NĐ30 PL I.I.II.3.b PDF22 đã đọc bản ký, bảng PL III.I PDF40. Prefix khác loại có một cảnh báo chính, không thêm cảnh báo lowercase trùng trên prefix đó; mã cơ quan chữ thường vẫn xét riêng. Không chọn đổi tên hay mã. Tooltip ba dòng, Family number/Manual, M04 và ví dụ TXT cập nhật; 24/8/1 giữ nguyên.

Sửa số/ký hiệu khác vẫn qua lựa chọn/CurrentTextPlan/BatchPlan/atomic cũ; hậu quét giữ mâu thuẫn cho đến khi sửa đúng một bên, không xóa theo lịch sử nút bấm. OfficeComponentCheck fake title BCĐX đi cùng code BCĐX để tách mục tiêu hồi quy. Core mới 117 assertion, local tổng1806; Windows CI/commit ở PR36. Không có Windows package/Word runtime trong B02; hiện 2 bước trong chu kỳ 5–10, giữ version chuẩn bị .60. [Nhật ký](BATCH-AFTER-0.1.59.md).

## Chu kỳ mới sau 0.1.59 — 24/10/2026 (UTC+7)

Theo yêu cầu mới, tích lũy khoảng **5–10 bước phát triển có kiểm thử** rồi mới tạo gói tải Windows. Mỗi bước vẫn ghi code, kết quả kiểm thử, giới hạn và việc tiếp theo vào [nhật ký đợt](BATCH-AFTER-0.1.59.md); không tính các thao tác commit/test/docs thành nhiều tính năng để đủ số. Đọc [quy trình phát hành](RELEASE-CADENCE.md) trước khi đóng gói.

Nhánh `feature/format-batch-after-0.1.59` dựa trên commit .59 `03bf183746d37f0ca543f386faf8045f2ee9a090`. Code đang chuẩn bị .60, **chưa có gói .60**; gói tải mới nhất vẫn .59. Windows checks chạy khi push, chỉ kiểm thử/build/log; Windows package chỉ chạy khi chủ động tạo tag hoặc gọi thủ công, đòi ghi chú và checklist đúng phiên bản. Không tạo tag/phát hành trong bước này.

B01 mở rộng C05: FindAbbreviation so sánh OrdinalIgnoreCase với danh mục, chỉ thay hoa/thường prefix đứng trước dấu nối đầu tiên; không đổi ký tự, dấu tiếng Việt, số hay mã cơ quan. Chỉ sửa khi có một tên loại tương ứng duy nhất ở đầu văn bản, chữ thuần và trước nội dung; thiếu/trùng/khác tên thì giữ thủ công, tránh đổi mã cơ quan của công văn. Bản sao chưa được cấp quyền sửa mã theo nhận diện tên loại gốc. Canonical QyĐ/CTr/TTr/TrS giữ nguyên. Unknown/CV/thiếu dấu/mẫu riêng, trang hai, tab, nhiều dòng ứng viên hoặc XML chưa đủ vẫn qua guard cũ. BCĐX giữ nguồn người dùng trong citation/tooltip. CurrentTextPlan, BatchPlan, AtomicTextBatch và hậu kiểm Word giữ nguyên. Lỗi .56 vẫn chờ bằng chứng runtime; không lấy CI làm Word PASS.

## Mốc tiếp tục 0.1.59 — 24/10/2026 (UTC+7)

DocumentTypeCatalog là nguồn chung: 27 administrative + 3 copy của NĐ30 PLIII.I PDF40, 1 user BCĐX. Exact abbreviations QyĐ/CTr/TTr/TrS; source/kind riêng, copy không EligibleHeading. Nghị quyết/Quyết định qualifier cá biệt thuộc reference, heading không thêm qualifier. Giấy uỷ/ủy quyền cùng entry nhưng C24 giữ nguyên cách đặt dấu. ShapeLineRules không giữ hashset13 rời; DocumentHeadingRules/WordSession dùng nguồn chung. Custom-name provenance đi theo citation/LineTarget.UserDefinedType, tooltip và TXT. AdministrativeHeaderRules chỉ miễn cờ lowercase cho prefix canonical, vẫn kiểm mã cơ quan và không tự đổi viết tắt theo title.

DocumentTypeCatalogWindow read-only không gọi Word, menu Trợ giúp; TXT có toàn bộ bảng ba nguồn. Core đối chiếu31 viết tắt, 28 tên/case/font/Line/nguồn, alias/ngoại lệ, codecases; WPF reference-window thực + ảnh public reference. OfficeComponentCheck giả chuyển title sai sang Báo cáo đề xuất để kiểm source mới trong batch phông/Line/Undo/rollback/lưu-mở, CI chỉ biên dịch. [Chi tiết/checklist](CHANGES-0.1.59.md). Giữ24/8/1, XPS/proof/rollback; chưa Office2021 PASS hoặc khép lỗi .56.

## Mốc tiếp tục 0.1.58 — 24/10/2026 (UTC+7)

Thêm FixKind.DocumentTypeText/C24 cho chính xác 13 tên loại đã qua cùng guard khối đầu Nhà nước của DocumentHeadingRules. Căn cứ NĐ30 PL I.II.5.b đã đối chiếu PDF 24. Thay đúng span tên (giữ ASCII spaces ngoài), ToUpperInvariant cùng độ dài; TextEditable bắt buộc, không nâng quyền qua FormatEditable. Không viết thường trích yếu, không chọn/đổi tên loại theo nội dung. BatchPlan kiểm payload và chỉ cho chia sẻ span với DocumentHeadingFormat; CurrentTextPlan quét lại trước ghi. Giữ range phông track qua thay chữ, expected story, rollback và một Undo.

Core mẫu 13 tên, ngoại lệ/stale/payload, giữ nội dung/ngắt đoạn và batch phông; OfficeComponentCheck giả thêm Báo cáo, kiểm ghép nhóm/Undo/rollback/lưu-mở, chỉ biên dịch trong CI. Báo cáo C24/ví dụ, 24/8/1; M03 giữ trích yếu toàn hoa/vùng chưa rõ. Xem [ghi chú .58](CHANGES-0.1.58.md).

Trạng thái chưa khép: người dùng xác nhận lỗi cơ quan/trích yếu trên .56, đang chờ log/DOCX sau sửa và kết quả .57 hoặc mới hơn. Không coi phát triển C24 hoặc Core/WPF PASS là Office2021 PASS. Tiếp theo ưu tiên nhận bằng chứng runtime và thể thức phần còn thiếu; ngữ cảnh/từ điển sau cùng.

## Mốc tiếp tục 0.1.57

Phản hồi lặp lại sau .56 chưa có log/DOCX sau sửa hoặc xác nhận bản đang chạy, không kết luận root cause thực tế đã được khép lại. Bổ sung nhánh Word AlternateContent/Choice wps + fallback Line không chữ và run gộp trong OwnedLineFormatProof, không bỏ run chứa chữ. VisibleFormatReader dùng actual Characters Range khi Font vùng không xác định, đối chiếu toàn chuỗi; dùng cả khi hậu kiểm phông nhiều nhóm. C20/C21 CanFix vẫn Bắt buộc sửa; phiên bản UI lấy từ assembly, log thêm MVID Desktop/Core và cờ định dạng khi manual.

RepairProgressTracker/WordSession.Progress chỉ quan sát giao dịch; cập nhật bước sửa/đo/kiểm/phục hồi, số hậu kiểm tạm thời. RepairProgressDialog có STA riêng cho popup/heartbeat, COM giữ STA hiện có; không async Word hoặc bơm reentrant UI. Giao diện chính bị khóa trong lượt; chỉ hoàn tất sau EditThenScan và Scan thành công, rollback reset đếm. Xem [nội dung .57](CHANGES-0.1.57.md). Kiểm Core/COM giả/WPF thực không thay runtime Office2021. Tiếp theo cần log đúng phiên bản, DOCX sau sửa và bài Kiểm tra Word; bảo toàn nét XPS, một Undo/rollback, chưa mở ngữ nghĩa/thư viện.

## Mốc tiếp tục 0.1.56

Từ .55, sửa cảnh báo phông mới sau chèn Line ở cơ quan/trích yếu: `OwnedLineFormatProof` cấp riêng FormatEditable cho đoạn XML/live khớp có Line nổi của ứng dụng, marker/ID/vai trò duy nhất, không chứa chữ và run riêng. TextEditable qua drawing vẫn false; ComponentRules dùng hai quyền riêng cho phông/chữ. Không xóa run thật, không thay kiểm nét XPS hay giới hạn hình học. C20/C21 CanFix chuyển Bắt buộc sửa theo lựa chọn người dùng, không đổi căn cứ/CanFix/duyệt.

Hồi quy dùng drawing từ DocxLineEditor, tái hiện lỗi trên map/guard .55, chặn foreign/inline/embedded/ID/role sai; bài Office giả có cả ba Line, phông sai, rollback sau Line, Undo, lưu/mở. Chưa chạy runtime Office 2021; xem [ghi chú và checklist .56](CHANGES-0.1.56.md). Các bước tiếp: nhận kết quả đúng phiên bản trên Office 2021, tiếp tục hoàn thiện thể thức/lỗi chữ trong phạm vi chắc chắn; ngữ nghĩa/từ điển sau cùng. Dữ liệu người dùng ở ngoài kho và release.

## Thứ tự ưu tiên theo người dùng — 23/10/2026

1. Hoàn thiện các kiểm/sửa thể thức theo nguồn hiện có, giữ bằng chứng và hồi quy.
2. Lỗi chữ có quy tắc rõ, khoảng trắng, từ/cụm từ giống nhau lặp liên tiếp; trước hết phát hiện cơ học và gợi ý có duyệt. Chưa có từ điển thì không quảng bá là đã kiểm đầy đủ chính tả.
3. Ngữ cảnh, ngữ nghĩa và thư viện phục vụ các phần khó thực hiện sau cùng. Không bổ sung suy đoán ngữ cảnh trong khi người dùng đang thử phần cơ bản.

## Bắt đầu từ đâu

Mốc đã chốt: [0.1.27](STATUS-0.1.27.md), mã gói `1ec3092ccd68ed0acd8878b2737664116a6b11fe`, tag `v0.1.27`. Đọc biên bản này trước để phân biệt bằng chứng CI, xác nhận người dùng và kịch bản còn chờ. Phát triển từ `main` sau khi chốt, tạo nhánh mới; không tiếp tục phát hành vào tag hoặc ghi đè ZIP 0.1.27.

Nhánh đang phát triển và nền chính xác được ghi ở đầu tài liệu và nhật ký đợt. Chuỗi bản thử dựa trên main đã chốt `da6cf3b0999dbfb60d16ab1eae9733b0f1f20c81`; chưa nhập main hoặc coi CI là bằng chứng Office 2021.

## Bản đồ mã nguồn

| Thành phần | Nơi đọc/sửa |
|---|---|
| UI, lựa chọn lỗi, quét lại, preview | `src/ChinhTa.Desktop/MainWindow.xaml.cs`, `IssueChoice.cs`, `PreviewHighlighter.cs` |
| Phiên Word, checkpoint, giao dịch sửa, export, save/undo | `src/ChinhTa.Desktop/WordSession.cs` |
| Cảnh báo vị trí số/ngày trên XPS | `src/ChinhTa.Core/AdministrativePositionRules.cs`, `src/ChinhTa.Desktop/AdministrativePositionInspection.cs` |
| Bộ kiểm Line dùng cho danh sách | `src/ChinhTa.Desktop/LineInspection.cs`, `ReferenceMeasurement.cs` |
| Nhận diện vùng và chính sách độ dài/tâm/khoảng cách | `src/ChinhTa.Core/ShapeLineRules.cs` |
| Ghép XML với range Word thật, cấu trúc bảng | `src/ChinhTa.Core/WordXmlTableMap.cs` |
| Marker/kiểm kê/gói Line | `src/ChinhTa.Core/DocxLineMetadata.cs`, `DocxLineInventory.cs`, `DocxLineEditor.cs` |
| Đối chiếu mốc và CP/chuỗi | `src/ChinhTa.Core/LineReferenceBinding.cs` (gồm `WordPreviewAnchor`) |
| Đo glyph, kiểm nét, phản hồi vị trí | `src/ChinhTa.Core/GlyphAdvanceRange.cs`, `RenderedLineVerifier.cs`, `LinePlacementFeedback.cs` |
| Giao dịch chữ/số/ngày, đối chiếu toàn bộ chữ | `src/ChinhTa.Core/AtomicTextBatch.cs`, `tests/ChinhTa.Checks/AtomicTextBatchChecks.cs` |
| Giao dịch nhiều Line và rollback tệp | `src/ChinhTa.Core/AtomicLineBatch.cs`, `CheckpointSwap.cs` |
| Số/ký hiệu, địa danh/ngày tháng ở đầu Nhà nước | `src/ChinhTa.Core/AdministrativeHeaderRules.cs`, `tests/ChinhTa.Checks/AdministrativeHeaderChecks.cs` |
| Ngôn ngữ minh họa, khoảng trắng, thể thức/giấy/lề | `src/ChinhTa.Core/Checker.cs`, `SpacingRules.cs`, `HeadingRules.cs`, `PageRules.cs` |
| Nhật ký và bài kiểm Word trên mẫu giả | `src/ChinhTa.Desktop/Diagnostics.cs`, `OfficeIntegrationCheck.cs` |
| Regression | `tests/ChinhTa.Checks`, `tests/ChinhTa.DesktopChecks` |

## Luồng Line hiện tại

1. Quét range Word; đối chiếu XML và text trước khi nhận diện vùng. CP lấy từ Word, không suy ra bằng độ dài XML.
2. Dựng XPS; ánh xạ CP bằng Range.Text của tiền tố thật. Đo dòng cuối bằng glyph advances/cluster và ghép nét theo vị trí thực.
3. Trước sửa, kiểm nguyên văn, lưu checkpoint riêng. Chỉ sửa marker của ứng dụng; Shape lạ hoặc vùng mơ hồ phải giữ cảnh báo thủ công.
4. Ghi DrawingML vào candidate từ checkpoint không bị khóa; mở candidate, lấy lại layout/mốc và kiểm chữ, marker, nét thật, tham chiếu và các nét cần giữ.
5. Có tối đa một candidate bù vị trí theo phản hồi đủ chắc chắn. Khi quay về checkpoint, chuyển cả kế hoạch mã hóa và đích sang CP của đúng nguồn đang mở trước khi ghi lại. Metadata đích không thay cho nét XPS.
6. Trong giao dịch nhóm, sửa theo Agency → Subject → Motto. Lưu/quét lại, có tối đa một lượt đặt lại các cảnh báo đủ điều kiện của đúng lựa chọn đã đối chiếu. Kiểm cuối chưa đạt thì rollback cả lượt. Hoàn tác giữ một checkpoint cho cả lượt.
7. UI quét lại từ trạng thái mới. Lịch sử nét đã sửa luôn phải được xác minh lại với XPS và dòng tham chiếu; không xóa cảnh báo theo lịch sử bấm nút.

## Những điều phải giữ khi thay đổi

- Không quay lại getter Shape COM AlternativeText/ID/Type hoặc setter AddLine/LineFormat để né lỗi đã gặp trên Office 2021.
- Không báo đạt dựa vào số Shape, ghi XML thành công, marker hoặc tọa độ metadata.
- Không dùng CP như chỉ số Range.Text; không đo biên chữ bằng caret hit testing. Không đoán mốc thiếu/trùng hoặc glyph không hỗ trợ.
- Khi ghép lại mốc, giữ điều kiện toàn bộ story không đổi, đúng role/nguyên văn duy nhất và điều kiện bố cục hiện tại. Không nới lựa chọn sang lỗi chưa chọn.
- Giữ bù tọa độ độc lập với đích trang; thay Line đã có phải giữ các nét khác. Không bù vô hạn hoặc ẩn cảnh báo để hoàn tất.
- Giữ chữ/định dạng không liên quan, file gốc, checkpoint, một Undo và rollback. Nếu rollback cũng lỗi, giữ hiện vật phục hồi.
- 3/7 là đích tạo cơ quan/trích yếu; khoảng 1/3–1/2 vẫn hợp lệ. Khoảng cách và dung sai hiện tại là chính sách đo, không tự gán thành con số bắt buộc của văn bản pháp lý.
- Chỉ miễn khoảng trắng trong mẫu trường số/ngày chưa điền được nhận diện hẹp. Không miễn mọi khoảng trắng trong đoạn hoặc trong nội dung thường.
- Không đưa tài liệu/log/XPS/font riêng vào GitHub hoặc ghi chữ của tài liệu vào diagnostics. Mẫu đưa lên kho phải là mẫu giả.

## Kiểm thử phù hợp với thay đổi

Trên Windows với .NET 8 SDK:

```powershell
dotnet run --project tests/ChinhTa.Checks -c Release
dotnet run --project tests/ChinhTa.DesktopChecks -c Release
```

Mốc 0.1.27: 223 Core/OpenXML + 20 WPF, [CI](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/39570349615). Không cố giữ số đếm này khi thêm test; ghi kết quả mới theo đúng commit. Linux chỉ chạy Core/biên dịch chéo được, không thay thế Word COM hoặc WPF runtime trên Windows.

Khi thay Word/Line/layout: chạy **Kiểm tra → Kiểm tra Word tự động…** trên máy có Word và lưu kết quả phiên bản tương ứng. Thử cả một đường, nhóm 1+3 và cả ba; quét lại không còn lỗi đã sửa; lưu/mở lại; một Undo bỏ cả lượt; thử tình huống sai để kiểm rollback. Các mục này là checklist, không được tự đánh dấu đạt chỉ vì CI pass. Các kịch bản cũ và phạm vi đã xác nhận nằm trong [WINDOWS-VALIDATION.md](WINDOWS-VALIDATION.md).

Tài liệu riêng chỉ dùng cục bộ theo đường dẫn tùy chọn của `tests/ChinhTa.Checks`; không commit fixture riêng. Không cần chạy lại toàn bộ runtime/build chỉ vì sửa tài liệu; phải bảo đảm mã ứng dụng và gói đã thử không bị thay.

## Đóng gói và lưu mốc

```powershell
dotnet publish src/ChinhTa.Desktop -c Release -r win-x64 --self-contained true -o artifacts/win-x64
```

Kèm thư mục `samples`, ghi chú phiên bản, ZIP và SHA-256. Chạy kiểm thử trước khi phát hành. Khi tạo bản mới, cập nhật version trong csproj, tiêu đề UI, tên gói/tag, ghi chú và workflow Windows; không phát hành lại vào tag cũ. Theo chu kỳ mới, `windows-checks.yml` kiểm thử mỗi bước trên nhánh, không publish/ZIP/release. `windows-package.yml` chỉ chạy thủ công hoặc khi push tag `v*`; đọc version/ghi chú/checklist từ commit hiện tại, chạy lại các kiểm bắt buộc rồi mới đóng gói. Xem RELEASE-CADENCE; không ghi đè tag/asset đã phát hành. Workflow `finalize-0.1.27.yml` chỉ chốt metadata của bản đã thử, có kiểm tag, digest, CI và thay đổi chỉ thuộc tài liệu/workflow được cho phép; không build hoặc thay asset.

Khi nhận lỗi, lấy log đúng version và phân biệt lỗi đọc Word, ghép XML, CP/mốc, marker, candidate, nét XPS hay rollback. Các mã `Word.Preview.CpMapping`, `Word.Preview.Unmapped`, `Word.LineBatch.ReferenceBinding.*`, `Word.LineBatch.StoryBinding`, `Word.LineBatch.Rescan` ghi số liệu mà không ghi chữ tài liệu. Không kết luận nguyên nhân chỉ từ ngoại lệ tổng quát hoặc một điều kiện guard gộp.

## Việc tiếp theo chưa triển khai

1. Bổ sung xác nhận riêng lưu/mở lại, một Undo và bài Office check đầy đủ trên 0.1.27 khi có máy Word; không ghi là đã PASS thực tế trước khi có kết quả.
2. Số/ký hiệu và địa danh/ngày tháng đã triển khai hẹp ở nhánh thử 0.1.28; chờ CI và Office thực tế. Không tự mở rộng phạm vi khi chưa có mẫu và căn cứ. Cần kiểm cả sửa riêng, nhóm, quét lại, lưu/mở và Undo; giữ nguyên ô trống và cấu trúc Word.
3. Sau đó mới mở rộng tên cơ quan, nội dung, chữ ký/nơi nhận, số trang hoặc kiểm chính tả tiếng Việt thực sự. Bộ ngôn ngữ hiện vẫn là minh họa, chưa có từ điển hoàn chỉnh.

Không gộp tính năng mới vào đợt chốt 0.1.27. Mỗi đợt tiếp theo cần phạm vi, căn cứ, mẫu giả, test có ý nghĩa và ghi rõ phần chưa xác nhận.

## Ghi chú triển khai 0.1.28

`Paragraph` có dấu quan sát định dạng và bằng chứng chữ thuần. `WordXmlTableMap.Read` chỉ cấp bằng chứng sau khi khớp XML với range sống; trường/control/link/tab hoặc tiền tố chưa khớp không được sửa tự động. Nhận diện đầu Nhà nước tách khỏi các profile khác; quy tắc khoảng trắng chỉ nhường các span tự sửa và giữ các dòng cấu trúc. Gợi ý số/ngày dùng `BatchPlan` và giao dịch hiện có, không tạo cơ chế viết toàn đoạn mới. Mẫu giả sinh bằng `scripts/create_header_samples.py`, không lấy từ tài liệu người dùng.

## Ghi chú triển khai 0.1.29

Đã tái hiện hai lỗi biên trên 0.1.28 trước khi sửa: gợi ý khoảng trắng có thể thu hẹp ô ngày trống khi có ghi chú cuối dòng, và `w:pPr/w:tabs/w:tab` bị coi nhầm là ký tự tab. `DateBlankZones` đọc riêng các vùng trống trong đầu Nhà nước đã nhận diện; không dùng parser này để sửa nội dung hoặc chấp nhận ngày có ghi chú là đúng thể thức. Chỉ miễn đúng vùng trống, các khoảng trắng còn lại vẫn kiểm. `WordXmlTableMap` chỉ coi `w:tab` trong `w:r` là ký tự; trường/control/link và các bất định khác vẫn giữ thủ công. Không thay luồng sửa/kiểm nét XPS hoặc rollback Line.

Test mới quét lại sau sửa riêng/nhóm với CP đã dịch theo độ dài mới, kiểm các vùng trống có chú thích và điểm dừng tab so với tab thật. Office check tự tạo một bản sao mẫu giả có ghi chú/điểm dừng tab, kiểm nguyên văn sau sửa, một Undo và lưu/mở; chưa có kết quả runtime trên máy Word. Không đưa file riêng vào kho.

Bằng chứng 0.1.28: [CI](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/39630158659), 283 Core/OpenXML + 20 WPF, tag/gói giữ nguyên. Kết quả 0.1.29 phải lấy theo commit và CI riêng; không chuyển xác nhận Office của 0.1.27 sang tính năng mới.

## Ghi chú triển khai 0.1.30

Đã thay nhánh sửa chữ trong `WordSession.FixManyCore` bằng `AtomicTextBatch`. Trước đó lỗi sau một phần thao tác chỉ báo dùng Undo, lỗi lưu cuối ở ngoài catch và checkpoint được đẩy lên stack trước khi hoàn tất. Nay kiểm toàn bộ nguyên văn trước ghi, ghi theo CP giảm dần, đọc lại range đã thay, đưa lưu và kiểm toàn bộ chữ vào trong giao dịch. Chỉ đẩy một checkpoint lên stack sau khi thành công.

Kế hoạch chữ dự kiến lấy vị trí từ `Range.Text` của tiền tố thật và `WordPreviewAnchor`; không dùng CP làm chỉ số chuỗi. Chỉ thay đúng span đã chọn. Rollback đóng phiên Word riêng, nạp checkpoint và kiểm chữ gốc. Nếu rollback lỗi, giữ cả lỗi gốc và lỗi phục hồi, `snapshotCurrent=false` để Dispose không xóa thư mục. Chưa xác minh được phục hồi thì không ghi là đã khôi phục. `Word.TextBatch.*` chỉ ghi mã, số bước và stack, không ghi chữ tài liệu. Không tự phát lại thao tác ghi.

Ứng dụng dùng stack checkpoint để Hoàn tác nên nhánh chữ không còn phụ thuộc Word UndoRecord; Undo công khai vẫn dùng checkpoint như trước. Nhánh Line, bù vị trí, kiểm marker/nét XPS và rollback Line giữ nguyên. Giao dịch nhóm chữ có thể gồm gợi ý định dạng tiêu đề; phục hồi từ DOCX giữ cả chữ và định dạng cũ, còn kiểm sau lưu mới đối chiếu toàn bộ chữ, chưa chứng minh mọi định dạng trên Word.

Test gây lỗi bao gồm từng bước ghi, đọc lại sai, nguồn thay sau preflight, lưu lỗi, hậu kiểm thấy thay đổi ngoài vùng và rollback lỗi; ghép thật với `CheckpointSwap` trên tệp giả, không cần dữ liệu riêng. Office check thêm lỗi có chủ đích sau bước đầu/cuối trong phiên riêng, kiểm khôi phục nguyên văn, giữ Undo của lượt trước và sửa tiếp. UI không bật điểm gây lỗi này. Chưa có kết quả Office thực tế cho 0.1.30.

## Ghi chú triển khai 0.1.31

`CharacterFormat` là payload chỉ đổi phông/cỡ/nghiêng/màu của dòng số/ngày thuần đã nhận diện, giữ Original == Replacement. `FixKind.AdministrativeFormat` được thêm ở cuối enum. Dòng phải có mẫu rõ, quan sát định dạng và không trộn phông/cỡ/nghiêng/màu. Trường/tab/link/control hoặc mẫu không rõ giữ thủ công. Cỡ ngày đang trong 13–14 được giữ, ngoài khoảng chọn 13; số là 13 đứng. Đậm/gạch chân/căn đoạn không nằm trong payload. Căn cứ đọc lại NĐ30 PL I Phần I.II.3.c, 4.c, PDF 23; chưa thêm chính sách tọa độ/căn giữa tương đối.

`BatchPlan` chỉ cho phép chồng lấn có chủ đích giữa đúng payload định dạng không đổi chữ và gợi ý chữ thuần nằm trọn bên trong; định dạng cạnh tranh, đổi chữ giả dạng format, vượt biên hoặc các chồng lấn khác vẫn chặn. Thực hiện toàn bộ CharacterFormat trước, rồi chữ theo CP giảm dần. Lựa chọn vẫn đúng tập đã chọn; không tự kéo gợi ý khác vào. `AtomicTextBatch.ExpectedStory` không ghi đè chữ bởi payload format. Quy tắc khoảng trắng không để gợi ý format cả dòng che lỗi nhỏ.

Word quan sát font trên phần chữ, loại dấu kết thúc đoạn/ô để không coi font khác của dấu đoạn là font trộn. Payload mới chỉ gọi Font.Name/Size/Italic/Color. Theo dõi range sống, dựng chữ dự kiến trong từng dòng bằng prefix thật; sau lưu kiểm chữ toàn story và chữ/phông/cỡ/nghiêng/màu của các dòng vừa định dạng. Không đạt thì dùng rollback 0.1.30; giải phóng range trước khi đóng Word phục hồi. Giữ một checkpoint Undo cho cả nhóm. Các luồng Line/XPS/bù vị trí không đổi.

Test Core mới kiểm sửa riêng/chung, cỡ hợp lệ, ô trống, dữ liệu trộn/ngoài phạm vi, ngoại lệ chồng lấn hẹp, chữ ngoài vùng và rollback mô phỏng cả chữ/lẫn định dạng. Office check tạo mẫu giả Arial/đỏ với đậm/gạch chân để kiểm định dạng riêng, lưu/mở, Undo, rollback sau hai bước format và một bước chữ, rồi sửa chung trong một lượt. Kiểm kiểu đậm/gạch chân/căn đoạn bằng getter chỉ trong phiên Office check. Các bài Word mới còn chờ chạy thực tế; không gán kết quả CI thành Office PASS.

## Ghi chú triển khai 0.1.32

Cảnh báo vị trí là read-only. `AdministrativePositionRules.Identify` dùng các ứng viên đầu Nhà nước đã nhận diện, range/XML cùng ô bảng đơn được xác minh. Dòng số cần toàn bộ phần tên cơ quan in hoa trước nó trong ô; ngày cần cả quốc hiệu và tiêu ngữ trong cùng ô phía trên. Thiếu vùng/mốc không được coi là đã kiểm đạt. Đầu ngoài bảng và bảng chưa ghép chắc chắn chưa được đánh giá hình học đầy đủ. Trường/control/tab thật, ứng viên trùng hoặc ngoài profile/trang bị loại theo nhận diện hiện có.

`PreviewHighlighter.Measure` đi qua cùng phép đo advances cũ; chỉ đường mới `MeasureSingleLine` thu thêm baseline theo transform thật. Nhiều glyph run cùng baseline hợp lệ; nhiều baseline, xoay, cắt clip, khung ngoài trang hoặc ánh xạ thiếu trả chưa đo. Không dùng caret hit testing, số Shape, cờ căn giữa hay chiều dài XML thay cho vị trí thật. Không dùng lại dòng cuối của đoạn xuống dòng để kết luận vị trí cả dòng.

Số so tâm với bao khối chữ cơ quan; ngày so tâm với cả quốc hiệu/tiêu ngữ khi hai mốc đồng tâm trong dung sai. Kiểm nằm dưới bằng khung glyph; cùng hàng dùng baseline thật để không lệch giả do cỡ 13/14 khác nhau. 3 pt là ngưỡng phát hiện của bản thử, 0,5 pt là dung sai đo nhỏ cho baseline/biên, không gán thành số bắt buộc trong NĐ30. Nếu quốc hiệu/tiêu ngữ không đồng tâm, không tùy chọn một mốc để báo ngày đúng.

UI dùng XPS đã xuất trong Scan và cùng cache preview, thêm probe CP thật cho dòng/mốc, không xuất Word lần hai. Mọi cảnh báo vị trí CanFix=false. Không có fallback đánh dấu theo chuỗi đơn nếu anchor Word thiếu. Thay font/chữ vẫn dùng giao dịch 0.1.30–0.1.31 và quét lại dựng XPS mới; Line/rollback giữ nguyên. Cảnh báo chưa đo được không bị xóa để làm danh sách trống.

Core kiểm vai trò/ô, thiếu/trùng mốc, dung sai, baseline khác cỡ chữ, ngoài trang/profile và dữ liệu vô hiệu. WPF kiểm glyph/transform thật, lệch tâm/hàng, mất anchor, mốc đối nghịch, clip/xoay/xuống dòng và giữ các bài Line cũ. Office check thêm mẫu giả chuẩn và bản cố ý thụt lề/giãn trước để phát hiện lệch; lưu hai XPS trong thư mục kết quả. Chưa có runtime Office 2021 cho 0.1.32; mẫu chuẩn chưa đo được phải báo chưa đạt, không bỏ qua để tạo PASS.


## Ghi chú triển khai 0.1.33

`AdministrativePositionTarget.ReferencesCentered` chỉ đến từ các paragraph tham chiếu có căn giữa, không thụt lề, không khoảng trắng đầu/cuối và lề ô cân bằng trong XML/range sống đã đối chiếu. Cờ này chưa đủ để sửa. `AdministrativeCentering.Proven` đòi toàn bộ glyph một dòng, cùng khung clip ô trên XPS, tham chiếu cùng tâm với khung và dòng sửa nằm trọn bên dưới. Thiếu clip/anchor, chữ xoay/cắt/xuống dòng, bảng gộp/lồng hoặc mốc chưa rõ tiếp tục thủ công.

`AdministrativeCenterFix` chỉ chứa vai trò, không tái dùng tọa độ cũ. `WordSession.Position` đo lại trước ghi và ghép vai trò + CP + nguyên văn hiện tại. Chỉ đặt Alignment=Center và ba thụt lề ngang về 0. `AtomicTextBatch` bao quanh cả ghi, lưu, dựng XPS mới và hậu kiểm: tâm đạt dung sai, nguyên văn toàn story giữ nguyên, baseline/kích thước dòng, vị trí mốc và khung ô không đổi trong dung sai 0,5 pt. Không suy thành đạt khi không còn gợi ý nhưng mất số đo. Thất bại phục hồi checkpoint, không thêm Undo; rollback lỗi giữ cả hai nguyên nhân và hiện vật. Không retry viết. Các mã `Word.PositionBatch.*` không ghi chữ nguồn.

UI phân ba nhóm chữ/phông → căn giữa số/ngày → Line. Sửa tất cả áp dụng nhóm đầu còn lỗi rồi quét lại; chọn tích lẫn nhóm bị chặn trước ghi. Hai dòng căn giữa có thể sửa chung một lượt và một Undo. Vẫn dùng quét thật để bỏ mục đã hết lỗi; cảnh báo khác hàng độc lập được giữ. Luồng Line và phép đo cũ không thay đổi.

Test mới kiểm gate an toàn, số đo trước/sau, dữ liệu thiếu, baseline/reference/frame thay đổi, payload giả và rollback khi setter không tạo kết quả hình học. WPF dùng glyph/clip thật để kiểm đề xuất và danh sách sau sửa. Office check thêm sửa riêng/nhóm, lỗi có chủ ý sau dòng đầu, Undo, lưu/mở lại và giữ cảnh báo khác hàng. Các kịch bản Office có trong chương trình, chưa chạy đạt trên Office 2021 ở môi trường phát triển này. Gói .33 tiếp nối .32, không thay main/tag/gói cũ.


## Ghi chú triển khai 0.1.34

Rà soát .33 tìm ra khoảng trống trong hậu kiểm: `Verify(before,after)` chỉ được gọi cho dòng đã chọn. Đã tái hiện bằng Core và glyph WPF: số được căn đúng nhưng ngày không chọn bị dịch, phép kiểm riêng số vẫn chấp nhận. Đây là trường hợp mô phỏng, chưa phải lỗi được quan sát trên Office 2021.

`AdministrativeCentering.VerifyBatch` nay đối chiếu toàn bộ tập dòng số/ngày trước và sau sửa: vai trò, CP/nguyên văn, mốc và cờ tham chiếu không đổi, không mất/thêm/trùng mục. Dòng chọn phải qua phép kiểm tâm thật cũ; dòng không chọn phải giữ mọi hộp chữ/baseline/khung đang đo được trong dung sai 0,5 pt. Không coi dòng chưa đo được là đạt: nó chỉ có thể giữ trạng thái chưa đo/cảnh báo thủ công, mất dữ liệu đã có thì từ chối. `VerifyRestored` kiểm mọi quan sát trở về ban đầu sau `CheckpointSwap`, rồi mới đánh dấu restored=true. Rollback lỗi vẫn giữ cả hai nguyên nhân và hiện vật.

`EditThenScan` phân biệt sửa thất bại với sửa đã commit nhưng quét lại lỗi. Không phát lại ghi hoặc tự Undo khi chỉ lần quét bị lỗi. `MainWindow.Run` tiếp tục xóa lựa chọn/anchor/highlight cũ, đồng thời đổi trạng thái rõ ràng và giữ nguyên nguyên nhân quét trong mã `UI.ScanAfterEdit.Cause`. Một Undo của lượt đã commit vẫn được giữ. Không dùng trạng thái UI hoặc lịch sử sửa để che cảnh báo thật.

Core kiểm lỗ hổng cũ, dòng không chọn bị thay đổi/mất mốc, rollback và giao dịch chữ đã commit trước khi giả lập lỗi XPS. WPF dùng glyph/clip mới để đối chiếu cả hai dòng, mất anchor, danh sách sau sửa một/cả hai và cảnh báo khác hàng vẫn không được tích. Bài khung clip khác nay đòi đo được toàn bộ chữ/khung để tránh vượt qua nhờ mất ánh xạ. Office check cố ý dịch thụt lề của ngày không chọn sau khi sửa số, yêu cầu hậu kiểm từ chối và rollback cả hai trạng thái, không thêm Undo; giữ XPS mẫu `position-unselected-drift.xps`. Chưa chạy trên Office 2021 trong môi trường này.


## Ghi chú triển khai 0.1.35

Rà luồng lưu/mở lại phát hiện `SaveCopy` dùng `File.Copy(RecoveryPath,path,true)` trực tiếp. Sao chép lỗi giữa chừng có thể để tệp đích cũ bị ghi dở. Đây là rủi ro được xác định từ mã, chưa có báo cáo tệp thực của người dùng bị hỏng. Đợt này không thay thuật toán định dạng hoặc Line.

`VerifiedSaveCopy` tạo `.ChinhTaViet-<guid>.tmp` bằng CreateNew trong đúng thư mục đích, đọc checkpoint được Word lưu, ghi vào stage và Flush(true). SHA-256 stage và checkpoint đọc lại phải khớp hash trước copy. Chỉ sau đó mới `File.Move(stage,destination,true)` cùng thư mục/volume; không fallback sang File.Copy đè trực tiếp nếu rename lỗi. Lỗi trước publish dọn đúng stage đã tự tạo, giữ tệp đích và trạng thái Changed. Nếu không dọn được thì AggregateException giữ cả hai lỗi và đường dẫn stage để phục hồi. Không tạo Undo cho thao tác lưu.

Guard chặn tài liệu gốc, RecoveryPath và toàn bộ stack Undo. Chuẩn hóa đường dẫn qua các liên kết cha/tệp; trên Windows dùng volume serial + file index từ GetFileInformationByHandle để chặn hard link/alias trỏ đến tệp bảo vệ. Kiểm guard cả trước staging và trước publish. Không ghi chữ/nội dung/hash/đường dẫn người dùng vào diagnostics; `Word.SaveCopy.Failed`/`Word.SaveCopy.Cause` chỉ mã ngoại lệ và stack. Có thể cần quyền đọc tệp đích cũ để xác minh danh tính; thiếu bằng chứng hoặc không rename được thì giữ lỗi thay vì tự đổi cách ghi.

`WordSession.Save` lưu checkpoint rồi gọi writer, chỉ Changed=false sau publish thành công. Save thất bại vẫn giữ mọi Undo và thay đổi ở bản làm việc. Byte verification không thay cho kiểm nội dung hoặc xác nhận chuẩn thể thức; không phải sao lưu lịch sử, bảo đảm mất điện hoặc chứng nhận mọi hệ thống tệp/NAS/đồng bộ đám mây. Tệp mới theo metadata/quyền thừa kế của thư mục đích; không cam kết giữ mọi ACL, ADS hay metadata đặc biệt của tệp đích cũ khi thay bằng stage.

Core kiểm tạo/thay bản sao, lỗi từng phần, thiếu/sai byte cùng kích thước, checkpoint đổi giữa copy, đích thư mục/không tồn tại thư mục cha, tệp bảo vệ và alias. Windows kiểm hard link và khóa không cho rename; Linux cục bộ kiểm symlink tệp/thư mục. Office check tạo mẫu đích giả có sẵn, gây lỗi sau 16 byte và thử đích đang khóa; kiểm giữ bản đích/gốc, Changed=true, công bố thành công đúng checkpoint, lưu/mở lại và một Undo chữ/phông như trước. Chưa có Word trong CI hoặc xác nhận Office 2021.


## Ghi chú triển khai 0.1.36

`DocumentHeadingRules.Check` nhận cả Paragraph và LayoutParagraph đã ghép XML/range. Chỉ đầu Nhà nước/trang 1/40 đoạn đầu với cặp quốc hiệu-tiêu ngữ duy nhất trong 16 đoạn đầu; tên loại trong whitelist hiện có, ngoài bảng, sau header và trước văn xuôi thường. Mốc Subject hiện có được tái dùng, không thay thuật toán Line. Tên loại/trích yếu phải căn giữa không thụt lề, khối 1–4 đoạn có điểm dừng trước nội dung không căn giữa. Thiếu mốc/trùng tên loại/điểm dừng chưa rõ hoặc dấu nội dung như Kính gửi/Căn cứ/Điều/Chương giữ thủ công. Đây là phạm vi bản thử, không phải parser ngữ nghĩa đầy đủ.

Quan sát phông trên visible range không gồm dấu đoạn, không đổi cách quan sát quốc hiệu/tiêu ngữ cũ. `Paragraph.BoldMixed` tách khỏi FormatMixed để không vô tình cấm định dạng số/ngày đang có kiểu đậm trộn được giữ nguyên. Vùng trích yếu có drawing/field/control/tab hoặc kiểu đậm/phông trộn chưa được tự sửa, kể cả Line đã neo trong cùng đoạn; các đoạn khác đủ chứng cứ vẫn có thể sửa. Cách viết hoa/thường chỉ cảnh báo, không đổi nguyên văn.

`CharacterFormat.Bold` nullable: null giữ hành vi cũ của số/ngày; true chỉ hợp lệ với Kind.DocumentHeadingFormat, đứng, cỡ 13–14. BatchPlan chặn payload lẫn vai trò, thiếu payload hoặc thay chữ, vẫn cho gộp định dạng với lỗi chữ đã chọn nằm trọn trong span. `WordSession` ghép lại các gợi ý tên loại/trích yếu với CheckCore ngay trước ghi; apply Font.Name/Size/Italic/Color/Bold, hậu kiểm cả Bold thật cùng tracked range và toàn story. Không dựa vào số mục để xác nhận sửa. Lưu/rollback trong AtomicTextBatch và một Undo giữ nguyên, không tự retry viết.

Giữ thứ tự chữ/phông → căn giữa số/ngày → Line. Đổi phông/cỡ có thể đổi số đo dòng; Scan dựng lại và kiểm nét XPS sau đó. Mục đã sửa phải biến mất theo quy tắc quét mới, cảnh báo khác còn thật vẫn giữ. Lưu bản sao có kiểm byte của .35 không đổi.

Core kiểm nhận diện/ranh giới, kiểu trộn, cỡ hợp lệ, câu nội dung bị căn giữa, chữ hoa thủ công, payload, overlap và lỗi hậu kiểm đậm. Office check thêm mẫu giả title + 2 subject, cỡ 16/14/13,5, Arial đỏ/nghiêng/chưa đậm, gạch chân và dấu đoạn phông khác: sửa riêng, lỗi có chủ ý giữa nhóm, sửa nhóm + một lỗi khoảng trắng, giữ toàn chữ/kiểu khác, lưu/mở và một Undo; thêm trích yếu trộn đậm giữ thủ công. Không đưa tài liệu/font riêng vào kho. CI không có Word; chưa có báo cáo Office check đúng version cho chức năng mới. Phản hồi chưa phát hiện thêm lỗi của người dùng không được ghi thành PASS mọi kịch bản.


## Ghi chú triển khai 0.1.37

Rà .36 thấy `Checker.Check` chưa dùng TextEditable cho spacing/con-đo, trong khi DocumentHeadingRules đã giữ field/control/drawing thủ công; HeadingRules cũng chưa dùng cờ. Chưa ghi nhận hỏng tài liệu thật. Nay generic text chỉ đưa lựa chọn nếu cờ cho phép; nếu có ứng viên chữ ở vùng không cho phép, tạo một Advisory toàn đoạn không payload, nguyên văn bằng thay thế. CP của đoạn lấy từ Word; tránh đưa substring CP suy từ chữ khi field/đối tượng có thể chiếm CP ẩn. Ngoại lệ ô số/ngày và protectedAdministrative cũ giữ nguyên. Kiểu trộn không tự suy thành chữ không sửa được.

WordSession áp cờ XML đã có cho mọi 40 đoạn đầu State và các ứng viên số/ngày cũ, không thêm COM từng đoạn ngoài phạm vi. Thiếu ghép/prefix dừng thì cờ false. Chưa mở rộng quan sát XML cho toàn story hoặc profile khác. Đừng quảng bá đây là hỗ trợ sửa field/control.

`CurrentTextPlan.Validate` ghép toàn bộ nhóm chữ/phông với các gợi ý CanFix hiện tại bằng Start/Length/Original/Replacement/Kind/HeadingFormat/CharacterFormat. Thiếu, trùng hoặc payload đổi từ chối trước Checkpoint/File.Copy; nhãn/citation không quyết định phép sửa. `FixManyCore` gọi CheckCore tươi trước mọi nhóm chữ/phông; Line và căn giữa vẫn dùng cơ chế đối chiếu/hậu kiểm riêng. Không lấy lại đề xuất mới rồi tự áp nếu người dùng chọn đề xuất khác; không retry viết hoặc bỏ riêng một mục lỗi. AtomicTextBatch, expected story, post-font, rollback và một Undo vẫn giữ nguyên.

Core thêm các ca cấu trúc và stale same-text; Office check có hyperlink nội bộ/bookmark và content control giả, lấy gợi ý từ bản plain có cùng chữ rồi mở bản structured để kiểm từ chối trước ghi, sau đó sửa vùng thường và giữ metadata/chữ vùng cấu trúc qua lưu/mở/Undo. Không upload nguồn riêng. CI không có Word, runtime Office còn chờ xác nhận. Ghi rõ các cảnh báo thủ công vẫn thật sau sửa vùng khác, không dùng số mục đơn thuần làm điều kiện đạt.


## Ghi chú triển khai 0.1.38

Rà `FixManyCore` thấy `formatRanges` trước đây chỉ có CharacterFormat. Các HeadingFormat cũ chỉ được kiểm nguyên văn toàn story, nên setter font/Alignment không tạo kết quả vẫn có thể commit. Core tái hiện trường hợp story-only chấp nhận rồi bổ sung hậu kiểm phải rollback toàn nhóm; chưa có sự cố Office thực được xác nhận.

`HeadingFormat.Matches` kiểm TNR (không phân biệt hoa/thường), cỡ mục tiêu hữu hạn/dương, Bold=-1, Italic=0, Color=0/Automatic và Alignment khi payload yêu cầu. Không suy đạt từ trạng thái scanner hoặc số mục. WordSession lưu Duplicate của range đã thay chữ và định dạng mỗi HeadingFormat, Expected là Replacement. Tạo range sau thay toàn chữ để bám span mới khác độ dài; các sửa CP thấp hơn tiếp tục dịch range. CharacterFormat giữ tracker và chính sách cũ. Sau Checkpoint, đọc lại đủ thuộc tính và range.Text của cả hai loại trong cùng transaction. COM getter lỗi hoặc mismatch đều đi qua rollback cũ; release các range trước đóng Word, giữ nguyên nguyên nhân và một Undo chỉ sau commit.

Quan sát heading dùng visible range không gồm dấu đoạn giống các vùng mới; ParagraphFormat vẫn lấy ở đoạn. Điều này giải quyết việc dấu đoạn phông khác làm cảnh báo cũ còn sau sửa dù chữ đã đúng; không sửa dấu đoạn. Chưa mở rộng nhận diện/vùng XML, không đổi định dạng cỡ theo nguồn pháp lý. Không kiểm thêm vị trí tuyệt đối/ô/glyph của tiêu đề bằng phép đọc phông này; Line/XPS vẫn riêng.

`FixHeadingDriftProbe` chỉ cho phiên OfficeCheck có thư mục artifact và nhóm chứa HeadingFormat, chặn Line/Center. Gây Italic=-1 sau Save nhưng không tự ném lỗi: phép hậu kiểm thật phải bắt và phục hồi. Mẫu State/Party giả có chữ/cỡ/phông sai và dấu đoạn Arial8; kiểm reject giữ checkpoint/Changed/Undo rồi sửa nhóm + khoảng trắng, fresh scan, save/reopen, một Undo và hash nguồn. Những kịch bản này có trong gói, CI không có Word và chưa xác nhận Office 2021. Không đưa tài liệu/font riêng lên kho.


## Ghi chú triển khai 0.1.39

`CheckpointSwap.Apply` trước đây File.Delete(rollback) trong finally có thể ném sau reopen thành công hoặc che lỗi gốc sau phục hồi. WordSession.Undo chưa pop stack nếu exception này xảy ra, dù current đã là candidate. Đây là lỗi tìm từ mã và fault test, chưa có Office thực xác nhận.

API nay trả `CheckpointCleanup?` khi thao tác thành công nhưng xóa backup gặp IOException/UnauthorizedAccessException; giữ đường dẫn backup và đúng ngoại lệ cleanup. Khi thao tác thất bại, vẫn throw nguyên failure và gắn warning tại Data[CleanupDataKey]. Không wrap lỗi XPS thành loại khác vì Line feedback cần RenderedLineMismatchException thật. Nếu restore cũng lỗi, preserveRollback như trước: AggregateException hai cause, không chạy cleanup. Các lỗi khác ngoài nhóm I/O/quyền không được tự coi là cảnh báo dọn bình thường. Callback deleteRollback là biên I/O kiểm thử tương tự copy callback của VerifiedSaveCopy; production dùng File.Delete, không có retry.

WordSession.Checkpoint tập trung SwapCheckpoint cho cả Line candidate, rollback chữ/căn giữa và Undo; ghi mã Word.CheckpointCleanup.Retained, không ghi message/path/nội dung. UndoCore pop đúng một mục sau swap thành công, cập nhật Changed/snapshot như trước, dù còn warning cleanup. UndoCleanupProbe chỉ khi có LineCheckArtifactFolder; inject lỗi tại delete để giữ backup thật, không đổi copy/open/verify. Office check tạo hai sửa tiêu đề riêng, kiểm lần Undo có lỗi cleanup về trạng thái trung gian, lần tiếp theo về nguyên bản và hết stack không đổi; nguồn mẫu giữ hash.

Core dùng file thật với callback lỗi dọn để kiểm byte/trình tự/cause; Windows còn giữ handle FileShare.Read không cho File.Delete của backup thật. Kiểm close/open/copy failure + cleanup vẫn giữ lỗi chính, failure phục hồi giữ hai lỗi/backup, và mô phỏng đúng biên stack Undo. Các bài Core/Line cũ không bỏ điều kiện. Chưa thay các finally dọn candidate/XPS/thư mục khác; đừng ghi thành đã xử lý mọi lỗi cleanup. Runtime Office 2021 còn chờ người dùng kiểm tra tổng thể, không chuyển phản hồi đang kiểm thành PASS.


## Ghi chú triển khai 0.1.40

`RepeatedTextRules` tách token chữ + dấu kết hợp, không lấy số/gạch dưới trong mã; so NFC ordinal đúng hoa/thường. Chia run bằng dấu cách ASCII, ngừng tại mọi dấu câu/control/tab/NBSP. Duyệt đơn vị ngắn nhất 1–8 token, gom các bản lặp đủ đơn vị và nhảy qua span đã dùng để không đề xuất lồng/chồng. Một token, toàn chữ hoa hoặc ứng viên header số/ngày giữ manual, không cần thư viện ngữ cảnh. Đây là phạm vi bảo thủ, có thể bỏ sót cụm dài/case khác/ranh giới phức tạp.

Gợi ý có Kind.RepeatedText, BatchSafe=false, Replacement rỗng và span chỉ gồm separator + bản lặp sau. Giữ lần đầu thay vì replace toàn nhóm, tránh ép kiểu của phần giữ lại. Checker bỏ các spacing/demo nằm trong deletion, giữ các mục ngoài; gate TextEditable/protectedAdministrative cũ vẫn áp dụng. Word normalize Text=null của range đã xóa thành ""; hậu kiểm story và giao dịch cũ không đổi. BatchPlan cho CharacterFormat cả dòng chứa RepeatedText như text edit khác; kiểm kỳ vọng hàng và phông sau lưu bằng tracker cũ. Không thay MainWindow stage/confirmation hoặc bắt đầu thư viện ngữ cảnh.

Core kiểm ranh giới, Unicode, hai nhóm, 8 tiếng, từ láy/mã/header, overlap spacing/demo/style, fresh scan và rollback sau delete. Office sample có phần giữ lại đậm/gạch chân, hai cụm lặp + một spacing + hai manual; kiểm riêng/nhóm, post-delete lỗi, nguyên văn/kiểu và Undo/save/reopen. Chưa xác nhận Office 2021. Bảo vệ XML sâu vẫn chỉ 40 đoạn đầu State; chưa dùng tính năng mới để tuyên bố field/control mọi trang/profile an toàn. Không có từ điển chính tả đầy đủ; `con đo` vẫn chỉ minh họa cũ. Danh sách người dùng .40 thêm C13, giữ nguyên tài liệu .39.


## Ghi chú triển khai 0.1.41

`DevelopmentReport` là nguồn cấu trúc của danh mục C01–C13, M01–M08, D01, milestone và roadmap. Tổng nhóm tính từ Mode, không dùng số lỗi/đếm thao tác trong DOCX. Report Build chỉ nhận version/thời điểm/evidence; không nhận tài liệu, đường dẫn hoặc nội dung Word. Giữ thứ tự user chốt: thể thức, lỗi chữ cơ bản, ngữ cảnh/thư viện sau cùng. Khi thêm loại lỗi phải cập nhật danh mục, guide và kiểm thử, không coi nhóm là mọi trường hợp đã hỗ trợ.

`ReportEvidence` kiểm JSON đúng version, số kiểm thử dương, commit/URL CI đúng định dạng. Workflow Tee log hai bài Core/WPF đã qua exitcode, lấy tổng thực tế, ghi build-checks.json và đưa vào publish folder. Bước riêng --verify-report-package chỉ kiểm manifest trong gói thật qua parser/renderer, không chạy lại toàn WPF; đồng thời xuất TXT mẫu cho ZIP/Release. File thông tin build không phải chữ ký/xác nhận Word; thiếu/sai không hiển thị số PASS. Không gán cứng số kiểm tra dự kiến cho version hiện tại.

`ProgressReportWindow` chỉ đọc và cho xuất TXT bằng delegate. Không dùng MainWindow.Run cho việc xuất báo cáo vì handler lỗi đó xóa choices/anchors của tài liệu. Report tự xử lý lỗi I/O, không gọi Scan/Word. WordSession.SaveProgressReport tạo nguồn UTF8-BOM riêng trong thư mục phiên, gọi VerifiedSaveCopy với toàn bộ Source/Recovery/Undo bảo vệ, không Checkpoint hay đổi Changed/Undo; cleanup nguồn riêng bắt I/O/quyền và log độc lập để không biến xuất thành công thành lỗi dọn. Đuôi TXT bắt buộc; alias/hardlink vẫn bị guard theo danh tính, không chỉ nhìn đuôi.

Core kiểm số nhóm và scope, evidence stale/malformed/negative/wrong URL, thứ tự roadmap và report không giả định Office PASS. WPF kiểm UI chỉ đọc, export trước mở Word, encoding/replace/locked target/hardlink/đuôi, giữ trạng thái/no Undo/stage. Office check khi có dirty edit kiểm không đổi story/Changed/checkpoint/nguồn và một Undo vẫn về đúng bản; chưa chạy Office 2021 trong môi trường CI. Không sửa quy tắc chữ/lặp/Line ở bản này. Hướng dẫn người dùng: REPORT-EXPORT.md.

## Ghi chú triển khai 0.1.42

`PunctuationSpaceRules` duyệt dấu phẩy/chấm phẩy, gom ASCII space sát hai bên, xác minh hai từ có ≥2 chữ cái (hỗ trợ combining mark), ranh giới và ngoại lệ kỹ thuật/header/toàn hoa. Span có độ dài dương, chỉ gồm dấu câu + space; không cần cho phép insertion rỗng trong BatchPlan. Cố ý bỏ sót ngoài phạm vi, không dùng từ điển hay suy nghĩa. TextEditable=false trả manual cả đoạn; Checker vẫn áp gate cũ. Đối chiếu XML sâu chưa mở rộng.

Checker nhường generic spacing chồng span cho C14, sau đó vẫn nhường span chứa trong deletion cụm lặp như cũ. BatchPlan cho punctuation chia sẻ CharacterFormat bao cả span. Không thay CurrentTextPlan/Word CP mapping, giao dịch, hậu kiểm story/phông, Undo hoặc nhánh Line. Office fixture dùng chữ trước dấu câu có đậm/gạch chân riêng; lỗi cố ý sau text edit thứ ba (dấu câu đầu trong thứ tự từ cuối) kiểm rollback nhóm hỗn hợp. Sau Undo sửa riêng, mở lại nguồn mẫu để baseline Changed=false trước fault probe.

Cập nhật catalog C14/milestone, guide và báo cáo mẫu từ renderer/gói thực; workflow lấy số kiểm từ log đã đạt, không dự đoán trước. Chưa có xác nhận Word/Office 2021 cho .42; không merge main hoặc thay stable.

## Ghi chú triển khai 0.1.43

`ParenthesisSpaceRules` trước hết kiểm toàn đoạn có cặp ASCII round parentheses cân bằng/không lồng; mỗi cặp xét ranh giới ngoài và phần chữ hẹp. Duyệt chữ/dấu kết hợp, ASCII space, phẩy/chấm phẩy, ba dấu nối; yêu cầu từ ≥2 chữ cái và có chữ thường; không cố phân loại ngữ nghĩa mọi token. Bỏ qua dấu kỹ thuật/tab/header, regex tuyến tính ngày…tháng…năm ngay cả chú thích làm header parser chặt không nhận. Không thay AdministrativeHeaderRules để đoán header. Mỗi mép chỉ deletion ASCII space, tránh ghi lại cả cặp/runs. TextEditable=false trả manual cả đoạn; gate/giới hạn XML cũ giữ nguyên.

Checker gộp các generic-space chồng C14/C15 rồi nhường deletion cụm lặp như cũ. BatchPlan cho ParenthesisSpace chia sẻ full CharacterFormat. Suggestion có chữ rõ cho replacement rỗng. PunctuationSpaceRules nhận ranh giới ngoặc prose, chặn mở ngoặc dính tên hàm; sửa C15 một mình không được làm C14 chưa chọn biến mất. SpacingRules giữ span toàn ASCII space ngay giữa ( và ) để generic Fix All không thu hẹp ô trống — chỉ skip C15 là chưa đủ.

Office fixture có đậm/gạch chân từ đầu trong ngoặc; CP của probe lấy từ edge issue thật rồi dịch bằng delta ASCII của các edits đứng trước, không lấy string.IndexOf làm Word CP. Sau Undo riêng mở lại nguồn để baseline Changed=false; fault probe edit thứ hai đã xóa mép đóng, phải rollback cả nhóm. Kiểm nhóm, rescan, Undo, save/reopen, source hash; chưa có runtime Office. Cập nhật C15/milestone/guide và báo cáo gói; giữ main/stable/tag cũ.

## Ghi chú triển khai 0.1.44

`SentenceEndSpaceRules` chỉ dò một dấu .?! cuối đoạn sau khi bỏ terminator/ASCII trailing space khỏi vùng xét (không sửa chúng). Span chỉ là ASCII space ngay trước dấu; last word ≥2 chữ cái thường, có word prefix, ranh giới rõ, không Roman hoặc token sau chấm/chuỗi dấu. Bỏ qua technical markers/control/header/date words với regex tuyến tính; chưa là recognizer viết tắt/ngữ nghĩa đầy đủ. Chấp nhận bỏ sót để giữ phạm vi hẹp. TextEditable=false trả manual cả đoạn; không mở rộng giới hạn XML hiện có.

Gộp C16 vào đường punctuation hiện tại để suppress generic spaces chồng span; hỗ trợ full CharacterFormat trong BatchPlan. Replacement rỗng có Suggestion rõ; dùng normalization range.Text=null, whole-story proof, rollback/Undo cũ. Không thay Line/XPS/save/checkpoint. Office fixture có ba C16 + năm edits khác, style ở từ giữ lại, fault ngay write đầu (C16 ở cuối tài liệu); sau Undo riêng mở lại source để baseline Changed=false. CP probe lấy từ parenthesis issue thật và delta ASCII phía trước, không dùng string.IndexOf làm Word CP. Thêm C16/milestone/guide/metadata gói; chưa có xác nhận runtime Office.

## Ghi chú triển khai 0.1.45

`ReportExample(Before,Outcome)` là tham số bắt buộc của mỗi `ReportCapability`; thêm loại mới phải cung cấp ví dụ. Phông/vị trí mô tả định dạng/số đo; text đơn giản có kết quả nguyên văn. `ReportPlannedWork` riêng chứa P01–P05 và ví dụ/hướng nghiên cứu, không cộng vào CapabilityMode hoặc giả làm CanFix. Giữ thứ tự thể thức → chữ cơ bản → ngữ cảnh sau cùng. Báo cáo phân biệt trạng thái phát triển với lịch sử tài liệu; toàn bộ mẫu giả, không đọc DOCX.

Build render ví dụ từ cùng dữ liệu cho UI/TXT, không Trim/normalize dấu cách. [TAB] là ký hiệu hiển thị. Đối chiếu các ví dụ text C01/C05/C06/C13–C16 với Checker/BatchPlan/ExpectedStory, ví dụ dự kiến chưa có CanFix; kiểm render đủ ví dụ/trạng thái và unique milestone. WPF hiện có kiểm cửa sổ chỉ đọc/UTF-8 nguyên văn, mở rộng kiểm hiện ví dụ. Không thay engine hoặc tạo thêm Office scenario; bài Word xuất khi Changed/Undo đã có vẫn áp dụng. Workflow tạo sample TXT từ renderer/gói thực và giữ guide chức năng .44 vì engine không đổi. Chưa có runtime Office cho .45.

## Ghi chú triển khai 0.1.46

`ColonSpaceRules` duyệt colon, chỉ deletion ASCII space trước dấu. Cần hai word kề nhau bên trái ≥2 chữ cái, word cuối thường, word bên phải đã có ASCII separator và ≥2 chữ cái/có chữ thường/ranh giới rõ. Bỏ qua technical markers/control/header/date-words, token giao thức chuẩn và cặp Roman; đây là guard cú pháp, chưa phân loại ngữ nghĩa mọi mã. Giữ khoảng sau nguyên vẹn để generic spacing độc lập. TextEditable=false trả một manual cả đoạn; XML proof limit cũ giữ nguyên.

Gộp C17 trong đường punctuation suppression, cho chia sẻ full CharacterFormat trong BatchPlan và Suggestion rõ khi replacement rỗng. Core fault sau write cuối của nhóm tám edits đã xóa colon gap; Office fault giữa nhóm chín edits, có C17 và các loại cũ. CP style probe lấy từ edge issue thật và delta ASCII trước nó. Sau Undo riêng mở lại source để baseline Changed=false. Không thay Word CP mapping, transaction, Line/XPS/save/checkpoint; thêm Office fixture giả, C17/ReportExample/milestone, guide và kiểm ví dụ thực thi như .45. Chưa có runtime Word/Office 2021.

## Ghi chú triển khai 0.1.47

`WordTextProof.Read` kiểm Start/End thật không âm/trùng/chồng trước khi dùng WordXmlTableMap text-only (`includeLayout:false`). Dùng lại alignment/prefix/known-structure guards cũ, không dựng table geometry/styles trong pass chữ. OnMismatch giữ prefix đã chứng minh; Apply là gate cho mọi Paragraph, không bật lại TextEditable=false. Empty proof chặn tất cả, không còn legacy true ở sau 40 hoặc profile khác trong Word route. Pure Core Paragraph mặc định vẫn phục vụ text fixture không có Word; WordSession luôn áp gate mới.

CheckCore lấy liveText toàn thân, dùng snapshot layout cũ nếu có, chỉ thêm Range.End cho các đoạn khác; không thêm đọc page/alignment/table ngoài phạm vi cũ. WordOpenXML đọc một lần từ Content hiện tại. Text proof và header geometry chạy riêng từ cùng chuỗi XML, giữ header liveLayout/lastLayout 40 đoạn đầu State và thuật toán Line cũ; hai parse XML thuần, chưa benchmark COM. Lỗi XML/unsupported-read được log bằng mã/stack; text proof unavailable giữ empty. RPC/COM lỗi khác tiếp tục theo chính sách read recovery có sẵn, không nuốt thành PASS. Cảnh báo coverage chữ riêng không trộn lỗi mapping body với mốc Line.

Core kiểm sau 40/ba profile/các cấu trúc đã nhận ra, prefix stop, XML thiếu/hỏng, range bất hợp lệ, stale batch và 1.200 dòng giả. Office tạo nguồn plain/link/complex có 45 padding rows; ba mode kiểm eligibility/stale-before-write, State kiểm nhóm/fault/Undo/save/reopen/retained style và relationship hyperlink. CP lấy từ issue thật/delta ASCII, không IndexOf story; hash nguồn giữ nguyên. Không coi các test Core là số đo Word hoặc xác nhận Office. Danh mục 17/8/1, cập nhật M07/example/milestone/guide; chưa thêm rule lỗi mới, chưa hỗ trợ sửa field/control/header/footer/textbox.

## Ghi chú triển khai 0.1.48

C18 `SentenceInnerSpaceRules` chỉ xóa ASCII space trước một dấu .?! giữa đoạn. Hai từ trái có ≥2 chữ cái, từ cuối thường/không La Mã/không viết tắt trong danh sách; biên trước cặp cho phép space/phẩy/chấm phẩy để sửa nhóm không cần lượt hai sau C14. Từ phải đã ngăn space, viết hoa chữ đầu, phần còn lại thường, ≥2 chữ cái; biên sau space hoặc hết đoạn. Bỏ qua header/ngày, control/NBSP, dấu hiệu kỹ thuật, viết tắt/đuôi tệp phổ biến. Danh sách này hữu hạn, chưa kiểm toàn bộ viết tắt hoặc chính tả/ngữ nghĩa.

Thêm FixKind cuối enum; Checker gộp span punctuation tránh chồng C01, repetitions có quyền giữ deletion như trước, BatchPlan cho chia sẻ CharacterFormat. Không đổi CP mapping/WordSession/Line/XPS/save/transaction/checkpoint; bảo vệ XML toàn thân .47 vẫn áp dụng. Core kiểm span chính xác, Unicode, ngoại lệ, sửa riêng giữ mục chưa chọn, nhiều dấu cùng đoạn, nhóm hỗn hợp, stale/manual/style/fault rollback. Office fixture giả có ba C18 + chín lỗi độc lập, kiểm riêng/nhóm/fault/Undo/lưu-mở/kiểu và hash nguồn; probe từ issue Word thật + delta ASCII. Chưa chạy runtime Word/Office 2021. Báo cáo tăng 18/8/1; P03 đổi ví dụ sang thiếu khoảng sau, vẫn chưa triển khai.

## Ghi chú triển khai 0.1.49

C19 `SentenceAfterSpaceRules` dùng range dấu câu dài 1 (không tạo anchor chèn dài 0), replacement là cùng dấu + ASCII space; không ghi lại hai từ bên cạnh. Tách ngữ cảnh C18 sang `SentenceSpaceContext` dùng chung blacklist/ràng buộc cũ; C18 bỏ điều kiện đã có space sau, vì C19 xử lý phần đó độc lập. Pre-gap kết thúc ngay trước mark, C19 bắt đầu tại mark nên hai span kề nhau, không chồng và chạy được một lượt theo CP giảm dần. Chọn riêng mỗi phía giữ mục chưa chọn.

Append FixKind, thêm Checker/BatchPlan sharing như các rule chữ cũ; không đổi WordSession, CP mapping, transaction/checkpoint/save/Line/XPS. Kiểm Core cho chèn + xóa cùng nhóm, Unicode, scope/gates/stale, giữ lỗi chưa chọn, phông chứa toàn đoạn và fault ở nhiều điểm. Bài Word giả có ba C19 + chín mục độc lập, trong đó C18 sát C19. Probe kiểu từ `Tiếp` ngay sau dấu và `thành` ngay trước gap, ngoài probe giữ cụm `kế hoạch`; lấy CP từ issue thật, delta là các chèn/xóa ASCII trước probe. Kiểm riêng/nhóm/fault/Undo/lưu-mở/nguồn. Chưa chạy Word thực tế. Báo cáo 19/8/1, ví dụ C19, mốc .49, P03 vẫn dự kiến nhưng dùng chữ sau viết thường chưa hỗ trợ.

## Ghi chú triển khai 0.1.50

`IssueReviewCatalog` là metadata trình bày thuần: ba section, family/order/label. Không đổi CanFix/BatchSafe/payload. RequiredFormat là allowlist có citation; heading căn trong ô mà chữ giữ nguyên được phân loại thận trọng là cần duyệt, căn giữa và geometry Line theo cấu hình vẫn cần duyệt. CanFix=false luôn ở ManualWarning. LineInspection chỉ thêm Title từ nhánh xác định thiếu, có sẵn sai hoặc chưa khớp nét; thuật toán/điều kiện đo giữ nguyên.

`IssueReviewModel`/`IssueChoiceGroup` dùng cùng IssueChoice cho item/family/section/global; counters gia tăng O(1) mỗi thay đổi, không đếm lại cả danh sách trong từng event. Manual không tích được; tri-state phản ánh chọn một phần. Group expansion lưu trên model khi recycle. `IssueReviewPanel` dùng ListCollectionView hai cấp, virtualization when grouping, row28px, tooltip và chi tiết có thể mở bằng bàn phím. Snapshot PNG chỉ từ mẫu giả; không gửi nội dung tài liệu người dùng.

MainWindow giữ các Issue gốc và anchors cho highlight/ConfirmFix; chọn đang xem độc lập tick. Scan/lỗi thay model mới, bỏ state cũ; FixChecked/FixAll/ConfirmFix vẫn cùng stage guard, sự duyệt và engine cũ. Không đổi WordSession, transaction/checkpoint/save/Line/XPS/Undo; không tự loại issue chỉ vì đã tick hoặc mang nhãn bắt buộc. Báo cáo thêm mốc .50, không thêm loại lỗi: vẫn19/8/1. Kiểm Core classification, model selection1200items, WPF bindings/layout/keyboard selection/manual/reset, tên Line thiếu và ảnh giả; CI không thay thế Word/Office2021.

## Ghi chú triển khai 0.1.51

Người dùng yêu cầu thêm ngữ cảnh cấu trúc trước phần ngữ nghĩa: thứ tự mục và dấu đơn theo mục cha. C20/C21 là thao tác chữ được duyệt, không phải kết luận pháp lý hoặc suy nghĩa. `OutlineRules` dựng cây theo La Mã→số→chữ→dash→plus và thụt lề đã quan sát; nhóm siblings trong từng parent, marker start/label/separator tách rõ. Có 1/a/I mới đề xuất thứ tự, không đổi nội dung/dấu phân cách; restart/mixedstyle/ambiguousalphabet/references giữ manual. Đếm singleton chỉ khi explicit parent complete, parent unknown chặn removal; + đếm riêng dưới từng dash. Generic spacing trùng deletion prefix được gộp, formatting-contained được BatchPlan cho chia sẻ; appended FixKind giữ các giá trị enum cũ.

`WordOutlineEvidence` lấy từ live XML đã đối chiếu: ngoài bảng, không auto numPr trực tiếp/kế thừa, style chain/indent rõ; bookmark/field/link/control chưa hỗ trợ giữ thủ công. `WordXmlTableMap` có overload XDocument nội bộ, logic mapping/layout cũ giữ nguyên; text proof và outline dùng chung cây XML, không thêm COM getters. Header geometry vẫn parse riêng như .47 (hai parse tổng), không đưa tọa độ phỏng đoán vào ghi. Literal dẫn chiếu mục và REF/PAGEREF/NOTEREF chặn renumber có liên quan.

Ngoại lệ hẹp cho tab: helper plain-text cũ được tái sử dụng, chỉ cho allowTabs trong outline evidence sau khi text/XML khớp. `TextEditable` chung vẫn false; `LabelThroughTabs` riêng cho marker-only edits, giữ leading/separator tabs. C21 không bỏ separator tab; leading mix space/tab không rõ giữ manual. Cấp cùng family so sánh XML indent khi prefix giống, hoặc độ dài prefix cùng loại whitespace, không giả tọa độ tab bằng720twips. Bản mẫu riêng được đọc kiểm tra, không đưa lên kho/release; kiểm Core dùng snapshot giả không là Word PASS.

CurrentTextPlan quét lại toàn ngữ cảnh trước checkpoint, nên thêm sibling làm proposal cũ biến mất/bị từ chối. Transaction/checkpoint/save/Line/XPS/Undo không đổi. Office fixture giả có3 order+4 singleton+7 text edits, tab đầu các heading, CP probes từ Issue thật, kiểm single/stale-before-checkpoint/fault/oneUndo/save-reopen/styles/source hashes. C20/21 xuất hiện nhóm review trong UI .50; capability21/8/1 và báo cáo ví dụ nhiều đoạn. Ảnh CI thêm hai nhóm và nền sáng kín, guideISSUE-LIST/OUTLINE-CONTEXT được đưa vào gói. Word/Office2021 runtime chưa xác nhận.


## Ghi chú triển khai 0.1.52

- `IssueTooltipCatalog` chỉ tạo bản tóm tắt trình bày ba phần. Các nguồn đã biết có nội dung quy định theo `FORMAT-RULES.md` và ảnh PDF nguồn đã đối chiếu. Không suy khoản/điều không có trong phụ lục, không gọi NĐ30 là Thông tư. Nguồn chưa ánh xạ giữ tham chiếu và nhắc đối chiếu, không bịa nội dung.
- `IssueChoice.Tooltip` tách khỏi `Detail`; template WPF bind tooltip ngắn qua PlacementTarget.DataContext (Popup có cây hiển thị riêng), expander giữ toàn bộ mô tả/ngữ cảnh/số đo/căn cứ. Phân nhóm, CanFix, payload, chọn và giao dịch giữ nguyên.
- Mức Line 3/7 thuộc cấu hình (mặc định), dung sai/gap không đưa thành quy định pháp luật. Chiều dài tooltip lấy từ LineFix hiện tại, không tính lại hoặc ghi vào tài liệu.
- Bộ kiểm tóm tắt kiểm nguồn/điểm/nội dung/đối tượng/cỡ chữ thực, các mục thủ công và nguồn chưa rõ. WPF mở tooltip thật, kiểm binding ba phần và xuất `issue-tooltip-preview.png` bằng dữ liệu giả; không lấy tài liệu riêng để làm ảnh.
- Không thêm loại lỗi mới: 21 đề xuất, 8 thủ công, 1 minh họa. Chưa xác nhận runtime Office 2021 cho bản này. Main/tag/gói stable .27 giữ nguyên.


## Ghi chú triển khai 0.1.53

- `StagedRepairBatch` phân selection theo chữ/phông, căn giữa, Line; kiểm chồng lấn trong từng bước, giữ nguyên lỗi manual. Tính chữ dự kiến của mốc qua prefix Word thật, không coi CP là chỉ số chuỗi. Lỗi chồng lấn cắt qua mốc không thể chứng minh thì từ chối trước ghi.
- `WordSession.Staged` tạo checkpoint ngoài rồi dùng nguyên các giao dịch con đã hậu kiểm. Mỗi bước hình học xuất XPS mới, `StagedGeometryResolver` ghép đúng vai trò duy nhất và nội dung mốc sau sửa; dùng proposal mới. Không quét lấy tất cả lỗi mới phát hiện, không ghi lại tọa độ cũ. Một mục có thể đã đạt sau sửa chữ/phông, chỉ bỏ qua nếu xác minh thật.
- Hậu kiểm cuối xác minh toàn bộ chữ dự kiến, phông đã chọn, các mốc/Line đã chọn và giữ vị trí số/ngày quan sát được trước bước Line (cho phép CP khác sau mở lại, không bỏ qua hình học khác). Xóa/gộp checkpoint con chỉ sau thành công thành một Undo ngoài. Lỗi bất kỳ bước/đo/hậu kiểm phục hồi cả lượt, giữ Changed và Undo cũ. Lỗi rollback giữ checkpoint và hai nguyên nhân, không báo đạt.
- MainWindow bỏ chặn lựa chọn lẫn bước và bỏ cắt Sửa tất cả còn nhóm đầu. Duyệt một lần, status tách số đã áp dụng và số đã đạt sau đo lại; EditThenScan vẫn phân biệt sửa lỗi với quét danh sách lỗi.
- Bộ Core kiểm chuỗi bước, chọn hẹp, mốc/phông/text mới, manual/duplicate/mất chứng cứ, sai rollback/commit. WPF kiểm glyph đổi kích thước và CP mô phỏng, proposal Line mới, nét sai dù Shape tồn tại phải rollback. `OfficeStagedCheck` có mẫu giả chọn hẹp/toàn bộ, 3 điểm chèn lỗi, Undo cũ, lưu/mở/nguồn; CI chỉ biên dịch phần Word này, chưa có bằng chứng runtime Office 2021.
- Không thêm loại lỗi: 21 đề xuất, 8 thủ công, 1 minh họa. Không đổi nguồn pháp lý hoặc 3/7. Main, stable/tag/gói .27 giữ nguyên.


## Ghi chú triển khai 0.1.54

- Đối chiếu ảnh nguồn NĐ30 PDF21/22/24/28: PL I, Phần I.I.4, II.2.b, II.5.b và II.9.d/ô9b. Không gọi nguồn này là Thông tư. Không đặt giới hạn pháp lý bốn đoạn trích yếu.
- `DocumentHeadingRules` cho Kính gửi/các mốc thân rõ làm ranh giới dù căn giữa; kiểm phông theo tâm đoạn không thụt lề `UnindentedCenter`, không coi dấu cách cuối đoạn là lỗi phông. Hình học Line vẫn dùng Centered chặt và XPS thật.
- `WordTextProof`/`WordXmlTableMap` thêm FormatEditable riêng cho textWrapping break/cr đã khớp XML/range; không mở TextEditable, tab/page/column/field/object không được mở. `ObserveComponent` đọc từng phần chữ giữa ngắt dòng, bỏ kiểu riêng của ký tự ngắt; sửa font giữ nguyên nội dung/ngắt dòng.
- `RecipientContext` chỉ nhận nhãn Nơi nhận và Lưu cùng container XML, không lồng bảng/SDT/danh sách tự động/kiểu chưa rõ; không miễn trừ mọi gạch trong bảng. OutlineScope.Recipient tách khỏi cấu trúc mục. C23 kiểm nhãn/danh sách và sửa từng đoạn ký hiệu nhỏ, không đoán cơ quan nhận/số bản lưu. Chưa tự đo đủ tọa độ sát lề/ngang hàng.
- `ComponentRules` C22 nhận một/hai đoạn tên trên Số trong ô trái của bảng đầu; quan sát font riêng, role CharacterFormat được BatchPlan kiểm chặt. C23 có role nhãn và dòng riêng. CanFix/CurrentTextPlan kiểm lại, giao dịch chữ/phông cũ và một Undo/rollback dùng chung .53; font và dấu/case có thể chọn riêng/chung.
- Cảnh báo toàn phần XML chuyển sang CoverageNotice/CoverageInfo riêng, không neo tùy tiện vào BỘ CÔNG AN hay cộng vào danh sách lỗi. Không coi thiếu căn cứ của thông báo kỹ thuật là vi phạm pháp lý; vẫn thể hiện chưa kiểm đủ nếu thật sự mất proof. Chưa có log mới để kết luận nguyên nhân XML của lần chạy Office người dùng.
- Kiểm riêng DOCX mới chỉ bằng XML và CP/trang mô phỏng: 0 cảnh báo component/trích yếu/nơi nhận, vẫn giữ 1 thứ tự sai và 1 dấu đơn thân bài; nguồn không đổi. Đây không là PASS Word. Không đưa file/đoạn riêng vào repo/gói. Unit fixtures dùng tên giả; OfficeComponentCheck thêm mẫu đúng/sai, ngắt dòng, sửa nhóm/fault/Undo/lưu-mở nhưng chưa chạy Word tại đây.
- Danh mục 23 đề xuất/8 thủ công/1 minh họa, có ví dụ C22/C23; main và stable .27 giữ nguyên.


## Ghi chú triển khai 0.1.55

Kiểm toàn thân mới của C23 có thể đi qua đoạn 40; hậu kiểm cuối .53 cũ dùng lastLayout (chỉ header geometry) nên không tìm được font recipient dài sau Line mở lại. Thêm lastFormatLayout lấy đúng liveText đã có TextEditable/FormatEditable proof toàn thân. VerifyStagedFormats dùng prefix Word thật và danh sách này; không thêm COM getters/không mở rộng phép đo Line. Mất proof sau mở lại vẫn fail/rollback. Test Core cho recipient sau đoạn 40; OfficeComponentCheck thêm sửa font recipient dài cùng Line và một Undo (mới biên dịch, chưa runtime Word). 23/8/1 giữ nguyên; .54 là mốc nội bộ đã CI đạt, tag/gói không ghi đè, .55 là gói gửi người dùng.
