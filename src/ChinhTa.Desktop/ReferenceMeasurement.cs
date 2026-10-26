using System.Windows.Documents;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
public static class ReferenceMeasurement
{
    public static LineReference? Read(FixedDocumentSequence document,PreviewHighlighter preview,PreviewAnchor anchor)
    {
        var boxes=preview.MeasureReference(document,anchor);
        if(boxes.Count==0)return null;
        double bottom=boxes.Max(b=>b.Bottom);
        var last=boxes.Where(b=>Math.Abs(b.Bottom-bottom)<=3).ToArray();
        double left=last.Min(b=>b.Left),right=last.Max(b=>b.Right);
        return new(left*.75,bottom*.75,(right-left)*.75);
    }
}
