using ChinhTa.Core;

public static class DelimiterClusterChecks
{
    public static void Run(Action<bool,string> assert)
    {
        string Apply(string text,IEnumerable<Issue>? selected=null)=>AtomicTextBatch.ExpectedStory(
            BatchPlan.Create(selected??Checker.Check([new(0,text)]).Where(i=>i.CanFix)),text,i=>text[..i.Start]);
        bool Delimiter(Issue i)=>i.Kind is FixKind.ParenthesisSpace or FixKind.QuoteSpace;
        foreach(var pair in new[]{('(',')'),('“','”'),('"','"')})
        foreach(string beforeGap in new[]{""," "})
        foreach(string firstGap in new[]{""," ","   "})
        foreach(string lastGap in new[]{"","  "})
        foreach(string afterGap in new[]{""," "})
        {
            string before="Theo mẫu"+beforeGap+pair.Item1+firstGap+"nội dung"+lastGap+pair.Item2+afterGap+"đã duyệt.";
            string after="Theo mẫu "+pair.Item1+"nội dung"+pair.Item2+" đã duyệt.";
            var choices=Checker.Check([new(0,before)]).Where(i=>i.CanFix).ToArray();
            assert(Apply(before,choices)==after,"All 72 supported before/inside/after combinations normalize in one selected transaction");
            assert(!Checker.Check([new(0,after)]).Any(i=>i.CanFix),"Every normalized combination has no stale or newly exposed delimiter finding");
            string partial=Apply(before,choices.Take(1));
            assert(Apply(partial)==after,"Repairing any first proposed edge still leaves the remaining supported corrections discoverable");
        }
        foreach(var (before,after) in new[]{
            ("Theo mẫu( nội dung )đã duyệt.","Theo mẫu (nội dung) đã duyệt."),
            ("Theo mẫu“ nội dung ”đã duyệt.","Theo mẫu “nội dung” đã duyệt."),
            ("Theo mẫu\" nội dung \"đã duyệt.","Theo mẫu \"nội dung\" đã duyệt."),
            ("Theo mẫu(nội dung) ,báo cáo.","Theo mẫu (nội dung), báo cáo."),
            ("Theo mẫu“nội dung” ,báo cáo.","Theo mẫu “nội dung”, báo cáo."),
            ("Theo mẫu\"nội dung\" ,báo cáo.","Theo mẫu \"nội dung\", báo cáo."),
            ("Theo mẫu \" nội,dung \" đã duyệt.","Theo mẫu \"nội, dung\" đã duyệt."),
            ("Theo mẫu \"nội,dung.\" đã ghi.","Theo mẫu \"nội, dung.\" đã ghi."),
            ("( nội dung. )","(nội dung.)"),("( Đã hoàn thành! )","(Đã hoàn thành!)"),
            ("Theo mẫu( Đã hoàn thành. ) đã ghi.","Theo mẫu (Đã hoàn thành.) đã ghi."),
            ("Theo mẫu ( nội dung,báo cáo. ) đã ghi.","Theo mẫu (nội dung, báo cáo.) đã ghi."),
            ("Theo mẫu (nội dung,báo cáo.) đã ghi.","Theo mẫu (nội dung, báo cáo.) đã ghi."),
            ("😀 Theo ma\u0302\u0303u\" no\u0323\u0302i dung \"đã duyệt.\r\a","😀 Theo ma\u0302\u0303u \"no\u0323\u0302i dung\" đã duyệt.\r\a"),
            ("Theo mẫu( nội dung nội dung )đã  duyệt; theo mẫu\" kế hoạch \"cần làm.","Theo mẫu (nội dung) đã duyệt; theo mẫu \"kế hoạch\" cần làm.")})
        {
            var issues=Checker.Check([new(0,before)]).Where(i=>i.CanFix).ToArray();
            assert(issues.Length>0 && issues.All(i=>i.Length>0 && before.Substring(i.Start,i.Length)==i.Original) && Apply(before)==after,
                "The delimiter cluster repairs supported before/inside/after/straight-quote/terminal cases together without changing delimiters or words: "+before+" => "+Apply(before));
            assert(!Checker.Check([new(0,after)]).Any(i=>i.CanFix),"One selected cluster batch leaves no repaired or newly exposed actionable rows");
        }
        foreach(string text in new[]{"fn( nội dung )","Tính sin(alpha beta) đã duyệt.","Mẫu(nội dung) đã duyệt.","Theo Mẫu(nội dung) đã duyệt.",
            "Theo mẫu(12 34) đã duyệt.","Theo mẫu(NỘI DUNG) đã duyệt.","Theo mẫu(x y) đã duyệt.",
            "Theo mẫu\"nội dung\"Đã duyệt.","Theo mẫu\"nội dung\"đã", "Theo mẫu\"nội dung\"đã duyệt.docx",
            "Theo mẫu \" nội dung ” đã duyệt.","Theo mẫu \" nội dung \" và “ kế hoạch ” đã ghi.",
            "Theo mẫu \" nội dung \" còn thiếu \"","Theo mẫu( nội dung (bổ sung) ) đã duyệt.",
            "Theo mẫu\"nội dung \"bổ sung\" kết quả\"đã duyệt.",
            "( nội dung... )","( nội dung?! )","( nội dung . )","( Nội dung TP. )","( nội dung tp. )",
            "( Nội dung. Tiếp tục báo cáo. )","Theo mẫu( nội\tdung ) đã duyệt.","Theo mẫu( nội dung\u00a0) đã duyệt.",
            "Theo mẫu\"host/name\" đã duyệt.","Gọi fn( alpha,beta ) đã ghi.","x = \" nội dung \"", "12\" đã ghi.",
            "Số: Theo mẫu(nội dung) đã duyệt.","Hà Nội, ngày 03 tháng 10 năm 2026 theo mẫu(nội dung) đã ghi."})
            assert(!Checker.Check([new(0,text)]).Any(i=>i.CanFix && Delimiter(i)),
                "Ambiguous function/code/numeric/uppercase/blank/mixed/nested/unbalanced/date/control/terminal forms cannot gain delimiter writes: "+text);
        assert(Apply("Ô trống \"    \" và “    ”; nội dung  đã duyệt.")=="Ô trống \"    \" và “    ”; nội dung đã duyệt.",
            "Fix all retains straight and curly blank-field widths while correcting independent spaces");
        string many=string.Join(" ",Enumerable.Repeat("Theo mẫu\"nội dung\" ,báo cáo;",200));
        string manyExpected=string.Join(" ",Enumerable.Repeat("Theo mẫu \"nội dung\", báo cáo;",200));
        assert(Checker.Check([new(0,many)]).Count(i=>i.CanFix)==400 && Apply(many)==manyExpected,
            "A paragraph with 200 pairs retains all 400 independent opening/punctuation corrections without bridging pairs");
        const string textAll="Theo mẫu\" nội dung nội dung \"đã  duyệt;hoàn thành.";
        var found=Checker.Check([new(0,textAll)]).Where(i=>i.CanFix).ToArray();
        var beforeOnly=found.Single(i=>i.AddsSpaceBeforeOpeningQuote);
        var afterOnly=found.Single(i=>i.AddsSpaceAfterClosingQuote);
        var inner=found.Where(i=>i.Kind==FixKind.QuoteSpace && i.Replacement.Length==0).ToArray();
        assert(Apply(textAll,[beforeOnly])=="Theo mẫu \" nội dung nội dung \"đã  duyệt;hoàn thành." &&
            Checker.Check([new(0,Apply(textAll,[beforeOnly]))]).Count(i=>i.Kind==FixKind.QuoteSpace && i.CanFix)==3,
            "A before-only choice preserves both inner edges and the outside gap as actionable findings");
        assert(Apply(textAll,inner)=="Theo mẫu\"nội dung nội dung\"đã  duyệt;hoàn thành." &&
            Checker.Check([new(0,Apply(textAll,inner))]).Count(i=>i.Kind==FixKind.QuoteSpace && i.CanFix)==2,
            "Inner-only choices retain both missing exterior gaps even after words become adjacent to straight quotes");
        assert(Apply(textAll,[afterOnly])=="Theo mẫu\" nội dung nội dung \" đã  duyệt;hoàn thành." &&
            Apply(textAll)=="Theo mẫu \"nội dung\" đã duyệt; hoàn thành.",
            "After-only preserves the other defects, while the entire supported cluster composes in one batch");
        var source=new CharacterStyleEvidence(textAll,textAll.Select((c,i)=>char.IsWhiteSpace(c)?null:
            new ObservedCharacterStyle(new("Times New Roman",i%2==0?13:14,i%2==0?0:-1,i%3==0?-1:0,0),i%2)),new(0,0,0,36,new(0,6,0,12,0,0)));
        var projected=source.Project(found,i=>textAll[..i.Start]);
        assert(projected.Text==Apply(textAll) && projected.Complete && projected.Paragraph==source.Paragraph &&
            projected.Styles[projected.Text.IndexOf('"')]==source.Styles[textAll.IndexOf('"')] &&
            projected.Styles[projected.Text.LastIndexOf('"')]==source.Styles[textAll.LastIndexOf('"')],
            "Both straight quote glyphs retain exact source emphasis/size/underline; new spaces gain no invented glyph evidence");
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            const string text="😀 Theo mẫu( nội dung )đã duyệt.\r\a";
            var choices=Checker.Check([new(123,text)],profile).Where(i=>i.CanFix && Delimiter(i)).ToArray();
            assert(choices.Length==4 && choices.Single(i=>i.AddsSpaceBeforeOpeningParenthesis).Start==123+text.IndexOf('(') &&
                !Checker.Check([new(123,text,TextEditable:false)],profile).Any(i=>i.CanFix),
                "All profiles retain actual UTF-16 CPs and structured XML never grants a before/inside/after write");
        }
        bool Reject(Action a){try{a();return false;}catch(InvalidOperationException){return true;}}
        foreach(var bad in new[]{beforeOnly with {Replacement="  \""},beforeOnly with {Replacement="\u00a0\""},beforeOnly with {Original="x",Length=1},
            beforeOnly with {Kind=FixKind.ParagraphEndSpace},beforeOnly with {CharacterFormat=new(13,false)},afterOnly with {Replacement="\"  "}})
            assert(Reject(()=>BatchPlan.Create([bad])),"Only exact delimiter anchors with one ASCII space, or ASCII edge deletions, authorize the cluster payloads");
        assert(Reject(()=>CurrentTextPlan.Validate(found,Checker.Check([new(0,textAll,TextEditable:false)]))),
            "A stale cluster selection fails before mutation after XML permission is revoked");
        const string mixed="Nội dung( kế hoạch kế hoạch )đã được kiểm tra đầy đủ.";
        var sample=BodySpacingChecks.Sample();var rows=sample.P.ToArray();var row=rows[10];
        rows[10]=row with {Text=mixed+"\r",FormatEvidence=new(mixed,[new("Arial",12,0,0,255),new("Arial",14,-1,-1,255)]),
            ParagraphStyle=row.ParagraphStyle! with {Alignment=0,First=0},Outline=row.Outline! with {Alignment=0}};
        int start=0;for(int i=0;i<rows.Length;i++){rows[i]=rows[i] with {Start=start};start+=rows[i].Text.Length;}
        var styles=BodyRuleScan.Check(rows,sample.L,DocumentProfile.State).Where(i=>i.Start==rows[10].Start).ToArray();
        var edits=Checker.Check([rows[10]]).Where(i=>i.CanFix).ToArray();var selected=styles.Concat(edits).ToArray();
        var glyphs=new CharacterStyleEvidence(mixed,mixed.Select((c,i)=>char.IsWhiteSpace(c)?null:new ObservedCharacterStyle(new("Arial",i%2==0?12:14,i%2==0?0:-1,0,255),i%2)),rows[10].ParagraphStyle);
        var probe=CasePreservationPlan.Regions(selected,[new(rows[10].Start,rows[10].Start+rows[10].Text.Length,rows[10].Text,1,false,false)]).Single();
        var combined=CasePreservationPlan.Expected(probe,selected,glyphs,i=>mixed[..(i.Start-rows[10].Start)]);
        assert(styles.Length==4 && combined.Text=="Nội dung (kế hoạch) đã được kiểm tra đầy đủ." &&
            combined.Paragraph is {Alignment:3,First:36,Spacing:{After:6,Rule:0}} && combined.Styles.SequenceEqual(
                glyphs.WithFormat(styles.Single(i=>i.CharacterFormat is not null).CharacterFormat!).Project(edits,i=>mixed[..(i.Start-rows[10].Start)]).Styles),
            "All four body-style patches compose with before/inside/after and repetition while retaining every approved surviving glyph");
        string current=textAll;bool restored=false;var plan=BatchPlan.Create(found);int writes=0;
        try{AtomicTextBatch.Apply(plan,i=>current.Substring(i.Start,i.Length),i=>{writes++;current=current.Remove(i.Start,i.Length).Insert(i.Start,i.Replacement);return i.Replacement;},
            ()=>throw new IOException("synthetic save failure"),()=>{restored=true;current=textAll;});}catch(IOException){}
        assert(restored && current==textAll && writes==plan.Count,"Final verification failure restores the complete delimiter cluster transaction");
    }
}
