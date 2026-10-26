using ChinhTa.Core;

public static class RepairProgressChecks
{
    public static void Run(Action<bool,string> assert)
    {
        var events=new List<RepairProgressUpdate>();var progress=new RepairProgressTracker(5,events.Add);
        progress.Show(RepairActivity.Preparing,"prepare");progress.Confirm(2);progress.Show(RepairActivity.Measuring,"new XPS");progress.Confirm(3);
        assert(events.Last().Verified==5 && events.All(p=>p.Fraction<1),"All provisional stage counts still leave room for whole-batch verification and rescan");
        progress.Show(RepairActivity.Restoring,"restore");
        assert(events.Last().Verified==0 && events.Last().Activity==RepairActivity.Restoring,"Rollback resets provisional verified counts instead of leaving a successful green result");
        var nestedEvents=new List<RepairProgressUpdate>();
        var outer=RepairProgressTracker.ForSelection(null,7,nestedEvents.Add);
        var page=RepairProgressTracker.ForSelection(outer,6,_=>throw new Exception("Nested progress must retain the outer reporter"));
        page.Confirm(1);var body=RepairProgressTracker.ForSelection(page,5,null);body.Confirm(5);outer.Confirm(1);
        assert(ReferenceEquals(outer,page) && ReferenceEquals(page,body) && nestedEvents.All(e=>e.Total==7) &&
            nestedEvents.Select(e=>e.Verified).SequenceEqual(new[]{1,6,7}) && nestedEvents.All(e=>e.Fraction<1),
            "PAGE, page setup and body stages share one seven-item progress total and each item is confirmed once");
        body.Show(RepairActivity.Restoring,"restore whole selection");
        assert(outer.Verified==0 && nestedEvents.Last().Total==7,"A nested-stage failure clears the whole selection progress without shrinking its total");
        var text=new Issue(0,1,"5","3","",false,FixKind.OutlineOrder);
        var line=new Issue(10,3,"ABC","ABC","",false,FixKind.ShapeLine,LineFix:new(null,100,100,90,LineRole.Agency));
        string document="5";bool committed=false;
        var brokenView=new RepairProgressTracker(2,_=>throw new InvalidOperationException("closed view"));
        var result=StagedRepairBatch.Apply([new(text,"3"),new(line,"ABC")],_=>brokenView.Show(RepairActivity.Measuring,"measure"),
            request=>request.Requested,plan=>{if(plan[0].Kind==FixKind.OutlineOrder)document="3";brokenView.Confirm(plan.Count);},
            ()=>{if(document!="3")throw new Exception("bad text");},()=>committed=true,()=>document="5");
        assert(committed && document=="3" && result.Applied==2,"A failed progress observer cannot alter real staged edit/verification/commit behavior");
        bool failed=false;document="5";committed=false;
        try
        {
            StagedRepairBatch.Apply([new(text,"3"),new(line,"ABC")],_=>{},r=>r.Requested,
                plan=>{document="3";progress.Confirm(plan.Count);},()=>throw new InvalidOperationException("verification failed"),
                ()=>committed=true,()=>{progress.Show(RepairActivity.Restoring,"restore");document="5";});
        }
        catch(InvalidOperationException){failed=true;}
        assert(failed && !committed && document=="5" && events.Last().Verified==0,"Verification failure still restores the entire staged action with progress enabled");
    }
}
