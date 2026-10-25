using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
internal static class OfficeDelimiterClusterCheck
{
    private static readonly string[] Source=["Theo mẫu( nội dung nội dung )đã  duyệt;hoàn thành.",
        "Theo mẫu\" kế hoạch kế hoạch \"cần làm.","Theo mẫu“ báo cáo báo cáo ”  ,nội dung đã duyệt.","( Đã hoàn thành. )"];
    private static readonly string[] Expected=["Theo mẫu (nội dung) đã duyệt; hoàn thành.",
        "Theo mẫu \"kế hoạch\" cần làm.","Theo mẫu “báo cáo”, nội dung đã duyệt.","(Đã hoàn thành.)"];
    public static void Run(WordSession test,string seed,string folder,Action<string> pass)
    {
        string path=Path.Combine(folder,"delimiter-cluster-input.docx");File.Copy(seed,path);
        using(var zip=ZipFile.Open(path,ZipArchiveMode.Update))
        {
            var entry=zip.GetEntry("word/document.xml")!;XDocument xml;
            using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
            XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            XElement Run(string value,bool kept=false)=>new(w+"r",new XElement(w+"rPr",
                new XElement(w+"rFonts",new XAttribute(w+"ascii","Times New Roman"),new XAttribute(w+"hAnsi","Times New Roman")),
                new XElement(w+"sz",new XAttribute(w+"val",kept?28:26)),new XElement(w+"b",new XAttribute(w+"val",kept?1:0)),
                new XElement(w+"i",new XAttribute(w+"val",kept?1:0)),new XElement(w+"u",new XAttribute(w+"val",kept?"single":"none"))),
                new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),value));
            var body=xml.Descendants(w+"body").Single();
            foreach(string text in Source.Concat(["Ô trống \"    \" giữ chỗ.","Tính sin(alpha beta) đã ghi.","Theo mẫu \"nội dung” đã ghi."]))
            {
                var row=new XElement(w+"p",text.Select((c,i)=>Run(c.ToString(),i%2==0)));
                if(body.Element(w+"sectPr") is { } section)section.AddBeforeSelf(row);else body.Add(row);
            }
            entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);
        }
        var hash=SHA256.HashData(File.ReadAllBytes(path));test.Profile=DocumentProfile.State;test.Open(path);test.Check();
        string original=test.ReadCheckText();var before=Source.Select(test.ReadCheckCurrentStyles).ToArray();
        var anchor=test.ReadCheckCurrentParagraph(Source[0]);
        var one=test.Check().Single(i=>i.AddsSpaceBeforeOpeningParenthesis && i.Start>=anchor.Start && i.Start<anchor.Start+anchor.Length);
        var oneExpected=before[0].Project([one],i=>Source[0][..(i.Start-anchor.Start)]);test.Fix(one);test.Check();
        if(test.ReadCheckText()!=original.Replace(Source[0],oneExpected.Text,StringComparison.Ordinal) || !oneExpected.Matches(test.ReadCheckCurrentStyles(oneExpected.Text)) ||
            !test.Check().Any(i=>i.CanFix && i.AddsSpaceAfterClosingParenthesis) || test.CheckUndoCount!=1)
            throw new InvalidOperationException("Sửa riêng trước ngoặc chưa giữ quote/kiểu hoặc phần chưa chọn.");
        test.Undo();test.Check();
        Issue[] Selected()
        {
            var current=test.Check();var spans=Source.Select(test.ReadCheckCurrentParagraph).ToArray();
            return current.Where(i=>i.CanFix && !i.IsStyleOnly && i.Format is null && i.LineFix is null &&
                spans.Any(s=>i.Start>=s.Start && (long)i.Start+i.Length<=(long)s.Start+s.Length)).Concat(OfficeComponentCheck.ReadLines(test)).ToArray();
        }
        var selected=Selected();bool restored=false;
        try{test.FixStagedFailureProbe(selected,3);}catch(InvalidOperationException e) when(e.InnerException is OfficeStagedProbeException){restored=true;}
        test.Check();
        if(!restored || test.ReadCheckText()!=original || test.CheckUndoCount!=0 || test.Changed || Source.Where((t,i)=>!before[i].Matches(test.ReadCheckCurrentStyles(t))).Any())
            throw new InvalidOperationException("Rollback cụm ngoặc/dấu câu/chữ/Line chưa phục hồi đủ.");
        selected=Selected();var projected=new CharacterStyleEvidence[Source.Length];
        for(int n=0;n<Source.Length;n++)
        {
            var span=test.ReadCheckCurrentParagraph(Source[n]);
            projected[n]=before[n].Project(selected.Where(i=>i.LineFix is null && i.Start>=span.Start && (long)i.Start+i.Length<=(long)span.Start+span.Length).ToArray(),
                i=>Source[n][..(i.Start-span.Start)]);
            if(projected[n].Text!=Expected[n])throw new InvalidOperationException("Mẫu cụm chưa nhận đủ lỗi trước/trong/sau/dấu câu hoặc đang đổi nội dung.");
        }
        test.FixSelection(selected);test.Check();string result=original;
        for(int n=0;n<Source.Length;n++)result=result.Replace(Source[n],Expected[n],StringComparison.Ordinal);
        if(test.ReadCheckText()!=result || test.CheckUndoCount!=1 || projected.Any(p=>!p.Matches(test.ReadCheckCurrentStyles(p.Text))))
            throw new InvalidOperationException("Sửa cả cụm/chữ/baLine chưa giữ chữ/kiểu/cỡ hoặc còn lỗi đã sửa.");
        string saved=Path.Combine(folder,"delimiter-cluster-fixed.docx");test.SaveCopy(saved);test.Undo();test.Check();
        if(test.ReadCheckText()!=original || Source.Where((t,i)=>!before[i].Matches(test.ReadCheckCurrentStyles(t))).Any())throw new InvalidOperationException("Một Undo cụm chưa giữ nguồn.");
        test.Open(saved);test.Check();
        if(test.ReadCheckText()!=result || projected.Any(p=>!p.Matches(test.ReadCheckCurrentStyles(p.Text))) || !hash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(path))))
            throw new InvalidOperationException("Lưu/mở cụm ngoặc/dấu câu chưa đúng hoặc nguồn bị đổi.");
        pass("Cụm ngoặc: trước/trong/sau, quote thẳng/cong, câu có dấu cuối, chung chữ/baLine, giữ từng kiểu/cỡ, rollback/mộtUndo/lưu-mở/nguồn");
    }
}
