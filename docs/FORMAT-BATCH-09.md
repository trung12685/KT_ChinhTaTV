# Đợt thể thức09 — bằng chứng đối chiếu Word/XML cùng lần đọc

Ngày27/10/2026 (UTC+7). Tiếp đợt08 tại1f17ab28; cụm thứ năm kể từ0.1.73, chuẩn bị gói thử0.1.74. Danh mục33/8/1, bộ đọc cũ mặc định, công việc tối ưu tốc độ vẫn dừng.

## Vấn đề cần chẩn đoán

Nhật ký người dùng từng có XML-COUNT và XML-TEXT. Mã/số cho biết ranh giới không khớp nhưng không chứa XML hiện tại và các range Word tương ứng. DOCX checkpoint chỉ phản ánh lần lưu gần nhất, có thể khác WordOpenXML trong bộ nhớ; dùng file lưu để đoán nguyên nhân dễ nhầm. Chưa đủ dữ liệu để kết luận nguyên nhân mọi thông báo cũ hoặc mở rộng quyền sửa.

## Bằng chứng mới

Chỉ phiên **Kiểm tra Word tự động** trên các mẫu giả mới bật thu bằng chứng khi đối chiếu báo không khớp hoặc không đọc được. Một file mapping-evidence-legacy-01.json (hoặc experimental) chứa đồng thời:

- WordOpenXML hiện tại lấy từ chính lần CheckCore, cùng toàn bộ range Word Start/End/Text/Page và danh sách prefix đầu đã đọc.
- Tên mẫu, chế độ đọc; các Start đã được phép sửa chữ/phông và mã cảnh báo cố định. XML không đọc được ghi null và cờ Unavailable, không lấy XML từ checkpoint thay thế.
- Phiên bản cấu trúc EvidenceVersion=1 để có thể phân tích/tái chạy phép đối chiếu ngoài Word.

Mỗi bộ đọc giữ tối đa4 file trong vòng luân phiên riêng. Lần lỗi liên tiếp không đổi không viết lại; file đã mất được tạo lại. XML và các range nằm trong một JSON, ghi tạm rồi đổi tên để không ghép nhầm hai lần đọc. Tên file mới được ghi ở dòng MAPPING_EVIDENCE trong office-check.txt. Khi lỗi ghi bằng chứng, ghi chẩn đoán OfficeCheck.MappingEvidence.Capture; lỗi đối chiếu và quyền sửa vẫn giữ nguyên.

Không gọi thêm Word, không Save/close, không đọc file nguồn và không sửa trạng thái tài liệu/Undo. Phiên mở tài liệu thông thường không materialize hoặc ghi nội dung chẩn đoán này. Không thay thuật toán mapping, điều kiện định dạng, nét XPS, rollback hoặc bộ đọc.

## Kiểm chứng

Ca sinh có mismatch thật được lưu rồi tái chạy WordTextProof: danh sách quyền chữ/phông và đúng mã/ranh giới lỗi phải giống nhau. Kiểm Unicode, CP thật, body/prefix riêng, không ghi khi tắt/không lỗi, chống ghép XML/range khác lượt, lặp không viết, khôi phục file mất, vòng4file/mỗi chế độ, XML unavailable, lỗi riêng phần bảng và lỗi I/O rồi ghi lại. Kiểm toàn bộ tích lũy trước đóng gói; số kiểm/commit/run đúng nằm trong build-checks.json/TXT/CI.

Đây là bổ sung chẩn đoán cho bài kiểm mẫu, không phải sửa hết thông báo XML cũ hoặc thêm quyền định dạng. CI không chạy Word; cần kết quả Office thực trên máy người dùng để xác nhận toàn bài. File người dùng cũ không đưa vào source/gói.
