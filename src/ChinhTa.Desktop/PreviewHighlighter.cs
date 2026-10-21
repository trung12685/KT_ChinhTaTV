using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using ChinhTa.Core;
using LineGeometry = ChinhTa.Core.LineGeometry;

namespace ChinhTa.Desktop;

/// <summary>Transient shapes on cached fixed pages. No edits to Word or the on-disk XPS.</summary>
public sealed class PreviewHighlighter
{
    private record Run(Glyphs Glyph, int Start, int Length);
    private record PageIndex(FixedPage Page, string Text, List<Run> Runs);
    private readonly Dictionary<int, PageIndex> pages = new();
    private readonly List<(FixedPage Page, Rectangle Shape)> marks = new();

    public void Clear()
    {
        foreach (var (page, shape) in marks) page.Children.Remove(shape);
        marks.Clear();
    }
    public void Reset() { Clear(); pages.Clear(); }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T value) yield return value;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (var child in Descendants<T>(VisualTreeHelper.GetChild(root, i))) yield return child;
    }
    private PageIndex? GetIndex(FixedDocumentSequence document, int pageNumber)
    {
        if (!pages.TryGetValue(pageNumber, out var index))
        {
            var paginator = document.DocumentPaginator;
            if (pageNumber < 1) return null;
            if (!paginator.IsPageCountValid) paginator.ComputePageCount();
            if (pageNumber > paginator.PageCount) return null;
            DocumentPage renderedPage;
            try { renderedPage = paginator.GetPage(pageNumber - 1); }
            catch (ArgumentOutOfRangeException) { return null; }
            var visual = renderedPage.Visual;
            if (visual is null) return null;
            var page = Descendants<FixedPage>(visual).FirstOrDefault();
            if (page is null) return null;
            var runs = new List<Run>();
            var text = new System.Text.StringBuilder();
            foreach (var glyph in Descendants<Glyphs>(page))
            {
                string value = glyph.UnicodeString ?? "";
                runs.Add(new(glyph, text.Length, value.Length));
                text.Append(value);
            }
            index = new(page, text.ToString(), runs);
            pages[pageNumber] = index;
        }
        return index;
    }
    public IReadOnlyList<Rect> Measure(FixedDocumentSequence document, PreviewAnchor anchor)
    {
        var index = GetIndex(document, anchor.Page);
        if (index is null) return [];
        var span = PreviewLocator.Locate(anchor, index.Text);
        if (span is null) return [];
        var boxes = new List<Rect>();
        try
        {
            foreach (var run in index.Runs)
            {
                int start = Math.Max(span.Start, run.Start) - run.Start;
                int end = Math.Min(span.Start + span.Length, run.Start + run.Length) - run.Start;
                if (end <= start) continue;
                GlyphRun glyphRun = run.Glyph.ToGlyphRun();
                if(glyphRun.IsSideways || glyphRun.BidiLevel%2!=0)return [];
                var advance=GlyphAdvanceRange.Read(glyphRun.AdvanceWidths.ToArray(),glyphRun.ClusterMap?.ToArray(),run.Length,start,end);
                if(advance is null)return [];
                double x1=advance.Start,x2=advance.End;
                Rect alignment = glyphRun.ComputeAlignmentBox();
                var bounds = new Rect(glyphRun.BaselineOrigin.X + Math.Min(x1, x2),
                    glyphRun.BaselineOrigin.Y + alignment.Y,
                    Math.Max(2, Math.Abs(x2 - x1)), Math.Max(2, alignment.Height));
                bounds = run.Glyph.TransformToAncestor(index.Page).TransformBounds(bounds);
                if (!bounds.IsEmpty && double.IsFinite(bounds.X) && double.IsFinite(bounds.Y)) boxes.Add(bounds);
            }
        }
        catch (ArgumentException) { return []; }
        catch (InvalidOperationException) { return []; }
        return boxes;
    }
    public LineAnchorFrame? TableFrame(FixedDocumentSequence document, PreviewAnchor anchor)
    {
        var index=GetIndex(document,anchor.Page);
        if(index is null)return null;
        var span=PreviewLocator.Locate(anchor,index.Text);
        if(span is null)return null;
        var selected=index.Runs.Where(run=>run.Start<span.Start+span.Length && run.Start+run.Length>span.Start).ToArray();
        if(selected.Length==0)return null;
        var frames=new List<Rect>();
        foreach(var run in selected)
        {
            var candidates=new List<Rect>();
            for(DependencyObject? parent=VisualTreeHelper.GetParent(run.Glyph);parent is not null && parent!=index.Page;
                parent=VisualTreeHelper.GetParent(parent))
            {
                if(parent is not UIElement element || element.Clip is not Geometry clip || clip.Bounds.IsEmpty)continue;
                var difference=Geometry.Combine(clip,new RectangleGeometry(clip.Bounds),GeometryCombineMode.Xor,Transform.Identity);
                if(difference.GetArea()>.01)continue;
                var transform=element.TransformToAncestor(index.Page);
                // Only an axis-aligned rectangular clip can establish a table coordinate frame.
                var corners=new[]{clip.Bounds.TopLeft,clip.Bounds.TopRight,clip.Bounds.BottomLeft}.Select(transform.Transform).ToArray();
                if(Math.Abs(corners[0].Y-corners[1].Y)>.01 || Math.Abs(corners[0].X-corners[2].X)>.01)continue;
                var rect=transform.TransformBounds(clip.Bounds);
                if(rect.Width>0 && rect.Height>0 && rect.Width<index.Page.Width*.9 &&
                    rect.Left>0 && rect.Top>=0 && double.IsFinite(rect.Left) && double.IsFinite(rect.Top))candidates.Add(rect);
            }
            if(candidates.Count==0)return null;
            frames.Add(candidates.OrderBy(r=>r.Width*r.Height).First());
        }
        var frame=frames[0];
        if(frames.Any(r=>Math.Abs(r.Left-frame.Left)>.5 || Math.Abs(r.Top-frame.Top)>.5 ||
            Math.Abs(r.Width-frame.Width)>.5 || Math.Abs(r.Height-frame.Height)>.5))return null;
        return new(frame.Left*.75,frame.Top*.75,frame.Width*.75,frame.Height*.75,index.Page.Width*.75);
    }
    public IReadOnlyList<LineGeometry> HorizontalLines(FixedDocumentSequence document, int pageNumber)
    {
        var index = GetIndex(document, pageNumber);
        if (index is null) return [];
        var lines = new List<LineGeometry>();
        foreach (var path in Descendants<System.Windows.Shapes.Path>(index.Page))
        {
            if (path.Data is null || path.Stroke is null && path.Fill is null) continue;
            var geometry = path.Data.GetFlattenedPathGeometry();
            foreach (var figure in geometry.Figures)
            {
                var piece = new PathGeometry([figure]) { Transform = geometry.Transform };
                var rect = path.TransformToAncestor(index.Page).TransformBounds(piece.Bounds);
                // Word may render Line as a stroke or a thin filled rectangle. Ignore ordinary boxes.
                if (rect.Width >= 6 && rect.Height <= 2)
                    lines.Add(new(rect.Left * .75, (rect.Top + rect.Height / 2) * .75, rect.Width * .75));
            }
        }
        return lines;
    }
    public void ShowLineSuggestion(FixedDocumentSequence document, int pageNumber, ShapeLineFix fix)
    {
        var index = GetIndex(document, pageNumber);
        if (index is null) return;
        var box = new Rect(fix.Left / .75, fix.Top / .75 - 1, fix.Width / .75, 2);
        var shape = new Rectangle { Width=box.Width, Height=box.Height, Fill=Brushes.DarkOrange, IsHitTestVisible=false };
        FixedPage.SetLeft(shape,box.Left); FixedPage.SetTop(shape,box.Top);
        index.Page.Children.Add(shape); marks.Add((index.Page,shape));
        index.Page.BringIntoView(box);
    }
    public bool Show(FixedDocumentSequence document, PreviewAnchor anchor)
    {
        Clear();
        var index = GetIndex(document, anchor.Page);
        if (index is null) return false;
        Rect selectedBounds = Rect.Empty;
        foreach (var bounds in Measure(document, anchor))
        {
            var shape = new Rectangle
            {
                Width = bounds.Width, Height = bounds.Height,
                Fill = new SolidColorBrush(Color.FromArgb(85, 255, 215, 0)),
                Stroke = Brushes.DarkOrange, StrokeThickness = .8, IsHitTestVisible = false
            };
            FixedPage.SetLeft(shape, bounds.Left); FixedPage.SetTop(shape, bounds.Top);
            selectedBounds.Union(bounds);
            index.Page.Children.Add(shape); marks.Add((index.Page, shape));
        }
        if (!selectedBounds.IsEmpty) index.Page.BringIntoView(selectedBounds);
        return marks.Count > 0;
    }
}

