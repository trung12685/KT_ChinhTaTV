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
    private record PageIndex(FixedPage Page, string Text, List<Run> Runs,Lazy<PreviewTextIndex> Prepared);
    private readonly Dictionary<int, PageIndex> pages = new();
    private readonly List<(FixedPage Page, Rectangle Shape)> marks = new();
    private readonly PreviewTextCache sources=new();
    private FixedDocumentSequence? owner;
    public bool UsePreparedText { get; set; }=true;
    public int PreparedSources=>sources.Preparations;

    public void Clear()
    {
        foreach (var (page, shape) in marks) page.Children.Remove(shape);
        marks.Clear();
    }
    public void Reset() { Clear(); pages.Clear();sources.Clear();owner=null; }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T value) yield return value;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (var child in Descendants<T>(VisualTreeHelper.GetChild(root, i))) yield return child;
    }
    private PageIndex? GetIndex(FixedDocumentSequence document, int pageNumber)
    {
        if(!ReferenceEquals(owner,document)){Reset();owner=document;}
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
            string pageText=text.ToString();
            index = new(page,pageText,runs,new(()=>PreviewLocator.Prepare(pageText)));
            pages[pageNumber] = index;
        }
        return index;
    }
    public IReadOnlyList<Rect> Measure(FixedDocumentSequence document, PreviewAnchor anchor)
        =>MeasureCore(document,anchor,null,false);
    // Reference geometry authorizes edits; it needs stronger evidence than a
    // bounding highlight. Never return the remaining fragments of a partially
    // hidden selection as though they were the complete Word reference.
    public IReadOnlyList<Rect> MeasureReference(FixedDocumentSequence document,PreviewAnchor anchor)
        =>MeasureCore(document,anchor,null,true);
    public RenderedTextLine? MeasureSingleLine(FixedDocumentSequence document,PreviewAnchor anchor)
    {
        var rows=MeasureRows(document,anchor);
        return rows.Count==1?rows[0]:null;
    }
    public RenderedTextLine? MeasureLastLine(FixedDocumentSequence document,PreviewAnchor anchor)
    {
        var rows=MeasureRows(document,anchor);
        return rows.Count==0?null:rows[^1];
    }
    private IReadOnlyList<RenderedTextLine> MeasureRows(FixedDocumentSequence document,PreviewAnchor anchor)
    {
        var baselines=new List<double>();
        var boxes=MeasureCore(document,anchor,baselines,true);
        if(boxes.Count==0 || baselines.Count!=boxes.Count)return [];
        // Use the same half-point baseline tolerance as administrative rows.
        // Font size changes the alignment-box bottom without changing the row.
        const double tolerance=.5/.75;
        var rows=new List<RenderedTextLine>();
        int start=0;
        while(start<boxes.Count)
        {
            int end=start+1;double low=baselines[start],high=low;
            while(end<boxes.Count)
            {
                double nextLow=Math.Min(low,baselines[end]),nextHigh=Math.Max(high,baselines[end]);
                if(nextHigh-nextLow>tolerance)break;
                low=nextLow;high=nextHigh;end++;
            }
            var rowBoxes=boxes.Skip(start).Take(end-start).ToArray();
            double left=rowBoxes.Min(b=>b.Left),right=rowBoxes.Max(b=>b.Right),
                top=rowBoxes.Min(b=>b.Top),bottom=rowBoxes.Max(b=>b.Bottom);
            var row=new RenderedTextLine(left*.75,top*.75,(right-left)*.75,(bottom-top)*.75,
                baselines.Skip(start).Take(end-start).Average()*.75);
            if(!row.Valid)return [];
            // Selected text must progress down the page in disjoint rows.
            // Raised/lowered text, tight overlaps or a return to an earlier row
            // are ambiguous; never turn their visible fragments into a final row.
            if(rows.Count>0 && (row.Baseline<=rows[^1].Baseline+.5 || row.Top<rows[^1].Bottom-.5))return [];
            rows.Add(row);start=end;
        }
        return rows;
    }
    private IReadOnlyList<Rect> MeasureCore(FixedDocumentSequence document, PreviewAnchor anchor,List<double>? baselines,bool reference)
    {
        var index = GetIndex(document, anchor.Page);
        if (index is null) return [];
        var span = Locate(anchor,index);
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
                var transform=run.Glyph.TransformToAncestor(index.Page);
                bounds = transform.TransformBounds(bounds);
                if (!bounds.IsEmpty && double.IsFinite(bounds.X) && double.IsFinite(bounds.Y))
                {
                    if(reference)
                    {
                        var affine=PreviewPaintProof.ToPage(run.Glyph,index.Page);
                        if(affine is null)return [];
                        var matrix=affine.Value;
                        if(matrix.M11<=0 || matrix.M22<=0 || Math.Abs(matrix.M12)>.001 || Math.Abs(matrix.M21)>.001)return [];
                        var origin=transform.Transform(glyphRun.BaselineOrigin);
                        var next=transform.Transform(new Point(glyphRun.BaselineOrigin.X+1,glyphRun.BaselineOrigin.Y));
                        if(!double.IsFinite(origin.Y) || Math.Abs(origin.Y-next.Y)>.001 || next.X<=origin.X)return [];
                        if(!double.IsFinite(bounds.Width) || !double.IsFinite(bounds.Height) ||
                            PreviewPaintProof.VisibilityOf(run.Glyph,index.Page)!=PreviewPaintProof.Paint.Uniform ||
                            PreviewPaintProof.BrushPaint(run.Glyph.Fill)!=PreviewPaintProof.Paint.Uniform ||
                            !PreviewPaintProof.FullyExposed(new RectangleGeometry(bounds),run.Glyph,index.Page))return [];
                        // Italic/overhanging ink can extend outside the advance
                        // box. Check the run's actual ink bounds as well. For a
                        // partial-run selection this deliberately requires the
                        // whole run to remain exposed instead of guessing ink
                        // ownership at a cluster boundary.
                        var ink=glyphRun.ComputeInkBoundingBox();
                        if(!ink.IsEmpty)
                        {
                            ink.Offset(glyphRun.BaselineOrigin.X,glyphRun.BaselineOrigin.Y);
                            ink=transform.TransformBounds(ink);
                            if(!double.IsFinite(ink.X) || !double.IsFinite(ink.Y) ||
                                !double.IsFinite(ink.Width) || !double.IsFinite(ink.Height) ||
                                !PreviewPaintProof.FullyExposed(new RectangleGeometry(ink),run.Glyph,index.Page))return [];
                        }
                        baselines?.Add(origin.Y);
                    }
                    boxes.Add(bounds);
                }
                else if(reference)return [];
            }
        }
        catch (ArgumentException) { return []; }
        catch (InvalidOperationException) { return []; }
        return boxes;
    }
    public LineAnchorFrame? TableFrame(FixedDocumentSequence document, PreviewAnchor anchor)
    {
        if(MeasureReference(document,anchor).Count==0)return null;
        var index=GetIndex(document,anchor.Page);
        if(index is null)return null;
        var span=Locate(anchor,index);
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
        var evidence=ReadHorizontalLines(document,pageNumber);
        if(evidence.HasUnverifiedPaths)
            throw new InvalidOperationException("Có nét bị cắt một phần hoặc chưa xác minh được cách hiển thị; chưa đủ bằng chứng để tự sửa đường kẻ.");
        return evidence.Lines;
    }
    public HorizontalStrokeEvidence ReadHorizontalLines(FixedDocumentSequence document,int pageNumber)
    {
        var index = GetIndex(document, pageNumber);
        if (index is null) return new([],true);
        var lines = new List<LineGeometry>();bool unverified=false;
        foreach (var path in Descendants<System.Windows.Shapes.Path>(index.Page))
        {
            var evidence=VisibleHorizontalStrokeReader.Read(path,index.Page);
            lines.AddRange(evidence.Lines);unverified|=evidence.HasUnverifiedPaths;
        }
        return new(lines,unverified);
    }
    private TextSpan? Locate(PreviewAnchor anchor,PageIndex index)=>UsePreparedText?
        PreviewLocator.Locate(anchor,sources.Get(anchor.PageText),index.Prepared.Value):PreviewLocator.Locate(anchor,index.Text);
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

