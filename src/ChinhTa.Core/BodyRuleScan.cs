namespace ChinhTa.Core;

// One call-scoped snapshot, one role analysis, then independent rule policies.
// Never reuse observation candidates as write permission, or retain this scope
// across a Word edit/save/repagination. Fresh scans rebuild all evidence.
public static class BodyRuleScan
{
    public static IReadOnlyList<Issue> Check(IReadOnlyList<Paragraph> paragraphs,IReadOnlyList<LayoutParagraph> layout,DocumentProfile profile,
        Action<string?>? coverage=null)
    {
        if(profile!=DocumentProfile.State)return [];
        var source=paragraphs.ToArray();
        var targets=BodyFormatRules.RepairTargets(source,layout,profile);
        if(targets.Count==0)return [];
        coverage?.Invoke(BodySpacingCoverage.Describe(targets));
        var result=new List<Issue>();
        result.AddRange(BodyFormatRules.CheckTargets(targets));
        result.AddRange(BodyParagraphRules.CheckTargets(targets));
        result.AddRange(BodySpacingRules.CheckTargets(source,targets));
        return result;
    }
}
