using System.Windows.Documents;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

public static class AdministrativePositionInspection
{
    public static AdministrativeCenterEvidence MeasureCentering(FixedDocumentSequence document,PreviewHighlighter preview,
        AdministrativePositionTarget target,IReadOnlyDictionary<Issue,PreviewAnchor> anchors)
    {
        bool mapped=anchors.TryGetValue(target.Probe,out var anchor) && anchor.Page==1;
        var row=mapped?preview.MeasureSingleLine(document,anchor!):null;
        var frame=mapped?preview.TableFrame(document,anchor!):null;
        var refs=new List<RenderedTextLine?>();var frames=new List<LineAnchorFrame?>();
        foreach(var reference in target.References)
        {
            bool found=anchors.TryGetValue(reference.Probe,out var a) && a.Page==1;
            refs.Add(found?preview.MeasureSingleLine(document,a!):null);
            frames.Add(found?preview.TableFrame(document,a!):null);
        }
        return new(target,row,refs,frame,frames);
    }
    public static IReadOnlyList<(Issue Issue,PreviewAnchor Anchor)> Inspect(FixedDocumentSequence document,PreviewHighlighter preview,
        IReadOnlyList<AdministrativePositionTarget> targets,IReadOnlyDictionary<Issue,PreviewAnchor> anchors,
        Action<AdministrativePositionTarget,bool>? coverage=null)
        =>InspectMeasured(targets.Select(t=>MeasureCentering(document,preview,t,anchors)).ToArray(),anchors,coverage);
    public static IReadOnlyList<(Issue Issue,PreviewAnchor Anchor)> InspectMeasured(
        IReadOnlyList<AdministrativeCenterEvidence> evidence,IReadOnlyDictionary<Issue,PreviewAnchor> anchors,
        Action<AdministrativePositionTarget,bool>? coverage=null)
    {
        var result=new List<(Issue,PreviewAnchor)>();
        var measured=new Dictionary<AdministrativePositionRole,(AdministrativePositionTarget Target,RenderedTextLine Line)>();
        foreach(var observation in evidence)
        {
            var target=observation.Target;
            bool mapped=anchors.TryGetValue(target.Probe,out var anchor);
            // No text-only fallback: a missing Word anchor must not highlight a duplicate elsewhere.
            anchor??=new(1,"",0,0);
            var row=mapped && anchor.Page==1?observation.Row:null;
            var refs=mapped && anchor.Page==1?observation.References:observation.References.Select(_=>(RenderedTextLine?)null).ToArray();
            var assessment=AdministrativePositionRules.Assess(target,row,refs);
            coverage?.Invoke(target,assessment.Measured && AdministrativeCentering.Proven(observation));
            if(assessment.Measured && row is not null)measured[target.Role]=(target,row);
            if(assessment.Warnings.Count>0)
            {
                bool canCenter=mapped && anchor.Page==1 && AdministrativeCentering.CanOffer(observation);
                result.Add((target.Probe with {Message=string.Join(" ",assessment.Warnings),Citation=AdministrativePositionRules.Citation,
                    CanFix=canCenter,CenterFix=canCenter?new(target.Role):null},anchor));
            }
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
