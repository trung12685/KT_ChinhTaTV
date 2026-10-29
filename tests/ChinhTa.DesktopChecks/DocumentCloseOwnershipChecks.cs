using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using ChinhTa.Desktop;

public static class DocumentCloseOwnershipChecks
{
    public sealed class Document(Exception? failure=null)
    {
        public int Closes;
        public bool Closed;
        public void Close(int saveChanges)
        {
            if(saveChanges!=0)throw new Exception("Closing must not save the managed document");
            Closes++;Closed=true;
            if(failure is not null)throw failure;
        }
    }
    public sealed class Application
    {
        public int Quits;
        public void Quit(int saveChanges)
        {if(saveChanges!=0)throw new Exception("Closing must not save the managed application");Quits++;}
    }
    private static object? Field(WordSession session,string name)=>typeof(WordSession)
        .GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(session);
    private static void Field(WordSession session,string name,object? value)=>typeof(WordSession)
        .GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(session,value);
    private static void Property(WordSession session,string name,object? value)=>typeof(WordSession)
        .GetProperty(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)!.SetValue(session,value);
    private static Exception[] Causes(Exception? error)=>error switch
    {AggregateException aggregate=>aggregate.Flatten().InnerExceptions.ToArray(),null=>[],_=>[error]};

    public static void Run(Action<bool,string> assert)
    {
        foreach(string failureStage in new[]{"none","close","release","both"})
        {
            using var session=new WordSession();
            var closeFailure=new IOException("managed close failed after its document closed");
            var releaseFailure=new InvalidComObjectException("managed document release failed");
            var document=new Document(failureStage is "close" or "both"?closeFailure:null);
            var app=new Application();
            Field(session,"document",document);Field(session,"app",app);
            int releases=0;bool detachedDuringRelease=false;Exception? failure=null;
            try
            {
                session.CloseOwnedDocument(owned=>
                {
                    releases++;
                    detachedDuringRelease=!session.IsOpen && Field(session,"document") is null && ReferenceEquals(owned,document);
                    if(failureStage is "release" or "both")throw releaseFailure;
                });
            }
            catch(Exception error){failure=error;}
            var expected=new List<Exception>();
            if(failureStage is "close" or "both")expected.Add(closeFailure);
            if(failureStage is "release" or "both")expected.Add(releaseFailure);
            assert(!session.IsOpen && document.Closed && document.Closes==1 && releases==1 && detachedDuringRelease,
                "The shared production document-close boundary detaches ownership before draining both Close and release, even after either failure: "+failureStage);
            assert(Causes(failure).SequenceEqual(expected) && ReferenceEquals(Field(session,"app"),app) && app.Quits==0,
                "Document-only closing preserves exact cleanup causes and leaves the owned Word application for the caller: "+failureStage);
            session.CloseOwnedDocument(_=>throw new Exception("A detached document must not be released again"));
            assert(document.Closes==1 && app.Quits==0 && !session.IsOpen,
                "Repeating document-only cleanup cannot close or release a previously detached RCW again: "+failureStage);
            session.Dispose();
            assert(app.Quits==1 && Field(session,"app") is null && !session.IsOpen && document.Closes==1,
                "Later disposal still quits the owned application without reusing the detached document: "+failureStage);
        }
        using(var session=new WordSession())
        {
            var document=new Document();Field(session,"document",document);bool rejected=false;
            try{session.CloseOwnedDocument(null!);}catch(ArgumentNullException){rejected=true;}
            assert(rejected && session.IsOpen && ReferenceEquals(Field(session,"document"),document) && document.Closes==0,
                "A missing release callback is rejected before detaching an owned document");
        }

        string folder=Path.Combine(Path.GetTempPath(),"ctv-close-undo-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            foreach(bool previousChanged in new[]{false,true})
            {
                using var session=new WordSession();
                string source=Path.Combine(folder,Guid.NewGuid().ToString("N")+".docx");
                string before=Path.Combine(Path.GetDirectoryName(session.RecoveryPath)!,"prior.undo.docx");
                File.WriteAllText(source,"immutable user source");
                File.WriteAllText(session.RecoveryPath,"latest verified working");
                File.WriteAllText(before,"previous verified action");
                Property(session,"Source",source);Property(session,"Changed",previousChanged);
                var stack=(Stack<string>)Field(session,"undoCheckpoints")!;stack.Push(before);
                var closeFailure=new IOException("managed Undo close failed after the document closed");
                var document=new Document(closeFailure);var app=new Application();
                Field(session,"document",document);Field(session,"app",app);
                Exception? failure=null;
                try{session.Undo();}catch(Exception error){failure=error;}
                assert(ReferenceEquals(failure,closeFailure) && document.Closes==1 && document.Closed && !session.IsOpen &&
                    session.CanRecover && Field(session,"snapshotCurrent") is true && ReferenceEquals(Field(session,"app"),app) && app.Quits==0,
                    "Actual Undo close failure no longer publishes the closed managed document as open and retains explicit recovery of its unchanged checkpoint: "+previousChanged);
                assert(session.Changed==previousChanged && session.Source==source && stack.Count==1 && stack.Peek()==before &&
                    File.ReadAllText(source)=="immutable user source" && File.ReadAllText(session.RecoveryPath)=="latest verified working" &&
                    File.ReadAllText(before)=="previous verified action",
                    "Actual Undo close failure preserves source, dirty state, working bytes and the pending Undo entry without replacing either checkpoint: "+previousChanged);

                var reopened=new Document();int connections=0;
                session.ReopenPreservedDocument(()=>{connections++;Field(session,"document",reopened);});
                assert(connections==1 && session.IsOpen && session.Recovered && !session.CanRecover && session.Changed==previousChanged &&
                    session.Source==source && stack.Count==1 && stack.Peek()==before &&
                    File.ReadAllText(session.RecoveryPath)=="latest verified working" && File.ReadAllText(before)=="previous verified action" &&
                    document.Closes==1 && ReferenceEquals(Field(session,"app"),app),
                    "Explicit recovery after the actual failed Undo reopens its preserved checkpoint without consuming Undo or silently changing dirty state: "+previousChanged);
            }
        }
        finally{Directory.Delete(folder,true);}
    }
}
