using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using ChinhTa.Core;
using ChinhTa.Desktop;

static class VisibleReferenceChecks
{
    private const string Text="Độc lập – Tự do – Hạnh phúc";
    private static readonly PreviewAnchor Anchor=new(1,Text,0,Text.Length);
    private static FixedDocumentSequence Page(Action<Canvas,Glyphs>? setup=null)
    {
        var page=new FixedPage{Width=800,Height=1100};var canvas=new Canvas{RenderTransform=new ScaleTransform(4d/3,4d/3)};
        var glyph=new Glyphs{FontUri=new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"arial.ttf")),
            UnicodeString=Text,FontRenderingEmSize=14,OriginX=100,OriginY=100,Fill=Brushes.Black};
        page.Children.Add(canvas);canvas.Children.Add(glyph);setup?.Invoke(canvas,glyph);
        var content=new PageContent();((IAddChild)content).AddChild(page);var doc=new FixedDocument();doc.Pages.Add(content);
        var reference=new DocumentReference();reference.SetDocument(doc);var sequence=new FixedDocumentSequence();sequence.References.Add(reference);return sequence;
    }
    public static void Run(Action<bool,string> assert)
    {
        bool Reference(Action<Canvas,Glyphs>? setup=null)=>ReferenceMeasurement.Read(Page(setup),new PreviewHighlighter(),Anchor) is not null;
        assert(Reference(),"Visible ordinary text still establishes a Line reference");
        bool hidden=Reference((c,g)=>c.Opacity=0),transparent=Reference((c,g)=>g.Fill=Brushes.Transparent),
            partial=Reference((c,g)=>g.Clip=new RectangleGeometry(new Rect(100,70,5,50))),
            outside=Reference((c,g)=>g.RenderTransform=new TranslateTransform(1000,0)),
            rotated=Reference((c,g)=>g.RenderTransform=new RotateTransform(5));
        bool hiddenRow=new PreviewHighlighter().MeasureSingleLine(Page((c,g)=>g.Opacity=0),Anchor) is not null;
        assert(!hidden && !transparent && !partial && !outside && !rotated && !hiddenRow,
            $"Unshown reference text cannot authorize geometry (hidden={hidden}, transparent={transparent}, partial clip={partial}, outside page={outside}, rotated={rotated}, hidden single row={hiddenRow})");
    }
}
