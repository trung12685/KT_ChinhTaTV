using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
internal static class OfficeParenthesisAfterCheck
{
    public static void Run(WordSession test,string source,string folder,Action<string> pass)
    {
        string path=Create(source,folder);byte[] digest=SHA256.HashData(File.ReadAllBytes(path));
        test.Profile=DocumentProfile.State;test.Open(path);string original=test.ReadCheckText();
        bool Chosen(Issue i)=>i.CanFix && i.Kind==FixKind.ParenthesisSpace;
        var edits=test.Check().Where(Chosen).ToArray();
        if(edits.Length!=3 || edits.Count(i=>i.AddsSpaceAfterClosingParenthesis)!=1)
            throw new InvalidOperationException("Mẫu ngoặc chưa phân biệt hai khoảng trong và khoảng thiếu sau hoặc đang sửa công thức/ô trống.");
        var after=edits.Single(i=>i.AddsSpaceAfterClosingParenthesis);const string kept="nội dung";
        var probe=new Issue(after.Start-1-kept.Length,kept.Length,kept,kept,"",false);var decoration=test.ReadCheckDecorations(probe);
        if(decoration.Bold!=-1 || decoration.Underline!=1)throw new InvalidOperationException("Mẫu ngoặc thiếu kiểu chữ cần giữ.");
        test.Fix(after);
        if(test.ReadCheckText()!=original.Replace("Theo mẫu ( nội dung )đã duyệt.","Theo mẫu ( nội dung ) đã duyệt.",StringComparison.Ordinal) ||
            test.Check().Count(Chosen)!=2 || test.ReadCheckDecorations(probe)!=decoration || test.CheckUndoCount!=1)
            throw new InvalidOperationException("Sửa riêng khoảng sau ngoặc chưa giữ khoảng trong hoặc kiểu chữ.");
        test.Undo();if(test.ReadCheckText()!=original || test.CheckUndoCount!=0)throw new InvalidOperationException("Undo khoảng sau ngoặc chưa về nguyên bản.");
        var inside=test.Check().Where(i=>Chosen(i) && !i.AddsSpaceAfterClosingParenthesis).ToArray();test.FixMany(inside);
        if(test.ReadCheckText()!=original.Replace("Theo mẫu ( nội dung )đã duyệt.","Theo mẫu (nội dung)đã duyệt.",StringComparison.Ordinal) ||
            test.Check().Count(i=>i.CanFix && i.AddsSpaceAfterClosingParenthesis)!=1 || test.ReadCheckDecorations(probe with {Start=probe.Start-1})!=decoration)
            throw new InvalidOperationException("Sửa trong ngoặc làm mất lỗi ngoài chưa chọn hoặc đổi kiểu chữ.");
        test.Undo();if(test.ReadCheckText()!=original)throw new InvalidOperationException("Undo trong ngoặc chưa về nguyên bản.");
        pass("Ngoặc đóng: chọn riêng trong/ngoài, giữ phần chưa chọn và kiểu đậm/gạch chân, quét lại và Undo");
        edits=test.Check().Where(Chosen).ToArray();bool failed=false;
        try{test.FixTextFailureProbe(edits,1);}catch(InvalidOperationException error) when(error.InnerException is OfficeTextProbeException){failed=true;}
        if(!failed || test.ReadCheckText()!=original || test.Changed || test.CheckUndoCount!=0 || test.ReadCheckDecorations(probe)!=decoration)
            throw new InvalidOperationException("Rollback ngoặc chưa phục hồi cả lượt.");
        edits=test.Check().Where(Chosen).ToArray();test.FixMany(edits);
        string expected=original.Replace("Theo mẫu ( nội dung )đã duyệt.","Theo mẫu (nội dung) đã duyệt.",StringComparison.Ordinal);
        var fixedProbe=probe with {Start=probe.Start-1};
        if(test.ReadCheckText()!=expected || test.Check().Any(Chosen) || test.CheckUndoCount!=1 || test.ReadCheckDecorations(fixedProbe)!=decoration)
            throw new InvalidOperationException("Sửa nhóm ngoặc chưa đúng chữ/kiểu hoặc còn lỗi đã sửa.");
        string saved=Path.Combine(folder,"parenthesis-after-fixed.docx");test.SaveCopy(saved);test.Undo();
        if(test.ReadCheckText()!=original || test.ReadCheckDecorations(probe)!=decoration)throw new InvalidOperationException("Một Undo chưa phục hồi cả nhóm ngoặc.");
        test.Open(saved);
        if(test.ReadCheckText()!=expected || test.Check().Any(Chosen) || test.ReadCheckDecorations(fixedProbe)!=decoration ||
            !SHA256.HashData(File.ReadAllBytes(path)).SequenceEqual(digest))throw new InvalidOperationException("Lưu/mở ngoặc chưa đúng hoặc nguồn mẫu bị đổi.");
        pass("Ngoặc đóng: sửa nhóm, rollback, một Undo, lưu/mở và giữ nguyên nguồn/kiểu/chữ/dấu kết đoạn");
    }
    private static string Create(string source,string folder)
    {
        string path=Path.Combine(folder,"parenthesis-after-input.docx");File.Copy(source,path);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);var entry=zip.GetEntry("word/document.xml")!;
        XDocument xml;using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XElement Run(string value,bool kept=false)=>new(w+"r",new XElement(w+"rPr",
            new XElement(w+"rFonts",new XAttribute(w+"ascii","Times New Roman"),new XAttribute(w+"hAnsi","Times New Roman")),
            new XElement(w+"sz",new XAttribute(w+"val",26)),new XElement(w+"b",new XAttribute(w+"val",kept?1:0)),
            new XElement(w+"u",new XAttribute(w+"val",kept?"single":"none"))),new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),value));
        var rows=new[]{new XElement(w+"p",Run("Theo mẫu ( "),Run("nội dung",true),Run(" )đã duyệt.")),
            new XElement(w+"p",Run("Tính sin (alpha beta)gamma delta")),new XElement(w+"p",Run("Theo mẫu (    )đã duyệt."))};
        var body=xml.Descendants(w+"body").Single();
        foreach(var row in rows)if(body.Element(w+"sectPr") is XElement section)section.AddBeforeSelf(row);else body.Add(row);
        entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);return path;
    }
}
