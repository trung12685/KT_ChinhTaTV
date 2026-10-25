using ChinhTa.Core;

public static class BodyEmphasisLayoutChecks
{
    public static void Run(Action<bool,string> assert)
    {
        var sample=BodySpacingChecks.Sample();var rows=sample.P.ToArray();var row=rows[10];string text=row.Text.TrimEnd('\r');
        ObservedFont[] fonts=[new("Arial",12,0,0,255),new("Arial",14,-1,0,255),new("Arial",13,0,-1,255),new("Arial",14,-1,-1,255)];
        rows[10]=row with {FormatEvidence=new(text,fonts),Outline=row.Outline! with {Alignment=0},ParagraphStyle=row.ParagraphStyle! with {Alignment=0,First=0}};
        rows[11]=rows[11] with {FormatEvidence=new(rows[11].Text.TrimEnd('\r'),fonts)};
        IReadOnlyList<Issue> Find(Paragraph[] p)=>BodyFormatRules.Check(p,sample.L,DocumentProfile.State)
            .Concat(BodyParagraphRules.Check(p,sample.L,DocumentProfile.State)).Concat(BodySpacingRules.Check(p,sample.L,DocumentProfile.State)).ToArray();
        var styles=Find(rows).Where(i=>i.Start==row.Start).ToArray();
        assert(styles.Length==4 && styles.Select(i=>i.Kind).Distinct().Count()==4,
            "Ordinary body prose with some emphasized words offers independent font, alignment/indent, gap and line repairs");
        var source=new CharacterStyleEvidence(text,text.Select((c,i)=>char.IsWhiteSpace(c)?null:new ObservedCharacterStyle(fonts[i%4],i%2)),rows[10].ParagraphStyle);
        var proven=new[]{new WordParagraphSnapshot(row.Start,row.Start+row.Text.Length,row.Text,1,false,false)};
        var probe=CasePreservationPlan.Regions(styles,proven).Single();
        foreach(var fix in styles.Where(i=>i.CharacterFormat is null))
        {
            var expected=CasePreservationPlan.Expected(probe,[fix],source,_=>"");
            assert(expected.Text==text && expected.Styles.SequenceEqual(source.Styles),
                "Selecting only a layout/gap/line repair retains every original font, legal/illegal size, emphasis, underline and unselected text error");
            assert(expected.Paragraph==BodyStylePatchPlan.Apply(fix,source.Paragraph!),
                "A single emphasized-prose layout action changes only its approved paragraph attributes");
            foreach(var index in source.Styles.Select((s,i)=>(s,i)).Where(x=>x.s is not null).Take(4).Select(x=>x.i))
            {
                var altered=expected.Styles.ToArray();altered[index]=altered[index]! with {Font=altered[index]!.Font with {Bold=altered[index]!.Font.Bold==0?-1:0}};
                assert(!expected.Matches(new(text,altered,expected.Paragraph)),
                    "Losing or adding emphasis during a paragraph-only repair fails the retained-glyph postcondition");
            }
        }
        var edits=Checker.Check([rows[10]]).Where(i=>i.CanFix).ToArray();var selected=styles.Concat(edits).ToArray();
        var combined=CasePreservationPlan.Expected(probe,selected,source,i=>text[..(i.Start-row.Start)]);
        assert(combined.Paragraph is {Alignment:3,First:36,Spacing:{After:6,Rule:0}} && combined.Text=="Nội dung công tác đã được kiểm tra đầy đủ 0." &&
            combined.Styles.Where(s=>s is not null).All(s=>s!.Font.Name=="Times New Roman" && s.Font.Color==0 && s.Font.Size is 13 or 14),
            "Four body style groups compose with selected whitespace/repetition while retaining mixed emphasis and valid sizes");
        var fontOnly=source.WithFormat(styles.Single(i=>i.Kind==FixKind.BodyFormat).CharacterFormat!).Project(edits,i=>text[..(i.Start-row.Start)]);
        assert(combined.Styles.SequenceEqual(fontOnly.Styles),"Adding layout/spacing to a font-and-text batch cannot alter the approved retained glyph styles");
        foreach(var selection in new[]{selected,selected.Reverse().ToArray(),selected.OrderBy(i=>(int)i.Kind).ToArray()})
        {
            var actual=CasePreservationPlan.Expected(probe,selection,source,i=>text[..(i.Start-row.Start)]);
            assert(combined.Matches(actual) && actual.Matches(combined),"Checkbox order cannot change the mixed-emphasis batch result");
        }
        var fixedRows=rows.ToArray();
        fixedRows[10]=rows[10] with {ParagraphStyle=combined.Paragraph,Outline=rows[10].Outline! with {Alignment=3},
            FormatEvidence=new(text,fonts.Select(f=>f with {Name="Times New Roman",Color=0,Size=f.Size==12?13:f.Size}))};
        assert(!Find(fixedRows).Any(i=>i.Start==row.Start),"Fresh scanning removes all four corrected findings for mixed-emphasis prose");
        var gap=styles.Single(i=>i.Kind==FixKind.BodyGap);string story=string.Concat(rows.Select(p=>p.Text));
        var proofs=BodySpacingVerification.Build(styles,story,i=>story[..i.Start]);
        BodySpacingVerification.Verify(proofs,fixedRows,p=>story[..p.Start]);
        assert(true,"Mixed-emphasis neighbors still require the same exact adjacent text, XML context and expected paragraph state");
        bool Reject(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
        foreach(var badFonts in new ObservedFont[][]{[fonts[1]],[fonts[2]],[fonts[1],fonts[2]],
            [fonts[0] with {Bold=9999999},fonts[1]],[fonts[0],fonts[2] with {Italic=9999999}]})
        {
            var bad=rows.ToArray();bad[10]=rows[10] with {FormatEvidence=new(text,badFonts)};
            assert(!Find(bad).Any(i=>i.Start==row.Start && i.Kind!=FixKind.BodyFormat),
                "Entirely emphasized or unknown-emphasis paragraphs cannot grant alignment/spacing writes");
        }
        var noPlain=new CharacterStyleEvidence(text,source.Styles.Select(s=>s is null?null:s with {Font=s.Font with {Bold=-1}}),source.Paragraph);
        assert(Reject(()=>CasePreservationPlan.Expected(probe,[styles.Single(i=>i.Kind==FixKind.BodyParagraph)],noPlain,_=>"")),
            "A live paragraph that lost its plain glyph evidence is rejected before a layout write");
        var stale=source.Paragraph! with {Spacing=source.Paragraph!.Spacing! with {Before=1}};
        assert(Reject(()=>CasePreservationPlan.Expected(probe,[gap],new(text,source.Styles,stale),_=>"")),
            "A live spacing fingerprint change still rejects a mixed-emphasis batch before mutation");
        var missing=rows.ToArray();missing[10]=rows[10] with {FormatEvidence=new("stale",fonts)};
        assert(!Find(missing).Any(i=>i.Start==row.Start),"Mixed emphasis never bypasses exact-text evidence binding");
        var opening=BodyOpeningChecks.Sample();var openingRows=opening.P.ToArray();
        openingRows[9]=openingRows[9] with {FormatEvidence=new(openingRows[9].Text.TrimEnd('\r'),fonts)};
        assert(BodyFormatRules.RepairTargets(openingRows,opening.L,DocumentProfile.State).Count==0,
            "Mixed-emphasis support does not weaken the two plain opening anchors required without a greeting");
        var neighborDrift=fixedRows.ToArray();neighborDrift[11]=neighborDrift[11] with
            {ParagraphStyle=neighborDrift[11].ParagraphStyle! with {Spacing=neighborDrift[11].ParagraphStyle!.Spacing! with {Before=8}}};
        assert(Reject(()=>BodySpacingVerification.Verify(proofs,neighborDrift,p=>story[..p.Start])),
            "A changed unselected neighbor spacing still fails post-save verification even when both paragraphs contain emphasis");
    }
}
