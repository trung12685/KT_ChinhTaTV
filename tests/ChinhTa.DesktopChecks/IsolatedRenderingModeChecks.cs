using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChinhTa.Desktop;
using PathShape=System.Windows.Shapes.Path;

public static class IsolatedRenderingModeChecks
{
    private static readonly string[] Texts=["Nội dung thứ nhất ","dòng thứ hai ","dòng thứ ba.","Phần sau."];
    private static string Target=>string.Concat(Texts.Take(3));
    private sealed record Fixture(FixedDocumentSequence Sequence,FixedPage Page,Canvas Canvas,Glyphs[] Glyphs,PathShape Path);
    private static Fixture Create(bool trial=false,Action<Fixture>? change=null)
    {
        var page=new FixedPage{Width=640,Height=420,Background=Brushes.White};
        var canvas=new Canvas();page.Children.Add(canvas);
        double[] baselines=trial?[100.25,118.25,136.25,188.25]:[100.25,124.25,148.25,200.25];
        var glyphs=Texts.Select((text,index)=>new Glyphs
        {
            FontUri=new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"times.ttf")),
            FontRenderingEmSize=18.25,OriginX=60.25,OriginY=baselines[index],UnicodeString=text,Fill=Brushes.Black
        }).ToArray();
        foreach(var glyph in glyphs)canvas.Children.Add(glyph);
        var path=new PathShape{Data=Geometry.Parse("M 400.25,80.25 L 470.25,113.25 L 400.25,146.25 Z"),Fill=Brushes.Blue};
        canvas.Children.Add(path);
        var content=new PageContent();((IAddChild)content).AddChild(page);
        var document=new FixedDocument();document.Pages.Add(content);var reference=new DocumentReference();reference.SetDocument(document);
        var sequence=new FixedDocumentSequence();sequence.References.Add(reference);
        var fixture=new Fixture(sequence,page,canvas,glyphs,path);change?.Invoke(fixture);
        page.Measure(new Size(page.Width,page.Height));page.Arrange(new Rect(0,0,page.Width,page.Height));page.UpdateLayout();
        return fixture;
    }
    private static byte[] Pixels(Fixture fixture)
    {
        var bitmap=new RenderTargetBitmap((int)fixture.Page.Width,(int)fixture.Page.Height,96,96,PixelFormats.Pbgra32);
        bitmap.Render(fixture.Page);int stride=bitmap.PixelWidth*4;var pixels=new byte[stride*bitmap.PixelHeight];
        bitmap.CopyPixels(pixels,stride,0);return pixels;
    }
    private static bool Reject(Action action)
    {try{action();return false;}catch(InvalidOperationException){return true;}}
    private static void NeutralizeNewEvidence(IsolatedSpacingPreview source,IsolatedSpacingPreview changed)
    {
        // Replay the actual legacy comparator by neutralizing ONLY the evidence
        // introduced in this change. No production bypass or copied proof logic.
        var field=typeof(IsolatedSpacingPreview).GetField("rendering",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var before=(string[])field.GetValue(source)!;var after=(string[])field.GetValue(changed)!;
        if(before.Length!=after.Length)throw new Exception("The legacy probe must retain the same rendering leaves");
        Array.Copy(before,after,before.Length);
    }
    public static void Run(Action<bool,string> assert)
    {
        var fixture=Create();var source=IsolatedSpacingPreview.Capture(fixture.Sequence);
        source.RequireSame(IsolatedSpacingPreview.Capture(Create().Sequence));
        source.RequireSpacingTrial(IsolatedSpacingPreview.Capture(Create(trial:true).Sequence),[Target]);
        assert(true,"The existing unchanged-clone and spacing-trial controls still pass with default XPS rendering options");

        foreach(var sample in new (string Name,Action<Fixture> Change)[]
        {
            ("path-local edge mode",f=>RenderOptions.SetEdgeMode(f.Path,EdgeMode.Aliased)),
            ("canvas-inherited edge mode",f=>RenderOptions.SetEdgeMode(f.Canvas,EdgeMode.Aliased)),
            ("page-inherited edge mode",f=>RenderOptions.SetEdgeMode(f.Page,EdgeMode.Aliased)),
            ("glyph-local text rendering",f=>TextOptions.SetTextRenderingMode(f.Glyphs[1],TextRenderingMode.Aliased)),
            ("canvas-inherited text rendering",f=>TextOptions.SetTextRenderingMode(f.Canvas,TextRenderingMode.Aliased)),
            ("page-inherited text rendering",f=>TextOptions.SetTextRenderingMode(f.Page,TextRenderingMode.Aliased)),
            ("style-derived canvas edge mode",f=>f.Canvas.Style=new Style(typeof(Canvas))
                {Setters={new Setter(RenderOptions.EdgeModeProperty,EdgeMode.Aliased)}}),
            ("style-derived glyph text rendering",f=>f.Glyphs[1].Style=new Style(typeof(Glyphs))
                {Setters={new Setter(TextOptions.TextRenderingModeProperty,TextRenderingMode.Aliased)}}),
            ("style-derived canvas text rendering",f=>f.Canvas.Style=new Style(typeof(Canvas))
                {Setters={new Setter(TextOptions.TextRenderingModeProperty,TextRenderingMode.Aliased)}})
        })
        {
            var changedFixture=Create(change:sample.Change);
            var changed=IsolatedSpacingPreview.Capture(changedFixture.Sequence);
            assert(!Pixels(fixture).SequenceEqual(Pixels(changedFixture)),
                "Actual WPF raster pixels change with geometry, glyph shaping and solid paint retained: "+sample.Name);
            assert(Reject(()=>source.RequireSame(changed)),
                "The unchanged-copy proof rejects rendering-mode drift, including an ancestor setting: "+sample.Name);
            var legacy=IsolatedSpacingPreview.Capture(Create(change:sample.Change).Sequence);NeutralizeNewEvidence(source,legacy);
            source.RequireSame(legacy);
            assert(true,"The actual legacy proof accepted the different paint when only its missing rendering evidence is neutralized: "+sample.Name);
            var trial=IsolatedSpacingPreview.Capture(Create(trial:true,change:sample.Change).Sequence);
            assert(Reject(()=>source.RequireSpacingTrial(trial,[Target])),
                "A permitted baseline translation cannot also whitelist a rendering-mode change: "+sample.Name);
            NeutralizeNewEvidence(source,trial);source.RequireSpacingTrial(trial,[Target]);
            assert(true,"The legacy trial comparator also accepted rendering drift with every existing layout/paint guard retained: "+sample.Name);
        }

        var styled=Create(change:f=>f.Canvas.Style=new Style(typeof(Canvas))
            {Setters={new Setter(RenderOptions.EdgeModeProperty,EdgeMode.Aliased)}});
        assert(styled.Canvas.ReadLocalValue(RenderOptions.EdgeModeProperty)==DependencyProperty.UnsetValue &&
            RenderOptions.GetEdgeMode(styled.Canvas)==EdgeMode.Aliased,
            "The style-derived edge-mode control changes resolved renderer state while its local DP value remains unset");

        foreach(var sample in new (string Name,Action<Fixture> Change)[]
        {
            ("path snapping",f=>f.Path.SnapsToDevicePixels=true),
            ("glyph snapping",f=>f.Glyphs[1].SnapsToDevicePixels=true),
            ("ancestor snapping",f=>f.Canvas.SnapsToDevicePixels=true),
            ("path rounding",f=>f.Path.UseLayoutRounding=true),
            ("glyph rounding",f=>f.Glyphs[1].UseLayoutRounding=true),
            ("ancestor rounding",f=>f.Page.UseLayoutRounding=true),
            ("text hinting",f=>TextOptions.SetTextHintingMode(f.Glyphs[1],TextHintingMode.Animated)),
            ("ancestor text hinting",f=>TextOptions.SetTextHintingMode(f.Canvas,TextHintingMode.Animated)),
            ("text formatting",f=>TextOptions.SetTextFormattingMode(f.Glyphs[1],TextFormattingMode.Display)),
            ("ancestor text formatting",f=>TextOptions.SetTextFormattingMode(f.Canvas,TextFormattingMode.Display)),
            ("ClearType hint",f=>RenderOptions.SetClearTypeHint(f.Glyphs[1],ClearTypeHint.Enabled)),
            ("ancestor ClearType hint",f=>RenderOptions.SetClearTypeHint(f.Page,ClearTypeHint.Enabled))
        })
        {
            assert(Reject(()=>source.RequireSame(IsolatedSpacingPreview.Capture(Create(change:sample.Change).Sequence))),
                "Relevant glyph/path rendering state remains protected without claiming every option changes every backend's pixels: "+sample.Name);
            assert(Reject(()=>source.RequireSpacingTrial(IsolatedSpacingPreview.Capture(Create(trial:true,change:sample.Change).Sequence),[Target])),
                "Spacing trials retain relevant local/ancestor rendering state: "+sample.Name);
        }
        void Nondefault(Fixture f)
        {RenderOptions.SetEdgeMode(f.Canvas,EdgeMode.Aliased);TextOptions.SetTextRenderingMode(f.Page,TextRenderingMode.Aliased);}
        var nondefault=IsolatedSpacingPreview.Capture(Create(change:Nondefault).Sequence);
        nondefault.RequireSame(IsolatedSpacingPreview.Capture(Create(change:Nondefault).Sequence));
        nondefault.RequireSpacingTrial(IsolatedSpacingPreview.Capture(Create(trial:true,change:Nondefault).Sequence),[Target]);
        assert(true,"Consistently retained nondefault path/text rendering modes do not create a blanket calibration ban");

        foreach(bool rounding in new[]{false,true})
        {
            void Override(Fixture f,bool child)
            {
                if(rounding){f.Canvas.UseLayoutRounding=true;f.Glyphs[1].UseLayoutRounding=child;}
                else{f.Canvas.SnapsToDevicePixels=true;f.Glyphs[1].SnapsToDevicePixels=child;}
            }
            var overridden=IsolatedSpacingPreview.Capture(Create(change:f=>Override(f,false)).Sequence);
            assert(Reject(()=>overridden.RequireSame(IsolatedSpacingPreview.Capture(Create(change:f=>Override(f,true)).Sequence))),
                "An explicit false glyph override under a true ancestor cannot collapse into a single OR flag: "+rounding);
            assert(Reject(()=>overridden.RequireSpacingTrial(IsolatedSpacingPreview.Capture(Create(trial:true,change:f=>Override(f,true)).Sequence),[Target])),
                "Spacing trials preserve explicit child overrides under the same true ancestor: "+rounding);
        }
        void TextOverride(Fixture f,TextRenderingMode child)
        {TextOptions.SetTextRenderingMode(f.Canvas,TextRenderingMode.Aliased);TextOptions.SetTextRenderingMode(f.Glyphs[1],child);}
        var textOverride=IsolatedSpacingPreview.Capture(Create(change:f=>TextOverride(f,TextRenderingMode.Auto)).Sequence);
        assert(Reject(()=>textOverride.RequireSame(IsolatedSpacingPreview.Capture(Create(change:f=>TextOverride(f,TextRenderingMode.Aliased)).Sequence))),
            "An explicit default text-mode override under a nondefault ancestor remains distinct from inheriting that mode");

        var neutral=Create(change:f=>
        {
            var wrapper=new Canvas();RenderOptions.SetEdgeMode(wrapper,EdgeMode.Unspecified);
            TextOptions.SetTextRenderingMode(wrapper,TextRenderingMode.Auto);
            wrapper.SnapsToDevicePixels=false;wrapper.UseLayoutRounding=false;
            var children=f.Canvas.Children.Cast<UIElement>().ToArray();f.Canvas.Children.Clear();
            foreach(var child in children)wrapper.Children.Add(child);f.Canvas.Children.Add(wrapper);
        });
        source.RequireSame(IsolatedSpacingPreview.Capture(neutral.Sequence));
        assert(true,"An extra wrapper with neutral default rendering values does not invent clone paint drift");

        var inheritedWrapper=Create(change:f=>
        {
            Nondefault(f);var wrapper=new Canvas();
            var children=f.Canvas.Children.Cast<UIElement>().ToArray();f.Canvas.Children.Clear();
            foreach(var child in children)wrapper.Children.Add(child);f.Canvas.Children.Add(wrapper);
        });
        nondefault.RequireSame(IsolatedSpacingPreview.Capture(inheritedWrapper.Sequence));
        assert(true,"An empty wrapper retains nondefault inherited rendering state without inventing a new override");

        var irrelevant=IsolatedSpacingPreview.Capture(Create(change:f=>
        {
            TextOptions.SetTextRenderingMode(f.Path,TextRenderingMode.Aliased);
            RenderOptions.SetBitmapScalingMode(f.Glyphs[1],BitmapScalingMode.NearestNeighbor);
        }).Sequence);
        source.RequireSame(irrelevant);
        assert(true,"A text-only mode on a path and bitmap-only interpolation on Glyphs do not invent relevant paint drift");
    }
}
