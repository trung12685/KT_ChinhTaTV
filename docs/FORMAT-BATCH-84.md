# Đợt 84: ứng dụng có sẵn dữ liệu chính tả ngoại tuyến

Mã 0.1.86 thực hiện yêu cầu người dùng: mở lần đầu đã có dữ liệu, không yêu cầu từng người nạp ZIP. Bộ đi kèm được chép tự động khi build/publish; không tải mạng khi mở hoặc quét Word.

## Bộ chung và dữ liệu riêng

Bộ chung v1 dùng Viet74K của Hồ Ngọc Đức qua duyet/vietnamese-wordlist (GPL-2.0) và tần suất tự dựng từ snapshot UD_Vietnamese-VTB (CC BY-SA 4.0). Có 72.535 mục từ phân biệt sau NFC/chữ thường; 3.314 unigram, 30.607 bigram, 41.821 trigram, tổng 50.220 token từ 2.639 câu. Tách 659 câu theo hash trước khi đếm. Đây là bộ ngữ cảnh nhỏ, còn thiếu ngôn ngữ hành chính/chuyên ngành.

Đây **không phải nguyên bộ ZIP 2026-10-24 của người dùng**. Với bộ ZIP cũ, giấy phép mã GPL-3.0 chưa làm rõ riêng mọi nguồn từ điển/corpus/LM để đưa cả bộ vào gói công khai. Bản riêng đó vẫn được nạp và lưu trên máy như 0.1.85; bộ riêng hợp lệ luôn được ưu tiên, không bị thay khi nâng cấp. Người muốn dùng bộ chung có lệnh **Chính tả → Dùng bộ dữ liệu kèm ứng dụng**.

Nếu dữ liệu riêng hỏng, giữ tệp đó và dùng bộ chung, có thông báo. Nếu cả bộ chung thiếu/hỏng, ứng dụng chỉ dùng vài nhóm dự phòng và nói rõ cần giải nén lại gói đầy đủ; không giả báo đã nạp bộ lớn. Lệnh chuyển sang bộ chung phải đọc/xác minh thành công trước khi xóa lựa chọn riêng.

## Nguồn, tính toàn vẹn và chất lượng

Toàn bộ nguồn, commit, giấy phép, ghi công và thay đổi nằm ở `spelling/NOTICE.txt`, `GPL-2.0.txt`, `CC-BY-SA-4.0.txt` và các README gốc. Menu có cửa sổ xem nguồn. ZIP dữ liệu chứa chính word-list gốc không đổi và LM JSON đọc được; runtime đọc trực tiếp entry, không giải nén mã. Mã nguồn có script dựng lại từ các phiên bản công khai cố định. Dữ liệu giữ giấy phép riêng, không chép mã upstream vào ứng dụng.

Manifest bị ghim SHA256 trong mã; mọi entry, mô hình, thông báo và giấy phép đều được đối chiếu hash trước khi nhận. Thiếu thông báo giấy phép cũng là gói chưa đầy đủ. Cả bản publish và ZIP sau giải nén được kiểm thực sự nạp mô hình và tạo gợi ý mà không cần dữ liệu cá nhân hoặc Word.

Đếm tần suất ít không đủ để suy đoán nhiều từ. Bổ sung các cặp có ngữ cảnh đã duyệt như sử lý→xử lý, bổ xung→bổ sung, dứt điễm→dứt điểm, nghành giáo→ngành giáo, chât lượng→chất lượng, hiễu nội→hiểu nội; chúng không được cộng thành tần suất giả vào mô hình. Vẫn là cảnh báo vàng, cần duyệt, không sửa hàng loạt. Các quyền ghi, bảo toàn kiểu chữ, Undo và rollback giữ nguyên.

Kiểm dò cục bộ: 10/10 câu lỗi đã dùng trong phát triển có ứng viên mong đợi đứng đầu; 30 câu đối chứng và 659 câu news giữ lại không có gợi ý. Kết quả nhỏ và thận trọng này **không chứng minh độ chính xác/độ bao phủ chung**. Ví dụ `Con đo đưa khách sang sông` vẫn bỏ sót trong bộ chung; dữ liệu riêng 0.1.85 có thể gợi ý được. Chưa có bộ chuẩn do người dùng gán nhãn độc lập. Không huấn luyện thêm trên 659 câu giữ lại sau khi xem kết quả.

Kiểm hồi quy mới: nạp máy mới, ưu tiên dữ liệu riêng không sửa byte, fallback không mất tệp, payload bị đổi, thiếu giấy phép, phân phối đủ assets qua project reference/publish và nạp gói sau ZIP. Số kiểm đúng commit nằm trong build-checks.json. Word thực **NOT_RUN**; nhóm vietnamese-spelling vẫn dùng để xác nhận ghi/Undo trên máy có Word.
