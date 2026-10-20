# Kiểm thử chấp nhận trên Windows (chưa thực hiện trong cloud Linux)

1. Chạy core checks và mở giao diện bằng các lệnh trong README. Nếu chưa cài Word, khi mở file phải hiển thị thông báo dễ hiểu.
2. Sao chép file mẫu, tính SHA256 trước thử: `Get-FileHash samples/VanBanMau.docx`.
3. Mở file mẫu. Kỳ vọng 5 cảnh báo: quốc hiệu, khoảng trắng và ba vị trí `đo` (đoạn, bảng, trang hai). Câu `Tôi đo chiều dài` và `con đò` không bị cảnh báo.
4. Bản 0.1.2: lúc mở không có highlight. Chọn/rê chuột từng lỗi: chỉ lỗi đó có khung vàng; khung cũ phải biến mất, danh sách phải chọn đúng lỗi tương ứng. Kiểm tra ba từ `đo` giống nhau ở đoạn, bảng và trang hai để loại trừ việc tô lần xuất hiện đầu. Kiểm tra khoảng trắng chỉ tô khoảng trắng, quốc hiệu tô từng dòng khi xuống dòng. Chuyển qua lại ít nhất 20 lần; ghi thời gian `Đánh dấu` và thời gian cảm nhận, không xuất Word lại. Đổi zoom 50/100/150/200%, cuộn, đổi kích thước cửa sổ và kiểm tra khung luôn đi theo chữ. Thử trang có nhiều font, header/footer, ô gộp và văn bản lặp. Khi không ánh xạ chắc chắn, phải có thông báo và không tô vị trí khác. Sau sửa/hoàn tác, phải bỏ khung/index cũ, dựng lại và đánh dấu đúng nội dung mới. Ghi thời gian quét/dựng trang để so sánh cùng file/máy với 0.1.1.
5. Sửa `đo` ở trang hai; kiểm tra hai vị trí còn lại chưa thay đổi. Hoàn tác phải phục hồi đúng từ và số cảnh báo.
6. Sửa quốc hiệu: kiểm tra hoa, đậm, cỡ 13, font và căn giữa trong Word. Hoàn tác một lần phải phục hồi cả nội dung và định dạng.
7. Sửa khoảng trắng, lưu tên mới. Mở bản mới bằng Word, xác nhận không có màu highlight phát sinh; bảng, ngắt trang và các định dạng không liên quan còn nguyên. Kiểm tra SHA256 file gốc không thay đổi.
8. Thử mở file khác/đóng ứng dụng khi chưa lưu: chọn Không phải giữ phiên hiện tại; chọn Có mới bỏ bản sửa. Không được đóng phiên Word người dùng đã mở trước đó.
9. Thử tài liệu thực tế có ô gộp, nhiều font, trường tự động và nhiều trang. So sánh bản xem trước với Word trên cùng máy; ghi riêng các khác biệt, không khẳng định giống hệt trước khi so sánh.
10. Thử tài liệu có Track Changes, bảo vệ, file lỗi và file đang bị khóa. Kiểm tra thông báo lỗi, mở lại file hợp lệ và không để lại Word chạy ngầm sau khi thoát. Kiểm tra thư mục tạm `ChinhTa-*` được dọn sau phiên bình thường.
11. Tắt mạng sau khi build, chạy lại mở/quét/sửa/lưu để xác nhận offline. Thử trên bản sao tài liệu, không dùng duy nhất bản tài liệu gốc.

Ghi kết quả cùng phiên bản Windows, Word, .NET và cấu hình máy. Chỉ công bố các bước thực sự đã chạy.
