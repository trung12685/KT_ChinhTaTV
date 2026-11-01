using System.Text;
using ChinhTa.Core;
using ChinhTa.WordProtocol;

public static class EvaluationScoring
{
    public static bool Overlap(FindingDto f,Edit e)=>f.Offset<(long)e.Start+e.Length && e.Start<(long)f.Offset+f.Length;
    public static IEnumerable<string> Answers(Edit e)=>new[]{e.Target}.Concat(e.AlternativeTargets??[]).Distinct(StringComparer.Ordinal);
    public static void Validate(Case row)
    {
        if(row.Text is null || row.Edits is null || row.Terms is null)throw new InvalidOperationException("Invalid evaluation row");
        foreach(var e in row.Edits)
        {
            if(!Bound(row.Text,e.Start,e.Length,e.Original) || e.Target is null || Answers(e).Count()>8 || Answers(e).Any(s=>s is null))throw new InvalidOperationException("Invalid annotation: "+row.Id);
        }
        if(Overlapping(row.Edits.Select(e=>(e.Start,e.Length))))throw new InvalidOperationException("Overlapping targets: "+row.Id);
        if(Overlapping((row.CorrectSpans??[]).Select(s=>(s.Start,s.Length))))throw new InvalidOperationException("Overlapping source-correct spans: "+row.Id);
        foreach(var span in row.CorrectSpans??[])
            if(!Bound(row.Text,span.Start,span.Length,span.Original) || row.Edits.Any(e=>span.Start<(long)e.Start+e.Length && e.Start<(long)span.Start+span.Length))throw new InvalidOperationException("Invalid source-correct span: "+row.Id);
    }
    private static bool Bound(string text,int start,int length,string original)=>start>=0 && length>0 && start<=text.Length-length && text.Substring(start,length)==original;
    private static bool Overlapping(IEnumerable<(int Start,int Length)> spans)
    {
        long end=-1;foreach(var span in spans.OrderBy(s=>s.Start)){if(span.Start<end)return true;end=(long)span.Start+span.Length;}return false;
    }
    public static bool Correct(FindingDto finding,Edit target,Case row,IEnumerable<string> suggestions)
    {
        if(target.Start<finding.Offset || target.Start+target.Length>finding.Offset+finding.Length)return false;
        // Never award a partial phrase that crosses another annotated error.
        if(row.Edits.Any(e=>Overlap(finding,e) && (e.Start<finding.Offset || e.Start+e.Length>finding.Offset+finding.Length)))return false;
        var wanted=new HashSet<string>(StringComparer.Ordinal){row.Text.Substring(finding.Offset,finding.Length)};
        foreach(var e in row.Edits.Where(e=>e.Start>=finding.Offset && e.Start+e.Length<=finding.Offset+finding.Length).OrderByDescending(e=>e.Start))
        {
            var replacements=Answers(e).ToArray();
            if((long)wanted.Count*replacements.Length>256)throw new InvalidOperationException("Too many combined answers: "+row.Id);
            wanted=wanted.SelectMany(s=>replacements.Select(answer=>s.Remove(e.Start-finding.Offset,e.Length).Insert(e.Start-finding.Offset,answer))).ToHashSet(StringComparer.Ordinal);
        }
        var normalized=wanted.Select(s=>s.Normalize(NormalizationForm.FormC)).ToHashSet(StringComparer.Ordinal);
        return suggestions.Any(s=>normalized.Contains(s.Normalize(NormalizationForm.FormC)));
    }
    public static Result Measure(Case row,IReadOnlyList<FindingDto> red)
    {
        var targets=row.Edits.Select(e=>new TargetResult(e.Group??row.Group,e.OriginalInLexicon,
            red.Any(f=>Overlap(f,e)),red.Any(f=>Overlap(f,e) && Correct(f,e,row,f.Suggestions.Take(1))),
            red.Any(f=>Overlap(f,e) && Correct(f,e,row,f.Suggestions.Take(3))))).ToArray();
        var extra=red.Where(f=>!row.Edits.Any(e=>Overlap(f,e))).ToArray();
        int onCorrect=extra.Count(f=>(row.CorrectSpans??[]).Any(s=>f.Offset<(long)s.Start+s.Length && s.Start<(long)f.Offset+f.Length));
        int correctTokens=(row.CorrectSpans??[]).Sum(s=>VietnameseSpellingModel.Tokenize(s.Original).Count);
        return new(targets,red.Count-extra.Length,extra.Length,onCorrect,correctTokens,extra.Length-onCorrect);
    }
    public sealed record TargetResult(string Group,bool? OriginalInLexicon,bool Hit,bool Top1,bool Top3);
    public sealed record Result(TargetResult[] Targets,int MatchedWarnings,int ExtraWarnings,int WarningsOnCorrectSpans,int CorrectTokens,int UnlabeledWarnings);
}
