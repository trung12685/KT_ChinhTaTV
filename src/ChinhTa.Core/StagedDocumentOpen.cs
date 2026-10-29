namespace ChinhTa.Core;

public sealed class StagedDocumentOpenException : Exception
{
    public bool CurrentIsVerified { get; }
    public string RecoveryPath { get; }
    public IReadOnlyList<CheckpointCleanup> CleanupWarnings { get; }
    internal StagedDocumentOpenException(Exception failure,bool currentIsVerified,string recoveryPath,IReadOnlyList<CheckpointCleanup> cleanup)
        :base((currentIsVerified?"Chưa mở được tài liệu mới; bản làm việc trước vẫn được giữ nguyên. ":
            "Chưa xác minh được việc phục hồi phiên trước; giữ bản làm việc và các bản phục hồi. ")+failure.Message,failure)
    {CurrentIsVerified=currentIsVerified;RecoveryPath=recoveryPath;CleanupWarnings=cleanup;HResult=failure.HResult;}
}

// Prepare and verify the incoming bytes before closing the current document.
// Metadata/Undo belong to the caller and must only be committed after this returns.
public static class StagedDocumentOpen
{
    public static IReadOnlyList<CheckpointCleanup> Apply(string current,string source,IEnumerable<string> protectedFiles,
        bool hadPrevious,Action checkpointCurrent,Action close,Action open,Action<Stream,Stream>? copy=null,
        Action<string>? deleteTemporary=null)
    {
        ArgumentNullException.ThrowIfNull(checkpointCurrent);ArgumentNullException.ThrowIfNull(close);ArgumentNullException.ThrowIfNull(open);
        current=Path.GetFullPath(current);source=Path.GetFullPath(source);
        string stage=Path.Combine(Path.GetDirectoryName(current)!,Guid.NewGuid().ToString("N")+".open.docx");
        var cleanup=new List<CheckpointCleanup>();Exception? failure=null;bool currentVerified=true;
        try
        {
            var owned=protectedFiles.Select(Path.GetFullPath).Append(current).ToArray();
            // In reverse, the save guard rejects an input alias of any private
            // working/Undo file, including Windows hardlinks and resolved links.
            VerifiedSaveCopy.Guard(current,source,owned);
            if(!hadPrevious && File.Exists(current))
            {currentVerified=false;throw new InvalidOperationException("Bản làm việc hiện có chưa thuộc một phiên đã xác minh; hãy giữ lại để phục hồi.");}
            VerifiedSaveCopy.Write(source,stage,owned,copy);
            currentVerified=false;checkpointCurrent();currentVerified=true;
            if(hadPrevious)
            {
                try
                {
                    var warning=CheckpointSwap.Apply(current,stage,close,open,deleteTemporary);
                    if(warning is not null)cleanup.Add(warning);
                }
                catch(Exception error)
                {
                    currentVerified=CheckpointSwap.CurrentIsVerified(error);
                    if(error.Data[CheckpointSwap.CleanupDataKey] is CheckpointCleanup warning)cleanup.Add(warning);
                    throw;
                }
            }
            else
            {
                bool published=false;
                try
                {
                    close();File.Move(stage,current);published=true;currentVerified=false;
                    open();currentVerified=true;
                }
                catch(Exception error)
                {
                    if(published)
                    {
                        try{close();File.Delete(current);currentVerified=true;}
                        catch(Exception restore){throw new AggregateException("Không đóng/dọn được bản mở chưa thành công tại: "+current,error,restore);}
                    }
                    throw;
                }
            }
        }
        catch(Exception error){failure=error;}
        finally
        {
            if(File.Exists(stage))
                try{if(deleteTemporary is null)File.Delete(stage);else deleteTemporary(stage);}
                catch(Exception error) when(error is IOException or UnauthorizedAccessException){cleanup.Add(new(stage,error));}
        }
        if(failure is not null)throw new StagedDocumentOpenException(failure,currentVerified,current,cleanup.ToArray());
        return cleanup.ToArray();
    }
}
