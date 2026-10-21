using System.Windows.Documents;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

public static class AdministrativePositionInspection
{
    public static IReadOnlyList<(Issue Issue,PreviewAnchor Anchor)> Inspect(FixedDocumentSequence document,PreviewHighlighter preview,
        IReadOnlyList<AdministrativePositionTarget> targets,IReadOnlyDictionary<Issue,PreviewAnchor> anchors)
    {
        var result=new List<(Issue,PreviewAnchor)>();
        var measured=new Dictionary<AdministrativePositionRole,(AdministrativePositionTarget Target,RenderedTextLine Line)>();
        foreach(var target in targets)
        {
            bool mapped=anchors.TryGetValue(target.Probe,out var anchor);
            // No text-only fallback: a missing Word anchor must not highlight a duplicate elsewhere.
            anchor??=new(1,"",0,0);
            var row=mapped && anchor.Page==1?preview.MeasureSingleLine(document,anchor):null;
            var refs=target.References.Select(reference=>anchors.TryGetValue(reference.Probe,out var a) && a.Page==anchor.Page && a.Page==1?
                preview.MeasureSingleLine(document,a):null).ToArray();
            var assessment=AdministrativePositionRules.Assess(target,row,refs);
            if(assessment.Measured && row is not null)measured[target.Role]=(target,row);
            if(assessment.Warnings.Count>0)
                result.Add((target.Probe with {Message=string.Join(" ",assessment.Warnings),Citation=AdministrativePositionRules.Citation},anchor));
        }
        if(measured.TryGetValue(AdministrativePositionRole.Number,out var number) && measured.TryGetValue(AdministrativePositionRole.Date,out var date))
        {
            string? warning=AdministrativePositionRules.SameRow(number.Line,date.Line);
            if(warning is not null && anchors.TryGetValue(date.Target.Probe,out var anchor))
                result.Add((date.Target.Probe with {Title="Số và ngày tháng chưa cùng dòng",Message=warning,Citation=AdministrativePositionRules.Citation},anchor));
        }
        return result;
    }
}
