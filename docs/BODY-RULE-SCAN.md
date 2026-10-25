# Quét chung các quy tắc thể thức thân bài

B03 sau .63 trong đợt .64; trạng thái gói theo release/metadata đúng commit. C26 phông/màu/cỡ, C27 căn/thụt và C28–C29 khoảng đoạn/giãn dòng có chung phạm vi nhận diện thân bài. Trước đây mỗi lần CheckCore gọi ba bộ kiểm, dẫn đến ba lần phân tích cùng khối tên loại/trích yếu, mốc mở đầu và ranh giới chữ ký/Nơi nhận.

BodyRuleScan.Check chụp danh sách đoạn một lần, gọi RepairTargets một lần rồi chạy ba bộ chính sách độc lập trên đúng tập đã chứng minh đó. Các builder nội bộ không tự nhận diện thêm vùng. API kiểm riêng vẫn nhận diện đầy đủ khi gọi độc lập. CheckCore dùng entry chung; không có cache cấp tài liệu/static hoặc quyền sửa được giữ qua lần gọi tiếp theo.

Ví dụ một đoạn Arial đỏ12, căn trái/thụt0, Double và gap0+0 vẫn có cùng bốn loại lỗi, cùng đề xuất/tooltip và snapshot nguồn. Thay đổi chỉ giảm nhận diện lặp, không gộp các lỗi thành một mục hoặc bắt buộc chọn cả nhóm. Thứ tự kết quả giữ C26 → C27 → C29 → C28; grouping/checkbox được giữ bởi luồng UI hiện có.

C28 vẫn xét từng cặp trong danh sách đoạn gốc, gồm cả đoạn trống/mục/bảng làm ranh giới. Không dùng danh sách văn xuôi đã lọc để coi hai đoạn xa nhau là liền kề. Mỗi bộ vẫn kiểm riêng glyph/font/paragraph/spacing/context/flow và bằng chứng gắn đúng text; chưa chắc không cấp sửa. Nhánh không Kính gửi vẫn cần hai mốc plain đã quan sát; danh sách ObservationTargets không trở thành RepairTargets.

Sau sửa chữ/phông/căn/spacing, lưu hoặc Line, các lần CheckCore vẫn đọc và đối chiếu bằng chứng mới theo luồng cũ. Việc nhận diện chung chỉ có hiệu lực trong một lần gọi; không giảm XML/native/XPS, hậu kiểm nét thật, nguyên văn, cỡ/kiểu, Undo hoặc rollback. Đây là tối ưu CPU phân tích, chưa chứng minh thời gian toàn phiên Word giảm bao nhiêu.

Kiểm thử đối chiếu đầy đủ Issue gồm thứ tự/payload/căn cứ/source/neighbor trên ba profile và các mẫu unknown/list/grid/nhấn mạnh/stale/boundary; mẫu500 đoạn; kiểm quét lại không giữ mục đã sửa, mất ranh giới thu hồi quyền và snapshot cũ không đổi. Bộ đếm layout chứng minh một lượt phân tích thay ba mà không dùng ngưỡng thời gian dễ dao động. Đối chiếu riêng với assembly bước B02 trước đó trên51 trường hợp giả lập (ba profile, biến thể unknown/bố cục/nhấn mạnh/mốc mở đầu và mẫu500 đoạn) cho JSON Issue giống từng byte:3383779 byte, SHA256 `5af9e73980317acd75795a2f363a8eb3d9a25cc1e8c48b881c98bfff115dee98`. Đây là dữ liệu giả, không phải tài liệu người dùng hoặc bằng chứng Word runtime. Fixture Word hiện có sẽ dùng CheckCore mới khi chạy; CI chỉ biên dịch fixture, chưa chạy Office2021.
