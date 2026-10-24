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
    private FontSizeEvidence? ExpectedSizesFor(Issue issue,IReadOnlyList<Issue> edits)
    {
        if(issue.CharacterFormat?.PreserveSize!=true && issue.Format?.MaximumSize is null)return null;
        dynamic range=document!.Range(issue.Start,issue.Start+issue.Length);
        try
        {
            var source=VisibleFormatReader.ReadSizes((object)range,issue.Original);
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
