using ChinhTa.Core;

public static class FontSizeEvidenceChecks
{
    public static void Run(Action<bool,string> assert)
    {
        const string source="đơn  vị 3 kế hoạch kế hoạch";
        var sizes=new FontSizeEvidence(source,source.Select((c,i)=>char.IsWhiteSpace(c)?(float?)null:i%2==0?12:13));
        var edits=new[]{new Issue(0,3,"đơn","ĐƠN","",false,FixKind.AgencyText),new Issue(3,2,"  "," ","",false),
            new Issue(8,1,"3","03","",false,FixKind.AdministrativeNumber),
            new Issue(source.IndexOf(" kế hoạch",source.IndexOf("kế hoạch",StringComparison.Ordinal)+1,StringComparison.Ordinal),9," kế hoạch","","",false,FixKind.RepeatedText)};
        var plan=BatchPlan.Create(edits);var projected=sizes.Project(plan,i=>source[..i.Start]);
        assert(projected.Text==AtomicTextBatch.ExpectedStory(plan,source,i=>source[..i.Start]) && projected.Text=="ĐƠN vị 03 kế hoạch",
            "Retained-size projection composes uppercasing, spacing, zero-prefix insertion and repetition deletion in descending source order");
        assert(projected.Sizes.Take(3).SequenceEqual(sizes.Sizes.Take(3)) && projected.Sizes[7] is null && projected.Sizes[8]==sizes.Sizes[8],
            "Original glyph sizes survive case conversion and number movement while a newly inserted digit has no invented source size");
        var changed=projected.Sizes.ToArray();int index=Array.FindIndex(changed,s=>s is float);changed[index]=changed[index]==12?13:12;
        assert(!projected.Matches(new(projected.Text,changed)),"A collapsed or changed retained size fails even when both sizes remain legally allowed");
        var newGlyph=projected.Sizes.ToArray();newGlyph[7]=13;
        assert(projected.Matches(new(projected.Text,newGlyph)),"The preservation proof constrains retained glyphs while normal role checks validate a newly inserted glyph");
        assert(!projected.Matches(new(projected.Text+"x",newGlyph.Append(13))) &&
            !projected.Matches(new(projected.Text,Enumerable.Repeat<float?>(null,projected.Text.Length))),
            "Changed text and missing actual sizes cannot satisfy a retained-size fingerprint");
        foreach(var invalid in new[]{new float?[]{float.NaN},new float?[]{0},new float?[]{9999999},new float?[]{13,14}})
        {
            bool rejected=false;try{_ = new FontSizeEvidence("x",invalid);}catch(InvalidOperationException){rejected=true;}
            assert(rejected,"Invalid or non-covering size observations are rejected before editing");
        }
        bool stale=false;try{sizes.Project([edits[0] with {Original="khác",Length=4}],i=>source[..i.Start]);}catch(InvalidOperationException){stale=true;}
        assert(stale,"Size projection cannot accept an original substring that differs from the bound source");
    }
}
