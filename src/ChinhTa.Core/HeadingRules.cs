namespace ChinhTa.Core;

public static class HeadingRules
{
    public const string Motto = "Độc lập - Tự do - Hạnh phúc";
    public const string PartyTitle = "ĐẢNG CỘNG SẢN VIỆT NAM";
    private static string Canonical(string text) => System.Text.RegularExpressions.Regex.Replace(
        System.Text.RegularExpressions.Regex.Replace(text.TrimEnd('\r', '\a', '\n').Trim(), @"\s+", " "),
        @"\s*[-–—]\s*", " - ");
    private static string MottoWithOriginalDashes(string text)
    {
        var separators = System.Text.RegularExpressions.Regex.Matches(text, "[-–—]")
            .Select(match => match.Value is "-" or "–" ? match.Value : "-").ToArray();
        return separators.Length == 2 ? $"Độc lập {separators[0]} Tự do {separators[1]} Hạnh phúc" : Motto;
    }
    private static bool National(string text) => text.Equals(Checker.Heading, StringComparison.OrdinalIgnoreCase)
        || text.Equals(Checker.Heading.Replace("HÒA", "HOÀ"), StringComparison.OrdinalIgnoreCase);
    public static bool IsCandidate(string text, DocumentProfile profile)
    {
        text = Canonical(text);
        return profile == DocumentProfile.State && (National(text)
            || text.Equals(Motto, StringComparison.OrdinalIgnoreCase))
            || profile == DocumentProfile.Party && text.Equals(PartyTitle, StringComparison.OrdinalIgnoreCase);
    }
    public static IEnumerable<Issue> Check(Paragraph p, DocumentProfile profile)
    {
        if (p.Page != 1 || !IsCandidate(p.Text, profile)) yield break;
        string text = p.Text.TrimEnd('\r', '\a', '\n');
        bool party = profile == DocumentProfile.Party;
        bool motto = Canonical(text).Equals(Motto, StringComparison.OrdinalIgnoreCase);
        string expected = party ? PartyTitle : motto ? MottoWithOriginalDashes(text) :
            text.Contains("HOÀ", StringComparison.OrdinalIgnoreCase) ? Checker.Heading.Replace("HÒA", "HOÀ") : Checker.Heading;
        float min = party ? 15 : motto ? 13 : 12;
        float max = party ? 15 : motto ? 14 : 13;
        // Positioning at the right of the page is not equivalent to global paragraph centering.
        // Local centering of a national heading in a cell follows the user-requested layout convention.
        int? align = !party && p.InTable ? 1 : null;
        bool validSize = p.Size >= min - .01f && p.Size <= max + .01f;
        var assessment=p.FormatEvidence is null?null:new FormatPolicy(min,max,false,true).Assess(p);
        bool validFormat=assessment is null?p.Bold && !p.Italic && validSize && p.Font=="Times New Roman" && p.Color is 0 or -16777216:
            assessment.State==FormatState.Correct;
        if (text == expected && validFormat && (align is null || p.Alignment == align)) yield break;
        string citation = party ? "05-HD/VPTW (27/5/2026), Phần II.I.1 và Phụ lục 2, mục 1; PDF trang 2, 31."
            : "NĐ 30/2020/NĐ-CP, Phụ lục I, Phần I.I.4 và II.1; PDF trang 21.";
        if (!party && !motto && align is not null)
            citation += " Căn giữa quốc hiệu trong ô: lựa chọn trình bày theo yêu cầu người dùng.";
        string sizeLabel = min == max ? $"{min:0}" : $"{min:0}–{max:0}";
        string label = party ? "Tiêu đề Đảng" : motto ? "Tiêu ngữ" : "Quốc hiệu";
        if(assessment?.State is FormatState.Unknown or FormatState.NeedsReview ||
            !p.TextEditable && !(p.FormatEditable && text==expected && assessment?.State is FormatState.Correct or FormatState.Repairable) ||
            assessment?.PreserveSize==true && text!=expected)
        {
            yield return new(p.Start,text.Length,text,text,
                $"{label}: chưa đủ dữ liệu hoặc quyền sửa chữ/định dạng an toàn; cần duyệt thủ công, không kết luận dòng sai thể thức.",
                false,FixKind.Advisory,CanFix:false,Citation:citation,Title:$"Kiểm tra {label.ToLowerInvariant()}");
            yield break;
        }
        float targetSize=assessment?.PreserveSize==true?min:assessment?.Size??(validSize?p.Size:min);
        yield return new(p.Start, text.Length, text, expected,
            $"{label}: Times New Roman, chữ đứng, đậm, màu đen, cỡ {sizeLabel}. " +
            (align is not null ? "Căn giữa trong ô. " : "") +
            "Chưa kiểm tra tọa độ vùng đặt; đường kẻ được đánh giá riêng khi nhận diện/đo được.", false,
            party ? FixKind.PartyHeading : motto ? FixKind.StateMotto : FixKind.NationalHeading,
            Citation: citation, Format: new(targetSize, align,assessment?.PreserveSize==true?max:null));
    }
    public static IEnumerable<Issue> CheckConsistency(IReadOnlyList<Paragraph> paragraphs, DocumentProfile profile)
    {
        if (profile != DocumentProfile.State) yield break;
        var nationals = paragraphs.Where(p => p.Page == 1 && National(Canonical(p.Text))).ToArray();
        var mottos = paragraphs.Where(p => p.Page == 1 && Canonical(p.Text).Equals(Motto, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (nationals.Length != 1 || mottos.Length != 1) yield break;
        var national = nationals[0]; var motto = mottos[0];
        if(!national.FormatObserved || !motto.FormatObserved)yield break;
        if(new[]{national,motto}.Any(p=>p.FormatEvidence is { } e && e.Fonts.Select(f=>f.Size).Distinct().Count()!=1))yield break;
        if (national.Size < 12 || national.Size > 13 || motto.Size < 13 || motto.Size > 14 ||
            Math.Abs(motto.Size - national.Size - 1) < .01) yield break;
        string original = motto.Text.TrimEnd('\r', '\a', '\n');
        yield return new(motto.Start, original.Length, original, original,
            $"Quốc hiệu đang cỡ {national.Size}, tiêu ngữ cỡ {motto.Size}. Kiểm tra tính thống nhất tăng/giảm cỡ chữ; " +
            "phụ lục minh họa cặp 12/13 hoặc 13/14. Chưa tự chọn cặp để thay đổi.", false,
            FixKind.Advisory, CanFix: false, Citation: "NĐ 30/2020/NĐ-CP, Phụ lục I, Phần I.V, chú thích 1; PDF trang 32.",
            Title: "Kiểm tra thống nhất cỡ quốc hiệu/tiêu ngữ");
    }

}

