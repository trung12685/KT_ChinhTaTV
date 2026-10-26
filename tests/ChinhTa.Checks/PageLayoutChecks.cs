using System.IO.Compression;
using System.Xml.Linq;
using ChinhTa.Core;

public static class PageLayoutChecks
{
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    public static SectionLayout Bad=>new(1,0,"N",215.9,279.4,10,22,20,15,RepairEligible:true);
    public static XDocument Document(SectionLayout layout)
    {
        int Twip(double n)=>(int)Math.Round(n*1440/25.4);
        XElement P(string s)=>new(W+"p",new XElement(W+"r",new XElement(W+"rPr",new XElement(W+"b")),new XElement(W+"t",s)));
        return new(new XElement(W+"document",new XElement(W+"body",P("Nội dung  cần giữ nguyên."),
            new XElement(W+"tbl",new XElement(W+"tblPr",new XElement(W+"tblW",new XAttribute(W+"type","dxa"),new XAttribute(W+"w",8000))),
                new XElement(W+"tblGrid",new XElement(W+"gridCol",new XAttribute(W+"w",4000)),new XElement(W+"gridCol",new XAttribute(W+"w",4000))),
                new XElement(W+"tr",new XElement(W+"tc",P("Dữ liệu trong ô thứ nhất")),new XElement(W+"tc",P("Dữ liệu trong ô thứ hai")))),
            new XElement(W+"sectPr",new XElement(W+"pgSz",new XAttribute(W+"w",Twip(layout.WidthMm)),new XAttribute(W+"h",Twip(layout.HeightMm))),
                new XElement(W+"pgMar",new XAttribute(W+"top",Twip(layout.TopMm)),new XAttribute(W+"bottom",Twip(layout.BottomMm)),
                    new XAttribute(W+"left",Twip(layout.LeftMm)),new XAttribute(W+"right",Twip(layout.RightMm)),new XAttribute(W+"header",720),new XAttribute(W+"footer",720))))));
    }
    public static void Write(string path,XDocument xml)
    {
        using var zip=ZipFile.Open(path,ZipArchiveMode.Create);
        using(var s=zip.CreateEntry("word/document.xml").Open())xml.Save(s,SaveOptions.DisableFormatting);
        using(var s=new StreamWriter(zip.CreateEntry("word/styles.xml").Open()))s.Write("<w:styles xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"/>");
        using(var s=new StreamWriter(zip.CreateEntry("customXml/kept.xml").Open()))s.Write("Dữ liệu khác phải giữ nguyên byte.");
    }
    private static XDocument Read(string path)
    {using var zip=ZipFile.OpenRead(path);using var s=zip.GetEntry("word/document.xml")!.Open();return XDocument.Load(s,LoadOptions.PreserveWhitespace);}
    public static void Run(Action<bool,string> assert)
    {
        var fix=PageLayoutFix.Propose(Bad)!;var issue=PageRules.Check(Bad,DocumentProfile.State).Single();
        assert(fix.Valid && fix.After.WidthMm==210 && fix.After.HeightMm==297 && fix.After.TopMm==20 && fix.After.LeftMm==30 &&
            fix.After.BottomMm==22 && fix.After.RightMm==15,"One page proposal corrects A4 and invalid margins while retaining exact legal margins");
        assert(issue.CanFix && issue.PageFix==fix && IssueReviewCatalog.Describe(issue).Section==IssueReviewSection.RequiredFormat &&
            IssueTooltipCatalog.Describe(issue).Text.Split('\n').Length==3,"A verified page repair is a compact required-format choice with a three-line tooltip");
        foreach(var good in new[]{Bad with {WidthMm=210,HeightMm=297,TopMm=20,BottomMm=25,LeftMm=30,RightMm=20},
            Bad with {WidthMm=210,HeightMm=297,TopMm=25,BottomMm=20,LeftMm=35,RightMm=15}})
            assert(PageLayoutFix.Propose(good) is null && !PageRules.Check(good,DocumentProfile.State).Any(),"Legal page dimensions and both legal margin boundaries generate no repair");
        foreach(var no in new[]{Bad with {RepairEligible=false},Bad with {Number=2},Bad with {MirrorMargins=true},Bad with {WidthMm=297,HeightMm=210},
            Bad with {TopMm=double.NaN},Bad with {HeightMm=double.PositiveInfinity},Bad with {LeftMm=-1},Bad with {Anchor="\r"}})
            assert(PageLayoutFix.Propose(no) is null,"Unknown, special-layout or unbound page evidence never grants an automatic page write");
        foreach(var profile in new[]{DocumentProfile.Party,DocumentProfile.Personal})
            assert(!PageRules.Check(Bad,profile).Any(i=>i.CanFix),"The new State page repair does not extend into Party or personal profiles");
        var doc=Document(Bad);assert(PageLayoutGuard.Eligible(doc,Bad),"A single ordinary portrait section with fitting text tables passes the page guard");
        foreach(var mutation in new Action<XDocument>[]
        {
            x=>x.Descendants(W+"sectPr").Single().Add(new XElement(W+"headerReference")),
            x=>x.Descendants(W+"sectPr").Single().Add(new XElement(W+"footerReference")),
            x=>x.Descendants(W+"sectPr").Single().Add(new XElement(W+"cols",new XAttribute(W+"num",2))),
            x=>x.Descendants(W+"sectPr").Single().Add(new XElement(W+"docGrid",new XAttribute(W+"type","lines"))),
            x=>x.Descendants(W+"pgMar").Single().SetAttributeValue(W+"gutter",100),
            x=>x.Descendants(W+"pgMar").Single().SetAttributeValue(W+"gutter","unknown"),
            x=>x.Descendants(W+"sectPr").Single().Add(new XElement(W+"cols",new XAttribute(W+"num","unknown"))),
            x=>x.Descendants(W+"p").First().AddFirst(new XElement(W+"pPr",new XElement(W+"framePr"))),
            x=>x.Descendants(W+"pgSz").Single().SetAttributeValue(W+"orient","landscape"),
            x=>x.Descendants(W+"gridCol").First().SetAttributeValue(W+"w",15000),
            x=>x.Descendants(W+"r").First().Add(new XElement(W+"drawing")),
            x=>x.Descendants(W+"r").First().Add(new XElement(W+"pict")),
            x=>x.Descendants(W+"r").First().Add(new XElement(W+"tab")),
            x=>x.Descendants(W+"r").First().Add(new XElement(W+"fldChar")),
            x=>x.Descendants(W+"body").Single().AddFirst(new XElement(W+"sdt")),
            x=>x.Descendants(W+"tc").First().Add(new XElement(W+"tbl")),
            x=>x.Descendants(W+"body").Single().AddFirst(new XElement(W+"p",new XElement(W+"pPr",new XElement(W+"sectPr"))))
        })
        {
            var copy=new XDocument(doc);mutation(copy);
            assert(!PageLayoutGuard.Eligible(copy,Bad),"Headers, objects, fields, tabs, wide/complex tables and multi-section/special layouts stay outside page repair");
        }
        foreach(string flag in new[]{"mirrorMargins","gutterAtTop","bookFoldPrinting","bookFoldRevPrinting"})
        {
            var xml=new XDocument(new XElement("package",new XElement(doc.Root!),new XElement(W+"settings",new XElement(W+flag))));
            assert(!PageLayoutGuard.Eligible(xml,Bad),"Active print/booklet settings block automatic page repair");
            xml.Descendants(W+flag).Single().SetAttributeValue(W+"val",0);
            assert(PageLayoutGuard.Eligible(xml,Bad),"An explicitly disabled print flag is not mistaken for an active exception");
        }
        string folder=Path.Combine(Path.GetTempPath(),"ChinhTa-page-"+Guid.NewGuid());Directory.CreateDirectory(folder);
        try
        {
            string source=Path.Combine(folder,"source.docx"),candidate=Path.Combine(folder,"candidate.docx");Write(source,doc);byte[] original=File.ReadAllBytes(source);
            DocxPageLayoutEditor.Write(source,candidate,fix);DocxPageLayoutEditor.Verify(source,candidate,fix);
            var after=Read(candidate);var layout=PageLayoutGuard.Read(after,fix.After)!;
            assert(PageLayoutFix.Matches(fix.After,layout) && original.SequenceEqual(File.ReadAllBytes(source)),"The candidate has approved dimensions/margins and leaves the source package byte-identical");
            DocxPageLayoutEditor.VerifyBody(doc.ToString(),after.ToString(),fix);
            assert(after.Descendants(W+"t").Select(e=>e.Value).SequenceEqual(doc.Descendants(W+"t").Select(e=>e.Value)) &&
                after.Descendants(W+"tbl").Count()==1,"Page-only editing retains all text, table cells and font content");
            foreach(var mutate in new Action<XDocument>[]
            {
                x=>x.Descendants(W+"t").Last().Value="Mất dữ liệu ô",
                x=>x.Descendants(W+"b").First().Remove(),
                x=>x.Descendants(W+"gridCol").First().SetAttributeValue(W+"w",3999),
                x=>x.Descendants(W+"pgMar").Single().SetAttributeValue(W+"bottom",1000)
            })
            {
                var changed=new XDocument(after);mutate(changed);bool rejected=false;
                try{DocxPageLayoutEditor.VerifyBody(doc.ToString(),changed.ToString(),fix);}catch(InvalidOperationException){rejected=true;}
                assert(rejected,"Live-body verification detects unapproved text, font, table geometry or legal-margin drift");
            }
            string tampered=Path.Combine(folder,"tampered.docx");File.Copy(candidate,tampered);
            using(var z=ZipFile.Open(tampered,ZipArchiveMode.Update)){z.GetEntry("customXml/kept.xml")!.Delete();using var s=new StreamWriter(z.CreateEntry("customXml/kept.xml").Open());s.Write("changed");}
            bool badPart=false;try{DocxPageLayoutEditor.Verify(source,tampered,fix);}catch(InvalidOperationException){badPart=true;}
            assert(badPart,"Candidate verification detects changes to any unrelated ZIP part");
            foreach(string target in new[]{source,candidate})
            {
                bool rejected=false;try{DocxPageLayoutEditor.Write(source,target,fix);}catch(InvalidOperationException){rejected=true;}
                assert(rejected && original.SequenceEqual(File.ReadAllBytes(source)),"The page writer never overwrites its source or a pre-existing candidate");
            }
            bool stale=false;try{DocxPageLayoutEditor.Write(source,Path.Combine(folder,"stale.docx"),fix with {Before=Bad with {TopMm=12}});}catch(InvalidOperationException){stale=true;}
            assert(stale && !File.Exists(Path.Combine(folder,"stale.docx")),"Stale source dimensions fail before candidate creation");
        }
        finally{Directory.Delete(folder,true);}
        var font=new Issue(0,4,"Nội ","Nội ","",false,FixKind.DocumentHeadingFormat,CharacterFormat:new(13,false,true));
        var line=new Issue(200,4,"Mẫu.","Mẫu.","",false,FixKind.ShapeLine,LineFix:new(null,100,100,80,LineRole.Subject));
        var selected=PageLayoutBatch.Prepare([issue,font,line]);
        assert(selected.Count==3 && RepairDependencies.Expand(selected,selected).Count==3,"Page, overlapping text format and Line can be selected in one approval through the dedicated page-first route");
        var shifted=font with {Start=7};var measured=line with {Start=207,LineFix=line.LineFix! with {Left=110,Top=120}};
        var rebound=PageLayoutBatch.Rebind([new(font,""),new(line,"prefix")],[shifted,measured],i=>i==shifted?"":"prefix");
        assert(rebound.Contains(shifted) && rebound.Contains(measured) && !rebound.Contains(line),"After reopening, selection binds by exact text/prefix and uses fresh CPs and Line measurements");
        bool ambiguous=false;try{PageLayoutBatch.Rebind([new(font,"")],[shifted,shifted with {Start=9}],_=>"");}catch(InvalidOperationException){ambiguous=true;}
        assert(ambiguous,"Duplicate matching paragraph evidence cannot silently rebind a selected formatting repair");
        foreach(int failAt in Enumerable.Range(0,5))
        {
            int pageWrites=0,otherWrites=0,commits=0,restores=0;bool failed=false;
            void Fail(int step){if(step==failAt)throw new IOException("synthetic page fault");}
            try{PageLayoutBatch.Apply(()=>{pageWrites++;Fail(1);},()=>{otherWrites++;Fail(2);},()=>Fail(3),()=>{Fail(4);commits++;},()=>{restores++;pageWrites=otherWrites=0;});}
            catch(IOException){failed=true;}
            assert(failAt==0?!failed && commits==1 && restores==0:failed && commits==0 && restores==1 && pageWrites==0 && otherWrites==0,
                "Page and later selected groups publish one completion only after verification; any phase failure restores the whole selection");
        }
        bool forbidden=false;try{AtomicTextBatch.ExpectedStory([issue],"N",_=>"");}catch(InvalidOperationException){forbidden=true;}
        assert(forbidden,"A page payload cannot enter the ordinary body-text writer");
    }
}
