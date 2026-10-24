using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
internal static class OfficeBodyFormatCheck
{
    private const string First="Nội dung  kế hoạch kế hoạch đã được duyệt.";
    private const string Second="Công tác tổng hợp đã được thực hiện đầy đủ.";
    private const string Correct="Nội dung đã đạt yêu cầu cần được giữ nguyên.";
    private static bool Owned(Issue issue)=>issue.Kind==FixKind.BodyFormat && issue.Original is First or Second;
    public static void Run(WordSession test,string modern,string folder,Action<string> pass)
    {
        string isolated=Path.Combine(folder,"body-format");Directory.CreateDirectory(isolated);
        string seed=OfficeComponentCheck.Create(modern,isolated,false),source=Path.Combine(isolated,"body-input.docx");File.Copy(seed,source);
        using(var zip=ZipFile.Open(source,ZipArchiveMode.Update))
        {
            var entry=zip.GetEntry("word/document.xml")!;XDocument xml;
            using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
            XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            XElement Run(string value,int size,bool broken)=>new(w+"r",new XElement(w+"rPr",
                new XElement(w+"rFonts",new XAttribute(w+"ascii",broken?"Arial":"Times New Roman"),new XAttribute(w+"hAnsi",broken?"Arial":"Times New Roman")),
                new XElement(w+"sz",new XAttribute(w+"val",size*2)),new XElement(w+"b",new XAttribute(w+"val",0)),new XElement(w+"i",new XAttribute(w+"val",0)),
                new XElement(w+"u",new XAttribute(w+"val","single")),new XElement(w+"color",new XAttribute(w+"val",broken?"FF0000":"000000"))),
                new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),value));
            XElement Row(string text,int size,bool broken)=>new(w+"p",new XElement(w+"pPr",
                new XElement(w+"numPr",new XElement(w+"numId",new XAttribute(w+"val",0))),new XElement(w+"jc",new XAttribute(w+"val","both")),
                new XElement(w+"ind",new XAttribute(w+"firstLine",567))),Run(text[..10],size,broken),Run(text[10..],14,broken));
            var greeting=xml.Descendants(w+"p").Single(p=>string.Concat(p.Descendants(w+"t").Select(t=>t.Value)).StartsWith("Kính gửi:",StringComparison.Ordinal));
            greeting.AddAfterSelf(Row(First,12,true),Row(Second,12,true),Row(Correct,13,false),Row("1. Nội dung đánh số cần được giữ nguyên.",12,true));
            entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);
        }
        byte[] hash=SHA256.HashData(File.ReadAllBytes(source));test.Profile=DocumentProfile.State;test.Open(source);
        string original=test.ReadCheckText();var found=test.Check();var body=found.Where(i=>i.CanFix && Owned(i)).OrderBy(i=>i.Start).ToArray();
        if(body.Length!=2 || found.Any(i=>i.Kind==FixKind.BodyFormat && i.Original==Correct))
            throw new InvalidOperationException("Mẫu thân bài chưa phân biệt sai thật với cỡ13/14 đúng.");
        var sourceStyle=test.ReadCheckCurrentStyles(First);var correctStyle=test.ReadCheckCurrentStyles(Correct);
        var expectedStyle=sourceStyle.WithFormat(body[0].CharacterFormat!);
        test.Fix(body[0]);test.Check();
        if(test.ReadCheckText()!=original || test.Check().Count(i=>i.CanFix && Owned(i))!=1 || test.CheckUndoCount!=1 ||
            !expectedStyle.Matches(test.ReadCheckCurrentStyles(First)) || !correctStyle.Matches(test.ReadCheckCurrentStyles(Correct)))
            throw new InvalidOperationException("Sửa riêng thân bài chưa giữ chữ/cỡ đúng/gạch chân/căn đoạn hoặc mục chưa chọn.");
        test.Undo();test.Check();
        if(!sourceStyle.Matches(test.ReadCheckCurrentStyles(First)) || test.ReadCheckText()!=original)
            throw new InvalidOperationException("Undo thân bài chưa về nguyên phông/chữ.");
        Issue[] Selected()
        {
            var current=test.Check();var fonts=current.Where(i=>i.CanFix && Owned(i)).ToArray();
            var text=current.Where(i=>i.CanFix && i.CharacterFormat is null && i.Format is null &&
                fonts.Any(f=>i.Start>=f.Start && (long)i.Start+i.Length<=(long)f.Start+f.Length)).ToArray();
            return fonts.Concat(text).Concat(OfficeComponentCheck.ReadLines(test)).ToArray();
        }
        var selected=Selected();bool restored=false;
        try{test.FixStagedFailureProbe(selected,3);}catch(InvalidOperationException e) when(e.InnerException is OfficeStagedProbeException){restored=true;}
        test.Check();
        if(!restored || test.ReadCheckText()!=original || test.CheckUndoCount!=0 || test.Changed || !sourceStyle.Matches(test.ReadCheckCurrentStyles(First)))
            throw new InvalidOperationException("Rollback sau Line chưa phục hồi lượt thân bài/chữ/đường kẻ.");
        selected=Selected();var first=selected.Single(i=>Owned(i) && i.Original==First);
        var firstEdits=selected.Where(i=>i.CharacterFormat is null && i.Format is null && i.LineFix is null && i.Start>=first.Start &&
            (long)i.Start+i.Length<=(long)first.Start+first.Length).ToArray();
        var finalStyle=sourceStyle.WithFormat(first.CharacterFormat!).Project(firstEdits,i=>First[..(i.Start-first.Start)]);
        test.FixSelection(selected);test.Check();
        string expected=original.Replace(First,"Nội dung kế hoạch đã được duyệt.",StringComparison.Ordinal);
        if(test.ReadCheckText()!=expected || test.Check().Any(i=>i.CanFix && Owned(i)) || test.CheckUndoCount!=1 ||
            !finalStyle.Matches(test.ReadCheckCurrentStyles(finalStyle.Text)) || !correctStyle.Matches(test.ReadCheckCurrentStyles(Correct)))
            throw new InvalidOperationException("Sửa chung thân bài/chữ/Line chưa giữ từng cỡ/kiểu/căn đoạn hoặc còn lỗi đã sửa.");
        string saved=Path.Combine(isolated,"body-fixed.docx");test.SaveCopy(saved);test.Undo();test.Check();
        if(test.ReadCheckText()!=original || !sourceStyle.Matches(test.ReadCheckCurrentStyles(First)))
            throw new InvalidOperationException("Một Undo chưa khôi phục cả lượt thân bài/Line.");
        test.Open(saved);test.Check();
        if(test.ReadCheckText()!=expected || test.Check().Any(i=>i.CanFix && Owned(i)) || !finalStyle.Matches(test.ReadCheckCurrentStyles(finalStyle.Text)) ||
            !correctStyle.Matches(test.ReadCheckCurrentStyles(Correct)) || !hash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(source))))
            throw new InvalidOperationException("Lưu/mở thân bài chưa đúng hoặc nguồn bị đổi.");
        pass("Thân bài văn xuôi thường: sửa riêng/chung chữ và ba Line, giữ13/14/gạch chân/căn đoạn, rollback, một Undo, lưu-mở và nguồn");
    }
}
