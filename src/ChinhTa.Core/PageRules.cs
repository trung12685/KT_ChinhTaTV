namespace ChinhTa.Core;

public record SectionLayout(int Number, int Start, string Anchor, double WidthMm, double HeightMm,
    double TopMm, double BottomMm, double LeftMm, double RightMm, bool MirrorMargins = false);

public static class PageRules
{
    private static bool In(double value, double min, double max) => value >= min - .25 && value <= max + .25;
    public static IEnumerable<Issue> Check(SectionLayout s, DocumentProfile profile)
    {
        if (profile == DocumentProfile.Personal || string.IsNullOrEmpty(s.Anchor)) yield break;
        bool party = profile == DocumentProfile.Party;
        string citation = party ? "05-HD/VPTW, Phần II.IV.3–4; PDF trang 28."
            : "NĐ 30/2020/NĐ-CP, Phụ lục I, Phần I.I.1–3; PDF trang 21.";
        Issue Warning(string title, string message) => new(s.Start, s.Anchor.Length, s.Anchor, s.Anchor,
            message, false, FixKind.PageLayout, CanFix: false, Citation: citation,
            Title: $"Thiết lập trang {s.Number} — {title}");
        bool portrait = s.WidthMm <= s.HeightMm;
        bool a4 = In(Math.Min(s.WidthMm, s.HeightMm), 210, 210) && In(Math.Max(s.WidthMm, s.HeightMm), 297, 297);
        if (!a4)
            yield return Warning("Kiểm tra khổ giấy", $"Đang {s.WidthMm:F1} × {s.HeightMm:F1} mm; chuẩn thông thường A4 210 × 297 mm. " +
                (party ? "Giấy tờ hành chính có thể dùng A5 hoặc mẫu in sẵn; cần xác định loại trước khi đổi." : "Kiểm tra loại tài liệu trước khi đổi khổ giấy."));
        if (!portrait)
        {
            yield return Warning("Trang ngang — kiểm tra ngoại lệ", "Có thể dùng cho bảng/biểu theo quy định; chưa tự xác định nội dung này có thuộc ngoại lệ hay không. Không tự đổi hướng trang/lề.");
            yield break;
        }
        if (!party && s.MirrorMargins)
        {
            yield return Warning("Lề đối xứng", "Section đang dùng lề đối xứng; cần kiểm tra vùng in hai mặt theo mẫu của đơn vị. Chưa suy ra lề trái/phải vật lý của từng trang.");
            yield break;
        }
        double topMax = party ? 20 : 25, bottomMax = party ? 20 : 25;
        double leftMax = party ? 30 : 35, rightMax = party ? 15 : 20;
        if (!In(s.TopMm,20,topMax) || !In(s.BottomMm,20,bottomMax) || !In(s.LeftMm,30,leftMax) || !In(s.RightMm,15,rightMax))
            yield return Warning("Kiểm tra lề", $"Đang trên/dưới/trái/phải: {s.TopMm:F1}/{s.BottomMm:F1}/{s.LeftMm:F1}/{s.RightMm:F1} mm. " +
                (party ? "Chuẩn trang dọc: 20/20/30/15 mm; mặt sau in hai mặt đổi trái/phải thành 15/30 mm. " +
                         (s.MirrorMargins ? "Đang đối xứng: kiểm tra lề trong 30, ngoài 15 mm." : "Nếu đây là section riêng cho mặt sau, cần kiểm tra thủ công ngoại lệ.")
                : "Chuẩn trang dọc: trên/dưới 20–25, trái 30–35, phải 15–20 mm.") + " Chưa tự sửa thiết lập trang.");
    }
}
