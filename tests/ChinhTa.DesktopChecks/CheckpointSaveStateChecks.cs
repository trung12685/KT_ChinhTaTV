using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using ChinhTa.Desktop;

public static class CheckpointSaveStateChecks
{
    public sealed class Document(string path,Exception? failure,bool partialWrite)
    {
        public int Saves;
        public int Closes;
        public bool Saved{get;private set;}
        public void Save()
        {
            Saves++;
            if(failure is null || partialWrite)File.WriteAllText(path,failure is null?"verified saved working":"uncertain partial save");
            if(failure is not null)throw failure;
            Saved=true;
        }
        public void Close(int saveChanges)
        {
            if(saveChanges!=0)throw new Exception("The managed document must close without an additional save");
            Closes++;
        }
    }
    public sealed class Application
    {
        public int BackgroundSavingStatus=>0;
        public void Quit(int saveChanges)
        {if(saveChanges!=0)throw new Exception("The managed application must not save on close");}
    }
    private static object? Field(WordSession session,string name)=>typeof(WordSession)
        .GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(session);
    private static void Field(WordSession session,string name,object? value)=>typeof(WordSession)
        .GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(session,value);
    private static void Property(WordSession session,string name,object? value)=>typeof(WordSession)
        .GetProperty(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)!.SetValue(session,value);

    public static void Run(Action<bool,string> assert)
    {
        string folder=Path.Combine(Path.GetTempPath(),"ctv-checkpoint-save-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            foreach(bool priorSnapshot in new[]{false,true})
            foreach(bool priorChanged in new[]{false,true})
            foreach(bool partialWrite in new[]{false,true})
            foreach(bool disconnected in new[]{false,true})
            {
                using var session=new WordSession();
                string owned=Path.GetDirectoryName(session.RecoveryPath)!;
                try
                {
                    string source=Path.Combine(folder,Guid.NewGuid()+".source.docx");
                    string destination=Path.Combine(folder,Guid.NewGuid()+".destination.docx");
                    string undo=Path.Combine(owned,"prior.undo.docx");
                    File.WriteAllText(source,"immutable user source");File.WriteAllText(destination,"previous published copy");
                    File.WriteAllText(session.RecoveryPath,"previous working bytes");File.WriteAllText(undo,"retained prior action");
                    Property(session,"Source",source);Property(session,"Changed",priorChanged);Field(session,"snapshotCurrent",priorSnapshot);
                    var stack=(Stack<string>)Field(session,"undoCheckpoints")!;stack.Push(undo);
                    Exception saveFailure=disconnected?new COMException("managed Save disconnected",unchecked((int)0x80010108)):
                        new IOException("managed Save failed");
                    var document=new Document(session.RecoveryPath,saveFailure,partialWrite);Field(session,"document",document);
                    Exception? failure=null;
                    try{session.SaveCopy(destination);}catch(Exception error){failure=error;}
                    assert(ReferenceEquals(failure,saveFailure) && document.Saves==1 && session.Changed &&
                        Field(session,"snapshotCurrent") is false && session.IsOpen,
                        "Actual SaveCopy retains a failed Save's exact cause, marks the working state unverified/unsaved and never retries that mutation");
                    assert(session.Source==source && File.ReadAllText(source)=="immutable user source" &&
                        File.ReadAllText(destination)=="previous published copy" && stack.Count==1 && stack.Peek()==undo &&
                        File.ReadAllText(undo)=="retained prior action" && !Directory.EnumerateFiles(folder,".ChinhTaViet-*.tmp").Any(),
                        "A failed checkpoint cannot publish over an existing destination or consume the immutable source, Undo entry or create a copy stage");
                    assert(File.ReadAllText(session.RecoveryPath)==(partialWrite?"uncertain partial save":"previous working bytes"),
                        "The production failure boundary preserves actual working bytes without claiming a partially written file is the old verified checkpoint");
                    session.CloseOwnedDocument(_=>{});
                    int recoveries=0;bool recoveryRejected=false;
                    try{session.ReopenPreservedDocument(()=>recoveries++);}catch(InvalidOperationException){recoveryRejected=true;}
                    assert(!session.CanRecover && !session.IsOpen && recoveryRejected && recoveries==0 && document.Closes==1,
                        "After a failed Save and document close, explicit recovery cannot reopen uncertain checkpoint bytes as a verified working document");
                    session.Dispose();
                    assert(Directory.Exists(owned) && File.Exists(session.RecoveryPath) && File.Exists(undo) &&
                        File.ReadAllText(source)=="immutable user source" && File.ReadAllText(destination)=="previous published copy",
                        "Disposal retains uncertain working bytes and all private Undo material after a failed Save instead of deleting their directory");
                }
                finally{session.Dispose();if(Directory.Exists(owned))Directory.Delete(owned,true);}
            }
            foreach(bool priorSnapshot in new[]{false,true})
            foreach(bool priorChanged in new[]{false,true})
            {
                using var session=new WordSession();
                string owned=Path.GetDirectoryName(session.RecoveryPath)!;
                try
                {
                    string source=Path.Combine(folder,Guid.NewGuid()+".source.docx");
                    string destination=Path.Combine(folder,Guid.NewGuid()+".destination.docx");
                    string undo=Path.Combine(owned,"prior.undo.docx");
                    File.WriteAllText(source,"immutable user source");File.WriteAllText(session.RecoveryPath,"previous working bytes");
                    File.WriteAllText(undo,"retained prior action");
                    Property(session,"Source",source);Property(session,"Changed",priorChanged);Field(session,"snapshotCurrent",priorSnapshot);
                    var stack=(Stack<string>)Field(session,"undoCheckpoints")!;stack.Push(undo);
                    var document=new Document(session.RecoveryPath,null,false);Field(session,"document",document);Field(session,"app",new Application());
                    typeof(WordSession).GetMethod("Checkpoint",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(session,null);
                    assert(document.Saves==1 && Field(session,"snapshotCurrent") is true && session.Changed==priorChanged &&
                        File.ReadAllText(session.RecoveryPath)=="verified saved working",
                        "A successful actual checkpoint establishes current saved bytes without clearing or inventing the caller's prior dirty state");
                    session.SaveCopy(destination);
                    assert(document.Saves==2 && Field(session,"snapshotCurrent") is true && !session.Changed &&
                        File.ReadAllText(destination)=="verified saved working" && File.ReadAllText(source)=="immutable user source" &&
                        stack.Count==1 && stack.Peek()==undo && File.ReadAllText(undo)=="retained prior action",
                        "A complete successful SaveCopy still publishes verified bytes and clears dirty state while preserving source and Undo history");
                    session.Dispose();
                    assert(!Directory.Exists(owned) && File.ReadAllText(destination)=="verified saved working" && document.Closes==1,
                        "Successful saved-session disposal may clean its owned directory after publication while leaving the saved destination intact");
                }
                finally{session.Dispose();if(Directory.Exists(owned))Directory.Delete(owned,true);}
            }
        }
        finally{Directory.Delete(folder,true);}
    }
}
