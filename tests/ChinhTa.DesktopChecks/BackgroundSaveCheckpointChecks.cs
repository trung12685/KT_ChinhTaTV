using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using ChinhTa.Desktop;

public static class BackgroundSaveCheckpointChecks
{
    public sealed class Document(string path)
    {
        public int Saves,Closes,SavedReads;
        public bool Complete;
        public Action? OnSave;
        public Func<bool>? ReadSaved;
        public bool Saved {get{SavedReads++;return ReadSaved?.Invoke()??Complete;}}
        public void Save()
        {
            Saves++;Complete=false;File.WriteAllText(path,"partial background bytes");OnSave?.Invoke();
        }
        public void FinishSave(){File.WriteAllText(path,"complete verified bytes");Complete=true;}
        public void Close(int saveChanges)
        {if(saveChanges!=0)throw new Exception("The managed document must not save on close");Closes++;}
    }
    public sealed class Application(Func<int> pending)
    {
        public int Reads,Quits;
        public int BackgroundSavingStatus{get{Reads++;return pending();}}
        public void Quit(int saveChanges)
        {if(saveChanges!=0)throw new Exception("The managed application must not save on quit");Quits++;}
    }
    private static object? Field(WordSession session,string name)=>typeof(WordSession)
        .GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(session);
    private static void Field(WordSession session,string name,object? value)=>typeof(WordSession)
        .GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(session,value);
    private static void Property(WordSession session,string name,object value)=>typeof(WordSession)
        .GetProperty(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)!.SetValue(session,value);

    public static void Run(Action<bool,string> assert)
    {
        string folder=Path.Combine(Path.GetTempPath(),"ctv-background-save-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try
        {
            foreach(bool asynchronous in new[]{false,true})
            foreach(bool priorTrust in new[]{false,true})
            foreach(bool priorChanged in new[]{false,true})
            {
                using var session=new WordSession();string owned=Path.GetDirectoryName(session.RecoveryPath)!;
                string source=Path.Combine(folder,Guid.NewGuid()+".source.docx"),destination=Path.Combine(folder,Guid.NewGuid()+".destination.docx");
                string before=Path.Combine(owned,"prior.undo.docx");
                File.WriteAllText(source,"immutable user source");File.WriteAllText(destination,"published old copy");
                File.WriteAllText(session.RecoveryPath,"previous working");File.WriteAllText(before,"trusted prior action");
                Property(session,"Source",source);Property(session,"Changed",priorChanged);Field(session,"snapshotCurrent",priorTrust);
                var stack=(Stack<string>)Field(session,"undoCheckpoints")!;stack.Push(before);
                var document=new Document(session.RecoveryPath);Field(session,"document",document);
                bool pendingProtected=true;int statusReads=0;
                if(!asynchronous)document.OnSave=document.FinishSave;
                var app=new Application(()=>
                {
                    statusReads++;
                    pendingProtected&=Field(session,"snapshotCurrent") is false && File.ReadAllText(destination)=="published old copy";
                    if(asynchronous && statusReads<=2)return 1;
                    if(asynchronous)document.FinishSave();
                    return 0;
                });
                Field(session,"app",app);session.SaveCopy(destination);
                assert(document.Saves==1 && pendingProtected && app.Reads==(asynchronous?4:2) && document.SavedReads==1 &&
                    Field(session,"snapshotCurrent") is true && !session.Changed,
                    "Actual SaveCopy calls Save once and keeps trust/publication blocked until the private Word queue is idle and Saved is true");
                assert(File.ReadAllText(destination)=="complete verified bytes" && File.ReadAllText(source)=="immutable user source" &&
                    File.ReadAllText(before)=="trusted prior action" && stack.Count==1 && stack.Peek()==before &&
                    !Directory.EnumerateFiles(folder,".ChinhTaViet-*.tmp").Any(),
                    "The actual copy boundary publishes only complete background-save bytes and preserves source, Undo and stage cleanup");
                session.Dispose();
                assert(!Directory.Exists(owned) && document.Closes==1 && app.Quits==1 && File.ReadAllText(destination)=="complete verified bytes",
                    "A completed background save can clean its private session after publication without altering the published copy");
            }
            IdleToBusyCheck(folder,assert);
            foreach(string stage in new[]{"queue-read","saved-read","negative","saved-false","second-queue-read","second-negative","timeout","missing-app"})
            foreach(bool priorTrust in new[]{false,true})
            foreach(bool priorChanged in new[]{false,true})
            {
                using var session=new WordSession();string owned=Path.GetDirectoryName(session.RecoveryPath)!;
                try
                {
                    string source=Path.Combine(folder,Guid.NewGuid()+".source.docx"),destination=Path.Combine(folder,Guid.NewGuid()+".destination.docx");
                    string before=Path.Combine(owned,"prior.undo.docx");
                    File.WriteAllText(source,"immutable user source");File.WriteAllText(destination,"published old copy");
                    File.WriteAllText(session.RecoveryPath,"previous working");File.WriteAllText(before,"trusted prior action");
                    Property(session,"Source",source);Property(session,"Changed",priorChanged);Field(session,"snapshotCurrent",priorTrust);
                    var stack=(Stack<string>)Field(session,"undoCheckpoints")!;stack.Push(before);
                    Exception primary=stage=="timeout"?new TimeoutException("managed save completion timeout"):
                        stage=="queue-read"?new COMException("managed queue read failed",unchecked((int)0x80010108)):
                        new IOException("managed completion read failed");
                    var document=new Document(session.RecoveryPath);Field(session,"document",document);
                    if(stage=="saved-read")document.ReadSaved=()=>throw primary;
                    else if(stage is "second-queue-read" or "second-negative")document.OnSave=document.FinishSave;
                    int count=0;
                    var app=new Application(()=>
                    {
                        count++;
                        if(stage is "queue-read" or "timeout" || stage=="second-queue-read" && count==2)throw primary;
                        return stage=="negative" || stage=="second-negative" && count==2?-1:0;
                    });
                    if(stage!="missing-app")Field(session,"app",app);
                    Exception? failure=null;try{session.SaveCopy(destination);}catch(Exception error){failure=error;}
                    bool exact=stage is "queue-read" or "saved-read" or "second-queue-read" or "timeout";
                    assert(failure is not null && (!exact || ReferenceEquals(failure,primary)) && document.Saves==1 &&
                        Field(session,"snapshotCurrent") is false && session.Changed,
                        "Completion faults, false Saved, invalid counts and missing ownership fail closed without replaying Save or replacing its exact read cause: "+stage);
                    assert(File.ReadAllText(destination)=="published old copy" && File.ReadAllText(source)=="immutable user source" &&
                        File.ReadAllText(before)=="trusted prior action" && stack.Count==1 && stack.Peek()==before &&
                        !Directory.EnumerateFiles(folder,".ChinhTaViet-*.tmp").Any(),
                        "A failed completion check cannot start publication, overwrite the destination or consume source/Undo state: "+stage);
                    session.CloseOwnedDocument(_=>{});
                    assert(!session.CanRecover && !session.IsOpen && Field(session,"snapshotCurrent") is false && document.Closes==1,
                        "Unconfirmed save bytes cannot become an automatic reopen source after the document closes: "+stage);
                    session.Dispose();
                    assert(Directory.Exists(owned) && File.Exists(session.RecoveryPath) && File.ReadAllText(before)=="trusted prior action" &&
                        File.ReadAllText(destination)=="published old copy",
                        "Disposal retains every uncertain background-save checkpoint and Undo file instead of deleting them: "+stage);
                }
                finally{session.Dispose();if(Directory.Exists(owned))Directory.Delete(owned,true);}
            }
        }
        finally{Directory.Delete(folder,true);}
    }
    private static void IdleToBusyCheck(string folder,Action<bool,string> assert)
    {
        using var session=new WordSession();string owned=Path.GetDirectoryName(session.RecoveryPath)!;
        string source=Path.Combine(folder,Guid.NewGuid()+".source.docx"),destination=Path.Combine(folder,Guid.NewGuid()+".destination.docx");
        File.WriteAllText(source,"immutable user source");File.WriteAllText(destination,"published old copy");
        File.WriteAllText(session.RecoveryPath,"previous working");Property(session,"Source",source);
        var document=new Document(session.RecoveryPath);Field(session,"document",document);
        int queues=0;bool pendingProtected=true;
        document.ReadSaved=()=>true; // Saved can be set while background bytes are still pending.
        var app=new Application(()=>
        {
            queues++;pendingProtected&=Field(session,"snapshotCurrent") is false && File.ReadAllText(destination)=="published old copy";
            if(queues==2)return 1;
            if(queues>=3)document.FinishSave();
            return 0;
        });
        Field(session,"app",app);session.SaveCopy(destination);
        assert(pendingProtected && queues==4 && document.SavedReads==2 && document.Saves==1 &&
            File.ReadAllText(destination)=="complete verified bytes" && Field(session,"snapshotCurrent") is true,
            "An idle-to-busy transition after Saved=true makes actual SaveCopy wait rather than publish the earlier partial bytes");
        session.Dispose();
        assert(!Directory.Exists(owned) && app.Quits==1 && document.Closes==1 && File.ReadAllText(source)=="immutable user source",
            "The queue-transition success still preserves source and normal owned cleanup without a second Save");
    }
}
