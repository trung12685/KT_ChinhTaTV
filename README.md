# Chính tả Việt

Ứng dụng C# / WPF chạy trên Windows với Microsoft Word desktop, làm việc offline trên bản sao DOCX và xem trước bằng XPS. Không gửi nội dung tài liệu tới dịch vụ AI.

**[Danh sách lỗi có thể sửa và bảng kiểm tổng thể cho 0.1.40](docs/SUPPORTED-FIXES-0.1.40.md)** — gồm điều kiện, ví dụ, ngoại lệ ô trống và các mục chỉ cảnh báo.

Ưu tiên đã thống nhất: **thể thức → lỗi chữ và lặp từ/cụm từ → ngữ cảnh và thư viện sau cùng**. Phần chính tả tiếng Việt đầy đủ còn cần xây dựng; không coi quy tắc minh họa hiện tại là bộ kiểm hoàn chỉnh.

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
