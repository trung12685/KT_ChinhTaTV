# Word Add-in thử nghiệm 0.1.90: sửa lỗi nạp trong Word

Bản này xử lý phản hồi 0.1.89 có trong COM Add-ins nhưng bỏ tích, báo “A runtime error occurred during loading”, không hiện Ribbon. Người dùng mở Word bình thường và không có addin.log.

## Lỗi đã tìm thấy

Khai báo IDTExtensibility2 cũ dùng `ref System.Array` mà chưa chỉ rõ hợp đồng COM của Office: `[In, MarshalAs(UnmanagedType.SafeArray, SafeArraySubType=VarEnum.VT_VARIANT)]`. Bản mới khai báo đủ SAFEARRAY(VARIANT), hướng tham số, ComImport và metadata của interface đã có. Giữ nguyên IID đúng `B65AD801-ABAF-11D0-BB8B-00A0C90F2744` và CLSID/ProgID sản phẩm.

Phép thử cũ chỉ kích hoạt COM và QueryInterface, chưa gọi các callback qua vtable native. Phép thử mới dùng SAFEARRAY thật từ OleAut32 và gọi OnConnection, OnAddInsUpdate, OnStartupComplete, OnBeginShutdown, OnDisconnection qua con trỏ hàm unmanaged. Bản gốc 0.1.89 thất bại ở đoạn này trên cả x86/x64; bản sửa vượt qua. Có thử mảng rỗng/một phần tử, kiểm hàm thực sự nhận Application và giải phóng tham chiếu khi ngắt kết nối. Kiểm tương tự chạy lại trên DLL được cài thực, trong probe không tham chiếu host project.

Đây là lỗi giao tiếp COM đã tái hiện được, phù hợp hiện tượng người dùng gặp. CI không có Word, nên chưa kết luận mọi cấu hình Word sẽ nạp được hoặc tab/bảng đã hoạt động trên máy người dùng.

## Cài và thử lại

1. Máy đã báo Office 32-bit: tải **0.1.90 win-x86**, giải nén cả ZIP ngoài và ZIP gói bên trong vào thư mục mới.
2. Đóng mọi cửa sổ Word, chạy **CAI-ADDIN.cmd**. Bộ cài cập nhật đăng ký riêng sản phẩm sang DLL 0.1.90 ở thư mục phiên bản mới; không cần gỡ bản cũ bằng script cũ. Từ cá nhân được giữ ở thư mục chung. Nếu bộ cài dừng, giữ thông báo để kiểm tra.
3. Mở Word bình thường. Chọn tab **Chính tả Việt → Mở bảng kiểm tra**. Nếu Add-in xuất hiện trong COM Add-ins nhưng chưa tích, thử tích đúng mục 0.1.90 một lần.
4. Nếu Word đã xếp sản phẩm vào Disabled Items: Word → Tệp → Tùy chọn → Add-ins → Manage: Disabled Items → Go; chỉ bật lại Chính tả Việt nếu có, rồi mở lại Word. Không xóa hàng loạt Resiliency hoặc bật Add-in khác.
5. Tạo tài liệu thử `Cần sử lý và bổ xung nhân lực.` → Quét lại → chọn lỗi → gõ sửa trong Word → Quét lại → Ctrl+Z → lưu/mở lại. Tiếp tục thử bảng, từ trùng và snapshot cũ như [phạm vi 0.1.89](WORD-ADDIN-0.1.89.md).

## Nếu vẫn không nạp

Bản mới ghi các mốc Construct, OnConnection.Enter/Complete, GetCustomUI, CTPFactoryAvailable và mở bảng ở `%LOCALAPPDATA%\ChinhTaViet\WordAddin\addin.log`. Mỗi dòng có phiên bản, tên tiến trình và x86/x64. Dòng của WINWORD phản ánh Word thật; dòng powershell hoặc tiến trình kiểm chỉ thuộc phép chẩn đoán. Log không đọc/ghi nội dung tài liệu, đường dẫn tài liệu, vùng chọn hay mục từ cá nhân. Lỗi ghi log không được ngăn Add-in nạp. Log được giới hạn khoảng 1 MiB.

Đóng Word, chạy **CHANDOAN-ADDIN.cmd** trong gói vừa giải nén. Script tự chọn Windows PowerShell .NET Framework đúng bitness, kiểm kiến trúc Word, Framework, đúng các khóa đăng ký sản phẩm, CodeBase, hash tệp đã cài, dấu ZoneId của tệp tải về, policy quản lý riêng ProgID này, rồi thử kích hoạt COM và đọc XML Ribbon. Không dùng Assembly.LoadFrom để biến thất bại COM thành thành công; không mở Word/tài liệu/worker hay sửa registry. Không đọc từ cá nhân. Kết quả ở `%LOCALAPPDATA%\ChinhTaViet\WordAddin\load-diagnostic.txt`; có đường dẫn cài đặt Windows, có thể che tên tài khoản trước khi gửi. Báo NativeCOM=PASS vẫn chưa phải Word đã nạp.

Nếu vẫn lỗi, gửi báo cáo load-diagnostic.txt cùng addin.log nếu đã có và thông báo Word. Không cần gửi tài liệu. Chưa thay chính sách Word/UAC/antivirus hoặc tự bỏ chặn tệp theo giả định.

## Phạm vi và nguồn

Giữ manual trong Word là mặc định; không tự thay chữ/định dạng, dùng Undo và Lưu gốc. Giữ worker/protocol v2, kiểm bảng/tab, dictionary chung và từ cá nhân của 0.1.89. Desktop 0.1.86 độc lập. Có hai gói win-x86/win-x64; chưa phải VSTO/ClickOnce hoặc bản ký số để triển khai diện rộng. Gỡ bằng GO-ADDIN.cmd của đúng bản đang cài và đóng Word; kho cá nhân được giữ.

- Microsoft native COM Add-in lifecycle signatures: https://github.com/microsoft/SampleNativeCOMAddin/blob/main/connect.h
- Microsoft IDTExtensibility2: https://learn.microsoft.com/en-us/dotnet/api/extensibility.idtextensibility2
- NetOffice imported interface parameter metadata: https://github.com/NetOfficeFw/LateBindingApi.CodeGenerator/blob/main/NetOffice/Tools/IDTExtensibility2.cs

**Word thực x86/x64: NOT_RUN.** Các kết quả Windows CI, gói và commit cụ thể nằm trong build-checks.json của bản tải. Cần xác nhận trên Word thật trước khi mở rộng luật/gạch màu.
