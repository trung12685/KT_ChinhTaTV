# Ba Shape Line — bản thử 0.1.17

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

Từ bản 0.1.15, sau khi duyệt, ghi Shape Line VML vào candidate DOCX rồi mở lại Word; không gọi COM AddLine/LineFormat/Delete. Chỉ đặt lại Line có marker của ứng dụng trong vùng đã xác minh. Line nằm ngang, đen, nét liền 0,5pt, không mũi tên. Vị trí theo trang; neo vào đoạn mốc, không wrap/reflow chữ, không thay nội dung. Màu cam trên preview chỉ là vị trí đề xuất, chưa ghi Word. Sửa/chọn lỗi vẫn dùng XPS cache như trước.

Không trộn sửa chữ/định dạng và sửa Line trong cùng lượt vì chúng có thể làm đổi số đo trang. Nếu Sửa tất cả có cả hai nhóm, ứng dụng thông báo và sửa chữ trước; sau khi dựng lại, bấm Sửa tất cả lần nữa để sửa Line. Chọn ô thủ công cũng cần chọn riêng từng nhóm. Mỗi lượt Line dùng một snapshot trước sửa, một lần mở lại Word và một lượt quét/xuất lại. Nút Hoàn tác phục hồi snapshot của cả lượt; sửa chữ/định dạng cũng ghi snapshot để giữ thứ tự hoàn tác. Nếu tham chiếu có lỗi định dạng/nội dung chồng lấn chưa sửa, Line chỉ cảnh báo để sửa tham chiếu trước.

## Chưa xác minh trên Windows

Đã kiểm thử logic nhận diện/độ dài/tâm/khoảng cách, biên dịch chéo và kiểm nhận diện XML của file người dùng; chưa chạy đo XPS thực tế, dựng VML Line, neo trong bảng, đặt lại Line/Undo hoặc lưu/mở qua Office2021 trong cloud Linux. Không khẳng định đã sửa chính xác file người dùng. Cần thử trên bản sao theo WINDOWS-VALIDATION.md trước khi dùng thực tế.

Giới hạn khác: chưa nhận diện mọi cấu trúc header, nested/merged tables, nhóm Shape, mọi cách neo, ký tự underline thay Line hoặc mọi mẫu pháp lý. Khi căn giữa/mốc/ghép không chắc chắn, chỉ cảnh báo. Không tự xử lý Shape của văn bản Đảng hoặc cá nhân bằng bộ này.

Bản 0.1.10 đọc vị trí ô từ bản sao range thu gọn và mở rộng tới bảng (wdTable), tránh collection Tables rỗng của đoạn văn. Khi Word trả lỗi 5941, tắt tự kiểm/sửa Line cho lượt quét và hiển thị cảnh báo thủ công; vẫn chạy kiểm chính tả.

## Thay thế cách đọc bảng từ 0.1.11

Không dùng phương pháp COM của bản 0.1.10 nữa. Mỗi lượt quét đọc XML từ `Document.Content.WordOpenXML` đang mở, gồm nội dung/style, rồi ghép 40 đoạn đầu theo thứ tự và text với các range Word hiện tại. CellStart/CellEnd/TableStart lấy từ Start/End thật của các đoạn đã khớp, không dùng độ dài XML để đoán CP. Có kiểm trạng thái trong bảng, padding trực tiếp và style kế thừa; ô chưa xác minh đủ, ô gộp/lồng không có mốc tự sửa. Bảng sau không đầy đủ không vô hiệu hóa bảng đầu.

Việc đọc XML xác định cấu trúc, không thay phép đo XPS. Khi không có Line neo trong vùng và mọi Shape của trang được phân vùng chắc chắn, ứng dụng đề xuất tạo Shape Line mới. Lưu/mở và quét lại vẫn cần kiểm không chèn trùng trên Office 2021.

## Đối chiếu từ 0.1.12

Sau khi khớp text theo thứ tự, trạng thái trong bảng lấy từ XML; không chặn vì cờ COM tại ranh giới ô. Chỉ lấy ký tự thuộc run, không tính tab stop trong pPr là text. Ánh xạ ngắt trang/cột/dòng và gạch nối không ngắt/tùy chọn theo mã Word. Giữ phần đầu đã khớp nếu lỗi ở đoạn sau; ô chỉ có mốc tự sửa khi toàn ô đã khớp và trích yếu phải có mốc kết thúc khối. Mã XML-TEXT/XML-CONTENT/XML-COUNT cùng số đoạn và độ dài được ghi trong nhật ký, không ghi text. Thông báo thiếu mốc có mã cụ thể, không khẳng định Word không cung cấp thông tin bảng.

Nhật ký thực tế xác định điều kiện so sánh gộp của 0.1.11 thất bại, chưa phân biệt text hay cờ COM. Các kiểm thử mới bao phủ riêng những trường hợp này; chưa xác minh bản sửa trên Office 2021.

## Tạo Line từ 0.1.13

Line mới có hai điểm đầu/cuối ngang bằng nhau từ AddLine; không gọi setter Rotation hoặc Height. Trước đặt lại Line có sẵn, yêu cầu Height trong 0,5pt; không cố xoay Line chéo. Nếu lỗi sau khi tạo, cố xóa Line mới; lỗi dọn dẹp cũng được ghi và người dùng vẫn cần Undo/kiểm tra nhóm sửa bị gián đoạn. Các bước Create/HorizontalCheck/PositionMode/Wrap/Geometry/Style/Marker có tên riêng trong nhật ký.

Đối chiếu XML có thể bỏ qua đoạn Word rỗng bổ sung ngoài bảng sau một đoạn ngoài bảng đã khớp; không bỏ qua marker cuối ô, đoạn trong ô hoặc bước sau sai nội dung. Không thay đổi Word text/CP. Có kiểm thử giả lập setter Rotation/Height trả E_FAIL và lỗi geometry để xác minh dọn Line mới.

## Vòng đời COM từ 0.1.14

Collection Shapes còn sống suốt thao tác; WrapFormat/LineFormat/Color chỉ giải phóng sau cấu hình hoặc dọn Line lỗi. Thứ tự giải phóng Color, LineFormat, WrapFormat, Shape, Shapes. Mỗi acquisition dùng ReleaseComObject cân bằng một tham chiếu, không FinalReleaseComObject. Cấu hình nét/marker khi Shape còn ở trạng thái neo ban đầu, rồi mới đặt chế độ vị trí/wrap/số đo.

Kiểm thử giả lập bao phủ phụ thuộc bị vô hiệu hóa khi giải phóng WrapFormat sớm, thứ tự cấu hình nét trước đổi neo và Delete trước giải phóng các RCW. Mô hình này kiểm thứ tự/lifetime, chưa chứng minh nguyên nhân E_FAIL trong Word thực tế. Nhật ký xác nhận getter Line và Delete thất bại; Office 2021 cần thử riêng.

## Cơ chế thay thế từ 0.1.15

Các mục 0.1.13/0.1.14 bên trên là lịch sử, không còn là đường ghi hiện tại. `DocxLineEditor` đọc checkpoint đã lưu, đối chiếu text/range của từng đoạn với XML gói, thêm w:r/w:pict/v:line vào mốc đã xác minh. Line có from/to theo pt cùng cao độ, relative-page, stroke đen 0,5pt, không wrap, allowincell=false, anchorlock, marker theo vai trò. Không sửa part nào khác document.xml. Không đoán Word CP từ chiều dài XML.

`CheckpointSwap` đóng tài liệu làm việc rồi thay file và mở lại. Sau mở, xác minh Word story text giữ nguyên và mỗi marker vừa sửa được nhận là một msoLine. Nếu lỗi mở/kiểm, đóng candidate và mở lại checkpoint trước; nếu cả phục hồi cũng lỗi, giữ file rollback trong thư mục làm việc và báo đường dẫn. Không tự thử lại thao tác chỉnh Shape.

Mỗi lượt sửa lưu một snapshot trước đó. Nút Hoàn tác phục hồi snapshot theo thứ tự (kể cả các lượt sửa chữ), không dựa vào native Word Undo mất sau khi mở lại. Snapshot chỉ tồn tại trong phiên; app vẫn giữ backup khi rollback thất bại. Chưa có Redo.

Đã kiểm chuẩn bằng OpenXML SDK cho ba Line và không phát sinh lỗi chuẩn mới trên toàn gói từ bản sao tài liệu riêng tư; chữ/định dạng XML và byte các part khác giữ nguyên. Kiểm thử này không xác minh render hoặc COM mở/Save/XPS trên Office 2021.

## Khóa tệp từ 0.1.16

Nguồn đọc ZIP là snapshot `.undo.docx` đã sao chép ngay trước lượt sửa, không phải `working.docx` còn mở trong Word. Word giữ handle ghi; ZipFile.OpenRead mặc định FileShare.Read không tương thích với handle này trên Windows, dẫn đến 0x80070020 trước khi tạo candidate. Luồng: Save → File.Copy snapshot → đọc/sửa ZIP của snapshot thành candidate → Close tài liệu làm việc → thay file → Open/xác minh → chấp nhận hoặc rollback.

Kiểm thử độc lập giữ khóa file làm việc và xác minh chỉ bản sao được đọc. Bài kiểm tra qua menu chạy trên Word thật của máy Windows, sử dụng cùng WordSession/FixMany với mẫu riêng. Kết quả PASS chỉ cho các thao tác ghi/mở/lưu/XPS/Undo trên mẫu; không phải chứng nhận mọi file hoặc tọa độ thể thức. Cloud chưa chạy bài kiểm tra Office này.

## Vị trí và kiểm chứng từ 0.1.17

VML đặt hộp theo trang bằng margin-left/top, width và height=0; from=(0,0), to=(width,0) là đầu mút cục bộ. Không ghi tọa độ toàn trang vào đầu mút. Khi thay Line của ứng dụng, dọn pict rỗng trong đoạn tham chiếu đã xác minh.

Căn giữa cấu trúc dùng để duyệt toàn khối trích yếu độc lập với điều kiện đo an toàn của dòng cuối. Dấu cách cuối đoạn trước không làm mất mốc cuối. Mỗi vai trò chưa nhận diện có cảnh báo riêng. Trước chấp nhận candidate, so nét XPS với vị trí đề xuất: hai đầu và cao độ phải lệch không quá 1pt. Thiếu nét hoặc sai vị trí gây rollback, không chỉ kiểm marker/số Shape. Chưa xác minh thực tế trên Office 2021.
