using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
internal static class OfficeParagraphEndCheck
{
    public static void Run(WordSession test,string source,string folder,Action<string> pass)
    {
        string path=Create(source,folder);byte[] digest=SHA256.HashData(File.ReadAllBytes(path));
        test.Profile=DocumentProfile.State;test.Open(path);string original=test.ReadCheckText();
        bool Chosen(Issue i)=>i.CanFix && i.Kind is FixKind.ParagraphEndSpace or FixKind.SentenceEndSpace;
        var edits=test.Check().Where(Chosen).ToArray();
        var trailing=edits.Where(i=>i.Kind==FixKind.ParagraphEndSpace).OrderBy(i=>i.Start).ToArray();
        if(trailing.Length!=2 || edits.Length!=3)throw new InvalidOperationException("Mẫu cuối đoạn chưa phân biệt hai khoảng cuối, khoảng trước dấu câu và các vùng phải giữ.");
        const string kept="hoàn thành";
        var probe=new Issue(trailing[0].Start-"  .".Length-kept.Length,kept.Length,kept,kept,"",false);
        var decoration=test.ReadCheckDecorations(probe);
        if(decoration.Bold!=-1 || decoration.Underline!=1)throw new InvalidOperationException("Mẫu cuối đoạn thiếu kiểu chữ cần giữ.");
        test.Fix(trailing[0]);
        if(test.ReadCheckText()!=original.Replace("Đã hoàn thành  .   \r","Đã hoàn thành  .\r",StringComparison.Ordinal) ||
            test.Check().Count(i=>i.CanFix && i.Kind==FixKind.ParagraphEndSpace)!=1 ||
            test.ReadCheckDecorations(probe)!=decoration || test.CheckUndoCount!=1)
            throw new InvalidOperationException("Sửa riêng cuối đoạn chưa giữ khoảng trước/dấu kết đoạn/kiểu chữ hoặc còn lỗi đã sửa.");
        test.Undo();if(test.ReadCheckText()!=original || test.CheckUndoCount!=0)
            throw new InvalidOperationException("Undo cuối đoạn chưa về nguyên bản.");
        pass("Khoảng trắng cuối đoạn: sửa riêng, giữ khoảng trước và kiểu đậm/gạch chân, quét lại và Undo");
        edits=test.Check().Where(Chosen).ToArray();bool rolledBack=false;
        try{test.FixTextFailureProbe(edits,1);}catch(InvalidOperationException error) when(error.InnerException is OfficeTextProbeException){rolledBack=true;}
        if(!rolledBack || test.ReadCheckText()!=original || test.Changed || test.CheckUndoCount!=0 || test.ReadCheckDecorations(probe)!=decoration)
            throw new InvalidOperationException("Rollback cuối đoạn chưa phục hồi cả lượt.");
        edits=test.Check().Where(Chosen).ToArray();test.FixMany(edits);
        string expected=original.Replace("Đã hoàn thành  .   \r","Đã hoàn thành.\r",StringComparison.Ordinal)
            .Replace("Nội dung đã duyệt?  \r","Nội dung đã duyệt?\r",StringComparison.Ordinal);
        if(test.ReadCheckText()!=expected || test.Check().Any(Chosen) || test.CheckUndoCount!=1 || test.ReadCheckDecorations(probe)!=decoration)
            throw new InvalidOperationException("Sửa nhóm cuối đoạn chưa đúng chữ/kiểu hoặc dấu kết đoạn.");
        string saved=Path.Combine(folder,"paragraph-end-fixed.docx");test.SaveCopy(saved);test.Undo();
        if(test.ReadCheckText()!=original || test.ReadCheckDecorations(probe)!=decoration)
            throw new InvalidOperationException("Một Undo chưa phục hồi nhóm trước/sau dấu kết câu.");
        test.Open(saved);
        if(test.ReadCheckText()!=expected || test.Check().Any(Chosen) || test.ReadCheckDecorations(probe)!=decoration ||
            !SHA256.HashData(File.ReadAllBytes(path)).SequenceEqual(digest))
            throw new InvalidOperationException("Lưu/mở cuối đoạn chưa đúng hoặc nguồn mẫu bị đổi.");
        pass("Khoảng trắng cuối đoạn: nhóm trước/sau, rollback, một Undo, lưu/mở và giữ nguyên nguồn/kiểu/dấu kết đoạn");
        RunSemicolon(test,source,folder,pass);
    }
    private static void RunSemicolon(WordSession test,string source,string folder,Action<string> pass)
    {
        string path=Path.Combine(folder,"semicolon-end-input.docx");File.Copy(source,path);
        using(var zip=ZipFile.Open(path,ZipArchiveMode.Update))
        {
            var entry=zip.GetEntry("word/document.xml")!;XDocument xml;
            using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
            XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            XElement Row(string value)=>new(w+"p",new XElement(w+"r",new XElement(w+"rPr",
                new XElement(w+"rFonts",new XAttribute(w+"ascii","Times New Roman"),new XAttribute(w+"hAnsi","Times New Roman")),
                new XElement(w+"sz",new XAttribute(w+"val",26))),new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),value)));
            var body=xml.Descendants(w+"body").Single();
            foreach(string text in new[]{"Nội dung  kế hoạch kế hoạch;   ","Công tác đã xong;  ","Số:        /BC-ABC"})
                if(body.Element(w+"sectPr") is XElement section)section.AddBeforeSelf(Row(text));else body.Add(Row(text));
            entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);
        }
        byte[] hash=SHA256.HashData(File.ReadAllBytes(path));test.Open(path);test.Profile=DocumentProfile.State;
        string original=test.ReadCheckText();var gaps=test.Check().Where(i=>i.CanFix && i.Kind==FixKind.ParagraphEndSpace).OrderBy(i=>i.Start).ToArray();
        if(gaps.Length!=2)throw new InvalidOperationException("Mẫu chấm phẩy chưa nhận đúng hai khoảng cuối đoạn.");
        test.Fix(gaps[0]);
        if(test.ReadCheckText()!=original.Replace("kế hoạch;   ","kế hoạch;",StringComparison.Ordinal) ||
            test.Check().Count(i=>i.CanFix && i.Kind==FixKind.ParagraphEndSpace)!=1)
            throw new InvalidOperationException("Sửa riêng sau chấm phẩy làm đổi mục chưa chọn hoặc còn lỗi đã sửa.");
        test.Undo();test.Check();
        Issue[] Selected()
        {
            var current=test.Check();var ends=current.Where(i=>i.CanFix && i.Kind==FixKind.ParagraphEndSpace).OrderBy(i=>i.Start).ToArray();
            int start=ends[0].Start-"Nội dung  kế hoạch kế hoạch;".Length,end=ends[^1].Start+ends[^1].Length;
            return current.Where(i=>i.CanFix && i.Start>=start && (long)i.Start+i.Length<=end &&
                i.Kind is FixKind.ParagraphEndSpace or FixKind.RepeatedText or FixKind.Text).ToArray();
        }
        var selected=Selected();bool restored=false;
        try{test.FixTextFailureProbe(selected,1);}catch(InvalidOperationException e) when(e.InnerException is OfficeTextProbeException){restored=true;}
        if(!restored || test.ReadCheckText()!=original || test.CheckUndoCount!=0 || test.Changed)
            throw new InvalidOperationException("Rollback chấm phẩy chưa phục hồi toàn lượt.");
        selected=Selected();test.FixSelection(selected);
        string expected=original.Replace("Nội dung  kế hoạch kế hoạch;   ","Nội dung kế hoạch;",StringComparison.Ordinal)
            .Replace("Công tác đã xong;  ","Công tác đã xong;",StringComparison.Ordinal);
        if(test.ReadCheckText()!=expected || test.Check().Any(i=>i.CanFix && i.Kind==FixKind.ParagraphEndSpace) || test.CheckUndoCount!=1)
            throw new InvalidOperationException("Sửa chung chấm phẩy/chữ chưa giữ dấu câu/ô trống hoặc một Undo.");
        string saved=Path.Combine(folder,"semicolon-end-fixed.docx");test.SaveCopy(saved);test.Undo();
        if(test.ReadCheckText()!=original)throw new InvalidOperationException("Undo chấm phẩy chưa phục hồi chữ.");
        test.Open(saved);
        if(test.ReadCheckText()!=expected || !hash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(path))))
            throw new InvalidOperationException("Lưu/mở chấm phẩy chưa đúng hoặc nguồn đổi.");
        pass("Khoảng cuối sau chấm phẩy: chọn riêng/chung chữ, giữ dấu/ô trống, rollback, một Undo/lưu-mở/nguồn");
    }
    private static string Create(string source,string folder)
    {
        string path=Path.Combine(folder,"paragraph-end-input.docx");File.Copy(source,path);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);var entry=zip.GetEntry("word/document.xml")!;
        XDocument xml;using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XElement Run(string value,bool kept=false)=>new(w+"r",new XElement(w+"rPr",
            new XElement(w+"rFonts",new XAttribute(w+"ascii","Times New Roman"),new XAttribute(w+"hAnsi","Times New Roman")),
            new XElement(w+"sz",new XAttribute(w+"val",26)),new XElement(w+"b",new XAttribute(w+"val",kept?1:0)),
            new XElement(w+"u",new XAttribute(w+"val",kept?"single":"none"))),
            new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),value));
        var rows=new[]{new XElement(w+"p",Run("Đã "),Run("hoàn thành",true),Run("  .   ")),
            new XElement(w+"p",Run("Nội dung đã duyệt?  ")),new XElement(w+"p",Run("Giờ mở cửa 12:30.   ")),
            new XElement(w+"p",Run("Nội dung còn để trống    ")),new XElement(w+"p",Run("Số:        /BC-ABC"))};
        var body=xml.Descendants(w+"body").Single();
        foreach(var row in rows)if(body.Element(w+"sectPr") is XElement section)section.AddBeforeSelf(row);else body.Add(row);
        entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);return path;
    }
}
