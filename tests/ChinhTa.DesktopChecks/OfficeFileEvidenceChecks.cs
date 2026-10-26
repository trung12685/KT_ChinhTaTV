using System.IO;
using System.Security.Cryptography;
using ChinhTa.Desktop;

public static class OfficeFileEvidenceChecks
{
    public static void Run(Action<bool,string> assert)
    {
        string folder=Path.Combine(Path.GetTempPath(),"ChinhTa-shared-digest-"+Guid.NewGuid());Directory.CreateDirectory(folder);
        string path=Path.Combine(folder,"working.docx");byte[] bytes=Enumerable.Range(0,4096).Select(i=>(byte)(i%251)).ToArray();File.WriteAllBytes(path,bytes);
        try
        {
            using(var wordHandle=new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.Read))
            {
                if(OperatingSystem.IsWindows())
                {
                    bool reproduced=false;try{File.ReadAllBytes(path);}catch(IOException e) when((e.HResult&0xffff)==32){reproduced=true;}
                    assert(reproduced,"An existing Word-style writable handle reproduces the reported File.ReadAllBytes 0x80070020 on Windows");
                }
                long position=wordHandle.Position;var stamp=File.GetLastWriteTimeUtc(path);byte[] expected=SHA256.HashData(bytes);
                assert(expected.SequenceEqual(OfficeFileEvidence.Digest(path)) && wordHandle.Position==position && File.GetLastWriteTimeUtc(path)==stamp,
                    "The read-only shared digest hashes every byte of a Word-held checkpoint without writing, moving the owner handle or changing its save time");
                string copy=Path.Combine(folder,"evidence.docx");OfficeFileEvidence.CopyCheckpoint(path,copy);
                assert(File.ReadAllBytes(copy).SequenceEqual(bytes) && wordHandle.Position==position && File.GetLastWriteTimeUtc(path)==stamp,
                    "Capturing a Word-held checkpoint copies its saved bytes without saving Word, writing the source or moving its handle");
                bool overwrite=false;try{OfficeFileEvidence.CopyCheckpoint(path,copy);}catch(IOException){overwrite=true;}
                assert(overwrite && File.ReadAllBytes(copy).SequenceEqual(bytes),"Evidence capture never overwrites an earlier snapshot");
                wordHandle.WriteByte((byte)(bytes[0]^255));wordHandle.Flush(true);
                assert(!expected.SequenceEqual(OfficeFileEvidence.Digest(path)),"A real checkpoint byte change still fails the original before/after digest comparison");
            }
            using(var exclusive=new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
            {
                bool rejected=false;try{OfficeFileEvidence.Digest(path);}catch(IOException){rejected=true;}
                assert(rejected,"An actually exclusive owner still blocks hashing; the fix cannot swallow I/O failure and invent unchanged bytes");
                string blocked=Path.Combine(folder,"blocked.docx");bool captureBlocked=false;
                try{OfficeFileEvidence.CopyCheckpoint(path,blocked);}catch(IOException){captureBlocked=true;}
                assert(captureBlocked && !File.Exists(blocked),"An unreadable checkpoint cannot produce an invented evidence file");
            }
            bool missing=false;try{OfficeFileEvidence.Digest(Path.Combine(folder,"missing.docx"));}catch(FileNotFoundException){missing=true;}
            assert(missing,"Missing checkpoint evidence propagates as failure instead of a successful comparison");
            using(var exclusive=new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
                assert(exclusive.Length==bytes.Length,"The shared digest closes its own handle and leaves file ownership available to later operations");
        }
        finally{Directory.Delete(folder,true);}
    }
}
