> Cập nhật chốt bản 21/10/2026: 0.1.27 đạt 223 Core/OpenXML + 20 WPF trong CI Windows; người dùng xác nhận sửa cả ba đường cùng lúc chạy được trên Office 2021. Lưu/mở lại, một Undo và toàn bộ Office check của 0.1.27 chưa có xác nhận runtime riêng. Xem [biên bản](STATUS-0.1.27.md).
>
Để chọn từng nhóm lỗi khi kiểm tra, xem [Danh sách khả năng sửa 0.1.44](SUPPORTED-FIXES-0.1.44.md): C01–C16, các mục thủ công và bảng sửa riêng/nhóm/quét/Undo/lưu-mở. Danh sách mô tả chức năng đã lập trình, không phải xác nhận Office đã đạt.

> Danh sách bên dưới là kịch bản kiểm tra tích lũy theo từng phiên bản, không phải bảng kết quả đã đạt.

# Kiểm thử chấp nhận trên Windows (chưa thực hiện trong cloud Linux)

1. Chạy core checks và mở giao diện bằng các lệnh trong README. Nếu chưa cài Word, khi mở file phải hiển thị thông báo dễ hiểu.
2. Sao chép file mẫu, tính SHA256 trước thử: `Get-FileHash samples/VanBanMau.docx`.
3. Mở file mẫu. Kỳ vọng 5 cảnh báo: quốc hiệu, khoảng trắng và ba vị trí `đo` (đoạn, bảng, trang hai). Câu `Tôi đo chiều dài` và `con đò` không bị cảnh báo.
4. Bản 0.1.2: lúc mở không có highlight. Chọn/rê chuột từng lỗi: chỉ lỗi đó có khung vàng; khung cũ phải biến mất, danh sách phải chọn đúng lỗi tương ứng. Kiểm tra ba từ `đo` giống nhau ở đoạn, bảng và trang hai để loại trừ việc tô lần xuất hiện đầu. Kiểm tra khoảng trắng chỉ tô khoảng trắng, quốc hiệu tô từng dòng khi xuống dòng. Chuyển qua lại ít nhất 20 lần; ghi thời gian `Đánh dấu` và thời gian cảm nhận, không xuất Word lại. Đổi zoom 50/100/150/200%, cuộn, đổi kích thước cửa sổ và kiểm tra khung luôn đi theo chữ. Thử trang có nhiều font, header/footer, ô gộp và văn bản lặp. Khi không ánh xạ chắc chắn, phải có thông báo và không tô vị trí khác. Sau sửa/hoàn tác, phải bỏ khung/index cũ, dựng lại và đánh dấu đúng nội dung mới. Ghi thời gian quét/dựng trang để so sánh cùng file/máy với 0.1.1.
5. Sửa `đo` ở trang hai; kiểm tra hai vị trí còn lại chưa thay đổi. Hoàn tác phải phục hồi đúng từ và số cảnh báo.
6. Sửa quốc hiệu: kiểm tra hoa, đậm, cỡ 13, font và căn giữa trong Word. Hoàn tác một lần phải phục hồi cả nội dung và định dạng.
7. Sửa khoảng trắng, lưu tên mới. Mở bản mới bằng Word, xác nhận không có màu highlight phát sinh; bảng, ngắt trang và các định dạng không liên quan còn nguyên. Kiểm tra SHA256 file gốc không thay đổi.
8. Thử mở file khác/đóng ứng dụng khi chưa lưu: chọn Không phải giữ phiên hiện tại; chọn Có mới bỏ bản sửa. Không được đóng phiên Word người dùng đã mở trước đó.
9. Thử tài liệu thực tế có ô gộp, nhiều font, trường tự động và nhiều trang. So sánh bản xem trước với Word trên cùng máy; ghi riêng các khác biệt, không khẳng định giống hệt trước khi so sánh.
10. Thử tài liệu có Track Changes, bảo vệ, file lỗi và file đang bị khóa. Kiểm tra thông báo lỗi, mở lại file hợp lệ và không để lại Word chạy ngầm sau khi thoát. Kiểm tra thư mục tạm `ChinhTa-*` được dọn sau phiên bình thường.
11. Tắt mạng sau khi build, chạy lại mở/quét/sửa/lưu để xác nhận offline. Thử trên bản sao tài liệu, không dùng duy nhất bản tài liệu gốc.

Ghi kết quả cùng phiên bản Windows, Word, .NET và cấu hình máy. Chỉ công bố các bước thực sự đã chạy.

12. Bản 0.1.3: mở/đóng/chuyển giữa hai tài liệu ít nhất 10 lần. Xác minh tiến trình Word riêng cũ kết thúc và các phiên Word có sẵn của người dùng vẫn hoạt động.
13. Trên máy thử nghiệm, với file giả, xác định đúng PID Word do ứng dụng tạo (không chọn Word người dùng), kết thúc PID đó rồi chọn Quét lại. Kiểm tra khôi phục nội dung đã checkpoint, thông báo mất lịch sử Undo và highlight mới; không tự lặp sửa. Nếu Word không khởi động được sau một lần thử, hiển thị thông báo dễ hiểu và đường dẫn nhật ký, không thử vô hạn.
14. Sửa rồi hoàn tác, lưu bản sao và so sánh nội dung/định dạng để xác minh checkpoint không phá Undo. Thử lỗi quyền lưu bản làm việc: phải báo, không tự khôi phục rồi âm thầm bỏ thay đổi. Kiểm tra nhật ký không chứa nội dung tài liệu.

15. Bản 0.1.4: mở lại đúng file từng gây lỗi 0x800706BE ở bước lấy trang. Thử lặp 10 lần, kiểm tra highlight đầu/cuối trang và cụm lặp. Nhật ký mới phải có `v0.1.4.0`; nếu còn lỗi, gửi các dòng mới cùng stack để xác định điểm lỗi mới. Không kết luận Office đã ổn định chỉ từ kiểm thử core trên Linux.

16. Bản 0.1.5: mở file mẫu có 5 cảnh báo, tích hai lỗi ở hai trang khác nhau. Rê chuột qua lỗi khác: highlight đổi nhưng hai ô tích không mất. Sửa các lỗi đã tích, xác nhận chỉ hai lỗi đổi, lỗi không tích giữ nguyên. Hoàn tác một lần phải phục hồi cả hai (nội dung, định dạng nếu có).
17. Bỏ tích tất cả rồi bấm sửa các lỗi đã tích: phải báo chưa chọn và không sửa. Tích tất cả/bỏ tích phải cập nhật số lượng và ô tích; cuộn danh sách ra/vào không mất trạng thái. Hủy hộp xác nhận không thay đổi tài liệu.
18. Sửa tất cả lỗi khi chưa tích mục nào: phải liệt kê số gợi ý, có cảnh báo gợi ý cần duyệt; xác nhận sửa toàn bộ và chỉ dựng lại preview một lần. Hoàn tác một lần phục hồi toàn bộ; lưu bản sao kiểm tra file gốc nguyên vẹn. Kiểm tra hai lỗi khoảng trắng gây đổi độ dài và lỗi ngữ cảnh phía sau không bị sửa sai vị trí. Sau quét các ô tích phải đặt lại.
19. Thử lỗi giữa lượt sửa trên file giả: thông báo có thể sửa một phần, không tự chạy lại lượt sửa; không tuyên bố toàn bộ thành công. Kiểm tra Hoàn tác khi Word còn hoạt động, và checkpoint gần nhất nếu Word đã mất kết nối.

20. Bản 0.1.6: kiểm tra ba lựa chọn. Mở VanBanMau trong chế độ Nhà nước (5 cảnh báo minh họa), VanBanDangMau trong chế độ Đảng (3 cảnh báo), VanBanCaNhanMau trong chế độ cá nhân (4 cảnh báo, 2 mục thủ công). Đổi chế độ phải xóa trạng thái đã tích và quét lại; Đảng/cá nhân không áp quốc hiệu Nhà nước.
21. Tạo file thử quốc hiệu đậm, đứng, TNR cỡ12 rồi13: không báo sai cỡ. Font Arial nhưng cỡ13: sửa chỉ đưa về TNR, vẫn giữ13. Tiêu ngữ13/14 hợp lệ; cặp quốc hiệu12/tiêu ngữ14 tạo cảnh báo thủ công về thống nhất. Kiểm tiêu đề Đảng15 hợp lệ,14 được sửa15. Kiểm bold, italic, màu, cỡ, căn giữa trong ô và hoàn tác nhóm trên Word thật.
22. Thử trang dọc A4, lề Nhà nước20–25/20–25/30–35/15–20mm và Đảng20/20/30/15mm. Thử sai lề, A5 giấy tờ Đảng, trang ngang bảng, lề đối xứng, section riêng mặt sau. Phải có nhắc ngoại lệ, không tự đổi trang. Mục thủ công không tích được và Sửa tất cả không sửa chúng. File cá nhân A5/lề tùy ý không bị ép theo quy định Nhà nước/Đảng.
23. Kiểm đổi profile không sửa nội dung tài liệu. Sửa tất cả chỉ sửa gợi ý cho phép, một lần Undo phục hồi lượt sửa; trường hợp hết gợi ý sửa nhưng còn cảnh báo thủ công không được tuyên bố đạt toàn bộ quy định. Mở tài liệu dùng header/footer/textbox để xác minh UI ghi rõ phạm vi chưa hỗ trợ.

24. Bản 0.1.7: thử tiêu ngữ đúng với hai dấu `-`, hai dấu `–` và trộn `-`/`–`: không có cảnh báo chỉ vì dấu. Đổi font sang Arial/cỡ12 rồi dùng sửa riêng và Sửa tất cả: font/cỡ phải sửa đúng nhưng dấu giữ nguyên. Đổi chữ thường rồi sửa: khôi phục hoa thường, giữ từng dấu cũ. Lưu bản sao và mở Word để kiểm tra nội dung lẫn định dạng; hoàn tác phục hồi cả lượt sửa.

25. Bản0.1.8: chọn Nhà nước, mở BaDuongKeThieu.docx. Dự kiến nhận diện ba vùng (không phải lúc nào cũng đủ điều kiện tự sửa). Chọn mục để xem dòng mốc và vạch cam đề xuất; chưa bấm Sửa thì lưu bản sao không được xuất hiện Line thật. Nếu báo chưa đo/ghép chắc chắn, ghi lại thông báo; không coi đó là sửa thành công.
26. Với mốc đo chắc chắn, duyệt thêm ba Line: mở bản sao bằng Word, chọn từng đối tượng để xác nhận là Shape Line, nét liền đen, đúng neo, không làm đổi nội dung/chia trang. Kiểm độ dài/center/gap bằng Word ở zoom100% và bản in/PDF. Một lần Undo lượt Line phải bỏ cả ba, rồi khôi phục/sửa lại không tạo đường trùng.
27. Kéo Line lệch, đổi width, kéo quá xa (vẫn neo trong vùng), quét lại: kiểm cảnh báo chính xác, duyệt đặt lại đúng ID không thêm Line khác. Thử group/neo xa/border/đường chéo: phải báo thủ công khi không ghép chắc chắn, không chèn trùng. Bản 0.1.9: giao diện không còn tùy chọn 4/7; độ dài cơ quan/trích yếu ở hai cận 1/3 và 1/2 được chấp nhận, vượt 1/2 quá dung sai phải cảnh báo; tiêu ngữ giữ độ dài bằng dòng chữ.
28. Sửa font/cỡ hoặc trích yếu làm xuống dòng khác rồi quét: Line phải đo theo dòng hiển thị cuối mới. Thử Sửa tất cả với lỗi chữ+Line: thông báo hai lượt, sửa chữ trước/dựng lại, sau đó sửa Line. Không dùng tọa độ cũ sau sửa chữ. Kiểm vị trí neo trong hai ô bảng, ô gộp và đổi khổ/lề; trường hợp chưa hỗ trợ phải báo thủ công.
29. Thử bản sao Bao cao.docx riêng của người dùng trên Office2021; xác minh các mục agency/motto/subject đúng ba vùng đã thống nhất. Không đưa file này hoặc nội dung lên GitHub; mẫu public dùng tên giả.

30. Bản 0.1.10, Office 2021: mở lại file từng báo Word.Check/ReadLayout 0x800A1735 ở bản 0.1.9. Kiểm tra mở/quét được, nhận diện đúng ba vùng ở bảng, hover và sửa Line trên bản sao. Nếu Word không cung cấp thông tin bảng, phải hiện “Chưa kiểm tra được đường kẻ”, vẫn kiểm chính tả và không tự thêm Line. Kiểm lại số Line sau lưu/mở, không chèn trùng. Kiểm thử giả lập chỉ kiểm thứ tự gọi và giải phóng tham chiếu; chưa thay thế lần thử Word thực tế này.

31. Bản 0.1.11: mở lại Bao cao.docx ở Office 2021. Dự kiến không còn vướng đọc COM Cells/Tables; nếu đo XPS/mốc hợp lệ, hiện ba lỗi thiếu Shape Line và vạch cam đề xuất. Sửa chữ/định dạng mốc trước nếu được yêu cầu, quét lại, chọn ba lỗi thiếu Line và bấm sửa lỗi đã chọn. Lưu bản sao, mở bằng Word: đủ ba Line, đúng vị trí/độ dài; quét lại không báo thiếu/chèn trùng. Thử Undo một nhóm Line, đổi văn bản trích yếu, quét lại để xác minh XML đọc từ tài liệu hiện tại. Nếu thông báo khác, giữ nhật ký và chụp thông báo để xác định bước chưa hỗ trợ.

32. Bản 0.1.12: mở lại tài liệu gây Word.XmlTableMap.Unavailable ở 0.1.11. Kiểm tra có ba vùng thiếu Line khi XPS đo được; sửa nhóm Line, lưu/mở bản sao và quét lại không chèn trùng. Nếu thiếu mốc, ghi lại mã XML-* và gửi diagnostics.log; mã phải có số đoạn/độ dài, không có nội dung. Kiểm không có Line trích yếu nếu đối chiếu dừng giữa khối trích yếu.

33. Bản 0.1.13: mở file gốc chưa lưu đường kẻ của lượt sửa lỗi; hover xác minh vạch đề xuất rồi bấm sửa từng Line, sau đó thử nhóm ba Line trên bản sao mới. Lưu/mở lại trong Office 2021: Line ngang, đúng độ dài/vị trí, chữ không dịch; quét lại không tạo trùng. Thử Undo nhóm. Nếu lỗi, nhật ký phải chỉ rõ Word.ApplyLine.<bước>; kiểm không còn Line mới dang dở (nếu dọn dẹp cũng thất bại, Undo cả lượt).

34. Bản 0.1.14: mở bản gốc mới, sửa một Line rồi nhóm Line; xác minh lưu/mở lại trong Word và quét không trùng. Lặp mở/sửa/Undo/đóng vài lần để kiểm không có E_FAIL tại Style.Line hoặc Cleanup, không mất kết nối và không sót Word riêng của ứng dụng sau đóng. Chưa xác minh kiểm thử COM thực tế trong cloud.

35. Bản 0.1.15 thay cơ chế ghi Line, các bước COM cũ chỉ còn giá trị lịch sử. Mở file gốc chưa lưu Line dang dở, chọn một lỗi thiếu Line rồi sửa. Dự kiến Word mở lại bản làm việc; đủ một Shape Line đúng vị trí/độ dài, chữ giữ nguyên, quét không chèn trùng. Lặp bằng nhóm ba Line trên bản sao mới. Lưu bản sao, mở trong Word, chọn từng Line để xác minh là Shape thật.
36. Kiểm Undo theo thứ tự: sửa chữ A, sửa nhóm Line B, sửa chữ C, rồi Undo ba lần. Phải lần lượt về trước C, trước B (Line mất), trước A (chữ gốc); lưu/mở xác minh. Sửa Line đã tạo bằng app phải cập nhật, không thêm trùng. Line khác phải cảnh báo thủ công.
37. Nếu candidate không mở hoặc xác minh marker/text thất bại, ứng dụng phải quay về checkpoint trước; nếu cả phục hồi lỗi, giữ file .rollback.docx và .undo.docx. Không báo sửa thành công khi Word chưa nhận đủ marker msoLine. Kiểm thời gian mở lại theo một Line/nhóm ba Line; không xuất lại Word khi rê chuột.

38. Bản 0.1.16: chọn Kiểm tra → Kiểm tra Word tự động… sau giải nén đủ ZIP. Chờ Explorer mở thư mục OfficeCheck-*. `office-check.txt` phải có PASS từng bước và RESULT PASS. Nếu FAIL, gửi chính báo cáo mới (có version/Word Build/giai đoạn/chuỗi HRESULT), không cần thử tiếp bằng văn bản thật. Mẫu dùng tọa độ cố định để kiểm thao tác; chỉ sau khi bài kiểm tra đạt mới kiểm đo/vị trí trên Bao cao.docx.
39. Trên Windows, kiểm lại nguồn ZIP luôn là snapshot không được Word mở. Trường hợp 0x80070020 ở 0.1.15 không được tái diễn. Kiểm một Line và nhóm ba Line, SaveCopy, Undo, đọc lại trong Word. Đối chiếu báo cáo Office với phiên bản ứng dụng để tránh đọc lỗi cũ.

39. Bản 0.1.17: kiểm tra Word tự động phải xác minh cả nét XPS tại tọa độ dự kiến. Mở bản gốc Bao cao.docx, kiểm đủ ba cảnh báo Line và ba vị trí rê chuột. Sửa ba mục cùng lượt, lưu bản sao/mở lại: cả ba nét phải đúng vị trí, không có pict trùng. Nếu nét thiếu/lệch, thao tác phải rollback và ghi lỗi thay vì báo đã sửa. Thử bản từng sửa bằng 0.1.16 để kiểm thay hai Line cũ và thêm Line trích yếu, không nhân đôi.

40. Bản 0.1.18: trước tiên chạy Kiểm tra → Kiểm tra Word tự động… trên mẫu giả. Phải PASS cả nhận Shape type9, vị trí nét XPS, lưu/mở lại và Undo. Nếu FAIL, thư mục OfficeCheck-* chứa office-check.txt và (nếu đã tới bước tương ứng) failed-line-candidate.docx/failed-line-preview.xps; dùng chúng để đối chiếu Word/XPS mà không cần tài liệu người dùng. Sau PASS mới thử ba Line trên DOCX thật. Kiểm legacy VML được thay thành DrawingML không trùng; rê chuột sau quét/Undo không ném lỗi trang ngoài phạm vi.



## Thử 0.1.33 — căn giữa số/ngày

Giữ gói/tag cũ. Giải nén .33 vào thư mục riêng và chạy Kiểm tra Word tự động. Kịch bản mới phải nhận đủ hai tâm lệch có thể sửa và cảnh báo khác hàng thủ công trên mẫu giả; nếu không có clip/mốc chắc chắn thì FAIL rõ ràng, không bỏ điều kiện để tạo PASS.

Thử lỗi có chủ ý sau dòng đầu để phục hồi cả lượt, không thêm Undo; sửa riêng mỗi vai trò, sửa nhóm hai dòng; kiểm lại tâm thật, giữ toàn chữ/ô trống, một Undo, lưu/mở lại. XPS được lưu với tên `position-*.xps`, gồm trước sửa, rollback, sau sửa, Undo và mở lại. Sau sửa nhóm phải chỉ còn cảnh báo khác hàng đã cố ý tạo; không được giữ cảnh báo lệch tâm cũ hoặc xóa nhầm cảnh báo khác hàng. Thử chọn lẫn chữ/phông, căn giữa và Line: UI phải yêu cầu theo nhóm trước khi ghi; Sửa tất cả xử lý lần lượt theo số đo mới.

Trên bản sao tài liệu thực, dòng chưa đủ điều kiện vẫn chỉ cảnh báo. Chưa tự căn dọc/hàng, không sửa bảng gộp/lồng. CI Windows không có Word; chỉ kết quả Office check đúng phiên bản cùng thử tài liệu thật mới xác nhận được Office 2021.


## Thử 0.1.34 — hậu kiểm ngoài dòng chọn và trạng thái quét lại

Chạy đầy đủ Kiểm tra Word tự động trên gói .34. Bài mới cố ý dịch ngày không chọn trong khi sửa riêng số; hậu kiểm phải từ chối, phục hồi cả lượt, đo lại cả hai vị trí trước khi ghi là đã phục hồi. XPS cố ý sai `position-unselected-drift.xps`, trước/sau phục hồi `position-drift-before.xps`, `position-drift-restored.xps`; `office-check.txt` phải có PASS đúng bước này và vẫn chạy hết các bài Line/căn giữa cũ.

Sau sửa riêng, mục đã hết phải biến mất và lỗi chưa chọn còn thật phải được giữ; sau sửa cả hai tâm, cảnh báo khác hàng có chủ ý vẫn thủ công. Không đánh dấu đạt dựa vào số mục đơn thuần. Nếu sửa đã commit nhưng xuất/đọc XPS hoặc quét lại bị lỗi, UI phải nói rõ lượt sửa vẫn ở bản làm việc, xóa danh sách/lựa chọn cũ, cho Quét lại hoặc một Hoàn tác; không tự ghi lại lượt cũ. Lỗi sửa trước commit giữ thông báo rollback/lỗi gốc như trước. Nhánh lỗi quét này được giả lập trong test Core, chưa gây lỗi xuất XPS có chủ ý trên Office thật.

Chưa có xác nhận runtime Office 2021 cho .34; CI Windows không có Word. Giữ nguyên main và các gói/tag cũ trong lúc dùng thử.


## Thử 0.1.35 — lưu bản sao khi lỗi/đích đang khóa

Chạy toàn bộ Kiểm tra Word tự động. Sau sửa nhóm chữ/phông trên mẫu giả, bài kiểm tạo `save-copy-existing.docx` có sẵn, gây lỗi giữa copy và thử giữ khóa không cho thay tên. Tệp đích/gốc phải giữ nguyên hash, Changed vẫn true, không còn stage đã xử lý; lưu đè Source/RecoveryPath bị chặn. Sau bỏ khóa, bản sao mới phải bằng checkpoint, Changed=false; lưu/mở và một Undo của nhóm chữ/phông vẫn chạy đạt. Các bài Line/XPS và hậu kiểm vị trí cũ vẫn chạy đầy đủ.

Trên bản sao tài liệu thực: sửa vài mục, lưu vào tên mới; sửa tiếp và lưu thay bản sao đó, mở lại để kiểm chữ/phông. Thử đích không có quyền ghi hoặc đang khóa: không báo đã lưu, không mất thay đổi và không làm hỏng bản đích cũ; sau xử lý khóa/quyền có thể lưu lại. Không thử ghi đè chính tài liệu gốc. Lưu bản sao giữ các checkpoint Hoàn tác; lưu không tạo thêm checkpoint. Kết quả CI không thay cho thử Word thật; .35 vẫn chưa có xác nhận runtime Office 2021.


## Thử 0.1.36 — tên loại và trích yếu

Chạy đầy đủ Kiểm tra Word tự động. Cuối bài có mẫu `document-heading-input.docx` với tên loại + 2 đoạn trích yếu cần sửa; dấu đoạn có phông khác không được làm sai kết quả quan sát chữ. Sửa riêng tên loại phải chỉ còn hai lỗi trích yếu; Undo khôi phục. Lỗi có chủ ý sau bước định dạng thứ hai phải phục hồi cả nhóm. Sửa nhóm + đúng một lỗi khoảng trắng phải giữ mọi chữ/ô trống còn lại, cỡ hợp lệ 14/13,5, gạch chân/căn đoạn; sau quét không còn mục định dạng cũ, lưu/mở lại và một Undo đều đúng. Bản đã sửa: `document-heading-fixed.docx`.

Mẫu `document-heading-mixed.docx` có kiểu đậm trộn trong trích yếu đầu: mục đó phải thủ công, không tích được; hai đoạn khác vẫn đủ điều kiện. Ngoài phạm vi: Công văn/V/v, tên loại chưa nhận diện/trùng, tiêu đề nằm sau văn xuôi, bảng/field/drawing/tab, trích yếu chưa có ranh giới, quá 4 đoạn hoặc profile khác. Chữ hoa/thường không tự đổi, vị trí tuyệt đối vẫn giới hạn. Nếu có Line trong cùng đoạn, bản thử có thể giữ định dạng đoạn đó thủ công vì chưa xác minh chữ thuần; không nới điều kiện để có PASS.

Giải nén vào thư mục mới, giữ samples. Thử trên bản sao tài liệu, sửa phông/chữ trước rồi quét lại trước khi sửa Line theo số đo mới. Chưa có xác nhận runtime Office 2021 cho .36; CI không thay bài Word thực tế.


## Thử 0.1.37 — giữ vùng cấu trúc khi sửa chữ/phông

Chạy toàn bộ Kiểm tra Word tự động. Cuối bài có hai thư mục `structured-hyperlink`/`structured-sdt`: bản plain và bản structured phải cùng nguyên văn, nhóm gợi ý plain cũ phải bị từ chối toàn bộ sau mở structured, không đổi checkpoint/Changed và không tạo Undo. Trích yếu có cấu trúc chứa lỗi chữ minh họa và hai khoảng trắng chỉ được cảnh báo thủ công; tên loại/đoạn thường bên cạnh vẫn sửa được. Sửa nhóm vùng thường phải chỉ thay đúng chữ đã chọn và phông đã chọn, giữ cảnh báo/định danh/chữ vùng cấu trúc, lưu/mở và một Undo phục hồi cả nhóm; nguồn không đổi.

Trên bản sao thực, nếu trích yếu có hyperlink/control/field/Line neo, không được tích sửa chữ vòng qua cảnh báo thủ công. Ô số/ngày trống không trở thành lỗi khoảng trắng. Phạm vi đối chiếu cấu trúc mới chỉ 40 đoạn đầu State; không coi hết cảnh báo là toàn bộ tài liệu an toàn/đúng thể thức. Giữ các bài Line/XPS/rollback và lưu bản sao trước. Chưa có xác nhận runtime Office 2021 cho .37.


## Thử 0.1.38 — hậu kiểm tiêu đề và dấu đoạn khác phông

Chạy hết Kiểm tra Word tự động. Hai mẫu `heading-proof-State`/`heading-proof-Party` cố ý sai chữ/phông/cỡ của quốc hiệu, tiêu ngữ hoặc tiêu đề Đảng; dấu đoạn có Arial8. Gây nghiêng sau Save phải bị phép hậu kiểm định dạng bình thường phát hiện, rollback toàn nhóm cùng lỗi khoảng trắng khác, giữ checkpoint/Changed và không tạo Undo. Không được báo đạt chỉ nhờ chữ đúng.

Sửa nhóm bình thường phải đổi đúng chữ/kiểu yêu cầu; dấu đoạn khác phông không được gây cảnh báo phông cũ cho phần chữ đã đúng. Quét lại, lưu/mở lại và một Undo phục hồi cả nhóm; nguồn giả giữ hash. Giữ nguyên các bài Line/XPS/rollback, cấu trúc và lưu bản sao trước đó. Trên bản sao tài liệu thực thử quốc hiệu/tiêu ngữ có dấu đoạn khác phông và sửa chung một lỗi khoảng trắng ngoài dòng. Chưa có xác nhận runtime Office 2021 cho .38; kiểm phông không xác nhận vị trí nét hoặc toàn bộ thể thức.


## Thử 0.1.39 — cleanup backup và thứ tự Hoàn tác

Chạy hết Kiểm tra Word tự động. Bài cuối tạo hai lượt sửa tiêu đề độc lập trên mẫu giả; lần Undo đầu gây lỗi I/O chỉ ở dọn backup. Phải trả về thành công, giữ backup, Changed=true, chữ/cảnh báo đúng trạng thái sau lượt sửa đầu. Undo tiếp theo về nguyên bản; Undo khi hết stack không đổi; nguồn giữ hash. Log có Word.CheckpointCleanup.Retained, không ghi nội dung hoặc đường dẫn người dùng.

Core Windows giữ handle không cho xóa rollback thật và kiểm các lỗi đóng/chép/mở/restore cùng cleanup. Đây là test file; CI không có Word. Thử thao tác sửa nhiều lượt, Hoàn tác từng lượt, lưu/mở và các bài nét XPS cũ trên bản sao. Chưa có xác nhận runtime Office 2021 cho .39; không tự tạo khóa/xóa tệp tài liệu thật để thử lỗi này.


## Thử 0.1.40 — từ/cụm từ lặp

Chạy hết Kiểm tra Word tự động. Mẫu `repeated-text-input.docx` có hai đề xuất xóa cụm lặp, một spacing độc lập và hai cảnh báo “đều đều”, “nhà nhà” không được tích. Lỗi cố ý sau lần xóa đầu phải rollback cả nhóm, không tạo Undo. Sửa riêng một cụm phải giữ chữ/đậm/gạch chân lần đầu và gợi ý còn lại; Undo phục hồi. Sửa nhóm ba mục phải hết các mục đã sửa trong một lượt, giữ hai manual, giữ kiểu lần đầu sau spacing dịch CP. Lưu/mở và một Undo phục hồi toàn nhóm, nguồn không đổi. `repeated-text-fixed.docx` là bản lưu.

Trên bản sao thực: thử cụm hai và ba lần, hai cụm độc lập, chọn riêng so với nhóm; xác nhận lặp có chủ ý trước khi xóa. Không kỳ vọng đối chiếu qua dấu câu/tab/ngắt đoạn hoặc cụm trên 8 tiếng, case khác, chính tả từ điển/ngữ cảnh. Sửa chữ/phông trước căn giữa/Line và quét lại theo số đo mới. Chưa có runtime Office 2021 cho .40.


## Thử 0.1.41 — xem/xuất tiến độ và chức năng

Ngay sau mở chương trình, chưa mở DOCX: Trợ giúp → Tiến độ và chức năng… phải có phiên bản, tổng nhóm 13/8/1, danh mục, mốc đã làm và bước tiếp theo; xuất TXT đọc đúng tiếng Việt. Nếu gói có build-checks.json đúng version, hiện số kiểm thử thực tế; thử bản sao thư mục thiếu file này phải báo chưa có thông tin, không tự ghi PASS. CI xác minh metadata trong thư mục publish và tạo báo cáo TXT mẫu.

Khi đang sửa DOCX: mở/đóng/xuất báo cáo không làm quét lại, xóa danh sách/tích chọn, đổi chữ/Changed hoặc thêm Undo. Thử chọn đích đang khóa/không có quyền: chỉ báo lỗi trong cửa sổ báo cáo, đích cũ giữ nguyên; vẫn sửa/lưu/Undo tài liệu như trước. Report không phải thống kê lỗi đã sửa trong DOCX. Chạy toàn bộ Kiểm tra Word tự động; bài cuối kiểm xuất khi có dirty edit, checkpoint/hash và một Undo. Chưa có xác nhận runtime Office 2021 cho .41.

## Bổ sung .42 — khoảng trắng quanh dấu phẩy/chấm phẩy (chưa xác nhận runtime)

Chạy bài Word đầy đủ. Cuối bài có `punctuation-input.docx` và `punctuation-fixed.docx`: hai dấu câu, một cụm lặp, một khoảng trắng độc lập. Sửa riêng một dấu câu giữ các lỗi còn lại; lỗi cố ý sau lần sửa dấu câu đầu trong nhóm phải phục hồi toàn bộ, không tạo Undo. Sửa nhóm một lần phải quét hết mục đã sửa, giữ đậm/gạch chân chữ trước dấu câu, một Undo trả cả nhóm, lưu/mở đúng bản và hash nguồn không đổi. Đối chiếu dòng số/mã/địa chỉ không đổi. Thử thêm tab, NBSP, từ một chữ cái, trường cấu trúc và kế hoạch cũ; không cho sửa nếu không còn đủ điều kiện. Đọc giới hạn C14, không suy thành hỗ trợ mọi dấu câu.

Trong Trợ giúp, báo cáo phải có C14 và mốc .42, đúng version và số kiểm CI đi kèm; xuất TXT phải giữ tài liệu, lựa chọn và Undo như .41. CI chỉ chứng minh Core/WPF/gói, chưa chứng minh Word/Office 2021.

## Bổ sung .43 — mép trong ngoặc tròn (chưa xác nhận runtime)

Bài Word thêm `parenthesis-input.docx`/`parenthesis-fixed.docx`: hai mép ngoặc, cụm lặp, dấu câu và khoảng trắng ngoài. Sửa riêng một mép giữ mục không chọn và đậm/gạch chân từ đầu trong ngoặc. Lỗi cố ý sau xóa mép trong nhóm phải phục hồi toàn chữ/kiểu, không tạo Undo. Sửa cả nhóm một lần, quét bỏ đúng các mục, một Undo phục hồi cả nhóm; lưu/mở giữ chữ/kiểu và hash nguồn. Dòng ô trống/số/mã/ký hiệu trong mẫu phải nguyên vẹn.

Thử riêng `( công,tác )`: sửa hai mép ngoặc, dấu phẩy chưa chọn phải còn gợi ý sau quét; chọn cả hai loại phải ra `(công, tác)`. `(    )` không bị gộp thành `( )` khi sửa tất cả; lỗi khoảng trắng độc lập vẫn sửa được. Ngoặc lồng/thiếu, tab/NBSP, số/toàn hoa, dấu công thức và dòng ngày có chú thích không tạo C15. Báo cáo Trợ giúp có C15/mốc .43/version và số kiểm đúng gói. Chưa coi Core/WPF/gói là xác nhận Word/Office 2021.

## Bổ sung .44 — dấu cách trước dấu kết câu cuối đoạn (chưa xác nhận runtime)

Bài Word thêm `sentence-end-input.docx` và bản lưu `sentence-end-fixed.docx`: ba dấu .?! cuối đoạn, năm lỗi chữ/ngoặc độc lập, các dòng ngoại lệ. Sửa riêng một C16 giữ lỗi khác và kiểu phần chữ; lỗi cố ý sau C16 đầu trong nhóm phải rollback toàn chữ/kiểu, không tạo Undo. Sửa cả nhóm một lượt, quét hết đúng mục, một Undo trả toàn nhóm, lưu/mở giữ chữ/kiểu/hash nguồn. Các dòng số, TS, địa chỉ và dấu lửng giữ nguyên.

Thử Unicode tổ hợp/surrogate trước lỗi, khoảng trắng cuối và dấu đoạn/ô; C16 chỉ xóa ASCII space trước dấu, không ghi lại từ/dấu câu. Kiểm ngoại lệ tab/NBSP, dấu giữa câu/chuỗi dấu/header/ngày. Báo cáo có C16/mốc .44 và dữ liệu kiểm đúng gói. Chưa coi CI là xác nhận Word/Office 2021.
