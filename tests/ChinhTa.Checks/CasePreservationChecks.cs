using ChinhTa.Core;

public static class CasePreservationChecks
{
    private static CharacterStyleEvidence Evidence(string text,bool bad=false)=>new(text,text.Select((c,i)=>char.IsWhiteSpace(c)?null:
        new ObservedCharacterStyle(new(bad?"Arial":"Times New Roman",i%2==0?13:14,bad?0:-1,bad?-1:0,bad?255:0),i%3==0?1:0)),new(1,0,0,0));
    public static void Run(Action<bool,string> assert)
    {
        foreach(string name in DocumentTypeCatalog.Entries.Where(e=>e.NamedHeading).Select(e=>e.Heading.ToLowerInvariant()).Append("Giấy ủy quyền"))
        {
            var sample=DocumentHeadingChecks.Sample(name);
            var rows=sample.P.Select(p=>p with {Font="Times New Roman",Size=13,Bold=true,Italic=false,Color=0}).ToArray();
            var title=rows[6];string text=title.Text.TrimEnd('\r');
            rows[6]=title with {FormatMixed=true,FormatEvidence=new(text,[new("Times New Roman",13,-1,0,0),new("Times New Roman",14,-1,0,0)])};
            var issue=DocumentHeadingRules.Check(rows,sample.L,DocumentProfile.State).Single();
            assert(issue.Kind==FixKind.DocumentTypeText && issue.CanFix && issue.Replacement==text.ToUpperInvariant(),
                "Every supported front-title name may be capitalized with proven mixed legal sizes, including user and tone-mark aliases");
            var snapshots=rows.Select(p=>new WordParagraphSnapshot(p.Start,p.Start+p.Text.Length,p.Text,p.Page,p.InTable,true)).ToArray();
            var probe=CasePreservationPlan.Regions([issue],snapshots).Single();
            var source=Evidence(text);var expected=CasePreservationPlan.Expected(probe,[issue],source,i=>"");
            assert(expected.Text==text.ToUpperInvariant() && expected.Styles.SequenceEqual(source.Styles) && expected.Paragraph==source.Paragraph,
                "Case-only proof retains each glyph's size/font/color/bold/italic/underline and paragraph formatting");
            assert(expected.Matches(new(expected.Text,source.Styles,source.Paragraph)),
                "Fresh uppercase text with all original styles passes the case-only postcondition");
            var repaired=rows.Select(p=>p.Start==title.Start?p with {Text=issue.Replacement+"\r",FormatEvidence=new(issue.Replacement,p.FormatEvidence!.Fonts)}:p).ToArray();
            var layout=sample.L.Select(p=>p.Start==title.Start?p with {Text=issue.Replacement+"\r"}:p).ToArray();
            assert(DocumentHeadingRules.Check(repaired,layout,DocumentProfile.State).Count==0,
                "A fresh title scan removes the repaired capitalization issue without a disabled mixed-size warning");
        }
        const string padded="  Báo cáo  ";
        var upper=new Issue(5002,7,"Báo cáo","BÁO CÁO","",false,FixKind.DocumentTypeText);
        WordParagraphSnapshot[] proven=[new(5000,5012,padded+"\r",1,false,true)];
        var whole=CasePreservationPlan.Regions([upper],proven).Single();var original=Evidence(padded,bad:true);
        var onlyCase=CasePreservationPlan.Expected(whole,[upper],original,i=>"  ");
        assert(whole.Start==5000 && whole.Original==padded && onlyCase.Text=="  BÁO CÁO  " && onlyCase.Styles.SequenceEqual(original.Styles),
            "Whole-paragraph tracking retains layout spaces and uses a verified prefix rather than treating a story CP as a string index");
        var font=new Issue(5000,padded.Length,padded,padded,"",false,FixKind.DocumentHeadingFormat,
            CharacterFormat:new(13,false,true,PreserveSize:true));
        var plan=BatchPlan.Create([upper,font]);
        var combined=CasePreservationPlan.Expected(CasePreservationPlan.Regions(plan,proven).Single(),plan,original,i=>"  ");
        assert(combined.Text==onlyCase.Text && combined.Styles.Where(s=>s is not null).All(s=>s!.Font.Name=="Times New Roman" &&
            s.Font.Color==0 && s.Font.Bold==-1 && s.Font.Italic==0) && combined.Styles.Select(s=>s?.Font.Size).SequenceEqual(original.Styles.Select(s=>s?.Font.Size)) &&
            combined.Styles.Select(s=>s?.Underline).SequenceEqual(original.Styles.Select(s=>s?.Underline)),
            "A selected font repair changes only approved attributes and preserves every size and underline during capitalization");
        assert(!onlyCase.Matches(combined) && !combined.Matches(onlyCase),
            "Unselected font repairs cannot be smuggled into a case-only action, and selected repairs must actually take effect");
        var invalidSize=original.Styles.Select(s=>s is null?null:s with {Font=s.Font with {Size=s.Font.Size==13?12:14}}).ToArray();
        var selective=font with {CharacterFormat=font.CharacterFormat! with {RepairInvalidSizes=true}};
        var mixed=CasePreservationPlan.Expected(whole,[upper,selective],new(padded,invalidSize,original.Paragraph),i=>"  ");
        assert(mixed.Styles.Where(s=>s is not null).All(s=>s!.Font.Size is 13 or 14) &&
            mixed.Styles.Where((_,i)=>original.Styles[i]?.Font.Size==14).All(s=>s!.Font.Size==14),
            "Case plus selective sizing normalizes only invalid glyphs while preserving legal 14pt glyphs");
        var uniform=CasePreservationPlan.Expected(whole,[upper,font with {CharacterFormat=new(13,false,true)}],original,i=>"  ");
        assert(uniform.Styles.Where(s=>s is not null).All(s=>s!.Font.Size==13),
            "An explicitly selected uniform-size patch is reflected in the case fingerprint instead of demanding the old sizes");
        var one=combined.Styles.First(s=>s is not null)!;
        foreach(var drift in new[]{one with {Font=one.Font with {Size=12}},one with {Font=one.Font with {Name="Arial"}},
            one with {Font=one.Font with {Bold=0}},one with {Font=one.Font with {Italic=-1}},
            one with {Font=one.Font with {Color=255}},one with {Underline=one.Underline==0?1:0}})
        {
            var changed=combined.Styles.ToArray();changed[2]=drift;
            assert(!combined.Matches(new(combined.Text,changed,combined.Paragraph)),
                "A change to one retained size/font/bold/italic/color/underline fails the case postcondition");
        }
        foreach(var layout in new[]{new ObservedParagraphStyle(0,0,0,0),new(1,2,0,0),new(1,0,2,0),new(1,0,0,2)})
            assert(!combined.Matches(new(combined.Text,combined.Styles,layout)),
                "Changed paragraph alignment or indentation also fails case preservation");
        var automatic=combined.Styles.Select(s=>s is null?null:s with {Font=s.Font with {Name="TIMES NEW ROMAN",Color=-16777216}}).ToArray();
        assert(combined.Matches(new(combined.Text,automatic,combined.Paragraph)),
            "Equivalent black encodings and font-name casing cannot create a false preservation failure");
        bool Reject(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
        assert(Reject(()=>CasePreservationPlan.Regions([upper],[])) && Reject(()=>CasePreservationPlan.Regions([upper],[proven[0],proven[0]])),
            "Missing or ambiguous proven paragraph bounds cannot create a case tracking region");
        var partial=font with {Start=5001,Length=9,Original=padded[1..^1],Replacement=padded[1..^1]};
        assert(Reject(()=>CasePreservationPlan.Regions([upper,partial],proven)),
            "A partial formatting patch cannot escape the full-title preservation contract");
        assert(Reject(()=>CasePreservationPlan.Expected(whole,[upper],new(padded,Enumerable.Repeat<ObservedCharacterStyle?>(null,padded.Length)),i=>"  ")) &&
            Reject(()=>CasePreservationPlan.Expected(whole,[upper],original,i=>"x")),
            "Incomplete source styles or an incorrect real Word prefix fail before any mutation");
        foreach(var bad in new[]{one with {Font=one.Font with {Name=""}},one with {Font=one.Font with {Size=float.NaN}},
            one with {Font=one.Font with {Bold=9999999}},one with {Underline=9999999}})
            assert(Reject(()=>new CharacterStyleEvidence("x",[bad])),"Unknown source styles never become a retention fingerprint");
        assert(Reject(()=>new CharacterStyleEvidence("x",[one],new(9999999,0,0,0))) &&
            Reject(()=>new CharacterStyleEvidence("x",[one],new(1,float.NaN,0,0))),"Unknown paragraph properties cannot gain a preservation proof");
        var badTitle=DocumentHeadingChecks.Sample("Báo cáo");var badRow=badTitle.P[6];
        var badEvidence=badRow with {FormatMixed=true,FormatEvidence=new("Báo cáo",[new("Arial",12,0,-1,255),new("Arial",14,0,-1,255)])};
        var proposals=DocumentHeadingRules.Check(badTitle.P.Select(p=>p.Start==badRow.Start?badEvidence:p).ToArray(),badTitle.L,DocumentProfile.State);
        assert(proposals.Any(i=>i.Kind==FixKind.DocumentTypeText && i.CanFix) && proposals.Any(i=>i.CharacterFormat?.RepairInvalidSizes==true),
            "A mixed-size title may expose independent capitalization and selective font repairs for separate or combined selection");
        var story="Báo cáo";var edit=upper with {Start=0};var proof=Evidence(story);var expectedProof=proof.Project([edit],i=>"");
        string current=story;int restores=0;bool committed=false;
        try
        {
            AtomicTextBatch.Apply([edit],i=>current,i=>{current=i.Replacement;return current;},()=>
            {
                var lost=expectedProof.Styles.Select(s=>s is null?null:s with {Underline=0}).ToArray();
                if(!expectedProof.Matches(new(current,lost,expectedProof.Paragraph)))throw new InvalidOperationException("synthetic case drift");
                committed=true;
            },()=>{current=story;restores++;});
        }
        catch(InvalidOperationException){}
        assert(!committed && restores==1 && current==story,"A case setter returning correct text still rolls back when it changes unselected glyph styles");
    }
}
