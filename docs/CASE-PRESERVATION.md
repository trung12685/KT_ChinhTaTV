# Viết hoa tên loại và giữ định dạng từng ký tự

## Phạm vi B03 trong code .62

C24 áp dụng cho tên loại thuộc danh mục đã có, nhận diện chắc trong khối đầu Nhà nước và có proof sửa chữ. Ví dụ `Báo cáo` có ký tự cỡ13/14 và gạch chân có thể đổi thành `BÁO CÁO` mà giữ từng cỡ/gạch chân. Cỡ khác nhau nhưng đều hợp lệ không còn tự chặn viết hoa. Font/cỡ/kiểu đã đọc chưa chắc, mất proof, field/Line/drawing chỉ có quyền định dạng hoặc khối trích yếu chưa rõ vẫn chặn sửa chữ. Không đổi tên chưa biết, viết thường trích yếu, sửa tên bản sao theo mẫu văn bản gốc hoặc đoán từ ngữ cảnh.

| Chọn | Kết quả yêu cầu |
| --- | --- |
| Chỉ viết hoa | Giữ cả phông/màu/kiểu/cỡ chưa chọn sửa, kể cả chúng đang sai |
| Viết hoa + sửa phông, cỡ13/14 đều hợp lệ | Sửa phông/màu/kiểu được duyệt, giữ từng cỡ13/14 và gạch chân |
| Viết hoa + sửa phông/cỡ12/14 | Chỉ đưa12 về13 theo B01, giữ14 và gạch chân |
| Chọn Line trích yếu có phụ thuộc | Checkbox tích kèm đúng viết hoa/phông cần trước; duyệt, sửa, đo XPS mới rồi kẻ/đo lại |

## Cơ chế bảo toàn

Một vùng theo dõi là toàn paragraph đã proof XML, bao gồm khoảng trắng ngoài tên. CasePreservationPlan lấy Start/End Word thật, không dùng vị trí chuỗi toàn thân làm CP. Vùng theo dõi không trở thành Issue được sửa và không thêm vào số mục duyệt/tiến độ. Các edit vẫn là đúng ký hiệu chữ hoặc CharacterFormat đã chọn.

Trước ghi, ReadStyles đọc từng Characters range thực, đối chiếu toàn chuỗi và biên liên tiếp nằm trọn trong paragraph. Chỉ ký tự nhìn thấy cần style; dấu cách không lấy kiểu của nó để suy cho chữ. Chụp font, cỡ, đậm/nghiêng, màu, gạch chân và alignment/ba thụt lề. Thuộc tính chưa đọc chắc hoặc COM lỗi không biến thành bằng chứng hợp lệ. Không có phép đọc Shape.AlternativeText/ID/Type mới.

Nếu chọn kèm CharacterFormat đúng toàn paragraph tên loại, tính trước các thuộc tính được duyệt: Times New Roman/đen/đậm/đứng và chính sách cỡ đồng nhất/giữ cỡ/sửa riêng cỡ sai. Gạch chân và căn/thụt đoạn giữ nguyên. Nếu không chọn sửa font thì toàn style nhìn thấy giữ nguyên. RetainedGlyphProjection dùng chung với FontSizeEvidence để chiếu qua các chữ giữ lại hoặc chỉ đổi hoa/thường, bằng prefix Word thật. Ký tự mới không nhận style nguồn được đoán; hậu kiểm vẫn cần đọc style thực tế đầy đủ.

Backend đổi hoa bằng Range.Case; không gán lại toàn dòng bằng Range.Text. Theo dõi Range qua các edit CP khác, kiểm nguyên văn và fingerprint sau lưu. Với nhiều nhóm, lấy lại paragraph hiện tại theo text/prefix đã tính và proof XML sau mở lại, kiểm tiếp sau Line. Cỡ14 bị ép thành13, mất gạch chân, đổi phông/màu/kiểu chưa duyệt hoặc đổi căn/thụt đoạn đều thất bại và rollback cả lượt, dù text/nét đang đúng. Một Undo và nguồn gốc vẫn giữ như luồng cũ. Đen RGB/đen tự động và casing tên font được đối chiếu tương đương để tránh cảnh báo giả.

Hậu kiểm mới bao gồm các thuộc tính đã liệt kê, chưa đối chiếu mọi thuộc tính OpenXML. Không mở thêm phạm vi thân bài/chữ ký/header/footer/textbox. Không có cache style qua sửa/Undo/mở file; chỉ đọc sâu vùng tên loại ngắn đang được chọn viết hoa. Chưa có benchmark Word thực tế.

## Kiểm chứng

Core có toàn danh mục/bổ sung/alias, cỡ hợp lệ trộn, padding/CP, chọn riêng/chung, nguồn thiếu/stale, từng thuộc tính drift, role/patch không khớp và rollback; projection cỡ cũ vẫn chạy hồi quy. Desktop đọc các actual-range giả với emoji/Unicode tổ hợp, bounds/coverage, COM errors, paragraph layout, checkbox và đo glyph/nét thật. Một nét đã đạt vẫn không được che cỡ title bị sai. OfficeCaseStyleCheck dùng ba mẫu giả đúng/phông sai/cỡ sai hỗn hợp, kiểm riêng chữ/phông, phụ thuộc Line, giữ phần chưa chọn, lỗi giữa lượt, một Undo/lưu-mở/băm nguồn. CI chỉ biên dịch fixture Word; không xác nhận Office2021 hoặc khép lỗi phông cũ bằng CI. Hiện .62 có ba hạng mục, chưa đóng gói Windows.
