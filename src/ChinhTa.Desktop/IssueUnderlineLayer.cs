using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

// View-only marks. They never touch Word formatting or the XPS file on disk.
public sealed class IssueUnderlineLayer
{
    private readonly List<(FixedPage Page,Rectangle Mark)> marks=[];
    public int Count=>marks.Count;
    public void Clear(){foreach(var (page,mark) in marks)page.Children.Remove(mark);marks.Clear();}
    public int Show(FixedDocumentSequence document,PreviewHighlighter highlighter,IEnumerable<Issue> issues,
        IReadOnlyDictionary<Issue,PreviewAnchor> anchors,Action<Issue> select)
    {
        Clear();int mapped=0;
        foreach(var issue in issues)
        {
            if(issue.Kind is FixKind.PageLayout or FixKind.PageNumber || !anchors.TryGetValue(issue,out var anchor))continue;
            var boxes=highlighter.MeasureReference(document,anchor);if(boxes.Count==0)continue;
            var paginator=document.DocumentPaginator;using var rendered=paginator.GetPage(anchor.Page-1);
            var page=FindPage(rendered.Visual);if(page is null)continue;
            bool required=IssueReviewCatalog.Describe(issue).Section==IssueReviewSection.RequiredFormat;
            foreach(var box in boxes)
            {
                var mark=new Rectangle{Width=Math.Max(2,box.Width),Height=2,Fill=required?Brushes.Firebrick:Brushes.DarkGoldenrod,
                    ToolTip=(required?"Đỏ: thể thức đã nhận diện. ":"Vàng: cần xem xét. ")+IssueReviewCatalog.Describe(issue).Label,
                    Tag=issue,Cursor=System.Windows.Input.Cursors.Hand};
                FixedPage.SetLeft(mark,box.Left);FixedPage.SetTop(mark,Math.Max(0,box.Bottom-1));
                mark.MouseLeftButtonDown+=(_,e)=>{select(issue);e.Handled=true;};
                page.Children.Add(mark);marks.Add((page,mark));
            }
            mapped++;
        }
        return mapped;
    }
    private static FixedPage? FindPage(DependencyObject? root)
    {
        if(root is null)return null;if(root is FixedPage page)return page;
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)if(FindPage(VisualTreeHelper.GetChild(root,i)) is { } found)return found;
        return null;
    }
}
