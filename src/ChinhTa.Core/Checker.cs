using System.Text.RegularExpressions;

namespace ChinhTa.Core;

public record Paragraph(int Start, string Text, bool Bold = false, float Size = 13,
    int Alignment = 1, string Font = "Times New Roman", bool Italic = false,
    int Color = 0, bool InTable = false, int Page = 1);
public enum FixKind { Text, NationalHeading, StateMotto, PartyHeading, Advisory, PageLayout, ShapeLine }
public record HeadingFormat(float Size, int? Alignment = null);
public record Issue(int Start, int Length, string Original, string Replacement,
    string Message, bool BatchSafe, FixKind Kind = FixKind.Text,
    bool CanFix = true, string? Citation = null, HeadingFormat? Format = null, string? Title = null, ShapeLineFix? LineFix = null)
{
    public string DisplayText => Title ?? Original;
    public string Suggestion => !CanFix ? "Kiểm tra thủ công; không tự sửa."
        : LineFix is not null ? "Tạo/đặt lại Shape Line theo số đo và cấu hình đã chọn."
        : Format is null ? Replacement
        : Original == Replacement ? "Sửa định dạng theo mô tả." : Replacement + " — sửa định dạng theo mô tả.";
}

public static class Checker
{
    public const string Heading = "CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM";
    public static IReadOnlyList<Issue> Check(IEnumerable<Paragraph> paragraphs, DocumentProfile profile = DocumentProfile.State)
    {
        var result = new List<Issue>();
        var items = paragraphs.ToArray();
        foreach (var p in items)
        {
            if (!(p.Page == 1 && HeadingRules.IsCandidate(p.Text, profile)))
            foreach (Match m in Regex.Matches(p.Text, @"(?<=\S) {2,}(?=\S)"))
                if(!SpacingRules.IsBlankField(p.Text,m.Index,m.Length)) result.Add(new(p.Start + m.Index, m.Length, m.Value, " ",
                    "Khoảng trắng liên tiếp; kiểm tra trước nếu dùng để dàn trang.", false));
            foreach (Match m in Regex.Matches(p.Text, @"\bcon\s+(đo)\b", RegexOptions.IgnoreCase))
            {
                var word = m.Groups[1];
                result.Add(new(p.Start + word.Index, word.Length, word.Value,
                    char.IsUpper(word.Value[0]) ? "Đò" : "đò",
                    "Có thể là ‘con đò’. Đây là quy tắc minh họa, cần người dùng xác nhận.", false));
            }
            if (profile == DocumentProfile.Personal)
            {
                foreach (Match tabs in Regex.Matches(p.Text, @"\t{2,}"))
                    result.Add(new(p.Start + tabs.Index, tabs.Length, tabs.Value, tabs.Value,
                        "Nhiều tab liên tiếp; có thể dùng để dàn bảng. Kiểm tra thủ công, không tự xóa tab.",
                        false, FixKind.Advisory, CanFix: false, Title: "Tab liên tiếp"));
                if (p.Alignment == 1 && Regex.IsMatch(p.Text, @"^[ \t]+\S"))
                    result.Add(new(p.Start, p.Text.TrimEnd('\r', '\a').Length,
                        p.Text.TrimEnd('\r', '\a'), p.Text.TrimEnd('\r', '\a'),
                        "Đoạn căn giữa có tab/khoảng trắng đầu dòng; kiểm tra xem có làm lệch bố cục không.",
                        false, FixKind.Advisory, CanFix: false, Title: "Kiểm tra đoạn căn giữa"));
            }
            result.AddRange(HeadingRules.Check(p, profile));
        }
        result.AddRange(HeadingRules.CheckConsistency(items, profile));
        return result.OrderBy(x => x.Start).ToArray();
    }
}

