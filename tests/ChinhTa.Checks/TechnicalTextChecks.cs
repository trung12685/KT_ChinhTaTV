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
        foreach(string extension in new[]{"doc","docx","docm","dot","dotx","dotm","rtf","odt","ott",
            "xls","xlsx","xlsm","xlsb","xlt","xltx","xltm","ods","ots","ppt","pptx","pptm","pot","potx","potm","pps","ppsx","ppsm","odp","otp"})
        {
            string text="Tên tệp Báo  cáo kế hoạch kế hoạch con đo."+extension;
            var issues=Checker.Check([new(31,text)],DocumentProfile.Personal);
            assert(TechnicalTextContext.IsTechnical(text) && issues.Count==1 && !issues[0].CanFix && issues[0].Start==31 &&
                issues[0].Original==text && issues[0].Replacement==text,
                "Bare Office/OpenDocument filenames preserve significant spaces, repeated words and demo-spelling text: "+extension);
        }
        foreach(string text in new[]{"Tên tệp kế hoạch kế hoạch a\u0301.docm","Tên tệp kế hoạch kế hoạch a\u0323.odt","Tên tệp Báo  cáo.RTF\r\a"})
            assert(TechnicalTextContext.IsTechnical(text) && !Checker.Check([new(0,text)],DocumentProfile.Personal).Any(i=>i.CanFix),
                "Filename detection preserves decomposed final letters, uppercase extensions and Word paragraph endings");
        foreach(string text in new[]{"Nội dung kế hoạch kế hoạch đã duyệt.","Công tác ngày/đêm kế hoạch kế hoạch đã xong."})
            assert(!TechnicalTextContext.IsTechnical(text) && Checker.Check([new(0,text)],DocumentProfile.Personal).Any(i=>i.Kind==FixKind.RepeatedText && i.CanFix),
                "Additional known filename suffixes do not turn ordinary prose into a technical filename scope");
        foreach(var sample in new (string Text,bool Repeats)[]
        {
            ("Console.WriteLine(\"kế hoạch kế hoạch\");",true),
            ("print(\"Báo  cáo\")",false),
            ("Tên hàm module.ghiNhan( 'kế hoạch kế hoạch').",true),
            ("Tên hàm ghi\u0301Nhan(\"kế hoạch kế hoạch\").",true),
            ("Mở /tài liệu tài liệu",true),
            ("/tài liệu tài liệu",true),
            ("   /Báo  cáo",false),
            ("Đường dẫn: /tài liệu tài liệu",true),
            ("File: /Báo  cáo",false),
            ("Mở \"/Báo  cáo\"",false),
            ("Mở (/tài liệu tài liệu)\r\a",true),
            ("Địa chỉ \"tài liệu tài liệu\"@co\u0302ngty.vn",true),
            ("Địa chỉ \"tài liệu tài liệu\"@côngty.vn",true),
            ("Địa chỉ \"tài liệu tài liệu\"@te\u0302n.co\u0302ng",true)
        })
        {
            string whole=sample.Text.TrimEnd('\r','\a','\n');
            assert(TechnicalTextContext.IsTechnical(sample.Text),
                "Quoted call arguments, complete root filenames and canonically decomposed email domains retain technical context");
            var direct=RepeatedTextRules.Check(new(83,sample.Text));
            assert(sample.Repeats?direct.Count==1 && !direct[0].CanFix && direct[0].Start==83 &&
                direct[0].Original==whole && direct[0].Replacement==whole:direct.Count==0,
                "Literal repetitions remain exact whole-paragraph manual notices; spacing-only literals do not invent a repetition");
            foreach(var profile in Enum.GetValues<DocumentProfile>())
            {
                var issues=Checker.Check([new(83,sample.Text)],profile);
                assert(issues.Any(i=>!i.CanFix && i.Start==83 && i.Original==whole && i.Replacement==whole) &&
                    !issues.Any(i=>i.CanFix && !i.IsStyleOnly && i.Original!=i.Replacement),
                    "The shared review path preserves literal words and double spaces in every profile while retaining the manual warning");
            }
        }
        foreach(string text in new[]{"Nội dung / ta / tài liệu tài liệu đã duyệt.",
            "Công tác ngày/đêm kế hoạch kế hoạch đã xong.",
            "Cần duyệt (\"kế hoạch kế hoạch\") hôm nay.",
            "Nội dung (\"Báo  cáo\") cần duyệt."})
            assert(!TechnicalTextContext.IsTechnical(text) && Checker.Check([new(0,text)],DocumentProfile.Personal)
                .Any(i=>i.CanFix && i.Original!=i.Replacement),
                "A prose slash or spaced prose parenthesis containing a quotation does not become a function call or root filename");
        var oldProse=Checker.Check([new(0,"Nội dung  báo cáo.")],DocumentProfile.Personal).Where(i=>i.CanFix).ToArray();
        foreach(string suffix in new[]{" print(\"mẫu\")"," Mở /tài liệu"," nhan@co\u0302ngty.vn"})
        {
            bool rejected=false;
            try{CurrentTextPlan.Validate(oldProse,Checker.Check([new(0,"Nội dung  báo cáo."+suffix)],DocumentProfile.Personal));}
            catch(InvalidOperationException){rejected=true;}
            assert(rejected,"An unchanged selected whitespace span loses permission when current text acquires the new technical context");
        }
        foreach(string text in new[]{"Số:  /BC-ABCD  nội dung.\r","Số:        /BC-ABCD  (bản sao)\r"})
        {
            var spaces=Checker.Check([new(41,text)]).Where(i=>i.Kind==FixKind.Text && i.Replacement==" ").ToArray();
            int fieldEnd=text.IndexOf("/BC-ABCD",StringComparison.Ordinal)+"/BC-ABCD".Length;
            assert(!TechnicalTextContext.IsTechnical(text) && spaces.Length==1 && spaces[0].Start==41+fieldEnd &&
                spaces[0].Original=="  ",
                "An administrative blank-number slash is not a root filename; only field padding is exempt and prose spacing retains its exact offset");
        }
        const string guarded=@"Mở C:\tài liệu tài liệu\bao-cao.docx";
        var unsafeRow=RepeatedTextRules.Check(new(40,guarded,TextEditable:false));
        assert(unsafeRow.Count==1 && !unsafeRow[0].CanFix && unsafeRow[0].Start==40 && unsafeRow[0].Original==guarded,
            "The technical context cannot bypass the structured/unverified text permission gate");
    }
}
