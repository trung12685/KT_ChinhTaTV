namespace ChinhTa.Core;

public sealed record AdvanceRange(double Start,double End);

// Geometric advance boundaries. Caret stops describe cursor navigation, not text extents.
public static class GlyphAdvanceRange
{
    public static AdvanceRange? Read(IReadOnlyList<double> advances,IReadOnlyList<ushort>? clusterMap,
        int characterCount,int start,int end)
    {
        if(characterCount<=0 || start<0 || end<=start || end>characterCount || advances.Count==0 ||
            advances.Any(a=>!double.IsFinite(a)||a<0))return null;
        if(clusterMap is null && characterCount!=advances.Count || clusterMap is not null && clusterMap.Count!=characterCount)return null;
        int[] map=clusterMap is null?Enumerable.Range(0,characterCount).ToArray():clusterMap.Select(i=>(int)i).ToArray();
        if(map[0]!=0 || map.Any(i=>i>=advances.Count) || map.Skip(1).Where((g,i)=>g<map[i]).Any())return null;
        // A selected boundary inside a shaping cluster has no independent geometric edge.
        if(start>0 && map[start]==map[start-1] || end<characterCount && map[end]==map[end-1])return null;
        int first=map[start],last=end==characterCount?advances.Count:map[end];
        double x1=advances.Take(first).Sum(),x2=advances.Take(last).Sum();
        return double.IsFinite(x1)&&double.IsFinite(x2)?new(x1,x2):null;
    }
}
