using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using ChinhTa.Core;
using ChinhTa.Desktop;
using Path=System.Windows.Shapes.Path;

public static class PageNumberPaintChecks
{
    private static readonly double Width=210*96/25.4,Height=297*96/25.4;
    private static readonly double Center=(30*96/25.4+Width-15*96/25.4)/2;
    private static Glyphs Glyph(string text,double x,double y)=>new()
    {
        FontUri=new Uri(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"times.ttf")),
        FontRenderingEmSize=14*96d/72,UnicodeString=text,Fill=Brushes.Black,OriginX=x,OriginY=y
    };
    private static FixedDocumentSequence Pages(Action<Glyphs,Canvas>? number=null,Action<Glyphs,Path,Canvas>? body=null,
        bool first=false,bool scaled=false,Action<Glyphs>? footer=null)
    {
        var document=new FixedDocument();
        for(int n=0;n<2;n++)
        {
            var page=new FixedPage{Width=Width,Height=Height};
            if(n==1 || first)
            {
                var glyph=Glyph(n==0?"1":"2",0,40);glyph.OriginX=Center-glyph.ToGlyphRun().ComputeAlignmentBox().Width/2;
                var canvas=new Canvas();canvas.Children.Add(glyph);number?.Invoke(glyph,canvas);page.Children.Add(canvas);
            }
            var text=Glyph("Nội dung giữ nguyên",100,150);
            var shape=new Path{Data=new RectangleGeometry(new Rect(100,200,60,30)),Fill=Brushes.Blue};
            var footerText=Glyph("Ghi chú cuối trang",100,Height-30);footer?.Invoke(footerText);
            var contentCanvas=new Canvas();contentCanvas.Children.Add(text);contentCanvas.Children.Add(shape);contentCanvas.Children.Add(footerText);
            body?.Invoke(text,shape,contentCanvas);page.Children.Add(contentCanvas);
            if(scaled)
            {
                const double scale=4d/3;var outer=new Canvas{RenderTransform=new ScaleTransform(scale,scale)};
                var children=page.Children.Cast<UIElement>().ToArray();page.Children.Clear();
                foreach(var child in children)
                {
                    var canvas=(Canvas)child;
                    foreach(var visual in canvas.Children)
                    {
                        if(visual is Glyphs glyph){glyph.OriginX/=scale;glyph.OriginY/=scale;glyph.FontRenderingEmSize/=scale;}
                        if(visual is Path path)path.Data.Transform=new ScaleTransform(1/scale,1/scale);
                    }
                    outer.Children.Add(child);
                }
                page.Children.Add(outer);
            }
            var content=new PageContent();((IAddChild)content).AddChild(page);document.Pages.Add(content);
        }
        var reference=new DocumentReference();reference.SetDocument(document);
        var sequence=new FixedDocumentSequence();sequence.References.Add(reference);return sequence;
    }
    private static bool Reject(Action action)
    {try{action();return false;}catch(InvalidOperationException){return true;}}
    public static void Run(Action<bool,string> assert)
    {
        PageNumberPreviewVerifier.VerifyNumbers(Pages(),30,15,20);
        PageNumberPreviewVerifier.VerifyNumbers(Pages(scaled:true),30,15,20);
        PageNumberPreviewVerifier.VerifyNumbers(Pages((_,c)=>c.OpacityMask=Brushes.White),30,15,20);
        PageNumberPreviewVerifier.VerifyNumbers(Pages((_,c)=>c.Clip=new RectangleGeometry(new Rect(0,0,Width,Height))),30,15,20);
        assert(true,"PAGE proof accepts opaque complete numerals, positive Word-style scaling, full clips and solid opaque masks");

        var invalid=new (string Name,Action<Glyphs,Canvas> Change)[]
        {
            ("glyph opacity0",(g,_)=>g.Opacity=0),
            ("ancestor opacity0",(_,c)=>c.Opacity=0),
            ("hidden ancestor",(_,c)=>c.Visibility=Visibility.Hidden),
            ("ancestor fades black",(_,c)=>c.Opacity=.5),
            ("fill opacity",(g,_)=>g.Fill=new SolidColorBrush(Colors.Black){Opacity=.5}),
            ("fill alpha",(g,_)=>g.Fill=new SolidColorBrush(Color.FromArgb(128,0,0,0))),
            ("mask alpha",(_,c)=>c.OpacityMask=new SolidColorBrush(Color.FromArgb(128,255,255,255))),
            ("unknown mask",(_,c)=>c.OpacityMask=new LinearGradientBrush(Colors.Black,Colors.White,0)),
            ("partial clip",(_,c)=>c.Clip=new RectangleGeometry(new Rect(Center,0,Width,Height))),
            ("outside clip",(_,c)=>c.Clip=new RectangleGeometry(new Rect(0,200,50,50))),
            ("mirrored numeral",(_,c)=>c.RenderTransform=new MatrixTransform(-1,0,0,1,Center*2,0)),
            ("inverted numeral",(_,c)=>c.RenderTransform=new MatrixTransform(1,0,0,-1,0,80)),
            ("skewed numeral",(_,c)=>c.RenderTransform=new MatrixTransform(1,0,.1,1,-4,0)),
            ("unknown effect",(g,_)=>g.Effect=new DropShadowEffect()),
            ("outside page",(g,_)=>g.OriginY=5)
        };
        foreach(var test in invalid)
            assert(Reject(()=>PageNumberPreviewVerifier.VerifyNumbers(Pages(test.Change),30,15,20)),
                "An invisible, faded, clipped or unproven PAGE numeral cannot satisfy rendered verification: "+test.Name);
        assert(Reject(()=>PageNumberPreviewVerifier.VerifyNumbers(Pages((g,_)=>g.OriginX+=80),30,15,20)),
            "A PAGE numeral outside the old40px candidate band cannot disappear from first-page or centering checks");
        assert(Reject(()=>PageNumberPreviewVerifier.VerifyNumbers(Pages((g,_)=>g.OriginX+=80,first:true),30,15,20)),
            "A misplaced visible first-page numeral still fails instead of being excluded by a center-band guess");

        var before=PageNumberPreviewVerifier.Capture(Pages(),20);
        PageNumberPreviewVerifier.Preserve(before,PageNumberPreviewVerifier.Capture(Pages(),20));
        PageNumberPreviewVerifier.Preserve(before,PageNumberPreviewVerifier.Capture(Pages(scaled:true),20));
        PageNumberPreviewVerifier.Preserve(before,PageNumberPreviewVerifier.Capture(Pages(body:(_,p,_)=>p.StrokeThickness=0),20));
        assert(true,"Body PAGE preservation accepts unchanged actual paint and equivalent positive canvas coordinates");
        var bodyChanges=new (string Name,Action<Glyphs,Path,Canvas> Change)[]
        {
            ("hidden text",(g,_,_)=>g.Opacity=0),
            ("faded body ancestor",(_,_,c)=>c.Opacity=.5),
            ("body text color",(g,_,_)=>g.Fill=Brushes.Red),
            ("body text alpha",(g,_,_)=>g.Fill=new SolidColorBrush(Color.FromArgb(128,0,0,0))),
            ("shape color",(_,p,_)=>p.Fill=Brushes.Red),
            ("hidden shape",(_,p,_)=>p.Opacity=0),
            ("faded shape",(_,p,_)=>p.Opacity=.5),
            ("body partial clip",(_,_,c)=>c.Clip=new RectangleGeometry(new Rect(0,0,130,Height))),
            ("unknown body mask",(_,_,c)=>c.OpacityMask=new LinearGradientBrush(Colors.Black,Colors.White,0)),
            ("body effect",(g,_,_)=>g.Effect=new DropShadowEffect())
        };
        foreach(var test in bodyChanges)
            assert(Reject(()=>PageNumberPreviewVerifier.Preserve(before,PageNumberPreviewVerifier.Capture(Pages(body:test.Change),20))),
                "PAGE cannot preserve unchanged text/bounds while unselected body/footer paint changes: "+test.Name);
        foreach(var change in new Action<Glyphs>[] {g=>g.Opacity=0,g=>g.Fill=Brushes.Red,g=>g.Opacity=.5})
            assert(Reject(()=>PageNumberPreviewVerifier.Preserve(before,PageNumberPreviewVerifier.Capture(Pages(footer:change),20))),
                "Unselected footer glyph paint is retained independently of unchanged content and position");
        var faded=PageNumberPreviewVerifier.Capture(Pages(body:(_,_,c)=>c.Opacity=.5),20);
        PageNumberPreviewVerifier.Preserve(faded,PageNumberPreviewVerifier.Capture(Pages(body:(_,_,c)=>c.Opacity=.5),20));
        assert(true,"Known unchanged body transparency is preserved without forcing all body artwork to opaque black");
        foreach(double invalidMargin in new[]{double.NaN,double.PositiveInfinity,-1d,0d})
            assert(Reject(()=>PageNumberPreviewVerifier.Capture(Pages(),invalidMargin)) &&
                Reject(()=>PageNumberPreviewVerifier.VerifyNumbers(Pages(),30,15,invalidMargin)),
                "An unknown upper-margin boundary cannot produce an empty or fabricated PAGE proof");
        bool restored=false;var failure=new InvalidOperationException("synthetic PAGE paint failure");
        try{PageLayoutBatch.Apply(()=>{},()=>{},()=>
            {PageNumberPreviewVerifier.VerifyNumbers(Pages((_,c)=>c.Opacity=0),30,15,20);throw failure;},
            ()=>throw new InvalidOperationException("Unverified PAGE must not commit"),()=>restored=true);}
        catch(InvalidOperationException){}
        assert(restored,"A rendered PAGE visibility failure reaches the existing whole-selection rollback boundary");
    }
}
