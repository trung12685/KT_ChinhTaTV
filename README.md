# Chính tả Việt — bản thử nghiệm desktop

Ứng dụng C# / WPF chạy offline trên Windows, dùng Microsoft Word desktop để đọc, sửa và xuất XPS xem trước theo bố cục của Word trên máy đó. Không gọi dịch vụ AI hoặc gửi nội dung tài liệu lên mạng.

## Chạy trên Windows

Cần Windows 10/11, Microsoft Word desktop đã kích hoạt và .NET 8 SDK. Clone/tải kho mã, mở PowerShell tại thư mục chứa `ChinhTa.sln`:

```powershell
dotnet run --project tests/ChinhTa.Checks
dotnet run --project src/ChinhTa.Desktop
```

Mở `samples/VanBanMau.docx`. Chọn hoặc rê chuột lên một lỗi khoảng nửa giây để tô vàng vị trí trong bản xem trước; chọn lỗi rồi nhấn **Sửa lỗi đã chọn**. Mỗi lần sửa sẽ quét lại và dựng lại trang. Có **Hoàn tác** và **Lưu bản sao**. Không thể lưu đè đường dẫn tài liệu gốc.

XPS được Word xuất trên một bản sao tạm riêng: màu highlight không được ghi vào file kết quả. File gốc không được Word mở để chỉnh sửa. Bản thử dùng một phiên Word riêng, không đóng các cửa sổ Word của người dùng. Các thao tác Word hiện chạy đồng bộ nên xuất tài liệu lớn có thể làm giao diện chờ; không rê chuột liên tục khi thử tài liệu lớn.

## Phạm vi hiện có

- Cảnh báo nhiều dấu cách giữa nội dung; không tự xóa dấu cách đầu dòng.
- Gợi ý minh họa `con đo` → `con đò`, luôn cần duyệt. Đây **không phải mô hình AI**, có thể cảnh báo nhầm.
- Kiểm tra quốc hiệu theo mẫu thử: chữ hoa, đậm, Times New Roman 13, căn giữa. Chỉ nhận diện khi nội dung khớp quốc hiệu không phân biệt hoa/thường. Chưa kiểm tra thiếu quốc hiệu, lỗi chính tả trong quốc hiệu hoặc quy định thể thức đầy đủ; không xem đây là kết luận tuân thủ pháp luật.
- Quét các đoạn trong phần nội dung chính, bao gồm bảng. Chưa hỗ trợ đầu/chân trang, textbox, chú thích, Track Changes và tài liệu được bảo vệ.
- Chưa có sửa tất cả, từ điển tiếng Việt, bộ cài hay sửa trực tiếp trên trang. Muốn tự sửa: lưu bản sao, sửa bằng Word rồi mở lại để quét.

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
