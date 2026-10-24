using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
internal static class OfficeParentheticalPunctuationCheck
{
    public static void Run(WordSession test,string source,string folder,Action<string> pass)
    {
        string path=Create(source,folder);byte[] digest=SHA256.HashData(File.ReadAllBytes(path));
        test.Profile=DocumentProfile.State;test.Open(path);string original=test.ReadCheckText();
        bool Punctuation(Issue i)=>i.CanFix && i.Kind==FixKind.PunctuationSpace;
        bool Inside(Issue i)=>i.CanFix && i.Kind==FixKind.ParenthesisSpace;
        bool Chosen(Issue i)=>Punctuation(i)||Inside(i);
        var edits=test.Check().Where(Punctuation).OrderBy(i=>i.Start).ToArray();
        if(edits.Length!=2 || edits[0].Original!="  ," || edits[1].Original!=" ;" || test.Check().Count(Inside)!=2)
            throw new InvalidOperationException("Mẫu dấu câu sau ngoặc chưa phân biệt hai lỗi ngoài/hai mép trong hoặc đã sửa hàm/ngoặc lồng.");
        const string kept="nội dung";
        var probe=new Issue(edits[0].Start-2-kept.Length,kept.Length,kept,kept,"",false);var decoration=test.ReadCheckDecorations(probe);
        if(decoration.Bold!=-1 || decoration.Underline!=1)throw new InvalidOperationException("Mẫu dấu câu sau ngoặc thiếu kiểu chữ cần giữ.");
        test.Fix(edits[0]);
        if(test.ReadCheckText()!=original.Replace("Theo mẫu ( nội dung )  ,báo cáo.","Theo mẫu ( nội dung ), báo cáo.",StringComparison.Ordinal) ||
            test.Check().Count(Punctuation)!=1 || test.Check().Count(Inside)!=2 || test.ReadCheckDecorations(probe)!=decoration || test.CheckUndoCount!=1)
            throw new InvalidOperationException("Sửa riêng dấu câu chưa giữ ngoặc/kiểu chữ hoặc phần chưa chọn.");
        test.Undo();if(test.ReadCheckText()!=original || test.CheckUndoCount!=0)throw new InvalidOperationException("Undo dấu câu sau ngoặc chưa về nguyên bản.");
        test.FixMany(test.Check().Where(Inside));
        if(test.ReadCheckText()!=original.Replace("Theo mẫu ( nội dung )  ,báo cáo.","Theo mẫu (nội dung)  ,báo cáo.",StringComparison.Ordinal) ||
            test.Check().Count(Punctuation)!=2 || test.Check().Any(Inside) || test.ReadCheckDecorations(probe with {Start=probe.Start-1})!=decoration)
            throw new InvalidOperationException("Sửa riêng trong ngoặc làm mất lỗi dấu câu chưa chọn hoặc đổi kiểu chữ.");
        test.Undo();if(test.ReadCheckText()!=original)throw new InvalidOperationException("Undo trong ngoặc chưa về nguyên bản.");
        pass("Dấu câu sau ngoặc: chọn riêng trong/ngoài, giữ chữ/kiểu và lỗi chưa chọn, quét lại/Undo");
        var all=test.Check().Where(Chosen).ToArray();bool failed=false;
        try{test.FixTextFailureProbe(all,1);}catch(InvalidOperationException error) when(error.InnerException is OfficeTextProbeException){failed=true;}
        if(!failed || test.ReadCheckText()!=original || test.Changed || test.CheckUndoCount!=0 || test.ReadCheckDecorations(probe)!=decoration)
            throw new InvalidOperationException("Rollback dấu câu sau ngoặc chưa phục hồi cả lượt.");
        test.FixMany(test.Check().Where(Chosen));
        string expected=original.Replace("Theo mẫu ( nội dung )  ,báo cáo.","Theo mẫu (nội dung), báo cáo.",StringComparison.Ordinal)
            .Replace("Theo mẫu (kế hoạch) ;đã duyệt.","Theo mẫu (kế hoạch); đã duyệt.",StringComparison.Ordinal);
        var fixedProbe=probe with {Start=probe.Start-1};
        if(test.ReadCheckText()!=expected || test.Check().Any(Chosen) || test.CheckUndoCount!=1 || test.ReadCheckDecorations(fixedProbe)!=decoration)
            throw new InvalidOperationException("Sửa nhóm dấu câu/ngoặc chưa đúng nguyên văn/kiểu hoặc còn lỗi đã sửa.");
        string saved=Path.Combine(folder,"parenthetical-punctuation-fixed.docx");test.SaveCopy(saved);test.Undo();
        if(test.ReadCheckText()!=original || test.ReadCheckDecorations(probe)!=decoration)throw new InvalidOperationException("Một Undo chưa phục hồi cả nhóm dấu câu/ngoặc.");
        test.Open(saved);
        if(test.ReadCheckText()!=expected || test.Check().Any(Chosen) || test.ReadCheckDecorations(fixedProbe)!=decoration ||
            !SHA256.HashData(File.ReadAllBytes(path)).SequenceEqual(digest))throw new InvalidOperationException("Lưu/mở dấu câu sau ngoặc chưa đúng hoặc nguồn bị đổi.");
        pass("Dấu câu sau ngoặc: sửa nhóm, rollback, một Undo, lưu/mở; giữ nguồn, ngoặc và kiểu chữ");
    }
    private static string Create(string source,string folder)
    {
        string path=Path.Combine(folder,"parenthetical-punctuation-input.docx");File.Copy(source,path);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);var entry=zip.GetEntry("word/document.xml")!;
        XDocument xml;using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XElement Run(string value,bool kept=false)=>new(w+"r",new XElement(w+"rPr",
            new XElement(w+"rFonts",new XAttribute(w+"ascii","Times New Roman"),new XAttribute(w+"hAnsi","Times New Roman")),
            new XElement(w+"sz",new XAttribute(w+"val",26)),new XElement(w+"b",new XAttribute(w+"val",kept?1:0)),
            new XElement(w+"u",new XAttribute(w+"val",kept?"single":"none"))),new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),value));
        var rows=new[]{new XElement(w+"p",Run("Theo mẫu ( "),Run("nội dung",true),Run(" )  ,báo cáo.")),
            new XElement(w+"p",Run("Theo mẫu (kế hoạch) ;đã duyệt.")),new XElement(w+"p",Run("Tính sin (alpha beta) ,gamma delta")),
            new XElement(w+"p",Run("Theo mẫu (nội dung (bổ sung)) ,báo cáo."))};
        var body=xml.Descendants(w+"body").Single();
        foreach(var row in rows)if(body.Element(w+"sectPr") is XElement section)section.AddBeforeSelf(row);else body.Add(row);
        entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);return path;
    }
}
