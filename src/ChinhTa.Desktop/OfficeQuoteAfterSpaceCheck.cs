using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
internal static class OfficeQuoteAfterSpaceCheck
{
    private const string Text="Theo mẫu “ nội dung nội dung ”đã  duyệt;hoàn thành.";
    private const string Other="Theo mẫu “kế hoạch”cần làm.";
    public static void Run(WordSession test,string seed,string folder,Action<string> pass)
    {
        string source=Path.Combine(folder,"quote-after-input.docx");File.Copy(seed,source);
        using(var zip=ZipFile.Open(source,ZipArchiveMode.Update))
        {
            var entry=zip.GetEntry("word/document.xml")!;XDocument xml;
            using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
            XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            XElement Run(string value,bool kept=false)=>new(w+"r",new XElement(w+"rPr",
                new XElement(w+"rFonts",new XAttribute(w+"ascii","Times New Roman"),new XAttribute(w+"hAnsi","Times New Roman")),
                new XElement(w+"sz",new XAttribute(w+"val",kept?28:26)),new XElement(w+"b",new XAttribute(w+"val",kept?1:0)),
                new XElement(w+"i",new XAttribute(w+"val",kept?1:0)),new XElement(w+"u",new XAttribute(w+"val",kept?"single":"none"))),
                new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),value));
            var rows=new[]{new XElement(w+"p",Run("Theo mẫu “ "),Run("nội dung",true),Run(" nội dung "),Run("”",true),Run("đã  duyệt;hoàn thành.")),
                new XElement(w+"p",Run(Other)),new XElement(w+"p",Run("Theo mẫu “nội dung “bổ sung” ”đã duyệt.")),
                new XElement(w+"p",Run("Tính sin “alpha beta”gamma delta"))};
            var body=xml.Descendants(w+"body").Single();
            foreach(var row in rows)if(body.Element(w+"sectPr") is { } section)section.AddBeforeSelf(row);else body.Add(row);
            entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);
        }
        byte[] hash=SHA256.HashData(File.ReadAllBytes(source));test.Profile=DocumentProfile.State;test.Open(source);test.Check();
        string original=test.ReadCheckText();var before=test.ReadCheckCurrentStyles(Text);var other=test.ReadCheckCurrentStyles(Other);
        var current=test.Check();var edges=current.Where(i=>i.CanFix && i.Kind==FixKind.QuoteSpace && !i.AddsSpaceAfterClosingQuote).ToArray();
        var gaps=current.Where(i=>i.CanFix && i.AddsSpaceAfterClosingQuote).OrderBy(i=>i.Start).ToArray();
        if(edges.Length!=2 || gaps.Length!=2)throw new InvalidOperationException("Mẫu sau ngoặc kép chưa tách đúng mép trong/hai gap ngoài hoặc đang sửa hàm/ngoặc lồng.");
        int start=edges.Min(i=>i.Start)-Text.IndexOf('“')-1;
        var afterOnly=before.Project([gaps[0]],i=>Text[..(i.Start-start)]);test.Fix(gaps[0]);test.Check();
        if(test.ReadCheckText()!=original.Replace(Text,afterOnly.Text,StringComparison.Ordinal) || test.CheckUndoCount!=1 ||
            !afterOnly.Matches(test.ReadCheckCurrentStyles(afterOnly.Text)) || test.Check().Count(i=>i.CanFix && i.AddsSpaceAfterClosingQuote)!=1 ||
            test.Check().Count(i=>i.CanFix && i.Kind==FixKind.QuoteSpace && !i.AddsSpaceAfterClosingQuote)!=2)
            throw new InvalidOperationException("Thêm riêng gap sau quote chưa giữ mép trong/kiểu của quote và chữ.");
        test.Undo();test.Check();
        edges=test.Check().Where(i=>i.CanFix && i.Kind==FixKind.QuoteSpace && !i.AddsSpaceAfterClosingQuote).ToArray();
        var insideOnly=before.Project(edges,i=>Text[..(i.Start-start)]);test.FixMany(edges);test.Check();
        if(test.ReadCheckText()!=original.Replace(Text,insideOnly.Text,StringComparison.Ordinal) || !insideOnly.Matches(test.ReadCheckCurrentStyles(insideOnly.Text)) ||
            test.Check().Count(i=>i.CanFix && i.AddsSpaceAfterClosingQuote)!=2)
            throw new InvalidOperationException("Sửa mép trong làm mất gap ngoài chưa chọn hoặc đổi kiểu chữ.");
        test.Undo();test.Check();
        Issue[] Selected()
        {
            var found=test.Check();start=found.Where(i=>i.CanFix && i.Kind==FixKind.QuoteSpace).Min(i=>i.Start)-Text.IndexOf('“')-1;
            return found.Where(i=>i.CanFix && !i.IsStyleOnly && i.Format is null && i.LineFix is null &&
                i.Start>=start && (long)i.Start+i.Length<=(long)start+Text.Length).Concat(OfficeComponentCheck.ReadLines(test)).ToArray();
        }
        var selected=Selected();bool restored=false;
        try{test.FixStagedFailureProbe(selected,3);}catch(InvalidOperationException e) when(e.InnerException is OfficeStagedProbeException){restored=true;}
        test.Check();
        if(!restored || test.ReadCheckText()!=original || test.CheckUndoCount!=0 || test.Changed || !before.Matches(test.ReadCheckCurrentStyles(Text)) ||
            !other.Matches(test.ReadCheckCurrentStyles(Other)))throw new InvalidOperationException("Rollback quote/gap/chữ/Line chưa phục hồi cả lượt.");
        selected=Selected();var result=before.Project(selected.Where(i=>i.LineFix is null).ToArray(),i=>Text[..(i.Start-start)]);
        test.FixSelection(selected);test.Check();string expected=original.Replace(Text,result.Text,StringComparison.Ordinal);
        if(result.Text!="Theo mẫu “nội dung” đã duyệt; hoàn thành." || test.ReadCheckText()!=expected || test.CheckUndoCount!=1 ||
            !result.Matches(test.ReadCheckCurrentStyles(result.Text)) || !other.Matches(test.ReadCheckCurrentStyles(Other)) ||
            test.Check().Count(i=>i.CanFix && i.AddsSpaceAfterClosingQuote)!=1 || test.Check().Any(i=>i.CanFix && i.Kind==FixKind.QuoteSpace && !i.AddsSpaceAfterClosingQuote))
            throw new InvalidOperationException("Sửa chung mép/gap/chữ/baLine chưa giữ quote/kiểu/cỡ hoặc phần chưa chọn.");
        string saved=Path.Combine(folder,"quote-after-fixed.docx");test.SaveCopy(saved);test.Undo();test.Check();
        if(test.ReadCheckText()!=original || !before.Matches(test.ReadCheckCurrentStyles(Text)))throw new InvalidOperationException("Một Undo quote/gap chưa về nguyên bản.");
        test.Open(saved);test.Check();
        if(test.ReadCheckText()!=expected || !result.Matches(test.ReadCheckCurrentStyles(result.Text)) || !other.Matches(test.ReadCheckCurrentStyles(Other)) ||
            !hash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(source))))throw new InvalidOperationException("Lưu/mở quote/gap chưa đúng hoặc nguồn bị đổi.");
        pass("Sau ngoặc kép: riêng trong/ngoài, chung chữ/baLine, giữ quote/kiểu/cỡ/phần chưa chọn, rollback/mộtUndo/lưu-mở/nguồn");
    }
}
