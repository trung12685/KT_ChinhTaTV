using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using ChinhTa.Core;
using ChinhTa.Desktop;
using Path=System.Windows.Shapes.Path;

public static class PageLayoutBodyChecks
{
    private const double Units=96/25.4;
    private static readonly SectionLayout Layout=new(1,0,"N",210,297,20,20,30,15);
    private static readonly Rect Body=new(30*Units,20*Units,165*Units,257*Units);
    private static FixedDocumentSequence Pages(Rect? rectangle=null,Action<Path,Canvas>? change=null,
        double width=210*Units,double height=297*Units,bool blankLast=false)
    {
        var document=new FixedDocument();
        for(int n=0;n<(blankLast?2:1);n++)
        {
            var page=new FixedPage{Width=width,Height=height};
            if(n==0)
            {
                var path=new Path{Fill=Brushes.Black,Data=new RectangleGeometry(rectangle??new Rect(Body.Left+10,Body.Top+10,80,25))};
                var canvas=new Canvas();canvas.Children.Add(path);change?.Invoke(path,canvas);page.Children.Add(canvas);
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
        PageLayoutBodyPreviewVerifier.Verify(Pages(),Layout);
        PageLayoutBodyPreviewVerifier.Verify(Pages(blankLast:true),Layout);
        PageLayoutBodyPreviewVerifier.Verify(Pages(new Rect(Body.Left-1,Body.Top-1,Body.Width+2,Body.Height+2)),Layout);
        assert(true,"Empty header/footer C32 scope accepts contained paint, a genuine blank page, and one-point rendering tolerance");
        foreach(var rectangle in new[]{
            new Rect(Body.Left-2,Body.Top+10,30,20),new Rect(Body.Left+10,Body.Top-2,30,20),
            new Rect(Body.Right-28,Body.Top+10,30,20),new Rect(Body.Left+10,Body.Bottom-18,30,20),
            new Rect(Body.Left+10,10,30,20)})
            assert(Reject(()=>PageLayoutBodyPreviewVerifier.Verify(Pages(rectangle),Layout)),
                "C32 rejects visible paint beyond every approved body edge, including upper-margin paint omitted by PAGE capture");
        foreach(var layout in new[]{Layout with {TopMm=double.NaN},Layout with {BottomMm=double.PositiveInfinity},
            Layout with {LeftMm=-1},Layout with {RightMm=200},Layout with {Number=2},Layout with {MirrorMargins=true},
            Layout with {WidthMm=297,HeightMm=210}})
            assert(Reject(()=>PageLayoutBodyPreviewVerifier.Verify(Pages(),layout)),"C32 cannot verify unknown, mirrored or unsupported page geometry");
        assert(Reject(()=>PageLayoutBodyPreviewVerifier.Verify(Pages(width:210*Units+2),Layout)),"C32 rejects changed XPS page width");
        assert(Reject(()=>PageLayoutBodyPreviewVerifier.Verify(Pages(height:297*Units+2),Layout)),"C32 rejects changed XPS page height");
        var overhang=new Glyphs
        {
            FontUri=new Uri(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"timesi.ttf")),
            FontRenderingEmSize=100,UnicodeString="f",Fill=Brushes.Black,OriginY=Body.Top+130
        };
        var run=overhang.ToGlyphRun();var alignment=run.ComputeAlignmentBox();var ink=run.ComputeInkBoundingBox();
        overhang.OriginX=Body.Right-alignment.Right;
        assert(ink.Right-alignment.Right>96d/72,"Italic Times fixture has actual ink beyond its alignment box");
        assert(Reject(()=>PageLayoutBodyPreviewVerifier.Verify(Pages(change:(_,c)=>c.Children.Add(overhang)),Layout)),
            "C32 containment includes actual ink overhang even when glyph alignment alone fits the body");
        foreach(var change in new Action<Path,Canvas>[]{
            (_,c)=>c.Clip=new RectangleGeometry(new Rect(Body.Left+30,Body.Top,20,100)),
            (_,c)=>c.OpacityMask=new LinearGradientBrush(Colors.Black,Colors.White,0),
            (p,_)=>p.Effect=new DropShadowEffect(),
            (_,c)=>c.Children.Add(new System.Windows.Shapes.Rectangle{Width=50,Height=20,Fill=Brushes.Red})})
            assert(Reject(()=>PageLayoutBodyPreviewVerifier.Verify(Pages(change:change),Layout)),"C32 rejects clipped, masked, affected or unsupported rendered content");
        assert(Reject(()=>PageLayoutBodyPreviewVerifier.Verify(Pages(change:(p,_)=>p.Opacity=0),Layout)),
            "An empty rendered snapshot cannot prove a nonempty source body survived pagination");
        PageLayoutBodyPreviewVerifier.Verify(Pages(change:(_,c)=>c.Children.Add(new Path{
            Fill=Brushes.Black,Opacity=0,Data=new RectangleGeometry(new Rect(0,0,30,30))})),Layout);
        assert(true,"Known fully transparent paint does not invent an out-of-body overflow");
        bool restored=false,committed=false;
        try{PageLayoutBatch.Apply(()=>{},()=>{},()=>PageLayoutBodyPreviewVerifier.Verify(Pages(new Rect(Body.Left,Body.Bottom,30,20)),Layout),
            ()=>committed=true,()=>restored=true);}catch(InvalidOperationException){}
        assert(restored && !committed,"Failed rendered C32 containment restores the whole batch before commit");
    }
}
