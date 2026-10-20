# Chính tả Việt — bản thử nghiệm desktop

Ứng dụng C# / WPF chạy offline trên Windows, dùng Microsoft Word desktop để đọc, sửa và xuất XPS xem trước theo bố cục của Word trên máy đó. Không gọi dịch vụ AI hoặc gửi nội dung tài liệu lên mạng.

## Chạy trên Windows

Cần Windows 10/11, Microsoft Word desktop đã kích hoạt và .NET 8 SDK. Clone/tải kho mã, mở PowerShell tại thư mục chứa `ChinhTa.sln`:

```powershell
dotnet run --project tests/ChinhTa.Checks
dotnet run --project src/ChinhTa.Desktop
```

Mở `samples/VanBanMau.docx`. Rê chuột hoặc chọn một lỗi để đến vị trí và vẽ khung vàng riêng lỗi đó trên bản xem trước. Rê chuột cũng chọn lỗi bên phải để nút sửa khớp với vị trí đang đánh dấu. Chọn **Sửa lỗi đang xem** để duyệt một lỗi; có **Hoàn tác** và **Lưu bản sao**, không lưu đè đường dẫn tài liệu gốc.

Bản 0.1.2 xuất XPS không có highlight từ tài liệu làm việc một lần mỗi lần quét. Lớp WPF đọc các glyph XPS, ánh xạ nội dung trang/ngữ cảnh duy nhất về vị trí ký tự Word, rồi vẽ các hình chữ nhật tạm thời. Chuyển lỗi chỉ dùng trang và glyph đã cache, không gọi Word, không xuất XPS. Màu đánh dấu không được lưu vào Word hoặc XPS trên đĩa. Không sửa định dạng Word để highlight, không mở thêm bản Word chỉ để xem trước.

Khi ánh xạ không chắc chắn (thứ tự vẽ phức tạp, nội dung mơ hồ, glyph thiếu chữ Unicode hoặc lỗi qua ranh giới trang), ứng dụng đến trang và báo **chưa ánh xạ chắc chắn**, không đoán vị trí. Cần xác minh tọa độ thực tế trên Windows với nhiều font/zoom/bảng. Các thao tác mở, quét và xuất Word vẫn đồng bộ; lần mở và sau mỗi sửa/hoàn tác có thể làm giao diện chờ. Thanh trạng thái ghi thời gian quét/dựng trang, và thời gian đánh dấu khi chọn lỗi. Chưa có số đo trên Windows.

## Phạm vi hiện có — 0.1.8

Chọn **Loại văn bản** ở phía trên: Nhà nước, Đảng hoặc cá nhân. Đổi loại khi đang mở tài liệu sẽ quét và dựng lại bản xem trước; các ô tích cũ được đặt lại. Mặc định là Nhà nước. Không tự đoán loại tài liệu từ vài từ trong nội dung.

- **Nhà nước:** theo hai nguồn PDF người dùng cung cấp, bước đầu nhận diện quốc hiệu/tiêu ngữ trên trang đầu, kiểm tra font, màu, chữ hoa/thường, đậm/nghiêng, khoảng cỡ chữ hợp lệ và căn giữa cục bộ trong ô. Lề/khổ giấy từng vùng thiết lập và tính thống nhất cỡ chữ là cảnh báo thủ công.
- **Đảng:** theo Hướng dẫn 05-HD/VPTW ngày 27/5/2026; bước đầu kiểm tiêu đề Đảng cỡ 15 trên trang đầu, font/màu/đậm/nghiêng; lề/khổ giấy và ngoại lệ là cảnh báo thủ công. Không áp quốc hiệu/tiêu ngữ Nhà nước.
- **Cá nhân:** không ép bố cục hành chính; giữ ngôn ngữ minh họa/khoảng trắng, thêm gợi ý kiểm tra tab liên tiếp và đoạn căn giữa có tab/khoảng trắng đầu dòng; không tự xóa tab.
- Mỗi cảnh báo thể thức có căn cứ mục/phụ lục và số trang PDF. Những mục chỉ kiểm tra thủ công không tích được; **Sửa tất cả** bỏ qua chúng. Có thể sửa định dạng các dòng đã nhận diện, giữ cỡ chữ đang hợp lệ thay vì ép mọi quốc hiệu về 13.
- Kiểm tra ngôn ngữ vẫn chỉ là khoảng trắng và quy tắc minh họa `con đo` → `con đò`, chưa có từ điển hoặc mô hình AI. Không coi việc hết cảnh báo là đạt toàn bộ NĐ30/HD05.
- Chưa kiểm đủ tên cơ quan, số/ký hiệu, ngày tháng, thân bài, chữ ký/nơi nhận, số trang, đường kẻ ngoài ba vùng được nhận diện, thiếu tiêu đề, toàn bộ các mẫu và ngoại lệ; chưa quét header/footer/textbox. Không hỗ trợ tài liệu bảo vệ/Track Changes.

Căn cứ và giới hạn chi tiết: [FORMAT-RULES.md](docs/FORMAT-RULES.md). Nguồn là hai PDF đính kèm của người dùng, chưa xác minh độc lập mọi văn bản sửa đổi sau đó.

File thử:
- `samples/VanBanMau.docx`: chọn Nhà nước; mẫu giả ban đầu.
- `samples/VanBanDangMau.docx`: chọn Đảng; tiêu đề cỡ 14/chữ thường và hai lỗi ngôn ngữ minh họa, dự kiến 3 cảnh báo.
- `samples/VanBanCaNhanMau.docx`: chọn cá nhân; A5 không bị ép A4, có tab/căn giữa và hai lỗi ngôn ngữ minh họa, dự kiến 4 cảnh báo (2 thủ công).

## Kiểm thử

`tests/ChinhTa.Checks` là trình kiểm thử có exit code khác 0 nếu sai, kiểm tra vị trí lỗi, offset tiếng Việt, nhiều lỗi giống nhau, quốc hiệu trong ô bảng và các câu không nên cảnh báo.

Linux có thể chạy core và biên dịch chéo WPF, nhưng **không chạy được giao diện hoặc Word COM**. Xem [quy trình kiểm thử Windows](docs/WINDOWS-VALIDATION.md) trước khi sử dụng thực tế. Chất lượng giữ bố cục và vòng đời COM chưa được xác nhận cho đến khi chạy quy trình này trên Windows.

## Đóng gói thử trên Windows

```powershell
dotnet publish src/ChinhTa.Desktop -c Release -r win-x64 --self-contained true -o artifacts/win-x64
```

Sau restore/publish, thư mục kết quả chạy offline; máy đích vẫn cần Word. Chưa phải bộ cài chính thức.

## Cấu trúc

- `src/ChinhTa.Core`: quy tắc độc lập với Word, có thể mở rộng bằng bộ kiểm tra AI.
- `src/ChinhTa.Desktop`: giao diện và phiên Word COM, bản xem trước XPS.
- `samples/VanBanMau.docx`: dữ liệu giả có đoạn, bảng và hai trang; tạo lại bằng `python scripts/create_sample.py`.

## Bản 0.1.1 — tối ưu chuyển lỗi

Chỉ xuất XPS một lần khi quét hoặc sau sửa/hoàn tác; cache số trang của từng lỗi. Không xuất lại khi hover/chọn lỗi. Giảm lời gọi COM đọc font bằng cách chỉ đọc định dạng quốc hiệu. Chưa tối ưu dựng trang sau từng sửa hoặc tích hợp AI; mở Word lần đầu vẫn có chi phí khởi động.

## Bản 0.1.2 — chỉ đánh dấu lỗi đang chọn

Thay highlight hàng loạt của 0.1.1 bằng lớp hình vẽ trên trang XPS. Phân biệt lỗi trùng từ bằng offset trong nội dung trang hoặc ngữ cảnh duy nhất; từ chối ánh xạ mơ hồ. Khôi phục chỉ highlight một lỗi, giữ điều hướng không xuất lại Word. Bỏ mở/lưu bản Word thứ hai cho việc xuất preview. Chưa triển khai dựng trang nền hoặc giảm số lần dựng sau sửa.

## Bản 0.1.3 — khôi phục kết nối Word

Mỗi lần mở file tạo phiên Word mới, không dùng lại tham chiếu COM cũ. Khi mở/quét/xuất gặp mã mất kết nối RPC, đóng tham chiếu cũ và thử đọc lại đúng một lần từ bản làm việc. Không tự phát lại thao tác sửa hoặc hoàn tác. Lưu checkpoint sau sửa/hoàn tác; checkpoint bổ sung có thể tăng thời gian sửa và cần kiểm thử Undo trên Windows. Nếu chưa lưu được thay đổi cuối, không tự khôi phục vì có thể mất thay đổi; thông báo đường dẫn bản lưu gần nhất. Khôi phục Word sẽ mất lịch sử Undo của tiến trình cũ, được thông báo trên thanh trạng thái.

Thông báo lỗi có đường dẫn bản làm việc và nhật ký `%LOCALAPPDATA%/ChinhTaViet/diagnostics.log`. Nhật ký ghi mã HRESULT và stack, không ghi nội dung tài liệu. Sau mất kết nối, chọn Kiểm tra → Quét lại; nếu thất bại ngay khi mở thì mở lại file. Không cần đóng các phiên Word người dùng đang mở. Chưa tái hiện/kiểm thử lỗi RPC thật trên Windows; bản này bổ sung phòng vệ và chẩn đoán, không bảo đảm xử lý mọi nguyên nhân Word bị thoát.

## Bản 0.1.4 — sửa theo nhật ký Office 2021

Nhật ký thực tế ghi `0x800706BE` tại `Document.GoTo` trong bước ánh xạ preview, trước khi xuất XPS. Bỏ hoàn toàn Word.GoTo và việc tìm đầu/cuối trang bằng điều hướng cửa sổ Word; thay bằng một lần đọc nội dung chính, giữ offset gốc và số trang của từng lỗi. Khi đối chiếu XPS, thử ngữ cảnh ngắn hơn tại biên trang, chỉ dùng khi duy nhất trong cả nội dung nguồn và trang XPS; không đoán vị trí nếu cụm lặp còn mơ hồ. Bổ sung `0x800706BE/0x800706BF` vào cơ chế khôi phục đọc đúng một lần. Nhật ký mới có phiên bản ứng dụng để phân biệt các bản thử.

Đã chạy 28 kiểm thử core và đóng gói Windows x64; chưa tái hiện/kiểm thử Office 2021 trực tiếp. Việc bỏ lệnh trùng với điểm lỗi trong log là điều chỉnh có căn cứ, không chứng minh mọi nguyên nhân RPC đã được giải quyết.

## Bản 0.1.5 — sửa nhiều lỗi

Mỗi dòng lỗi có ô **Chọn để sửa hàng loạt**, độc lập với lỗi đang xem/highlight. Tích các lỗi rồi nhấn **Sửa các lỗi đã tích**; hoặc nhấn **Sửa tất cả lỗi** để áp dụng toàn bộ gợi ý hiện tại, kể cả lỗi chưa tích. Có Tích tất cả/Bỏ tích và số lượng đã tích. Rê chuột không thay đổi trạng thái ô tích. Hộp xác nhận hiển thị số lượng, tối đa 8 gợi ý mẫu và cảnh báo với gợi ý ngữ cảnh/thể thức; chọn Không sẽ không sửa.

Trước khi sửa, kiểm tra mọi đoạn gốc còn khớp; từ chối lỗi chồng lấn. Áp dụng từ vị trí cuối lên đầu để việc thay độ dài không làm lệch lỗi trước đó. Một lượt thành công dùng chung một Word Undo record, một checkpoint và một lần quét/xuất preview. Nút Hoàn tác dự kiến bỏ cả lượt sửa; cần xác minh thực tế trên Office 2021. Sau quét, các ô tích được đặt lại để không giữ lựa chọn cũ trên vị trí đã thay đổi.

Nếu Word lỗi giữa lượt sửa, có thể đã áp dụng một phần; ứng dụng báo và không tự phát lại sửa. Nếu còn kết nối, dùng Hoàn tác hoặc lưu bản sao để kiểm tra. Checkpoint trước lượt sửa vẫn còn nếu chưa lưu lượt mới. Đây không phải giao dịch nguyên tử khi tiến trình Word gặp sự cố. Tính năng sửa tất cả chỉ áp dụng các gợi ý mà bản thử hiện phát hiện, chưa bổ sung từ điển hoặc AI.

## Bản 0.1.6 — ba loại văn bản và quy tắc thể thức đầu tiên

Đã đối chiếu NĐ30 Phụ lục I (PDF trang 21, 32) và HD05 Phần II/Phụ lục 2 (PDF trang 2, 28, 31). Không còn ép quốc hiệu về cỡ 13; cỡ 12 cũng hợp lệ. Mẫu quốc hiệu chấp nhận HÒA/HOÀ, không tự đổi cách đặt dấu. Các ngoại lệ cần phân loại đúng được giữ ở mức cảnh báo thủ công. Chưa phải bộ chứng nhận tuân thủ pháp luật hoặc bộ kiểm đủ mọi mẫu.

## Bản 0.1.7 — giữ dấu tiêu ngữ do Word tự đổi

Tiêu ngữ chấp nhận cả `Độc lập - Tự do - Hạnh phúc` và `Độc lập – Tự do – Hạnh phúc`, kể cả trộn hai dấu. Không cảnh báo chỉ vì khác dấu. Sửa định dạng hoặc hoa thường không tự đổi dấu đang dùng. Phạm vi thể thức/ngôn ngữ còn lại giữ như 0.1.6.

## Bản 0.1.8 — ba đường kẻ Shape Line

Chế độ Nhà nước có kiểm tra/thêm/đặt lại ba Line khi nhận diện và đo chắc chắn: dưới cơ quan trong ô trái, dưới tiêu ngữ và dưới dòng hiển thị cuối của trích yếu. Có hai cấu hình độ dài: NĐ30 (1/3–1/2, đích 5/12) hoặc đơn vị (1/3–2/3, đích 4/7). Khoảng cách theo yêu cầu người dùng: 6–15pt cho cơ quan/trích yếu, 3–13pt cho tiêu ngữ; mặc định tạo cách 6pt. Không gán thông số riêng thành quy định pháp lý.

Chọn cảnh báo để thấy dòng mốc và vạch cam vị trí đề xuất; vạch cam chưa phải Shape đã lưu. Duyệt sửa để tạo/đặt lại Line. Nếu có cả lỗi chữ và Line, sửa chữ trước rồi sửa Line theo số đo mới; Sửa tất cả sẽ thông báo cách chạy hai lượt. Không ghép chắc chắn thì chỉ cảnh báo, không tự chèn trùng. Chi tiết: docs/SHAPE-LINES.md.

Thử `samples/BaDuongKeThieu.docx` (dữ liệu giả) hoặc bản sao tài liệu của bạn. Cloud Linux chưa chạy Office2021, nên cần kiểm thử tạo/neo/căn Line và Undo trên Windows. File thực tế người dùng cung cấp không được đưa vào gói tải hoặc kho GitHub.
