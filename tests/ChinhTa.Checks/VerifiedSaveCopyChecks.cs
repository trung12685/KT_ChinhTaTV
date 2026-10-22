using System.ComponentModel;
using System.Runtime.InteropServices;
using ChinhTa.Core;

public static class VerifiedSaveCopyChecks
{
    public static void Run(Action<bool,string> assert)
    {
        string folder=Path.Combine(Path.GetTempPath(),"SaveCopyChecks-"+Guid.NewGuid());Directory.CreateDirectory(folder);
        try
        {
            string original=Path.Combine(folder,"original.docx"),working=Path.Combine(folder,"working.docx"),
                undo=Path.Combine(folder,"undo.docx"),saved=Path.Combine(folder,"saved.docx");
            byte[] source=[1,2,3,4],current=Enumerable.Range(0,65537).Select(i=>(byte)(i%251)).ToArray(),previous=[9,8,7,6];
            File.WriteAllBytes(original,source);File.WriteAllBytes(working,current);File.WriteAllBytes(undo,previous);
            string[] protectedFiles=[original,undo];
            void NoStage()=>assert(!Directory.EnumerateFiles(folder,".ChinhTaViet-*.tmp").Any(),"The save operation leaves no partial staging file after a verified publication or handled failure");
            VerifiedSaveCopy.Write(working,saved,protectedFiles);
            assert(File.ReadAllBytes(saved).SequenceEqual(current) && File.ReadAllBytes(original).SequenceEqual(source) &&
                File.ReadAllBytes(working).SequenceEqual(current) && File.ReadAllBytes(undo).SequenceEqual(previous),
                "A new saved copy matches every working byte and leaves original, working and Undo files unchanged");NoStage();
            foreach(bool existing in new[]{true,false})
            {
                if(existing)File.WriteAllBytes(saved,previous);else File.Delete(saved);
                var failure=new IOException("synthetic interrupted copy");bool interrupted=false;
                try{VerifiedSaveCopy.Write(working,saved,protectedFiles,(input,output)=>
                {
                    var bytes=new byte[5];input.ReadExactly(bytes);output.Write(bytes);throw failure;
                });}
                catch(IOException error){interrupted=ReferenceEquals(error,failure);}
                assert(interrupted && (existing?File.ReadAllBytes(saved).SequenceEqual(previous):!File.Exists(saved)),
                    "Interrupted staging preserves an old destination or leaves a new destination absent, instead of publishing partial bytes");NoStage();
            }
            foreach(bool truncate in new[]{true,false})
            {
                File.WriteAllBytes(saved,previous);bool invalid=false;
                try{VerifiedSaveCopy.Write(working,saved,protectedFiles,(input,output)=>
                {
                    if(truncate)output.WriteByte(1);
                    else {var bytes=current.ToArray();bytes[100]^=1;output.Write(bytes);}
                });}
                catch(IOException){invalid=true;}
                assert(invalid && File.ReadAllBytes(saved).SequenceEqual(previous),
                    "Length mismatch or same-length corruption fails byte verification before an existing saved copy is replaced");NoStage();
            }
            bool changedWorking=false;
            try{VerifiedSaveCopy.Write(working,saved,protectedFiles,(input,output)=>
            {
                input.CopyTo(output);var changed=current.ToArray();changed[0]^=1;File.WriteAllBytes(working,changed);
            });}
            catch(IOException){changedWorking=true;}
            assert(changedWorking && File.ReadAllBytes(saved).SequenceEqual(previous),"A working file that changes during copying cannot publish an obsolete snapshot");
            File.WriteAllBytes(working,current);NoStage();
            foreach(string target in new[]{original,working,undo,Path.Combine(folder,".","original.docx")})
            {
                bool rejected=false;int copies=0;
                try{VerifiedSaveCopy.Write(working,target,protectedFiles,(input,output)=>{copies++;input.CopyTo(output);});}
                catch(InvalidOperationException){rejected=true;}
                assert(rejected && copies==0 && File.ReadAllBytes(original).SequenceEqual(source) && File.ReadAllBytes(working).SequenceEqual(current) &&
                    File.ReadAllBytes(undo).SequenceEqual(previous),"Direct and normalized aliases of original, working and Undo files are rejected before staging");
            }
            bool directoryRejected=false;
            try{VerifiedSaveCopy.Write(working,folder,protectedFiles);}catch(IOException){directoryRejected=true;}
            assert(directoryRejected && File.ReadAllBytes(original).SequenceEqual(source),"A directory cannot be used as the output file");
            string absent=Path.Combine(folder,"absent","out.docx");bool missingParent=false;
            try{VerifiedSaveCopy.Write(working,absent,protectedFiles);}catch(IOException){missingParent=true;}
            assert(missingParent && !File.Exists(absent),"A missing output directory fails without creating or truncating a destination");NoStage();
            if(OperatingSystem.IsWindows())
            {
                string alias=Path.Combine(folder,"original-hardlink.docx");
                if(!CreateHardLink(alias,original,IntPtr.Zero))throw new Win32Exception(Marshal.GetLastWin32Error());
                bool hardlinkRejected=false;
                try{VerifiedSaveCopy.Write(working,alias,protectedFiles);}catch(InvalidOperationException){hardlinkRejected=true;}
                assert(hardlinkRejected && File.ReadAllBytes(original).SequenceEqual(source) && File.ReadAllBytes(alias).SequenceEqual(source),
                    "Windows file identity rejects a hard-link alias of the original document");
                File.WriteAllBytes(saved,previous);bool locked=false;
                using(var held=new FileStream(saved,FileMode.Open,FileAccess.Read,FileShare.Read))
                {
                    try{VerifiedSaveCopy.Write(working,saved,protectedFiles);}catch(IOException){locked=true;}
                    assert(locked && File.ReadAllBytes(saved).SequenceEqual(previous),
                        "An existing destination that denies replacement stays intact when Windows rejects publication");
                }
                NoStage();
            }
            else
            {
                string alias=Path.Combine(folder,"original-link.docx");File.CreateSymbolicLink(alias,original);
                bool linkRejected=false;
                try{VerifiedSaveCopy.Write(working,alias,protectedFiles);}catch(InvalidOperationException){linkRejected=true;}
                assert(linkRejected && File.ReadAllBytes(original).SequenceEqual(source),"Resolved file symlinks cannot redirect a saved copy onto the original");
                string directory=Path.Combine(folder,"directory-link");Directory.CreateSymbolicLink(directory,folder);
                bool parentLinkRejected=false;
                try{VerifiedSaveCopy.Write(working,Path.Combine(directory,"original.docx"),protectedFiles);}catch(InvalidOperationException){parentLinkRejected=true;}
                assert(parentLinkRejected && File.ReadAllBytes(original).SequenceEqual(source),"Resolved parent-directory aliases cannot redirect a saved copy onto the original");
                Directory.Delete(directory);
            }
            File.WriteAllBytes(saved,previous);
            VerifiedSaveCopy.Write(working,saved,protectedFiles);
            assert(File.ReadAllBytes(saved).SequenceEqual(current) && File.ReadAllBytes(original).SequenceEqual(source) &&
                File.ReadAllBytes(undo).SequenceEqual(previous),"A successful verified replacement publishes the entire copy without modifying protected files");NoStage();
        }
        finally{Directory.Delete(folder,true);}
    }
    [DllImport("kernel32.dll",EntryPoint="CreateHardLinkW",CharSet=CharSet.Unicode,SetLastError=true)]
    [return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateHardLink(string name,string existing,IntPtr security);
}
