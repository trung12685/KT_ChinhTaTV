using System.Globalization;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Controls;
using System.Text;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

// A diagnostic proof stricter than PAGE preservation: all pages and every glyph
// baseline/advance/cluster/font, plus existing paint/bitmap/path verification.
internal sealed class IsolatedSpacingPreview
{
    private readonly IsolatedLayoutProof layout;
    private readonly PageNumberVisualSnapshot artwork;
    private readonly string[] drawingOrder;
    private readonly string[] rendering;
    private readonly Rect[] glyphBounds;
    internal IsolatedLayoutProof Layout=>layout;
    internal int Pages=>layout.PageCount;
    internal int GlyphRuns=>layout.GlyphRuns;
    private IsolatedSpacingPreview(IsolatedLayoutProof layout,PageNumberVisualSnapshot artwork,string[] drawingOrder,string[] rendering,Rect[] glyphBounds)
    {this.layout=layout;this.artwork=artwork;this.drawingOrder=drawingOrder;this.rendering=rendering;this.glyphBounds=glyphBounds;}
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
    private static string Rendering(Visual leaf,FixedPage page,bool text)
    {
        // Geometry, glyph advances and solid paint do not describe rasterization.
        // Keep the resolved leaf values and relevant ancestor transitions;
        // do not infer that a default on a child cancels every renderer option.
        // Empty wrappers contribute nothing, while an explicit override remains
        // conservative evidence even if a particular backend draws it identically.
        DependencyProperty[] properties=text?
            [TextOptions.TextRenderingModeProperty,TextOptions.TextFormattingModeProperty,TextOptions.TextHintingModeProperty,
                RenderOptions.ClearTypeHintProperty,UIElement.SnapsToDevicePixelsProperty,FrameworkElement.UseLayoutRoundingProperty]:
            [RenderOptions.EdgeModeProperty,UIElement.SnapsToDevicePixelsProperty,FrameworkElement.UseLayoutRoundingProperty];
        static string Value(DependencyObject node,DependencyProperty property)
        {
            object value=node.GetValue(property);
            if(value is not (bool or Enum) || value is Enum mode && !Enum.IsDefined(mode.GetType(),mode))
                throw new InvalidOperationException("Chế độ vẽ XPS chưa được chứng minh cho bản sao cô lập.");
            return property.Name+"="+value;
        }
        var evidence=new List<string>{"leaf:"+string.Join(",",properties.Select(property=>Value(leaf,property)))};
        for(DependencyObject? node=leaf;node is Visual;node=VisualTreeHelper.GetParent(node))
        {
            var current=node;var parent=ReferenceEquals(current,page)?null:VisualTreeHelper.GetParent(current);
            // Style setters and inherited values can affect rendering without
            // a local DP value. Compare resolved transitions, not just locals.
            var local=properties.Where(property=>!Equals(current.GetValue(property),
                    parent?.GetValue(property)??property.GetMetadata(current.GetType()).DefaultValue))
                .Select(property=>Value(current,property)).ToArray();
            if(local.Length>0)evidence.Add(string.Join(",",local));
            if(ReferenceEquals(node,page))return string.Join("|",evidence);
        }
        throw new InvalidOperationException("Chưa ghép được chế độ vẽ XPS về đúng trang.");
    }
    internal static IsolatedSpacingPreview Capture(FixedDocumentSequence sequence)
    {
        var artwork=PageNumberPreviewVerifier.CaptureAll(sequence);
        var paginator=sequence.DocumentPaginator;paginator.ComputePageCount();
        var sizes=new List<IsolatedPageSize>();var glyphs=new List<IsolatedGlyphEvidence>();var order=new List<string>();var rendering=new List<string>();var glyphBounds=new List<Rect>();
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
                    rendering.Add("path:"+Rendering(path,page,false));
                }
                else if(visual is Glyphs glyph){order.Add("glyph");rendering.Add("glyph:"+Rendering(glyph,page,true));}
                else if(BitmapPageVisualProof.IsBitmapLeaf(visual))
                {
                    var bitmap=BitmapPageVisualProof.Read(visual,page);
                    order.Add(bitmap is null?"bitmap:none":"bitmap:"+bitmap.Value.Paint+"|"+
                        string.Join(",",new[]{bitmap.Value.Bounds.X,bitmap.Value.Bounds.Y,bitmap.Value.Bounds.Width,bitmap.Value.Bounds.Height}.Select(Number)));
                }
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
                // CaptureAll includes alignment and ink, not a guessed font
                // height. Keep the identical per-run rectangle to bind paint
                // evidence back to the exact glyph whose baseline may move.
                var alignment=run.ComputeAlignmentBox();alignment.Offset(run.BaselineOrigin.X,run.BaselineOrigin.Y);
                var ink=run.ComputeInkBoundingBox();if(!ink.IsEmpty)ink.Offset(run.BaselineOrigin.X,run.BaselineOrigin.Y);
                var bounds=transform.TransformBounds(alignment);if(!ink.IsEmpty)bounds.Union(transform.TransformBounds(ink));
                glyphBounds.Add(bounds);
            }
        }
        return new(new(sizes,glyphs),artwork,order.ToArray(),rendering.ToArray(),glyphBounds.ToArray());
    }
    private static bool Near(double a,double b)=>double.IsFinite(a) && double.IsFinite(b) && Math.Abs(a-b)<=.01;
    private static bool SameRect(Rect a,Rect b,double verticalShift=0)=>!a.IsEmpty && !b.IsEmpty &&
        Near(a.X,b.X) && Near(a.Y+verticalShift,b.Y) && Near(a.Width,b.Width) && Near(a.Height,b.Height);
    private PageBodyVisual[] BindGlyphPaint()
    {
        var painted=artwork.Body.Where(v=>v.Kind==typeof(Glyphs).FullName).ToArray();
        if(painted.Length!=layout.GlyphEvidence.Count || glyphBounds.Length!=painted.Length)
            throw new InvalidOperationException("Chưa ghép đủ glyph với paint XPS; không dùng bản thử spacing.");
        var result=new PageBodyVisual[painted.Length];var consumed=new HashSet<PageBodyVisual>();
        for(int i=0;i<result.Length;i++)
        {
            var glyph=layout.GlyphEvidence[i];
            var matches=painted.Where(v=>v.Page==glyph.Page && v.Text==glyph.Text.Normalize(NormalizationForm.FormC) && SameRect(v.Bounds,glyphBounds[i])).ToArray();
            if(matches.Length!=1 || !consumed.Add(matches[0]))
                throw new InvalidOperationException("Paint/glyph XPS thiếu hoặc trùng mốc; không đoán vùng hiệu chuẩn.");
            result[i]=matches[0];
        }
        return result;
    }
    internal void RequireSpacingTrial(IsolatedSpacingPreview trial,IReadOnlyList<string> visibleParagraphTexts)
    {
        // Core proves unique whole-run paragraph spans, baseline order and a
        // single same-page suffix translation; it does not inspect WPF paint.
        SpacingCalibration.RequireTrial(layout,trial.layout,visibleParagraphTexts);
        if(!drawingOrder.SequenceEqual(trial.drawingOrder))
            throw new InvalidOperationException("Bản thử đổi hình học nét/thứ tự vẽ XPS; không hiệu chuẩn spacing.");
        if(!rendering.SequenceEqual(trial.rendering))
            throw new InvalidOperationException("Bản thử đổi chế độ vẽ chữ/nét XPS; không hiệu chuẩn spacing.");
        var before=BindGlyphPaint();var after=trial.BindGlyphPaint();
        for(int i=0;i<before.Length;i++)
        {
            double shift=trial.layout.GlyphEvidence[i].Baseline-layout.GlyphEvidence[i].Baseline;
            if(before[i].Page!=after[i].Page || before[i].Kind!=after[i].Kind || before[i].Text!=after[i].Text || before[i].Paint!=after[i].Paint ||
                !SameRect(before[i].Bounds,after[i].Bounds,shift))
                throw new InvalidOperationException("Bản thử làm đổi paint/clip/khung glyph ngoài dịch baseline đã chứng minh.");
        }
        var otherBefore=artwork.Body.Where(v=>v.Kind!=typeof(Glyphs).FullName).ToArray();
        var otherAfter=trial.artwork.Body.Where(v=>v.Kind!=typeof(Glyphs).FullName).ToArray();
        if(otherBefore.Length!=otherAfter.Length)throw new InvalidOperationException("Bản thử thêm/mất hình hoặc ảnh XPS.");
        for(int i=0;i<otherBefore.Length;i++)
        {
            var a=otherBefore[i];var b=otherAfter[i];
            // No shape/image attachment is inferred from vertical proximity.
            // A moved bitmap/path remains manual even when nearby text shifts.
            if(a.Page!=b.Page || a.Kind!=b.Kind || a.Text!=b.Text || a.Paint!=b.Paint || !SameRect(a.Bounds,b.Bounds))
                throw new InvalidOperationException("Hình/ảnh của bản thử đổi hoặc dịch chưa được chứng minh; không hiệu chuẩn spacing.");
        }
        var spans=visibleParagraphTexts.Select(text=>SpacingCalibration.ResolveParagraphSpan(layout,text)).ToArray();
        for(int i=0;i<before.Length;i++)
        {
            double shift=trial.layout.GlyphEvidence[i].Baseline-layout.GlyphEvidence[i].Baseline;
            if(Near(shift,0) && !spans.Any(s=>i>=s.Start && i<s.EndExclusive))continue;
            bool Overlaps(PageBodyVisual glyph,IReadOnlyList<PageBodyVisual> others)=>others.Any(item=>
            {
                if(item.Page!=glyph.Page)return false;
                var overlap=Rect.Intersect(glyph.Bounds,item.Bounds);
                return !overlap.IsEmpty && overlap.Width>.01 && overlap.Height>.01;
            });
            // Clip exposure alone does not prove that a later opaque drawing
            // has not covered the moved text. Do not infer occlusion/attachment
            // from bounding boxes; leave any possible overlap unsupported.
            if(Overlaps(before[i],otherBefore) || Overlaps(after[i],otherAfter))
                throw new InvalidOperationException("Glyph hiệu chuẩn có thể bị hình/ảnh che sau dịch dòng; giữ kiểm tra thủ công.");
        }
    }
    internal void RequireSame(IsolatedSpacingPreview copy)
    {
        layout.RequireSame(copy.layout);
        if(!drawingOrder.SequenceEqual(copy.drawingOrder))
            throw new InvalidOperationException("Bản sao đổi hình học nét hoặc thứ tự vẽ XPS; không hiệu chuẩn spacing.");
        if(!rendering.SequenceEqual(copy.rendering))
            throw new InvalidOperationException("Bản sao đổi chế độ vẽ chữ/nét XPS; không hiệu chuẩn spacing.");
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
