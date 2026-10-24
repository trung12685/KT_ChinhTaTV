# Kiểm định dạng theo vùng đã nhận diện

## Vấn đề chung

Luồng cũ có hai điểm gây cảnh báo giả tái hiện được bằng kiểm thử: coi mọi mẫu phông khác nhau là định dạng chưa đồng nhất (kể cả cỡ 13/14 cùng hợp lệ, đen RGB/đen tự động); và chỉ bật đọc từng ký tự ở một số vùng có Line của ứng dụng, khiến vùng chữ thuần có aggregate trộn vì dấu cách vẫn giữ thủ công. Hậu kiểm lượt chữ/phông dùng aggregate nhưng hậu kiểm sau Line dùng fallback, nên có thể kết luận khác nhau.

Đây là nguyên nhân trong code đã tái hiện, không phải khẳng định đã chứng minh toàn bộ nguyên nhân của log Office 2021 cũ. Cần chạy mẫu Word mới và tài liệu thực tế để khép phản hồi .56.

## Luồng mới

1. Đọc chữ/range/bố cục; đối chiếu XML hiện tại và quyền sửa trước khi đọc sâu.
2. Nhận diện cơ quan, tên loại/trích yếu, quốc hiệu/tiêu ngữ, số/ngày và nơi nhận. Dedupe Start, đọc mỗi vùng một lần; không đọc phông mọi đoạn căn giữa.
3. Định dạng toàn vùng đã rõ: dùng nhanh một lần. Chưa rõ: đọc Range thực của từng Word; chỉ xuống Characters ở từ chưa rõ. Words vượt biên hoặc không khớp toàn text thì bỏ kết quả đó và đọc lại Characters toàn vùng. Chuỗi con phải ghép chính xác về text nguồn; không suy tọa độ con từ chỉ số chữ. COM mất kết nối vẫn chuyển recovery, không biến thành bằng chứng phông đúng.
4. Gắn dữ liệu phông bất biến vào text hiện tại. Một FormatPolicy đánh giá theo vai trò: đúng, sai có thể sửa, chưa đọc chắc hoặc sai cỡ hỗn hợp cần duyệt. Màu đen tương đương và cỡ trong khoảng hợp lệ không tự thành lỗi. Đậm chỉ bắt buộc khi vai trò yêu cầu.
5. Sửa cả vùng khi có thể bảo toàn: nếu mọi cỡ hợp lệ thì bỏ qua setter Size; nếu có một cỡ đích chung an toàn thì đổi cỡ. Từ B01 sau .61, trộn cỡ sai/đúng trong CharacterFormat đã nhận diện có thể sửa riêng ký tự ngoài khoảng, giữ từng cỡ đúng. Quốc hiệu/tiêu ngữ/Đảng chưa opt-in; thiếu dữ liệu vẫn thủ công. Xem [thiết kế](SELECTIVE-SIZE-REPAIR.md).
6. Hậu kiểm trước/sau lưu và sau các nhóm khác dùng cùng bộ đọc, so từng phông theo chính sách. Với phép giữ cỡ, chụp từng cỡ ký tự Word trước sửa; chiếu qua sửa chữ rồi so lại cỡ ký tự giữ lại. Đen/phông đúng nhưng cỡ 14 bị ép về 13 vẫn thất bại và rollback cả lượt.

## Bảo toàn và giới hạn

- Không sửa chữ qua Line/field/control/object. FormatEditable và TextEditable vẫn tách riêng.
- Giữ ngắt dòng, dấu kết đoạn/ô, gạch chân và đậm không thuộc chính sách sửa. Đổi hoa tên cơ quan/tên loại bằng Range.Case khi chỉ đổi hoa/thường để giữ run; vẫn kiểm text thực tế và rollback nếu sai.
- Đổi hoa tên loại có cỡ hỗn hợp vẫn cần duyệt thay vì thay text cả vùng có thể mất cỡ. Các trường hợp đồng nhất cũ giữ sửa chung.
- Dữ liệu text cũ hoặc mất quyền XML không được dùng sửa; không dùng lịch sử để che cảnh báo.
- Không dùng số Shape/dấu nhận diện để chứng minh nét đúng; XPS và đo lại sau thay phông còn nguyên.
- Không tự mở rộng quá vùng đầu trang đã nhận diện; nơi nhận vẫn dùng proof toàn thân. Chưa xác nhận runtime Office 2021 cho .61.

## Kiểm thử

Core kiểm các vai trò, màu đen/cỡ hợp lệ trộn, sai thật, một glyph chưa đọc được, mất quyền/stale text, giữ cỡ, payload giả và thuộc tính đậm được giữ. Dùng ba Line thật của DocxLineEditor cùng dữ liệu phông để kiểm không sinh cảnh báo giả.

Desktop kiểm aggregate/Words/Characters, chỉ fallback từ cần thiết, Words vượt biên, COM lỗi/mất kết nối, thiếu ký tự, một glyph sai và cỡ hợp lệ bị ép về cỡ hợp lệ khác. Mẫu giả 2400 ký tự dùng 601 lượt đọc Font thay 1801 nếu đọc từng glyph: đây là kiểm số lời gọi bằng đối tượng giả, **không phải số đo tốc độ Office thực tế**. Tài liệu uniform vẫn một lượt Font. Lượt giữ cỡ phải đọc thêm Size để kiểm bảo toàn; không bỏ hậu kiểm để chạy nhanh.

OfficeFormatSystemCheck tạo mẫu giả hai cỡ hợp lệ, dấu cách/ngắt dòng khác kiểu; thử mẫu đúng trước/sau ba Line, mẫu sai thật sửa riêng/chung ba Line, giữ cỡ/gạch chân, quét/Undo/rollback/lưu-mở/băm nguồn. CI chỉ biên dịch bài Word. Cần chạy Kiểm tra Word tự động trên máy có Word và giữ log/TXT đúng phiên bản để xác nhận thực tế.
