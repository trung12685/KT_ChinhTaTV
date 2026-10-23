# 0.1.42 — Khoảng trắng quanh dấu phẩy/chấm phẩy (bản thử)

Tiếp nối 0.1.41, commit `d21be190f56e101201ae057f809e9e223b80b9c1`. Tiếp tục lỗi chữ cơ bản theo thứ tự đã thống nhất, chưa bắt đầu thư viện ngữ cảnh.

## Gợi ý mới

Giữa hai từ có ít nhất hai chữ cái, đề xuất bỏ dấu cách trước dấu phẩy/chấm phẩy và giữ một dấu cách sau:

- `Nội dung  ,  báo cáo` → `Nội dung, báo cáo`.
- `báo cáo;công tác` → `báo cáo; công tác`.

Chỉ xử lý dấu cách ASCII. Người dùng cần đọc và duyệt cả câu; chương trình chưa phán đoán dấu câu có đúng nghĩa hay không. Không tự thêm/xóa dấu phẩy hoặc chấm phẩy; chưa mở rộng sang chấm, hai chấm, hỏi, than, ngoặc hay dấu nháy.

Quy tắc mới bỏ qua ứng viên dòng số/ngày, mọi đoạn có tab hoặc dấu hiệu kỹ thuật `@ / \ _ =` và dấu mã `` ` < > [ ] { } ``; không tách số, từ một chữ cái, cặp toàn chữ hoa hoặc token có đuôi tên miền/tệp. Phạm vi cố ý hẹp, có thể bỏ sót câu chữ có thể sửa. Đây là giới hạn của quy tắc dấu câu mới, không phải cam kết mọi quy tắc cũ đều bỏ qua các đoạn này.

## Sửa nhóm và đối chiếu

Một gợi ý bao gồm dấu câu cùng các dấu cách sát hai bên; gợi ý khoảng trắng cũ trùng vùng được gộp, tránh chọn nhiều lỗi ghi chồng nhau. Có thể sửa riêng hoặc gộp với lỗi khoảng trắng độc lập, cụm lặp và phông cả dòng đủ điều kiện. Vẫn áp phông trước, chữ từ cuối về đầu, kiểm kế hoạch hiện tại/nguyên văn/toàn story và rollback cả lượt khi thất bại. Không tự lặp lượt ghi. Quét lại bằng dữ liệu thật để bỏ mục đã sửa; không ẩn mục theo lịch sử bấm nút.

Giữ cơ chế TextEditable và các giới hạn bảo vệ hiện có: đối chiếu XML sâu mới phủ 40 đoạn đầu Nhà nước, chưa chứng minh mọi vùng/trang/profile. Không thay thuật toán ba Line, kiểm nét XPS, rollback, lưu bản sao hay checkpoint.

Báo cáo **Trợ giúp → Tiến độ và chức năng…** thêm C14, mốc .42 và giới hạn; tổng hiện tại 14 nhóm có đề xuất, 8 nhóm thủ công và 1 minh họa. Số nhóm không phải số lỗi đã sửa của tài liệu. Báo cáo TXT mẫu và dữ liệu kiểm thử thực tế tiếp tục đi kèm gói.

## Kiểm thử và cách thử

Core kiểm sửa riêng/nhóm, Unicode tổ hợp/surrogate trước lỗi, ngoại lệ số/mã/địa chỉ/header/tab/NBSP/control, chống chồng với khoảng trắng/cụm lặp, gộp phông, kế hoạch cũ và lỗi giữa lượt. Windows CI chạy toàn bộ Core/OpenXML, WPF, kiểm dữ liệu báo cáo của gói đã publish rồi đóng ZIP.

Bài **Kiểm tra → Kiểm tra Word tự động…** thêm mẫu giả `punctuation-input.docx`: hai gợi ý dấu câu, một cụm lặp và một khoảng trắng độc lập; dòng giá trị/mã/địa chỉ giữ nguyên. Kiểm sửa riêng, lỗi cố ý sau lần sửa dấu câu đầu trong nhóm, sửa cả nhóm, quét lại hết mục, giữ đậm/gạch chân chữ trước dấu câu, một Undo, lưu/mở `punctuation-fixed.docx` và hash nguồn. Kịch bản Word đã lập trình, chưa xác nhận chạy đạt.

**Chưa có bằng chứng 0.1.42 chạy đạt trên Office 2021.** CI không có Word. Giữ main, stable .27 và các tag/gói cũ; phát hành này tiếp tục là bản thử.
