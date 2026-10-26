using ChinhTa.Core;

public static class QuotedPunctuationChecks
{
    public static void Run(Action<bool,string> assert)
    {
        Issue[] Find(string text)=>Checker.Check([new(0,text)]).Where(i=>i.Kind==FixKind.PunctuationSpace).ToArray();
        string Apply(string text,IEnumerable<Issue>? selected=null)=>AtomicTextBatch.ExpectedStory(
            BatchPlan.Create(selected??Checker.Check([new(0,text)]).Where(i=>i.CanFix)),text,i=>text[..i.Start]);
        foreach(var (before,after) in new[]{("Theo mẫu “nội dung” ,báo cáo.","Theo mẫu “nội dung”, báo cáo."),
            ("Theo mẫu “nội dung”,báo cáo.","Theo mẫu “nội dung”, báo cáo."),
            ("Theo mẫu “nội dung”  ,  báo cáo.","Theo mẫu “nội dung”, báo cáo."),
            ("Theo mẫu “kế hoạch” ;đã duyệt.","Theo mẫu “kế hoạch”; đã duyệt."),
            ("Theo mẫu “ nội dung ” ,Báo cáo.","Theo mẫu “nội dung”, Báo cáo."),
            ("😀 Theo ma\u0302\u0303u “no\u0323\u0302i dung” ,ba\u0301o cáo.\r\a","😀 Theo ma\u0302\u0303u “no\u0323\u0302i dung”, ba\u0301o cáo.\r\a"),
            ("Theo mẫu “nội dung” ,báo cáo; theo mẫu (kế hoạch) ;đã duyệt.","Theo mẫu “nội dung”, báo cáo; theo mẫu (kế hoạch); đã duyệt."),
            ("Theo mẫu “nội dung” ,báo cáo; theo mẫu “kế hoạch” ;đã duyệt.","Theo mẫu “nội dung”, báo cáo; theo mẫu “kế hoạch”; đã duyệt.")})
        {
            var found=Find(before);
            assert(found.Length>0 && found.All(i=>i.CanFix && !i.BatchSafe && i.Original==before.Substring(i.Start,i.Length) &&
                !i.Original.Contains('”') && !i.Original.Contains('“')) && Apply(before)==after,
                "Punctuation after a proven quotation changes only its comma/semicolon/ASCII-space span, preserving quotes and exact Unicode/terminators");
            assert(!Checker.Check([new(0,after)]).Any(i=>i.CanFix),"One combined quoted-punctuation repair leaves no stale actionable rows");
        }
        foreach(string text in new[]{"Theo mẫu “nội dung”, báo cáo.","“nội dung” ,báo cáo.","Mẫu “nội dung” ,báo cáo.",
            "Theo Mẫu “nội dung” ,báo cáo.","Theo mẫu “nội” ,báo cáo.",
            "Theo mẫu “x y” ,báo cáo.","Theo mẫu “12 34” ,báo cáo.","Theo mẫu “NỘI DUNG” ,báo cáo.",
            "Theo mẫu “nội dung” ,BC.","Theo mẫu “nội dung” ,12 giờ.","Theo mẫu “nội dung” ,x.","Theo mẫu “nội dung” ,báo.com",
            "Theo mẫu “nội dung” ,,báo cáo.","Theo mẫu “nội dung” ,;báo cáo.","Theo mẫu “nội dung.” ,báo cáo.",
            "Theo mẫu “nội dung (bổ sung)” ,báo cáo.","Theo mẫu “nội dung “bổ sung” ” ,báo cáo.",
            "Theo mẫu “nội dung” ,báo cáo “","Theo mẫu “nội dung” ,báo cáo ”","Theo mẫu “nội dung” ,báo cáo ‘mẫu’",
            "Theo mẫu “    ” ,báo cáo.","Tính sin “alpha beta” ,gamma delta",
            "Theo mẫu “nội dung”\t,báo cáo.","Theo mẫu “nội dung”,\u00a0báo cáo.","Theo mẫu “nội\ndung” ,báo cáo.",
            "Theo mẫu “nội dung” ,báo cáo /data","Theo mẫu “nội dung” ,báo cáo a=b","Theo mẫu “nội dung” ,báo cáo x+y",
            "Số: Theo mẫu “nội dung” ,báo cáo.","Hà Nội, ngày 03 tháng 10 năm 2026 theo mẫu “nội dung” ,báo cáo."})
            assert(Find(text).Length==0,"Unclear/numeric/technical/uppercase/blank/mixed/nested/unbalanced/terminal quote contexts cannot grant outside punctuation writes: "+text);
        const string mixed="Kế  hoạch kế hoạch; theo mẫu “ nội dung ”  ,  báo cáo; ghi chú:nội dung mẫu .   \r\a";
        const string expected="Kế hoạch; theo mẫu “nội dung”, báo cáo; ghi chú: nội dung mẫu.\r\a";
        var all=Checker.Check([new(0,mixed)]).Where(i=>i.CanFix).ToArray();var punctuation=Find(mixed).Single();
        assert(Apply(mixed,[punctuation])=="Kế  hoạch kế hoạch; theo mẫu “ nội dung ”, báo cáo; ghi chú:nội dung mẫu .   \r\a",
            "Punctuation-only repair retains both unselected quote edges and every independent old text defect");
        string withoutOutside=Apply(mixed,all.Where(i=>i.Kind!=FixKind.PunctuationSpace));
        assert(Find(withoutOutside) is [{CanFix:true}] && Apply(mixed)==expected && !Checker.Check([new(0,expected)]).Any(i=>i.CanFix),
            "Quote-edge-only repair keeps outside punctuation discoverable, and choosing all text groups fixes both in one transaction");
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            const string text="😀 Theo mẫu “nội dung” ,báo cáo.\r";
            var item=Checker.Check([new(107,text)],profile).Single(i=>i.Kind==FixKind.PunctuationSpace);
            assert(item.Start==107+text.IndexOf(" ,",StringComparison.Ordinal) && item.Original==" ," && item.CanFix,
                "Every profile anchors quoted punctuation at the real paragraph CP, including an earlier surrogate pair");
            assert(Checker.Check([new(107,text,TextEditable:false)],profile).All(i=>!i.CanFix),
                "A structured or XML-unproven paragraph remains entirely manual for this new punctuation path");
        }
        bool Reject(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
        const string simple="Theo mẫu “nội dung” ,báo cáo.";var chosen=Find(simple).Single();
        foreach(var row in new[]{new Paragraph(0,simple,TextEditable:false),new(0,simple.Replace("mẫu","Mẫu",StringComparison.Ordinal)),
            new(0,simple.Replace("nội dung","12 34   ",StringComparison.Ordinal))})
            assert(Reject(()=>CurrentTextPlan.Validate([chosen],Checker.Check([row]))),
                "Unchanged punctuation bytes and CP cannot bypass a changed quote role, word context or XML permission");
        var sample=BodySpacingChecks.Sample();var rows=sample.P.ToArray();var rowBody=rows[10];
        const string body="Nội dung  “  kế hoạch kế hoạch  ”  ,  báo cáo đã được duyệt.";
        rows[10]=rowBody with {Text=body+"\r",FormatEvidence=new(body,[new("Arial",12,0,0,255),new("Arial",14,-1,-1,255)]),
            ParagraphStyle=rowBody.ParagraphStyle! with {Alignment=0,First=0},Outline=rowBody.Outline! with {Alignment=0}};
        int start=0;for(int i=0;i<rows.Length;i++){rows[i]=rows[i] with {Start=start};start+=rows[i].Text.Length;}
        var styles=BodyRuleScan.Check(rows,sample.L,DocumentProfile.State).Where(i=>i.Start==rows[10].Start).ToArray();
        var textEdits=Checker.Check([rows[10]]).Where(i=>i.CanFix).ToArray();var selected=styles.Concat(textEdits).ToArray();
        var source=new CharacterStyleEvidence(body,body.Select((c,i)=>char.IsWhiteSpace(c)?null:new ObservedCharacterStyle(
            new("Arial",i%2==0?12:14,i%2==0?0:-1,i%3==0?-1:0,255),i%2)),rows[10].ParagraphStyle);
        var probe=CasePreservationPlan.Regions(selected,[new(rows[10].Start,rows[10].Start+rows[10].Text.Length,rows[10].Text,1,false,false)]).Single();
        var projected=CasePreservationPlan.Expected(probe,selected,source,i=>body[..(i.Start-rows[10].Start)]);
        assert(styles.Length==4 && projected.Text=="Nội dung “kế hoạch”, báo cáo đã được duyệt." &&
            projected.Paragraph is {Alignment:3,First:36,Spacing:{After:6,Rule:0}},
            "Quoted punctuation, inner edges, repetition and spaces compose with all four independent body-style groups");
        assert(projected.Styles.SequenceEqual(source.WithFormat(styles.Single(i=>i.CharacterFormat is not null).CharacterFormat!).Project(textEdits,i=>body[..(i.Start-rows[10].Start)]).Styles),
            "The mixed batch retains each surviving quote/word glyph's approved font, size, emphasis and underline");
        string current=mixed;bool restored=false;int writes=0;var plan=BatchPlan.Create(all);
        try{AtomicTextBatch.Apply(plan,i=>current.Substring(i.Start,i.Length),i=>{writes++;current=current.Remove(i.Start,i.Length).Insert(i.Start,i.Replacement);return i.Replacement;},
            ()=>throw new IOException("synthetic save failure"),()=>{current=mixed;restored=true;});}catch(IOException){}
        assert(restored && writes==plan.Count && current==mixed,"Failed final-save verification restores the whole quoted-punctuation batch");
        var tooltip=IssueTooltipCatalog.Describe(chosen);var review=IssueReviewCatalog.Describe(chosen);
        assert(tooltip.Text.Split('\n').Length==3 && tooltip.Basis.Contains("Quy tắc ứng dụng") && review.FamilyKey=="comma" &&
            review.Section==IssueReviewSection.ReviewSuggestion && DevelopmentReport.Count(CapabilityMode.Reviewable)==31,
            "The extension keeps the existing compact punctuation group, reviewed application basis and capability count");
    }
}
