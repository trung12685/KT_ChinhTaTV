# Tiếp tục ngày30/10/2026 — đợt27–29

Nhánh: [feature/isolated-spacing-proof-batch22](https://github.com/trung12685/KT_ChinhTaTV/tree/feature/isolated-spacing-proof-batch22). Mã vẫn0.1.77, chưa gói Windows mới; bản đã phát hành gần nhất0.1.76 giữ nguyên. Commit triển khai đợt này `918c9e41ba129e2bdda02e8a6fe1ae01e41cd54c`, tiếp sau `f3898b5e78ee196d30a2b7bb4fa78135c1df3fb5`.

## Các phần làm tiếp ngoài việc chờ Word thực

1. Sửa nguy cơ làm đổi khoảng/lặp trong tên tệp, đường dẫn, URL/email/code. Chuyển gợi ý chữ của dòng kỹ thuật nhận diện được sang thủ công; giữ văn xuôi thường và quyền thể thức độc lập.
2. Phân loại lỗi đo với thông điệp/hướng xử lý an toàn; chỉ typed thiếu bằng chứng được phép thành kết quả khoảng đoạn chưa đo. Lỗi audit/toàn vẹn/COM/I/O/cleanup không bị nuốt. Kết quả trái vector/hiệu ứng không được hiện là đạt.
3. Nhóm kiểm Word độc lập về sửa chữ và giữ dữ liệu kỹ thuật, tổng12nhóm: chọn riêng/chung, lỗi giữa lượt, một Undo, lưu/mở, giữ nguồn và kiểu chữ. Kịch bản đã viết, chưa chạy Word thật.

Chi tiết [FORMAT-BATCH-27-29.md](FORMAT-BATCH-27-29.md). Giữ danh mục33đề xuất/8thủcông/1minh họa, bộ đọc thử nghiệmOFF và khoảng ký tay80pt. Chưa có từ điển hoặc sửa ngữ cảnh mới.

## Kết quả đã xác minh

| Kiểm tra | Kết quả |
| --- | --- |
| Core/OpenXML trên Linux | 5.307 assertion đạt, exit0; tăng164 |
| Build DesktopChecks/Desktop Release | Đạt, 0cảnh báo/0lỗi |
| Windows CI | SUCCESS, [run40921878984](https://github.com/trung12685/KT_ChinhTaTV/actions/runs/40921878984), đúng commit triển khai |
| Core/OpenXML, WPF/build, ghi evidence trong CI | Từng bước đều success |
| Microsoft Word thực | NOT_RUN — môi trường hiện tại là Linux |
| Quyền sửa Exactly/AtLeast | Giữ nguyên; phép đo chưa mở writer mới |
| Main, PR61, tag/gói cũ | Không sửa/merge/ghi đè |

Không cộng số assertion harness phụ vào runner chính. Số assertion Windows không được suy đoán; kết luận từng bước đã đọc trực tiếp trên trang run/job. Log local ở `artifacts/batch27-29/` (ignored); [JSON bằng chứng](handoff-evidence/batch27-29-evidence.json) gắn đúng commit mã triển khai. Các commit tài liệu tiếp theo không thay mã đã kiểm.

## Tiếp theo

Trên Windows có Word desktop, chạy từ mã nguồn với .NET8SDK rồi chọn hai nhóm clone/hiệu chuẩn và nhóm sửa chữ mới. Chỉ khi các trial đã được Word thực chứng minh mới thiết kế writer Exactly/AtLeast/khoảng đoạn gộp với hậu kiểm và phục hồi. Các phần thể thức độc lập, dữ liệu ngoại lệ dấu câu/khoảng trắng/lặp và rà soát báo nhầm vẫn có thể phát triển song song; từ điển/ngữ cảnh làm sau.
