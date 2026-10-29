using System.Globalization;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Controls;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

// A diagnostic proof stricter than PAGE preservation: all pages and every glyph
// baseline/advance/cluster/font, plus existing paint/bitmap/path verification.
internal sealed class IsolatedSpacingPreview
{
    private readonly IsolatedLayoutProof layout;
    private readonly PageNumberVisualSnapshot artwork;
    private readonly string[] drawingOrder;
    internal int Pages=>layout.PageCount;
    internal int GlyphRuns=>layout.GlyphRuns;
    private IsolatedSpacingPreview(IsolatedLayoutProof layout,PageNumberVisualSnapshot artwork,string[] drawingOrder)
    {this.layout=layout;this.artwork=artwork;this.drawingOrder=drawingOrder;}
    private static IEnumerable<DependencyObject> Walk(DependencyObject root)
    {
        yield return root;
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)
            foreach(var child in Walk(VisualTreeHelper.GetChild(root,i)))yield return child;
    }
    private static string Number(double value)
    {
        if(!double.IsFinite(value))throw new InvalidOperationException("Glyph/baseline XPS không hữu hạn.");
        return value.ToString("R",CultureInfo.InvariantCulture);
    }
    internal static IsolatedSpacingPreview Capture(FixedDocumentSequence sequence)
    {
        var artwork=PageNumberPreviewVerifier.CaptureAll(sequence);
        var paginator=sequence.DocumentPaginator;paginator.ComputePageCount();
        var sizes=new List<IsolatedPageSize>();var glyphs=new List<IsolatedGlyphEvidence>();var order=new List<string>();
        var fonts=new Dictionary<GlyphTypeface,string>();
        for(int n=0;n<paginator.PageCount;n++)
        {
            using var rendered=paginator.GetPage(n);
            var page=Walk(rendered.Visual).OfType<FixedPage>().Single();
            if(Math.Abs(rendered.Size.Width-page.Width)>.01 || Math.Abs(rendered.Size.Height-page.Height)>.01)
                throw new InvalidOperationException("Khung trang XPS chưa đồng nhất.");
            sizes.Add(new(page.Width,page.Height));
            foreach(var visual in Walk(page).OfType<Visual>())
            {
                if(visual is Canvas {Background:not null})throw new InvalidOperationException("Nền canvas chưa được chứng minh cho bản sao cô lập.");
                if(visual is System.Windows.Shapes.Path path && !BitmapPageVisualProof.IsBitmapLeaf(path))
                {
                    var transform=PreviewPaintProof.ToPage(path,page);
                    if(transform is null || path.Data is null)throw new InvalidOperationException("Chưa có hình học nét XPS đầy đủ.");
                    var m=transform.Value;
                    order.Add("path:"+path.Data.GetFlattenedPathGeometry(.000001,ToleranceType.Absolute).ToString(CultureInfo.InvariantCulture)+"|"+
                        string.Join(",",new[]{m.M11,m.M12,m.M21,m.M22,m.OffsetX,m.OffsetY}.Select(Number)));
                }
                else if(visual is Glyphs)order.Add("glyph");
                else if(BitmapPageVisualProof.IsBitmapLeaf(visual))order.Add("bitmap");
            }
            foreach(var glyph in Walk(page).OfType<Glyphs>())
            {
                var run=glyph.ToGlyphRun();var transform=PreviewPaintProof.ToPage(glyph,page);
                if(transform is null || string.IsNullOrEmpty(glyph.UnicodeString))
                    throw new InvalidOperationException("Chưa có chữ/glyph XPS để đối chiếu bản sao.");
                var m=transform.Value;
                if(m.M11<=0 || m.M22<=0 || Math.Abs(m.M12)>.000001 || Math.Abs(m.M21)>.000001 || run.IsSideways || run.BidiLevel!=0)
                    throw new InvalidOperationException("Glyph xoay/lật hoặc hướng chữ chưa hỗ trợ hiệu chuẩn spacing.");
                if(!fonts.TryGetValue(run.GlyphTypeface,out string? font))
                {
                    using var stream=run.GlyphTypeface.GetFontStream();font=Convert.ToHexString(SHA256.HashData(stream));
                    fonts.Add(run.GlyphTypeface,font);
                }
                string shape=font+"|"+glyph.StyleSimulations+"|"+Number(run.FontRenderingEmSize)+"|"+
                    string.Join(",",new[]{m.M11,m.M12,m.M21,m.M22}.Select(Number))+"|"+
                    string.Join(",",run.GlyphIndices)+"|"+string.Join(",",run.AdvanceWidths.Select(Number))+"|"+
                    string.Join(";",run.GlyphOffsets?.Select(p=>Number(p.X)+","+Number(p.Y))??[])+"|"+
                    string.Join(",",run.ClusterMap??Array.Empty<ushort>())+"|"+string.Join(",",run.CaretStops??Array.Empty<bool>());
                var baseline=transform.Transform(run.BaselineOrigin);
                glyphs.Add(new(n+1,glyph.UnicodeString,shape,baseline.X,baseline.Y));
            }
        }
        return new(new(sizes,glyphs),artwork,order.ToArray());
    }
    internal void RequireSame(IsolatedSpacingPreview copy)
    {
        layout.RequireSame(copy.layout);
        if(!drawingOrder.SequenceEqual(copy.drawingOrder))
            throw new InvalidOperationException("Bản sao đổi hình học nét hoặc thứ tự vẽ XPS; không hiệu chuẩn spacing.");
        PageNumberPreviewVerifier.Preserve(artwork,copy.artwork);
        // Existing PAGE proof permits .5 XPS units. Clone calibration requires
        // matching positions and dimensions more strictly, including artwork.
        for(int i=0;i<artwork.Body.Count;i++)
        {
            var a=artwork.Body[i].Bounds;var b=copy.artwork.Body[i].Bounds;
            if(new[]{a.X-b.X,a.Y-b.Y,a.Width-b.Width,a.Height-b.Height}.Any(d=>!double.IsFinite(d) || Math.Abs(d)>.01))
                throw new InvalidOperationException("Bản sao làm đổi vị trí/kích thước nội dung XPS; không hiệu chuẩn spacing.");
        }
    }
}
