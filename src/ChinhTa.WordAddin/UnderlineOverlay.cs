using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace ChinhTa.WordAddin;
internal sealed class UnderlineMark
{
    internal Rectangle Bounds {get;}
    internal Color Color {get;}
    internal UnderlineMark(Rectangle bounds,Color color){Bounds=bounds;Color=color;}
}
internal static class UnderlinePalette
{
    internal static Color ColorFor(string category)=>category=="Spelling"?Color.Firebrick:category=="Format"?Color.DarkGoldenrod:Color.RoyalBlue;
}
// A click-through owned screen window. No Word text/font/highlight/shape writes.
internal sealed class UnderlineOverlay:Form
{
    private IReadOnlyList<UnderlineMark> marks=Array.Empty<UnderlineMark>();
    internal const int ExtendedFlags=0x00080000|0x00000020|0x08000000|0x00000080;
    protected override bool ShowWithoutActivation=>true;
    protected override CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle|=ExtendedFlags;return p;}}
    internal UnderlineOverlay(){FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;BackColor=Color.Magenta;TransparencyKey=Color.Magenta;AutoScaleMode=AutoScaleMode.None;DoubleBuffered=true;}
    internal void Present(IntPtr owner,IReadOnlyList<UnderlineMark> values)
    {
        if(values.Count==0 || !NativeView.CanRender(owner) || !NativeView.TryClientBounds(owner,out var bounds)){Hide();marks=Array.Empty<UnderlineMark>();return;}
        Bounds=bounds;marks=values.Where(m=>bounds.Contains(m.Bounds)).ToArray();
        if(marks.Count==0){Hide();return;}
        if(!Visible)Show(new OwnerWindow(owner));Invalidate();
    }
    internal void Clear(){marks=Array.Empty<UnderlineMark>();Hide();}
    protected override void WndProc(ref Message m)
    {
        if(m.Msg==0x84){m.Result=new IntPtr(-1);return;}
        if(m.Msg==0x21){m.Result=new IntPtr(3);return;}
        base.WndProc(ref m);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        foreach(var mark in marks)
        {
            var b=mark.Bounds;float left=b.Left-Left,right=b.Right-Left,y=b.Bottom-Top-1;
            using(var pen=new Pen(mark.Color,1.4f))
                for(float x=left;x<right;x+=4){float middle=Math.Min(x+2,right),end=Math.Min(x+4,right);e.Graphics.DrawLine(pen,x,y,middle,y-2);e.Graphics.DrawLine(pen,middle,y-2,end,y);}
        }
    }
    private sealed class OwnerWindow(IntPtr handle):IWin32Window{public IntPtr Handle=>handle;}
}
internal static class NativeView
{
    [StructLayout(LayoutKind.Sequential)]private struct Rect{public int Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential)]private struct Point{public int X,Y;}
    [StructLayout(LayoutKind.Sequential)]private struct GuiInfo{public int Size,Flags;public IntPtr Active,Focus,Capture,MenuOwner,MoveSize,Caret;public Rect CaretBounds;}
    [DllImport("user32.dll")]internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]private static extern bool GetClientRect(IntPtr window,out Rect rectangle);
    [DllImport("user32.dll")]private static extern bool ClientToScreen(IntPtr window,ref Point point);
    [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(IntPtr window,out uint process);
    [DllImport("user32.dll")]private static extern bool GetGUIThreadInfo(uint thread,ref GuiInfo info);
    internal static bool CanRender(IntPtr window)
    {
        if(window==IntPtr.Zero || GetForegroundWindow()!=window)return false;
        var info=new GuiInfo{Size=Marshal.SizeOf(typeof(GuiInfo))};uint thread=GetWindowThreadProcessId(window,out _);
        return thread!=0 && GetGUIThreadInfo(thread,ref info) && (info.Flags&0x1E)==0;
    }
    internal static bool TryClientBounds(IntPtr window,out Rectangle bounds)
    {
        bounds=Rectangle.Empty;var origin=new Point();if(!GetClientRect(window,out var r) || !ClientToScreen(window,ref origin) || r.Right<=r.Left || r.Bottom<=r.Top)return false;
        bounds=new Rectangle(origin.X,origin.Y,r.Right-r.Left,r.Bottom-r.Top);return true;
    }
}
