using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChinhTa.Desktop;

public static class SpacingTrialPreviewChecks
{
    private const string First="Nội dung thứ nhất dòng hai dòng ba.";
    private const string Second="Đoạn liền sau còn nguyên chữ.";
    private static readonly string[] Texts=["Tiền tố.","Nội dung thứ nhất ","dòng hai ","dòng ba.","Đoạn liền sau ","còn nguyên ","chữ.","Phần cuối."];
    private static readonly double[] SourceY=[80,150,174,198,240,264,288,330];
    private static FixedDocumentSequence Page(double[]? baselines=null,Action<FixedPage,Canvas>? edit=null)
    {
        var page=new FixedPage{Width=794,Height=1123};var canvas=new Canvas();page.Children.Add(canvas);
        for(int i=0;i<Texts.Length;i++)canvas.Children.Add(new Glyphs
        {
            FontUri=new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"times.ttf")),FontRenderingEmSize=18,
            OriginX=100,OriginY=(baselines??SourceY)[i],UnicodeString=Texts[i],Fill=Brushes.Black
        });
        canvas.Children.Add(new System.Windows.Shapes.Path{Data=Geometry.Parse("M 500,100 L 550,110 L 500,120 Z"),Fill=Brushes.Blue});
        var image=new Image{Width=24,Height=24,Stretch=Stretch.Fill,
            Source=BitmapSource.Create(1,1,96,96,PixelFormats.Bgra32,null,new byte[]{255,0,0,255},4)};
        Canvas.SetLeft(image,500);Canvas.SetTop(image,150);canvas.Children.Add(image);
        edit?.Invoke(page,canvas);
        var content=new PageContent();((IAddChild)content).AddChild(page);var doc=new FixedDocument();doc.Pages.Add(content);
        var reference=new DocumentReference();reference.SetDocument(doc);var sequence=new FixedDocumentSequence();sequence.References.Add(reference);return sequence;
    }
    public static void Run(Action<bool,string> assert)
    {
        var source=IsolatedSpacingPreview.Capture(Page());
        double[] single=[80,150,168,186,228,252,276,318];
        var singlePreview=IsolatedSpacingPreview.Capture(Page(single));source.RequireSpacingTrial(singlePreview,[First]);
        assert(singlePreview.Layout.GlyphEvidence.Count==8,"A line-spacing trial preserves complete glyph/paint evidence while only target baselines and same-page suffix translate");
        double[] half=[80,150,177,204,246,270,294,336];
        source.RequireSpacingTrial(IsolatedSpacingPreview.Capture(Page(half)),[First]);
        assert(true,"A separate 1.5-line trial can expand the target vector without relaxing phông, artwork or prefix preservation");
        double[] zeroGap=[80,150,174,198,228,252,276,318];
        source.RequireSpacingTrial(IsolatedSpacingPreview.Capture(Page(zeroGap)),[First,Second]);
        assert(true,"A zero-gap trial preserves the first paragraph's leading and moves the adjacent second paragraph and suffix together");
        double[] sixPointGap=[80,150,174,198,236,260,284,326];
        source.RequireSpacingTrial(IsolatedSpacingPreview.Capture(Page(sixPointGap)),[First,Second]);
        assert(true,"A measured six-point reference trial can move the adjacent paragraph and suffix without altering paint or line leading");
        void Rejected(double[]? y,Action<FixedPage,Canvas>? edit,string[] targets,string description)
        {
            bool failed=false;
            try{source.RequireSpacingTrial(IsolatedSpacingPreview.Capture(Page(y,edit)),targets);}
            catch(InvalidOperationException){failed=true;}
            assert(failed,description);
        }
        Rejected(single,(_,c)=>((Glyphs)c.Children[0]).OriginY+=1,[First],"An unselected prefix glyph cannot move during spacing calibration");
        Rejected(single,(_,c)=>((Glyphs)c.Children[6]).OriginY+=1,[First],"A nonuniform suffix shift is rejected rather than explained by target spacing");
        Rejected(single,(_,c)=>((Glyphs)c.Children[2]).OriginX+=1,[First],"Changed horizontal layout/reflow blocks trial calibration");
        Rejected(single,(_,c)=>((Glyphs)c.Children[2]).Fill=Brushes.Red,[First],"The permitted baseline shift never whitelists a changed glyph color");
        Rejected(single,(_,c)=>((Glyphs)c.Children[6]).Opacity=.5,[First],"An unchanged suffix's opacity remains protected while its baseline translates");
        Rejected(single,(_,c)=>((Glyphs)c.Children[2]).Clip=new RectangleGeometry(new Rect(0,0,10,10)),[First],"A clipped target glyph cannot calibrate line spacing");
        Rejected(single,(_,c)=>((Glyphs)c.Children[2]).FontUri=new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"arial.ttf")),[First],"Trial font substitution is rejected even when the visible text stays the same");
        Rejected(single,(_,c)=>((Glyphs)c.Children[2]).UnicodeString="dòng đã đổi ",[First],"Calibration cannot accept a textual edit alongside spacing");
        Rejected(single,(_,c)=>((Glyphs)c.Children[2]).FontRenderingEmSize=19,[First],"Font size changes are not interchangeable with measured Single baselines");
        Rejected(single,(p,_)=>p.Height+=1,[First],"A trial cannot silently change page dimensions");
        Rejected(single,(_,c)=>c.Children.RemoveAt(2),[First],"A lost/split glyph run cannot be remapped by paragraph substring guessing");
        Rejected(single,(_,c)=>((System.Windows.Shapes.Path)c.Children[8]).Data=Geometry.Parse("M 500,100 L 550,100 L 550,120 Z"),[First],"Changing a path while retaining the same paint and bounding box still invalidates the trial");
        Rejected(single,(_,c)=>c.Children[8].RenderTransform=new TranslateTransform(0,-12),[First],"A drawing does not inherit permission to move merely because nearby text shifts");
        Rejected(single,(_,c)=>Canvas.SetTop(c.Children[9],138),[First],"A shifted image has no inferred attachment proof and remains outside calibration");
        Rejected(single,(_,c)=>((Image)c.Children[9]).Source=BitmapSource.Create(1,1,96,96,PixelFormats.Bgra32,null,new byte[]{0,0,255,255},4),[First],"Changing image pixels cannot hide behind a permitted paragraph spacing trial");
        Rejected(single,(_,c)=>{var path=c.Children[8];c.Children.RemoveAt(8);c.Children.Insert(0,path);},[First],"Draw order stays protected when glyph baselines change");
        Rejected(single,null,["Nội dung thứ nhất"],"A partial glyph/paragraph substring cannot become a calibration target");
        Rejected(single,null,[First,"Phần cuối."],"Two nonadjacent targets cannot whitelist unrelated intervening paragraphs");
        Rejected(single,null,[],"An empty target set cannot whitelist all preview differences");
        Rejected(single,null,[First,Second,"Phần cuối."],"A multi-paragraph selection outside the narrow one/two-paragraph trial scope is rejected");
        var repeatedSource=IsolatedSpacingPreview.Capture(Page(null,(_,c)=>((Glyphs)c.Children[7]).UnicodeString=First));
        bool repeatedRejected=false;
        try{repeatedSource.RequireSpacingTrial(IsolatedSpacingPreview.Capture(Page(single,(_,c)=>((Glyphs)c.Children[7]).UnicodeString=First)),[First]);}
        catch(InvalidOperationException){repeatedRejected=true;}
        assert(repeatedRejected,"A repeated source target cannot be bound by taking the first matching occurrence");
        void Cover(FixedPage _,Canvas c){Canvas.SetLeft(c.Children[9],100);Canvas.SetTop(c.Children[9],170);}
        var coveredSource=IsolatedSpacingPreview.Capture(Page(null,Cover));bool occludedRejected=false;
        try{coveredSource.RequireSpacingTrial(IsolatedSpacingPreview.Capture(Page(single,Cover)),[First]);}
        catch(InvalidOperationException){occludedRejected=true;}
        assert(occludedRejected,"Clipped-in but potentially artwork-occluded glyphs cannot provide visible trial baseline evidence");
        void SecondImage(Canvas c)
        {
            var image=new Image{Width=24,Height=24,Stretch=Stretch.Fill,
                Source=BitmapSource.Create(1,1,96,96,PixelFormats.Bgra32,null,new byte[]{0,0,255,255},4)};
            Canvas.SetLeft(image,510);Canvas.SetTop(image,160);c.Children.Add(image);
        }
        var imageOrderSource=IsolatedSpacingPreview.Capture(Page(null,(_,c)=>SecondImage(c)));bool imageOrderRejected=false;
        try
        {
            imageOrderSource.RequireSpacingTrial(IsolatedSpacingPreview.Capture(Page(single,(_,c)=>
            {SecondImage(c);var image=c.Children[10];c.Children.RemoveAt(10);c.Children.Insert(9,image);})),[First]);
        }
        catch(InvalidOperationException){imageOrderRejected=true;}
        assert(imageOrderRejected,"Swapping two overlapping differently painted bitmaps is rejected even when sorted bitmap snapshots are unchanged");
        source.RequireSame(IsolatedSpacingPreview.Capture(Page()));
        bool unchangedRejected=false;try{source.RequireSame(singlePreview);}catch(InvalidOperationException){unchangedRejected=true;}
        assert(unchangedRejected,"Trial support does not weaken the stricter unchanged-copy baseline requirement");
    }
}
