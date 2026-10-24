namespace ChinhTa.Core;

// One export only: share expensive Word position reads at the same CP, never
// share them across edits/documents. Every issue still validates its own text.
public static class PreviewAnchorBatch
{
    public static IReadOnlyDictionary<Issue,PreviewAnchor> Read(IReadOnlyList<Issue> issues,string story,
        Func<int,(int Page,string Prefix)> readPosition,Action<Issue,int>? onUnmapped=null,Action<Issue,int>? onMapped=null)
    {
        var positions=new Dictionary<int,(int Page,string Prefix)>();var anchors=new Dictionary<Issue,PreviewAnchor>();
        foreach(var issue in issues)
        {
            if(!positions.TryGetValue(issue.Start,out var position))
            {position=readPosition(issue.Start);positions.Add(issue.Start,position);}
            PreviewAnchor anchor;
            try{anchor=WordPreviewAnchor.Create(position.Page,story,position.Prefix,issue.Original);}
            catch(InvalidOperationException){onUnmapped?.Invoke(issue,position.Prefix.Length);continue;}
            anchors[issue]=anchor;onMapped?.Invoke(issue,position.Prefix.Length);
        }
        return anchors;
    }
}
