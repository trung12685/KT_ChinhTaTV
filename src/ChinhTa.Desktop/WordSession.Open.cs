using System.IO;
using System.Runtime.ExceptionServices;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

public sealed partial class WordSession
{
    public bool CanRecover=>!IsOpen && Source.Length>0 && snapshotCurrent && File.Exists(RecoveryPath);
    public void ReopenPreservedDocument()=>ReopenPreservedDocument(Connect);
    internal void ReopenPreservedDocument(Action connect)
    {
        ArgumentNullException.ThrowIfNull(connect);
        if(!CanRecover)throw new InvalidOperationException("Chưa có bản làm việc đã xác minh để mở lại; giữ các tệp phục hồi và nhật ký.");
        bool previousRecovered=Recovered;
        try
        {
            ReadWithRecovery("Word.ReopenPreserved",()=>{connect();return true;});
            if(document is null)throw new InvalidOperationException("Chưa mở được bản làm việc được giữ lại.");
            Recovered=true;
        }
        catch(Exception primary)
        {
            Recovered=previousRecovered;
            object? ownedDocument=document;document=null;object? ownedApplication=app;app=null;
            var cleanup=CloseOwnedConnection(ownedDocument,ownedApplication,Release);
            if(cleanup is not null)throw new AggregateException("Chưa mở/đóng được phiên phục hồi; giữ bản làm việc: "+RecoveryPath,primary,cleanup);
            throw;
        }
    }
    internal static void EnsureEditableOpenedDocument(object opened,Action close)
    {
        ArgumentNullException.ThrowIfNull(opened);ArgumentNullException.ThrowIfNull(close);
        dynamic candidate=opened;
        string? reason=(bool)candidate.ReadOnly?
            "Word đã mở bản làm việc ở chế độ chỉ đọc. Có thể tệp đang bị một phiên Word khác giữ; hãy đóng phiên đó rồi thử mở lại.":
            (bool)candidate.TrackRevisions || (int)candidate.ProtectionType!=-1?
            "Bản thử chưa hỗ trợ Track Changes hoặc tài liệu được bảo vệ.":null;
        if(reason is null)return;
        try{close();}
        catch(Exception cleanup){throw new AggregateException(reason+" Chưa đóng được phiên vừa mở.",new InvalidOperationException(reason),cleanup);}
        throw new InvalidOperationException(reason);
    }
    private void OpenTransactional(string path)
    {
        using var timing=new WordOperationTiming("Word.Perf.Open");
        bool hadPrevious=Source.Length>0 || document is not null;
        bool oldRecovered=Recovered,checkpointVerified=snapshotCurrent,candidateRecovered=false;
        var ownedFiles=Directory.EnumerateFiles(folder).Concat(undoCheckpoints).ToArray();
        try
        {
            var warnings=StagedDocumentOpen.Apply(RecoveryPath,path,ownedFiles,hadPrevious,()=>
            {
                // A failed mutation may still have newer bytes only in Word.
                // Do not switch away until those bytes have a verified checkpoint.
                if(document is not null)Checkpoint();
                else if(hadPrevious && !snapshotCurrent)throw new InvalidOperationException("Phiên trước còn thay đổi chưa được lưu chắc chắn; giữ bản phục hồi: "+RecoveryPath);
                checkpointVerified=snapshotCurrent;
                timing.Mark("PrepareCopy");
            },CloseForOpen,()=>
            {
                snapshotCurrent=true;Recovered=false;
                try{ReadWithRecovery("Word.Open",()=>{Connect();return true;});candidateRecovered=Recovered;}
                finally{Recovered=oldRecovered;}
            });
            foreach(var warning in warnings)Diagnostics.Write("Word.Open.Cleanup.Retained",warning.Error);
        }
        catch(StagedDocumentOpenException failure)
        {
            snapshotCurrent=failure.CurrentIsVerified && checkpointVerified;
            if(!failure.CurrentIsVerified)Changed=true;
            Recovered=oldRecovered;
            foreach(var warning in failure.CleanupWarnings)Diagnostics.Write("Word.Open.Cleanup.Retained",warning.Error);
            if(CanRecover)failure.Data["ChinhTa.RecoveryInstruction"]="Bản làm việc trước được giữ lại; chọn Kiểm tra → Quét lại để thử mở lại.";
            throw;
        }
        // Until this point Source, Changed, Undo and all previous observations
        // still belong to the prior document, including after a successful rollback.
        Source=Path.GetFullPath(path);Changed=false;Recovered=candidateRecovered;snapshotCurrent=true;
        undoCheckpoints.Clear();verifiedLineEdits.Clear();
        lastLayout=[];lastFormatLayout=[];lastWordParagraphs=[];lastBodyParagraphs=[];
        AdministrativePositionTargets=[];LineTargets=[];LineShapes=[];HasGroupedShapes=false;
        Coverage=null;CoverageNotice=null;NumberCoveragePreviewLayout=null;
        spacingReferencePath=spacingReferenceFlat=null;
        timing.Mark("Connect");timing.Count("Completed",1);
    }
    private void CloseForOpen()=>WordOpenSessionPolicy.Prepare(UseOptimizedReading,CloseDocumentForOpen,CloseApplicationForOpen);
    private void CloseDocumentForOpen()
    {
        object? owned=document;document=null;
        var failure=CloseOwnedConnection(owned,null,Release);
        if(failure is not null)ExceptionDispatchInfo.Capture(failure).Throw();
    }
    private void CloseApplicationForOpen()
    {
        object? ownedDocument=document;document=null;object? ownedApplication=app;app=null;
        var failure=CloseOwnedConnection(ownedDocument,ownedApplication,Release,(stage,error)=>
        {
            if(!WordConnectionPolicy.IsDisconnected(error))return false;
            Diagnostics.Write(stage,error);if(LineCheckArtifactFolder is not null)checkCleanupFailures.Add(error);
            return true;
        });
        if(failure is not null)ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
