namespace ChinhTa.Core;

public enum OfficeSuiteStatus { Pass, Fail, NotRun }
public sealed record OfficeSuiteCase(string Id,string Name);
public sealed record OfficeSuiteResult(OfficeSuiteCase Suite,OfficeSuiteStatus Status,Exception? Error=null);
public sealed record OfficeSuiteReport(IReadOnlyList<OfficeSuiteResult> Results)
{
    public int PassedCount=>Results.Count(r=>r.Status==OfficeSuiteStatus.Pass);
    public int FailedCount=>Results.Count(r=>r.Status==OfficeSuiteStatus.Fail);
    public int NotRunCount=>Results.Count(r=>r.Status==OfficeSuiteStatus.NotRun);
    public bool Passed=>Results.Count>0 && Results.All(r=>r.Status==OfficeSuiteStatus.Pass);
}

// A failed family cannot suppress the evidence from later, independently opened families.
// Initialization failure is NOT_RUN, and cleanup failure cannot earn a PASS.
public static class OfficeSuiteRunner
{
    public static OfficeSuiteReport Run<T>(IReadOnlyList<OfficeSuiteCase> selected,Func<OfficeSuiteCase,T> open,
        Action<T,OfficeSuiteCase> check,Action<T> close,Action<OfficeSuiteResult>? observe=null) where T:class
    {
        if(selected.Any(s=>string.IsNullOrWhiteSpace(s.Id) || string.IsNullOrWhiteSpace(s.Name)) ||
            selected.Select(s=>s.Id).Distinct(StringComparer.Ordinal).Count()!=selected.Count)
            throw new ArgumentException("Mỗi nhóm kiểm Word phải có mã và tên riêng.",nameof(selected));
        var results=new List<OfficeSuiteResult>();
        // Recording I/O is outside the test/cleanup catches: a lost report must
        // stop the run, not be disguised as a failure of an otherwise tested suite.
        void Record(OfficeSuiteResult result){results.Add(result);observe?.Invoke(result);}
        foreach(var suite in selected)
        {
            T current;
            try{current=open(suite)??throw new InvalidOperationException("Chưa khởi tạo được phiên Word riêng.");}
            catch(Exception error){Record(new(suite,OfficeSuiteStatus.NotRun,error));continue;}
            Exception? failure=null;
            try{check(current,suite);}catch(Exception error){failure=error;}
            try{close(current);}
            catch(Exception cleanup){failure=failure is null?cleanup:new AggregateException("Nhóm kiểm và đóng phiên Word đều chưa hoàn tất.",failure,cleanup);}
            Record(new(suite,failure is null?OfficeSuiteStatus.Pass:OfficeSuiteStatus.Fail,failure));
        }
        return new(results.ToArray());
    }
}
