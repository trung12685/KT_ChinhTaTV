using ChinhTa.Core;

namespace ChinhTa.Desktop;
public sealed partial class WordSession
{
    internal FontSizeEvidence ReadCheckSizes(Issue probe)
    {
        dynamic range=document!.Range(probe.Start,probe.Start+probe.Length);
        try{return VisibleFormatReader.ReadSizes((object)range,probe.Original);}
        finally{Release(range);}
    }
    internal FontSizeEvidence ReadCheckCurrentSizes(Issue original)
    {
        if(LineCheckArtifactFolder is null)throw new InvalidOperationException("Chỉ dùng trong phiên kiểm Word riêng.");
        var rows=lastFormatLayout.Where(p=>p.Text.TrimEnd('\r','\a','\n')==original.Original).ToArray();
        if(rows.Length!=1)throw new InvalidOperationException("Mẫu kiểm chưa có một vùng phông hiện tại duy nhất.");
        return ReadCheckSizes(original with {Start=rows[0].Start});
    }
    internal CharacterStyleEvidence ReadCheckCurrentStyles(string text)
    {
        if(LineCheckArtifactFolder is null)throw new InvalidOperationException("Chỉ dùng trong phiên kiểm Word riêng.");
        var rows=lastFormatLayout.Where(p=>p.Text.TrimEnd('\r','\a','\n')==text).ToArray();
        if(rows.Length!=1)throw new InvalidOperationException("Mẫu kiểm chưa có một đoạn hiện tại duy nhất để đối chiếu viết hoa.");
        dynamic range=document!.Range(rows[0].Start,rows[0].Start+text.Length);
        try{return VisibleFormatReader.ReadStyles((object)range,text);}finally{Release(range);}
    }
    internal IReadOnlyList<Issue> CaseTrackingRegions(IReadOnlyList<Issue> plan)=>CasePreservationPlan.Regions(plan,lastFormatLayout);
    private CharacterStyleEvidence ExpectedCaseStyles(Issue probe,IReadOnlyList<Issue> plan)
    {
        dynamic range=document!.Range(probe.Start,probe.Start+probe.Length);
        try
        {
            var source=VisibleFormatReader.ReadStyles((object)range,probe.Original);
            return CasePreservationPlan.Expected(probe,plan,source,item=>
            {
                dynamic prefix=document!.Range(probe.Start,item.Start);
                try{return (string?)prefix.Text??"";}finally{Release(prefix);}
            });
        }
        finally{Release(range);}
    }
    private FontSizeEvidence? ExpectedSizesFor(Issue issue,IReadOnlyList<Issue> edits)
    {
        if(issue.CharacterFormat?.PreserveSize!=true && issue.Format?.MaximumSize is null)return null;
        dynamic range=document!.Range(issue.Start,issue.Start+issue.Length);
        try
        {
            var source=VisibleFormatReader.ReadSizes((object)range,issue.Original);
            if(issue.CharacterFormat is {RepairInvalidSizes:true} selective)
                source=source.CorrectInvalid(selective.SizeRange.Minimum,selective.SizeRange.Maximum);
            else if(issue.Format is {RepairInvalidSizes:true,MaximumSize:float maximum} heading)
                source=source.CorrectInvalid(heading.Size,maximum);
            var contained=edits.Where(i=>i.CharacterFormat is null && i.Format is null && i.Start>=issue.Start &&
                (long)i.Start+i.Length<=(long)issue.Start+issue.Length).ToArray();
            return source.Project(contained,item=>
            {
                dynamic prefix=document!.Range(issue.Start,item.Start);
                try{return (string?)prefix.Text??"";}finally{Release(prefix);}
            });
        }
        finally{Release(range);}
    }
    private Paragraph ObserveComponent(Paragraph row)
    {
        if(!row.TextEditable && !row.FormatEditable)return row;
        string text=row.Text.TrimEnd('\r','\a','\n');
        var observations=new List<VisibleFont>();int offset=0;
        // A discretionary line break can carry a different run style from its
        // visible words. Observe each nonblank line; never remove the break.
        foreach(string part in text.Split('\v'))
        {
            if(!string.IsNullOrWhiteSpace(part))
            {
                dynamic range=document!.Range(row.Start+offset,row.Start+offset+part.Length);
                try
                {
                    observations.AddRange(VisibleFormatReader.Read((object)range,part,allowCharacterFallback:true));
                }
                finally{Release(range);}
            }
            offset+=part.Length+1;
        }
        if(observations.Count==0)return row;
        var first=observations[0];
        return row with {Font=first.Name,Size=first.Size,Bold=first.Bold==-1,Italic=first.Italic!=0,Color=first.Color,FormatObserved=true,
            FormatMixed=observations.Any(o=>o!=first || !o.Known),
            BoldMixed=observations.Any(o=>o.Bold is not (0 or -1)),
            FormatEvidence=new(text,observations.Select(o=>new ObservedFont(o.Name,o.Size,o.Bold,o.Italic,o.Color)))};
    }
}
