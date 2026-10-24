# Chính tả Việt

Ứng dụng C# / WPF chạy trên Windows với Microsoft Word desktop, làm việc offline trên bản sao DOCX và xem trước bằng XPS. Không gửi nội dung tài liệu tới dịch vụ AI.

**[Danh sách nhóm sửa và bảng kiểm bản thử 0.1.60](docs/SUPPORTED-FIXES-0.1.60.md)** — gồm điều kiện, ví dụ, ngoại lệ ô trống và các mục chỉ cảnh báo.

Ưu tiên đã thống nhất: **thể thức → lỗi chữ và lặp từ/cụm từ → ngữ cảnh và thư viện sau cùng**. Phần chính tả tiếng Việt đầy đủ còn cần xây dựng; không coi quy tắc minh họa hiện tại là bộ kiểm hoàn chỉnh.

## Đang phát triển sau 0.1.60 — B01

Mở rộng C13: `Kế hoạch kế hoạch đã duyệt.` → `Kế hoạch đã duyệt.` khi khác biệt duy nhất là chữ hoa đầu đoạn. Giữ nguyên lần đầu, Unicode và kiểu chữ; không gộp hoa/thường giữa câu, từ đơn, mã hoặc sau nhãn mục. Vẫn cần duyệt, sửa riêng/chung và một Undo/rollback. Code mang version **0.1.61**, mới **1 bước**, chưa tạo ZIP/tag/release .61. [Nhật ký, ví dụ và giới hạn](docs/BATCH-AFTER-0.1.60.md). Gom 5–10 bước đã kiểm rồi mới tạo gói tiếp theo.

## Bản thử 0.1.60 — gom B01–B05

Gom **5 bước** sau .59: chuẩn hóa hoa/thường viết tắt theo tên loại chắc chắn; cảnh báo tên loại/ký hiệu mâu thuẫn để đối chiếu; sửa thiếu khoảng sau hai chấm; C25 bỏ dấu cách cuối đoạn có điều kiện; C15 thêm khoảng sau ngoặc đóng trong cụm chữ rõ. Ví dụ `Ghi chú:nội dung` → `Ghi chú: nội dung`, `Theo mẫu (nội dung)đã duyệt.` → `Theo mẫu (nội dung) đã duyệt.`. Chọn riêng/chung, giữ từ/khoảng chưa chọn, dấu kết đoạn/ô, kiểm nét XPS và một Undo/rollback. Tổng **25/8/1**, TXT có ví dụ và lịch sử đợt. **Chưa xác nhận toàn bộ bài Word .60 trên Office 2021; lỗi phông sau nhóm .56 vẫn chờ bằng chứng.**

[Tải Windows .60](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.60) · [Nội dung/cách thử](docs/CHANGES-0.1.60.md) · [Nhật ký B01–B05](docs/BATCH-AFTER-0.1.59.md) · [Chu kỳ 5–10 bước](docs/RELEASE-CADENCE.md). Gói .60 đã phát hành từ commit bd86e13, đạt 2105 Core/OpenXML + 113 WPF và kiểm metadata gói; chưa xác nhận bài Word trên Office 2021. Main/stable vẫn .27.

## Bản thử 0.1.59

Cập nhật [đầy đủ tên loại/viết tắt](docs/DOCUMENT-TYPES.md) theo NĐ30 PL III.I trang 40: 27 tên hành chính, 3 loại bản sao; thêm **Báo cáo đề xuất — BCĐX** theo yêu cầu người dùng, tách nguồn. Giữ đúng QyĐ/CTr/TTr/TrS, không báo chữ thường chỉ vì các viết tắt đúng bảng. Có cửa sổ **Trợ giúp → Danh mục tên loại văn bản…** và danh mục trong TXT; dùng chung cho nhận diện/C24/phông/mốc trích yếu nhưng vẫn qua guard. Bản sao chưa áp thể thức văn bản gốc, không tự đổi viết tắt theo tên loại hoặc mã cơ quan. [Tải Windows](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.59) · [Nội dung/cách thử](docs/CHANGES-0.1.59.md). Giữ 24/8/1, popup/XPS/Undo/rollback, stable .27; chưa Office2021 PASS, lỗi cũ .56 chờ bằng chứng mới.

## Bản thử 0.1.58

Thêm C24: tên loại đã nhận diện chắc như `Báo cáo` → `BÁO CÁO`, theo NĐ30 PL I.II.5.b. Giữ trích yếu/ngắt đoạn, phông/căn đoạn và khoảng ngoài tên; có thể sửa chung phông sau duyệt. Không đổi tên loại chưa rõ, trường/đối tượng hoặc tự viết thường trích yếu. [Tải Windows](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.58) · [Phạm vi và cách thử](docs/CHANGES-0.1.58.md). Báo cáo 24/8/1 có ví dụ C24. Giữ tiến độ .57, XPS, một Undo/rollback; lỗi cơ quan/trích yếu đã được người dùng xác nhận trên .56, đang chờ bằng chứng runtime .57 hoặc mới hơn. Chưa xác nhận Office 2021 cho .58; stable .27 giữ nguyên.

## Bản thử 0.1.57

Có cửa sổ tiến độ khi sửa: đang sửa nhóm nào, số mục hậu kiểm tạm thời, thời gian đã chờ, lúc đo lại/quét lại/phục hồi. Cửa sổ vẫn cập nhật khi Word bận; giữ COM trên luồng STA cũ. Kiểm phông hỗ trợ thêm Line do Word bọc nhánh tương thích/gộp run; nếu phông vùng bị trộn, đọc các ký tự Word đã xác minh. Giữ C20/C21 đủ điều kiện ở Bắt buộc sửa. [Tải Windows](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.57) · [Nội dung, giới hạn và cách thử](docs/CHANGES-0.1.57.md). Chưa có log đúng phiên bản của phản hồi lặp lại hoặc xác nhận runtime Office 2021; không kết luận đã hết lỗi thực tế chỉ từ kiểm XML/CI. Giữ stable .27, kiểm nét XPS và rollback.

## Bản thử 0.1.56

Sửa cảnh báo phông giả tại cơ quan/trích yếu sau chèn cả ba Line: chứng minh định dạng riêng cho Line nổi của ứng dụng đã xác minh, giữ chặn sửa chữ qua đối tượng. C20/C21 đủ điều kiện chuyển sang **Bắt buộc sửa** theo yêu cầu người dùng, vẫn duyệt trước khi ghi. [Tải Windows](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.56) · [Nội dung và cách thử](docs/CHANGES-0.1.56.md). Giữ kiểm nét XPS, một Undo/rollback, danh mục 23/8/1; chưa xác nhận runtime Office 2021, stable .27 giữ nguyên.

## Bản thử 0.1.55

Bao gồm toàn bộ sửa nhận diện/cơ quan/nơi nhận của .54; hoàn thiện hậu kiểm phông khi nơi nhận ở sau đoạn 40 được chọn cùng Line. Chỉ dùng toàn thân đã có bằng chứng XML/range, giữ giới hạn đo hình học. [Tải Windows](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.55) · [Nội dung .54](docs/CHANGES-0.1.54.md) · [Bổ sung .55](docs/CHANGES-0.1.55.md). Giữ 23/8/1; chưa có xác nhận runtime Office 2021, stable .27 giữ nguyên.

## Bản thử 0.1.54

Sửa nhận diện trích yếu nhiều đoạn/ngắt dòng, dấu cách cuối đoạn và Kính gửi căn giữa. Thêm kiểm/sửa tên cơ quan C22, nơi nhận 9b C23 theo Nghị định 30/2020/NĐ-CP: không nhầm danh sách nơi nhận với mục thân bài. Thông tin chưa đối chiếu XML tách khỏi lỗi dòng đầu. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.54) · [Nội dung và cách thử](docs/CHANGES-0.1.54.md). Danh mục 23 đề xuất/8 thủ công/1 minh họa. Giữ sửa nhiều nhóm/Undo/rollback, chưa xác nhận runtime Office 2021; stable .27 giữ nguyên.

## Bản thử 0.1.53

Chọn nhiều nhóm rồi bấm **Sửa các lỗi đã tích** hoặc **Sửa tất cả**: chương trình tự sửa chữ/phông (gồm thứ tự mục, dấu đầu dòng), đo lại, căn giữa và sửa Line theo các mục đã chọn. Không dùng vị trí/số đo cũ, không nhận thêm lỗi chưa chọn. Một Undo bỏ cả lượt; bước sau không xác minh được thì phục hồi tất cả. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.53) · [Cách thử](docs/MULTI-GROUP-REPAIR.md) · [Nội dung .53](docs/CHANGES-0.1.53.md). Giữ tooltip .52 và danh mục 21/8/1; chưa xác nhận runtime Office 2021, stable vẫn .27.

## Bản thử 0.1.52

Tooltip ngắn theo ba phần **Lỗi — Căn cứ — Cách xử lý**; căn cứ có vị trí và nội dung quy định áp dụng. Phân biệt Nghị định 30/2020/NĐ-CP, Hướng dẫn 05-HD/VPTW, quy tắc ứng dụng và lựa chọn người dùng; đường kẻ hiện chiều dài đề xuất thật, mức 3/7 theo cấu hình. Ngữ cảnh/số đo đầy đủ vẫn ở **Chi tiết lỗi**. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.52) · [Nội dung .52](docs/CHANGES-0.1.52.md) · [Cách dùng](docs/ISSUE-LIST.md). Giữ danh mục 21 đề xuất/8 thủ công/1 minh họa; chưa xác nhận đầy đủ Office 2021, stable vẫn .27.

## Bản thử 0.1.51

Thêm ngữ cảnh cấu trúc: gợi ý sửa thứ tự mục gõ tay `1,2,4 → 1,2,3`, tương tự chữ cái/La Mã; bỏ một dấu `-` hoặc `+` đơn trong mục cha đã xác định trọn vẹn theo yêu cầu người dùng. Dấu + đếm riêng dưới từng dấu -; có từ hai dấu cùng cấp thì giữ. Có bằng chứng XML cho thân/kiểu/thụt lề; nhãn mục có tab bố cục được xét riêng và giữ nguyên tab, không mở sửa chữ chung. Danh sách tự động/bảng/cấp hoặc dẫn chiếu chưa chắc giữ thủ công. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.51) · [Phạm vi và ví dụ](docs/OUTLINE-CONTEXT.md) · [Nội dung .51](docs/CHANGES-0.1.51.md). Báo cáo thêm C20/C21; chưa xác nhận đầy đủ Office 2021, stable vẫn .27.

## Bản thử 0.1.50

Thu gọn danh sách lỗi còn một dòng mỗi mục, giữ checkbox và số trang; mô tả/số đo/căn cứ chuyển sang tooltip và **Chi tiết lỗi**. Gom theo ba phần và theo loại, có checkbox từng lỗi/nhóm/phần/tất cả, trạng thái chọn một phần và thu gọn nhóm. Chỉ ghi **Thiếu đường kẻ…** ở nhánh đã xác định thiếu; Line có sẵn hoặc chưa chắc có tên riêng. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.50) · [Cách dùng danh sách mới](docs/ISSUE-LIST.md) · [Nội dung .50](docs/CHANGES-0.1.50.md). Giữ điều kiện sửa, lượt chữ/phông → căn giữa → Line, rollback/Undo và hậu kiểm XPS. Chưa xác nhận đầy đủ Office 2021; stable vẫn .27.

## Bản thử 0.1.49

Thêm C19: `Đã hoàn thành.Tiếp tục báo cáo` → `Đã hoàn thành. Tiếp tục báo cáo` sau duyệt, trong phạm vi ranh giới chữ rõ. Có thể chọn riêng C18/C19 hoặc sửa cả hai phía dấu câu trong một lượt (`hoàn thành .Tiếp tục`). Giữ chữ, dấu câu, khoảng chưa chọn và các giao dịch hiện có. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.49) · [Phạm vi và cách thử](docs/CHANGES-0.1.49.md). Báo cáo cập nhật C19/ví dụ/giới hạn; chưa xác nhận đầy đủ trên Office 2021, stable vẫn .27.

## Bản thử 0.1.48

Thêm C18: bỏ dấu cách trước một dấu kết câu giữa đoạn có ranh giới rõ, ví dụ `Đã hoàn thành . Tiếp tục báo cáo` → `Đã hoàn thành. Tiếp tục báo cáo`. Giữ chữ, dấu câu và khoảng sau; sửa riêng hoặc chung lỗi chữ cũ, báo cáo TXT có ví dụ. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.48) · [Phạm vi và cách thử](docs/CHANGES-0.1.48.md). Số/viết tắt nhận diện được/ký hiệu kỹ thuật/vùng XML chưa chắc vẫn loại trừ; chưa xác nhận toàn bộ bài Word trên Office 2021, stable vẫn .27.

## Bản thử 0.1.47

Mở rộng đối chiếu XML vùng sửa chữ cho toàn thân và cả ba chế độ, thay giới hạn cũ ở 40 đoạn đầu Nhà nước. Giữ thủ công các vùng cấu trúc/chưa chứng minh, chặn kế hoạch cũ; chữ thuần đã khớp vẫn xét sửa. Báo cáo cập nhật M07, ví dụ và giới hạn. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.47) · [Nội dung và cách thử](docs/CHANGES-0.1.47.md). Field phức tạp/lệch XML có thể chặn phần sau; chưa xác nhận toàn bộ bài Word trên Office 2021, stable vẫn .27.

## Bản thử 0.1.46

Thêm gợi ý xóa dấu cách trước dấu hai chấm như `Ghi chú : nội dung` → `Ghi chú: nội dung`, giữ chữ/dấu câu và khoảng sau. Có thể sửa chung các lỗi chữ cũ; báo cáo cập nhật C17 và ví dụ trước/sau. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.46) · [Nội dung và cách thử](docs/CHANGES-0.1.46.md). Chưa xác nhận toàn bộ bài Word trên Office 2021; stable vẫn .27.

## Bản thử 0.1.45

Báo cáo Trợ giúp/TXT có ví dụ trước/sau hoặc hướng xử lý cho đủ C01–C16, M01–M08, D01; thêm 5 hướng dự kiến có ví dụ và trạng thái chưa triển khai. Giữ nguyên dấu cách trong ví dụ C01 theo yêu cầu người dùng. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.45) · [Nội dung .45](docs/CHANGES-0.1.45.md) · [Cách xem/xuất báo cáo](docs/REPORT-EXPORT.md). Chưa xác nhận toàn bộ bài Word trên Office 2021; stable vẫn .27.

## Bản thử 0.1.44

Thêm gợi ý bỏ dấu cách trước một dấu .?! cuối đoạn trong phạm vi chữ rõ ràng; giữ chữ/dấu câu và khoảng sau, có thể sửa chung cùng ngoặc/phẩy/cụm lặp. Báo cáo Trợ giúp cập nhật C16, lịch sử và giới hạn. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.44) · [Nội dung và cách thử](docs/CHANGES-0.1.44.md). Chưa xác nhận bài Word trên Office 2021; stable vẫn .27.

## Bản thử 0.1.43

Thêm gợi ý xóa khoảng trắng sát trong ngoặc tròn có điều kiện; giữ ô ngoặc trống khi sửa tất cả và giữ cảnh báo dấu phẩy/chấm phẩy chưa chọn sau sửa ngoặc. Báo cáo trong Trợ giúp cập nhật C15, lịch sử và giới hạn. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.43) · [Nội dung và cách thử](docs/CHANGES-0.1.43.md). Chưa xác nhận bài Word trên Office 2021; stable vẫn .27.

## Bản thử 0.1.42

Thêm gợi ý khoảng trắng quanh dấu phẩy/chấm phẩy trong phạm vi chữ thông thường; gộp vùng khoảng trắng để sửa nhóm cùng cụm lặp không ghi chồng. Báo cáo trong Trợ giúp cập nhật C14, việc đã làm và giới hạn. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.42) · [Nội dung và cách thử](docs/CHANGES-0.1.42.md). Chưa xác nhận bài Word trên Office 2021; stable vẫn .27.

## Bản thử 0.1.41

Thêm **Trợ giúp → Tiến độ và chức năng…** để xem/xuất TXT: phần đã làm, số nhóm và danh sách lỗi có điều kiện, kết quả kiểm thử kèm đúng gói, giới hạn và việc tiếp theo. Không cần mở DOCX/Word cho báo cáo; không đổi tài liệu hoặc Undo. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.41) · [Cách dùng báo cáo](docs/REPORT-EXPORT.md) · [Nội dung .41](docs/CHANGES-0.1.41.md). Chưa xác nhận toàn bộ bài Office 2021; stable vẫn .27.

## Bản thử 0.1.40

Phát hiện lặp liên tiếp trong cùng đoạn. Cụm 2–8 tiếng giống nhau có thể đề xuất xóa bản lặp sau khi duyệt, giữ chữ/kiểu của lần đầu; một tiếng lặp, chuỗi toàn chữ hoa và dòng số/ngày giữ thủ công. Không suy đoán nghĩa câu. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.40) · [Nội dung và cách thử](docs/CHANGES-0.1.40.md). Chưa xác nhận runtime Office 2021; mốc ổn định vẫn 0.1.27.

## Bản thử 0.1.39

Tách lỗi dọn backup khỏi kết quả thay checkpoint: phục hồi/Hoàn tác đã mở lại đúng bản vẫn được ghi nhận khi tệp dự phòng chưa xóa được; giữ backup và ghi nhật ký. Lỗi thực khi đóng/chép/mở và nguyên nhân kiểm nét vẫn giữ nguyên. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.39) · [Nội dung và cách thử](docs/CHANGES-0.1.39.md). Chưa xác nhận runtime Office 2021; mốc ổn định vẫn 0.1.27.

## Bản thử 0.1.38

Bổ sung hậu kiểm phông/cỡ/đậm/đứng/màu và căn đoạn được yêu cầu sau lưu quốc hiệu, tiêu ngữ, tiêu đề Đảng; sai định dạng cũng phục hồi cả lượt. Đọc phông trên phần chữ, bỏ dấu kết thúc đoạn để tránh giữ cảnh báo sai khi dấu đoạn có kiểu khác. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.38) · [Nội dung và cách thử](docs/CHANGES-0.1.38.md). Chưa xác nhận runtime Office 2021; mốc ổn định vẫn 0.1.27.

## Bản thử 0.1.37

Chặn gợi ý sửa chữ/khoảng trắng trong vùng đã xác định có cấu trúc hoặc chưa đối chiếu được ở 40 đoạn đầu Nhà nước; tránh sửa vòng qua cảnh báo thủ công. Trước ghi nhóm chữ/phông, quét lại và đối chiếu toàn bộ lựa chọn, giữ rollback và kiểm nét XPS như trước. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.37) · [Nội dung và cách thử](docs/CHANGES-0.1.37.md). Chưa xác nhận runtime Office 2021; mốc ổn định vẫn 0.1.27.

## Bản thử 0.1.36

Bổ sung kiểm/sửa phông Times New Roman, màu đen, cỡ 13–14, đứng và đậm cho tên loại/trích yếu đã nhận diện chắc chắn ở đầu văn bản Nhà nước. Giữ chữ, cỡ đang hợp lệ, gạch chân và căn đoạn; cách viết hoa/thường và vùng chưa chắc vẫn thủ công. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.36) · [Phạm vi và cách thử](docs/CHANGES-0.1.36.md). Chưa xác nhận runtime Office 2021 cho chức năng mới; mốc ổn định vẫn 0.1.27.

## Bản thử 0.1.35

Lưu bản sao qua tệp tạm cùng thư mục, kiểm SHA-256 rồi thay tên để tránh công bố bản bị thiếu khi sao chép lỗi. Giữ tệp đích cũ và trạng thái chưa lưu nếu chưa thay thành công; chặn ghi đè gốc/bản làm việc/Undo, kể cả alias được xác minh. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.35) · [Phạm vi và cách thử](docs/CHANGES-0.1.35.md). Chưa xác nhận Office 2021; mốc ổn định vẫn 0.1.27.

## Bản thử 0.1.34

Siết hậu kiểm căn giữa: kiểm cả dòng số/ngày không chọn để phát hiện dịch chuyển ngoài ý muốn; sau rollback đo lại vị trí gốc. Nếu sửa đã xong nhưng quét lại thất bại, thông báo rõ kết quả và bỏ danh sách cũ để tránh sửa lại nhầm. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.34) · [Phạm vi và cách thử](docs/CHANGES-0.1.34.md). Chưa xác nhận Office 2021; 0.1.27 vẫn là mốc ổn định.

## Bản thử 0.1.33

Bổ sung căn giữa số/ký hiệu và địa danh/ngày tháng khi mốc chữ và cùng khung ô bảng được xác minh trên XPS. Giữ chữ, ô trống và khoảng cách dọc; đo lại sau lưu, rollback cả lượt nếu không đạt, một Undo cho nhóm. Khác hàng hoặc thiếu bằng chứng vẫn thủ công. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.33) · [Phạm vi và cách thử](docs/CHANGES-0.1.33.md). Chưa xác nhận Office 2021.

## Bản thử 0.1.32

Bổ sung cảnh báo vị trí số/ký hiệu và địa danh/ngày tháng theo khung chữ và đường cơ sở đo trên XPS: tâm so với khối cơ quan/quốc hiệu–tiêu ngữ, nằm dưới và cùng hàng. Chỉ cảnh báo để duyệt, chưa tự dịch chuyển. Thiếu mốc, chữ bị cắt/xoay/xuống dòng được ghi rõ chưa kiểm được. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.32) · [Phạm vi và cách thử](docs/CHANGES-0.1.32.md). Chưa xác nhận Office 2021.

## Bản thử 0.1.31

Bổ sung sửa Times New Roman, màu đen, cỡ và kiểu đứng/nghiêng cho dòng số/ký hiệu và địa danh/ngày tháng đủ điều kiện. Có thể sửa chung với cách ghi số/ngày trong một lượt; giữ chữ, ô trống, đậm/gạch chân và căn đoạn. Định dạng trộn hoặc mẫu chưa rõ vẫn thủ công; vị trí tương đối chưa tự sửa. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.31) · [Chi tiết và cách thử](docs/CHANGES-0.1.31.md). Chưa xác nhận Office 2021.

## Bản thử 0.1.30

Sửa nhóm chữ/số/ngày có giao dịch phục hồi: kiểm nguyên văn, lưu và đối chiếu toàn bộ chữ trong phạm vi đã chọn trước khi báo hoàn tất. Lỗi giữa lượt hoặc khi lưu sẽ phục hồi checkpoint và giữ lượt Hoàn tác trước đó; nếu phục hồi lỗi thì giữ hiện vật. [Tải bản thử 0.1.30](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.30) · [Chi tiết và cách thử](docs/CHANGES-0.1.30.md). Chưa xác nhận runtime Office 2021.

## Bản thử 0.1.29

Rà soát tiếp 0.1.28: giữ ô ngày/tháng/năm chưa điền khi cuối dòng có ghi chú; phân biệt điểm dừng tab trong định dạng với ký tự tab thật. Vẫn kiểm khoảng trắng thừa trong ghi chú và nội dung thường. [Tải bản thử 0.1.29](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.29) · [Chi tiết và cách thử](docs/CHANGES-0.1.29.md). Chưa xác nhận runtime Office 2021; 0.1.27 vẫn là mốc ổn định.

## Bản thử 0.1.28

Bổ sung kiểm số/ký hiệu và địa danh/ngày tháng đã nhận diện ở đầu văn bản Nhà nước; sửa hẹp nhãn, dấu phân cách và cách ghi số, giữ trường chưa điền. Chưa có xác nhận chạy đạt trên Office 2021. [Tải bản thử](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.28) · [Phạm vi, căn cứ và cách thử](docs/CHANGES-0.1.28.md).

## Mốc ổn định 0.1.27 — 21/10/2026

Đã chốt phần sửa nhóm ba đường kẻ Nhà nước. Người dùng xác nhận thao tác sửa cả ba đường cùng lúc đã chạy được trên Office 2021 sau các bản thử 0.1.18–0.1.27. Đây là mốc phát triển của chức năng đã xác nhận; ứng dụng chưa kiểm đủ toàn bộ thể thức và chính tả tiếng Việt.

**[Tải Windows x64 0.1.27](https://github.com/trung12685/KT_ChinhTaTV/releases/download/v0.1.27/ChinhTaViet-0.1.27-win-x64.zip)** — giải nén cả gói vào thư mục mới, giữ thư mục `samples` đi kèm. Gói có sẵn .NET runtime; máy chạy vẫn cần Word desktop đã kích hoạt. SDK chỉ cần khi phát triển từ mã nguồn.

- [Biên bản chốt bản, nội dung đã xử lý, bằng chứng và giới hạn](docs/STATUS-0.1.27.md).
- [Hướng dẫn tiếp tục phát triển: kiến trúc, điều kiện an toàn, kiểm thử và việc tiếp theo](docs/DEVELOPMENT.md).
- [Kịch bản kiểm tra Windows/Office](docs/WINDOWS-VALIDATION.md).
- [Lịch sử các bản thử](docs/HISTORY-THROUGH-0.1.27.md), [chi tiết thay đổi cuối](docs/CHANGES-0.1.27.md), [căn cứ thể thức](docs/FORMAT-RULES.md).

## Những phần đã có

- Mở DOCX, chọn/rê chuột để đánh dấu riêng lỗi trong XPS; sửa lỗi đang xem, các lỗi đã tích hoặc tất cả gợi ý được phép; Hoàn tác và Lưu bản sao.
- Ba chế độ Nhà nước, Đảng, cá nhân; kiểm quốc hiệu/tiêu ngữ hoặc tiêu đề Đảng đã nhận diện, một số định dạng và cảnh báo giấy/lề theo từng chế độ.
- Nhà nước: nhận diện đường dưới cơ quan, tiêu ngữ và trích yếu; đọc marker từ DOCX, kiểm nét XPS thực tế và đối chiếu dòng chữ. Sửa nhóm có checkpoint, kiểm cuối lượt và rollback; một lần Hoàn tác cho cả lượt.
- Đường cơ quan/trích yếu chấp nhận dài 1/3–1/2 dòng tham chiếu; đích tự tạo 3/7. Đường tiêu ngữ bằng dòng tiêu ngữ, theo dung sai hiện có.
- Bản thử 0.1.28: số/ký hiệu, dấu phẩy sau địa danh, cách ghi ngày/tháng/năm; trường không rõ giữ thủ công. Không tự chọn mã, địa danh hay giá trị ngày/số.
- Khoảng trắng liên tiếp được kiểm tra, có ngoại lệ hẹp cho trường số văn bản/ngày còn để trống. Giữ các dấu gạch tiêu ngữ Word đã tự đổi.
- Phần ngôn ngữ hiện vẫn là quy tắc minh họa và khoảng trắng; chưa có từ điển chính tả tiếng Việt hoàn chỉnh. Hết cảnh báo không đồng nghĩa đạt toàn bộ NĐ30/HD05.

Chưa kiểm đầy đủ số/ký hiệu, địa danh/ngày tháng, nội dung, chữ ký/nơi nhận, số trang hoặc mọi mẫu và ngoại lệ. Header/footer/textbox, bảng gộp/lồng, Shape lạ và vùng chưa ghép chắc chắn còn giới hạn; một số trường hợp chỉ cảnh báo thủ công. Tài liệu bảo vệ hoặc bật Track Changes chưa được hỗ trợ.

## Chạy từ mã nguồn và kiểm thử

Cần .NET 8 SDK. WPF và Word COM phải chạy trên Windows.

```powershell
dotnet run --project tests/ChinhTa.Checks -c Release
dotnet run --project tests/ChinhTa.DesktopChecks -c Release
dotnet run --project src/ChinhTa.Desktop
```

Mốc 0.1.27 đạt **223 kiểm thử Core/OpenXML và 20 kiểm thử WPF** trong [CI Windows](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/39570349615). CI không có Word desktop. Xác nhận của người dùng trên Office 2021 áp dụng cho thao tác sửa ba đường đã báo đạt; lưu/mở lại, Hoàn tác trên máy người dùng và toàn bộ bài Office check chưa có xác nhận riêng được ghi nhận.

Menu **Kiểm tra → Kiểm tra Word tự động…** chạy một phiên Word riêng với mẫu giả trong `samples`, kiểm ghi Line, nét XPS, quét lại, lưu/mở và Hoàn tác. Kịch bản này có trong ứng dụng; sự tồn tại của kịch bản không phải bằng chứng đã chạy đạt trên mọi máy.

## Đóng gói bản tiếp theo

```powershell
dotnet publish src/ChinhTa.Desktop -c Release -r win-x64 --self-contained true -o artifacts/win-x64
```

Gói phân phối cần kèm `samples` và ghi chú phiên bản. Giữ nguyên tag/file tải của 0.1.27; tính năng mới dùng phiên bản mới. Xem quy trình trong [DEVELOPMENT.md](docs/DEVELOPMENT.md).
