# Kiểm thử chấp nhận trên Windows (chưa thực hiện trong cloud Linux)

1. Chạy core checks và mở giao diện bằng các lệnh trong README. Nếu chưa cài Word, khi mở file phải hiển thị thông báo dễ hiểu.
2. Sao chép file mẫu, tính SHA256 trước thử: `Get-FileHash samples/VanBanMau.docx`.
3. Mở file mẫu. Kỳ vọng 5 cảnh báo: quốc hiệu, khoảng trắng và ba vị trí `đo` (đoạn, bảng, trang hai). Câu `Tôi đo chiều dài` và `con đò` không bị cảnh báo.
4. Rê chuột lên từng lỗi; xác minh đúng trang và đúng từ được tô vàng, bao gồm hai từ giống nhau trong các vị trí khác nhau. Đổi zoom và kiểm tra lại. Xuất XPS lại có độ trễ là giới hạn bản thử.
5. Sửa `đo` ở trang hai; kiểm tra hai vị trí còn lại chưa thay đổi. Hoàn tác phải phục hồi đúng từ và số cảnh báo.
6. Sửa quốc hiệu: kiểm tra hoa, đậm, cỡ 13, font và căn giữa trong Word. Hoàn tác một lần phải phục hồi cả nội dung và định dạng.
7. Sửa khoảng trắng, lưu tên mới. Mở bản mới bằng Word, xác nhận không có màu highlight phát sinh; bảng, ngắt trang và các định dạng không liên quan còn nguyên. Kiểm tra SHA256 file gốc không thay đổi.
8. Thử mở file khác/đóng ứng dụng khi chưa lưu: chọn Không phải giữ phiên hiện tại; chọn Có mới bỏ bản sửa. Không được đóng phiên Word người dùng đã mở trước đó.
9. Thử tài liệu thực tế có ô gộp, nhiều font, trường tự động và nhiều trang. So sánh bản xem trước với Word trên cùng máy; ghi riêng các khác biệt, không khẳng định giống hệt trước khi so sánh.
10. Thử tài liệu có Track Changes, bảo vệ, file lỗi và file đang bị khóa. Kiểm tra thông báo lỗi, mở lại file hợp lệ và không để lại Word chạy ngầm sau khi thoát. Kiểm tra thư mục tạm `ChinhTa-*` được dọn sau phiên bình thường.
11. Tắt mạng sau khi build, chạy lại mở/quét/sửa/lưu để xác nhận offline. Thử trên bản sao tài liệu, không dùng duy nhất bản tài liệu gốc.

Ghi kết quả cùng phiên bản Windows, Word, .NET và cấu hình máy. Chỉ công bố các bước thực sự đã chạy.
