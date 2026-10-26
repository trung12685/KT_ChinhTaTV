using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using ChinhTa.Desktop;
using ChinhTa.Core;
using Path=System.Windows.Shapes.Path;

static class VisibleStrokeChecks
{
    private const string Text="Độc lập – Tự do – Hạnh phúc";
    private static FixedDocumentSequence Page(Action<Canvas,Path>? setup=null)
    {
        var page=new FixedPage{Width=800,Height=1100};
        var root=new Canvas{RenderTransform=new ScaleTransform(4d/3,4d/3)};page.Children.Add(root);
        var path=new Path{Data=new System.Windows.Media.LineGeometry(new Point(100,120),new Point(180,120)),Stroke=Brushes.Black,StrokeThickness=.5};root.Children.Add(path);
        root.Children.Add(new Glyphs{FontUri=new Uri(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"arial.ttf")),
            FontRenderingEmSize=14,OriginX=100,OriginY=80,UnicodeString=Text,Fill=Brushes.Black});
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
        var invisible=new (string Name,Action<Canvas,Path> Set)[]{
            ("path opacity",(c,p)=>p.Opacity=0),
            ("hidden path",(c,p)=>p.Visibility=Visibility.Hidden),
            ("collapsed ancestor",(c,p)=>c.Visibility=Visibility.Collapsed),
            ("brush opacity",(c,p)=>p.Stroke=new SolidColorBrush(Colors.Black){Opacity=0}),
            ("transparent mask",(c,p)=>p.OpacityMask=Brushes.Transparent),
            ("transparent gradient",(c,p)=>p.Stroke=new LinearGradientBrush(Colors.Transparent,Colors.Transparent,0)),
            ("zero stroke thickness",(c,p)=>p.StrokeThickness=0),
            ("path clip outside",(c,p)=>p.Clip=new RectangleGeometry(new Rect(0,0,20,20))),
            ("outside physical page",(c,p)=>p.RenderTransform=new TranslateTransform(1000,0)),
            ("layout clip outside",(c,p)=>{c.Width=90;c.Height=200;c.ClipToBounds=true;}),
            ("hidden dominates unknown mask",(c,p)=>{c.Opacity=0;p.OpacityMask=new LinearGradientBrush(Colors.Transparent,Colors.Black,0);})
        };
        foreach(var (name,set) in invisible)
        {
            var evidence=new PreviewHighlighter().ReadHorizontalLines(Page(set),1);
            assert(evidence.Lines.Count==0 && !evidence.HasUnverifiedPaths,"Known invisible evidence is absent, not invented or uncertain: "+name);
        }
        var visible=new (string Name,Action<Canvas,Path> Set)[]{
            ("full path clip",(c,p)=>p.Clip=new RectangleGeometry(new Rect(90,110,100,20))),
            ("full ancestor clip",(c,p)=>c.Clip=new RectangleGeometry(new Rect(90,110,100,20))),
            ("transformed clip",(c,p)=>p.Clip=new RectangleGeometry(new Rect(0,0,100,20)){Transform=new TranslateTransform(90,110)}),
            ("nonrectangular containing clip",(c,p)=>c.Clip=new EllipseGeometry(new Point(140,120),100,30)),
            ("uniform partial opacity",(c,p)=>{c.Opacity=.4;p.Opacity=.3;p.Stroke=new SolidColorBrush(Colors.Black){Opacity=.5};p.OpacityMask=new SolidColorBrush(Colors.White){Opacity=.8};}),
            ("full layout clip",(c,p)=>{c.Width=250;c.Height=250;c.ClipToBounds=true;})
        };
        foreach(var (name,set) in visible)
        {
            var evidence=new PreviewHighlighter().ReadHorizontalLines(Page(set),1);
            assert(!evidence.HasUnverifiedPaths && evidence.Lines.Count==1 &&
                RenderedLineVerifier.Find(new(100,120,80),evidence.Lines) is not null,"Complete painted strokes keep page coordinates: "+name);
        }
        var filled=new PreviewHighlighter().ReadHorizontalLines(Page((c,p)=>{p.Stroke=null;p.Fill=Brushes.Black;p.Data=new RectangleGeometry(new Rect(100,120,80,.5));}),1);
        assert(!filled.HasUnverifiedPaths && filled.Lines.Count==1 && RenderedLineVerifier.Find(new(100,120.25,80),filled.Lines) is not null,
            "A genuinely filled thin rectangle still establishes the Word Line representation");
        assert(Count((c,p)=>{p.Fill=Brushes.Black;p.Data=new RectangleGeometry(new Rect(100,120,80,.5));})==1,
            "Fill and stroke painting on the same thin figure are unioned, not cancelled by an even-odd group");
        assert(Count((c,p)=>{p.Stroke=null;p.Fill=Brushes.Black;p.Data=Geometry.Parse("F0 M 100,120 L 180,120 180,120.5 100,120.5 Z M 100,140 L 180,140 180,140.5 100,140.5 Z");})==2,
            "Separate filled bars in one multi-figure Path remain separate painted occurrences");
        assert(Count((c,p)=>{p.Stroke=null;p.Fill=Brushes.Black;p.Data=Geometry.Parse("F0 M 100,120 L 180,120 180,120.5 100,120.5 Z M 100,120 L 180,120 180,120.5 100,120.5 Z");})==0,
            "Identical even-odd filled figures that cancel each other cannot invent two visible Lines");
        var moved=new PreviewHighlighter().ReadHorizontalLines(Page((c,p)=>{
            c.RenderTransform=new TransformGroup{Children={new ScaleTransform(4d/3,4d/3),new TranslateTransform(40,20)}};
            c.Clip=new RectangleGeometry(new Rect(90,110,100,20));}),1);
        assert(!moved.HasUnverifiedPaths && RenderedLineVerifier.Find(new(130,135,80),moved.Lines) is not null,
            "Ancestor clipping and translation are evaluated in the same page coordinates as the stroke");
        var dataMoved=new PreviewHighlighter().ReadHorizontalLines(Page((c,p)=>p.Data.Transform=new TranslateTransform(20,3)),1);
        assert(!dataMoved.HasUnverifiedPaths && RenderedLineVerifier.Find(new(120,123,80),dataMoved.Lines) is not null,
            "A Data geometry transform is applied once before the visual/page transform");
        var uncertain=new (string Name,Action<Canvas,Path> Set)[]{
            ("partial path clip",(c,p)=>p.Clip=new RectangleGeometry(new Rect(100,118,40,4))),
            ("partial ancestor clip",(c,p)=>c.Clip=new RectangleGeometry(new Rect(100,118,40,4))),
            ("partial layout clip",(c,p)=>{c.Width=140;c.Height=200;c.ClipToBounds=true;}),
            ("triangle clip bounds are not coverage",(c,p)=>c.Clip=Geometry.Parse("M 90,110 L 190,110 L 90,130 Z")),
            ("hole in clip",(c,p)=>p.Clip=Geometry.Parse("F0 M 90,110 L 190,110 190,130 90,130 Z M 135,110 L 145,110 145,130 135,130 Z")),
            ("nonuniform opacity mask",(c,p)=>p.OpacityMask=new LinearGradientBrush(Colors.Transparent,Colors.Black,0)),
            ("unsupported paint brush",(c,p)=>p.Stroke=new LinearGradientBrush(Colors.Black,Colors.Red,0)),
            ("dashed stroke",(c,p)=>p.StrokeDashArray=new DoubleCollection{2,1})
        };
        foreach(var (name,set) in uncertain)
        {
            var sequence=Page(set);var preview=new PreviewHighlighter();var evidence=preview.ReadHorizontalLines(sequence,1);
            assert(evidence.HasUnverifiedPaths && evidence.Lines.Count==0,"Incomplete paint never becomes a full-length proven stroke: "+name);
            bool blocked=false;try{preview.HorizontalLines(sequence,1);}catch(InvalidOperationException){blocked=true;}
            assert(blocked,"Strict verification cannot silently omit uncertainty and authorize a replacement: "+name);
        }
        var nested=new PreviewHighlighter().ReadHorizontalLines(Page((c,p)=>{
            c.Children.Remove(p);var inner=new Canvas{Clip=new RectangleGeometry(new Rect(90,110,40,20))};inner.Children.Add(p);c.Children.Add(inner);
            c.Clip=new RectangleGeometry(new Rect(150,110,40,20));}),1);
        assert(nested.Lines.Count==0 && !nested.HasUnverifiedPaths,"The intersection of nested clips can prove the entire stroke invisible");
        Path? retained=null;Canvas? parent=null;
        var unchangedPage=Page((c,p)=>{retained=p;parent=c;p.Clip=new RectangleGeometry(new Rect(90,110,100,20));});
        var oldData=retained!.Data;var oldClip=retained.Clip;int children=parent!.Children.Count;
        new PreviewHighlighter().ReadHorizontalLines(unchangedPage,1);
        assert(ReferenceEquals(retained.Data,oldData) && ReferenceEquals(retained.Clip,oldClip) && parent.Children.Count==children,
            "Visibility inspection leaves the source visual tree, geometry and clipping unchanged");
        var partialPage=Page((c,p)=>p.Clip=new RectangleGeometry(new Rect(100,118,40,4)));
        var target=new LineTarget(LineRole.Motto,0,Text,0,Text.Length+1,true);
        var anchors=new Dictionary<Issue,PreviewAnchor>{{target.Probe,new(1,Text,0,Text.Length)}};
        var notice=LineInspection.Inspect(partialPage,new PreviewHighlighter(),[target],[],anchors,[],LinePolicy.Decree30,false).Single().Issue;
        assert(!notice.CanFix && notice.LineFix is null && notice.Message.Contains("cắt"),
            "A partial stroke becomes manual review, never a missing-Line creation in the issue list");
        var accepted=target.Probe with {CanFix=true,LineFix=new(1,100,120,80,LineRole.Motto)};
        var historyNotice=LineInspection.Inspect(partialPage,new PreviewHighlighter(),[target],[],anchors,[],LinePolicy.Decree30,false,[accepted]).Single().Issue;
        assert(!historyNotice.CanFix && historyNotice.Message.Contains("cắt"),"Verified edit history cannot hide an incomplete painted stroke");
        string folder=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"ChinhTaVisibleStroke-"+Guid.NewGuid());System.IO.Directory.CreateDirectory(folder);
        try
        {
            string current=System.IO.Path.Combine(folder,"working"),candidate=System.IO.Path.Combine(folder,"candidate");
            System.IO.File.WriteAllText(current,"before");System.IO.File.WriteAllText(candidate,"uncertain");int opens=0;bool rejected=false;
            try{CheckpointSwap.Apply(current,candidate,()=>{},()=>{if(++opens==1)new PreviewHighlighter().HorizontalLines(partialPage,1);});}
            catch(InvalidOperationException){rejected=true;}
            assert(rejected && opens==2 && System.IO.File.ReadAllText(current)=="before","Unknown visible-stroke evidence after candidate reopen restores the complete checkpoint");
        }
        finally{System.IO.Directory.Delete(folder,true);}
    }
}
