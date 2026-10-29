> Đã có [bản thử Windows 0.1.81](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40973832589/artifacts/12941004409), nguồn `bd1ec9ea0bbb9bc6664756ac92ccf5af25918398`, gom [đợt 69–72](FORMAT-BATCH-69-72.md) và [73–77](FORMAT-BATCH-73-77.md). Xem [hướng dẫn](PREVIEW-0.1.81.md) và [bằng chứng](PREVIEW-0.1.81-EVIDENCE.md). Core/WPF, EXE, metadata và ZIP đã qua; Word thực vẫn NOT_RUN. Gói trước [0.1.80](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40967945181/artifacts/12938519861) và main/tag/gói cũ giữ nguyên. Release gần nhất vẫn 0.1.76.

Đợt 22 bổ sung nền tảng bản sao toàn gói và đối chiếu XPS trước hiệu chuẩn, cùng nhóm kiểm Word độc lập cho Exactly/AtLeast. Xem [FORMAT-BATCH-22.md](FORMAT-BATCH-22.md). Chưa đo Single/1,5 hoặc bỏ Before/After trên clone; C28/C29 vẫn giữ quyền sửa cũ. Nhóm Word mới chưa được thực thi trên môi trường Linux.

Đợt 23–26 bổ sung mã hiệu chuẩn Single/1,5 và các trial khoảng đoạn 0/6pt, so vector baseline cùng paint/ảnh/nét, phiên Word chỉ đọc và cửa sổ đo thử theo yêu cầu. Xem [FORMAT-BATCH-23-26.md](FORMAT-BATCH-23-26.md). Các đoạn lịch sử bên dưới mô tả giới hạn trước đợt này; quyền sửa tự động C28/C29 vẫn giữ nguyên. Cần Word thật xác minh toàn luồng mới trước khi thiết kế writer/hậu kiểm sửa Exactly/AtLeast.

Đợt 27–29 xử lý các việc độc lập trong lúc chưa có Word-runtime: bảo toàn khoảng/lặp thuộc đường dẫn và tên tệp; phân loại lỗi đo để lỗi toàn vẹn/COM/cleanup không biến thành thiếu bằng chứng; thêm nhóm kiểm Word riêng cho sửa chữ, tổng12nhóm. Xem [FORMAT-BATCH-27-29.md](FORMAT-BATCH-27-29.md). Cần tiếp tục giảm báo nhầm theo dữ liệu và mẫu, không dừng toàn bộ phát triển để chờ riêng Exactly/AtLeast.

Đợt30–32 tiếp tục bảo vệ tên tệp Office/OpenDocument, nhận đúng manifest DOCX hỏng, thêm lệnh kiểm ba nhóm và giữ lỗi đóng phiên kiểm. Bản thử có runtime đi kèm, kiểm EXE/ZIP/manifest và thử thay đổi file trên bản sao trước khi tải lên. Việc tiếp theo cần kết quả `office-groups.txt` trên máy Windows có Word cho ba nhóm clone, hiệu chuẩn và sửa chữ; đồng thời tiếp tục giảm báo nhầm theo mẫu. Quyền sửa Exactly/AtLeast vẫn giữ nguyên.

Đợt 33–36 xử lý thêm các lỗi mở/đóng và báo nhầm đã tái hiện: chuyển tài liệu qua bản tạm, giữ trạng thái cũ tới khi mở thành công; phân biệt rollback thành công với lỗi phục hồi thật; giữ chuỗi trong lời gọi hàm/email Unicode và đường dẫn POSIX; bỏ preview cũ khi quét lỗi, thử dọn mọi tài nguyên độc lập. Kiểm callback và tệp thật không thay kiểm luồng Word thực.

Đợt 37–40 tiếp tục sửa lỗi release range có thể bỏ qua rollback và lỗi xóa tệp tạm che kết quả chính. C01 giữ ô vuông trống và các trường ngày slash có nhãn rõ; C25 nhận khoảng cuối sau một cặp quote/ngoặc đơn văn xuôi đã chứng minh. Kịch bản Word sửa chữ bổ sung các mẫu này, vẫn cần chạy thực sau khi có gói mới. Chưa mở parser mọi biểu mẫu, cú pháp mã hoặc dấu ngoặc lồng.

Đợt 41–44 giữ dấu câu cũ trong bằng chứng style/cỡ của lượt sửa chung; đọc cỡ theo ranh giới Word thực; bổ sung ô đã đánh dấu, nhãn NFC/NFD và trường ngày bằng chữ; dọn đủ tài nguyên hậu kiểm phông mà không che lỗi chính. Phép chiếu chỉ nhận một dấu không đổi giữa ASCII spaces; chưa có fingerprint cho mọi lượt chỉ sửa dấu câu. Không thêm mutation path hoặc suy việc Word giữ định dạng từ kiểm Core/fake.

Đợt 45–47 bổ sung fingerprint cho riêng C14 dấu phẩy/chấm phẩy trong một đoạn đã chứng minh, dùng lại đường hậu kiểm/rollback hiện có; bảo vệ dẫn chiếu NFC/NFD, khoảng ngang Unicode và nhãn có đệm 0; giữ placeholder ô số văn bản và ngày slash. Các đoạn lịch sử nói chưa có fingerprint chỉ sửa dấu câu mô tả trạng thái đến bản 0.1.78. Chưa mở cho mọi dấu câu hoặc tổ hợp style; chưa đổi writer Word. Cần Word thật xác minh dấu phẩy có style khác khoảng bên cạnh, nếu mất style phải FAIL/rollback chứ không coi chữ đúng là PASS.

Đợt 48–51 giữ cả nhãn trong khoảng dẫn chiếu số, nhận ngoặc riêng trong danh sách và giữ các chuỗi literal có tiền tố/đối số sau thuộc dạng rõ. Ô số/ngày/ô chọn đã nhận diện được giữ đệm ASCII khi có NBSP/NNBSP, không mở writer header mới. Đường đóng tài liệu khi Undo/đổi checkpoint tách ownership trước Close/release; lỗi không để document đã đóng còn được dùng như phiên mở. Có kiểm lỗi managed và mở phục hồi tường minh; bài Word số nhiều cấp/sửa chữ bổ sung mẫu mới, vẫn NOT_RUN. Không thêm parser mọi dẫn chiếu/literal/biểu mẫu hoặc quyền sửa Exactly/AtLeast.

Đợt 52–56 hoàn thiện dẫn chiếu lặp cùng cue (“từ mục… đến mục…”); thêm fingerprint khi giữ dấu hai chấm/kết câu/ngoặc/nháy để chèn một khoảng. Save lỗi không còn giữ cờ checkpoint đã xác minh hoặc xóa dữ liệu phục hồi khi đóng. Hoàn tác xong nhưng quét lỗi có thông báo riêng; chọn mục thiếu anchor xóa dấu cũ. Có nhóm Word độc lập thứ 13 `outline-references` để kiểm dẫn chiếu không cần các bước Line trước đạt. Không đổi writer dấu câu hoặc mở quyền Exactly/AtLeast; các phần cần Word thực vẫn chưa được xác nhận.

Đợt 57–60 xử lý chuỗi Save ghi dở rồi Undo lỗi: kết quả byte không đổi/được trả lại không còn nâng đầu vào chưa chắc thành đã xác minh. XML có lịch sử moveTo/moveFrom hoặc mốc move range không còn chỉ cần chữ khớp để cấp quyền sửa. Đối số lệnh có nháy được giữ dữ liệu trong grammar hẹp; UI không hướng dẫn mở lại tự động khi phiên/bản tạm chưa đủ điều kiện. Không chấp nhận/từ chối revision, không mở quyền Track Changes hoặc Exactly/AtLeast. Gói 0.1.79 giữ nguyên.

Đợt 61–64 giữ vùng chữ có bookmark/comment/permission trong phạm vi đã ghép, nhận ô chọn tròn và nhãn ngày sinh đầy đủ; chờ queue/Saved của Word trước xác nhận checkpoint, dọn cửa sổ tiến độ nếu tạo muộn sau lỗi khởi động. Không thay Options, tự lặp Save, đoán CP hoặc tuyên bố bảo toàn mọi annotation. Chờ có giới hạn không cắt được COM call đang treo; còn cần kết quả Word thực. Danh mục 33/8/1 và 13 nhóm Word giữ nguyên.

Đợt 65–68 chặn thêm lịch sử thuộc tính chữ/đoạn/bảng chưa hỗ trợ; giữ ô “Sinh ngày … tháng … năm …”; giải phóng từng acquisition đã lấy ở các đường section/cặp child khi getter lỗi và giữ nguyên lỗi thao tác. UI nhận đúng mất kết nối sau bản lưu chưa chắc dù còn document reference. Nhóm Word sửa chữ thêm mẫu bookmark cùng/qua đoạn và các bước giữ phần chưa chọn qua sửa/Undo/lưu-mở; chưa chạy Word thật, chưa mở quyền annotation hoặc Exactly/AtLeast.

Đợt 69–72 bổ sung `retained-marks`/`annotated-text` chạy độc lập, tổng 15 nhóm trong mã mới (gói 0.1.80 vẫn 13). Giữ dữ liệu trong mảng JSON dạng đoạn và nội dung khoảng dẫn chiếu La Mã canonical; từ chối công bố DOCX/TXT vào thư mục riêng sẽ bị xóa khi đóng phiên. Không gán kết quả Word từ kiểm schema/mô phỏng. Cần chạy các bài mới trên đúng gói tương lai có mã 0.1.81; chưa tạo ZIP trong đợt này.

Đợt 73–77 giữ dẫn chiếu có dấu đóng nháy/ngoặc và thống nhất loại trừ ngày tháng NFC/NFD; kiểm đích lưu chỉ đọc trước Save, giữ trạng thái duyệt khi chọn nhầm. Đối chiếu bản sao/trial so thêm chế độ vẽ glyph/nét XPS, chưa mở quyền Exactly/AtLeast. Cửa sổ phạm vi kiểm có xuất TXT snapshot, không quét lại hoặc đổi Word/Undo. Gom cùng 69–72 cho bản thử 0.1.81 sau kiểm chứng; vẫn cần kết quả Word thực.

# Các phần còn thiếu và thứ tự hoàn thiện

Phân tích nền dưới đây lập cho mã0.1.76 ngày29/10/2026 (UTC+7); các đoạn đầu tài liệu cập nhật đợt20–32 và gói thử0.1.77. Danh mục vẫn33 nhóm có đề xuất có điều kiện,8 nhóm thủ công,1 quy tắc minh họa. Phân tích phạm vi mã và bằng chứng không xác nhận ứng dụng đã kiểm mọi tài liệu hoặc mọi phiên bản Word.

## Việc cần giải quyết trước khi mở rộng luật

| Phần | Phát hiện trong mã | Hướng xử lý và tiêu chí hoàn thành |
| --- | --- | --- |
| Bài kiểm Office | `OfficeIntegrationCheck.Run` dừng ở lỗi đầu tiên. Lỗi Subject xuất hiện trước các bài nội dung, ký và số trang, nên chưa có kết quả cho các phần sau. | Đợt 14 thêm cách chạy từng nhóm độc lập, mỗi nhóm có phiên Word và thư mục mẫu riêng. Ghi PASS/FAIL/NOT_RUN; một nhóm lỗi không ngăn nhóm sau, cũng không biến tổng thành ĐẠT. Bài toàn bộ giữ kết quả riêng. Đợt 18–19 thêm hai nhóm PAGE cuối trang và khổ/lề có đầu–cuối trang trống, tổng tám nhóm, không chờ Subject. |
| Khoảng đoạn/giãn dòng | `WordOutlineEvidence` trước đây chưa giữ nguồn `beforeLines/afterLines` và một số bố cục đặc biệt trong quyền sửa C28/C29. COM đọc số point không đủ chứng minh nguồn đơn vị dòng. | Đợt 13 chặn quyền sửa spacing khi nguồn XML trực tiếp/kế thừa/default còn đơn vị dòng, frame, border, section hoặc hướng chữ chưa hỗ trợ. Giữ bằng chứng chữ/phông độc lập. Kiểm lại context sau sửa; thay context phải phục hồi. |
| Thông báo phạm vi | Không có đề xuất sửa không đồng nghĩa nhóm đã được kiểm đầy đủ. | Đợt 13 hiển thị số đoạn nội dung có phần spacing chưa kiểm và lý do. Đây mới là coverage của spacing trong vùng nội dung đã nhận diện. Đợt 15 thêm sổ phạm vi cho 33 nhóm có đề xuất, kể cả nhóm không nhận diện được vùng; đếm trạng thái và cảnh báo riêng, reset khi quét/mở/lỗi, chỉ xác nhận hình học sau bản xem trước. 8 nhóm thủ công/1 minh họa giữ chú thích riêng; chưa phải sổ cho mọi cấu trúc đặc thù. |
| Bằng chứng Word thực | CI chạy Core/OpenXML, WPF và biên dịch ứng dụng; không chạy Microsoft Word. | Cần kết quả trên đúng gói mới, trước hết nhóm độc lập, sau đó toàn bài. Ghi rõ phiên bản, bộ đọc, nhóm, bước, lỗi, thư mục mẫu; không gán PASS từ số kiểm CI. |

## Thể thức còn giới hạn

| Nhóm | Đã làm trong phạm vi chắc chắn | Phần tiếp theo |
| --- | --- | --- |
| C28 khoảng đoạn | Hai đoạn văn xuôi liền nhau, nguồn XML/native rõ, không auto/contextual/grid/bố cục đặc biệt. Tổng hai khoảng dưới 6 pt chỉ là điều kiện đủ để chứng minh thiếu; không coi tổng là khoảng Word thực vẽ. | Đo tác động bố cục theo mốc dòng/line box và nguồn style, kể cả gộp khoảng. Không so trực tiếp khoảng giữa hai hộp mực chữ với 6 pt. Phải phân biệt thiết lập đoạn với leading của phông; cần bản sao Word cô lập giữ đủ part/quan hệ/ảnh và đối chiếu XPS trước khi dùng các lần hiệu chuẩn spacing. |
| C29 giãn dòng | Nhận Single/1.5 và Multiple hợp lệ; sửa Double/Multiple ngoài phạm vi khi đủ bằng chứng. | Exactly/AtLeast còn cần đối chiếu dòng đơn của cùng chữ/phông và bối cảnh Word trên bản sao cô lập. Trước hiệu chuẩn phải chứng minh bản sao giữ nguyên ngắt dòng/phông/tọa độ nguồn; sau đó mới đo các mức Single/1.5. Không lấy cỡ chữ point làm chiều cao dòng đơn hoặc đổi chỉ vì tên chế độ. Hiện thông báo phần chưa kiểm thay vì kết luận sai. |
| C32 khổ giấy/lề | Một section dọc thông thường, đầy đủ hậu kiểm. Đợt 19 cho phép tham chiếu header/footer thực sự trống khi cấu trúc/style/quan hệ đủ chứng cứ; sau Word phải đo toàn bộ nội dung XPS nằm trong bốn lề. Giữ mọi part khác và phục hồi nếu tràn lề hoặc part trống thay đổi. | Trang ngang, nhiều section, lề đối xứng/gutter, cột, hình và header/footer có nội dung phải có chính sách giữ nội dung và kiểm phân trang riêng. Chưa mở quyền từ hình thức trông trống. |
| C33 số trang | Header trống/PAGE đơn giản; đợt 16 yêu cầu chữ số hiện đủ, giữ paint nội dung/footer. Đợt 17 thêm bảo toàn bitmap toàn ảnh đã giải mã/ánh xạ chắc. Đợt 18 chuyển footer chỉ PAGE trong một section dọc, số Ả Rập từ 1 và 1–9 trang; ghép đúng glyph nguồn trong XPS, giữ part/quan hệ và footer không chọn, chưa rõ thì phục hồi. | Logo/header nhiều thành phần, footer có nhãn/trang trí/field chưa hỗ trợ, số bắt đầu lại, chẵn/lẻ và nhiều section cần hậu kiểm riêng. Bitmap cắt/tile/profile màu hoặc ánh xạ chưa chứng minh vẫn thủ công/phục hồi. |
| Số/ngày cùng hàng | Căn giữa theo khung và cảnh báo lệch dọc khi đo được. | Cần writer dịch dọc có giới hạn, xác minh ô/bảng và không đẩy nội dung; hiện chưa tự sửa vị trí dọc. |
| Khối ký | Phông của vai trò rõ trong bảng cuối; tiện ích chừa khoảng 80 pt theo yêu cầu người dùng. | Khối ký ngoài bảng hoặc chưa nhận diện chắc còn thủ công. Không ưu tiên mở rộng ảnh chữ ký; không suy thiếu ảnh là lỗi. 80 pt là lựa chọn sử dụng, không phải quy định NĐ30. |
| Cơ quan, tên loại, trích yếu | Nhận diện và sửa chữ/phông có điều kiện, giữ cỡ hợp lệ. | Tên/mã cơ quan chính thức cần dữ liệu đối chiếu. Trích yếu toàn hoa không tự chuyển thường vì có tên riêng/mã. Không tự đoán giá trị ngày/số hoặc viết tắt lạ. |
| Mục/dấu đầu dòng | Nhãn gõ tay và quan hệ cha/con đã chứng minh, kiểm dẫn chiếu trước sửa. | Danh sách tự động, cấu trúc lạ và dẫn chiếu chưa rõ giữ thủ công; không tự xóa nội dung để làm mất cảnh báo. |
| Field/control/bảng phức tạp | Vùng đã khớp XML/native mới có quyền sửa tương ứng. | Cần bổ sung mẫu thực, không mở quyền chỉ vì đọc được chữ. Track Changes/tài liệu bảo vệ vẫn ngoài hỗ trợ. |

## Chất lượng luồng sửa và trải nghiệm

- Mỗi nhóm mở thêm phải có: tái hiện trước sửa, quét nguồn hiện tại, giữ phần chưa chọn, sửa riêng/chung, quét lại, lỗi giữa/cuối lượt, một Hoàn tác, lưu bản sao/mở lại và kiểm nguồn bất biến. Không chỉ viết test lặp lại điều kiện của writer.
- Đợt 15 đã có sổ phạm vi lần quét cho 33 nhóm; trạng thái quan sát không thay quyền sửa, không dùng số lỗi bằng 0 làm bằng chứng đã kiểm. Các nhóm không nhận diện được vẫn hiện. Tiếp theo bổ sung phạm vi sâu cho cấu trúc mới khi có mẫu và bằng chứng Word, không gán đủ chỉ vì có tên nhóm trong bảng.
- Đợt 14 đã tách mẫu nội dung/khoảng đoạn theo nhóm, không phải chờ Subject ở bài toàn bộ. Tiếp tục kiểm các fixture mới độc lập, kể cả các bước phối hợp Line trong từng nhóm; lỗi bước đó vẫn là FAIL của nhóm, không che nhóm sau.
- Các lớp `WordSession` và bài Office lớn cần tách tiếp theo trách nhiệm sau khi có hồi quy; tránh cải tổ toàn bộ cùng một lượt với thay đổi luật. Mục tiêu là giảm phụ thuộc giữa nhóm, giữ checkpoint/rollback rõ ràng.
- Đợt 16–19 bổ sung hậu kiểm hiển thị, bitmap, PAGE cuối trang và phạm vi đầu/cuối trang trống. Chưa kết luận có lỗi phá hủy dữ liệu mới trong luồng Undo/lưu từ rà soát này. Các kiểm Core/OpenXML/WPF và biên dịch bài Office không thay bằng chứng Word thực; cần kết quả trên đúng gói mới.

## Sau thể thức

1. Mở rộng lỗi chữ cơ bản có ranh giới kiểm chứng: khoảng trắng, dấu câu, ngoặc và từ/cụm lặp; giữ các ngoại lệ số, mã, đường dẫn, viết tắt và vùng không được phép sửa.
2. Xây tập từ và ngoại lệ có nguồn, bộ mẫu đánh giá đúng/sai và số liệu cảnh báo nhầm. Các ví dụ như “xẽ → sẽ” chưa được coi đã hỗ trợ nếu chưa có dữ liệu và test phù hợp.
3. Ngữ cảnh/ngữ nghĩa làm cuối; không tự suy đoán “giành/dành” hoặc viết lại câu từ vài ví dụ.

Ưu tiên đã thống nhất: thể thức trước, lỗi chữ cơ bản tiếp theo, ngữ cảnh sau cùng. Bộ đọc thử nghiệm tắt mặc định; công việc tối ưu tốc độ dừng. Đóng gói theo khoảng 5–10 cụm có ý nghĩa, kiểm đúng commit; giữ main và tag/gói cũ.

## Nguồn kỹ thuật dùng cho spacing

- [Microsoft: SpacingBetweenLines](https://learn.microsoft.com/en-us/dotnet/api/documentformat.openxml.wordprocessing.spacingbetweenlines?view=openxml-3.0.1): cách kết hợp khoảng dòng và khoảng trước/sau, không cộng hai thuộc tính để suy khoảng hiển thị.
- [Microsoft MS-OI29500, 17.3.1.33](https://learn.microsoft.com/en-us/openspecs/office_standards/ms-oi29500/264a0139-8174-4574-b20d-ab778e770931): nguồn đơn vị dòng không bằng 0 ở mức style trước có thể thắng nguồn point áp sau trong Word.
- [Microsoft: Paragraphs.LineSpacing](https://learn.microsoft.com/en-us/office/vba/api/word.paragraphs.linespacing): Exactly cố định theo point, AtLeast có hành vi khác; tên chế độ không chứng minh tương đương dòng đơn/1,5 dòng.
