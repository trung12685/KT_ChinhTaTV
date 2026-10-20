namespace ChinhTa.Core;

public record PreviewAnchor(int Page, string PageText, int Offset, int Length);
public record TextSpan(int Start, int Length);

/// <summary>Aligns text with source offsets. Ambiguous anchors never pick a first occurrence.</summary>
public static class PreviewLocator
{
    private static (string Text, int[] Offsets) Compact(string text)
    {
        var offsets = Enumerable.Range(0, text.Length)
            .Where(i => !char.IsWhiteSpace(text[i]) && !char.IsControl(text[i]) && text[i] != '\u00ad').ToArray();
        return (new string(offsets.Select(i => text[i]).ToArray()), offsets);
    }
    private static int Unique(string text, string pattern)
    {
        if (pattern.Length == 0) return -1;
        int first = text.IndexOf(pattern, StringComparison.Ordinal);
        return first >= 0 && text.IndexOf(pattern, first + 1, StringComparison.Ordinal) < 0 ? first : -1;
    }
    public static TextSpan? Locate(PreviewAnchor anchor, string rendered)
    {
        if (anchor.Offset < 0 || anchor.Length <= 0 || anchor.Offset > anchor.PageText.Length - anchor.Length) return null;
        var source = Compact(anchor.PageText);
        var target = Compact(rendered);
        int begin = Array.FindIndex(source.Offsets, x => x >= anchor.Offset);
        if (begin < 0) return null;
        int finish = begin;
        while (finish < source.Offsets.Length && source.Offsets[finish] < anchor.Offset + anchor.Length) finish++;
        int sourceBase = 0;
        int match = Unique(target.Text, source.Text);
        if (match < 0)
        {
            // Page headers, footers or drawing order can differ: require unique surrounding context.
            sourceBase = Math.Max(0, begin - 48);
            int contextEnd = Math.Min(source.Text.Length, finish + 48);
            match = Unique(target.Text, source.Text[sourceBase..contextEnd]);
        }
        if (match < 0) return null;
        int first = match + begin - sourceBase;
        int last = match + finish - sourceBase;
        if (finish == begin)
        {
            // A whitespace-only issue lives between two mapped characters, not on either word.
            if (first <= 0 || first >= target.Offsets.Length) return null;
            int start = target.Offsets[first - 1] + 1;
            int end = target.Offsets[first];
            return end > start ? new(start, end - start) : null;
        }
        return new(target.Offsets[first], target.Offsets[last - 1] - target.Offsets[first] + 1);
    }
}
