# Chính tả Việt 0.1.69 — giảm các lượt đọc Word/COM và chuẩn bị chữ Preview

Tiếp nối 0.1.68 tại `bea78b753338586600abec9c03c612d4b0e4baa3`, sau phản hồi tốc độ thực tế cải thiện chưa nhiều. Đây là bản thử tiếp theo trên nhánh riêng; mốc tải/source/tag **0.1.67** (`784a41ae9db8dcad02b9cccd69ab8836b9a56dae`) và các gói cũ giữ nguyên để quay lại. Không nhập main.

## Các thay đổi trong lượt này

1. **Mở các tài liệu tiếp theo bằng phiên Word riêng đang có.** Đóng tài liệu cũ, giữ đối tượng Word do chính ứng dụng tạo; khi mất kết nối thì giải phóng và tạo lại theo cơ chế phục hồi. Không gắn vào Word đang mở của người dùng. Lần khởi động Word đầu tiên vẫn có chi phí như trước. Bỏ tích thử nghiệm trở lại cách khởi tạo cũ khi mở file.
2. **Duyệt đoạn tuần tự.** Thử enumerator của collection Word thay cho lấy từng mục theo chỉ số. Kiểm đủ số đoạn và thứ tự CP; nếu enumeration không hỗ trợ, thiếu/thừa, trùng/sai thứ tự hoặc lỗi tùy chọn, bỏ toàn bộ kết quả tạm rồi đọc lại theo cách cũ. Lỗi mất kết nối vẫn đi qua phục hồi, không dùng COM đã chết. Các đoạn trong bảng vẫn được quan sát; ranh giới quyền sửa do proof XML/range hiện có quyết định.
3. **Đọc gộp phông và thuộc tính đoạn khi Word xác nhận đồng nhất.** Chỉ gộp các mục đã có quyền quan sát, liền nhau theo biên Word thật, cùng loại vùng, tối đa16 đoạn/12.000 ký tự. Kiểm Start/End/Text của toàn nhóm trước/sau đọc. Phông dùng giá trị aggregate thực tế của Word; không suy đoán từ XML. Định dạng đoạn chỉ dùng chung khi chữ ký pPr XML giống nhau, không có ngữ cảnh ô/khung/section/đánh số cần đọc riêng, số Paragraphs của range đúng, và mọi thuộc tính Word đều đọc chắc. Phông trộn vẫn đọc riêng dù khoảng đoạn đồng nhất; thuộc tính đoạn trộn vẫn đọc riêng dù phông đồng nhất. Nhóm hoàn toàn chưa rõ chỉ tách một lần trước khi về bộ đọc cũ. Mỗi đoạn xác nhận lại biên/chữ trước dùng bằng chứng nhóm. Số trang đầu/cuối vẫn đo riêng, không suy trang từ nhóm.
4. **Dùng lại chỉ mục chữ cho cùng XPS.** Cùng chuỗi nguồn bất biến chỉ chuẩn hóa một lần; chữ từng trang chuẩn bị một lần, tìm offset bằng tìm kiếm nhị phân. Giữ phép kiểm ngữ cảnh duy nhất và xử lý khoảng trắng cũ; không chọn đại lần xuất hiện đầu. Đổi FixedDocumentSequence hoặc đóng Preview sẽ xóa chỉ mục/đánh dấu, tránh dùng trang cũ cho tài liệu khác. Không lưu cache qua sửa tài liệu hoặc lần dựng XPS khác.
5. **Hiện thêm thời gian mở.** Thanh trạng thái có thời gian mở Word/tệp và tổng mở, ngoài Quét/dựng trang. Log có thêm số lần đọc theo chỉ số, trạng thái enumeration/fallback, số nhóm và số đoạn dùng chung phông/định dạng, số nguồn Preview đã chuẩn bị. Chỉ ghi tên chặng cố định và số; không ghi nội dung/tên file.

## Những điều được giữ

Bảng, ảnh, định dạng và Preview vẫn do Word/XPS dựng đầy đủ. Giữ 30 đề xuất/8 nhóm thủ công/1 minh họa và các điều kiện bảo vệ vùng chưa chứng minh. Không bỏ phân trang, số đo nét XPS, đối chiếu chữ/định dạng trước ghi, hậu kiểm sau lưu, lưu bản sao, một Undo hoặc rollback. Code editor Line, kiểm nét, save/checkpoint/staged rollback và các writer giữ nguyên. Việc xử lý Word vẫn trên STA hiện tại; chưa đổi thành preview chữ thuần hay chuyển COM sang nền.

## Cách thử để xác định có cải thiện thật không

1. Giải nén thư mục mới, giữ nguyên thư mục .67/.68. Mở bản sao cùng `Bao cao.docx` và kiểm đủ từng ô bảng, cơ quan, trích yếu nhiều đoạn/ngắt dòng, nơi nhận, hình/đường kẻ và trang.
2. Menu **Kiểm tra → Thử nghiệm đọc dữ liệu nhanh** mặc định bật. Bỏ tích để dùng các đường đọc cũ; thay đổi chế độ sẽ quét/dựng lại đầy đủ. So khoảng5 lần ở mỗi chế độ, xen kẽ thứ tự. Khi so quét không sửa, dùng đúng cùng trạng thái tài liệu.
3. Ghi riêng ba mốc: **mở Word/tệp**, **Quét**, **dựng trang** và tổng mở. Đo lượt mở đầu riêng; lợi ích giữ Word chỉ áp dụng các lần mở tiếp theo trong cùng phiên ứng dụng. Chưa sửa nội dung khi so hai lượt đọc.
4. Nếu còn chậm, lấy log tại Kiểm tra → Mở thư mục chẩn đoán. Các dòng Word.Perf.Open/Connect/Check/Preview/Scan/OpenUI cho biết chặng nào tốn thời gian. `WarmWordAvailable` là có đối tượng phiên riêng trước Connect, không khẳng định nó còn sống; xem cả các dòng Reconnect khi có lỗi. `SequentialParagraphs`, `IndexedParagraphReads`, `BatchRanges`, `BatchFontParagraphs`, `BatchStyleParagraphs`, `BatchFallbacks` và `PreparedPreviewSources` cho biết nhánh tối ưu có dùng được trên file thực tế hay không.
5. Thử sửa riêng/nhiều nhóm, quét lại, lưu/mở và một Undo trên các bản sao giống nhau. Nếu khác kết quả hoặc không hiệu quả, bỏ tích thử nghiệm hoặc chạy lại [bản .67](https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.67). Quay lại phiên bản ứng dụng không tự hoàn tác DOCX đã lưu, cần giữ nguồn riêng.

## Kiểm chứng và giới hạn số đo

Mẫu đối tượng Word giả lập16 đoạn đồng nhất cho cùng bằng chứng phông/đoạn: lượt lấy thuộc tính phông80→5, thuộc tính đoạn160→10. Đây là **số getter trên mô hình**, không phải số lần gọi COM thực tế toàn ứng dụng hoặc tuyên bố Word nhanh gấp16. Các kiểm biên/chữ, page-flow và các chặng khác vẫn cần chạy.

Kiểm thử phủ enumeration thiếu/thừa/trùng/lỗi giữa chừng, Word mất kết nối, reuse private app và lỗi đóng không được che; nhóm định dạng thật trộn, XML giống nhưng Word không đồng nhất, CP/chữ sai, ngữ cảnh bảng/khung, giới hạn chia nhóm và fallback. Đối chiếu vị trí Preview giữa hai thuật toán cho nhiều offset/độ dài, Unicode, khoảng trắng, chữ lặp/không duy nhất; WPF kiểm cache đổi tài liệu và cùng hộp glyph. Fixture Office tiếp tục so hai cách đọc trước/sau ba Line/Undo và thêm kiểm giữ đúng phiên Word riêng qua các lần mở.

Tổng kiểm/commit/run thực tế do pipeline bổ sung ở release, build-checks.json và TXT. CI chỉ biên dịch fixture Office; **chưa có bằng chứng chạy 0.1.69 đạt trên Office2021 hoặc số đo thời gian thực Word mới**. Không suy từ cải thiện XML của .68 hoặc số getter mô hình thành cải thiện toàn bộ. Khởi động Word lần đầu, phân trang và xuất XPS vẫn có thể chiếm phần lớn thời gian. Giới hạn/phản hồi định dạng còn mở trước đây tiếp tục giữ.
