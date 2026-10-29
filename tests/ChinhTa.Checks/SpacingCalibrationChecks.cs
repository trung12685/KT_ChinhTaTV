using ChinhTa.Core;

public static class SpacingCalibrationChecks
{
    private const string Text="AlphaBetaGamma";
    private static readonly IsolatedPageSize[] Pages=[new(794,1123)];
    private static IsolatedLayoutProof Snapshot(double[] target,double suffix=200)=>new(Pages,
        new[]{new IsolatedGlyphEvidence(1,"Heading","font-heading",100,50)}
        .Concat(target.Select((y,i)=>new IsolatedGlyphEvidence(1,new[]{"Alpha","Beta","Gamma"}[i],"font-13pt-glyphs-"+i,100,y)))
        .Append(new(1,"Suffix","font-suffix",100,suffix)));
    private static IsolatedLayoutProof Changed(IsolatedLayoutProof before,Func<IsolatedGlyphEvidence,int,IsolatedGlyphEvidence> mutate,
        IReadOnlyList<IsolatedPageSize>? pages=null)=>new(pages??before.PageSizes,before.GlyphEvidence.Select(mutate));
    public static void Run(Action<bool,string> assert)
    {
        var source=Snapshot([100,120,140]);var single=Snapshot([98,114,130],190);var oneHalf=Snapshot([102,126,150],210);
        SpacingCalibrationResult Measure(IsolatedLayoutProof? s=null,IsolatedLayoutProof? baseline=null,IsolatedLayoutProof? lower=null,
            IsolatedLayoutProof? upper=null,string text=Text)=>SpacingCalibration.MeasureLines(s??source,baseline??s??source,lower??single,upper??oneHalf,text);
        var valid=Measure();
        assert(valid.Status==SpacingCalibrationStatus.WithinRange && valid.SourceStepsPt.SequenceEqual(new[]{15d,15}) &&
            valid.SingleStepsPt.SequenceEqual(new[]{12d,12}) && valid.OneAndHalfStepsPt.SequenceEqual(new[]{18d,18}),
            "Line calibration compares every baseline interval against measured Single/1.5 vectors in physical points");
        assert(!valid.Reason.Contains(Text,StringComparison.Ordinal) && !valid.Reason.Contains("Alpha",StringComparison.Ordinal),
            "Calibration diagnostics do not expose paragraph text in their reason");
        assert(Measure(Snapshot([100,110,120],180)).Status==SpacingCalibrationStatus.BelowSingle,
            "Exactly-style baselines below the measured Single vector are diagnosed without substituting font size");
        assert(Measure(Snapshot([100,128,156],216)).Status==SpacingCalibrationStatus.AboveOneAndHalf,
            "AtLeast-style baselines above all measured1.5 intervals are diagnosed from geometry, not the mode name");
        assert(Measure(single).Status==SpacingCalibrationStatus.WithinRange && Measure(oneHalf).Status==SpacingCalibrationStatus.WithinRange,
            "Both measured Single and1.5 boundary vectors are accepted within measurement tolerance");
        assert(Measure(Snapshot([100,115.95,131.9],192)).Status==SpacingCalibrationStatus.WithinRange,
            "Sub-tolerance baseline rounding does not invent a below-Single defect");
        void Unproven(SpacingCalibrationResult result,string description)=>assert(result.Status==SpacingCalibrationStatus.Unproven,description);
        Unproven(Measure(Snapshot([100,110,140])),"Mixed interval classifications stay manual instead of averaging into a false pass");
        Unproven(Measure(baseline:Changed(source,(g,i)=>i==0?g with {Baseline=g.Baseline+.1}:g)),
            "A clone that differs from the original cannot authorize either trial");
        Unproven(Measure(lower:Changed(single,(g,i)=>i==1?g with {Shape="substituted-font"}:g)),
            "Single trial font/shaping drift invalidates calibration");
        Unproven(Measure(upper:Changed(oneHalf,(g,i)=>i==2?g with {Text="Changed"}:g)),
            "Unchanged rendered text is required in every trial");
        Unproven(Measure(lower:Changed(single,(g,i)=>i==2?g with {X=g.X+.1}:g)),
            "Horizontal glyph drift is reflow evidence even if all baselines look valid");
        Unproven(Measure(lower:Changed(single,(g,i)=>i==0?g with {Baseline=51}:g)),
            "A trial cannot translate an unselected heading before the paragraph");
        Unproven(Measure(lower:Snapshot([40,56,72],132)),"A changed first target baseline cannot cross an unselected preceding heading");
        Unproven(Measure(lower:Snapshot([98,114,130],125)),"The target's final line cannot cross a uniformly translated following paragraph");
        Unproven(Measure(lower:Changed(single,(g,i)=>g,pages:[new(795,1123)])),
            "Trial page dimension drift stays unproven");
        Unproven(Measure(lower:Changed(single,(g,i)=>g,pages:[new(794,1123),new(794,1123)])),
            "Repagination blocks line calibration before reference-vector comparison");
        Unproven(Measure(lower:new(Pages,single.GlyphEvidence.Where((_,i)=>i!=2))),
            "A missing glyph/run cannot be covered by a plausible baseline average");
        Unproven(Measure(lower:Snapshot([98,98,130],190)),"Merged baseline groups are rejected as changed line topology");
        Unproven(Measure(lower:Snapshot([98,130,114],190)),"Non-monotonic reading/baseline order cannot produce reference measurements");
        Unproven(Measure(lower:oneHalf,upper:single),"Reversed or indistinguishable trial reference modes stay unknown");
        Unproven(Measure(upper:single),"Equal Single/1.5 vectors are insufficient evidence, even with correct source glyphs");
        Unproven(Measure(text:"BetaGamma"),"Two-line paragraphs do not satisfy the three-baseline line-calibration prerequisite");
        Unproven(Measure(text:"lphaBetaGamma"),"A substring inside the first glyph cannot be matched as a whole paragraph");
        Unproven(Measure(text:"Alpha\vBetaGamma"),"Visible-paragraph matching does not silently erase controls supplied by an unproven caller");
        Unproven(Measure(text:""),"An empty target cannot match the first convenient glyph");
        Unproven(Measure(lower:Snapshot([-100,-84,-68],-8)),"Finite but off-page reference baselines do not establish a physically rendered measurement");
        Unproven(Measure(lower:Changed(single,(g,i)=>i==2?g with {X=795}:g)),"Finite glyph coordinates beyond the page width stay unproven");
        var huge=new IsolatedLayoutProof([new(double.MaxValue,double.MaxValue)],
            [new(1,"Alpha","font-a",100,-double.MaxValue),new(1,"Beta","font-b",100,double.MaxValue/2),new(1,"Gamma","font-c",100,double.MaxValue)]);
        Unproven(Measure(huge,huge,huge,huge),"Finite inputs cannot exploit subtraction overflow to manufacture infinite reference vectors");
        var duplicated=new IsolatedLayoutProof(Pages,source.GlyphEvidence.Concat(source.GlyphEvidence.Skip(1).Take(3).Select(g=>g with {Baseline=g.Baseline+300})));
        Unproven(Measure(duplicated,duplicated,duplicated,duplicated),"Repeated rendered target paragraphs are ambiguous and never use first-match fallback");
        var splitSource=new IsolatedLayoutProof(Pages,[source.GlyphEvidence[0],new(1,"Al","font-split-a",100,100),new(1,"pha","font-split-b",120,100),source.GlyphEvidence[2],source.GlyphEvidence[3],source.GlyphEvidence[4]]);
        var splitSingle=Changed(splitSource,(g,i)=>i is 3?g with {Baseline=116}:i is 4?g with {Baseline=132}:i is 5?g with {Baseline=192}:g);
        var splitHalf=Changed(splitSource,(g,i)=>i is 3?g with {Baseline=124}:i is 4?g with {Baseline=148}:i is 5?g with {Baseline=208}:g);
        assert(Measure(splitSource,splitSource,splitSingle,splitHalf).Status==SpacingCalibrationStatus.WithinRange,
            "Several glyph runs on one baseline remain one line when every trial retains their exact grouping");
        Unproven(Measure(splitSource,splitSource,Changed(splitSingle,(g,i)=>i==2?g with {Baseline=101}:g),splitHalf),
            "Splitting one source line into two trial baselines is rejected despite equal glyph counts");
        Unproven(Measure(splitSource,splitSource,Changed(splitSingle,(g,i)=>i==2?g with {X=100}:g),splitHalf),
            "Uncertain horizontal ordering within a line cannot earn a measurement");
        var extraSuffix=new IsolatedLayoutProof(Pages,source.GlyphEvidence.Append(new(1,"Footer","font-footer",100,1000)));
        var extraLower=new IsolatedLayoutProof(Pages,single.GlyphEvidence.Append(new(1,"Footer","font-footer",100,1000)));
        var extraUpper=new IsolatedLayoutProof(Pages,oneHalf.GlyphEvidence.Append(new(1,"Footer","font-footer",100,1000)));
        Unproven(Measure(extraSuffix,extraSuffix,extraLower,extraUpper),
            "A mixed fixed-footer/translated-body suffix is outside the narrow uniform-flow proof");
        IsolatedLayoutProof TwoPages(IsolatedLayoutProof a,double otherY)=>new([new(794,1123),new(794,1123)],a.GlyphEvidence.Append(new(2,"Other page","other-font",100,otherY)));
        var twoSource=TwoPages(source,100);var twoSingle=TwoPages(single,100);var twoHalf=TwoPages(oneHalf,100);
        assert(Measure(twoSource,twoSource,twoSingle,twoHalf).Status==SpacingCalibrationStatus.WithinRange,
            "A trial may retain other pages exactly while a same-page suffix translates uniformly");
        Unproven(Measure(twoSource,twoSource,TwoPages(single,101),twoHalf),"A later page cannot shift as an unexamined consequence of calibration");
        Unproven(Measure(source,source,Changed(single,(g,i)=>i==3?g with {Page=2}:g,[new(794,1123),new(794,1123)])),
            "A target crossing onto another page blocks calibration");

        const string First="FirstAFirstB",Second="SecondASecondB";
        IsolatedLayoutProof Pair(double a1=100,double a2=120,double b1=170,double b2=190,double suffix=230)=>new(Pages,
            [new(1,"Heading","heading",100,50),new(1,"FirstA","first-a",100,a1),new(1,"FirstB","first-b",100,a2),
                new(1,"SecondA","second-a",100,b1),new(1,"SecondB","second-b",100,b2),new(1,"Suffix","suffix",100,suffix)]);
        var pair=Pair();var zero=Pair(96,116,158,178,218);
        SpacingGapCalibrationResult Gap(IsolatedLayoutProof? trial=null,IsolatedLayoutProof? original=null,IsolatedLayoutProof? clone=null,
            IsolatedLayoutProof? reference=null,string first=First,string second=Second)
            =>SpacingCalibration.MeasureGap(original??pair,clone??original??pair,trial??zero,reference??pair,first,second);
        var gap=Gap();
        assert(gap.Status==SpacingGapCalibrationStatus.AtLeastSixPoints && gap.SourceEffectPt==6 && gap.SixPointReferenceEffectPt==6,
            "Paragraph diagnostics compare source and explicit6pt reference effects after subtracting the same zero-spacing baseline");
        gap=Gap(Pair(96,116,162,182,222),reference:Pair(96,116,170,190,230));
        assert(gap.Status==SpacingGapCalibrationStatus.BelowSixPoints && gap.SourceEffectPt==3 && gap.SixPointReferenceEffectPt==6,
            "A source effect below the measured6pt reference stays below even if nominal Before+After settings could sum to6pt");
        gap=Gap(Pair(96,116,162,182,222));
        assert(gap.Status==SpacingGapCalibrationStatus.AtLeastSixPoints && gap.SourceEffectPt==3 && gap.SixPointReferenceEffectPt==3,
            "A genuine6pt setting contributing only3pt above leading is not falsely diagnosed below6 by its differential alone");
        gap=Gap(Pair(96,116,146,166,206),reference:Pair(96,116,154,174,214));
        assert(gap.Status==SpacingGapCalibrationStatus.AtLeastSixPoints && gap.SourceEffectPt==15 && gap.SixPointReferenceEffectPt==6,
            "Source effects above the measured6pt reference remain accepted without forcing an exact6pt setting");
        gap=Gap(pair,reference:Pair(100,120,178,198,238));
        assert(gap.Status==SpacingGapCalibrationStatus.BelowSixPoints && gap.SourceEffectPt==0 && gap.SixPointReferenceEffectPt==6,
            "A large source baseline boundary with zero spacing effect is not mistaken for the measured6pt reference");
        void GapUnknown(SpacingGapCalibrationResult result,string description)=>assert(result.Status==SpacingGapCalibrationStatus.Unproven && result.SourceEffectPt is null && result.SixPointReferenceEffectPt is null,description);
        GapUnknown(Gap(pair),"A6pt reference indistinguishable from the zero trial cannot classify spacing in a leading-dominated context");
        GapUnknown(Gap(Pair(96,115,158,178,218)),"Changing the first paragraph's line spacing invalidates a gap differential");
        GapUnknown(Gap(Pair(96,116,158,179,218)),"Changing the second paragraph's line spacing invalidates a gap differential");
        GapUnknown(Gap(reference:Pair(96,116,170,191,230)),"The explicit6pt reference must also preserve both internal line-spacing vectors");
        GapUnknown(Gap(Pair(96,116,190,210,250)),"A negative spacing differential is unknown rather than a manufactured negative gap defect");
        GapUnknown(Gap(clone:Changed(pair,(g,i)=>i==1?g with {Shape="different-font"}:g)),"Gap measurements also require a faithful unmodified clone");
        GapUnknown(Gap(trial:Changed(zero,(g,i)=>i==3?g with {X=101}:g)),"Gap trials cannot reflow the second paragraph horizontally");
        GapUnknown(Gap(first:Second,second:First),"Reversing adjacent paragraph anchors cannot invent a gap reference");
        GapUnknown(Gap(first:"FirstB"),"A one-line first paragraph cannot separate spacing from line leading");
        var separated=new IsolatedLayoutProof(Pages,pair.GlyphEvidence.Take(3).Append(new(1,"Intervening","other",100,145)).Concat(pair.GlyphEvidence.Skip(3)));
        GapUnknown(Gap(separated,separated,separated),"An intervening paragraph cannot be skipped to invent an adjacent pair");
        assert(!Gap().Reason.Contains("First",StringComparison.Ordinal) && !Gap().Reason.Contains("Second",StringComparison.Ordinal),
            "Paragraph-gap reports retain measurements and neutral reasons without source document text");
        var immutable=Measure();bool resultReadOnly=false;
        try{((IList<double>)immutable.SourceStepsPt)[0]=999;}catch(NotSupportedException){resultReadOnly=true;}
        assert(resultReadOnly && Measure().SourceStepsPt[0]==15,"Exposed calibration vectors cannot be mutated by a caller after validation");
        bool glyphReadOnly=false;try{((IList<IsolatedGlyphEvidence>)source.GlyphEvidence)[0]=source.GlyphEvidence[1];}catch(NotSupportedException){glyphReadOnly=true;}
        assert(glyphReadOnly && source.GlyphEvidence[0].Text=="Heading","Layout evidence views remain immutable while supplying full trial comparisons");
    }
}
