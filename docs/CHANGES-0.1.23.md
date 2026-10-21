# 0.1.23 — sửa nhóm Line, độ dài 3/7 và trường trống

1. Sửa nhóm Line: dùng từng thao tác đơn đã được kiểm chứng trong một giao dịch chung, thứ tự Agency → Subject → Motto. Mỗi bước vẫn kiểm chữ, dấu DOCX, nét XPS và các nét đã có. Nếu bước sau lỗi, khôi phục cả nhóm về checkpoint ban đầu; không để lại kết quả một phần. Thành công chỉ thêm một mục Hoàn tác. Bộ ba cùng lúc trước đây không đi qua nhánh xử lý một Motto mất nét của 0.1.22. Không nhận được log mới kèm yêu cầu bốn mục; chẩn đoán này dựa trên mã và các log đã có.

2. Line Agency/Subject: giữ khoảng chấp nhận 1/3–1/2 dòng tham chiếu, có dung sai đo hiện hành. Đích khi tự tạo/đặt lại chuyển từ 5/12 sang 3/7. Tiêu ngữ vẫn bằng chiều dài dòng chữ. 3/7 là lựa chọn tạo nét của người dùng, không gán thành tỷ lệ pháp lý bắt buộc. Đường hợp lệ ở 1/3 hoặc 1/2 không bị buộc đổi sang 3/7.

3. Quét lại trích yếu: thông báo bổ sung xác định đây là cảnh báo lệch tâm trên số đo dòng cuối 228,5 pt, không chỉ là một mục UI cũ. Bổ sung đo lại dòng chữ ngay trong XPS candidate và áp dụng cùng bộ kiểm như lượt quét mới. Nếu số đo đã đổi sau khi Word lưu/mở, cập nhật đích tâm/chiều dài theo dòng cuối mới trong lần bù có giới hạn; bắt buộc xác minh lại candidate thứ hai. Nếu vẫn không đạt thì rollback thay vì báo thành công rồi để lại cảnh báo. Phép tái tính và phép bù vị trí được gộp trong cùng một candidate thứ hai; không thử thêm lần ba.

Về danh sách: theo phản hồi cụ thể, sửa chung đường 1 và 3 đã tạo được đường 3 nhưng vẫn để lại mục cảnh báo của đường 3. Xóa lựa chọn/danh sách cũ trước khi dựng kết quả mới. Giữ kết quả sửa Line đã xác minh trong phiên và chỉ loại mục sau khi đối chiếu lại nét XPS mới cùng dòng tham chiếu hiện tại, đúng vai trò/vị trí/nội dung. Lịch sử này bị xóa khi mở/hoàn tác/sửa chữ hoặc định dạng; không lọc mù theo lỗi đã bấm sửa. Một nét riêng do ứng dụng tạo, ghép duy nhất và có XPS khớp cũng không bị cờ đối tượng chưa ghép ở nơi khác biến thành cảnh báo thủ công. Nếu nét hiện tại lệch, tham chiếu thay đổi hoặc chưa đo được thì vẫn báo.

4. Khoảng trắng: bỏ qua riêng vùng điền trống sau “Số:” trước /mã văn bản và sau “ngày” trước “tháng … năm …” trong dòng ngày tháng dạng mẫu. Nếu đã có số/ngày thì vẫn báo khoảng trắng dư. Khoảng trắng khác trong cùng dòng hoặc nội dung vẫn được kiểm tra.

Kiểm thử gồm nguyên tử cả nhóm, lỗi ở bước ba và lỗi rollback, điểm tạo 3/7 và hai biên hợp lệ, các mẫu trường trống/đã điền cùng offset tiếng Việt. WPF kiểm việc nét đã đúng không còn bị báo thủ công bởi đối tượng không liên quan. Office check bổ sung sửa cả ba cùng lượt, quét lại không còn cảnh báo Line và Hoàn tác một lần.

Kiểm thử/build không thay thế xác nhận trên Word thật. Chưa có bằng chứng bản 0.1.23 chạy đạt toàn bộ bốn tình huống trên Office 2021 của người dùng. Giữ kiểm nét thực tế và rollback; tài liệu riêng không được đưa lên GitHub.
