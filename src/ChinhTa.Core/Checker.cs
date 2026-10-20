using System.Text.RegularExpressions;

namespace ChinhTa.Core;

public record Paragraph(int Start, string Text, bool Bold = false, float Size = 13,
    int Alignment = 1, string Font = "Times New Roman");
public enum FixKind { Text, NationalHeading }
public record Issue(int Start, int Length, string Original, string Replacement,
    string Message, bool BatchSafe, FixKind Kind = FixKind.Text);

public static class Checker
{
    public const string Heading = "CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM";
    public static IReadOnlyList<Issue> Check(IEnumerable<Paragraph> paragraphs, DocumentProfile profile = DocumentProfile.State)
    {
        var result = new List<Issue>();
        foreach (var p in paragraphs)
        {
            foreach (Match m in Regex.Matches(p.Text, @"(?<=\S) {2,}(?=\S)"))
                result.Add(new(p.Start + m.Index, m.Length, m.Value, " ",
                    "Khoảng trắng liên tiếp; kiểm tra trước nếu dùng để dàn trang.", false));
            foreach (Match m in Regex.Matches(p.Text, @"\bcon\s+(đo)\b", RegexOptions.IgnoreCase))
            {
                var word = m.Groups[1];
                result.Add(new(p.Start + word.Index, word.Length, word.Value,
                    char.IsUpper(word.Value[0]) ? "Đò" : "đò",
                    "Có thể là ‘con đò’. Đây là quy tắc minh họa, cần người dùng xác nhận.", false));
            }
            var text = p.Text.TrimEnd('\r', '\a', '\n');
            if (profile == DocumentProfile.State && text.Trim().Equals(Heading, StringComparison.OrdinalIgnoreCase) &&
                (text != Heading || !p.Bold || p.Size is < 12.99f or > 13.01f ||
                 p.Alignment != 1 || p.Font != "Times New Roman"))
                result.Add(new(p.Start, text.Length, text, Heading,
                    "Mẫu thử quốc hiệu: chữ hoa, đậm, Times New Roman 13, căn giữa. Chưa phải bộ kiểm tra pháp lý đầy đủ.", false, FixKind.NationalHeading));
        }
        return result.OrderBy(x => x.Start).ToArray();
    }
}
