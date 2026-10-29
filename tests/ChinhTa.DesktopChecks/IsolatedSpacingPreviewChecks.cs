using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using ChinhTa.Desktop;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;

public static class IsolatedSpacingPreviewChecks
{
    private static FixedDocumentSequence Page(Action<FixedPage,Canvas> change)
    {
        var page=new FixedPage{Width=794,Height=1123};var canvas=new Canvas();page.Children.Add(canvas);
        for(int i=0;i<3;i++)canvas.Children.Add(new Glyphs
        {
            FontUri=new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"times.ttf")),
            FontRenderingEmSize=18,OriginX=100,OriginY=150+i*24,UnicodeString="Dòng tiếng Việt "+i,Fill=Brushes.Black
        });
        canvas.Children.Add(new System.Windows.Shapes.Path{Data=Geometry.Parse("M 100,250 L 150,260 L 100,270 Z"),Fill=Brushes.Blue});
        var bitmap=BitmapSource.Create(1,1,96,96,PixelFormats.Bgra32,null,new byte[]{255,0,0,255},4);
        var image=new Image{Source=bitmap,Width=24,Height=24,Stretch=Stretch.Fill};Canvas.SetLeft(image,200);Canvas.SetTop(image,300);canvas.Children.Add(image);
        change(page,canvas);
        var content=new PageContent();((IAddChild)content).AddChild(page);var document=new FixedDocument();document.Pages.Add(content);
        var reference=new DocumentReference();reference.SetDocument(document);var sequence=new FixedDocumentSequence();sequence.References.Add(reference);return sequence;
    }
    public static void Run(Action<bool,string> assert)
    {
        var proof=IsolatedSpacingPreview.Capture(Page((_,_)=>{}));proof.RequireSame(IsolatedSpacingPreview.Capture(Page((_,_)=>{})));
        assert(proof.Pages==1 && proof.GlyphRuns==3,"Identical multi-line XPS with bitmap and path establishes a clone baseline without Word");
        void Rejected(Action<FixedPage,Canvas> change,string description)
        {
            bool failed=false;try{proof.RequireSame(IsolatedSpacingPreview.Capture(Page(change)));}catch(InvalidOperationException){failed=true;}assert(failed,description);
        }
        Rejected((_,c)=>((Glyphs)c.Children[1]).OriginY+=.1,"Baseline drift smaller than PAGE's preservation tolerance still blocks isolated calibration");
        Rejected((_,c)=>((Glyphs)c.Children[0]).FontUri=new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"arial.ttf")),"Same text with a substituted font cannot calibrate Word line spacing");
        Rejected((_,c)=>((Glyphs)c.Children[1]).Fill=Brushes.Red,"Clone equivalence retains glyph paint, not only glyph text and baseline");
        Rejected((_,c)=>((Glyphs)c.Children[1]).Opacity=.5,"A faded glyph invalidates the copy baseline");
        Rejected((_,c)=>((Glyphs)c.Children[1]).Clip=new RectangleGeometry(new Rect(0,0,10,10)),"Clipped glyphs cannot earn a calibration proof");
        Rejected((p,_)=>p.Width+=1,"Clone paper dimensions are compared before spacing calibration");
        Rejected((_,c)=>c.Children.RemoveAt(2),"A missing line cannot hide behind equal paragraph content elsewhere");
        Rejected((_,c)=>((System.Windows.Shapes.Path)c.Children[3]).Data=Geometry.Parse("M 100,250 L 150,250 L 150,270 Z"),"Different paths with identical bounding boxes and paint must still fail clone equivalence");
        Rejected((_,c)=>Canvas.SetTop(c.Children[4],300.1),"A small bitmap displacement prevents faithful-copy approval");
        Rejected((_,c)=>((Image)c.Children[4]).Source=BitmapSource.Create(1,1,96,96,PixelFormats.Bgra32,null,new byte[]{0,0,255,255},4),"Changed decoded image pixels cannot be ignored by the spacing baseline");
        Rejected((_,c)=>{var path=c.Children[3];c.Children.RemoveAt(3);c.Children.Insert(0,path);},"Changing paint order relative to text is not hidden by sorted visual bounds");
        Rejected((_,c)=>c.Background=Brushes.White,"A painted container background outside the leaf proof stays unsupported");
        Rejected((_,c)=>c.Children[0].RenderTransform=new RotateTransform(2),"Rotated text remains outside the narrow line-spacing calibration scope");

        string folder=Path.Combine(Path.GetTempPath(),"ChinhTa-isolated-fixture-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try
        {
            string seed=OfficeFocusedCheck.CreateSeed(Path.Combine("samples","BaDuongKeThieu.docx"),folder);
            foreach(string rule in new[]{"exact","atLeast"})
            {
                string source=OfficeIsolatedSpacingCheck.Create(seed,folder,rule);
                using(var doc=WordprocessingDocument.Open(source,false))
                    assert(!new OpenXmlValidator().Validate(doc).Any(),"Isolated Word "+rule+" diagnostic fixture is schema-valid before running Office");
                using var zip=ZipFile.OpenRead(source);using var input=zip.GetEntry("word/document.xml")!.Open();var xml=XDocument.Load(input);
                XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
                assert(xml.Descendants(w+"spacing").Count(e=>(string?)e.Attribute(w+"lineRule")==rule)==2 && xml.Descendants(w+"br").Count()>=4,
                    "The "+rule+" fixture contains two paragraph-spacing contexts with at least three baselines each, not a font-size approximation");
            }
        }
        finally{Directory.Delete(folder,true);}
    }
}
