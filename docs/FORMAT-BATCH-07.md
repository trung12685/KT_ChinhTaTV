# Đợt thể thức07 — dòng tham chiếu phải hiển thị đầy đủ

Ngày 27/10/2026 (UTC+7). Tiếp từ đợt06/PR51 tại c024c93d; mã phát triển0.1.74, cụm thứ ba sau gói0.1.73. Giữ danh mục33/8/1, bộ đọc cũ mặc định và mọi tag/gói đã phát hành.

## Lỗi tái hiện

Commit ededed2a chỉ thêm kiểm WPF vào mã đợt06. CI 40388239277 đạt Core rồi thất bại vì cả sáu trường hợp vẫn có số đo: chữ dưới ancestor opacity0, fill trong suốt, chữ bị clip một phần, chữ ngoài trang, chữ xoay và dòng đơn bị opacity0. ReferenceMeasurement dùng đường Measure cho bounding highlight nên chưa áp các kiểm clip/hướng của MeasureSingleLine; cả hai còn chưa xét khả năng vẽ của glyph.

## Cụm sửa

- Phép đo dùng làm tham chiếu và dòng số/ngày đều yêu cầu fill được chứng minh, Visibility/opacity/mask của Glyphs và ancestor hợp lệ. Nếu một run được chọn không hiện đủ thì không trả các mảnh còn lại thành một tham chiếu ngắn hơn.
- Dùng chung proof paint/transform/clip của đợt06 cho chữ và nét, giữ nguyên xử lý nét đã kiểm trước. Xét clip hình học thật, clip layout/trang và phần glyph nhô ngoài advance box; không chỉ nhìn vào bounds để kết luận chữ hiện đủ.
- Chỉ cho hình học tự sửa khi các trục tọa độ hữu hạn, dương và thẳng hàng theo kiểm có sẵn; chữ xoay, skew hoặc mirror không được đưa vào phép đặt Line/căn dòng. TableFrame cũng cần tham chiếu đã xác minh trước khi trả khung tọa độ.
- Bounding highlight để xem vị trí vẫn dùng API Measure hiện có. MeasureReference riêng phục vụ quyền tự sửa; ReferenceMeasurement và MeasureSingleLine dùng đường này. Quét vẫn báo thủ công khi mất mốc; lịch sử sửa không che được, hậu kiểm sau mở candidate thất bại thì phục hồi checkpoint.

Không sửa CP/chuỗi gốc, không bỏ glyph ẩn khỏi chỉ mục để đoán vị trí và không thay cách gom dòng cuối hiện có. Không đổi dung sai thể thức/Line, bộ ghi DOCX, chữ/bảng, giấy/lề/PAGE, một Undo/rollback hoặc bộ đọc Word. Phần proof clip được tách dùng chung, vẫn giữ độ chính xác/epsilon đã kiểm ở đợt06.

## Kiểm chứng và giới hạn

Có kiểm WPF âm tính trước sửa và ca sau sửa cho ẩn/trong suốt/mask, clip glyph/ancestor/layout/có lỗ, ngoài trang, trục xoay/skew/mirror, giữ tọa độ qua transform, clip chứa trọn và mức opacity đồng nhất khác0. Có ca nhiều run với phần cuối bị ẩn, glyph ẩn không được chọn ở ngoài vùng, khung tọa độ, chữ nghiêng nhô khỏi advance box, UI/hậu kiểm/lịch sử sửa và phục hồi cả checkpoint. Các kiểm nét của đợt06 và phối hợp trước vẫn phải đạt.

Để kiểm phần mực nhô của một vùng chọn nằm trong một GlyphRun lớn, hiện yêu cầu toàn run còn trong vùng hiển thị; không đoán mực từng ký tự ở biên cluster. Đây là điều kiện bảo thủ và có thể chuyển thủ công. Không thêm kiểm tương phản màu hoặc hỗ trợ bút/mask không đồng nhất. CI không chạy Word thực; số kiểm/commit đúng xem CI/PR, không suy ra toàn bộ Office đã đạt.

Chưa đóng gói ZIP mới. Sau ba cụm tiếp tục tích lũy theo nhịp5–10 bước đã thống nhất; gói tải gần nhất vẫn0.1.73.
