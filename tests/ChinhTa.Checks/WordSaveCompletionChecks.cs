using ChinhTa.Core;

public static class WordSaveCompletionChecks
{
    private static Exception? Failure(Action action)
    {try{action();return null;}catch(Exception error){return error;}}
    public static void Run(Action<bool,string> assert)
    {
        var limit=TimeSpan.FromMilliseconds(200);var interval=TimeSpan.FromMilliseconds(50);
        TimeSpan elapsed=TimeSpan.Zero;int reads=0,savedReads=0;var pauses=new List<TimeSpan>();
        int[] queued=[2,1,0,0];
        WordSaveCompletion.Wait(()=>queued[reads++],()=>{savedReads++;return true;},()=>elapsed,
            delay=>{pauses.Add(delay);elapsed+=delay;},limit,interval);
        assert(reads==4 && savedReads==1 && pauses.SequenceEqual(new[]{interval,interval}) && elapsed==interval+interval,
            "Save completion waits for every queued background write, then observes Saved and a second idle queue before returning");

        elapsed=TimeSpan.Zero;reads=savedReads=0;pauses.Clear();int[] restarted=[0,1,0,0];
        WordSaveCompletion.Wait(()=>restarted[reads++],()=>{savedReads++;return true;},()=>elapsed,
            delay=>{pauses.Add(delay);elapsed+=delay;},limit,interval);
        assert(reads==4 && savedReads==2 && pauses.SequenceEqual(new[]{interval}),
            "A queue that becomes busy after Saved was read cannot be accepted from its earlier idle observation");

        elapsed=TimeSpan.Zero;reads=savedReads=0;pauses.Clear();
        var timeout=Failure(()=>WordSaveCompletion.Wait(()=>{reads++;return 1;},()=>{savedReads++;return true;},()=>elapsed,
            delay=>{pauses.Add(delay);elapsed+=delay;},limit,interval));
        assert(timeout is TimeoutException && reads==4 && savedReads==0 && elapsed==limit && pauses.Count==4,
            "A permanently busy queue times out once without treating an early Saved flag as completion");

        elapsed=TimeSpan.Zero;reads=savedReads=0;pauses.Clear();
        timeout=Failure(()=>WordSaveCompletion.Wait(()=>++reads%2==1?0:1,()=>{savedReads++;return true;},()=>elapsed,
            delay=>{pauses.Add(delay);elapsed+=delay;},limit,interval));
        assert(timeout is TimeoutException && reads==8 && savedReads==4 && elapsed==limit && pauses.Count==4,
            "Repeated idle-to-busy transitions share one deadline and cannot reset the completion timeout");

        elapsed=TimeSpan.Zero;pauses.Clear();
        timeout=Failure(()=>WordSaveCompletion.Wait(()=>1,()=>true,()=>elapsed,
            delay=>{pauses.Add(delay);elapsed+=delay;},TimeSpan.FromMilliseconds(30),interval));
        assert(timeout is TimeoutException && pauses.SequenceEqual(new[]{TimeSpan.FromMilliseconds(30)}),
            "The final pause is capped by the remaining deadline instead of oversleeping a full polling interval");

        foreach(bool failAfterSaved in new[]{false,true})
        {
            reads=savedReads=0;int[] counts=failAfterSaved?[0,-1]:[-1];
            var invalid=Failure(()=>WordSaveCompletion.Wait(()=>counts[reads++],()=>{savedReads++;return true;},()=>TimeSpan.Zero,
                _=>throw new Exception("Invalid queue counts must not be retried"),limit,interval));
            assert(invalid is InvalidOperationException && reads==counts.Length && savedReads==(failAfterSaved?1:0),
                "Negative background-save counts fail closed both before and after the document Saved observation");
        }
        savedReads=0;int waits=0;
        var unsaved=Failure(()=>WordSaveCompletion.Wait(()=>0,()=>{savedReads++;return false;},()=>TimeSpan.Zero,
            _=>waits++,limit,interval));
        assert(unsaved is InvalidOperationException && savedReads==1 && waits==0,
            "An idle queue with Saved=false is not a verified checkpoint and cannot trigger another Save or optimistic completion");

        foreach(string stage in new[]{"queue","saved","second-queue","clock","pause"})
        {
            var primary=new IOException("managed completion observer failed: "+stage);reads=0;
            Exception? observed=Failure(()=>WordSaveCompletion.Wait(
                ()=>{reads++;if(stage=="queue" || stage=="second-queue" && reads==2)throw primary;return stage=="pause"?1:0;},
                ()=>{if(stage=="saved")throw primary;return true;},
                ()=>{if(stage=="clock")throw primary;return TimeSpan.Zero;},
                _=>throw primary,limit,interval));
            assert(ReferenceEquals(observed,primary),
                "A completion observation/pause failure keeps its exact cause without replaying Save or inventing completion: "+stage);
        }
        foreach(bool busy in new[]{false,true})
        {
            elapsed=TimeSpan.Zero;reads=0;
            timeout=Failure(()=>WordSaveCompletion.Wait(()=>{reads++;elapsed=limit;return busy?1:0;},()=>true,()=>elapsed,
                _=>throw new Exception("A late observation must not pause again"),limit,interval));
            assert(timeout is TimeoutException,
                "Even an idle/Saved result cannot confirm a checkpoint after its observation exhausted the total deadline");
        }

        foreach(bool backwards in new[]{false,true})
        {
            int ticks=0;
            var invalid=Failure(()=>WordSaveCompletion.Wait(()=>1,()=>true,
                ()=>backwards && ticks++==0?TimeSpan.FromMilliseconds(1):TimeSpan.FromMilliseconds(-1),
                _=>{},limit,interval));
            assert(invalid is InvalidOperationException,
                "A negative or backwards injected elapsed clock cannot make a bounded save wait silently unbounded");
        }
        foreach(bool zero in new[]{false,true})
        {
            TimeSpan invalid=zero?TimeSpan.Zero:TimeSpan.FromTicks(-1);
            assert(Failure(()=>WordSaveCompletion.Wait(()=>0,()=>true,()=>TimeSpan.Zero,_=>{},invalid,interval)) is ArgumentOutOfRangeException &&
                Failure(()=>WordSaveCompletion.Wait(()=>0,()=>true,()=>TimeSpan.Zero,_=>{},limit,invalid)) is ArgumentOutOfRangeException,
                "Nonpositive completion timeouts and intervals are rejected before observing Word");
        }
        assert(Failure(()=>WordSaveCompletion.Wait(null!,()=>true,()=>TimeSpan.Zero,_=>{},limit,interval)) is ArgumentNullException &&
            Failure(()=>WordSaveCompletion.Wait(()=>0,null!,()=>TimeSpan.Zero,_=>{},limit,interval)) is ArgumentNullException &&
            Failure(()=>WordSaveCompletion.Wait(()=>0,()=>true,null!,_=>{},limit,interval)) is ArgumentNullException &&
            Failure(()=>WordSaveCompletion.Wait(()=>0,()=>true,()=>TimeSpan.Zero,null!,limit,interval)) is ArgumentNullException,
            "A completion guard cannot run with missing queue, document, clock or pause observations");
        assert(WordSaveCompletion.MaximumWait==TimeSpan.FromSeconds(30) && WordSaveCompletion.PollInterval==TimeSpan.FromMilliseconds(50),
            "Production save completion has one explicit 30-second polling deadline and 50ms interval");
    }
}
