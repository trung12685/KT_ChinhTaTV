using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
internal static class OfficeQuoteSpaceCheck
{
    private const string Simple="Nội dung  “  kế hoạch kế hoạch  ” đã duyệt;hoàn thành.";
    public static void Run(WordSession test,string seed,string folder,Action<string> pass)
    {
        RunVariant(test,seed,folder,pass,false);
        RunVariant(test,seed,folder,pass,true);
        OfficeQuotedPunctuationCheck.Run(test,seed,folder,pass);
    }
    private static void RunVariant(WordSession test,string seed,string folder,Action<string> pass,bool sentenceEnd)
    {
        string Text=sentenceEnd?"Nội dung  “  kế hoạch kế hoạch đã duyệt.  ” đã duyệt;hoàn thành.":Simple;
        string source=Path.Combine(folder,sentenceEnd?"quote-sentence-input.docx":"quote-space-input.docx");File.Copy(seed,source);
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
            var rows=new[]{new XElement(w+"p",Run("Nội dung  “  "),Run("kế hoạch",true),Run(sentenceEnd?" kế hoạch đã duyệt":" kế hoạch"),Run(sentenceEnd?".":"",true),Run("  ” đã duyệt;hoàn thành.")),
                new XElement(w+"p",Run("Ô trống “    ” giữ chỗ điền nội dung.")),
                new XElement(w+"p",Run("“ nội dung ” “ phụ “ thêm ” ”")),new XElement(w+"p",Run("Mẫu “ host/name ” giữ nguyên.")),new XElement(w+"p",Run("“ Đã hoàn thành?! ”"))};
            var body=xml.Descendants(w+"body").Single();
            foreach(var row in rows)if(body.Element(w+"sectPr") is { } section)section.AddBeforeSelf(row);else body.Add(row);
            entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);
        }
        byte[] hash=SHA256.HashData(File.ReadAllBytes(source));test.Profile=DocumentProfile.State;test.Open(source);test.Check();
        string original=test.ReadCheckText();var before=test.ReadCheckCurrentStyles(Text);
        var edges=test.Check().Where(i=>i.CanFix && i.Kind==FixKind.QuoteSpace).OrderBy(i=>i.Start).ToArray();
        if(edges.Length!=2)throw new InvalidOperationException("Mẫu ngoặc kép chưa nhận đúng hai phía hoặc đang sửa ô trống/ngoặc lồng/mã.");
        var first=edges[0];int rowStart=first.Start-Text.IndexOf('“')-1;
        var one=before.Project([first],i=>Text[..(i.Start-rowStart)]);test.Fix(first);test.Check();
        if(test.ReadCheckText()!=original.Replace(Text,one.Text,StringComparison.Ordinal) || test.CheckUndoCount!=1 ||
            !one.Matches(test.ReadCheckCurrentStyles(one.Text)) || test.Check().Count(i=>i.CanFix && i.Kind==FixKind.QuoteSpace)!=1 ||
            !test.Check().Any(i=>i.CanFix && i.Kind==FixKind.RepeatedText && i.Start>=rowStart && i.Start<rowStart+one.Text.Length))
            throw new InvalidOperationException("Sửa riêng mép ngoặc kép chưa giữ phía chưa chọn, lỗi lặp hoặc kiểu chữ.");
        test.Undo();test.Check();if(!before.Matches(test.ReadCheckCurrentStyles(Text)))throw new InvalidOperationException("Undo mép ngoặc kép chưa về nguyên kiểu.");
        Issue[] Selected()
        {
            var current=test.Check();rowStart=current.Where(i=>i.CanFix && i.Kind==FixKind.QuoteSpace).Min(i=>i.Start)-Text.IndexOf('“')-1;
            return current.Where(i=>i.CanFix && !i.IsStyleOnly && i.Format is null && i.LineFix is null &&
                i.Start>=rowStart && (long)i.Start+i.Length<=(long)rowStart+Text.Length).Concat(OfficeComponentCheck.ReadLines(test)).ToArray();
        }
        var selected=Selected();bool restored=false;
        try{test.FixStagedFailureProbe(selected,3);}catch(InvalidOperationException e) when(e.InnerException is OfficeStagedProbeException){restored=true;}
        test.Check();
        if(!restored || test.ReadCheckText()!=original || test.CheckUndoCount!=0 || test.Changed || !before.Matches(test.ReadCheckCurrentStyles(Text)))
            throw new InvalidOperationException("Rollback ngoặc kép/chữ/Line chưa phục hồi cả lượt.");
        selected=Selected();var result=before.Project(selected.Where(i=>i.LineFix is null).ToArray(),i=>Text[..(i.Start-rowStart)]);
        test.FixSelection(selected);test.Check();string expected=original.Replace(Text,result.Text,StringComparison.Ordinal);
        if(result.Text!=(sentenceEnd?"Nội dung “kế hoạch đã duyệt.” đã duyệt; hoàn thành.":"Nội dung “kế hoạch” đã duyệt; hoàn thành.") || test.ReadCheckText()!=expected || test.CheckUndoCount!=1 ||
            !result.Matches(test.ReadCheckCurrentStyles(result.Text)) || test.Check().Any(i=>i.CanFix && i.Kind==FixKind.QuoteSpace))
            throw new InvalidOperationException("Sửa chung ngoặc kép/chữ/baLine chưa giữ kiểu/cỡ hoặc còn mục đã sửa.");
        string saved=Path.Combine(folder,sentenceEnd?"quote-sentence-fixed.docx":"quote-space-fixed.docx");test.SaveCopy(saved);test.Undo();test.Check();
        if(test.ReadCheckText()!=original || !before.Matches(test.ReadCheckCurrentStyles(Text)))throw new InvalidOperationException("Một Undo ngoặc kép chưa giữ chữ và định dạng nguồn.");
        test.Open(saved);test.Check();
        if(test.ReadCheckText()!=expected || !result.Matches(test.ReadCheckCurrentStyles(result.Text)) ||
            !hash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(source))))throw new InvalidOperationException("Lưu/mở ngoặc kép chưa đúng hoặc nguồn đã thay đổi.");
        pass((sentenceEnd?"Câu trích dẫn có dấu kết câu: ":"Ngoặc kép: ")+"chọn một phía/chung chữ/baLine, giữ dấu/Unicode/kiểu/cỡ/ô trống, rollback/mộtUndo/lưu-mở/nguồn");
    }
}
