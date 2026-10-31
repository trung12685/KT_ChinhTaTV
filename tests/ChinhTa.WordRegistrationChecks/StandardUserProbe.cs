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
        IntPtr token=IntPtr.Zero,sid=IntPtr.Zero,label=IntPtr.Zero,descriptor=IntPtr.Zero,station=IntPtr.Zero,desktop=IntPtr.Zero,defaultDacl=IntPtr.Zero;
        IntPtr originalStation=IntPtr.Zero;
        var process=new ProcessInfo();string? probeFolder=null;
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
            // The service runner's desktop ACL grants administrator access.
            // Use new, private GUI objects for this lower-privilege child; do
            // not change the ACL of an existing desktop or security policy.
            string user=WindowsIdentity.GetCurrent().User!.Value;
            if(!ConvertStringSecurityDescriptorToSecurityDescriptor("D:(A;;GA;;;"+user+")(A;;GA;;;SY)S:(ML;;NW;;;ME)",1,out descriptor,out _))throw new Win32Exception(Marshal.GetLastWin32Error());
            if(!GetSecurityDescriptorDacl(descriptor,out bool present,out IntPtr acl,out _) || !present)throw new Win32Exception(Marshal.GetLastWin32Error());
            // A service administrator's default DACL may grant only its admin
            // group. The restricted child must still be able to open its own
            // process, threads and newly created synchronization objects.
            defaultDacl=Marshal.AllocHGlobal(IntPtr.Size);Marshal.WriteIntPtr(defaultDacl,acl);
            if(!SetTokenInformation(token,6,defaultDacl,IntPtr.Size))throw new Win32Exception(Marshal.GetLastWin32Error());
            var security=new SecurityAttributes{Length=Marshal.SizeOf(typeof(SecurityAttributes)),Descriptor=descriptor};
            string stationName="ChinhTaVietCi"+Guid.NewGuid().ToString("N");
            station=CreateWindowStation(stationName,0,0x000F037F,ref security);
            if(station==IntPtr.Zero)throw new Win32Exception(Marshal.GetLastWin32Error());
            originalStation=GetProcessWindowStation();
            if(!SetProcessWindowStation(station))throw new Win32Exception(Marshal.GetLastWin32Error());
            try{desktop=CreateDesktop("default",IntPtr.Zero,IntPtr.Zero,0,0x000F01FF,ref security);if(desktop==IntPtr.Zero)throw new Win32Exception(Marshal.GetLastWin32Error());}
            finally{if(!SetProcessWindowStation(originalStation))throw new Win32Exception(Marshal.GetLastWin32Error());originalStation=IntPtr.Zero;}
            string source=System.Reflection.Assembly.GetExecutingAssembly().Location;
            probeFolder=Path.Combine(args[1],"ci-probe-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(probeFolder);
            string executable=Path.Combine(probeFolder,Path.GetFileName(source));File.Copy(source,executable);
            if(File.Exists(source+".config"))File.Copy(source+".config",executable+".config");
            string log=Path.Combine(probeFolder,"result.log");
            var command=new StringBuilder('"'+executable+'"'+" "+args[0]+" \""+args[1]+"\" --standard-user \""+log+"\"");
            var startup=new StartupInfo{Size=Marshal.SizeOf(typeof(StartupInfo)),Desktop=stationName+"\\default"};
            Console.WriteLine("Launching the same architecture registration probe with administrator privileges removed.");
            if(!CreateProcessAsUser(token,executable,command,ref security,ref security,false,0x08000000,IntPtr.Zero,Path.GetDirectoryName(executable)!,ref startup,out process))throw new Win32Exception(Marshal.GetLastWin32Error());
            if(WaitForSingleObject(process.Process,60000)!=0){TerminateProcess(process.Process,1);throw new TimeoutException("Standard-user COM probe did not exit");}
            if(!GetExitCodeProcess(process.Process,out uint result))throw new Win32Exception(Marshal.GetLastWin32Error());
            Console.WriteLine("Standard-user probe exit code: 0x"+result.ToString("X8"));
            if(File.Exists(log))Console.WriteLine(File.ReadAllText(log));else Console.WriteLine("Standard-user probe did not produce its managed startup log.");
            return unchecked((int)result);
        }
        finally
        {
            if(process.Thread!=IntPtr.Zero)CloseHandle(process.Thread);if(process.Process!=IntPtr.Zero)CloseHandle(process.Process);
            if(originalStation!=IntPtr.Zero)SetProcessWindowStation(originalStation);
            if(desktop!=IntPtr.Zero)CloseDesktop(desktop);if(station!=IntPtr.Zero)CloseWindowStation(station);if(descriptor!=IntPtr.Zero)LocalFree(descriptor);
            if(token!=IntPtr.Zero)CloseHandle(token);if(sid!=IntPtr.Zero)Marshal.FreeHGlobal(sid);if(label!=IntPtr.Zero)Marshal.FreeHGlobal(label);
            if(defaultDacl!=IntPtr.Zero)Marshal.FreeHGlobal(defaultDacl);
            if(probeFolder is not null && Directory.Exists(probeFolder))Directory.Delete(probeFolder,true);
        }
    }
    [StructLayout(LayoutKind.Sequential)]private struct SidAndAttributes{public IntPtr Sid;public uint Attributes;}
    [StructLayout(LayoutKind.Sequential)]private struct SecurityAttributes{public int Length;public IntPtr Descriptor;public int Inherit;}
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
    [DllImport("advapi32.dll",CharSet=CharSet.Unicode,SetLastError=true)]private static extern bool CreateProcessAsUser(IntPtr token,string application,StringBuilder command,ref SecurityAttributes processAttributes,ref SecurityAttributes threadAttributes,bool inherit,uint flags,IntPtr environment,string directory,ref StartupInfo startup,out ProcessInfo process);
    [DllImport("kernel32.dll",SetLastError=true)]private static extern bool CloseHandle(IntPtr handle);
    [DllImport("kernel32.dll",SetLastError=true)]private static extern uint WaitForSingleObject(IntPtr handle,uint timeout);
    [DllImport("kernel32.dll",SetLastError=true)]private static extern bool GetExitCodeProcess(IntPtr handle,out uint code);
    [DllImport("kernel32.dll",SetLastError=true)]private static extern bool TerminateProcess(IntPtr handle,uint code);
    [DllImport("kernel32.dll")]private static extern IntPtr GetStdHandle(int kind);
    [DllImport("advapi32.dll",CharSet=CharSet.Unicode,SetLastError=true)]private static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(string value,uint revision,out IntPtr descriptor,out uint size);
    [DllImport("advapi32.dll",SetLastError=true)]private static extern bool GetSecurityDescriptorDacl(IntPtr descriptor,out bool present,out IntPtr acl,out bool defaulted);
    [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)]private static extern IntPtr CreateWindowStation(string name,uint flags,uint access,ref SecurityAttributes security);
    [DllImport("user32.dll",SetLastError=true)]private static extern IntPtr GetProcessWindowStation();
    [DllImport("user32.dll",SetLastError=true)]private static extern bool SetProcessWindowStation(IntPtr station);
    [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)]private static extern IntPtr CreateDesktop(string name,IntPtr device,IntPtr mode,uint flags,uint access,ref SecurityAttributes security);
    [DllImport("user32.dll",SetLastError=true)]private static extern bool CloseDesktop(IntPtr desktop);
    [DllImport("user32.dll",SetLastError=true)]private static extern bool CloseWindowStation(IntPtr station);
    [DllImport("kernel32.dll")]private static extern IntPtr LocalFree(IntPtr memory);
}
