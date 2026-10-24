namespace ChinhTa.Core;

// Re-scan before the first write: identical visible text alone does not prove
// that a range is still editable or that its proposed style is still current.
public static class CurrentTextPlan
{
    public static void Validate(IReadOnlyList<Issue> plan,IEnumerable<Issue> current)
    {
        var available=current.Where(i=>i.CanFix && i.LineFix is null && i.CenterFix is null).ToArray();
        foreach(var requested in plan)
            if(!requested.CanFix || requested.LineFix is not null || requested.CenterFix is not null ||
                available.Count(i=>i.Start==requested.Start && i.Length==requested.Length &&
                    i.Original==requested.Original && i.Replacement==requested.Replacement &&
                    i.Kind==requested.Kind && i.OutlineContext==requested.OutlineContext && i.Format==requested.Format && i.CharacterFormat==requested.CharacterFormat)!=1)
                throw new InvalidOperationException("Gợi ý chữ/định dạng đã thay đổi hoặc vùng này chưa cho phép sửa; chưa áp dụng lượt sửa. Hãy quét lại.");
    }
}
