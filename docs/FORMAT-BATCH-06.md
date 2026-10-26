# Đợt thể thức 06 — nét thực sự được vẽ trong bản xem trước

Ngày 27/10/2026 (UTC+7). Tiếp từ đợt05/PR50 tại 83ce495c; vẫn mã phát triển 0.1.74, chưa đóng gói. Danh mục 33 đề xuất có điều kiện / 8 thủ công / 1 minh họa, bộ đọc cũ và mốc tải 0.1.73 giữ nguyên.

## Lỗi tái hiện trên Windows

Commit a8cb7096 chỉ thêm kiểm WPF vào mã đợt05. CI 40384101625 đạt Core rồi thất bại ở phép đo nét: ancestor opacity=0, bút trong suốt, nét ngoài clip và đoạn chỉ Fill nhưng diện tích bằng0 đều bị đếm là1. Bộ đọc cũ chỉ lấy bounds mảnh, chưa chứng minh có phần được tô/vẽ. Log kiểm âm tính ghi đủ cả bốn số1; không phải lỗi biên dịch.

## Cụm sửa

- Xét Visibility, opacity và opacity mask trên Path lẫn các ancestor; không tính nét chắc chắn không được vẽ. Không dùng IsVisible phụ thuộc cửa sổ vì trang XPS được đo ngoài màn hình.
- Phân biệt stroke có độ dày/bút thật và fill có diện tích. Giữ biểu diễn hình chữ nhật mảnh của Word; Fill/Stroke hợp thành union, tôn trọng fill rule khi Path có nhiều figure để không lấp nhầm lỗ hoặc tạo nét từ các figure triệt tiêu nhau.
- Đưa phần vẽ và clip vào cùng tọa độ trang, xét clip của Path/ancestor, layout ClipToBounds và kích thước trang vật lý. Clip hình học dùng giao hình thật, không chỉ hình chữ nhật bounds; nét hoàn toàn ngoài vùng cắt không tính. Transform của Data và ancestor được áp đúng một lần.
- Nét bị cắt một phần, bút/mask chưa chứng minh được hoặc nét đứt giữ trạng thái chưa xác minh. Quét báo thủ công, kể cả có lịch sử sửa; không bỏ nét chưa rõ rồi suy là thiếu để thêm Line. API hậu kiểm nghiêm ngặt từ chối trạng thái này; lỗi sau mở candidate phục hồi checkpoint.

Giữ ngưỡng hình mảnh hiện có và dung sai Line, không nới để lấy PASS. Giao hình dùng độ chính xác0,001 DIP và epsilon diện tích1e-6 DIP² để tránh sai số Boolean sát biên; đây không phải dung sai vị trí/độ dài của thể thức. Không thay bộ ghi DOCX/Line, giấy/lề/PAGE, chữ/bảng, một Undo/rollback hoặc bộ đọc Word.

## Kiểm chứng

Có ca WPF cho Path và ancestor ẩn/trong suốt, bút/mask, độ dày0, fill rỗng và chữ nhật mảnh, nhiều figure/fill rule, clip đầy đủ/cắt một phần/nằm ngoài, clip tam giác/có lỗ, clip lồng nhau/ClipToBounds, Data/ancestor transform và trang vật lý. Kiểm tích hợp danh sách lỗi không tạo đề xuất khi nét chưa rõ, lịch sử sửa không che được và checkpoint được phục hồi khi hậu kiểm muộn từ chối. Đọc bằng chứng không sửa visual tree hoặc file XPS.

Giới hạn: bút gradient hoặc mask không đồng nhất chưa có chứng minh bao phủ toàn nét thì giữ thủ công; chưa thêm kiểm tương phản màu. CI là WPF và gói mô phỏng, không chạy Word; không xác nhận toàn bộ Office thực đã qua 0.1.73 hoặc mã phát triển 0.1.74. Số kiểm và commit đúng xem CI/PR.

Đây là cụm thứ hai sau gói0.1.73. Chưa tạo ZIP mới; tiếp tục tích lũy thể thức theo nhịp đã thống nhất. Giữ main và mọi tag/gói đã phát hành.
