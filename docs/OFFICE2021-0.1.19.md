# 0.1.19 — cần xác minh trên Office 2021

Tiếp nối main a2fa5a90b4de1bb24edfbaaccac20f13ec044fe9 (0.1.18).
Log Office 16.0 build 16.0.20326 báo COMException 0x80004005 khi đọc Shape.AlternativeText trong VerifyPackageLines, trước kiểm tra XPS. Candidate có ba wp:docPr/@descr Agency/Motto/Subject, ID 3/2/1; dữ liệu đó không chứng minh vị trí hiển thị.

Thay đọc AlternativeText trong xác minh, quét lại và chẩn đoán bằng metadata DOCX. Đối chiếu docPr ID với Shape.ID và loại Line; không suy diễn theo thứ tự Shape. ID không khớp sẽ từ chối sửa. Từ chối dấu thiếu, trùng, không hợp lệ hoặc không phải DrawingML Line. Các Line cũ không có metadata được hỗ trợ sẽ không được nhận là Line của ứng dụng.

Giữ kiểm tra chữ, nét XPS thực tế và CheckpointSwap rollback. Chỉ cập nhật trạng thái thành công sau cả xác minh DOCX/Word và XPS. Kiểm thử hồi quy bao gồm thứ tự ID đảo, dấu trùng/thiếu/sai hình và nét hiển thị lệch; bộ kiểm thử hiện có kiểm tra rollback.

GitHub Actions chạy kiểm thử Core và đóng gói Windows x64. Runner không có Word desktop: build đạt không chứng minh chạy đạt trên Office 2021. Chưa có bằng chứng 0.1.18 hoặc 0.1.19 chạy đạt trên Office 2021.

Sau giải nén, chạy Kiểm tra Word trên Office 2021 và giữ office-check.txt cùng failed-line-candidate.docx/failed-line-preview.xps nếu thất bại. Cần kiểm tra sửa ba nét, xem trước, lưu/mở lại, hoàn tác và rollback khi nét sai vị trí. Không sử dụng số Shape làm bằng chứng thành công.
