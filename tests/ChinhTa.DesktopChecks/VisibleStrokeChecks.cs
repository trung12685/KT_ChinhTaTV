using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using ChinhTa.Desktop;
using Path=System.Windows.Shapes.Path;

static class VisibleStrokeChecks
{
    private static FixedDocumentSequence Page(Action<Canvas,Path>? setup=null)
    {
        var page=new FixedPage{Width=800,Height=1100};
        var root=new Canvas{RenderTransform=new ScaleTransform(4d/3,4d/3)};page.Children.Add(root);
        var path=new Path{Data=new System.Windows.Media.LineGeometry(new Point(100,120),new Point(180,120)),Stroke=Brushes.Black,StrokeThickness=.5};root.Children.Add(path);
        setup?.Invoke(root,path);
        var content=new PageContent();((IAddChild)content).AddChild(page);var document=new FixedDocument();document.Pages.Add(content);
        var reference=new DocumentReference();reference.SetDocument(document);var sequence=new FixedDocumentSequence();sequence.References.Add(reference);return sequence;
    }
    public static void Run(Action<bool,string> assert)
    {
        int Count(Action<Canvas,Path>? setup=null)=>new PreviewHighlighter().HorizontalLines(Page(setup),1).Count;
        assert(Count()==1,"A normal opaque transformed WPF stroke remains available for Line verification");
        int hidden=Count((c,p)=>c.Opacity=0),transparent=Count((c,p)=>p.Stroke=Brushes.Transparent),
            clipped=Count((c,p)=>c.Clip=new RectangleGeometry(new Rect(0,0,20,20))),
            emptyFill=Count((c,p)=>{p.Stroke=null;p.Fill=Brushes.Black;});
        assert(hidden==0 && transparent==0 && clipped==0 && emptyFill==0,
            $"Invisible/unpainted paths cannot prove a rendered Line (ancestor opacity={hidden}, transparent brush={transparent}, clipped away={clipped}, fill-only open segment={emptyFill})");
    }
}
