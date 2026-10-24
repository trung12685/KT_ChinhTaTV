# Đợt phát triển sau 0.1.61

Mốc v0.1.61 `1a53d2c6f304adc913c139fb4e4f3d3b03d2db46`, đã phát hành ZIP/TXT/SHA-256: 2435 Core/OpenXML +144 WPF +1 kiểm metadata gói. Gói .61, main và stable .27 giữ nguyên. Đợt code **0.1.62** đã gom B01–B05 theo nhịp người dùng yêu cầu. Tag/gói chỉ được tạo sau kiểm bắt buộc đúng commit; kết quả công bố theo PR/release, không suy có bản tải chỉ từ version. Tính năng cùng tests/docs là một bước, không đếm theo số commit.

## B01 — Sửa cỡ hỗn hợp mà giữ phần đã đúng

Hoàn thiện cả luồng nhận diện → đề xuất → duyệt/sửa chung → đo lại → hậu kiểm. Trước đây vùng có cỡ sai trộn cỡ đúng khác đích phải giữ thủ công; nay CharacterFormat của cơ quan/tên loại/trích yếu/ngày cho sửa riêng các ký tự sai cỡ. Ví dụ cơ quan cỡ10/12/13 →12/12/13; trích yếu12/13/14 →13/13/14. Giữ từng cỡ đúng, nguyên văn/ngắt dòng/gạch chân và các thuộc tính không yêu cầu thay. Một mục vùng nên sửa hoa/thường, khoảng trắng hoặc lặp cắt qua ranh giới cỡ vẫn cùng lượt.

Không set Size toàn vùng khi cần bảo toàn; dùng đúng Range ký tự Word, kiểm toàn text/biên trước khi ghi. Bản cỡ kỳ vọng chuẩn hóa đúng phần sai rồi chiếu qua sửa chữ; kiểm chính xác cỡ sau lưu/Line, không chỉ kiểm khoảng hợp lệ. Cỡ đúng bị ép hoặc ký tự sai không về đúng đích đều rollback toàn lượt. Lựa chọn Line tích kèm mục phông, đo lại từ XPS mới; không bỏ proof, nguyên văn, một Undo hoặc kiểm nét thật.

Giữ thủ công khi chưa đọc chắc, mất quyền XML hoặc ngoài nhận diện. Quốc hiệu/tiêu ngữ/Đảng chưa mở nhánh này; C24 tên loại có cỡ hỗn hợp vẫn có giới hạn riêng. Không tăng nhóm loại lỗi:25 đề xuất/8 thủ công/1 minh họa. Báo cáo xem/xuất TXT có ví dụ cỡ sai/đúng và ghi rõ đang phát triển .62, chưa gói.

Core/Desktop kiểm policy/role, cỡ đúng/ngoài khoảng, sai payload, Unicode/CP thực, sửa chung chữ, stale, đọc/ghi COM lỗi, hậu kiểm drift/rollback, checkbox phụ thuộc và glyph/nét mới. Fixture Word mở rộng mẫu đúng/sai cỡ hỗn hợp và lưu/mở/Undo/rollback/băm nguồn, CI chỉ biên dịch. Kết quả Windows đúng commit ghi tại PR và workflow checks; không lấy số kiểm cục bộ thay cho CI. Chưa xác nhận Office2021 hoặc khép phản hồi .56.

[Thiết kế và cách thử](SELECTIVE-SIZE-REPAIR.md). Bước B01 đã hoàn thành kiểm bắt buộc: commit triển khai `3a9d2267b6bf060c60e4e5f261bd665b55313c0d` đạt **2471 Core/OpenXML +163 WPF**, build ứng dụng thành công, không có warning/error biên dịch trong log. [CI đúng commit](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40074445118) · [PR38 và kết quả head mới nhất](https://github.com/trung12685/KT_ChinhTaTV/pull/38). Runner cục bộ chọn lọc đạt2436 lượt assertion, không thay cho tổng CI hoặc Word PASS. Khi chốt B01 có **1 bước hoàn tất**, chưa đóng gói/tag .62. Các commit chốt tài liệu không tính thêm bước.

## B02 — Quốc hiệu/tiêu ngữ giữ nguyên chữ, chỉ sửa cỡ sai

Nối B01 sang HeadingFormat thay vì mở một đường ghi khác. Trong quốc hiệu đã đúng chữ, cỡ10/12/13 →12/12/13; tiêu ngữ12/13/14 →13/13/14. Giữ nguyên HÒA/HOÀ, dấu -/– được chấp nhận, mỗi cỡ đúng và toàn text. Có thể sửa căn giữa cục bộ trong ô theo lựa chọn đã có; không suy ra vị trí toàn trang. Chỉ opt-in khi chữ khớp đúng; cỡ trộn kèm chữ cần thay vẫn thủ công, Đảng cỡ15 cố định giữ route riêng. Khi nội dung đúng, proof định dạng qua Line của ứng dụng đã xác minh vẫn cho sửa font, không mở TextEditable.

BatchPlan chặn cờ thiếu khoảng/nhầm vai trò/thay chữ; hai loại format gọi cùng writer Range ký tự thực. Bản cỡ kỳ vọng chỉ chuẩn hóa phần sai; hậu kiểm trước/sau lưu và sau Line giữ từng cỡ đúng. Line trong ô tiêu ngữ tích kèm đúng hai mục quốc hiệu/tiêu ngữ, đo mới trước ghi/đo lại, không dùng số Shape làm bằng chứng. Bất kỳ lỗi đọc/ghi/nét/cỡ đều vào rollback cả lượt và một Undo cũ.

Core kiểm các biến thể chữ/dấu, cỡ sai/đúng, quyền XML, stale, payload giả và drift; thêm trường hợp dùng drawing thật của DocxLineEditor. Desktop kiểm chung writer, COM read/write failures và glyph/nét tiêu ngữ sau đổi cỡ. OfficeHeadingSizeCheck dùng mẫu giả đã có ba Line, giữ drawing trong run gộp; sửa riêng/nhóm, lỗi sau nhóm phông, một Undo, lưu/mở và băm nguồn. Đọc cỡ ở paragraph hiện tại đã proof, không đoán CP từ text trước mở lại. Bài Word mới chỉ biên dịch trong CI, chưa xác nhận Office2021 hoặc khép phản hồi .56.

B01–B02 là **2 hạng mục** trong code .62, vẫn25 đề xuất/8 thủ công/1 minh họa. Báo cáo TXT và giao diện có ví dụ C02/C03 cập nhật. Kết quả kiểm đúng commit và trạng thái hoàn thành ghi tại [PR38](https://github.com/trung12685/KT_ChinhTaTV/pull/38)/Windows checks; runner cục bộ chọn lọc không thay CI/Word. Chưa tạo gói/tag .62; gom thêm bước theo chu kỳ người dùng yêu cầu.

## B03 — Viết hoa tên loại có cỡ hỗn hợp, giữ định dạng đã đúng

Trước đây C24 còn thủ công khi assessment cần giữ nhiều cỡ, dù đã nhận diện đúng tên loại và đủ proof. Nay `Báo cáo` cỡ13/14 →`BÁO CÁO`, giữ từng cỡ/gạch chân; chọn riêng giữ cả phông/màu/kiểu đang sai nhưng chưa chọn. Chọn kèm sửa phông/cỡ thì kỳ vọng chỉ đổi thuộc tính đã duyệt; cỡ sai/đúng theo B01 vẫn chuẩn hóa riêng. Giữ guard danh mục/NĐ30/bổ sung BCĐX, khối đầu/trích yếu, field/drawing và TextEditable. Không viết thường trích yếu hoặc đoán tên loại.

CasePreservationPlan lấy đúng paragraph hiện tại đã proof làm vùng theo dõi, không tạo thêm mục sửa hoặc dùng chỉ số text toàn thân làm Word CP. CharacterStyleEvidence chụp từng ký tự nhìn thấy và căn/thụt đoạn; RetainedGlyphProjection dùng chung với cỡ để chiếu theo prefix Word thật. WordSession dùng Range.Case, kiểm nguyên văn và fingerprint sau lưu; sửa nhiều nhóm bind lại paragraph/prefix và kiểm sau Line. Một cỡ đúng bị ép, gạch chân bị mất hoặc phông chưa chọn bị sửa đều rollback cả lượt, dù nét Line và chữ đã đúng. Đen tự động/đen RGB, casing tên font không gây lỗi giả; không đối chiếu mọi thuộc tính OpenXML.

Core kiểm toàn danh mục/bổ sung/alias, padding, CP không phải chỉ số string, chọn riêng/chung, snapshot thiếu/stale/payload không khớp, từng thuộc tính drift và rollback; hồi quy cỡ cũ dùng cùng projection. Desktop kiểm reader actual-range giả/Unicode/bounds/COM/layout, lựa chọn phụ thuộc và glyph/nét thật với cỡ title bị drift. OfficeCaseStyleCheck có mẫu tốt, phông sai và cỡ sai trộn cỡ đúng: chọn riêng chữ/phông, chỉ chọn Line trích yếu kéo đúng phụ thuộc, giữ hai Line chưa chọn, rollback sau ghi chữ/sau Line, một Undo/lưu-mở/băm nguồn. CI chỉ biên dịch bài Word, chưa xác nhận Office2021 hay khép phản hồi .56.

[Thiết kế/cách thử](CASE-PRESERVATION.md). B01–B03 là **3 hạng mục**, vẫn25 đề xuất/8 thủ công/1 minh họa; báo cáo UI/TXT có ví dụ C24. Kết quả đúng commit và trạng thái hoàn thành theo [PR38](https://github.com/trung12685/KT_ChinhTaTV/pull/38)/Windows checks; không thay tổng CI bằng runner cục bộ. Chưa tạo gói/tag .62, tiếp tục gom khoảng5–10 bước.

## B04 — Mục số nhiều cấp, chỉ sửa phần số cuối

Mở C20 từ nhãn đơn sang2–4 cấp gõ tay có dấu `.`/`)` và khoảng phân cách, cha số trực tiếp duy nhất đang mở, mỗi thành phần1–999 không số0 đầu, sibling bắt đầu1 và cùng kiểu/thụt lề. Ví dụ trong mục1: `1.1., 1.2., 1.4.` →`1.1., 1.2., 1.3.`. Span chỉ gồm số cuối; giữ tiền tố, delimiter, leading/separator tabs và nội dung. Các cha khác có bộ đếm riêng, chọn một nhóm không đổi phần chưa chọn. OutlineEditContext gắn nhãn/kiểu dấu, cha và đầu nhóm vào proposal; đổi tiền tố không được dùng lại gợi ý cũ dù span số cuối còn y hệt. C21 đếm dash/plus đúng theo cha nhiều cấp.

Chặn đổi số cha nếu còn tiền tố con hoặc hint chưa chắc phụ thuộc; giữ cả nhóm sửa thứ tự để tránh tạo trùng/sai tiền tố quanh mục thủ công. Thiếu/nhảy/ambiguous cha, số0 đầu, trộn cách ghi hoặc sâu hơn4 cấp không được tự sửa. Không coi một số thập phân trong nội dung là lỗi mục; thiếu chứng cứ thì chỉ dừng phép sửa chưa chắc. Dẫn chiếu chữ đến nhãn hiện tại, nhãn dự kiến hoặc hậu duệ, gồm danh sách/range đơn giản, cùng REF fields giữ thủ công nhóm liên quan; không tự sửa dẫn chiếu. Index một lần và chỉ lấy4 prefix ancestor cần thiết. Không gán các lựa chọn này thành điều khoản NĐ30.

Core kiểm 2–4 cấp/punctuation/tab/Unicode, multi-digit tail, nhiều cha, sửa riêng/chung với dấu đơn/khoảng trắng/lặp, missing/ambiguous/zero/restart/auto/reference/unknown-scope, stale và rollback; 1200 đoạn giả kiểm300 cha độc lập, không là benchmark Word. XML thật/giả kiểm LabelThroughTabs giữ quyền chữ chung đóng. Desktop kiểm compact full-label rows, RequiredFormat, check nhóm/từng lỗi và manual cascade. OfficeHierarchicalOutlineCheck dùng mẫu giả có nhãn con, giữ prefix đậm/gạch chân, sửa riêng/chung chữ + Line, lỗi sau Line, một Undo/lưu-mở/băm nguồn; CI chỉ biên dịch Word.

B01–B04 là **4 hạng mục** trong .62, vẫn25 đề xuất/8 thủ công/1 minh họa. [Phạm vi/cách thử](HIERARCHICAL-OUTLINE.md); C20 trong báo cáo TXT có ví dụ mới. Kết quả đúng commit và hoàn thành theo [PR38](https://github.com/trung12685/KT_ChinhTaTV/pull/38)/Windows checks. Chưa gói/tag .62, chưa Office2021 PASS hoặc khép .56 chỉ bằng CI. Khi chốt gói tiếp theo cần kèm hướng dẫn CASE-PRESERVATION và HIERARCHICAL-OUTLINE cùng tài liệu định dạng/đợt.

## B05 — Khoảng trắng cuối đoạn sau chấm phẩy và chốt đợt

Mở C25 cho `Nội dung đã duyệt;   ` →`Nội dung đã duyệt;`, dùng cùng guard câu chữ/ngoại lệ và quyền XML. Chỉ xóa ASCII spaces sau dấu ; cuối đoạn, giữ nguyên dấu câu, khoảng trước chưa chọn, CR/LF/kết ô. Không tự thêm/thay dấu, xóa tab/NBSP/ô số/ngày hoặc thay đoạn chưa kết thúc. Nhãn spacing/tooltip/cách xử lý vẫn ngắn và có ví dụ TXT. C14 khoảng trước dấu chấm phẩy ở cuối đoạn chưa được mở thêm bởi bước này.

Core kiểm các dấu kết đoạn, Unicode/emoji/NFD, các chế độ, ngoại lệ, stale, giữ phần chưa chọn, sửa chung spaces/repetitions và rollback lỗi lưu. OfficeParagraphEndCheck thêm mẫu giả semicolon, sửa riêng/chung chữ, một Undo, rollback/lưu-mở/băm nguồn; CI chỉ biên dịch. Không tăng nhóm lỗi, vẫn25/8/1.

Đủ **5 hạng mục**, chuẩn bị CHANGES/SUPPORTED .62 và workflow kèm toàn bộ guide mới. Windows checks phải đạt đúng commit trước tạo nhánh release/gói. Gói chạy lại Core/OpenXML/WPF, kiểm metadata TXT từ assembly, ZIP/SHA-256 và prerelease; lưu số kiểm/commit/run thật ở PR/release. Các tài liệu B01–B04 ghi chưa gói mô tả thời điểm từng bước, không thay bằng chứng phát hành. Main/stable .27 giữ nguyên; chưa có runtime Word/Office2021 để khép phản hồi .56.

## Việc tiếp theo

Tiếp tục các vùng thể thức còn thiếu khi có nhận diện/căn cứ rõ, mở rộng lỗi chữ có ngoại lệ và giảm thao tác Word trùng khi có bằng chứng. Phần từ điển/chính tả đầy đủ và ngữ nghĩa vẫn theo giai đoạn sau; không coi chức năng cỡ chữ là hoàn thành các phần này. Chỉ tạo Windows sau đủ đợt và kiểm bắt buộc đạt.
