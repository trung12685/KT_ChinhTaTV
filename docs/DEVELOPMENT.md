# Tiếp tục phát triển Chính tả Việt

## Thứ tự ưu tiên theo người dùng — 23/10/2026

1. Hoàn thiện các kiểm/sửa thể thức theo nguồn hiện có, giữ bằng chứng và hồi quy.
2. Lỗi chữ có quy tắc rõ, khoảng trắng, từ/cụm từ giống nhau lặp liên tiếp; trước hết phát hiện cơ học và gợi ý có duyệt. Chưa có từ điển thì không quảng bá là đã kiểm đầy đủ chính tả.
3. Ngữ cảnh, ngữ nghĩa và thư viện phục vụ các phần khó thực hiện sau cùng. Không bổ sung suy đoán ngữ cảnh trong khi người dùng đang thử phần cơ bản.

## Bắt đầu từ đâu

Mốc đã chốt: [0.1.27](STATUS-0.1.27.md), mã gói `1ec3092ccd68ed0acd8878b2737664116a6b11fe`, tag `v0.1.27`. Đọc biên bản này trước để phân biệt bằng chứng CI, xác nhận người dùng và kịch bản còn chờ. Phát triển từ `main` sau khi chốt, tạo nhánh mới; không tiếp tục phát hành vào tag hoặc ghi đè ZIP 0.1.27.

Nhánh đang phát triển: `feature/sentence-end-spacing-0.1.44`, từ 0.1.43 (`dd95a1dc1c62beca90d3ef9544913f36907aeb2b`). Chuỗi bản thử dựa trên main đã chốt `da6cf3b0999dbfb60d16ab1eae9733b0f1f20c81`. Đọc các CHANGES và [phạm vi 0.1.44](CHANGES-0.1.44.md). Chưa nhập main hoặc coi CI là bằng chứng Office 2021.

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

Kèm thư mục `samples`, ghi chú phiên bản, ZIP và SHA-256. Chạy kiểm thử trước khi phát hành. Khi tạo bản mới, cập nhật version trong csproj, tiêu đề UI, tên gói/tag, ghi chú và workflow Windows; không phát hành lại vào tag cũ. Workflow `windows-package.yml` gắn `feature/sentence-end-spacing-0.1.44` và phát hành prerelease 0.1.44 sau kiểm thử. Mỗi phiên bản sau phải đổi nhánh/version và không ghi đè tag đã phát hành. Workflow `finalize-0.1.27.yml` chỉ chốt metadata của bản đã thử, có kiểm tag, digest, CI và thay đổi chỉ thuộc tài liệu/workflow được cho phép; không build hoặc thay asset.

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


## Ghi chú triển khai 0.1.36

`DocumentHeadingRules.Check` nhận cả Paragraph và LayoutParagraph đã ghép XML/range. Chỉ đầu Nhà nước/trang 1/40 đoạn đầu với cặp quốc hiệu-tiêu ngữ duy nhất trong 16 đoạn đầu; tên loại trong whitelist hiện có, ngoài bảng, sau header và trước văn xuôi thường. Mốc Subject hiện có được tái dùng, không thay thuật toán Line. Tên loại/trích yếu phải căn giữa không thụt lề, khối 1–4 đoạn có điểm dừng trước nội dung không căn giữa. Thiếu mốc/trùng tên loại/điểm dừng chưa rõ hoặc dấu nội dung như Kính gửi/Căn cứ/Điều/Chương giữ thủ công. Đây là phạm vi bản thử, không phải parser ngữ nghĩa đầy đủ.

Quan sát phông trên visible range không gồm dấu đoạn, không đổi cách quan sát quốc hiệu/tiêu ngữ cũ. `Paragraph.BoldMixed` tách khỏi FormatMixed để không vô tình cấm định dạng số/ngày đang có kiểu đậm trộn được giữ nguyên. Vùng trích yếu có drawing/field/control/tab hoặc kiểu đậm/phông trộn chưa được tự sửa, kể cả Line đã neo trong cùng đoạn; các đoạn khác đủ chứng cứ vẫn có thể sửa. Cách viết hoa/thường chỉ cảnh báo, không đổi nguyên văn.

`CharacterFormat.Bold` nullable: null giữ hành vi cũ của số/ngày; true chỉ hợp lệ với Kind.DocumentHeadingFormat, đứng, cỡ 13–14. BatchPlan chặn payload lẫn vai trò, thiếu payload hoặc thay chữ, vẫn cho gộp định dạng với lỗi chữ đã chọn nằm trọn trong span. `WordSession` ghép lại các gợi ý tên loại/trích yếu với CheckCore ngay trước ghi; apply Font.Name/Size/Italic/Color/Bold, hậu kiểm cả Bold thật cùng tracked range và toàn story. Không dựa vào số mục để xác nhận sửa. Lưu/rollback trong AtomicTextBatch và một Undo giữ nguyên, không tự retry viết.

Giữ thứ tự chữ/phông → căn giữa số/ngày → Line. Đổi phông/cỡ có thể đổi số đo dòng; Scan dựng lại và kiểm nét XPS sau đó. Mục đã sửa phải biến mất theo quy tắc quét mới, cảnh báo khác còn thật vẫn giữ. Lưu bản sao có kiểm byte của .35 không đổi.

Core kiểm nhận diện/ranh giới, kiểu trộn, cỡ hợp lệ, câu nội dung bị căn giữa, chữ hoa thủ công, payload, overlap và lỗi hậu kiểm đậm. Office check thêm mẫu giả title + 2 subject, cỡ 16/14/13,5, Arial đỏ/nghiêng/chưa đậm, gạch chân và dấu đoạn phông khác: sửa riêng, lỗi có chủ ý giữa nhóm, sửa nhóm + một lỗi khoảng trắng, giữ toàn chữ/kiểu khác, lưu/mở và một Undo; thêm trích yếu trộn đậm giữ thủ công. Không đưa tài liệu/font riêng vào kho. CI không có Word; chưa có báo cáo Office check đúng version cho chức năng mới. Phản hồi chưa phát hiện thêm lỗi của người dùng không được ghi thành PASS mọi kịch bản.


## Ghi chú triển khai 0.1.37

Rà .36 thấy `Checker.Check` chưa dùng TextEditable cho spacing/con-đo, trong khi DocumentHeadingRules đã giữ field/control/drawing thủ công; HeadingRules cũng chưa dùng cờ. Chưa ghi nhận hỏng tài liệu thật. Nay generic text chỉ đưa lựa chọn nếu cờ cho phép; nếu có ứng viên chữ ở vùng không cho phép, tạo một Advisory toàn đoạn không payload, nguyên văn bằng thay thế. CP của đoạn lấy từ Word; tránh đưa substring CP suy từ chữ khi field/đối tượng có thể chiếm CP ẩn. Ngoại lệ ô số/ngày và protectedAdministrative cũ giữ nguyên. Kiểu trộn không tự suy thành chữ không sửa được.

WordSession áp cờ XML đã có cho mọi 40 đoạn đầu State và các ứng viên số/ngày cũ, không thêm COM từng đoạn ngoài phạm vi. Thiếu ghép/prefix dừng thì cờ false. Chưa mở rộng quan sát XML cho toàn story hoặc profile khác. Đừng quảng bá đây là hỗ trợ sửa field/control.

`CurrentTextPlan.Validate` ghép toàn bộ nhóm chữ/phông với các gợi ý CanFix hiện tại bằng Start/Length/Original/Replacement/Kind/HeadingFormat/CharacterFormat. Thiếu, trùng hoặc payload đổi từ chối trước Checkpoint/File.Copy; nhãn/citation không quyết định phép sửa. `FixManyCore` gọi CheckCore tươi trước mọi nhóm chữ/phông; Line và căn giữa vẫn dùng cơ chế đối chiếu/hậu kiểm riêng. Không lấy lại đề xuất mới rồi tự áp nếu người dùng chọn đề xuất khác; không retry viết hoặc bỏ riêng một mục lỗi. AtomicTextBatch, expected story, post-font, rollback và một Undo vẫn giữ nguyên.

Core thêm các ca cấu trúc và stale same-text; Office check có hyperlink nội bộ/bookmark và content control giả, lấy gợi ý từ bản plain có cùng chữ rồi mở bản structured để kiểm từ chối trước ghi, sau đó sửa vùng thường và giữ metadata/chữ vùng cấu trúc qua lưu/mở/Undo. Không upload nguồn riêng. CI không có Word, runtime Office còn chờ xác nhận. Ghi rõ các cảnh báo thủ công vẫn thật sau sửa vùng khác, không dùng số mục đơn thuần làm điều kiện đạt.


## Ghi chú triển khai 0.1.38

Rà `FixManyCore` thấy `formatRanges` trước đây chỉ có CharacterFormat. Các HeadingFormat cũ chỉ được kiểm nguyên văn toàn story, nên setter font/Alignment không tạo kết quả vẫn có thể commit. Core tái hiện trường hợp story-only chấp nhận rồi bổ sung hậu kiểm phải rollback toàn nhóm; chưa có sự cố Office thực được xác nhận.

`HeadingFormat.Matches` kiểm TNR (không phân biệt hoa/thường), cỡ mục tiêu hữu hạn/dương, Bold=-1, Italic=0, Color=0/Automatic và Alignment khi payload yêu cầu. Không suy đạt từ trạng thái scanner hoặc số mục. WordSession lưu Duplicate của range đã thay chữ và định dạng mỗi HeadingFormat, Expected là Replacement. Tạo range sau thay toàn chữ để bám span mới khác độ dài; các sửa CP thấp hơn tiếp tục dịch range. CharacterFormat giữ tracker và chính sách cũ. Sau Checkpoint, đọc lại đủ thuộc tính và range.Text của cả hai loại trong cùng transaction. COM getter lỗi hoặc mismatch đều đi qua rollback cũ; release các range trước đóng Word, giữ nguyên nguyên nhân và một Undo chỉ sau commit.

Quan sát heading dùng visible range không gồm dấu đoạn giống các vùng mới; ParagraphFormat vẫn lấy ở đoạn. Điều này giải quyết việc dấu đoạn phông khác làm cảnh báo cũ còn sau sửa dù chữ đã đúng; không sửa dấu đoạn. Chưa mở rộng nhận diện/vùng XML, không đổi định dạng cỡ theo nguồn pháp lý. Không kiểm thêm vị trí tuyệt đối/ô/glyph của tiêu đề bằng phép đọc phông này; Line/XPS vẫn riêng.

`FixHeadingDriftProbe` chỉ cho phiên OfficeCheck có thư mục artifact và nhóm chứa HeadingFormat, chặn Line/Center. Gây Italic=-1 sau Save nhưng không tự ném lỗi: phép hậu kiểm thật phải bắt và phục hồi. Mẫu State/Party giả có chữ/cỡ/phông sai và dấu đoạn Arial8; kiểm reject giữ checkpoint/Changed/Undo rồi sửa nhóm + khoảng trắng, fresh scan, save/reopen, một Undo và hash nguồn. Những kịch bản này có trong gói, CI không có Word và chưa xác nhận Office 2021. Không đưa tài liệu/font riêng lên kho.


## Ghi chú triển khai 0.1.39

`CheckpointSwap.Apply` trước đây File.Delete(rollback) trong finally có thể ném sau reopen thành công hoặc che lỗi gốc sau phục hồi. WordSession.Undo chưa pop stack nếu exception này xảy ra, dù current đã là candidate. Đây là lỗi tìm từ mã và fault test, chưa có Office thực xác nhận.

API nay trả `CheckpointCleanup?` khi thao tác thành công nhưng xóa backup gặp IOException/UnauthorizedAccessException; giữ đường dẫn backup và đúng ngoại lệ cleanup. Khi thao tác thất bại, vẫn throw nguyên failure và gắn warning tại Data[CleanupDataKey]. Không wrap lỗi XPS thành loại khác vì Line feedback cần RenderedLineMismatchException thật. Nếu restore cũng lỗi, preserveRollback như trước: AggregateException hai cause, không chạy cleanup. Các lỗi khác ngoài nhóm I/O/quyền không được tự coi là cảnh báo dọn bình thường. Callback deleteRollback là biên I/O kiểm thử tương tự copy callback của VerifiedSaveCopy; production dùng File.Delete, không có retry.

WordSession.Checkpoint tập trung SwapCheckpoint cho cả Line candidate, rollback chữ/căn giữa và Undo; ghi mã Word.CheckpointCleanup.Retained, không ghi message/path/nội dung. UndoCore pop đúng một mục sau swap thành công, cập nhật Changed/snapshot như trước, dù còn warning cleanup. UndoCleanupProbe chỉ khi có LineCheckArtifactFolder; inject lỗi tại delete để giữ backup thật, không đổi copy/open/verify. Office check tạo hai sửa tiêu đề riêng, kiểm lần Undo có lỗi cleanup về trạng thái trung gian, lần tiếp theo về nguyên bản và hết stack không đổi; nguồn mẫu giữ hash.

Core dùng file thật với callback lỗi dọn để kiểm byte/trình tự/cause; Windows còn giữ handle FileShare.Read không cho File.Delete của backup thật. Kiểm close/open/copy failure + cleanup vẫn giữ lỗi chính, failure phục hồi giữ hai lỗi/backup, và mô phỏng đúng biên stack Undo. Các bài Core/Line cũ không bỏ điều kiện. Chưa thay các finally dọn candidate/XPS/thư mục khác; đừng ghi thành đã xử lý mọi lỗi cleanup. Runtime Office 2021 còn chờ người dùng kiểm tra tổng thể, không chuyển phản hồi đang kiểm thành PASS.


## Ghi chú triển khai 0.1.40

`RepeatedTextRules` tách token chữ + dấu kết hợp, không lấy số/gạch dưới trong mã; so NFC ordinal đúng hoa/thường. Chia run bằng dấu cách ASCII, ngừng tại mọi dấu câu/control/tab/NBSP. Duyệt đơn vị ngắn nhất 1–8 token, gom các bản lặp đủ đơn vị và nhảy qua span đã dùng để không đề xuất lồng/chồng. Một token, toàn chữ hoa hoặc ứng viên header số/ngày giữ manual, không cần thư viện ngữ cảnh. Đây là phạm vi bảo thủ, có thể bỏ sót cụm dài/case khác/ranh giới phức tạp.

Gợi ý có Kind.RepeatedText, BatchSafe=false, Replacement rỗng và span chỉ gồm separator + bản lặp sau. Giữ lần đầu thay vì replace toàn nhóm, tránh ép kiểu của phần giữ lại. Checker bỏ các spacing/demo nằm trong deletion, giữ các mục ngoài; gate TextEditable/protectedAdministrative cũ vẫn áp dụng. Word normalize Text=null của range đã xóa thành ""; hậu kiểm story và giao dịch cũ không đổi. BatchPlan cho CharacterFormat cả dòng chứa RepeatedText như text edit khác; kiểm kỳ vọng hàng và phông sau lưu bằng tracker cũ. Không thay MainWindow stage/confirmation hoặc bắt đầu thư viện ngữ cảnh.

Core kiểm ranh giới, Unicode, hai nhóm, 8 tiếng, từ láy/mã/header, overlap spacing/demo/style, fresh scan và rollback sau delete. Office sample có phần giữ lại đậm/gạch chân, hai cụm lặp + một spacing + hai manual; kiểm riêng/nhóm, post-delete lỗi, nguyên văn/kiểu và Undo/save/reopen. Chưa xác nhận Office 2021. Bảo vệ XML sâu vẫn chỉ 40 đoạn đầu State; chưa dùng tính năng mới để tuyên bố field/control mọi trang/profile an toàn. Không có từ điển chính tả đầy đủ; `con đo` vẫn chỉ minh họa cũ. Danh sách người dùng .40 thêm C13, giữ nguyên tài liệu .39.


## Ghi chú triển khai 0.1.41

`DevelopmentReport` là nguồn cấu trúc của danh mục C01–C13, M01–M08, D01, milestone và roadmap. Tổng nhóm tính từ Mode, không dùng số lỗi/đếm thao tác trong DOCX. Report Build chỉ nhận version/thời điểm/evidence; không nhận tài liệu, đường dẫn hoặc nội dung Word. Giữ thứ tự user chốt: thể thức, lỗi chữ cơ bản, ngữ cảnh/thư viện sau cùng. Khi thêm loại lỗi phải cập nhật danh mục, guide và kiểm thử, không coi nhóm là mọi trường hợp đã hỗ trợ.

`ReportEvidence` kiểm JSON đúng version, số kiểm thử dương, commit/URL CI đúng định dạng. Workflow Tee log hai bài Core/WPF đã qua exitcode, lấy tổng thực tế, ghi build-checks.json và đưa vào publish folder. Bước riêng --verify-report-package chỉ kiểm manifest trong gói thật qua parser/renderer, không chạy lại toàn WPF; đồng thời xuất TXT mẫu cho ZIP/Release. File thông tin build không phải chữ ký/xác nhận Word; thiếu/sai không hiển thị số PASS. Không gán cứng số kiểm tra dự kiến cho version hiện tại.

`ProgressReportWindow` chỉ đọc và cho xuất TXT bằng delegate. Không dùng MainWindow.Run cho việc xuất báo cáo vì handler lỗi đó xóa choices/anchors của tài liệu. Report tự xử lý lỗi I/O, không gọi Scan/Word. WordSession.SaveProgressReport tạo nguồn UTF8-BOM riêng trong thư mục phiên, gọi VerifiedSaveCopy với toàn bộ Source/Recovery/Undo bảo vệ, không Checkpoint hay đổi Changed/Undo; cleanup nguồn riêng bắt I/O/quyền và log độc lập để không biến xuất thành công thành lỗi dọn. Đuôi TXT bắt buộc; alias/hardlink vẫn bị guard theo danh tính, không chỉ nhìn đuôi.

Core kiểm số nhóm và scope, evidence stale/malformed/negative/wrong URL, thứ tự roadmap và report không giả định Office PASS. WPF kiểm UI chỉ đọc, export trước mở Word, encoding/replace/locked target/hardlink/đuôi, giữ trạng thái/no Undo/stage. Office check khi có dirty edit kiểm không đổi story/Changed/checkpoint/nguồn và một Undo vẫn về đúng bản; chưa chạy Office 2021 trong môi trường CI. Không sửa quy tắc chữ/lặp/Line ở bản này. Hướng dẫn người dùng: REPORT-EXPORT.md.

## Ghi chú triển khai 0.1.42

`PunctuationSpaceRules` duyệt dấu phẩy/chấm phẩy, gom ASCII space sát hai bên, xác minh hai từ có ≥2 chữ cái (hỗ trợ combining mark), ranh giới và ngoại lệ kỹ thuật/header/toàn hoa. Span có độ dài dương, chỉ gồm dấu câu + space; không cần cho phép insertion rỗng trong BatchPlan. Cố ý bỏ sót ngoài phạm vi, không dùng từ điển hay suy nghĩa. TextEditable=false trả manual cả đoạn; Checker vẫn áp gate cũ. Đối chiếu XML sâu chưa mở rộng.

Checker nhường generic spacing chồng span cho C14, sau đó vẫn nhường span chứa trong deletion cụm lặp như cũ. BatchPlan cho punctuation chia sẻ CharacterFormat bao cả span. Không thay CurrentTextPlan/Word CP mapping, giao dịch, hậu kiểm story/phông, Undo hoặc nhánh Line. Office fixture dùng chữ trước dấu câu có đậm/gạch chân riêng; lỗi cố ý sau text edit thứ ba (dấu câu đầu trong thứ tự từ cuối) kiểm rollback nhóm hỗn hợp. Sau Undo sửa riêng, mở lại nguồn mẫu để baseline Changed=false trước fault probe.

Cập nhật catalog C14/milestone, guide và báo cáo mẫu từ renderer/gói thực; workflow lấy số kiểm từ log đã đạt, không dự đoán trước. Chưa có xác nhận Word/Office 2021 cho .42; không merge main hoặc thay stable.

## Ghi chú triển khai 0.1.43

`ParenthesisSpaceRules` trước hết kiểm toàn đoạn có cặp ASCII round parentheses cân bằng/không lồng; mỗi cặp xét ranh giới ngoài và phần chữ hẹp. Duyệt chữ/dấu kết hợp, ASCII space, phẩy/chấm phẩy, ba dấu nối; yêu cầu từ ≥2 chữ cái và có chữ thường; không cố phân loại ngữ nghĩa mọi token. Bỏ qua dấu kỹ thuật/tab/header, regex tuyến tính ngày…tháng…năm ngay cả chú thích làm header parser chặt không nhận. Không thay AdministrativeHeaderRules để đoán header. Mỗi mép chỉ deletion ASCII space, tránh ghi lại cả cặp/runs. TextEditable=false trả manual cả đoạn; gate/giới hạn XML cũ giữ nguyên.

Checker gộp các generic-space chồng C14/C15 rồi nhường deletion cụm lặp như cũ. BatchPlan cho ParenthesisSpace chia sẻ full CharacterFormat. Suggestion có chữ rõ cho replacement rỗng. PunctuationSpaceRules nhận ranh giới ngoặc prose, chặn mở ngoặc dính tên hàm; sửa C15 một mình không được làm C14 chưa chọn biến mất. SpacingRules giữ span toàn ASCII space ngay giữa ( và ) để generic Fix All không thu hẹp ô trống — chỉ skip C15 là chưa đủ.

Office fixture có đậm/gạch chân từ đầu trong ngoặc; CP của probe lấy từ edge issue thật rồi dịch bằng delta ASCII của các edits đứng trước, không lấy string.IndexOf làm Word CP. Sau Undo riêng mở lại nguồn để baseline Changed=false; fault probe edit thứ hai đã xóa mép đóng, phải rollback cả nhóm. Kiểm nhóm, rescan, Undo, save/reopen, source hash; chưa có runtime Office. Cập nhật C15/milestone/guide và báo cáo gói; giữ main/stable/tag cũ.

## Ghi chú triển khai 0.1.44

`SentenceEndSpaceRules` chỉ dò một dấu .?! cuối đoạn sau khi bỏ terminator/ASCII trailing space khỏi vùng xét (không sửa chúng). Span chỉ là ASCII space ngay trước dấu; last word ≥2 chữ cái thường, có word prefix, ranh giới rõ, không Roman hoặc token sau chấm/chuỗi dấu. Bỏ qua technical markers/control/header/date words với regex tuyến tính; chưa là recognizer viết tắt/ngữ nghĩa đầy đủ. Chấp nhận bỏ sót để giữ phạm vi hẹp. TextEditable=false trả manual cả đoạn; không mở rộng giới hạn XML hiện có.

Gộp C16 vào đường punctuation hiện tại để suppress generic spaces chồng span; hỗ trợ full CharacterFormat trong BatchPlan. Replacement rỗng có Suggestion rõ; dùng normalization range.Text=null, whole-story proof, rollback/Undo cũ. Không thay Line/XPS/save/checkpoint. Office fixture có ba C16 + năm edits khác, style ở từ giữ lại, fault ngay write đầu (C16 ở cuối tài liệu); sau Undo riêng mở lại source để baseline Changed=false. CP probe lấy từ parenthesis issue thật và delta ASCII phía trước, không dùng string.IndexOf làm Word CP. Thêm C16/milestone/guide/metadata gói; chưa có xác nhận runtime Office.
