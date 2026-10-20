namespace ChinhTa.Core;

public static class BatchPlan
{
    public static IReadOnlyList<Issue> Create(IEnumerable<Issue> issues)
    {
        var ordered = issues.Distinct().OrderBy(x => x.Start).ToArray();
        for (int i = 0; i < ordered.Length; i++)
        {
            var current = ordered[i];
            if (!current.CanFix)
                throw new InvalidOperationException("Có cảnh báo chỉ kiểm tra thủ công, không thể sửa tự động.");
            if (current.Start < 0 || current.Length <= 0 || current.Original.Length != current.Length)
                throw new InvalidOperationException("Vị trí lỗi không hợp lệ. Hãy quét lại.");
            if (i > 0 && (long)ordered[i - 1].Start + ordered[i - 1].Length > current.Start)
                throw new InvalidOperationException("Các lỗi chồng lấn vị trí. Hãy sửa riêng từng lỗi rồi quét lại.");
        }
        return ordered.Reverse().ToArray();
    }
    public static void Validate(IReadOnlyList<Issue> plan, Func<Issue, string> read)
    {
        foreach (var issue in plan)
            if (read(issue) != issue.Original)
                throw new InvalidOperationException("Nội dung đã thay đổi; chưa áp dụng lượt sửa này. Hãy quét lại.");
    }
}
