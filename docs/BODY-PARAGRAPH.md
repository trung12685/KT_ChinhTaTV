# C27 — căn đều và thụt đầu dòng văn xuôi thường

B05 mở thêm nhánh [không Kính gửi có hai mốc văn xuôi đủ proof](BODY-OPENING.md). Các hạn chế Kính gửi dưới đây là phạm vi ban đầu; .63 chỉ có bản tải khi pipeline phát hành đúng commit đạt.

B03/B04 đã có [C28/C29](BODY-SPACING.md) với duyệt riêng cho khoảng đoạn/giãn dòng đủ bằng chứng. Các giới hạn dưới đây là phạm vi riêng của C26/C27 lúc bổ sung; không mở toàn bộ thân bài hoặc mọi kiểu giãn dòng.

B02 sau .62, code .63 đang phát triển; chưa đóng gói Windows .63. Căn cứ đã đối chiếu trang nguồn: NĐ30 Phụ lục I, phần I, II.6.e (PDF25): nội dung căn đều hai lề; dòng đầu lùi1cm hoặc1,27cm. Không gọi phần này là điểm b.

## Ví dụ và phạm vi

Đoạn văn xuôi thường căn trái, dòng đầu0cm → sau duyệt: căn đều, dòng đầu1,27cm. Đang thụt1cm hoặc1,27cm thì giữ nguyên; nếu chỉ sai căn thì chỉ đổi căn, chỉ sai thụt thì chỉ đổi dòng đầu. C27 nằm ở Bắt buộc sửa, vẫn cần người dùng duyệt trước khi áp dụng.

Dùng ranh giới văn bản Nhà nước đã có ở C26: tên loại/trích yếu chắc → Kính gửi → chữ ký/Nơi nhận, XML chữ/style đã đối chiếu. Chỉ văn xuôi thường không đậm/nghiêng, không nhãn mục gõ tay/Word auto-list, bảng, field/link/control/object, tiêu đề hoặc vùng chưa chắc. Không mở chế độ Đảng/cá nhân.

Bước này chỉ nhận căn trái/căn đều, lề trái/phải của đoạn gần0, dòng đầu không âm và không quá144pt. Căn giữa/phải, thụt cả đoạn, hanging và khoảng trắng đầu/cuối dùng dàn chữ giữ ngoài phạm vi. Chưa có cảnh báo không chứng minh toàn bộ nội dung đã đúng.

## Giữ phần chưa chọn

Ghi qua thuộc tính ParagraphFormat.Alignment/FirstLineIndent có trong đề xuất, không đụng Font hoặc thay cả Range.Text. Chọn riêng C27 giữ nguyên các lỗi phông/cỡ/chữ chưa chọn. Chọn chung C26 + C27 + khoảng trắng/cụm lặp dùng một lượt, cho phép đúng hai đề xuất định dạng phủ cùng một đoạn, sửa định dạng trước chữ, rồi đo lại trước các bước Line.

Chụp kiểu từng glyph, căn/thụt/lề đoạn và SpaceBefore/After, LineSpacingRule/LineSpacing, SpaceBeforeAuto/AfterAuto. Single/1,5/Double giữ đúng rule; số pt đọc lại có thể đi theo cỡ chữ đã chọn sửa. AtLeast/Exactly/Multiple phải giữ cả rule và trị số. Gợi ý ràng buộc vào snapshot trước sửa; thuộc tính thay đổi làm lựa chọn cũ mất hiệu lực. Sau lưu và sau Line, chỉ căn/thụt đã duyệt được đổi, còn phông/cỡ/gạch chân/khoảng đoạn/giãn dòng phải khớp. Sai hậu kiểm hoặc COM lỗi thì rollback cả lượt; một Undo, lưu bản sao, giữ file nguồn.

Mức đích mặc định khi sai là1,27cm (36pt); chấp nhận1cm (28,35pt) hiện có, dung sai0,05pt để đọc Word/twip. Không suy mọi đoạn lùi khác là lỗi: phải qua toàn bộ nhận diện ở trên.

## Kiểm chứng và bước tiếp theo

Core kiểm hai mức hợp lệ, sửa riêng từng thuộc tính, ngoại lệ/stale, cùng phông/chữ, giữ kiểu/khoảng đoạn và rollback. Desktop giả lập kiểm writer chỉ ghi thuộc tính đã chọn, không đọc Font, phát hiện spacing drift và đưa COMException sau setter vào rollback. Fixture Word tổng hợp kiểm riêng/chung chữ/phông/ba Line, kiểu/cỡ, khoảng đoạn/giãn dòng, Undo, lưu-mở và hash nguồn.

CI chỉ biên dịch fixture Word, không chạy Word/Office2021. Kiểm căn/giãn đoạn thực tế vẫn cần máy có Word; chưa khép phản hồi phông cũ bằng CI. Hạng mục tiếp theo: khoảng cách đoạn tối thiểu6pt và giãn dòng đơn đến1,5 lines, chỉ khi đủ bằng chứng và duyệt riêng; chưa tự sửa hai phần này trong B02.
