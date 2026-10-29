# Đợt 23–26 — hiệu chuẩn và đo thử spacing

Tiếp tục mã 0.1.77 sau commit `3e3f05ca0d157c97e799c31fdc5cddcff171cacb`, cùng nhánh `feature/isolated-spacing-proof-batch22`. Không đổi main, tag, gói đã phát hành, bộ đọc mặc định hoặc khoảng ký tay80pt. Chưa có Word-runtime PASS; các kết quả mới chỉ chẩn đoán, không mở quyền sửa C28/C29.

## 23. Gói thử đầy đủ, gắn đúng nguồn

`IsolatedSpacingTrial` dùng anchor gồm chỉ số phần tử thân, nguyên văn đoạn (kể cả softbreak) và SHA256 full Flat OPC. Anchor cũ, trùng hoặc sai context không được dùng. Mỗi lần tạo bản thử đều kiểm toàn gói nguồn và xác minh độc lập rằng chỉ thuộc tính spacing cho phép đổi:

- Single: `lineRule=auto`, `line=240`.
- 1,5 dòng: `lineRule=auto`, `line=360`.
- Không khoảng ở ranh giới: đoạn đầu `after=0`, đoạn sau `before=0`.
- Mốc6pt cùng bối cảnh: đoạn đầu `after=120`, đoạn sau `before=0`.

Các khoảng ngoài ranh giới, lineRule không chọn, chữ/phông và mọi XML/nhị phân/quan hệ khác được giữ nguyên. Chèn pPr/spacing thiếu theo thứ tự schema. Phạm vi hẹp: đoạn trực tiếp của một section dọc thông thường; không bảng, field, hình trong đoạn, đánh số tự động, đơn vị dòng, auto/contextual spacing, lưới, frame/border, ngắt/giữ trang hoặc style chưa chứng minh. Không ghi bất cứ trial nào lên nguồn.

## 24. Đo vector baseline và tác động khoảng đoạn

`SpacingCalibration` yêu cầu clone chưa chỉnh khớp nguồn trước. Mỗi đoạn phải ghép duy nhất trọn glyph/dòng; không chọn chuỗi con hoặc suy Word CP thành chỉ số chữ. Trial giữ số/khổ trang, glyph/phông/shaping, tọa độ ngang và ngắt dòng. Phần trước giữ vị trí; phần sau cùng trang chỉ được dịch dọc đồng nhất; trang khác không đổi. Chặn dòng vượt trang, vượt ranh giới đoạn hoặc số đo không hữu hạn.

Giãn dòng cần ít nhất ba baseline cùng trang và hai vector Single/1,5 phân biệt. So từng bước, không lấy cỡ phông làm Single và không lấy trung bình che một dòng sai. Kết quả: dướiSingle, trong khoảng, trên1,5 hoặc chưa đủ bằng chứng.

Khoảng đoạn cần hai baseline ở mỗi đoạn liền nhau. Giữ nguyên vector bên trong từng đoạn, đo ranh giới trong nguồn, trial0 và trial6pt. So tác động nguồn với **tác động mốc6pt đã đo trong cùng bối cảnh**. Chênh lệch với trial0 không phải tổng khoảng trắng thực vẽ và không được so thẳng với literal6pt. Mốc6pt không tạo được khác biệt rõ thì chưa đủ bằng chứng.

WPF kiểm thêm paint/opacity/clip và khung glyph theo baseline tương ứng; ảnh/nét phải đứng yên. Hình có thể che vùng chữ đã chọn/dịch chuyển thì từ chối. Kiểm thứ tự vẽ cả bitmap có nội dung khác nhau, không chỉ bounding box. Cách xử lý bảo thủ này có thể từ chối tài liệu hợp lệ; không nới guard để tạo kết quả đạt.

## 25. Nguồn hiện tại và dọn dẹp phiên riêng

Khi xuất bản xem trước thông thường, phiên làm việc gắn đường XPS với full XML sau xuất. Lệnh đo chỉ đọc bản sao riêng của XPS đó; không gọi Export/Save lên tài liệu người dùng. So full XML trước/sau và không dùng cache cũ sau thay đổi nguồn. Bản sao XPS riêng tránh xung đột PackageStore với cửa sổ xem trước đang mở.

Phiên Word mới mở tối đa năm DOCX (clone và bốn trial) chỉ đọc, tắt macro/cập nhật liên kết/field khi in, không save hoặc tạo Undo. Tất cả bản thử xuất phát từ cùng nguồn, không nối sửa trial trước thành nguồn trial sau. Chỉ kết xuất trial sau khi clone ban đầu khớp nguồn.

`IsolatedSessionCleanup` thử riêng từng Close, Release, khôi phục option và Quit kể cả khi trước đó lỗi. Giữ lỗi ban đầu cùng các lỗi cleanup, không ghi đè bởi lỗi cuối. Chỉ xóa thư mục tạm khi mọi thao tác đóng/khôi phục thành công; nếu chưa chắc phiên đã đóng thì giữ bản phục hồi. Không kill Word của người dùng. Đo khoảng đoạn không đủ điều kiện được báo riêng, không làm mất kết quả dòng đủ bằng chứng.

## 26. Cửa sổ đo theo yêu cầu và bài Word độc lập

Menu **Kiểm tra → Đo thử giãn dòng và khoảng đoạn…** cho chọn đoạn nội dung từ lần quét. Mở cửa sổ không gọi Word; người dùng bấm đo mới tạo các trial. Kết quả có vector baseline theo point và hai tác động khoảng đoạn so với trial0; luôn nói rõ chưa sửa hoặc chứng nhận toàn tài liệu. Đổi đoạn/lỗi xóa kết quả cũ. Chặn chạy lặp/đóng khi đang đo, phục hồi điều khiển sau lỗi. Nguyên văn lựa chọn chỉ hiện cục bộ trong hộp chọn, không nằm trong báo cáo/exception được hiện hoặc log mới.

Menu nhóm kiểm Word thêm **Đo Single/1,5 và khoảng đoạn trên bản sao**, tổng11nhóm. Mẫu Exactly/AtLeast nhiều dòng kiểm hai lượt cùng kết quả, nguồn/hash/Undo/trạng thái không đổi. Đây là kịch bản đã viết và biên dịch; CI Windows chạy WPF nhưng không chạy Microsoft Word.

## Kiểm chứng và việc tiếp theo

Các runner chuẩn:

```powershell
dotnet run --project tests/ChinhTa.Checks -c Release
dotnet run --project tests/ChinhTa.DesktopChecks -c Release
```

Log và số kiểm thực tế được ghi trong biên bản tiếp tục; không cộng số assertion từ harness phụ vào runner chính. Các ca mới bao phủ bảo toàn toàn gói/schema, context/style/stale, math dương/âm, reflow/phông/clip/paint/ảnh/nét, thứ tự vẽ, lỗi đóng/khôi phục kép và UI không lộ chữ/không giữ kết quả cũ.

Kết quả cuối trên mã triển khai: **5.143 assertion Core/OpenXML đạt trên Linux**, tăng155 so với4.988 của đợt22; build DesktopChecks/Desktop Release không cảnh báo hoặc lỗi. **Windows CI đạt**, [run40915207524](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40915207524), [job123593485346](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40915207524/job/123593485346): Core/OpenXML, WPF/build và ghi evidence đều `success`. Đã đối chiếu đúng commit `2eabc4d`; số assertion Windows không suy từ Linux hoặc baseline. Xem [bằng chứng đợt23–26](handoff-evidence/batch23-26-evidence.json).

Bước còn cần Word thật: clone nguồn khớp trên Office đang dùng; các mức Single/1,5, trial0/6pt không reflow; font subset/PAGE ổn định; dọn dẹp khi COM lỗi; nguồn/Undo không đổi. Sau đó mới thiết kế writer Exactly/AtLeast và khoảng đoạn gộp với sửa riêng/chung, hậu kiểm, lỗi giữa/cuối lượt, một Undo và lưu/mở lại. Chưa có quyền sửa hoặc kết quảWordPASS mới ở đợt này.
