# Chính tả Việt 0.1.64 — giữ nhấn mạnh, giảm xử lý lặp và khoảng trắng ngoặc kép

Gom năm hạng mục sau .63, tổng **30 nhóm đề xuất/8 thủ công/1 minh họa**. Đây là bản thử; chưa xác nhận chạy đạt trên Office2021 hoặc hoàn thành toàn bộ thể thức/chính tả. Main/stable .27 và gói .63 giữ nguyên.

| Bước | Ví dụ hoặc hành vi mới | Giới hạn chính |
| --- | --- | --- |
| B01 | Arial đỏ12 ở từ đậm xen14 ở từ nghiêng → Times New Roman đen13/14, giữ từng kiểu/cỡ đúng | C26 chỉ trong vùng thân bài đã xác minh; không ép cỡ cả vùng, giữ gạch chân/căn/spacing |
| B02 | Đoạn văn thường xen từ nhấn mạnh vẫn được đề xuất căn/thụt/khoảng đoạn/giãn dòng sai | Có chữ thường đứng và dữ liệu chắc; giữ từng kiểu/cỡ, không sửa bố cục đoạn toàn đậm/nghiêng hoặc mốc mở đầu chưa chắc |
| B03 | Một lần nhận diện thân bài cho C26–C29 trong mỗi lần quét thay ba lượt trùng | Giữ nguyên lỗi/thứ tự/payload/căn cứ/adjacency; không cache qua sửa/lưu/Line, chưa đo tốc độ toàn phiên Word |
| B04 | `“ nội dung báo cáo ”` → `“nội dung báo cáo”` | C30 chỉ xóa mép ASCII trong cặp cong cân bằng/lời văn rõ; giữ dấu/chữ/kiểu và ô trống |
| B05 | `“ Đã hoàn thành. ”` → `“Đã hoàn thành.”`, tương tự ? và ! | Giữ một dấu gắn cuối câu rõ; chuỗi dấu, viết tắt, nhiều câu hoặc khoảng trước dấu chưa mở |

Chọn từng lỗi, từng nhóm hoặc nhiều nhóm: chữ/phông/căn/spacing → số đo XPS mới → Line nếu cần → hậu kiểm. Mỗi thuộc tính độc lập; phần chưa chọn được giữ. Sửa xong phải quét lại và bỏ mục đã đạt. Một Undo cho cả lượt, lỗi COM/lưu/hậu kiểm thì phục hồi cả lượt. Không báo thành công từ số Shape hoặc dấu nhận diện DOCX.

## Cách thử tổng thể

1. Giải nén vào thư mục mới, kiểm phiên bản0.1.64, mở bản sao tài liệu và chọn đúng chế độ.
2. Với đoạn thường có từ đậm/nghiêng, thử Arial đỏ12 xen cỡ14; sửa riêng phông, kiểm giữ từng kiểu/cỡ đúng và lỗi chữ chưa chọn. Thử căn/thụt/gap/line riêng rồi chọn chung; nhóm đã sửa phải biến mất, nhóm chưa chọn còn tích được.
3. Thử `“  kế hoạch kế hoạch đã duyệt.  ”` kèm khoảng chữ và dấu phẩy/chấm phẩy: chọn một mép giữ phía kia, chọn nhiều nhóm sửa chung. Giữ nguyên dấu kết, kiểu/cỡ/gạch chân. Mẫu `“    ”` giữ độ rộng; mã/ngoặc lồng/thiếu dấu không bị C30 tự xử lý.
4. Chọn chung với ba đường kẻ, kiểm tiến độ và nét thực; thử một Undo, lưu bản sao/mở lại và giữ tệp nguồn. Tài liệu mẫu Bao cao.docx cần kiểm cơ quan/trích yếu đúng không sinh cảnh báo giả sau sửa nhóm.
5. Chạy Kiểm tra Word tự động trên máy có Word/Office2021. Nếu lỗi, giữ office-check.txt, diagnostics.log và các bản xuất của đúng phiên bản; đối chiếu thêm bằng mắt. Các bài mới có trong code nhưng CI chỉ biên dịch.

CI kiểm Core/OpenXML/WPF, biên dịch ứng dụng và kiểm metadata báo cáo trong gói. **CI không cài/chạy Word**. Tổng kiểm/commit/run thực tế do pipeline bổ sung tại release và build-checks.json; không suy ra Office2021 PASS từ số kiểm hoặc việc đóng gói.

Release kèm ZIP Windows x64 tự chứa .NET, TXT tiến độ có ví dụ trước/sau và việc tiếp theo, SHA256.txt, build-checks.json và bốn ảnh minh họa. [Danh sách chức năng](https://github.com/trung12685/KT_ChinhTaTV/blob/v0.1.64/docs/SUPPORTED-FIXES-0.1.64.md) · [Nhật ký](https://github.com/trung12685/KT_ChinhTaTV/blob/v0.1.64/docs/BATCH-AFTER-0.1.63.md) · [Ngoặc kép](https://github.com/trung12685/KT_ChinhTaTV/blob/v0.1.64/docs/QUOTE-SPACING.md).
