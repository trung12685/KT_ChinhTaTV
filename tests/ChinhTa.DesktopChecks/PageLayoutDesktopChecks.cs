using System.IO;
using System.IO.Compression;
using System.Xml.Linq;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class PageLayoutDesktopChecks
{
    public static void Run(Action<bool,string> assert)
    {
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
        }
        finally{Directory.Delete(folder,true);}
    }
}
