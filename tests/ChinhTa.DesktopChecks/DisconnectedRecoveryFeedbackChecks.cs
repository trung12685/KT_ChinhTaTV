using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class DisconnectedRecoveryFeedbackChecks
{
    public sealed class DisconnectedDocument(string path,Exception saveFailure,Exception readFailure)
    {
        public int Saves,Reads,Closes;
        public void Save(){Saves++;File.WriteAllText(path,"unverified partial save");throw saveFailure;}
        public void Repaginate(){Reads++;throw readFailure;}
        public void Close(int saveChanges)
        {
            if(saveChanges!=0)throw new Exception("The test must not save on close");
            Closes++;
        }
    }
    private static object? Field(WordSession session,string name)=>typeof(WordSession)
        .GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(session);
    private static void Field(WordSession session,string name,object? value)=>typeof(WordSession)
        .GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(session,value);
    private static void Property(WordSession session,string name,object? value)=>typeof(WordSession)
        .GetProperty(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)!.SetValue(session,value);
    private static Exception Capture(Action action)
    {
        try{action();}catch(Exception error){return error;}
        throw new Exception("The synthetic operation must fail");
    }
    public static void Run(Action<bool,string> assert)
    {
        string fixture=Path.Combine(Path.GetTempPath(),"ctv-disconnected-feedback-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(fixture);var session=new WordSession();
        string owned=Path.GetDirectoryName(session.RecoveryPath)!;
        try
        {
            string source=Path.Combine(fixture,"source.docx"),destination=Path.Combine(fixture,"copy.docx"),
                before=Path.Combine(owned,"prior.undo.docx");
            File.WriteAllText(source,"immutable source");File.WriteAllText(destination,"old published copy");
            File.WriteAllText(session.RecoveryPath,"last verified working");File.WriteAllText(before,"trusted prior action");
            Property(session,"Source",source);var undo=(Stack<string>)Field(session,"undoCheckpoints")!;undo.Push(before);
            var saveFailure=new IOException("managed partial save failure");
            var rpc=new COMException("managed Word disconnected after partial save",unchecked((int)0x800706BA));
            var document=new DisconnectedDocument(session.RecoveryPath,saveFailure,rpc);Field(session,"document",document);
            var failedSave=Capture(()=>session.SaveCopy(destination));
            assert(ReferenceEquals(failedSave,saveFailure) && session.IsOpen && session.Changed &&
                Field(session,"snapshotCurrent") is false && !session.CanRecover,
                "A failed actual SaveCopy leaves the document reference present but its partial checkpoint ineligible for automatic recovery");
            var failedScan=Capture(()=>session.Check());
            assert(failedScan is WordRecoveryUnavailableException && ReferenceEquals(failedScan.InnerException,rpc),
                "An actual Check that loses RPC after an unverified Save reports typed blocked recovery while retaining the original RPC cause");
            assert(document.Reads==1 && document.Saves==1 && document.Closes==0 && session.IsOpen && !session.Recovered &&
                Field(session,"snapshotCurrent") is false && ReferenceEquals(Field(session,"document"),document),
                "The blocked scan performs one read and cannot close, reopen, repeat Save or bless the retained document reference");
            failedScan.Data["ChinhTa.RecoveryInstruction"]="Hãy Quét lại để mở bản chưa xác minh.";
            var feedback=MainWindow.DescribeOperationFailure(session,failedScan);
            assert(feedback.Message.Contains(failedScan.Message,StringComparison.Ordinal) &&
                feedback.Message.Contains(MainWindow.DisconnectedRecoveryNotice,StringComparison.Ordinal) &&
                feedback.Status.Contains(MainWindow.DisconnectedRecoveryNotice,StringComparison.Ordinal),
                "Feedback distinguishes a disconnected retained document reference from an actually usable open session and preserves the blocked-recovery cause");
            assert(!feedback.Message.Contains("Hãy Quét lại",StringComparison.Ordinal) &&
                !feedback.Status.Contains("hãy Quét lại",StringComparison.Ordinal) &&
                !feedback.Message.Contains("Phiên tài liệu đã đóng",StringComparison.Ordinal),
                "An unverified disconnected document cannot advertise automatic scan recovery or falsely claim that its owned reference has already closed");
            foreach(bool afterUndo in new[]{false,true})
            {
                var after=(ScanAfterEditException)Capture(()=>
                {
                    if(afterUndo)EditThenScan.RunUndo(()=>{},()=>throw failedScan);
                    else EditThenScan.Run(()=>{},()=>throw failedScan);
                });
                var completed=MainWindow.DescribeOperationFailure(session,after);
                assert(completed.Status.StartsWith(after.CompletedActionNotice,StringComparison.Ordinal) &&
                    completed.Message.Contains(after.CompletedActionNotice,StringComparison.Ordinal) &&
                    completed.Message.Contains(failedScan.Message,StringComparison.Ordinal) &&
                    completed.Message.Contains(MainWindow.DisconnectedRecoveryNotice,StringComparison.Ordinal) &&
                    !completed.Message.Contains("Hãy Quét lại",StringComparison.Ordinal),
                    "Completed repair/Undo feedback keeps its completed-action truth when its subsequent scan is blocked by an unverified disconnected checkpoint");
            }
            var ordinary=MainWindow.DescribeOperationFailure(session,new IOException("ordinary connected-operation failure"));
            assert(ordinary.Status.Contains("hãy Quét lại",StringComparison.Ordinal) &&
                !ordinary.Message.Contains(MainWindow.DisconnectedRecoveryNotice,StringComparison.Ordinal),
                "An ordinary open-session failure still permits a scan without treating every unsaved checkpoint as a disconnected session");
            var repeated=Capture(()=>session.Check());
            assert(repeated is WordRecoveryUnavailableException && ReferenceEquals(repeated.InnerException,rpc) &&
                document.Reads==2 && document.Saves==1 && document.Closes==0 && !session.Recovered,
                "A later explicit scan attempt cannot silently bypass checkpoint trust or replay the failed Save");
            assert(File.ReadAllText(source)=="immutable source" && File.ReadAllText(destination)=="old published copy" &&
                File.ReadAllText(session.RecoveryPath)=="unverified partial save" && File.ReadAllText(before)=="trusted prior action" &&
                undo.Count==1 && undo.Peek()==before,
                "Composing blocked-recovery feedback preserves source, destination, partial working bytes and the prior Undo checkpoint");
            session.Dispose();
            var closed=MainWindow.DescribeOperationFailure(session,repeated);
            assert(!session.IsOpen && closed.Message.Contains(MainWindow.UnavailableRecoveryNotice,StringComparison.Ordinal) &&
                !closed.Message.Contains(MainWindow.DisconnectedRecoveryNotice,StringComparison.Ordinal) && document.Closes==1 &&
                Directory.Exists(owned) && File.Exists(before) && File.Exists(session.RecoveryPath),
                "After explicit disposal the same typed failure receives closed-session guidance while uncertain working and Undo files remain retained");
        }
        finally
        {
            session.Dispose();if(Directory.Exists(owned))Directory.Delete(owned,true);
            Directory.Delete(fixture,true);
        }
    }
}
