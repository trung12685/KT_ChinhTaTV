using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using ChinhTa.Desktop;

public static class WordCheckCleanupChecks
{
    // Public nested fakes let the production dynamic COM call shape be tested
    // from another assembly without creating or attaching to Microsoft Word.
    public sealed class Document(Action close)
    {public void Close(int saveChanges){if(saveChanges!=0)throw new Exception("Must not save a check document");close();}}
    public sealed class Application(Action quit)
    {public void Quit(int saveChanges){if(saveChanges!=0)throw new Exception("Must not save a check application");quit();}}
    private static Exception[] Causes(Exception? error)=>error switch
    {AggregateException aggregate=>aggregate.Flatten().InnerExceptions.ToArray(),null=>[],_=>[error]};
    private static void Inject(WordSession session,string field,object value)=>typeof(WordSession)
        .GetField(field,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(session,value);

    public static void Run(Action<bool,string> assert)
    {
        string folder=Path.Combine(Path.GetTempPath(),"ChinhTa-cleanup-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try
        {
            var order=new List<string>();var doc=new Document(()=>order.Add("close"));var app=new Application(()=>order.Add("quit"));
            object foreign=new();
            var released=new List<object?>();int deletes=0;
            var error=WordSession.CompleteCheckCleanup(doc,app,o=>{released.Add(o);order.Add(ReferenceEquals(o,doc)?"release-document":"release-app");},
                ()=>{order.Add("delete");deletes++;},[],true);
            assert(error is null && order.SequenceEqual(new[]{"close","release-document","quit","release-app","delete"}) && deletes==1,
                "Clean focused Word disposal closes/releases both owned resources before deleting its temporary folder");
            assert(released.Count==2 && ReferenceEquals(released[0],doc) && ReferenceEquals(released[1],app) && !released.Contains(foreign),
                "Strict cleanup touches only the document/application references owned by the check");

            var closeFailure=new COMException("synthetic-close");var quitFailure=new COMException("synthetic-quit");var releaseFailure=new IOException("synthetic-release");
            order.Clear();deletes=0;
            doc=new Document(()=>{order.Add("close");throw closeFailure;});app=new Application(()=>{order.Add("quit");throw quitFailure;});
            error=WordSession.CompleteCheckCleanup(doc,app,o=>{order.Add(ReferenceEquals(o,doc)?"release-document":"release-app");if(ReferenceEquals(o,doc))throw releaseFailure;},()=>deletes++,[],true);
            assert(order.SequenceEqual(new[]{"close","release-document","quit","release-app"}) && deletes==0,
                "Close/release failures cannot skip Quit or the remaining release, and an uncertain close retains the recovery folder");
            var causes=Causes(error);
            assert(causes.Length==3 && causes.Contains(closeFailure) && causes.Contains(quitFailure) && causes.Contains(releaseFailure),
                "Every cleanup exception is retained rather than replacing the first failure with the last");

            order.Clear();deletes=0;var historical=new COMException("earlier-suppressed-close");
            doc=new Document(()=>order.Add("close"));app=new Application(()=>order.Add("quit"));
            error=WordSession.CompleteCheckCleanup(doc,app,_=>order.Add("release"),()=>deletes++,[historical],true);
            assert(error is not null && Causes(error).Contains(historical) && deletes==0 && order.SequenceEqual(new[]{"close","release","quit","release"}),
                "A previous suppressed close failure cannot become PASS after a later apparently clean shutdown");
            deletes=0;error=WordSession.CompleteCheckCleanup(null,null,_=>throw new Exception("Nothing is owned"),()=>deletes++,[],false);
            assert(error is InvalidOperationException && deletes==0,"An unverified checkpoint keeps the recovery copy and cannot earn a focused-check PASS");
            deletes=0;error=WordSession.CompleteCheckCleanup(null,null,_=>throw new Exception("Nothing is owned"),()=>deletes++,[],true);
            assert(error is null && deletes==1,"A check that acquired no COM resources can clean its own temporary directory");
            var deleteFailure=new IOException("synthetic-delete");error=WordSession.CompleteCheckCleanup(null,null,_=>{},()=>throw deleteFailure,[],true);
            assert(ReferenceEquals(error,deleteFailure),"A failed directory cleanup remains a failed focused check");

            var ordinary=new WordSession();string ordinaryFolder=Path.GetDirectoryName(ordinary.RecoveryPath)!;bool guarded=false;
            try{ordinary.DisposeCheckSession();}catch(InvalidOperationException){guarded=true;}
            assert(guarded && Directory.Exists(ordinaryFolder),"The strict check-only disposal entry point cannot act on a normal user session");
            ordinary.Dispose();

            var failed=new WordSession{LineCheckArtifactFolder=folder};string failedFolder=Path.GetDirectoryName(failed.RecoveryPath)!;
            File.WriteAllText(failed.RecoveryPath,"recoverable synthetic checkpoint");
            int closes=0,quits=0;
            Inject(failed,"document",new Document(()=>{closes++;throw closeFailure;}));Inject(failed,"app",new Application(()=>{quits++;throw quitFailure;}));
            bool rejected=false;try{failed.DisposeCheckSession();}catch(InvalidOperationException){rejected=true;}
            assert(rejected && closes==1 && quits==1 && !failed.IsOpen && failed.OfficeDescription=="Word chưa mở" &&
                File.ReadAllText(failed.RecoveryPath)=="recoverable synthetic checkpoint",
                "Strict session disposal detaches both references and retains the checkpoint when Close and Quit fail");
            bool sticky=false;try{failed.DisposeCheckSession();}catch(InvalidOperationException){sticky=true;}
            assert(sticky && closes==1 && quits==1 && Directory.Exists(failedFolder),
                "Repeating strict disposal after a failed close does not retry detached handles or silently turn the failure into PASS");
            Directory.Delete(failedFolder,true);

            var clean=new WordSession{LineCheckArtifactFolder=folder};string cleanFolder=Path.GetDirectoryName(clean.RecoveryPath)!;
            closes=quits=0;Inject(clean,"document",new Document(()=>closes++));Inject(clean,"app",new Application(()=>quits++));
            clean.DisposeCheckSession();clean.DisposeCheckSession();
            assert(closes==1 && quits==1 && !clean.IsOpen && clean.OfficeDescription=="Word chưa mở" && !Directory.Exists(cleanFolder),
                "Successful strict disposal is repeatable, releases owned state once and removes only its own session directory");
        }
        finally{Directory.Delete(folder,true);}
    }
}
