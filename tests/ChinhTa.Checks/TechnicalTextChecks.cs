using ChinhTa.Core;

public static class TechnicalTextChecks
{
    public static void Run(Action<bool,string> assert)
    {
        string[] technical=
        [
            @"Mở C:\tài liệu tài liệu\bao-cao.docx",
            @"Mở D:/tài liệu tài liệu/bao-cao.docx",
            @"Mở \\server\tài liệu tài liệu\bao-cao.docx",
            @"Mở ./tài liệu tài liệu/bao-cao.docx",
            @"Mở ../tài liệu tài liệu/bao-cao.docx",
            @"Mở ~/tài liệu tài liệu/bao-cao.docx",
            @"Mở /home/tài liệu tài liệu/bao-cao.docx",
            "Mở /Báo cáo/tài liệu tài liệu/bao-cao",
            "Mở /a\u0301 b/tài liệu tài liệu/bao-cao",
            @"Mở TaiLieu\tài liệu tài liệu\bao-cao.docx",
            "Mở tệp tài liệu tài liệu.docx",
            "Địa chỉ example.vn/tài liệu tài liệu/bao-cao",
            "Địa chỉ https://example.vn/tài liệu tài liệu/bao-cao.docx",
            "Địa chỉ file:C:\\tài liệu tài liệu\\bao-cao.docx",
            "Địa chỉ mailto:nhan@example.vn; tài liệu tài liệu.",
            "Địa chỉ \"tài liệu tài liệu\"@example.vn",
            "Mã `tài liệu tài liệu` cần giữ nguyên.",
            "Mã `tài liệu tài liệu chưa đóng.",
            "Giá trị config = \"tài liệu tài liệu\".",
            "Dữ liệu {\"ten\": \"tài liệu tài liệu\"}",
            "Dữ liệu <value>tài liệu tài liệu</value>",
            "Mở C:\\ta\u0300i lie\u0323\u0302u ta\u0300i lie\u0323\u0302u\\bao-cao.docx",
            "😀 Mở C:\\tài liệu tài liệu\\bao-cao.docx"
        ];
        foreach(string text in technical)
        {
            assert(TechnicalTextContext.IsTechnical(text),"Explicit path/address/code context is recognized without changing its text");
            var direct=RepeatedTextRules.Check(new(73,text));
            assert(direct.Count==1 && direct.All(i=>!i.CanFix && !i.BatchSafe && i.Start==73 && i.Original==text && i.Replacement==text),
                "Repeated words in technical text become one source-exact manual paragraph notice");
            foreach(var profile in Enum.GetValues<DocumentProfile>())
            {
                var found=Checker.Check([new(73,text)],profile);
                assert(found.Any(i=>!i.CanFix) && !found.Any(i=>i.CanFix && !i.IsStyleOnly && i.Original!=i.Replacement),
                    "Technical text retains its complete path/address/literal under all document profiles");
            }
        }
        foreach(string text in new[]{"Nội dung tài liệu tài liệu đã được duyệt.","Nội dung và/hoặc tài liệu tài liệu đã được duyệt.","Công tác ngày/đêm tài liệu tài liệu đã được duyệt.","Nội dung / ta / tài liệu tài liệu đã được duyệt."})
        {
            assert(!TechnicalTextContext.IsTechnical(text),"A prose slash is not by itself a path/code classification");
            var edit=RepeatedTextRules.Check(new(73,text)).Single(i=>i.CanFix);
            assert(edit.Start>=73 && edit.Original==text.Substring(edit.Start-73,edit.Length) && edit.Replacement=="" && !edit.BatchSafe,
                "Ordinary prose keeps its existing exact-span reviewable repetition correction");
        }
        foreach(string text in new[]{@"Mở C:\Báo  cáo\mau.docx",@"Mở \\server\Báo  cáo\mau.docx","Giá trị config = \"Báo  cáo\".","Đường dẫn `Báo  cáo` cần được giữ nguyên.","Tệp Báo  cáo.docx"})
        {
            var found=Checker.Check([new(9,text)]);
            assert(TechnicalTextContext.IsTechnical(text) && !found.Any(i=>i.CanFix && !i.IsStyleOnly && i.Original!=i.Replacement),
                "Significant repeated ASCII spaces in filenames and literals cannot be collapsed by the shared Checker path");
        }
        foreach(string text in new[]{@"Mở C:\TaiLieu\mau.docx","https://example.vn/mau.docx","Giá trị config = 12;"})
            assert(RepeatedTextRules.Check(new(0,text)).Count==0,"A technical paragraph without an actual mechanical repetition receives no repetition warning");
        const string guarded=@"Mở C:\tài liệu tài liệu\bao-cao.docx";
        var unsafeRow=RepeatedTextRules.Check(new(40,guarded,TextEditable:false));
        assert(unsafeRow.Count==1 && !unsafeRow[0].CanFix && unsafeRow[0].Start==40 && unsafeRow[0].Original==guarded,
            "The technical context cannot bypass the structured/unverified text permission gate");
    }
}
