# Bàn giao Chính tả Việt sang tài khoản Pro

Ngày 29/10/2026, giờ Việt Nam (UTC+7). Đây là ảnh chụp trạng thái đã kiểm chứng trong phiên làm việc cũ; tài khoản mới cần kiểm tra lại GitHub trước khi thay đổi nhánh. Không có công việc sửa mã nào được thực hiện trong lượt tạo bản bàn giao này.

## Cách dùng bản bàn giao trên GitHub

Bản bàn giao nằm trên nhánh riêng handoff/pro-2026-10-29, từ đúng mã0.1.77 commit6180757eebdbf59b82b6435bb351cba68c712103. Chỉ bổ sung tài liệu và bằng chứng; không đổi mã ứng dụng, main hoặc các gói cũ.

1. Tải ZIP của nhánh bàn giao bằng Code → Download ZIP, rồi gửi ZIP cho tài khoản Pro. Mã nguồn nằm ở thư mục gốc sau giải nén, tài liệu này ở docs/HANDOFF-PRO-2026-10-29.md.
2. Mở docs/PROMPT-FOR-PRO-2026-10-29.txt, sao chép và dán vào chat Pro. Nếu không tải ZIP được, gửi Pro đường dẫn tài liệu này và repo; mọi tệp cũng đọc trực tiếp được trên GitHub khi có quyền truy cập.
3. Kết nối GitHub của bạn nếu muốn Pro đẩy mã/tạo PR. Không gửi mật khẩu, token hoặc cookie trong chat.
4. Pro đọc tài liệu, docs/handoff-evidence và mã, kiểm tra lại head trước khi push. Tiếp tục từ0.1.77, không bắt đầu từ main hoặc bản tải0.1.76.

Tài khoản mới không tự nhận lịch sử chat, file đính kèm, kết nối hoặc môi trường tài khoản cũ. Bản ZIP GitHub không có .git. Các đường /workspace/... trong evidence thuộc máy cũ, không chắc tồn tại ở máy mới. File OfficeCheck/diagnostics gốc của người dùng không được đưa vào repo/ZIP bàn giao; khi cần tái hiện phải gửi lại.

## Dự án và mục tiêu

- Repo: https://github.com/trung12685/KT_ChinhTaTV
- Ứng dụng Windows C#/.NET 8/WPF, làm việc qua Microsoft Word COM, DOCX/OpenXML và bản xem trước XPS.
- Mục tiêu: kiểm và sửa có duyệt các phần thể thức văn bản tiếng Việt; bảo toàn phần chưa chọn, Undo, checkpoint/rollback và nguồn đầu vào. Chưa phải bộ kiểm chính tả/ngữ nghĩa tiếng Việt đầy đủ.
- Danh mục hiện có: 33 nhóm đề xuất có điều kiện, 8 nhóm thủ công, 1 minh họa. Đây là nhóm chức năng, không phải số lỗi đã sửa hoặc chứng nhận kiểm đủ mọi tài liệu.
- DevelopmentReport.cs là nguồn danh mục/tiến độ trong ứng dụng; đọc cùng docs/SUPPORTED-FIXES-0.1.76.md để phân biệt phạm vi bản đã phát hành với mã đang phát triển.

## Quyết định và yêu cầu người dùng phải giữ

1. Thể thức và độ ổn định trước; lỗi chữ/khoảng trắng/dấu câu/từ-cụm lặp tiếp theo; từ điển và ngữ cảnh/ngữ nghĩa sau cùng.
2. DỪNG tối ưu tốc độ. Bộ đọc thử nghiệm OFF mặc định. Không tự mở lại công việc hiệu năng hoặc đổi mặc định bộ đọc.
3. Tài liệu thường in rồi mới ký: ưu tiên khoảng trống khoảng 80 pt. Không ưu tiên nhận diện ảnh chữ ký. Giữ bảo toàn ảnh có sẵn; không tự thêm/xóa ảnh. 80 pt là lựa chọn của người dùng, không phải quy định NĐ30.
4. Người dùng muốn làm thực tế, chủ động, theo nhiều cụm có ý nghĩa; không chỉ đưa kế hoạch hoặc hỏi lại việc thường lệ. Giữ chất lượng và báo tiến độ trung thực.
5. Khoảng 5–10 cụm hoàn thành mới đóng gói Windows, không phát hành ZIP cho từng chỉnh sửa nhỏ.
6. Giữ main và tag/gói cũ; PR hiện tại là draft, chưa merge. Không force-push/ghi đè bản phát hành cũ.
7. Không nới điều kiện chỉ để hiện “sửa được”, không xóa nội dung để làm mất cảnh báo. Unknown/thiếu chứng cứ phải nêu rõ hoặc phục hồi.
8. Không gọi CI là Word thực. Mỗi phạm vi mới cần sửa riêng/chung, quét lại, lỗi giữa/cuối lượt, một Undo, lưu/mở lại, nguồn bất biến và giữ phần chưa chọn.

Đọc thêm docs/USER-PREFERENCES.md và RELEASE-CADENCE.md.

## Trạng thái chính xác tại điểm bàn giao

### Mã đang phát triển — tiếp tục từ đây

- Version: 0.1.77, CHƯA có ZIP 0.1.77.
- Branch: feature/multidigit-page-batch20
- Commit: 6180757eebdbf59b82b6435bb351cba68c712103
- Tree: 7d85c829ef54938916454c47572baeecdc7cc541
- Draft PR #61: https://github.com/trung12685/KT_ChinhTaTV/pull/61
- PR base: feature/page-layout-proof-batch17-19, commit dd6858881887e9168547aaf13cb2e39e59832708.
- CI đúng commit: https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40818456375
- Job 123265053249: SUCCESS, 4.929 Core/OpenXML + 857 WPF; ứng dụng biên dịch thành công.
- Hash 14 tệp thay đổi đã đối chiếu với tree GitHub. Local harness phụ có 5.123 assertion; KHÔNG cộng số đó vào số kiểm CI.
- Hai cụm hoàn thành sau bản 0.1.76: đợt20, đợt21. Chưa đủ nhịp đóng gói mới.
- Chưa có kết quả Microsoft Word thực đã PASS cho mã này.

### Bản chạy Windows gần nhất — 0.1.76

- Release: https://github.com/trung12685/KT_ChinhTaTV/releases/tag/v0.1.76
- ZIP: https://github.com/trung12685/KT_ChinhTaTV/releases/download/v0.1.76/ChinhTaViet-0.1.76-win-x64.zip
- TXT tiến độ: https://github.com/trung12685/KT_ChinhTaTV/releases/download/v0.1.76/ChinhTaViet-0.1.76-tien-do.txt
- Commit/tag v0.1.76: dd6858881887e9168547aaf13cb2e39e59832708
- Branch release/windows-0.1.76; prerelease ID 427462330, 8 asset.
- ZIP 72.944.807 byte; SHA256:
  801c3ff17564fe006e8cabcd18522f1d9777ca8cd8163ff6c8599b8dcad27fec
- Package CI: https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40816855249
- Job 123259839118: SUCCESS, 4.922 Core/OpenXML + 838 WPF + 3 kiểm metadata gói.
- Metadata tải công khai, SHA256.txt và digest các asset nhỏ đã đối chiếu; ZIP digest server khớp log build. Không chạy Microsoft Word trong CI.
- Bản 0.1.75: commit fa04375d56255abddb8307d528d5956178fcb9e9, giữ nguyên.

### Mốc bảo toàn

- main: da6cf3b0999dbfb60d16ab1eae9733b0f1f20c81
- Checkpoint khôi phục v0.1.67: 784a41ae9db8dcad02b9cccd69ab8836b9a56dae
- Các nhánh feature xếp chồng, chưa merge main. Chỉ đọc main để đối chiếu; KHÔNG checkout main làm nguồn phát triển mới.

## Những gì đã làm — nguồn đầy đủ nằm trong docs và mã

### Nền tảng và các đợt trước

Nguồn mang theo toàn bộ tài liệu lịch sử, CHANGES từng bản, FORMAT-BATCH-*.md, checklist hỗ trợ, mô tả UI/luồng sửa và bài Office. Không nên chỉ dựa bản tóm tắt này để sửa các luật cũ.

- Luồng chọn/sửa nhiều nhóm, giữ chữ/phông/cỡ/căn-khoảng đoạn và phần chưa chọn; checkpoint, rollback, một Undo, lưu bản sao/mở lại.
- Ba Line Agency/Motto/Subject: nhận diện, ghép XML/native, chỉnh DOCX, đo thật XPS và đo lại sau thay đổi font/bố cục. Không nhận nét ở sai vị trí chỉ vì cùng chiều dài.
- Các nhóm thể thức Nhà nước, nội dung, nơi nhận, khối ký, trang và PAGE có phạm vi điều kiện riêng. Lỗi chữ cơ bản có ngoại lệ; xem danh mục thực thay vì suy “đã có bộ chính tả đầy đủ”.
- Đợt10–14 / bản0.1.75: khối ký cuối tài liệu dài; bảo toàn ảnh inline đã có; lệnh chừa chỗ ký tay80pt; siết nguồn XML spacing và báo phần chưa kiểm; chạy bài Word theo nhóm độc lập để lỗi Subject không che nhóm sau.

### Đợt15–19 / bản0.1.76 đã phát hành

15. Sổ phạm vi bất biến của lần quét cho33nhóm: trạng thái quan sát khác số cảnh báo; reset khi mở/quét/lỗi. C11/C12/C33 chỉ hoàn tất hình học từ bằng chứng bản xem trước. Cửa sổ đọc/sao chép báo cáo không chứa nội dung/CP tài liệu.
16. PAGE phải được vẽ thật, đen đục, đúng font/cỡ/đứng, trọn clip, không mask/effect chưa rõ hoặc xoay/lật. Bảo toàn paint/font/opacity của chữ/nét/footer, không chỉ text và bounds.
17. Bảo toàn bitmap thông thường qua pixel Pbgra32 đã giải mã, khung, alpha, resampling/snapping. Image và ImageBrush toàn ảnh tương đương có thể khớp khi XPS đổi URI/mã hóa. Ảnh thân bài trong lề trên vẫn được giữ. Crop/tile/profile/độ sâu cao/float/mapping chưa chứng minh vẫn thủ công/rollback. Không phải nhận diện chữ ký.
18. Chuyển footer chỉ PAGE lên header trong một section dọc, sốẢRập từ1; bản0.1.76 giới hạn1–9trang. Kế hoạch gắn nguồn, glyph nguồn đúng duy nhất/phông/cỡ/đậm/nghiêng/màu; chỉ bỏ reference đã duyệt, giữ part/quan hệ footer gốc. Sau sửa không có whitelist. Word bỏ orphan part/quan hệ thì rollback bảo thủ; chưa chứng minh mọi Word giữ chúng.
19. C32 cho header/footer thực sự trống: kiểm reference/quan hệ/style/default; không coi spaces, field, hình hoặc trang trí là trống. Candidate chỉ đổi pgSz/pgMar, giữ byte mọi part khác. Sau Word phải còn proof trống và chữ/mực/nét nằm trong vùng bốn lề, trước sửa nhóm kèm và cuối lượt. Nhận trang trắng thật, chặn nền/mask/effect chưa rõ.

### Đợt20–21 / mã0.1.77 đã kiểm, chưa đóng gói

20. PAGE footer mở tới10.000trang, có thể tách thành nhiều glyph. Ghép toàn cụm theo dòng nền/chiều cao/khoảng ngang, không chọn chuỗi con hoặc cụm chồng/lệch; kiểm đúng kiểu từng glyph. Giới hạn128glyph số mỗi vùng footer. Hậu kiểm số dư dùng cùng phép ghép. Core có các mốc9/10/99/100/999/1000/10000 và chặn10001. WPF có tài liệu12/101trang. Thêm bài Word “Chuyển PAGE từ10trang”; tổng9nhóm.
21. Header phải là một cụm số nhất quán, không chỉ nối chữ số trái–phải cho ra giá trị mong muốn. Các ca âm giữ tâm đúng nhưng lệch dòng/cách xa/chồng vẫn bị chặn. Giữ mọi điều kiện font/cỡ/đen/đứng/clip/vị trí và trang đầu.

## Lịch sử lỗi và bằng chứng còn thiếu

- Người dùng từng gửi office-check.txt, diagnostics.log, các ZIP OfficeCheck và ảnh chụp hộp thoại.
- 0.1.69: IOException0x80070020 tại File.ReadAllBytes checkpoint đang được Word mở, trước bước so sánh hai bộ đọc. Đã xử lý ở chuỗi bản sau bằng handle đọc chia sẻ ReadWrite/Delete; vẫn bắt mất file, exclusive lock hoặc byte đổi. Không quy mọi lỗi thành sai khác hai bộ đọc.
- Kiểm Word mặc định dùng bộ đọc cũ. Nếu chủ động bật bộ đọc thử nghiệm, bước so hai bộ đọc vẫn bắt buộc; không lấy NOT_RUN làm PASS.
- Phản hồi Word thực mới nhất được lưu ở phiên cũ liên quan0.1.72, bước “Đo và sửa mẫu Word2013: Subject”. Có tài liệu sửa và nhiều hậu kiểm sau đó, nhưng chưa có kết quả người dùng xác nhận toàn bài bản mới PASS.
- Log cũ có nhiều lỗi COM, XML mapping, hình học và cả các event “MeasuredCorrection.Verified”; không coi event chẩn đoán ban đầu là kết luận cuối lượt.
- Bài toàn bộ vẫn dừng ở lỗi đầu tiên. Bài theo nhóm chạy phiên/mẫu riêng, ghi PASS/FAIL/NOT_RUN, không thay kết quả toàn bài.
- CI Windows có WPF thực, nhưng KHÔNG cài/chạy Microsoft Word. Linux chỉ compile WPF reference được, không thực thi WPF. Không gán PASS Word từ hai loại bằng chứng này.

## Việc tiếp theo — ưu tiên triển khai

Đọc docs/REMAINING-WORK.md và mã hiện tại trước, không mở quyền sửa chỉ dựa danh sách dưới.

### A. Nền tảng đo giãn dòng/khoảng đoạn trên Word (ưu tiên tiếp)

- Exactly/AtLeast và khoảng đoạn Word gộp chưa được tự sửa chung.
- C28: tổng Before+After nhỏ hơn6pt chỉ là điều kiện đủ trong phạm vi đã chứng minh; tổng không phải khoảng Word thực vẽ. Khoảng giữa hai hộp mực chữ có cả leading, KHÔNG so trực tiếp với6pt.
- C29: không dùng cỡ font point thay chiều cao Single; không đổi chỉ vì tên Exactly/AtLeast.
- Bước nền tảng đề xuất: tạo bản sao cô lập trung thực từ FULL Document.WordOpenXML/Flat OPC, giữ mọi XML/binary part và quan hệ (ảnh, theme, styles, numbering...). PageNumberPackage hiện chỉ tải các XML phục vụ PAGE, không dùng nó làm bộ clone đầy đủ.
- Mở bản sao bằng phiên Word riêng, tắt macro/link/field updates khi phù hợp; không đụng bản làm việc đang mở của người dùng.
- Trước hiệu chuẩn phải chứng minh XPS bản sao khớp nguồn: ngắt dòng, font, glyph, baseline, trang, hình/paint. Không khớp thì manual, không tiếp tục hiệu chuẩn.
- Với phạm vi hẹp (ít nhất2dòng, cùng trang, context/style rõ, không reflow), đo Single/1.5 cùng chữ/font, đối chiếu vector baseline. C28 có thể cần clone bỏ Before/After để tách tác động spacing. Phải giữ nguồn và cleanup/recovery khi COM lỗi.
- Đây là thiết kế cần triển khai/kiểm chứng, CHƯA có hạ tầng clone/hiệu chuẩn hoàn chỉnh trong nguồn hiện tại.

### B. Phạm vi bố cục còn thủ công

- Nhiều section, trang ngang, lề đối xứng/gutter/cột, header logo/nhiều thành phần, odd/even PAGE, PAGE restart/field lạ.
- Cần chính sách ngoại lệ theo loại văn bản, hậu kiểm từng section và nội dung chưa chọn. Không mặc định mọi trang ngang đều sai.
- Di chuyển dọc dòng số/ngày: hiện đo/cảnh báo khi đủ chứng cứ, chưa có writer dịch dọc an toàn.
- Khối ký ngoài bảng hoặc vai trò chưa rõ; danh sách tự động; bảng/field/control phức tạp vẫn cần mẫu và proof riêng. Track Changes/tài liệu bảo vệ ngoài hỗ trợ.

### C. Sau thể thức

- Lỗi chữ cơ bản: khoảng trắng, dấu câu, ngoặc, từ/cụm lặp với bộ ngoại lệ và test cảnh báo nhầm.
- Từ điển/ngữ cảnh cuối cùng, không suy “xẽ→sẽ” hoặc “giành/dành” đã hỗ trợ khi chưa có dữ liệu và quy tắc tương ứng.

## Bản đồ mã để đọc nhanh

- src/ChinhTa.Core/DevelopmentReport.cs: danh mục, milestone, report tiến độ; không biến nó thành chứng nhận tài liệu.
- BodySpacingRules.cs, BodySpacingVerification.cs, BodySpacingCoverage.cs, WordOutlineEvidence.cs: spacing và quyền sửa/context XML.
- WordSession*.cs: Word COM, checkpoint, luồng nhiều nhóm, hậu kiểm/Undo. Đọc các partial theo trách nhiệm, không cải tổ toàn bộ cùng lúc mở luật.
- WordSession.Page.cs, PageLayoutGuard.cs, EmptyHeaderFooterScope.cs, DocxPageLayoutEditor.cs, PageLayoutBodyPreviewVerifier.cs: C32.
- PageNumberRules.cs, PageNumberPackage.cs, DocxPageNumberEditor.cs, WordSession.PageNumber.cs, PageNumberPreviewVerifier.cs: C33; GroupNumerals dùng cho cả header/footer.
- PreviewPaintProof.cs, BitmapPageVisualProof.cs: paint/opacity/clip/bitmap evidence.
- DocumentCoverage*.cs, PageCoverageEvidence.cs, DocumentCoverageWindow*: phạm vi lần quét.
- OfficeFocusedCheck.cs:9nhóm độc lập; OfficeFooterPageMigrationCheck.cs: mẫu/Run2trang và từ10trang.
- OfficeIntegrationCheck.cs và Office*Check.cs: bài Word thật (CI chỉ biên dịch).
- tests/ChinhTa.Checks: Core/OpenXML; tests/ChinhTa.DesktopChecks: WPF/report/fixture.
- .github/workflows/windows-checks.yml: push feature/**/fix/** chạy checks. windows-package.yml: branch release/windows-VERSION hoặc tagvVERSION đóng gói/phát hành.

## Làm việc tiếp trên máy mới

Nếu có GitHub/git:

    git clone https://github.com/trung12685/KT_ChinhTaTV.git
    cd KT_ChinhTaTV
    git fetch origin feature/multidigit-page-batch20
    git switch -c continue-formatting-077 6180757eebdbf59b82b6435bb351cba68c712103

Trước push, đọc head hiện tại PR61/branch để tránh ghi đè công việc mới của người dùng. Tạo branch feature/... mới từ head đã đối chiếu hoặc dùng nhánh hiện có khi đã xác nhận lease; không force.

Nếu không có GitHub: dùng  trong ZIP, không cần token để đọc/chạy mã. Không giả vờ đã tạo PR/CI khi chưa có kết nối.

Lệnh kiểm chuẩn trên Windows, có .NET8SDK:

    dotnet run --project tests/ChinhTa.Checks -c Release
    dotnet run --project tests/ChinhTa.DesktopChecks -c Release

Linux chạy Core khi đủ NuGet/ref; không coi việc cross-compile net8.0-windows là đã chạy WPF. Các harness và SDK /workspace/.dotnet của máy cũ không có trong gói bàn giao. Dùng SDK/môi trường mới hoặc CI Windows.

Mỗi cụm: làm rõ phạm vi → tái hiện → sửa → test âm/dương/bảo toàn → đúng head WindowsCI → cập nhật docs/report/checklist khi cần. Kiểm hash tree/commit và PR đúng head. Giữ draft/unmerged nếu chưa được yêu cầu merge.

Đóng gói khi tích lũy5–10cụm: chuẩn bị CHANGES/SUPPORTED-FIXES/OFFICE-CHECK đúng version, báo cáo lấy số kiểm thực; chạy packageworkflow ở đúng commit, kiểm tag/CI/head/ZIPdigest và metadata. Không tái sử dụng tag đã phát hành. Hiện mới2cụm ở0.1.77.

## Bằng chứng và tính di động

- docs/handoff-evidence/number77-final-evidence.json: commit và CI0.1.77, các đường máy cũ chỉ để tham chiếu.
- docs/handoff-evidence/release76-final-evidence.json: thông tin bản0.1.76 đã phát hành.
- Các bằng chứng này được kiểm trong phiên làm việc cũ; trước thao tác ghi GitHub cần kiểm lại trạng thái hiện tại.
- GitHub commit/tag/CI là mốc đối chiếu; không coi đường /workspace ở máy cũ là file có thể mở trên máy mới.
