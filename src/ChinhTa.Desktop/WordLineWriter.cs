using ChinhTa.Core;

namespace ChinhTa.Desktop;

// Shared with portable regression checks. Real COM stays on the WordSession STA thread.
internal static class WordLineWriter
{
    internal static void Apply(object? existing, Func<object> create, ShapeLineFix fix,
        Action<object?> release, Action<string, Exception> report)
    {
        dynamic? shape = existing;
        dynamic? wrap = null, line = null, color = null;
        bool created = existing is null;
        string stage = "Create";
        try
        {
            shape ??= create(); // AddLine's endpoints already describe a horizontal line.
            stage = "HorizontalCheck";
            if (Math.Abs((float)shape.Height) > .5)
                throw new InvalidOperationException("Line không nằm ngang. Hãy quét lại và kiểm tra thủ công trước khi sửa.");
            // Configure appearance while the new Shape is still in its original anchor state.
            stage = "Style.Line";
            line = shape.Line;
            stage = "Style.Visible";
            line.Visible = -1;
            stage = "Style.Weight";
            line.Weight = .5f;
            stage = "Style.Dash";
            line.DashStyle = 1;
            stage = "Style.Arrows";
            line.BeginArrowheadStyle = 1; line.EndArrowheadStyle = 1;
            stage = "Style.Color";
            color = line.ForeColor;
            color.RGB = 0;
            stage = "Marker";
            if (created) shape.AlternativeText = "ChinhTaViet.ShapeLine." + fix.Role;
            stage = "PositionMode";
            shape.RelativeHorizontalPosition = 1;
            shape.RelativeVerticalPosition = 1;
            shape.LayoutInCell = 0;
            shape.LockAnchor = -1;
            stage = "Wrap";
            wrap = shape.WrapFormat;
            wrap.Type = 3;
            stage = "Geometry";
            // Word can reject Rotation=0 and Height=0 on msoLine (E_FAIL).
            // Preserve the horizontal geometry created by AddLine; change position/width only.
            shape.Left = (float)fix.Left;
            shape.Top = (float)fix.Top;
            shape.Width = (float)fix.Width;

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
        finally
        {
            // Retain all child RCWs through configuration and any failed-line cleanup.
            if (color is not null) release(color);
            if (line is not null) release(line);
            if (wrap is not null) release(wrap);
            if (shape is not null) release(shape);
        }
    }
}
