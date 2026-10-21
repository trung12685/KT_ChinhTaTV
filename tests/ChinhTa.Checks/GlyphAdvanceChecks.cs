using ChinhTa.Core;

public static class GlyphAdvanceChecks
{
    public static void Run(Action<bool,string> assert)
    {
        var advances=new[]{8d,7d,6.23162109375};
        var all=GlyphAdvanceRange.Read(advances,null,3,0,3)!;
        assert(Math.Abs(all.End-21.23162109375)<1e-10,"Full geometric range includes the final glyph advance");
        var tail=GlyphAdvanceRange.Read(advances,null,3,2,3)!;
        assert(tail.Start==15 && Math.Abs(tail.End-tail.Start-6.23162109375)<1e-10,"Last character has its complete typographic width");
        assert(GlyphAdvanceRange.Read([10d,7d],[0,0,1],3,0,2)==new AdvanceRange(0,10),"Multiple code units in one shaping cluster share the complete advance");
        assert(GlyphAdvanceRange.Read([5d,5d,7d],[0,2],2,0,1)==new AdvanceRange(0,10),"One character represented by multiple glyphs includes all advances");
        assert(GlyphAdvanceRange.Read([10d,7d],[0,0,1],3,1,2) is null,"Partial shaping-cluster boundaries do not invent a caret edge");
        assert(GlyphAdvanceRange.Read([10d],[0,0],2,0,2)==new AdvanceRange(0,10),"A full surrogate-pair cluster can be measured geometrically");
        assert(GlyphAdvanceRange.Read([double.NaN],null,1,0,1) is null && GlyphAdvanceRange.Read([-1d],null,1,0,1) is null,
            "Non-finite or negative glyph advances fail closed");
        assert(GlyphAdvanceRange.Read([10d,7d],[1,0],2,0,2) is null && GlyphAdvanceRange.Read([10d],null,2,0,2) is null,
            "Invalid or ambiguous glyph maps do not yield reference measurements");
        var original=new Issue(0,1,"x","x","",false,FixKind.ShapeLine,LineFix:new(2,333.67,93.76,175.94,LineRole.Motto));
        var encoded=original with {LineFix=original.LineFix! with {Left=57.38,Top=37.12}};
        var desired=original with {LineFix=original.LineFix! with {Left=337.67,Width=182.94}};
        var feedback=LinePlacementFeedback.Correct([original],[],[new(333.67,93.76,175.94)])!;
        var retained=LinePlacementFeedback.Rebase(feedback,[original],[encoded]);
        var final=LinePlacementFeedback.Rebase(retained,[original],[desired])[0].LineFix!;
        assert(Math.Abs(final.Left-61.38)<.001 && Math.Abs(final.Top-37.12)<.001 && final.Width==182.94,
            "Rebased retry preserves the existing encoding translation without applying it twice");
        double explicitAdvance=(55.556*5+24.786)*14.04/100;
        var wordRun=GlyphAdvanceRange.Read([14.04*.55556,14.04*.55556,14.04*.24786,14.04*.55556,14.04*.55556,14.04*.55556,6.23162109375],null,7,0,7)!;
        assert(Math.Abs(453.22+wordRun.End-325.99-175.94188749375)<.00001 && Math.Abs(453.22+explicitAdvance-325.99-169.7102664)<.00001,
            "Word XPS trailing c explains the exact 169.71 versus 175.94pt measurement difference");
    }
}
