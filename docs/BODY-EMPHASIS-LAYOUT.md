# C27–C29 — bố cục đoạn văn có từ nhấn mạnh

B02 sau .63, code .64 đang phát triển, chưa có gói Windows mới. Căn cứ NĐ30, Phụ lục I, Phần I, mục II.6, điểm e: nội dung căn đều; đầu dòng lùi 1 cm hoặc 1,27 cm; giữa đoạn tối thiểu 6 pt; giãn dòng từ dòng đơn đến 1,5 dòng. Nhấn mạnh trong câu không tự tạo lỗi căn/spacing.

Phạm vi nhận diện thân bài, XML chữ thuần, ranh giới tên loại/trích yếu và chữ ký/Nơi nhận không đổi. Mọi phông/đậm/nghiêng phải đọc chắc theo đúng text, đoạn có ít nhất một phần chữ thường đứng; có thể xen từ đậm/nghiêng. Đoạn toàn đậm/nghiêng hoặc chưa đọc chắc vẫn không cấp sửa bố cục. Không mở mục, bảng, field, thụt cả đoạn/hanging, căn giữa/phải hoặc mốc mở đầu không Kính gửi có nhấn mạnh.

Ví dụ: câu thường xen từ **đậm** và *nghiêng*, căn trái, dòng đầu 0 cm, Double, khoảng sau0pt/trước đoạn kế0pt. Đề xuất theo từng nhóm: C27 căn đều và đầu dòng1,27cm; C28 sau đoạn6pt; C29 Single. Mức thụt1cm/1,27cm đúng giữ nguyên; đủ khoảng hoặc Single/1,5 không báo lỗi. Các hạn chế đo gap gộp, auto/contextual, khác trang, grid, Exactly/AtLeast giữ nguyên.

Chọn riêng bố cục giữ phông/màu/cỡ kể cả đang sai, nguyên văn và từng đậm/nghiêng/gạch chân. Chọn chung C26–C29 và lỗi chữ hợp lệ cập nhật thuộc tính độc lập trên một snapshot gốc; giữ cỡ đúng13/14, chỉ sửa cỡ sai khi chọn C26. Không viết lại toàn bộ ParagraphFormat/Font, không thêm lượt COM để suy đoán cấu trúc.

Trước ghi, proof glyph thực tế cũng phải có chữ thường đứng, text và snapshot đoạn/spacing khớp; stale thì quét lại. Sau lưu/Line kiểm lại từng glyph còn lại, thuộc tính đã chọn và chưa chọn, XML/context/đoạn kế của gap và nét XPS thực. Quét mới bỏ mục đã sửa; lỗi chưa chọn còn có thể tích. Bất kỳ hậu kiểm/COM nào thất bại thì rollback cả lượt; một Undo, giữ tệp nguồn.

Core kiểm riêng từng thuộc tính, chung bốn nhóm/chữ và đổi thứ tự chọn; giữ từng kiểu/cỡ/gạch chân, stale/drift/neighbor, đoạn toàn nhấn mạnh/unknown/no-greeting. Writer giả hiện có buộc lỗi nếu chạm Font trong sửa đoạn/spacing. Fixture Word mới kiểm riêng căn/gap/line, chung bốn nhóm/chữ/baLine, Undo/rollback/lưu-mở/hash nguồn. Windows CI chỉ biên dịch fixture, chưa chứng minh Office2021 chạy đạt hoặc khép phản hồi phông cũ.
