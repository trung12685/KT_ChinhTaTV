using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using ChinhTa.Core;

public static class DirectoryContainmentSaveChecks
{
    private static bool Reject(Action action)
    {try{action();return false;}catch(InvalidOperationException){return true;}}

    public static void Run(Action<bool,string> assert)
    {
        string folder=Path.Combine(Path.GetTempPath(),"ctv-save-containment-"+Guid.NewGuid().ToString("N"));
        string owned=Path.Combine(folder,"private-work"),outside=owned+"-published",nested=Path.Combine(owned,"nested");
        Directory.CreateDirectory(nested);Directory.CreateDirectory(outside);
        try
        {
            string working=Path.Combine(folder,"working.docx"),source=Path.Combine(folder,"source.docx");
            File.WriteAllText(working,"complete working bytes");File.WriteAllText(source,"immutable source");
            string existing=Path.Combine(owned,"existing.docx");File.WriteAllText(existing,"old private entry");
            foreach(string root in new[]{owned,owned+Path.DirectorySeparatorChar})
            foreach(string target in new[]{owned,existing,Path.Combine(owned,"new.docx"),Path.Combine(nested,"new.docx"),
                Path.Combine(nested,"missing","new.docx"),Path.Combine(owned,"..","private-work","new.docx")})
            {
                assert(Reject(()=>VerifiedSaveCopy.GuardOutsideDirectory(root,target)),
                    "An owned directory, existing/new child, nested missing child or normalized alias cannot be a published result, with or without a trailing root separator");
                int copies=0;
                assert(Reject(()=>VerifiedSaveCopy.Write(working,target,[source],(input,output)=>{copies++;input.CopyTo(output);},[root])) &&
                    copies==0 && File.ReadAllText(existing)=="old private entry" && File.ReadAllText(source)=="immutable source" &&
                    !Directory.EnumerateFiles(owned,".ChinhTaViet-*.tmp",SearchOption.AllDirectories).Any(),
                    "The owned-directory boundary rejects before copying or creating a stage and leaves existing entries/source unchanged");
            }
            foreach(string target in new[]{Path.Combine(outside,"published.docx"),Path.Combine(folder,"published.docx")})
            {
                VerifiedSaveCopy.GuardOutsideDirectory(owned,target);
                VerifiedSaveCopy.Write(working,target,[source],protectedDirectories:[owned]);
                assert(File.ReadAllText(target)=="complete working bytes",
                    "Independent destinations and sibling directory names sharing a prefix remain valid, including ordinary temporary test directories");
            }
            assert(Reject(()=>VerifiedSaveCopy.Write(working,source,[source],protectedDirectories:[owned])) && File.ReadAllText(source)=="immutable source",
                "Adding a disposable-directory guard cannot bypass existing protected source-file identity checks");
            string external=Path.Combine(outside,"external.docx");File.WriteAllText(external,"unrelated external content");
            bool symlinks=true;
            string parentAlias=Path.Combine(folder,"parent-alias");
            try{Directory.CreateSymbolicLink(parentAlias,owned);}
            catch(Exception error) when(OperatingSystem.IsWindows() && error is UnauthorizedAccessException or IOException)
            {symlinks=false;Console.WriteLine("SKIP owned-directory symlink fixtures: "+error.GetType().Name);}
            if(symlinks)
            {
                string escapingEntry=Path.Combine(owned,"escaping.docx");File.CreateSymbolicLink(escapingEntry,external);
                foreach(string target in new[]{Path.Combine(parentAlias,"new.docx"),escapingEntry,Path.Combine(parentAlias,"escaping.docx")})
                {
                    assert(Reject(()=>VerifiedSaveCopy.GuardOutsideDirectory(owned,target)) &&
                        Reject(()=>VerifiedSaveCopy.Write(working,target,[source],protectedDirectories:[owned])) &&
                        File.ReadAllText(external)=="unrelated external content",
                        "Directory aliases into the workspace and final symlinks out of it cannot hide the disposable directory entry being published");
                }
                assert(Reject(()=>VerifiedSaveCopy.GuardOutsideDirectory(parentAlias,Path.Combine(owned,"new.docx"))),
                    "An owned-root alias resolves to the same physical disposable directory");
                string rootAlias=Path.Combine(folder,"root-alias");Directory.CreateSymbolicLink(rootAlias,owned);
                string destination=Path.Combine(outside,"recheck.docx");File.WriteAllText(destination,"old published value");
                int copies=0;bool rejected=Reject(()=>VerifiedSaveCopy.Write(working,destination,[source],(input,output)=>
                {
                    copies++;input.CopyTo(output);Directory.Delete(rootAlias);Directory.CreateSymbolicLink(rootAlias,outside);
                },[rootAlias]));
                assert(rejected && copies==1 && File.ReadAllText(destination)=="old published value" &&
                    !Directory.EnumerateFiles(outside,".ChinhTaViet-*.tmp").Any(),
                    "Directory containment is checked again after copying before publication, preserving the prior destination and cleaning the stage if its boundary resolves differently");
                Directory.Delete(rootAlias);File.Delete(escapingEntry);Directory.Delete(parentAlias);
            }
            if(OperatingSystem.IsWindows())
            {
                string extended=owned.StartsWith(@"\\",StringComparison.Ordinal)?@"\\?\UNC\"+owned[2..]:@"\\?\"+owned;
                foreach(string alias in new[]{owned+".",owned+" ",extended})
                {
                    assert(Directory.Exists(alias) && Reject(()=>VerifiedSaveCopy.GuardOutsideDirectory(owned,alias)) &&
                        Reject(()=>VerifiedSaveCopy.GuardOutsideDirectory(owned,Path.Combine(alias,"new.docx"))),
                        "A Windows trailing-dot/space or extended namespace directory alias cannot publish inside the disposable workspace");
                }
                assert(Reject(()=>VerifiedSaveCopy.GuardOutsideDirectory(extended,Path.Combine(owned,"new.docx"))),
                    "Owned and output directories use the same normalized Windows handle namespace");
                var shortName=new StringBuilder(65536);uint length=GetShortPathName(owned,shortName,(uint)shortName.Capacity);
                if(length==0)throw new Win32Exception(Marshal.GetLastWin32Error());
                if(length>=shortName.Capacity)throw new Exception("The synthetic directory's short-name buffer must be sufficient");
                string shortPath=shortName.ToString();
                if(!string.Equals(shortPath,owned,StringComparison.OrdinalIgnoreCase))
                    assert(Directory.Exists(shortPath) && Reject(()=>VerifiedSaveCopy.GuardOutsideDirectory(owned,Path.Combine(shortPath,"new.docx"))),
                        "An available Windows 8.3 directory alias cannot publish inside the disposable workspace");
                else Console.WriteLine("SKIP owned-directory 8.3 alias fixture: no alternate short name is available");
            }
        }
        finally{Directory.Delete(folder,true);}
    }
    [DllImport("kernel32.dll",EntryPoint="GetShortPathNameW",CharSet=CharSet.Unicode,SetLastError=true)]
    private static extern uint GetShortPathName(string path,StringBuilder shortPath,uint capacity);
}
