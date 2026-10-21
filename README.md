## Bản thử 0.1.26 — đối chiếu lại mốc và ánh xạ vị trí Word

Log 0.1.25 thất bại tại bước so khớp mốc sau lưu, trước khi quét lại. Bản này lấy vị trí chuỗi xem trước từ văn bản tiền tố do Word trả về và ghép lại mốc bằng vai trò/nội dung chính xác trong bố cục mới, thay vì yêu cầu CP cũ giữ nguyên. Giữ xác minh nét XPS, kiểm cuối nhóm và rollback. Xem [bằng chứng và giới hạn](docs/CHANGES-0.1.26.md).

## Bản thử 0.1.25 — xác minh nét thật và hoàn tất nhóm trong một lượt

Đánh giá thể thức trên nét XPS thực đo, quét lại sau lưu ngay bên trong giao dịch sửa và cho phép một lượt đặt lại các Line vừa chọn còn cảnh báo. Chỉ báo đạt khi kiểm cuối không còn cảnh báo ở nhóm đã chọn; sai thì khôi phục cả lượt, Hoàn tác vẫn một lần. Xem [thay đổi và giới hạn kiểm chứng](docs/CHANGES-0.1.25.md).

## Bản thử 0.1.24 — đo đủ glyph cuối và giữ phần bù khi sửa lại

Bỏ phép đo theo điểm dừng con trỏ để nền xác minh và DocumentViewer dùng cùng độ rộng hình học. Khi đặt lại Line đã có, giữ phần bù tọa độ của Line đó và xác minh XPS như trước. Xem [bằng chứng, thay đổi và kiểm chứng](docs/CHANGES-0.1.24.md).

## Bản thử 0.1.23 — bốn chỉnh sửa theo phản hồi

Sửa cả nhóm ba Line với một checkpoint hoàn tác; đích tự tạo cơ quan/trích yếu 3/7, vẫn chấp nhận 1/3–1/2; đo lại dòng trích yếu trên candidate trước khi báo thành công và làm mới kết quả; giữ khoảng trống dành điền số/ngày. Xem [chi tiết và kiểm chứng](docs/CHANGES-0.1.23.md).

## Bản thử 0.1.22 — Line tiêu ngữ ở ô phải

Giữ phép bù nét của 0.1.21. Bổ sung một candidate cho Motto không hiển thị, dùng khung ô thực đo từ XPS và bắt buộc kiểm lại nét mới cùng các nét đã có; sai thì rollback. Xem [phân tích và điều kiện áp dụng](docs/OFFICE2021-0.1.22.md).

## Bản thử 0.1.21 — bù theo nét XPS và xác minh lại

0.1.20 đã chạy đạt bài kiểm tra mẫu trên Word người dùng, nhưng tài liệu riêng vẫn có nét lệch 30/20 mm. Bản này cho phép đúng một lần bù candidate dựa trên nét XPS ghép chắc chắn; bắt buộc kiểm lại đích ban đầu và rollback nếu sai. Xem [bằng chứng và giới hạn](docs/OFFICE2021-0.1.21.md).

## Bản thử 0.1.20 — Office 2021 chưa được xác minh

Log 0.1.19 vẫn lỗi khi đọc ID/Type của Shape. Bản này bỏ hoàn toàn Shape COM getters, dùng kiểm kê DOCX và bắt buộc xác minh nét XPS; giữ rollback. Xem [phân tích và giới hạn kiểm chứng](docs/OFFICE2021-0.1.20.md).

## Bản thử 0.1.19 — Office 2021 chưa được xác minh

Đọc dấu nhận diện từ DOCX thay Shape.AlternativeText; giữ xác minh nét XPS và rollback. Xem [phân tích và giới hạn kiểm chứng](docs/OFFICE2021-0.1.19.md).

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

## Phạm vi hiện có — 0.1.18

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

Chế độ Nhà nước có kiểm tra/thêm/đặt lại ba Line khi nhận diện và đo chắc chắn: dưới cơ quan trong ô trái, dưới tiêu ngữ và dưới dòng hiển thị cuối của trích yếu. Bản 0.1.9 chỉ dùng độ dài theo NĐ30 (1/3–1/2; từ 0.1.23 đích tự tạo là 3/7 theo lựa chọn người dùng); đã bỏ tùy chọn 4/7 khỏi giao diện và bộ kiểm. Khoảng cách theo yêu cầu người dùng: 6–15pt cho cơ quan/trích yếu, 3–13pt cho tiêu ngữ; mặc định tạo cách 6pt. Không gán thông số riêng thành quy định pháp lý.

Chọn cảnh báo để thấy dòng mốc và vạch cam vị trí đề xuất; vạch cam chưa phải Shape đã lưu. Duyệt sửa để tạo/đặt lại Line. Nếu có cả lỗi chữ và Line, sửa chữ trước rồi sửa Line theo số đo mới; Sửa tất cả sẽ thông báo cách chạy hai lượt. Không ghép chắc chắn thì chỉ cảnh báo, không tự chèn trùng. Chi tiết: docs/SHAPE-LINES.md.

Thử `samples/BaDuongKeThieu.docx` (dữ liệu giả) hoặc bản sao tài liệu của bạn. Cloud Linux chưa chạy Office2021, nên cần kiểm thử tạo/neo/căn Line và Undo trên Windows. File thực tế người dùng cung cấp không được đưa vào gói tải hoặc kho GitHub.

## Bản 0.1.10 — sửa lỗi đọc bảng 5941

Sửa cách đọc bảng chứa đoạn văn: thu gọn bản sao range về đầu đoạn rồi mở rộng tới bảng, không truy cập `paragraph.Range.Tables[1]` vốn có thể rỗng trong Word. Nếu Word vẫn không cung cấp phần tử bảng (0x800A1735), bộ kiểm chính tả tiếp tục và hiện cảnh báo chưa kiểm được đường kẻ; không tự thêm Line khi chưa đủ thông tin. Giữ khoảng độ dài 1/3–1/2 từ bản 0.1.9.

## Bản 0.1.11 — nhận diện bảng qua XML

Thay cách đọc ô/bảng bằng cấu trúc XML của bản Word đang mở (`Content.WordOpenXML`), đối chiếu từng đoạn với text, Start/End, trạng thái trong bảng và căn giữa do Word cung cấp. Không dùng `Range.Tables/Cells` để nhận diện ba mốc đường kẻ. Vẫn dùng glyph XPS để đo dòng chữ, danh sách Shape để xác định thiếu Line và `Shapes.AddLine` để tạo khi người dùng bấm sửa. Giữ tỷ lệ 1/3–1/2. Không đoán vị trí ký tự từ XML.

Bảng khác chưa đọc đủ trong 40 đoạn đầu không làm tắt kiểm tra bảng đầu. Ô gộp/lồng, padding không chắc chắn hoặc XML không khớp Word vẫn cần kiểm thủ công. Đã kiểm riêng cấu trúc file người dùng để nhận diện ba vùng; chưa thử COM/XPS và chèn Line thực tế trên Office 2021.

## Bản 0.1.12 — sửa bước đối chiếu Word/XML

Nhật ký 0.1.11 cho thấy lỗi ở điều kiện so sánh nội dung/cờ trong bảng. Bản mới dùng cấu trúc XML để xác định ô sau khi khớp text theo thứ tự; không dùng cờ COM làm điều kiện chặn. Chỉ đọc ký tự trong run, bỏ tab stop định dạng và ánh xạ ngắt trang/cột/dòng, gạch nối đặc biệt theo mã Word. Khi đoạn sau không khớp, vẫn giữ vùng đầu đã khớp đầy đủ; không tự sửa ô/trích yếu chưa được xác minh hết. Nếu còn lỗi, hiển thị/ghi mã nguyên nhân và số đoạn, không ghi nội dung tài liệu.

## Bản 0.1.13 — sửa thao tác tạo Line

Nhật ký thực tế xác định E_FAIL (0x80004005) tại setter Shape.Rotation. Bỏ setter Rotation và Height cho Line đã tạo nằm ngang bằng hai điểm AddLine; chỉ đặt vị trí, độ dài và nét. Nếu cấu hình thất bại, xóa riêng Line mới tạo và ghi bước lỗi; không xóa Line có sẵn. Nhóm sửa vẫn có thể áp dụng một phần nếu lỗi sau các mục đã thành công: dùng Undo để bỏ cả lượt.

Bổ sung căn khớp đoạn Word rỗng ngoài bảng khi XML không có đoạn tương ứng, sau phần đã khớp; giữ CP thật và không bỏ qua đoạn trong ô hay sau lỗi khác. Chưa xác minh trực tiếp Office 2021.

## Bản 0.1.14 — giữ vòng đời COM trong lượt tạo Line

Nhật ký 0.1.13 có E_FAIL lúc đọc Shape.Line và lúc Delete. Bản mới giữ collection Shapes cùng WrapFormat/LineFormat/Color tới hết cấu hình và dọn dẹp; không FinalReleaseComObject các RCW có thể dùng chung. Lấy/đặt nét trước khi thay đổi chế độ neo/vị trí, ghi rõ từng bước Style.Line/Visible/Weight/Dash/Arrows/Color. Đây là sửa quản lý vòng đời/thứ tự gọi theo điểm lỗi; chưa đủ bằng chứng kết luận duy nhất nguyên nhân trên Office 2021. Cần thử lại trên bản sao.

## Bản 0.1.15 — ghi Shape Line vào gói DOCX

Không dùng đường ghi COM AddLine/LineFormat/Delete của các bản trước nữa. Sau khi duyệt, lưu checkpoint, tạo candidate DOCX riêng, thêm `v:line` (Shape Line Word, không phải border) vào đoạn đã đối chiếu, đóng tài liệu làm việc và mở candidate. Kiểm nội dung Word không đổi và đúng một Shape Line cho mỗi vai trò vừa sửa trước khi chấp nhận; nếu mở/kiểm thất bại, khôi phục checkpoint trước. File gốc không bị thay đổi.

Nút Hoàn tác dùng snapshot theo từng lượt cho cả sửa chữ/định dạng và Line, nên vẫn hoạt động sau lượt Line cần mở lại Word. Lịch sử chỉ trong phiên đang mở, không có Redo. Chọn nhiều Line chỉ mở lại một lần, nhưng lượt sửa có thể chờ lâu hơn thao tác COM trực tiếp. Rê chuột vẫn dùng preview cache.

Chỉ tự đặt lại Line có marker của ứng dụng trong vùng đã xác minh; Line có sẵn khác cần chỉnh thủ công. Bộ kiểm thử ghi DOCX thật từ tài liệu đã cung cấp và mẫu độc lập, dùng OpenXML SDK kiểm chuẩn Line/toàn gói, đối chiếu giữ nguyên XML chữ/định dạng và byte của các part khác, thử rollback khi mở thất bại. Chưa xác minh Word/Office 2021 thực tế hoặc độ chính xác dựng trang của VML trong môi trường Windows.

## Bản 0.1.16 — nguồn DOCX không bị khóa và kiểm tra Office tự động

Nhật ký 0.1.15 xác định IOException 0x80070020 khi ZipFile.OpenRead mở RecoveryPath đang được Word giữ handle ghi. File.Copy checkpoint đã thành công trước bước đó; dùng chính bản `.undo.docx` riêng làm đầu vào ZIP thay vì RecoveryPath. Không thay cơ chế VML/rollback đã có. Kiểm thử mới giữ khóa độc quyền trên file làm việc và vẫn ghi được candidate từ bản sao.

Menu **Kiểm tra → Kiểm tra Word tự động…** mở một WordSession riêng với mẫu giả đi kèm ZIP, nhận diện mốc, chạy đúng FixMany/ghi gói/mở lại/xác minh, xuất XPS, lưu bản sao, Undo nhóm, rồi mở lại bản đã lưu để kiểm ba Line còn tồn tại. Báo cáo `office-check.txt`, XPS và DOCX mẫu ở thư mục OfficeCheck-* trong thư mục diagnostics; Explorer tự mở sau kiểm tra. Không dùng tài liệu người dùng đang mở. Tọa độ mẫu cố định chỉ kiểm thao tác ghi/lưu, không chứng minh vị trí thể thức hay pipeline đo XPS. Chưa chạy bài kiểm tra Windows này trong cloud Linux.

## Bản 0.1.17 — vị trí Line và nhận diện trích yếu

So sánh DOCX gốc và bản sau sửa xác định hai vấn đề: VML dùng tọa độ trang trong đầu mút cục bộ nhưng thiếu vị trí hộp; dấu cách cuối đoạn trích yếu đầu làm ngắt nhận diện cả khối. Nay ghi margin-left/top theo trang với đầu mút cục bộ, tách căn giữa cấu trúc khỏi điều kiện đo chữ và dọn khung pict rỗng khi thay Line của ứng dụng.

Phân trang trước khi đọc metadata. Vai trò chưa nhận diện phải có cảnh báo. Trước chấp nhận sửa, xuất XPS và kiểm đầu/cuối/cao độ nét thực tế trong dung sai 1pt; nếu thiếu hoặc lệch, khôi phục bản trước. Menu kiểm tra Word tự động cũng chạy bước xác minh nét này. 139 kiểm tra đạt với cả cặp tài liệu riêng tư, build Windows thành công; chưa chạy Office 2021 trong cloud Linux. Tài liệu riêng tư không được đưa vào Git hoặc ZIP.

## Bản 0.1.18 — thay VML bằng DrawingML

Nhật ký 0.1.17 xác nhận kiểm nét thất bại ở Agency: mong đợi Left=145,17pt, Top=92,68pt, Width=70,90pt; bộ đọc XPS chỉ tìm một nét và không khớp. Candidate đã bị từ chối/rollback. Nhật ký cũ không ghi tọa độ nét thực tế nên chưa đủ kết luận Word đặt sai hay bộ đọc bỏ sót.

Thay đường ghi VML bằng DrawingML WordprocessingShape connector (`prstGeom=line`) với wp:anchor/positionH/positionV theo trang bằng EMU, layoutInCell=false, wrapNone; vẫn là Shape Line và giữ nguyên chữ. Đặt lại được VML cũ có marker hoặc DrawingML của ứng dụng. Không bỏ kiểm marker/type/nét XPS hoặc rollback. Nhật ký bổ sung tọa độ nét và metadata hình học native, không ghi nội dung. Chặn số trang ngoài phạm vi khi rê chuột sau dựng lại preview.

Kiểm tra Word tự động trên mẫu giả sẽ giữ candidate và XPS thất bại ngay trong OfficeCheck-* để phân biệt lỗi dựng trang với lỗi đọc nét. Không tự lưu các hiện vật này khi xử lý tài liệu thật. 129 kiểm tra mẫu và 139 kiểm tra với cặp DOCX riêng tư đã đạt; gói DrawingML qua kiểm chuẩn OpenXML SDK và build Windows. Cloud chưa chạy Office2021, nên đây vẫn là bản thử cần xác nhận bằng menu kiểm tra Word trước.

