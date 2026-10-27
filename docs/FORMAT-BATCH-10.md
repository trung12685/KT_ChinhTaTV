# Đợt thể thức10 — khối ký sau đoạn40

Ngày27/10/2026 (UTC+7). Phát triển0.1.75 từ gói0.1.74 tại14ecf6a6; cụm đầu tiên sau gói này. Chưa đóng ZIP mới; danh mục33/8/1 giữ nguyên.

## Lỗi tái hiện

Bước ReadNativeParagraph chỉ đọc Page/InTable đầy đủ ở40 đoạn đầu của chế độ Nhà nước. Các đoạn sau có Page=0/InTable=false làm giá trị chưa quan sát. WordTextProof vẫn đọc/đối chiếu XML hiện tại toàn thân và nhận diện khối ký trong bảng cuối. SignatureRules lại yêu cầu thêm InTable=true nên loại khối ký ở cuối tài liệu dài trước khi đọc phông; đây là cờ chưa quan sát, không phải bằng chứng khối ký nằm ngoài bảng.

Ca hồi quy chạy trên mã0.1.74 chưa sửa tái hiện ở padding35, chế độ XML cũ, không ngắt mềm: XML nhận đúng2 vai trò nhưng bộ chọn quan sát trả0. Các kiểm chữ ký cũ truyền sẵn InTable thật cho mọi đoạn nên chưa bao phủ bước nối quan sát native này. Log âm tính cục bộ được giữ trong bằng chứng phát triển; không coi đây là lần Word thực chạy.

## Cụm sửa

- C31 dùng Scope.Signature/vai trò được chứng minh từ bảng cuối của XML hiện tại đã khớp range Word. Cờ InTable chưa đọc sau đoạn40 không phủ định bằng chứng này.
- Vẫn phải có đúng một họ tên,1–3 dòng chức vụ và quyền sửa chữ/phông hiện tại của từng vai trò. Thiếu/mismatch trước tên phải đóng cả nhóm. Không suy vai trò từ tên, kiểu chữ hoặc ordinal.
- Không tạo lại Page/InTable, không thêm đọc COM toàn tài liệu và không mở rộng geometry/header/Line sang sau40. Các mẫu bảng gộp/lồng/control/field, tên/chức vụ không rõ và mọi guard hiện có tiếp tục giữ thủ công.
- Mở rộng bài Word mẫu khối ký: chạy cả mẫu ngắn và mẫu có60 đoạn nội dung trước bảng cuối, vẫn kiểm sửa riêng/chung ba Line, đúng tên/cấp bậc/cỡ13–14/gạch chân/bố cục/ảnh, rollback, một Undo và lưu/mở.

## Kiểm chứng

Hồi quy Core đi qua WordTextProof → Apply → SignatureRules tại các ranh giới0/35/39/40/41/60/150 đoạn, XML cũ/chia sẻ, Enter/ngắt mềm. Kiểm đủ hai vai trò và sửa xong quét lại, giữ giá trị native ban đầu, chặn quyền thiếu và mismatch ở tên.

Kiểm Windows dùng chính ReadNativeParagraph/NativeParagraphTraversal của desktop với range giả, rồi đối chiếu XML của DOCX mẫu thật sinh ra; không dựng sẵn Outline. Chạy cả hai chế độ trên mẫu ngắn/dài, schema/ảnh và nhóm phông; xác nhận các đoạn sau40 không phát sinh đọc thông tin trang/bảng. Đây là kiểm luồng quan sát với COM giả, không phải Word thực.

CI biên dịch bài Office dài mới; số kiểm/commit/run đúng xem PR/CI. Chưa có xác nhận toàn bộ Office0.1.75 hoặc0.1.74 đã đạt trên máy người dùng. Bộ đọc thử nghiệm vẫn tắt và tối ưu tốc độ vẫn dừng. Gói0.1.74/main/tag cũ giữ nguyên; tiếp tục tích lũy5–10 cụm trước gói tiếp theo.
