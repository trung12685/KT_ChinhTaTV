namespace ChinhTa.Core;

// Require evidence in the exported page, not just Shape metadata or a successful XML write.
public static class RenderedLineVerifier
{
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
