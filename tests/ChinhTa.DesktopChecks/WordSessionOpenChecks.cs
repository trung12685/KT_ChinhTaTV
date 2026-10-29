using System.IO;
using System.Reflection;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class WordSessionOpenChecks
{
    public sealed class Document
    {
        public int Saves,Closes;
        public bool ReadOnly{get;set;}
        public bool TrackRevisions{get;set;}
        public int ProtectionType{get;set;}=-1;
        public Exception? CloseFailure{get;set;}
        public void Save(){Saves++;throw new Exception("Staging failure must not save the old document");}
        public void Close(int saveChanges){Closes++;if(saveChanges!=0)throw new Exception("Must not save on close");if(CloseFailure is not null)throw CloseFailure;}
    }
    public sealed class Application
    {public int Quits;public void Quit(int saveChanges){Quits++;if(saveChanges!=0)throw new Exception("Must not save on quit");}}
    private static void Field(WordSession session,string name,object? value)=>typeof(WordSession).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(session,value);
    private static object? Field(WordSession session,string name)=>typeof(WordSession).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(session);
    private static void Property(WordSession session,string name,object? value)=>typeof(WordSession).GetProperty(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)!.SetValue(session,value);
    public static void Run(Action<bool,string> assert)
    {
        string folder=Path.Combine(Path.GetTempPath(),"ctv-open-state-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try
        {
            foreach(string kind in new[]{"missing","self","undo","directory","locked"})
            {
                if(kind=="locked" && !OperatingSystem.IsWindows())continue;
                using var session=new WordSession();var document=new Document();var app=new Application();
                string original=Path.Combine(folder,Guid.NewGuid().ToString("N")+".docx");File.WriteAllText(original,"source unchanged");
                File.WriteAllText(session.RecoveryPath,"latest private working");
                string undo=Path.Combine(Path.GetDirectoryName(session.RecoveryPath)!,"prior.undo.docx");File.WriteAllText(undo,"prior action");
                var stack=(Stack<string>)Field(session,"undoCheckpoints")!;stack.Push(undo);
                var coverage=DocumentCoverageBuilder.Build(new(DocumentProfile.State,[],[],true,true));
                var paragraphs=new Paragraph[]{new(0,"previous prose")};
                Field(session,"document",document);Field(session,"app",app);Field(session,"snapshotCurrent",false);
                Field(session,"lastBodyParagraphs",paragraphs);Field(session,"spacingReferencePath","previous-reference.xps");Field(session,"spacingReferenceFlat","previous-flat");
                Property(session,"Source",original);Property(session,"Changed",true);Property(session,"Recovered",true);Property(session,"Coverage",coverage);
                Property(session,"CoverageNotice","previous coverage notice");
                string input=kind switch {"self"=>session.RecoveryPath,"undo"=>undo,"directory"=>folder,"locked"=>original,_=>Path.Combine(folder,"missing.docx")};
                FileStream? held=kind=="locked"?new FileStream(original,FileMode.Open,FileAccess.Read,FileShare.None):null;
                StagedDocumentOpenException? failure=null;
                try{session.Open(input);}catch(StagedDocumentOpenException error){failure=error;}finally{held?.Dispose();}
                assert(failure is {CurrentIsVerified:true} && session.IsOpen && session.Source==original && session.Changed && session.Recovered &&
                    document.Saves==0 && document.Closes==0 && app.Quits==0 && ReferenceEquals(Field(session,"document"),document) && ReferenceEquals(Field(session,"app"),app),
                    "Actual WordSession staging failure preserves the previous source/dirty/open/owned connection state without touching Word: "+kind);
                assert(File.ReadAllText(session.RecoveryPath)=="latest private working" && File.ReadAllText(original)=="source unchanged" &&
                    stack.Count==1 && stack.Peek()==undo && File.ReadAllText(undo)=="prior action" && Field(session,"snapshotCurrent") is false,
                    "Actual WordSession failed-open staging preserves working/Undo bytes and never promotes an unconfirmed previous snapshot: "+kind);
                assert(ReferenceEquals(session.Coverage,coverage) && session.CoverageNotice=="previous coverage notice" &&
                    ReferenceEquals(Field(session,"lastBodyParagraphs"),paragraphs) && (string?)Field(session,"spacingReferencePath")=="previous-reference.xps" &&
                    (string?)Field(session,"spacingReferenceFlat")=="previous-flat",
                    "Actual WordSession failed-open staging leaves previous coverage, body observations and spacing references attached to their original document: "+kind);
                // Allow ordinary disposal only after assertions; the managed
                // fakes ensure no Office installation/process is involved.
                Field(session,"snapshotCurrent",true);
            }
            using(var session=new WordSession())
            {
                string original=Path.Combine(folder,"checkpoint-original.docx"),incoming=Path.Combine(folder,"checkpoint-incoming.docx");
                File.WriteAllText(original,"original");File.WriteAllText(incoming,"incoming");File.WriteAllText(session.RecoveryPath,"last confirmed old bytes");
                var document=new Document();var app=new Application();Field(session,"document",document);Field(session,"app",app);
                Property(session,"Source",original);Property(session,"Changed",false);StagedDocumentOpenException? failure=null;
                try{session.Open(incoming);}catch(StagedDocumentOpenException error){failure=error;}
                assert(failure is {CurrentIsVerified:false} && session.Changed && Field(session,"snapshotCurrent") is false && session.IsOpen &&
                    document.Saves==1 && document.Closes==0 && app.Quits==0 && session.Source==original &&
                    File.ReadAllText(session.RecoveryPath)=="last confirmed old bytes" && File.ReadAllText(incoming)=="incoming",
                    "An actual WordSession old-checkpoint failure blocks close/switch, retains both files and marks dirty/uncertain so closing cannot bypass the discard warning");
                Field(session,"snapshotCurrent",true);
            }
            using(var session=new WordSession())
            {
                int connections=0;bool blocked=false;
                try{session.ReopenPreservedDocument(()=>connections++);}catch(InvalidOperationException){blocked=true;}
                assert(!session.CanRecover && blocked && connections==0 && !session.IsOpen,
                    "A new empty session cannot invent a recoverable document or open Word through retry");
                Property(session,"Source",Path.Combine(folder,"original-recovery.docx"));Property(session,"Changed",true);
                File.WriteAllText(session.RecoveryPath,"preserved latest working");
                string undo=Path.Combine(Path.GetDirectoryName(session.RecoveryPath)!,"retry.undo.docx");File.WriteAllText(undo,"preserved undo");
                var stack=(Stack<string>)Field(session,"undoCheckpoints")!;stack.Push(undo);
                Field(session,"snapshotCurrent",false);blocked=false;
                try{session.ReopenPreservedDocument(()=>connections++);}catch(InvalidOperationException){blocked=true;}
                assert(!session.CanRecover && blocked && connections==0 && session.Changed && File.ReadAllText(session.RecoveryPath)=="preserved latest working",
                    "Explicit recovery refuses an unconfirmed checkpoint without opening Word or discarding dirty state");
                Field(session,"snapshotCurrent",true);var document=new Document();var app=new Application();
                assert(session.CanRecover && !session.IsOpen,"A preserved verified checkpoint with its original source can be retried explicitly");
                session.ReopenPreservedDocument(()=>{connections++;Field(session,"document",document);Field(session,"app",app);});
                assert(connections==1 && session.IsOpen && !session.CanRecover && session.Recovered && session.Changed &&
                    session.Source==Path.Combine(folder,"original-recovery.docx") && stack.Count==1 && stack.Peek()==undo &&
                    File.ReadAllText(session.RecoveryPath)=="preserved latest working" && File.ReadAllText(undo)=="preserved undo",
                    "A successful explicit recovery reopens the preserved session without resetting source, dirty state, working bytes or Undo");
                blocked=false;
                try{session.ReopenPreservedDocument(()=>connections++);}catch(InvalidOperationException){blocked=true;}
                assert(blocked && connections==1 && document.Closes==0 && app.Quits==0,
                    "Recovery cannot replace or close an already open owned document");
            }
            foreach(bool failCleanup in new[]{false,true})
            {
                using var session=new WordSession();Property(session,"Source",Path.Combine(folder,"retry-original.docx"));Property(session,"Changed",true);
                File.WriteAllText(session.RecoveryPath,"preserved before retry");
                var primary=new IOException("managed open rejected");var closeFailure=new IOException("managed close rejected");
                var document=new Document{CloseFailure=failCleanup?closeFailure:null};var app=new Application();Exception? failure=null;
                try{session.ReopenPreservedDocument(()=>{Field(session,"document",document);Field(session,"app",app);throw primary;});}
                catch(Exception error){failure=error;}
                assert(!session.IsOpen && session.CanRecover && session.Changed && !session.Recovered && Field(session,"app") is null &&
                    document.Closes==1 && app.Quits==1 && File.ReadAllText(session.RecoveryPath)=="preserved before retry" &&
                    (failCleanup?failure is AggregateException aggregate && aggregate.Flatten().InnerExceptions.Contains(primary) && aggregate.Flatten().InnerExceptions.Contains(closeFailure):ReferenceEquals(failure,primary)),
                    "Failed explicit recovery detaches partial ownership, attempts every cleanup and preserves checkpoint/causes without claiming open: "+failCleanup);
            }
            foreach(string state in new[]{"writable","read-only","tracked","protected"})
            {
                var document=new Document{ReadOnly=state=="read-only",TrackRevisions=state=="tracked",ProtectionType=state=="protected"?1:-1};
                int closes=0;Exception? failure=null;
                try{WordSession.EnsureEditableOpenedDocument(document,()=>closes++);}catch(Exception error){failure=error;}
                assert(state=="writable"?failure is null && closes==0:failure is InvalidOperationException && closes==1 &&
                    (state!="read-only" || failure.Message.Contains("chỉ đọc",StringComparison.Ordinal)),
                    "The actual Word-open guard accepts only an observed writable, untracked, unprotected document: "+state);
            }
            {
                var closeFailure=new IOException("read-only close failed");Exception? failure=null;
                try{WordSession.EnsureEditableOpenedDocument(new Document{ReadOnly=true},()=>throw closeFailure);}catch(Exception error){failure=error;}
                assert(failure is AggregateException both && both.InnerExceptions.Count==2 && ReferenceEquals(both.InnerExceptions[1],closeFailure) &&
                    both.InnerExceptions[0].Message.Contains("chỉ đọc",StringComparison.Ordinal),
                    "Rejecting a read-only Word document retains both the reason and any owned-close failure");
            }
        }
        finally{Directory.Delete(folder,true);}
    }
}
