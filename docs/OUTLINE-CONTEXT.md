# Ngữ cảnh cấu trúc — bản thử 0.1.51

C20/C21 kiểm cấu trúc mục gõ tay, chưa suy nghĩa câu hoặc biết một đoạn nội dung có bị xóa thiếu hay không. Mọi gợi ý ở **Đề xuất sửa — cần duyệt**; người dùng đọc và chấp nhận trước khi áp dụng.

## C20 — Thứ tự mục trong cùng mục cha

Mỗi dòng dưới đây là một đoạn Word riêng:

```text
1. Tình hình
2. Kết quả công tác
4. Đề xuất
```

Đề xuất đổi riêng ký hiệu `4` thành `3`, giữ `. Đề xuất` và mọi nội dung. Tương tự `a), b), d)` → `a), b), c)`; `I., II., IV.` → `I., II., III.`. Nhận dấu `.` hoặc `)` sau số/chữ/LaMã và khoảng phân cách rõ. LaMã viết hoa, chữ cái viết thường; chưa kiểm nhãn trần không dấu, `1.1.` hoặc mọi mẫu tùy biến.

Cấp thông thường: **LaMã → số → chữ → gạch → cộng**. Khi chuyển mục cha, chuỗi con bắt đầu riêng; cùng family có thụt lề rõ có thể là cấp con. Thân bài xen giữa không tự tạo chuỗi mới. Tiêu đề không số có outline level/viết hoa rõ làm ranh giới. Bắt đầu lại, mốc đầu không phải1/a/I, trộn delimiter/độ rộng, cấp/indent chưa rõ giữ kiểm tra thủ công. Chữ cái dùng dấu hiệu quan sát để phân biệt bảng Latin/Vietnamese; khi kỳ vọng sau d) chưa chắc là e) hay đ) thì không tự chọn.

Nếu tài liệu có dẫn chiếu liên quan như `xem mục 4`, hoặc field REF/PAGEREF/NOTEREF, có thể giữ đề xuất thủ công. Chưa tự đổi dẫn chiếu, hyperlink/bookmark hoặc số Word sinh tự động. Cần đối chiếu xem đang thiếu nhãn hay thiếu thật cả nội dung trước khi duyệt.

## C21 — Một dấu đầu dòng trong một mục cha

```text
1. Nội dung chính
- Nội dung riêng
2. Mục khác
```

Trong mục1 chỉ có một dấu gạch cùng cấp, đề xuất bỏ `- `, giữ `Nội dung riêng`. Nếu có hai dấu gạch trở lên trong mục1 thì giữ. Dấu của mục2 không được cộng vào mục1.

```text
1. Nội dung chính
- Nhánh đầu
+ Nội dung riêng
- Nhánh sau
+ Nội dung khác
```

Có hai dấu gạch nên giữ cả hai. Mỗi dấu gạch lại chỉ có một dấu cộng con: đề xuất bỏ riêng hai dấu `+ `. Nếu một nhánh có hai cộng trở lên thì giữ cộng của nhánh đó. Nếu một dấu gạch và một dấu cộng con đều đơn, có thể đề xuất bỏ cả hai trong cùng lượt. Plus không nhất thiết phải có dash cha, nhưng phải có một mục cha rõ.

Đếm theo mục cha đã xác định trọn vẹn. Biên gặp bảng, field, danh sách tự động, kiểu chưa rõ hoặc XML không khớp có thể khiến việc đếm chưa đầy đủ: không tự kết luận đơn. Dấu ở gốc mà chưa biết mục cha giữ thủ công. Dấu `-`, `–`, `—` cùng loại gạch; chưa coi `•`, `*` hoặc công thức là gạch gõ tay.

## Giữ bố cục và giới hạn Word

Bằng chứng lấy từ XML đang mở và Start/End/Text Word thật; không gọi getter Shape từng lỗi. Nhận diện body ngoài bảng và kiểu/numPr kế thừa; auto-list không biến thành chữ để sửa. Chỉ đổi nhãn hoặc bỏ marker+separator ASCII; không xóa đoạn, đổi kiểu toàn dòng hay di chuyển nội dung.

Tab đầu dòng hoặc phân cách của nhãn số có bằng chứng riêng: chỉ thay ký hiệu, **giữ nguyên tab**, quyền sửa chữ chung vẫn không mở. Với gạch/cộng, leading tab được giữ, nhưng tab làm separator ngay sau marker hoặc prefix trộn chưa rõ sẽ giữ thủ công. Không suy tọa độ tab bằng số twip cố định.

Chọn riêng giữ lỗi chưa chọn. Có thể sửa chung C01–C21 và phông đủ điều kiện; nhóm căn giữa/Line vẫn tách lượt. Quét lại trước ghi kiểm ngữ cảnh mới: thêm dấu cùng cấp phải làm đề xuất đơn cũ hết hiệu lực. Lỗi giữa lượt rollback, một Undo cho cả lượt, lưu/mở giữ chữ/kiểu/nguồn. Không báo đã sửa hết chính tả/ngữ cảnh chỉ vì không có cảnh báo.

Core/Windows CI không có Word; kịch bản Kiểm tra Word tự động đã bổ sung mẫu giả, chưa xác nhận đầy đủ Office2021 cho .51. Các file người dùng không đưa vào release.
