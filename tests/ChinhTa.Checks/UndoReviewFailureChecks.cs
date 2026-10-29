using ChinhTa.Core;

public static class UndoReviewFailureChecks
{
    public static void Run(Action<bool,string> assert)
    {
        const string source="Nội dung  kế hoạch kế hoạch.",first="Nội dung kế hoạch kế hoạch.",second="Nội dung kế hoạch.";
        var checkpoints=new Stack<string>();checkpoints.Push(source);checkpoints.Push(first);
        string current=second;int undos=0,scans=0;
        void Undo()
        {
            undos++;
            if(checkpoints.Count>0)current=checkpoints.Pop();
        }
        var failure=new IOException("synthetic preview failure after Undo");
        ScanAfterEditException? actual=null;
        try{EditThenScan.RunUndo(Undo,()=>{scans++;throw failure;});}
        catch(ScanAfterEditException error){actual=error;}
        assert(actual is not null && ReferenceEquals(actual.InnerException,failure) &&
            actual.Message.Contains("Lệnh Hoàn tác đã hoàn tất",StringComparison.Ordinal),
            "A failed review after a completed Undo identifies the completed action and keeps its original scan cause");
        assert(current==first && undos==1 && scans==1 && checkpoints.Count==1 && checkpoints.Peek()==source,
            "A committed Undo followed by scan failure consumes exactly one action and leaves the preceding checkpoint untouched");
        assert(actual!.Message.Contains("Hãy Quét lại",StringComparison.Ordinal) &&
            actual.Message.Contains("chỉ Hoàn tác thêm nếu",StringComparison.Ordinal) &&
            !actual.Message.Contains("Lượt sửa đã được áp dụng",StringComparison.Ordinal),
            "The Undo-specific message directs a scan retry without suggesting that the already completed Undo should be repeated");
        // This is the UI's independent Quét lại action, not another Undo.
        scans++;
        var observed=current;
        assert(observed==first && undos==1 && scans==2 && checkpoints.Count==1 && checkpoints.Peek()==source,
            "A review-only retry keeps the completed Undo and cannot silently discard the previous repair");
        EditThenScan.RunUndo(Undo,()=>scans++);
        assert(current==source && undos==2 && scans==3 && checkpoints.Count==0,
            "A second deliberately requested Undo reaches the preceding action exactly once after review recovery");

        var undoFailure=new IOException("synthetic checkpoint restore failure");Exception? operationError=null;
        try{EditThenScan.RunUndo(()=>throw undoFailure,()=>scans++);}
        catch(Exception error){operationError=error;}
        assert(ReferenceEquals(operationError,undoFailure) && operationError is not ScanAfterEditException && scans==3,
            "An Undo failure is preserved unchanged and never reported as a completed Undo with a scan failure");
        actual=null;
        try{EditThenScan.RunUndo(Undo,()=>{scans++;throw failure;});}
        catch(ScanAfterEditException error){actual=error;}
        assert(current==source && checkpoints.Count==0 && undos==3 && scans==4 && actual is not null &&
            !actual.Message.Contains("đã thay đổi",StringComparison.Ordinal) &&
            !actual.Message.Contains("đã áp dụng",StringComparison.Ordinal),
            "The completed-command message does not invent a document change when Undo had no pending action");

        bool applied=false;actual=null;
        try{EditThenScan.Run(()=>applied=true,()=>throw failure);}
        catch(ScanAfterEditException error){actual=error;}
        assert(applied && actual is not null && ReferenceEquals(actual.InnerException,failure) &&
            actual.Message.Contains("Lượt sửa đã được áp dụng",StringComparison.Ordinal) &&
            !actual.Message.Contains("Lệnh Hoàn tác đã hoàn tất",StringComparison.Ordinal),
            "The existing repair path keeps its committed-repair wording and original failure cause");
        var legacy=new ScanAfterEditException(failure);
        assert(ReferenceEquals(legacy.InnerException,failure) && legacy.Message==actual!.Message,
            "Existing callers of ScanAfterEditException retain the same repair message and cause");
        int events=0;
        EditThenScan.RunUndo(()=>{assert(events==0,"A successful Undo executes before its scanner");events++;},
            ()=>{assert(events==1,"A successful Undo scanner executes once after the action");events++;});
        assert(events==2,"A successful Undo-and-review boundary completes without replay or a synthetic error");
    }
}
