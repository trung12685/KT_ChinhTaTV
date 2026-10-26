using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

// Optional real-Word scenario. Building this fixture is not a claim that Office
// ran it; CI only compiles it. All text and the marker image are synthetic.
internal static class OfficeSignatureFormatCheck
{
    private const string Authority="TRƯỞNG PHÒNG";
    private const string Signer="Đại tá Nguyễn Văn An";
    private const string ImagePart="word/media/signature-format-fixture.png";
    private static bool Owned(Issue i)=>i.Kind==FixKind.SignatureFormat;
    public static void Run(WordSession test,string modern,string folder,Action<string> pass)
    {
        string isolated=Path.Combine(folder,"signature-format");Directory.CreateDirectory(isolated);
        string source=Create(modern,isolated);var digest=SHA256.HashData(File.ReadAllBytes(source));
        test.Profile=DocumentProfile.State;test.Open(source);string original=test.ReadCheckText();
        var found=test.Check();var formats=found.Where(i=>i.CanFix && Owned(i)).ToArray();
        if(formats.Length!=2 || formats.Any(i=>i.CharacterFormat is not {PreserveSize:true,RepairInvalidSizes:true}))
            throw new InvalidOperationException("Mẫu chữ ký chưa nhận đủ hai vai trò có cỡ sai xen13/14.");
        var beforeAuthority=test.ReadCheckCurrentStyles(Authority);var beforeName=test.ReadCheckCurrentStyles(Signer);
        var authority=formats.Single(i=>i.Original==Authority);var name=formats.Single(i=>i.Original==Signer);
        var expectedAuthority=beforeAuthority.WithFormat(authority.CharacterFormat!);var expectedName=beforeName.WithFormat(name.CharacterFormat!);
        test.Fix(authority);test.Check();
        if(test.Check().Count(Owned)!=1 || test.ReadCheckText()!=original || test.CheckUndoCount!=1 ||
            !expectedAuthority.Matches(test.ReadCheckCurrentStyles(Authority)) || !beforeName.Matches(test.ReadCheckCurrentStyles(Signer)))
            throw new InvalidOperationException("Sửa riêng chức vụ chưa giữ họ tên chưa chọn/kiểu/cỡ/căn đoạn.");
        test.Undo();test.Check();
        if(test.ReadCheckText()!=original || !beforeAuthority.Matches(test.ReadCheckCurrentStyles(Authority)))
            throw new InvalidOperationException("Undo chữ ký chưa về nguyên bản.");
        Issue[] Selection()=>test.Check().Where(i=>i.CanFix && Owned(i)).Concat(OfficeComponentCheck.ReadLines(test)).ToArray();
        var selected=Selection();
        if(selected.Count(i=>i.LineFix is not null)!=3)throw new InvalidOperationException("Mẫu chữ ký chưa nhận đủ ba Line để kiểm sửa chung.");
        bool restored=false;
        try{test.FixStagedFailureProbe(selected,3);}catch(InvalidOperationException e) when(e.InnerException is OfficeStagedProbeException){restored=true;}
        test.Check();
        if(!restored || test.ReadCheckText()!=original || test.CheckUndoCount!=0 || test.Changed ||
            !beforeAuthority.Matches(test.ReadCheckCurrentStyles(Authority)) || !beforeName.Matches(test.ReadCheckCurrentStyles(Signer)))
            throw new InvalidOperationException("Rollback chữ ký/ba Line chưa khôi phục cả lượt.");
        test.FixSelection(Selection());test.Check();
        if(test.Check().Any(Owned) || test.ReadCheckText()!=original || test.CheckUndoCount!=1 ||
            !expectedAuthority.Matches(test.ReadCheckCurrentStyles(Authority)) || !expectedName.Matches(test.ReadCheckCurrentStyles(Signer)))
            throw new InvalidOperationException("Sửa chung chữ ký/Line chưa giữ chữ/cỡ đúng/gạch chân/căn-khoảng đoạn hoặc còn lỗi cũ.");
        string saved=Path.Combine(isolated,"signature-fixed.docx");test.SaveCopy(saved);VerifyImage(source,saved);
        test.Undo();test.Check();
        if(test.ReadCheckText()!=original || !beforeName.Matches(test.ReadCheckCurrentStyles(Signer)))
            throw new InvalidOperationException("Một Undo chưa phục hồi họ tên người ký.");
        test.Open(saved);test.Check();
        if(test.Check().Any(Owned) || test.ReadCheckText()!=original || !expectedName.Matches(test.ReadCheckCurrentStyles(Signer)) ||
            !digest.SequenceEqual(SHA256.HashData(File.ReadAllBytes(source))))
            throw new InvalidOperationException("Lưu/mở chữ ký chưa đạt hoặc nguồn đã đổi.");
        pass("Chữ ký: hai vai trò, giữ tên/cấp bậc/cỡ13-14/gạch chân/bố cục/ảnh; sửa riêng/chung ba Line, rollback, một Undo và lưu-mở");
    }
    internal static void VerifyImage(string source,string saved)
    {
        byte[] Read(string path)
        {
            using var zip=ZipFile.OpenRead(path);using var stream=zip.GetEntry(ImagePart)?.Open()??throw new InvalidOperationException("Mất ảnh mẫu chữ ký.");
            using var memory=new MemoryStream();stream.CopyTo(memory);return memory.ToArray();
        }
        byte[] original=Read(source);
        using var z=ZipFile.OpenRead(saved);using var s=z.GetEntry("word/document.xml")!.Open();var xml=XDocument.Load(s);
        XNamespace a="http://schemas.openxmlformats.org/drawingml/2006/main",r="http://schemas.openxmlformats.org/officeDocument/2006/relationships",
            wp="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing";
        using var relStream=z.GetEntry("word/_rels/document.xml.rels")!.Open();var rels=XDocument.Load(relStream);
        var ids=new List<string>();
        foreach(var rel in rels.Root!.Elements().Where(e=>(string?)e.Attribute("Type")==r.NamespaceName+"/image" && (string?)e.Attribute("TargetMode")!="External"))
        {
            string target=new Uri(new Uri("https://fixture/word/document.xml"),(string?)rel.Attribute("Target")??"").AbsolutePath.TrimStart('/');
            var image=z.GetEntry(target);if(image is null)continue;
            using var stream=image.Open();using var memory=new MemoryStream();stream.CopyTo(memory);
            if(original.SequenceEqual(memory.ToArray()))ids.Add((string)rel.Attribute("Id")!);
        }
        var blips=xml.Descendants(a+"blip").Where(e=>ids.Contains((string?)e.Attribute(r+"embed")??"")).ToArray();
        var extent=blips.SingleOrDefault()?.Ancestors(wp+"inline").SingleOrDefault()?.Element(wp+"extent");
        if(blips.Length!=1 || (long?)extent?.Attribute("cx")!=635000 || (long?)extent?.Attribute("cy")!=254000)
            throw new InvalidOperationException("Ảnh mẫu chữ ký mất dữ liệu/liên kết hiển thị hoặc đổi kích thước sau lưu.");
    }
    internal static string Create(string modern,string folder)
    {
        string seed=OfficeComponentCheck.Create(modern,folder,false),path=Path.Combine(folder,"signature-input.docx");File.Copy(seed,path);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);
        XDocument Read(string part){using var s=zip.GetEntry(part)!.Open();return XDocument.Load(s,LoadOptions.PreserveWhitespace);}
        void Write(string part,XDocument xml){zip.GetEntry(part)?.Delete();using var s=zip.CreateEntry(part).Open();xml.Save(s);}
        var doc=Read("word/document.xml");XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XElement Run(string text,int size)=>new(w+"r",new XElement(w+"rPr",
            new XElement(w+"rFonts",new XAttribute(w+"ascii","Arial"),new XAttribute(w+"hAnsi","Arial")),
            new XElement(w+"b",new XAttribute(w+"val",0)),new XElement(w+"i"),new XElement(w+"color",new XAttribute(w+"val","FF0000")),
            new XElement(w+"sz",new XAttribute(w+"val",size*2)),new XElement(w+"u",new XAttribute(w+"val","single"))),
            new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),text));
        XElement P(string text)=>new(w+"p",new XElement(w+"pPr",new XElement(w+"spacing",new XAttribute(w+"before",0),new XAttribute(w+"after",120)),
            new XElement(w+"jc",new XAttribute(w+"val","center"))),Run(text[..3],12),Run(text[3..6],13),Run(text[6..],14));
        var cell=doc.Descendants(w+"body").Single().Elements(w+"tbl").Last().Elements(w+"tr").Single().Elements(w+"tc").Last();
        cell.Elements(w+"p").Remove();
        const string relId="rIdSignatureFormatFixture";
        XNamespace wp="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing",a="http://schemas.openxmlformats.org/drawingml/2006/main",
            pic="http://schemas.openxmlformats.org/drawingml/2006/picture",r="http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        int id=doc.Descendants(wp+"docPr").Select(e=>(int?)e.Attribute("id")??0).DefaultIfEmpty(0).Max()+1;
        var geometry=new XElement(pic+"spPr",new XElement(a+"xfrm",
            new XElement(a+"off",new XAttribute("x",0),new XAttribute("y",0)),
            new XElement(a+"ext",new XAttribute("cx",635000),new XAttribute("cy",254000))),
            new XElement(a+"prstGeom",new XAttribute("prst","rect"),new XElement(a+"avLst")));
        var drawing=new XElement(pic+"pic",
            new XElement(pic+"nvPicPr",new XElement(pic+"cNvPr",new XAttribute("id",0),new XAttribute("name","signature-format-fixture.png")),new XElement(pic+"cNvPicPr")),
            new XElement(pic+"blipFill",new XElement(a+"blip",new XAttribute(r+"embed",relId)),new XElement(a+"stretch",new XElement(a+"fillRect"))),geometry);
        var inline=new XElement(wp+"inline",new XElement(wp+"extent",new XAttribute("cx",635000),new XAttribute("cy",254000)),
            new XElement(wp+"docPr",new XAttribute("id",id),new XAttribute("name","Synthetic signature marker")),
            new XElement(a+"graphic",new XElement(a+"graphicData",new XAttribute("uri",pic.NamespaceName),drawing)));
        var picture=new XElement(w+"p",new XElement(w+"r",new XElement(w+"drawing",inline)));
        cell.Add(P(Authority),new XElement(w+"p"),picture,new XElement(w+"p"),P(Signer));
        var rels=zip.GetEntry("word/_rels/document.xml.rels") is null?
            new XDocument(new XElement(XNamespace.Get("http://schemas.openxmlformats.org/package/2006/relationships")+"Relationships")):
            Read("word/_rels/document.xml.rels");
        rels.Root!.Add(new XElement(rels.Root.Name.Namespace+"Relationship",new XAttribute("Id",relId),new XAttribute("Type",r.NamespaceName+"/image"),new XAttribute("Target","media/signature-format-fixture.png")));
        var types=Read("[Content_Types].xml");
        if(!types.Root!.Elements().Any(e=>(string?)e.Attribute("Extension")=="png"))types.Root.Add(new XElement(types.Root.Name.Namespace+"Default",new XAttribute("Extension","png"),new XAttribute("ContentType","image/png")));
        using(var s=zip.CreateEntry(ImagePart).Open())s.Write(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4nGNgYPj/HwADAgH/5ncLrgAAAABJRU5ErkJggg=="));
        // Earlier synthetic seeds use Word-tolerated property ordering. Put
        // only this new test package into schema order before strict validation.
        void Order(string parent,string order)
        {
            var names=order.Split(' ');
            foreach(var properties in doc.Descendants(w+parent))
            {
                var children=properties.Elements().OrderBy(e=>{int index=Array.IndexOf(names,e.Name.LocalName);return index<0?int.MaxValue:index;}).ToArray();
                properties.ReplaceNodes(children);
            }
        }
        Order("rPr","rStyle rFonts b bCs i iCs caps smallCaps strike dstrike outline shadow emboss imprint noProof snapToGrid vanish webHidden color spacing w kern position sz szCs highlight u effect bdr shd fitText vertAlign rtl cs em lang eastAsianLayout specVanish oMath rPrChange");
        Order("pPr","pStyle keepNext keepLines pageBreakBefore framePr widowControl numPr suppressLineNumbers pBdr shd tabs suppressAutoHyphens kinsoku wordWrap overflowPunct topLinePunct autoSpaceDE autoSpaceDN bidi adjustRightInd snapToGrid spacing ind contextualSpacing mirrorIndents suppressOverlap jc textDirection textAlignment textboxTightWrap outlineLvl divId cnfStyle rPr sectPr pPrChange");
        Write("word/document.xml",doc);Write("word/_rels/document.xml.rels",rels);Write("[Content_Types].xml",types);return path;
    }
}
