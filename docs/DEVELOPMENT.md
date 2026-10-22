# Tiếp tục phát triển Chính tả Việt

## Bắt đầu từ đâu

Mốc đã chốt: [0.1.27](STATUS-0.1.27.md), mã gói `1ec3092ccd68ed0acd8878b2737664116a6b11fe`, tag `v0.1.27`. Đọc biên bản này trước để phân biệt bằng chứng CI, xác nhận người dùng và kịch bản còn chờ. Phát triển từ `main` sau khi chốt, tạo nhánh mới; không tiếp tục phát hành vào tag hoặc ghi đè ZIP 0.1.27.

Nhánh đang phát triển: `feature/admin-position-review-0.1.32`, từ tag 0.1.31 (`7bf086637622f4a1657edb4e66dc79e3b9e92838`). Chuỗi bản thử dựa trên main đã chốt `da6cf3b0999dbfb60d16ab1eae9733b0f1f20c81`. Đọc các CHANGES và [phạm vi 0.1.32](CHANGES-0.1.32.md). Chưa nhập main hoặc coi CI là bằng chứng Office 2021.

## Bản đồ mã nguồn

| Thành phần | Nơi đọc/sửa |
|---|---|
| UI, lựa chọn lỗi, quét lại, preview | `src/ChinhTa.Desktop/MainWindow.xaml.cs`, `IssueChoice.cs`, `PreviewHighlighter.cs` |
| Phiên Word, checkpoint, giao dịch sửa, export, save/undo | `src/ChinhTa.Desktop/WordSession.cs` |
| Cảnh báo vị trí số/ngày trên XPS | `src/ChinhTa.Core/AdministrativePositionRules.cs`, `src/ChinhTa.Desktop/AdministrativePositionInspection.cs` |
| Bộ kiểm Line dùng cho danh sách | `src/ChinhTa.Desktop/LineInspection.cs`, `ReferenceMeasurement.cs` |
| Nhận diện vùng và chính sách độ dài/tâm/khoảng cách | `src/ChinhTa.Core/ShapeLineRules.cs` |
| Ghép XML với range Word thật, cấu trúc bảng | `src/ChinhTa.Core/WordXmlTableMap.cs` |
| Marker/kiểm kê/gói Line | `src/ChinhTa.Core/DocxLineMetadata.cs`, `DocxLineInventory.cs`, `DocxLineEditor.cs` |
| Đối chiếu mốc và CP/chuỗi | `src/ChinhTa.Core/LineReferenceBinding.cs` (gồm `WordPreviewAnchor`) |
| Đo glyph, kiểm nét, phản hồi vị trí | `src/ChinhTa.Core/GlyphAdvanceRange.cs`, `RenderedLineVerifier.cs`, `LinePlacementFeedback.cs` |
| Giao dịch chữ/số/ngày, đối chiếu toàn bộ chữ | `src/ChinhTa.Core/AtomicTextBatch.cs`, `tests/ChinhTa.Checks/AtomicTextBatchChecks.cs` |
| Giao dịch nhiều Line và rollback tệp | `src/ChinhTa.Core/AtomicLineBatch.cs`, `CheckpointSwap.cs` |
| Số/ký hiệu, địa danh/ngày tháng ở đầu Nhà nước | `src/ChinhTa.Core/AdministrativeHeaderRules.cs`, `tests/ChinhTa.Checks/AdministrativeHeaderChecks.cs` |
| Ngôn ngữ minh họa, khoảng trắng, thể thức/giấy/lề | `src/ChinhTa.Core/Checker.cs`, `SpacingRules.cs`, `HeadingRules.cs`, `PageRules.cs` |
| Nhật ký và bài kiểm Word trên mẫu giả | `src/ChinhTa.Desktop/Diagnostics.cs`, `OfficeIntegrationCheck.cs` |
| Regression | `tests/ChinhTa.Checks`, `tests/ChinhTa.DesktopChecks` |

## Luồng Line hiện tại

1. Quét range Word; đối chiếu XML và text trước khi nhận diện vùng. CP lấy từ Word, không suy ra bằng độ dài XML.
2. Dựng XPS; ánh xạ CP bằng Range.Text của tiền tố thật. Đo dòng cuối bằng glyph advances/cluster và ghép nét theo vị trí thực.
3. Trước sửa, kiểm nguyên văn, lưu checkpoint riêng. Chỉ sửa marker của ứng dụng; Shape lạ hoặc vùng mơ hồ phải giữ cảnh báo thủ công.
4. Ghi DrawingML vào candidate từ checkpoint không bị khóa; mở candidate, lấy lại layout/mốc và kiểm chữ, marker, nét thật, tham chiếu và các nét cần giữ.
5. Có tối đa một candidate bù vị trí theo phản hồi đủ chắc chắn. Khi quay về checkpoint, chuyển cả kế hoạch mã hóa và đích sang CP của đúng nguồn đang mở trước khi ghi lại. Metadata đích không thay cho nét XPS.
6. Trong giao dịch nhóm, sửa theo Agency → Subject → Motto. Lưu/quét lại, có tối đa một lượt đặt lại các cảnh báo đủ điều kiện của đúng lựa chọn đã đối chiếu. Kiểm cuối chưa đạt thì rollback cả lượt. Hoàn tác giữ một checkpoint cho cả lượt.
7. UI quét lại từ trạng thái mới. Lịch sử nét đã sửa luôn phải được xác minh lại với XPS và dòng tham chiếu; không xóa cảnh báo theo lịch sử bấm nút.

## Những điều phải giữ khi thay đổi

- Không quay lại getter Shape COM AlternativeText/ID/Type hoặc setter AddLine/LineFormat để né lỗi đã gặp trên Office 2021.
- Không báo đạt dựa vào số Shape, ghi XML thành công, marker hoặc tọa độ metadata.
- Không dùng CP như chỉ số Range.Text; không đo biên chữ bằng caret hit testing. Không đoán mốc thiếu/trùng hoặc glyph không hỗ trợ.
- Khi ghép lại mốc, giữ điều kiện toàn bộ story không đổi, đúng role/nguyên văn duy nhất và điều kiện bố cục hiện tại. Không nới lựa chọn sang lỗi chưa chọn.
- Giữ bù tọa độ độc lập với đích trang; thay Line đã có phải giữ các nét khác. Không bù vô hạn hoặc ẩn cảnh báo để hoàn tất.
- Giữ chữ/định dạng không liên quan, file gốc, checkpoint, một Undo và rollback. Nếu rollback cũng lỗi, giữ hiện vật phục hồi.
- 3/7 là đích tạo cơ quan/trích yếu; khoảng 1/3–1/2 vẫn hợp lệ. Khoảng cách và dung sai hiện tại là chính sách đo, không tự gán thành con số bắt buộc của văn bản pháp lý.
- Chỉ miễn khoảng trắng trong mẫu trường số/ngày chưa điền được nhận diện hẹp. Không miễn mọi khoảng trắng trong đoạn hoặc trong nội dung thường.
- Không đưa tài liệu/log/XPS/font riêng vào GitHub hoặc ghi chữ của tài liệu vào diagnostics. Mẫu đưa lên kho phải là mẫu giả.

## Kiểm thử phù hợp với thay đổi

Trên Windows với .NET 8 SDK:

```powershell
dotnet run --project tests/ChinhTa.Checks -c Release
dotnet run --project tests/ChinhTa.DesktopChecks -c Release
```

Mốc 0.1.27: 223 Core/OpenXML + 20 WPF, [CI](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/39570349615). Không cố giữ số đếm này khi thêm test; ghi kết quả mới theo đúng commit. Linux chỉ chạy Core/biên dịch chéo được, không thay thế Word COM hoặc WPF runtime trên Windows.

Khi thay Word/Line/layout: chạy **Kiểm tra → Kiểm tra Word tự động…** trên máy có Word và lưu kết quả phiên bản tương ứng. Thử cả một đường, nhóm 1+3 và cả ba; quét lại không còn lỗi đã sửa; lưu/mở lại; một Undo bỏ cả lượt; thử tình huống sai để kiểm rollback. Các mục này là checklist, không được tự đánh dấu đạt chỉ vì CI pass. Các kịch bản cũ và phạm vi đã xác nhận nằm trong [WINDOWS-VALIDATION.md](WINDOWS-VALIDATION.md).

Tài liệu riêng chỉ dùng cục bộ theo đường dẫn tùy chọn của `tests/ChinhTa.Checks`; không commit fixture riêng. Không cần chạy lại toàn bộ runtime/build chỉ vì sửa tài liệu; phải bảo đảm mã ứng dụng và gói đã thử không bị thay.

## Đóng gói và lưu mốc

```powershell
dotnet publish src/ChinhTa.Desktop -c Release -r win-x64 --self-contained true -o artifacts/win-x64
```

Kèm thư mục `samples`, ghi chú phiên bản, ZIP và SHA-256. Chạy kiểm thử trước khi phát hành. Khi tạo bản mới, cập nhật version trong csproj, tiêu đề UI, tên gói/tag, ghi chú và workflow Windows; không phát hành lại vào tag cũ. Workflow `windows-package.yml` gắn `fix/verified-save-copy-0.1.35` và phát hành prerelease 0.1.35 sau kiểm thử. Mỗi phiên bản sau phải đổi nhánh/version và không ghi đè tag đã phát hành. Workflow `finalize-0.1.27.yml` chỉ chốt metadata của bản đã thử, có kiểm tag, digest, CI và thay đổi chỉ thuộc tài liệu/workflow được cho phép; không build hoặc thay asset.

Khi nhận lỗi, lấy log đúng version và phân biệt lỗi đọc Word, ghép XML, CP/mốc, marker, candidate, nét XPS hay rollback. Các mã `Word.Preview.CpMapping`, `Word.Preview.Unmapped`, `Word.LineBatch.ReferenceBinding.*`, `Word.LineBatch.StoryBinding`, `Word.LineBatch.Rescan` ghi số liệu mà không ghi chữ tài liệu. Không kết luận nguyên nhân chỉ từ ngoại lệ tổng quát hoặc một điều kiện guard gộp.

## Việc tiếp theo chưa triển khai

1. Bổ sung xác nhận riêng lưu/mở lại, một Undo và bài Office check đầy đủ trên 0.1.27 khi có máy Word; không ghi là đã PASS thực tế trước khi có kết quả.
2. Số/ký hiệu và địa danh/ngày tháng đã triển khai hẹp ở nhánh thử 0.1.28; chờ CI và Office thực tế. Không tự mở rộng phạm vi khi chưa có mẫu và căn cứ. Cần kiểm cả sửa riêng, nhóm, quét lại, lưu/mở và Undo; giữ nguyên ô trống và cấu trúc Word.
3. Sau đó mới mở rộng tên cơ quan, nội dung, chữ ký/nơi nhận, số trang hoặc kiểm chính tả tiếng Việt thực sự. Bộ ngôn ngữ hiện vẫn là minh họa, chưa có từ điển hoàn chỉnh.

Không gộp tính năng mới vào đợt chốt 0.1.27. Mỗi đợt tiếp theo cần phạm vi, căn cứ, mẫu giả, test có ý nghĩa và ghi rõ phần chưa xác nhận.

## Ghi chú triển khai 0.1.28

`Paragraph` có dấu quan sát định dạng và bằng chứng chữ thuần. `WordXmlTableMap.Read` chỉ cấp bằng chứng sau khi khớp XML với range sống; trường/control/link/tab hoặc tiền tố chưa khớp không được sửa tự động. Nhận diện đầu Nhà nước tách khỏi các profile khác; quy tắc khoảng trắng chỉ nhường các span tự sửa và giữ các dòng cấu trúc. Gợi ý số/ngày dùng `BatchPlan` và giao dịch hiện có, không tạo cơ chế viết toàn đoạn mới. Mẫu giả sinh bằng `scripts/create_header_samples.py`, không lấy từ tài liệu người dùng.

## Ghi chú triển khai 0.1.29

Đã tái hiện hai lỗi biên trên 0.1.28 trước khi sửa: gợi ý khoảng trắng có thể thu hẹp ô ngày trống khi có ghi chú cuối dòng, và `w:pPr/w:tabs/w:tab` bị coi nhầm là ký tự tab. `DateBlankZones` đọc riêng các vùng trống trong đầu Nhà nước đã nhận diện; không dùng parser này để sửa nội dung hoặc chấp nhận ngày có ghi chú là đúng thể thức. Chỉ miễn đúng vùng trống, các khoảng trắng còn lại vẫn kiểm. `WordXmlTableMap` chỉ coi `w:tab` trong `w:r` là ký tự; trường/control/link và các bất định khác vẫn giữ thủ công. Không thay luồng sửa/kiểm nét XPS hoặc rollback Line.

Test mới quét lại sau sửa riêng/nhóm với CP đã dịch theo độ dài mới, kiểm các vùng trống có chú thích và điểm dừng tab so với tab thật. Office check tự tạo một bản sao mẫu giả có ghi chú/điểm dừng tab, kiểm nguyên văn sau sửa, một Undo và lưu/mở; chưa có kết quả runtime trên máy Word. Không đưa file riêng vào kho.

Bằng chứng 0.1.28: [CI](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/39630158659), 283 Core/OpenXML + 20 WPF, tag/gói giữ nguyên. Kết quả 0.1.29 phải lấy theo commit và CI riêng; không chuyển xác nhận Office của 0.1.27 sang tính năng mới.

## Ghi chú triển khai 0.1.30

Đã thay nhánh sửa chữ trong `WordSession.FixManyCore` bằng `AtomicTextBatch`. Trước đó lỗi sau một phần thao tác chỉ báo dùng Undo, lỗi lưu cuối ở ngoài catch và checkpoint được đẩy lên stack trước khi hoàn tất. Nay kiểm toàn bộ nguyên văn trước ghi, ghi theo CP giảm dần, đọc lại range đã thay, đưa lưu và kiểm toàn bộ chữ vào trong giao dịch. Chỉ đẩy một checkpoint lên stack sau khi thành công.

Kế hoạch chữ dự kiến lấy vị trí từ `Range.Text` của tiền tố thật và `WordPreviewAnchor`; không dùng CP làm chỉ số chuỗi. Chỉ thay đúng span đã chọn. Rollback đóng phiên Word riêng, nạp checkpoint và kiểm chữ gốc. Nếu rollback lỗi, giữ cả lỗi gốc và lỗi phục hồi, `snapshotCurrent=false` để Dispose không xóa thư mục. Chưa xác minh được phục hồi thì không ghi là đã khôi phục. `Word.TextBatch.*` chỉ ghi mã, số bước và stack, không ghi chữ tài liệu. Không tự phát lại thao tác ghi.

Ứng dụng dùng stack checkpoint để Hoàn tác nên nhánh chữ không còn phụ thuộc Word UndoRecord; Undo công khai vẫn dùng checkpoint như trước. Nhánh Line, bù vị trí, kiểm marker/nét XPS và rollback Line giữ nguyên. Giao dịch nhóm chữ có thể gồm gợi ý định dạng tiêu đề; phục hồi từ DOCX giữ cả chữ và định dạng cũ, còn kiểm sau lưu mới đối chiếu toàn bộ chữ, chưa chứng minh mọi định dạng trên Word.

Test gây lỗi bao gồm từng bước ghi, đọc lại sai, nguồn thay sau preflight, lưu lỗi, hậu kiểm thấy thay đổi ngoài vùng và rollback lỗi; ghép thật với `CheckpointSwap` trên tệp giả, không cần dữ liệu riêng. Office check thêm lỗi có chủ đích sau bước đầu/cuối trong phiên riêng, kiểm khôi phục nguyên văn, giữ Undo của lượt trước và sửa tiếp. UI không bật điểm gây lỗi này. Chưa có kết quả Office thực tế cho 0.1.30.

## Ghi chú triển khai 0.1.31

`CharacterFormat` là payload chỉ đổi phông/cỡ/nghiêng/màu của dòng số/ngày thuần đã nhận diện, giữ Original == Replacement. `FixKind.AdministrativeFormat` được thêm ở cuối enum. Dòng phải có mẫu rõ, quan sát định dạng và không trộn phông/cỡ/nghiêng/màu. Trường/tab/link/control hoặc mẫu không rõ giữ thủ công. Cỡ ngày đang trong 13–14 được giữ, ngoài khoảng chọn 13; số là 13 đứng. Đậm/gạch chân/căn đoạn không nằm trong payload. Căn cứ đọc lại NĐ30 PL I Phần I.II.3.c, 4.c, PDF 23; chưa thêm chính sách tọa độ/căn giữa tương đối.

`BatchPlan` chỉ cho phép chồng lấn có chủ đích giữa đúng payload định dạng không đổi chữ và gợi ý chữ thuần nằm trọn bên trong; định dạng cạnh tranh, đổi chữ giả dạng format, vượt biên hoặc các chồng lấn khác vẫn chặn. Thực hiện toàn bộ CharacterFormat trước, rồi chữ theo CP giảm dần. Lựa chọn vẫn đúng tập đã chọn; không tự kéo gợi ý khác vào. `AtomicTextBatch.ExpectedStory` không ghi đè chữ bởi payload format. Quy tắc khoảng trắng không để gợi ý format cả dòng che lỗi nhỏ.

Word quan sát font trên phần chữ, loại dấu kết thúc đoạn/ô để không coi font khác của dấu đoạn là font trộn. Payload mới chỉ gọi Font.Name/Size/Italic/Color. Theo dõi range sống, dựng chữ dự kiến trong từng dòng bằng prefix thật; sau lưu kiểm chữ toàn story và chữ/phông/cỡ/nghiêng/màu của các dòng vừa định dạng. Không đạt thì dùng rollback 0.1.30; giải phóng range trước khi đóng Word phục hồi. Giữ một checkpoint Undo cho cả nhóm. Các luồng Line/XPS/bù vị trí không đổi.

Test Core mới kiểm sửa riêng/chung, cỡ hợp lệ, ô trống, dữ liệu trộn/ngoài phạm vi, ngoại lệ chồng lấn hẹp, chữ ngoài vùng và rollback mô phỏng cả chữ/lẫn định dạng. Office check tạo mẫu giả Arial/đỏ với đậm/gạch chân để kiểm định dạng riêng, lưu/mở, Undo, rollback sau hai bước format và một bước chữ, rồi sửa chung trong một lượt. Kiểm kiểu đậm/gạch chân/căn đoạn bằng getter chỉ trong phiên Office check. Các bài Word mới còn chờ chạy thực tế; không gán kết quả CI thành Office PASS.

## Ghi chú triển khai 0.1.32

Cảnh báo vị trí là read-only. `AdministrativePositionRules.Identify` dùng các ứng viên đầu Nhà nước đã nhận diện, range/XML cùng ô bảng đơn được xác minh. Dòng số cần toàn bộ phần tên cơ quan in hoa trước nó trong ô; ngày cần cả quốc hiệu và tiêu ngữ trong cùng ô phía trên. Thiếu vùng/mốc không được coi là đã kiểm đạt. Đầu ngoài bảng và bảng chưa ghép chắc chắn chưa được đánh giá hình học đầy đủ. Trường/control/tab thật, ứng viên trùng hoặc ngoài profile/trang bị loại theo nhận diện hiện có.

`PreviewHighlighter.Measure` đi qua cùng phép đo advances cũ; chỉ đường mới `MeasureSingleLine` thu thêm baseline theo transform thật. Nhiều glyph run cùng baseline hợp lệ; nhiều baseline, xoay, cắt clip, khung ngoài trang hoặc ánh xạ thiếu trả chưa đo. Không dùng caret hit testing, số Shape, cờ căn giữa hay chiều dài XML thay cho vị trí thật. Không dùng lại dòng cuối của đoạn xuống dòng để kết luận vị trí cả dòng.

Số so tâm với bao khối chữ cơ quan; ngày so tâm với cả quốc hiệu/tiêu ngữ khi hai mốc đồng tâm trong dung sai. Kiểm nằm dưới bằng khung glyph; cùng hàng dùng baseline thật để không lệch giả do cỡ 13/14 khác nhau. 3 pt là ngưỡng phát hiện của bản thử, 0,5 pt là dung sai đo nhỏ cho baseline/biên, không gán thành số bắt buộc trong NĐ30. Nếu quốc hiệu/tiêu ngữ không đồng tâm, không tùy chọn một mốc để báo ngày đúng.

UI dùng XPS đã xuất trong Scan và cùng cache preview, thêm probe CP thật cho dòng/mốc, không xuất Word lần hai. Mọi cảnh báo vị trí CanFix=false. Không có fallback đánh dấu theo chuỗi đơn nếu anchor Word thiếu. Thay font/chữ vẫn dùng giao dịch 0.1.30–0.1.31 và quét lại dựng XPS mới; Line/rollback giữ nguyên. Cảnh báo chưa đo được không bị xóa để làm danh sách trống.

Core kiểm vai trò/ô, thiếu/trùng mốc, dung sai, baseline khác cỡ chữ, ngoài trang/profile và dữ liệu vô hiệu. WPF kiểm glyph/transform thật, lệch tâm/hàng, mất anchor, mốc đối nghịch, clip/xoay/xuống dòng và giữ các bài Line cũ. Office check thêm mẫu giả chuẩn và bản cố ý thụt lề/giãn trước để phát hiện lệch; lưu hai XPS trong thư mục kết quả. Chưa có runtime Office 2021 cho 0.1.32; mẫu chuẩn chưa đo được phải báo chưa đạt, không bỏ qua để tạo PASS.


## Ghi chú triển khai 0.1.33

`AdministrativePositionTarget.ReferencesCentered` chỉ đến từ các paragraph tham chiếu có căn giữa, không thụt lề, không khoảng trắng đầu/cuối và lề ô cân bằng trong XML/range sống đã đối chiếu. Cờ này chưa đủ để sửa. `AdministrativeCentering.Proven` đòi toàn bộ glyph một dòng, cùng khung clip ô trên XPS, tham chiếu cùng tâm với khung và dòng sửa nằm trọn bên dưới. Thiếu clip/anchor, chữ xoay/cắt/xuống dòng, bảng gộp/lồng hoặc mốc chưa rõ tiếp tục thủ công.

`AdministrativeCenterFix` chỉ chứa vai trò, không tái dùng tọa độ cũ. `WordSession.Position` đo lại trước ghi và ghép vai trò + CP + nguyên văn hiện tại. Chỉ đặt Alignment=Center và ba thụt lề ngang về 0. `AtomicTextBatch` bao quanh cả ghi, lưu, dựng XPS mới và hậu kiểm: tâm đạt dung sai, nguyên văn toàn story giữ nguyên, baseline/kích thước dòng, vị trí mốc và khung ô không đổi trong dung sai 0,5 pt. Không suy thành đạt khi không còn gợi ý nhưng mất số đo. Thất bại phục hồi checkpoint, không thêm Undo; rollback lỗi giữ cả hai nguyên nhân và hiện vật. Không retry viết. Các mã `Word.PositionBatch.*` không ghi chữ nguồn.

UI phân ba nhóm chữ/phông → căn giữa số/ngày → Line. Sửa tất cả áp dụng nhóm đầu còn lỗi rồi quét lại; chọn tích lẫn nhóm bị chặn trước ghi. Hai dòng căn giữa có thể sửa chung một lượt và một Undo. Vẫn dùng quét thật để bỏ mục đã hết lỗi; cảnh báo khác hàng độc lập được giữ. Luồng Line và phép đo cũ không thay đổi.

Test mới kiểm gate an toàn, số đo trước/sau, dữ liệu thiếu, baseline/reference/frame thay đổi, payload giả và rollback khi setter không tạo kết quả hình học. WPF dùng glyph/clip thật để kiểm đề xuất và danh sách sau sửa. Office check thêm sửa riêng/nhóm, lỗi có chủ ý sau dòng đầu, Undo, lưu/mở lại và giữ cảnh báo khác hàng. Các kịch bản Office có trong chương trình, chưa chạy đạt trên Office 2021 ở môi trường phát triển này. Gói .33 tiếp nối .32, không thay main/tag/gói cũ.


## Ghi chú triển khai 0.1.34

Rà soát .33 tìm ra khoảng trống trong hậu kiểm: `Verify(before,after)` chỉ được gọi cho dòng đã chọn. Đã tái hiện bằng Core và glyph WPF: số được căn đúng nhưng ngày không chọn bị dịch, phép kiểm riêng số vẫn chấp nhận. Đây là trường hợp mô phỏng, chưa phải lỗi được quan sát trên Office 2021.

`AdministrativeCentering.VerifyBatch` nay đối chiếu toàn bộ tập dòng số/ngày trước và sau sửa: vai trò, CP/nguyên văn, mốc và cờ tham chiếu không đổi, không mất/thêm/trùng mục. Dòng chọn phải qua phép kiểm tâm thật cũ; dòng không chọn phải giữ mọi hộp chữ/baseline/khung đang đo được trong dung sai 0,5 pt. Không coi dòng chưa đo được là đạt: nó chỉ có thể giữ trạng thái chưa đo/cảnh báo thủ công, mất dữ liệu đã có thì từ chối. `VerifyRestored` kiểm mọi quan sát trở về ban đầu sau `CheckpointSwap`, rồi mới đánh dấu restored=true. Rollback lỗi vẫn giữ cả hai nguyên nhân và hiện vật.

`EditThenScan` phân biệt sửa thất bại với sửa đã commit nhưng quét lại lỗi. Không phát lại ghi hoặc tự Undo khi chỉ lần quét bị lỗi. `MainWindow.Run` tiếp tục xóa lựa chọn/anchor/highlight cũ, đồng thời đổi trạng thái rõ ràng và giữ nguyên nguyên nhân quét trong mã `UI.ScanAfterEdit.Cause`. Một Undo của lượt đã commit vẫn được giữ. Không dùng trạng thái UI hoặc lịch sử sửa để che cảnh báo thật.

Core kiểm lỗ hổng cũ, dòng không chọn bị thay đổi/mất mốc, rollback và giao dịch chữ đã commit trước khi giả lập lỗi XPS. WPF dùng glyph/clip mới để đối chiếu cả hai dòng, mất anchor, danh sách sau sửa một/cả hai và cảnh báo khác hàng vẫn không được tích. Bài khung clip khác nay đòi đo được toàn bộ chữ/khung để tránh vượt qua nhờ mất ánh xạ. Office check cố ý dịch thụt lề của ngày không chọn sau khi sửa số, yêu cầu hậu kiểm từ chối và rollback cả hai trạng thái, không thêm Undo; giữ XPS mẫu `position-unselected-drift.xps`. Chưa chạy trên Office 2021 trong môi trường này.


## Ghi chú triển khai 0.1.35

Rà luồng lưu/mở lại phát hiện `SaveCopy` dùng `File.Copy(RecoveryPath,path,true)` trực tiếp. Sao chép lỗi giữa chừng có thể để tệp đích cũ bị ghi dở. Đây là rủi ro được xác định từ mã, chưa có báo cáo tệp thực của người dùng bị hỏng. Đợt này không thay thuật toán định dạng hoặc Line.

`VerifiedSaveCopy` tạo `.ChinhTaViet-<guid>.tmp` bằng CreateNew trong đúng thư mục đích, đọc checkpoint được Word lưu, ghi vào stage và Flush(true). SHA-256 stage và checkpoint đọc lại phải khớp hash trước copy. Chỉ sau đó mới `File.Move(stage,destination,true)` cùng thư mục/volume; không fallback sang File.Copy đè trực tiếp nếu rename lỗi. Lỗi trước publish dọn đúng stage đã tự tạo, giữ tệp đích và trạng thái Changed. Nếu không dọn được thì AggregateException giữ cả hai lỗi và đường dẫn stage để phục hồi. Không tạo Undo cho thao tác lưu.

Guard chặn tài liệu gốc, RecoveryPath và toàn bộ stack Undo. Chuẩn hóa đường dẫn qua các liên kết cha/tệp; trên Windows dùng volume serial + file index từ GetFileInformationByHandle để chặn hard link/alias trỏ đến tệp bảo vệ. Kiểm guard cả trước staging và trước publish. Không ghi chữ/nội dung/hash/đường dẫn người dùng vào diagnostics; `Word.SaveCopy.Failed`/`Word.SaveCopy.Cause` chỉ mã ngoại lệ và stack. Có thể cần quyền đọc tệp đích cũ để xác minh danh tính; thiếu bằng chứng hoặc không rename được thì giữ lỗi thay vì tự đổi cách ghi.

`WordSession.Save` lưu checkpoint rồi gọi writer, chỉ Changed=false sau publish thành công. Save thất bại vẫn giữ mọi Undo và thay đổi ở bản làm việc. Byte verification không thay cho kiểm nội dung hoặc xác nhận chuẩn thể thức; không phải sao lưu lịch sử, bảo đảm mất điện hoặc chứng nhận mọi hệ thống tệp/NAS/đồng bộ đám mây. Tệp mới theo metadata/quyền thừa kế của thư mục đích; không cam kết giữ mọi ACL, ADS hay metadata đặc biệt của tệp đích cũ khi thay bằng stage.

Core kiểm tạo/thay bản sao, lỗi từng phần, thiếu/sai byte cùng kích thước, checkpoint đổi giữa copy, đích thư mục/không tồn tại thư mục cha, tệp bảo vệ và alias. Windows kiểm hard link và khóa không cho rename; Linux cục bộ kiểm symlink tệp/thư mục. Office check tạo mẫu đích giả có sẵn, gây lỗi sau 16 byte và thử đích đang khóa; kiểm giữ bản đích/gốc, Changed=true, công bố thành công đúng checkpoint, lưu/mở lại và một Undo chữ/phông như trước. Chưa có Word trong CI hoặc xác nhận Office 2021.
