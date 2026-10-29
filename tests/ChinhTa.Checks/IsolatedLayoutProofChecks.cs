using ChinhTa.Core;

public static class IsolatedLayoutProofChecks
{
    public static void Run(Action<bool,string> assert)
    {
        IsolatedPageSize[] pages=[new(794,1123)];
        IsolatedGlyphEvidence[] glyphs=[new(1,"Dòng thứ nhất","font:verified|glyphs:1,2|advances:10,11",90,100),new(1,"Dòng thứ hai","font:verified|glyphs:3,4|advances:12,13",90,120)];
        var original=new IsolatedLayoutProof(pages,glyphs);original.RequireSame(new(pages,glyphs));
        assert(original.PageCount==1 && original.GlyphRuns==2,"Unmodified same-font multi-line baseline is comparable without guessing Single from font points");
        void Rejected(IsolatedPageSize[] p,IsolatedGlyphEvidence[] g,string reason)
        {
            bool failed=false;try{original.RequireSame(new(p,g));}catch(InvalidOperationException){failed=true;}assert(failed,reason);
        }
        Rejected([new(794,1123),new(794,1123)],glyphs,"Repagination invalidates clone baseline before calibration");
        Rejected([new(795,1123)],glyphs,"Paper size changes invalidate otherwise identical glyphs");
        Rejected(pages,[glyphs[0] with {Text="Dòng thứ"},glyphs[1] with {Text="nhất Dòng thứ hai"}],"Equal concatenated story text with changed line breaks is not a faithful clone");
        Rejected(pages,[glyphs[0],glyphs[1] with {Baseline=120.1}],"A changed baseline fails even when ink boxes or font size appear unchanged");
        Rejected(pages,[glyphs[0] with {Shape="substituted-font"},glyphs[1]],"Font substitution cannot earn a clone-equivalence proof");
        Rejected(pages,[glyphs[0] with {Shape=glyphs[0].Shape+"|different-cluster"},glyphs[1]],"Changed shaping/advances/cluster mapping invalidates calibration input");
        Rejected(pages,[glyphs[1],glyphs[0]],"Reading/drawing order is preserved, not sorted away to manufacture equivalence");
        Rejected(pages,[],"A zero-glyph run is not successful calibration evidence");
        Rejected(pages,[glyphs[0] with {Baseline=double.NaN},glyphs[1]],"Non-finite baseline must fail closed");
        Rejected(pages,[glyphs[0] with {X=double.PositiveInfinity},glyphs[1]],"Infinite horizontal coordinate must fail closed");
        Rejected(pages,[glyphs[0] with {Page=2},glyphs[1]],"Glyphs cannot refer to a page missing from the snapshot");
        pages[0]=new(1,1);glyphs[0]=glyphs[0] with {Text="mutated caller buffer"};
        original.RequireSame(new([new(794,1123)],[new(1,"Dòng thứ nhất","font:verified|glyphs:1,2|advances:10,11",90,100),glyphs[1]]));
        assert(true,"The comparison holds an immutable snapshot even when caller arrays later change");
    }
}
