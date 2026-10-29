using System.IO;
using System.Reflection;
using ChinhTa.Desktop;

public static class TrackedRangeCleanupStateChecks
{
    private static List<Exception> Failures(WordSession session)=>(List<Exception>)typeof(WordSession)
        .GetField("checkCleanupFailures",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(session)!;

    public static void Run(Action<bool,string> assert)
    {
        string artifacts=Path.Combine(Path.GetTempPath(),"ctv-range-check-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(artifacts);
        var rangeFailure=new InvalidOperationException("synthetic range cleanup");
        var focused=new WordSession{LineCheckArtifactFolder=artifacts};string folder=Path.GetDirectoryName(focused.RecoveryPath)!;
        try
        {
            File.WriteAllText(focused.RecoveryPath,"verified checkpoint retained after RCW cleanup failure");
            focused.RecordTrackedRangeCleanup(null);
            assert(Failures(focused).Count==0,"A clean range drain does not invent a focused-check cleanup failure");
            focused.RecordTrackedRangeCleanup(rangeFailure);
            assert(Failures(focused).Count==1 && ReferenceEquals(Failures(focused)[0],rangeFailure),
                "The production session records a tracked-range fault in focused-check ownership history");
            bool failed=false;try{focused.DisposeCheckSession();}catch(InvalidOperationException){failed=true;}
            assert(failed && Directory.Exists(folder) && File.ReadAllText(focused.RecoveryPath)=="verified checkpoint retained after RCW cleanup failure",
                "A verified checkpoint cannot turn an earlier range-release uncertainty into focused-check PASS or delete its recovery evidence");
            failed=false;try{focused.DisposeCheckSession();}catch(InvalidOperationException){failed=true;}
            assert(failed && Directory.Exists(folder),"Repeated focused disposal cannot erase sticky tracked-range cleanup uncertainty");
            using var ordinary=new WordSession();
            ordinary.RecordTrackedRangeCleanup(rangeFailure);
            assert(Failures(ordinary).Count==0 && !ordinary.IsOpen && !ordinary.Changed,
                "Normal range-cleanup diagnostics do not invent edits, open Word or alter the focused-check-only history");
        }
        finally
        {
            if(Directory.Exists(folder))Directory.Delete(folder,true);
            Directory.Delete(artifacts,true);
        }
    }
}
