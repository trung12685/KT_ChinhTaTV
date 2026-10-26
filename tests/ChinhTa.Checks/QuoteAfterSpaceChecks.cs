using ChinhTa.Core;

public static class QuoteAfterSpaceChecks
{
    public static void Run(Action<bool,string> assert)
    {
        Issue[] Find(string text)=>Checker.Check([new(0,text)]).Where(i=>i.AddsSpaceAfterClosingQuote).ToArray();
        string Apply(string text,IEnumerable<Issue>? selected=null)=>AtomicTextBatch.ExpectedStory(
            BatchPlan.Create(selected??Checker.Check([new(0,text)]).Where(i=>i.CanFix)),text,i=>text[..i.Start]);
        foreach(var (before,after) in new[]{("Theo mẫu “nội dung”đã duyệt.","Theo mẫu “nội dung” đã duyệt."),
            ("Theo mẫu “ nội dung ”đã duyệt.","Theo mẫu “nội dung” đã duyệt."),
            ("Theo mẫu “  kế hoạch  ”đã  duyệt.","Theo mẫu “kế hoạch” đã duyệt."),
            ("😀 Theo ma\u0302\u0303u “no\u0323\u0302i dung”đã duye\u0323\u0302t.\r\a","😀 Theo ma\u0302\u0303u “no\u0323\u0302i dung” đã duye\u0323\u0302t.\r\a"),
            ("Theo mẫu “nội dung”đã duyệt; theo mẫu “kế hoạch”cần làm.","Theo mẫu “nội dung” đã duyệt; theo mẫu “kế hoạch” cần làm."),
            ("Theo mẫu ( nội dung )đã duyệt; theo mẫu “ kế hoạch ”cần làm.","Theo mẫu (nội dung) đã duyệt; theo mẫu “kế hoạch” cần làm.")})
        {
            var found=Find(before);
            assert(found.Length>0 && found.All(i=>i.CanFix && !i.BatchSafe && i.Length==1 && i.Original=="”" && i.Replacement=="” " &&
                before.Substring(i.Start,i.Length)==i.Original && i.Suggestion.Contains("Thêm một dấu cách")) && Apply(before)==after,
                "A proven missing gap uses only the actual closing quote anchor and preserves exact words, Unicode and terminators");
            assert(!Checker.Check([new(0,after)]).Any(i=>i.CanFix),"One combined inner/after-quote batch leaves no stale repaired rows");
        }
        foreach(string text in new[]{"Theo mẫu “nội dung” đã duyệt.","“ nội dung ”đã duyệt.","Mẫu “nội dung”đã duyệt.",
            "Theo Mẫu “nội dung”đã duyệt.","Theo mẫu “nội”đã duyệt.","Theo mẫu “x y”đã duyệt.",
            "Theo mẫu “NỘI DUNG”đã duyệt.","Theo mẫu “12 34”đã duyệt.","Theo mẫu “    ”đã duyệt.",
            "Theo mẫu “nội dung”Đã duyệt.","Theo mẫu “nội dung”a duyệt.","Theo mẫu “nội dung”đã x.",
            "Theo mẫu “nội dung”đã", "Theo mẫu “nội dung”đã duyệt.docx", "Theo mẫu “nội dung”đã duyệt.Tiếp tục",
            "Theo mẫu “nội dung.”đã duyệt.","Theo mẫu “nội dung, báo cáo”đã duyệt.","Theo mẫu “kế-hoạch mới”đã duyệt.",
            "Theo mẫu “nội dung “bổ sung” ”đã duyệt.","Theo mẫu “nội dung”đã duyệt “","Theo mẫu “nội dung”đã duyệt ”",
            "Theo mẫu ‘nội dung’đã duyệt.","Tính sin “alpha beta”gamma delta",
            "Theo mẫu “nội dung”đã\tduyệt.","Theo mẫu “nội dung”đã\u00a0duyệt.","Theo mẫu “nội dung”đã duyệt /data",
            "Số: Theo mẫu “nội dung”đã duyệt.","Hà Nội, ngày 03 tháng 10 năm 2026 theo mẫu “nội dung”đã duyệt."})
            assert(Find(text).Length==0,"Uncertain boundaries, words, codes, numeric/blank/uppercase/mixed/nested/unbalanced/terminal/header contexts cannot grant an after-quote insertion: "+text);
        const string mixed="Nội dung  “  kế hoạch kế hoạch  ”đã duyệt;hoàn thành.";
        var quotes=Checker.Check([new(0,mixed)]).Where(i=>i.CanFix && i.Kind==FixKind.QuoteSpace).ToArray();
        var afterOnly=quotes.Single(i=>i.AddsSpaceAfterClosingQuote);var inside=quotes.Where(i=>!i.AddsSpaceAfterClosingQuote).ToArray();
        string afterText=Apply(mixed,[afterOnly]),insideText=Apply(mixed,inside);
        assert(afterText=="Nội dung  “  kế hoạch kế hoạch  ” đã duyệt;hoàn thành." &&
            Checker.Check([new(0,afterText)]).Count(i=>i.CanFix && i.Kind==FixKind.QuoteSpace)==2,
            "Selecting only the outside gap leaves both inner edges and every unselected repetition/spacing defect actionable");
        assert(insideText=="Nội dung  “kế hoạch kế hoạch”đã duyệt;hoàn thành." && Find(insideText).Length==1 &&
            Apply(mixed)=="Nội dung “kế hoạch” đã duyệt; hoàn thành.",
            "Selecting only inner edges retains the missing outside gap, while all groups compose in one batch");
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            const string text="😀 Theo mẫu “nội dung”đã duyệt.\r\a";
            var item=Checker.Check([new(81,text)],profile).Single(i=>i.AddsSpaceAfterClosingQuote);
            assert(item.Start==81+text.IndexOf('”') && item.Length==1 && !Checker.Check([new(81,text,TextEditable:false)],profile).Any(i=>i.CanFix),
                "Every profile binds the live quote CP and keeps XML-unproven/cell-terminated text outside writes");
        }
        bool Reject(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
        foreach(var row in new[]{new Paragraph(0,mixed,TextEditable:false),new(0,mixed.Replace("đã duyệt","Đã duyệt",StringComparison.Ordinal)),
            new(0,mixed.Replace("kế hoạch kế hoạch","12 34 12 34      ",StringComparison.Ordinal))})
            assert(Reject(()=>CurrentTextPlan.Validate([afterOnly],Checker.Check([row]))),
                "Unchanged quote bytes cannot authorize an old choice after XML or the word context becomes invalid");
        foreach(var bad in new[]{afterOnly with {Original="” ",Length=2},afterOnly with {Replacement="”  "},afterOnly with {Replacement="”\u00a0"},
            afterOnly with {Kind=FixKind.ParagraphEndSpace},afterOnly with {CharacterFormat=new(13,false)},afterOnly with {Original="x",Length=1}})
            assert(Reject(()=>BatchPlan.Create([bad])),"Only the exact closing-quote plus one ASCII-space payload is accepted for the new insertion");
        var sample=BodySpacingChecks.Sample();var rows=sample.P.ToArray();var rowBody=rows[10];
        const string body="Nội dung  “  kế hoạch kế hoạch  ”đã được kiểm tra đầy đủ.";
        rows[10]=rowBody with {Text=body+"\r",FormatEvidence=new(body,[new("Arial",12,0,0,255),new("Arial",14,-1,-1,255)]),
            ParagraphStyle=rowBody.ParagraphStyle! with {Alignment=0,First=0},Outline=rowBody.Outline! with {Alignment=0}};
        int start=0;for(int i=0;i<rows.Length;i++){rows[i]=rows[i] with {Start=start};start+=rows[i].Text.Length;}
        var styles=BodyRuleScan.Check(rows,sample.L,DocumentProfile.State).Where(i=>i.Start==rows[10].Start).ToArray();
        var edits=Checker.Check([rows[10]]).Where(i=>i.CanFix).ToArray();var selected=styles.Concat(edits).ToArray();
        var source=new CharacterStyleEvidence(body,body.Select((c,i)=>char.IsWhiteSpace(c)?null:new ObservedCharacterStyle(
            new("Arial",i%2==0?12:14,i%2==0?0:-1,i%3==0?-1:0,255),i%2)),rows[10].ParagraphStyle);
        var probe=CasePreservationPlan.Regions(selected,[new(rows[10].Start,rows[10].Start+rows[10].Text.Length,rows[10].Text,1,false,false)]).Single();
        var expected=CasePreservationPlan.Expected(probe,selected,source,i=>body[..(i.Start-rows[10].Start)]);
        assert(styles.Length==4 && expected.Text=="Nội dung “kế hoạch” đã được kiểm tra đầy đủ." &&
            expected.Paragraph is {Alignment:3,First:36,Spacing:{After:6,Rule:0}},
            "Inner/after quote repairs compose with selected repetition/spaces and all four independent body-style patches");
        var retained=source.WithFormat(styles.Single(i=>i.CharacterFormat is not null).CharacterFormat!).Project(edits,i=>body[..(i.Start-rows[10].Start)]);
        assert(expected.Styles.SequenceEqual(retained.Styles) && expected.Styles[expected.Text.IndexOf('”')] is not null && expected.Complete,
            "The actual quote glyph keeps its approved size/emphasis/underline; the new gap does not invent a source glyph style");
        string current=mixed;bool restored=false;int writes=0;var plan=BatchPlan.Create(Checker.Check([new(0,mixed)]).Where(i=>i.CanFix));
        try{AtomicTextBatch.Apply(plan,i=>current.Substring(i.Start,i.Length),i=>{writes++;current=current.Remove(i.Start,i.Length).Insert(i.Start,i.Replacement);return i.Replacement;},
            ()=>throw new IOException("synthetic save failure"),()=>{restored=true;current=mixed;});}catch(IOException){}
        assert(restored && writes==plan.Count && current==mixed,"A failing final save rolls back the entire inner/after quote/text batch");
        var tooltip=IssueTooltipCatalog.Describe(afterOnly);
        assert(tooltip.Text.Split('\n').Length==3 && tooltip.Basis.StartsWith("Quy tắc ứng dụng") && tooltip.Action.StartsWith("Thêm một dấu cách") &&
            IssueReviewCatalog.Describe(afterOnly).FamilyKey=="quotes" && DevelopmentReport.Count(CapabilityMode.Reviewable)==33,
            "The new insertion stays in the existing quote group with a concise application-rule tooltip and unchanged capability count");
    }
}
