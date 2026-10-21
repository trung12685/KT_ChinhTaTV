namespace ChinhTa.Core;

// Require evidence in the exported page, not just Shape metadata or a successful XML write.
public static class RenderedLineVerifier
{
    public static IReadOnlyList<LineGeometry> RetainOtherStrokes(IReadOnlyList<LineGeometry> before,
        IReadOnlyList<LineGeometry> replaced)
    {
        var preserve=before.ToList();
        foreach(var geometry in replaced)
        {
            var matches=preserve.Where(l=>Find(geometry,[l]) is not null).ToArray();
            if(matches.Length>1)
                throw new InvalidOperationException("Không ghép chắc chắn nét cũ cần thay trên trang; phải giữ bản trước.");
            if(matches.Length==1)preserve.Remove(matches[0]);
            // An invisible owned Line cannot justify removing any visible baseline stroke.
        }
        return preserve;
    }
    public static (LineGeometry? Actual, ShapeLineFix Fix, string[] Errors) Assess(LineTarget target,
        LineReference reference, ShapeLineFix expected, IEnumerable<LineGeometry> rendered, LinePolicy policy)
    {
        var actual=Find(new(expected.Left,expected.Top,expected.Width),rendered);
        var assessment=ShapeLineRules.Evaluate(target,reference,actual,expected.ShapeId,policy);
        return (actual,assessment.Fix,assessment.Errors);
    }
    public static LineGeometry? Find(LineGeometry expected, IEnumerable<LineGeometry> rendered, double tolerance = 1)
    {
        bool Finite(LineGeometry line) => double.IsFinite(line.Left) && double.IsFinite(line.Y) && double.IsFinite(line.Width) && line.Width > 0;
        if (!Finite(expected)) return null;
        return rendered.FirstOrDefault(line => Finite(line) &&
            Math.Abs(line.Left - expected.Left) <= tolerance &&
            Math.Abs(line.Left + line.Width - expected.Left - expected.Width) <= tolerance &&
            Math.Abs(line.Y - expected.Y) <= tolerance);
    }
}

