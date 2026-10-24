# Chính tả Việt 0.1.63 — năm hạng mục thân bài sau .62

Bản thử tích lũy5 bước, tổng **29 nhóm đề xuất/8 thủ công/1 minh họa**. Không xác nhận toàn bộ NĐ30/chính tả hoặc Word/Office2021. Main/stable .27 và gói .62 giữ nguyên.

| Bước | Ví dụ trước → sau khi duyệt | Giới hạn chính |
| --- | --- | --- |
| B01/C26 | Arial đỏ cỡ12 xen14 → Times New Roman đen,12 về13 và giữ14 | Chỉ văn xuôi thường đủ proof; giữ glyph/gạch chân/căn đoạn |
| B02/C27 | Căn trái, dòng đầu0cm → căn đều,1,27cm | Giữ mức1cm/1,27cm đúng; giữ khoảng đoạn/giãn dòng/phông chưa chọn |
| B03/C28 | Hai đoạn liền nhau cùng trang: After0 + Before0 → After6pt | Xét cả hai phía, chỉ kết luận khi tổng chưa thể đạt6pt; không bắc qua dòng trống/mục/bảng/trang hoặc đoán gộp/auto/contextual |
| B04/C29 | Double hoặc Multiple ngoài1–1,5 → Single | Chỉ ghi rule, giữ pt/Before/After chưa chọn; Exactly/AtLeast/grid chờ đo |
| B05 | Có tên loại/trích yếu nhưng không có Kính gửi vẫn nhận thân bài khi hai đoạn mở đầu có proof | Hai mốc văn xuôi căn đều/thụt1cm hoặc1,27cm, kiểu chắc; có chữ ký/Nơi nhận; không dùng font chưa rõ/mục/nhấn mạnh làm mốc |

Chọn riêng giữ phần chưa chọn. Chọn chung C26/C27/C28/C29/chữ → đo lại XPS → Line nếu cần → hậu kiểm mới. Snapshot bất biến, mỗi patch chỉ đổi thuộc tính đã duyệt; thứ tự checkbox không làm mất kết quả nhóm trước. Hậu kiểm đủ chữ/glyph/cỡ/gạch chân/căn/thụt/spacing và đoạn kế. Một Undo, rollback cả lượt khi COM/hậu kiểm lỗi, giữ nguồn và chỉ lưu bản sao. Không coi số Shape hoặc marker DOCX là bằng chứng nét đúng.

## Cách thử tổng thể

1. Giải nén vào thư mục mới, mở chương trình và xác nhận phiên bản0.1.63. Mở bản sao tài liệu, chọn chế độ đúng rồi quét.
2. Thử cỡ12/14 trong đoạn văn xuôi, căn trái/đầu dòng0, khoảng0+0pt và giãn Double. Sửa riêng từng nhóm: nhóm đã đạt biến mất; lỗi chưa chọn phải còn chọn được.
3. Chọn nhiều nhóm kèm ba đường kẻ; xem tiến độ, kiểm nội dung/định dạng/vị trí nét. Thử một Undo, lưu bản sao và mở lại, xác nhận nguồn giữ nguyên.
4. Với văn bản không Kính gửi: cần tên loại/trích yếu xác định, hai đoạn mở đầu đã căn đều/thụt1cm hoặc1,27cm, không nhấn mạnh và có chữ ký/Nơi nhận. Mẫu chưa đủ điều kiện được bỏ qua, không coi việc không có lỗi là xác nhận đúng.
5. Giữ trường số/ngày trống, tiêu đề/danh sách/bảng/field và các phần chưa chọn. Exactly/AtLeast, khoảng đoạn gộp/auto/contextual hoặc grid chưa đo không tự kết luận sai.

CI Windows kiểm Core/OpenXML/WPF, biên dịch ứng dụng và metadata báo cáo trong gói. **Không cài/chạy Word**; fixture Word chỉ biên dịch. Chưa có bằng chứng runtime .63 trên Office2021 hoặc khép phản hồi phông cũ. Cần chạy bài Kiểm tra Word tự động trên máy có Word đúng phiên bản và đối chiếu bằng mắt.

Release kèm ZIP Windows x64 tự chứa .NET, TXT tiến độ có ví dụ, SHA256.txt, build-checks.json và ảnh minh họa. Tổng kiểm/commit/run thực tế do pipeline bổ sung ở release; chỉ có bản tải khi pipeline phát hành đúng commit đạt.

[Danh sách chức năng](https://github.com/trung12685/KT_ChinhTaTV/blob/v0.1.63/docs/SUPPORTED-FIXES-0.1.63.md) · [Nhật ký](https://github.com/trung12685/KT_ChinhTaTV/blob/v0.1.63/docs/BATCH-AFTER-0.1.62.md) · [Không Kính gửi](https://github.com/trung12685/KT_ChinhTaTV/blob/v0.1.63/docs/BODY-OPENING.md).
