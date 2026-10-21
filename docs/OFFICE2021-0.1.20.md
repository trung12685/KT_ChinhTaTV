# 0.1.20 — bỏ đọc từng Shape qua COM; Office 2021 chưa được xác minh

Log mới xác nhận 0.1.19 vẫn lỗi COMException 0x80004005 tại VerifyPackageLines dòng 275, khi đọc Shape.ID hoặc Shape.Type. Dòng này gồm hai getter nên log chưa chỉ ra getter nào thất bại. Lỗi xảy ra trước XPS; không phải bằng chứng nét đã đúng hoặc sai vị trí.

0.1.20 bỏ toàn bộ truy cập Document.Shapes/Shape.* khỏi WordSession, gồm xác minh, quét lại và chẩn đoán. Không còn giả định docPr ID bằng COM Shape.ID. Nhận diện bằng cấu trúc DrawingML và dấu DOCX; kiểm kê Line theo đoạn XML đã đối chiếu với snapshot đoạn Word. Dùng ID chỉ trong gói DOCX, giữ Line không có dấu để tránh thêm trùng. Line/group/VML hoặc neo chưa ánh xạ được sẽ chặn tự sửa khi không chắc chắn.

Tọa độ DOCX chỉ là tọa độ khai báo, không được coi là vị trí hiển thị. Sau mở candidate, vẫn đối chiếu chữ và bắt buộc xuất XPS để kiểm tra vị trí/chiều dài nét thực tế. Lỗi ở bất kỳ bước nào vẫn dùng CheckpointSwap rollback. Bài Kiểm tra Word tiếp tục xác minh XPS sau lưu/mở lại, bên cạnh kiểm tra hoàn tác.

Bổ sung hồi quy về kiểm kê không dùng COM, thứ tự ID, ánh xạ neo, Line không có dấu, tọa độ tương đối cột và rollback khi dấu hợp lệ nhưng nét XPS lệch.

GitHub Actions chạy bộ kiểm thử và đóng gói Windows x64. Runner không có Word desktop. Chưa có bằng chứng 0.1.20 chạy đạt trên Office 2021; 0.1.19 đã có bằng chứng thất bại như trên. Cần chạy lại Kiểm tra Word trên máy Office 2021. Nếu lỗi tiếp, gửi office-check.txt cùng failed-line-candidate.docx và failed-line-preview.xps từ bài kiểm tra mẫu; không cần gửi tài liệu công việc.
