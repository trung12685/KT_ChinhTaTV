using ChinhTa.Core;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Path=System.Windows.Shapes.Path;

namespace ChinhTa.Desktop;

public sealed record PageBodyVisual(int Page,string Kind,string Text,Rect Bounds,string Paint="");
public sealed record PageNumberVisualSnapshot(int Pages,IReadOnlyList<PageBodyVisual> Body);

// Read-only rendered evidence. Main-story text, drawings and footer leaves outside
// the upper margin must retain both content and physical positions.
public static class PageNumberPreviewVerifier
{
    private static IEnumerable<DependencyObject> Walk(DependencyObject root)
    {
        yield return root;
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)foreach(var child in Walk(VisualTreeHelper.GetChild(root,i)))yield return child;
    }
    private static FixedPage Page(DocumentPage page)=>Walk(page.Visual??throw new InvalidOperationException("Thiếu trang XPS.")).OfType<FixedPage>().Single();
    private static Rect Bounds(Visual node,FixedPage page)
    {
        var bounds=VisualTreeHelper.GetDescendantBounds(node);
        return bounds.IsEmpty?bounds:node.TransformToAncestor(page).TransformBounds(bounds);
    }
    private static bool Finite(Rect bounds)=>!bounds.IsEmpty &&
        new[]{bounds.X,bounds.Y,bounds.Width,bounds.Height}.All(double.IsFinite) && bounds.Width>0 && bounds.Height>0;
    private static double Opacity(Visual leaf,FixedPage page)
    {
        var visibility=PreviewPaintProof.VisibilityOf(leaf,page);
        if(visibility==PreviewPaintProof.Paint.None)return 0;
        var opacity=PreviewPaintProof.EffectiveOpacity(leaf,page);
        if(visibility!=PreviewPaintProof.Paint.Uniform || opacity is null)
            throw new InvalidOperationException("Chưa chứng minh được độ hiển thị/mask của nội dung XPS khi kiểm PAGE.");
        return opacity.Value;
    }
    private static string Brush(Brush? brush,double opacity)
    {
        var paint=PreviewPaintProof.BrushPaint(brush);
        if(paint==PreviewPaintProof.Paint.None)return "none";
        if(paint!=PreviewPaintProof.Paint.Uniform || brush is not SolidColorBrush solid ||
            !double.IsFinite(brush.Opacity) || brush.Opacity<0 || brush.Opacity>1)
            throw new InvalidOperationException("Chưa chứng minh được màu/paint của nội dung XPS khi kiểm PAGE.");
        var color=solid.Color;
        return $"{color.R:X2}{color.G:X2}{color.B:X2}@"+(opacity*brush.Opacity*color.ScA).ToString("0.########",CultureInfo.InvariantCulture);
    }
    private static (Rect Bounds,string Paint)? GlyphEvidence(Glyphs glyph,FixedPage page,bool number,bool includeInk=false)
    {
        double opacity=Opacity(glyph,page);string paint=Brush(glyph.Fill,opacity);
        if(opacity==0 || paint=="none")return null;
        var transform=PreviewPaintProof.ToPage(glyph,page);
        if(transform is null)throw new InvalidOperationException("Chưa chứng minh được khung tọa độ chữ XPS khi kiểm PAGE.");
        var matrix=transform.Value;var run=glyph.ToGlyphRun();
        if(number && (run.IsSideways || run.BidiLevel%2!=0 || matrix.M11<=0 || matrix.M22<=0 ||
            Math.Abs(matrix.M12)>.001 || Math.Abs(matrix.M21)>.001))
            throw new InvalidOperationException("Số trang XPS xoay/lật hoặc chưa cùng trục trang; phải phục hồi.");
        var alignment=run.ComputeAlignmentBox();alignment.Offset(run.BaselineOrigin.X,run.BaselineOrigin.Y);
        var bounds=transform.TransformBounds(alignment);var ink=run.ComputeInkBoundingBox();
        if(ink.IsEmpty && string.IsNullOrWhiteSpace(glyph.UnicodeString))return null;
        if(!ink.IsEmpty)ink.Offset(run.BaselineOrigin.X,run.BaselineOrigin.Y);
        var inkBounds=ink.IsEmpty?Rect.Empty:transform.TransformBounds(ink);
        if(!Finite(bounds) || !Finite(inkBounds) ||
            !PreviewPaintProof.FullyExposed(new RectangleGeometry(bounds),glyph,page) ||
            !PreviewPaintProof.FullyExposed(new RectangleGeometry(inkBounds),glyph,page))
            throw new InvalidOperationException("Chữ XPS bị cắt hoặc chưa chứng minh hiển thị trọn khi kiểm PAGE.");
        if(number && (opacity!=1 || glyph.Fill is not SolidColorBrush fill || fill.Opacity!=1 ||
            fill.Color.A!=255 || fill.Color.R!=0 || fill.Color.G!=0 || fill.Color.B!=0))
            throw new InvalidOperationException("Số trang XPS chưa được vẽ đen đục hoàn toàn.");
        var typeface=run.GlyphTypeface;
        double scale=Math.Sqrt(matrix.M21*matrix.M21+matrix.M22*matrix.M22);
        if(scale<=0 || Math.Abs(matrix.Determinant)<.000001)
            throw new InvalidOperationException("Khung chữ XPS bị suy biến khi kiểm PAGE.");
        double size=glyph.FontRenderingEmSize*scale*.75;
        if(number && (!typeface.FamilyNames.Values.Any(v=>v.Equals("Times New Roman",StringComparison.OrdinalIgnoreCase)) ||
            size<12.99 || size>14.01 || typeface.Style!=FontStyles.Normal ||
            (glyph.StyleSimulations&StyleSimulations.ItalicSimulation)!=0))
            throw new InvalidOperationException("Số trang XPS chưa có phông/cỡ/đứng đã duyệt.");
        string style=string.Join("/",typeface.FamilyNames.Values.OrderBy(x=>x,StringComparer.Ordinal))+"|"+typeface.Style+"|"+
            typeface.Weight+"|"+typeface.Stretch+"|"+glyph.StyleSimulations+"|"+size.ToString("0.####",CultureInfo.InvariantCulture)+"|"+
            run.IsSideways+"|"+run.BidiLevel+"|"+string.Join(",",new[]{matrix.M11,matrix.M12,matrix.M21,matrix.M22}
                .Select(v=>(v/scale).ToString("0.########",CultureInfo.InvariantCulture)));
        if(includeInk)bounds.Union(inkBounds);
        return (bounds,paint+"|"+style);
    }
    private static (Rect Bounds,string Paint)? PathEvidence(Path path,FixedPage page)
    {
        double opacity=Opacity(path,page);if(opacity==0)return null;
        string fill=Brush(path.Fill,opacity),stroke=Brush(path.Stroke,opacity);
        if(path.Data is null || fill=="none" && stroke=="none")return null;
        if(path.Stretch!=Stretch.None || !double.IsFinite(path.StrokeThickness) || path.StrokeThickness<0)
            throw new InvalidOperationException("Chưa chứng minh được hình XPS khi kiểm PAGE.");
        Geometry? area=fill!="none" && path.Data.GetArea()>0?path.Data:null;
        if(area is null)fill="none";
        if(path.StrokeThickness==0)stroke="none";
        if(stroke!="none" && path.StrokeThickness>0)
        {
            var dashes=path.StrokeDashArray?.ToArray()??[];
            var pen=new Pen(Brushes.Black,path.StrokeThickness){StartLineCap=path.StrokeStartLineCap,EndLineCap=path.StrokeEndLineCap,
                DashCap=path.StrokeDashCap,LineJoin=path.StrokeLineJoin,MiterLimit=path.StrokeMiterLimit,
                DashStyle=new DashStyle(dashes,path.StrokeDashOffset)};
            var outline=path.Data.GetWidenedPathGeometry(pen);
            area=area is null?outline:PreviewPaintProof.Combine(area,outline,GeometryCombineMode.Union);
        }
        if(area is null || area.GetArea()<=0)return null;
        var transform=PreviewPaintProof.ToPage(path,page);
        if(transform is null)throw new InvalidOperationException("Chưa rõ tọa độ hình XPS khi kiểm PAGE.");
        var painted=PreviewPaintProof.InPage(area,transform);var bounds=painted.Bounds;
        if(!Finite(bounds) || !PreviewPaintProof.FullyExposed(painted,path,page))
            throw new InvalidOperationException("Hình XPS bị cắt hoặc chưa chứng minh hiển thị trọn khi kiểm PAGE.");
        return (bounds,fill+"|"+stroke+(stroke=="none"?"":"|"+path.StrokeThickness.ToString("0.####",CultureInfo.InvariantCulture)+"|"+
            path.StrokeStartLineCap+"|"+path.StrokeEndLineCap+"|"+path.StrokeDashCap+"|"+path.StrokeLineJoin+"|"+
            string.Join(",",(path.StrokeDashArray?.ToArray()??[]).Select(x=>x.ToString("0.####",CultureInfo.InvariantCulture)))+"|"+
            path.StrokeDashOffset.ToString("0.####",CultureInfo.InvariantCulture)+"|"+path.StrokeMiterLimit.ToString("0.####",CultureInfo.InvariantCulture)));
    }
    private static bool FooterStyle(Glyphs glyph,FixedPage page,PageFooterSource source)
    {
        var transform=PreviewPaintProof.ToPage(glyph,page);if(transform is null)return false;
        var matrix=transform.Value;var run=glyph.ToGlyphRun();var font=run.GlyphTypeface;
        bool italic=font.Style!=FontStyles.Normal || (glyph.StyleSimulations&StyleSimulations.ItalicSimulation)!=0;
        bool bold=font.Weight>=FontWeights.SemiBold || (glyph.StyleSimulations&StyleSimulations.BoldSimulation)!=0;
        string color=source.Color=="auto"?"000000":source.Color;
        return !run.IsSideways && run.BidiLevel%2==0 && matrix.M11>0 && matrix.M22>0 && Math.Abs(matrix.M12)<.001 && Math.Abs(matrix.M21)<.001 &&
            font.FamilyNames.Values.Any(v=>v.Equals(source.Font,StringComparison.OrdinalIgnoreCase)) &&
            Math.Abs(glyph.FontRenderingEmSize*matrix.M22*.75-source.Size)<.02 && italic==source.Italic && bold==source.Bold &&
            Opacity(glyph,page)==1 && glyph.Fill is SolidColorBrush brush && brush.Opacity==1 && brush.Color.A==255 &&
            $"{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}"==color;
    }
    private static HashSet<Glyphs> FooterGlyphs(FixedPage page,int number,PageFooterMigration migration,double bottomMm)
    {
        var source=migration.Source(number);if(source is null)return [];
        double edge=page.Height-bottomMm*96/25.4;string expected=number.ToString(CultureInfo.InvariantCulture);
        var found=new List<Glyphs>();
        foreach(var glyph in Walk(page).OfType<Glyphs>().Where(g=>g.UnicodeString==expected))
        {
            var initial=Bounds(glyph,page);if(initial.IsEmpty || initial.Bottom<edge-.25)continue;
            var evidence=GlyphEvidence(glyph,page,false);if(evidence is null)continue;
            if(evidence.Value.Bounds.Top<edge-.25 || evidence.Value.Bounds.Bottom>page.Height+.25 || !FooterStyle(glyph,page,source))
                throw new InvalidOperationException("Chữ số cuối trang chưa khớp phông/vùng PAGE đã duyệt; không di chuyển.");
            found.Add(glyph);
        }
        if(found.Count!=1)throw new InvalidOperationException("Chưa ghép duy nhất một số PAGE cuối trang với phần đã duyệt.");
        return found.ToHashSet();
    }
    public static void VerifyNoMigratedFooter(FixedDocumentSequence sequence,PageFooterMigration migration,double bottomMm)
    {
        if(!migration.Valid || !double.IsFinite(bottomMm) || bottomMm<=0)throw new InvalidOperationException("Thiếu mốc PAGE cuối trang để hậu kiểm.");
        var paginator=sequence.DocumentPaginator;paginator.ComputePageCount();
        if(paginator.PageCount!=migration.Pages)throw new InvalidOperationException("Số trang thay đổi khi di chuyển PAGE.");
        for(int n=0;n<paginator.PageCount;n++)
        {
            if(migration.Source(n+1) is null)continue;
            using var rendered=paginator.GetPage(n);var page=Page(rendered);double edge=page.Height-bottomMm*96/25.4;
            string expected=(n+1).ToString(CultureInfo.InvariantCulture);
            foreach(var glyph in Walk(page).OfType<Glyphs>().Where(g=>g.UnicodeString==expected))
            {
                var bounds=Bounds(glyph,page);if(bounds.IsEmpty || bounds.Bottom<edge-.25)continue;
                if(GlyphEvidence(glyph,page,false) is not null)throw new InvalidOperationException("Còn số PAGE cuối trang sau di chuyển; phải phục hồi.");
            }
        }
    }
    public static PageNumberVisualSnapshot Capture(FixedDocumentSequence sequence,double topMm,PageFooterMigration? footerMigration=null,double bottomMm=0)
    {
        if(!double.IsFinite(topMm) || topMm<=0)throw new InvalidOperationException("Chưa rõ vùng lề trên để bảo toàn nội dung PAGE.");
        return CaptureCore(sequence,topMm,footerMigration,bottomMm,false);
    }
    // C32 containment must inspect every painted leaf, including the upper band.
    // Include ink overhang as well as alignment bounds for that proof.
    public static PageNumberVisualSnapshot CaptureAll(FixedDocumentSequence sequence)=>CaptureCore(sequence,0,null,0,true);
    private static PageNumberVisualSnapshot CaptureCore(FixedDocumentSequence sequence,double topMm,PageFooterMigration? footerMigration,double bottomMm,bool all)
    {
        var paginator=sequence.DocumentPaginator;paginator.ComputePageCount();var items=new List<PageBodyVisual>();
        if(paginator.PageCount<1)throw new InvalidOperationException("Không có trang XPS để bảo toàn nội dung PAGE.");
        if(footerMigration is not null && (!footerMigration.Valid || paginator.PageCount!=footerMigration.Pages || !double.IsFinite(bottomMm) || bottomMm<=0))
            throw new InvalidOperationException("Thiếu số đo/trang nguồn PAGE cuối trang đã duyệt.");
        for(int n=0;n<paginator.PageCount;n++)
        {
            using var rendered=paginator.GetPage(n);var page=Page(rendered);
            // A bare FixedPage still reports its paper background as visual
            // bounds. It is an intentional blank page, not an unknown body leaf.
            // Colored/unknown paper or page effects remain outside this proof.
            if(page.Background is not null && (page.Background is not SolidColorBrush paper || paper.Color!=Colors.White || paper.Opacity!=1) ||
                PreviewPaintProof.VisibilityOf(page,page)!=PreviewPaintProof.Paint.Uniform || PreviewPaintProof.EffectiveOpacity(page,page)!=1)
                throw new InvalidOperationException("Nền/độ hiển thị trang XPS chưa được chứng minh để bảo toàn nội dung.");
            var removed=footerMigration is null?new HashSet<Glyphs>():FooterGlyphs(page,n+1,footerMigration,bottomMm);
            foreach(var visual in Walk(page).OfType<Visual>().Where(v=>VisualTreeHelper.GetChildrenCount(v)==0))
            {
                if(ReferenceEquals(visual,page))continue;
                if(visual is Glyphs removedGlyph && removed.Contains(removedGlyph))continue;
                var bounds=Bounds(visual,page);
                if(bounds.IsEmpty || !all && !BitmapPageVisualProof.IsBitmapLeaf(visual) && bounds.Bottom<=topMm*96/25.4+.25)continue;
                var evidence=visual switch
                {Visual bitmap when BitmapPageVisualProof.IsBitmapLeaf(bitmap)=>BitmapPageVisualProof.Read(bitmap,page),
                    Glyphs leafGlyph=>GlyphEvidence(leafGlyph,page,false,all),Path path=>PathEvidence(path,page),
                    _=>throw new InvalidOperationException("Loại nội dung XPS chưa hỗ trợ bảo toàn paint khi kiểm PAGE; giữ thủ công.")};
                if(evidence is null)continue;
                string text=visual is Glyphs glyph?(glyph.UnicodeString??"").Normalize(NormalizationForm.FormC):"";
                string kind=BitmapPageVisualProof.IsBitmapLeaf(visual)?BitmapPageVisualProof.Kind:visual.GetType().FullName??visual.GetType().Name;
                items.Add(new(n+1,kind,text,evidence.Value.Bounds,evidence.Value.Paint));
            }
        }
        return new(paginator.PageCount,items.OrderBy(i=>i.Page).ThenBy(i=>i.Kind,StringComparer.Ordinal).ThenBy(i=>i.Text,StringComparer.Ordinal)
            .ThenBy(i=>i.Bounds.X).ThenBy(i=>i.Bounds.Y).ToArray());
    }
    public static void Preserve(PageNumberVisualSnapshot before,PageNumberVisualSnapshot after)
    {
        if(before.Pages!=after.Pages || before.Body.Count!=after.Body.Count)throw new InvalidOperationException("Nội dung/số trang thay đổi khi sửa PAGE.");
        for(int i=0;i<before.Body.Count;i++)
        {
            var a=before.Body[i];var b=after.Body[i];
            if(a.Page!=b.Page || a.Kind!=b.Kind || a.Text!=b.Text || a.Paint!=b.Paint ||
                new[]{a.Bounds.X-b.Bounds.X,a.Bounds.Y-b.Bounds.Y,a.Bounds.Width-b.Bounds.Width,a.Bounds.Height-b.Bounds.Height}.Any(d=>Math.Abs(d)>.5))
                throw new InvalidOperationException("Chữ/bảng/ảnh/footer bị dịch hoặc thay đổi sau sửa PAGE; phải phục hồi.");
        }
    }
    public static void VerifyNumbers(FixedDocumentSequence sequence,double leftMm,double rightMm,double topMm)
    {
        if(!new[]{leftMm,rightMm,topMm}.All(double.IsFinite) || leftMm<0 || rightMm<0 || topMm<=0)
            throw new InvalidOperationException("Chưa rõ lề để hậu kiểm số trang XPS.");
        var paginator=sequence.DocumentPaginator;paginator.ComputePageCount();
        if(paginator.PageCount<1)throw new InvalidOperationException("Không có trang XPS để hậu kiểm số trang.");
        for(int n=0;n<paginator.PageCount;n++)
        {
            using var rendered=paginator.GetPage(n);var page=Page(rendered);var numbers=new List<(string Text,Rect Bounds)>();
            double center=(leftMm*96/25.4+page.Width-rightMm*96/25.4)/2;
            foreach(var glyph in Walk(page).OfType<Glyphs>())
            {
                string value=glyph.UnicodeString??"";if(value.Length==0 || value.Any(c=>c is <'0' or >'9'))continue;
                var initial=Bounds(glyph,page);
                if(initial.IsEmpty || initial.Top>=topMm*96/25.4+.5 || initial.Bottom<=0)continue;
                var evidence=GlyphEvidence(glyph,page,true);if(evidence is null)continue;
                if(evidence.Value.Bounds.Top<0 || evidence.Value.Bounds.Bottom>topMm*96/25.4+.5)
                    throw new InvalidOperationException("Số trang XPS chưa nằm trọn trong lề trên.");
                numbers.Add((value,evidence.Value.Bounds));
            }
            if(n==0)
            {if(numbers.Count!=0)throw new InvalidOperationException("XPS còn số ở vùng số trang đầu.");continue;}
            string actual=string.Concat(numbers.OrderBy(i=>i.Bounds.Left).Select(i=>i.Text));
            if(actual!=(n+1).ToString(CultureInfo.InvariantCulture) || numbers.Count==0)
                throw new InvalidOperationException("Số trang thực tế trên XPS chưa liên tiếp hoặc chưa có PAGE.");
            double left=numbers.Min(i=>i.Bounds.Left),right=numbers.Max(i=>i.Bounds.Right);
            if(Math.Abs((left+right)/2-center)>4)throw new InvalidOperationException("Số trang XPS chưa căn giữa vùng lề trên.");
        }
    }
}
