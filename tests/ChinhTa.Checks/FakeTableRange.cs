// Reproduce a paragraph in a table with no paragraph.Tables member available.
// This checks the calls and cleanup; it does not emulate Word rendering.
public sealed class FakeTableRange
{
    public int Start { get; set; } = 130;
    public int End { get; set; } = 170;
    public bool ThrowOnExpand { get; set; }
    public FakeTableRange? LastDuplicate { get; private set; }
    public FakeTableRange Duplicate => LastDuplicate = new() { Start = Start, End = End, ThrowOnExpand = ThrowOnExpand };
    public object Tables => throw new InvalidOperationException("Paragraph.Tables must not be indexed");
    public FakeCells Cells { get; } = new();
    public bool Collapsed { get; private set; }
    public void Collapse(int direction)
    {
        if (direction != 1) throw new InvalidOperationException("Must collapse to start");
        End = Start;
        Collapsed = true;
    }
    public void Expand(int unit)
    {
        if (!Collapsed || unit != 15) throw new InvalidOperationException("Must expand collapsed range to table");
        if (ThrowOnExpand) throw new System.Runtime.InteropServices.COMException("Missing table", unchecked((int)0x800A1735));
        Start = 100;
        End = 300;
    }
}
public sealed class FakeCells
{
    public int Count => 1;
    public FakeCell Cell { get; } = new();
    public FakeCell this[int index] => index == 1 ? Cell : throw new IndexOutOfRangeException();
}
public sealed class FakeCell
{
    public FakeCellRange Range { get; } = new();
    public float LeftPadding => 5;
    public float RightPadding => 5;
}
public sealed class FakeCellRange
{
    public int Start => 120;
    public int End => 200;
}
