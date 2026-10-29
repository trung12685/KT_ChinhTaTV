using ChinhTa.Core;

public static class InsertedMarkPreservationChecks
{
    private static readonly ObservedCharacterStyle Plain=new(new("Arial",12,0,0,255),0);
    private static readonly ObservedCharacterStyle Mark=new(new("Times New Roman",14,-1,0,0),1);
    private static readonly ObservedParagraphStyle Layout=new(0,0,0,0,new(0,0,4,18,0,0));
    private static CharacterStyleEvidence Evidence(string text)=>new(text,
        text.Select(c=>char.IsWhiteSpace(c)?null:char.IsPunctuation(c)?Mark:Plain),Layout);
    private static WordParagraphSnapshot Row(int cp,string text)=>new(cp,cp+text.Length+2,text+"\r\a",1,true,false);
    private static ObservedCharacterStyle?[] Actual(CharacterStyleEvidence expected)=>expected.Styles
        .Select((style,index)=>char.IsWhiteSpace(expected.Text[index])?null:style??Plain).ToArray();
    private static bool Reject(Action action)
    {try{action();return false;}catch(InvalidOperationException){return true;}}

    public static void Run(Action<bool,string> assert)
    {
        const int cp=417;
        var samples=new (string Text,FixKind Kind,string Original,string Replacement)[]
        {
            ("Nội dung:báo cáo đã xong.",FixKind.ColonSpace,":",": "),
            ("Đã hoàn thành.Cần làm tiếp.",FixKind.SentenceAfterSpace,".",". "),
            ("Đã hoàn thành?Cần làm tiếp.",FixKind.SentenceAfterSpace,"?","? "),
            ("Đã hoàn thành!Cần làm tiếp.",FixKind.SentenceAfterSpace,"!","! "),
            ("Theo mẫu(nội dung) đã duyệt.",FixKind.ParenthesisSpace,"("," ("),
            ("Theo mẫu (nội dung)đã duyệt.",FixKind.ParenthesisSpace,")",") "),
            ("Theo mẫu“nội dung” đã duyệt.",FixKind.QuoteSpace,"“"," “"),
            ("Theo mẫu “nội dung”đã duyệt.",FixKind.QuoteSpace,"”","” "),
            ("Theo mẫu\"nội dung\" đã duyệt.",FixKind.QuoteSpace,"\""," \""),
            ("Theo mẫu \"nội dung\"đã duyệt.",FixKind.QuoteSpace,"\"","\" ")
        };
        foreach(var sample in samples)
        {
            string text="😀 "+sample.Text;
            var edit=Checker.Check([new(cp,text)]).Single(i=>i.CanFix && i.Kind==sample.Kind &&
                i.Original==sample.Original && i.Replacement==sample.Replacement);
            var probe=CasePreservationPlan.Regions([edit],[Row(cp,text)]).Single();
            var source=Evidence(text);
            var expected=CasePreservationPlan.Expected(probe,[edit],source,i=>text[..(i.Start-cp)]);
            int mark=edit.Start-cp+(edit.Replacement.StartsWith(' ')?1:0);
            assert(probe.Kind==FixKind.PunctuationSpace && probe.Start==cp && probe.Original==text &&
                probe.Original==probe.Replacement && probe.Length==text.Length && !probe.IsStyleOnly &&
                expected.Text==AtomicTextBatch.ExpectedStory([edit],text,i=>text[..(i.Start-cp)]),
                "A real checker mark-anchored gap edit creates one read-only whole-paragraph fingerprint at its nonzero source CP");
            assert(expected.Styles[mark]==Mark && expected.Text[mark].ToString()==sample.Original &&
                expected.Paragraph==Layout && expected.Matches(new(expected.Text,Actual(expected),Layout)),
                "The original colon, sentence mark, parenthesis or Unicode/straight quote retains its own 14pt bold underline through a verified prefix projection");
            foreach(var drift in new[]{Mark with {Underline=0},Mark with {Font=Mark.Font with {Bold=0}},
                Mark with {Font=Mark.Font with {Italic=-1}},Mark with {Font=Mark.Font with {Size=13}},
                Mark with {Font=Mark.Font with {Name="Arial"}},Mark with {Font=Mark.Font with {Color=255}}})
            {
                var actual=Actual(expected);actual[mark]=drift;
                assert(!expected.Matches(new(expected.Text,actual,Layout)),
                    "Correct inserted gap text cannot certify a retained punctuation glyph with changed underline, emphasis, size, font or color");
            }
            foreach(var drift in new[]{Layout with {Alignment=3},Layout with {First=36},
                Layout with {Spacing=Layout.Spacing! with {Before=6}},Layout with {Spacing=Layout.Spacing! with {Rule=0,Line=12}}})
                assert(!expected.Matches(new(expected.Text,Actual(expected),drift)),
                    "A mark-anchored insertion cannot alter unselected alignment, indentation, paragraph spacing or line rule");
            var missing=source.Styles.ToArray();missing[edit.Start-cp]=null;
            assert(Reject(()=>CasePreservationPlan.Expected(probe,[edit],new(text,missing,Layout),i=>text[..(i.Start-cp)])) &&
                Reject(()=>CasePreservationPlan.Expected(probe,[edit],new(text,source.Styles),i=>text[..(i.Start-cp)])) &&
                Reject(()=>CasePreservationPlan.Expected(probe,[edit],new(text,source.Styles,Layout with {Spacing=null}),i=>text[..(i.Start-cp)])) &&
                Reject(()=>CasePreservationPlan.Expected(probe,[edit],source,_=>"wrong native prefix")),
                "Insertion fingerprints need complete source glyphs, known paragraph spacing and exact native prefixes before writing");
            AtomicCheck(edit,text,source,expected,mark,cp,assert);
            foreach(var profile in Enum.GetValues<DocumentProfile>())
            {
                var current=Checker.Check([new(cp,text+"\r\a")],profile).Single(i=>i.CanFix && i.Kind==sample.Kind &&
                    i.Original==sample.Original && i.Replacement==sample.Replacement);
                assert(CasePreservationPlan.Regions([current],[Row(cp,text)]).Single()==probe,
                    "Every document profile uses the same existing retained-mark guard without including paragraph or cell terminators");
            }
        }
        CombinedChecks(cp,assert);
        GuardChecks(cp,assert);
        LegacyOwnerChecks(cp,assert);
    }

    private static void CombinedChecks(int cp,Action<bool,string> assert)
    {
        foreach(var (text,fixedText) in new[]{
            ("Nội dung  :báo cáo đã xong.","Nội dung: báo cáo đã xong."),
            ("Đã hoàn thành  .Cần làm tiếp.","Đã hoàn thành. Cần làm tiếp."),
            ("Theo mẫu( nội dung )đã duyệt.","Theo mẫu (nội dung) đã duyệt."),
            ("Theo mẫu“ nội dung ”đã duyệt.","Theo mẫu “nội dung” đã duyệt."),
            ("Theo mẫu\" nội dung \"đã duyệt.","Theo mẫu \"nội dung\" đã duyệt."),
            ("Nội dung:báo cáo kế hoạch kế hoạch đã xong.","Nội dung: báo cáo kế hoạch đã xong."),
            ("Nội dung  ,báo cáo đã xong.Cần làm tiếp.","Nội dung, báo cáo đã xong. Cần làm tiếp.")})
        {
            var edits=Checker.Check([new(cp,text)]).Where(i=>i.CanFix).ToArray();
            var source=Evidence(text);var probe=CasePreservationPlan.Regions(edits,[Row(cp,text)]).Single();
            var expected=CasePreservationPlan.Expected(probe,edits,source,i=>text[..(i.Start-cp)]);
            var reverse=CasePreservationPlan.Expected(CasePreservationPlan.Regions(edits.Reverse().ToArray(),[Row(cp,text)]).Single(),
                edits.Reverse().ToArray(),source,i=>text[..(i.Start-cp)]);
            assert(edits.Length>=2 && expected.Text==fixedText && expected.Matches(reverse) && reverse.Matches(expected) &&
                expected.Styles.Where((_,index)=>char.IsPunctuation(expected.Text[index])).All(style=>style==Mark),
                "Paired adjacent deletion/insertion, multiple delimiter triggers and C14/repetition companions share one source-exact probe independent of checkbox order");
        }
        foreach(string text in new[]{"Nội dung  : báo cáo đã xong.","Đã hoàn thành  . Cần làm tiếp.",
            "Theo mẫu ( nội dung ) đã duyệt.","Theo mẫu “ nội dung ” đã duyệt."})
        {
            var deletions=Checker.Check([new(cp,text)]).Where(i=>i.CanFix).ToArray();
            assert(deletions.Length>0 && deletions.All(i=>i.Original.All(c=>c==' ') && i.Replacement.Length==0) &&
                CasePreservationPlan.Regions(deletions,[Row(cp,text)]).Count==0,
                "Spaces-only deletions keep their existing behavior and do not invent a retained-mark insertion fingerprint");
        }
    }

    private static void GuardChecks(int cp,Action<bool,string> assert)
    {
        const string text="Nội dung:báo cáo đã xong.";
        var edit=Checker.Check([new(cp,text)]).Single(i=>i.CanFix);var row=Row(cp,text);
        assert(Reject(()=>CasePreservationPlan.Regions([edit],[])) &&
            Reject(()=>CasePreservationPlan.Regions([edit],[row,row])),
            "A new insertion fingerprint requires exactly one already proven source paragraph");
        foreach(char control in new[]{'\t','\r','\n','\a','\v','\f'})
            assert(Reject(()=>CasePreservationPlan.Regions([edit],[Row(cp,text+control+"Phần tiếp")])),
                "Mark insertion tracking cannot expand through internal Word, line, page or tab controls");
        foreach(var malformed in new[]{edit with {Replacement=":  "},edit with {Replacement="; "},
            edit with {Length=2,Original="::",Replacement=": "},edit with {Length=2,Original=" :"},
            edit with {Length=2,Original="\t:"},edit with {Prerequisites=Array.Empty<Issue>()},
            edit with {Kind=FixKind.SentenceAfterSpace,Original=".",Replacement=" ."},
            edit with {Kind=FixKind.ParenthesisSpace,Original=")",Replacement=" )"},
            edit with {Kind=FixKind.QuoteSpace,Original="“",Replacement="“ "}})
            assert(Reject(()=>CasePreservationPlan.Regions([malformed],[row])),
                "Only the exact approved single original mark and one ASCII gap in its allowed direction enter insertion tracking");
        var heading=new Issue(cp,text.Length,text,text,"",false,FixKind.DocumentHeadingFormat,CharacterFormat:new(13,false,true));
        int writes=0;
        assert(Reject(()=>
        {
            var selected=new[]{edit,heading};var probes=CasePreservationPlan.Regions(selected,[row]);
            foreach(var probe in probes)CasePreservationPlan.Expected(probe,selected,Evidence(text),i=>text[..(i.Start-cp)]);
            AtomicTextBatch.Apply(selected,i=>i.Original,i=>{writes++;return i.Replacement;},()=>{},()=>{});
        }) && writes==0,"An unsupported untracked heading combination is rejected before a mark insertion writes anything");
    }

    private static void LegacyOwnerChecks(int cp,Action<bool,string> assert)
    {
        foreach(string text in new[]{"Nội dung:báo cáo đã xong.","Đã hoàn thành.Cần làm tiếp.",
            "Theo mẫu(nội dung)đã duyệt.","Theo mẫu“nội dung”đã duyệt."})
        {
            var edits=Checker.Check([new(cp,text)]).Where(i=>i.CanFix).ToArray();
            var body=new Issue(cp,text.Length,text,text,"",false,FixKind.BodyFormat,
                CharacterFormat:new(13,false,null,ComponentRole.Body,PreserveSize:true,RepairInvalidSizes:true,PreserveEmphasis:true));
            var selection=edits.Append(body).ToArray();var source=Evidence(text);
            var probe=CasePreservationPlan.Regions(selection,[Row(cp,text)]).Single();
            var expected=CasePreservationPlan.Expected(probe,selection,source,i=>text[..(i.Start-cp)]);
            var legacy=source.WithFormat(body.CharacterFormat!).Project(edits,i=>text[..(i.Start-cp)]);
            assert(probe.Kind==FixKind.BodyFormat && expected.Matches(legacy) && legacy.Matches(expected) &&
                expected.Paragraph==Layout && expected.Styles.Where((_,index)=>char.IsPunctuation(expected.Text[index]))
                    .All(style=>style is {Underline:1,Font:{Size:14,Bold:-1}}),
                "Existing body ownership wins before new insertion probes and applies only its approved font patch while retaining mark emphasis, size and paragraph state");
        }
    }

    private static void AtomicCheck(Issue edit,string text,CharacterStyleEvidence source,CharacterStyleEvidence expected,int mark,int cp,Action<bool,string> assert)
    {
        foreach(bool drift in new[]{false,true})
        {
            string current=text;var styles=source.Styles.ToArray();int commits=0,undos=0,restores=0;bool rejected=false;
            try
            {
                AtomicTextBatch.Apply([edit],i=>current.Substring(i.Start-cp,i.Length),i=>
                {
                    current=current.Remove(i.Start-cp,i.Length).Insert(i.Start-cp,i.Replacement);
                    styles=Actual(expected);if(drift)styles[mark]=Plain;return i.Replacement;
                },()=>
                {
                    if(!expected.Matches(new(current,styles,Layout)))throw new InvalidOperationException("Synthetic retained mark lost its source style");
                    commits++;undos++;
                },()=>{current=text;styles=source.Styles.ToArray();restores++;});
            }
            catch(InvalidOperationException){rejected=true;}
            assert(drift?rejected && commits==0 && undos==0 && restores==1 && current==text && styles.SequenceEqual(source.Styles):
                !rejected && commits==1 && undos==1 && restores==0 && current==expected.Text,
                "Actual AtomicTextBatch commits a single mark insertion only after its source-style postcondition or rolls back once with no published Undo on drift");
        }
    }
}
