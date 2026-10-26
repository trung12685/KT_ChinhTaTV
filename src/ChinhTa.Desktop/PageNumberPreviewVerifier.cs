using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ChinhTa.Desktop;

public sealed record PageBodyVisual(int Page,string Kind,string Text,Rect Bounds);
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
    public static PageNumberVisualSnapshot Capture(FixedDocumentSequence sequence,double topMm)
    {
        var paginator=sequence.DocumentPaginator;paginator.ComputePageCount();var items=new List<PageBodyVisual>();
        for(int n=0;n<paginator.PageCount;n++)
        {
            using var rendered=paginator.GetPage(n);var page=Page(rendered);
            foreach(var visual in Walk(page).OfType<Visual>().Where(v=>VisualTreeHelper.GetChildrenCount(v)==0))
            {
                var bounds=Bounds(visual,page);
                if(bounds.IsEmpty || bounds.Bottom<=topMm*96/25.4+.25)continue;
                if(!double.IsFinite(bounds.X) || !double.IsFinite(bounds.Y) || !double.IsFinite(bounds.Width) || !double.IsFinite(bounds.Height))
                    throw new InvalidOperationException("Chưa đo được nội dung trang trước/sau PAGE.");
                string text=visual is Glyphs glyph?(glyph.UnicodeString??"").Normalize(NormalizationForm.FormC):"";
                items.Add(new(n+1,visual.GetType().FullName??visual.GetType().Name,text,bounds));
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
            if(a.Page!=b.Page || a.Kind!=b.Kind || a.Text!=b.Text ||
                new[]{a.Bounds.X-b.Bounds.X,a.Bounds.Y-b.Bounds.Y,a.Bounds.Width-b.Bounds.Width,a.Bounds.Height-b.Bounds.Height}.Any(d=>Math.Abs(d)>.5))
                throw new InvalidOperationException("Chữ/bảng/ảnh/footer bị dịch hoặc thay đổi sau sửa PAGE; phải phục hồi.");
        }
    }
    public static void VerifyNumbers(FixedDocumentSequence sequence,double leftMm,double rightMm,double topMm)
    {
        var paginator=sequence.DocumentPaginator;paginator.ComputePageCount();
        for(int n=0;n<paginator.PageCount;n++)
        {
            using var rendered=paginator.GetPage(n);var page=Page(rendered);var numbers=new List<(string Text,Rect Bounds)>();
            double center=(leftMm*96/25.4+page.Width-rightMm*96/25.4)/2;
            foreach(var glyph in Walk(page).OfType<Glyphs>())
            {
                string value=glyph.UnicodeString??"";if(value.Length==0 || value.Any(c=>c is <'0' or >'9'))continue;
                var bounds=Bounds(glyph,page);if(bounds.IsEmpty || bounds.Top<-.5 || bounds.Bottom>topMm*96/25.4+.5)continue;
                if(Math.Abs((bounds.Left+bounds.Right)/2-center)>40)continue;
                var run=glyph.ToGlyphRun();var typeface=run.GlyphTypeface;
                bool family=typeface.FamilyNames.Values.Any(v=>v.Equals("Times New Roman",StringComparison.OrdinalIgnoreCase));
                var transform=glyph.TransformToAncestor(page);var origin=transform.Transform(new Point(0,0));
                var end=transform.Transform(new Point(0,glyph.FontRenderingEmSize));
                double size=Math.Sqrt(Math.Pow(end.X-origin.X,2)+Math.Pow(end.Y-origin.Y,2))*.75;
                if(!family || size<12.99 || size>14.01 || typeface.Style!=FontStyles.Normal ||
                    (glyph.StyleSimulations&StyleSimulations.ItalicSimulation)!=0 || glyph.Fill is not SolidColorBrush fill ||
                    fill.Color.A!=255 || fill.Color.R!=0 || fill.Color.G!=0 || fill.Color.B!=0)
                    throw new InvalidOperationException("Số trang XPS chưa có phông/màu/cỡ/đứng đã duyệt.");
                numbers.Add((value,bounds));
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
