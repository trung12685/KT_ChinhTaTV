# Lộ trình nâng chất lượng chính tả tiếng Việt

Ngày lập: 2026-11-01. Mốc đối chiếu: Word Add-in 0.1.91, commit de67fd81599e6e6f04eb793527cb59afe1db554e.

**Ưu tiên hiện tại là chất lượng bộ kiểm khi người dùng soạn thảo.** Phản hồi “Hom qua” không được báo cho thấy bản 0.1.91 chưa đáp ứng việc này. Chưa xuất bộ cài mới từ đợt nghiên cứu này. Nguyên mẫu được bật riêng trong chương trình đánh giá; ứng dụng vẫn dùng hành vi cũ cho tới khi qua các cửa kiểm dưới đây.

## Vì sao thêm từ điển chưa giải quyết được

“hom” và “hôm” đều có trong dữ liệu. “hom giống” có nghĩa; trong “Hom qua” cần xét cả cụm để nhận ra thiếu dấu. Bộ cũ chỉ tạo ứng viên cho một danh sách nhỏ các cặp dễ nhầm, nên nhiều từ hợp lệ nhưng dùng sai ngữ cảnh không được xem xét. Bỏ qua từ viết hoa để tránh báo tên riêng còn làm mất lỗi ở đầu câu.

Đường tra Vietnamese.dic và bộ từ vựng lớn cũng chưa thống nhất cách bảo vệ tên/biến thể. Đo qua toàn bộ worker thấy bản cũ có thể cảnh báo Nguyễn hoặc một dạng mà nguồn từ vựng lớn chấp nhận. Bổ sung nhiều mục từ chỉ giảm một số báo lạ; nó không tự tạo khả năng hiểu câu. Kho ngữ cảnh hiện có 2.639 câu huấn luyện, quá nhỏ để coi là bộ kiểm ngữ cảnh hoàn chỉnh.

## Việc đã làm trong đợt này

1. Tái hiện hành vi 0.1.91 qua toàn bộ WorkerService, bao gồm tra từ đơn, chế độ chính tả, ranh giới bảng/tab và từ cá nhân.
2. Thêm bộ đo có thể chạy lại: 61 câu phát triển, 659 câu tin tức chưa gán nhãn và 659 câu tạo nhiễu bỏ dấu. Kiểm hash nguồn, tái dựng toàn bộ số đếm và xác nhận các câu kiểm toán gốc nằm ngoài tập huấn luyện. Tập 659 câu đã được dùng trong phát triển trước đây, vì vậy không gọi là tập kiểm mù mới.
3. Viết nguyên mẫu tạo ứng viên từ các cụm trong từ điển và ngữ cảnh có dữ liệu. Khóa tra bỏ dấu dùng chung cho mọi từ, không chứa ngoại lệ Hom → Hôm. Giữ dấu người dùng đã gõ; chưa xử lý thay dấu đã có. Chỉ đưa ứng viên khi cửa sổ có một cách phục hồi; bỏ qua trường hợp mơ hồ hoặc có bằng chứng ủng hộ cụm gốc.
4. Trong nguyên mẫu, dùng thêm từ vựng lớn và bảo vệ chuỗi giống tên riêng/từ cá nhân. Giữ kiểm năm lỗi chính tả đã đối chiếu: điễm, nghành, sử lý, bổ xung, sáng lạng.
5. Kiểm vị trí UTF-16 với Unicode tổ hợp/emoji, giữ ranh giới ô/tab và thêm CI chỉ xuất báo cáo chất lượng. Không sửa nội dung Word, không tự thay từ và không phát hành bộ cài từ workflow này.

## Kết quả phát triển ban đầu

Chi tiết và cách chạy lại: [báo cáo kiểm toán](SPELLING-QUALITY-AUDIT.md), [chương trình đánh giá](../scripts/SpellingEvaluation/README.md).

| Nhóm có đích gán trước | Bản 0.1.91 tương thích | Nguyên mẫu |
|---|---:|---:|
| Thiếu dấu, 15 ca phát triển | 3/15 | 13/15 |
| Năm lỗi đã đối chiếu | 5/5 | 5/5 |
| Từ hợp lệ dùng sai / dấu đã gõ sai, 4 ca | 1/4 | 1/4 |
| Lỗi gõ / ranh giới từ, 4 ca | 3/4 | 3/4 |
| Unicode/vị trí, 2 ca | 1/2 | 2/2 |
| HOA toàn bộ / viết hoa từng từ, 2 ca | 0/2 | 0/2 |
| Nhiễu bỏ dấu trên 659 câu lịch sử | 187/659 | 422/659 |

Đây là số đích có cảnh báo đỏ phủ lên; cần xem thêm gợi ý có sửa đúng đích không. Ví dụ nhóm lỗi gõ bắt 3/4 nhưng chỉ có gợi ý đầu đúng 1/4. Với nhóm thiếu dấu, nguyên mẫu vừa phát hiện vừa gợi ý đầu đúng 13/15. Bộ phát triển có 29 câu đối chứng không cần cảnh báo; số câu có cảnh báo thừa giảm 4 → 0, nhưng 29 câu chưa đại diện cho sử dụng thực tế.

Trên 659 câu tin tức chưa gán nhãn, mức cảnh báo đỏ là 110 → 105, tương ứng 9,02 → 8,61 cảnh báo/1.000 token. **Đây không phải tỷ lệ báo nhầm.** Cần người đối chiếu từng cảnh báo mới xác định được đúng/sai. Nhiễu tổng hợp cũng có thể tạo một từ khác vẫn hợp nghĩa; phục hồi câu gốc không chứng minh câu nhiễu luôn sai.

Các ví dụ vẫn bỏ sót được giữ trong bộ đánh giá, không đổi nhãn hoặc loại bỏ để làm đẹp số liệu: “Hôm qua tôi da gửi báo cáo”, “Chung ta cần kiểm tra lại”, “Tôi cần sữa lỗi”, “Cô ấy uống sửa tươi”, “Chính phủ ban hàng văn bản”, “Hmok qua”, “HOM QUA”, “Hom Qua”.

## Thứ tự phát triển tiếp theo

| Bước | Công việc cụ thể | Sản phẩm bàn giao và điều kiện sang bước |
|---|---|---|
| 1 — nền đo, đã có bản đầu | Duy trì đo cả luồng, phân nhóm, giữ ca bỏ sót/báo nhầm, lưu hash dữ liệu và commit | CI tái lập được báo cáo; phân biệt kiểm code với chất lượng ngôn ngữ. Nguyên mẫu còn tắt trong ứng dụng. |
| 2 — dữ liệu chuẩn thực tế, cần làm tiếp | Tạo bộ lỗi thực và văn bản đúng; có hành chính, văn xuôi, tên riêng, địa danh, thuật ngữ và văn bản không dấu | Mục tiêu đầu: ít nhất 2.000 đích lỗi, 3.000 đoạn đối chứng đúng với ít nhất 30.000 từ và 300 trường hợp tên/thuật ngữ. Hai người gán nhãn độc lập, xử lý bất đồng, ghi các đáp án được chấp nhận và trường hợp mơ hồ. |
| 3 — mở rộng ứng viên và xếp hạng | Dùng ngữ cảnh rộng hơn để xử lý từ đúng nhưng dùng sai; nhiều từ mất dấu; dấu đã gõ sai; phụ âm dễ nhầm; lỗi Telex/VNI, thừa/thiếu/đảo ký tự; tách/dính từ | Đo riêng từng nhóm trên tập phát triển; có mức tin cậy và khả năng không kết luận. Một bộ chính sách tên/từ cá nhân/biến thể dùng chung các tầng. Không biến từ phổ biến thành đáp án mặc định cho mọi tên riêng. |
| 4 — kiểm mù và hiệu năng | Đóng băng mã/ngưỡng rồi chạy tập kiểm độc lập; kiểm gõ tiếng Việt, vị trí gạch và độ trễ trên Word 32/64-bit | Báo cáo đạt các cửa kiểm bên dưới; ghi phần chưa đạt. Gợi ý đỏ/xanh phải có lý do, giữ sửa tay mặc định và Undo/Lưu của Word. |
| 5 — gói dùng thử có kiểm soát | Chỉ bật cơ chế mới sau đánh giá, thử một nhóm người dùng rồi đối chiếu lỗi mới | Gói gắn đúng commit và báo cáo; có đường quay về. Chưa qua cửa kiểm thì tiếp tục nghiên cứu, không gọi là hoàn thiện. |

Bước 2 và nghiên cứu bước 3 có thể làm song song. Tập kiểm cuối phải đóng băng theo tài liệu/nguồn, tách khỏi cả dữ liệu huấn luyện và dữ liệu chỉnh ngưỡng; không chỉ chia ngẫu nhiên các câu cùng một tài liệu. Tập 659 câu lịch sử tiếp tục là kiểm toán phát triển. Không huấn luyện lại trên nó để công bố điểm kiểm độc lập.

Nguồn lỗi thực đáng khảo sát là [VSEC2021/VSEC](https://github.com/VSEC2021/VSEC), công bố cặp câu và nhãn lỗi. Trước khi đưa vào gói hoặc huấn luyện cần xác minh quyền sử dụng/phân phối từ nguồn gốc; chưa nạp nguồn này vào ứng dụng trong đợt này. Văn bản hành chính được rà soát vẫn cần bổ sung vì dữ liệu tin tức không đại diện đủ. Không đưa tài liệu riêng hoặc kho từ cá nhân của người dùng lên GitHub/artifact.

Quy tắc chấp nhận biến thể (ví dụ cách viết i/y) phải được ghi thành chính sách có nguồn, phạm vi và cấu hình; một mục có trong từ điển không tự chứng minh nó phù hợp mọi chuẩn văn bản. Đo riêng lỗi đầu câu, chữ HOA và tên riêng để tránh việc bảo vệ tên che luôn lỗi chính tả.

## Cửa kiểm đề xuất trước khi bật cho người dùng

Các ngưỡng sau là **mục tiêu chưa đạt/chưa được đo trên bộ chuẩn độc lập**, không phải lời chứng nhận hiện tại:

- Ít nhất 98% cảnh báo đỏ là lỗi thật trên tập kiểm đã thẩm định; công bố cả số mẫu và khoảng tin cậy 95%. Trường hợp chỉ là lựa chọn văn phong/mơ hồ không tính là lỗi đỏ chắc chắn.
- Phát hiện ít nhất 90% ở từng nhóm ưu tiên: thiếu dấu, lỗi gõ, lỗi chính tả đã đối chiếu và từ/dấu dùng sai ngữ cảnh. Báo riêng nhóm và phần bỏ qua; không lấy tổng cao để che nhóm yếu.
- Với lỗi có đáp án, ít nhất 90% có đáp án chấp nhận được trong ba gợi ý đầu. Cảnh báo không có gợi ý phải được thống kê riêng.
- Không quá 1 cảnh báo sai/1.000 từ trên văn bản **đã xác nhận đúng**. Tách kết quả tên riêng, địa danh, mã hiệu, từ chuyên ngành, biến thể và từ cá nhân.
- Trên máy tham chiếu Windows/Word được ghi rõ CPU/RAM/phiên bản: P95 xử lý nóng một đoạn 300–500 ký tự không quá 150 ms; đo riêng thời gian lấy chữ/vẽ dấu của Word. Mục tiêu nạp lạnh mô hình/chỉ mục không quá 3 giây. Số đo Linux/CI không thay số đo máy người dùng.
- Khi đang gõ Telex/VNI, không chen sửa hoặc vẽ kết quả cũ lên đoạn mới; kiểm bảng, tab, xuống dòng, zoom/cuộn, Unicode tổ hợp và vòng đời tài liệu. Không thay phông/gạch thật trong DOCX, không làm mất Undo, không tự sửa khi người dùng chỉ xem gợi ý.

Nếu bộ chuẩn hoặc hiệu năng chưa đủ bằng chứng, trạng thái vẫn là CHƯA ĐỦ ĐIỀU KIỆN dù CI chạy xanh. Ưu tiên hiện tại là hoàn thành các bước trên trước khi mở rộng thêm giao diện và luật thể thức. Mục tiêu là tăng độ bao phủ có đo lường; chưa có cơ sở hứa phát hiện mọi lỗi tiếng Việt chỉ bằng một lần mở rộng từ điển.
