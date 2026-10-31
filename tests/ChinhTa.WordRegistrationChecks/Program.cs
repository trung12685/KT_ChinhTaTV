using System.Reflection;
using System.Runtime.InteropServices;

// No reference to the host/protocol projects: COM must load the installed DLL.
internal static class Program
{
    [STAThread]private static void Main(string[] args)
    {
        if(args.Length!=2)throw new ArgumentException("Expected architecture and installed package directory");
        int checks=0;void Assert(bool value,string message){if(!value)throw new Exception("FAIL: "+message);checks++;Console.WriteLine("PASS "+message);}
        Assert(args[0]==(Environment.Is64BitProcess?"x64":"x86"),"Registration probe runs as "+args[0]);
        object connector=Activator.CreateInstance(Type.GetTypeFromProgID("ChinhTaViet.WordAddin.Connect",true)!)!;
        object? pane=null;
        try
        {
            dynamic command=connector;string xml=command.GetCustomUI("Microsoft.Word.Document");
            Assert(System.Xml.Linq.XDocument.Parse(xml).Descendants().Any(e=>e.Name.LocalName=="button"),"The registered connector exposes its real ribbon through COM activation");
            pane=Activator.CreateInstance(Type.GetTypeFromProgID("ChinhTaViet.WordAddin.Pane",true)!)!;
            var loaded=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="ChinhTa.WordAddin");
            Assert(string.Equals(Path.GetFullPath(loaded.Location),Path.GetFullPath(Path.Combine(args[1],"ChinhTa.WordAddin.dll")),StringComparison.OrdinalIgnoreCase),"COM loaded the installed host rather than a test-project copy");
            Assert(loaded.GetName().Version==new Version(0,1,89,0),"The installed COM host has the package assembly version");
            Assert(!loaded.GetReferencedAssemblies().Any(a=>a.Name=="Office"),"Activation requires no redistributed Office PIA");
            void Query(object value,string text,string label)
            {
                IntPtr unknown=Marshal.GetIUnknownForObject(value),result=IntPtr.Zero;
                try{Guid id=new Guid(text);Assert(Marshal.QueryInterface(unknown,ref id,out result)==0,label);}
                finally{if(result!=IntPtr.Zero)Marshal.Release(result);Marshal.Release(unknown);}
            }
            Query(connector,"B65AD801-ABAF-11D0-BB8B-00A0C90F2744","Registered connection IID is available");
            Query(connector,"000C0396-0000-0000-C000-000000000046","Registered Ribbon IID is available");
            Query(connector,"000C033E-0000-0000-C000-000000000046","Registered task-pane consumer IID is available");
            Query(pane,"00000112-0000-0000-C000-000000000046","Registered pane supplies IOleObject in this process architecture");
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
