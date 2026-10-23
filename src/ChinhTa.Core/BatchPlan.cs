namespace ChinhTa.Core;

public static class BatchPlan
{
    public static IReadOnlyList<Issue> Create(IEnumerable<Issue> issues)
    {
        var ordered = issues.Distinct().OrderBy(x => x.Start).ToArray();
        var active=new List<Issue>();
        for (int i = 0; i < ordered.Length; i++)
        {
            var current = ordered[i];
            if (!current.CanFix)
                throw new InvalidOperationException("Có cảnh báo chỉ kiểm tra thủ công, không thể sửa tự động.");
            if (current.Start < 0 || current.Length <= 0 || current.Original.Length != current.Length)
                throw new InvalidOperationException("Vị trí lỗi không hợp lệ. Hãy quét lại.");
            if(current.CharacterFormat is CharacterFormat style && (!style.Valid ||
                !(current.Kind==FixKind.AdministrativeFormat && style.Bold is null ||
                  current.Kind==FixKind.DocumentHeadingFormat && style.Bold==true && !style.Italic) ||
                current.Original!=current.Replacement || current.Format is not null || current.LineFix is not null ||
                current.Original.IndexOfAny(['\r','\n','\a','\t'])>=0))
                throw new InvalidOperationException("Gợi ý định dạng ký tự không hợp lệ. Hãy quét lại.");
            if(current.CenterFix is not null && (current.Kind!=FixKind.AdministrativePosition ||
                !Enum.IsDefined(current.CenterFix.Role) || current.Original!=current.Replacement ||
                current.Original!=current.Original.Trim() || current.Original.IndexOfAny(['\r','\n','\a','\t'])>=0 ||
                current.CharacterFormat is not null || current.Format is not null || current.LineFix is not null) ||
                current.Kind==FixKind.AdministrativePosition && current.CenterFix is null)
                throw new InvalidOperationException("Gợi ý căn giữa không hợp lệ. Hãy quét lại.");
            if(current.Kind==FixKind.DocumentHeadingFormat && current.CharacterFormat is null)
                throw new InvalidOperationException("Gợi ý tên loại/trích yếu thiếu định dạng đã xác minh. Hãy quét lại.");
            active.RemoveAll(previous=>(long)previous.Start+previous.Length<=current.Start);
            foreach(var previous in active)
                if(!SharesFormat(previous,current) && !SharesFormat(current,previous))
                    throw new InvalidOperationException("Các lỗi chồng lấn vị trí. Hãy sửa riêng từng lỗi rồi quét lại.");
            active.Add(current);
        }
        // Character-only formatting does not change CPs. Apply it before descending text edits.
        return ordered.OrderByDescending(i=>i.CharacterFormat is not null).ThenByDescending(i=>i.Start).ToArray();
    }
    private static bool SharesFormat(Issue format,Issue text)=>format.CharacterFormat is not null &&
        text.CharacterFormat is null && text.Format is null && text.LineFix is null &&
        text.Kind is FixKind.Text or FixKind.AdministrativeNumber or FixKind.AdministrativeDate or FixKind.RepeatedText or FixKind.PunctuationSpace &&
        text.Start>=format.Start && (long)text.Start+text.Length<=(long)format.Start+format.Length;
    public static void Validate(IReadOnlyList<Issue> plan, Func<Issue, string> read)
    {
        foreach (var issue in plan)
            if (read(issue) != issue.Original)
                throw new InvalidOperationException("Nội dung đã thay đổi; chưa áp dụng lượt sửa này. Hãy quét lại.");
    }
}

