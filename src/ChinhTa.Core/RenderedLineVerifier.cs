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
        var matches=rendered.Where(line=>Matches(expected,line,tolerance)).Take(2).ToArray();
        return matches.Length==1?matches[0]:null;
    }
    public static int MatchCount(LineGeometry expected,IEnumerable<LineGeometry> rendered,double tolerance=1)
        =>rendered.Count(line=>Matches(expected,line,tolerance));

    // Each required stroke needs a separate rendered occurrence. In particular,
    // the same old border cannot prove both preservation and a new Line.
    // Augmenting paths avoid making correctness depend on XPS enumeration order
    // when the measurement tolerance admits more than one assignment.
    public static bool Preserves(IReadOnlyList<LineGeometry> required,IReadOnlyList<LineGeometry> rendered,double tolerance=1)
    {
        if(!double.IsFinite(tolerance) || tolerance<0 || required.Count>rendered.Count ||
            required.Any(l=>!Finite(l)) || rendered.Any(l=>!Finite(l)))return false;
        var owners=Enumerable.Repeat(-1,rendered.Count).ToArray();
        bool Assign(int requirement,bool[] visited)
        {
            for(int r=0;r<rendered.Count;r++)
            {
                if(visited[r] || !Matches(required[requirement],rendered[r],tolerance))continue;
                visited[r]=true;
                if(owners[r]<0 || Assign(owners[r],visited)){owners[r]=requirement;return true;}
            }
            return false;
        }
        for(int i=0;i<required.Count;i++)if(!Assign(i,new bool[rendered.Count]))return false;
        return true;
    }
    private static bool Finite(LineGeometry line)=>double.IsFinite(line.Left) && double.IsFinite(line.Y) &&
        double.IsFinite(line.Width) && line.Width>0 && double.IsFinite(line.Left+line.Width);
    private static bool Matches(LineGeometry expected,LineGeometry line,double tolerance)=>
        double.IsFinite(tolerance) && tolerance>=0 && Finite(expected) && Finite(line) &&
        Math.Abs(line.Left-expected.Left)<=tolerance &&
        Math.Abs(line.Left+line.Width-expected.Left-expected.Width)<=tolerance && Math.Abs(line.Y-expected.Y)<=tolerance;
}

