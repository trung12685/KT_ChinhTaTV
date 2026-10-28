# Các phần còn thiếu và thứ tự hoàn thiện

Cập nhật ngày 29/10/2026 (UTC+7), mã phát triển 0.1.76, tiếp từ gói 0.1.75. Đối chiếu danh mục 33 nhóm có đề xuất có điều kiện, 8 nhóm thủ công, 1 quy tắc minh họa với đường quét, writer, hậu kiểm và bài Office. Đây là bản phân tích phạm vi mã và bằng chứng hiện có, không xác nhận ứng dụng đã kiểm mọi tài liệu hoặc mọi phiên bản Word.

## Việc cần giải quyết trước khi mở rộng luật

| Phần | Phát hiện trong mã | Hướng xử lý và tiêu chí hoàn thành |
| --- | --- | --- |
| Bài kiểm Office | `OfficeIntegrationCheck.Run` dừng ở lỗi đầu tiên. Lỗi Subject xuất hiện trước các bài nội dung, ký và số trang, nên chưa có kết quả cho các phần sau. | Đợt 14 thêm cách chạy từng nhóm độc lập, mỗi nhóm có phiên Word và thư mục mẫu riêng. Ghi PASS/FAIL/NOT_RUN; một nhóm lỗi không ngăn nhóm sau, cũng không biến tổng thành ĐẠT. Bài toàn bộ giữ kết quả riêng. |
| Khoảng đoạn/giãn dòng | `WordOutlineEvidence` trước đây chưa giữ nguồn `beforeLines/afterLines` và một số bố cục đặc biệt trong quyền sửa C28/C29. COM đọc số point không đủ chứng minh nguồn đơn vị dòng. | Đợt 13 chặn quyền sửa spacing khi nguồn XML trực tiếp/kế thừa/default còn đơn vị dòng, frame, border, section hoặc hướng chữ chưa hỗ trợ. Giữ bằng chứng chữ/phông độc lập. Kiểm lại context sau sửa; thay context phải phục hồi. |
| Thông báo phạm vi | Không có đề xuất sửa không đồng nghĩa nhóm đã được kiểm đầy đủ. | Đợt 13 hiển thị số đoạn nội dung có phần spacing chưa kiểm và lý do. Đây mới là coverage của spacing trong vùng nội dung đã nhận diện. Đợt 15 thêm sổ phạm vi cho 33 nhóm có đề xuất, kể cả nhóm không nhận diện được vùng; đếm trạng thái và cảnh báo riêng, reset khi quét/mở/lỗi, chỉ xác nhận hình học sau bản xem trước. 8 nhóm thủ công/1 minh họa giữ chú thích riêng; chưa phải sổ cho mọi cấu trúc đặc thù. |
| Bằng chứng Word thực | CI chạy Core/OpenXML, WPF và biên dịch ứng dụng; không chạy Microsoft Word. | Cần kết quả trên đúng gói mới, trước hết nhóm độc lập, sau đó toàn bài. Ghi rõ phiên bản, bộ đọc, nhóm, bước, lỗi, thư mục mẫu; không gán PASS từ số kiểm CI. |

## Thể thức còn giới hạn

| Nhóm | Đã làm trong phạm vi chắc chắn | Phần tiếp theo |
| --- | --- | --- |
| C28 khoảng đoạn | Hai đoạn văn xuôi liền nhau, nguồn XML/native rõ, không auto/contextual/grid/bố cục đặc biệt. Tổng hai khoảng dưới 6 pt chỉ là điều kiện đủ để chứng minh thiếu; không coi tổng là khoảng Word thực vẽ. | Đo tác động bố cục theo mốc dòng/line box và nguồn style, kể cả gộp khoảng. Không so trực tiếp khoảng giữa hai hộp mực chữ với 6 pt. Phải phân biệt thiết lập đoạn với leading của phông. |
| C29 giãn dòng | Nhận Single/1.5 và Multiple hợp lệ; sửa Double/Multiple ngoài phạm vi khi đủ bằng chứng. | Exactly/AtLeast còn cần đối chiếu dòng đơn của cùng chữ/phông và bối cảnh Word. Không lấy cỡ chữ point làm chiều cao dòng đơn; không đổi chỉ vì tên chế độ. Hiện thông báo phần chưa kiểm thay vì kết luận sai. |
| C32 khổ giấy/lề | Một section dọc thông thường, đầy đủ hậu kiểm. | Trang ngang, nhiều section, lề đối xứng/gutter, cột, hình và header/footer phải có chính sách giữ nội dung và kiểm phân trang riêng. |
| C33 số trang | Header trống/PAGE đơn giản trong phạm vi đã công bố; giữ footer. Đợt 16 bắt buộc chữ được vẽ thật/hiện đủ, giữ paint và độ hiển thị nội dung/footer; chưa rõ thì phục hồi. | Logo, header nhiều thành phần, footer PAGE, chẵn/lẻ và nhiều section cần hậu kiểm field, vị trí và nội dung từng section trước khi tự sửa. |
| Số/ngày cùng hàng | Căn giữa theo khung và cảnh báo lệch dọc khi đo được. | Cần writer dịch dọc có giới hạn, xác minh ô/bảng và không đẩy nội dung; hiện chưa tự sửa vị trí dọc. |
| Khối ký | Phông của vai trò rõ trong bảng cuối; tiện ích chừa khoảng 80 pt theo yêu cầu người dùng. | Khối ký ngoài bảng hoặc chưa nhận diện chắc còn thủ công. Không ưu tiên mở rộng ảnh chữ ký; không suy thiếu ảnh là lỗi. 80 pt là lựa chọn sử dụng, không phải quy định NĐ30. |
| Cơ quan, tên loại, trích yếu | Nhận diện và sửa chữ/phông có điều kiện, giữ cỡ hợp lệ. | Tên/mã cơ quan chính thức cần dữ liệu đối chiếu. Trích yếu toàn hoa không tự chuyển thường vì có tên riêng/mã. Không tự đoán giá trị ngày/số hoặc viết tắt lạ. |
| Mục/dấu đầu dòng | Nhãn gõ tay và quan hệ cha/con đã chứng minh, kiểm dẫn chiếu trước sửa. | Danh sách tự động, cấu trúc lạ và dẫn chiếu chưa rõ giữ thủ công; không tự xóa nội dung để làm mất cảnh báo. |
| Field/control/bảng phức tạp | Vùng đã khớp XML/native mới có quyền sửa tương ứng. | Cần bổ sung mẫu thực, không mở quyền chỉ vì đọc được chữ. Track Changes/tài liệu bảo vệ vẫn ngoài hỗ trợ. |

## Chất lượng luồng sửa và trải nghiệm

- Mỗi nhóm mở thêm phải có: tái hiện trước sửa, quét nguồn hiện tại, giữ phần chưa chọn, sửa riêng/chung, quét lại, lỗi giữa/cuối lượt, một Hoàn tác, lưu bản sao/mở lại và kiểm nguồn bất biến. Không chỉ viết test lặp lại điều kiện của writer.
- Đợt 15 đã có sổ phạm vi lần quét cho 33 nhóm; trạng thái quan sát không thay quyền sửa, không dùng số lỗi bằng 0 làm bằng chứng đã kiểm. Các nhóm không nhận diện được vẫn hiện. Tiếp theo bổ sung phạm vi sâu cho cấu trúc mới khi có mẫu và bằng chứng Word, không gán đủ chỉ vì có tên nhóm trong bảng.
- Các bài Word cần tách phụ thuộc fixture khỏi kết quả nhóm: mẫu nội dung/khoảng đoạn phải tạo được trước khi thử Line, để lỗi Subject không che kết quả spacing.
- Các lớp `WordSession` và bài Office lớn cần tách tiếp theo trách nhiệm sau khi có hồi quy; tránh cải tổ toàn bộ cùng một lượt với thay đổi luật. Mục tiêu là giảm phụ thuộc giữa nhóm, giữ checkpoint/rollback rõ ràng.
- Chưa kết luận có lỗi phá hủy dữ liệu mới trong luồng Undo/lưu từ rà soát này. Các đường hiện có đã có nhiều hậu kiểm; bằng chứng Word thực vẫn cần bổ sung.

## Sau thể thức

1. Mở rộng lỗi chữ cơ bản có ranh giới kiểm chứng: khoảng trắng, dấu câu, ngoặc và từ/cụm lặp; giữ các ngoại lệ số, mã, đường dẫn, viết tắt và vùng không được phép sửa.
2. Xây tập từ và ngoại lệ có nguồn, bộ mẫu đánh giá đúng/sai và số liệu cảnh báo nhầm. Các ví dụ như “xẽ → sẽ” chưa được coi đã hỗ trợ nếu chưa có dữ liệu và test phù hợp.
3. Ngữ cảnh/ngữ nghĩa làm cuối; không tự suy đoán “giành/dành” hoặc viết lại câu từ vài ví dụ.

Ưu tiên đã thống nhất: thể thức trước, lỗi chữ cơ bản tiếp theo, ngữ cảnh sau cùng. Bộ đọc thử nghiệm tắt mặc định; công việc tối ưu tốc độ dừng. Đóng gói theo khoảng 5–10 cụm có ý nghĩa, kiểm đúng commit; giữ main và tag/gói cũ.

## Nguồn kỹ thuật dùng cho spacing

- [Microsoft: SpacingBetweenLines](https://learn.microsoft.com/en-us/dotnet/api/documentformat.openxml.wordprocessing.spacingbetweenlines?view=openxml-3.0.1): cách kết hợp khoảng dòng và khoảng trước/sau, không cộng hai thuộc tính để suy khoảng hiển thị.
- [Microsoft MS-OI29500, 17.3.1.33](https://learn.microsoft.com/en-us/openspecs/office_standards/ms-oi29500/264a0139-8174-4574-b20d-ab778e770931): nguồn đơn vị dòng không bằng 0 ở mức style trước có thể thắng nguồn point áp sau trong Word.
- [Microsoft: Paragraphs.LineSpacing](https://learn.microsoft.com/en-us/office/vba/api/word.paragraphs.linespacing): Exactly cố định theo point, AtLeast có hành vi khác; tên chế độ không chứng minh tương đương dòng đơn/1,5 dòng.
