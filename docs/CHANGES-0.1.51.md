# 0.1.51 — Thứ tự mục và dấu đầu dòng theo mục cha

Theo yêu cầu mới về ngữ cảnh cấu trúc, thêm hai nhóm gợi ý cần duyệt:

- **C20:** `1.,2.,4.` → `1.,2.,3.`; tương tự `a),b),d)` và `I.,II.,IV.`. Đổi nhãn theo vị trí cùng cấp, giữ chữ/delimiter/nội dung.
- **C21:** trong mục cha rõ nếu chỉ có một dấu `-` hoặc `+` cùng cấp, đề xuất bỏ dấu và separator ASCII. Gạch/cộng của các cha khác nhau được đếm riêng; có≥2 thì giữ.

Mỗi mục ở đầu một đoạn riêng; cây thông thường LaMã→số→chữ→dash→plus, có đối chiếu thụt lề và biên. Không đoán nội dung thiếu, không chuyển đoạn hoặc sửa dẫn chiếu. Chuỗi không có mốc đầu/restart/mixedstyle/chữ cái/dẫn chiếu chưa chắc, bảng/auto-list/field/link/bookmark/kiểu chưa xác minh giữ thủ công. [Ví dụ và giới hạn](OUTLINE-CONTEXT.md).

## Bằng chứng và an toàn

Outline evidence dùng cùng live XML đã đối chiếu với WordStart/End/Text; style-numPr kế thừa/numId0 được kiểm. Không thêm COM getters hoặc thay kiểm Line/XPS. Parser XML nội bộ dùng lại XDocument; layout header vẫn độc lập như .47. Quyền sửa chữ chung không đổi.

Có ngoại lệ hẹp cho **nhãn** trong đoạn chỉ có tab bố cục, chữ/XML khớp và không cấu trúc khác: giữ nguyên leading/separator tabs khi đổi nhãn; không mở sửa chữ chung. C21 chỉ bỏ ASCII separator, tab separator giữ thủ công. Không giả vị trí tab theo twips cố định.

New FixKinds appended, gộp span spacing trùng deletion, cho chia sẻ phông chứa toàn đoạn. CurrentTextPlan dùng quét mới của toàn cấu trúc: thêm sibling khiến lựa chọn đơn cũ bị từ chối trước checkpoint. Giữ transaction/rollback/Undo/save-copy và nguyên văn/kiểu ngoài lựa chọn.

## Báo cáo, giao diện và kiểm thử

Danh mục tăng **21 đề xuất/8 thủ công/1 minh họa** (C01–C21), thêm ví dụ nhiều đoạn C20/C21 và mốc .51; ngữ nghĩa/thư viện vẫn là hướng sau. Giao diện .50 giữ một dòng/nhóm/checkbox, hai nhóm mới ở phần cần duyệt. Ảnh mẫu CI thêm hai loại và nền sáng kín; gói kèm ISSUE-LIST/OUTLINE-CONTEXT.

Core kiểm parent/sibling/complete boundary, alphabets/format/references, XML/auto/style/field/table/tab, stale và nhóm chữ/phông/fault rollback. Office script giả có3 order+4 singleton+7 lỗi chữ, leading tabs, ba profile, single/stale/fault/Undo/save-reopen/retained styles/source hashes. File riêng chỉ đọc phân tích, không đưa vào mã/gói.

Chỉ phát hành sau Core/OpenXML, WPF và kiểm báo cáo đóng gói; số đếm thực tế có trong JSON/TXT kèm gói. **Chưa xác nhận đầy đủ Word/Office2021 cho .51.** Main/stable .27 và tag/asset cũ giữ nguyên.
