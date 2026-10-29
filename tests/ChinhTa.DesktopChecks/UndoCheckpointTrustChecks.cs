using System.IO;
using System.Reflection;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class UndoCheckpointTrustChecks
{
    public sealed class Document(string path,Exception? closeFailure=null,Exception? saveFailure=null)
    {
        public int Closes;
        public int Saves;
        public bool ReadOnly=>false;
        public bool TrackRevisions=>false;
        public int ProtectionType=>-1;
        public void Save()
        {
            Saves++;
            File.WriteAllText(path,saveFailure is null?"verified save":"uncertain partial save");
            if(saveFailure is not null)throw saveFailure;
        }
        public void Close(int saveChanges)
        {
            if(saveChanges!=0)throw new Exception("Undo must close the managed document without saving");
            Closes++;
            if(closeFailure is not null)throw closeFailure;
        }
    }
    public sealed class Documents(Exception? firstOpenFailure=null)
    {
        public int Opens;
        public readonly List<string> OpenedBytes=[];
        public object Open(string path,bool ReadOnly,bool AddToRecentFiles,bool Visible)
        {
            if(ReadOnly || AddToRecentFiles || Visible)throw new Exception("Unexpected managed connection options");
            OpenedBytes.Add(File.ReadAllText(path));
            if(++Opens==1 && firstOpenFailure is not null)throw firstOpenFailure;
            return new Document(path);
        }
    }
    public sealed class Application(Documents documents)
    {
        public bool Visible {get;set;}
        public int DisplayAlerts {get;set;}
        public bool ScreenUpdating {get;set;}
        public int AutomationSecurity {get;set;}
        public Documents Documents=>documents;
        public int Quits;
        public void Quit(int saveChanges)
        {if(saveChanges!=0)throw new Exception("The managed application must not save during cleanup");Quits++;}
    }
    private static object? Field(WordSession session,string name)=>typeof(WordSession)
        .GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(session);
    private static void Field(WordSession session,string name,object? value)=>typeof(WordSession)
        .GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(session,value);
    private static void Property(WordSession session,string name,object? value)=>typeof(WordSession)
        .GetProperty(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)!.SetValue(session,value);

    public static void Run(Action<bool,string> assert)
    {
        string folder=Path.Combine(Path.GetTempPath(),"ctv-undo-trust-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try
        {
            foreach(bool currentWasVerified in new[]{false,true})
            foreach(bool priorChanged in new[]{false,true})
            foreach(bool failOpen in new[]{false,true})
            {
                using var session=new WordSession();string owned=Path.GetDirectoryName(session.RecoveryPath)!;
                try
                {
                    string source=Path.Combine(folder,Guid.NewGuid()+".docx"),before=Path.Combine(owned,"prior.undo.docx");
                    string current=currentWasVerified?"latest verified working":"uncertain partial save";
                    File.WriteAllText(source,"immutable user source");File.WriteAllText(session.RecoveryPath,current);
                    File.WriteAllText(before,"trusted prior action");
                    Property(session,"Source",source);Property(session,"Changed",priorChanged);Field(session,"snapshotCurrent",currentWasVerified);
                    var stack=(Stack<string>)Field(session,"undoCheckpoints")!;stack.Push(before);
                    var primary=new IOException(failOpen?"managed candidate open failed":"managed current close failed");
                    var document=new Document(session.RecoveryPath,failOpen?null:primary);
                    var documents=new Documents(failOpen?primary:null);var app=new Application(documents);
                    Field(session,"document",document);Field(session,"app",app);
                    Exception? failure=null;try{session.Undo();}catch(Exception error){failure=error;}
                    assert(ReferenceEquals(failure,primary) && Field(session,"snapshotCurrent") is bool verified && verified==currentWasVerified &&
                        session.Changed==(priorChanged || !currentWasVerified),
                        "A failed actual Undo preserves prior checkpoint trust instead of promoting unchanged/restored unverified bytes");
                    assert(stack.Count==1 && stack.Peek()==before && File.ReadAllText(before)=="trusted prior action" &&
                        session.Source==source && File.ReadAllText(source)=="immutable user source" && File.ReadAllText(session.RecoveryPath)==current,
                        "Failure before close or after candidate-open restores exact original bytes and leaves source and Undo candidate untouched");
                    assert(document.Closes==1 && documents.Opens==(failOpen?2:0) && app.Quits==0 &&
                        (!failOpen || documents.OpenedBytes.SequenceEqual(new[]{"trusted prior action",current})),
                        "The actual swap either stops at close or reopens its restored original bytes once, without replaying Undo or saving");
                    session.CloseOwnedDocument(_=>{});
                    assert(!session.IsOpen && session.CanRecover==currentWasVerified &&
                        (!currentWasVerified || CheckpointSwap.CurrentIsVerified(failure!)),
                        "Only the previously verified input may become an explicit recovery source after failed Undo and closing its connection");
                    session.Dispose();
                    assert(Directory.Exists(owned)!=currentWasVerified &&
                        (currentWasVerified || File.ReadAllText(session.RecoveryPath)==current && File.ReadAllText(before)=="trusted prior action"),
                        "Disposal retains uncertain original working bytes and Undo material after a failed Undo; verified cleanups remain permitted");
                }
                finally{session.Dispose();if(Directory.Exists(owned))Directory.Delete(owned,true);}
            }
            foreach(bool currentWasVerified in new[]{false,true})
            foreach(bool priorChanged in new[]{false,true})
            {
                using var session=new WordSession();string owned=Path.GetDirectoryName(session.RecoveryPath)!;
                try
                {
                    string source=Path.Combine(folder,Guid.NewGuid()+".docx"),before=Path.Combine(owned,"prior.undo.docx");
                    File.WriteAllText(source,"immutable user source");File.WriteAllText(session.RecoveryPath,"original current bytes");
                    File.WriteAllText(before,"trusted prior action");
                    Property(session,"Source",source);Property(session,"Changed",priorChanged);Field(session,"snapshotCurrent",currentWasVerified);
                    var stack=(Stack<string>)Field(session,"undoCheckpoints")!;stack.Push(before);
                    var document=new Document(session.RecoveryPath);var documents=new Documents();var app=new Application(documents);
                    Field(session,"document",document);Field(session,"app",app);session.Undo();
                    assert(Field(session,"snapshotCurrent") is true && session.Changed && session.IsOpen && stack.Count==0 &&
                        documents.Opens==1 && documents.OpenedBytes.SequenceEqual(new[]{"trusted prior action"}) && document.Closes==1,
                        "Successful actual Undo can establish its trusted candidate even when the discarded current bytes were unverified");
                    assert(File.ReadAllText(session.RecoveryPath)=="trusted prior action" && File.ReadAllText(before)=="trusted prior action" &&
                        File.ReadAllText(source)=="immutable user source" && session.Source==source,
                        "A completed Undo consumes one entry and preserves the independent immutable source and saved candidate bytes");
                    session.CloseOwnedDocument(_=>{});
                    assert(session.CanRecover && Field(session,"snapshotCurrent") is true,
                        "A successfully installed trusted Undo candidate remains eligible for explicit recovery");
                    session.Dispose();assert(!Directory.Exists(owned) && app.Quits==1,
                        "Successful trusted Undo keeps normal owned application and directory cleanup semantics");
                }
                finally{session.Dispose();if(Directory.Exists(owned))Directory.Delete(owned,true);}
            }
            FailedSaveThenUndo(folder,assert);
        }
        finally{Directory.Delete(folder,true);}
    }
    private static void FailedSaveThenUndo(string folder,Action<bool,string> assert)
    {
        using var session=new WordSession();string owned=Path.GetDirectoryName(session.RecoveryPath)!;
        try
        {
            string source=Path.Combine(folder,Guid.NewGuid()+".source.docx"),destination=Path.Combine(folder,Guid.NewGuid()+".destination.docx");
            string before=Path.Combine(owned,"prior.undo.docx");
            File.WriteAllText(source,"immutable user source");File.WriteAllText(destination,"published old copy");
            File.WriteAllText(session.RecoveryPath,"latest verified working");File.WriteAllText(before,"trusted prior action");
            Property(session,"Source",source);Property(session,"Changed",false);
            var stack=(Stack<string>)Field(session,"undoCheckpoints")!;stack.Push(before);
            var saveFailure=new IOException("managed partial Save failed");var closeFailure=new IOException("managed Undo close failed");
            var document=new Document(session.RecoveryPath,closeFailure,saveFailure);Field(session,"document",document);
            Exception? saveError=null;try{session.SaveCopy(destination);}catch(Exception error){saveError=error;}
            assert(ReferenceEquals(saveError,saveFailure) && Field(session,"snapshotCurrent") is false && session.Changed &&
                File.ReadAllText(session.RecoveryPath)=="uncertain partial save",
                "The actual end-to-end regression first establishes an unverified partial checkpoint through a failed SaveCopy");
            Exception? undoError=null;try{session.Undo();}catch(Exception error){undoError=error;}
            assert(ReferenceEquals(undoError,closeFailure) && Field(session,"snapshotCurrent") is false && !session.IsOpen && !session.CanRecover &&
                session.Changed && document.Saves==1 && document.Closes==1 && stack.Count==1,
                "A subsequent failed actual Undo cannot turn the failed Save's partial checkpoint into verified recovery or consume its Undo entry");
            session.Dispose();
            assert(Directory.Exists(owned) && File.ReadAllText(session.RecoveryPath)=="uncertain partial save" &&
                File.ReadAllText(before)=="trusted prior action" && File.ReadAllText(source)=="immutable user source" &&
                File.ReadAllText(destination)=="published old copy",
                "Closing after both failures retains private recovery and Undo files while preserving source and published destination");
        }
        finally{session.Dispose();if(Directory.Exists(owned))Directory.Delete(owned,true);}
    }
}
