using System.Xml.Linq;
using ChinhTa.Core;

public static class PageCoverageChecks
{
    public static void Run(Action<bool,string> assert)
    {
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var section=new SectionLayout(1,0,"N",210,297,20,20,30,15);
        PageCoverageEvidence Read(Dictionary<string,XDocument>? parts,int? pages=2,bool complete=true,SectionLayout[]? sections=null)=>
            PageCoverageEvidence.Read(DocumentProfile.State,sections??[section],parts is null?null:PageNumberChecks.Package(parts),complete,pages);
        var ordinary=PageNumberChecks.Parts(footer:false);var good=Read(ordinary,1);
        assert(good.Observations["C32"].State==CoverageState.Observed && good.Observations["C32"].ObservedCount==1,
            "Coverage of an ordinary native/XML-matched section is explicit even when no margin repair was proposed");
        assert(good.Observations["C33"].State==CoverageState.PendingGeometry && good.NumberPreviewLayout is not null,
            "A one-page document with no numbering defect still waits for actual preview evidence instead of a zero-issues PASS");
        var correct=Read(PageNumberChecks.Parts(header:true,correct:true));
        assert(correct.Observations["C32"].State==CoverageState.Partial && correct.Observations["C33"].State==CoverageState.PendingGeometry,
            "A known PAGE header does not grant full page-layout coverage, and correct PAGE metadata still waits for visible numbers");
        var missing=Read(ordinary);
        assert(missing.Observations["C33"].State==CoverageState.Partial && missing.NumberPreviewLayout is null,
            "A known missing-number warning is retained independently of incomplete preview coverage");
        var unavailable=Read(null);
        assert(unavailable.Observations["C33"].State==CoverageState.Unavailable && unavailable.Observations["C32"].State==CoverageState.Partial,
            "Native margin values without the current package cannot stand for complete page or PAGE coverage");
        foreach(int? pages in new int?[]{null,0,10001})
            assert(Read(ordinary,pages).Observations["C33"].State==CoverageState.Unavailable,
                "Missing or impossible current page counts cannot certify numbering coverage");
        foreach(string flag in new[]{"bookFoldPrinting","mirrorMargins"})
        {
            var parts=PageNumberChecks.Parts(footer:false);parts["word/settings.xml"]=new(new XElement(w+"settings",new XElement(w+flag)));
            assert(Read(parts,1).Observations["C32"].State==CoverageState.Partial,
                "Page coverage reads settings parts as well as the main document, retaining book-fold and mirror exclusions");
        }
        var multi=Read(ordinary,sections:[section,section with{Number=2}]);
        assert(multi.Observations["C32"].State==CoverageState.Partial && multi.Observations["C32"].RecognizedCount==2,
            "Two native sections remain explicit partial coverage instead of being collapsed to a single ordinary layout");
        var incomplete=Read(PageNumberChecks.Parts(header:true,correct:true),complete:false);
        assert(incomplete.Observations.Values.All(o=>o.State!=CoverageState.Observed) && incomplete.NumberPreviewLayout is null,
            "A partial text/package proof never provides a complete page observation or preview-verification candidate");
        var logo=PageNumberChecks.Parts(header:true,correct:true);logo["word/header1.xml"].Root!.Add(new XElement(w+"p",new XElement(w+"r",new XElement(w+"drawing"))));
        assert(Read(logo).Observations["C33"].State==CoverageState.Partial && Read(logo).NumberPreviewLayout is null,
            "An unsupported mixed header is visible as partial coverage even when numbering rules emit no concrete repair");
        var unknown=Read(ordinary,sections:[section with{WidthMm=double.NaN}]);
        assert(unknown.Observations["C32"].State==CoverageState.Unavailable && unknown.Observations["C32"].ObservedCount==0,
            "Invalid native geometry cannot become observed simply because XML has plausible page dimensions");
        foreach(var profile in new[]{DocumentProfile.Party,DocumentProfile.Personal})
            assert(PageCoverageEvidence.Read(profile,[section],PageNumberChecks.Package(ordinary),true,1).Observations.Count==0,
                "State repair-family page evidence does not certify another document profile's manual layout policy");
        var ledger=DocumentCoverageBuilder.Build(new(DocumentProfile.State,[],[],true,true,External:correct.Observations));
        assert(ledger.Rows.Single(r=>r.Id=="C33").State==CoverageState.PendingGeometry && ledger.Rows.Single(r=>r.Id=="C32").State==CoverageState.Partial,
            "The final immutable document ledger preserves explicit page and pending preview evidence independently of findings");
    }
}
