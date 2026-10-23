# 0.1.54 — Đúng vai trò cơ quan, trích yếu và nơi nhận

Đối chiếu Nghị định 30/2020/NĐ-CP, Phụ lục I, Phần I.I.4, II.2.b, II.5.b và II.9.d/ô 9b. Đây là Nghị định; các yêu cầu đang dùng không thuộc một “Thông tư 30”. Không đưa tài liệu riêng của người dùng vào mã nguồn hoặc gói phát hành.

## Trích yếu

Khối trích yếu có thể nhiều đoạn hoặc ngắt dòng để giữ cụm từ liền mạch. Giữ nguyên ngắt, chữ và cách chia đoạn; bỏ giới hạn bốn đoạn vốn là giới hạn ứng dụng, không phải quy định. Dòng Kính gửi có thể căn giữa nhưng vẫn là ranh giới thân bài, không làm khối trích yếu thành chưa rõ. Khoảng trắng cuối đoạn không tự trở thành lỗi định dạng của toàn khối.

Sửa phông chỉ khi có bằng chứng XML/range và quan sát chữ rõ; thêm bằng chứng FormatEditable riêng cho textWrapping break/cr. Không mở quyền thay chữ chung qua break; tab, ngắt trang/cột, field/link/control/object vẫn bị bảo vệ. Kiểm tâm/nét Line vẫn dùng quy tắc chặt và XPS thực tế.

## Tên cơ quan — C22

Nhận diện một/hai đoạn tên trên dòng Số trong ô trái khối đầu đã xác minh. Cơ quan chủ quản: in hoa, TNR đen, cỡ 12–13, đứng; cơ quan ban hành: in hoa, TNR đen, cỡ 12–13, đứng đậm. Đúng các thuộc tính đã kiểm thì không báo. Khi sai, đề xuất sửa phông hoặc viết hoa từng từ; giữ tên, mã, căn đoạn và cỡ trong khoảng hợp lệ. Chưa xác minh tên chính thức, mọi cấp/khối tên nhiều đoạn hoặc mọi mẫu ngoài bảng.

## Nơi nhận 9b — C23

Nhãn Nơi nhận: cỡ 12, nghiêng, đậm; danh sách cỡ 11, đứng; có gạch đầu dòng, cuối các dòng chấm phẩy, dòng Lưu cuối kết thúc bằng chấm. Tách danh sách này khỏi quy tắc thứ tự mục/dấu đơn của thân bài. Chỉ nhận vùng có nhãn và dòng Lưu cùng container XML, không miễn trừ mọi gạch trong bảng.

Sai định dạng/dấu đầu-cuối dòng thì có gợi ý riêng, có thể chọn cùng phông; không thay tên đơn vị nhận hay tự điền số bản lưu. Danh sách tự động, XML/vùng không chắc vẫn giữ bảo vệ. Chưa đo đủ tọa độ sát lề/ngang hàng với chức vụ hoặc xác minh mọi thành phần của dòng Lưu.

## Thông tin phạm vi kiểm tra

Thông báo chưa đối chiếu đủ XML hiển thị riêng ở vùng thông tin, không neo vào dòng đầu tài liệu và không cộng vào danh sách lỗi của dòng BỘ CÔNG AN. Vẫn nêu rõ phần chưa kiểm đủ khi thật sự mất bằng chứng. Không đổi một giới hạn kỹ thuật thành lỗi thể thức, cũng không tuyên bố tài liệu đã đạt toàn bộ khi thiếu phạm vi kiểm tra.

## Kiểm chứng và cách thử

Bộ Core kiểm mẫu đúng không báo, mẫu sai có đề xuất, recipient không thành outline, gạch thân thật vẫn được kiểm; sửa phông giữ ngắt dòng, không mở qua tab/ngắt trang/cột/đối tượng, kiểm chặt role payload. Đã phân tích DOCX riêng bằng XML và CP/trang mô phỏng: không còn ba nhóm báo nhầm trên; vẫn phát hiện một thứ tự sai và một dấu đơn trong thân bài, nguồn không đổi. Chưa có log của lần chạy Word để kết luận nguyên nhân thiếu đối chiếu XML thực tế.

OfficeComponentCheck bổ sung mẫu giả đúng/sai, trích yếu nhiều đoạn/break, Kính gửi căn giữa, danh sách nơi nhận, sửa nhóm/fault/Undo/lưu-mở; Windows CI chỉ biên dịch phần Word này, không có Word. Số kiểm Core/OpenXML/WPF thực nằm trong build-checks.json và TXT. Chưa xác nhận runtime Office 2021 cho .54.

Thử lại ba thành phần trong tài liệu người dùng, rồi tạo bản sao sai phông/cỡ/kiểu, dấu gạch/dấu chấm phẩy để xem gợi ý. Chọn riêng và sửa chung, quét, một Hoàn tác, lưu/mở; giữ nguyên ngắt dòng và nội dung không chọn. Giữ cơ chế nhiều nhóm .53, XPS thật, rollback và một Undo.

Báo cáo có ví dụ C22/C23, tổng 23 đề xuất/8 thủ công/1 minh họa và nhật ký .54. Bản thử riêng, chưa nhập main; stable/tag/gói .27 giữ nguyên.
