using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using System.Windows.Xps.Packaging;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

internal static class OfficeEmptyHeaderFooterCheck
{
    private const string Text="Nội dung  cần giữ nguyên trong lượt sửa trang.";
    internal static string Create(string modern,string folder)
    {
        string seed=OfficePageLayoutCheck.Create(modern,folder),path=Path.Combine(folder,"empty-header-footer-page-input.docx");File.Copy(seed,path);
        using(var zip=ZipFile.Open(path,ZipArchiveMode.Update))
        {
            XDocument Read(string name){using var input=zip.GetEntry(name)!.Open();return XDocument.Load(input,LoadOptions.PreserveWhitespace);}
            void Save(string name,XDocument doc){zip.GetEntry(name)?.Delete();using var output=zip.CreateEntry(name).Open();doc.Save(output,SaveOptions.DisableFormatting);}
            XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main",r="http://schemas.openxmlformats.org/officeDocument/2006/relationships",
                rel="http://schemas.openxmlformats.org/package/2006/relationships",ct="http://schemas.openxmlformats.org/package/2006/content-types";
            var doc=Read("word/document.xml");var section=doc.Descendants(w+"body").Single().Element(w+"sectPr")!;
            section.Elements().Where(e=>e.Name==w+"headerReference" || e.Name==w+"footerReference").Remove();
            var relations=zip.GetEntry("word/_rels/document.xml.rels") is null?new XDocument(new XElement(rel+"Relationships")):Read("word/_rels/document.xml.rels");
            var types=Read("[Content_Types].xml");
            void Add(string kind,string id,string name)
            {
                section.AddFirst(new XElement(w+(kind+"Reference"),new XAttribute(w+"type","default"),new XAttribute(r+"id",id)));
                relations.Root!.Add(new XElement(rel+"Relationship",new XAttribute("Id",id),new XAttribute("Type",r.NamespaceName+"/"+kind),new XAttribute("Target",name)));
                types.Root!.Add(new XElement(ct+"Override",new XAttribute("PartName","/word/"+name),
                    new XAttribute("ContentType","application/vnd.openxmlformats-officedocument.wordprocessingml."+kind+"+xml")));
                Save("word/"+name,new XDocument(new XElement(w+(kind=="header"?"hdr":"ftr"),new XElement(w+"p",
                    new XElement(w+"pPr",new XElement(w+"pStyle",new XAttribute(w+"val","EmptyPageProof")))))));
            }
            Add("footer","rIdEmptyPageFooter","footer-empty-page.xml");Add("header","rIdEmptyPageHeader","header-empty-page.xml");
            var margins=section.Element(w+"pgMar")!;margins.SetAttributeValue(w+"header",454);margins.SetAttributeValue(w+"footer",454);
            // The source fixture has no styles part. Define safe defaults and one
            // explicit basedOn style so emptiness never depends on Word's template.
            if(zip.GetEntry("word/styles.xml") is not null)throw new InvalidOperationException("Mẫu kiểm đầu/cuối trang trống cần nguồn không có styles riêng.");
            var styles=new XDocument(new XElement(w+"styles",
                new XElement(w+"docDefaults",new XElement(w+"rPrDefault",new XElement(w+"rPr")),new XElement(w+"pPrDefault",new XElement(w+"pPr"))),
                new XElement(w+"style",new XAttribute(w+"type","paragraph"),new XAttribute(w+"default",1),new XAttribute(w+"styleId","Normal"),
                    new XElement(w+"name",new XAttribute(w+"val","Normal")),new XElement(w+"pPr",new XElement(w+"spacing",new XAttribute(w+"before",0),new XAttribute(w+"after",0),new XAttribute(w+"line",240),new XAttribute(w+"lineRule","auto"))),
                    new XElement(w+"rPr",new XElement(w+"rFonts",new XAttribute(w+"ascii","Times New Roman"),new XAttribute(w+"hAnsi","Times New Roman")),new XElement(w+"sz",new XAttribute(w+"val",26)))),
                new XElement(w+"style",new XAttribute(w+"type","paragraph"),new XAttribute(w+"styleId","EmptyPageProof"),new XElement(w+"name",new XAttribute(w+"val","Empty page proof")),
                    new XElement(w+"basedOn",new XAttribute(w+"val","Normal")),new XElement(w+"pPr",new XElement(w+"spacing",new XAttribute(w+"before",0),new XAttribute(w+"after",0))))));
            relations.Root!.Add(new XElement(rel+"Relationship",new XAttribute("Id","rIdEmptyPageStyles"),new XAttribute("Type",r.NamespaceName+"/styles"),new XAttribute("Target","styles.xml")));
            types.Root!.Add(new XElement(ct+"Override",new XAttribute("PartName","/word/styles.xml"),new XAttribute("ContentType","application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml")));
            Save("word/document.xml",doc);Save("word/_rels/document.xml.rels",relations);Save("[Content_Types].xml",types);Save("word/styles.xml",styles);
        }
        OfficeFocusedCheck.OrderFixtureProperties(path);return path;
    }
    internal static void Run(WordSession test,string modern,string folder,Action<string> pass)
    {
        string isolated=Path.Combine(folder,"empty-header-footer-page-layout");Directory.CreateDirectory(isolated);string source=Create(modern,isolated);
        byte[] digest=SHA256.HashData(File.ReadAllBytes(source));test.Profile=DocumentProfile.State;test.Open(source);
        Issue Page()=>test.Check().Single(i=>i.CanFix && i.PageFix is not null);
        var page=Page();string original=test.ReadCheckText();var styles=test.ReadCheckCurrentStyles(Text);
        bool restored=false;
        try{test.FixStagedFailureProbe([page],1);}catch(InvalidOperationException error) when(error.InnerException is OfficeStagedProbeException){restored=true;}
        if(!restored || test.CheckUndoCount!=0 || test.Changed || test.ReadCheckText()!=original || !styles.Matches(test.ReadCheckCurrentStyles(Text)) || Page().PageFix!=page.PageFix)
            throw new InvalidOperationException("Lỗi sau đổi trang có đầu/cuối trang trống chưa phục hồi nguồn/kiểu hoặc tạo Undo sai.");
        page=Page();test.Fix(page);test.Check();
        if(test.Check().Any(i=>i.Kind==FixKind.PageLayout) || test.ReadCheckText()!=original || test.CheckUndoCount!=1 || !styles.Matches(test.ReadCheckCurrentStyles(Text)))
            throw new InvalidOperationException("Sửa riêng khổ/lề có đầu/cuối trang trống chưa giữ chữ/bảng/kiểu hoặc chưa đạt hậu kiểm XPS.");
        string saved=Path.Combine(isolated,"empty-header-footer-page-fixed.docx");test.SaveCopy(saved);test.Undo();test.Check();
        if(test.ReadCheckText()!=original || test.CheckUndoCount!=0 || !styles.Matches(test.ReadCheckCurrentStyles(Text)) || Page().PageFix!=page.PageFix)
            throw new InvalidOperationException("Một Undo chưa phục hồi thiết lập trang có đầu/cuối trang trống.");
        test.Open(saved);test.Check();
        var package=PageNumberPackage.FromFile(saved);XDocument xml;
        using(var zip=ZipFile.OpenRead(saved))using(var stream=zip.GetEntry("word/document.xml")!.Open())xml=XDocument.Load(stream,LoadOptions.PreserveWhitespace);
        var layout=PageLayoutGuard.Read(xml,page.PageFix!.After)??throw new InvalidOperationException("Thiếu thiết lập bản đã lưu.");
        if(test.Check().Any(i=>i.Kind==FixKind.PageLayout) || test.ReadCheckText()!=original || !styles.Matches(test.ReadCheckCurrentStyles(Text)) ||
            !EmptyHeaderFooterScope.HasReferences(xml) || !EmptyHeaderFooterScope.Proven(xml,package) || !PageLayoutFix.Matches(page.PageFix.After,layout))
            throw new InvalidOperationException("Lưu/mở khổ/lề chưa giữ phần trống hoặc kết quả thiết lập đã duyệt.");
        string preview=test.ExportPreview([],new Dictionary<Issue,PreviewAnchor>());
        try{using var xps=new XpsDocument(preview,FileAccess.Read);PageLayoutBodyPreviewVerifier.Verify(xps.GetFixedDocumentSequence(),layout);}
        finally{if(File.Exists(preview))File.Delete(preview);}
        if(!digest.SequenceEqual(SHA256.HashData(File.ReadAllBytes(source))))throw new InvalidOperationException("Nguồn mẫu đầu/cuối trang trống bị thay đổi.");
        pass("Khổ/lề có đầu–cuối trang trống: sửa riêng với style tường minh, kiểm vùng chữ/bảng XPS, lỗi giữa lượt phục hồi, một Undo và lưu/mở giữ nguyên nguồn");
    }
}
