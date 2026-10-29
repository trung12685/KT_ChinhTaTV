using System.IO;
using ChinhTa.Desktop;

public static class TemporaryFileCleanupChecks
{
    public static void Run(Action<bool,string> assert)
    {
        string folder=Path.Combine(Path.GetTempPath(),"ctv-temporary-cleanup-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string path=Path.Combine(folder,"owned-preview.xps"),other=Path.Combine(folder,"owned-candidate.docx");
        byte[] bytes=[1,3,5,7];
        try
        {
            File.WriteAllBytes(path,bytes);WordSession.DeleteStagedBackup(path);
            assert(!File.Exists(path),"Owned intermediate preview/candidate cleanup removes a normal temporary file");
            WordSession.DeleteStagedBackup(path);
            assert(!File.Exists(path),"Repeated cleanup of an already removed intermediate file is harmless");

            File.WriteAllBytes(path,bytes);File.WriteAllBytes(other,bytes);
            using(var held=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read))
            {
                WordSession.DeleteStagedBackup(path);
                assert(File.Exists(path) && File.ReadAllBytes(path).SequenceEqual(bytes),
                    "A real Windows sharing lock retains intermediate bytes without turning cleanup into an operation failure");
                var primary=new InvalidOperationException("synthetic measurement failed");Exception? observed=null;
                try
                {
                    try{throw primary;}
                    finally{WordSession.DeleteStagedBackup(path);}
                }
                catch(Exception failure){observed=failure;}
                assert(ReferenceEquals(observed,primary) && File.Exists(path),
                    "A locked temporary XPS cannot replace the original measurement/rollback exception in a finally block");

                int commits=0;
                int CompleteVerifiedWork()
                {
                    try{commits++;return 73;}
                    finally{WordSession.DeleteStagedBackup(path);}
                }
                assert(CompleteVerifiedWork()==73 && commits==1 && File.Exists(path),
                    "Intermediate cleanup denial cannot erase a verified result or make the committed action run twice");
                foreach(string owned in new[]{path,other})WordSession.DeleteStagedBackup(owned);
                assert(File.Exists(path) && !File.Exists(other),
                    "A locked intermediate file does not skip deletion of another independently owned temporary file");
            }
            WordSession.DeleteStagedBackup(path);
            assert(!File.Exists(path),"Retained intermediate files can be removed after their sharing lock is released");
        }
        finally{Directory.Delete(folder,true);}
    }
}
