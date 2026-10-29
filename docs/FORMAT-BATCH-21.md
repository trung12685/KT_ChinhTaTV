# Đợt21 — hậu kiểm toàn cụm số PAGE đầu trang

Mã phát triển0.1.77, tiếp đợt20. Không ghép chữ số chỉ bằng cách sắp trái–phải rồi nối chuỗi. Toàn bộ số đầu trang phải là một cụm duy nhất, cùng dòng nền/chiều cao, không chồng và khoảng ngang hữu hạn; vẫn phải đúng giá trị vật lý, giữa lề trên, đúng phông/cỡ/đen/đứng, hiển thị trọn và ẩn trang đầu.

Dùng cùng phép ghép hình học với PAGE nguồn cuối trang. Kiểm WPF số đầu trang tách cụm trên12/101trang; cố ý lệch dòng, cách xa hoặc chồng chữ số phải bị từ chối dù nối chuỗi cho ra số đúng. Giữ hậu kiểm nội dung khác và rollback. CI không chạy Word thực; chưa ZIP0.1.77.
