using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace ChinhTa.Core;

// Stage on the destination volume. Never fall back to truncating/copying over the destination.
public static class VerifiedSaveCopy
{
    private static FileStream Read(string path)=>new(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
    private static byte[] Digest(string path){using var input=Read(path);return SHA256.HashData(input);}
    private static string Canonical(string path)
    {
        path=Path.GetFullPath(path);
        string current=Path.GetPathRoot(path)!;
        foreach(string part in path[current.Length..].Split([Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar],StringSplitOptions.RemoveEmptyEntries))
        {
            current=Path.Combine(current,part);
            FileAttributes attributes;
            try{attributes=File.GetAttributes(current);}
            catch(FileNotFoundException){continue;}
            catch(DirectoryNotFoundException){continue;}
            if((attributes&FileAttributes.ReparsePoint)!=0)
            {
                FileSystemInfo item=(attributes&FileAttributes.Directory)!=0?new DirectoryInfo(current):new FileInfo(current);
                var target=item.ResolveLinkTarget(true);
                if(target is not null)current=Path.GetFullPath(target.FullName);
            }
        }
        return current;
    }
    internal static void Guard(string working,string destination,IReadOnlyList<string> protectedFiles)
    {
        string resolved=Canonical(destination);
        if(Directory.Exists(destination))throw new IOException("Đích lưu là thư mục; hãy chọn tên tệp DOCX khác.");
        foreach(string path in protectedFiles.Prepend(working))
        {
            if(string.Equals(resolved,Canonical(path),StringComparison.OrdinalIgnoreCase) ||
                OperatingSystem.IsWindows() && File.Exists(destination) && File.Exists(path) && SameWindowsFile(destination,path))
                throw new InvalidOperationException("Hãy chọn tên khác để giữ nguyên tài liệu gốc, bản làm việc và các bản Hoàn tác.");
        }
    }
    // A published result must outlive its session's disposable workspace. Check
    // the directory entry being replaced, not only a final symlink's target.
    public static void GuardOutsideDirectory(string ownedDirectory,string destination)
    {
        string owned=Path.GetFullPath(ownedDirectory),target=Path.GetFullPath(destination);
        static bool Within(string path,string directory)
        {
            directory=Path.TrimEndingDirectorySeparator(directory);
            string prefix=Path.EndsInDirectorySeparator(directory)?directory:directory+Path.DirectorySeparatorChar;
            return string.Equals(path,directory,StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(prefix,StringComparison.OrdinalIgnoreCase);
        }
        string parent=Path.GetDirectoryName(target)??Path.GetPathRoot(target)!;
        string entry=Path.Combine(CanonicalDirectory(parent),Path.GetFileName(target));
        string resolvedOwned=CanonicalDirectory(owned);
        if(Within(target,owned) || Within(entry,resolvedOwned) ||
            Directory.Exists(target) && Within(CanonicalDirectory(target),resolvedOwned))
            throw new InvalidOperationException("Hãy lưu bên ngoài thư mục bản làm việc tạm; thư mục này có thể bị dọn khi đóng phiên.");
    }
    private static string CanonicalDirectory(string path)
    {
        path=Path.TrimEndingDirectorySeparator(Canonical(path));
        if(!OperatingSystem.IsWindows())return path;
        // New output names have no file identity yet. Resolve their nearest
        // existing parent by handle so Win32 dot/space and short-name aliases
        // cannot hide ownership of the directory entry that will be created.
        var missing=new Stack<string>();
        while(!Directory.Exists(path))
        {
            string parent=Path.GetDirectoryName(path)??throw new IOException("Chưa xác minh được thư mục lưu.");
            missing.Push(Path.GetFileName(path));path=parent;
        }
        using var handle=OpenDirectory(path,0,7,IntPtr.Zero,3,0x02000000,IntPtr.Zero);
        if(handle.IsInvalid)throw new Win32Exception(Marshal.GetLastWin32Error());
        var buffer=new StringBuilder(512);string? resolved=null;
        for(int attempt=0;attempt<3;attempt++)
        {
            buffer.Clear();uint length=GetFinalPathNameByHandle(handle,buffer,(uint)buffer.Capacity,0);
            if(length==0)throw new Win32Exception(Marshal.GetLastWin32Error());
            if(length<buffer.Capacity){resolved=buffer.ToString();break;}
            if(length>=65536)throw new IOException("Tên thư mục lưu vượt giới hạn đối chiếu.");
            buffer.EnsureCapacity((int)length+1);
        }
        if(resolved is null)throw new IOException("Chưa đối chiếu được tên thư mục lưu ổn định.");
        if(resolved.StartsWith(@"\\?\UNC\",StringComparison.OrdinalIgnoreCase))resolved=@"\\"+resolved[8..];
        else if(resolved.StartsWith(@"\\?\",StringComparison.Ordinal) && resolved.Length>=7 && resolved[5]==':' && resolved[6]=='\\')
            resolved=resolved[4..];
        while(missing.TryPop(out string? part))resolved=Path.Combine(resolved,part);
        return resolved;
    }
    public static void Write(string working,string destination,IEnumerable<string> protectedFiles,
        Action<Stream,Stream>? copy=null,IEnumerable<string>? protectedDirectories=null)
    {
        working=Path.GetFullPath(working);destination=Path.GetFullPath(destination);
        var protectedPaths=protectedFiles.Select(Path.GetFullPath).ToArray();
        var protectedFolders=protectedDirectories?.Select(Path.GetFullPath).ToArray()??[];
        void GuardDestination()
        {
            foreach(string directory in protectedFolders)GuardOutsideDirectory(directory,destination);
            Guard(working,destination,protectedPaths);
        }
        GuardDestination();
        string stage=Path.Combine(Path.GetDirectoryName(destination)!,".ChinhTaViet-"+Guid.NewGuid().ToString("N")+".tmp");
        bool created=false;
        try
        {
            byte[] expected;
            using(var input=Read(working))
            {
                expected=SHA256.HashData(input);input.Position=0;
                using var output=new FileStream(stage,FileMode.CreateNew,FileAccess.Write,FileShare.None);
                created=true;
                if(copy is null)input.CopyTo(output);else copy(input,output);
                output.Flush(true);
            }
            if(!CryptographicOperations.FixedTimeEquals(expected,Digest(stage)) ||
                !CryptographicOperations.FixedTimeEquals(expected,Digest(working)))
                throw new IOException("Bản sao tạm chưa khớp bản làm việc hoặc bản làm việc đã thay đổi; chưa thay tệp đích.");
            GuardDestination();
            File.Move(stage,destination,true); // Same directory/volume; failure must not use File.Copy as a fallback.
        }
        catch(Exception failure)
        {
            if(created)
                try{File.Delete(stage);}
                catch(Exception cleanup) when(cleanup is IOException or UnauthorizedAccessException)
                {throw new AggregateException("Chưa dọn được bản sao tạm; giữ tệp tại: "+stage,failure,cleanup);}
            throw;
        }
    }
    private static bool SameWindowsFile(string a,string b)
    {
        using var first=File.OpenHandle(a,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
        using var second=File.OpenHandle(b,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
        if(!GetFileInformationByHandle(first,out var left) || !GetFileInformationByHandle(second,out var right))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        return left.Volume==right.Volume && left.IndexHigh==right.IndexHigh && left.IndexLow==right.IndexLow;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct FileInformation
    {
        public uint Attributes,CreationLow,CreationHigh,AccessLow,AccessHigh,WriteLow,WriteHigh,
            Volume,SizeHigh,SizeLow,Links,IndexHigh,IndexLow;
    }
    [DllImport("kernel32.dll",SetLastError=true)]
    [return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandle(SafeFileHandle handle,out FileInformation information);
    [DllImport("kernel32.dll",EntryPoint="CreateFileW",CharSet=CharSet.Unicode,SetLastError=true)]
    private static extern SafeFileHandle OpenDirectory(string path,uint access,uint share,IntPtr security,uint disposition,uint flags,IntPtr template);
    [DllImport("kernel32.dll",EntryPoint="GetFinalPathNameByHandleW",CharSet=CharSet.Unicode,SetLastError=true)]
    private static extern uint GetFinalPathNameByHandle(SafeFileHandle handle,StringBuilder path,uint capacity,uint flags);
}
