# Đợt phát triển sau 0.1.63

Nền `e693191ff6caf93b0ca28c9532e0df3a7d622f4d`, tag v0.1.63: 3116 Core/OpenXML +232 WPF +1 kiểm metadata gói; chưa Word runtime. Code0.1.64 đang phát triển, chưa có gói Windows .64. Tích lũy khoảng5–10 bước theo yêu cầu người dùng; main/stable .27 và gói .63 giữ nguyên.

## B01 — C26 giữ chữ nhấn mạnh

Ví dụ: đoạn thân bài đã nhận diện có từ **đậm** Arial đỏ cỡ12 và từ *nghiêng* cỡ14 → Times New Roman đen, cỡ12 về13, cỡ14 giữ; chữ đậm vẫn đậm, nghiêng vẫn nghiêng, gạch chân vẫn giữ. Nếu phông/màu/cỡ đã đúng thì đậm/nghiêng tự nó không tạo lỗi C26.

Thêm hợp đồng PreserveEmphasis chỉ cho Body C26, không dùng nó làm yếu yêu cầu đậm/đứng của tên loại/cơ quan/quốc hiệu. Mỗi glyph phải có kiểu chắc; chưa đọc được font, bold/italic hoặc snapshot đoạn/spacing thì chưa cấp sửa. C27–C29 và hai mốc mở đầu không Kính gửi vẫn giữ guard không nhấn mạnh cũ.

Tách writer ký tự có kiểm text/valid trước setter. C26 chỉ ghi Name/Color và cỡ sai ở glyph, không đọc/ghi Bold/Italic tổng hợp, không ép Size cả vùng kể cả cỡ hợp lệ ở khoảng trắng. Giữ từng cỡ đúng/đậm/nghiêng/gạch chân, căn/thụt/spacing theo source; chọn riêng giữ lỗi chữ chưa chọn, chọn chung có phép chiếu glyph còn lại và kiểm sau lưu/Line. Một Undo/rollback và giữ nguồn.

C26 có proof kiểu đầy đủ gồm cỡ nên không chạy thêm một lượt đọc cỡ riêng trùng; vẫn giữ postcondition cỡ chính xác từng glyph. Core kiểm policy/guard/giữ kiểu/cỡ/stale/drift/nhánh không Kính gửi; Desktop giả lập chứng minh không chạm aggregate Bold/Italic/Size và không ghi lại cỡ đúng. Fixture Word kiểm riêng/chung chữ+ba Line, kiểu/cỡ/spacing, Undo/rollback/lưu-mở/nguồn; CI chỉ biên dịch fixture.

Tổng vẫn29 đề xuất/8 thủ công/1 minh họa. Kết quả kiểm theo commit tại PR/Actions; không lấy tổng .63 làm chứng cứ cho code mới. Chưa xác nhận runtime Office2021 hoặc khép phản hồi phông cũ.

## B02 — C27–C29 giữ từ nhấn mạnh

Ví dụ: đoạn văn thường có từ đậm/nghiêng, căn trái, thụt0, Double, khoảng0+0 với đoạn kế → căn đều/thụt1,27cm/Single/sau6pt theo nhóm đã chọn; giữ đậm/nghiêng/gạch chân và từng cỡ. Không buộc chọn sửa phông kèm bố cục. Chọn riêng một thuộc tính giữ toàn bộ lỗi chữ/phông/spacing chưa chọn; chọn chung4 nhóm với chữ và baLine vẫn qua proof glyph, snapshot và XPS thực.

Đoạn phải có ít nhất một phần chữ thường đứng đã đọc chắc; toàn đậm/nghiêng hoặc unknown không cấp sửa bố cục. Không mở cấu trúc/mục/bảng/chữ ký, mốc không Kính gửi vẫn hai đoạn plain có proof. Chỉ bỏ guard từ chối mọi nhấn mạnh trong C27–C29; writer đoạn không thêm setter Font. Core kiểm riêng/chung/đổi thứ tự chọn, postcondition từng kiểu/cỡ, stale source/neighbor/context và quét mới; fixture Word kiểm riêng từng căn/gap/line, chung4 nhóm/chữ/baLine, rollback/mộtUndo/lưu-mở/hashnguồn. CI chỉ biên dịch fixture. Tổng29/8/1; hai bước .64 chưa đóng gói.

## B03 — giảm phân tích lặp khi quét thân bài

Trước đây CheckCore gọi C26, C27 và C28/C29 riêng, mỗi nhóm nhận diện lại cùng tên loại/trích yếu/mốc mở đầu/ranh giới và các đoạn văn xuôi. Nay BodyRuleScan chụp một danh sách đoạn trong lần gọi, chạy RepairTargets một lần rồi truyền cùng tập đã chứng minh cho ba bộ chính sách độc lập. Giữ thứ tự C26 → C27 → C29 → C28 và mọi payload/citation/source/neighbor; spacing vẫn xét đoạn liền kề gốc, không dùng danh sách đã lọc để bắc qua đoạn trống/mục/bảng. Profile ngoài Nhà nước/vùng chưa chứng minh trả rỗng.

Không thêm cache dài hạn, không dùng ObservationTargets làm quyền sửa, không bỏ đọc XML/COM/XPS hoặc quét mới trước/sau sửa/lưu/Line. Đây là giảm CPU phân tích trùng; chưa có số đo tốc độ toàn phiên Word/Office2021. Các API kiểm riêng vẫn giữ cho kiểm độc lập và so sánh, không còn gọi ba lần trong CheckCore.

Kiểm hợp đồng đối chiếu toàn bộ Issue và thứ tự trên ba profile, biến thể an toàn/unknown/nhấn mạnh/list/grid/stale/duplicate-start/boundary; no-greeting chưa quan sát không cấp quyền. Mẫu500 đoạn giữ đủ từng lỗi; bộ đếm đọc layout chứng minh chỉ còn một lần phân tích thay ba; quét sau sửa bỏ lỗi đã sửa, mất ranh giới thu hồi quyền và snapshot cũ không được thay đổi/tái dùng. Đối chiếu riêng assembly B02 với luồng mới trên51 trường hợp giả cho JSON Issue giống từng byte; xem hash trong tài liệu thiết kế. Toàn bộ fixture Word cũ vẫn đi qua CheckCore mới khi chạy thật; CI chỉ biên dịch fixture. Tổng29/8/1, ba bước .64 chưa đóng gói. [Chi tiết](BODY-RULE-SCAN.md).

## Tiếp theo

Kiểm tổng thể C26 mới trên Word; mở từng phạm vi thể thức/lỗi chữ có bằng chứng, hoàn thiện đo spacing còn thiếu theo mẫu và ngoại lệ. Không tự đoán ngữ nghĩa hoặc coi nhấn mạnh là sai để xóa. [Chi tiết](BODY-EMPHASIS.md).
