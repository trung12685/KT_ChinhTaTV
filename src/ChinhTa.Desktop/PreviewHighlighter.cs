using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using System.Windows.Shapes;
using ChinhTa.Core;

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
    public bool Show(FixedDocumentSequence document, PreviewAnchor anchor)
    {
        Clear();
        if (!pages.TryGetValue(anchor.Page, out var index))
        {
            var visual = document.DocumentPaginator.GetPage(anchor.Page - 1).Visual;
            if (visual is null) return false;
            var page = Descendants<FixedPage>(visual).FirstOrDefault();
            if (page is null) return false;
            var runs = new List<Run>();
            var text = new System.Text.StringBuilder();
            foreach (var glyph in Descendants<Glyphs>(page))
            {
                string value = glyph.UnicodeString ?? "";
                runs.Add(new(glyph, text.Length, value.Length));
                text.Append(value);
            }
            index = new(page, text.ToString(), runs);
            pages[anchor.Page] = index;
        }
        var span = PreviewLocator.Locate(anchor, index.Text);
        if (span is null) return false;
        try
        {
            Rect selectedBounds = Rect.Empty;
            foreach (var run in index.Runs)
            {
                int start = Math.Max(span.Start, run.Start) - run.Start;
                int end = Math.Min(span.Start + span.Length, run.Start + run.Length) - run.Start;
                if (end <= start) continue;
                GlyphRun glyphRun = run.Glyph.ToGlyphRun();
                double x1 = glyphRun.GetDistanceFromCaretCharacterHit(new CharacterHit(start, 0));
                double x2 = glyphRun.GetDistanceFromCaretCharacterHit(new CharacterHit(end, 0));
                Rect alignment = glyphRun.ComputeAlignmentBox();
                var bounds = new Rect(glyphRun.BaselineOrigin.X + Math.Min(x1, x2),
                    glyphRun.BaselineOrigin.Y + alignment.Y,
                    Math.Max(2, Math.Abs(x2 - x1)), Math.Max(2, alignment.Height));
                bounds = run.Glyph.TransformToAncestor(index.Page).TransformBounds(bounds);
                if (bounds.IsEmpty || !double.IsFinite(bounds.X) || !double.IsFinite(bounds.Y)) continue;
                var shape = new Rectangle
                {
                    Width = bounds.Width, Height = bounds.Height,
                    Fill = new SolidColorBrush(Color.FromArgb(85, 255, 215, 0)),
                    Stroke = Brushes.DarkOrange, StrokeThickness = 0.8,
                    IsHitTestVisible = false
                };
                FixedPage.SetLeft(shape, bounds.Left); FixedPage.SetTop(shape, bounds.Top);
                selectedBounds.Union(bounds);
                index.Page.Children.Add(shape);
                marks.Add((index.Page, shape));
            }
            if (!selectedBounds.IsEmpty) index.Page.BringIntoView(selectedBounds);
            return marks.Count > 0;
        }
        catch (ArgumentException) { Clear(); return false; }
        catch (InvalidOperationException) { Clear(); return false; }
    }
}
