using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
internal static class OfficeInitialRepeatCheck
{
    public static void Run(WordSession test,string source,string folder,Action<string> pass)
    {
        string path=Create(source,folder);byte[] digest=SHA256.HashData(File.ReadAllBytes(path));
        test.Profile=DocumentProfile.State;test.Open(path);string original=test.ReadCheckText();
        bool Chosen(Issue i)=>i.CanFix && i.Kind==FixKind.RepeatedText;
        var edits=test.Check().Where(Chosen).OrderBy(i=>i.Start).ToArray();
        if(edits.Length!=2 || edits[0].Original!=" kế hoạch" || edits[1].Original!=" báo cáo")
            throw new InvalidOperationException("Mẫu lặp đầu đoạn chưa nhận đúng hai cụm hoặc đã sửa hoa/thường giữa câu/từ đơn.");
        const string kept="Kế hoạch";
        var probe=new Issue(edits[0].Start-kept.Length,kept.Length,kept,kept,"",false);var decoration=test.ReadCheckDecorations(probe);
        if(decoration.Bold!=-1 || decoration.Underline!=1)throw new InvalidOperationException("Mẫu lặp đầu đoạn thiếu kiểu chữ cần giữ.");
        test.Fix(edits[0]);
        if(test.ReadCheckText()!=original.Replace("Kế hoạch kế hoạch đã duyệt.","Kế hoạch đã duyệt.",StringComparison.Ordinal) ||
            test.Check().Count(Chosen)!=1 || test.ReadCheckDecorations(probe)!=decoration || test.CheckUndoCount!=1)
            throw new InvalidOperationException("Sửa riêng lặp đầu đoạn chưa giữ lần đầu/kiểu chữ hoặc lỗi chưa chọn.");
        test.Undo();if(test.ReadCheckText()!=original || test.CheckUndoCount!=0)throw new InvalidOperationException("Undo lặp đầu đoạn chưa về nguyên bản.");
        pass("Lặp đầu đoạn: chọn riêng, giữ nguyên chữ hoa/đậm/gạch chân và lỗi chưa chọn, quét lại/Undo");
        edits=test.Check().Where(Chosen).ToArray();bool failed=false;
        try{test.FixTextFailureProbe(edits,1);}catch(InvalidOperationException error) when(error.InnerException is OfficeTextProbeException){failed=true;}
        if(!failed || test.ReadCheckText()!=original || test.Changed || test.CheckUndoCount!=0 || test.ReadCheckDecorations(probe)!=decoration)
            throw new InvalidOperationException("Rollback lặp đầu đoạn chưa phục hồi cả lượt.");
        test.FixMany(test.Check().Where(Chosen));
        string expected=original.Replace("Kế hoạch kế hoạch đã duyệt.","Kế hoạch đã duyệt.",StringComparison.Ordinal)
            .Replace("Báo cáo báo cáo.","Báo cáo.",StringComparison.Ordinal);
        if(test.ReadCheckText()!=expected || test.Check().Any(Chosen) || test.CheckUndoCount!=1 || test.ReadCheckDecorations(probe)!=decoration)
            throw new InvalidOperationException("Sửa nhóm lặp đầu đoạn chưa đúng nguyên văn/kiểu hoặc còn lỗi đã sửa.");
        string saved=Path.Combine(folder,"initial-repeat-fixed.docx");test.SaveCopy(saved);test.Undo();
        if(test.ReadCheckText()!=original || test.ReadCheckDecorations(probe)!=decoration)throw new InvalidOperationException("Một Undo chưa phục hồi cả nhóm lặp đầu đoạn.");
        test.Open(saved);
        if(test.ReadCheckText()!=expected || test.Check().Any(Chosen) || test.ReadCheckDecorations(probe)!=decoration ||
            !SHA256.HashData(File.ReadAllBytes(path)).SequenceEqual(digest))throw new InvalidOperationException("Lưu/mở lặp đầu đoạn chưa đúng hoặc tệp nguồn bị đổi.");
        pass("Lặp đầu đoạn: sửa nhóm, rollback, một Undo, lưu/mở; giữ nguồn, chữ hoa và kiểu lần đầu");
    }
    private static string Create(string source,string folder)
    {
        string path=Path.Combine(folder,"initial-repeat-input.docx");File.Copy(source,path);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);var entry=zip.GetEntry("word/document.xml")!;
        XDocument xml;using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XElement Run(string value,bool kept=false)=>new(w+"r",new XElement(w+"rPr",
            new XElement(w+"rFonts",new XAttribute(w+"ascii","Times New Roman"),new XAttribute(w+"hAnsi","Times New Roman")),
            new XElement(w+"sz",new XAttribute(w+"val",26)),new XElement(w+"b",new XAttribute(w+"val",kept?1:0)),
            new XElement(w+"u",new XAttribute(w+"val",kept?"single":"none"))),new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),value));
        var rows=new[]{new XElement(w+"p",Run("Kế hoạch",true),Run(" kế hoạch đã duyệt.")),
            new XElement(w+"p",Run("Báo cáo báo cáo.")),new XElement(w+"p",Run("Theo Kế hoạch kế hoạch")),new XElement(w+"p",Run("Đều đều"))};
        var body=xml.Descendants(w+"body").Single();
        foreach(var row in rows)if(body.Element(w+"sectPr") is XElement section)section.AddBeforeSelf(row);else body.Add(row);
        entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);return path;
    }
}
