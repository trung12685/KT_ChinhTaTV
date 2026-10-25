using System.Collections;
using ChinhTa.Core;

public static class BodyRuleScanChecks
{
    private sealed class CountedLayout(IReadOnlyList<LayoutParagraph> rows):IReadOnlyList<LayoutParagraph>
    {
        public int Reads {get;private set;}
        public int Count=>rows.Count;
        public LayoutParagraph this[int index]{get{Reads++;return rows[index];}}
        public IEnumerator<LayoutParagraph> GetEnumerator()
        {foreach(var row in rows){Reads++;yield return row;}}
        IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
    }
    private static Issue[] Separate(IReadOnlyList<Paragraph> paragraphs,IReadOnlyList<LayoutParagraph> layout,DocumentProfile profile)=>
        BodyFormatRules.Check(paragraphs,layout,profile).Concat(BodyParagraphRules.Check(paragraphs,layout,profile))
            .Concat(BodySpacingRules.Check(paragraphs,layout,profile)).ToArray();
    public static void Run(Action<bool,string> assert)
    {
        var sample=BodySpacingChecks.Sample(5);var original=sample.P[10];string text=original.Text.TrimEnd('\r');
        var variants=new List<Paragraph[]> {sample.P};
        foreach(var replacement in new[]{original with {TextEditable=false},original with {FormatObserved=false},original with {FormatEvidence=null},
            original with {FormatEvidence=new("stale",original.FormatEvidence!.Fonts)},original with {Outline=null},
            original with {Outline=original.Outline! with {Scope=OutlineScope.AutomaticList}},original with {InTable=true},
            original with {Outline=original.Outline! with {SpacingContext=new(Grid:true)}},original with {Flow=new(2,2)},
            original with {Text="\r"},original with {Text="1. Nội dung cần được giữ nguyên.\r"},
            original with {ParagraphStyle=original.ParagraphStyle! with {Alignment=0,First=0},Outline=original.Outline! with {Alignment=0}},
            original with {FormatEvidence=new(text,[new("Arial",12,0,0,255),new("Arial",14,-1,-1,255)])},
            original with {FormatEvidence=new(text,[new("Arial",12,-1,0,255)])},
            original with {FormatEvidence=new(text,[new("Arial",12,9999999,0,255)])}})
        {
            var rows=sample.P.ToArray();rows[10]=replacement;variants.Add(rows);
        }
        var duplicate=sample.P.ToArray();duplicate[11]=duplicate[11] with {Start=original.Start};variants.Add(duplicate);
        var noBoundary=sample.P[..^1];variants.Add(noBoundary);
        var noTitle=sample.P.ToArray();noTitle[7]=noTitle[7] with {Text="Khối chưa có tên loại.\r"};variants.Add(noTitle);
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        foreach(var rows in variants)
        {
            var expected=Separate(rows,sample.L,profile);var actual=BodyRuleScan.Check(rows,sample.L,profile);
            assert(expected.SequenceEqual(actual),
                "One body scan retains every issue, order, repair target, source/neighbor fingerprint, citation and required-group contract across safe/unsafe variants");
        }
        var opening=BodyOpeningChecks.Sample();
        foreach(bool known in new[]{true,false})
        {
            var rows=opening.P.ToArray();if(!known)rows[9]=rows[9] with {FormatObserved=false};
            var actual=BodyRuleScan.Check(rows,opening.L,DocumentProfile.State);
            assert(actual.SequenceEqual(Separate(rows,opening.L,DocumentProfile.State)) && (known?actual.Count>0:actual.Count==0),
                "Shared analysis preserves the distinction between provisional observation and proven no-greeting write permission");
        }
        var separateLayout=new CountedLayout(sample.L);var sharedLayout=new CountedLayout(sample.L);
        var separate=Separate(sample.P,separateLayout,DocumentProfile.State);var shared=BodyRuleScan.Check(sample.P,sharedLayout,DocumentProfile.State);
        assert(shared.SequenceEqual(separate) && sharedLayout.Reads>0 && separateLayout.Reads==3*sharedLayout.Reads,
            "The shared scan performs one front-title/layout role analysis instead of three without changing any resulting issue");
        var large=BodySpacingChecks.Sample(500);
        assert(BodyRuleScan.Check(large.P,large.L,DocumentProfile.State).SequenceEqual(Separate(large.P,large.L,DocumentProfile.State)),
            "A 500-paragraph body retains every independent gap and line finding in the same order");
        var live=sample.P.ToList();var first=BodyRuleScan.Check(live,sample.L,DocumentProfile.State).ToArray();
        for(int i=10;i<live.Count-1;i++)
        {
            var row=live[i];live[i]=row with {ParagraphStyle=row.ParagraphStyle! with {Spacing=row.ParagraphStyle.Spacing! with {After=6,Rule=0}},
                FormatEvidence=new(row.Text.TrimEnd('\r'),row.FormatEvidence!.Fonts.Select(f=>f with {Name="Times New Roman",Size=13,Color=0}))};
        }
        var after=BodyRuleScan.Check(live,sample.L,DocumentProfile.State);
        assert(first.Length>0 && after.Count==0 && first.Any(i=>i.SpacingFix?.Before.Spacing?.Rule==2),
            "A fresh call uses repaired evidence, removes corrected rows and leaves the earlier snapshot unchanged");
        bool rejected=false;try{CurrentTextPlan.Validate(first,after);}catch(InvalidOperationException){rejected=true;}
        assert(rejected,"An old selection cannot reuse the previous shared scope after the document changes");
        live=sample.P.ToList();live[^1]=live[^1] with {Text="Kết thúc chưa rõ.\r"};
        assert(BodyRuleScan.Check(live,sample.L,DocumentProfile.State).Count==0,
            "Losing the signature/recipient boundary on a later scan revokes all prior body repair permission");
        var blank=sample.P.ToArray();blank[11]=blank[11] with {Text="\r"};
        var gaps=BodyRuleScan.Check(blank,sample.L,DocumentProfile.State).Where(i=>i.Kind==FixKind.BodyGap).ToArray();
        assert(!gaps.Any(i=>i.Start==blank[10].Start) && gaps.Select(i=>i.Start).SequenceEqual([blank[12].Start,blank[13].Start]),
            "A shared candidate list never replaces original paragraph adjacency or bridges across a blank boundary");
        assert(BodyRuleScan.Check([],[],DocumentProfile.State).Count==0,"An empty or unproven document produces no body write grant");
    }
}
