using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

// Hosted Windows runners use an administrator service token. Exercise the
// per-user COM registration as the same user with administrator rights removed.
// This test helper is never shipped in the Add-in package.
internal static class StandardUserProbe
{
    internal static bool Administrator=>new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
    internal static int Run(string[] args)
    {
        if(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")!="true")throw new InvalidOperationException("Standard-user launch is only used on disposable CI runners");
        IntPtr token=IntPtr.Zero,sid=IntPtr.Zero,label=IntPtr.Zero;
        var process=new ProcessInfo();
        try
        {
            var administrator=new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid,null);
            byte[] bytes=new byte[administrator.BinaryLength];administrator.GetBinaryForm(bytes,0);
            sid=Marshal.AllocHGlobal(bytes.Length);Marshal.Copy(bytes,0,sid,bytes.Length);
            var disable=new[]{new SidAndAttributes{Sid=sid}};
            using(var identity=WindowsIdentity.GetCurrent())
                if(!CreateRestrictedToken(identity.Token,1,1,disable,0,IntPtr.Zero,0,IntPtr.Zero,out token))throw new Win32Exception(Marshal.GetLastWin32Error());
            var medium=new SecurityIdentifier("S-1-16-8192");bytes=new byte[medium.BinaryLength];medium.GetBinaryForm(bytes,0);
            int size=Marshal.SizeOf(typeof(SidAndAttributes));label=Marshal.AllocHGlobal(size+bytes.Length);
            IntPtr integrity=IntPtr.Add(label,size);Marshal.Copy(bytes,0,integrity,bytes.Length);
            Marshal.StructureToPtr(new SidAndAttributes{Sid=integrity,Attributes=0x20},label,false);
            if(!SetTokenInformation(token,25,label,size+bytes.Length))throw new Win32Exception(Marshal.GetLastWin32Error());
            string executable=System.Reflection.Assembly.GetExecutingAssembly().Location;
            var command=new StringBuilder('"'+executable+'"'+" "+args[0]+" \""+args[1]+"\" --standard-user");
            var startup=new StartupInfo{Size=Marshal.SizeOf(typeof(StartupInfo)),Flags=0x100,StdInput=GetStdHandle(-10),StdOutput=GetStdHandle(-11),StdError=GetStdHandle(-12)};
            Console.WriteLine("Launching the same architecture registration probe with administrator privileges removed.");
            if(!CreateProcessAsUser(token,executable,command,IntPtr.Zero,IntPtr.Zero,true,0x08000000,IntPtr.Zero,Path.GetDirectoryName(executable)!,ref startup,out process))throw new Win32Exception(Marshal.GetLastWin32Error());
            if(WaitForSingleObject(process.Process,60000)!=0){TerminateProcess(process.Process,1);throw new TimeoutException("Standard-user COM probe did not exit");}
            if(!GetExitCodeProcess(process.Process,out uint result))throw new Win32Exception(Marshal.GetLastWin32Error());
            return unchecked((int)result);
        }
        finally
        {
            if(process.Thread!=IntPtr.Zero)CloseHandle(process.Thread);if(process.Process!=IntPtr.Zero)CloseHandle(process.Process);
            if(token!=IntPtr.Zero)CloseHandle(token);if(sid!=IntPtr.Zero)Marshal.FreeHGlobal(sid);if(label!=IntPtr.Zero)Marshal.FreeHGlobal(label);
        }
    }
    [StructLayout(LayoutKind.Sequential)]private struct SidAndAttributes{public IntPtr Sid;public uint Attributes;}
    [StructLayout(LayoutKind.Sequential)]private struct ProcessInfo{public IntPtr Process,Thread;public uint ProcessId,ThreadId;}
    #pragma warning disable CS0649 // Native STARTUPINFO fields intentionally left zero.
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]private struct StartupInfo
    {
        public int Size;public string? Reserved,Desktop,Title;public uint X,Y,Width,Height,CharsX,CharsY,Fill,Flags;
        public ushort Show,ReservedSize;public IntPtr ReservedBytes,StdInput,StdOutput,StdError;
    }
    #pragma warning restore CS0649
    [DllImport("advapi32.dll",SetLastError=true)]private static extern bool CreateRestrictedToken(IntPtr existing,uint flags,uint disabled,[In]SidAndAttributes[] sids,uint deleted,IntPtr privileges,uint restricted,IntPtr restricting,out IntPtr token);
    [DllImport("advapi32.dll",SetLastError=true)]private static extern bool SetTokenInformation(IntPtr token,int type,IntPtr information,int length);
    [DllImport("advapi32.dll",CharSet=CharSet.Unicode,SetLastError=true)]private static extern bool CreateProcessAsUser(IntPtr token,string application,StringBuilder command,IntPtr processAttributes,IntPtr threadAttributes,bool inherit,uint flags,IntPtr environment,string directory,ref StartupInfo startup,out ProcessInfo process);
    [DllImport("kernel32.dll",SetLastError=true)]private static extern bool CloseHandle(IntPtr handle);
    [DllImport("kernel32.dll",SetLastError=true)]private static extern uint WaitForSingleObject(IntPtr handle,uint timeout);
    [DllImport("kernel32.dll",SetLastError=true)]private static extern bool GetExitCodeProcess(IntPtr handle,out uint code);
    [DllImport("kernel32.dll",SetLastError=true)]private static extern bool TerminateProcess(IntPtr handle,uint code);
    [DllImport("kernel32.dll")]private static extern IntPtr GetStdHandle(int kind);
}
