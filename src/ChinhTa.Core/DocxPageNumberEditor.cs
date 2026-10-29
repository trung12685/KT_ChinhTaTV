using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;

namespace ChinhTa.Core;

// Page-only header and section flags, in a separate candidate. No body range is
// replaced and no existing footer part is edited or deleted.
public static class DocxPageNumberEditor
{
    private static readonly XNamespace W=PageNumberPackage.W,R=PageNumberPackage.R,Rel=PageNumberPackage.Rel;
    private static readonly XNamespace Ct="http://schemas.openxmlformats.org/package/2006/content-types";
    private static void Child(XElement parent,string name,XElement value,string[] order)
    {
        parent.Elements(W+name).Remove();int rank=Array.IndexOf(order,name);
        var next=parent.Elements().FirstOrDefault(e=>Array.IndexOf(order,e.Name.LocalName)>rank);
        if(next is null)parent.Add(value);else next.AddBeforeSelf(value);
    }
    private static readonly string[] SectionOrder="headerReference footerReference footnotePr endnotePr type pgSz pgMar paperSrc pgBorders lnNumType pgNumType cols formProt vAlign noEndnote titlePg textDirection bidi rtlGutter docGrid printerSettings sectPrChange".Split(' ');
    private static readonly string[] RunOrder="rStyle rFonts b bCs i iCs caps smallCaps strike dstrike outline shadow emboss imprint noProof snapToGrid vanish webHidden color spacing w kern position sz szCs highlight u effect bdr shd fitText vertAlign rtl cs em lang eastAsianLayout specVanish oMath rPrChange".Split(' ');
    private static PageNumberPackage Patch(PageNumberPackage source,PageNumberFix fix,int pages)
    {
        var analysis=PageNumberRules.Analyze(source,pages);
        if(!fix.Valid || !analysis.Repairable || analysis.Key!=fix.SourceKey || analysis.Migration!=fix.FooterMigration || analysis.Errors.Count==0)
            throw new InvalidOperationException("Cấu hình PAGE hoặc điều kiện sửa đã thay đổi; hãy quét lại.");
        var target=source.Clone();var section=target.Section!;
        var references=section.Elements(W+"headerReference").Where(e=>(string?)e.Attribute(W+"type")=="default").ToArray();
        XElement header;
        if(references.Length==1)header=target.Resolve(references[0])!.Value.Root;
        else
        {
            string name="";for(int i=1;i<=10000;i++)if(!target.Parts.ContainsKey("word/header"+i+".xml")){name="word/header"+i+".xml";break;}
            if(name.Length==0)throw new InvalidOperationException("Không có tên phần header mới an toàn.");
            header=new XElement(W+"hdr");target.Parts.Add(name,new(header));
            if(!target.Parts.TryGetValue("word/_rels/document.xml.rels",out var relationships))
                target.Parts.Add("word/_rels/document.xml.rels",relationships=new(new XElement(Rel+"Relationships")));
            string id="rIdChinhTaPage";int suffix=0;
            while(relationships.Root!.Elements().Any(e=>(string?)e.Attribute("Id")==id))id="rIdChinhTaPage"+(++suffix).ToString(CultureInfo.InvariantCulture);
            relationships.Root!.Add(new XElement(Rel+"Relationship",new XAttribute("Id",id),new XAttribute("Type",R.NamespaceName+"/header"),new XAttribute("Target",name[5..])));
            Child(section,"headerReference",new(W+"headerReference",new XAttribute(W+"type","default"),new XAttribute(R+"id",id)),SectionOrder);
            if(!target.Parts.TryGetValue("[Content_Types].xml",out var content))throw new InvalidOperationException("Thiếu Content Types của DOCX.");
            content.Root!.Add(new XElement(Ct+"Override",new XAttribute("PartName","/"+name),new XAttribute("ContentType","application/vnd.openxmlformats-officedocument.wordprocessingml.header+xml")));
        }
        var inherited=analysis.Header;
        if(inherited is null)
        {
            var first=source.Section!.Elements(W+"headerReference").SingleOrDefault(e=>(string?)e.Attribute(W+"type")=="first");
            if(first is not null && source.Resolve(first) is { } part && PageNumberRules.Header(source,part.Root) is {Font.Known:true} oldFirst)inherited=oldFirst;
        }
        inherited??=analysis.FooterSource;
        float size=inherited?.Font.Size is >=13 and <=14?inherited.Font.Size.Value:13;
        var rpr=inherited?.Result.Element(W+"rPr") is { } original?new XElement(original):new XElement(W+"rPr");
        Child(rpr,"rFonts",new(W+"rFonts",new XAttribute(W+"ascii","Times New Roman"),new XAttribute(W+"hAnsi","Times New Roman")),RunOrder);
        Child(rpr,"i",new(W+"i",new XAttribute(W+"val",0)),RunOrder);
        Child(rpr,"color",new(W+"color",new XAttribute(W+"val","000000")),RunOrder);
        Child(rpr,"sz",new(W+"sz",new XAttribute(W+"val",(int)(size*2))),RunOrder);
        XElement Run(XElement value)=>new(W+"r",new XElement(rpr),value);
        var ppr=new XElement(W+"pPr",new XElement(W+"snapToGrid",new XAttribute(W+"val",0)),
            new XElement(W+"spacing",new XAttribute(W+"before",0),new XAttribute(W+"after",0),new XAttribute(W+"line",240),new XAttribute(W+"lineRule","auto")),
            new XElement(W+"ind",new XAttribute(W+"left",0),new XAttribute(W+"right",0),new XAttribute(W+"firstLine",0)),new XElement(W+"jc",new XAttribute(W+"val","center")));
        // Preserve a paragraph style: it may carry legitimate bold/underline.
        var style=inherited?.Paragraph.Element(W+"pPr")?.Element(W+"pStyle");if(style is not null)ppr.AddFirst(new XElement(style));
        header.ReplaceNodes(new XElement(W+"p",ppr,
            Run(new XElement(W+"fldChar",new XAttribute(W+"fldCharType","begin"),new XAttribute(W+"dirty",1))),
            Run(new XElement(W+"instrText",new XAttribute(XNamespace.Xml+"space","preserve")," PAGE \\* MERGEFORMAT ")),
            Run(new XElement(W+"fldChar",new XAttribute(W+"fldCharType","separate"))),
            Run(new XElement(W+"t","1")),Run(new XElement(W+"fldChar",new XAttribute(W+"fldCharType","end")))));
        // Do not delete a first-header part: dropping its reference hides only the
        // PAGE-only header. Custom first headers are excluded by the source guard.
        section.Elements(W+"headerReference").Where(e=>(string?)e.Attribute(W+"type")=="first").Remove();
        // Preserve every old footer part and relationship. Only the explicitly
        // approved PAGE-only references are detached; no mixed footer is edited.
        if(fix.FooterMigration is { } migration)
            section.Elements(W+"footerReference").Where(e=>migration.Removes((string?)e.Attribute(W+"type")??"")).Remove();
        if(analysis.CopyFooter)
        {
            var footer=section.Elements(W+"footerReference").Single(e=>(string?)e.Attribute(W+"type")=="default");
            var first=new XElement(footer);first.SetAttributeValue(W+"type","first");footer.AddAfterSelf(first);
        }
        Child(section,"pgNumType",new(W+"pgNumType",new XAttribute(W+"fmt","decimal"),new XAttribute(W+"start",1)),SectionOrder);
        Child(section,"titlePg",new(W+"titlePg"),SectionOrder);
        section.Element(W+"pgMar")!.SetAttributeValue(W+"header",454); // 8.0mm; safe inside the confirmed 20–25mm upper margin.
        return target;
    }
    public static void Write(string source,string candidate,PageNumberFix fix,int pages)
    {
        if(Path.GetFullPath(source)==Path.GetFullPath(candidate) || File.Exists(candidate))throw new InvalidOperationException("Candidate PAGE phải là file mới tách khỏi nguồn.");
        var old=PageNumberPackage.FromFile(source);var expected=Patch(old,fix,pages);File.Copy(source,candidate);
        try
        {
            using(var zip=ZipFile.Open(candidate,ZipArchiveMode.Update))
            foreach(var pair in expected.Parts)
            {
                if(old.Parts.TryGetValue(pair.Key,out var before) && XNode.DeepEquals(before,pair.Value))continue;
                zip.GetEntry(pair.Key)?.Delete();using var stream=zip.CreateEntry(pair.Key).Open();pair.Value.Save(stream,SaveOptions.DisableFormatting);
            }
            Verify(source,candidate,fix,pages);
        }
        catch{File.Delete(candidate);throw;}
    }
    public static void Verify(string source,string candidate,PageNumberFix fix,int pages)
    {
        var original=PageNumberPackage.FromFile(source);var expected=Patch(original,fix,pages);var actual=PageNumberPackage.FromFile(candidate);
        using var a=ZipFile.OpenRead(source);using var b=ZipFile.OpenRead(candidate);
        var added=expected.Parts.Keys.Except(original.Parts.Keys).ToArray();
        if(!a.Entries.Select(e=>e.FullName).Concat(added).Order().SequenceEqual(b.Entries.Select(e=>e.FullName).Order()))throw new InvalidOperationException("Danh sách phần DOCX thay đổi ngoài PAGE.");
        foreach(var part in expected.Parts)
            if(!actual.Parts.TryGetValue(part.Key,out var found) || PageNumberRules.Canonical(part.Value.Root!)!=PageNumberRules.Canonical(found.Root!))throw new InvalidOperationException("Phần XML không khớp thay đổi PAGE đã duyệt.");
        foreach(var entry in a.Entries)
        {
            bool changed=expected.Parts.TryGetValue(entry.FullName,out var after) && original.Parts.TryGetValue(entry.FullName,out var before) && !XNode.DeepEquals(before,after);
            if(changed)continue;
            using var x=entry.Open();using var y=b.GetEntry(entry.FullName)!.Open();using var xx=new MemoryStream();using var yy=new MemoryStream();x.CopyTo(xx);y.CopyTo(yy);
            if(!xx.ToArray().SequenceEqual(yy.ToArray()))throw new InvalidOperationException("Nội dung/phần khác bị đổi khi sửa PAGE.");
        }
    }
    public static void VerifyLive(PageNumberPackage before,PageNumberPackage after,PageNumberFix? fix=null,int pages=0)
    {
        var migration=fix?.FooterMigration;
        if(migration is not null)PageNumberRules.VerifyFooterMigration(before,pages,fix!);
        XElement Body(PageNumberPackage package)
        {
            var body=new XElement(package.Body??throw new InvalidOperationException("Thiếu phần thân để hậu kiểm PAGE."));
            body.Descendants(W+"lastRenderedPageBreak").Remove();var section=body.Elements(W+"sectPr").Single();
            section.Elements().Where(e=>e.Name==W+"headerReference" || e.Name==W+"footerReference" || e.Name==W+"pgNumType" || e.Name==W+"titlePg").Remove();
            section.Element(W+"pgMar")?.Attribute(W+"header")?.Remove();
            section.Attributes().Where(a=>a.Name.Namespace==W && a.Name.LocalName.StartsWith("rsid",StringComparison.Ordinal)).Remove();return body;
        }
        if(PageNumberRules.Canonical(Body(before))!=PageNumberRules.Canonical(Body(after)))throw new InvalidOperationException("Phần thân/chữ/bảng bị đổi ngoài PAGE.");
        var oldRefs=before.Section!.Elements(PageNumberPackage.W+"footerReference").ToArray();
        var expectedTypes=oldRefs.Where(e=>migration?.Removes((string?)e.Attribute(W+"type")??"")!=true)
            .Select(e=>(string?)e.Attribute(W+"type")??"").ToList();
        if(!PageNumberRules.On(before.Section.Element(W+"titlePg")) && migration?.Default is null &&
            oldRefs.Any(e=>(string?)e.Attribute(W+"type")=="default") && !expectedTypes.Contains("first"))expectedTypes.Add("first");
        if(!expectedTypes.Order(StringComparer.Ordinal).SequenceEqual(after.Section!.Elements(W+"footerReference")
            .Select(e=>(string?)e.Attribute(W+"type")??"").Order(StringComparer.Ordinal)))
            throw new InvalidOperationException("Tham chiếu footer thay đổi ngoài phần PAGE đã duyệt.");
        foreach(var old in oldRefs)
        {
            var current=after.Section!.Elements(W+"footerReference").SingleOrDefault(e=>(string?)e.Attribute(W+"type")== (string?)old.Attribute(W+"type"));
            string type=(string?)old.Attribute(W+"type")??"";
            if(migration?.Removes(type)==true)
            {
                if(current is not null || before.Resolve(old) is not { } removed ||
                    !after.Parts.TryGetValue(removed.Path,out var retained) || retained.Root is null ||
                    PageNumberRules.Canonical(removed.Root,ignoreParagraphIds:true)!=PageNumberRules.Canonical(retained.Root,ignoreParagraphIds:true))
                    throw new InvalidOperationException("PAGE cuối trang chưa bỏ đúng tham chiếu hoặc phần footer gốc không còn nguyên.");
                var oldRelations=before.Parts.GetValueOrDefault("word/_rels/document.xml.rels")?.Root?.Elements(Rel+"Relationship")
                    .Where(e=>(string?)e.Attribute("Id")== (string?)old.Attribute(PageNumberPackage.R+"id")).ToArray();
                var newRelations=after.Parts.GetValueOrDefault("word/_rels/document.xml.rels")?.Root?.Elements(Rel+"Relationship")
                    .Where(e=>(string?)e.Attribute("Id")== (string?)old.Attribute(PageNumberPackage.R+"id")).ToArray();
                if(oldRelations is not {Length:1} || newRelations is not {Length:1} ||
                    PageNumberRules.Canonical(oldRelations[0])!=PageNumberRules.Canonical(newRelations[0]))
                    throw new InvalidOperationException("Quan hệ footer PAGE gốc đã mất hoặc thay đổi.");
                continue;
            }
            if(current is null || before.Resolve(old) is not { } source || after.Resolve(current) is not { } target ||
                PageNumberRules.Canonical(source.Root,ignoreParagraphIds:true)!=PageNumberRules.Canonical(target.Root,ignoreParagraphIds:true))throw new InvalidOperationException("Footer cũ thay đổi hoặc mất sau sửa PAGE.");
        }
        // Enabling first-page mode must preserve the footer previously rendered on page 1.
        if(!PageNumberRules.On(before.Section.Element(W+"titlePg")))
        {
            var sourceRef=oldRefs.SingleOrDefault(e=>(string?)e.Attribute(W+"type")=="default");
            if(sourceRef is not null && migration?.Default is null)
            {
                var first=after.Section!.Elements(W+"footerReference").SingleOrDefault(e=>(string?)e.Attribute(W+"type")=="first");
                if(first is null || before.Resolve(sourceRef) is not { } source || after.Resolve(first) is not { } target ||
                    PageNumberRules.Canonical(source.Root,ignoreParagraphIds:true)!=PageNumberRules.Canonical(target.Root,ignoreParagraphIds:true))throw new InvalidOperationException("Footer trang đầu không còn như trước.");
            }
        }
    }
}
