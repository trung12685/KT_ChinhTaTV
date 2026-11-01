using System.Reflection;
using System.Runtime.InteropServices;

// No reference to the host/protocol projects: COM must load the installed DLL.
internal static class Program
{
    [STAThread]private static void Main(string[] args)
    {
        if(args.Length==2 && StandardUserProbe.Administrator){Environment.ExitCode=StandardUserProbe.Run(args);return;}
        if(args.Length==4 && args[2]=="--standard-user")
        {
            var log=new StreamWriter(args[3],false,new System.Text.UTF8Encoding(false)){AutoFlush=true};Console.SetOut(log);Console.SetError(log);
            AppDomain.CurrentDomain.UnhandledException+=(_,e)=>Console.Error.WriteLine(e.ExceptionObject);
            args=args.Take(2).ToArray();
        }
        if(args.Length!=2)throw new ArgumentException("Expected architecture and installed package directory");
        int checks=0;void Assert(bool value,string message){if(!value)throw new Exception("FAIL: "+message);checks++;Console.WriteLine("PASS "+message);}
        Assert(!StandardUserProbe.Administrator,"Per-user COM activation is tested without administrator privileges");
        Assert(args[0]==(Environment.Is64BitProcess?"x64":"x86"),"Registration probe runs as "+args[0]);
        Console.WriteLine("Administrator role enabled: "+new System.Security.Principal.WindowsPrincipal(System.Security.Principal.WindowsIdentity.GetCurrent()).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator));
        Console.WriteLine("EnableLUA: "+Microsoft.Win32.Registry.GetValue("HKEY_LOCAL_MACHINE\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System","EnableLUA",null));
        string registered="CLSID\\{E0382B17-15A1-49D9-AC6A-9322607C6AF7}\\InprocServer32";
        using(var key=Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(registered))
        {
            Console.WriteLine("Merged COM registry: "+(key is null?"MISSING":string.Join("; ",key.GetValueNames().Select(n=>n+"="+key.GetValue(n)))));
        }
        object connector;
        try{connector=Activator.CreateInstance(Type.GetTypeFromProgID("ChinhTaViet.WordAddin.Connect",true)!)!;}
        catch(Exception failure)
        {
            Console.Error.WriteLine("Native COM activation failed before any explicit host assembly load: "+failure);
            try
            {
                var diagnostic=Assembly.LoadFrom(Path.Combine(args[1],"ChinhTa.WordAddin.dll"));
                Console.Error.WriteLine("Diagnostic assembly: "+diagnostic.FullName+" at "+diagnostic.Location);
                var instance=diagnostic.CreateInstance("ChinhTa.WordAddin.Connect");
                Console.Error.WriteLine("Diagnostic managed creation: "+instance?.GetType().FullName);
            }
            catch(Exception diagnosticFailure){Console.Error.WriteLine("Diagnostic load failure: "+diagnosticFailure);}
            throw;
        }
        object? pane=null;
        try
        {
            dynamic command=connector;string xml=command.GetCustomUI("Microsoft.Word.Document");
            Assert(System.Xml.Linq.XDocument.Parse(xml).Descendants().Any(e=>e.Name.LocalName=="button"),"The registered connector exposes its real ribbon through COM activation");
            pane=Activator.CreateInstance(Type.GetTypeFromProgID("ChinhTaViet.WordAddin.Pane",true)!)!;
            var loaded=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="ChinhTa.WordAddin");
            Assert(string.Equals(Path.GetFullPath(loaded.Location),Path.GetFullPath(Path.Combine(args[1],"ChinhTa.WordAddin.dll")),StringComparison.OrdinalIgnoreCase),"COM loaded the installed host rather than a test-project copy");
            Assert(loaded.GetName().Version==new Version(0,1,91,0),"The installed COM host has the package assembly version");
            Assert(!loaded.GetReferencedAssemblies().Any(a=>a.Name=="Office"),"Activation requires no redistributed Office PIA");
            void Query(object value,string text,string label)
            {
                IntPtr unknown=Marshal.GetIUnknownForObject(value),result=IntPtr.Zero;
                try{Guid id=new Guid(text);Assert(Marshal.QueryInterface(unknown,ref id,out result)==0,label);}
                finally{if(result!=IntPtr.Zero)Marshal.Release(result);Marshal.Release(unknown);}
            }
            NativeLifecycleChecks.Run(connector,Assert);
            Query(connector,"B65AD801-ABAF-11D0-BB8B-00A0C90F2744","Registered connection IID is available");
            Query(connector,"000C0396-0000-0000-C000-000000000046","Registered Ribbon IID is available");
            Query(connector,"000C033E-0000-0000-C000-000000000046","Registered task-pane consumer IID is available");
            Query(pane,"00000112-0000-0000-C000-000000000046","Registered pane supplies IOleObject in this process architecture");
            using(var diagnostic=new System.Diagnostics.Process())
            {
                diagnostic.StartInfo=new System.Diagnostics.ProcessStartInfo
                {
                    FileName=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"WindowsPowerShell","v1.0","powershell.exe"),
                    Arguments="-NoProfile -ExecutionPolicy Bypass -File \""+Path.Combine(args[1],"Diagnose-WordAddin.ps1")+"\"",
                    UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true
                };
                diagnostic.Start();
                var output=diagnostic.StandardOutput.ReadToEndAsync();var errors=diagnostic.StandardError.ReadToEndAsync();
                if(!diagnostic.WaitForExit(45000)){diagnostic.Kill();throw new TimeoutException("Installed diagnostic timed out");}
                Console.WriteLine(output.GetAwaiter().GetResult());Console.WriteLine(errors.GetAwaiter().GetResult());
                string report=File.ReadAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ChinhTaViet","WordAddin","load-diagnostic.txt"));
                Assert(diagnostic.ExitCode==0 && report.Contains("NativeCOM=PASS") && report.Contains("WordActual=NOT_RUN") && report.Contains("Marshal=SafeArray") && !report.Contains("TenRiengCI"),"The shipped Windows PowerShell diagnostic runs in the package architecture and verifies native activation without reading personal terms");
            }
        }
        finally
        {
            if(pane is IDisposable disposable)disposable.Dispose();
            if(pane is not null && Marshal.IsComObject(pane))Marshal.ReleaseComObject(pane);
            if(Marshal.IsComObject(connector))Marshal.ReleaseComObject(connector);
        }
        Console.WriteLine($"{checks} registration checks passed ({args[0]}). Microsoft Word: NOT_RUN.");
    }
}
