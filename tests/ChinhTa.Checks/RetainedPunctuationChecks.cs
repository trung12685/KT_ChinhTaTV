using ChinhTa.Core;

public static class RetainedPunctuationChecks
{
    private static readonly ObservedCharacterStyle Plain=new(new("Times New Roman",13,0,0,0),0);
    private static readonly ObservedCharacterStyle Mark=new(new("Times New Roman",14,-1,0,0),1);
    private static CharacterStyleEvidence Evidence(string text,ObservedParagraphStyle? paragraph=null)=>new(text,
        text.Select(c=>char.IsWhiteSpace(c)?null:c is ',' or ';'?Mark:Plain),paragraph);

    public static void Run(Action<bool,string> assert)
    {
        foreach(char mark in new[]{',',';'})
        {
            string text=$"Nội dung  {mark}báo cáo đã xong.";
            const int cp=700;
            var edit=PunctuationSpaceRules.Check(new(cp,text)).Single();
            var source=Evidence(text);var expected=source.Project([edit],i=>text[..(i.Start-cp)]);
            int retained=expected.Text.IndexOf(mark);
            var sizes=new FontSizeEvidence(text,source.Styles.Select(s=>(float?)s?.Font.Size));
            var expectedSizes=sizes.Project([edit],i=>text[..(i.Start-cp)]);
            assert(edit.Original==$"  {mark}" && edit.Replacement==$"{mark} " && expected.Styles[retained]==Mark &&
                expectedSizes.Sizes[retained]==14 && expected.Text==$"Nội dung{mark} báo cáo đã xong.",
                "A real combined punctuation-space proposal retains the unique moved source mark and its exact style/size at a nonzero Word CP");
            var actual=expected.Styles.Select((style,i)=>char.IsWhiteSpace(expected.Text[i])?null:style??Plain).ToArray();
            assert(expected.Matches(new(expected.Text,actual)) && expectedSizes.Matches(new(expected.Text,actual.Select(s=>(float?)s?.Font.Size))),
                "Spacing-only movement accepts the original comma/semicolon style without inventing a source style for inserted whitespace");
            foreach(var changed in new[]{Mark with {Underline=0},Mark with {Font=Mark.Font with {Size=13}},
                Mark with {Font=Mark.Font with {Bold=0}},Mark with {Font=Mark.Font with {Italic=-1}},
                Mark with {Font=Mark.Font with {Color=255}},Mark with {Font=Mark.Font with {Name="Arial"}}})
            {
                var drift=actual.ToArray();drift[retained]=changed;
                assert(!expected.Matches(new(expected.Text,drift)),
                    "An unchanged punctuation glyph cannot silently lose unselected underline, size, bold, italic, color or font during a spacing repair");
            }
            var flattened=actual.Select(s=>(float?)s?.Font.Size).ToArray();flattened[retained]=13;
            assert(!expectedSizes.Matches(new(expected.Text,flattened)),
                "The retained-size fingerprint independently rejects flattening an unchanged 14pt punctuation glyph to legal 13pt");
            CombinedSpacingStageCheck(text,edit,assert);
        }
        foreach(var sample in new[]{("  ,","; "),("  ",", "),(" , ,",", "),("\t,",", "),("x ,",", x ")})
        {
            var source=Evidence(sample.Item1);
            var edit=new Issue(40,sample.Item1.Length,sample.Item1,sample.Item2,"",false);
            var projected=source.Project([edit],_=>"");
            assert(projected.Styles.Select((style,i)=>!char.IsPunctuation(projected.Text[i]) || style is null).All(v=>v),
                "Changed/new marks, ambiguous multiple marks, tabs and complex text replacements never gain guessed punctuation source evidence");
        }
    }

    private static void CombinedSpacingStageCheck(string text,Issue edit,Action<bool,string> assert)
    {
        const int cp=700;
        var paragraph=new ObservedParagraphStyle(0,0,0,0,new(0,0,2,24,0,0));
        var spacing=new Issue(cp,text.Length,text,text,"",false,FixKind.BodyLineSpacing,
            SpacingFix:new(paragraph,new(),new(1,1),Rule:0));
        var plan=BatchPlan.Create([edit,spacing]);
        var probe=CasePreservationPlan.Regions(plan,[new(cp,cp+text.Length+1,text+"\r",1,false,true)]).Single();
        var source=Evidence(text,paragraph);
        var expected=CasePreservationPlan.Expected(probe,plan,source,i=>text[..(i.Start-cp)]);
        int mark=expected.Text.IndexOf(text.Contains(',')?',':';');
        assert(expected.Styles[mark]==Mark && expected.Paragraph?.Spacing?.Rule==0 && expected.Paragraph.Alignment==paragraph.Alignment,
            "The production whole-paragraph case/style plan retains punctuation while applying only the approved line-spacing patch");
        foreach(bool drift in new[]{false,true})
        {
            string current=text;ObservedParagraphStyle currentParagraph=paragraph;
            var actual=source.Styles.ToArray();int commits=0,restores=0,innerRestores=0;bool failed=false;
            var line=new Issue(1400,4,"MỐC!","MỐC!","",false,FixKind.ShapeLine,LineFix:new(null,100,100,90,LineRole.Subject));
            var requests=new[]{new DeferredRepair(edit,edit.Replacement),new(spacing,spacing.Replacement),new(line,line.Original)};
            try
            {
                StagedRepairBatch.Apply(requests,_=>{},request=>request.Requested,items=>
                {
                    if(StagedRepairBatch.Stage(items[0])==RepairStage.Line)
                    {if(drift)actual[mark]=Plain;return;}
                    AtomicTextBatch.Apply(items,i=>current.Substring(i.Start-cp,i.Length),i=>
                    {
                        if(i.SpacingFix is { } fix)currentParagraph=fix.Apply(currentParagraph);
                        else current=current.Remove(i.Start-cp,i.Length).Insert(i.Start-cp,i.Replacement);
                        return i.Replacement;
                    },()=>
                    {
                        actual=expected.Styles.Select((s,i)=>char.IsWhiteSpace(expected.Text[i])?null:s??Plain).ToArray();
                        if(current!=expected.Text || !expected.Matches(new(current,actual,currentParagraph)))throw new InvalidOperationException("Text-stage proof failed");
                    },()=>{innerRestores++;current=text;currentParagraph=paragraph;});
                },()=>
                {
                    if(!expected.Matches(new(current,actual,currentParagraph)))throw new InvalidOperationException("Final retained-punctuation proof failed");
                },()=>commits++,()=>{restores++;current=text;currentParagraph=paragraph;actual=source.Styles.ToArray();});
            }
            catch(InvalidOperationException){failed=true;}
            assert(drift?failed && commits==0 && restores==1 && innerRestores==0 && current==text && currentParagraph==paragraph:
                !failed && commits==1 && restores==0 && innerRestores==0 && current==expected.Text && currentParagraph.Spacing?.Rule==0,
                "Actual staged/atomic text orchestration commits one approved group only when the moved punctuation style survives the later geometry stage");
        }
    }
}
