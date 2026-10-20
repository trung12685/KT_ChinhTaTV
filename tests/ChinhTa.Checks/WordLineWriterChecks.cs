using ChinhTa.Core;
using ChinhTa.Desktop;
using System.Runtime.InteropServices;

public static class WordLineWriterChecks
{
    public static void Run(Action<bool,string> assert)
    {
        var fix = new ShapeLineFix(null,100,120,80,LineRole.Agency);
        var released = new List<object?>();
        var errors = new List<string>();
        void Report(string stage,Exception _) => errors.Add(stage);
        var line = new FakeWordLine();
        WordLineWriter.Apply(null,()=>line,fix,released.Add,Report);
        assert(errors.Count==0 && !line.Deleted && line.Left==100 && line.Top==120 && line.Width==80,
            "Tạo Line thành công khi Word từ chối setter Rotation/Height như lỗi nhật ký");
        assert(line.AlternativeText=="ChinhTaViet.ShapeLine.Agency" && line.WrapFormat.Type==3 &&
            line.Line.Weight==.5f && line.Line.ForeColor.RGB==0 && line.LayoutInCell==0,
            "Line mới giữ marker, nét, wrap và vị trí ngoài ô");
        assert(released.Count==4 && released.Distinct().Count()==4,
            "Giải phóng đủ shape/wrap/line/color sau khi tạo Line");
        var broken = new FakeWordLine { FailWidth = true };
        errors.Clear(); released.Clear();
        bool failed = false;
        try { WordLineWriter.Apply(null,()=>broken,fix,released.Add,Report); }
        catch(COMException ex) { failed=ex.HResult==unchecked((int)0x80004005); }
        assert(failed && broken.Deleted && errors.SequenceEqual(["Word.ApplyLine.Geometry"]) && released.Contains(broken),
            "Lỗi cấu hình xóa Line mới, giữ nguyên HRESULT và ghi đúng bước");
        var existing = new FakeWordLine { FailWidth = true, AlternativeText="Existing" };
        failed=false;
        try { WordLineWriter.Apply(existing,()=>throw new InvalidOperationException(),fix with {ShapeId=42},_=>{},Report); }
        catch(COMException) { failed=true; }
        assert(failed && !existing.Deleted && existing.AlternativeText=="Existing",
            "Không xóa Line có sẵn khi đặt lại bị lỗi; giữ để người dùng Undo");
        var tilted = new FakeWordLine { CurrentHeight=20 };
        failed=false;
        try { WordLineWriter.Apply(tilted,()=>throw new InvalidOperationException(),fix with {ShapeId=42},_=>{},Report); }
        catch(InvalidOperationException) { failed=true; }
        assert(failed && !tilted.Deleted && tilted.Width==0 && tilted.RelativeHorizontalPosition==0,
            "Không xoay hay sửa Line có sẵn không nằm ngang");
    }
}
public sealed class FakeWordLine
{
    public float Rotation { get=>0; set=>throw new COMException("Unsupported rotation",unchecked((int)0x80004005)); }
    public float CurrentHeight { get; set; }
    public float Height { get=>CurrentHeight; set=>throw new COMException("Unsupported height",unchecked((int)0x80004005)); }
    public bool FailWidth { get; set; }
    public bool Deleted { get; private set; }
    public int RelativeHorizontalPosition { get; set; }
    public int RelativeVerticalPosition { get; set; }
    public int LayoutInCell { get; set; }
    public int LockAnchor { get; set; }
    public float Left { get; set; }
    public float Top { get; set; }
    private float width;
    public float Width { get=>width; set { if(FailWidth)throw new COMException("Width failed",unchecked((int)0x80004005)); width=value; } }
    public string AlternativeText { get; set; } = "";
    public FakeWordWrap WrapFormat { get; } = new();
    public FakeWordStroke Line { get; } = new();
    public void Delete() => Deleted=true;
}
public sealed class FakeWordWrap { public int Type { get; set; } }
public sealed class FakeWordColor { public int RGB { get; set; } }
public sealed class FakeWordStroke
{
    public int Visible { get; set; }
    public float Weight { get; set; }
    public int DashStyle { get; set; }
    public int BeginArrowheadStyle { get; set; }
    public int EndArrowheadStyle { get; set; }
    public FakeWordColor ForeColor { get; } = new();
}
