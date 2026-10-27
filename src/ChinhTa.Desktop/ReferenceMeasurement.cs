using System.Windows.Documents;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
public static class ReferenceMeasurement
{
    public static LineReference? Read(FixedDocumentSequence document,PreviewHighlighter preview,PreviewAnchor anchor)
    {
        var last=preview.MeasureLastLine(document,anchor);
        return last is null?null:new(last.Left,last.Bottom,last.Width);
    }
}
