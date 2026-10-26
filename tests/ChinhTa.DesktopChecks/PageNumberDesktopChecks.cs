using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml.Linq;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class PageNumberDesktopChecks
{
    public sealed class DocumentProbe(string xml)
    {
        public int Reads {get;private set;}
        public int RangeReads {get;private set;}
        public string WordOpenXML {get{Reads++;return xml;}}
        public object Content {get{RangeReads++;throw new InvalidOperationException("A partial range cannot establish header/footer absence.");}}
    }
    private static FixedDocumentSequence Pages(bool first=false,string number="2",bool shifted=false,bool missingDrawing=false,bool scaled=false)
    {
        var document=new FixedDocument();double width=210*96/25.4,height=297*96/25.4;
        for(int n=0;n<2;n++)
        {
            var page=new FixedPage {Width=width,Height=height};
            Glyphs Glyph(string text,double x,double y)=>new() {FontUri=new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"times.ttf")),
                FontRenderingEmSize=14*96d/72,UnicodeString=text,Fill=Brushes.Black,OriginX=x,OriginY=y};
            if(n==1 || first)
            {
                var glyph=Glyph(n==0?"1":number,0,40);double center=(30*96/25.4+width-15*96/25.4)/2;
                var run=glyph.ToGlyphRun();glyph.OriginX=center-run.ComputeAlignmentBox().Width/2;page.Children.Add(glyph);
            }
            page.Children.Add(Glyph("Nội dung giữ nguyên",100,150+(shifted?5:0)));
            if(!missingDrawing)page.Children.Add(new System.Windows.Shapes.Path {Data=new RectangleGeometry(new Rect(100,200,60,30)),Fill=Brushes.Blue});
            if(scaled)
            {
                const double factor=4d/3;var canvas=new Canvas {RenderTransform=new ScaleTransform(factor,factor)};
                var children=page.Children.Cast<UIElement>().ToArray();page.Children.Clear();
                foreach(var child in children)
                {
                    if(child is Glyphs glyph){glyph.OriginX/=factor;glyph.OriginY/=factor;glyph.FontRenderingEmSize/=factor;}
                    if(child is System.Windows.Shapes.Path path)path.Data.Transform=new ScaleTransform(1/factor,1/factor);
                    canvas.Children.Add(child);
                }
                page.Children.Add(canvas);
            }
            var content=new PageContent();((IAddChild)content).AddChild(page);document.Pages.Add(content);
        }
        var reference=new DocumentReference();reference.SetDocument(document);var sequence=new FixedDocumentSequence();sequence.References.Add(reference);return sequence;
    }
    public static void Run(Action<bool,string> assert)
    {
        var sequence=Pages();PageNumberPreviewVerifier.VerifyNumbers(sequence,30,15,20);
        PageNumberPreviewVerifier.VerifyNumbers(Pages(scaled:true),30,15,20);
        assert(true,"PAGE font verification uses effective ancestor scaling, including Word-style point-coordinate canvases");
        var before=PageNumberPreviewVerifier.Capture(sequence,20);PageNumberPreviewVerifier.Preserve(before,PageNumberPreviewVerifier.Capture(Pages(),20));
        assert(before.Pages==2 && before.Body.Count>=4,"Rendered PAGE proof accepts page2 with page1 hidden and retains body text/drawing geometry");
        foreach(var wrong in new[]{Pages(first:true),Pages(number:"3"),Pages(number:"12")})
        {
            bool rejected=false;try{PageNumberPreviewVerifier.VerifyNumbers(wrong,30,15,20);}catch(InvalidOperationException){rejected=true;}
            assert(rejected,"A visible first-page number or wrong actual XPS sequence cannot pass PAGE verification");
        }
        foreach(var changed in new[]{Pages(shifted:true),Pages(missingDrawing:true)})
        {
            bool rejected=false;try{PageNumberPreviewVerifier.Preserve(before,PageNumberPreviewVerifier.Capture(changed,20));}catch(InvalidOperationException){rejected=true;}
            assert(rejected,"PAGE postconditions reject shifted text and missing body drawing data");
        }
        var pageIssue=new Issue(0,1,"C","C","",false,FixKind.PageNumber,Citation:PageNumberRules.Citation,NumberFix:new(new string('a',64)));
        var font=new Issue(0,4,"CƠ Q","CƠ Q","",false,FixKind.DocumentHeadingFormat,CharacterFormat:new(13,false,true));
        var manual=pageIssue with {CanFix=false,NumberFix=null,Start=400};var model=new IssueReviewModel([pageIssue,font,manual]);model.All.IsChecked=true;
        assert(model.All.SelectedCount==2 && RepairDependencies.Expand(model.Choices.Where(c=>c.IsChecked).Select(c=>c.Issue).ToArray(),model.Choices.Select(c=>c.Issue).ToArray()).Count==2,
            "Select all combines PAGE and an overlapping body format while keeping manual header warnings unselected");
        assert(model.Choices.Single(c=>c.Issue==pageIssue).Info.Section==IssueReviewSection.RequiredFormat &&
            model.Choices.Single(c=>c.Issue==pageIssue).Tooltip.Contains("I.7") && model.Choices.Single(c=>c.Issue==pageIssue).Tooltip.Split('\n').Length==3,
            "PAGE uses a compact required-format group and the requested three-line legal tooltip");
        string folder=Path.Combine(Path.GetTempPath(),"ChinhTa-number-fixture-"+Guid.NewGuid());Directory.CreateDirectory(folder);
        try
        {
            string source=OfficePageNumberCheck.Create(Path.Combine("samples","BaDuongKeThieu.docx"),folder),candidate=Path.Combine(folder,"candidate.docx");
            XNamespace pkg="http://schemas.microsoft.com/office/2006/xmlPackage";
            var parts=new List<XElement>();
            using(var zip=ZipFile.OpenRead(source))
            foreach(var entry in zip.Entries.Where(e=>e.FullName.EndsWith(".xml",StringComparison.Ordinal) || e.FullName.EndsWith(".rels",StringComparison.Ordinal)))
            {
                using var stream=entry.Open();var root=XDocument.Load(stream).Root!;
                parts.Add(new XElement(pkg+"part",new XAttribute(pkg+"name","/"+entry.FullName),new XElement(pkg+"xmlData",root)));
            }
            var probe=new DocumentProbe(new XDocument(new XElement(pkg+"package",parts)).ToString());
            var packageSnapshot=PageNumberDocumentReader.ReadCurrent(probe);
            assert(probe.Reads==1 && probe.RangeReads==0 && PageNumberRules.Check(packageSnapshot,2,0,"C",DocumentProfile.State).Single().CanFix,
                "PAGE reads the current full document package once, retaining footer relationships instead of relying on partial-range XML");
            var choice=PageNumberRules.Check(PageNumberPackage.FromFile(source),2,0,"C",DocumentProfile.State).Single();
            DocxPageNumberEditor.Write(source,candidate,choice.NumberFix!,2);DocxPageNumberEditor.Verify(source,candidate,choice.NumberFix!,2);
            DocxPageNumberEditor.VerifyLive(PageNumberPackage.FromFile(source),PageNumberPackage.FromFile(candidate));
            using(var package=DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(candidate,false))
            {
                var validator=new DocumentFormat.OpenXml.Validation.OpenXmlValidator();
                var headers=package.MainDocumentPart!.HeaderParts.ToArray();
                var errors=headers.SelectMany(h=>validator.Validate(h.Header)).Concat(validator.Validate(package.MainDocumentPart.Document.Body!.Elements<DocumentFormat.OpenXml.Wordprocessing.SectionProperties>().Single())).ToArray();
                assert(headers.Length==1 && errors.Length==0,"The generated PAGE header/field/section satisfy OpenXML schema: "+string.Join("; ",errors.Select(e=>e.Description)));
            }
            assert(PageNumberRules.ProvenStandard(PageNumberPackage.FromFile(candidate),2),"The actual Office fixture has canonical PAGE and preserves the old footer, including its first-page reference");
        }
        finally{Directory.Delete(folder,true);}
    }
}
