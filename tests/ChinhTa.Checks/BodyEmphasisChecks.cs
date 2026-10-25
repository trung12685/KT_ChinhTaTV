using ChinhTa.Core;

public static class BodyEmphasisChecks
{
    public static void Run(Action<bool,string> assert)
    {
        var sample=BodyFormatChecks.Sample();var row=sample.P[10];string text=row.Text.TrimEnd('\r');
        ObservedFont[] fonts=[new("Arial",12,-1,0,255),new("Arial",14,0,-1,255),new("Arial",13,-1,-1,255)];
        var rows=sample.P.ToArray();rows[10]=row with {FormatEvidence=new(text,fonts)};
        var issue=BodyFormatRules.Check(rows,sample.L,DocumentProfile.State).Single(i=>i.Start==row.Start);var format=issue.CharacterFormat!;
        assert(format is {Role:ComponentRole.Body,PreserveEmphasis:true,PreserveSize:true,RepairInvalidSizes:true,Bold:null} && format.Valid,
            "Proven body font/color/size repair retains mixed bold/italic instead of rejecting or normalizing emphasis");
        var source=new CharacterStyleEvidence(text,text.Select((c,i)=>char.IsWhiteSpace(c)?null:new ObservedCharacterStyle(fonts[i%3],i%2)),new(3,0,0,36,new(0,6,0,12,0,0)));
        var expected=source.WithFormat(format);
        assert(expected.Styles.Select(s=>s?.Font.Bold).SequenceEqual(source.Styles.Select(s=>s?.Font.Bold)) &&
            expected.Styles.Select(s=>s?.Font.Italic).SequenceEqual(source.Styles.Select(s=>s?.Font.Italic)) &&
            expected.Styles.Select(s=>s?.Underline).SequenceEqual(source.Styles.Select(s=>s?.Underline)),
            "Every retained glyph keeps its own bold, italic and underline through the approved font patch");
        assert(expected.Styles.Where(s=>s is not null).All(s=>s!.Font.Name=="Times New Roman" && s.Font.Color==0 && s.Font.Size is 13 or 14) &&
            expected.Styles.Where((_,i)=>source.Styles[i]?.Font.Size==14).All(s=>s!.Font.Size==14),
            "Mixed emphasis cannot hide invalid sizes and valid 14pt glyphs cannot be flattened to 13pt");
        var plan=BatchPlan.Create(Checker.Check([rows[10]]).Where(i=>i.CanFix).Append(issue));
        var probe=CasePreservationPlan.Regions(plan,[new(row.Start,row.Start+row.Text.Length,row.Text,2,false,false)]).Single();
        var combined=CasePreservationPlan.Expected(probe,plan,source,i=>text[..(i.Start-row.Start)]);
        assert(combined.Text=="Nội dung công tác đã được kiểm tra đầy đủ 0." && combined.Paragraph==source.Paragraph &&
            AtomicTextBatch.ExpectedStory([issue],row.Text,i=>"")==row.Text,
            "Emphasis-preserving body format composes with selected spaces/repetition while format-only retains unselected text errors");
        assert(!expected.Matches(new(text,expected.Styles,source.Paragraph! with {Spacing=source.Paragraph.Spacing! with {After=0}})),
            "A font-only body patch still rejects unselected paragraph-spacing drift");
        var unknownParagraph=rows.ToArray();unknownParagraph[10]=rows[10] with {ParagraphStyle=null};
        assert(!BodyFormatRules.Check(unknownParagraph,sample.L,DocumentProfile.State).Any(i=>i.Start==row.Start),
            "A missing paragraph snapshot cannot offer a body repair whose retained spacing cannot be verified");
        var repaired=rows.ToArray();repaired[10]=rows[10] with {FormatEvidence=new(text,fonts.Select(f=>f with {Name="Times New Roman",Color=0,Size=f.Size==12?13:f.Size}))};
        assert(!BodyFormatRules.Check(repaired,sample.L,DocumentProfile.State).Any(i=>i.Start==row.Start),
            "Correct font/color/sizes with mixed emphasis do not create a C26 defect or a stale disabled warning");
        foreach(var font in new[]{new ObservedFont("Arial",12,9999999,0,255),new("Arial",12,0,9999999,255),new("",12,-1,0,255)})
        {
            var bad=rows.ToArray();bad[10]=row with {FormatEvidence=new(text,[font])};
            assert(!BodyFormatRules.Check(bad,sample.L,DocumentProfile.State).Any(i=>i.Start==row.Start),
                "Unknown emphasis/font remains unproven and cannot grant a write");
        }
        foreach(var wrong in new[]{format with {Role=ComponentRole.Agency},format with {Bold=true},format with {Italic=true},format with {Role=null}})
            assert(!wrong.Valid,"The preserve-emphasis flag cannot weaken required heading, agency or upright-format policies");
        bool Reject(Action a){try{a();return false;}catch(InvalidOperationException){return true;}}
        assert(Reject(()=>CurrentTextPlan.Validate([issue],[issue with {CharacterFormat=format with {PreserveEmphasis=false,Bold=false}}])),
            "A changed emphasis-retention contract invalidates stale body selections");
        foreach(var (index,drift) in new[]{(0,expected.Styles[0]! with {Font=expected.Styles[0]!.Font with {Bold=0}}),
            (1,expected.Styles[1]! with {Font=expected.Styles[1]!.Font with {Italic=0}}),(1,expected.Styles[1]! with {Font=expected.Styles[1]!.Font with {Size=13}})})
        {
            var actual=expected.Styles.ToArray();actual[index]=drift;
            assert(!expected.Matches(new(text,actual,source.Paragraph)),"Lost emphasis or a changed valid size fails the exact body glyph postcondition");
        }
        var opening=BodyOpeningChecks.Sample();var later=opening.P[11];var laterRows=opening.P.ToArray();laterRows[11]=later with {FormatEvidence=new(later.Text.TrimEnd('\r'),fonts)};
        assert(BodyFormatRules.Check(laterRows,opening.L,DocumentProfile.State).Any(i=>i.Start==later.Start) &&
            !BodyParagraphRules.Check(laterRows,opening.L,DocumentProfile.State).Any(i=>i.Start==later.Start),
            "A later emphasized paragraph in a proven no-greeting body gets C26 without widening paragraph-layout repair");
        assert(issue.Suggestion.Contains("giữ chữ, đậm/nghiêng") && IssueTooltipCatalog.Describe(issue).Action.Contains("đậm/nghiêng"),
            "The compact action explicitly preserves emphasis rather than promising upright/plain text");
        foreach(var size in new[]{12f,13f,14f})
        {
            var uniform=rows.ToArray();uniform[10]=row with {FormatEvidence=new(text,[new("Arial",size,-1,-1,255)])};
            var proposed=BodyFormatRules.Check(uniform,sample.L,DocumentProfile.State).Single(i=>i.Start==row.Start).CharacterFormat!;
            assert(proposed.PreserveSize && proposed.RepairInvalidSizes==(size==12),
                "Even uniform body corrections avoid a range-size write and only repair invalid visible glyph sizes");
        }
    }
}
