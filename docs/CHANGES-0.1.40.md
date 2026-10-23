# 0.1.40 — Phát hiện từ và cụm từ lặp (bản thử)

Tiếp nối mã 0.1.39 và tài liệu danh sách khả năng tại commit `9f147916928bfd3af73cd06a1688f3bfe1d1f093`. Người dùng chốt ưu tiên thể thức, lỗi chữ/lặp từ-cụm từ trước; ngữ cảnh khó và thư viện làm sau cùng. Bản này bắt đầu phần lặp cơ học, chưa bổ sung từ điển hay sửa lỗi ngữ nghĩa.

## Phần mới

- Phát hiện trong cùng đoạn các cụm 2–8 tiếng giống nhau liền nhau, kể cả lặp ba lần trở lên: `kế hoạch kế hoạch` → đề xuất giữ `kế hoạch`; `công tác công tác công tác` → giữ `công tác`.
- So sánh đúng chữ hoa/thường; so dạng Unicode tương đương nhưng giữ nguyên mã chữ của lần đầu. Chỉ nối qua dấu cách thường, không qua dấu câu, tab, khoảng trắng đặc biệt, ngắt dòng/đoạn/ô. Không coi số hoặc thành phần mã có số/gạch dưới là từ để tự xóa.
- Chỉ xóa phần từ khoảng cách trước bản lặp đến hết bản lặp; không ghi lại lần đầu. Kiểu chữ của phần giữ lại được giữ theo cơ chế range Word. Cần duyệt cả câu: cụm lặp có thể có chủ ý, đây không phải kết luận sai ngữ nghĩa.
- Một tiếng lặp như `đều đều`, `nhà nhà`, chuỗi toàn chữ hoa và dòng ứng viên số/ngày chỉ cảnh báo thủ công. Không dùng danh sách từ láy/từ điển để phán nghĩa trong đợt này.
- Một cụm dài hơn 8 tiếng, cách viết khác chữ hoa/thường hoặc lặp qua dấu câu có thể chưa được phát hiện. Không suy ra hết cảnh báo nghĩa là không còn lặp. Khi có các mẫu lặp lồng nhau, ưu tiên đơn vị lặp ngắn nhất ở vị trí đầu, không đưa các lượt sửa chồng nhau.

## Gộp sửa và giữ an toàn

Gợi ý khoảng trắng/chữ minh họa nằm trong phần sẽ xóa được nhường cho gợi ý lặp; các lỗi độc lập ở phần giữ lại vẫn chọn được. Sửa lặp một mình không tự sửa khoảng trắng còn lại. Có thể gộp với định dạng ký tự của cả dòng đã đủ điều kiện; vẫn áp định dạng trước, đối chiếu chữ và kiểu sau lưu. Xóa hết range trong Word có thể trả Text=null; đọc kết quả chuẩn hóa về chuỗi rỗng trước hậu kiểm.

Tôn trọng TextEditable và chặn dòng số/ngày có tab bố cục như trước. Đối chiếu sâu cấu trúc XML của Word vẫn giới hạn 40 đoạn đầu Nhà nước; chưa mở rộng bảo vệ này cho mọi trang/profile. Dữ liệu cấu trúc chưa được chứng minh không được coi là đã hỗ trợ biên tập an toàn ở mọi vùng. Đề xuất mới vẫn qua đối chiếu hiện tại, nguyên văn/range/story, giao dịch rollback và một Undo; không tự retry ghi.

Giữ các kiểm thể thức, nét XPS thật/marker DOCX, rollback Line, lưu bản sao có kiểm byte và sửa cleanup checkpoint .39. Quy tắc `con đo` cũ chỉ là minh họa, chưa phải bộ kiểm chính tả đầy đủ và không được mở rộng thành suy đoán ngữ cảnh trong đợt này.

## Kiểm thử và cách thử

Core kiểm cụm hai/ba lần, giới hạn 8 tiếng, hai nhóm độc lập, từ láy/mã/header thủ công, chữ hoa, Unicode tổ hợp, ranh giới, gộp với khoảng trắng/chữ/định dạng, stale proposal và lỗi sau xóa. Windows CI chạy toàn bộ Core/OpenXML + WPF và đóng gói; CI không có Word.

Giải nén thư mục mới, giữ `samples`, chạy **Kiểm tra → Kiểm tra Word tự động…**. Cuối bài có mẫu `repeated-text-input.docx`: hai cụm lặp sửa được, một lỗi khoảng trắng và hai mục từ láy thủ công. Kiểm lỗi cố ý sau xóa, sửa riêng, sửa nhóm, giữ đậm/gạch chân lần đầu, danh sách sau quét, một Undo và lưu/mở. Bản lưu là `repeated-text-fixed.docx`, nguồn giả phải giữ hash.

**Chưa có bằng chứng 0.1.40 chạy đạt trên Office 2021.** Giữ main, stable 0.1.27 và mọi tag/ZIP cũ; bản mới tiếp tục thử nghiệm, không coi người dùng chốt thứ tự phát triển là xác nhận mọi chức năng đã đạt.
