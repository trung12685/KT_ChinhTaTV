using System.IO;
using System.Reflection;
using ChinhTa.Desktop;

public static class SaveDestinationPreflightChecks
{
    public sealed class Document(string path,Exception? failure=null,Action? afterSave=null)
    {
        public int Saves,Closes;
        public bool Saved{get;private set;}
        public void Save(){Saves++;File.WriteAllText(path,"complete current checkpoint");afterSave?.Invoke();if(failure is not null)throw failure;Saved=true;}
        public void Close(int changes){if(changes!=0)throw new Exception("Synthetic close must not save");Closes++;}
    }
    public sealed class Application
    {
        public int BackgroundSavingStatus=>0;
        public void Quit(int changes){if(changes!=0)throw new Exception("Synthetic quit must not save");}
    }
    internal static object? Field(WordSession session,string name)=>typeof(WordSession).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(session);
    internal static void Field(WordSession session,string name,object? value)=>typeof(WordSession).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(session,value);
    internal static void Property(WordSession session,string name,object? value)=>typeof(WordSession).GetProperty(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)!.SetValue(session,value);
    private static Exception? Capture(Action action){try{action();return null;}catch(Exception error){return error;}}
    public static void Run(Action<bool,string> assert)
    {
        string folder=Path.Combine(Path.GetTempPath(),"ctv-save-preflight-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try
        {
            foreach(bool verified in new[]{false,true})
            foreach(bool changed in new[]{false,true})
            {
                using var session=new WordSession();string owned=Path.GetDirectoryName(session.RecoveryPath)!;
                try
                {
                    string source=Path.Combine(folder,Guid.NewGuid()+".source.docx"),undo=Path.Combine(owned,"prior.undo.docx");
                    string directory=Path.Combine(folder,Guid.NewGuid()+".docx");Directory.CreateDirectory(directory);
                    File.WriteAllText(source,"immutable source");File.WriteAllText(session.RecoveryPath,"unchanged checkpoint");File.WriteAllText(undo,"unchanged Undo");
                    Property(session,"Source",source);Property(session,"Changed",changed);Field(session,"snapshotCurrent",verified);
                    var stack=(Stack<string>)Field(session,"undoCheckpoints")!;stack.Push(undo);
                    var document=new Document(session.RecoveryPath,new IOException("Save must never run for a rejected target"));
                    Field(session,"document",document);Field(session,"app",new Application());
                    foreach(string target in new[]{source,session.RecoveryPath,undo,Path.Combine(owned,"new.docx"),directory,
                        Path.Combine(folder,"absent",Guid.NewGuid()+".docx"),Path.Combine(folder,"wrong.txt"),"",null!})
                    {
                        var failure=Capture(()=>session.SaveCopy(target));
                        assert(failure is not null && document.Saves==0 && document.Closes==0 && session.IsOpen,
                            "Actual SaveCopy rejects protected/private/directory/missing-parent/wrong-extension/empty targets before Word Save or Close");
                        assert(session.Source==source && session.Changed==changed && Field(session,"snapshotCurrent") is bool actual && actual==verified &&
                            stack.Count==1 && stack.Peek()==undo && File.ReadAllText(source)=="immutable source" &&
                            File.ReadAllText(session.RecoveryPath)=="unchanged checkpoint" && File.ReadAllText(undo)=="unchanged Undo" &&
                            !Directory.EnumerateFiles(folder,".ChinhTaViet-*.tmp",SearchOption.AllDirectories).Any(),
                            "Read-only destination rejection preserves exact dirty/checkpoint/Undo/source/working state and creates no publication stage");
                    }
                    string valid=Path.Combine(folder,Guid.NewGuid()+".DOCX");
                    string prepared=session.ValidateSaveCopyDestination(valid);
                    assert(prepared==Path.GetFullPath(valid) && document.Saves==0 && !File.Exists(valid) && session.Changed==changed &&
                        Field(session,"snapshotCurrent") is bool current && current==verified,
                        "A valid case-insensitive DOCX preflight only returns the normalized target and cannot save, publish or bless a checkpoint");
                }
                finally{session.Dispose();if(Directory.Exists(owned))Directory.Delete(owned,true);}
            }
            foreach(bool fail in new[]{false,true})
            {
                using var session=new WordSession();string owned=Path.GetDirectoryName(session.RecoveryPath)!;
                try
                {
                    string source=Path.Combine(folder,Guid.NewGuid()+".source.docx"),destination=Path.Combine(folder,Guid.NewGuid()+".docx");
                    File.WriteAllText(source,"immutable source");File.WriteAllText(session.RecoveryPath,"before Save");
                    Property(session,"Source",source);Property(session,"Changed",true);
                    var primary=new InvalidOperationException("managed Word Save failure, not a destination rejection");
                    var document=new Document(session.RecoveryPath,fail?primary:null);Field(session,"document",document);Field(session,"app",new Application());
                    var failure=Capture(()=>session.SaveCopy(destination));
                    assert(document.Saves==1 && (fail?ReferenceEquals(failure,primary) && session.Changed && Field(session,"snapshotCurrent") is false && !File.Exists(destination):
                        failure is null && !session.Changed && Field(session,"snapshotCurrent") is true && File.ReadAllText(destination)=="complete current checkpoint"),
                        "A valid target enters the original Save transaction once: a Word failure retains its exact identity and uncertainty, while successful publication clears dirty state");
                    assert(File.ReadAllText(source)=="immutable source","Preflight cannot weaken source preservation on valid or failed Save transactions");
                }
                finally{session.Dispose();if(Directory.Exists(owned))Directory.Delete(owned,true);}
            }
        }
        finally{Directory.Delete(folder,true);}
    }
}
