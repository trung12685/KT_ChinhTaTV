using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

// Part of the existing technical-text Word group, using synthetic samples only.
internal static class OfficeRetainedMarkCheck
{
    internal sealed record Sample(string Source,string Expected,char Mark,FixKind Kind);
    internal static IReadOnlyList<Sample> Samples {get;}=Array.AsReadOnly(new[]
    {
        new Sample("Nội dung:báo cáo đã xong.","Nội dung: báo cáo đã xong.",':',FixKind.ColonSpace),
        new Sample("Đã hoàn thành.Cần làm tiếp.","Đã hoàn thành. Cần làm tiếp.",'.',FixKind.SentenceAfterSpace),
        new Sample("Theo mẫu (nội dung)đã duyệt.","Theo mẫu (nội dung) đã duyệt.",')',FixKind.ParenthesisSpace),
        new Sample("Theo mẫu “nội dung”đã duyệt.","Theo mẫu “nội dung” đã duyệt.",'”',FixKind.QuoteSpace)
    });
    internal static string Create(string seed,string folder)
    {
        string path=Path.Combine(folder,"retained-mark-input.docx");File.Copy(seed,path,false);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);var entry=zip.GetEntry("word/document.xml")!;
        XDocument xml;using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var body=xml.Root!.Element(w+"body")!;var section=body.Elements(w+"sectPr").SingleOrDefault();
        if(section is null || body.Descendants(w+"sectPr").Count()!=1)
            throw new InvalidOperationException("Mẫu giữ dấu câu cần một section rõ ràng.");
        XElement Run(string text,bool retained=false)=>new(w+"r",new XElement(w+"rPr",
            new XElement(w+"rFonts",new XAttribute(w+"ascii","Times New Roman"),new XAttribute(w+"hAnsi","Times New Roman")),
            new XElement(w+"b",new XAttribute(w+"val",retained?1:0)),new XElement(w+"i",new XAttribute(w+"val",0)),
            new XElement(w+"color",new XAttribute(w+"val","000000")),new XElement(w+"sz",new XAttribute(w+"val",retained?28:26)),
            new XElement(w+"u",new XAttribute(w+"val",retained?"single":"none"))),
            new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),text));
        XElement Row(Sample sample)
        {
            int mark=sample.Source.IndexOf(sample.Mark);
            return new(w+"p",new XElement(w+"pPr",new XElement(w+"jc",new XAttribute(w+"val","left"))),
                Run(sample.Source[..mark]),Run(sample.Mark.ToString(),true),Run(sample.Source[(mark+1)..]));
        }
        body.ReplaceNodes(Samples.Select(Row),new XElement(section));
        entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);
        return path;
    }
    internal static void Run(WordSession test,string seed,string folder,Action<string> pass)
    {
        string source=Create(seed,folder);byte[] digest=SHA256.HashData(File.ReadAllBytes(source));
        test.Profile=DocumentProfile.Personal;test.Open(source);test.Check();string original=test.ReadCheckText();
        var baseline=Samples.Select(sample=>test.ReadCheckCurrentStyles(sample.Source)).ToArray();
        Issue[] Select()
        {
            var selected=test.Check().Where(i=>i.CanFix && i.Kind is FixKind.ColonSpace or FixKind.SentenceAfterSpace or FixKind.ParenthesisSpace or FixKind.QuoteSpace).ToArray();
            if(selected.Length!=Samples.Count || Samples.Any(sample=>selected.Count(i=>i.Kind==sample.Kind)!=1))
                throw new InvalidOperationException("Mẫu dấu câu chưa có đúng các gợi ý giữ nguyên dấu và thêm một khoảng.");
            return selected;
        }
        void VerifyBaseline()
        {
            if(test.ReadCheckText()!=original || test.CheckUndoCount!=0 ||
                !baseline.All(style=>style.Matches(test.ReadCheckCurrentStyles(style.Text))))
                throw new InvalidOperationException("Chưa phục hồi nguyên văn/kiểu dấu câu và stack Undo của mẫu.");
        }
        foreach(var sample in Samples)
        {
            var selected=Select();var edit=selected.Single(i=>i.Kind==sample.Kind);
            var expected=test.ReadCheckExpectedStyles(test.CaseTrackingRegions([edit]).Single(),[edit]);
            test.Fix(edit);test.Check();
            if(test.ReadCheckText()!=original.Replace(sample.Source,sample.Expected,StringComparison.Ordinal) ||
                test.CheckUndoCount!=1 || !expected.Matches(test.ReadCheckCurrentStyles(sample.Expected)) ||
                !baseline.Where(style=>style.Text!=sample.Source).All(style=>style.Matches(test.ReadCheckCurrentStyles(style.Text))))
                throw new InvalidOperationException("Sửa riêng khoảng dấu câu chưa giữ dấu 14 pt đậm/gạch chân hoặc phần chưa chọn.");
            test.Undo();test.Check();VerifyBaseline();
        }
        var plan=Select();bool restored=false,previousChanged=test.Changed;
        try{test.FixTextFailureProbe(plan,plan.Length);}
        catch(InvalidOperationException error) when(error.InnerException is OfficeTextProbeException){restored=true;}
        if(!restored || test.Changed!=previousChanged)throw new InvalidOperationException("Lỗi cuối lượt mẫu dấu câu chưa được phục hồi.");
        test.Check();VerifyBaseline();plan=Select();
        var expectedStyles=test.CaseTrackingRegions(plan).Select(probe=>test.ReadCheckExpectedStyles(probe,plan)).ToArray();
        string final=original;foreach(var sample in Samples)final=final.Replace(sample.Source,sample.Expected,StringComparison.Ordinal);
        void VerifyFixed()
        {
            if(test.ReadCheckText()!=final || test.Check().Any(i=>i.CanFix) ||
                !expectedStyles.All(style=>style.Matches(test.ReadCheckCurrentStyles(style.Text))))
                throw new InvalidOperationException("Sửa nhóm/lưu-mở chưa giữ nguyên văn và kiểu dấu câu của mẫu.");
        }
        test.FixMany(plan);VerifyFixed();
        if(test.CheckUndoCount!=1)throw new InvalidOperationException("Nhóm dấu câu chưa tạo đúng một Undo.");
        string saved=Path.Combine(folder,"retained-mark-fixed.docx");test.SaveCopy(saved);test.Undo();test.Check();VerifyBaseline();
        test.Open(saved);VerifyFixed();
        if(!digest.SequenceEqual(SHA256.HashData(File.ReadAllBytes(source))))
            throw new InvalidOperationException("Nguồn mẫu dấu câu đã bị đổi.");
        pass("Dấu hai chấm/kết câu/đóng ngoặc/đóng nháy 14 pt đậm/gạch chân: sửa riêng/chung, lỗi cuối lượt/rollback, một Undo, lưu-mở và nguồn bất biến");
    }
}
