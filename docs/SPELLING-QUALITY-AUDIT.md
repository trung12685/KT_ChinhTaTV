# Báo cáo chất lượng chính tả — kiểm toán phát triển

Trạng thái phát hành: **CHƯA ĐỦ ĐIỀU KIỆN**. Không tạo bộ cài từ quy trình này.

Cùng dữ liệu và toàn bộ luồng worker: bản so sánh tái hiện 0.1.91; nguyên mẫu bật phục hồi dấu tổng quát và chính sách bảo vệ chuỗi tên/tra cứu từ vựng rộng hơn. Không huấn luyện lại dữ liệu trong lần đo.

| Tập / nhóm | Bản so sánh: phát hiện/đích | Nguyên mẫu: phát hiện/đích | Gợi ý đầu đúng đích: so sánh → nguyên mẫu |
|---|---:|---:|---:|
| developer_fixture / missing_diacritics | 3/15 | 13/15 | 6.7% → 86.7% |
| developer_fixture / reviewed_spelling | 5/5 | 5/5 | 100% → 100% |
| developer_fixture / real_word_and_wrong_mark | 1/4 | 1/4 | 0% → 0% |
| developer_fixture / typing_and_segmentation | 3/4 | 3/4 | 25.0% → 25.0% |
| developer_fixture / unicode_offsets | 1/2 | 2/2 | 0% → 100% |
| developer_fixture / capitalization | 0/2 | 0/2 | 0% → 0% |
| synthetic_counterfactual / synthetic_drop_marks | 187/659 | 422/659 | 3.9% → 53.7% |

Mức cảnh báo đỏ trên 659 câu tin tức chưa gán nhãn: 9.02 → 8.61 cảnh báo/1.000 token. Đây **không phải tỷ lệ báo nhầm**; các cảnh báo chưa được thẩm định.

61 câu do dự án biên soạn là kiểm thử phát triển, không phải tập đánh giá độc lập. 659 lỗi bỏ dấu là nhiễu tạo có chủ đích; sửa lại bản gốc không chứng minh câu nhiễu luôn sai trong ngữ cảnh.

Chưa có bộ lỗi thực/văn bản đúng được hai người rà soát độc lập. Chưa đo vị trí gạch và độ trễ trên Word thật. CI xanh chỉ xác nhận quy trình chạy đúng và tái lập, không chứng nhận bộ kiểm đạt chất lượng sử dụng rộng rãi.
