# Đợt 83: chính tả tiếng Việt, người dùng duyệt từng chỗ

Phiên bản 0.1.85 tiếp nối sửa tại chữ 0.1.84. Bộ C# được viết riêng; dùng cấu trúc dữ liệu từ ZIP người dùng gửi. Không chạy Python hoặc đưa trình ghi DOCX của ZIP vào ứng dụng. Giữ Word/XPS và đường sửa manual có đối chiếu toàn bộ part, một Undo, rollback khi lỗi.

## Nạp dữ liệu

Menu Chính tả nhận ZIP tối đa 64 MiB. Đọc trực tiếp hai nguồn có đuôi `dictionary/words.txt` (JSONL, tối đa 16 MiB) và `app/lm.json` (tối đa 48 MiB). Không giải nén hoặc thực thi mã. Mirror cùng tên chỉ nhận nếu cùng byte; bản khác nhau bị từ chối. Giới hạn số entry/mục từ/token, kiểm tổng unigram, số đếm, cấu trúc và nguồn; SHA256 của hai nguồn được dùng làm định danh. Import ghi stage riêng và chỉ thay cache sau khi xác minh, lỗi giữ bộ cũ. Cache ở `%LOCALAPPDATA%\ChinhTaViet\Spelling\local-import.zip`, không tải mạng và không gửi nội dung Word.

ZIP đã kiểm có 79.198 dòng mục từ, còn **78.239 mục phân biệt sau NFC/chữ thường**; 7.064 unigram, 204.936 bigram, 568.230 trigram, tổng 2.516.561 token. Không coi danh sách âm tiết sinh tự động là từ điển hợp lệ. Headword nhiều từ được giữ khi đọc rồi tách thành token để tìm ứng viên; chưa có bộ phân đoạn từ tiếng Việt hoàn chỉnh.

Khóa tra dùng NFC/chữ thường, mốc chọn giữ nguyên UTF-16 nguồn kể cả NFD và emoji. Tra ứng viên theo gần chữ/dấu có thứ tự ổn định; xếp bằng bigram và trigram thật. Không thêm BOS/EOS giả quanh cửa sổ ngữ cảnh. Cụm đã duyệt đứng trước gợi ý từng token. Từ có trong từ điển chỉ được gợi ý trong nhóm cặp dễ nhầm đã duyệt và có tần suất ngữ cảnh hỗ trợ. Chưa thấy trong từ điển không đồng nghĩa sai. Bảo vệ tên ghép/viết tắt, identifier và các đoạn kỹ thuật đã nhận diện; không bảo đảm nhận mọi tên riêng.

## Tương tác

Gợi ý chính tả là nhóm riêng, màu vàng, `CanFix=false`; BatchPlan từ chối cả đề xuất bị gán giả CanFix. Bấm từ đơn hoặc chọn cảnh báo rồi bấm nút Ô sửa từ/cụm để mở bản nháp đúng vùng. Chọn một trong tối đa ba ứng viên chỉ điền vào ô. Người dùng có thể tự gõ, hủy hoặc Ghi. Sửa cụm đi qua nhiều run định dạng vẫn bị chặn; giữ bản nháp và sửa từng vùng/Word. Chưa kiểm hoặc sửa các vùng không đủ bằng chứng, trong bảng/field/đối tượng/lịch sử.

Chưa nạp ZIP chỉ dùng bộ mẫu nhỏ của dự án, được ghi rõ trên giao diện. Không mô tả bộ mẫu là từ điển chính tả hoàn chỉnh. Bộ đầy đủ lưu tại máy người dùng; bản phát hành chỉ có mã và mẫu do dự án viết.

## Đánh giá giới hạn

Đã chạy C# trực tiếp với ZIP gốc người dùng gửi: 10/10 câu lỗi được cài sẵn có ứng viên mong đợi đứng đầu; 30 câu đối chứng không có cảnh báo; bổ sung hiễu→hiểu, con đo→con đò và emoji giữ đúng mốc. Đây là tập dò lỗi nhỏ đã dùng khi phát triển, **không phải đánh giá độc lập hoặc tỷ lệ chính xác chung**. Bộ corpus thiên về hành chính vẫn có thể gợi ý nhầm và bỏ sót. Lượt đầu chỉ dùng tần suất đã cảnh báo nhầm 8/30 câu, nên đã chặn thay từ có nghĩa ngoài các cặp duyệt trước.

Kiểm hồi quy có fixture riêng cho Unicode/dấu ở nguyên âm sau, thứ tự ứng viên, trigram phá hòa, bảo vệ vùng kỹ thuật/tên, phrase overlap, cấm batch, ZIP hỏng/mirror/path/tổng token và import không ghi đè cache tốt. WPF kiểm việc chọn gợi ý không ghi, Ghi một lần, nhóm Word độc lập. Số kiểm thực và commit lấy từ build-checks.json của gói. Word thực **NOT_RUN**; nhóm mới `vietnamese-spelling` cần chạy trên Windows có Word.

## Nguồn và điều kiện phân phối

ZIP gốc SHA256: `d2401111c6d9a7f7d62cefba5b815961ebf6bc319e3bb34d5d2927745a10ccd4`.

SHA256 dictionary+LM: `c82ce0ad7c2a37437f57a697037327be90636bfe645b4a978133dbda7b57b590`.

Các mục ghi nguồn hongocduc, tudientv, wiktionary. README upstream undertheseanlp/dictionary nói mã GPL-3.0; chưa xác nhận riêng quyền phân phối từng nguồn dữ liệu hoặc corpus/LM. Vì vậy không chép dữ liệu đầy đủ hay mã Python vào repo/gói công khai. Người dùng nạp bản họ đã cung cấp để đánh giá cục bộ; cần làm rõ giấy phép, phiên bản nguồn và nghĩa vụ ghi công trước khi phát hành bộ dữ liệu mặc định.
