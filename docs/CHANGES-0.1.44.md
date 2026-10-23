# 0.1.44 — Khoảng trắng trước dấu kết câu cuối đoạn (bản thử)

Tiếp nối 0.1.43, commit `dd95a1dc1c62beca90d3ef9544913f36907aeb2b`. Tiếp tục lỗi chữ cơ bản; chưa bắt đầu thư viện ngữ cảnh.

## Gợi ý mới C16

- `Đã hoàn thành .` → `Đã hoàn thành.`; tương tự với `?` và `!`.
- Chỉ xóa dấu cách ASCII ngay trước một dấu `.?!` ở cuối đoạn. Giữ nguyên chữ, dấu câu, khoảng trắng sau và dấu kết thúc đoạn/ô; không ghi lại cả câu.
- Phạm vi hẹp: từ cuối ít nhất hai chữ cái, toàn chữ thường, có từ trước nó và ranh giới rõ. Bỏ qua từ viết hoa/hoa trộn, dạng số/ký hiệu La Mã, từ một chữ cái, token có chấm liền hoặc đoạn một từ sau dấu câu. Đây là các dấu hiệu bảo thủ để tránh nhiều viết tắt/mã; chưa nhận biết mọi viết tắt/ngữ nghĩa.
- Bỏ qua dòng ứng viên số/ngày hoặc cụm ngày…tháng…năm, tab/ngắt dòng trong đoạn, khoảng trắng đặc biệt, dấu kỹ thuật của địa chỉ/đường dẫn/mã/công thức và chuỗi dấu như `...`, `. . .`, `?!`. Ví dụ số `1,25 .`, `TS .`, `host.com .`, `www . com .` không nhận C16.
- Chưa xử lý dấu giữa câu, dấu trước ngoặc/nháy đóng, tự thêm/xóa dấu câu hoặc thêm khoảng trắng sau dấu. Có thể bỏ sót nhiều trường hợp; mọi gợi ý vẫn cần duyệt cả câu.

## Sửa chung

Gợi ý khoảng trắng cũ trùng span C16 được gộp để tránh ghi chồng khi chọn nhóm. Có thể gộp cùng ngoặc, phẩy/chấm phẩy, cụm lặp và phông cả dòng đủ điều kiện. Vẫn đối chiếu kế hoạch hiện tại/nguyên văn/range, áp phông trước và chữ từ cuối về đầu, hậu kiểm toàn story và rollback cả nhóm khi lỗi, một Undo sau thành công. Không retry ghi; dùng quét mới để bỏ mục đã sửa.

Các ngoại lệ trên áp riêng cho C16, không phải thay phạm vi mọi quy tắc cũ. Bảo vệ ô số/ngày và ngoặc chỉ có dấu cách tiếp tục giữ. TextEditable và giới hạn XML sâu 40 đoạn đầu Nhà nước giữ nguyên; chưa chứng minh mọi vùng/trang/profile. Không thay thuật toán Line/kiểm nét XPS, lưu bản sao, checkpoint hoặc rollback cũ.

Báo cáo trong **Trợ giúp → Tiến độ và chức năng…** thêm C16/mốc .44: 16 nhóm có thể đề xuất sửa, 8 nhóm thủ công, 1 minh họa. Danh mục này không phải số lỗi đã sửa trong tài liệu. Gói kèm TXT mẫu, danh sách khả năng và dữ liệu kiểm thử đúng phiên bản.

## Kiểm thử và cách thử

Core thêm 71 ca cho dấu .?!, Unicode, giữ khoảng trắng sau và dấu đoạn/ô, số/viết tắt/địa chỉ/chuỗi dấu, ranh giới, gộp với các lỗi cũ/phông, TextEditable, kế hoạch cũ và lỗi sau xóa khoảng trắng. Windows CI chạy toàn bộ Core/OpenXML + WPF, kiểm dữ liệu báo cáo trong gói publish trước khi phát hành.

Bài **Kiểm tra → Kiểm tra Word tự động…** thêm `sentence-end-input.docx`: ba C16 và năm gợi ý chữ/ngoặc độc lập. Kiểm sửa riêng, lỗi cố ý sau C16 đầu trong nhóm, sửa cả nhóm, danh sách sau quét, giữ đậm/gạch chân từ đầu, một Undo, lưu/mở `sentence-end-fixed.docx`, hash nguồn. Các dòng số/viết tắt/địa chỉ/chuỗi chấm trong mẫu giữ nguyên. Bài Word đã lập trình, chưa xác nhận chạy đạt.

**Chưa có bằng chứng 0.1.44 chạy đạt trên Office 2021.** CI không có Word. Giữ main, stable .27 và mọi tag/gói cũ; đây tiếp tục là bản thử.
