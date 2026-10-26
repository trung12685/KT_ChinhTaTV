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
        var rejected=new (string Name,Action<Canvas,Glyphs> Set)[]{
            ("glyph opacity",(c,g)=>g.Opacity=0),
            ("ancestor opacity",(c,g)=>c.Opacity=0),
            ("hidden glyph",(c,g)=>g.Visibility=Visibility.Hidden),
            ("collapsed ancestor",(c,g)=>c.Visibility=Visibility.Collapsed),
            ("transparent fill",(c,g)=>g.Fill=Brushes.Transparent),
            ("missing fill",(c,g)=>g.Fill=null),
            ("transparent mask",(c,g)=>g.OpacityMask=Brushes.Transparent),
            ("nonuniform mask",(c,g)=>c.OpacityMask=new LinearGradientBrush(Colors.Transparent,Colors.Black,0)),
            ("unsupported paint",(c,g)=>g.Fill=new LinearGradientBrush(Colors.Black,Colors.Red,0)),
            ("partial glyph clip",(c,g)=>g.Clip=new RectangleGeometry(new Rect(100,70,5,50))),
            ("partial ancestor clip",(c,g)=>c.Clip=new RectangleGeometry(new Rect(100,70,5,50))),
            ("partial layout clip",(c,g)=>{c.Width=150;c.Height=200;c.ClipToBounds=true;}),
            ("outside physical page",(c,g)=>g.RenderTransform=new TranslateTransform(1000,0)),
            ("rotated glyph",(c,g)=>g.RenderTransform=new RotateTransform(5)),
            ("skewed glyph",(c,g)=>g.RenderTransform=new SkewTransform(5,0)),
            ("horizontal mirror",(c,g)=>g.RenderTransform=new TransformGroup{Children={new ScaleTransform(-1,1),new TranslateTransform(500,0)}}),
            ("vertical mirror",(c,g)=>g.RenderTransform=new TransformGroup{Children={new ScaleTransform(1,-1),new TranslateTransform(0,250)}}),
            ("clip with an internal hole",(c,g)=>g.Clip=Geometry.Parse("F0 M 80,50 L 430,50 430,160 80,160 Z M 145,50 L 155,50 155,160 145,160 Z"))
        };
        foreach(var (name,set) in rejected)
        {
            var page=Page(set);var preview=new PreviewHighlighter();
            assert(ReferenceMeasurement.Read(page,preview,Anchor) is null && preview.MeasureSingleLine(page,Anchor) is null,
                "Line and administrative-row geometry both require complete shown text: "+name);
            assert(preview.TableFrame(page,Anchor) is null,"A clip frame alone cannot authorize correction using an unshown reference: "+name);
        }
        var positive=new (string Name,Action<Canvas,Glyphs> Set)[]{
            ("full glyph clip",(c,g)=>g.Clip=new RectangleGeometry(new Rect(80,60,350,100))),
            ("full ancestor clip",(c,g)=>c.Clip=new RectangleGeometry(new Rect(80,60,350,100))),
            ("translated clip geometry",(c,g)=>g.Clip=new RectangleGeometry(new Rect(0,0,350,100)){Transform=new TranslateTransform(80,60)}),
            ("full layout clip",(c,g)=>{c.Width=450;c.Height=200;c.ClipToBounds=true;}),
            ("containing nonrectangular clip",(c,g)=>g.Clip=new EllipseGeometry(new Point(200,100),200,60)),
            ("known partial opacity",(c,g)=>{c.Opacity=.5;g.Opacity=.6;g.Fill=new SolidColorBrush(Colors.Black){Opacity=.8};g.OpacityMask=new SolidColorBrush(Colors.White){Opacity=.7};})
        };
        var baseline=ReferenceMeasurement.Read(Page(),new PreviewHighlighter(),Anchor)!;
        foreach(var (name,set) in positive)
        {
            var page=Page(set);var preview=new PreviewHighlighter();var measured=ReferenceMeasurement.Read(page,preview,Anchor);
            assert(measured is not null && Math.Abs(measured.Left-baseline.Left)<.001 && Math.Abs(measured.Width-baseline.Width)<.001 &&
                preview.MeasureSingleLine(page,Anchor) is not null,"Fully exposed valid text retains the same measured reference: "+name);
        }
        var movedPage=Page((c,g)=>c.RenderTransform=new TransformGroup{Children={new ScaleTransform(4d/3,4d/3),new TranslateTransform(40,20)}});
        var moved=ReferenceMeasurement.Read(movedPage,new PreviewHighlighter(),Anchor);
        assert(moved is not null && Math.Abs(moved.Left-baseline.Left-30)<.001 && Math.Abs(moved.Bottom-baseline.Bottom-15)<.001,
            "Reference coordinates apply the ancestor transform once and remain in page points");
        var framePage=Page((c,g)=>c.Clip=new RectangleGeometry(new Rect(80,60,350,100)));
        assert(new PreviewHighlighter().TableFrame(framePage,Anchor) is not null,"A real rectangular frame remains available around a fully visible reference");
        var rotatedPage=Page((c,g)=>g.RenderTransform=new RotateTransform(5));
        assert(new PreviewHighlighter().Measure(rotatedPage,Anchor).Count>0 &&
            ReferenceMeasurement.Read(rotatedPage,new PreviewHighlighter(),Anchor) is null,
            "Bounding highlights remain available while rotated geometry cannot authorize a horizontal Line");
        const string first="Độc lập – ";string second=Text[first.Length..];
        foreach(bool hideSecond in new[]{false,true})
        {
            var split=Page((c,g)=>{
                g.UnicodeString=first;double width=g.ToGlyphRun().AdvanceWidths.Sum();
                c.Children.Add(new Glyphs{FontUri=g.FontUri,FontRenderingEmSize=14,OriginX=100+width,OriginY=100,
                    UnicodeString=second,Fill=Brushes.Black,Opacity=hideSecond?0:1});});
            var measured=ReferenceMeasurement.Read(split,new PreviewHighlighter(),Anchor);
            assert(hideSecond?measured is null:measured is not null,
                hideSecond?"A hidden final run cannot turn the remaining visible prefix into a shorter reference":"A complete reference split over visible runs remains measurable");
        }
        const string extra=" EXTRA";
        var unrelated=Page((c,g)=>c.Children.Add(new Glyphs{FontUri=g.FontUri,FontRenderingEmSize=14,OriginX=100,OriginY=150,UnicodeString=extra,Fill=Brushes.Transparent}));
        assert(ReferenceMeasurement.Read(unrelated,new PreviewHighlighter(),new(1,Text+extra,0,Text.Length)) is not null,
            "Unselected hidden text elsewhere does not invalidate an independently proven selected reference");
        Glyphs? italic=null;
        var italicPage=Page((c,g)=>{italic=g;g.UnicodeString="j";g.FontRenderingEmSize=30;g.FontUri=new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"ariali.ttf"));});
        var italicAnchor=new PreviewAnchor(1,"j",0,1);var italicRun=italic!.ToGlyphRun();
        assert(ReferenceMeasurement.Read(italicPage,new PreviewHighlighter(),italicAnchor) is not null,"Unclipped italic ink remains measurable without assuming the advance box equals the painted glyph");
        var alignment=italicRun.ComputeAlignmentBox();var ink=italicRun.ComputeInkBoundingBox();ink.Offset(italicRun.BaselineOrigin.X,italicRun.BaselineOrigin.Y);
        var advanceBox=new Rect(italicRun.BaselineOrigin.X,italicRun.BaselineOrigin.Y+alignment.Y,Math.Max(2,italicRun.AdvanceWidths.Sum()),Math.Max(2,alignment.Height));
        assert(!advanceBox.Contains(ink),"The italic fixture has real ink outside its advance box");
        italic.Clip=new RectangleGeometry(advanceBox);
        assert(ReferenceMeasurement.Read(italicPage,new PreviewHighlighter(),italicAnchor) is null,
            "Clipping italic overhang cannot pass merely because the advance box still fits");
        var badReferencePage=Page((c,g)=>c.Opacity=0);
        var target=new LineTarget(LineRole.Motto,0,Text,0,Text.Length+1,true);
        var anchors=new Dictionary<Issue,PreviewAnchor>{{target.Probe,Anchor}};
        var previous=target.Probe with{CanFix=true,LineFix=new(1,100,120,80,LineRole.Motto)};
        var notice=LineInspection.Inspect(badReferencePage,new PreviewHighlighter(),[target],[],anchors,[],LinePolicy.Decree30,false,[previous]).Single().Issue;
        assert(!notice.CanFix && notice.LineFix is null,"A hidden reference cannot authorize Line creation or be concealed by prior verified edit history");
        string folder=Path.Combine(Path.GetTempPath(),"ChinhTaReferenceProof-"+Guid.NewGuid());Directory.CreateDirectory(folder);
        try
        {
            string current=Path.Combine(folder,"working"),candidate=Path.Combine(folder,"candidate");
            File.WriteAllText(current,"before");File.WriteAllText(candidate,"hidden reference");int opens=0;bool failed=false;
            try{CheckpointSwap.Apply(current,candidate,()=>{},()=>{if(++opens==1 && ReferenceMeasurement.Read(badReferencePage,new PreviewHighlighter(),Anchor) is null)
                throw new InvalidOperationException("Reference no longer shown");});}catch(InvalidOperationException){failed=true;}
            assert(failed && opens==2 && File.ReadAllText(current)=="before","Losing shown reference text after reopening the candidate restores the whole checkpoint");
        }
        finally{Directory.Delete(folder,true);}
    }
}
