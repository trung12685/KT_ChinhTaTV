using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
internal static class OfficeBodyEmphasisCheck
{
    private const string Old="Nội dung đã đạt yêu cầu cần được giữ nguyên.";
    private const string Text="Nội dung  kế hoạch kế hoạch đã được duyệt.";
    public static void Run(WordSession test,string seed,string folder,Action<string> pass)
    {
        string isolated=Path.Combine(folder,"body-emphasis");Directory.CreateDirectory(isolated);
        string source=Path.Combine(isolated,"emphasis-input.docx");File.Copy(seed,source);
        using(var zip=ZipFile.Open(source,ZipArchiveMode.Update))
        {
            var entry=zip.GetEntry("word/document.xml")!;XDocument xml;
            using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
            XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            XElement Run(string value,int size,bool bold,bool italic)=>new(w+"r",new XElement(w+"rPr",
                new XElement(w+"rFonts",new XAttribute(w+"ascii","Arial"),new XAttribute(w+"hAnsi","Arial")),new XElement(w+"sz",new XAttribute(w+"val",size*2)),
                new XElement(w+"b",new XAttribute(w+"val",bold?1:0)),new XElement(w+"i",new XAttribute(w+"val",italic?1:0)),
                new XElement(w+"u",new XAttribute(w+"val","single")),new XElement(w+"color",new XAttribute(w+"val","FF0000"))),
                new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),value));
            var row=xml.Descendants(w+"p").Single(p=>string.Concat(p.Descendants(w+"t").Select(t=>t.Value))==Old);
            row.Elements(w+"r").Remove();row.Add(Run("Nội dung  ",12,true,false),Run("kế hoạch ",14,false,true),Run("kế hoạch đã được duyệt.",13,true,true));
            entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);
        }
        byte[] hash=SHA256.HashData(File.ReadAllBytes(source));test.Profile=DocumentProfile.State;test.Open(source);
        string original=test.ReadCheckText();var found=test.Check();var font=found.Single(i=>i.CanFix && i.Kind==FixKind.BodyFormat && i.Original==Text);
        if(font.CharacterFormat?.PreserveEmphasis!=true || found.Any(i=>i.Original==Text && i.Kind is FixKind.BodyParagraph or FixKind.BodyGap or FixKind.BodyLineSpacing))
            throw new InvalidOperationException("Mẫu nhấn mạnh chưa có C26 giữ kiểu hoặc đã mở nhầm phạm vi căn/spacing.");
        var before=test.ReadCheckCurrentStyles(Text);var expected=before.WithFormat(font.CharacterFormat!);test.Fix(font);test.Check();
        if(test.ReadCheckText()!=original || test.CheckUndoCount!=1 || !expected.Matches(test.ReadCheckCurrentStyles(Text)) ||
            test.Check().Any(i=>i.Kind==FixKind.BodyFormat && i.Original==Text) || !test.Check().Any(i=>i.CanFix && i.Kind==FixKind.RepeatedText && i.Start>=font.Start && i.Start<font.Start+font.Length))
            throw new InvalidOperationException("Sửa riêng phông nhấn mạnh làm mất kiểu/cỡ hoặc sửa chữ chưa chọn.");
        test.Undo();test.Check();if(!before.Matches(test.ReadCheckCurrentStyles(Text)))throw new InvalidOperationException("Undo phông nhấn mạnh chưa về nguyên kiểu.");
        Issue[] Selected()
        {
            var current=test.Check();var currentFont=current.Single(i=>i.CanFix && i.Kind==FixKind.BodyFormat && i.Original==Text);
            return current.Where(i=>i.CanFix && (!i.IsStyleOnly && i.Format is null || i==currentFont) && i.Start>=currentFont.Start &&
                (long)i.Start+i.Length<=(long)currentFont.Start+currentFont.Length).Concat(OfficeComponentCheck.ReadLines(test)).ToArray();
        }
        var selected=Selected();bool restored=false;
        try{test.FixStagedFailureProbe(selected,3);}catch(InvalidOperationException e) when(e.InnerException is OfficeStagedProbeException){restored=true;}
        test.Check();
        if(!restored || test.ReadCheckText()!=original || test.CheckUndoCount!=0 || test.Changed || !before.Matches(test.ReadCheckCurrentStyles(Text)))
            throw new InvalidOperationException("Rollback nhấn mạnh/phông/chữ/Line chưa khôi phục đủ kiểu.");
        selected=Selected();font=selected.Single(i=>i.Kind==FixKind.BodyFormat);
        var edits=selected.Where(i=>!i.IsStyleOnly && i.Format is null && i.LineFix is null).ToArray();
        expected=before.WithFormat(font.CharacterFormat!).Project(edits,i=>Text[..(i.Start-font.Start)]);
        test.FixSelection(selected);test.Check();string expectedText=original.Replace(Text,expected.Text,StringComparison.Ordinal);
        if(test.ReadCheckText()!=expectedText || test.CheckUndoCount!=1 || !expected.Matches(test.ReadCheckCurrentStyles(expected.Text)) ||
            test.Check().Any(i=>i.Kind==FixKind.BodyFormat && i.Original==expected.Text))throw new InvalidOperationException("Sửa chung nhấn mạnh/phông/chữ/Line chưa giữ glyph/kiểu/cỡ.");
        string saved=Path.Combine(isolated,"emphasis-fixed.docx");test.SaveCopy(saved);test.Undo();test.Check();
        if(test.ReadCheckText()!=original || !before.Matches(test.ReadCheckCurrentStyles(Text)))throw new InvalidOperationException("Một Undo chưa phục hồi phông và nhấn mạnh.");
        test.Open(saved);test.Check();
        if(test.ReadCheckText()!=expectedText || !expected.Matches(test.ReadCheckCurrentStyles(expected.Text)) || !hash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(source))))
            throw new InvalidOperationException("Lưu/mở nhấn mạnh chưa giữ kiểu/cỡ hoặc nguồn bị đổi.");
        pass("C26 giữ nhấn mạnh: riêng/chung chữ và ba Line, giữ đậm/nghiêng/gạch chân/cỡ đúng, rollback/một Undo/lưu-mở/nguồn");
    }
}
