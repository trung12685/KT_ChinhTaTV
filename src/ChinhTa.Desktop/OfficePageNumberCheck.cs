using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

internal static class OfficePageNumberCheck
{
    private const string Text="Nội dung  trang hai cần giữ nguyên khi sửa số trang.";
    internal static string Create(string modern,string folder)
    {
        string seed=OfficeComponentCheck.Create(modern,folder,false),path=Path.Combine(folder,"number-input.docx");File.Copy(seed,path);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);
        XDocument Read(string name){using var s=zip.GetEntry(name)!.Open();return XDocument.Load(s,LoadOptions.PreserveWhitespace);}
        void Save(string name,XDocument doc){zip.GetEntry(name)?.Delete();using var s=zip.CreateEntry(name).Open();doc.Save(s,SaveOptions.DisableFormatting);}
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main",r="http://schemas.openxmlformats.org/officeDocument/2006/relationships",
            rel="http://schemas.openxmlformats.org/package/2006/relationships",ct="http://schemas.openxmlformats.org/package/2006/content-types";
        var doc=Read("word/document.xml");var body=doc.Root!.Element(w+"body")!;var section=body.Element(w+"sectPr")!;
        section.AddBeforeSelf(new XElement(w+"p",new XElement(w+"r",new XElement(w+"br",new XAttribute(w+"type","page")))),
            new XElement(w+"p",new XElement(w+"r",new XElement(w+"rPr",new XElement(w+"rFonts",new XAttribute(w+"ascii","Times New Roman"),new XAttribute(w+"hAnsi","Times New Roman")),
                new XElement(w+"sz",new XAttribute(w+"val",26)),new XElement(w+"u",new XAttribute(w+"val","single"))),new XElement(w+"t",Text))));
        section.Element(w+"pgMar")!.SetAttributeValue(w+"header",454);section.Element(w+"pgMar")!.SetAttributeValue(w+"footer",454);
        const string id="rIdNumberKeptFooter";
        section.AddFirst(new XElement(w+"footerReference",new XAttribute(w+"type","default"),new XAttribute(r+"id",id)));
        var relations=zip.GetEntry("word/_rels/document.xml.rels") is null?new XDocument(new XElement(rel+"Relationships")):Read("word/_rels/document.xml.rels");
        relations.Root!.Add(new XElement(rel+"Relationship",new XAttribute("Id",id),new XAttribute("Type",r.NamespaceName+"/footer"),new XAttribute("Target","footer-number-kept.xml")));
        var types=Read("[Content_Types].xml");types.Root!.Add(new XElement(ct+"Override",new XAttribute("PartName","/word/footer-number-kept.xml"),
            new XAttribute("ContentType","application/vnd.openxmlformats-officedocument.wordprocessingml.footer+xml")));
        var footer=new XDocument(new XElement(w+"ftr",new XElement(w+"p",new XElement(w+"r",new XElement(w+"rPr",new XElement(w+"rFonts",new XAttribute(w+"ascii","Times New Roman"),new XAttribute(w+"hAnsi","Times New Roman")),
            new XElement(w+"sz",new XAttribute(w+"val",22))),new XElement(w+"t","Footer phải giữ nguyên ở cả trang đầu.")))));
        Save("word/document.xml",doc);Save("word/_rels/document.xml.rels",relations);Save("[Content_Types].xml",types);Save("word/footer-number-kept.xml",footer);return path;
    }
    public static void Run(WordSession test,string modern,string folder,Action<string> pass)
    {
        string isolated=Path.Combine(folder,"page-number");Directory.CreateDirectory(isolated);string source=Create(modern,isolated);
        byte[] hash=SHA256.HashData(File.ReadAllBytes(source));test.Profile=DocumentProfile.State;test.Open(source);
        var page=test.Check().Single(i=>i.CanFix && i.NumberFix is not null);string original=test.ReadCheckText();var styles=test.ReadCheckCurrentStyles(Text);
        test.Fix(page);test.Check();
        if(test.Check().Any(i=>i.Kind==FixKind.PageNumber) || test.ReadCheckText()!=original || test.CheckUndoCount!=1 ||
            !styles.Matches(test.ReadCheckCurrentStyles(Text)))throw new InvalidOperationException("Sửa riêng PAGE chưa giữ thân/kiểu hoặc còn cảnh báo cũ.");
        test.Undo();test.Check();
        if(test.ReadCheckText()!=original || !test.Check().Any(i=>i.CanFix && i.NumberFix is not null))throw new InvalidOperationException("Undo PAGE chưa phục hồi cấu hình nguồn.");
        Issue[] Selection()
        {
            var found=test.Check();int start=test.ReadCheckCurrentParagraph(Text).Start;
            return found.Where(i=>i.CanFix && (i.NumberFix is not null || i.Kind==FixKind.Text && i.Original=="  " &&
                i.Start>=start && (long)i.Start+i.Length<=start+Text.Length)).Concat(OfficeComponentCheck.ReadLines(test)).ToArray();
        }
        if(Selection().Count(i=>i.LineFix is not null)!=3)throw new InvalidOperationException("Mẫu PAGE chưa có đủ ba Line để kiểm sửa chung.");
        foreach(int stage in new[]{1,2})
        {
            bool restored=false;
            try{test.FixStagedFailureProbe(Selection(),stage);}catch(InvalidOperationException e) when(e.InnerException is OfficeStagedProbeException){restored=true;}
            test.Check();
            if(!restored || test.ReadCheckText()!=original || test.CheckUndoCount!=0 || test.Changed || !test.Check().Any(i=>i.NumberFix is not null))
                throw new InvalidOperationException("Rollback PAGE/chữ/Line chưa phục hồi cả lượt.");
        }
        var selected=Selection();int originalStart=test.ReadCheckCurrentParagraph(Text).Start;
        var expectedStyle=styles.Project(selected.Where(i=>i.Kind==FixKind.Text).ToArray(),i=>Text[..(i.Start-originalStart)]);
        test.FixSelection(selected);test.Check();string corrected=Text.Replace("  "," ",StringComparison.Ordinal),expected=original.Replace(Text,corrected,StringComparison.Ordinal);
        if(test.Check().Any(i=>i.Kind==FixKind.PageNumber) || test.ReadCheckText()!=expected || test.CheckUndoCount!=1 ||
            !expectedStyle.Matches(test.ReadCheckCurrentStyles(corrected)))throw new InvalidOperationException("Sửa chung PAGE/chữ/Line chưa đạt.");
        OfficeComponentCheck.ReadLines(test,true);string saved=Path.Combine(isolated,"number-fixed.docx");test.SaveCopy(saved);
        test.Undo();test.Check();
        if(test.ReadCheckText()!=original || !test.Check().Any(i=>i.NumberFix is not null))throw new InvalidOperationException("Một Undo chưa phục hồi cả lượt số trang.");
        test.Open(saved);test.Check();OfficeComponentCheck.ReadLines(test,true);
        if(test.Check().Any(i=>i.Kind==FixKind.PageNumber) || test.ReadCheckText()!=expected || !expectedStyle.Matches(test.ReadCheckCurrentStyles(corrected)) ||
            !hash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(source))))throw new InvalidOperationException("Lưu/mở PAGE chưa giữ kết quả hoặc nguồn.");
        pass("PAGE: số thực trên XPS từ trang2, ẩn trang1, giữ thân/bảng/ảnh/footer trang đầu; sửa riêng/chung chữ/Line, rollback, một Undo và lưu-mở");
    }
}
