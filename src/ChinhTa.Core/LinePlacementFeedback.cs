using System.Globalization;

namespace ChinhTa.Core;

public sealed class RenderedLineMismatchException(IReadOnlyList<LineGeometry> rendered)
    : InvalidOperationException("Word chưa hiển thị Line đúng tọa độ đề xuất; phải khôi phục bản trước.")
{
    public IReadOnlyList<LineGeometry> Rendered { get; } = rendered;
    public IReadOnlyList<Issue>? RebasedPlan { get; init; }
}

public sealed record LineAnchorFrame(double Left, double Top, double Width, double Height, double PageWidth);

// A single measured correction, followed by the same mandatory rendered verification.
public static class LinePlacementFeedback
{
    public static IReadOnlyList<Issue>? Correct(IReadOnlyList<Issue> plan,
        IReadOnlyList<LineGeometry> before, IReadOnlyList<LineGeometry> after)
    {
        bool Finite(LineGeometry l) => double.IsFinite(l.Left) && double.IsFinite(l.Y) && double.IsFinite(l.Width) && l.Width > 0;
        if (plan.Count == 0 || plan.Any(i => i.LineFix is null) || before.Any(l => !Finite(l)) || after.Any(l => !Finite(l))) return null;
        // Match the baseline one-to-one; an existing border/stroke cannot be used as a newly inserted Line.
        var remaining = after.ToList();
        foreach (var old in before)
        {
            var matches = remaining.Where(l => RenderedLineVerifier.Find(old, [l]) is not null).ToArray();
            if (matches.Length != 1) return null;
            remaining.Remove(matches[0]);
        }
        if (remaining.Count != plan.Count) return null;
        var expected = plan.OrderBy(i => i.LineFix!.Top).ToArray();
        var actual = remaining.OrderBy(l => l.Y).ToArray();
        for (int i = 0; i < expected.Length; i++)
        {
            var f = expected[i].LineFix!;
            if (!Finite(new(f.Left, f.Top, f.Width)) || Math.Abs(actual[i].Width - f.Width) > 1 ||
                i > 0 && (f.Top - expected[i-1].LineFix!.Top <= 2 || actual[i].Y - actual[i-1].Y <= 2)) return null;
        }
        return expected.Select((item, i) =>
        {
            var f = item.LineFix!;
            return item with {LineFix = f with {Left = 2 * f.Left - actual[i].Left, Top = 2 * f.Top - actual[i].Y}};
        }).ToArray();
    }
    public static IReadOnlyList<Issue> Rebase(IReadOnlyList<Issue> encoded,IReadOnlyList<Issue> original,IReadOnlyList<Issue> desired) =>
        desired.Select(item=>
        {
            var target=item.LineFix!;
            var old=original.Single(i=>i.LineFix!.Role==target.Role).LineFix!;
            var coordinate=encoded.Single(i=>i.LineFix!.Role==target.Role).LineFix!;
            return item with {LineFix=target with {Left=coordinate.Left+target.Left-old.Left,Top=coordinate.Top+target.Top-old.Top}};
        }).ToArray();

    // Only a new, invisible Motto in a verified right-hand table frame qualifies.
    public static IReadOnlyList<Issue>? CorrectHiddenMotto(IReadOnlyList<Issue> plan,
        IReadOnlyList<LineGeometry> before, IReadOnlyList<LineGeometry> after, LineAnchorFrame? frame)
    {
        if(plan.Count!=1 || plan[0].LineFix is not {Role:LineRole.Motto, ShapeId:null} fix || frame is null ||
            new[]{frame.Left,frame.Top,frame.Width,frame.Height,frame.PageWidth,fix.Left,fix.Top,fix.Width}.Any(v=>!double.IsFinite(v)) ||
            frame.Left<=0 || frame.Top<0 || frame.Width<=0 || frame.Height<=0 || frame.PageWidth<=0 || fix.Width<=0 ||
            frame.Width>=frame.PageWidth*.9 || fix.Left<frame.Left-1 || fix.Left+fix.Width>frame.Left+frame.Width+1 ||
            fix.Top<frame.Top || fix.Top>frame.Top+frame.Height || fix.Left+frame.Left<frame.PageWidth-1 || before.Count!=after.Count)
            return null;
        var remaining=after.ToList();
        foreach(var old in before)
        {
            var matches=remaining.Where(l=>RenderedLineVerifier.Find(old,[l]) is not null).ToArray();
            if(matches.Length!=1)return null;
            remaining.Remove(matches[0]);
        }
        if(remaining.Count!=0)return null;
        return [plan[0] with {LineFix=fix with {Left=fix.Left-frame.Left,Top=fix.Top-frame.Top}}];
    }

}

// Stored in the standard accessibility title attribute, on application-created Lines only.
// This is intended page geometry; every use still requires independent XPS evidence.
public static class LinePageIntent
{
    private const string Prefix = "ChinhTaViet.PageLine.v1;";
    public static string Format(LineGeometry line) => Prefix + string.Join(";", new[]{line.Left,line.Y,line.Width}.Select(v=>v.ToString("R",CultureInfo.InvariantCulture)));
    public static LineGeometry? Read(string? title)
    {
        if (title is null || !title.StartsWith(Prefix,StringComparison.Ordinal)) return null;
        var values=title[Prefix.Length..].Split(';');
        if(values.Length!=3 || values.Any(v=>!double.TryParse(v,NumberStyles.Float,CultureInfo.InvariantCulture,out double n)||!double.IsFinite(n)))
            throw new InvalidDataException("Invalid application Line page intent");
        var numbers=values.Select(v=>double.Parse(v,CultureInfo.InvariantCulture)).ToArray();
        if(numbers[2]<=0)throw new InvalidDataException("Invalid Line intent width");
        return new(numbers[0],numbers[1],numbers[2]);
    }
}
