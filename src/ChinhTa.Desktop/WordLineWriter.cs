using ChinhTa.Core;

namespace ChinhTa.Desktop;

// Shared with portable regression checks. Real COM stays on the WordSession STA thread.
internal static class WordLineWriter
{
    internal static void Apply(object? existing, Func<object> create, ShapeLineFix fix,
        Action<object?> release, Action<string, Exception> report)
    {
        dynamic? shape = existing;
        bool created = existing is null;
        string stage = "Create";
        try
        {
            shape ??= create(); // AddLine's endpoints already describe a horizontal line.
            stage = "HorizontalCheck";
            if (Math.Abs((float)shape.Height) > .5)
                throw new InvalidOperationException("Line không nằm ngang. Hãy quét lại và kiểm tra thủ công trước khi sửa.");
            stage = "PositionMode";
            shape.RelativeHorizontalPosition = 1;
            shape.RelativeVerticalPosition = 1;
            shape.LayoutInCell = 0;
            shape.LockAnchor = -1;
            stage = "Wrap";
            dynamic wrap = shape.WrapFormat;
            try { wrap.Type = 3; } finally { release(wrap); }
            stage = "Geometry";
            // Word can reject Rotation=0 and Height=0 on msoLine (E_FAIL).
            // Preserve the horizontal geometry created by AddLine; change position/width only.
            shape.Left = (float)fix.Left;
            shape.Top = (float)fix.Top;
            shape.Width = (float)fix.Width;
            stage = "Style";
            dynamic line = shape.Line;
            try
            {
                line.Visible = -1; line.Weight = .5f; line.DashStyle = 1;
                line.BeginArrowheadStyle = 1; line.EndArrowheadStyle = 1;
                dynamic color = line.ForeColor;
                try { color.RGB = 0; } finally { release(color); }
            }
            finally { release(line); }
            stage = "Marker";
            if (created) shape.AlternativeText = "ChinhTaViet.ShapeLine." + fix.Role;
        }
        catch (Exception error)
        {
            report("Word.ApplyLine." + stage, error);
            if (created && shape is not null)
            {
                try { shape.Delete(); }
                catch (Exception cleanup) { report("Word.ApplyLine.Cleanup", cleanup); }
            }
            throw;
        }
        finally { if (shape is not null) release(shape); }
    }
}
