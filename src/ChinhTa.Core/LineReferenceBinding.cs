namespace ChinhTa.Core;

public static class LineReferenceBinding
{
    // A Word range position is not assumed to be an index into Range.Text.
    // Rebind only to the same uniquely identified role/text in a freshly verified layout.
    public static IReadOnlyList<Issue> Rebind(IReadOnlyList<Issue> selected,IReadOnlyList<LineTarget> current,
        string expectedStory,string currentStory)
    {
        if(currentStory!=expectedStory)
            throw new InvalidOperationException("Nội dung chữ thay đổi sau khi sửa Line; phải khôi phục cả lượt.");
        return selected.Select(item=>
        {
            var role=item.LineFix?.Role ?? throw new InvalidOperationException("Thiếu vai trò Line.");
            var matches=current.Where(t=>t.Role==role).ToArray();
            if(matches.Length!=1 || matches[0].Text!=item.Original || !matches[0].Centered)
                throw new InvalidOperationException("Không đối chiếu được duy nhất mốc Line hiện tại; phải khôi phục cả lượt.");
            var target=matches[0];
            return item with {Start=target.Start,Length=target.Text.Length};
        }).ToArray();
    }
}

public static class WordPreviewAnchor
{
    public static PreviewAnchor Create(int page,string story,string prefix,string original)
    {
        if(page<1 || original.Length==0 || prefix.Length>story.Length-original.Length ||
            !story.StartsWith(prefix,StringComparison.Ordinal) ||
            !story.AsSpan(prefix.Length,original.Length).SequenceEqual(original.AsSpan()))
            throw new InvalidOperationException("Không đối chiếu được vị trí Word với chữ trong bản xem trước.");
        return new(page,story,prefix.Length,original.Length);
    }
}
