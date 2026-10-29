# Đợt 61–64 — vùng đánh dấu, biểu mẫu, lưu nền và tiến độ

Ngày 30/10/2026 (UTC+7), tiếp tục từ `7bc6861de0c506e313da79070fdd173caa0af3b3`, nhánh `feature/isolated-spacing-proof-batch22`. Mã 0.1.80 tiếp tục tích lũy sau đợt 57–60; không tạo ZIP mới hoặc thay gói 0.1.79/main/tag cũ. Bộ đọc thử nghiệm OFF, khoảng ký tay 80 pt và quyền sửa Exactly/AtLeast giữ nguyên.

## 61. Không sửa chữ trong vùng đánh dấu chưa có proof giữ cấu trúc

Tái hiện bằng nguồn XML và native snapshots: một cặp bookmark/comment/permission hợp lệ bao chữ vẫn được cấp C01/lặp chữ. Writer dùng Range.Text; hậu kiểm chữ/phông hiện có không chứng minh thẻ/identity của annotation còn nguyên. Đây là khoảng trống quyền sửa, chưa phải lỗi xóa annotation đã quan sát trên Word thật.

Thêm một tập paragraph được bảo vệ từ cùng main-body XML; dùng chung trong map, snapshot và outline evidence. Chỉ hạ quyền sửa chữ ở vùng có nội dung được ghép; giữ native mapping, quan sát format-only và các paragraph sau không liên quan. Chặn đường tab/recipient/signature mở lại quyền. Vùng qua paragraph được giữ thận trọng kể cả khi chỉ có ranh giới paragraph.

Metadata điểm cùng paragraph với cặp duy nhất, như _GoBack, và lone orphan-start theo kiểm cũ không bị coi là vùng có nội dung. Khi có cả start/end nhưng pairing trùng/đảo chưa rõ, giữ phần envelope có nội dung để duyệt. Khóa so sánh ID thập phân nhận giá trị số tương đương mà không sửa attribute nguồn hoặc parse số nguyên bị giới hạn. Không ghép khác family/giá trị ID, không trộn story textbox hoặc đoán CP.

Đây chưa phải hỗ trợ bảo toàn mọi bookmark/comment/permission range hoặc mọi metadata lỗi. Không tự thay/xóa annotation để cho phép sửa.

## 62. Ô chọn tròn và nhãn ngày sinh đầy đủ

Tái hiện: `Giới tính: Nam (    X    ) Nữ (        )`, `(    ✓    )` hoặc `(    x    )` còn có hai C01 thu hẹp ô đã đánh dấu. Nay nhận đúng cặp tròn như cặp vuông, một dấu X/x/✓/✔ với ASCII/NBSP/NNBSP padding; ngoặc không khớp không tạo ngoại lệ. Văn xuôi `(    nội dung    )` vẫn theo quy tắc ngoặc hiện có.

`Ngày, tháng, năm sinh:    /    /____` trước đây không được nhận nhãn, còn hai C01 vào ô trống. Bổ sung chính xác nhãn có hai dấu phẩy và colon bắt buộc, dùng nguyên grammar slash và điều kiện từng slot blank/placeholder. Nhận NFC/NFD/khoảng ngang cũ, giữ source offset. Không mở dạng thiếu comma/colon hoặc mọi nhãn biểu mẫu. Bài Word sửa chữ thêm ba mẫu tương ứng; chưa chạy Word thật.

## 63. Chỉ xác nhận sau khi Word hoàn tất lưu

Theo API Word, Save có thể trả về trong khi còn tác vụ lưu nền. Harness gọi Checkpoint/SaveCopy thật với managed Save ghi byte dở rồi trả về đã tái hiện checkpoint được tin cậy, byte dở được công bố và folder phục hồi bị xóa, dù pending=1/Saved=false.

`WordSaveCompletion` đợi pending=0, đọc Saved=true rồi kiểm pending=0 lần nữa; một deadline monotonic 30 giây, poll 50 ms. Trạng thái âm, lỗi đọc, Saved=false hoặc timeout không tạo checkpoint đã xác minh và không bắt đầu công bố bản sao. Không đổi `Options.BackgroundSave`, đặt `Saved=true` hoặc gọi Save lại. Giữ nguyên nguyên nhân lỗi và chính sách giữ dữ liệu/Undo của đợt trước.

Deadline giới hạn vòng quan sát; không cưỡng bức hủy một Save/COM getter đang treo. Bộ kiểm managed không thay kết quả trên Word thật. Cơ sở API: [BackgroundSavingStatus](https://learn.microsoft.com/en-us/office/vba/api/word.application.backgroundsavingstatus), [Document.Saved](https://learn.microsoft.com/en-us/office/vba/api/word.document.saved), [BackgroundSave](https://learn.microsoft.com/en-us/office/vba/api/word.options.backgroundsave).

## 64. Dọn cửa sổ tiến độ khởi động muộn

Nguồn cũ có timeout 8 giây của constructor: nếu worker STA tạo view sau timeout thì không còn IDisposable trả về để đóng, trong khi view không cho người dùng đóng. Thêm yêu cầu đóng atomically, publish ownership trước Show, kiểm hủy trước/sau Show và dọn view/dispatcher trên chính STA ở mọi đường thoát.

Không Show view tạo muộn khi đã biết timeout; giữ lỗi khởi động trước lỗi cleanup, Finish/Dispose lặp an toàn. Public timeout vẫn 8 giây, không Join/chờ chéo UI trong production hoặc đưa Word/COM sang STA tiến độ. Test dùng factory/gate và timeout ngắn; chỉ compile trên Linux, cần Windows thực thi WPF.

## Kiểm chứng

Core/OpenXML cục bộ: **8086 kiểm qua**, tăng **412** từ 7674. Build DesktopChecks Release: **0 warning/error**. Harness riêng đã qua 143 kiểm annotation, 248 biểu mẫu, 21 hoàn tất lưu và 154 checkpoint/Save; số nằm trong runner tương ứng sau tích hợp, không cộng lặp. Cửa sổ tiến độ có 11 kiểm WPF mới, chưa thực thi WPF trên Linux. Fixture sửa chữ với ba FormRows mới qua 11 kiểm schema/part/mô hình nguồn. Windows CI của mã cuối đợt đang chờ; tổng Windows lấy từ evidence đúng lượt, không suy từ Linux.

Word thực vẫn **NOT_RUN**. Không xuất ZIP mới, giữ 13 nhóm Word và danh mục 33 nhóm đề xuất có điều kiện, 8 thủ công, 1 minh họa.
