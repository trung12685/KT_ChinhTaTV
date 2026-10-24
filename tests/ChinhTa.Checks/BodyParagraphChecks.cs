using ChinhTa.Core;

public static class BodyParagraphChecks
{
    public static ObservedParagraphStyle Style(int alignment=0,float first=0)=>new(alignment,0,0,first,new(3,8,5,18,0,0));
    private static CharacterStyleEvidence Styles(string text,ObservedParagraphStyle paragraph)=>new(text,text.Select((c,i)=>char.IsWhiteSpace(c)?null:
        new ObservedCharacterStyle(new("Arial",i%2==0?12:14,0,0,255),i%3==0?1:0)),paragraph);
    public static void Run(Action<bool,string> assert)
    {
        var sample=BodyFormatChecks.Sample();var original=sample.P[10];
        IReadOnlyList<Issue> Check(ObservedParagraphStyle? style,Paragraph? row=null)
        {
            var rows=sample.P.ToArray();rows[10]=(row??original) with {ParagraphStyle=style,Outline=new(OutlineScope.PlainBody,Alignment:style?.Alignment)};
            return BodyParagraphRules.Check(rows,sample.L,DocumentProfile.State);
        }
        foreach(float first in new[]{72/2.54f,28.35f,36f,35.99f})
        {
            assert(Check(Style(3,first)).Count==0,"Justified ordinary prose with either legal first-line indent has no paragraph defect");
            var fix=Check(Style(0,first)).Single().ParagraphFix!;
            assert(fix.Alignment==3 && fix.First is null && fix.Expected.First==first,"Alignment-only repair retains the existing legal 1cm or 1.27cm indent");
        }
        foreach(float first in new[]{0f,7f,20f,50f,100f})
        {
            var issue=Check(Style(3,first)).Single();
            assert(issue.ParagraphFix is {Alignment:null,First:36} && issue.Original==issue.Replacement && issue.IsStyleOnly,
                "Invalid nonhanging first-line indent gets a paragraph-only 1.27cm proposal while already-correct justification stays unchanged");
        }
        foreach(int rule in new[]{0,1,2})
        {
            var proportional=new ObservedParagraphSpacing(3,8,rule,12,0,0);
            assert(proportional.Matches(proportional with {Line=14}) && !proportional.Matches(proportional with {Rule=(rule+1)%3}),
                "Proportional line spacing preserves its exact rule while allowing derived point height to follow a selected font change");
        }
        var before=Style();var issueBoth=Check(before).Single();var paragraph=issueBoth.ParagraphFix!;
        assert(paragraph.Valid && paragraph.Alignment==3 && paragraph.First==36 && paragraph.Expected.Spacing==before.Spacing && paragraph.Expected.Left==before.Left && paragraph.Expected.Right==before.Right,
            "Combined justification/first-line repair preserves both side indents, spacing and line-spacing settings");
        foreach(var style in new ObservedParagraphStyle?[]{null,Style(1),Style(2),Style() with {Left=10},Style() with {Right=1},Style(first:-10),Style(first:145),
            Style() with {Spacing=null},Style() with {Spacing=new(9999999,0,0,12,0,0)},Style() with {First=float.NaN},Style() with {Spacing=new(0,0,9999999,12,0,0)}})
            assert(Check(style).Count==0,"Unknown, centered/right, inset, hanging or excessive/invalid paragraph settings never grant an ordinary-prose layout write");
        foreach(var row in new[]{original with {TextEditable=false},original with {InTable=true},original with {FormatObserved=false},original with {FormatEvidence=null},
            original with {Text=" "+original.Text},original with {FormatEvidence=new(original.Text.TrimEnd('\r'),[new("Arial",12,-1,0,255)])}})
            assert(Check(before,row).Count==0,"Unproven paragraphs, emphasis and spaces used as manual indentation stay outside layout repair");
        var body=original with {ParagraphStyle=before,Outline=new(OutlineScope.PlainBody,Alignment:0)};
        var rows=sample.P.ToArray();rows[10]=body;
        var font=BodyFormatRules.Check(rows,sample.L,DocumentProfile.State).Single(i=>i.Start==body.Start);
        var text=Checker.Check([body]).Where(i=>i.CanFix).ToArray();
        var plan=BatchPlan.Create(text.Append(font).Append(issueBoth));
        assert(plan.Count==text.Length+2 && plan.Take(2).All(i=>i.IsStyleOnly),"Same full paragraph accepts font and paragraph repairs before descending text edits without conflicting ranges");
        var proven=new[]{new WordParagraphSnapshot(body.Start,body.Start+body.Text.Length,body.Text,2,false,false)};
        var probe=CasePreservationPlan.Regions(plan,proven).Single();var source=Styles(probe.Original,before);
        var only=CasePreservationPlan.Expected(probe,[issueBoth],source,i=>"");
        assert(only.Styles.SequenceEqual(source.Styles) && only.Paragraph==paragraph.Expected && only.Text==source.Text,
            "Selecting only paragraph layout preserves every unselected wrong font/color/size and all text errors");
        var fontOnly=CasePreservationPlan.Expected(probe,[font],source,i=>"");
        assert(fontOnly.Paragraph==before,"Selecting only font leaves unselected alignment/indent defects unchanged");
        var combined=CasePreservationPlan.Expected(probe,plan,source,i=>probe.Original[..(i.Start-probe.Start)]);
        assert(combined.Text=="Nội dung công tác đã được kiểm tra đầy đủ 0." && combined.Paragraph==paragraph.Expected &&
            combined.Styles.Where(s=>s is not null).All(s=>s!.Font.Name=="Times New Roman" && s.Font.Size is 13 or 14 && s.Font.Color==0),
            "Combined layout/font/spacing/repetition projects retained glyphs and updates exactly the selected paragraph attributes");
        assert(AtomicTextBatch.ExpectedStory([issueBoth],body.Text,i=>"")==body.Text && StagedRepairBatch.ExpectedReference(issueBoth,plan,body.Text,i=>body.Text[..(i.Start-body.Start)])==combined.Text,
            "Paragraph repairs do not become fake text replacements and fresh later-stage references reflect selected text changes only");
        var current=rows.ToArray();current[10]=body with {ParagraphStyle=paragraph.Expected,Outline=body.Outline! with {Alignment=3}};
        assert(!BodyParagraphRules.Check(current,sample.L,DocumentProfile.State).Any(i=>i.Start==body.Start),"A new scan removes the corrected paragraph finding entirely");
        foreach(var drift in new[]{paragraph.Expected with {Spacing=before.Spacing! with {After=9}},paragraph.Expected with {Spacing=before.Spacing! with {Line=24}},
            paragraph.Expected with {Spacing=before.Spacing! with {BeforeAuto=-1}},paragraph.Expected with {Right=1},paragraph.Expected with {First=28.35f}})
            assert(!only.Matches(new(only.Text,only.Styles,drift)),"Unselected spacing/auto-spacing/side-indent drift or a different final first-line target fails the layout postcondition");
        var altered=only.Styles.ToArray();altered[0]=altered[0]! with {Font=altered[0]!.Font with {Size=13}};
        assert(!only.Matches(new(only.Text,altered,only.Paragraph)),"A paragraph-only repair cannot silently normalize an unselected glyph size");
        bool Reject(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
        foreach(var bad in new[]{issueBoth with {ParagraphFix=null},issueBoth with {Kind=FixKind.Text},issueBoth with {CharacterFormat=font.CharacterFormat},
            issueBoth with {Replacement=issueBoth.Original+" "},issueBoth with {ParagraphFix=paragraph with {First=20}},issueBoth with {ParagraphFix=paragraph with {Alignment=2}}})
            assert(Reject(()=>BatchPlan.Create([bad])),"Forged paragraph role/target, missing payload, text replacement or bundled font payload cannot grant a write");
        var changed=issueBoth with {ParagraphFix=paragraph with {Before=before with {Spacing=before.Spacing! with {After=10}}}};
        assert(Reject(()=>CurrentTextPlan.Validate([issueBoth],[changed])) && !RepairDependencies.Same(issueBoth,changed),"A changed paragraph/spacing fingerprint invalidates stale selections and dependency identity");
        assert(Reject(()=>CasePreservationPlan.Expected(probe,[issueBoth],Styles(probe.Original,before with {First=5}),i=>"")),"Changed actual Word paragraph settings fail before the first write");
        var partial=issueBoth with {Start=issueBoth.Start+1,Length=issueBoth.Length-1,Original=issueBoth.Original[1..],Replacement=issueBoth.Replacement[1..]};
        assert(Reject(()=>CasePreservationPlan.Regions([partial],proven)),"A partial text range cannot format the surrounding full paragraph");
        bool restored=false;int writes=0;try{AtomicTextBatch.Apply(plan,i=>i.Original,i=>{writes++;return i.Replacement;},()=>throw new InvalidOperationException("verify"),()=>restored=true);}catch(InvalidOperationException){}
        assert(restored && writes==plan.Count,"Failed combined paragraph/font/text verification rolls back the whole transaction");
        var info=IssueReviewCatalog.Describe(issueBoth);var tooltip=IssueTooltipCatalog.Describe(issueBoth);
        assert(info.Section==IssueReviewSection.RequiredFormat && info.FamilyKey=="body-paragraph" && tooltip.Text.Split('\n').Length==3 && tooltip.Basis.Contains("II.6, điểm e") && tooltip.Action.Contains("1,27 cm"),
            "Compact required layout rows keep the three-part tooltip with the correct legal clause and chosen indent target");
    }
}
