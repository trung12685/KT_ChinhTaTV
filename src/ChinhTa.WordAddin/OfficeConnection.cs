using System.Runtime.InteropServices;
using Office=Microsoft.Office.Core;
namespace ChinhTa.WordAddin;
// Office's established automation interface; kept independent of the VS SDK.
[ComVisible(true),Guid("B65AD801-ABAF-11D0-BB8B-00A0C90F2744"),InterfaceType(ComInterfaceType.InterfaceIsDual)]
public interface IOfficeConnection
{
    [DispId(1)]void OnConnection([MarshalAs(UnmanagedType.IDispatch)]object application,int mode,[MarshalAs(UnmanagedType.IDispatch)]object addin,ref Array custom);
    [DispId(2)]void OnDisconnection(int mode,ref Array custom);
    [DispId(3)]void OnAddInsUpdate(ref Array custom);
    [DispId(4)]void OnStartupComplete(ref Array custom);
    [DispId(5)]void OnBeginShutdown(ref Array custom);
}
[ComVisible(true),Guid(Ids.ConnectGuid),ProgId(Ids.ConnectProgId),ClassInterface(ClassInterfaceType.AutoDispatch)]
public sealed class Connect:IOfficeConnection,Office.IRibbonExtensibility,Office.ICustomTaskPaneConsumer
{
    private object? application;
    private Office.ICTPFactory? factory;
    private readonly Dictionary<int,Office.CustomTaskPane> panes=new Dictionary<int,Office.CustomTaskPane>();
    public void OnConnection(object app,int mode,object addin,ref Array custom){application=app;}
    public void OnDisconnection(int mode,ref Array custom)=>Close();
    public void OnAddInsUpdate(ref Array custom){}
    public void OnStartupComplete(ref Array custom){}
    public void OnBeginShutdown(ref Array custom)=>Close();
    public void CTPFactoryAvailable(Office.ICTPFactory value){factory=value;}
    public string GetCustomUI(string ribbonId)=>"<customUI xmlns='http://schemas.microsoft.com/office/2009/07/customui'><ribbon><tabs><tab id='ChinhTaVietTab' label='Chính tả Việt'><group id='ChinhTaVietReview' label='Sửa trực tiếp trong Word'><button id='ChinhTaVietOpen' label='Mở bảng kiểm tra' size='large' imageMso='SpellingAndGrammar' onAction='OpenPane'/></group></tab></tabs></ribbon></customUI>";
    public void OpenPane(object control)
    {
        object? window=null;
        try
        {
            if(application is null || factory is null)throw new InvalidOperationException("Word chưa cung cấp khung Add-in. Hãy mở lại Word sau khi cài đầy đủ gói.");
            dynamic app=application;window=app.ActiveWindow;dynamic w=window;int id=(int)w.Hwnd;
            if(!panes.TryGetValue(id,out var pane))
            {
                pane=factory.CreateCTP(Ids.PaneProgId,"Chính tả Việt — sửa thủ công",window);
                object content=pane.ContentControl;
                if(content is not TaskPaneView view)throw new InvalidOperationException("Word chưa tạo được bảng kiểm tra của Add-in.");
                view.Bind(application,window);window=null;pane.Width=390;pane.DockPosition=Office.MsoCTPDockPosition.msoCTPDockPositionRight;panes.Add(id,pane);
            }
            pane.Visible=true;
        }
        catch(Exception error){HostLog.Error("OpenPane",error);System.Windows.Forms.MessageBox.Show(error.Message,"Chính tả Việt");}
        finally{ComLifetime.Release(window);}
    }
    private void Close()
    {
        foreach(var pane in panes.Values)try{if(pane.ContentControl is TaskPaneView view)view.Dispose();pane.Delete();}catch(Exception error){HostLog.Error("ClosePane",error);}finally{ComLifetime.Release(pane);}
        panes.Clear();ComLifetime.Release(factory);factory=null;application=null;
    }
}
public static class Ids
{
    public const string ConnectGuid="E0382B17-15A1-49D9-AC6A-9322607C6AF7";
    public const string PaneGuid="493F2F97-913D-4E26-8F9C-7B461D073F9F";
    public const string ConnectProgId="ChinhTaViet.WordAddin.Connect";
    public const string PaneProgId="ChinhTaViet.WordAddin.Pane";
}
internal static class ComLifetime
{
    internal static void Release(object? value){if(value is not null && Marshal.IsComObject(value))Marshal.ReleaseComObject(value);}
    internal static bool Same(object left,object right)
    {
        if(ReferenceEquals(left,right))return true;IntPtr a=IntPtr.Zero,b=IntPtr.Zero;
        try{a=Marshal.GetIUnknownForObject(left);b=Marshal.GetIUnknownForObject(right);return a==b;}
        finally{if(a!=IntPtr.Zero)Marshal.Release(a);if(b!=IntPtr.Zero)Marshal.Release(b);}
    }
}
internal static class HostLog
{
    internal static void Error(string operation,Exception error)
    {
        try{string folder=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ChinhTaViet","WordAddin");System.IO.Directory.CreateDirectory(folder);System.IO.File.AppendAllText(System.IO.Path.Combine(folder,"addin.log"),DateTimeOffset.Now.ToString("O")+" "+operation+" "+error.GetType().Name+" 0x"+error.HResult.ToString("X8")+Environment.NewLine);}
        catch(System.IO.IOException){}catch(UnauthorizedAccessException){}
    }
}
