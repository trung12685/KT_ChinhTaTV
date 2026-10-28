# Kiểm thử 0.1.75 trên máy có Word

CI chỉ kiểm Core/OpenXML, WPF và build. Kết quả bên dưới cần chạy trên máy có Microsoft Word; không coi build thành công là Word đã đạt.

1. Giải nén ZIP vào thư mục mới; chạy ChinhTa.Desktop.exe và kiểm phiên bản 0.1.75. Giữ menu “Thử bộ đọc mới” bỏ tích.
2. Chọn **Kiểm tra → Kiểm tra từng nhóm Word…**. Mặc định chọn sáu nhóm; có thể chỉ chọn nhóm muốn kiểm. Mỗi nhóm dùng mẫu giả và phiên Word riêng, không dùng tài liệu đang mở của bạn. Nhóm khoảng đoạn bắt đầu trực tiếp, không đợi bài Subject/phông phía trước.
3. Xem từng PASS/FAIL/NOT_RUN và tổng số. PASS của các nhóm được chọn không xác nhận toàn bài. Có bước phối hợp với Line trong chính một số nhóm; bước này lỗi vẫn FAIL, các nhóm tiếp theo vẫn chạy.
4. Kết quả ở thư mục **OfficeGroups-…**, file **office-groups.txt** và thư mục con từng nhóm. Giữ ZIP cả thư mục này cùng diagnostics.log và build-checks.json. Mỗi kết quả/lỗi được ghi trước khi nhóm tiếp bắt đầu.
5. Chạy thêm **Kiểm tra → Kiểm tra Word tự động…** để kiểm luồng toàn bộ và đường kẻ. Bài này vẫn dừng khi lỗi đầu tiên; giữ **OfficeCheck-…/office-check.txt**, các modern-before-*.docx/.json/.xps và mapping-evidence-*.json nếu có. Không lấy kết quả nhóm thay kết quả toàn bài.

Chỉ khi chủ động chọn bộ đọc thử nghiệm, bài toàn bộ mới bắt buộc so hai cách đọc. Bài theo nhóm luôn dùng bộ đọc cũ và không xác nhận thử nghiệm. Không đổi tài liệu mẫu để làm mất FAIL.

## Thử chừa chỗ ký tay

Mở bản sao tài liệu Nhà nước có Nơi nhận/Lưu bên trái và chức vụ/họ tên rõ trong bảng cuối. Chọn **Kiểm tra → Chừa chỗ ký tay (80 pt)**. Lệnh đo khoảng đang có, giữ vùng đủ và chỉ bổ sung phần thiếu. Kiểm trang, chữ/phông, một Hoàn tác và bản sao lưu/mở lại. Không đủ chỗ trên cùng trang hoặc chưa rõ vùng thì giữ/phục hồi và báo, không thêm trang để đạt 80 pt. Không cần ảnh chữ ký; vùng có ảnh/đối tượng chưa được lệnh này hỗ trợ.

Khoảng 80 pt là thiết lập theo nhu cầu in rồi ký. Những vùng khác của tài liệu vẫn cần các nhóm kiểm tương ứng; xem phạm vi trong SUPPORTED-FIXES-0.1.75.md và REMAINING-WORK.md.
