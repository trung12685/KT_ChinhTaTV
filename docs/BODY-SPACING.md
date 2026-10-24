# C28/C29 — khoảng đoạn và giãn dòng văn xuôi thường

B05 mở thêm nhánh [không Kính gửi có hai mốc văn xuôi đủ proof](BODY-OPENING.md). Các hạn chế Kính gửi dưới đây là phạm vi ban đầu; .63 chỉ có bản tải khi pipeline phát hành đúng commit đạt.

B03/B04 sau .62, code .63 đang phát triển; chưa đóng gói Windows .63. Tái dùng ranh giới Nhà nước C26/C27: khối tên loại/trích yếu chắc, Kính gửi, chữ ký/Nơi nhận; XML chữ/style và phông đã quan sát. Không áp toàn văn bản, Đảng/cá nhân, nhấn mạnh/mục/bảng/field, hanging/thụt cả đoạn hoặc vùng chưa chắc.

Căn cứ đã đọc nguồn NĐ30, Phụ lục I, phần I, mục II.6.e (PDF25): giữa các đoạn tối thiểu6pt; giữa các dòng từ dòng đơn đến1,5lines. Hai nhóm ở Bắt buộc sửa nhưng vẫn cần người dùng duyệt, checkbox riêng/nhóm/all và tooltip3 dòng Lỗi/Căn cứ/Cách xử lý.

## C28 — khoảng cách giữa hai đoạn

Ví dụ: hai đoạn văn xuôi liền nhau trên cùng trang, sau đoạn trước0pt và trước đoạn sau0pt → đề xuất **sau đoạn trước6pt**, giữ khoảng trước đoạn sau.

Không coi After<6 tự nó là lỗi: Word còn có Before của đoạn kế, kiểu gộp/cộng khoảng, contextual/auto spacing và ngắt trang. Bước này chỉ kết luận khi **After trước + Before sau <6pt** (dung sai0,01pt). Tổng là chặn trên ở cả trường hợp cộng hoặc gộp; nếu tổng chưa đủ6 thì chắc chắn chưa đạt. Nếu tổng có thể đủ6 nhưng kết quả gộp chưa rõ thì bỏ qua, chưa tự kết luận đúng/sai; không tạo cảnh báo giả.

Chỉ hai đoạn trực tiếp kế nhau, đều thuộc văn xuôi thường và cùng trang vật lý: so cuối đoạn trước với đầu đoạn sau sau khi Word phân trang. Không bắc qua đoạn trống/tiêu đề/danh sách/bảng/vùng chưa chứng minh. Contextual spacing, auto Before/After, keep-with-next/page-break-before, lưới dòng đang hoạt động hoặc metadata không rõ giữ ngoài phạm vi. Cần hai snapshot spacing/flow, nội dung và vị trí đoạn kế để chặn stale.

Chỉ ghi SpaceAfter=6, không xóa đoạn trống hoặc sửa Before của đoạn kế. Không tuyên bố khoảng thực tế chính xác6pt trong mọi cách cộng: mục tiêu là ít nhất6pt và giữ phần chưa chọn. Chọn riêng vẫn giữ giãn dòng/phông/chữ chưa chọn. Đoạn cuối không bị bịa ra một lỗi thiếu khoảng sau khi không có đoạn văn xuôi liền kế.

## C29 — giãn dòng

Ví dụ: Double (2 dòng) → đề xuất **Single (dòng đơn)**. Multiple0,75 hoặc2 cũng đề xuất Single; Multiple1–1,5 đang đúng giữ nguyên. Word biểu diễn Multiple bằng pt theo12pt/một dòng, nên khoảng12–18 là1–1,5 dòng, không lấy cỡ phông làm mẫu quy đổi.

Chỉ xử lý Double và Multiple ngoài khoảng; Single/1,5 được chấp nhận ở mức thuộc tính. Exactly/AtLeast cần đo tương đương theo phông/bố cục để đánh giá, nên chưa bị coi là lỗi chỉ từ tên chế độ. Lưới dòng, page/keep hoặc vùng chưa chắc bỏ qua. Không có cảnh báo không xác nhận toàn bộ giãn dòng đã đúng.

Chỉ ghi LineSpacingRule=Single, **không gán LineSpacing=12** sau đó để tránh Word chuyển sang Exactly. Pt trả về ở Single/1,5/Double có thể đi theo cỡ chữ đã duyệt; giữ đúng rule. AtLeast/Exactly/Multiple phải giữ cả rule và trị số khi chưa chọn sửa. Before/After, căn/thụt và từng glyph/phông/cỡ/gạch chân chưa chọn được giữ.

## Sửa chung và hậu kiểm

C26 phông, C27 căn/thụt, C28 khoảng đoạn, C29 giãn dòng là các phần độc lập trên cùng đoạn đầy đủ. Snapshot trước sửa phải nhất quán; mỗi writer nhận trạng thái dự kiến sau những thay đổi đã duyệt trước đó, không lấy trạng thái vừa đọc tùy ý làm bằng chứng. Chọn bất kỳ thứ tự nào cũng ghép được, sau đó mới sửa chữ, đo lại XPS và xử lý Line.

Hậu kiểm glyph/cỡ/gạch chân và từng thuộc tính đoạn sau lưu và sau Line. Với C28 còn đối chiếu lại đúng đoạn kế, ngữ cảnh XML và Before/kiểu đoạn kế dự kiến; được chọn sửa đoạn kế thì tính đúng phần đó. Việc phân trang thay đổi do sửa đã duyệt không thay cho chứng minh SpaceAfter6 đang có thật. Bỏ lookup/prefix đọc lặp trong bước ràng buộc cuối. Một Undo; COM hoặc hậu kiểm thất bại thì phục hồi cả lượt, giữ nguồn và lưu bản sao.

Core kiểm cặp đoạn, biên trang/đoạn trống/auto/contextual/grid, kế thừa style, các chế độ giãn dòng, sửa riêng/chung, stale/forged payload, hậu kiểm đoạn kế và phần chưa chọn. Desktop giả lập chứng minh writer chỉ ghi After hoặc rule, không đụng Font/Before/pt/căn/thụt ngoài lựa chọn; test drift/COM/nhóm. Fixture Word tổng hợp kiểm riêng/chung chữ/phông/căn/ba Line, Undo/rollback, lưu-mở và hash nguồn. CI **chỉ biên dịch** fixture Word, không có runtime Word/Office2021 và chưa khép phản hồi phông cũ.

## Việc còn lại

Đo khoảng đoạn thực tế khi cộng/gộp/contextual/auto; đo Exactly/AtLeast và grid theo baseline/phông; mở nhận diện từng loại văn bản/mẫu có bằng chứng. Kiểm tổng thể trên Office2021. Không suy toàn bộ NĐ30 hoặc chính tả đã đạt chỉ từ việc danh sách lỗi trống.
