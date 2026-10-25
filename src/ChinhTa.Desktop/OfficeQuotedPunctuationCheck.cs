using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
internal static class OfficeQuotedPunctuationCheck
{
    private const string Text="Theo mẫu “ nội dung nội dung ”  ,báo cáo.";
    private const string Other="Theo mẫu “kế hoạch” ;đã duyệt.";
    public static void Run(WordSession test,string seed,string folder,Action<string> pass)
    {
        string source=Path.Combine(folder,"quoted-punctuation-input.docx");File.Copy(seed,source);
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
            var rows=new[]{new XElement(w+"p",Run("Theo mẫu “ "),Run("nội dung",true),Run(" nội dung ”  ,báo cáo.")),
                new XElement(w+"p",Run(Other)),new XElement(w+"p",Run("Theo mẫu “nội dung “bổ sung” ” ,báo cáo.")),
                new XElement(w+"p",Run("Tính sin “alpha beta” ,gamma delta"))};
            var body=xml.Descendants(w+"body").Single();
            foreach(var row in rows)if(body.Element(w+"sectPr") is { } section)section.AddBeforeSelf(row);else body.Add(row);
            entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);
        }
        byte[] hash=SHA256.HashData(File.ReadAllBytes(source));test.Profile=DocumentProfile.State;test.Open(source);test.Check();
        string original=test.ReadCheckText();var before=test.ReadCheckCurrentStyles(Text);var other=test.ReadCheckCurrentStyles(Other);
        bool Outside(Issue i)=>i.CanFix && i.Kind==FixKind.PunctuationSpace;
        var found=test.Check();var edges=found.Where(i=>i.CanFix && i.Kind==FixKind.QuoteSpace).ToArray();
        if(edges.Length!=2 || found.Count(Outside)!=2)throw new InvalidOperationException("Mẫu dấu câu sau ngoặc kép chưa tách đúng hai mép/hai lỗi ngoài hoặc đang sửa hàm/ngoặc lồng.");
        int start=edges.Min(i=>i.Start)-Text.IndexOf('“')-1;
        var punctuation=found.Where(Outside).OrderBy(i=>i.Start).First();
        var outsideOnly=before.Project([punctuation],i=>Text[..(i.Start-start)]);test.Fix(punctuation);test.Check();
        if(test.ReadCheckText()!=original.Replace(Text,outsideOnly.Text,StringComparison.Ordinal) || test.CheckUndoCount!=1 ||
            !outsideOnly.Matches(test.ReadCheckCurrentStyles(outsideOnly.Text)) || test.Check().Count(Outside)!=1 ||
            test.Check().Count(i=>i.CanFix && i.Kind==FixKind.QuoteSpace)!=2)
            throw new InvalidOperationException("Sửa riêng ngoài ngoặc kép chưa giữ mép trong/kiểu/cỡ hoặc lỗi chưa chọn.");
        test.Undo();test.Check();
        edges=test.Check().Where(i=>i.CanFix && i.Kind==FixKind.QuoteSpace).ToArray();
        var insideOnly=before.Project(edges,i=>Text[..(i.Start-start)]);test.FixMany(edges);test.Check();
        if(test.ReadCheckText()!=original.Replace(Text,insideOnly.Text,StringComparison.Ordinal) || !insideOnly.Matches(test.ReadCheckCurrentStyles(insideOnly.Text)) ||
            test.Check().Count(Outside)!=2 || test.Check().Any(i=>i.CanFix && i.Kind==FixKind.QuoteSpace))
            throw new InvalidOperationException("Sửa mép trong làm mất lỗi dấu câu ngoài hoặc đổi kiểu chữ.");
        test.Undo();test.Check();
        Issue[] Selected()
        {
            var current=test.Check();start=current.Where(i=>i.CanFix && i.Kind==FixKind.QuoteSpace).Min(i=>i.Start)-Text.IndexOf('“')-1;
            return current.Where(i=>i.CanFix && !i.IsStyleOnly && i.Format is null && i.LineFix is null &&
                i.Start>=start && (long)i.Start+i.Length<=(long)start+Text.Length).Concat(OfficeComponentCheck.ReadLines(test)).ToArray();
        }
        var selected=Selected();bool restored=false;
        try{test.FixStagedFailureProbe(selected,3);}catch(InvalidOperationException e) when(e.InnerException is OfficeStagedProbeException){restored=true;}
        test.Check();
        if(!restored || test.ReadCheckText()!=original || test.CheckUndoCount!=0 || test.Changed || !before.Matches(test.ReadCheckCurrentStyles(Text)) ||
            !other.Matches(test.ReadCheckCurrentStyles(Other)))throw new InvalidOperationException("Rollback ngoặc kép/dấu câu/chữ/Line chưa phục hồi đủ.");
        selected=Selected();var result=before.Project(selected.Where(i=>i.LineFix is null).ToArray(),i=>Text[..(i.Start-start)]);
        test.FixSelection(selected);test.Check();string expected=original.Replace(Text,result.Text,StringComparison.Ordinal);
        if(result.Text!="Theo mẫu “nội dung”, báo cáo." || test.ReadCheckText()!=expected || test.CheckUndoCount!=1 ||
            !result.Matches(test.ReadCheckCurrentStyles(result.Text)) || !other.Matches(test.ReadCheckCurrentStyles(Other)) || test.Check().Count(Outside)!=1 ||
            test.Check().Any(i=>i.CanFix && i.Kind==FixKind.QuoteSpace))throw new InvalidOperationException("Sửa chung dấu câu/ngoặc/chữ/baLine chưa giữ phần chưa chọn hoặc còn mục đã sửa.");
        string saved=Path.Combine(folder,"quoted-punctuation-fixed.docx");test.SaveCopy(saved);test.Undo();test.Check();
        if(test.ReadCheckText()!=original || !before.Matches(test.ReadCheckCurrentStyles(Text)))throw new InvalidOperationException("Một Undo dấu câu sau ngoặc kép chưa giữ nguyên nguồn.");
        test.Open(saved);test.Check();
        if(test.ReadCheckText()!=expected || !result.Matches(test.ReadCheckCurrentStyles(result.Text)) || !other.Matches(test.ReadCheckCurrentStyles(Other)) ||
            !hash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(source))))throw new InvalidOperationException("Lưu/mở dấu câu sau ngoặc kép chưa đúng hoặc nguồn bị đổi.");
        pass("Dấu câu sau ngoặc kép: riêng trong/ngoài, chung chữ/baLine, giữ quote/kiểu/cỡ/phần chưa chọn, rollback/mộtUndo/lưu-mở/nguồn");
    }
}
