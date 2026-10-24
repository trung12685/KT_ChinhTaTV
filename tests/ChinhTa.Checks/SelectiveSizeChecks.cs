using ChinhTa.Core;

public static class SelectiveSizeChecks
{
    public static void Run(Action<bool,string> assert)
    {
        ObservedFont Font(float size,int bold=-1,int italic=0)=>new("Times New Roman",size,bold,italic,0);
        Paragraph Observe(Paragraph row,params ObservedFont[] fonts)=>row with {FormatObserved=true,FormatMixed=true,
            FormatEvidence=new(row.Text.TrimEnd('\r','\a','\n'),fonts)};
        var sample=ComponentChecks.Sample(softBreak:true);
        foreach(var target in ComponentRules.Identify(sample.Rows,sample.Layout,DocumentProfile.State)
            .Where(t=>t.Role is ComponentRole.Agency or ComponentRole.AgencyParent))
        {
            int bold=target.Role==ComponentRole.Agency?-1:0;
            var row=Observe(target.Row,Font(10,bold),Font(12,bold),Font(13,bold));
            var rows=sample.Rows.Select(p=>p.Start==row.Start?row:p).ToArray();
            var fix=ComponentRules.Check(rows,sample.Layout,DocumentProfile.State).Single();
            assert(fix.CanFix && fix.CharacterFormat is {Size:12,PreserveSize:true,RepairInvalidSizes:true} &&
                fix.CharacterFormat.Role==target.Role && fix.Original==fix.Replacement,
                "Mixed invalid and valid agency sizes receive one selective whole-role proposal, retaining existing 12/13pt glyphs");
            BatchPlan.Create([fix]);
            var repaired=Observe(row,Font(12,bold),Font(12,bold),Font(13,bold));
            assert(ComponentRules.Check(rows.Select(p=>p.Start==row.Start?repaired:p).ToArray(),sample.Layout,DocumentProfile.State).Count==0,
                "Fresh proof removes a selectively repaired agency issue without retaining a disabled warning");
            assert(IssueTooltipCatalog.Describe(fix).Action.Contains("chỉ đưa cỡ ngoài 12–13 về 12") &&
                fix.Suggestion.Contains("giữ từng cỡ đúng"),"The selective-size tooltip states exactly which sizes will change");
        }
        foreach(var row in DocumentHeadingRules.ObservationTargets(sample.Rows,sample.Layout,DocumentProfile.State))
        {
            var observed=Observe(row,Font(12),Font(13),Font(14));
            var rows=sample.Rows.Select(p=>p.Start==row.Start?observed:p).ToArray();
            var fix=DocumentHeadingRules.Check(rows,sample.Layout,DocumentProfile.State).Single();
            assert(fix.CanFix && fix.CharacterFormat is {RepairInvalidSizes:true,PreserveSize:true,Size:13},
                "Named title and each subject paragraph including a soft break can repair only invalid sizes");
            BatchPlan.Create([fix]);
            var repaired=Observe(row,Font(13),Font(13),Font(14));
            assert(!DocumentHeadingRules.Check(rows.Select(p=>p.Start==row.Start?repaired:p).ToArray(),sample.Layout,DocumentProfile.State).Any(),
                "A fresh title/subject scan accepts the normalized invalid glyph and retained 14pt glyph");
        }
        var front=DocumentHeadingChecks.Sample();var date=front.P.Single(p=>p.Text.StartsWith("Địa danh,"));
        var dated=Observe(date,Font(12,0,-1),Font(13,-1,-1),Font(14,0,-1));
        var dateFix=AdministrativeHeaderRules.Check(front.P.Select(p=>p.Start==date.Start?dated:p).ToArray(),DocumentProfile.State)
            .Single(i=>i.Start==date.Start && i.CharacterFormat is not null);
        assert(dateFix.CanFix && dateFix.CharacterFormat is {RepairInvalidSizes:true,Italic:true,Bold:null},
            "Selective date sizing keeps blank date fields and unconstrained bold while enforcing italic");
        BatchPlan.Create([dateFix]);
        foreach(var forbidden in new[]{new CharacterFormat(13,false,null,PreserveSize:true,RepairInvalidSizes:true),
            new CharacterFormat(13,false,true,RepairInvalidSizes:true),
            new CharacterFormat(14,false,true,PreserveSize:true,RepairInvalidSizes:true),
            new CharacterFormat(11,false,Role:ComponentRole.RecipientItem,PreserveSize:true,RepairInvalidSizes:true)})
        {
            bool rejected=false;
            try{BatchPlan.Create([dateFix with {CharacterFormat=forbidden}]);}catch(InvalidOperationException){rejected=true;}
            assert(!forbidden.Valid && rejected,"Forged selective plans cannot bypass real variable-size role policies or the preserve-size contract");
        }
        var policy=new FormatPolicy(13,14,false,true);
        var normal=Observe(new Paragraph(0,"Nội dung"),Font(12),Font(14));
        assert(policy.Assess(normal).State==FormatState.NeedsReview &&
            policy.Assess(normal,true) is {State:FormatState.Repairable,RepairInvalidSizes:true,PreserveSize:true},
            "Only the character-format repair route opts in; legacy full-heading writes remain guarded");
        foreach(var unsafeRow in new[]{normal with {TextEditable=false,FormatEditable=false},normal with {Text="Khác"},
            Observe(normal,Font(12),Font(14) with {Name=""}),Observe(normal,Font(12),Font(14) with {Size=9999999}),
            Observe(normal,Font(12),Font(14) with {Italic=9999999})})
            assert(policy.Assess(unsafeRow,true).State==FormatState.Unknown,
                "Selective sizing does not open unproven, stale or unreadable font evidence");
        const string text="đơn  vị kế hoạch kế hoạch";
        var source=new FontSizeEvidence(text,text.Select((c,i)=>char.IsWhiteSpace(c)?(float?)null:i%3==0?10:i%3==1?12:13));
        var corrected=source.CorrectInvalid(12,13);
        assert(corrected.Text==text && corrected.Sizes.Where(s=>s is not null).All(s=>s is >=12 and <=13) &&
            corrected.Sizes.Select((s,i)=>source.Sizes[i] is not 13 || s==13).All(x=>x),
            "Expected size proof repairs invalid glyphs and preserves every already legal size");
        var caseEdit=new Issue(0,3,"đơn","ĐƠN","",false,FixKind.AgencyText);
        var gap=new Issue(3,2,"  "," ","",false);
        int repeat=text.LastIndexOf(" kế hoạch",StringComparison.Ordinal);
        var deletion=new Issue(repeat,text.Length-repeat,text[repeat..],"","",false,FixKind.RepeatedText);
        var format=new Issue(0,text.Length,text,text,"",false,FixKind.AgencyFormat,
            CharacterFormat:new(12,false,true,ComponentRole.Agency,true,true));
        var plan=BatchPlan.Create([format,caseEdit,gap,deletion]);
        var expected=corrected.Project([caseEdit,gap,deletion],i=>text[..i.Start]);
        assert(plan[0]==format && expected.Text=="ĐƠN vị kế hoạch" &&
            AtomicTextBatch.ExpectedStory(plan,text,i=>text[..i.Start])==expected.Text,
            "One whole-region selective plan composes with case, spaces and repetition even when a text edit crosses size boundaries");
        var collapsed=expected.Sizes.Select(s=>s is float?12:(float?)null).ToArray();
        assert(!expected.Matches(new(expected.Text,collapsed)),"Final verification rejects flattening 13pt to equally legal 12pt after selective repair");
        var wrongTarget=expected.Sizes.Select((s,i)=>s is 12?13:s).ToArray();
        assert(!expected.Matches(new(expected.Text,wrongTarget)),"Invalid glyphs must reach the approved minimum, not just any legal size");
        bool missing=false;try{new FontSizeEvidence("x",[null]).CorrectInvalid(12,13);}catch(InvalidOperationException){missing=true;}
        assert(missing,"An unreadable visible size cannot become a selective repair expectation");
        foreach(var range in new[]{(0f,13f),(13f,12f),(float.NaN,14f),(13f,float.PositiveInfinity)})
        {
            bool rejected=false;try{source.CorrectInvalid(range.Item1,range.Item2);}catch(InvalidOperationException){rejected=true;}
            assert(rejected,"Invalid correction intervals are rejected");
        }
        var old=dateFix;
        bool stale=false;try{CurrentTextPlan.Validate([old],[old with {CharacterFormat=old.CharacterFormat! with {RepairInvalidSizes=false}}]);}
        catch(InvalidOperationException){stale=true;}
        assert(stale,"A selective-size choice cannot be replayed after the current required operation changes");
        var rollback=source;bool restored=false;
        try{AtomicTextBatch.Apply([format],i=>text,i=>{rollback=corrected;return text;},()=>throw new InvalidOperationException("synthetic save"),()=>{rollback=source;restored=true;});}
        catch(InvalidOperationException){}
        assert(restored && ReferenceEquals(rollback,source),"A failed commit rolls back the whole selective formatting transaction");
        foreach(bool flatten in new[]{false,true})
        {
            restored=false;rollback=source;bool failed=false;
            try
            {
                AtomicTextBatch.Apply([format],i=>text,i=>
                {
                    rollback=new(text,corrected.Sizes.Select(s=>s is null?null:flatten?(float?)12:13));return text;
                },()=>
                {
                    if(!corrected.Matches(rollback))throw new InvalidOperationException("synthetic legal-size drift");
                },()=>{rollback=source;restored=true;});
            }
            catch(InvalidOperationException){failed=true;}
            assert(failed && restored && ReferenceEquals(rollback,source),
                "A post-save size still inside the legal interval rolls back when it changes the approved per-glyph result");
        }
    }
}
