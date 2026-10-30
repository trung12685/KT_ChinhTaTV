using System.IO;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class IndependentTextSuiteChecks
{
    public static void Run(Action<bool,string> assert)
    {
        var cases=OfficeFocusedCheck.Cases;
        var retained=cases.Single(s=>s.Id=="retained-marks");
        var annotated=cases.Single(s=>s.Id=="annotated-text");
        assert(OfficeFocusedCheck.Scenario(retained.Id).Method.DeclaringType==typeof(OfficeRetainedMarkCheck) &&
            OfficeFocusedCheck.Scenario(retained.Id).Method.Name==nameof(OfficeRetainedMarkCheck.Run) &&
            OfficeFocusedCheck.Scenario(annotated.Id).Method.DeclaringType==typeof(OfficeAnnotatedTextCheck) &&
            OfficeFocusedCheck.Scenario(annotated.Id).Method.Name==nameof(OfficeAnnotatedTextCheck.Run),
            "The standalone text suites resolve directly to their actual Word fixtures, without invoking an earlier technical-text scenario");
        assert(cases.All(s=>OfficeFocusedCheck.Scenario(s.Id) is not null),
            "Every selectable Word suite has an explicit production scenario route before Word starts");
        bool rejected=false;try{OfficeFocusedCheck.Scenario("unknown-text-family");}catch(InvalidOperationException){rejected=true;}
        assert(rejected,"An unknown Word scenario cannot silently resolve to an unrelated fixture");

        string all=string.Join(",",cases.Select(s=>s.Id));
        assert(WordCheckCommand.TryParse(["--word-check",all],out var every) && every.SequenceEqual(cases) && every.Count==19,
            "The explicit command list can select every one of the nineteen independent Word families in requested order");
        assert(!WordCheckCommand.TryParse(["--word-check","all"],out _) &&
            !WordCheckCommand.TryParse(["--word-check",retained.Id+","+retained.Id],out _) &&
            !WordCheckCommand.TryParse(["--word-check",annotated.Id+",unknown-text-family"],out _),
            "Adding standalone families preserves explicit-only selection and rejects ambiguous all, duplicate and unknown IDs before Word opens");
        assert(WordCheckCommand.Usage.Contains(retained.Id,StringComparison.Ordinal) &&
            WordCheckCommand.Usage.Contains(annotated.Id,StringComparison.Ordinal),
            "The command help discovers both standalone text suite IDs from the production catalog");

        string folder=Path.Combine(Path.GetDirectoryName(Diagnostics.LogPath)!,"OfficeGroups-"+Guid.NewGuid().ToString("N"));
        foreach(var suite in new[]{retained,annotated})
        foreach(var status in new[]{OfficeSuiteStatus.Pass,OfficeSuiteStatus.Fail,OfficeSuiteStatus.NotRun})
        {
            var result=WordCheckCommand.Execute(["--word-check",suite.Id],selected=>
                new(new([new(selected.Single(),status)]),folder));
            string token=status==OfficeSuiteStatus.Pass?"PASS":status==OfficeSuiteStatus.Fail?"FAIL":"NOT_RUN";
            assert(result.ExitCode==(status==OfficeSuiteStatus.Pass?0:1) && result.Folder==folder &&
                result.Output.Contains("SELECTED "+suite.Id,StringComparison.Ordinal) &&
                result.Output.Contains(token+" — "+suite.Name,StringComparison.Ordinal) &&
                result.Output.Contains(OfficeFocusedCheck.Scope,StringComparison.Ordinal),
                "A standalone fixture keeps its own actual result and limited scope instead of borrowing a technical-text PASS: "+suite.Id+"/"+status);
        }

        foreach(bool failOpen in new[]{false,true})
        {
            var opened=new List<(string Id,object Session)>();var checkedIds=new List<string>();var closed=new List<object>();
            var observed=new List<OfficeSuiteResult>();OfficeSuiteReport? report=null;
            var primary=new IOException("managed earlier technical-text failure");
            var unavailable=new IOException("managed retained-mark session unavailable");
            var result=WordCheckCommand.Execute(["--word-check","technical-text,retained-marks,annotated-text"],selected=>
            {
                report=OfficeSuiteRunner.Run(selected,suite=>
                {
                    if(failOpen && suite.Id==retained.Id)throw unavailable;
                    var session=new object();opened.Add((suite.Id,session));return session;
                },(_,suite)=>
                {
                    checkedIds.Add(suite.Id);
                    if(suite.Id=="technical-text")throw primary;
                },closed.Add,observed.Add);
                return new(report,folder);
            });
            assert(checkedIds.SequenceEqual(failOpen?new[]{"technical-text",annotated.Id}:new[]{"technical-text",retained.Id,annotated.Id}) &&
                opened.Select(p=>p.Session).Distinct().Count()==opened.Count && closed.SequenceEqual(opened.Select(p=>p.Session)),
                "An earlier technical-text failure cannot suppress a later standalone fixture; each opened family receives and closes its own session");
            assert(report is not null && report.Results.Select(row=>row.Status).SequenceEqual(new[]{OfficeSuiteStatus.Fail,
                failOpen?OfficeSuiteStatus.NotRun:OfficeSuiteStatus.Pass,OfficeSuiteStatus.Pass}) &&
                ReferenceEquals(report.Results[0].Error,primary) &&
                (!failOpen || ReferenceEquals(report.Results[1].Error,unavailable)) && observed.SequenceEqual(report.Results),
                "Independent Word evidence preserves the earlier failure, initialization NOT_RUN and later PASS in request order");
            assert(result.ExitCode==1 && result.Output.Contains("RESULT SELECTED_FAIL",StringComparison.Ordinal) &&
                result.Output.Contains("PASS — "+annotated.Name,StringComparison.Ordinal) &&
                result.Output.Contains((failOpen?"NOT_RUN":"PASS")+" — "+retained.Name,StringComparison.Ordinal) &&
                !result.Output.Contains("RESULT SELECTED_PASS",StringComparison.Ordinal),
                "Later standalone fixture success cannot upgrade a mixed command to PASS or hide an unrun family");
        }
    }
}
