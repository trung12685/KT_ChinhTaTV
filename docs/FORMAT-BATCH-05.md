# Đợt thể thức 05 — chứng minh nét duy nhất và giữ đủ nét cũ

Ngày 27/10/2026 (UTC+7). Nhánh phát triển tiếp từ v0.1.73; mã 0.1.74 chưa đóng gói. Danh mục vẫn 33 nhóm đề xuất có điều kiện / 8 thủ công / 1 minh họa. Giữ bộ đọc cũ, dừng tối ưu tốc độ.

## Các trường hợp đã tái hiện

Trên mã 0.1.73: hai nét XPS trùng nhau vẫn được Find lấy nét đầu tiên và coi là đạt; phép bù vị trí vẫn có thể đề xuất lại khi một nét cũ không còn; kiểm giữ nét độc lập có thể lấy một nét sống sót để chứng minh hai nét trước đó. Các ca này được tái hiện bằng harness gọi đúng assembly trước sửa, không suy ra từ tài liệu hoặc số lượng marker.

## Bốn phần sửa chung

1. Chỉ một nét khớp tọa độ mới chứng minh được một Line. Hai nét chồng hoặc cùng nằm trong dung sai giữ trạng thái chưa xác minh; không chọn nét đầu tiên/gần nhất.
2. Hậu kiểm giữ nét ghép từng yêu cầu với một lần xuất hiện riêng. Kiểm cả tập gồm nét phải giữ và Line đã sửa; một border có sẵn không được dùng đồng thời làm bằng chứng cho Line mới. Dùng phép ghép đầy đủ để không phụ thuộc thứ tự đọc XPS khi các khoảng dung sai giao nhau.
3. Phép bù vị trí từ chối ngay khi mất hoặc không ghép duy nhất một nét nền; không thử bù dựa trên phần còn sót. Lượt bù hợp lệ vẫn giữ một lần và kiểm XPS sau đó.
4. Quét và lịch sử sửa cùng nhận nét chồng là cảnh báo thủ công, không ẩn cảnh báo đã sửa hoặc cho tạo thêm Line. Nếu vị trí đề xuất đã có nét nhưng chưa ghép được với Shape trong vùng, cũng chưa cho tạo thêm. Hậu kiểm muộn thiếu nét gây phục hồi checkpoint cả lượt.

## Kiểm chứng

Có regression Core cho nét trùng/gần, thiếu nét nền, giữ đúng số lần xuất hiện, không dùng chung nét cũ/mới, ghép trong dung sai chồng theo cả hai thứ tự, nét lệch, số không hữu hạn và phục hồi khi lỗi muộn. Kiểm WPF tạo hai Path thật qua transform rồi đo/quét: nét chồng phải thủ công kể cả có lịch sử sửa; xóa nét thừa thì nét đơn hợp lệ trở lại.

Không đổi các kích thước/dung sai hình học hoặc luật thể thức. Không đổi bộ ghi DOCX, nhánh DrawingML/VML đã sửa ở 0.1.73, giấy/lề, PAGE, bảng/chữ hoặc đường dẫn Undo. Không đưa file người dùng vào repository. Kết quả Windows/Core/OpenXML/WPF của đúng commit nằm ở CI/PR. CI không chạy Word; chưa xác nhận toàn bộ bài Office thực qua 0.1.73 hoặc mã phát triển 0.1.74.

Đây là cụm hậu kiểm, chưa phát hành ZIP mới. Gói tải gần nhất vẫn v0.1.73; tiếp tục tích lũy các bước thể thức có ích trước đợt đóng gói tiếp theo theo nhịp đã thống nhất. Mọi tag/gói cũ và main giữ nguyên.
