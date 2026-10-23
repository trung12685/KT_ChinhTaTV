# 0.1.43 — Khoảng trắng sát bên trong ngoặc tròn (bản thử)

Tiếp nối 0.1.42, commit `73acbfcf6e688be858dcaa823097110308cfcb14`. Tiếp tục lỗi chữ cơ bản; chưa bắt đầu thư viện ngữ cảnh.

## Phần mới

- `Theo mẫu ( nội dung ) đã duyệt` → đề xuất `Theo mẫu (nội dung) đã duyệt`.
- Mỗi mép có dấu cách thừa là một mục để chọn riêng. Chỉ xóa dấu cách ASCII ngay sau `(` hoặc trước `)`, giữ chữ, dấu ngoặc, định dạng các từ và khoảng cách ngoài. Không ghi lại cả cụm trong ngoặc.
- Chỉ xét ngoặc tròn cân bằng, không lồng trong cùng đoạn; có ranh giới ngoài rõ và nội dung chữ đơn giản. Bỏ qua đoạn có tab/dấu kỹ thuật như email, đường dẫn, mã, dòng ứng viên số/ngày hoặc cụm ngày…tháng…năm, kể cả có chú thích ngoài mẫu header chặt. Không mở rộng bộ nhận diện hành chính để tự sửa các dòng ngày đó.
- Bỏ qua phần trong ngoặc chỉ có số, toàn chữ hoa, từ một chữ cái, ký hiệu ngoài phạm vi, khoảng trắng đặc biệt hoặc ngắt dòng. Phần chữ cho phép chữ/dấu tổ hợp, dấu cách, phẩy/chấm phẩy và ba dạng dấu nối; cần có từ ít nhất hai chữ cái và chữ thường. Chưa phân biệt mọi mã/công thức viết bằng chữ với văn xuôi, nên vẫn cần người dùng duyệt cả câu.
- Không thêm/xóa dấu ngoặc, sửa ngoặc thiếu/lồng, khoảng ngoài ngoặc, ngoặc vuông hoặc dấu nháy. Phạm vi cố ý hẹp; không có gợi ý không có nghĩa đã kiểm hết dấu câu.

## Sửa chung và giữ ô trống

Gợi ý khoảng trắng cũ trùng mép trong ngoặc được gộp vào mục mới để tránh sửa chồng. Có thể chọn chung với khoảng trắng độc lập, cụm lặp, phẩy/chấm phẩy và phông cả dòng đủ điều kiện. Vẫn đối chiếu kế hoạch hiện tại/nguyên văn, áp phông trước, chữ từ cuối về đầu, kiểm toàn story và rollback cả lượt khi lỗi; không retry ghi.

Dấu phẩy/chấm phẩy nhận thêm ranh giới ngoặc tròn trong văn xuôi. Ví dụ chỉ sửa ngoặc của `( công,tác )` thành `(công,tác)` thì gợi ý dấu phẩy chưa sửa vẫn phải còn; chọn cả hai loại cho ra `(công, tác)` một lượt. Không tự coi hàm viết liền như `fn(công,tác)` là văn xuôi.

Ô chỉ chứa dấu cách như `(    )` được giữ nguyên cả trong đường sửa khoảng trắng cũ khi bấm sửa tất cả. Các mẫu chứa số/ký hiệu được bỏ qua riêng bởi quy tắc ngoặc mới; không cam kết mọi quy tắc cũ đều bỏ qua toàn bộ đoạn có chúng. Trường trống số/ngày cũ tiếp tục được bảo vệ.

Tôn trọng TextEditable như trước; đối chiếu XML sâu hiện vẫn chỉ 40 đoạn đầu Nhà nước, chưa chứng minh mọi trang/profile. Không thay thuật toán Line/kiểm nét XPS, lưu bản sao, checkpoint hoặc rollback cũ. Không ẩn mục lỗi theo lịch sử; dùng quét thực tế.

Báo cáo trong **Trợ giúp → Tiến độ và chức năng…** thêm C15 và mốc .43: 15 nhóm có thể đề xuất sửa, 8 nhóm thủ công, 1 minh họa. TXT mẫu, danh sách khả năng hiện tại và dữ liệu kiểm thử đúng gói đi kèm ZIP.

## Kiểm thử và cách thử

Core thêm các ca sửa riêng/nhóm, giữ ô trống qua Fix All, ngoại lệ/Unicode, gộp phông, không chồng với khoảng trắng/cụm lặp, kế hoạch cũ, giữ gợi ý dấu câu chưa chọn sau sửa ngoặc và rollback sau xóa mép ngoặc. Windows CI chạy toàn bộ Core/OpenXML + WPF và kiểm báo cáo trong gói publish trước khi phát hành.

Bài **Kiểm tra → Kiểm tra Word tự động…** thêm `parenthesis-input.docx`: hai mép ngoặc, một cụm lặp, một phẩy/chấm phẩy và một khoảng trắng ngoài độc lập; dòng ô trống/số/mã/ký hiệu giữ nguyên. Kiểm sửa riêng, lỗi cố ý sau xóa mép ngoặc trong nhóm, sửa cả nhóm, danh sách quét mới, đậm/gạch chân phần chữ giữ lại, một Undo, lưu/mở `parenthesis-fixed.docx`, hash nguồn. Bài Word đã lập trình, chưa xác nhận chạy đạt.

**Chưa có bằng chứng 0.1.43 chạy đạt trên Office 2021.** CI không có Word. Giữ main, stable .27 và mọi tag/gói cũ; đây tiếp tục là bản thử.
