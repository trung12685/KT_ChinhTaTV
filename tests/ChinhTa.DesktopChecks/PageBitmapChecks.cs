using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChinhTa.Desktop;
using Path=System.Windows.Shapes.Path;

public static class PageBitmapChecks
{
    private const double Width=210*96/25.4,Height=297*96/25.4;
    private static byte[] Data()=>[255,0,0,255,0,255,0,255,0,0,255,128,9,8,7,0];
    private static BitmapSource Pixels(byte[]? data=null,int width=2,int height=2,double dpi=96)=>
        BitmapSource.Create(width,height,dpi,dpi,PixelFormats.Bgra32,null,data??Data(),width*4);
    private static Glyphs Glyph(string text,double x,double y,double scale)=>new()
    {
        FontUri=new Uri(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),"times.ttf")),
        FontRenderingEmSize=14*96d/72/scale,UnicodeString=text,Fill=Brushes.Black,OriginX=x/scale,OriginY=y/scale
    };
    private static FixedDocumentSequence Scene(BitmapSource? bitmap=null,bool brush=false,bool relative=false,double opacity=1,
        bool scaled=false,Action<Image>? imageChange=null,Action<Path,ImageBrush>? brushChange=null,
        Action<Canvas>? containerChange=null,ImageSource? alternate=null,double imageTop=250)
    {
        ImageSource source=alternate??bitmap??Pixels();var document=new FixedDocument();double scale=scaled?4d/3:1;
        for(int n=0;n<2;n++)
        {
            var page=new FixedPage{Width=Width,Height=Height};var outer=new Canvas{RenderTransform=new ScaleTransform(scale,scale)};
            outer.Children.Add(Glyph("Nội dung giữ nguyên",100,150,scale));outer.Children.Add(Glyph("Ghi chú cuối trang",100,Height-30,scale));
            if(n==1)
            {
                var numeral=Glyph("2",0,40,scale);double center=(30*96/25.4+Width-15*96/25.4)/2;
                numeral.OriginX=center/scale-numeral.ToGlyphRun().ComputeAlignmentBox().Width/2;outer.Children.Add(numeral);
            }
            var container=new Canvas();containerChange?.Invoke(container);
            if(!brush)
            {
                var image=new Image{Source=source,Width=80/scale,Height=40/scale,Stretch=Stretch.Fill,Opacity=opacity};
                Canvas.SetLeft(image,100/scale);Canvas.SetTop(image,imageTop/scale);imageChange?.Invoke(image);container.Children.Add(image);
            }
            else
            {
                var area=new Rect(100/scale,imageTop/scale,80/scale,40/scale);
                var fill=new ImageBrush(source){TileMode=TileMode.None,Stretch=Stretch.Fill,Opacity=opacity,
                    ViewboxUnits=relative?BrushMappingMode.RelativeToBoundingBox:BrushMappingMode.Absolute,
                    Viewbox=relative?new Rect(0,0,1,1):new Rect(0,0,source.Width,source.Height),
                    ViewportUnits=relative?BrushMappingMode.RelativeToBoundingBox:BrushMappingMode.Absolute,
                    Viewport=relative?new Rect(0,0,1,1):area};
                var path=new Path{Data=new RectangleGeometry(area),Fill=fill};brushChange?.Invoke(path,fill);container.Children.Add(path);
            }
            outer.Children.Add(container);page.Children.Add(outer);
            var content=new PageContent();((IAddChild)content).AddChild(page);document.Pages.Add(content);
        }
        var reference=new DocumentReference();reference.SetDocument(document);var result=new FixedDocumentSequence();result.References.Add(reference);return result;
    }
    private static PageNumberVisualSnapshot Snapshot(FixedDocumentSequence scene)=>PageNumberPreviewVerifier.Capture(scene,20);
    private static bool Reject(Action action)
    {try{action();return false;}catch(InvalidOperationException){return true;}}
    private static void Preserve(PageNumberVisualSnapshot before,FixedDocumentSequence current)=>PageNumberPreviewVerifier.Preserve(before,Snapshot(current));
    public static void Run(Action<bool,string> assert)
    {
        var original=Snapshot(Scene());
        assert(original.Body.Count(v=>v.Kind==BitmapPageVisualProof.Kind)==2,
            "PAGE snapshots retain ordinary body bitmaps on both pages using actual pixel proof rather than an unknown-leaf fallback");
        var upper=Snapshot(Scene(imageTop:5));
        assert(upper.Body.Count(v=>v.Kind==BitmapPageVisualProof.Kind && v.Bounds.Bottom<20*96/25.4)==2,
            "Body bitmap paint wholly inside the upper PAGE band remains in the actual preservation snapshot");
        Preserve(upper,Scene(imageTop:5));Preserve(upper,Scene(brush:true,imageTop:5));
        var upperChanged=Data();upperChanged[0]=0;
        var upperMutations=new[]{Scene(imageTop:5,imageChange:i=>i.Source=null),Scene(imageTop:6),
            Scene(Pixels(upperChanged),imageTop:5),Scene(brush:true,imageTop:5,opacity:0)};
        assert(upperMutations.Select(scene=>Reject(()=>Preserve(upper,scene))).All(rejected=>rejected),
            "Deleting, moving, repainting or hiding upper-band artwork cannot disappear into the PAGE-number exclusion");
        Preserve(original,Scene(Pixels()));
        Preserve(original,Scene(brush:true));
        Preserve(original,Scene(brush:true,relative:true));
        assert(true,"A complete Image and equivalent full-image ImageBrush share the same body preservation proof across absolute and relative mappings");
        Preserve(original,Scene(scaled:true));
        Preserve(original,Scene(brush:true,scaled:true));
        Preserve(original,Scene(Pixels(dpi:300)));
        Preserve(original,Scene(Pixels(dpi:300),brush:true));
        assert(true,"Positive Word-style canvas scaling and non96DPI full-source Viewboxes retain the same displayed bitmap geometry");
        var transparent=Data();transparent[^4]=1;transparent[^3]=2;transparent[^2]=3;
        Preserve(original,Scene(Pixels(transparent)));
        assert(true,"Canonical premultiplied pixels ignore invisible RGB behind alpha0 without discarding visible pixel or alpha evidence");
        var faded=Snapshot(Scene(opacity:.5));Preserve(faded,Scene(brush:true,opacity:.5));
        assert(true,"Equivalent known image transparency remains preserved across Image and ImageBrush representations");
        var uniform=Snapshot(Scene(imageChange:i=>i.Stretch=Stretch.Uniform));Preserve(uniform,Scene(imageChange:i=>i.Stretch=Stretch.Uniform));
        assert(true,"A complete Uniform Image uses its arranged rendered size and retains its actual source aspect ratio");
        PageNumberPreviewVerifier.VerifyNumbers(Scene(brush:true),30,15,20);
        assert(true,"Allowing ordinary body bitmap paint does not weaken rendered header PAGE validation");

        foreach(var mutate in new Action<byte[]>[] {d=>d[0]=0,d=>d[11]=64})
        {
            var data=Data();mutate(data);
            assert(Reject(()=>Preserve(original,Scene(Pixels(data)))),
                "Unchanged image text and bounds cannot hide changed decoded pixels or pixel alpha");
        }
        assert(Reject(()=>Preserve(original,Scene(Pixels(Data(),4,1)))),
            "Changing the bitmap pixel grid cannot masquerade as the original full-image mapping");
        foreach(var scene in new[]
        {
            Scene(opacity:.5),Scene(opacity:0),Scene(brush:true,opacity:.5),Scene(brush:true,opacity:0),
            Scene(containerChange:c=>c.Opacity=.5),Scene(containerChange:c=>c.Visibility=Visibility.Hidden),
            Scene(imageChange:i=>RenderOptions.SetBitmapScalingMode(i,BitmapScalingMode.NearestNeighbor)),
            Scene(containerChange:c=>RenderOptions.SetBitmapScalingMode(c,BitmapScalingMode.HighQuality)),
            Scene(brush:true,brushChange:(p,_)=>RenderOptions.SetEdgeMode(p,EdgeMode.Aliased)),
            Scene(imageChange:i=>i.SnapsToDevicePixels=true)
        })assert(Reject(()=>Preserve(original,scene)),"PAGE preserves the effective visibility, alpha and rendering modes of unselected bitmap artwork");
        foreach(var change in new Action<Image>[]
        {
            i=>i.Stretch=Stretch.UniformToFill,
            i=>i.Clip=new RectangleGeometry(new Rect(0,0,40,40)),
            i=>i.RenderTransform=new MatrixTransform(-1,0,0,1,80,0),
            i=>i.RenderTransform=new RotateTransform(15),
            i=>i.OpacityMask=new LinearGradientBrush(Colors.White,Colors.Black,0)
        })assert(Reject(()=>Preserve(original,Scene(imageChange:change))),
            "Cropped, clipped, mirrored, rotated or unproven masked Image content stays outside bitmap PAGE proof");
        var invalidBrushes=new Action<Path,ImageBrush>[]
        {
            (_,b)=>b.TileMode=TileMode.Tile,
            (_,b)=>b.Stretch=Stretch.Uniform,
            (_,b)=>b.Viewbox=new Rect(0,0,1,1),
            (_,b)=>b.Viewport=new Rect(101,250,80,40),
            (_,b)=>b.Transform=new TranslateTransform(1,0),
            (_,b)=>b.RelativeTransform=new ScaleTransform(1.1,1),
            (p,_)=>p.Data=new EllipseGeometry(new Rect(100,250,80,40)),
            (p,_)=>p.Stroke=Brushes.Black,
            (p,_)=>p.Stretch=Stretch.Fill,
            (p,_)=>p.Clip=new RectangleGeometry(new Rect(100,250,40,40))
        };
        foreach(var change in invalidBrushes)
            assert(Reject(()=>Preserve(original,Scene(brush:true,brushChange:change))),
                "An ImageBrush crop, tile, mapping, stroke or incomplete painted rectangle cannot pass body preservation");
        var unknown=new DrawingImage(new GeometryDrawing(Brushes.Red,null,new RectangleGeometry(new Rect(0,0,2,2))));
        assert(Reject(()=>Preserve(original,Scene(alternate:unknown))),"DrawingImage is not accepted as decoded BitmapSource proof");
        var highDepth=BitmapSource.Create(2,2,96,96,PixelFormats.Rgb48,null,new byte[24],12);
        assert(Reject(()=>Snapshot(Scene(highDepth))),
            "RGB48 body bitmap remains manual because conversion to eight-bit Pbgra32 would discard source color evidence");
        var floating=BitmapSource.Create(2,2,96,96,PixelFormats.Rgba128Float,null,new byte[64],32);
        assert(Reject(()=>Snapshot(Scene(floating))),
            "Floating-point body bitmap remains manual because clipping or quantization could hide changed source pixels");
        var tooWide=Pixels(new byte[16385*4],16385,1);
        assert(Reject(()=>Preserve(original,Scene(tooWide))),"Bitmap proof rejects dimensions outside its bounded decoded-image scope before allocating pixel rows");

        string folder=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"ChinhTa-bitmap-proof-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try
        {
            var names=new[]{System.IO.Path.Combine(folder,"first.png"),System.IO.Path.Combine(folder,"renamed.png")};
            foreach(string name in names)
            {
                var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(Pixels()));
                using var output=File.Create(name);encoder.Save(output);
            }
            BitmapImage Read(string path,bool ignoreProfile)
            {
                var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;
                image.CreateOptions=ignoreProfile?BitmapCreateOptions.IgnoreColorProfile:BitmapCreateOptions.None;
                image.UriSource=new Uri(path);image.EndInit();image.Freeze();return image;
            }
            var first=Read(names[0],true);var renamed=Read(names[1],true);
            var uriBefore=Snapshot(Scene(first));Preserve(uriBefore,Scene(renamed));
            assert(first.UriSource!=renamed.UriSource,"Renaming or repackaging an image URI preserves its decoded-pixel and destination evidence");
            assert(Reject(()=>Preserve(original,Scene(Read(names[0],false)))),
                "Implicit BitmapImage color-profile behavior remains unsupported rather than guessing color equivalence");
        }
        finally{Directory.Delete(folder,true);}
    }
}
