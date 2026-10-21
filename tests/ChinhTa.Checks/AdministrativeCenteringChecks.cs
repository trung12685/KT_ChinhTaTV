using ChinhTa.Core;

public static class AdministrativeCenteringChecks
{
    public static void Run(Action<bool,string> assert)
    {
        var target=new AdministrativePositionTarget(AdministrativePositionRole.Number,new(80,"Số:        /BC-ABC"),
            [new(10,"AGENCY TEST"),new(30,"UNIT TEST")],true);
        var frame=new LineAnchorFrame(100,40,120,100,600);
        RenderedTextLine[] refs=[new(110,50,100,14,61),new(130,70,60,14,81)];
        var before=new AdministrativeCenterEvidence(target,new(125,100,90,14,111),refs,frame,[frame,frame]);
        var after=before with {Row=before.Row! with {Left=115}};
        assert(AdministrativeCentering.CanOffer(before) && !AdministrativeCentering.CanOffer(after) && AdministrativeCentering.Verify(before,after),
            "Center repair requires a real offset, accepts the actual corrected center, and retains blank text");
        assert(!AdministrativeCentering.Verify(before,before),"Unchanged off-center geometry fails even if Word reported a successful setter");
        foreach(var unsafeEvidence in new[]{before with {Target=target with {ReferencesCentered=false}},before with {Frame=null},
            before with {ReferenceFrames=[frame,null]},before with {ReferenceFrames=[frame,frame with {Left=101}]},
            before with {ReferenceFrames=[]},before with {References=[refs[0],null]},before with {Row=null},
            before with {Row=before.Row! with {Width=double.NaN}},before with {Frame=frame with {PageWidth=150}},
            before with {Row=before.Row! with {Left=95}},before with {Row=before.Row! with {Top=60,Baseline=71}},
            before with {References=[refs[0],refs[1] with {Left=140}]},
            before with {Target=target with {Row=target.Row with {Text=" Số: /BC-ABC"}}},
            before with {Target=target with {Row=target.Row with {Text="Số:\t/BC-ABC"}}}})
            assert(!AdministrativeCentering.CanOffer(unsafeEvidence),"Incomplete, conflicting, outside-cell or untrusted evidence cannot enable centering");
        foreach(var failed in new[]{after with {Row=after.Row! with {Baseline=112}},after with {Row=after.Row! with {Top=101}},
            after with {Row=after.Row! with {Width=89}},after with {Row=after.Row! with {Height=15}},
            after with {Frame=frame with {Top=41}},after with {References=[refs[0] with {Left=111},refs[1]]},
            after with {References=[refs[0] with {Baseline=62},refs[1]]},
            after with {Target=target with {Row=target.Row with {Start=81}}},
            after with {Target=target with {References=[target.References[1],target.References[0]]}},after with {Row=null}})
            assert(!AdministrativeCentering.Verify(before,failed),"Fresh geometry must preserve baseline, dimensions, reference bindings and cell frame");
        var date=before with {Target=target with {Role=AdministrativePositionRole.Date}};
        assert(AdministrativeCentering.CanOffer(date) && AdministrativeCentering.Verify(date,date with {Row=after.Row}),
            "Date centering uses both proven national/motto references and actual post-save glyphs");
        assert(!AdministrativeCentering.CanOffer(before with {Row=before.Row! with {Left=118}}),"An offset within the existing 3pt tolerance does not become an edit");
        var issue=target.Probe with {CanFix=true,CenterFix=new(target.Role)};
        assert(BatchPlan.Create([issue]).Count==1 && issue.Suggestion.Contains("Căn giữa"),"A valid position-only suggestion is selectable with a concrete action label");
        foreach(var bad in new[]{issue with {Replacement="changed"},issue with {Kind=FixKind.Text},issue with {CenterFix=null},
            issue with {CharacterFormat=new(13,false)},issue with {Format=new(13)},issue with {CenterFix=new((AdministrativePositionRole)99)}})
        {
            bool rejected=false;try{BatchPlan.Create([bad]);}catch(InvalidOperationException){rejected=true;}
            assert(rejected,"Malformed or mixed-property center payloads are rejected before mutation");
        }
        bool changed=false,restored=false;int edits=0;bool failedVerification=false;
        try
        {
            AtomicTextBatch.Apply([issue],i=>i.Original,i=>{edits++;changed=true;return i.Original;},()=>
            {
                if(!AdministrativeCentering.Verify(before,before))throw new InvalidOperationException("unmoved glyphs");
            },()=>{restored=true;changed=false;});
        }
        catch(InvalidOperationException){failedVerification=true;}
        assert(failedVerification && edits==1 && restored && !changed,"A post-save real-geometry failure rolls back the position action once");
    }
}
