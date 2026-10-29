using System.IO;
using System.Runtime.InteropServices;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class TrackedRangeCleanupChecks
{
    private static Exception[] Causes(Exception? error)=>error switch
    {AggregateException all=>all.Flatten().InnerExceptions.ToArray(),null=>[],_=>[error]};

    public static void Run(Action<bool,string> assert)
    {
        object first=new(),second=new(),later=new();
        var owned=new List<object>{first,second};var released=new List<object?>();
        var releaseFailure=new InvalidComObjectException("synthetic first range release");
        var failure=TrackedRangeCleanup.Drain(owned,item=>item,item=>
        {
            released.Add(item);
            if(ReferenceEquals(item,first))
            {
                if(owned.Count!=0)throw new Exception("Ownership was not detached before release");
                throw releaseFailure;
            }
        });
        assert(ReferenceEquals(failure,releaseFailure) && owned.Count==0 && released.SequenceEqual(new[]{first,second}),
            "Tracked text ranges detach ownership before the first release and attempt every owned range after an RCW failure");
        released.Clear();
        assert(TrackedRangeCleanup.Drain(owned,item=>item,item=>released.Add(item)) is null && released.Count==0,
            "The outer cleanup cannot release ranges twice after rollback already drained their ownership");

        owned.AddRange([first,second]);released.Clear();
        failure=TrackedRangeCleanup.Drain(owned,item=>item,item=>
        {released.Add(item);if(ReferenceEquals(item,first))owned.Add(later);});
        assert(failure is null && released.SequenceEqual(new[]{first,second}) && owned.SequenceEqual(new[]{later}),
            "A callback cannot mutate the captured release set or erase a newly acquired range that belongs to later cleanup");
        released.Clear();TrackedRangeCleanup.Drain(owned,item=>item,item=>released.Add(item));
        assert(owned.Count==0 && released.SequenceEqual(new[]{later}),
            "A range acquired by a cleanup callback remains owned and is released exactly once on the next drain");

        owned.AddRange([first,second]);var secondFailure=new IOException("synthetic second range release");
        failure=TrackedRangeCleanup.Drain(owned,item=>item,item=>
        {if(ReferenceEquals(item,first))throw releaseFailure;throw secondFailure;});
        assert(owned.Count==0 && Causes(failure).SequenceEqual(new Exception[]{releaseFailure,secondFailure}),
            "Multiple tracked-range release failures preserve their original identities in ownership order");
        owned.AddRange([first,second]);released.Clear();
        failure=TrackedRangeCleanup.Drain(owned,item=>ReferenceEquals(item,first)?throw releaseFailure:item,item=>released.Add(item));
        assert(ReferenceEquals(failure,releaseFailure) && owned.Count==0 && released.SequenceEqual(new[]{second}),
            "A failed range selector cannot prevent release of the remaining tracked resources");

        owned.AddRange([first,second]);released.Clear();int restores=0;string current="modified";
        var restored=TrackedRangeCleanup.Restore(owned,item=>item,item=>
        {released.Add(item);if(ReferenceEquals(item,first))throw releaseFailure;},()=>{restores++;current="checkpoint";});
        assert(restored.Restored && restored.RestoreFailure is null && restored.Failure is null &&
            ReferenceEquals(restored.CleanupFailure,releaseFailure) && restores==1 && current=="checkpoint" &&
            owned.Count==0 && released.SequenceEqual(new[]{first,second}),
            "The real restore orchestration still restores the checkpoint after a release fault and keeps cleanup uncertainty separate from the verified result");

        owned.AddRange([first,second]);var restoreFailure=new IOException("synthetic checkpoint reopen");restores=0;
        var notRestored=TrackedRangeCleanup.Restore(owned,item=>item,item=>
        {if(ReferenceEquals(item,first))throw releaseFailure;},()=>{restores++;throw restoreFailure;});
        assert(!notRestored.Restored && restores==1 && ReferenceEquals(notRestored.RestoreFailure,restoreFailure) &&
            ReferenceEquals(notRestored.CleanupFailure,releaseFailure) && Causes(notRestored.Failure).SequenceEqual(new Exception[]{restoreFailure,releaseFailure}),
            "An actual checkpoint restoration failure retains both its cause and the preceding RCW cleanup fault");
        notRestored=TrackedRangeCleanup.Restore(owned,item=>item,_=>{},()=>throw restoreFailure);
        assert(!notRestored.Restored && notRestored.CleanupFailure is null && ReferenceEquals(notRestored.Failure,restoreFailure),
            "A restore-only failure retains its original identity instead of being confused with range cleanup");

        var plan=new[]{new Issue(0,1,"x","y","",false)};
        var operationFailure=new InvalidOperationException("synthetic edit failure");Exception? observed=null;
        TrackedRangeRestoration? recovery=null;current="x";restores=0;owned.AddRange([first,second]);
        try
        {
            AtomicTextBatch.Apply(plan,_=>current,_=>{current="partial";throw operationFailure;},()=>{},()=>
            {
                recovery=TrackedRangeCleanup.Restore(owned,item=>item,item=>
                {if(ReferenceEquals(item,first))throw releaseFailure;},()=>{restores++;current="x";});
                if(recovery.Failure is { } error)throw error;
            });
        }
        catch(Exception error){observed=error;}
        assert(ReferenceEquals(observed,operationFailure) && current=="x" && restores==1 && recovery is {Restored:true} &&
            ReferenceEquals(recovery.CleanupFailure,releaseFailure) && owned.Count==0,
            "Actual AtomicTextBatch rollback preserves its primary edit failure and restored story even when tracked-range release fails");

        observed=null;owned.AddRange([first,second]);
        try
        {
            AtomicTextBatch.Apply(plan,_=>"x",_=>throw operationFailure,()=>{},()=>
            {
                var result=TrackedRangeCleanup.Restore(owned,item=>item,item=>
                {if(ReferenceEquals(item,first))throw releaseFailure;},()=>throw restoreFailure);
                if(result.Failure is { } error)throw error;
            });
        }
        catch(Exception error){observed=error;}
        assert(Causes(observed).SequenceEqual(new Exception[]{operationFailure,restoreFailure,releaseFailure}) && owned.Count==0,
            "Actual AtomicTextBatch retains the edit, checkpoint-restore and range-release causes when recovery genuinely fails");
    }
}
