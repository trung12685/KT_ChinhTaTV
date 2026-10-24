# Sửa riêng cỡ sai, giữ từng cỡ đúng

## Phạm vi

B01 sau .61, code .62 chưa đóng gói. Áp dụng cho CharacterFormat đã có nhận diện và proof XML: tên cơ quan/chủ quản 12–13; tên loại/trích yếu/ngày 13–14. Nơi nhận và số có cỡ cố định vẫn dùng phép sửa đã có. Không mở nhận diện thân bài/chữ ký, header/footer/textbox hoặc quyền thay chữ qua đối tượng. Từ B02, quốc hiệu/tiêu ngữ dùng HeadingFormat có thể sửa riêng cỡ sai khi nội dung đã khớp đúng và giữ nguyên chữ: quốc hiệu12–13, tiêu ngữ13–14. Giữ HÒA/HOÀ và hyphen/en dash được chấp nhận. Tiêu đề Đảng cỡ15 cố định vẫn dùng route cũ; không dùng khoảng cỡ Nhà nước.

| Ví dụ | Khi duyệt sửa |
| --- | --- |
| Cơ quan có ký tự cỡ 10, 12, 13 | Chỉ đưa 10 về 12; giữ từng ký tự 12/13 |
| Trích yếu có ký tự cỡ 12, 13, 14 và Arial đỏ | Chỉ đưa 12 về 13; giữ 13/14, sửa phông/màu/kiểu theo vai trò |
| Ngày có ký tự cỡ 12 và 14 | Đưa 12 về 13, giữ 14 và ô trống; chữ nghiêng, giữ đậm/gạch chân |
| Quốc hiệu đúng chữ có cỡ10/12/13 | Chỉ đưa10 về12; giữ12/13, có thể căn giữa trong ô theo lựa chọn hiện có |
| Tiêu ngữ đúng chữ có cỡ12/13/14 | Chỉ đưa12 về13; giữ13/14 và dấu nối; đo lại Line nếu chọn kèm |
| Một ký tự chưa đọc chắc hoặc mất proof XML | Chưa cho sửa, không suy từ ký tự đầu |

## Một mục duyệt cho cả vùng

Không tạo nhiều Issue theo từng run/ký tự. Các mảnh sẽ cắt qua phép viết hoa tên cơ quan hoặc xóa cụm lặp, gây chồng lấn và khó hậu kiểm sau đổi CP. Thay vào đó CharacterFormat có `RepairInvalidSizes=true`, chỉ hợp lệ cùng `PreserveSize` ở khoảng cỡ thay đổi và đích bằng cận dưới đã có. FormatPolicy opt-in ở đúng đường sửa hỗ trợ; các caller khác vẫn nhận NeedsReview. B02 thêm cờ tương ứng vào HeadingFormat, BatchPlan chỉ chấp nhận quốc hiệu12–13 hoặc tiêu ngữ13–14 và Original==Replacement. Thiếu khoảng cỡ, nhầm vai trò hoặc thay chữ đều bị chặn. Hai route dùng chung writer Size và proof cỡ, không tạo cơ chế ghi khác. Khi các cỡ sai/đúng có một đích chung bảo toàn được, vẫn dùng fast path sửa cỡ cũ.

Trước ghi, quét lại điều kiện/quyền sửa như mọi đề xuất. Chụp cỡ từng ký tự nguồn; tạo bản kỳ vọng bằng cách chỉ thay cỡ ngoài khoảng. Trong lượt sửa phông, không set Size toàn vùng ở nhánh này. Đọc các Range ký tự thực của Word, đối chiếu toàn chuỗi và các biên liên tiếp nằm trọn trong vùng đã duyệt rồi mới ghi Size vào các Range sai cỡ. Không lấy chỉ số UTF-16 để dựng CP ký tự, không ghi cỡ dấu cách/ngắt dòng. Không đọc Name/AlternativeText/ID của COM Shape.

Chiếu bản kỳ vọng qua các sửa chữ được chọn, gồm hoa/thường, dấu cách và phần lặp. Hậu kiểm sau lưu và sau các nhóm căn/Line kiểm cả chính sách font và cỡ chính xác từng ký tự còn lại. Một ký tự đúng 14 bị ép về 13 vẫn thất bại dù cả hai nằm trong khoảng hợp lệ; ký tự sai phải về đúng cỡ đề xuất. Ký tự mới do sửa chữ chưa có cỡ nguồn vẫn phải qua kiểm font/cỡ theo vai trò. Sửa chữ/phông xong đo lại XPS trước Line; không dùng geometry trước sửa cỡ. Lỗi đọc/ghi/hậu kiểm vào rollback toàn lượt và không báo hoàn tất.

## Hiệu năng và kiểm chứng

Chỉ nhánh cần sửa cỡ hỗn hợp mới đọc/ghi Size từng ký tự; các trường hợp uniform, mọi cỡ đã hợp lệ và cùng một đích an toàn giữ đường cũ. Không thêm lần xuất XPS; không cache bằng chứng qua sửa. Đây là giới hạn số thao tác theo thiết kế, chưa có số đo tốc độ Word thực tế.

Core kiểm vai trò, ngoại lệ, payload giả, nguyên văn, cỡ kỳ vọng qua sửa chữ, stale, hậu kiểm cỡ vẫn hợp lệ nhưng sai và rollback. Desktop kiểm Range thật/CP khác UTF-16, Unicode tổ hợp/emoji, dấu trắng/ngắt dòng, thiếu/vượt biên/không khớp ký tự, COM read/write failure, lựa chọn phụ thuộc và đo glyph/nét mới. OfficeFormatSystemCheck có thêm mẫu cỡ sai/đúng, sửa riêng/chung ba Line, một Undo, rollback sau Line, lưu/mở, gạch chân và băm nguồn. CI chỉ biên dịch bài Word; không coi là đã chạy Office 2021, không khép phản hồi phông .56 bằng CI.

B02 có thêm Core kiểm vai trò quốc hiệu/tiêu ngữ, các cách HÒA/HOÀ và dấu nối, proof Line thật, stale/quyền XML và hậu kiểm drift; Desktop dùng cùng writer, lỗi COM và glyph tiêu ngữ đo lại. OfficeHeadingSizeCheck tạo mẫu từ tài liệu giả đã có ba Line, giữ drawing khi Word gộp run, sửa riêng/nhóm/rollback giữa lượt, một Undo/lưu-mở/băm nguồn. Mốc cỡ đọc lại từ paragraph hiện tại đã đối chiếu XML, không dùng CP trước mở lại. Bài Word chỉ được biên dịch trong CI.
