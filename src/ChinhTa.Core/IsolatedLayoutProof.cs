namespace ChinhTa.Core;

public sealed record IsolatedPageSize(double Width,double Height);
public sealed record IsolatedGlyphEvidence(int Page,string Text,string Shape,double X,double Baseline);

// Comparison of an unmodified copy is a prerequisite, never a spacing diagnosis.
// No ink-box gap, font point size, or nominal Word spacing mode is a substitute
// for matching line breaks, actual glyph shaping and baseline coordinates.
public sealed class IsolatedLayoutProof
{
    private const double Tolerance=.01; // XPS units (1/96 inch), well below a point.
    private readonly IsolatedPageSize[] pages;
    private readonly IsolatedGlyphEvidence[] glyphs;
    public int PageCount=>pages.Length;
    public int GlyphRuns=>glyphs.Length;
    public IsolatedLayoutProof(IEnumerable<IsolatedPageSize> pages,IEnumerable<IsolatedGlyphEvidence> glyphs)
    {
        this.pages=pages.ToArray();this.glyphs=glyphs.ToArray();
        if(this.pages.Length==0 || this.glyphs.Length==0 || this.pages.Any(p=>!Finite(p.Width,p.Height) || p.Width<=0 || p.Height<=0) ||
            this.glyphs.Any(g=>g.Page<1 || g.Page>this.pages.Length || g.Text.Length==0 || g.Shape.Length==0 || !Finite(g.X,g.Baseline)))
            throw new InvalidOperationException("Thiếu bằng chứng trang/glyph/baseline của bản sao Word cô lập.");
    }
    private static bool Finite(params double[] values)=>values.All(double.IsFinite);
    private static bool Equal(double a,double b)=>Math.Abs(a-b)<=Tolerance;
    public void RequireSame(IsolatedLayoutProof copy)
    {
        if(pages.Length!=copy.pages.Length || glyphs.Length!=copy.glyphs.Length ||
            pages.Where((p,i)=>!Equal(p.Width,copy.pages[i].Width) || !Equal(p.Height,copy.pages[i].Height)).Any() ||
            glyphs.Where((g,i)=>g.Page!=copy.glyphs[i].Page || g.Text!=copy.glyphs[i].Text || g.Shape!=copy.glyphs[i].Shape ||
                !Equal(g.X,copy.glyphs[i].X) || !Equal(g.Baseline,copy.glyphs[i].Baseline)).Any())
            throw new InvalidOperationException("Bản sao Word chưa khớp nguồn về trang/ngắt dòng/phông/glyph/baseline; không hiệu chuẩn spacing.");
    }
}
