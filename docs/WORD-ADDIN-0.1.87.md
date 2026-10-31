# Word Add-in thử nghiệm 0.1.87

Bản thử chuyển thao tác chính vào Word trên Windows x64. Bộ xử lý .NET 8 và bộ dữ liệu có sẵn được giữ, thêm lớp COM .NET Framework 4.8 để mở bảng kiểm tra trong Word. Đây là COM Add-in, không phải gói VSTO/ClickOnce; không cần cài Visual Studio để dùng gói đã đóng. Bản Desktop 0.1.86 vẫn giữ riêng.

## Phạm vi có trong bản thử

- Tab Chính tả Việt → Mở bảng kiểm tra.
- Quét nội dung chính, ngoài bảng/đoạn có tab; kiểm một số lỗi chữ, khoảng trắng, dấu câu và từ/cụm lặp bằng nền đã có. Chưa chuyển toàn bộ kiểm thể thức Nhà nước/Đảng, đầu/cuối trang, hộp chữ hoặc đường kẻ.
- Bấm Chọn chữ hoặc đúp một mục để chọn đúng vùng hiện tại. Sau đó người dùng gõ sửa trong Word; dùng Ctrl+Z và Lưu của Word. Add-in không tự thay chữ, ghi lại DOCX, đóng/mở tài liệu hay thêm định dạng gạch chân. Người dùng vẫn có thể sửa mọi vùng mà Word cho phép, kể cả vùng bản thử chưa kiểm.
- Bất kỳ sửa chữ nào sau quét đều làm cũ snapshot. Bấm Quét lại trước khi dùng vị trí lỗi cũ; không tìm đoán từ giống nhau. Tài liệu khác dù cùng chữ cũng không dùng lại snapshot.
- Từ cá nhân: lấy chữ đang chọn hoặc tự nhập, Thêm, tải danh sách, Xóa. Giữ tên hiển thị, tra NFC không phân biệt hoa/thường, không bỏ dấu; không tự học tất cả nội dung. Lưu qua phiên Word và nâng cấp.
- Thêm từ chỉ bỏ cảnh báo dạng từ chưa được nhận. Các cụm như bổ xung/sử lý vẫn được kiểm độc lập. Chưa có giao diện nhập/xuất, sửa mục, bỏ qua theo tài liệu hoặc tự kiểm khi gõ trong phiên bản đầu này.
- Đồng bộ từ đơn với Word là thao tác riêng. Xuất tệp UTF-16LE có BOM, chỉ mục gồm chữ/dấu kết hợp; cụm nhiều từ, số/gạch nối vẫn giữ trong Add-in. Đăng ký tệp riêng, giữ các từ điển/default đã có. Word có thể cần mở lại để nạp cập nhật. Nếu tệp bị khóa hoặc thay đổi bên ngoài, giữ dữ liệu và báo chưa đồng bộ, không ghi đè mù.

Gợi ý của Add-in nằm trong bảng. Dấu gạch đỏ do Word tự tạo phụ thuộc proofing/ngôn ngữ/cấu hình Word; Add-in chưa bơm cảnh báo riêng vào bộ proofing, chưa tạo lớp gạch vàng riêng.

## Cài thử

1. Cần Word desktop **64-bit**, Windows x64 và .NET Framework 4.8. Bộ xử lý .NET 8 đã được đóng kèm. Chưa có gói Word x86, Mac hay Word Web.
2. Giải nén toàn bộ gói vào thư mục mới, đóng mọi cửa sổ Word.
3. Chạy **CAI-ADDIN.cmd**. Script chép vào thư mục phiên bản/commit của người dùng rồi đăng ký hai lớp COM và một Add-in Word dưới HKCU; không thay đăng ký của ứng dụng khác hoặc dừng tiến trình Word. Script có chế độ `Install-WordAddin.ps1 -DryRun` để chỉ kiểm và xem đích.
4. Mở Word → tab **Chính tả Việt** → **Mở bảng kiểm tra** → **Quét lại**.
5. Thử một tài liệu mới có `Cần sử lý và bổ xung nhân lực.`. Chọn từng lỗi, gõ sửa trong Word, Quét lại, Ctrl+Z, lưu bản sao, mở lại để đối chiếu.
6. Thử thêm một từ/tên mới ở tab **Từ cá nhân**, đóng/mở Word và tải lại danh sách; xóa để kiểm lại. Thêm xung phải vẫn giữ cảnh báo bổ xung. Khi muốn Word nhận mục từ đơn, dùng nút Đồng bộ riêng.
7. Muốn gỡ: đóng Word rồi chạy **GO-ADDIN.cmd**. Từ cá nhân và tệp Word đã đăng ký được giữ. Gỡ ở đây bỏ Add-in/COM của sản phẩm; không tự xóa từ điển người dùng trong Word.

Script .cmd dùng PowerShell cho chính lượt cài/gỡ do người dùng khởi chạy; chính sách quản trị máy vẫn có thể ngăn cài Add-in. Gói thử chưa ký số để triển khai diện rộng. Nếu Word không hiện tab hoặc chặn Add-in, giữ thông báo và log, không tắt các cơ chế bảo vệ của máy để thử.

## Dữ liệu và quyền ghi

`%LOCALAPPDATA%\ChinhTaViet\WordAddin\personal-terms.json` là kho cá nhân, độc lập thư mục phiên bản. Ghi có khóa giữa các phiên, stage/replace; dữ liệu hỏng hoặc vượt giới hạn không bị thay bằng tệp rỗng. Giới hạn 20.000 mục/4 MiB, mỗi mục tối đa 128 ký tự.

`ChinhTaViet.User.dic` ở cùng thư mục là bản từ đơn cho Word, có dấu hash để phát hiện thay đổi bên ngoài. Đây là luồng từ Add-in sang Word, chưa phải đồng bộ hai chiều với CUSTOM.DIC. Các từ thêm bằng menu gốc của Word có thể đi vào từ điển mặc định khác.

Kho mô hình riêng người dùng đã nạp ở 0.1.85/0.1.86 được ưu tiên; nếu không có dùng bộ ngoại tuyến kèm với đầy đủ nguồn/giấy phép. Gói không chứa từ cá nhân, tài liệu người dùng hoặc dictionary đính kèm không rõ nguồn. Worker giao tiếp bằng pipe stdin/stdout do tiến trình cha sở hữu, không mở HTTP port, không gửi nội dung ra mạng. Log chỉ ghi loại lỗi/mã HRESULT, không ghi nội dung quét.

## Kiểm thử và giới hạn xác nhận

Build/checks của Core, Desktop cũ, worker/protocol, từ cá nhân và COM control được ghi đúng commit trong build-checks.json của gói. Kiểm host có COM callable wrapper, IOleObject, Ribbon, native range giả lập và giữ Unicode qua pipe cũ Windows. Có kiểm worker child process và installer dry run; kiểm ZIP sau giải nén không cần Word.

**Microsoft Word thực: NOT_RUN.** Chưa tuyên bố Word đã load COM/ActiveX pane, đưa con trỏ vào trang đúng trên mọi build, nhận dictionary đang mở, hoặc chạy an toàn trên mọi bản Office. Các kiểm bằng đối tượng giả lập/COM wrapper không thay cho các bước Word thật bên trên.

Nếu lỗi cài/không load: ghi build Word và x64/x86, thông báo Word, `%LOCALAPPDATA%\ChinhTaViet\WordAddin\addin.log`. Không gửi tài liệu hoặc từ cá nhân có thông tin riêng nếu không cần.

## Nền API tham khảo

- Microsoft Office extensibility interfaces: https://learn.microsoft.com/en-us/visualstudio/vsto/customizing-ui-features-by-using-extensibility-interfaces
- Microsoft IRibbonExtensibility: https://learn.microsoft.com/en-us/dotnet/api/microsoft.office.core.iribbonextensibility
- Microsoft Word custom dictionaries: https://learn.microsoft.com/en-us/office/vba/api/word.application.customdictionaries

Giữ manual là luồng chính; việc chuyển nốt các luật thể thức, xử lý bảng, gạch riêng và quản trị từ điển là các đợt sau khi bản đầu qua thử Word thực.
