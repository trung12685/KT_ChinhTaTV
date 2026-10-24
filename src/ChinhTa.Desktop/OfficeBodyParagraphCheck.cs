using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
internal static class OfficeBodyParagraphCheck
{
    private const string First="Nội dung  kế hoạch kế hoạch đã được duyệt.";
    private const string Correct="Nội dung đã đạt yêu cầu cần được giữ nguyên.";
    public static void Run(WordSession test,string seed,string folder,Action<string> pass)
    {
        string isolated=Path.Combine(folder,"paragraph-layout");Directory.CreateDirectory(isolated);
        string source=Path.Combine(isolated,"paragraph-input.docx");File.Copy(seed,source);
        using(var zip=ZipFile.Open(source,ZipArchiveMode.Update))
        {
            var entry=zip.GetEntry("word/document.xml")!;XDocument xml;
            using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
            XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            var row=xml.Descendants(w+"p").Single(p=>string.Concat(p.Descendants(w+"t").Select(t=>t.Value))==First);
            var properties=row.Element(w+"pPr")!;properties.Element(w+"jc")!.SetAttributeValue(w+"val","left");
            properties.Element(w+"ind")!.SetAttributeValue(w+"firstLine",0);
            properties.Element(w+"spacing")?.Remove();properties.Add(new XElement(w+"spacing",new XAttribute(w+"before",60),new XAttribute(w+"after",160),
                new XAttribute(w+"line",360),new XAttribute(w+"lineRule","exact"),new XAttribute(w+"beforeAutospacing",0),new XAttribute(w+"afterAutospacing",0)));
            entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);
        }
        byte[] hash=SHA256.HashData(File.ReadAllBytes(source));test.Profile=DocumentProfile.State;test.Open(source);
        string original=test.ReadCheckText();var current=test.Check();var paragraph=current.Single(i=>i.CanFix && i.Kind==FixKind.BodyParagraph && i.Original==First);
        var originalStyle=test.ReadCheckCurrentStyles(First);var correctStyle=test.ReadCheckCurrentStyles(Correct);
        var expectedOnly=originalStyle.WithParagraph(paragraph.ParagraphFix!);
        test.Fix(paragraph);test.Check();
        if(test.ReadCheckText()!=original || test.CheckUndoCount!=1 || !expectedOnly.Matches(test.ReadCheckCurrentStyles(First)) ||
            test.Check().Any(i=>i.Kind==FixKind.BodyParagraph && i.Original==First) || !test.Check().Any(i=>i.Kind==FixKind.BodyFormat && i.Original==First))
            throw new InvalidOperationException("Sửa riêng căn/thụt chưa giữ phông/chữ/khoảng đoạn chưa chọn hoặc còn lỗi đã sửa.");
        test.Undo();test.Check();
        if(test.ReadCheckText()!=original || !originalStyle.Matches(test.ReadCheckCurrentStyles(First)))throw new InvalidOperationException("Undo căn/thụt chưa khôi phục cả kiểu và khoảng đoạn.");
        Issue[] Selected()
        {
            var found=test.Check();var layouts=found.Where(i=>i.CanFix && i.Kind==FixKind.BodyParagraph && i.Original==First).ToArray();
            var fonts=found.Where(i=>i.CanFix && i.Kind==FixKind.BodyFormat && (i.Original==First || i.Original.StartsWith("Công tác tổng hợp",StringComparison.Ordinal))).ToArray();
            var text=found.Where(i=>i.CanFix && !i.IsStyleOnly && i.Format is null && fonts.Any(f=>i.Start>=f.Start && (long)i.Start+i.Length<=(long)f.Start+f.Length)).ToArray();
            return layouts.Concat(fonts).Concat(text).Concat(OfficeComponentCheck.ReadLines(test)).ToArray();
        }
        var selected=Selected();bool restored=false;
        try{test.FixStagedFailureProbe(selected,3);}catch(InvalidOperationException e) when(e.InnerException is OfficeStagedProbeException){restored=true;}
        test.Check();
        if(!restored || test.ReadCheckText()!=original || test.CheckUndoCount!=0 || test.Changed || !originalStyle.Matches(test.ReadCheckCurrentStyles(First)))
            throw new InvalidOperationException("Rollback căn/thụt/phông/chữ/Line chưa khôi phục cả lượt.");
        selected=Selected();paragraph=selected.Single(i=>i.Kind==FixKind.BodyParagraph);
        var font=selected.Single(i=>i.Kind==FixKind.BodyFormat && i.Original==First);
        var textEdits=selected.Where(i=>!i.IsStyleOnly && i.Format is null && i.LineFix is null && i.Start>=paragraph.Start && (long)i.Start+i.Length<=(long)paragraph.Start+paragraph.Length).ToArray();
        var expected=originalStyle.WithParagraph(paragraph.ParagraphFix!).WithFormat(font.CharacterFormat!).Project(textEdits,i=>First[..(i.Start-paragraph.Start)]);
        test.FixSelection(selected);test.Check();string expectedText=original.Replace(First,expected.Text,StringComparison.Ordinal);
        if(test.ReadCheckText()!=expectedText || test.CheckUndoCount!=1 || !expected.Matches(test.ReadCheckCurrentStyles(expected.Text)) ||
            !correctStyle.Matches(test.ReadCheckCurrentStyles(Correct)) || test.Check().Any(i=>i.Kind==FixKind.BodyParagraph && i.Original==expected.Text))
            throw new InvalidOperationException("Sửa chung căn/thụt/phông/chữ/Line làm đổi cỡ/khoảng đoạn hoặc chưa đạt.");
        string saved=Path.Combine(isolated,"paragraph-fixed.docx");test.SaveCopy(saved);test.Undo();test.Check();
        if(test.ReadCheckText()!=original || !originalStyle.Matches(test.ReadCheckCurrentStyles(First)))throw new InvalidOperationException("Một Undo chưa về nguyên bản căn/thụt.");
        test.Open(saved);test.Check();
        if(test.ReadCheckText()!=expectedText || !expected.Matches(test.ReadCheckCurrentStyles(expected.Text)) || !correctStyle.Matches(test.ReadCheckCurrentStyles(Correct)) ||
            !hash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(source))))throw new InvalidOperationException("Lưu/mở căn/thụt chưa giữ đủ cỡ/khoảng đoạn hoặc nguồn bị đổi.");
        pass("Căn/thụt văn xuôi: sửa riêng giữ lỗi phông, sửa chung chữ/phông/ba Line, giữ cỡ/gạch chân/giãn dòng, rollback/một Undo/lưu-mở/nguồn");
        OfficeBodySpacingCheck.Run(test,source,isolated,pass);
    }
}
