using ChinhTa.Desktop;

public static class OfficeReaderCheckPolicyChecks
{
    public static void Run(Action<bool,string> assert)
    {
        int compared=0;var notes=new List<string>();
        OfficeReaderCheckPolicy.CompareIfSelected(false,()=>{compared++;throw new Exception("An explicitly disabled experiment ran");},notes.Add);
        assert(compared==0 && notes.Single().StartsWith("NOT_RUN ",StringComparison.Ordinal) && !notes[0].StartsWith("PASS ",StringComparison.Ordinal),
            "Default Office validation never forces the disabled experimental reader on and explicitly records its untested scope");
        OfficeReaderCheckPolicy.CompareIfSelected(true,()=>compared++,notes.Add);
        assert(compared==1 && notes.Count==1,"Selecting the experimental reader requires its comparison rather than silently skipping it");
        bool failed=false;
        try{OfficeReaderCheckPolicy.CompareIfSelected(true,()=>throw new InvalidOperationException("mismatch"),notes.Add);}
        catch(InvalidOperationException){failed=true;}
        assert(failed && notes.Count==1,"An explicitly selected experimental comparison failure remains a failure");
        var result=new OfficeCheckResult(true,"last functional stage","synthetic");
        assert(!result.ExperimentalReading && result.ReaderScope.Contains("Chưa chạy") &&
            (result with {ExperimentalReading=true}).ReaderScope.Contains("đã chọn"),
            "A legacy Office PASS carries an explicit untested-experiment scope for the visible result");
        OfficeReadComparisonEvidence.RequireEqual("{\"Issues\":[]}","{\"Issues\":[]}","same","old.json","new.json");
        assert(true,"Identical comparison snapshots still pass the exact existing equality requirement");
        foreach(var sample in new[]{
            ("{\"Issues\":[{\"CanFix\":true}]}","{\"Issues\":[{\"CanFix\":false}]}","$.Issues[0].CanFix"),
            ("{\"Shapes\":[]}","{\"Shapes\":[{}]}","$.Shapes.Length"),
            ("{\"MeasuredLineIssues\":[{\"Left\":100.0}]}","{\"MeasuredLineIssues\":[{\"Left\":100.00000001}]}","$.MeasuredLineIssues[0].Left"),
            ("{\"Targets\":[{\"Start\":3}]}","{\"Targets\":[{}]}","$.Targets[0].Start"),
            ("{\"Issues\":[]}","{\"Issues\":null}","$.Issues")})
        {
            Exception? failure=null;
            try{OfficeReadComparisonEvidence.RequireEqual(sample.Item1,sample.Item2,"sample","old.json","new.json");}catch(InvalidOperationException e){failure=e;}
            assert(failure is not null && failure.Message.Contains(sample.Item3,StringComparison.Ordinal) &&
                failure.Data["ChinhTa.ReadComparison"] is string detail && detail.Contains("old.json") && detail.Contains("new.json"),
                "Exact reader differences in permissions, counts, tiny XPS coordinates, anchors or types fail with a precise JSON path and both evidence files");
        }
        failed=false;
        try{OfficeReadComparisonEvidence.RequireEqual("{\"a\":1,\"b\":2}","{\"b\":2,\"a\":1}","order","a","b");}catch(InvalidOperationException){failed=true;}
        assert(failed,"Diagnostic path extraction does not relax strict snapshot equality or hide representation differences");
    }
}
