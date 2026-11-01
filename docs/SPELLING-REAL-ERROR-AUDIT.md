# Đo bộ kiểm trên lỗi thực VSEC — tập phát triển

**CHƯA ĐỦ ĐIỀU KIỆN PHÁT HÀNH.** Đây là đo nghiên cứu trên nhãn của nguồn ngoài, chưa được dự án rà soát độc lập hai lượt. Nguyên mẫu vẫn tắt trong ứng dụng.

Nguồn: [VSEC2021/VSEC](https://github.com/VSEC2021/VSEC/tree/82e68012bac9d6a14e9b517ba449c9952d8fa0da); SHA-256 nguồn: 7034f103f7be6a9dddbae12222442926da74b1e3ab4634fcde85dc5e548afbc9.
Đọc 9,341 câu; loại 1 câu có đáp án chấp nhận chính chữ gốc và 1 câu không ghép được vị trí chính xác. Loại trùng huấn luyện: 0; trùng kiểm toán lịch sử: 0.
Đã đo 7,547 câu / 9,030 đích lỗi. Giữ lại 1,792 câu / 2,169 đích, chưa xuất và chưa chấm. Các câu gốc/câu sửa tương đương được gom cùng nhóm trước chia tập.

| Chỉ số theo nhãn nguồn | Bản 0.1.91 tương thích | Nguyên mẫu |
|---|---:|---:|
| Đích có cảnh báo đỏ | 2485/9030 (27.52%) | 3747/9030 (41.50%) |
| Cảnh báo đỏ phủ đích lỗi / toàn bộ cảnh báo đỏ | 2485/3772 (65.88%) | 3747/5998 (62.47%) |
| Cảnh báo trên phần được nguồn gán đúng | 1287 | 2251 |
| Cảnh báo / 1.000 token được nguồn gán đúng | 5.87 | 10.27 |

| Nhóm lỗi | Đích | Bản cũ: phát hiện | Nguyên mẫu: phát hiện | Nguyên mẫu: gợi ý đầu / ba gợi ý đúng |
|---|---:|---:|---:|---:|
| letters_or_other | 2839 | 1019 | 1028 | 95 / 95 |
| missing_diacritics | 2676 | 623 | 1991 | 1800 / 1800 |
| wrong_diacritics | 3070 | 699 | 585 | 38 / 39 |
| deletion | 269 | 63 | 61 | 0 / 0 |
| word_boundaries | 156 | 78 | 77 | 0 / 0 |
| multiple_answer_types | 20 | 3 | 5 | 3 / 3 |

| Mục gốc có trong từ vựng của bộ kiểm | Đích | Bản cũ: phát hiện | Nguyên mẫu: phát hiện |
|---|---:|---:|---:|
| Không | 2508 | 2188 | 2207 |
| Có | 6522 | 297 | 1540 |

“Phủ đích lỗi” được tính theo giao nhau giữa vùng cảnh báo và vùng lỗi gán nhãn. Gợi ý đúng phải khớp một đáp án được chấp nhận, kể cả đáp án thứ hai hoặc xóa từ; một gợi ý phủ nhiều đích phải sửa đúng mọi đích bên trong. Kiểm vị trí bằng chuỗi UTF-16 gốc.

Bộ dữ liệu chỉ gồm câu có lỗi. Các token được gán đúng nằm trong những câu đó; chúng không thay thế một kho văn bản hoàn toàn đúng được thẩm định. Tỷ lệ khớp nhãn nguồn không phải chứng nhận độ chính xác thực tế hoặc kết quả đạt cửa kiểm 98%. Không có ranh giới tài liệu gốc để chứng minh độc lập theo tài liệu.

Nguyên mẫu tăng phát hiện nhưng tăng cảnh báo trên phần được gán đúng. Cần giảm báo nhầm, cải thiện từ hợp lệ dùng sai/dấu đã gõ sai và xếp hạng đáp án trước khi bật cho người dùng. Nhóm chữ HOA/viết hoa chưa có đích riêng trong nguồn này; vẫn cần bộ đối chứng khác.

Dữ liệu/từ điển ứng dụng không được huấn luyện hoặc bổ sung từ nguồn này. Kho gốc chưa nêu giấy phép; không đưa câu nguồn, nhãn gốc, tập dự trữ hoặc tệp cases vào GitHub artifact/bộ cài. Báo cáo chỉ chứa số tổng hợp, hash và nguồn. Word thật chưa được đo trong bài này.
