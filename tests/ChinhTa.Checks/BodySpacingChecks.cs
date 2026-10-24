using System.Xml.Linq;
using ChinhTa.Core;

public static class BodySpacingChecks
{
    public static (Paragraph[] P,LayoutParagraph[] L) Sample(int count=3)
    {
        var sample=BodyFormatChecks.Sample(count);
        return (sample.P.Select((p,i)=>i>=10 && i<10+count?p with
        {Outline=p.Outline! with {SpacingContext=new()},Flow=new(1,1),ParagraphStyle=new(3,0,0,36,new(0,0,2,24,0,0))}:p).ToArray(),sample.L);
    }
    private static CharacterStyleEvidence Styles(string text,ObservedParagraphStyle style)=>new(text,text.Select((c,i)=>char.IsWhiteSpace(c)?null:
        new ObservedCharacterStyle(new("Arial",i%2==0?12:14,0,0,255),i%3==0?1:0)),style);
    public static void Run(Action<bool,string> assert)
    {
        var sample=Sample();var p=sample.P;var l=sample.L;var row=p[10];
        IReadOnlyList<Issue> Check(Paragraph[]? rows=null)=>BodySpacingRules.Check(rows??p,l,DocumentProfile.State);
        var found=Check();
        assert(found.Count(i=>i.Kind==FixKind.BodyGap)==2 && found.Count(i=>i.Kind==FixKind.BodyLineSpacing)==3,
            "Three proven ordinary paragraphs provide two adjacent gap repairs and three double-line repairs, never a fictional last-paragraph gap");
        foreach(var (after,beforeGap,wanted) in new[]{(0f,0f,true),(2f,3f,true),(3f,3f,false),(0f,6f,false),(6f,0f,false),(4f,4f,false),(0f,12f,false)})
        {
            var rows=p.ToArray();rows[10]=row with {ParagraphStyle=row.ParagraphStyle! with {Spacing=row.ParagraphStyle.Spacing! with {After=after}}};
            rows[11]=p[11] with {ParagraphStyle=p[11].ParagraphStyle! with {Spacing=p[11].ParagraphStyle!.Spacing! with {Before=beforeGap}}};
            assert(Check(rows).Any(i=>i.Kind==FixKind.BodyGap && i.Start==row.Start)==wanted,
                "Paragraph gaps use both sides: already-sufficient or collapse/addition-ambiguous spacing is never flagged from After alone");
        }
        foreach(var (rule,points,wanted) in new[]{(0,12f,false),(1,18f,false),(2,24f,true),(5,12f,false),(5,15f,false),(5,18f,false),
            (5,9f,true),(5,24f,true),(3,8f,false),(4,36f,false)})
        {
            var rows=p.ToArray();rows[10]=row with {ParagraphStyle=row.ParagraphStyle! with {Spacing=row.ParagraphStyle.Spacing! with {Rule=rule,Line=points}}};
            assert(Check(rows).Any(i=>i.Kind==FixKind.BodyLineSpacing && i.Start==row.Start)==wanted,
                "Line spacing accepts Single/1.5/Multiple1–1.5, repairs explicit Double/outside multiples and leaves unmeasured Exactly/AtLeast alone");
        }
        foreach(var unsafeRow in new[]{row with {Flow=null},row with {Flow=new(0,0)},row with {InTable=true},row with {TextEditable=false},
            row with {Outline=row.Outline! with {SpacingContext=null}},row with {Outline=row.Outline! with {SpacingContext=new(Grid:true)}},
            row with {Outline=row.Outline! with {SpacingContext=new(KeepNext:true)}},row with {Outline=row.Outline! with {SpacingContext=new(PageBreakBefore:true)}},
            row with {ParagraphStyle=row.ParagraphStyle! with {First=-1}},row with {ParagraphStyle=row.ParagraphStyle! with {Left=10}},
            row with {FormatEvidence=new(row.Text.TrimEnd('\r'),[new("Arial",12,-1,0,255)])}})
        {
            var rows=p.ToArray();rows[10]=unsafeRow;
            assert(!Check(rows).Any(i=>i.Start==row.Start),"Unknown flow/XML, grid, page/keep constraints, inset/hanging or emphasis cannot grant spacing repairs");
        }
        foreach(var neighbor in new[]{p[11] with {Flow=new(2,2)},p[11] with {Outline=p[11].Outline! with {SpacingContext=new(Contextual:true)}},
            p[11] with {ParagraphStyle=p[11].ParagraphStyle! with {Spacing=p[11].ParagraphStyle!.Spacing! with {BeforeAuto=-1}}},
            p[11] with {Text="\r"},p[11] with {Text="1. Nội dung đã được kiểm tra đầy đủ.\r"}})
        {
            var rows=p.ToArray();rows[11]=neighbor;
            assert(!Check(rows).Any(i=>i.Kind==FixKind.BodyGap && i.Start==row.Start),"A page boundary, contextual/auto spacing, blank or heading is not skipped to invent a pair");
        }
        var separated=Sample(4);separated.P[11]=separated.P[11] with {Text="\r"};
        assert(BodySpacingRules.Check(separated.P,separated.L,DocumentProfile.State).Where(i=>i.Kind==FixKind.BodyGap).Select(i=>i.Start).SequenceEqual([separated.P[12].Start]),
            "A blank boundary leaves a later independent adjacent pair repairable without bridging across the blank");
        var gap=found.Single(i=>i.Kind==FixKind.BodyGap && i.Start==row.Start);
        var line=found.Single(i=>i.Kind==FixKind.BodyLineSpacing && i.Start==row.Start);
        var source=Styles(gap.Original,row.ParagraphStyle!);var font=BodyFormatRules.Check(p,l,DocumentProfile.State).Single(i=>i.Start==row.Start);
        var layout=new Issue(row.Start,gap.Length,gap.Original,gap.Original,"",false,FixKind.BodyParagraph,
            ParagraphFix:new(row.ParagraphStyle! with {Alignment=0,First=0},3,36));
        var leftRows=p.ToArray();leftRows[10]=row with {Outline=row.Outline! with {Alignment=0},ParagraphStyle=layout.ParagraphFix!.Before};
        var leftIssues=Check(leftRows).Where(i=>i.Start==row.Start).ToArray();
        var fixes=leftIssues.Append(layout).Append(font).Concat(Checker.Check([leftRows[10]]).Where(i=>i.CanFix)).ToArray();
        var plan=BatchPlan.Create(fixes);var probe=CasePreservationPlan.Regions(plan,[new(row.Start,row.Start+row.Text.Length,row.Text,1,false,false)]).Single();
        var before=Styles(probe.Original,layout.ParagraphFix!.Before);
        var expected=CasePreservationPlan.Expected(probe,plan,before,i=>probe.Original[..(i.Start-probe.Start)]);
        assert(expected.Paragraph is {Alignment:3,First:36,Spacing:{After:6,Rule:0}} && expected.Text=="Nội dung công tác đã được kiểm tra đầy đủ 0." &&
            expected.Styles.Where(s=>s is not null).All(s=>s!.Font.Name=="Times New Roman" && s.Font.Size is 13 or 14),
            "Independent full-paragraph font/layout/gap/line patches compose with selected text without resetting another patch's attributes");
        foreach(var ordered in new[]{plan.ToArray(),plan.Reverse().ToArray()})
        {
            var replan=BatchPlan.Create(ordered);var current=before.Paragraph!;
            foreach(var patch in replan.Where(i=>i.ParagraphFix is not null || i.SpacingFix is not null))
            {
                assert(BodyStylePatchPlan.Before(patch,replan).Matches(current),"Each paragraph writer is bound to the expected state after earlier approved disjoint patches");
                current=BodyStylePatchPlan.Apply(patch,current);
            }
            assert(current.Matches(expected.Paragraph!),"Changing checkbox order does not change the final approved paragraph state");
        }
        var onlyGap=CasePreservationPlan.Expected(probe,[gap],source,i=>"");
        var onlyLine=CasePreservationPlan.Expected(probe,[line],source,i=>"");
        assert(onlyGap.Styles.SequenceEqual(source.Styles) && onlyGap.Paragraph!.Spacing!.Rule==2 && onlyGap.Paragraph.Spacing.After==6 &&
            onlyLine.Styles.SequenceEqual(source.Styles) && onlyLine.Paragraph!.Spacing!.After==0 && onlyLine.Paragraph.Spacing.Rule==0,
            "Gap-only and line-only actions preserve all glyphs and the independent unselected spacing defect");
        var repaired=p.Select(x=>x.ParagraphStyle is null?x:x with {ParagraphStyle=x.ParagraphStyle with {Spacing=x.ParagraphStyle.Spacing! with {After=6,Rule=0}}}).ToArray();
        assert(BodySpacingRules.Check(repaired,l,DocumentProfile.State).Count==0,"Fresh scanning removes corrected gap/line findings without disabled stale rows");
        string story=string.Concat(p.Select(x=>x.Text));
        var proofs=BodySpacingVerification.Build([gap,line],story,i=>story[..i.Start]);
        var selectedRows=p.Select((x,i)=>i==10?repaired[i]:x).ToArray();
        BodySpacingVerification.Verify(proofs,selectedRows,x=>story[..x.Start]);assert(true,"Post-save proof requires the same paragraph, XML context and immediate neighbor with actual target settings");
        bool Reject(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
        foreach(var rows in new[]{selectedRows.Select((x,i)=>i==10?x with {Outline=x.Outline! with {SpacingContext=new(Contextual:true)}}:x).ToArray(),
            selectedRows.Select((x,i)=>i==11?x with {Text="Mục liền sau đã bị thay thế.\r"}:x).ToArray(),
            selectedRows.Select((x,i)=>i==11?x with {ParagraphStyle=x.ParagraphStyle! with {Spacing=x.ParagraphStyle.Spacing! with {Before=10}}}:x).ToArray(),
            selectedRows.Select((x,i)=>i==11?x with {ParagraphStyle=x.ParagraphStyle! with {Spacing=x.ParagraphStyle.Spacing! with {After=6,Rule=0}}}:x).ToArray(),
            selectedRows.Select((x,i)=>i==10?x with {ParagraphStyle=x.ParagraphStyle! with {Spacing=x.ParagraphStyle.Spacing! with {After=0}}}:x).ToArray()})
            assert(Reject(()=>BodySpacingVerification.Verify(proofs,rows,x=>story[..x.Start])),"Changed XML context, neighbor/before spacing or missing 6pt setting cannot pass final verification");
        var repaged=selectedRows.Select((x,i)=>i==11?x with {Flow=new(2,2)}:x).ToArray();
        BodySpacingVerification.Verify(proofs,repaged,x=>story[..x.Start]);assert(true,"Expected repagination does not replace or invalidate proof of the selected 6pt setting and retained neighbor");
        foreach(var bad in new[]{gap with {Kind=FixKind.Text},gap with {SpacingFix=null},gap with {CharacterFormat=font.CharacterFormat},
            gap with {SpacingFix=gap.SpacingFix! with {After=4}},line with {SpacingFix=line.SpacingFix! with {Rule=4}},line with {SpacingFix=line.SpacingFix! with {Line=12}}})
            assert(Reject(()=>BatchPlan.Create([bad])),"Forged spacing targets/roles or mixed payloads fail before writes");
        var changed=gap with {SpacingFix=gap.SpacingFix! with {Next=gap.SpacingFix.Next! with {Text="Nội dung kế tiếp đã đổi."}}};
        assert(Reject(()=>CurrentTextPlan.Validate([gap],[changed])) && !RepairDependencies.Same(gap,changed),"Changed neighbor context invalidates a stale gap selection and its dependency identity");
        foreach(var issue in new[]{gap,line})
        {
            var info=IssueReviewCatalog.Describe(issue);var tooltip=IssueTooltipCatalog.Describe(issue);
            assert(info.Section==IssueReviewSection.RequiredFormat && tooltip.Text.Split('\n').Length==3 && tooltip.Basis.Contains("II.6, điểm e"),
                "Gap and line repairs use separate compact required groups with the verified three-part legal tooltip");
        }
        var shortRows=p.ToArray();const string shortText="Nội dung  nội dung đã được duyệt.";
        shortRows[10]=row with {Text=shortText+"\r",FormatEvidence=new(shortText,row.FormatEvidence!.Fonts)};
        Paragraph[] Rebase(IEnumerable<Paragraph> rows)
        {int start=0;return rows.Select(x=>{var rebased=x with {Start=start};start+=x.Text.Length;return rebased;}).ToArray();}
        shortRows=Rebase(shortRows);string shortStory=string.Concat(shortRows.Select(x=>x.Text));
        var shortPlan=BodySpacingRules.Check(shortRows,l,DocumentProfile.State).Where(i=>i.Start==shortRows[10].Start)
            .Concat(Checker.Check([shortRows[10]]).Where(i=>i.CanFix)).ToArray();
        var shortProofs=BodySpacingVerification.Build(shortPlan,shortStory,i=>shortStory[..i.Start]);
        var shortened=shortRows.ToArray();shortened[10]=shortened[10] with {Text="Nội dung đã được duyệt.\r",ParagraphStyle=row.ParagraphStyle! with {Spacing=row.ParagraphStyle.Spacing! with {After=6,Rule=0}}};
        shortened=Rebase(shortened);string newStory=string.Concat(shortened.Select(x=>x.Text));
        BodySpacingVerification.Verify(shortProofs,shortened,x=>newStory[..x.Start]);
        assert(!BodySpacingRules.Check(shortened,l,DocumentProfile.State).Any(i=>i.Start==shortened[10].Start),
            "A selected duplicate deletion may shorten prose below discovery threshold while exact XML/native span evidence still verifies the approved spacing targets");
        Xml(assert);
    }
    private static void Xml(Action<bool,string> assert)
    {
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";const string text="Nội dung đã được kiểm tra đầy đủ.";
        var live=new[]{new WordParagraphSnapshot(100,100+text.Length+1,text+"\r",1,false,false)};
        foreach(var (flag,val,known) in new[]{("contextualSpacing","1",true),("contextualSpacing","0",true),("keepNext","true",true),("pageBreakBefore","off",true),("contextualSpacing","invalid",false)})
        {
            var xml=new XDocument(new XElement(w+"document",new XElement(w+"body",new XElement(w+"p",new XElement(w+"pPr",new XElement(w+flag,new XAttribute(w+"val",val))),new XElement(w+"r",new XElement(w+"t",text))))));
            OutlineParagraphInfo? found=null;WordTextProof.Read(xml.ToString(),live,onOutline:(_,x)=>found=x);
            assert((found?.SpacingContext is not null)==known,"XML spacing flags require known on/off values; malformed data cannot grant spacing permission");
        }
        foreach(string type in new[]{"default","lines","linesAndChars"})
        {
            var xml=new XDocument(new XElement(w+"document",new XElement(w+"body",new XElement(w+"p",new XElement(w+"r",new XElement(w+"t",text))),
                new XElement(w+"sectPr",new XElement(w+"docGrid",new XAttribute(w+"type",type))))));
            OutlineParagraphInfo? found=null;WordTextProof.Read(xml.ToString(),live,onOutline:(_,x)=>found=x);
            assert(found?.SpacingContext?.Grid==(type!="default"),"Active document grids remain an explicit spacing exclusion rather than a guessed baseline measurement");
        }
        foreach(bool overridden in new[]{false,true})
        {
            var properties=new XElement(w+"pPr",new XElement(w+"pStyle",new XAttribute(w+"val","Body")));
            if(overridden)properties.Add(new XElement(w+"contextualSpacing",new XAttribute(w+"val",0)));
            var xml=new XDocument(new XElement(w+"document",new XElement(w+"styles",new XElement(w+"style",new XAttribute(w+"styleId","Body"),new XAttribute(w+"type","paragraph"),
                new XElement(w+"pPr",new XElement(w+"contextualSpacing")))),new XElement(w+"body",new XElement(w+"p",properties,new XElement(w+"r",new XElement(w+"t",text))),
                new XElement(w+"sectPr",new XElement(w+"docGrid",new XAttribute(w+"linePitch",312))))));
            OutlineParagraphInfo? found=null;WordTextProof.Read(xml.ToString(),live,onOutline:(_,x)=>found=x);
            assert(found?.SpacingContext is {Grid:false} context && context.Contextual!=overridden,
                "Inherited contextual spacing and its explicit false override are resolved; a default grid record is not mistaken for an active line grid");
        }
    }
}
