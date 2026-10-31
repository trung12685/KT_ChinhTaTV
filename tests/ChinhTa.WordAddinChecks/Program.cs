using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using ChinhTa.WordAddin;
using ChinhTa.WordProtocol;

internal static class Program
{
    [STAThread]private static void Main()
    {
        int checks=0;void Assert(bool condition,string message){if(!condition)throw new Exception("FAIL: "+message);checks++;Console.WriteLine("PASS "+message);}
        var connect=new Connect();string ribbon=connect.GetCustomUI("Microsoft.Word.Document");
        var xml=System.Xml.Linq.XDocument.Parse(ribbon);
        Assert(xml.Descendants().Any(e=>e.Name.LocalName=="button" && (string?)e.Attribute("onAction")=="OpenPane"),"The real COM host exposes a Word ribbon command for its task pane");
        Assert(typeof(IOfficeConnection).GUID==new Guid("B65AD801-ABAF-11D0-BB8B-00A0C90F2744"),"Office can query the expected connection notification contract");
        var json=new JavaScriptSerializer();string text="NFD tiếng Việt 😀";
        var request=new BridgeRequest{Id=Guid.NewGuid().ToString(),Term=text};
        string ascii=WorkerClient.AsciiJson(json.Serialize(request));
        Assert(ascii.All(c=>c<128) && json.Deserialize<BridgeRequest>(ascii).Term==text,"Vietnamese/NFD/surrogate text survives the Framework process pipe even on a legacy Windows code page");
        using(var pane=new TaskPaneView())
        {
            Assert(pane.Controls.OfType<TabControl>().Single().TabPages.Count==2,"The actual ActiveX task pane exposes independent review and personal dictionary tabs");
            IntPtr unknown=Marshal.GetIUnknownForObject(pane),ole=IntPtr.Zero;
            try{Guid id=new Guid("00000112-0000-0000-C000-000000000046");Assert(Marshal.QueryInterface(unknown,ref id,out ole)==0,"The managed pane supplies IOleObject for Office ActiveX hosting");}
            finally{if(ole!=IntPtr.Zero)Marshal.Release(ole);Marshal.Release(unknown);}
        }
        IntPtr com=Marshal.GetIUnknownForObject(connect),connection=IntPtr.Zero;
        try
        {
            Guid id=typeof(IOfficeConnection).GUID;Assert(Marshal.QueryInterface(com,ref id,out connection)==0,"The built connector supplies Office's actual connection IID through its COM callable wrapper");
            foreach(string key in new[]{"000C0396-0000-0000-C000-000000000046","000C033E-0000-0000-C000-000000000046"})
            {IntPtr pointer=IntPtr.Zero;try{id=new Guid(key);Assert(Marshal.QueryInterface(com,ref id,out pointer)==0,"Office can query the embedded Ribbon and task-pane consumer interfaces: "+key);}finally{if(pointer!=IntPtr.Zero)Marshal.Release(pointer);}}
        }
        finally{if(connection!=IntPtr.Zero)Marshal.Release(connection);Marshal.Release(com);}
        FakeWordChecks.Run(Assert);
        Assert(!typeof(Connect).Assembly.GetReferencedAssemblies().Any(a=>a.Name=="Office"),"The COM host embeds the required Office interface metadata and does not redistribute an Office runtime DLL");
        Console.WriteLine($"{checks} Word host checks passed. Microsoft Word: NOT_RUN.");
    }
}
