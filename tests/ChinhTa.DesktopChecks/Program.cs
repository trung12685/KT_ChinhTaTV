using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using ChinhTa.Core;
using ChinhTa.Desktop;
using LineGeometry = ChinhTa.Core.LineGeometry;

static class Program
{
    const string Text="Độc lập – Tự do – Hạnh phúc";
    static int checks;
    static void Assert(bool condition,string name)
    {
        if(!condition)throw new InvalidOperationException(name);
        Console.WriteLine("PASS "+name);checks++;
    }
    static FixedDocumentSequence Page(Geometry? clip, bool split=false)
    {
        var page=new FixedPage {Width=794,Height=1123};
        var root=new Canvas {RenderTransform=new ScaleTransform(4d/3,4d/3)};
        page.Children.Add(root);
        void Add(string text,double x,Geometry? region)
        {
            var canvas=new Canvas {Clip=region};root.Children.Add(canvas);
            canvas.Children.Add(new Glyphs {FontUri=new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"arial.ttf")),
                FontRenderingEmSize=14,OriginX=x,OriginY=95.064,UnicodeString=text,Fill=Brushes.Black});
        }
        if(split)
        {
            Add(Text[..10],325.99,clip);
            Add(Text[10..],400,new RectangleGeometry(new Rect(85,56.67,191,104)));
        }
        else Add(Text,325.99,clip);
        var content=new PageContent();((IAddChild)content).AddChild(page);
        var document=new FixedDocument();document.Pages.Add(content);
        var reference=new DocumentReference();reference.SetDocument(document);
        var sequence=new FixedDocumentSequence();sequence.References.Add(reference);
        return sequence;
    }
    [STAThread]
    static void Main()
    {
        var anchor=new PreviewAnchor(1,Text,0,Text.Length);
        var clip=new RectangleGeometry(new Rect(276.2,56.67,290.55,104));
        var frame=new PreviewHighlighter().TableFrame(Page(clip),anchor);
        Assert(frame is not null && Math.Abs(frame.Left-276.2)<.01 && Math.Abs(frame.Top-56.67)<.01 &&
            Math.Abs(frame.Width-290.55)<.01,"WPF ancestor transforms yield the actual right-cell frame in page points");
        Assert(new PreviewHighlighter().TableFrame(Page(null),anchor) is null,"No frame without clipping evidence");
        Assert(new PreviewHighlighter().TableFrame(Page(new RectangleGeometry(new Rect(0,0,595,842))),anchor) is null,
            "Full-page clip cannot masquerade as a table-cell frame");
        Assert(new PreviewHighlighter().TableFrame(Page(Geometry.Parse("M 276,56 L 566,56 L 276,160 Z")),anchor) is null,
            "Non-rectangular clip cannot establish a cell origin");
        Assert(new PreviewHighlighter().TableFrame(Page(clip,true),anchor) is null,"Reference text split across different frames is ambiguous");
        var motto=new Issue(0,Text.Length,Text,Text,"",false,FixKind.ShapeLine,LineFix:new(null,333.67,93.76,175.94,LineRole.Motto));
        var existing=new[]{new LineGeometry(145.15,92.7,70.85),new LineGeometry(254.95,206.45,92.25)};
        var correction=LinePlacementFeedback.CorrectHiddenMotto([motto],existing,existing,frame);
        Assert(correction is not null && Math.Abs(correction[0].LineFix!.Left-57.47)<.01,
            "Measured WPF table frame feeds the invisible-Motto correction without reusing the left-cell offset");
        Console.WriteLine($"{checks} WPF checks passed.");
    }
}
