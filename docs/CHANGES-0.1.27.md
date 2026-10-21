> Cập nhật 21/10/2026: người dùng đã xác nhận thao tác sửa cả ba đường trên Office 2021 chạy được với 0.1.27. Phạm vi xác nhận và điểm còn chờ được ghi ở [STATUS-0.1.27.md](STATUS-0.1.27.md). Ghi chú bên dưới là bản ghi tại thời điểm phát hành thử.

# 0.1.27 — ghép lại mốc sau lưu và ánh xạ CP sang chuỗi

Log người dùng xác nhận 0.1.25 đã qua xác minh nét từng Line rồi thất bại tại ReadRemainingLines, dòng 402: điều kiện yêu cầu vai trò, nội dung và Start của mốc sau lưu đồng thời bằng lựa chọn ban đầu. Không có lỗi COM Shape mới. Log cũ chưa ghi vai trò hay trường khác biệt ở guard này; không khẳng định chỉ riêng CP đã được chứng minh là nguyên nhân cụ thể. Đây là lỗi ở bước đối chiếu bổ sung, không phải bằng chứng 0.1.25 đạt Office 2021.

Hai thay đổi liên quan đến vị trí:

- Vị trí Word Range (CP) không được giả định là chỉ số trong Range.Text. ExportPreview trước đây trừ hai CP để làm chỉ số chuỗi. Nay đọc Range.Text của tiền tố thật tới mỗi CP, kiểm tiền tố và toàn bộ đoạn chữ khớp chính xác trong story, rồi dùng độ dài tiền tố làm chỉ số. Không tìm lần xuất hiện đầu tiên và không rút ngắn đoạn chữ. Kiểm thử cho thấy dùng CP làm chỉ số có thể bỏ glyph cuối tiêu ngữ ngay cả khi đã sửa phép đo advances ở 0.1.24.
- Trước mỗi bước sửa và sau lưu, lấy bố cục Word/XML mới. Chỉ ghép lại lựa chọn khi toàn bộ story không đổi, cùng vai trò, đúng nguyên văn và có duy nhất một mốc hiện tại đủ điều kiện tự sửa. Sau mỗi lần mở candidate cũng như sau rollback, lấy lại bố cục và ghép mốc theo CP của đúng bản đang mở. Trước lần ghi candidate hiệu chỉnh, cả số đo mã hóa và số đo đích được ghép về CP của checkpoint đã khôi phục; hình học không đổi theo CP. CP hiện tại được dùng cho candidate, preview, phần kiểm cuối và bộ nhớ nét đã xác minh. Không bỏ điều kiện nội dung, không dùng mốc cũ nếu thiếu/trùng/thay đổi mốc. Phạm vi sửa thêm vẫn đúng các vai trò/nội dung ban đầu; vị trí phải khớp lựa chọn hiện tại đã đối chiếu.

Giữ kiểm marker DOCX, nét thực đo XPS, nội dung chữ, preservation các nét khác, một lượt đặt lại có giới hạn, rollback cả lượt và một lần Hoàn tác. Giữ tỷ lệ tự tạo 3/7, chấp nhận 1/3–1/2 và khoảng trống trường số/ngày.

Log mới ghi số lượng mốc theo vai trò, số mốc khớp nguyên văn, CP trước/sau, độ dài và trạng thái story, cùng chênh lệch CP/chỉ số chuỗi nếu có. Không ghi nội dung chữ hoặc tệp riêng. Nếu vẫn không thể ghép mốc chắc chắn, rollback và các số liệu này phân biệt được thiếu vai trò, khác chữ hay khác CP.

Kiểm thử thêm: ghép CP mới chỉ khi story/vai trò/nguyên văn hợp lệ; từ chối thiếu/trùng/khác mốc hoặc mốc không còn căn giữa; ánh xạ tiền tố lấy đủ ký tự cuối; giao dịch ba Line với hai sửa thêm theo CP mới, không mở rộng lựa chọn. Bài DOCX retry còn kiểm mốc được chuyển về đúng CP nguồn sau khi mô phỏng candidate có CP khác, giữ nguyên phần bù và đích trang. WPF kiểm đoạn tiêu ngữ đầy đủ trong story có dấu kết thúc ô và tái hiện lỗi hụt chữ khi dùng CP làm chỉ số. Bài Office check hiện có chạy qua các nhánh sửa nhóm, quét lại, lưu/mở lại, đặt lại hai nét và Hoàn tác.

CI chạy Core/OpenXML và WPF trên Windows rồi đóng gói; CI không có Word desktop. 0.1.27 vẫn cần xác nhận trên Office 2021 thực tế. Tài liệu, XPS, log và font riêng của người dùng không được đưa lên GitHub.
