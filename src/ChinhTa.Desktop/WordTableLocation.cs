namespace ChinhTa.Desktop;

// Word's paragraph.Range.Tables can be empty inside a cell (error 5941).
// Work on a collapsed duplicate, then expand it to the containing table.
internal record WordTableLocation(int CellStart, int CellEnd, int TableStart, bool BalancedPadding)
{
    internal static WordTableLocation Read(dynamic range, Action<object?> release)
    {
        dynamic probe = range.Duplicate;
        try
        {
            probe.Collapse(1); // wdCollapseStart; do not include the next cell's end marker.
            dynamic cells = probe.Cells;
            try
            {
                if ((int)cells.Count < 1)
                    throw new System.Runtime.InteropServices.COMException("Cell unavailable", unchecked((int)0x800A1735));
                dynamic cell = cells[1];
                try
                {
                    dynamic cellRange = cell.Range;
                    try
                    {
                        int start = (int)cellRange.Start, end = (int)cellRange.End;
                        bool balanced = Math.Abs((float)cell.LeftPadding - (float)cell.RightPadding) <= 1;
                        probe.Expand(15); // wdTable: full table range, without indexing paragraph.Tables.
                        return new(start, end, (int)probe.Start, balanced);
                    }
                    finally { release(cellRange); }
                }
                finally { release(cell); }
            }
            finally { release(cells); }
        }
        finally { release(probe); }
    }
}
