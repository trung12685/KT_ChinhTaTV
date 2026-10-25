namespace ChinhTa.Core;

public sealed record ObservedFont(string Name,float Size,int Bold,int Italic,int Color)
{
    public bool Known(bool requireBold=true)=>!string.IsNullOrWhiteSpace(Name) && float.IsFinite(Size) && Size>0 && Size<=1638 &&
        (!requireBold || Bold is 0 or -1) && Italic is 0 or -1 && Color is not (9999999 or -9999999);
}

// Bound to the exact current visible text. Only the Word reader builds production
// evidence after concatenating every real child range back to the requested text.
public sealed class FormatEvidence
{
    public string Text {get;}
    public IReadOnlyList<ObservedFont> Fonts {get;}
    public FormatEvidence(string text,IEnumerable<ObservedFont> fonts)
    {Text=text;Fonts=Array.AsReadOnly(fonts.ToArray());}
}
public enum FormatState { Correct,Repairable,Unknown,NeedsReview }
public sealed record FormatAssessment(FormatState State,float Size,bool PreserveSize=false,bool RepairInvalidSizes=false);

// Shared role policy: variation is not itself a defect. In particular both black
// encodings and every allowed size may coexist. Unknown data never grants a write.
public sealed record FormatPolicy(float Minimum,float Maximum,bool Italic,bool? Bold,bool PreserveEmphasis=false)
{
    public bool SizeMatches(float size)=>float.IsFinite(size) && size>=Minimum && size<=Maximum;
    public bool Matches(ObservedFont font)=>font.Known(PreserveEmphasis || Bold is not null) &&
        font.Name.Equals("Times New Roman",StringComparison.OrdinalIgnoreCase) && SizeMatches(font.Size) &&
        font.Color is 0 or -16777216 && (PreserveEmphasis || font.Italic==(Italic?-1:0) && (Bold is null || font.Bold==(Bold.Value?-1:0)));
    public FormatAssessment Assess(Paragraph row,bool allowSelectiveSizeRepair=false)
    {
        if(!row.FormatObserved || !(row.TextEditable || row.FormatEditable))return new(FormatState.Unknown,Minimum);
        var evidence=row.FormatEvidence;
        if(evidence is not null && evidence.Text!=row.Text.TrimEnd('\r','\a','\n'))return new(FormatState.Unknown,Minimum);
        IReadOnlyList<ObservedFont> fonts=evidence?.Fonts??[new(row.Font,row.Size,row.Bold?-1:0,row.Italic?-1:0,row.Color)];
        if(fonts.Count==0 || fonts.Any(f=>!f.Known(PreserveEmphasis || Bold is not null)) ||
            evidence is null && (row.FormatMixed || (PreserveEmphasis || Bold is not null) && row.BoldMixed))return new(FormatState.Unknown,Minimum);
        if(fonts.All(Matches))return new(FormatState.Correct,fonts[0].Size,fonts.Select(f=>f.Size).Distinct().Count()>1);
        var sizes=fonts.Select(f=>SizeMatches(f.Size)?f.Size:Minimum).Distinct().ToArray();
        if(sizes.Length==1)return new(FormatState.Repairable,sizes[0]);
        if(fonts.All(f=>SizeMatches(f.Size)))return new(FormatState.Repairable,Minimum,PreserveSize:true);
        // A whole-range size write would destroy a valid size elsewhere. Only the
        // character-format route opts into a verified per-character size write.
        return allowSelectiveSizeRepair?new(FormatState.Repairable,Minimum,PreserveSize:true,RepairInvalidSizes:true):
            new(FormatState.NeedsReview,Minimum);
    }
}
