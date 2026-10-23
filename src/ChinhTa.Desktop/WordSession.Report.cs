using System.IO;
using System.Text;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

public sealed partial class WordSession
{
    // No Word calls, checkpoint, dirty-state changes or Undo actions for a program report.
    public void SaveProgressReport(string destination,string report)
    {
        if(!Path.GetExtension(destination).Equals(".txt",StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Chọn tên báo cáo có đuôi .txt.");
        string source=Path.Combine(folder,Guid.NewGuid()+".report.txt");
        try
        {
            File.WriteAllText(source,report,new UTF8Encoding(true));
            var protectedFiles=undoCheckpoints.Append(RecoveryPath);
            if(Source.Length>0)protectedFiles=protectedFiles.Append(Source);
            VerifiedSaveCopy.Write(source,destination,protectedFiles);
        }
        finally
        {
            try{File.Delete(source);}
            catch(Exception error) when(error is IOException or UnauthorizedAccessException)
            {Diagnostics.Write("Word.ProgressReport.Cleanup",error);}
        }
    }
}
