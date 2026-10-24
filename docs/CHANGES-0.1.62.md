# Chính tả Việt 0.1.62 — năm hạng mục sau .61

Đợt này hoàn thiện sửa định dạng cỡ hỗn hợp, viết hoa giữ kiểu và mục số nhiều cấp, cùng khoảng cuối sau chấm phẩy. Tổng25 nhóm đề xuất/8 thủ công/1 minh họa; không phải xác nhận toàn bộ chính tả hoặc NĐ30. Không nâng main/mốc ổn định .27, không ghi đè gói .61.

| Bước | Trước → sau khi duyệt | Giữ lại/giới hạn |
| --- | --- | --- |
| B01 | Trích yếu cỡ12/13/14 →13/13/14 | Chỉ sửa cỡ sai, giữ từng cỡ đúng; cơ quan12–13, tên loại/trích yếu/ngày13–14 cần nhận diện/proof chắc |
| B02 | Quốc hiệu10/12/13 →12/12/13; tiêu ngữ12/13/14 →13/13/14 | Khi giữ nguyên chữ; giữ HÒA/HOÀ và dấu nối được chấp nhận, không mở thay chữ qua Line/field/object |
| B03 | Báo cáo13/14 →BÁO CÁO | Giữ từng cỡ, gạch chân và phông/màu/kiểu/căn đoạn chưa chọn. Chọn chung sửa phông thì chỉ đổi thuộc tính đã duyệt |
| B04 | Dưới mục1:1.1.,1.2.,1.4. →1.1.,1.2.,1.3. | Chỉ đổi số cuối, giữ tiền tố/tab; cha trực tiếp duy nhất và2–4 cấp rõ; cascade/ref/gợi ý cũ không đủ bằng chứng giữ thủ công |
| B05 | `Nội dung đã duyệt;   ` →`Nội dung đã duyệt;` | Chỉ xóa dấu cách ASCII cuối; giữ dấu, phần chưa chọn, kết đoạn/ô và trường số/ngày trống |

Luồng sửa vẫn duyệt mục đã chọn → chữ/phông → đo XPS mới → căn/Line nếu cần → hậu kiểm. Một nét đúng không được che cỡ/kiểu bị đổi. Một Undo, nguyên văn, phạm vi phần chưa chọn và rollback toàn bộ khi thất bại được giữ. Không báo đạt chỉ từ số Shape hoặc dấu nhận diện DOCX.

## Cách kiểm tra tổng thể

1. Mở bản sao tài liệu, xác nhận thanh tiêu đề0.1.62; quét và xem tooltip ngắn/chi tiết.
2. Thử sửa riêng một mục; quét lại phải bỏ mục đã đạt và giữ mục chưa chọn. Sau đó chọn nhiều nhóm, kiểm tiến độ và kết quả mới.
3. Thử cỡ sai trộn đúng, viết hoa tên loại có gạch chân/cỡ13–14, mục nhiều cấp và dòng kết thúc bằng chấm phẩy có khoảng dư. Những chỗ đúng hoặc chưa đủ bằng chứng không được tự thay.
4. Kiểm ba đường kẻ bằng mắt, giữ đích3/7 cho cơ quan/trích yếu; tiêu ngữ theo bề rộng chữ. Thử một Undo, lưu bản sao rồi mở lại, giữ nguyên file gốc.
5. Có lỗi thực tế: giữ diagnostics.log và file kiểm tra ở máy riêng; bài Kiểm tra Word tự động mới cần chạy trên máy có Word.

CI chạy Core/OpenXML/WPF, build và kiểm metadata gói, nhưng **không cài/chạy Word**. Các fixture Word mới chỉ được biên dịch; chưa có bằng chứng runtime để khép phản hồi phông Office2021 cũ. Không tự sửa đầy đủ thân bài/chữ ký/lề/số trang, Word auto-list, thư viện chính tả hoặc ngữ nghĩa.

Gói phát hành kèm ZIP, SHA256.txt, build-checks.json, báo cáo TXT có ví dụ và hướng dẫn. Số kiểm/commit/run thực tế được pipeline bổ sung ở release; link có gói chỉ khi phát hành đúng commit đạt.

[Danh sách đầy đủ](https://github.com/trung12685/KT_ChinhTaTV/blob/v0.1.62/docs/SUPPORTED-FIXES-0.1.62.md) · [Nhật ký](https://github.com/trung12685/KT_ChinhTaTV/blob/v0.1.62/docs/BATCH-AFTER-0.1.61.md) · [Giữ cỡ](https://github.com/trung12685/KT_ChinhTaTV/blob/v0.1.62/docs/SELECTIVE-SIZE-REPAIR.md) · [Viết hoa giữ kiểu](https://github.com/trung12685/KT_ChinhTaTV/blob/v0.1.62/docs/CASE-PRESERVATION.md) · [Mục nhiều cấp](https://github.com/trung12685/KT_ChinhTaTV/blob/v0.1.62/docs/HIERARCHICAL-OUTLINE.md).
