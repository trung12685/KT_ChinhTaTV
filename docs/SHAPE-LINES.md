# Ba Shape Line — bản thử 0.1.9

## Phạm vi

Chỉ cho chế độ Nhà nước. Nhận diện trong tối đa 40 đoạn đầu, trên trang đầu:
- Cơ quan ban hành: ô ngay trước ô có tiêu ngữ; lấy dòng chữ in hoa dưới cùng trong ô, không lấy dòng cơ quan chủ quản hoặc số/ký hiệu.
- Tiêu ngữ: một dòng được nhận diện duy nhất trong nội dung chính; chấp nhận hyphen/en dash như bản 0.1.7.
- Trích yếu: khối đoạn căn giữa sau một tên loại nhận diện được (BÁO CÁO, THÔNG BÁO, KẾ HOẠCH…); dùng đoạn cuối, rồi đo dòng hiển thị cuối trong XPS, không đo toàn đoạn. Chưa áp dụng cho công văn không có tên loại.

File người dùng gửi được đọc riêng để kiểm thử nhận diện ba vùng qua XML; không đưa file hoặc nội dung lên GitHub. `samples/BaDuongKeThieu.docx` là ví dụ giả độc lập.

## Đo và nhận diện

Word chỉ xuất XPS một lần cho lượt quét. Đo các glyph của dòng cuối trong tọa độ trang, quy đổi 96 DIP/inch thành 72 pt/inch. Khoảng cách được đo từ đáy hộp chữ typographic của dòng cuối đến tâm nét Line, không phải từ đáy paragraph spacing hoặc số lần nhấn Enter. Đây là quy ước đo ứng dụng, cần kiểm chứng với Word thực tế.

Chỉ dùng mốc để sửa khi đoạn căn giữa, không tab/indent, không khoảng trắng đầu/cuối và padding trái/phải ô cân bằng trong 1pt. Với điều kiện này, tâm dòng chữ đại diện tâm vùng đặt. Nếu chưa chắc chắn, chỉ cảnh báo, không tự chèn hoặc suy đoán tâm cột.

Đọc `Document.Shapes`, chỉ xét loại Office msoLine (9). Phân vùng dựa vào Anchor trong ô/khối trích yếu. Với Line do ứng dụng tạo có marker riêng, hai tọa độ đều relative-page, nét đang hiện và Height gần 0, đọc trực tiếp Left/Top/Width theo pt để kiểm lại sau tạo hoặc kéo dịch. Với các Line hiện hữu khác, đối chiếu các đoạn ngang/nét chữ nhật mảnh trong XPS với số Shape Line của trang. Chỉ ghép khi có đúng một Line neo trong vùng, đúng một đường gần mốc và số đường render khớp số Line. Khi không ghép được (group/canvas, nhiều đường, neo ngoài vùng, borders, đường chéo hoặc nét quá dày), báo thủ công; không nhận đường bất kỳ là đường đúng và không chèn trùng.

Khi không có Line trong vùng, chỉ báo thiếu chắc chắn nếu toàn bộ Shape Line ở trang đã được phân về các vùng đã nhận diện và không có group/canvas. Line bị ẩn hoặc khó ghép sẽ không được tự coi là thiếu.

## Độ dài theo NĐ30

| Vùng | Độ dài |
|---|---|
| Cơ quan/trích yếu | 1/3–1/2 dòng cuối; khi tạo chọn 5/12 |
| Tiêu ngữ | Bằng độ dài dòng tiêu ngữ |

Chỉ sử dụng khoảng theo NĐ30; đã bỏ lựa chọn riêng 4/7 khỏi giao diện và bộ kiểm từ bản 0.1.9.

Căn cứ độ dài: NĐ30 PL I, Phần I.II.2.b (cơ quan, PDF22); II.1.b (tiêu ngữ, PDF21); II.5.b (trích yếu, PDF24), theo PDF đã cung cấp. Việc dùng dòng hiển thị cuối ở trích yếu, khoảng cách và dung sai bên dưới là quy ước được người dùng xác định, không trích thành con số bắt buộc của NĐ30.

Thông số hiện cố định trong mã: cơ quan/trích yếu cách 6–15pt, tiêu ngữ 3–13pt; khi tạo chọn 6pt. Tâm lệch quá 3pt thì cảnh báo. Dung sai đo chiều dài 1pt; riêng tiêu ngữ chấp nhận thêm 5%; khoảng cách có dung sai đo 0,5pt. Khoảng độ dài được ghi rõ trong UI và căn cứ cảnh báo.

## Sửa

Sau khi người dùng duyệt, tạo `Shapes.AddLine` hoặc đặt lại Shape ID đã ghép. Line nằm ngang, đen, nét liền 0,5pt, không mũi tên. Vị trí theo trang; neo vào đoạn mốc, không wrap/reflow chữ, không thay nội dung. Màu cam trên preview chỉ là vị trí đề xuất, chưa ghi Word. Sửa/chọn lỗi vẫn dùng XPS cache như trước.

Không trộn sửa chữ/định dạng và sửa Line trong cùng lượt vì chúng có thể làm đổi số đo trang. Nếu Sửa tất cả có cả hai nhóm, ứng dụng thông báo và sửa chữ trước; sau khi dựng lại, bấm Sửa tất cả lần nữa để sửa Line. Chọn ô thủ công cũng cần chọn riêng từng nhóm. Mỗi lượt Line dùng một Undo record/checkpoint, rồi quét/xuất lại một lần. Nếu tham chiếu có lỗi định dạng/nội dung chồng lấn chưa sửa, Line chỉ cảnh báo để sửa tham chiếu trước.

## Chưa xác minh trên Windows

Đã kiểm thử logic nhận diện/độ dài/tâm/khoảng cách, biên dịch chéo và kiểm nhận diện XML của file người dùng; chưa chạy đo XPS thực tế, `Shapes.AddLine`, neo trong bảng, đặt lại Line/Undo hoặc lưu/mở qua Office2021 trong cloud Linux. Không khẳng định đã sửa chính xác file người dùng. Cần thử trên bản sao theo WINDOWS-VALIDATION.md trước khi dùng thực tế.

Giới hạn khác: chưa nhận diện mọi cấu trúc header, nested/merged tables, nhóm Shape, mọi cách neo, ký tự underline thay Line hoặc mọi mẫu pháp lý. Khi căn giữa/mốc/ghép không chắc chắn, chỉ cảnh báo. Không tự xử lý Shape của văn bản Đảng hoặc cá nhân bằng bộ này.
