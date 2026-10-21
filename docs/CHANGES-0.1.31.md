# 0.1.31 — Định dạng số và ngày tháng (bản thử)

Tiếp nối tag 0.1.30 (`2186d4daa8c5ffb26e13a30a84f2ae4d1861405e`). Bổ sung sửa định dạng ở phần đầu Nhà nước đã nhận diện chắc chắn. Bản ổn định 0.1.27 và các tag/gói cũ giữ nguyên; các bản thử chưa có bằng chứng Office 2021 riêng.

## Phần bổ sung

- Dòng số/ký hiệu: Times New Roman, màu đen, cỡ 13, chữ đứng.
- Địa danh/ngày tháng: Times New Roman, màu đen, cỡ 13–14, chữ nghiêng. Giữ cỡ hiện tại khi hợp lệ; ngoài khoảng đề xuất 13, không ép mọi dòng về cùng cỡ.
- Sửa định dạng giữ nguyên chữ, ô chưa điền, đậm/gạch chân và căn đoạn. Có thể chọn riêng hoặc cùng các gợi ý cách ghi số/ngày trong một lượt, một Hoàn tác.
- Dòng phải là chữ thuần đã đối chiếu XML, mẫu rõ, font/cỡ/nghiêng/màu quan sát đồng nhất. Định dạng trộn, trường Word, tab thật, control/link, ngày không hợp lệ hoặc mẫu chưa rõ vẫn duyệt thủ công.

Căn cứ đọc lại: NĐ30/2020/NĐ-CP, Phụ lục I, Phần I.II.3.c và 4.c, PDF 23; phông/màu tại I.I.4. Vị trí và căn giữa tương đối chưa tự sửa. Bản này không kết luận đậm/gạch chân hiện có là đúng thể thức và chưa xác minh mã được cấp, địa danh chính thức, mọi ngoại lệ hay sửa đổi pháp lý sau bản nguồn.

## Giữ an toàn khi sửa nhóm

Định dạng chỉ thay thuộc tính ký tự, thực hiện trước các đoạn sửa chữ nhỏ. Chỉ cho phép gợi ý chữ nằm trọn trong dòng định dạng dùng chung lượt; các chồng lấn khác vẫn chặn. Không tự chọn thêm gợi ý chưa được chọn.

Sau lưu, kiểm toàn bộ chữ theo tập đã chọn và kiểm lại phông/cỡ/nghiêng/màu của dòng vừa định dạng. Chưa đạt thì khôi phục cả lượt theo giao dịch 0.1.30; phục hồi lỗi giữ checkpoint và nguyên nhân. Quan sát và sửa không gồm dấu kết thúc đoạn/ô, tránh font riêng của dấu đoạn gây cảnh báo còn sót.

Giữ luồng Line, marker DOCX, nét XPS thật, dòng tham chiếu, bù vị trí và rollback Line. Không báo thành công chỉ từ số Shape hoặc hết cảnh báo.

## Kiểm thử và dùng thử

Core bổ sung test cỡ hợp lệ/sai, chữ trộn, trường trống, phạm vi/profile, sửa riêng/chung, chồng lấn hợp lệ và bị cấm, không đổi chữ ngoài vùng và rollback khi sửa/kiểm định dạng thất bại. Workflow Windows chạy toàn bộ Core/OpenXML + WPF và publish trước prerelease; CI không có Word.

Giải nén vào thư mục mới, giữ `samples`. Chạy **Kiểm tra → Kiểm tra Word tự động…**: bài mới tự tạo `typography-input.docx` từ mẫu giả, thử sửa riêng, sửa chung, giữ đậm/gạch chân/căn đoạn, lưu/mở lại, một Undo và lỗi giả lập sau định dạng. Các bước này còn chờ máy Word; sự có mặt của kịch bản không phải bằng chứng đã chạy đạt.

Có thể mở bản sao `typography-input.docx` trong thư mục kết quả để thử giao diện. Gói đã bao gồm sửa số/ngày/ô trống và giao dịch phục hồi của 0.1.28–0.1.30. Tập trung thử gói mới, gửi `office-check.txt` và log đúng phiên bản nếu chưa đạt. Chưa nhập main trước kết quả dùng thử.
