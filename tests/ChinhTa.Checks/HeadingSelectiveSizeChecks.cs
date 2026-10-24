using ChinhTa.Core;

public static class HeadingSelectiveSizeChecks
{
    public static void Run(Action<bool,string> assert)
    {
        ObservedFont Font(float size)=>new("Times New Roman",size,-1,0,0);
        foreach(string text in new[]{Checker.Heading,Checker.Heading.Replace("HÒA","HOÀ"),HeadingRules.Motto,
            HeadingRules.Motto.Replace(" - "," – "),"Độc lập - Tự do – Hạnh phúc"})
        foreach(bool throughOwnedLine in new[]{false,true})
        foreach(int alignment in new[]{0,1})
        {
            bool motto=text.StartsWith("Độc");float min=motto?13:12,max=min+1;
            var row=new Paragraph(50,text+"\r",InTable:true,Alignment:alignment,FormatObserved:true,
                FormatMixed:true,TextEditable:!throughOwnedLine,FormatEditable:throughOwnedLine,
                FormatEvidence:new(text,[Font(min-2),Font(min),Font(max)]));
            var issue=HeadingRules.Check(row,DocumentProfile.State).Single();
            assert(issue.CanFix && issue.Kind==(motto?FixKind.StateMotto:FixKind.NationalHeading) &&
                issue.Format is {RepairInvalidSizes:true,Alignment:1} && issue.Format.Size==min && issue.Format.MaximumSize==max &&
                issue.Original==text && issue.Replacement==text,
                "Mixed invalid/legal national and motto sizes offer a format-only repair with exact dash/tone spelling and existing XML permission");
            BatchPlan.Create([issue]);
            assert(issue.Suggestion.Contains($"chỉ đưa cỡ ngoài {min:0}–{max:0} về {min:0}") &&
                IssueTooltipCatalog.Describe(issue).Action==issue.Suggestion,
                "Approval and tooltip expose selective heading sizing rather than a destructive whole-line size write");
            var repaired=row with {Alignment=1,FormatEvidence=new(text,[Font(min),Font(min),Font(max)])};
            assert(!HeadingRules.Check(repaired,DocumentProfile.State).Any(),
                "Fresh heading evidence accepts the repaired glyph and retained larger legal size without a disabled residual issue");
            assert(!HeadingRules.Check(row with {TextEditable=false,FormatEditable=false},DocumentProfile.State).Single().CanFix,
                "A heading size policy never grants XML permission through an unknown shape/field/control");
            assert(!HeadingRules.Check(row with {Text=text.ToLowerInvariant()+"\r",
                FormatEvidence=new(text.ToLowerInvariant(),[Font(min-2),Font(max)])},DocumentProfile.State).Single().CanFix,
                "Mixed-size repair cannot silently become a full text replacement while casing also needs repair");
        }
        foreach(bool motto in new[]{false,true})
        {
            string text=motto?HeadingRules.Motto:Checker.Heading;float min=motto?13:12,max=min+1;
            var baseRow=new Paragraph(0,text,InTable:true,FormatObserved:true,
                FormatEvidence:new(text,[Font(min-2),Font(max)]));
            foreach(var unknown in new[]{Font(max) with {Name=""},Font(max) with {Size=float.NaN},Font(max) with {Bold=9999999},
                Font(max) with {Italic=9999999},Font(max) with {Color=9999999}})
                assert(!HeadingRules.Check(baseRow with {FormatEvidence=new(text,[Font(min-2),unknown])},DocumentProfile.State).Single().CanFix,
                    "An unreadable required glyph property keeps selective heading repair unavailable");
            assert(!HeadingRules.Check(baseRow with {FormatEvidence=new("Khác",[Font(min-2),Font(max)])},DocumentProfile.State).Single().CanFix,
                "Stale text-bound evidence cannot authorize a selective heading repair");
            var issue=HeadingRules.Check(baseRow,DocumentProfile.State).Single();
            var source=new FontSizeEvidence(text,text.Select((c,i)=>char.IsWhiteSpace(c)?(float?)null:i%3==0?min-2:i%3==1?min:max));
            var expected=source.CorrectInvalid(min,max);
            assert(expected.Sizes.Where(s=>s is not null).All(s=>s==min || s==max) &&
                !expected.Matches(new(text,expected.Sizes.Select(s=>s is not null?(float?)min:null))),
                "Heading preservation proof rejects flattening a larger legal glyph even though every result stays in the legal interval");
            bool stale=false;
            try{CurrentTextPlan.Validate([issue],[issue with {Format=issue.Format! with {RepairInvalidSizes=false}}]);}
            catch(InvalidOperationException){stale=true;}
            assert(stale,"The selective heading flag is part of exact current-proposal validation");
            var spacing=new Issue(0,2,"  "," ","",false);
            var moved=issue with {Start=20};string story="  "+new string('x',18)+text;
            var plan=BatchPlan.Create([spacing,moved]);
            assert(AtomicTextBatch.ExpectedStory(plan,story,i=>story[..i.Start])==story.Remove(0,1),
                "A selective format-only heading still composes with an earlier text edit without replacing its text");
            int restored=0,committed=0;var current=source;
            try
            {
                AtomicTextBatch.Apply([issue],i=>text,i=>{current=expected;return text;},()=>
                {
                    current=new(text,expected.Sizes.Select(s=>s is not null?(float?)min:null));
                    if(!expected.Matches(current))throw new InvalidOperationException("synthetic size drift");
                    committed++;
                },()=>{current=source;restored++;});
            }
            catch(InvalidOperationException){}
            assert(restored==1 && committed==0 && ReferenceEquals(current,source),
                "Wrong post-save heading sizes restore the complete transaction instead of accepting legal-size drift");
        }
        var valid=new Issue(0,Checker.Heading.Length,Checker.Heading,Checker.Heading,"",false,FixKind.NationalHeading,
            Format:new(12,1,13,true));
        foreach(var invalid in new[]{valid with {Format=new(12,1,null,true)},valid with {Format=new(12,1,14,true)},
            valid with {Format=new(13,1,14,true)},valid with {Kind=FixKind.PartyHeading},
            valid with {Replacement=Checker.Heading.ToLowerInvariant()},valid with {Kind=FixKind.Text},
            valid with {CharacterFormat=new(13,false,true)}})
        {
            bool rejected=false;try{BatchPlan.Create([invalid]);}catch(InvalidOperationException){rejected=true;}
            assert(rejected,"Selective heading payloads cannot invent ranges, cross role policies, change text or mix patch kinds");
        }
        var party=new Paragraph(0,HeadingRules.PartyTitle,FormatObserved:true,
            FormatEvidence:new(HeadingRules.PartyTitle,[Font(12),Font(15)]));
        var partyFix=HeadingRules.Check(party,DocumentProfile.Party).Single();
        assert(partyFix.Format is {Size:15,MaximumSize:null,RepairInvalidSizes:false},
            "A fixed-size Party heading keeps its existing single-size route rather than receiving a State interval");
        assert(!HeadingRules.Check(new(0,HeadingRules.Motto,Page:2),DocumentProfile.State).Any() &&
            !HeadingRules.Check(new(0,HeadingRules.Motto),DocumentProfile.Personal).Any(),
            "Selective sizing does not broaden heading detection to later pages or personal documents");
    }
}
