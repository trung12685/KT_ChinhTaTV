using ChinhTa.Core;

static class RenderedLineProofChecks
{
    public static void Run(Action<bool,string> assert)
    {
        var expected=new LineGeometry(100,120,80);
        assert(RenderedLineVerifier.Find(expected,[expected,expected]) is null,
            "Two overlapping XPS strokes cannot prove one uniquely rendered Line");
        var plan=new[]{new Issue(0,1,"x","x","",false,FixKind.ShapeLine,LineFix:new(null,100,120,80,LineRole.Agency))};
        assert(LinePlacementFeedback.Correct(plan,[new(20,30,40)],[new(180,176,80)]) is null,
            "Measured correction must stop when an unrelated baseline stroke disappears");
        assert(RenderedLineVerifier.Find(expected,[new(10,20,30),expected])==expected &&
            RenderedLineVerifier.Find(expected,[expected,new(10,20,30)])==expected,
            "A sole matching stroke remains accepted independently of XPS order");
        assert(RenderedLineVerifier.Find(expected,[expected,expected with {Left=100.5}]) is null &&
            RenderedLineVerifier.MatchCount(expected,[expected,expected with {Left=100.5}])==2,
            "Near-overlapping strokes within tolerance remain ambiguous rather than picking the closest");
        assert(!RenderedLineVerifier.Preserves([expected,expected],[expected]) &&
            RenderedLineVerifier.Preserves([expected,expected],[expected,expected]),
            "Preservation counts separate occurrences: two old strokes cannot be certified by one survivor");
        assert(!RenderedLineVerifier.Preserves([expected,expected with {Left=100.5}],[expected]) &&
            RenderedLineVerifier.Preserves([expected,expected with {Left=100.5}],[expected,expected with {Left=100.5}]),
            "One visible stroke cannot prove both a retained border and a newly created Line");
        var required=new[]{expected,expected with {Left=101.5}};
        var candidates=new[]{expected with {Left=100.75},expected with {Left=99.5}};
        assert(RenderedLineVerifier.Preserves(required,candidates) && RenderedLineVerifier.Preserves(required.Reverse().ToArray(),candidates.Reverse().ToArray()),
            "Overlapping tolerance intervals use a complete assignment, not order-dependent greedy matching");
        assert(!RenderedLineVerifier.Preserves([expected,new(250,300,40)],[expected,expected]) &&
            !RenderedLineVerifier.Preserves([expected],[expected with {Y=122}]),
            "Unrelated extra strokes cannot hide a lost or displaced retained stroke");
        foreach(var invalid in new[]{-1d,double.NaN,double.PositiveInfinity})
            assert(RenderedLineVerifier.Find(expected,[expected],invalid) is null &&
                !RenderedLineVerifier.Preserves([expected],[expected],invalid),"Invalid tolerance cannot turn arbitrary render evidence into success");
        assert(RenderedLineVerifier.Find(expected,[expected with {Width=double.PositiveInfinity}]) is null &&
            !RenderedLineVerifier.Preserves([expected],[expected with {Y=double.NaN}]),
            "Nonfinite rendered coordinates cannot prove uniqueness or preservation");
        assert(RenderedLineVerifier.Preserves([],[]) && RenderedLineVerifier.Preserves([],[expected]) &&
            RenderedLineVerifier.Find(expected,[expected],0)==expected,"Empty preservation and exact-match tolerance keep valid boundary behavior");
        assert(LinePlacementFeedback.Correct(plan,[new(20,30,40)],[new(20,30,40),new(180,176,80)]) is not null,
            "The bounded correction still works when every unrelated baseline stroke is actually retained");
        string folder=Path.Combine(Path.GetTempPath(),"ChinhTaStrokeProof-"+Guid.NewGuid());Directory.CreateDirectory(folder);
        try
        {
            string current=Path.Combine(folder,"working"),candidate=Path.Combine(folder,"candidate");
            File.WriteAllText(current,"approved before");File.WriteAllText(candidate,"candidate with lost border");int opened=0;bool failed=false;
            try{CheckpointSwap.Apply(current,candidate,()=>{},()=>
            {
                if(++opened==1 && !RenderedLineVerifier.Preserves([expected,expected],[expected]))
                    throw new InvalidOperationException("Lost retained occurrence");
            });}catch(InvalidOperationException){failed=true;}
            assert(failed && opened==2 && File.ReadAllText(current)=="approved before",
                "A late stroke multiplicity failure restores the whole checkpoint instead of committing the candidate");
        }
        finally{Directory.Delete(folder,true);}
    }
}
