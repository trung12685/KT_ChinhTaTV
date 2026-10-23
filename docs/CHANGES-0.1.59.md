# 0.1.59 — Danh mục tên loại, chữ viết tắt và BCĐX

Ngày 24/10/2026 (UTC+7). Đối chiếu trực tiếp bản ký Nghị định 30/2020/NĐ-CP, Phụ lục III, phần I, trang PDF 40.

## Danh mục và nguồn

Thay danh sách rời 13 tên cũ bằng DocumentTypeCatalog: **27 tên loại văn bản hành chính + 3 loại bản sao** đúng thứ tự bảng, kèm tên/viết tắt/nguồn và tên thể hiện ở đầu văn bản. Giữ nguyên cách viết **QyĐ, CTr, TTr, TrS**, không ép mọi mã thành chữ in hoa. Nghị quyết/Quyết định có ghi chú cá biệt trong danh mục; Giấy uỷ quyền nhận cả cách đặt dấu ủy mà không sửa dấu qua lại.

Thêm **Báo cáo đề xuất — BCĐX** theo yêu cầu người dùng. Đây là mục bổ sung của người dùng, không có trong bảng Phụ lục III. Nhãn nguồn tách riêng trong cửa sổ, TXT và tooltip đề xuất liên quan. Căn cứ thể thức chung được dùng cho mẫu nhận diện; không ghi BCĐX là tên loại do bảng Nghị định quy định.

Có cửa sổ tra cứu **Trợ giúp → Danh mục tên loại văn bản…**, không cần mở Word; bảng chỉ đọc và có cột nguồn/phạm vi, hàng bổ sung tô nhạt. Báo cáo TXT liệt kê đầy đủ 31 mục theo ba nhóm nguồn; gói có tài liệu [danh mục đầy đủ](DOCUMENT-TYPES.md).

## Hành vi kiểm/sửa

Một catalog dùng cho nhận diện tên loại, C24 viết hoa, phông tên loại/trích yếu và mốc Line dưới trích yếu. 28 tên hành chính/bổ sung có thể tham gia nhận diện, nhưng vẫn cần cặp quốc hiệu/tiêu ngữ, tên duy nhất, khối trích yếu có ranh giới/căn giữa và proof Word/XML hiện có. Chỉ có trong danh mục không chứng minh mẫu đặc thù của biên bản/hợp đồng/giấy/phiếu đã được hỗ trợ đầy đủ. Bản sao có thể thức riêng, chỉ tra cứu, chưa tự áp quy tắc tên loại/trích yếu/Line của văn bản gốc. Công văn không có viết tắt riêng trong bảng này, không tự thêm CV.

Dòng Số: không còn cảnh báo “chữ thường” chỉ do đầu ký hiệu đúng bảng như TTr/CTr/QyĐ/TrS. Chữ thường ở phần mã cơ quan vẫn cảnh báo, ví dụ TTr-Abc. Các sửa nhãn Số, thêm 0, dấu nối và khoảng trắng vẫn giữ đúng viết tắt. Chưa tự đổi chữ viết tắt theo tên loại, không đoán mã cơ quan, không coi mọi mã in hoa là đã đúng danh mục.

Không thêm loại lỗi mới: danh mục năng lực giữ 24 đề xuất/8 thủ công/1 minh họa. Không thay nét XPS, popup tiến độ, một Undo/rollback, proof chữ/định dạng hoặc mẫu Đảng/cá nhân. Không dùng danh mục để nới quyền sửa qua field/control/drawing. Lỗi cơ quan/trích yếu đã xác nhận trên .56 vẫn chờ runtime .57 hoặc mới hơn; chưa kết luận đã khép lỗi đó.

## Kiểm chứng và cách thử

Core đối chiếu 31 viết tắt theo bảng và nguồn, nhận diện/case/font/Line của 28 tên, alias uỷ/ủy, không áp bản sao/Công văn, ghi rõ nguồn BCĐX trong issue/tooltip/Line/TXT, mã đúng hoa-thường và mã cơ quan sai, giữ sửa khoảng/số 0. WPF mở cửa sổ thật không có Word, kiểm read-only/cột/nguồn/hàng BCĐX và chụp dữ liệu tham chiếu công khai. Bài Word giả dùng Báo cáo đề xuất cùng các nhóm phông/Line; chỉ được CI biên dịch vì CI không có Word. Số kiểm thực trong build-checks.json/TXT, chưa Office2021 PASS.

1. Mở Trợ giúp → Danh mục tên loại: 27 hành chính, 3 bản sao, BCĐX ở hàng người dùng bổ sung; xem QyĐ/CTr/TTr/TrS đúng hoa-thường.
2. Trong mẫu Nhà nước được nhận diện rõ, thử “Báo cáo đề xuất”, “Tờ trình” hoặc tên mới trong bảng: viết hoa/phông chỉ đề xuất khi đủ guard. Giữ trích yếu, ngắt đoạn và lỗi chưa chọn; một Undo và lưu/mở phải đúng.
3. Với dòng Số: 01/TTr-ABC, không báo lỗi chỉ do Tr có chữ thường; Số: 01/TTr-Abc vẫn giữ cảnh báo mã cơ quan. Không tự thay BC thành BCĐX hoặc đổi mã cơ quan khi đổi tên loại.
4. Xuất TXT: có đủ danh mục và nhãn riêng cho mục bổ sung. Nếu còn lỗi Word thật, ghi phiên bản và giữ log/DOCX sau sửa; không đưa tài liệu riêng vào kho/release.

[Danh mục chi tiết](DOCUMENT-TYPES.md) · [năng lực .59](SUPPORTED-FIXES-0.1.59.md) · [nhật ký phát triển](DEVELOPMENT.md). Giữ main và stable .27.
