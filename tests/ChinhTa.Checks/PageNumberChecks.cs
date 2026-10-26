using System.IO.Compression;
using System.Xml.Linq;
using ChinhTa.Core;

public static class PageNumberChecks
{
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main",R="http://schemas.openxmlformats.org/officeDocument/2006/relationships",
        Rel="http://schemas.openxmlformats.org/package/2006/relationships",Ct="http://schemas.openxmlformats.org/package/2006/content-types";
    public static Dictionary<string,XDocument> Parts(bool header=false,bool correct=false,bool footer=true,float size=14)
    {
        var doc=PageLayoutChecks.Document(new(1,0,"N",210,297,20,20,30,15));var section=doc.Descendants(W+"sectPr").Single();
        section.Element(W+"pgMar")!.SetAttributeValue(W+"header",454);
        var types=new XDocument(new XElement(Ct+"Types",new XElement(Ct+"Default",new XAttribute("Extension","rels"),new XAttribute("ContentType","application/vnd.openxmlformats-package.relationships+xml")),
            new XElement(Ct+"Default",new XAttribute("Extension","xml"),new XAttribute("ContentType","application/xml")),
            new XElement(Ct+"Override",new XAttribute("PartName","/word/document.xml"),new XAttribute("ContentType","application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"))));
        var rels=new XDocument(new XElement(Rel+"Relationships"));var parts=new Dictionary<string,XDocument>
        {
            ["word/document.xml"]=doc,["word/_rels/document.xml.rels"]=rels,["[Content_Types].xml"]=types,
            ["_rels/.rels"]=new(new XElement(Rel+"Relationships",new XElement(Rel+"Relationship",new XAttribute("Id","rId1"),new XAttribute("Type",R.NamespaceName+"/officeDocument"),new XAttribute("Target","word/document.xml"))))
        };
        void Add(string kind,string path,string id,XDocument xml)
        {
            parts.Add(path,xml);rels.Root!.Add(new XElement(Rel+"Relationship",new XAttribute("Id",id),new XAttribute("Type",R.NamespaceName+"/"+kind),new XAttribute("Target",path[5..])));
            section.AddFirst(new XElement(W+(kind+"Reference"),new XAttribute(W+"type","default"),new XAttribute(R+"id",id)));
            types.Root!.Add(new XElement(Ct+"Override",new XAttribute("PartName","/"+path),new XAttribute("ContentType","application/vnd.openxmlformats-officedocument.wordprocessingml."+kind+"+xml")));
        }
        if(footer)Add("footer","word/footer1.xml","rIdFooter",new(new XElement(W+"ftr",new XElement(W+"p",new XElement(W+"r",new XElement(W+"t","Footer phải giữ nguyên."))))));
        if(header)
        {
            var result=new XElement(W+"r",new XElement(W+"rPr",new XElement(W+"rFonts",new XAttribute(W+"ascii",correct?"Times New Roman":"Arial"),new XAttribute(W+"hAnsi",correct?"Times New Roman":"Arial")),
                new XElement(W+"b"),new XElement(W+"i",new XAttribute(W+"val",correct?0:1)),new XElement(W+"color",new XAttribute(W+"val",correct?"000000":"FF0000")),
                new XElement(W+"sz",new XAttribute(W+"val",(int)(size*2))),new XElement(W+"u",new XAttribute(W+"val","single"))),new XElement(W+"t","2"));
            Add("header","word/header1.xml","rIdHeader",new(new XElement(W+"hdr",new XElement(W+"p",new XElement(W+"pPr",new XElement(W+"jc",new XAttribute(W+"val",correct?"center":"right"))),
                new XElement(W+"fldSimple",new XAttribute(W+"instr"," PAGE "),result)))));
            if(correct)section.Add(new XElement(W+"pgNumType",new XAttribute(W+"fmt","decimal"),new XAttribute(W+"start",1)),new XElement(W+"titlePg"));
        }
        return parts;
    }
    public static PageNumberPackage Package(Dictionary<string,XDocument> parts)
    {
        XNamespace pkg="http://schemas.microsoft.com/office/2006/xmlPackage";
        return PageNumberPackage.FromFlat(new XDocument(new XElement(pkg+"package",parts.Select(p=>new XElement(pkg+"part",new XAttribute(pkg+"name","/"+p.Key),
            new XElement(pkg+"xmlData",new XElement(p.Value.Root!)))))).ToString());
    }
    public static void Write(string path,Dictionary<string,XDocument> parts)
    {
        using var zip=ZipFile.Open(path,ZipArchiveMode.Create);foreach(var part in parts)
        {using var s=zip.CreateEntry(part.Key).Open();part.Value.Save(s,SaveOptions.DisableFormatting);}
        using(var other=new StreamWriter(zip.CreateEntry("customXml/untouched.txt").Open()))other.Write("Dữ liệu riêng phải giữ nguyên byte.");
        using(var opaque=new StreamWriter(zip.CreateEntry("customXml/opaque.xml").Open()))opaque.Write("Opaque data: do not parse or execute this unrelated part.");
    }
    public static void Run(Action<bool,string> assert)
    {
        IReadOnlyList<Issue> Check(Dictionary<string,XDocument> parts,int pages=2)=>PageNumberRules.Check(Package(parts),pages,0,"N",DocumentProfile.State);
        var missing=Parts();var issue=Check(missing).Single();
        assert(issue.CanFix && issue.NumberFix is {Valid:true} && issue.Title=="Thiếu số trang từ trang 2","Empty headers on a multi-page State document receive one concrete PAGE proposal");
        assert(Check(missing,1).Count==0,"A one-page document without a page header has no missing-number warning because page 1 is hidden");
        var correct=Parts(header:true,correct:true);
        var leading=Parts(header:true,correct:true);leading["word/document.xml"].Descendants(W+"pgNumType").Single().SetAttributeValue(W+"start","01");
        assert(Check(leading).Count==0,"An integer start serialized as 01 is semantically 1 and does not become a false numbering error");
        assert(Check(correct).Count==0 && PageNumberRules.ProvenStandard(Package(correct),2),"A correct PAGE header, Arabic start and hidden first page do not create a false warning");
        foreach(float size in new[]{13f,13.5f,14f})
        {
            var legal=Parts(header:true,correct:true,size:size);
            assert(Check(legal).Count==0,"All legal PAGE sizes including half-points are retained as correct");
        }
        var internalMode=Parts(header:true,correct:true);
        foreach(var rel in internalMode["word/_rels/document.xml.rels"].Root!.Elements())rel.SetAttributeValue("TargetMode","Internal");
        assert(Check(internalMode).Count==0,"Explicit Internal relationship mode is equivalent to the default and does not lose header/footer recognition");
        var wrong=Parts(header:true);var defect=Check(wrong).Single();
        assert(defect.CanFix && defect.Message.Contains("trang đầu") && defect.Message.Contains("căn giữa") && defect.Message.Contains("Phông/cỡ/kiểu"),
            "Known first-page, alignment and font defects share one reviewed PAGE approval");
        foreach(var profile in new[]{DocumentProfile.Party,DocumentProfile.Personal})
            assert(PageNumberRules.Check(Package(missing),2,0,"N",profile).Count==0,"State PAGE rules do not extend into Party/personal modes");
        foreach(var mutate in new Action<Dictionary<string,XDocument>>[]
        {
            p=>p["word/header1.xml"].Descendants(W+"fldSimple").Single().AddBeforeSelf(new XElement(W+"r",new XElement(W+"t","Nội dung header riêng"))),
            p=>p["word/header1.xml"].Descendants(W+"pPr").Single().Add(new XElement(W+"pBdr")),
            p=>p["word/header1.xml"].Descendants(W+"fldSimple").Single().SetAttributeValue(W+"fldLock",1),
            p=>p["word/header1.xml"].Descendants(W+"sz").Remove(),
            p=>p["word/document.xml"].Descendants(W+"pgMar").Single().SetAttributeValue(W+"gutter",100),
            p=>p.Add("word/settings.xml",new(new XElement(W+"settings",new XElement(W+"evenAndOddHeaders")))),
            p=>p["word/document.xml"].Descendants(W+"pgSz").Single().SetAttributeValue(W+"orient","landscape")
        })
        {
            var parts=Parts(header:true);mutate(parts);var found=Check(parts);
            assert(found.Count==1 && !found[0].CanFix,"Custom, locked, unknown-font or special-layout headers keep real defects manual without destructive replacement");
        }
        var custom=Parts();custom.Add("word/header1.xml",new(new XElement(W+"hdr",new XElement(W+"p",new XElement(W+"r",new XElement(W+"t","Tên cơ quan ở header"))))));
        custom["word/_rels/document.xml.rels"].Root!.Add(new XElement(Rel+"Relationship",new XAttribute("Id","rIdHeader"),new XAttribute("Type",R.NamespaceName+"/header"),new XAttribute("Target","header1.xml")));
        custom["word/document.xml"].Descendants(W+"sectPr").Single().AddFirst(new XElement(W+"headerReference",new XAttribute(W+"type","default"),new XAttribute(R+"id","rIdHeader")));
        assert(Check(custom).Count==0,"Unrecognized custom header text is not labeled a missing PAGE merely because it uses another layout");
        var roman=Parts(header:true,correct:true);roman["word/document.xml"].Descendants(W+"pgNumType").Single().SetAttributeValue(W+"fmt","lowerRoman");
        roman["word/header1.xml"].Descendants(W+"t").Single().Value="ii";
        assert(Check(roman).Single() is {CanFix:true,NumberFix:not null},"A recognized PAGE field with a Roman cached result can be normalized to Arabic numbering");
        var footerNumber=Parts();footerNumber["word/footer1.xml"].Root!.ReplaceNodes(new XElement(W+"p",new XElement(W+"fldSimple",new XAttribute(W+"instr"," PAGE "),new XElement(W+"r",new XElement(W+"t","2")))));
        assert(Check(footerNumber).Single() is {CanFix:false,NumberFix:null},"A PAGE field in the footer is a genuine position finding but is not destructively moved by this scope");
        var baseStyle=Parts(header:true,correct:true);var ppr=baseStyle["word/header1.xml"].Descendants(W+"pPr").Single();ppr.Elements(W+"jc").Remove();ppr.AddFirst(new XElement(W+"pStyle",new XAttribute(W+"val","Header")));
        baseStyle.Add("word/styles.xml",new(new XElement(W+"styles",new XElement(W+"style",new XAttribute(W+"type","paragraph"),new XAttribute(W+"styleId","Header"),new XElement(W+"pPr",new XElement(W+"jc",new XAttribute(W+"val","center")))))));
        assert(Check(baseStyle).Count==0,"Inherited centered paragraph style is resolved rather than falsely flagged as missing direct alignment");
        string folder=Path.Combine(Path.GetTempPath(),"ChinhTa-number-"+Guid.NewGuid());Directory.CreateDirectory(folder);
        try
        {
            foreach(bool existing in new[]{false,true})
            foreach(float size in new[]{10f,13f,14f,16f})
            {
                var parts=existing?Parts(header:true,size:size):Parts();var old=Package(parts);var choice=Check(parts).Single();
                string source=Path.Combine(folder,Guid.NewGuid()+".docx"),candidate=Path.Combine(folder,Guid.NewGuid()+".docx");Write(source,parts);byte[] original=File.ReadAllBytes(source);
                DocxPageNumberEditor.Write(source,candidate,choice.NumberFix!,2);DocxPageNumberEditor.Verify(source,candidate,choice.NumberFix!,2);
                var after=PageNumberPackage.FromFile(candidate);
                assert(PageNumberRules.ProvenStandard(after,2) && PageNumberRules.Check(after,2,0,"N",DocumentProfile.State).Count==0,
                    "Creation/repair yields a real PAGE field, proper presentation and no stale warning on rescan");
                DocxPageNumberEditor.VerifyLive(old,after);
                assert(original.SequenceEqual(File.ReadAllBytes(source)),"PAGE candidate editing never overwrites the original source");
                using(var z=ZipFile.OpenRead(candidate))
                using(var input=z.GetEntry("word/header1.xml")!.Open())
                {
                    var hdr=XDocument.Load(input);float expected=existing && size is >=13 and <=14?size:13;
                    assert(hdr.Descendants(W+"sz").All(e=>(int?)e.Attribute(W+"val")==expected*2),"PAGE keeps valid 13/14 sizes and normalizes only invalid sizes");
                    if(existing)assert(hdr.Descendants(W+"b").Any() && hdr.Descendants(W+"u").Any(),"Unconstrained bold and underline survive PAGE font normalization");
                }
                bool stale=false;try{DocxPageNumberEditor.Write(source,Path.Combine(folder,Guid.NewGuid()+".docx"),new(new string('a',64)),2);}catch(InvalidOperationException){stale=true;}
                assert(stale,"A stale or forged PAGE configuration key cannot create a candidate");
            }
            var parts0=Parts();string a=Path.Combine(folder,"a.docx"),b=Path.Combine(folder,"b.docx");Write(a,parts0);
            var plan=Check(parts0).Single().NumberFix!;DocxPageNumberEditor.Write(a,b,plan,2);
            using(var z=ZipFile.Open(b,ZipArchiveMode.Update)){z.GetEntry("customXml/untouched.txt")!.Delete();using var output=new StreamWriter(z.CreateEntry("customXml/untouched.txt").Open());output.Write("changed");}
            bool tamper=false;try{DocxPageNumberEditor.Verify(a,b,plan,2);}catch(InvalidOperationException){tamper=true;}
            assert(tamper,"PAGE verification detects tampering with an unrelated non-XML ZIP part");
        }
        finally{Directory.Delete(folder,true);}
        var fresh=Parts();var selected=Check(fresh).Single();var font=new Issue(0,2,"Nộ","Nộ","",false,FixKind.DocumentHeadingFormat,CharacterFormat:new(13,false,true));
        assert(PageLayoutBatch.Prepare([selected,font]).Count==2 && RepairDependencies.Expand([selected,font],[selected,font]).Count==2,
            "PAGE can be selected together with overlapping body/header-text formatting through its dedicated route");
        bool ordinary=false;try{AtomicTextBatch.ExpectedStory([selected],"N",_=>"");}catch(InvalidOperationException){ordinary=true;}
        assert(ordinary,"A PAGE payload cannot reach the ordinary main-story text writer");
        assert(PageNumberRules.Satisfied(Package(fresh),1,selected.NumberFix!),"If selected body repairs reduce the document to one page, missing numbering is positively satisfied without adding a header");
    }
}
