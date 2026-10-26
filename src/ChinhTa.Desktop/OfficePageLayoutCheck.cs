using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

internal static class OfficePageLayoutCheck
{
    private const string Text="Nội dung  cần giữ nguyên trong lượt sửa trang.";
    internal static string Create(string modern,string folder)
    {
        string seed=OfficeComponentCheck.Create(modern,folder,false),path=Path.Combine(folder,"page-input.docx");File.Copy(seed,path);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);var entry=zip.GetEntry("word/document.xml")!;XDocument xml;
        using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var section=xml.Descendants(w+"body").Single().Element(w+"sectPr")!;
        section.Element(w+"pgSz")!.SetAttributeValue(w+"w",12240);section.Element(w+"pgSz")!.SetAttributeValue(w+"h",15840);
        var margins=section.Element(w+"pgMar")!;margins.SetAttributeValue(w+"top",567);margins.SetAttributeValue(w+"left",1134);
        margins.SetAttributeValue(w+"bottom",1260);margins.SetAttributeValue(w+"right",850);
        var greeting=xml.Descendants(w+"p").Single(p=>string.Concat(p.Descendants(w+"t").Select(e=>e.Value)).StartsWith("Kính gửi:",StringComparison.Ordinal));
        greeting.AddAfterSelf(new XElement(w+"p",new XElement(w+"pPr",new XElement(w+"jc",new XAttribute(w+"val","both"))),
            new XElement(w+"r",new XElement(w+"rPr",new XElement(w+"rFonts",new XAttribute(w+"ascii","Times New Roman"),new XAttribute(w+"hAnsi","Times New Roman")),
                new XElement(w+"sz",new XAttribute(w+"val",26)),new XElement(w+"u",new XAttribute(w+"val","single"))),
                new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),Text))));
        entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output,SaveOptions.DisableFormatting);return path;
    }
    public static void Run(WordSession test,string modern,string folder,Action<string> pass)
    {
        string isolated=Path.Combine(folder,"page-layout");Directory.CreateDirectory(isolated);string source=Create(modern,isolated);
        byte[] digest=SHA256.HashData(File.ReadAllBytes(source));test.Profile=DocumentProfile.State;test.Open(source);
        var found=test.Check();var page=found.Single(i=>i.CanFix && i.PageFix is not null);string original=test.ReadCheckText();
        var originalStyles=test.ReadCheckCurrentStyles(Text);
        test.Fix(page);test.Check();
        if(test.Check().Any(i=>i.Kind==FixKind.PageLayout) || test.ReadCheckText()!=original || test.CheckUndoCount!=1 ||
            !originalStyles.Matches(test.ReadCheckCurrentStyles(Text)))throw new InvalidOperationException("Sửa riêng trang chưa giữ chữ/bảng/kiểu hoặc còn cảnh báo cũ.");
        test.Undo();test.Check();
        if(!test.Check().Any(i=>i.PageFix is not null) || test.ReadCheckText()!=original || !originalStyles.Matches(test.ReadCheckCurrentStyles(Text)))
            throw new InvalidOperationException("Undo thiết lập trang chưa khôi phục nguồn.");
        Issue[] Selection()
        {
            var current=test.Check();int start=test.ReadCheckCurrentParagraph(Text).Start;
            return current.Where(i=>i.CanFix && (i.PageFix is not null || i.Kind==FixKind.Text && i.Original=="  " &&
                i.Start>=start && (long)i.Start+i.Length<=start+Text.Length)).Concat(OfficeComponentCheck.ReadLines(test)).ToArray();
        }
        var selected=Selection();
        if(selected.Count(i=>i.LineFix is not null)!=3 || selected.Count(i=>i.PageFix is not null)!=1 || !selected.Any(i=>i.Kind==FixKind.Text))
            throw new InvalidOperationException("Mẫu sửa chung trang/chữ/ba Line chưa đủ nhóm.");
        foreach(int phase in new[]{1,2})
        {
            bool restored=false;
            try{test.FixStagedFailureProbe(Selection(),phase);}catch(InvalidOperationException e) when(e.InnerException is OfficeStagedProbeException){restored=true;}
            test.Check();
            if(!restored || test.ReadCheckText()!=original || test.CheckUndoCount!=0 || test.Changed || !test.Check().Any(i=>i.PageFix is not null))
                throw new InvalidOperationException("Lỗi giữa lượt trang/chữ/Line chưa phục hồi toàn bộ.");
        }
        selected=Selection();int originalTextStart=test.ReadCheckCurrentParagraph(Text).Start;test.FixSelection(selected);test.Check();
        string corrected=Text.Replace("  "," ",StringComparison.Ordinal),expected=original.Replace(Text,corrected,StringComparison.Ordinal);
        var textEdits=selected.Where(i=>i.Kind==FixKind.Text).ToArray();
        var expectedStyles=originalStyles.Project(textEdits,i=>Text[..(i.Start-originalTextStart)]);
        if(test.ReadCheckText()!=expected || test.Check().Any(i=>i.Kind==FixKind.PageLayout) || test.CheckUndoCount!=1 ||
            !expectedStyles.Matches(test.ReadCheckCurrentStyles(corrected)))throw new InvalidOperationException("Sửa chung trang/chữ/Line chưa đạt hậu kiểm.");
        OfficeComponentCheck.ReadLines(test,true);
        string saved=Path.Combine(isolated,"page-fixed.docx");test.SaveCopy(saved);test.Undo();test.Check();
        if(test.ReadCheckText()!=original || !test.Check().Any(i=>i.PageFix is not null))throw new InvalidOperationException("Một Undo chưa phục hồi cả lượt sửa trang.");
        test.Open(saved);test.Check();OfficeComponentCheck.ReadLines(test,true);
        if(test.ReadCheckText()!=expected || test.Check().Any(i=>i.Kind==FixKind.PageLayout) || !expectedStyles.Matches(test.ReadCheckCurrentStyles(corrected)) ||
            !digest.SequenceEqual(SHA256.HashData(File.ReadAllBytes(source))))throw new InvalidOperationException("Lưu/mở trang chưa giữ kết quả/nguồn.");
        pass("Khổ giấy/lề: sửa riêng/chung chữ và ba Line theo số đo mới, giữ lề đúng/chữ/bảng/kiểu; rollback, một Undo và lưu-mở");
    }
}
