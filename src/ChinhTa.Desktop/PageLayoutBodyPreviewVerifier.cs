using ChinhTa.Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ChinhTa.Desktop;

// Only used after Core proves the referenced header/footer parts are genuinely
// empty. Empty paragraph metrics may still reserve space, so the rendered main
// story must fit the approved body rectangle after Word paginates the candidate.
public static class PageLayoutBodyPreviewVerifier
{
    private const double UnitsPerMm=96/25.4;
    private const double BodyTolerance=96d/72; // one physical point
    private static IEnumerable<DependencyObject> Walk(DependencyObject root)
    {
        yield return root;
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)
            foreach(var child in Walk(VisualTreeHelper.GetChild(root,i)))yield return child;
    }
    public static void Verify(FixedDocumentSequence sequence,SectionLayout expected)
    {
        if(expected.Number!=1 || expected.MirrorMargins || !PageLayoutFix.Known(expected) || expected.WidthMm>expected.HeightMm)
            throw new InvalidOperationException("Chưa có khổ/lề dọc hữu hạn để hậu kiểm nội dung trang.");
        var paginator=sequence.DocumentPaginator;paginator.ComputePageCount();
        if(paginator.PageCount<1)throw new InvalidOperationException("Thiếu trang XPS để hậu kiểm vùng nội dung.");
        for(int i=0;i<paginator.PageCount;i++)
        {
            using var rendered=paginator.GetPage(i);
            var page=Walk(rendered.Visual??throw new InvalidOperationException("Thiếu hình trang XPS.")).OfType<FixedPage>().Single();
            if(!double.IsFinite(page.Width) || !double.IsFinite(page.Height) ||
                Math.Abs(page.Width-expected.WidthMm*UnitsPerMm)>1 || Math.Abs(page.Height-expected.HeightMm*UnitsPerMm)>1 ||
                Math.Abs(rendered.Size.Width-page.Width)>1 || Math.Abs(rendered.Size.Height-page.Height)>1)
                throw new InvalidOperationException("Khổ trang XPS chưa khớp thiết lập đã duyệt; phải phục hồi.");
        }
        var snapshot=PageNumberPreviewVerifier.CaptureAll(sequence);
        if(snapshot.Pages!=paginator.PageCount || snapshot.Body.Count==0)
            throw new InvalidOperationException("Chưa chứng minh được nội dung hiển thị sau đổi khổ/lề.");
        double left=expected.LeftMm*UnitsPerMm,top=expected.TopMm*UnitsPerMm;
        double right=(expected.WidthMm-expected.RightMm)*UnitsPerMm,bottom=(expected.HeightMm-expected.BottomMm)*UnitsPerMm;
        foreach(var item in snapshot.Body)
        {
            var bounds=item.Bounds;
            if(item.Page<1 || item.Page>snapshot.Pages || bounds.IsEmpty || bounds.Width<=0 || bounds.Height<=0 ||
                !new[]{bounds.Left,bounds.Top,bounds.Right,bounds.Bottom}.All(double.IsFinite) ||
                bounds.Left<left-BodyTolerance || bounds.Top<top-BodyTolerance ||
                bounds.Right>right+BodyTolerance || bounds.Bottom>bottom+BodyTolerance)
                throw new InvalidOperationException("Nội dung XPS tràn vùng khổ/lề đã duyệt sau đổi trang; phải phục hồi cả lượt.");
        }
    }
}
