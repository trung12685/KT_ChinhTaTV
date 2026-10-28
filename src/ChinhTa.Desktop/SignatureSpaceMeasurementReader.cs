using System.Windows.Documents;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

public static class SignatureSpaceMeasurementReader
{
    public static SignatureSpaceMeasurement Read(FixedDocumentSequence document,PreviewHighlighter preview,SignatureWritingSpaceTarget target,IReadOnlyDictionary<Issue,PreviewAnchor> anchors)
    {
        var authority=SignatureWritingSpaceTarget.Probe(target.Authorities[^1]);var name=SignatureWritingSpaceTarget.Probe(target.Name);
        var paginator=document.DocumentPaginator;if(!paginator.IsPageCountValid)paginator.ComputePageCount();
        if(!anchors.TryGetValue(authority,out var a) || !anchors.TryGetValue(name,out var n))return new(0,0,paginator.PageCount,null);
        var last=preview.MeasureLastLine(document,a);var signer=preview.MeasureSingleLine(document,n);
        if(last is null || signer is null)return new(0,0,paginator.PageCount,null);
        return new(a.Page,n.Page,paginator.PageCount,a.Page==n.Page?signer.Top-last.Bottom:null);
    }
}
