# Chính tả Việt

Ứng dụng C# / WPF chạy trên Windows với Microsoft Word desktop, làm việc offline trên bản sao DOCX và xem trước bằng XPS. Không gửi nội dung tài liệu tới dịch vụ AI.

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
