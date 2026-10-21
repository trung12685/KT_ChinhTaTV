# 0.1.21 — phản hồi vị trí từ nét XPS

Bằng chứng đã có:
- Bài Kiểm tra Word 0.1.20 đã PASS trên Word 16.0 build 16.0.20326 của người dùng. Ba Line trong DOCX mẫu khớp nét XPS ở các tọa độ đã ghi; có bằng chứng lưu/mở/hoàn tác trên mẫu.
- Tài liệu riêng vẫn thất bại kiểm nét Agency: yêu cầu (145.17,92.68,70.90) pt, thấy (230.20,149.35,70.85) pt. Lệch 85.03/56.67 pt, tương ứng 30/20 mm.
- DOCX riêng có lề trái 30 mm, trên 20 mm và compatibilityMode=15; mẫu cũ không có settings tương ứng. Điều này chưa chứng minh thiết lập cụ thể nào gây dịch vị trí. Không thay chế độ tương thích hay định dạng tài liệu để né lỗi.

Luồng sửa mới:
1. Xuất nét XPS trước khi thay candidate, giữ làm nền đối chiếu.
2. Ghi candidate và kiểm chữ, dấu DOCX, vị trí nét XPS như trước.
3. Nếu chỉ lỗi vị trí và ghép được duy nhất các nét mới theo thứ tự cao độ/độ dài sau khi loại nét nền, rollback candidate đầu và tính một lần bù tọa độ bằng sai lệch thực đo. Không sử dụng số lề cố định. Nét thiếu/thừa, sai chiều dài, cao độ trùng hoặc dữ liệu không hữu hạn sẽ bị từ chối.
4. Ghi candidate thứ hai từ checkpoint gốc; kiểm lại chữ và nét XPS theo tọa độ đích ban đầu. Chỉ thành công khi phép kiểm này đạt. Nếu không đạt, rollback. Không lặp tiếp.

Tọa độ đích của Line đã bù được lưu trong thuộc tính title chuẩn của docPr với tiền tố riêng, để quét lại vẫn so với vị trí mong muốn trên trang; title không tự chứng minh đúng, luôn cần nét XPS. Không đọc Shape COM getters. Một lượt sửa thành công vẫn chỉ thêm một checkpoint hoàn tác.

Bài Kiểm tra Word bổ sung mẫu compatibilityMode=15, đo chữ bằng cùng LineInspection với UI rồi sửa, xác minh XPS, lưu/mở lại và hoàn tác. Mẫu không chứa nội dung tài liệu người dùng. Kiểm thử hồi quy mô phỏng chính sai lệch đã thấy, các trường hợp mơ hồ và rollback khi lần hai vẫn sai. Kiểm tra riêng DOCX người dùng được chạy cục bộ; không đưa tài liệu, nội dung hoặc metadata nhận dạng lên GitHub/gói tải.

GitHub Actions chạy kiểm thử Core/OpenXML và build Windows x64; không có Word desktop. Chưa có bằng chứng 0.1.21 chạy đạt trên tài liệu riêng với Office 2021. Cần xác nhận thực tế bằng bản này; không coi build/kiểm thử mô phỏng là bằng chứng đó.
