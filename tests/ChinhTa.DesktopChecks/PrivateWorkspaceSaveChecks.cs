using System.IO;
using System.Reflection;
using ChinhTa.Desktop;

public static class PrivateWorkspaceSaveChecks
{
    public sealed class Document(string path,bool rejectSave)
    {
        public int Saves,Closes;
        public bool Saved{get;private set;}
        public void Save()
        {
            Saves++;
            if(rejectSave)throw new IOException("No Save is permitted before rejecting a disposable output path");
            File.WriteAllText(path,"complete current working bytes");Saved=true;
        }
        public void Close(int changes){if(changes!=0)throw new Exception("The synthetic close must not save");Closes++;}
    }
    public sealed class Application
    {
        public int BackgroundSavingStatus=>0;
        public void Quit(int changes){if(changes!=0)throw new Exception("The synthetic quit must not save");}
    }
    private static object? Field(WordSession session,string name)=>typeof(WordSession)
        .GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(session);
    private static void Field(WordSession session,string name,object? value)=>typeof(WordSession)
        .GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(session,value);
    private static void Property(WordSession session,string name,object? value)=>typeof(WordSession)
        .GetProperty(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)!.SetValue(session,value);
    private static Exception? Capture(Action action)
    {try{action();return null;}catch(Exception error){return error;}}
    public static void Run(Action<bool,string> assert)
    {
        string folder=Path.Combine(Path.GetTempPath(),"ctv-private-save-checks-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try
        {
            foreach(bool report in new[]{false,true})
            foreach(bool verified in new[]{false,true})
            foreach(bool changed in new[]{false,true})
            foreach(bool existing in new[]{false,true})
            {
                using var session=new WordSession();string owned=Path.GetDirectoryName(session.RecoveryPath)!;
                try
                {
                    string source=Path.Combine(folder,Guid.NewGuid()+".source.docx"),undo=Path.Combine(owned,"prior.undo.docx");
                    string destination=Path.Combine(owned,report?"my-published-report.txt":"my-published-copy.docx");
                    File.WriteAllText(source,"immutable source");File.WriteAllText(session.RecoveryPath,"original working bytes");
                    File.WriteAllText(undo,"trusted prior action");if(existing)File.WriteAllText(destination,"old private entry");
                    Property(session,"Source",source);Property(session,"Changed",changed);Field(session,"snapshotCurrent",verified);
                    var stack=(Stack<string>)Field(session,"undoCheckpoints")!;stack.Push(undo);
                    var document=new Document(session.RecoveryPath,true);Field(session,"document",document);Field(session,"app",new Application());
                    var before=Directory.EnumerateFiles(owned).ToDictionary(path=>path,File.ReadAllBytes);
                    var failure=Capture(()=>{if(report)session.SaveProgressReport(destination,"new report");else session.SaveCopy(destination);});
                    assert(failure is InvalidOperationException && failure.Message.Contains("thư mục bản làm việc tạm",StringComparison.Ordinal) && document.Saves==0,
                        "Actual SaveCopy/SaveProgressReport rejects a disposable output path before touching Word, even when its old checkpoint is unverified");
                    assert(session.Changed==changed && Field(session,"snapshotCurrent") is bool actual && actual==verified &&
                        session.Source==source && stack.Count==1 && stack.Peek()==undo && session.IsOpen,
                        "Rejecting a private-folder output preserves the prior dirty/checkpoint/Source/Undo and connection state");
                    assert(before.Count==Directory.EnumerateFiles(owned).Count() && before.All(pair=>File.ReadAllBytes(pair.Key).SequenceEqual(pair.Value)) &&
                        File.ReadAllText(source)=="immutable source" && (existing?File.ReadAllText(destination)=="old private entry":!File.Exists(destination)),
                        "Rejected output cannot replace an existing private entry or create a report/copy stage, and all working/Undo/source bytes stay unchanged");
                    session.Dispose();
                    assert(document.Closes==1 && Directory.Exists(owned)==!verified &&
                        (!Directory.Exists(owned) || File.ReadAllText(session.RecoveryPath)=="original working bytes" && File.ReadAllText(undo)=="trusted prior action") &&
                        File.ReadAllText(source)=="immutable source",
                        "Normal disposal still follows prior checkpoint trust after rejection and never presents a disposable file as a successful publication");
                }
                finally{session.Dispose();if(Directory.Exists(owned))Directory.Delete(owned,true);}
            }
            foreach(bool report in new[]{false,true})
            foreach(bool verified in new[]{false,true})
            foreach(bool changed in new[]{false,true})
            {
                using var session=new WordSession();string owned=Path.GetDirectoryName(session.RecoveryPath)!;
                try
                {
                    string source=Path.Combine(folder,Guid.NewGuid()+".source.docx");File.WriteAllText(source,"immutable source");
                    string destination=Path.Combine(folder,Guid.NewGuid()+(report?".txt":".docx"));
                    File.WriteAllText(session.RecoveryPath,"original working bytes");Property(session,"Source",source);
                    Property(session,"Changed",changed);Field(session,"snapshotCurrent",verified);
                    var document=new Document(session.RecoveryPath,false);Field(session,"document",document);Field(session,"app",new Application());
                    if(report)session.SaveProgressReport(destination,"exported progress report");else session.SaveCopy(destination);
                    string published=File.ReadAllText(destination);
                    assert((report?document.Saves==0 && published.EndsWith("exported progress report",StringComparison.Ordinal):
                        document.Saves==1 && published=="complete current working bytes") && File.ReadAllText(source)=="immutable source",
                        "Actual document/report exports remain available in independent destinations, including ordinary temporary test folders");
                    assert(session.Changed==(report?changed:false) && Field(session,"snapshotCurrent") is bool current && current==(report?verified:true),
                        "Successful external document publication clears dirty state only after verification while a program report retains session state");
                    session.Dispose();
                    assert(File.ReadAllText(destination)==published && File.ReadAllText(source)=="immutable source" &&
                        Directory.Exists(owned)==(report && !verified) && document.Closes==1,
                        "A successfully published external copy/report survives disposal of its private workspace");
                }
                finally{session.Dispose();if(Directory.Exists(owned))Directory.Delete(owned,true);}
            }
        }
        finally{Directory.Delete(folder,true);}
    }
}
