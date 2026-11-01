# Word Add-in thử nghiệm 0.1.91 — kiểm khi gõ, Vietnamese.dic, thể thức và kiểm sâu

Bản này phát triển trên 0.1.90 đã sửa hợp đồng COM SAFEARRAY khi nạp. Có gói Word 32-bit win-x86 và Word 64-bit win-x64. **Word thực: NOT_RUN** trong môi trường xây dựng; phải thử trực tiếp trước khi kết luận các vị trí gạch và Ribbon hoạt động trên build Word đang dùng.

## Dùng ngay trong Word

1. Giải nén đầy đủ ZIP ngoài và ZIP bên trong, đóng Word, chạy **CAI-ADDIN.cmd** của đúng kiến trúc. Office 32-bit dùng win-x86 dù Windows 64-bit. Không cần gỡ bằng script cũ. Từ cá nhân giữ nguyên.
2. Mở Word bình thường. **Gạch lỗi khi gõ bật mặc định**, kể cả khi chưa mở bảng. Add-in kiểm đoạn nội dung chính đang đặt con trỏ, kể cả ô bảng, sau khi dừng gõ khoảng ngắn. Không quét lại cả tài liệu mỗi lần bấm phím.
3. Thử `điễm, nghành, sử lý, bổ xung, sáng lạng.`. Các lỗi này là **chính tả — màu đỏ**. Sửa thành `điểm, ngành, xử lý, bổ sung, xán lạn.` rồi chờ kiểm lại. Cụm sai vẫn được báo dù từng tiếng như “sử”, “xung” có trong từ điển. Thêm từ không vô hiệu hóa nhóm lỗi đã đối chiếu.
4. Bạn bấm vào chữ và gõ sửa trực tiếp trong Word. Dùng Ctrl+Z, Lưu và In của Word. Add-in không tự thay chữ, đặt Font.Underline, đổi màu chữ hoặc chèn Shape đánh dấu. Dấu gạch riêng chỉ trên màn hình, không ghi vào DOCX hoặc bản in.
5. Tab **Chính tả Việt → Mở bảng kiểm tra**: danh sách phía trên là đoạn đang soạn, đúp gợi ý để chọn chữ sau khi đối chiếu lại. Các nút **Chính tả / Kiểm sâu / Thể thức** kiểm toàn nội dung chính trong giới hạn bản thử và đưa kết quả vào danh sách phía dưới. Chọn chữ rồi tự sửa. Mỗi mục có nhãn nhóm.
6. **Thể thức**: chọn đúng Nhà nước, Đảng hoặc Cá nhân trước khi kiểm. Đọc nhận xét/căn cứ; nhấp Chọn chữ sẽ chọn đúng vùng và hiện gợi ý vàng ngắn tại vùng chọn nếu đo được. Chưa gạch mọi lỗi thể thức của cả tài liệu cùng lúc.
7. Tab **Từ cá nhân**: nhập/lấy chữ đang chọn, Thêm hoặc Xóa. Gạch riêng được kiểm lại theo dữ liệu vừa lưu. Từ đơn được thử đồng bộ sang tệp riêng cho Word; nếu Word khóa tệp thì dữ liệu cá nhân vẫn được giữ và thông báo phần chưa đồng bộ. Cụm nhiều từ chỉ dùng trong Add-in. Từ điển mặc định/khác của Word được giữ.

Đỏ: lỗi chính tả đã đối chiếu hoặc chữ chưa có trong từ điển, có thể cần thêm tên riêng. Xanh: gợi ý dùng từ, dấu câu hoặc bất nhất ngữ cảnh cần cân nhắc. Vàng: gợi ý thể thức đang chọn. Dấu gạch của Add-in cho phép chuột/bàn phím đi xuyên vào Word. Menu chuột phải gốc của Word chỉ hiểu lỗi từ bộ proofing của Word; xem gợi ý riêng của Add-in trong bảng.

## Vietnamese.dic và dấu gạch gốc của Word

Gói có **proofing/Vietnamese.dic nguyên bản**, SHA-256 `1417975f8479f3a4009cf6af120e16ccea190f89b392fb16889d67ed66b6e769`, từ tệp người dùng đính kèm. Tệp mang tên ZIP nhưng thực là RAR v4, chỉ có một từ điển văn bản UTF-16LE. Bộ đọc Add-in lấy 6.634 mục chỉ gồm chữ/dấu kết hợp; bỏ dòng đếm, dòng rỗng và mục có gạch nối. Bản nguyên byte và nguồn được giữ rõ trong assets/word-proofing; chưa gán giấy phép của bộ dữ liệu chung khác cho dữ liệu này.

Khi có tài liệu đang soạn, Add-in tự đăng ký bản riêng tại `%LOCALAPPDATA%\ChinhTaViet\WordAddin\proofing\Vietnamese.dic`, dành cho ngôn ngữ Vietnamese. Giữ Custom Dictionaries và ActiveCustomDictionary đã có. Nếu bản ở ngoài đã đổi, giữ tệp và báo chưa đăng ký lại; không ghi đè mù. Bộ từ chung có giấy phép và mô hình ngữ cảnh hiện có vẫn được dùng song song, không bắt người dùng nạp ZIP.

Các bài FPT/TGDD mô tả việc thêm custom dictionary và bật Check spelling as you type. **Custom dictionary không thay thế bộ proofing của ngôn ngữ và không cung cấp luật ngữ cảnh**. Add-in kiểm `Languages(1066).ActiveSpellingDictionary` thực tế; nếu Word chưa có bộ tiếng Việt thì vẫn dùng lớp gạch riêng của Add-in, không ép văn bản thành English hoặc bỏ CUSTOM.DIC.

Nút **Tiếng Việt cho vùng chọn** là thao tác riêng: nếu Word có bộ proofing tiếng Việt, bật CheckSpellingAsYouType/ShowSpellingErrors, đặt ngôn ngữ Vietnamese và bỏ NoProofing cho vùng chọn/vị trí đang gõ. Thao tác ngôn ngữ có thể làm tài liệu cần lưu; không tự đổi ngôn ngữ toàn bộ tài liệu có nhiều thứ tiếng. Nếu thiếu bộ proofing, hướng dẫn cài Vietnamese Language Accessory Pack đúng Office x86/x64 từ Microsoft. Không tự tải/cài gói hệ thống.

## Phạm vi ba chế độ

- **Chính tả**: danh sách từ/âm tiết kèm theo, từ cá nhân, các cụm sai đã đối chiếu, khoảng trắng và dấu câu. Chữ chưa nhận là nghi vấn, không kết luận mọi tên riêng/thuật ngữ là sai. Chính tả kiểm từ/cụm, không chỉ tiếng rời.
- **Kiểm sâu**: thêm xếp hạng ngữ cảnh từ mô hình ngoại tuyến đã có, đối chiếu ngày tháng thực trong lịch và thứ/ngày/năm. Ví dụ `ngày 31/02/2026` hoặc `thứ hai, ngày 01/11/2026` là gợi ý xanh. Không tự quyết định sửa thứ hay ngày. Chưa phải dịch vụ AI suy luận toàn văn, đánh giá pháp lý, kiểm sự thật hoặc đọc hiểu mọi câu. Không gửi nội dung ra mạng.
- **Thể thức**: đọc phông/cỡ/đậm/nghiêng/màu/căn đoạn, trang và danh tính ô/bảng nếu Word cung cấp rõ, cộng số đo khổ giấy/lề section. Dùng lại Checker/HeadingRules/AdministrativeHeaderRules, DocumentHeadingRules, OfficialLetterRules và PageRules của Core. Công văn V/v trong phần đầu bên trái dùng chính sách riêng, không yêu cầu đường kẻ dưới trích yếu; văn bản có tên loại dùng tên loại/trích yếu riêng ngoài bảng. Các cỡ hợp lệ theo chính sách Core được giữ, không tự ép mọi dòng về một cỡ. Thuộc tính hỗn hợp/không đọc được để trạng thái chưa quan sát, không giả định đúng/sai. Giao diện báo số đoạn có quan sát định dạng và section có số đo.

Chưa chuyển đầy đủ phép đo XPS của Desktop, vị trí thật mọi thành phần, đường kẻ, thân bài/giao động giãn dòng, chữ ký, số trang, header/footer, hộp chữ và mọi mẫu riêng. Desktop 0.1.86 giữ độc lập. Không có thao tác tự sửa hàng loạt trong Add-in này.

## Giới hạn và xử lý kết quả cũ

Gạch khi gõ chỉ theo đoạn hiện tại, tối đa 16.000 ký tự; bỏ vùng ngoài nội dung chính, trường/đối tượng/control chưa hỗ trợ. Giới hạn mỗi lượt vẽ 24 gợi ý/240 ký tự; vùng không nhìn thấy/không đo được bị bỏ qua, không đoán vị trí. Mục quá dài có thể chỉ xem trong bảng. Toàn tài liệu vẫn có giới hạn 2.000 đoạn/500.000 ký tự/1.000 kết quả như trước; có báo dừng và vùng bỏ qua.

Mỗi lượt có danh tính tài liệu, tọa độ đoạn, chữ và thế hệ kiểm. Sửa chữ, đổi đoạn/tài liệu, tắt gạch hoặc chọn thể thức làm cũ kết quả đang chờ. Kết quả quay về được xử lý trên UI/COM STA của Word qua dispatcher riêng. Đối chiếu lại sau đo rồi mới hiển thị. Không dùng riêng tổng số ký tự để suy ra Word CP. Dấu gạch ẩn khi Word không ở foreground; được đo lại theo viewport/zoom, chưa xác nhận trên mọi DPI/chế độ xem. Cần thử thực tế màn hình và Word của người dùng.

## Kiểm thử và bước xác nhận Word thật

Có kiểm dịch vụ/giao thức v3, 5 ví dụ chính tả màu đỏ, whitelist cá nhân, ngày/thứ, phân biệt trích yếu công văn/văn bản có tên loại, định dạng chưa quan sát và lề trang. Kiểm host dùng đối tượng Word giả lập để xác nhận lấy glyph bằng GetPoint sau proof vị trí, không Select/ghi chữ khi chỉ vẽ; chặn kết quả cũ/khác tài liệu, style overlay click-through/nonactivating, giữ từ điển/default, không đăng ký trùng. Giữ kiểm native COM lifecycle/SAFEARRAY và công cụ CHANDOAN-ADDIN.cmd của 0.1.90.

Gói publish chạy worker thật self-contained đúng PE x86/x64 với đủ cả năm ví dụ và kiểm hash Vietnamese.dic. build-checks.json ghi đúng commit và số kiểm CI. **Các kiểm này không thay cho Word thật.** Thử bật/tắt Ribbon, quét, sửa/Undo/lưu/in, hai cửa sổ/tài liệu giống chữ, bảng/tab/emoji/dấu tổ hợp, đổi zoom/cuộn và DPI. Xem bản in/PDF không có dấu gạch của Add-in. Nếu COM vẫn không nạp: chạy CHANDOAN-ADDIN.cmd, gửi load-diagnostic.txt/addin.log; không cần gửi tài liệu hoặc từ cá nhân.

Gỡ bằng GO-ADDIN.cmd của đúng bản đang cài, đóng Word trước. Kho cá nhân và từ điển đã xuất được giữ; muốn bỏ tệp khỏi danh sách Custom Dictionaries của Word thì chỉ bỏ mục sản phẩm này, không xóa dictionary khác.

Nguồn nghiên cứu:

- FPT: https://fptshop.com.vn/tin-tuc/thu-thuat/kiem-tra-chinh-ta-tieng-viet-trong-word-188399
- TGDD (liên kết người dùng; máy chủ có lúc không trả nội dung): https://www.thegioididong.com/hoi-dap/cach-kiem-tra-loi-chinh-ta-tieng-viet-trong-word-100-hieu-1392165
- Microsoft custom dictionaries: https://learn.microsoft.com/en-us/office/vba/api/word.dictionaries.add
- Microsoft kiểm bộ proofing đã có: https://learn.microsoft.com/en-us/office/vba/api/word.language.activespellingdictionary
- Microsoft bật kiểm khi gõ: https://learn.microsoft.com/en-us/office/vba/api/word.options.checkspellingasyoutype
- Microsoft GetPoint theo tọa độ màn hình: https://learn.microsoft.com/en-us/office/vba/api/word.window.getpoint
- Microsoft Language Accessory Pack: https://support.microsoft.com/en-us/office/languages/language-accessory-pack-for-microsoft-365
