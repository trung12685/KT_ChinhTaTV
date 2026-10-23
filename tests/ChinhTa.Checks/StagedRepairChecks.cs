using ChinhTa.Core;

public static class StagedRepairChecks
{
    public static void Run(Action<bool,string> assert)
    {
        var marker=new Issue(40,2,"- ","","",false,FixKind.OutlineMarker);
        var order=new Issue(30,1,"5","3","",false,FixKind.OutlineOrder);
        var line=new Issue(100,4,"MỐC!","MỐC!","",false,FixKind.ShapeLine,LineFix:new(null,100,100,90,LineRole.Subject));
        var center=new Issue(70,3,"Số:","Số:","",false,FixKind.AdministrativePosition,CenterFix:new(AdministrativePositionRole.Number));
        bool Reject(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
        var requested=StagedRepairBatch.Prepare([line,order,marker,center]);
        assert(requested.Count==4 && requested.Contains(line),"A selection may contain outline text, centering and Lines without validating them as one overlapping CP batch");
        assert(Reject(()=>StagedRepairBatch.Prepare([line,line with {Start=110}])) &&
            Reject(()=>StagedRepairBatch.Prepare([center,center with {Start=80}])) &&
            Reject(()=>StagedRepairBatch.Prepare([marker with {CanFix=false},line])),
            "Duplicate geometry roles and manual warnings are rejected before a staged write");
        var overlapping=order with {Start=101,Original="Ố",Replacement="Ô"};
        assert(StagedRepairBatch.Prepare([line,overlapping]).Count==2,"Different stages may overlap a reference while each stage still validates its own ranges");
        assert(Reject(()=>StagedRepairBatch.Prepare([order,order with {Replacement="4"},line])),
            "Real text conflicts within one stage remain blocked");
        string story="PREFIXMỐC!TAIL";
        var target=line with {Start=20};var inside=order with {Start=21,Original="Ố",Replacement="Ô"};
        string Prefix(Issue i)=>i==target?"PREFIX":"PREFIXM";
        assert(StagedRepairBatch.ExpectedReference(target,[inside],story,Prefix)=="MÔC!",
            "Expected reference uses the actual Word prefix, even when Word CP differs from string offset");
        assert(StagedRepairBatch.ExpectedReference(target,[],story,Prefix)=="MỐC!" &&
            Reject(()=>StagedRepairBatch.ExpectedReference(target,[inside with {Start=19,Length=3,Original="XMỐ",Replacement="Y"}],story,Prefix)),
            "Unchanged references remain exact and edits crossing a deferred target boundary are rejected");
        assert(Reject(()=>StagedRepairBatch.ExpectedReference(target,[inside],story,_=>"wrong")),
            "A stale or fabricated Word prefix cannot rebind a later target");
        var requests=requested.Select(i=>new DeferredRepair(i,i.Original)).ToArray();
        var calls=new List<string>();int undo=0;string text="original";double width=90;
        Issue? Resolve(DeferredRepair r)=>r.Requested.LineFix is not null?r.Requested with {Start=88,LineFix=r.Requested.LineFix with {Width=112}}:
            r.Requested with {Start=68};
        var result=StagedRepairBatch.Apply(requests,s=>calls.Add("measure:"+s),Resolve,plan=>
        {
            var stage=StagedRepairBatch.Stage(plan[0]);calls.Add("apply:"+stage);
            if(stage==RepairStage.Text){text="fixed";assert(plan.Count==2,"Both outline families run together in the text stage");}
            if(stage==RepairStage.Line)width=plan.Single().LineFix!.Width;
        },()=>calls.Add("verify"),()=>{calls.Add("commit");undo++;},()=>{text="original";calls.Add("restore");});
        assert(calls.SequenceEqual(new[]{"apply:Text","measure:Center","apply:Center","measure:Line","apply:Line","verify","commit"}) &&
            result==new RepairBatchResult(4,4,0) && width==112 && undo==1 && text=="fixed",
            "Stages use fresh CPs and geometry in order, verify the whole result and publish one Undo only");
        foreach(var invalid in new[]{line with {CanFix=false},line with {Original="OTHER",Length=5},line with {LineFix=line.LineFix! with {Role=LineRole.Motto}},
            center,line with {Kind=FixKind.Text}})
        {
            bool restored=false,wroteLine=false;
            assert(Reject(()=>StagedRepairBatch.Apply([new(marker,marker.Replacement),new(line,line.Original)],_=>{},_=>invalid,
                plan=>{if(plan[0].LineFix is not null)wroteLine=true;},()=>{},()=>{},()=>restored=true)) && restored && !wroteLine,
                "Fresh resolutions cannot add a manual, changed-text, different-role, different-stage or different-kind repair");
        }
        int writes=0;bool verified=false;
        var settled=StagedRepairBatch.Apply([new(marker,marker.Replacement),new(line,line.Original)],_=>{},_=>null,
            _=>writes++,()=>verified=true,()=>{},()=>{});
        assert(writes==1 && verified && settled==new RepairBatchResult(2,1,1),
            "A positively proven already-satisfied target is not rewritten and is reported separately");
        foreach(string fail in new[]{"Text","Center","Line","measure","verify","commit"})
        {
            string current="original";int commits=0,restores=0;
            bool failed=Reject(()=>StagedRepairBatch.Apply(requests,_=>{if(fail=="measure")throw new InvalidOperationException();},Resolve,
                plan=>{current="changed";if(StagedRepairBatch.Stage(plan[0]).ToString()==fail)throw new InvalidOperationException();},
                ()=>{if(fail=="verify")throw new InvalidOperationException();},
                ()=>{if(fail=="commit")throw new InvalidOperationException();commits++;},
                ()=>{current="original";restores++;}));
            assert(failed && current=="original" && commits==0 && restores==1,"Failure at "+fail+" restores all prior stages and publishes no Undo");
        }
        var targetLine=new LineTarget(LineRole.Subject,200,line.Original,190,220,true);
        var measuredLine=line with {Start=200,LineFix=line.LineFix! with {Width=112}};
        var geometry=new StagedGeometryReview([targetLine],[measuredLine],[],[]);
        var lineRequest=new DeferredRepair(line,line.Original);
        assert(StagedGeometryResolver.Resolve(lineRequest,geometry)==measuredLine,
            "Deferred Line resolution returns the newly measured target and its current CP/width");
        foreach(var broken in new[]{geometry with {Lines=[]},geometry with {Lines=[targetLine,targetLine]},
            geometry with {Lines=[targetLine with {Text="changed"}]},
            geometry with {LineIssues=[measuredLine with {CanFix=false,LineFix=null}]},
            geometry with {LineIssues=[measuredLine,measuredLine]}})
            assert(Reject(()=>StagedGeometryResolver.Resolve(lineRequest,broken)),
                "A lost/duplicate/changed Line target or blocked measurement cannot be counted as repaired");
        assert(StagedGeometryResolver.Resolve(lineRequest,geometry with {LineIssues=[]}) is null,
            "Only a uniquely bound target with a positive fresh inspector result can already pass");
        var frame=new LineAnchorFrame(100,40,120,100,600);
        var centerTarget=new AdministrativePositionTarget(AdministrativePositionRole.Number,new(98,"Số:"),
            [new(10,"AGENCY"),new(30,"UNIT")],true);
        var centerEvidence=new AdministrativeCenterEvidence(centerTarget,new(125,100,90,14,111),
            [new(110,50,100,14,61),new(130,70,60,14,81)],frame,[frame,frame]);
        var newCenter=center with {Start=98};var centerRequest=new DeferredRepair(center,center.Original);
        var centerReview=geometry with {Centers=[centerEvidence],CenterIssues=[newCenter]};
        assert(StagedGeometryResolver.Resolve(centerRequest,centerReview)==newCenter,
            "Deferred centering uses current evidence and the fresh matching repair");
        assert(StagedGeometryResolver.Resolve(centerRequest,centerReview with {Centers=[centerEvidence with {Row=centerEvidence.Row! with {Left=115}}],CenterIssues=[]}) is null,
            "A center that settles after font/text correction is accepted only with proven current geometry");
        foreach(var broken in new[]{centerReview with {Centers=[]},centerReview with {Centers=[centerEvidence,centerEvidence]},
            centerReview with {Centers=[centerEvidence with {Row=null}]},centerReview with {Centers=[centerEvidence with {Frame=null}]},
            centerReview with {Centers=[centerEvidence with {Target=centerTarget with {Row=new(98,"other")}}]},
            centerReview with {CenterIssues=[]},centerReview with {CenterIssues=[newCenter with {CanFix=false}]}})
            assert(Reject(()=>StagedGeometryResolver.Resolve(centerRequest,broken)),
                "Missing, clipped, changed, duplicate or manually blocked centering evidence fails the whole action");
        var savedCenter=centerEvidence with {Target=centerTarget with {Row=centerTarget.Row with {Start=101},
            References=centerTarget.References.Select(r=>r with {Start=r.Start+1}).ToArray()}};
        assert(StagedGeometryResolver.PreservesCenters([centerEvidence],[savedCenter]),
            "Line reopen may rebind Word CPs while preserving actual selected and unselected header geometry");
        foreach(var drift in new[]{savedCenter with {Row=savedCenter.Row! with {Left=126}},savedCenter with {Row=null},
            savedCenter with {Frame=null},savedCenter with {References=[savedCenter.References[0],null]},
            savedCenter with {Target=savedCenter.Target with {Row=new(101,"Changed")}}})
            assert(!StagedGeometryResolver.PreservesCenters([centerEvidence],[drift]),
                "Final Line verification cannot conceal collateral movement or missing evidence in a header row");
        assert(!StagedGeometryResolver.PreservesCenters([centerEvidence],[]) &&
            !StagedGeometryResolver.PreservesCenters([centerEvidence],[savedCenter,savedCenter]) &&
            StagedGeometryResolver.PreservesCenters([],[]),"A missing/duplicate header target cannot pass the inter-stage preservation guard");
        var cause=new InvalidOperationException("apply");var recovery=new IOException("restore");bool aggregate=false;
        try{StagedRepairBatch.Apply(requests,_=>{},Resolve,_=>throw cause,()=>{},()=>{},()=>throw recovery);}
        catch(AggregateException error){aggregate=error.InnerExceptions.SequenceEqual(new Exception[]{cause,recovery});}
        assert(aggregate,"A failed outer rollback preserves both causes instead of reporting success");
        bool touched=false;
        assert(Reject(()=>StagedRepairBatch.Apply([new(line,line.Original),new(line,line.Original)],_=>touched=true,Resolve,
            _=>touched=true,()=>touched=true,()=>touched=true,()=>touched=true)) && !touched,
            "Malformed duplicate selections fail before measurements, writes, commit or rollback");
    }
}
