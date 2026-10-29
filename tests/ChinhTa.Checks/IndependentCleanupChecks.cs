using ChinhTa.Core;

public static class IndependentCleanupChecks
{
    public static void Run(Action<bool,string> assert)
    {
        var order=new List<string>();
        var first=new IOException("preview close failed");
        var second=new InvalidOperationException("Word cleanup failed");
        foreach(int mask in new[]{0,1,2,3})
        {
            order.Clear();
            var result=IndependentCleanup.Complete(null,
                [()=>{order.Add("preview");if((mask&1)!=0)throw first;},
                 ()=>{order.Add("Word");if((mask&2)!=0)throw second;}]);
            assert(order.SequenceEqual(new[]{"preview","Word"}),
                "Independent cleanup attempts preview and Word in order even for failure combination "+mask);
            assert(mask switch
            {
                0=>result is null,
                1=>ReferenceEquals(result,first),
                2=>ReferenceEquals(result,second),
                _=>result is AggregateException both && both.InnerExceptions.Count==2 &&
                    ReferenceEquals(both.InnerExceptions[0],first) && ReferenceEquals(both.InnerExceptions[1],second)
            },"Independent cleanup retains the exact original error(s) for failure combination "+mask);
        }
        var primary=new InvalidOperationException("repair committed but review failed");
        var failure=IndependentCleanup.Complete(primary,[()=>throw first,()=>throw second]);
        assert(failure is AggregateException all && all.InnerExceptions.Count==3 &&
            ReferenceEquals(all.InnerExceptions[0],primary) && ReferenceEquals(all.InnerExceptions[1],first) && ReferenceEquals(all.InnerExceptions[2],second),
            "Cleanup failure cannot replace a preceding operation failure or discard another owned-resource failure");
        assert(ReferenceEquals(IndependentCleanup.Complete(primary,[()=>{}]),primary),
            "Successful cleanup preserves the original operation exception without wrapping it");
        order.Clear();var mutable=new List<Action>();
        mutable.Add(()=>{order.Add("first");mutable.Clear();});mutable.Add(()=>order.Add("last"));
        assert(IndependentCleanup.Complete(null,mutable) is null && order.SequenceEqual(new[]{"first","last"}),
            "Cleanup snapshots resource callbacks before closing resources can mutate the caller's state");
        assert(IndependentCleanup.Complete(null,[]) is null,
            "No acquired resource and no operation failure is a successful no-op cleanup");
    }
}
