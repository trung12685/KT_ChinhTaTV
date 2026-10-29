# Đợt 48–51 — dẫn chiếu, literal, đệm biểu mẫu và đóng tài liệu

Ngày 30/10/2026 (UTC+7), tiếp tục từ `f45c19c179e62c7ab96cee62efaeeab464b42698` trên nhánh `feature/isolated-spacing-proof-batch22`. Mã tiếp tục tích lũy **0.1.79** sau đợt 45–47, chưa xuất ZIP mới. Giữ main, tag/gói cũ, bộ đọc thử nghiệm OFF, khoảng ký tay 80 pt và quyền sửa Exactly/AtLeast.

## 48. Dẫn chiếu theo khoảng và nhãn có ngoặc riêng

Tái hiện trước sửa: chuỗi 1/2/9 có “Xem mục 4 đến 10.” vẫn đề xuất 9 → 3 vì bộ bảo vệ chỉ giữ hai đầu 4/10. Tương tự 1/2/4 có “Xem mục (8), (4).” còn bỏ sót nhãn sau trong ngoặc.

`OutlineRules` giữ từng nhãn của danh sách, cho phép ngoặc riêng và giữ quan hệ khoảng số tường minh với `đến/tới/-/–/—`. Khoảng cùng cấp/cùng tiền tố bảo vệ cả nhãn cũ, dự kiến hoặc cây con nằm bên trong. So sánh các thành phần số bằng độ dài/thứ tự chuỗi sau bỏ đệm 0, không parse số nguyên hoặc đổi nhãn/CP nguồn. Dấu phẩy/`và` vẫn là danh sách, không suy toàn khoảng giữa các phần tử.

Khoảng số khác tiền tố/độ sâu không đủ chứng minh nội dung bên trong nên giữ cây số chung để duyệt; nếu không có tiền tố chung thì phạm vi giữ thủ công rộng hơn. Không suy khoảng La Mã/chữ cái. Dạng lặp cue “từ mục 4 đến mục 10” và parser dẫn chiếu pháp lý tổng quát vẫn ngoài phạm vi. Lựa chọn cũ bị chặn khi xuất hiện dẫn chiếu mới dù nhãn/CP cần sửa chưa đổi.

Bài Word số nhiều cấp bổ sung sáu biến thể dẫn chiếu đơn/danh sách/khoảng, gồm Unicode tổ hợp và số có đệm 0. Mỗi mẫu phải giữ nhóm 1.4 thủ công nhưng vẫn cho duyệt nhóm 2.4 độc lập; quét không làm đổi bản làm việc, nguồn hoặc Undo. Kiểm schema/part và mô hình lựa chọn chạy trong runner Desktop; chưa phải Word thực.

## 49. Chuỗi literal có tiền tố và đối số sau

Tái hiện trước sửa: `Console.WriteLine(@"Báo  cáo")`, `print(r"Báo  cáo")`, `print(f"kế hoạch kế hoạch")` hoặc literal ở đối số sau một số/tên biến còn có đề xuất gộp khoảng/xóa từ lặp.

`TechnicalTextContext` nhận prefix C# `@/$/$@/@$`, Python `r/f/b/u/br/rb/fr/rf` khi có ranh giới lời gọi `identifier(` liền nhau. Đối số trước literal chỉ được nhận là tên đơn/qualified hoặc số thập phân ngăn bằng dấu phẩy. Gợi ý sửa chữ của cả đoạn giữ thủ công theo cơ chế cũ, không xóa dữ liệu literal. Dấu nháy hoặc ngoặc có khoảng cách trong văn xuôi không tự tạo phạm vi kỹ thuật; literal không có gợi ý sửa cũng không tự sinh cảnh báo.

Đây là nhận diện một số dạng rõ, chưa phân tích biểu thức lồng/mọi ngôn ngữ. Không chuẩn hóa nguồn Unicode. Bài Word sửa chữ thêm các literal này và giữ yêu cầu sửa riêng/chung, lỗi muộn/rollback, một Undo, lưu/mở và nguồn bất biến đã có.

## 50. Biểu mẫu trộn khoảng không ngắt dòng

Tái hiện trước sửa: `Ngày sinh:     /    /____`, `Số:     ...    /BC-ABC` hoặc `ngày     tháng    năm    ` mất nhận diện vì NBSP/NNBSP, khiến C01 còn gộp ASCII padding trong ô trống khác của cùng trường.

`SpacingRules` nhận thêm đúng U+00A0/U+202F trong grammar ô số/ngày/ô chọn đã nhận diện; giữ nguyên các ký tự và CP, không thay bằng ASCII space. Chỉ phần blank/placeholder được miễn, phần đã điền và khoảng ngoài ô vẫn cần duyệt. Nhãn sai, giá trị lỗi, control dọc/ngắt trang và mọi Unicode whitespace khác không tự được miễn.

Không đổi parser hoặc writer header: giá trị NBSP chưa được grammar sửa cũ hỗ trợ vẫn thủ công. Kiểm phối hợp với header đã nhận diện để không có đường C01/chuẩn hóa khác viết đè ô chưa điền. Fixture Word sửa chữ bổ sung hai mẫu số/ngày nói trên.

## 51. Đóng tài liệu không để trạng thái phiên giả còn mở

Tái hiện bằng managed faults trước sửa: `CloseDocumentOnly` bỏ qua release nếu Close ném lỗi; nếu release ném lỗi sau Close thì field `document` vẫn còn. `IsOpen` có thể tiếp tục true cho document đã đóng, ảnh hưởng Undo và các lượt đổi checkpoint.

Dùng chung đường đóng tài liệu nghiêm ngặt với Open: tách field trước khi thử Close/release, thử hai bước độc lập và giữ đủ nguyên nhân. Giữ ownership application riêng cho caller. Không dùng lại document đã tách; cleanup lặp không đóng/release lại RCW cũ.

Kiểm gọi Undo sản xuất với Close lỗi, giữ nguyên nguồn, dirty state, byte bản làm việc/checkpoint, stack Undo và điều kiện mở phục hồi; mở lại bản được giữ không tiêu thụ Undo. Kiểm callback release lỗi riêng/chung, giữ application tới Dispose. Managed fakes/harness không xác nhận hành vi COM Word thực.

## Kiểm chứng

Core/OpenXML trên Linux: **6703 kiểm qua**, tăng **554** từ mốc 6149. Build DesktopChecks Release: **0 warning/error**. Các harness cô lập đã qua 124 kiểm mới về dẫn chiếu, 262 về literal, 168 về NBSP/NNBSP và 23 về đóng tài liệu. Các số Core đã nằm trong runner tổng, không cộng lại harness thành tổng khác; số WPF lấy từ evidence Windows.

Lượt Windows đầu phát hiện lỗi schema trong fixture cũ: `OfficeComponentCheck`/`OfficeHierarchicalOutlineCheck` tạo `w:sz` trước `w:b` trong `w:rPr`. Đã sửa thứ tự thuộc tính ngay nơi tạo mẫu, không nới validator hoặc thay giá trị định dạng. Các kiểm mô hình giữ dẫn chiếu/lựa chọn đều qua trong harness cô lập. Runner Desktop bổ sung annotation lỗi chưa xử lý khi chạy GitHub Actions để giữ nguyên exit code và hiện rõ nguyên nhân từ mẫu tổng hợp.

[Windows CI cuối đợt](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40957803539) của commit `346301d45d05c763827a0eeb3a7dba6685bc08df` đã **Success**. [Job kiểm tra](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40957803539/job/123742436269) qua Core/OpenXML, WPF/build, ghi evidence và tải log. Harness fixture tạo lại từ mã đã sửa qua **41/41** kiểm (30 dẫn chiếu + 11 kỹ thuật), không còn lỗi schema; không cộng số harness này vào tổng CI. Tổng Core/WPF Windows nằm trong `check-evidence.json` của artifact kiểm tra. Workflow không đóng gói ứng dụng hoặc chạy Microsoft Word.

Word thực vẫn **NOT_RUN**, tổng 12 nhóm kiểm Word giữ nguyên. Không xuất ZIP 0.1.79 hoặc thay gói 0.1.78 trong đợt này.
