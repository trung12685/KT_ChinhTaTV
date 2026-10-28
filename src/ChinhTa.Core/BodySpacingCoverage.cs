namespace ChinhTa.Core;

// Coverage is not a defect or write permission. An unsupported line-spacing
// mode must remain visible even when no safely repairable issue was emitted.
public static class BodySpacingCoverage
{
    public static string? Describe(IReadOnlyList<Paragraph> targets)
    {
        if(targets.Count==0)return null;
        var reasons=new Dictionary<string,int>();int partial=0;
        foreach(var row in targets)
        {
            var found=new HashSet<string>();var context=row.Outline?.SpacingContext;var spacing=row.ParagraphStyle?.Spacing;
            if(context is null || spacing is not {Known:true} || row.Flow is not {Known:true})found.Add("thiếu quan sát chắc chắn");
            if(context?.LineUnits==true)found.Add("khoảng đoạn theo đơn vị dòng");
            if(context?.SpecialLayout==true)found.Add("bố cục đoạn đặc biệt");
            if(context?.Grid==true)found.Add("lưới dòng");
            if(context?.Contextual==true || spacing is {Known:true,BeforeAuto:not 0} or {Known:true,AfterAuto:not 0})found.Add("khoảng tự động/theo kiểu đoạn");
            if(context?.PageBreakBefore==true || context?.KeepNext==true)found.Add("ràng buộc ngắt/giữ trang");
            if(spacing?.Rule is 3 or 4)found.Add("giãn dòng Exactly/AtLeast chưa đo tương đương");
            if(found.Count==0)continue;
            partial++;
            foreach(string reason in found)reasons[reason]=reasons.GetValueOrDefault(reason)+1;
        }
        if(partial==0)return null;
        return $"Phạm vi khoảng đoạn/giãn dòng: {partial}/{targets.Count} đoạn nội dung đã nhận diện còn phần cần kiểm tra thủ công ("+
            string.Join("; ",reasons.Select(r=>$"{r.Key}: {r.Value}"))+"). Đây là giới hạn kiểm tra, không phải kết luận các đoạn này sai.";
    }
}
