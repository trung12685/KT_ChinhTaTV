using ChinhTa.Core;

public static class PunctuationPreservationChecks
{
    private static readonly ObservedCharacterStyle Plain=new(new("Arial",12,0,0,255),0);
    private static readonly ObservedCharacterStyle Mark=new(new("Times New Roman",14,-1,0,0),1);
    private static readonly ObservedParagraphStyle ParagraphStyle=new(0,0,0,0,new(0,0,4,18,0,0));
    private static CharacterStyleEvidence Evidence(string text,ObservedParagraphStyle? paragraph=null)=>new(text,
        text.Select(c=>char.IsWhiteSpace(c)?null:c is ',' or ';'?Mark:Plain),paragraph??ParagraphStyle);
    private static WordParagraphSnapshot Row(int cp,string text,bool cell=false)=>new(cp,cp+text.Length+1+(cell?1:0),
        text+"\r"+(cell?"\a":""),1,cell,false);
    private static bool Reject(Action action)
    {
        try{action();return false;}catch(InvalidOperationException){return true;}
    }
    private static ObservedCharacterStyle?[] Actual(CharacterStyleEvidence expected)=>expected.Styles
        .Select((style,i)=>char.IsWhiteSpace(expected.Text[i])?null:style??Plain).ToArray();

    public static void Run(Action<bool,string> assert)
    {
        const int cp=720;
        foreach(char mark in new[]{',',';'})
        {
            string text=$"  Nội dung  {mark}báo cáo đã xong.  ";
            var edit=PunctuationSpaceRules.Check(new(cp,text)).Single();
            var row=Row(cp,text,true);var source=Evidence(text);
            var probe=CasePreservationPlan.Regions([edit],[row]).Single();
            string prefix=text[..text.IndexOf("  "+mark,StringComparison.Ordinal)];
            var expected=CasePreservationPlan.Expected(probe,[edit],source,_=>prefix);
            assert(probe.Kind==FixKind.PunctuationSpace && probe.Start==cp && probe.Original==text && probe.Length==text.Length &&
                probe.Original==probe.Replacement && !probe.IsStyleOnly && expected.Text==$"  Nội dung{mark} báo cáo đã xong.  ",
                "A punctuation-only selection creates one read-only whole-paragraph probe including layout spaces and excluding paragraph/cell markers");
            int retained=expected.Text.IndexOf(mark);
            assert(expected.Paragraph==source.Paragraph && expected.Styles[retained]==Mark &&
                expected.Matches(new(expected.Text,Actual(expected),source.Paragraph)),
                "A native prefix at a nonzero Word CP retains the moved comma/semicolon's 14pt bold underline and complete paragraph state");
            foreach(var changed in new[]{Mark with {Underline=0},Mark with {Font=Mark.Font with {Size=13}},
                Mark with {Font=Mark.Font with {Bold=0}},Mark with {Font=Mark.Font with {Italic=-1}},
                Mark with {Font=Mark.Font with {Color=255}},Mark with {Font=Mark.Font with {Name="Arial"}}})
            {
                var actual=Actual(expected);actual[retained]=changed;
                assert(!expected.Matches(new(expected.Text,actual,source.Paragraph)),
                    "The production punctuation-only fingerprint rejects changed underline, legal size, bold, italic, color or font on a retained mark");
            }
            foreach(var changed in new[]{ParagraphStyle with {Alignment=3},ParagraphStyle with {Left=1},
                ParagraphStyle with {Right=1},ParagraphStyle with {First=36},
                ParagraphStyle with {Spacing=ParagraphStyle.Spacing! with {Before=2}},
                ParagraphStyle with {Spacing=ParagraphStyle.Spacing! with {After=6}},
                ParagraphStyle with {Spacing=ParagraphStyle.Spacing! with {Line=19}},
                ParagraphStyle with {Spacing=ParagraphStyle.Spacing! with {Rule=5}},
                ParagraphStyle with {Spacing=ParagraphStyle.Spacing! with {BeforeAuto=-1}},
                ParagraphStyle with {Spacing=ParagraphStyle.Spacing! with {AfterAuto=-1}}})
                assert(!expected.Matches(new(expected.Text,Actual(expected),changed)),
                    "A punctuation-only repair cannot change unselected alignment, indentation, spacing, line rule or automatic spacing");
            assert(Reject(()=>CasePreservationPlan.Expected(probe,[edit],
                new(text,Enumerable.Repeat<ObservedCharacterStyle?>(null,text.Length),ParagraphStyle),_=>prefix)) &&
                Reject(()=>CasePreservationPlan.Expected(probe,[edit],new(text,source.Styles),_=>prefix)) &&
                Reject(()=>CasePreservationPlan.Expected(probe,[edit],new(text,source.Styles,ParagraphStyle with {Spacing=null}),_=>prefix)) &&
                Reject(()=>CasePreservationPlan.Expected(probe,[edit],source,_=>"wrong prefix")),
                "A punctuation-only probe requires complete glyph observations, known paragraph spacing and an exact real Word prefix before mutation");
            AtomicCheck(edit,text,source,expected,cp,assert);
            StagedCheck(edit,text,source,expected,cp,assert);
        }
        GuardChecks(cp,assert);
        MixedTextChecks(cp,assert);
        ExistingOwnerChecks(cp,assert);
    }

    private static void GuardChecks(int cp,Action<bool,string> assert)
    {
        const string text="Nội dung  ,báo cáo đã xong.";
        var edit=PunctuationSpaceRules.Check(new(cp,text)).Single();var row=Row(cp,text);
        assert(Reject(()=>CasePreservationPlan.Regions([edit],[])) &&
            Reject(()=>CasePreservationPlan.Regions([edit],[row,row])),
            "Missing or ambiguous proven native paragraphs cannot create a punctuation retention region");
        var wide=row with {End=row.End+23};
        assert(CasePreservationPlan.Regions([edit],[wide]).Single().Original==text,
            "A native paragraph bound need not have the same width as its UTF-16 text; prefixes establish projection offsets");
        foreach(char control in new[]{'\r','\n','\a','\t','\f','\v'})
            assert(Reject(()=>CasePreservationPlan.Regions([edit],[Row(cp,text+control+"Phần sau")])),
                "An internal paragraph, cell, line, tab or page control cannot enter a new punctuation-only fingerprint");
        var cross=new Issue(row.End-1,3,"  ,",", ","",false,FixKind.PunctuationSpace);
        assert(Reject(()=>CasePreservationPlan.Regions([cross],[row,Row(row.End,"Đoạn tiếp theo.")])),
            "A punctuation span crossing native paragraph bounds is rejected before any write");
        foreach(var malformed in new[]{edit with {Replacement="; "},edit with {Replacement=edit.Original},
            edit with {Length=3,Original=", ;"},edit with {Length=2,Original="\t,"},
            edit with {Length=2,Original=" ,",Replacement=",  "}})
            assert(Reject(()=>CasePreservationPlan.Regions([malformed],[row])),
                "A new C14 retention region requires exactly one retained comma or semicolon surrounded only by ASCII spaces and its approved replacement");
        var crossingText=new Issue(cp-1,2,"xN","x N","",false);
        assert(Reject(()=>
        {
            var selection=new[]{edit,crossingText};
            var probe=CasePreservationPlan.Regions(selection,[row]).Single();
            CasePreservationPlan.Expected(probe,selection,Evidence(text),i=>i==edit?text[..(i.Start-cp)]:"x");
        }),"An approved text edit crossing the punctuation paragraph boundary cannot escape the paragraph projection");
        foreach(var format in new[]{
            new Issue(cp,text.Length,text,text,"",false,FixKind.DocumentHeadingFormat,CharacterFormat:new(13,false,true)),
            new Issue(cp,text.Length,text,text,"",false,FixKind.AgencyFormat,CharacterFormat:new(12,false,true,ComponentRole.Agency)),
            new Issue(cp,text.Length,text,text,"",false,FixKind.RecipientFormat,CharacterFormat:new(11,false,null,ComponentRole.RecipientItem))})
        {
            var selection=new[]{edit,format};int writes=0;
            assert(BatchPlan.Create(selection).Count==2 && Reject(()=>
            {
                var probes=CasePreservationPlan.Regions(selection,[row]);
                foreach(var probe in probes)CasePreservationPlan.Expected(probe,selection,Evidence(text),i=>text[..(i.Start-cp)]);
                AtomicTextBatch.Apply(selection,i=>i.Original,i=>{writes++;return i.Replacement;},()=>{},()=>{});
            }) && writes==0,
                "The first pure-punctuation scope rejects an otherwise batch-compatible untracked heading, agency or recipient patch before mutation");
        }
    }

    private static void MixedTextChecks(int cp,Action<bool,string> assert)
    {
        const string text="Nội dung  kế hoạch kế hoạch  ,  báo cáo;công tác đã  xong.";
        var all=Checker.Check([new(cp,text)]).Where(i=>i.CanFix).ToArray();
        var punctuation=all.Where(i=>i.Kind==FixKind.PunctuationSpace).ToArray();
        var row=Row(cp,text);var source=Evidence(text);
        var probes=CasePreservationPlan.Regions(punctuation,[row]);
        var expected=CasePreservationPlan.Expected(probes.Single(),punctuation,source,i=>text[..(i.Start-cp)]);
        assert(punctuation.Length==2 && probes.Count==1 &&
            expected.Text=="Nội dung  kế hoạch kế hoạch, báo cáo; công tác đã  xong.",
            "Multiple C14 edits in one proven paragraph share one probe and preserve every unselected repeated phrase and C01 space");
        var combinedProbe=CasePreservationPlan.Regions(all,[row]).Single();
        var combined=CasePreservationPlan.Expected(combinedProbe,all,source,i=>text[..(i.Start-cp)]);
        var projected=source.Project(all,i=>text[..(i.Start-cp)]);
        assert(all.Any(i=>i.Kind==FixKind.Text) && all.Any(i=>i.Kind==FixKind.RepeatedText) &&
            combined.Text=="Nội dung kế hoạch, báo cáo; công tác đã xong." &&
            combined.Styles.SequenceEqual(projected.Styles) && combined.Paragraph==source.Paragraph &&
            combined.Styles[combined.Text.IndexOf(',')]==Mark && combined.Styles[combined.Text.IndexOf(';')]==Mark,
            "A pure punctuation trigger composes with approved C01 and repetition edits using the existing retained-source projection");
        var reversed=CasePreservationPlan.Expected(CasePreservationPlan.Regions(all.Reverse().ToArray(),[row]).Single(),
            all.Reverse().ToArray(),source,i=>text[..(i.Start-cp)]);
        assert(combined.Matches(reversed) && reversed.Matches(combined),
            "Checkbox order does not change a punctuation and other-text retention fingerprint");
    }

    private static void ExistingOwnerChecks(int cp,Action<bool,string> assert)
    {
        const string text="Nội dung  ,báo cáo đã xong.";
        var edit=PunctuationSpaceRules.Check(new(cp,text)).Single();
        var paragraph=ParagraphStyle with {Spacing=ParagraphStyle.Spacing! with {Rule=2,Line=24}};
        var body=new Issue(cp,text.Length,text,text,"",false,FixKind.BodyFormat,
            CharacterFormat:new(13,false,null,ComponentRole.Body,PreserveSize:true,RepairInvalidSizes:true,PreserveEmphasis:true));
        var layout=new Issue(cp,text.Length,text,text,"",false,FixKind.BodyParagraph,ParagraphFix:new(paragraph,3,36));
        var gap=new Issue(cp,text.Length,text,text,"",false,FixKind.BodyGap,SpacingFix:new(paragraph,new(),new(1,1),
            new(cp+text.Length+100,"Đoạn liền sau.",paragraph,new(),new(1,1)),After:6));
        var line=new Issue(cp,text.Length,text,text,"",false,FixKind.BodyLineSpacing,SpacingFix:new(paragraph,new(),new(1,1),Rule:0));
        var bodyPatches=new[]{body,layout,gap,line};
        foreach(var patches in bodyPatches.Select(p=>new[]{p}).Append(bodyPatches))
        foreach(bool reverse in new[]{false,true})
        {
            var selection=patches.Append(edit).ToArray();if(reverse)selection=selection.Reverse().ToArray();
            var probe=CasePreservationPlan.Regions(selection,[Row(cp,text)]).Single();var source=Evidence(text,paragraph);
            var expected=CasePreservationPlan.Expected(probe,selection,source,i=>text[..(i.Start-cp)]);
            var projected=source;
            foreach(var patch in BatchPlan.Create(patches))projected=patch.CharacterFormat is { } font?projected.WithFormat(font):
                patch.ParagraphFix is { } fix?projected.WithParagraph(fix,paragraph):projected.WithSpacing(patch.SpacingFix!,paragraph);
            projected=projected.Project([edit],i=>text[..(i.Start-cp)]);
            assert(bodyPatches.Any(p=>p.Kind==probe.Kind) && probe.Kind!=FixKind.PunctuationSpace &&
                expected.Matches(projected) && projected.Matches(expected),
                "Existing body font, paragraph, gap and line owners retain one authoritative probe in either selection order");
        }
        const string title="Báo cáo  ,nội dung.";
        var titleEdit=PunctuationSpaceRules.Check(new(cp,title)).Single();
        var upper=new Issue(cp,7,"Báo cáo","BÁO CÁO","",false,FixKind.DocumentTypeText);
        foreach(bool reverse in new[]{false,true})
        {
            var selection=reverse?new[]{titleEdit,upper}:new[]{upper,titleEdit};
            var probe=CasePreservationPlan.Regions(selection,[Row(cp,title)]).Single();
            var expected=CasePreservationPlan.Expected(probe,selection,Evidence(title),i=>title[..(i.Start-cp)]);
            assert(probe.Kind==FixKind.DocumentTypeText && expected.Text=="BÁO CÁO, nội dung." &&
                expected.Styles[expected.Text.IndexOf(',')]==Mark,
                "A DocumentTypeText owner keeps the existing case and punctuation projection regardless of checkbox order");
        }
        foreach(string signature in new[]{text,text+"\vĐại tá Nguyễn Văn An"})
        foreach(bool reverse in new[]{false,true})
        {
            var punctuation=PunctuationSpaceRules.Check(new(cp,signature)).Single();
            var format=new Issue(cp,signature.Length,signature,signature,"",false,FixKind.SignatureFormat,
                CharacterFormat:new(13,false,true,ComponentRole.SignatureName,PreserveSize:true,RepairInvalidSizes:true));
            var selection=reverse?new[]{punctuation,format}:new[]{format,punctuation};
            var probe=CasePreservationPlan.Regions(selection,[Row(cp,signature,true)]).Single();
            var source=Evidence(signature);var expected=CasePreservationPlan.Expected(probe,selection,source,i=>signature[..(i.Start-cp)]);
            var projected=source.WithFormat(format.CharacterFormat!).Project([punctuation],i=>signature[..(i.Start-cp)]);
            assert(probe.Kind==FixKind.SignatureFormat && expected.Matches(projected) && projected.Matches(expected) &&
                expected.Paragraph==source.Paragraph && expected.Text.Contains('\v')==signature.Contains('\v'),
                "An existing signature owner wins before pure-control restrictions and retains its permitted soft break in either selection order");
        }
        string longText=text+new string('x',12000);
        var longEdit=edit;var longBody=body with {Length=longText.Length,Original=longText,Replacement=longText};
        var longSelection=new[]{longEdit,longBody};var longProbe=CasePreservationPlan.Regions(longSelection,[Row(cp,longText)]).Single();
        var longExpected=CasePreservationPlan.Expected(longProbe,longSelection,Evidence(longText,paragraph),i=>longText[..(i.Start-cp)]);
        assert(longProbe.Kind==FixKind.BodyFormat && longProbe.Length==longText.Length &&
            longExpected.Text.Length==longText.Length+edit.Replacement.Length-edit.Length,
            "Adding pure punctuation tracking leaves existing full-paragraph owners usable without a new paragraph-length cap");
    }

    private static void AtomicCheck(Issue edit,string text,CharacterStyleEvidence source,CharacterStyleEvidence expected,int cp,Action<bool,string> assert)
    {
        foreach(bool drift in new[]{false,true})
        {
            string current=text;var actual=source.Styles.ToArray();int writes=0,saves=0,commits=0,undos=0,restores=0;
            bool failed=false;
            try
            {
                AtomicTextBatch.Apply([edit],i=>current.Substring(i.Start-cp,i.Length),i=>
                {
                    writes++;current=current.Remove(i.Start-cp,i.Length).Insert(i.Start-cp,i.Replacement);
                    actual=Actual(expected);if(drift)actual[current.IndexOf(',')>=0?current.IndexOf(','):current.IndexOf(';')]=Plain;
                    return i.Replacement;
                },()=>
                {
                    saves++;
                    if(!expected.Matches(new(current,actual,source.Paragraph)))throw new InvalidOperationException("Synthetic immediate style drift");
                    commits++;undos++;
                },()=>{restores++;current=text;actual=source.Styles.ToArray();});
            }
            catch(InvalidOperationException){failed=true;}
            assert(drift?failed && current==text && actual.SequenceEqual(source.Styles) && writes==1 && saves==1 && commits==0 && undos==0 && restores==1:
                !failed && current==expected.Text && writes==1 && saves==1 && commits==1 && undos==1 && restores==0,
                "Actual AtomicTextBatch commits one punctuation action and Undo only after its source-style proof, or restores once on immediate drift");
        }
    }

    private static void StagedCheck(Issue edit,string text,CharacterStyleEvidence source,CharacterStyleEvidence expected,int cp,Action<bool,string> assert)
    {
        foreach(bool drift in new[]{false,true})
        {
            string current=text;var actual=source.Styles.ToArray();int stages=0,commits=0,undos=0,restores=0,innerRestores=0;
            bool failed=false;
            var line=new Issue(cp+text.Length+100,4,"MỐC!","MỐC!","",false,FixKind.ShapeLine,LineFix:new(null,100,100,90,LineRole.Subject));
            var requests=new[]{new DeferredRepair(edit,edit.Replacement),new(line,line.Original)};
            try
            {
                var result=StagedRepairBatch.Apply(requests,_=>{},request=>request.Requested,items=>
                {
                    stages++;
                    if(StagedRepairBatch.Stage(items[0])==RepairStage.Line)
                    {if(drift)actual[current.IndexOf(',')>=0?current.IndexOf(','):current.IndexOf(';')]=Plain;return;}
                    AtomicTextBatch.Apply(items,i=>current.Substring(i.Start-cp,i.Length),i=>
                    {
                        current=current.Remove(i.Start-cp,i.Length).Insert(i.Start-cp,i.Replacement);
                        actual=Actual(expected);return i.Replacement;
                    },()=>
                    {
                        if(!expected.Matches(new(current,actual,source.Paragraph)))throw new InvalidOperationException("Synthetic text-stage proof failure");
                    },()=>{innerRestores++;current=text;actual=source.Styles.ToArray();});
                },()=>
                {
                    if(!expected.Matches(new(current,actual,source.Paragraph)))throw new InvalidOperationException("Synthetic late-stage style drift");
                },()=>{commits++;undos++;},()=>{restores++;current=text;actual=source.Styles.ToArray();});
                assert(result.Selected==2 && result.Applied==2 && result.AlreadySatisfied==0,
                    "The successful punctuation and geometry orchestration accounts for both approved stages");
            }
            catch(InvalidOperationException){failed=true;}
            assert(drift?failed && current==text && actual.SequenceEqual(source.Styles) && stages==2 && commits==0 && undos==0 && restores==1 && innerRestores==0:
                !failed && current==expected.Text && stages==2 && commits==1 && undos==1 && restores==0 && innerRestores==0,
                "Actual StagedRepairBatch publishes one commit and Undo after the late punctuation proof, or restores the outer transaction once on later drift");
        }
    }
}
