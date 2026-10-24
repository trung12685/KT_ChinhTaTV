# Đợt phát triển sau 0.1.60

Tiếp tục ứng dụng hiện tại; prompt dành cho Claude chỉ là tài liệu nghiên cứu, không thay mục tiêu phát triển. Mốc đầu đợt là tag v0.1.60, commit `bd86e13f005fe41d3b9d068a317dd8a8795753ab`, gói đã phát hành với 2105 Core/OpenXML + 113 WPF và 1 kiểm metadata gói. CI không có Word.

Code mới dùng version **0.1.61**, chưa tạo gói/tag/release. Gom khoảng 5–10 bước có kiểm thử rồi chuẩn bị một gói; giữ nguyên bản .60 và main/stable .27.

## B01 — Cụm từ lặp chỉ khác chữ hoa đầu đoạn

| Trước | Sau khi duyệt |
|---|---|
| `Kế hoạch kế hoạch đã duyệt.` | `Kế hoạch đã duyệt.` |
| `Báo cáo báo cáo báo cáo.` | `Báo cáo.` |
| `  Kế  hoạch kế hoạch` | `  Kế  hoạch` nếu chỉ chọn lặp; khoảng chưa chọn giữ nguyên |

Mở rộng C13, không tăng số nhóm: vẫn 25 đề xuất/8 thủ công/1 minh họa. So sánh cụm 2–8 tiếng, cách nhau bằng ASCII space; đối chiếu Unicode NFC nhưng luôn giữ nguyên byte/ký tự của lần đầu, xóa đúng phần lặp theo CP nguồn. Chỉ cho phép ký tự đầu từ đầu tiên của đoạn viết hoa, bản lặp bắt đầu thường; phần còn lại vẫn khớp chính xác. Cho phép khoảng ASCII đầu đoạn nhưng giữ nguyên nếu chưa chọn lỗi khoảng trắng.

Không gộp hoa/thường giữa câu hoặc sau nhãn mục/tab/emoji; không xóa từ đơn như `Đều đều`, mã toàn hoa, hoặc khi chữ khác bên trong cụm không khớp. Không vượt dấu câu, ngắt đoạn/tab/NBSP. Bằng chứng XML vẫn bắt buộc; gợi ý cũ mất hiệu lực nếu quyền sửa hoặc ngữ cảnh đầu đoạn thay đổi. Đây là quy tắc ứng dụng cần duyệt, có thể lặp có chủ ý; không gán căn cứ pháp lý hoặc suy nghĩa.

Kiểm sửa riêng/chung cùng khoảng trắng, hai chấm, dấu kết câu/cuối đoạn và định dạng ký tự; giữ phần chưa chọn. Hậu kiểm nguyên văn, một lượt lưu và rollback toàn bộ khi lỗi. Bài Word có nguồn giả nhiều run, giữ kiểu đậm/gạch chân lần đầu, chọn riêng hai cụm, quét lại, một Undo, rollback, lưu/mở và băm nguồn; cần máy có Word để chạy thật, CI chỉ biên dịch.

Kiểm cục bộ đạt **2082 lượt assertion**, gồm 54 lượt bổ sung so với mốc .60 và các hồi quy cũ; đây là runner chọn lọc, không phải toàn bộ CI. Kết quả Windows đúng commit được ghi trong PR và Windows checks. Lỗi cảnh báo phông cơ quan/trích yếu sau sửa nhóm trên .56 vẫn chờ bằng chứng runtime để khép lỗi. Không dùng tính năng C13 mới để tuyên bố lỗi đó đã hết.

## Tiếp theo

Tiếp tục thể thức/lỗi chữ có điều kiện rõ, ngoại lệ và hồi quy; rà nhận diện và hiệu năng khi có số đo. Chính tả qua từ điển và ngữ nghĩa làm sau. Chỉ chốt gói đợt mới khi đủ bước và các kiểm bắt buộc đạt.
