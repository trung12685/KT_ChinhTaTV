# Vietnamese.dic do người dùng cung cấp

Nguồn đầu vào: check-chinh-ta-cho-word.zip đính kèm trong cuộc hội thoại dự án. Tệp có tên .zip nhưng định dạng thực là RAR v4; chỉ có Vietnamese.dic dạng văn bản UTF-16LE, không có mã thực thi. SHA-256 nguyên bản: `1417975f8479f3a4009cf6af120e16ccea190f89b392fb16889d67ed66b6e769`.

Người dùng yêu cầu tích hợp từ điển này vào Add-in. Kho giữ bản base64 để bảo toàn byte UTF-16; bộ đóng gói giải mã thành proofing/Vietnamese.dic và kiểm hash. Bản words.txt bỏ dòng đếm đầu và dòng rỗng, chuẩn hóa NFC để bộ kiểm Add-in sử dụng. Dòng đếm khai 6634; số mục thực không rỗng là 6635. Không dùng dòng đếm làm mục từ.

Đây là dữ liệu chấp nhận từ/âm tiết, không phải bộ ngữ pháp hoặc mô hình ngữ cảnh. Nguồn đính kèm không có thông tin giấy phép; ghi nhận nguồn người dùng, chưa gán giấy phép của bộ dữ liệu chung khác. Cần làm rõ quyền phân phối thương mại trước khi triển khai sản phẩm rộng rãi. Bộ từ chung có giấy phép trong assets/spelling vẫn giữ riêng.
