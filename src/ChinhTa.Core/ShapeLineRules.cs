namespace ChinhTa.Core;

public enum LineRole { Agency, Motto, Subject }
public record LayoutParagraph(int Start, string Text, int Page, bool InTable, int CellStart,
    int CellEnd, bool Centered, int TableStart = -1);
public record LineTarget(LineRole Role, int Start, string Text, int RegionStart, int RegionEnd, bool Centered)
{
    public string Label => Role switch { LineRole.Agency => "cơ quan ban hành", LineRole.Motto => "tiêu ngữ", _ => "trích yếu" };
    public Issue Probe => new(Start, Text.Length, Text, Text, "Đang kiểm tra Shape Line.", false,
        FixKind.ShapeLine, CanFix: false, Title: "Đường kẻ dưới " + Label);
}
public record LinePolicy(double MinFraction, double MaxFraction, double TargetFraction)
{
    public static LinePolicy Decree30 { get; } = new(1d / 3, .5, 5d / 12);
}
public record LineGeometry(double Left, double Y, double Width);
public record LineReference(double Left, double Bottom, double Width);
public record ShapeLineFix(int? ShapeId, double Left, double Top, double Width, LineRole Role);

public static class ShapeLineRules
{
    private static readonly HashSet<string> Types = new(StringComparer.OrdinalIgnoreCase)
    { "BÁO CÁO", "THÔNG BÁO", "KẾ HOẠCH", "TỜ TRÌNH", "QUYẾT ĐỊNH", "NGHỊ QUYẾT", "HƯỚNG DẪN", "CHỈ THỊ", "CHƯƠNG TRÌNH", "PHƯƠNG ÁN", "ĐỀ ÁN", "QUY CHẾ", "QUY ĐỊNH" };
    private static string Clean(string text) => text.TrimEnd('\r', '\a', '\n');
    public static IReadOnlyList<LineTarget> Identify(IReadOnlyList<LayoutParagraph> paragraphs, DocumentProfile profile)
    {
        if (profile != DocumentProfile.State) return [];
        var first = paragraphs.Where(p => p.Page == 1).ToArray();
        var targets = new List<LineTarget>();
        var mottos = first.Where(p => HeadingRules.IsCandidate(p.Text, DocumentProfile.State) &&
            p.Text.Trim().StartsWith("Độc lập", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (mottos.Length == 1)
        {
            var motto = mottos[0];
            targets.Add(new(LineRole.Motto, motto.Start, Clean(motto.Text), motto.InTable ? motto.CellStart : motto.Start,
                motto.InTable ? motto.CellEnd : motto.Start + motto.Text.Length + 1, motto.Centered));
            if (motto.InTable)
            {
                var left = first.Where(p => p.InTable && p.TableStart == motto.TableStart && p.CellEnd <= motto.CellStart + 1 && p.CellStart < motto.CellStart).ToArray();
                if (left.Length > 0)
                {
                    int nearestCell = left.Max(p => p.CellStart);
                    var agency = left.Where(p => p.CellStart == nearestCell && Clean(p.Text).Trim().Any(char.IsLetter) &&
                        Clean(p.Text).Trim() == Clean(p.Text).Trim().ToUpperInvariant() &&
                        !HeadingRules.IsCandidate(p.Text, DocumentProfile.State)).OrderBy(p => p.Start).LastOrDefault();
                    if (agency is not null) targets.Add(new(LineRole.Agency, agency.Start, Clean(agency.Text), agency.CellStart, agency.CellEnd, agency.Centered));
                }
            }
        }
        var types = first.Select((p,i) => (p,i)).Where(x => !x.p.InTable && Types.Contains(Clean(x.p.Text).Trim())).ToArray();
        if (types.Length == 1)
        {
            var (type, index) = types[0];
            LayoutParagraph? last = null;
            int end = type.Start + type.Text.Length;
            foreach (var p in first.Skip(index + 1))
            {
                if (string.IsNullOrWhiteSpace(Clean(p.Text)))
                {
                    if (last is not null) { end = p.Start + p.Text.Length; continue; }
                    break;
                }
                if (p.InTable || !p.Centered || last is not null && end > last.Start + last.Text.Length) break;
                last = p; end = p.Start + p.Text.Length;
            }
            if (last is not null) targets.Add(new(LineRole.Subject, last.Start, Clean(last.Text), type.Start, end + 1, last.Centered));
        }
        return targets.OrderBy(t => t.Start).ToArray();
    }
    public static string Citation(LineRole role) => role switch
    {
        LineRole.Agency => "NĐ30, PL I, Phần I.II.2.b (PDF 22).",
        LineRole.Motto => "NĐ30, PL I, Phần I.II.1.b (PDF 21).",
        _ => "NĐ30, PL I, Phần I.II.5.b (PDF 24)."
    };
    public static (ShapeLineFix Fix, string[] Errors) Evaluate(LineTarget target, LineReference text,
        LineGeometry? line, int? shapeId, LinePolicy policy)
    {
        if (!double.IsFinite(text.Left) || !double.IsFinite(text.Bottom) || !double.IsFinite(text.Width) || text.Width <= 0)
            throw new ArgumentException("Không đo được dòng chữ tham chiếu.");
        bool motto = target.Role == LineRole.Motto;
        double fraction = motto ? 1 : policy.TargetFraction;
        double width = text.Width * fraction;
        double gap = 6;
        var fix = new ShapeLineFix(shapeId, text.Left + (text.Width - width) / 2, text.Bottom + gap, width, target.Role);
        if (line is null) return (fix, ["Thiếu Shape Line."]);
        if (!double.IsFinite(line.Left) || !double.IsFinite(line.Y) || !double.IsFinite(line.Width) || line.Width <= 0)
            throw new ArgumentException("Không đo được Shape Line.");
        var errors = new List<string>();
        double min = motto ? .95 : policy.MinFraction, max = motto ? 1.05 : policy.MaxFraction;
        // Text/line lengths have 1pt measurement tolerance; center tolerance 3pt is user policy.
        if (line.Width < text.Width * min - 1 || line.Width > text.Width * max + 1) errors.Add("Độ dài chưa phù hợp.");
        if (Math.Abs(line.Left + line.Width / 2 - (text.Left + text.Width / 2)) > 3) errors.Add("Đường kẻ lệch tâm quá 3 pt.");
        double actualGap = line.Y - text.Bottom;
        if (actualGap < (motto ? 3 : 6) - .5 || actualGap > (motto ? 13 : 15) + .5) errors.Add("Khoảng cách dưới dòng chữ ngoài dung sai đã chọn.");
        return (fix, errors.ToArray());
    }
}
