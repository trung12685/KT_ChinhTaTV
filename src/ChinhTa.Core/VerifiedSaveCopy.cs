using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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
    private static void Guard(string working,string destination,IReadOnlyList<string> protectedFiles)
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
    public static void Write(string working,string destination,IEnumerable<string> protectedFiles,
        Action<Stream,Stream>? copy=null)
    {
        working=Path.GetFullPath(working);destination=Path.GetFullPath(destination);
        var protectedPaths=protectedFiles.Select(Path.GetFullPath).ToArray();
        Guard(working,destination,protectedPaths);
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
            Guard(working,destination,protectedPaths);
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
}
