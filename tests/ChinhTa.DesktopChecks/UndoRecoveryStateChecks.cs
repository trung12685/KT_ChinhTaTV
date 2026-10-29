using System.IO;
using System.Reflection;
using ChinhTa.Desktop;

public static class UndoRecoveryStateChecks
{
    private static object? Field(WordSession session,string name)=>typeof(WordSession)
        .GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(session);

    public static void Run(Action<bool,string> assert)
    {
        var session=new WordSession();
        string sessionFolder=Path.GetDirectoryName(session.RecoveryPath)!;
        string source=Path.Combine(Path.GetTempPath(),"ctv-undo-source-"+Guid.NewGuid().ToString("N")+".docx");
        string before=Path.Combine(sessionFolder,"retained.undo.docx");
        byte[] original=[1,2,3,4],previous=[9,8,7,6];
        try
        {
            File.WriteAllBytes(source,original);File.WriteAllBytes(before,previous);
            typeof(WordSession).GetProperty(nameof(WordSession.Source))!.SetValue(session,source);
            var undo=(Stack<string>)Field(session,"undoCheckpoints")!;undo.Push(before);
            assert(!session.IsOpen && !session.Changed && Field(session,"app") is null &&
                Field(session,"snapshotCurrent") is true && !File.Exists(session.RecoveryPath),
                "The Undo regression starts with a clean owned session, retained Undo backup and missing working checkpoint, without opening Word");

            Exception? failure=null;
            try{session.Undo();}catch(Exception error){failure=error;}
            assert(failure is FileNotFoundException && !session.IsOpen && Field(session,"app") is null &&
                session.Changed && Field(session,"snapshotCurrent") is false,
                "Actual Undo failure before backup/close cannot leave a clean state or claim a verified checkpoint; no Word connection is acquired");
            assert(undo.Count==1 && undo.Peek()==before && File.ReadAllBytes(before).SequenceEqual(previous) &&
                session.Source==source && File.ReadAllBytes(source).SequenceEqual(original),
                "A failed real Undo preserves its pending stack entry, backup bytes and original source instead of consuming or overwriting them");
            assert(!session.CanRecover && !File.Exists(session.RecoveryPath),
                "An Undo failure cannot offer automatic recovery from a missing or unverified working checkpoint");

            session.Dispose();
            assert(Directory.Exists(sessionFolder) && File.ReadAllBytes(before).SequenceEqual(previous) &&
                session.Changed && undo.Count==1 && !session.IsOpen && Field(session,"app") is null,
                "Ordinary disposal retains the recovery folder and pending Undo backup after the production Undo path marks its checkpoint uncertain");
        }
        finally
        {
            session.Dispose();
            if(Directory.Exists(sessionFolder))Directory.Delete(sessionFolder,true);
            File.Delete(source);
        }
    }
}
