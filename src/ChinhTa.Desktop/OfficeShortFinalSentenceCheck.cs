using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
internal static class OfficeShortFinalSentenceCheck
{
    public static void Run(WordSession test,string source,string folder,Action<string> pass)
    {
        string path=Create(source,folder);byte[] digest=SHA256.HashData(File.ReadAllBytes(path));
        test.Profile=DocumentProfile.State;test.Open(path);string original=test.ReadCheckText();
        bool Chosen(Issue i)=>i.CanFix && i.Kind is FixKind.SentenceInnerSpace or FixKind.SentenceAfterSpace;
        var edits=test.Check().Where(Chosen).ToArray();
        if(edits.Length!=2 || edits.Single(i=>i.Kind==FixKind.SentenceInnerSpace).Original!=" " ||
            edits.Single(i=>i.Kind==FixKind.SentenceAfterSpace).Original!=".")
            throw new InvalidOperationException("Mẫu câu cuối một từ chưa phân biệt hai phía dấu câu hoặc đang sửa viết tắt/chuỗi dấu.");
        var after=edits.Single(i=>i.Kind==FixKind.SentenceAfterSpace);
        var probe=new Issue(after.Start+1,4,"Xong","Xong","",false);var decoration=test.ReadCheckDecorations(probe);
        if(decoration.Bold!=-1 || decoration.Underline!=1)throw new InvalidOperationException("Mẫu câu ngắn thiếu kiểu chữ cần giữ.");
        test.Fix(after);
        if(test.ReadCheckText()!=original.Replace("Đã hoàn thành .Xong!","Đã hoàn thành . Xong!",StringComparison.Ordinal) ||
            test.Check().Where(Chosen).Single().Kind!=FixKind.SentenceInnerSpace ||
            test.ReadCheckDecorations(probe with {Start=probe.Start+1})!=decoration || test.CheckUndoCount!=1)
            throw new InvalidOperationException("Thêm khoảng sau chưa giữ khoảng trước chưa chọn hoặc kiểu chữ câu cuối.");
        test.Undo();if(test.ReadCheckText()!=original || test.CheckUndoCount!=0)throw new InvalidOperationException("Undo khoảng sau chưa về nguyên bản.");
        test.Fix(test.Check().Single(i=>Chosen(i) && i.Kind==FixKind.SentenceInnerSpace));
        if(test.ReadCheckText()!=original.Replace("Đã hoàn thành .Xong!","Đã hoàn thành.Xong!",StringComparison.Ordinal) ||
            test.Check().Where(Chosen).Single().Kind!=FixKind.SentenceAfterSpace || test.ReadCheckDecorations(probe with {Start=probe.Start-1})!=decoration)
            throw new InvalidOperationException("Bỏ khoảng trước chưa giữ lỗi khoảng sau hoặc kiểu chữ câu cuối.");
        test.Undo();if(test.ReadCheckText()!=original)throw new InvalidOperationException("Undo khoảng trước chưa về nguyên bản.");
        pass("Câu cuối một từ: chọn riêng trước/sau, giữ chữ/kiểu và lỗi chưa chọn, quét lại/Undo");
        edits=test.Check().Where(Chosen).ToArray();bool failed=false;
        try{test.FixTextFailureProbe(edits,1);}catch(InvalidOperationException error) when(error.InnerException is OfficeTextProbeException){failed=true;}
        if(!failed || test.ReadCheckText()!=original || test.Changed || test.CheckUndoCount!=0 || test.ReadCheckDecorations(probe)!=decoration)
            throw new InvalidOperationException("Rollback câu cuối một từ chưa phục hồi cả lượt.");
        test.FixMany(test.Check().Where(Chosen));
        string expected=original.Replace("Đã hoàn thành .Xong!","Đã hoàn thành. Xong!",StringComparison.Ordinal);
        if(test.ReadCheckText()!=expected || test.Check().Any(Chosen) || test.CheckUndoCount!=1 || test.ReadCheckDecorations(probe)!=decoration)
            throw new InvalidOperationException("Sửa hai phía câu cuối chưa đúng nguyên văn/kiểu hoặc còn lỗi đã sửa.");
        string saved=Path.Combine(folder,"short-final-sentence-fixed.docx");test.SaveCopy(saved);test.Undo();
        if(test.ReadCheckText()!=original || test.ReadCheckDecorations(probe)!=decoration)throw new InvalidOperationException("Một Undo chưa phục hồi cả hai phía câu ngắn.");
        test.Open(saved);
        if(test.ReadCheckText()!=expected || test.Check().Any(Chosen) || test.ReadCheckDecorations(probe)!=decoration ||
            !SHA256.HashData(File.ReadAllBytes(path)).SequenceEqual(digest))throw new InvalidOperationException("Lưu/mở câu cuối một từ chưa đúng hoặc nguồn bị đổi.");
        pass("Câu cuối một từ: sửa hai phía, rollback, một Undo, lưu/mở; giữ nguồn/chữ hoa/kiểu");
    }
    private static string Create(string source,string folder)
    {
        string path=Path.Combine(folder,"short-final-sentence-input.docx");File.Copy(source,path);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);var entry=zip.GetEntry("word/document.xml")!;
        XDocument xml;using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XElement Run(string value,bool kept=false)=>new(w+"r",new XElement(w+"rPr",
            new XElement(w+"rFonts",new XAttribute(w+"ascii","Times New Roman"),new XAttribute(w+"hAnsi","Times New Roman")),
            new XElement(w+"sz",new XAttribute(w+"val",26)),new XElement(w+"b",new XAttribute(w+"val",kept?1:0)),
            new XElement(w+"u",new XAttribute(w+"val",kept?"single":"none"))),new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),value));
        var rows=new[]{new XElement(w+"p",Run("Đã hoàn thành ."),Run("Xong",true),Run("!")),
            new XElement(w+"p",Run("Đã hoàn thành.Docx.")),new XElement(w+"p",Run("Đã hoàn thành.Xong?!"))};
        var body=xml.Descendants(w+"body").Single();
        foreach(var row in rows)if(body.Element(w+"sectPr") is XElement section)section.AddBeforeSelf(row);else body.Add(row);
        entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);return path;
    }
}
