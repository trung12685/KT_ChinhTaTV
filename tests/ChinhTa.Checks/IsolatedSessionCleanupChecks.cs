using System.Runtime.InteropServices;
using ChinhTa.Core;

public static class IsolatedSessionCleanupChecks
{
    public static void Run(Action<bool,string> assert)
    {
        string folder=Path.Combine(Path.GetTempPath(),"ChinhTa-isolated-cleanup-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string unrelated=Path.Combine(folder,"unrelated.docx");File.WriteAllText(unrelated,"original source remains intact");
        int sequence=0;
        string Candidate()
        {
            string candidate=Path.Combine(folder,"session-"+(++sequence));Directory.CreateDirectory(candidate);
            File.WriteAllText(Path.Combine(candidate,"copy.docx"),"disposable working copy");return candidate;
        }
        try
        {
            string candidate=Candidate();var order=new List<string>();
            var result=IsolatedSessionCleanup.Complete(null,
                [()=>order.Add("close-document"),()=>order.Add("release-document"),()=>order.Add("restore-options"),()=>order.Add("quit-application"),()=>order.Add("release-application")],
                ()=>{order.Add("delete");Directory.Delete(candidate,true);});
            assert(result is null && order.SequenceEqual(new[]{"close-document","release-document","restore-options","quit-application","release-application","delete"}) &&
                !Directory.Exists(candidate) && File.ReadAllText(unrelated)=="original source remains intact",
                "Normal isolated cleanup closes and releases in ownership order, then deletes only its own temporary copy");

            candidate=Candidate();order.Clear();var primary=new InvalidOperationException("baseline comparison failed");
            result=IsolatedSessionCleanup.Complete(primary,[()=>order.Add("close"),()=>order.Add("release"),()=>order.Add("quit")],()=>Directory.Delete(candidate,true));
            assert(ReferenceEquals(result,primary) && order.SequenceEqual(new[]{"close","release","quit"}) && !Directory.Exists(candidate),
                "A comparison failure keeps its original exception while a fully closed isolated session still cleans its disposable directory");

            foreach(string failedStage in new[]{"close","release-document","restore-links","restore-fields","restore-print-links","restore-normal-prompt","quit","release-application"})
            {
                candidate=Candidate();order.Clear();int deletes=0;
                var failure=new COMException("synthetic "+failedStage,unchecked((int)0x800706BA));
                string[] stages=["close","release-document","restore-links","restore-fields","restore-print-links","restore-normal-prompt","quit","release-application"];
                Action[] actions=stages.Select(stage=>(Action)(()=>{order.Add(stage);if(stage==failedStage)throw failure;})).ToArray();
                result=IsolatedSessionCleanup.Complete(null,actions,()=>{deletes++;Directory.Delete(candidate,true);});
                assert(ReferenceEquals(result,failure) && order.SequenceEqual(stages) && deletes==0 && File.ReadAllText(Path.Combine(candidate,"copy.docx"))=="disposable working copy",
                    "Failure at isolated cleanup stage "+failedStage+" cannot skip later resources or delete the unconfirmed recovery copy");
            }

            candidate=Candidate();order.Clear();int deleteCount=0;
            var closeFailure=new COMException("close disconnected",unchecked((int)0x800706BA));
            var restoreFailure=new InvalidOperationException("restore options failed");
            var quitFailure=new COMException("quit disconnected",unchecked((int)0x800706BE));
            result=IsolatedSessionCleanup.Complete(primary,
                [()=>{order.Add("close");throw closeFailure;},()=>order.Add("release-document"),()=>{order.Add("restore");throw restoreFailure;},
                 ()=>{order.Add("quit");throw quitFailure;},()=>order.Add("release-application")],()=>{deleteCount++;Directory.Delete(candidate,true);});
            var aggregate=result as AggregateException;
            assert(aggregate is not null && aggregate.InnerExceptions.Count==4 &&
                ReferenceEquals(aggregate.InnerExceptions[0],primary) && ReferenceEquals(aggregate.InnerExceptions[1],closeFailure) &&
                ReferenceEquals(aggregate.InnerExceptions[2],restoreFailure) && ReferenceEquals(aggregate.InnerExceptions[3],quitFailure) &&
                order.SequenceEqual(new[]{"close","release-document","restore","quit","release-application"}) && deleteCount==0 && Directory.Exists(candidate),
                "A failed comparison plus failed Close, option restore and Quit retains every original cause in order and still releases the application");

            candidate=Candidate();var deleteDenied=new UnauthorizedAccessException("temporary directory deletion denied");
            result=IsolatedSessionCleanup.Complete(null,[()=>{}],()=>throw deleteDenied);
            assert(ReferenceEquals(result,deleteDenied) && Directory.Exists(candidate),
                "A temporary-directory deletion denial is an explicit failed cleanup rather than a false successful diagnostic");
            result=IsolatedSessionCleanup.Complete(primary,[()=>{}],()=>throw deleteDenied);
            aggregate=result as AggregateException;
            assert(aggregate is not null && aggregate.InnerExceptions.Count==2 && ReferenceEquals(aggregate.InnerExceptions[0],primary) &&
                ReferenceEquals(aggregate.InnerExceptions[1],deleteDenied),
                "Deletion failure cannot replace the primary calibration failure or disappear from its evidence");

            var nested=new AggregateException("comparison and source verification both failed",primary,new IOException("source verification failed"));
            result=IsolatedSessionCleanup.Complete(nested,[()=>throw closeFailure],()=>throw new Exception("must not delete"));
            aggregate=result as AggregateException;
            assert(aggregate is not null && aggregate.InnerExceptions.Count==2 && ReferenceEquals(aggregate.InnerExceptions[0],nested) &&
                ReferenceEquals(aggregate.InnerExceptions[1],closeFailure),
                "An existing aggregate retains its operation boundaries when cleanup adds another failure");

            candidate=Candidate();order.Clear();var callbacks=new List<Action>();
            callbacks.Add(()=>{order.Add("close");callbacks.Clear();});callbacks.Add(()=>order.Add("release"));callbacks.Add(()=>order.Add("quit"));
            result=IsolatedSessionCleanup.Complete(null,callbacks,()=>Directory.Delete(candidate,true));
            assert(result is null && order.SequenceEqual(new[]{"close","release","quit"}) && !Directory.Exists(candidate),
                "Releasing caller state cannot mutate the pending cleanup sequence and silently omit another owned resource");

            candidate=Candidate();deleteCount=0;
            result=IsolatedSessionCleanup.Complete(null,[],()=>{deleteCount++;Directory.Delete(candidate,true);});
            assert(result is null && deleteCount==1 && !Directory.Exists(candidate),
                "Failure before acquiring any COM resource can still remove the isolated copy exactly once");
            assert(File.ReadAllText(unrelated)=="original source remains intact",
                "All isolated close/release/failure cases leave unrelated original files unchanged");
        }
        finally{Directory.Delete(folder,true);}
    }
}
