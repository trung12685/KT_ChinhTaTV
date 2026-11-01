using System.Runtime.InteropServices;
using Office=Microsoft.Office.Core;
namespace ChinhTa.WordAddin;
// Office's established automation interface; kept independent of the VS SDK.
[ComImport,Guid("B65AD801-ABAF-11D0-BB8B-00A0C90F2744"),InterfaceType(ComInterfaceType.InterfaceIsDual),TypeLibType((TypeLibTypeFlags)4160)]
public interface IOfficeConnection
{
    [DispId(1)]void OnConnection([In,MarshalAs(UnmanagedType.IDispatch)]object application,[In]int mode,[In,MarshalAs(UnmanagedType.IDispatch)]object addin,[In,MarshalAs(UnmanagedType.SafeArray,SafeArraySubType=VarEnum.VT_VARIANT)]ref Array custom);
    [DispId(2)]void OnDisconnection([In]int mode,[In,MarshalAs(UnmanagedType.SafeArray,SafeArraySubType=VarEnum.VT_VARIANT)]ref Array custom);
    [DispId(3)]void OnAddInsUpdate([In,MarshalAs(UnmanagedType.SafeArray,SafeArraySubType=VarEnum.VT_VARIANT)]ref Array custom);
    [DispId(4)]void OnStartupComplete([In,MarshalAs(UnmanagedType.SafeArray,SafeArraySubType=VarEnum.VT_VARIANT)]ref Array custom);
    [DispId(5)]void OnBeginShutdown([In,MarshalAs(UnmanagedType.SafeArray,SafeArraySubType=VarEnum.VT_VARIANT)]ref Array custom);
}
[ComVisible(true),Guid(Ids.ConnectGuid),ProgId(Ids.ConnectProgId),ClassInterface(ClassInterfaceType.AutoDispatch)]
public sealed class Connect:IOfficeConnection,Office.IRibbonExtensibility,Office.ICustomTaskPaneConsumer
{
    private object? application;
    private Office.ICTPFactory? factory;
    private LiveReviewSession? live;
    private readonly Dictionary<int,Office.CustomTaskPane> panes=new Dictionary<int,Office.CustomTaskPane>();
    public Connect(){HostLog.Event("Construct");}
    public void OnConnection(object app,int mode,object addin,ref Array custom){HostLog.Event("OnConnection.Enter");application=app;live?.Dispose();live=new LiveReviewSession(app);HostLog.Event("OnConnection.Complete");}
    public void OnDisconnection(int mode,ref Array custom){HostLog.Event("OnDisconnection");Close();}
    public void OnAddInsUpdate(ref Array custom){HostLog.Event("OnAddInsUpdate");}
    public void OnStartupComplete(ref Array custom){HostLog.Event("OnStartupComplete");}
    public void OnBeginShutdown(ref Array custom){HostLog.Event("OnBeginShutdown");Close();}
    public void CTPFactoryAvailable(Office.ICTPFactory value){HostLog.Event("CTPFactoryAvailable");factory=value;}
    public string GetCustomUI(string ribbonId)
    {
        HostLog.Event("GetCustomUI");
        return "<customUI xmlns='http://schemas.microsoft.com/office/2009/07/customui'><ribbon><tabs><tab id='ChinhTaVietTab' label='Chính tả Việt'><group id='ChinhTaVietReview' label='Sửa trực tiếp trong Word'><button id='ChinhTaVietOpen' label='Mở bảng kiểm tra' size='large' imageMso='SpellingAndGrammar' onAction='OpenPane'/><checkBox id='ChinhTaVietLive' label='Gạch lỗi khi gõ' screentip='Bật/tắt dấu gạch riêng của Add-in. Dấu gạch gốc của Word theo tùy chọn Word.' getPressed='GetLivePressed' onAction='ToggleLive'/><button id='ChinhTaVietNative' label='Tiếng Việt cho vùng chọn' onAction='EnableNativeProofing'/></group></tab></tabs></ribbon></customUI>";
    }
    public bool GetLivePressed(object control)=>live?.Enabled??true;
    public void ToggleLive(object control,bool pressed){if(live is not null)live.Enabled=pressed;}
    public void EnableNativeProofing(object control)
    {
        try{if(application is null)return;WordNativeProofing.EnableForSelection(application);System.Windows.Forms.MessageBox.Show("Đã bật kiểm chính tả Word và đặt tiếng Việt cho vùng chọn/vị trí đang gõ. Tài liệu có thể cần lưu thay đổi ngôn ngữ.","Chính tả Việt");}
        catch(Exception error){HostLog.Error("NativeProofing",error);System.Windows.Forms.MessageBox.Show(error.Message,"Chính tả Việt");}
    }
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
                view.Bind(application,window,live);window=null;pane.Width=420;pane.DockPosition=Office.MsoCTPDockPosition.msoCTPDockPositionRight;panes.Add(id,pane);
            }
            pane.Visible=true;
            HostLog.Event("OpenPane.Complete");
        }
        catch(Exception error){HostLog.Error("OpenPane",error);System.Windows.Forms.MessageBox.Show(error.Message,"Chính tả Việt");}
        finally{ComLifetime.Release(window);}
    }
    private void Close()
    {
        live?.Dispose();live=null;
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
    internal static Action<string>? Observer {get;set;}
    internal static void Event(string operation)=>Write(operation);
    internal static void Error(string operation,Exception error)=>Write(operation+" "+error.GetType().Name+" 0x"+error.HResult.ToString("X8"));
    private static void Write(string detail)
    {
        try
        {
            if(Observer is not null){Observer(detail);return;}
            string folder=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ChinhTaViet","WordAddin");
            System.IO.Directory.CreateDirectory(folder);string path=System.IO.Path.Combine(folder,"addin.log");
            // No document text, paths, selection or dictionary terms are logged.
            if(System.IO.File.Exists(path) && new System.IO.FileInfo(path).Length>1024*1024)System.IO.File.Delete(path);
            using(var process=System.Diagnostics.Process.GetCurrentProcess())
                System.IO.File.AppendAllText(path,DateTimeOffset.Now.ToString("O")+" v"+typeof(Connect).Assembly.GetName().Version+" "+process.ProcessName+" "+(Environment.Is64BitProcess?"x64":"x86")+" "+detail+Environment.NewLine);
        }
        catch(Exception){} // Diagnostic I/O must never prevent Office from loading.
    }
}
