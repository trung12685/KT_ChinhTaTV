namespace ChinhTa.Core;

public enum AdministrativePositionRole { Number,Date }
public record RenderedTextLine(double Left,double Top,double Width,double Height,double Baseline)
{
    public double Center=>Left+Width/2;
    public double Bottom=>Top+Height;
    public bool Valid=>double.IsFinite(Left) && double.IsFinite(Top) && double.IsFinite(Width) &&
        double.IsFinite(Height) && double.IsFinite(Baseline) && Width>0 && Height>0 && Baseline>=Top && Baseline<=Bottom;
}
public record PositionTextAnchor(int Start,string Text)
{
    public Issue Probe=>new(Start,Text.Length,Text,Text,"Mốc đo vị trí.",false,FixKind.AdministrativePosition,CanFix:false);
}
public record AdministrativePositionTarget(AdministrativePositionRole Role,PositionTextAnchor Row,IReadOnlyList<PositionTextAnchor> References)
{
    public string Label=>Role==AdministrativePositionRole.Number?"số/ký hiệu":"địa danh/ngày tháng";
    public Issue Probe=>Row.Probe with {Title="Vị trí "+Label};
}
public record PositionAssessment(bool Measured,IReadOnlyList<string> Warnings);

// Read-only review using verified table roles and actual rendered single-line glyphs.
public static class AdministrativePositionRules
{
    public const double Tolerance=3; // Measurement policy, not a numerical requirement stated by NĐ30.
    private static string Clean(string text)=>text.TrimEnd('\r','\a','\n');
    public const string Citation="NĐ30, PL I, Phần I.II.3.c và 4.c (PDF 23). Ngưỡng phát hiện 3 pt là chính sách đo của bản thử; chưa tự sửa vị trí.";
    public static IReadOnlyList<AdministrativePositionTarget> Identify(IReadOnlyList<Paragraph> paragraphs,
        IReadOnlyList<LayoutParagraph> layout,DocumentProfile profile)
    {
        var header=AdministrativeHeaderRules.Identify(paragraphs,profile);
        var result=new List<AdministrativePositionTarget>();
        foreach(var role in Enum.GetValues<AdministrativePositionRole>())
        {
            var rows=header.Where(p=>AdministrativeHeaderRules.IsNumber(p.Text)==(role==AdministrativePositionRole.Number)).ToArray();
            if(rows.Length!=1 || !rows[0].TextEditable || rows[0].Text.Contains('\t'))continue;
            var row=rows[0];
            var matching=layout.Where(p=>p.Start==row.Start && Clean(p.Text)==Clean(row.Text) && p.Page==1).ToArray();
            var references=Array.Empty<PositionTextAnchor>();
            if(matching.Length==1)
            {
                var field=matching[0];
                if(field.InTable && field.CellStart<=field.Start && field.CellEnd>field.Start && field.TableStart>=0)
                {
                    var sameCell=layout.Where(p=>p.Page==1 && p.InTable && p.TableStart==field.TableStart &&
                        p.CellStart==field.CellStart && p.CellEnd==field.CellEnd && p.Start<field.Start).ToArray();
                    if(role==AdministrativePositionRole.Number)
                    {
                        var names=sameCell.Where(p=>!string.IsNullOrWhiteSpace(Clean(p.Text)) && Clean(p.Text).Any(char.IsLetter) &&
                            Clean(p.Text)==Clean(p.Text).ToUpperInvariant() && !HeadingRules.IsCandidate(p.Text,profile) &&
                            !AdministrativeHeaderRules.IsCandidate(p.Text)).ToArray();
                        // Every nonblank preceding row must be an identified agency-name row.
                        if(names.Length>0 && sameCell.Count(p=>!string.IsNullOrWhiteSpace(Clean(p.Text)))==names.Length)
                            references=names.Select(p=>new PositionTextAnchor(p.Start,Clean(p.Text))).ToArray();
                    }
                    else
                    {
                        var headings=sameCell.Where(p=>HeadingRules.IsCandidate(p.Text,profile)).ToArray();
                        if(headings.Length==2 && headings.Count(p=>Clean(p.Text).TrimStart().StartsWith("Độc",StringComparison.OrdinalIgnoreCase))==1)
                            references=headings.Select(p=>new PositionTextAnchor(p.Start,Clean(p.Text))).ToArray();
                    }
                }
            }
            result.Add(new(role,new(row.Start,Clean(row.Text)),references));
        }
        return result;
    }
    public static PositionAssessment Assess(AdministrativePositionTarget target,RenderedTextLine? row,IReadOnlyList<RenderedTextLine?> references)
    {
        if(row is null || !row.Valid || target.References.Count==0 || references.Count!=target.References.Count ||
            references.Any(r=>r is null || !r.Valid))
            return new(false,["Chưa đủ mốc cùng vùng hoặc chưa đo được trọn dòng chữ trên XPS (có thể xuống dòng/không ánh xạ được). Chưa kết luận vị trí đúng hoặc sai."]);
        var refs=references.Select(r=>r!).ToArray();
        if(target.Role==AdministrativePositionRole.Date && refs.Max(r=>r.Center)-refs.Min(r=>r.Center)>Tolerance)
            return new(false,["Quốc hiệu và tiêu ngữ chưa cùng tâm trong dung sai đo; chưa chọn một tâm để kết luận vị trí ngày tháng. Cần duyệt thủ công."]);
        double center=target.Role==AdministrativePositionRole.Date?refs.Average(r=>r.Center):
            (refs.Min(r=>r.Left)+refs.Max(r=>r.Left+r.Width))/2;
        var warnings=new List<string>();
        if(Math.Abs(row.Center-center)>Tolerance)
            warnings.Add($"Dòng {target.Label} lệch tâm khối chữ phía trên {Math.Abs(row.Center-center):F1} pt, vượt ngưỡng đo {Tolerance:0} pt.");
        if(row.Top<refs.Max(r=>r.Bottom)-.5)
            warnings.Add($"Dòng {target.Label} chưa nằm dưới khối chữ tham chiếu trên bản xem trước.");
        return new(true,warnings);
    }
    public static string? SameRow(RenderedTextLine number,RenderedTextLine date)
    {
        if(!number.Valid || !date.Valid)return null;
        double difference=Math.Abs(number.Baseline-date.Baseline);
        return difference>Tolerance?$"Số/ký hiệu và ngày tháng chênh đường cơ sở {difference:F1} pt trên XPS; cần kiểm tra yêu cầu cùng một dòng (ngưỡng đo {Tolerance:0} pt).":null;
    }
}
