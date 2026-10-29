using System.IO.Compression;
using System.Xml.Linq;
using ChinhTa.Core;

public static class EmptyHeaderFooterChecks
{
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main",R="http://schemas.openxmlformats.org/officeDocument/2006/relationships",
        Rel="http://schemas.openxmlformats.org/package/2006/relationships";
    private static Dictionary<string,XDocument> Parts()
    {
        var parts=PageNumberChecks.Parts(header:true,footer:true);
        parts["word/document.xml"]=PageLayoutChecks.Document(PageLayoutChecks.Bad);
        var section=parts["word/document.xml"].Descendants(W+"sectPr").Single();
        section.AddFirst(new XElement(W+"headerReference",new XAttribute(W+"type","default"),new XAttribute(R+"id","rIdHeader")),
            new XElement(W+"footerReference",new XAttribute(W+"type","default"),new XAttribute(R+"id","rIdFooter")));
        parts["word/header1.xml"].Root!.ReplaceNodes(new XElement(W+"p"));
        parts["word/footer1.xml"].Root!.ReplaceNodes(new XElement(W+"p"));
        return parts;
    }
    private static string Flat(Dictionary<string,XDocument> parts)
    {
        XNamespace pkg="http://schemas.microsoft.com/office/2006/xmlPackage";
        return new XDocument(new XElement(pkg+"package",parts.Select(p=>new XElement(pkg+"part",new XAttribute(pkg+"name","/"+p.Key),
            new XElement(pkg+"xmlData",new XElement(p.Value.Root!)))))).ToString();
    }
    private static XElement Header(Dictionary<string,XDocument> parts)=>parts["word/header1.xml"].Root!;
    private static XElement Section(Dictionary<string,XDocument> parts)=>parts["word/document.xml"].Descendants(W+"sectPr").Single();
    private static XDocument Styles(params XElement[] styles)=>new(new XElement(W+"styles",styles));
    private static XElement Style(string id,XElement? properties=null,string? parent=null)=>new(W+"style",new XAttribute(W+"type","paragraph"),new XAttribute(W+"styleId",id),
        parent is null?null:new XElement(W+"basedOn",new XAttribute(W+"val",parent)),properties);
    public static void Run(Action<bool,string> assert)
    {
        bool Eligible(Dictionary<string,XDocument> parts)=>PageLayoutGuard.Eligible(XDocument.Parse(Flat(parts),LoadOptions.PreserveWhitespace),PageLayoutChecks.Bad);
        bool Scope(Dictionary<string,XDocument> parts)=>EmptyHeaderFooterScope.Proven(XDocument.Parse(Flat(parts),LoadOptions.PreserveWhitespace));
        var ordinary=Parts();
        assert(Eligible(ordinary) && Scope(ordinary),"C32 admits a single ordinary section whose referenced header and footer are genuinely empty");
        var noRefs=PageLayoutChecks.Document(PageLayoutChecks.Bad);
        assert(EmptyHeaderFooterScope.Proven(noRefs) && PageLayoutGuard.Eligible(noRefs,PageLayoutChecks.Bad),"Existing no-reference C32 documents remain eligible");
        foreach(string kind in new[]{"header","footer"})
        foreach(string role in new[]{"default","first","even"})
        {
            var parts=Parts();Section(parts).Elements(W+(kind+"Reference")).Single().SetAttributeValue(W+"type",role);
            assert(Eligible(parts),"An empty "+kind+" is proven for the "+role+" reference, including currently inactive first/even roles");
        }
        var all=Parts();
        foreach(string kind in new[]{"header","footer"})
        foreach(string role in new[]{"first","even"})
            Section(all).AddFirst(new XElement(W+(kind+"Reference"),new XAttribute(W+"type",role),new XAttribute(R+"id",kind=="header"?"rIdHeader":"rIdFooter")));
        assert(Eligible(all),"All six supported empty references may share their proven empty parts");
        var namespaceAlias=Parts();namespaceAlias["word/document.xml"].Root!.Add(new XAttribute(XNamespace.Xmlns+"alias",W.NamespaceName));
        assert(PageLayoutGuard.Eligible(namespaceAlias["word/document.xml"],PageLayoutChecks.Bad,PageNumberChecks.Package(namespaceAlias)),
            "Reference equality uses expanded names and values instead of namespace-prefix serialization");
        var styled=Parts();Header(styled).Element(W+"p")!.AddFirst(new XElement(W+"pPr",new XElement(W+"pStyle",new XAttribute(W+"val","Header"))));
        styled["word/styles.xml"]=Styles(Style("Normal",new XElement(W+"pPr",new XElement(W+"spacing",new XAttribute(W+"after",10000)))),
            Style("Header",new XElement(W+"pPr",new XElement(W+"jc",new XAttribute(W+"val","center"))),"Normal"));
        assert(Eligible(styled),"Known empty paragraph style inheritance is allowed; actual reserved height is checked in the rendered body guard");
        var defaultStyle=Parts();var normal=Style("Normal",new XElement(W+"pPr",new XElement(W+"spacing",new XAttribute(W+"line",240))));normal.SetAttributeValue(W+"default",1);
        defaultStyle["word/styles.xml"]=Styles(normal,new XElement(W+"docDefaults",new XElement(W+"rPrDefault",new XElement(W+"rPr",new XElement(W+"sz",new XAttribute(W+"val",26))))));
        assert(Eligible(defaultStyle),"A unique known default paragraph style and ordinary run defaults do not invent visible header content");
        var emptyRun=Parts();Header(emptyRun).Element(W+"p")!.Add(new XElement(W+"r",new XElement(W+"rPr",new XElement(W+"b")),new XElement(W+"t","")));
        assert(Eligible(emptyRun),"A truly empty ordinary run remains empty even when it has non-painting font settings");
        foreach(var change in new Action<Dictionary<string,XDocument>>[]
        {
            p=>Header(p).Element(W+"p")!.Add(new XElement(W+"r",new XElement(W+"t"," "))),
            p=>Header(p).Element(W+"p")!.Add(new XElement(W+"r",new XElement(W+"t","Header"))),
            p=>Header(p).Element(W+"p")!.Add(new XElement(W+"fldSimple",new XAttribute(W+"instr"," PAGE "))),
            p=>Header(p).Element(W+"p")!.Add(new XElement(W+"r",new XElement(W+"fldChar",new XAttribute(W+"fldCharType","begin")))),
            p=>Header(p).Element(W+"p")!.Add(new XElement(W+"r",new XElement(W+"drawing"))),
            p=>Header(p).Element(W+"p")!.Add(new XElement(W+"sdt")),
            p=>Header(p).Add(new XElement(W+"p")),
            p=>Header(p).Add(new XElement(W+"tbl")),
            p=>Header(p).Add("Unexpected literal content"),
            p=>Header(p).Element(W+"p")!.AddFirst(new XElement(W+"pPr",new XElement(W+"pBdr"))),
            p=>Header(p).Element(W+"p")!.AddFirst(new XElement(W+"pPr",new XElement(W+"shd"))),
            p=>Header(p).Element(W+"p")!.AddFirst(new XElement(W+"pPr",new XElement(W+"numPr"))),
            p=>Header(p).Element(W+"p")!.AddFirst(new XElement(W+"pPr",new XElement(W+"rPr",new XElement(W+"u")))),
            p=>Header(p).Element(W+"p")!.Add(new XElement(W+"r",new XElement(W+"rPr",new XElement(W+"u")))),
            p=>Header(p).Element(W+"p")!.Add(new XElement(W+"r",new XElement(W+"rPr",new XElement(W+"strike")))),
            p=>Header(p).Element(W+"p")!.Add(new XElement(W+"r",new XElement(W+"rPr",new XElement(W+"bdr")))),
            p=>Header(p).Element(W+"p")!.Add(new XElement(W+"r",new XElement(W+"rPr",new XElement(W+"color",new XElement(W+"u"))))),
            p=>Header(p).Element(W+"p")!.AddFirst(new XElement(W+"pPr",new XElement(W+"spacing"),new XElement(W+"spacing"))),
            p=>p["word/footer1.xml"].Root!.ReplaceNodes(new XElement(W+"p",new XElement(W+"r",new XElement(W+"t"," "))))
        })
        {var parts=Parts();change(parts);assert(!Eligible(parts),"Text, whitespace, fields, object/control, multiple paragraphs and direct decoration stay outside empty-header C32");}
        foreach(string decoration in new[]{"pBdr","shd","numPr","rPr","framePr","outlineLvl"})
        {
            var parts=Parts();var style=Style("Normal",new XElement(W+"pPr",new XElement(W+decoration)));style.SetAttributeValue(W+"default",1);parts["word/styles.xml"]=Styles(style);
            assert(!Eligible(parts),"The default paragraph style cannot hide "+decoration+" in an empty-looking header");
        }
        foreach(string decoration in new[]{"u","strike","dstrike","bdr","shd","rStyle","effect","outline"})
        {
            var parts=Parts();parts["word/styles.xml"]=Styles(new XElement(W+"docDefaults",new XElement(W+"rPrDefault",new XElement(W+"rPr",new XElement(W+decoration)))));
            assert(!Eligible(parts),"Run defaults with "+decoration+" are not treated as paint-free empty content");
        }
        foreach(var change in new Action<Dictionary<string,XDocument>>[]
        {
            p=>{Header(p).Element(W+"p")!.AddFirst(new XElement(W+"pPr",new XElement(W+"pStyle",new XAttribute(W+"val","Missing"))));},
            p=>Header(p).Element(W+"p")!.AddFirst(new XElement(W+"pPr",new XElement(W+"pStyle"))),
            p=>{var style=Style("Normal");style.SetAttributeValue(W+"default",1);style.Add(new XElement(W+"basedOn"));p["word/styles.xml"]=Styles(style);},
            p=>{var style=Style("Normal",parent:"Normal");style.SetAttributeValue(W+"default",1);p["word/styles.xml"]=Styles(style);},
            p=>{var a=Style("A",parent:"B");a.SetAttributeValue(W+"default",1);p["word/styles.xml"]=Styles(a,Style("B",parent:"A"));},
            p=>{var a=Style("Normal");a.SetAttributeValue(W+"default",1);p["word/styles.xml"]=Styles(a,Style("Normal"));},
            p=>{var a=Style("A");a.SetAttributeValue(W+"default",1);var b=Style("B");b.SetAttributeValue(W+"default",1);p["word/styles.xml"]=Styles(a,b);},
            p=>{var a=Style("Normal");a.SetAttributeValue(W+"default","unknown");p["word/styles.xml"]=Styles(a);},
            p=>{var a=Style("Normal");a.SetAttributeValue(W+"default",1);a.Attribute(W+"styleId")!.Remove();p["word/styles.xml"]=Styles(a);},
            p=>{var a=Style("Normal",parent:"Missing");a.SetAttributeValue(W+"default",1);p["word/styles.xml"]=Styles(a);},
            p=>p["word/styles.xml"]=new(new XElement(W+"settings")),
            p=>p["word/styles.xml"]=Styles(new XElement(W+"docDefaults"),new XElement(W+"docDefaults")),
            p=>p["word/styles.xml"]=Styles(new XElement(W+"docDefaults",new XElement(W+"pPrDefault",new XElement(W+"numPr")))),
            p=>{var a=Style("Normal",new XElement(W+"pPr",new XElement(W+"pStyle",new XAttribute(W+"val","Other"))));a.SetAttributeValue(W+"default",1);p["word/styles.xml"]=Styles(a);}
        })
        {var parts=Parts();change(parts);assert(!Eligible(parts),"Unknown, missing, duplicate, cyclic and malformed style evidence keeps C32 manual");}
        foreach(var change in new Action<Dictionary<string,XDocument>>[]
        {
            p=>Section(p).Elements(W+"headerReference").Single().Attribute(R+"id")!.Remove(),
            p=>Section(p).Elements(W+"headerReference").Single().SetAttributeValue(R+"id",""),
            p=>Section(p).Elements(W+"headerReference").Single().SetAttributeValue(R+"id","Missing"),
            p=>Section(p).Elements(W+"headerReference").Single().SetAttributeValue(W+"type","unknown"),
            p=>Section(p).Elements(W+"headerReference").Single().SetAttributeValue(W+"mystery",1),
            p=>Section(p).Add(new XElement(Section(p).Elements(W+"headerReference").Single())),
            p=>p["word/document.xml"].Descendants(W+"p").First().Add(new XElement(W+"headerReference",new XAttribute(W+"type","first"),new XAttribute(R+"id","rIdHeader"))),
            p=>p["word/_rels/document.xml.rels"].Root!.Add(new XElement(p["word/_rels/document.xml.rels"].Root!.Elements().Last())),
            p=>p["word/_rels/document.xml.rels"].Root!.Elements().Last().SetAttributeValue("TargetMode","External"),
            p=>p["word/_rels/document.xml.rels"].Root!.Elements().Last().SetAttributeValue("TargetMode","unknown"),
            p=>p["word/_rels/document.xml.rels"].Root!.Elements().Last().SetAttributeValue("Target","https://example.com/header.xml"),
            p=>p["word/_rels/document.xml.rels"].Root!.Elements().Last().SetAttributeValue("Target","http://["),
            p=>p["word/_rels/document.xml.rels"].Root!.Elements().Last().SetAttributeValue("Target","header1.xml#part"),
            p=>p["word/_rels/document.xml.rels"].Root!.Elements().Last().SetAttributeValue("Target","missing.xml"),
            p=>p["word/_rels/document.xml.rels"].Root!.Elements().Last().SetAttributeValue("Type",R.NamespaceName+"/footer"),
            p=>p["word/header1.xml"]=new(new XElement(W+"ftr",new XElement(W+"p"))),
            p=>p.Remove("word/header1.xml"),
            p=>Section(p).AddAfterSelf(new XElement(W+"p"))
        })
        {var parts=Parts();change(parts);assert(!Eligible(parts),"Missing/external/duplicate/rogue/malformed references and non-final sections fail closed without granting a C32 write");}
        var unrelated=Parts();unrelated["word/_rels/document.xml.rels"].Root!.Elements().Last().SetAttributeValue("TargetMode","Internal");
        assert(Eligible(unrelated),"Explicit Internal relationship mode retains empty-header proof");
        var fix=PageLayoutFix.Propose(PageLayoutChecks.Bad)!;string folder=Path.Combine(Path.GetTempPath(),"ChinhTa-empty-page-"+Guid.NewGuid());Directory.CreateDirectory(folder);
        try
        {
            string source=Path.Combine(folder,"source.docx"),candidate=Path.Combine(folder,"candidate.docx");var parts=Parts();PageNumberChecks.Write(source,parts);var original=File.ReadAllBytes(source);
            DocxPageLayoutEditor.Write(source,candidate,fix);DocxPageLayoutEditor.Verify(source,candidate,fix);
            assert(original.SequenceEqual(File.ReadAllBytes(source)),"C32 with empty referenced parts preserves the source package byte for byte");
            using(var a=ZipFile.OpenRead(source))using(var b=ZipFile.OpenRead(candidate))
            {
                foreach(var item in a.Entries.Where(e=>e.FullName!="word/document.xml"))
                {using var x=item.Open();using var y=b.GetEntry(item.FullName)!.Open();using var xx=new MemoryStream();using var yy=new MemoryStream();x.CopyTo(xx);y.CopyTo(yy);assert(xx.ToArray().SequenceEqual(yy.ToArray()),"C32 preserves the bytes of unrelated package part "+item.FullName);}
                using var input=b.GetEntry("word/document.xml")!.Open();parts["word/document.xml"]=XDocument.Load(input,LoadOptions.PreserveWhitespace);
            }
            string before=Flat(Parts()),after=Flat(parts);DocxPageLayoutEditor.VerifyBody(before,after,fix);
            assert(PageLayoutGuard.Eligible(XDocument.Parse(after,LoadOptions.PreserveWhitespace),fix.After),"Saved/reopened Flat OPC with unchanged empty references keeps its C32 scope");
            foreach(var change in new Action<Dictionary<string,XDocument>>[]
            {
                p=>Header(p).Element(W+"p")!.Add(new XElement(W+"r",new XElement(W+"t","Inserted header"))),
                p=>Header(p).Element(W+"p")!.Add(new XElement(W+"r",new XElement(W+"t"," "))),
                p=>Header(p).Element(W+"p")!.Add(new XElement(W+"fldSimple",new XAttribute(W+"instr"," PAGE "))),
                p=>Header(p).Element(W+"p")!.AddFirst(new XElement(W+"pPr",new XElement(W+"pBdr"))),
                p=>p["word/styles.xml"]=Styles(new XElement(W+"docDefaults",new XElement(W+"rPrDefault",new XElement(W+"rPr",new XElement(W+"u"))))),
                p=>p["word/_rels/document.xml.rels"].Root!.Elements().Last().SetAttributeValue("Target","missing.xml")
            })
            {var tampered=parts.ToDictionary(p=>p.Key,p=>new XDocument(p.Value));change(tampered);bool failed=false;try{DocxPageLayoutEditor.VerifyBody(before,Flat(tampered),fix);}catch(InvalidOperationException){failed=true;}assert(failed,"New header paint, styles or broken references after Word require whole-batch restoration");}
            string tamperedPath=Path.Combine(folder,"tampered.docx");File.Copy(candidate,tamperedPath);
            using(var zip=ZipFile.Open(tamperedPath,ZipArchiveMode.Update)){zip.GetEntry("word/header1.xml")!.Delete();using var output=zip.CreateEntry("word/header1.xml").Open();new XDocument(new XElement(W+"hdr",new XElement(W+"p",new XElement(W+"r",new XElement(W+"t","changed"))))).Save(output);}
            bool rejected=false;try{DocxPageLayoutEditor.Verify(source,tamperedPath,fix);}catch(InvalidOperationException){rejected=true;}
            assert(rejected,"Candidate verification rejects an edited empty header part even though only page attributes were approved");
        }
        finally{Directory.Delete(folder,true);}
    }
}
