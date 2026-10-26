# Thử nghiệm đọc DOCX 0.1.68

Mốc quay lại: **v0.1.67**, commit `784a41ae9db8dcad02b9cccd69ab8836b9a56dae`. Bản thử theo yêu cầu người dùng ngày 26/10/2026 (giờ Việt Nam), sau khi đã lưu ZIP/source/tag .67. Không nhập main và không ghi đè gói/tag .67.

Phạm vi đã chuyển từ nghiên cứu sang triển khai thử theo tin nhắn mới của người dùng: “Bây giờ bạn xử lý theo thuật toán của code trên xem nào.” Việc thay code, kiểm thử và tạo nhánh thử riêng nhằm thực hiện yêu cầu này; các yêu cầu trước đây chỉ nghiên cứu áp dụng cho lượt phân tích trước. Phát hành/gói thử chỉ sau các kiểm bắt buộc đạt; không sửa mốc .67.

## Thuật toán được áp dụng

1. Tạo một snapshot từ **WordOpenXML hiện tại trong bộ nhớ Word**, dùng chung cho đối chiếu chữ, đánh số, nơi nhận và bản đồ bảng. Lập danh sách đoạn và chuỗi chữ một lần; gồm các đoạn trong bảng. Thông tin vùng ô, độ phức tạp bảng và lề ô được dùng lại trong cùng lượt đối chiếu. Không lấy DOCX nguồn cũ để thay các sửa chưa lưu.
2. Vùng định dạng trộn: thử chia theo các run XML đã đối chiếu. Mỗi vùng vẫn đọc **Font của Word thật**, kiểm Start/End/Text thật và toàn bộ chữ vùng cha. Không suy đoán phông/cỡ từ XML hoặc coi thuộc tính kế thừa là lỗi. Chỉ dùng kết quả nếu mọi vùng có dữ liệu chắc chắn; thất bại một vùng thì bỏ toàn bộ kết quả tối ưu và dùng bộ Words/Characters hiện có. Lỗi mất kết nối COM vẫn đi qua cơ chế phục hồi hiện tại.
3. Run plan chỉ sống trong lần CheckCore hiện tại, chỉ cho đọc vùng đã có quyền; không dùng để mở rộng quyền sửa. Ký tự ngoài BMP, field/control/link, ngắt dòng, CP không khớp hoặc hơn64 run giữ bộ đọc cũ. Line của ứng dụng chỉ được bỏ khỏi phép đọc chữ khi đã qua OwnedLineFormatProof; marker vẫn không chứng minh nét render đúng.
4. Thêm số đo tách chặng mở Word, mở tài liệu, phân trang, đọc đoạn/XML, đối chiếu, quan sát định dạng, luật, gắn vị trí, xuất XPS và cập nhật danh sách. Log chỉ chứa tên chặng cố định, số đếm và thời gian; không ghi nội dung hay tên tài liệu.

Giữ nguyên thuật toán luật, full Preview Word/XPS, dữ liệu trong bảng, phân trang, kiểm nét thực tế, checkpoint, hậu kiểm, một Undo và rollback. Chưa áp dụng đọc preview chữ thuần, bỏ Word, sửa cache xuyên các lượt thay đổi hoặc giảm hậu kiểm. Luồng Word vẫn trên STA hiện tại, chưa chuyển đọc/quét sang nền.

## So sánh trên cùng ứng dụng

Menu **Kiểm tra → Thử nghiệm đọc dữ liệu nhanh** mặc định bật. Bỏ tích dùng lối đọc chuỗi XML riêng và Words/Characters; bật lại dùng snapshot chung và run khi đủ chứng cứ. Mỗi lần đổi chế độ sẽ quét lại đầy đủ và dựng lại Preview. Danh sách lỗi/các lựa chọn cũ được thay bằng kết quả mới, không giữ vị trí đo từ lượt trước.

1. Dùng bản sao cùng một DOCX, giữ nguồn và ZIP .67 riêng. Xác nhận tiêu đề0.1.68.
2. Đo riêng lượt mở đầu; sau đó thử khoảng5 lượt ở mỗi chế độ, xen kẽ thứ tự để giảm thiên lệch cache. Chưa sửa nội dung khi so các lượt quét không thay đổi.
3. So thời gian Quét/dựng trang trên trạng thái; dùng menu Mở thư mục chẩn đoán để lấy `diagnostics.log`. Ghép các dòng `Word.Perf.Open`, `Word.Perf.Connect`, `Word.Perf.Check`, `Word.Perf.Preview`, `Word.Perf.Scan`. `Optimized=1/0` phân biệt chế độ; `FormatRuns/RunSuccesses/RunFallbacks/FormatWords/FormatCharacters` cho biết có giảm đọc nhỏ thật không.
4. Đối chiếu cùng danh sách lỗi/quyền sửa, đầy đủ chữ ở từng ô bảng, cơ quan, trích yếu nhiều đoạn/ngắt dòng, nơi nhận, số trang và nét kẻ. Thử sửa riêng/nhóm, quét lại, lưu/mở và một Undo trên các bản sao giống nhau.
5. So thêm bản .67 riêng nếu cần đối chiếu nguyên bản trước thử nghiệm; chế độ cũ trong .68 vẫn có phép đo mới. Mốc .67 giữ source/binary hoàn chỉnh.

Không kết luận nhanh toàn bộ từ riêng số đo XML. Thời gian mở Word/COM/phân trang/XPS có thể chiếm phần lớn và chưa được rút ngắn trong thử nghiệm này. Nếu kết quả sai hoặc chậm hơn trên tài liệu cụ thể, tắt tùy chọn hoặc chạy lại thư mục .67; giữ log và file thử. Quay lại ứng dụng không tự hoàn tác file DOCX đã lưu, cần giữ bản nguồn.

## Kiểm chứng tự động

- So quyền sửa chữ/phông, outline/nơi nhận, cảnh báo và geometry-prefix giữa hai cách đọc trên đoạn thường, bảng, bảng lồng/gộp, thiếu/thừa đoạn, sai prefix, field/control/link/drawing và hộp chữ riêng.
- Run reader: dữ liệu Word giả lập có định dạng trộn/kế thừa; cùng tập phông quan sát, ít lần đọc hơn; sai chữ/Start/End, ký tự/CP đặc biệt, unknown và COM lỗi phải quay lại hoặc phục hồi, không chấp nhận bằng chứng một phần.
- Line thật do DocxLineEditor tạo: snapshot giữ quyền format, giữ đóng quyền sửa chữ; vẫn xác minh nét XPS qua luồng hiện tại.
- `dotnet run --project tests/ChinhTa.Checks -c Release -- --benchmark-readers` đo riêng XML/proof/outline/40 đoạn geometry. Chín lượt sau ba warmup, xen kẽ thứ tự, kiểm mọi đầu ra giống nhau. Có tính chi phí serialize chữ ký kết quả như nhau ở hai phía; không đo Word, COM, XPS, giao diện hoặc sửa/lưu.
- Bài Kiểm tra Word tự động có thêm fixture so hai cách đọc trên thành phần đúng/sai, sau chèn ba Line và sau Undo; so danh sách/payload/mốc/số đo XPS và giữ checkpoint/Changed/Undo. **CI chỉ biên dịch fixture này; chưa phải bằng chứng chạy đạt trên Office2021.**

Kết quả thực tế theo đúng commit nằm trong release, build-checks.json và các artifact/log CI. Không khép phản hồi Office2021 cũ chỉ bằng số kiểm tự động.
