using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Path=System.Windows.Shapes.Path;

namespace ChinhTa.Desktop;

// Body artwork proof for PAGE preservation, not signature recognition. Compare
// decoded premultiplied pixels: XPS can rename or re-encode a resource without
// changing its appearance. Only complete, non-tiled, positive-axis mappings are
// admitted. Unknown source/color/mapping states remain manual.
internal static class BitmapPageVisualProof
{
    internal const string Kind="Bitmap";
    private const int MaximumDimension=16384;
    private const long MaximumPixels=16_777_216;
    internal static bool IsBitmapLeaf(Visual leaf)=>leaf is Image or Path {Fill:ImageBrush};
    internal static (Rect Bounds,string Paint)? Read(Visual leaf,FixedPage page)=>leaf switch
    {
        Image image=>ReadImage(image,page),
        Path {Fill:ImageBrush brush} path=>ReadBrush(path,brush,page),
        _=>throw Unknown("Loại ảnh XPS chưa được chứng minh.")
    };
    private static InvalidOperationException Unknown(string message,Exception? inner=null)=>
        new(message+" Giữ thủ công để bảo toàn ảnh khi sửa PAGE.",inner);
    private static bool Finite(Rect r)=>!r.IsEmpty && r.Width>0 && r.Height>0 &&
        new[]{r.X,r.Y,r.Width,r.Height}.All(double.IsFinite);
    private static bool Same(Rect a,Rect b)=>Finite(a) && Finite(b) &&
        new[]{a.X-b.X,a.Y-b.Y,a.Width-b.Width,a.Height-b.Height}.All(v=>Math.Abs(v)<=.000001);
    private static double Opacity(Visual leaf,FixedPage page)
    {
        var visibility=PreviewPaintProof.VisibilityOf(leaf,page);
        if(visibility==PreviewPaintProof.Paint.None)return 0;
        var opacity=PreviewPaintProof.EffectiveOpacity(leaf,page);
        if(visibility!=PreviewPaintProof.Paint.Uniform || opacity is null || opacity<0 || opacity>1)
            throw Unknown("Chưa chứng minh được opacity/mask của ảnh.");
        return opacity.Value;
    }
    private static Transform Mapping(Visual leaf,FixedPage page)
    {
        var transform=PreviewPaintProof.ToPage(leaf,page);
        if(transform is null)throw Unknown("Chưa đọc được tọa độ ảnh.");
        var m=transform.Value;
        if(m.M11<=0 || m.M22<=0 || Math.Abs(m.M12)>.000001 || Math.Abs(m.M21)>.000001)
            throw Unknown("Ảnh xoay/lật hoặc chưa cùng trục trang.");
        return transform;
    }
    private static (Rect Bounds,string Paint) Complete(Visual leaf,FixedPage page,Rect area,BitmapSource source,double opacity)
    {
        if(!Finite(area))throw Unknown("Chưa có vùng ảnh hữu hạn.");
        var transform=Mapping(leaf,page);
        var painted=PreviewPaintProof.InPage(new RectangleGeometry(area),transform);var bounds=painted.Bounds;
        if(!Finite(bounds) || !PreviewPaintProof.FullyExposed(painted,leaf,page))
            throw Unknown("Ảnh bị cắt hoặc chưa hiển thị trọn.");
        return (bounds,"bitmap:"+Pixels(source)+"|alpha:"+opacity.ToString("0.########",CultureInfo.InvariantCulture)+"|full|"+Rendering(leaf,page));
    }
    private static string Rendering(Visual leaf,FixedPage page)
    {
        // Source pixels alone cannot prove the displayed resampling. A changed
        // inherited interpolation or pixel-snapping mode can repaint an image
        // even when its source hash and destination rectangle are unchanged.
        BitmapScalingMode scaling=BitmapScalingMode.Unspecified;EdgeMode edge=EdgeMode.Unspecified;
        bool snap=false;
        for(DependencyObject? node=leaf;node is Visual visual;node=VisualTreeHelper.GetParent(node))
        {
            if(scaling==BitmapScalingMode.Unspecified)scaling=RenderOptions.GetBitmapScalingMode(visual);
            if(edge==EdgeMode.Unspecified)edge=RenderOptions.GetEdgeMode(visual);
            if(node is UIElement {SnapsToDevicePixels:true})snap=true;
            if(ReferenceEquals(node,page))return scaling+"|"+edge+"|snap:"+snap;
        }
        throw Unknown("Chưa xác định được cách vẽ ảnh trên trang.");
    }
    private static BitmapSource Bitmap(ImageSource source)
    {
        if(source is not BitmapSource bitmap || bitmap.IsDownloading ||
            bitmap.PixelWidth<=0 || bitmap.PixelHeight<=0 || bitmap.PixelWidth>MaximumDimension || bitmap.PixelHeight>MaximumDimension ||
            (long)bitmap.PixelWidth*bitmap.PixelHeight>MaximumPixels ||
            !double.IsFinite(bitmap.Width) || !double.IsFinite(bitmap.Height) || bitmap.Width<=0 || bitmap.Height<=0 ||
            !double.IsFinite(bitmap.DpiX) || !double.IsFinite(bitmap.DpiY) || bitmap.DpiX<=0 || bitmap.DpiY<=0)
            throw Unknown("Nguồn ảnh chưa giải mã hoặc ngoài giới hạn kiểm bitmap.");
        // Canonical Pbgra32 conversion is only an appearance proof for these
        // ordinary eight-bit sources. Wide/float pixels can differ below that
        // conversion's precision; they require independent source evidence.
        if(!new[]{PixelFormats.Pbgra32,PixelFormats.Bgra32,PixelFormats.Bgr32,PixelFormats.Bgr24,PixelFormats.Rgb24,PixelFormats.Gray8}.Contains(bitmap.Format))
            throw Unknown("Độ sâu/không gian pixel ảnh chưa được chứng minh không mất dữ liệu khi đối chiếu.");
        // A decoded frame exposes its embedded profiles; conversion sources and
        // BitmapImage's implicit profile handling need a separate color proof.
        if(bitmap is ColorConvertedBitmap || bitmap is BitmapFrame {ColorContexts.Count:>0} ||
            bitmap is BitmapImage image && (image.CreateOptions&BitmapCreateOptions.IgnoreColorProfile)==0)
            throw Unknown("Profile màu ảnh chưa được đối chiếu.");
        return bitmap;
    }
    private static string Pixels(BitmapSource source)
    {
        try
        {
            BitmapSource pixels=source.Format==PixelFormats.Pbgra32?source:new FormatConvertedBitmap(source,PixelFormats.Pbgra32,null,0);
            int stride=checked(pixels.PixelWidth*4);var row=new byte[stride];
            using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            hash.AppendData(Encoding.ASCII.GetBytes($"Pbgra32|{pixels.PixelWidth}|{pixels.PixelHeight}|"));
            for(int y=0;y<pixels.PixelHeight;y++)
            {
                pixels.CopyPixels(new Int32Rect(0,y,pixels.PixelWidth,1),row,stride,0);hash.AppendData(row);
            }
            return Convert.ToHexString(hash.GetHashAndReset());
        }
        catch(Exception error) when(error is ArgumentException or InvalidOperationException or NotSupportedException or System.IO.IOException)
        {throw Unknown("Chưa đọc đủ pixel ảnh đã giải mã.",error);}
    }
    private static (Rect Bounds,string Paint)? ReadImage(Image image,FixedPage page)
    {
        double opacity=Opacity(image,page);if(opacity==0 || image.Source is null)return null;
        if(image.Stretch==Stretch.UniformToFill || image.Stretch is not (Stretch.None or Stretch.Fill or Stretch.Uniform))
            throw Unknown("Ảnh có chế độ cắt để lấp đầy chưa được hỗ trợ.");
        var source=Bitmap(image.Source);
        // WPF Image.OnRender draws the complete source into RenderSize, which
        // is the size computed by ArrangeOverride. Any layout clip is checked
        // by the shared exposure proof before this snapshot can be retained.
        return Complete(image,page,new Rect(new Point(),image.RenderSize),source,opacity);
    }
    private static (Rect Bounds,string Paint)? ReadBrush(Path path,ImageBrush brush,FixedPage page)
    {
        if(path.Stroke is not null || path.Stretch!=Stretch.None || path.Data is null)
            throw Unknown("ImageBrush có nét viền hoặc hình giãn chưa được hỗ trợ.");
        double opacity=Opacity(path,page);
        if(!double.IsFinite(brush.Opacity) || brush.Opacity<0 || brush.Opacity>1)
            throw Unknown("Opacity ImageBrush chưa rõ.");
        opacity*=brush.Opacity;if(opacity==0 || brush.ImageSource is null)return null;
        if(brush.TileMode!=TileMode.None || brush.Stretch!=Stretch.Fill ||
            brush.Transform?.Value.IsIdentity==false || brush.RelativeTransform?.Value.IsIdentity==false)
            throw Unknown("ImageBrush có tile/transform hoặc cách trải ảnh chưa hỗ trợ.");
        if(!Finite(brush.Viewbox) || !Finite(brush.Viewport))throw Unknown("Viewbox/Viewport ảnh chưa hữu hạn.");
        var source=Bitmap(brush.ImageSource);var area=path.Data.Bounds;
        if(!Finite(area))throw Unknown("Chưa có khung ImageBrush rõ.");
        var rectangle=new RectangleGeometry(area);
        if(PreviewPaintProof.Combine(rectangle,path.Data,GeometryCombineMode.Exclude).GetArea(PreviewPaintProof.GeometryTolerance,ToleranceType.Absolute)>PreviewPaintProof.AreaTolerance ||
            PreviewPaintProof.Combine(path.Data,rectangle,GeometryCombineMode.Exclude).GetArea(PreviewPaintProof.GeometryTolerance,ToleranceType.Absolute)>PreviewPaintProof.AreaTolerance)
            throw Unknown("ImageBrush chưa phủ đúng một hình chữ nhật đầy đủ.");
        var fullSource=brush.ViewboxUnits switch
        {BrushMappingMode.Absolute=>new Rect(0,0,source.Width,source.Height),BrushMappingMode.RelativeToBoundingBox=>new Rect(0,0,1,1),_=>Rect.Empty};
        if(!Same(brush.Viewbox,fullSource))throw Unknown("ImageBrush đang lấy một phần ảnh hoặc Viewbox chưa rõ.");
        var viewport=brush.ViewportUnits switch
        {
            BrushMappingMode.Absolute=>brush.Viewport,
            BrushMappingMode.RelativeToBoundingBox=>new Rect(area.X+brush.Viewport.X*area.Width,area.Y+brush.Viewport.Y*area.Height,
                brush.Viewport.Width*area.Width,brush.Viewport.Height*area.Height),
            _=>Rect.Empty
        };
        if(!Same(viewport,area))throw Unknown("ImageBrush chưa ánh xạ trọn ảnh đúng khung vẽ.");
        return Complete(path,page,area,source,opacity);
    }
}
