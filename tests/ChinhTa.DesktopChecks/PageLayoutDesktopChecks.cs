using System.IO;
using System.IO.Compression;
using System.Xml.Linq;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class PageLayoutDesktopChecks
{
    public static void Run(Action<bool,string> assert)
    {
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main",pkg="http://schemas.microsoft.com/office/2006/xmlPackage";
        var fullBody=OrdinaryDocument();
        var full=new XDocument(new XElement(pkg+"package",
            new XElement(pkg+"part",new XAttribute(pkg+"name","/word/document.xml"),new XElement(pkg+"xmlData",fullBody.Root)),
            new XElement(pkg+"part",new XAttribute(pkg+"name","/word/settings.xml"),new XElement(pkg+"xmlData",new XElement(w+"settings",new XElement(w+"bookFoldPrinting"))))));
        var probe=new PageNumberDesktopChecks.DocumentProbe(full.ToString());
        string currentXml=PageNumberDocumentReader.ReadXml(probe);var observedLayout=PageLayoutGuard.Read(XDocument.Parse(currentXml),new(1,0,"N",0,0,0,0,0,0))!;
        assert(probe.Reads==1 && probe.RangeReads==0 && !PageLayoutGuard.Eligible(XDocument.Parse(currentXml),observedLayout),
            "Page eligibility reads full current Word settings, so booklet mode omitted from body-range XML cannot receive an automatic page approval");
        var before=new SectionLayout(1,0,"C",215.9,279.4,10,22.225,20,15,RepairEligible:true);
        var page=PageRules.Check(before,DocumentProfile.State).Single();
        var font=new Issue(0,4,"CƠ Q","CƠ Q","",false,FixKind.DocumentHeadingFormat,CharacterFormat:new(13,false,true));
        var manual=page with {PageFix=null,CanFix=false,Start=900};
        var model=new IssueReviewModel([page,font,manual]);model.All.IsChecked=true;
        var chosen=model.Choices.Where(c=>c.IsChecked).Select(c=>c.Issue).ToArray();
        assert(chosen.Length==2 && PageLayoutBatch.Prepare(chosen).Count==2 && !model.Choices.Single(c=>c.Issue==manual).IsChecked,
            "Select all includes page setup plus overlapping header formatting while leaving special-layout warnings unselected");
        assert(model.Choices.Single(c=>c.Issue==page).Info.Section==IssueReviewSection.RequiredFormat &&
            model.Choices.Single(c=>c.Issue==page).Tooltip.Contains("I.1–3") && model.Choices.Single(c=>c.Issue==page).Tooltip.Split('\n').Length==3,
            "The new page approval is a required-format group with concise legal basis and target measurements");
        var refreshed=new IssueReviewModel([font]);
        assert(!refreshed.Choices.Any(c=>c.Issue.Kind==FixKind.PageLayout),"A successful page rescan drops the repaired page row entirely");
        string folder=Path.Combine(Path.GetTempPath(),"ChinhTa-page-fixture-"+Guid.NewGuid());Directory.CreateDirectory(folder);
        try
        {
            string source=OfficePageLayoutCheck.Create(Path.Combine("samples","BaDuongKeThieu.docx"),folder),candidate=Path.Combine(folder,"page-candidate.docx");
            using(var zip=ZipFile.OpenRead(source))
            using(var stream=zip.GetEntry("word/document.xml")!.Open())
            {
                var xml=XDocument.Load(stream);var observed=PageLayoutGuard.Read(xml,before)!;
                assert(PageLayoutGuard.Eligible(xml,observed),"The actual Office fixture has a fitting table and qualifies for single-section page repair");
                var fix=PageLayoutFix.Propose(observed with {RepairEligible=true})!;
                DocxPageLayoutEditor.Write(source,candidate,fix);DocxPageLayoutEditor.Verify(source,candidate,fix);
            }
            using(var package=DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(candidate,false))
            {
                var section=package.MainDocumentPart!.Document.Body!.Elements<DocumentFormat.OpenXml.Wordprocessing.SectionProperties>().Single();
                var errors=new DocumentFormat.OpenXml.Validation.OpenXmlValidator().Validate(section).ToArray();
                assert(errors.Length==0,"The corrected page section satisfies the OpenXML schema: "+string.Join("; ",errors.Select(e=>e.Description)));
            }
            string mixedFolder=Path.Combine(folder,"mixed");Directory.CreateDirectory(mixedFolder);
            string mixed=OfficeFormatBatchCheck.Create(Path.Combine("samples","BaDuongKeThieu.docx"),mixedFolder);
            using var mixedZip=ZipFile.OpenRead(mixed);using var mixedStream=mixedZip.GetEntry("word/document.xml")!.Open();var mixedXml=XDocument.Load(mixedStream);
            var mixedLayout=PageLayoutGuard.Read(mixedXml,before)!;
            var number=PageNumberRules.Check(PageNumberPackage.FromFile(mixed),2,0,"C",DocumentProfile.State).Single();
            assert(PageLayoutGuard.Eligible(mixedXml,mixedLayout) && number.CanFix,
                "The combined Office fixture qualifies for both page layout and PAGE before any write, retaining its original table and text");
            string paged=Path.Combine(mixedFolder,"paged.docx"),numbered=Path.Combine(mixedFolder,"numbered.docx");
            DocxPageLayoutEditor.Write(mixed,paged,PageLayoutFix.Propose(mixedLayout with {RepairEligible=true})!);
            DocxPageNumberEditor.Write(paged,numbered,number.NumberFix!,2);
            DocxPageNumberEditor.VerifyLive(PageNumberPackage.FromFile(paged),PageNumberPackage.FromFile(numbered));
            assert(PageNumberRules.ProvenStandard(PageNumberPackage.FromFile(numbered),2),
                "Layout then PAGE candidate editing preserves its approval binding and all body/table content across the actual combined fixture");
        }
        finally{Directory.Delete(folder,true);}
    }
    private static XDocument OrdinaryDocument()
    {
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        return new(new XElement(w+"document",new XElement(w+"body",new XElement(w+"p",new XElement(w+"r",new XElement(w+"t","Nội dung"))),
            new XElement(w+"sectPr",new XElement(w+"pgSz",new XAttribute(w+"w",12240),new XAttribute(w+"h",15840)),
                new XElement(w+"pgMar",new XAttribute(w+"top",567),new XAttribute(w+"bottom",1260),new XAttribute(w+"left",1134),new XAttribute(w+"right",850))))));
    }
}
