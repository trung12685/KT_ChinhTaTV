namespace ChinhTa.Core;

public record AdministrativeCenterFix(AdministrativePositionRole Role);
public record AdministrativeCenterEvidence(AdministrativePositionTarget Target,RenderedTextLine? Row,
    IReadOnlyList<RenderedTextLine?> References,LineAnchorFrame? Frame,IReadOnlyList<LineAnchorFrame?> ReferenceFrames);

public static class AdministrativeCentering
{
    private const double Slack=.5;
    private static bool FrameValid(LineAnchorFrame? f)=>f is not null && double.IsFinite(f.Left) &&
        double.IsFinite(f.Top) && double.IsFinite(f.Width) && double.IsFinite(f.Height) && double.IsFinite(f.PageWidth) &&
        f.Left>=0 && f.Top>=0 && f.Width>0 && f.Height>0 && f.PageWidth>0 && f.Left+f.Width<=f.PageWidth+Slack;
    private static bool SameFrame(LineAnchorFrame? a,LineAnchorFrame? b)=>FrameValid(a) && FrameValid(b) &&
        Math.Abs(a!.Left-b!.Left)<=Slack && Math.Abs(a.Top-b.Top)<=Slack && Math.Abs(a.Width-b.Width)<=Slack &&
        Math.Abs(a.Height-b.Height)<=Slack && Math.Abs(a.PageWidth-b.PageWidth)<=Slack;
    private static bool Contains(LineAnchorFrame f,RenderedTextLine line)=>line.Left>=f.Left-Slack &&
        line.Left+line.Width<=f.Left+f.Width+Slack && line.Top>=f.Top-Slack && line.Bottom<=f.Top+f.Height+Slack;
    public static double ReferenceCenter(AdministrativeCenterEvidence evidence)=>
        evidence.Target.Role==AdministrativePositionRole.Date?evidence.References.Average(r=>r!.Center):
        (evidence.References.Min(r=>r!.Left)+evidence.References.Max(r=>r!.Left+r.Width))/2;
    public static bool Proven(AdministrativeCenterEvidence e)
    {
        if(!Enum.IsDefined(e.Target.Role) || e.Target.Row.Text.Length==0 || !e.Target.ReferencesCentered || e.Target.Row.Text!=e.Target.Row.Text.Trim() ||
            e.Target.Row.Text.IndexOfAny(['\r','\n','\a','\t'])>=0 || !FrameValid(e.Frame) ||
            !AdministrativePositionRules.Assess(e.Target,e.Row,e.References).Measured ||
            e.ReferenceFrames.Count!=e.References.Count || e.ReferenceFrames.Any(f=>!SameFrame(e.Frame,f)))return false;
        var row=e.Row!;var frame=e.Frame!;
        return Contains(frame,row) && e.References.All(r=>Contains(frame,r!) &&
            Math.Abs(r!.Center-(frame.Left+frame.Width/2))<=AdministrativePositionRules.Tolerance) &&
            row.Top>=e.References.Max(r=>r!.Bottom)-Slack;
    }
    public static bool CanOffer(AdministrativeCenterEvidence e)=>Proven(e) &&
        Math.Abs(e.Row!.Center-ReferenceCenter(e))>AdministrativePositionRules.Tolerance;
    // Acceptance requires fresh geometry, unchanged references/frame/baseline, and the actual final center.
    public static bool Verify(AdministrativeCenterEvidence before,AdministrativeCenterEvidence after)
    {
        if(!Proven(before) || !Proven(after) || before.Target.Role!=after.Target.Role ||
            before.Target.Row!=after.Target.Row || !before.Target.References.SequenceEqual(after.Target.References) ||
            !SameFrame(before.Frame,after.Frame) ||
            Math.Abs(after.Row!.Center-ReferenceCenter(after))>AdministrativePositionRules.Tolerance ||
            Math.Abs(before.Row!.Baseline-after.Row.Baseline)>Slack ||
            Math.Abs(before.Row.Top-after.Row.Top)>Slack || Math.Abs(before.Row.Width-after.Row.Width)>Slack ||
            Math.Abs(before.Row.Height-after.Row.Height)>Slack)return false;
        return before.References.Zip(after.References).All(pair=>
            Math.Abs(pair.First!.Left-pair.Second!.Left)<=Slack && Math.Abs(pair.First.Top-pair.Second.Top)<=Slack &&
            Math.Abs(pair.First.Width-pair.Second.Width)<=Slack && Math.Abs(pair.First.Height-pair.Second.Height)<=Slack &&
            Math.Abs(pair.First.Baseline-pair.Second.Baseline)<=Slack);
    }
}
