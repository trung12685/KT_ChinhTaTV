using System.IO.Compression;
using System.Xml.Linq;
using ChinhTa.Core;

public static class FooterPageMigrationChecks
{
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main",R="http://schemas.openxmlformats.org/officeDocument/2006/relationships",
        Rel="http://schemas.openxmlformats.org/package/2006/relationships",Ct="http://schemas.openxmlformats.org/package/2006/content-types";
    public static Dictionary<string,XDocument> Parts(bool complex=false)
    {
        var parts=PageNumberChecks.Parts(header:true,correct:true);var section=parts["word/document.xml"].Descendants(W+"sectPr").Single();
        section.Elements(W+"headerReference").Remove();section.Elements(W+"titlePg").Remove();
        var p=new XElement(parts["word/header1.xml"].Root!.Element(W+"p")!);
        if(complex)
        {
            var simple=p.Element(W+"fldSimple")!;var result=new XElement(simple.Element(W+"r")!);simple.Remove();
            p.Add(new XElement(W+"r",new XElement(W+"fldChar",new XAttribute(W+"fldCharType","begin"))),
                new XElement(W+"r",new XElement(W+"instrText"," PAGE \\* MERGEFORMAT ")),
                new XElement(W+"r",new XElement(W+"fldChar",new XAttribute(W+"fldCharType","separate"))),result,
                new XElement(W+"r",new XElement(W+"fldChar",new XAttribute(W+"fldCharType","end"))));
        }
        p.Descendants(W+"u").Remove();
        parts["word/footer1.xml"].Root!.ReplaceNodes(p);return parts;
    }
    public static void AddFirst(Dictionary<string,XDocument> parts,bool page,bool different,bool shared=false)
    {
        var section=parts["word/document.xml"].Descendants(W+"sectPr").Single();string id="rIdFooter";
        if(!shared)
        {
            id="rIdFirstFooter";
            parts.Add("word/footer2.xml",page?new XDocument(parts["word/footer1.xml"]):new(new XElement(W+"ftr",new XElement(W+"p",new XElement(W+"r",new XElement(W+"t","First footer kept"))))));
            parts["word/_rels/document.xml.rels"].Root!.Add(new XElement(Rel+"Relationship",new XAttribute("Id",id),new XAttribute("Type",R.NamespaceName+"/footer"),new XAttribute("Target","footer2.xml")));
            parts["[Content_Types].xml"].Root!.Add(new XElement(Ct+"Override",new XAttribute("PartName","/word/footer2.xml"),new XAttribute("ContentType","application/vnd.openxmlformats-officedocument.wordprocessingml.footer+xml")));
        }
        section.AddFirst(new XElement(W+"footerReference",new XAttribute(W+"type","first"),new XAttribute(R+"id",id)));
        if(different)section.Add(new XElement(W+"titlePg"));
    }
    public static void Run(Action<bool,string> assert)
    {
        IReadOnlyList<Issue> Check(Dictionary<string,XDocument> parts,int pages=2)=>PageNumberRules.Check(PageNumberChecks.Package(parts),pages,0,"N",DocumentProfile.State);
        foreach(bool complex in new[]{false,true})
        {
            var parts=Parts(complex);var issue=Check(parts).Single();var migration=issue.NumberFix?.FooterMigration;
            assert(issue.CanFix && migration is {Valid:true,Pages:2,DifferentFirst:false,Default.Size:14,First:null},
                "A proven simple or complex PAGE-only footer receives a bound concrete migration approval instead of a blanket manual warning");
            assert(migration!.Source(1) is not null && migration.Source(2) is not null,"An ordinary default footer is measured on both physical pages before enabling hidden-first numbering");
        }
        var boldParts=Parts();var boldFix=Check(boldParts).Single().NumberFix!;
        assert(boldFix.FooterMigration?.Default?.Bold==true,"Footer PAGE approval binds effective source bold formatting as well as font, size, italic and color");
        boldParts["word/footer1.xml"].Descendants(W+"b").Single().SetAttributeValue(W+"val",0);
        assert(Check(boldParts).Single().NumberFix?.FooterMigration?.Default?.Bold==false && Check(boldParts).Single().NumberFix!.SourceKey!=boldFix.SourceKey,
            "An explicit normal-weight source result changes both the measured style and immutable approval key");
        var styled=Parts();var styledRun=styled["word/footer1.xml"].Descendants(W+"rPr").Single();styledRun.Element(W+"b")!.Remove();
        styledRun.AddFirst(new XElement(W+"rStyle",new XAttribute(W+"val","FooterBold")));
        styled.Add("word/styles.xml",new XDocument(new XElement(W+"styles",new XElement(W+"style",new XAttribute(W+"type","character"),new XAttribute(W+"styleId","FooterBold"),new XElement(W+"rPr",new XElement(W+"b"))))));
        assert(Check(styled).Single().NumberFix?.FooterMigration?.Default?.Bold==true,"A single proven character-style bold source is included in rendered footer binding");
        styled["word/styles.xml"].Root!.Elements(W+"style").Single().AddFirst(new XElement(W+"basedOn",new XAttribute(W+"val","BaseBold")));
        styled["word/styles.xml"].Root!.Add(new XElement(W+"style",new XAttribute(W+"type","character"),new XAttribute(W+"styleId","BaseBold"),new XElement(W+"rPr",new XElement(W+"b"))));
        assert(!Check(styled).Single().CanFix,"Multiple inherited bold toggles without a direct override remain manual instead of guessing the source weight");
        styledRun.Add(new XElement(W+"b",new XAttribute(W+"val",0)));
        assert(Check(styled).Single().NumberFix?.FooterMigration?.Default?.Bold==false,"An explicit direct normal-weight override resolves inherited bold toggles for footer binding");
        foreach(var mutate in new Action<Dictionary<string,XDocument>>[]
        {
            p=>p["word/footer1.xml"].Root!.Add(new XElement(W+"p")),
            p=>p["word/footer1.xml"].Descendants(W+"fldSimple").Single().AddBeforeSelf(new XElement(W+"r",new XElement(W+"t","Trang "))),
            p=>p["word/footer1.xml"].Descendants(W+"fldSimple").Single().SetAttributeValue(W+"fldLock",1),
            p=>p["word/footer1.xml"].Descendants(W+"rFonts").Remove(),
            p=>p["word/footer1.xml"].Descendants(W+"b").Single().SetAttributeValue(W+"val","unknown"),
            p=>p["word/footer1.xml"].Descendants(W+"rPr").Single().Add(new XElement(W+"u",new XAttribute(W+"val","single"))),
            p=>p["word/footer1.xml"].Descendants(W+"rPr").Single().Add(new XElement(W+"shd",new XAttribute(W+"fill","FF0000"))),
            p=>p["word/document.xml"].Descendants(W+"pgNumType").Single().SetAttributeValue(W+"start",3),
            p=>p["word/document.xml"].Descendants(W+"pgNumType").Single().SetAttributeValue(W+"fmt","lowerRoman"),
            p=>p["word/footer1.xml"].Descendants(W+"fldSimple").Single().SetAttributeValue(W+"instr"," PAGE \\* ROMAN "),
            p=>p["word/footer1.xml"].Descendants(W+"fldSimple").Single().SetAttributeValue(W+"instr"," PAGE NUMPAGES "),
            p=>p["word/footer1.xml"].Descendants(W+"p").Single().Add(new XElement(W+"r",new XElement(W+"tab")))
        })
        {
            var parts=Parts();mutate(parts);assert(Check(parts).Single() is {CanFix:false,NumberFix:null},
                "Mixed, locked, unknown-font, non-Arabic/restarted or structurally different footer fields retain a manual finding");
        }
        foreach(int pages in new[]{9,10,99,100,999,1000,10000})
            assert(Check(Parts(),pages).Single() is {CanFix:true,NumberFix.FooterMigration.Valid:true},"PAGE-only footer supports bound multi-digit document page counts");
        assert(Check(Parts(),10001).All(i=>!i.CanFix),"Out-of-bound page counts remain manual");
        var hiddenCustom=Parts();AddFirst(hiddenCustom,false,false);
        assert(!Check(hiddenCustom).Single().CanFix,"Migration cannot expose a previously inactive custom first footer");
        var defaultOnly=Parts();AddFirst(defaultOnly,false,true);
        assert(Check(defaultOnly).Single().NumberFix?.FooterMigration is {Default:not null,First:null,DifferentFirst:true},
            "A visible custom first footer remains independent while later PAGE-only default footers can migrate");
        string folder=Path.Combine(Path.GetTempPath(),"ChinhTa-footer-migrate-"+Guid.NewGuid());Directory.CreateDirectory(folder);
        try
        {
            foreach(int mode in Enumerable.Range(0,5))
            {
                var parts=Parts(mode==1);
                if(mode==2)AddFirst(parts,true,true);
                if(mode==3)AddFirst(parts,false,true);
                if(mode==4)AddFirst(parts,true,false,true);
                var issue=Check(parts).Single();var fix=issue.NumberFix!;
                string source=Path.Combine(folder,"source-"+mode+".docx"),candidate=Path.Combine(folder,"candidate-"+mode+".docx");
                PageNumberChecks.Write(source,parts);byte[] sourceBytes=File.ReadAllBytes(source);
                DocxPageNumberEditor.Write(source,candidate,fix,2);DocxPageNumberEditor.Verify(source,candidate,fix,2);
                var after=PageNumberPackage.FromFile(candidate);DocxPageNumberEditor.VerifyLive(PageNumberChecks.Package(parts),after,fix,2);
                assert(PageNumberRules.ProvenStandard(after,2) && sourceBytes.SequenceEqual(File.ReadAllBytes(source)),
                    "Migration produces a standard header while preserving the source and explicit first/default footer roles");
                using var old=ZipFile.OpenRead(source);using var updated=ZipFile.OpenRead(candidate);
                foreach(string name in parts.Keys.Where(n=>n.Contains("footer",StringComparison.Ordinal)))
                {
                    using var a=old.GetEntry(name)!.Open();using var b=updated.GetEntry(name)!.Open();using var x=new MemoryStream();using var y=new MemoryStream();a.CopyTo(x);b.CopyTo(y);
                    assert(x.ToArray().SequenceEqual(y.ToArray()),"Every source footer part remains byte-for-byte in the candidate even after its PAGE reference is detached");
                }
                XDocument Read(string name){using var input=updated.GetEntry(name)!.Open();return XDocument.Load(input);}
                var document=Read("word/document.xml");
                assert(document.Descendants(W+"footerReference").Count()==(mode==3?1:0),"Only the approved PAGE-only footer references are removed; custom first footers remain referenced");
                var header=Read("word/header1.xml");
                assert(header.Descendants(W+"sz").Any(e=>(string?)e.Attribute(W+"val")=="28") && header.Descendants(W+"b").Any(),
                    "Creating the header from a footer PAGE retains legal14pt and bold source formatting");
            }
            // Verify the live guard independently from the byte-exact candidate
            // verifier: Word may rewrite packages during open/save.
            var liveParts=Parts();var liveFix=Check(liveParts).Single().NumberFix!;
            string liveSource=Path.Combine(folder,"live-source.docx"),liveCandidate=Path.Combine(folder,"live-candidate.docx");
            PageNumberChecks.Write(liveSource,liveParts);DocxPageNumberEditor.Write(liveSource,liveCandidate,liveFix,2);
            var liveAfter=new Dictionary<string,XDocument>();
            using(var zip=ZipFile.OpenRead(liveCandidate))
            foreach(var entry in zip.Entries.Where(e=>e.FullName.EndsWith(".xml",StringComparison.Ordinal) && !e.FullName.StartsWith("customXml/",StringComparison.Ordinal) || e.FullName.EndsWith(".rels",StringComparison.Ordinal)))
            {using var input=entry.Open();liveAfter.Add(entry.FullName,XDocument.Load(input));}
            foreach(var mutate in new Action<Dictionary<string,XDocument>>[]
            {
                p=>p["word/document.xml"].Descendants(W+"sectPr").Single().AddFirst(new XElement(W+"footerReference",new XAttribute(W+"type","default"),new XAttribute(R+"id","rIdFooter"))),
                p=>p.Remove("word/footer1.xml"),
                p=>p["word/footer1.xml"].Descendants(W+"t").Single().Value="999",
                p=>p["word/_rels/document.xml.rels"].Root!.Elements(Rel+"Relationship").Where(e=>(string?)e.Attribute("Id")=="rIdFooter").Remove(),
                p=>p["word/_rels/document.xml.rels"].Root!.Elements(Rel+"Relationship").Single(e=>(string?)e.Attribute("Id")=="rIdFooter").SetAttributeValue("Target","different.xml"),
                p=>p["word/document.xml"].Descendants(W+"sectPr").Single().AddFirst(new XElement(W+"footerReference",new XAttribute(W+"type","first"),new XAttribute(R+"id","rIdFooter"))),
                p=>p["word/document.xml"].Descendants(W+"body").Single().AddFirst(new XElement(W+"p",new XElement(W+"r",new XElement(W+"t","Unexpected body edit"))))
            })
            {
                var tampered=liveAfter.ToDictionary(p=>p.Key,p=>new XDocument(p.Value));mutate(tampered);bool denied=false;
                try{DocxPageNumberEditor.VerifyLive(PageNumberChecks.Package(liveParts),PageNumberChecks.Package(tampered),liveFix,2);}catch(InvalidOperationException){denied=true;}
                assert(denied,"Live migration rejects reattached references, orphan-part/relation loss, footer/body mutation or unapproved new footer roles");
            }
            var stale=Parts();var accepted=Check(stale).Single().NumberFix!;stale["word/footer1.xml"].Descendants(W+"sz").Single().SetAttributeValue(W+"val",26);
            string changed=Path.Combine(folder,"stale.docx"),rejected=Path.Combine(folder,"rejected.docx");PageNumberChecks.Write(changed,stale);bool blocked=false;
            try{DocxPageNumberEditor.Write(changed,rejected,accepted,2);}catch(InvalidOperationException){blocked=true;}
            assert(blocked && !File.Exists(rejected),"A changed footer style invalidates the source key before a migration candidate is published");
            var cached=Parts();var key=Check(cached).Single().NumberFix!.SourceKey;cached["word/footer1.xml"].Descendants(W+"t").Single().Value="1";
            assert(Check(cached).Single().NumberFix!.SourceKey==key,"Ordinary cached PAGE updates do not change an otherwise identical approved footer configuration");
        }
        finally{Directory.Delete(folder,true);}
    }
}
