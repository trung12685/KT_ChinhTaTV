using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class RecoveryFeedbackChecks
{
    public sealed class Document(string path,Exception? saveFailure=null,Exception? closeFailure=null)
    {
        public int Saves,Closes;
        public void Save()
        {
            Saves++;File.WriteAllText(path,"uncertain partial save");
            if(saveFailure is not null)throw saveFailure;
        }
        public void Close(int saveChanges)
        {
            if(saveChanges!=0)throw new Exception("A recovery feedback test must not save on close");
            Closes++;if(closeFailure is not null)throw closeFailure;
        }
    }
    private static object? Field(WordSession session,string name)=>typeof(WordSession)
        .GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(session);
    private static void Field(WordSession session,string name,object? value)=>typeof(WordSession)
        .GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(session,value);
    private static void Property(WordSession session,string name,object? value)=>typeof(WordSession)
        .GetProperty(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)!.SetValue(session,value);
    private static ScanAfterEditException AfterReviewFailure(bool undo,Exception cause)
    {
        try
        {
            if(undo)EditThenScan.RunUndo(()=>{},()=>throw cause);
            else EditThenScan.Run(()=>{},()=>throw cause);
        }
        catch(ScanAfterEditException error){return error;}
        throw new Exception("The synthetic review must fail");
    }

    public static void Run(Action<bool,string> assert)
    {
        string folder=Path.Combine(Path.GetTempPath(),"ctv-recovery-feedback-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);var window=new MainWindow();
        var session=(WordSession)typeof(MainWindow).GetField("session",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(window)!;
        string owned=Path.GetDirectoryName(session.RecoveryPath)!;
        try
        {
            string source=Path.Combine(folder,"source.docx"),destination=Path.Combine(folder,"destination.docx"),before=Path.Combine(owned,"prior.undo.docx");
            File.WriteAllText(source,"immutable user source");File.WriteAllText(destination,"published old copy");
            File.WriteAllText(session.RecoveryPath,"last verified working");File.WriteAllText(before,"trusted prior action");
            Property(session,"Source",source);
            var stack=(Stack<string>)Field(session,"undoCheckpoints")!;stack.Push(before);
            var saveFailure=new IOException("managed partial save failed");var closeFailure=new IOException("managed Undo close failed");
            var document=new Document(session.RecoveryPath,saveFailure,closeFailure);Field(session,"document",document);
            Exception? saveError=null;try{session.SaveCopy(destination);}catch(Exception error){saveError=error;}
            Exception? undoError=null;try{session.Undo();}catch(Exception error){undoError=error;}
            assert(ReferenceEquals(saveError,saveFailure) && ReferenceEquals(undoError,closeFailure) && !session.IsOpen &&
                !session.CanRecover && session.Source==source && session.Changed && Field(session,"snapshotCurrent") is false,
                "Actual failed SaveCopy then failed Undo establishes a closed session whose partial checkpoint is ineligible for automatic recovery");
            const string staleInstruction="Hãy Quét lại để mở bản chưa xác minh.";
            closeFailure.Data["ChinhTa.RecoveryInstruction"]=staleInstruction;
            var feedback=MainWindow.DescribeOperationFailure(session,closeFailure);
            assert(feedback.Status.Contains(MainWindow.UnavailableRecoveryNotice,StringComparison.Ordinal) &&
                feedback.Message.Contains(closeFailure.Message,StringComparison.Ordinal) &&
                feedback.Message.Contains(MainWindow.UnavailableRecoveryNotice,StringComparison.Ordinal) &&
                !feedback.Message.Contains(staleInstruction,StringComparison.Ordinal),
                "Unavailable recovery feedback preserves the primary failure and replaces an ineligible retry instruction with retained-files guidance");
            var disconnected=new COMException("managed RPC disconnected",unchecked((int)0x800706BA));
            var disconnectedFeedback=MainWindow.DescribeOperationFailure(session,disconnected);
            assert(disconnectedFeedback.Message.Contains(disconnected.Message,StringComparison.Ordinal) &&
                disconnectedFeedback.Message.Contains("phục hồi thủ công",StringComparison.Ordinal) &&
                !disconnectedFeedback.Message.Contains("Hãy chọn Kiểm tra → Quét lại",StringComparison.Ordinal),
                "A disconnected closed uncertain session cannot advertise automatic scan recovery merely because its error is a Word RPC error");
            foreach(bool afterUndo in new[]{false,true})
            {
                var reviewFailure=new IOException("managed post-action preview failed");
                var afterReview=AfterReviewFailure(afterUndo,reviewFailure);
                var afterFeedback=MainWindow.DescribeOperationFailure(session,afterReview);
                assert(afterFeedback.Status.StartsWith(afterReview.CompletedActionNotice,StringComparison.Ordinal) &&
                    afterFeedback.Message.Contains(afterReview.CompletedActionNotice,StringComparison.Ordinal) &&
                    afterFeedback.Message.Contains(reviewFailure.Message,StringComparison.Ordinal) &&
                    afterFeedback.Message.Contains("phục hồi thủ công",StringComparison.Ordinal) &&
                    !afterFeedback.Message.Contains("Hãy Quét lại",StringComparison.Ordinal) &&
                    !afterFeedback.Message.Contains("Hoàn tác để bỏ",StringComparison.Ordinal),
                    "Completed repair/Undo truth survives an unavailable recovery state without retaining contradictory retry guidance");
            }

            var review=(IssueReviewPanel)window.FindName("Review");
            review.LoadIssues([new Issue(0,2,"  "," ","",false)]);
            var status=(TextBlock)window.FindName("Status");status.Text="stale retry guidance";
            var scan=typeof(MainWindow).GetMethod("ScanClick",BindingFlags.Instance|BindingFlags.NonPublic)!;
            scan.Invoke(window,[window,new RoutedEventArgs()]);
            assert(status.Text.Contains(MainWindow.UnavailableRecoveryNotice,StringComparison.Ordinal) &&
                status.Text.Contains(session.RecoveryPath,StringComparison.Ordinal) &&
                status.Text.Contains(Diagnostics.LogPath,StringComparison.Ordinal) && review.Choices.Count==0,
                "Clicking Scan in the blocked actual session visibly explains the state, retains recovery/log locations and clears stale review instead of returning silently");
            assert(!session.IsOpen && !session.CanRecover && session.Changed && Field(session,"snapshotCurrent") is false &&
                document.Saves==1 && document.Closes==1 && stack.Count==1 && stack.Peek()==before &&
                File.ReadAllText(session.RecoveryPath)=="uncertain partial save" && File.ReadAllText(before)=="trusted prior action" &&
                File.ReadAllText(source)=="immutable user source" && File.ReadAllText(destination)=="published old copy",
                "Feedback composition and a blocked Scan cannot open Word, promote checkpoint trust, replay Undo or overwrite retained/source/published bytes");
            NormalAndRecoverableControls(folder,assert);
        }
        finally
        {
            // No user file is involved; suppress only the test window's discard
            // dialog after all dirty-state assertions, then remove owned fixtures.
            Property(session,"Changed",false);window.InvalidateReview();window.Close();session.Dispose();
            if(Directory.Exists(owned))Directory.Delete(owned,true);
            Directory.Delete(folder,true);
        }
    }
    private static void NormalAndRecoverableControls(string folder,Action<bool,string> assert)
    {
        using var session=new WordSession();string owned=Path.GetDirectoryName(session.RecoveryPath)!;
        try
        {
            var primary=new IOException("ordinary operation failure");var empty=MainWindow.DescribeOperationFailure(session,primary);
            assert(empty.Message==primary.Message && !empty.Status.Contains(MainWindow.UnavailableRecoveryNotice,StringComparison.Ordinal),
                "An empty new session keeps the normal failure path rather than inventing an unverified retained document");
            Property(session,"Source",Path.Combine(folder,"recovery-source.docx"));
            var missing=MainWindow.DescribeOperationFailure(session,primary);
            assert(!session.CanRecover && missing.Status.Contains(MainWindow.UnavailableRecoveryNotice,StringComparison.Ordinal),
                "A missing checkpoint is also ineligible even when the trust flag alone was initially true");
            File.WriteAllText(session.RecoveryPath,"verified retained working");
            var disconnected=new COMException("managed RPC disconnected",unchecked((int)0x800706BA));
            disconnected.Data["ChinhTa.RecoveryInstruction"]="eligible retained checkpoint instruction";
            var recoverable=MainWindow.DescribeOperationFailure(session,disconnected);
            assert(session.CanRecover && recoverable.Message.Contains("Hãy chọn Kiểm tra → Quét lại",StringComparison.Ordinal) &&
                recoverable.Message.Contains("eligible retained checkpoint instruction",StringComparison.Ordinal) &&
                !recoverable.Status.Contains(MainWindow.UnavailableRecoveryNotice,StringComparison.Ordinal) && !session.IsOpen,
                "An actually eligible preserved checkpoint retains the existing explicit Quét lại recovery instruction without opening Word during feedback");
            var openDocument=new Document(session.RecoveryPath);Field(session,"document",openDocument);Field(session,"snapshotCurrent",false);
            foreach(bool afterUndo in new[]{false,true})
            {
                var afterReview=AfterReviewFailure(afterUndo,primary);
                var normal=MainWindow.DescribeOperationFailure(session,afterReview);
                assert(normal.Status==afterReview.Message && normal.Message==afterReview.Message &&
                    normal.Message.Contains("Hãy Quét lại",StringComparison.Ordinal),
                    "An open session preserves the existing completed-action and scan-retry guidance even when its latest checkpoint needs verification");
            }
            var ordinary=MainWindow.DescribeOperationFailure(session,primary);
            assert(ordinary.Message==primary.Message && ordinary.Status.Contains("hãy Quét lại",StringComparison.Ordinal) &&
                session.IsOpen && openDocument.Saves==0 && openDocument.Closes==0 && Field(session,"snapshotCurrent") is false,
                "Feedback for an open ordinary failure preserves normal guidance and cannot save, close or bless the document");
        }
        finally
        {
            session.Dispose();if(Directory.Exists(owned))Directory.Delete(owned,true);
        }
    }
}
