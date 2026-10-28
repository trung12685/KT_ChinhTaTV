using ChinhTa.Core;

public static class OfficeSuiteRunnerChecks
{
    private sealed class Session(string id){public string Id {get;}=id;}
    public static void Run(Action<bool,string> assert)
    {
        var cases=new[]{new OfficeSuiteCase("line","Đường kẻ"),new OfficeSuiteCase("space","Chỗ ký tay"),new OfficeSuiteCase("page","Số trang")};
        var opened=new List<string>();var checkedIds=new List<string>();var closed=new List<string>();
        var report=OfficeSuiteRunner.Run(cases,s=>{opened.Add(s.Id);return new Session(s.Id);},
            (s,c)=>checkedIds.Add(s.Id),s=>closed.Add(s.Id));
        assert(report.Passed && report.PassedCount==3 && report.FailedCount==0 && report.NotRunCount==0,
            "Only all selected independent Word suites passing earns the focused-suite PASS");
        assert(opened.SequenceEqual(cases.Select(c=>c.Id)) && checkedIds.SequenceEqual(opened) && closed.SequenceEqual(opened),
            "Each selected Word suite opens, runs and closes its own session in order");
        var error=new InvalidOperationException("synthetic Subject failure");checkedIds.Clear();closed.Clear();
        report=OfficeSuiteRunner.Run(cases,s=>new Session(s.Id),(s,c)=>{checkedIds.Add(s.Id);if(s.Id=="line")throw error;},s=>closed.Add(s.Id));
        assert(!report.Passed && report.PassedCount==2 && report.FailedCount==1 && ReferenceEquals(report.Results[0].Error,error),
            "An early Subject failure remains a failure while retaining later families' results");
        assert(checkedIds.Count==3 && closed.Count==3 && report.Results[1].Status==OfficeSuiteStatus.Pass,
            "A failed Line family cannot prevent handwriting or page families from running");
        checkedIds.Clear();closed.Clear();
        report=OfficeSuiteRunner.Run(cases,s=>s.Id=="line"?throw error:new Session(s.Id),
            (s,c)=>checkedIds.Add(s.Id),s=>closed.Add(s.Id));
        assert(!report.Passed && report.NotRunCount==1 && report.PassedCount==2 && report.Results[0].Status==OfficeSuiteStatus.NotRun,
            "Unavailable Word initialization is explicitly NOT_RUN and never a focused-suite PASS");
        assert(checkedIds.SequenceEqual(new[]{"space","page"}) && closed.SequenceEqual(checkedIds),
            "A missing session is not checked or closed and later suites use fresh sessions");
        report=OfficeSuiteRunner.Run(cases,s=>new Session(s.Id),(s,c)=>{},s=>{if(s.Id=="line")throw error;});
        assert(!report.Passed && report.FailedCount==1 && report.PassedCount==2 && ReferenceEquals(report.Results[0].Error,error),
            "A Word cleanup failure cannot turn a completed test into PASS or suppress later suites");
        var cleanup=new IOException("synthetic close failure");
        report=OfficeSuiteRunner.Run(cases,s=>new Session(s.Id),(s,c)=>{if(s.Id=="line")throw error;},s=>{if(s.Id=="line")throw cleanup;});
        assert(report.Results[0].Error is AggregateException a && a.InnerExceptions.Count==2 &&
            ReferenceEquals(a.InnerExceptions[0],error) && ReferenceEquals(a.InnerExceptions[1],cleanup),
            "Independent suite evidence retains both the test and cleanup failures");
        report=OfficeSuiteRunner.Run(cases,s=>new Session(s.Id),(s,c)=>throw new InvalidOperationException(s.Id),s=>{});
        assert(report.FailedCount==3 && report.Results.Select(r=>r.Error!.Message).SequenceEqual(cases.Select(c=>c.Id)),
            "Every selected family failure is retained without replacing it by the last failure");
        report=OfficeSuiteRunner.Run<Session>([],s=>throw error,(s,c)=>throw error,s=>throw error);
        assert(!report.Passed && report.Results.Count==0,"Selecting no Word suites cannot produce a successful validation");
        var events=new List<string>();
        report=OfficeSuiteRunner.Run(cases,s=>{events.Add("open:"+s.Id);return s.Id=="line"?throw error:new Session(s.Id);},
            (s,c)=>{events.Add("check:"+s.Id);if(s.Id=="space")throw error;},s=>events.Add("close:"+s.Id),
            result=>events.Add("record:"+result.Suite.Id+":"+result.Status));
        assert(events.SequenceEqual(new[]{"open:line","record:line:NotRun","open:space","check:space","close:space","record:space:Fail",
            "open:page","check:page","close:page","record:page:Pass"}),
            "Every initialization/test/cleanup result is recorded durably before opening the next independent Word suite");
        var reportError=new IOException("synthetic report write failure");int openedAfterReportFailure=0;bool observerPropagated=false;
        try
        {
            OfficeSuiteRunner.Run(cases,s=>{openedAfterReportFailure++;return new Session(s.Id);},(s,c)=>{},s=>{},_=>throw reportError);
        }
        catch(IOException observed) when(ReferenceEquals(observed,reportError)){observerPropagated=true;}
        assert(observerPropagated && openedAfterReportFailure==1,
            "Losing the durable suite report stops the run and propagates the I/O error rather than inventing a suite failure or PASS");
        foreach(var invalid in new[]{new[]{cases[0],cases[0]},new[]{new OfficeSuiteCase("","missing id")}})
        {
            bool rejected=false;try{OfficeSuiteRunner.Run(invalid,s=>new Session(s.Id),(s,c)=>{},s=>{});}catch(ArgumentException){rejected=true;}
            assert(rejected,"Duplicate or blank Word suite identifiers are rejected before opening Word");
        }
    }
}
