# Kết quả thử bộ đọc trên tài liệu người dùng

Ngày26/10/2026, trên file đang so Bao cao.docx: .68 quét3,5s/dựng1,4s; .69 bật thử nghiệm quét5,0s/dựng1,8s/tổng12,9s; .69 bỏ tích quét2,1s/dựng1,2s/tổng6,1s. Người dùng xác nhận trạng thái checkbox của .69. Chưa có số lượt lặp hoặc log tách chặng; không gọi đây là benchmark kiểm soát lạnh/ấm.

Quyết định: bộ đọc mới chưa đạt mục tiêu trên trường hợp này, không dùng mặc định. Mã .70 tắt mặc định backend/checkbox và đổi nhãn trung tính. Đang dùng .69 có thể tiếp tục để tùy chọn tắt, không cần tải thêm bản chỉ để tắt. Mốc .67 và các gói thử giữ nguyên để đối chiếu.

Không suy cải thiện tốc độ toàn ứng dụng từ số đo XML hoặc getter giả lập. Không bỏ bảng, Preview, phân trang, nét XPS, hậu kiểm hoặc rollback. Chỉ chọn tối ưu tiếp theo khi có stage timings thật để phân biệt chi phí enum/fallback, batch không dùng được, quan sát định dạng, Word startup và xuất XPS.
