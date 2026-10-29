using System.IO;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

public sealed partial class WordSession
{
    // This phase reads only paths and session ownership. It never asks Word to
    // save or changes checkpoint/dirty/Undo/review state.
    internal string ValidateSaveCopyDestination(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if(!Path.GetExtension(path).Equals(".docx",StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Chọn tên bản sao có đuôi .docx.");
        return VerifiedSaveCopy.ValidateDestination(RecoveryPath,path,undoCheckpoints.Prepend(Source),[folder]);
    }
    private void SaveCopyCore(string path,Action<Stream,Stream>? copy=null)
    {
        try
        {
            path=ValidateSaveCopyDestination(path);
            Checkpoint();
            VerifiedSaveCopy.Write(RecoveryPath,path,undoCheckpoints.Prepend(Source),copy,[folder]);
            Changed=false; // Only publication of the complete verified copy counts as saved.
        }
        catch(Exception error)
        {
            Diagnostics.Write("Word.SaveCopy.Failed",error);
            if(error is AggregateException aggregate)
                foreach(var cause in aggregate.InnerExceptions)Diagnostics.Write("Word.SaveCopy.Cause",cause);
            throw;
        }
    }
    internal void SaveCopyFailureProbe(string path)
    {
        if(LineCheckArtifactFolder is null)throw new InvalidOperationException("Chỉ dùng trong phiên kiểm Word riêng.");
        SaveCopyCore(path,(input,output)=>
        {
            var bytes=new byte[16];int read=input.Read(bytes);output.Write(bytes,0,read);
            throw new OfficeSaveProbeException();
        });
    }
}

internal sealed class OfficeSaveProbeException():IOException("Lỗi sao chép có chủ ý trong bài kiểm Word riêng.");
