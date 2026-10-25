using ChinhTa.Core;

public static class QuoteSpaceChecks
{
    public static void Run(Action<bool,string> assert)
    {
        Issue[] Find(string text)=>Checker.Check([new(0,text)]).Where(i=>i.Kind==FixKind.QuoteSpace).ToArray();
        string Apply(string text,IEnumerable<Issue>? selected=null)=>AtomicTextBatch.ExpectedStory(
            BatchPlan.Create(selected??Checker.Check([new(0,text)]).Where(i=>i.CanFix)),text,i=>text[..i.Start]);
        foreach(var (before,after) in new[]{("Theo mẫu “ nội dung báo cáo ” đã duyệt.","Theo mẫu “nội dung báo cáo” đã duyệt."),
            ("“  ở đây”","“ở đây”"),("“ý kiến  ”","“ý kiến”"),("“ nội dung ”; “ báo cáo ”.","“nội dung”; “báo cáo”."),
            ("😀 “ ke\u0302\u0301 hoa\u0323ch ” đã duyệt.","😀 “ke\u0302\u0301 hoa\u0323ch” đã duyệt."),
            ("Theo mẫu (“ nội dung ”).","Theo mẫu (“nội dung”)."),
            ("“ Công tác – báo cáo ”","“Công tác – báo cáo”"),
            ("Nội dung  “  kế hoạch kế hoạch  ” đã duyệt;hoàn thành.","Nội dung “kế hoạch” đã duyệt; hoàn thành."),
            ("Theo mẫu “  nội dung ,báo cáo  ” đã  duyệt.","Theo mẫu “nội dung, báo cáo” đã duyệt.")})
        {
            var edits=Find(before);
            assert(edits.Length>0 && edits.All(i=>i.CanFix && !i.BatchSafe && i.Length>0 && i.Original.All(c=>c==' ') &&
                i.Replacement=="" && before.Substring(i.Start,i.Length)==i.Original) && Apply(before)==after,
                "Curly-quote repairs delete only selected positive ASCII edge spans while preserving quotes, Unicode and outside text");
            assert(!Checker.Check([new(0,after)]).Any(i=>i.CanFix),"A combined quote/text batch leaves no stale repaired rows or new overlapping punctuation error");
        }
        foreach(string text in new[]{"“nội dung”","“”","“ ”","“    ”","“ 12 ”","“ 1,25 ”","“ ABC ”","“ HÀ NỘI ”","“ a ”","“ đúng ”",
            "“ x + y ”","“ alpha = beta ”","“ tên_tệp ”","“ host/name ”","“ host\\name ”","“ name@example.com ”",
            "“ nội\tdung ”","“ nội dung\u00a0 ”","“ nội\rdung ”","“ nội\ndung ”","“ nội\vdung ”",
            "“ nội dung ” “ phụ “ thêm ” ”","“ nội dung ” “ còn thiếu","“ nội dung ” ”","nêu“ nội dung ”","“ nội dung ”tiếp",
            "“ - nội dung ”","“ nội dung - ”","‘ nội dung ’","« nội dung »",
            "Số: “ nội dung ”","Ninh Bình, ngày “ bản nháp ” tháng 10 năm 2026"})
            assert(Find(text).Length==0,"Ambiguous, blank, numeric, technical, single-word, nested/unbalanced and unsupported quote forms cannot grant edge writes: "+text);
        assert(Apply("Ô trống “    ”; nội dung  đã duyệt.")=="Ô trống “    ”; nội dung đã duyệt.",
            "Fix All preserves blank curly-quote form width while correcting independent old spacing");
        const string mixed="Nội dung  “  kế hoạch kế hoạch  ” đã duyệt;hoàn thành.";
        var quotes=Find(mixed);var first=quotes.OrderBy(i=>i.Start).First();
        assert(Apply(mixed,[first])=="Nội dung  “kế hoạch kế hoạch  ” đã duyệt;hoàn thành.",
            "Selecting one quote edge preserves the other edge and all unselected spacing/repetition/punctuation defects");
        const string comma="“ nội,dung ”";
        var edges=Find(comma);var edgeOnly=Apply(comma,edges);
        assert(edgeOnly=="“nội,dung”" && Checker.Check([new(0,edgeOnly)]).Count(i=>i.Kind==FixKind.PunctuationSpace && i.CanFix)==1 &&
            Apply(comma)=="“nội, dung”", "C14 remains discoverable inside quotes after edge-only repair and can commit with both quote edges in one batch");
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            var edits=Checker.Check([new(90,"“ nội dung ”\r")],profile).Where(i=>i.Kind==FixKind.QuoteSpace).ToArray();
            assert(edits.Length==2 && edits[0].Start==91 && edits[1].Start==100,
                "Quote edges use actual paragraph offsets in all three profiles, keeping paragraph/cell terminators outside the edits");
            var guarded=Checker.Check([new(90,"“ nội dung ”",TextEditable:false)],profile);
            assert(guarded.Count==1 && !guarded[0].CanFix && guarded[0].Start==90 && guarded[0].Original=="“ nội dung ”",
                "Unverified XML or structured text produces one manual full-paragraph notice without guessed editable offsets");
        }
        var direct=QuoteSpaceRules.Check(new(90,"“ nội dung ”",TextEditable:false));
        assert(direct.Count==1 && !direct[0].CanFix,"The direct quote rule also enforces the structured-text gate");
        bool Reject(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
        assert(Reject(()=>CurrentTextPlan.Validate(quotes,Checker.Check([new(0,mixed,TextEditable:false)]))),
            "A newly unproven row invalidates a stale quote selection before writing");
        foreach(var bad in new[]{first with {Original="x",Length=1},first with {Replacement=" "},first with {Format=new(13)},
            first with {CharacterFormat=new(13,false)},first with {ParagraphFix=new(new(0,0,0,0,new(0,0,0,12,0,0)),3,36)}})
            assert(Reject(()=>BatchPlan.Create([bad])),"A forged quote payload cannot replace text or bundle unauthorized format writes");
        var sample=BodySpacingChecks.Sample();var rows=sample.P.ToArray();var row=rows[10];
        const string body="Nội dung  “  kế hoạch kế hoạch  ” đã được kiểm tra đầy đủ.";
        rows[10]=row with {Text=body+"\r",FormatEvidence=new(body,[new("Arial",12,0,0,255),new("Arial",14,-1,-1,255)]),
            Outline=row.Outline! with {Alignment=0},ParagraphStyle=row.ParagraphStyle! with {Alignment=0,First=0}};
        // Keep CPs consistent for every original neighbor after changing text.
        int offset=0;for(int i=0;i<rows.Length;i++){rows[i]=rows[i] with {Start=offset};offset+=rows[i].Text.Length;}
        var styleIssues=BodyRuleScan.Check(rows,sample.L,DocumentProfile.State).Where(i=>i.Start==rows[10].Start).ToArray();
        var textIssues=Checker.Check([rows[10]]).Where(i=>i.CanFix).ToArray();var selection=styleIssues.Concat(textIssues).ToArray();
        var source=new CharacterStyleEvidence(body,body.Select((c,i)=>char.IsWhiteSpace(c)?null:
            new ObservedCharacterStyle(new("Arial",i%2==0?12:14,i%2==0?0:-1,0,255),i%2)),rows[10].ParagraphStyle);
        var probe=CasePreservationPlan.Regions(selection,[new(rows[10].Start,rows[10].Start+rows[10].Text.Length,rows[10].Text,1,false,false)]).Single();
        var projected=CasePreservationPlan.Expected(probe,selection,source,i=>body[..(i.Start-rows[10].Start)]);
        assert(styleIssues.Length==4 && projected.Text=="Nội dung “kế hoạch” đã được kiểm tra đầy đủ." &&
            projected.Paragraph is {Alignment:3,First:36,Spacing:{After:6,Rule:0}},
            "Both quote edges compose with repetition, spaces and all four independent body-style groups");
        var kept=source.Project(textIssues,i=>body[..(i.Start-rows[10].Start)]).WithFormat(styleIssues.Single(i=>i.CharacterFormat is not null).CharacterFormat!);
        assert(projected.Styles.SequenceEqual(kept.Styles),"Combined quote deletions retain every quote mark and surviving glyph's approved emphasis, underline and valid size");
        string current=mixed;int writes=0;bool restored=false;
        var plan=BatchPlan.Create(Checker.Check([new(0,mixed)]).Where(i=>i.CanFix));
        try{AtomicTextBatch.Apply(plan,i=>current.Substring(i.Start,i.Length),i=>{writes++;current=current.Remove(i.Start,i.Length).Insert(i.Start,i.Replacement);return i.Replacement;},
            ()=>throw new InvalidOperationException("synthetic save failure"),()=>{current=mixed;restored=true;});}catch(InvalidOperationException){}
        assert(restored && current==mixed && writes==plan.Count,"A failed post-save check rolls back the entire quote/repetition/punctuation batch");
        var tooltip=IssueTooltipCatalog.Describe(first);var review=IssueReviewCatalog.Describe(first);
        assert(tooltip.Text.Split('\n').Length==3 && tooltip.Basis.StartsWith("Quy tắc ứng dụng") && !tooltip.Basis.Contains("Nghị định") &&
            review.FamilyKey=="quotes" && review.Section==IssueReviewSection.ReviewSuggestion,
            "Quote issues have one compact review group and a concise three-part tooltip without inventing a legal clause");
    }
}
