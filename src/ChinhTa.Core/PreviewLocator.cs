namespace ChinhTa.Core;

public record PreviewAnchor(int Page, string PageText, int Offset, int Length);
public record TextSpan(int Start, int Length);

public sealed class PreviewTextIndex
{
    public string Original { get; }
    internal string Text { get; }
    internal int[] Offsets { get; }
    internal PreviewTextIndex(string original)
    {
        Original=original;
        Offsets=Enumerable.Range(0,original.Length).Where(i=>!char.IsWhiteSpace(original[i]) &&
            !char.IsControl(original[i]) && original[i]!='\u00ad').ToArray();
        Text=new string(Offsets.Select(i=>original[i]).ToArray());
    }
    internal int LowerBound(int offset)
    {
        int left=0,right=Offsets.Length;
        while(left<right){int mid=left+(right-left)/2;if(Offsets[mid]<offset)left=mid+1;else right=mid;}
        return left;
    }
}

// Owned by one immutable XPS view; reference keys avoid hashing the entire story
// for every highlight. Different string instances never accidentally share data.
public sealed class PreviewTextCache
{
    private readonly Dictionary<string,PreviewTextIndex> values=new(ReferenceEqualityComparer.Instance);
    public int Preparations { get; private set; }
    public PreviewTextIndex Get(string text)
    {
        if(!values.TryGetValue(text,out var value))
        {
            if(values.Count>=4)values.Clear();
            value=PreviewLocator.Prepare(text);values.Add(text,value);Preparations++;
        }
        return value;
    }
    public void Clear(){values.Clear();Preparations=0;}
}

/// <summary>Aligns text with source offsets. Ambiguous anchors never pick a first occurrence.</summary>
public static class PreviewLocator
{
    public static PreviewTextIndex Prepare(string text)=>new(text);
    private static int Unique(string text, string pattern)
    {
        if (pattern.Length == 0) return -1;
        int first = text.IndexOf(pattern, StringComparison.Ordinal);
        return first >= 0 && text.IndexOf(pattern, first + 1, StringComparison.Ordinal) < 0 ? first : -1;
    }
    public static TextSpan? Locate(PreviewAnchor anchor, string rendered)
    {
        if(anchor.Offset<0 || anchor.Length<=0 || anchor.Offset>anchor.PageText.Length-anchor.Length)return null;
        return LocateCore(anchor,Prepare(anchor.PageText),Prepare(rendered),false);
    }
    public static TextSpan? Locate(PreviewAnchor anchor,PreviewTextIndex source,PreviewTextIndex rendered)
        =>source.Original==anchor.PageText?LocateCore(anchor,source,rendered,true):null;
    private static TextSpan? LocateCore(PreviewAnchor anchor,PreviewTextIndex source,PreviewTextIndex target,bool indexed)
    {
        if (anchor.Offset < 0 || anchor.Length <= 0 || anchor.Offset > anchor.PageText.Length - anchor.Length) return null;
        int begin=indexed?source.LowerBound(anchor.Offset):Array.FindIndex(source.Offsets,x=>x>=anchor.Offset);
        if(begin<0 || begin>=source.Offsets.Length)return null;
        int finish=indexed?source.LowerBound(anchor.Offset+anchor.Length):begin;
        if(!indexed)while(finish<source.Offsets.Length && source.Offsets[finish]<anchor.Offset+anchor.Length)finish++;
        int sourceBase = 0;
        int match = Unique(target.Text, source.Text);
        if (match < 0)
        {
            // Source may cover the whole story. Try smaller unique contexts at page boundaries.
            // Uniqueness in both texts prevents a shorter anchor matching a different repeated phrase.
            foreach (int context in new[] { 48, 24, 12, 6, 0 })
            {
                sourceBase = Math.Max(0, begin - context);
                int contextEnd = Math.Min(source.Text.Length, finish + context);
                string pattern = source.Text[sourceBase..contextEnd];
                if (Unique(source.Text, pattern) != sourceBase) continue;
                match = Unique(target.Text, pattern);
                if (match >= 0) break;
            }
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

