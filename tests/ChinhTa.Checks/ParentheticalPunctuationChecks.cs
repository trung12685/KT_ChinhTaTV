using ChinhTa.Core;

public static class ParentheticalPunctuationChecks
{
    public static void Run(Action<bool,string> assert)
    {
        Issue[] Find(string text)=>Checker.Check([new(0,text)]).Where(i=>i.Kind==FixKind.PunctuationSpace).ToArray();
        string Apply(string text,IEnumerable<Issue>? selected=null)=>AtomicTextBatch.ExpectedStory(
            BatchPlan.Create(selected??Checker.Check([new(0,text)]).Where(i=>i.CanFix)),text,i=>text[..i.Start]);
        foreach(var sample in new[]{("Theo mẫu (nội dung) ,báo cáo.","Theo mẫu (nội dung), báo cáo."),
            ("Theo mẫu (nội dung),báo cáo.","Theo mẫu (nội dung), báo cáo."),
            ("Theo mẫu (nội dung)  ,  báo cáo.","Theo mẫu (nội dung), báo cáo."),
            ("Theo mẫu (nội dung) ;đã duyệt.","Theo mẫu (nội dung); đã duyệt."),
            ("Theo mẫu (nội dung);  đã duyệt.","Theo mẫu (nội dung); đã duyệt."),
            ("Theo mẫu ( nội dung ) ,báo cáo.","Theo mẫu (nội dung), báo cáo."),
            ("Theo mẫu  (nội dung) ,báo cáo.","Theo mẫu (nội dung), báo cáo."),
            ("Theo mẫu (nội dung) ,Báo cáo.","Theo mẫu (nội dung), Báo cáo."),
            ("😀 Theo mẫu (nội dung) ,báo cáo.\r\a","😀 Theo mẫu (nội dung), báo cáo.\r\a"),
            ("Theo ma\u0302\u0303u (no\u0323\u0302i dung) ,ba\u0301o cáo.","Theo ma\u0302\u0303u (no\u0323\u0302i dung), ba\u0301o cáo."),
            ("Theo mẫu (nội dung),báo cáo; theo mẫu (kế hoạch) ;đã duyệt.","Theo mẫu (nội dung), báo cáo; theo mẫu (kế hoạch); đã duyệt.")})
        {
            var issues=Find(sample.Item1);
            assert(issues.Length>0 && issues.All(i=>i.CanFix && !i.BatchSafe && i.Length>0 &&
                i.Original==sample.Item1.Substring(i.Start,i.Length) && !i.Original.Contains(')')) && Apply(sample.Item1)==sample.Item2,
                "Punctuation after a prose parenthesis changes only the exact punctuation/space span, preserving delimiters, words, Unicode and paragraph ends");
            assert(!Checker.Check([new(0,sample.Item2)]).Any(i=>i.CanFix),"A fresh scan clears the combined parenthetical punctuation repair");
        }
        foreach(string text in new[]{"Theo mẫu (nội dung), báo cáo.","Theo mẫu (nội dung); đã duyệt.",
            "(nội dung) ,báo cáo.","Mẫu (nội dung) ,báo cáo.","Theo Mẫu (nội dung) ,báo cáo.",
            "Theo mẫu(nội dung) ,báo cáo.","Theo mẫu (nội) ,báo cáo.","Theo mẫu (x y) ,báo cáo.",
            "Theo mẫu (12 34) ,báo cáo.","Theo mẫu (NỘI DUNG) ,báo cáo.","Theo mẫu (nội dung) ,BC.",
            "Theo mẫu (nội dung) ,12 giờ.","Theo mẫu (nội dung) ,x.","Theo mẫu (nội dung) ,báo.com",
            "Theo mẫu (nội dung) ,;báo cáo.","Theo mẫu (nội dung) ,,báo cáo.",
            "Theo mẫu (nội dung (bổ sung)) ,báo cáo.","Theo mẫu (nội dung) ,báo cáo (",
            "Theo mẫu (nội dung) ,báo cáo)","Theo mẫu (nội dung) ,báo cáo; theo mẫu (kế hoạch) ,đã duyệt (",
            "Theo mẫu (    ) ,báo cáo.","Tính sin (alpha beta) ,gamma delta", "Tính sqrt (alpha beta) ,gamma delta",
            "Theo mẫu (nội dung)\t,báo cáo.","Theo mẫu (nội dung),\tbáo cáo.","Theo mẫu (nội dung)\u00a0,báo cáo.",
            "Theo mẫu (nội dung),\u00a0báo cáo.","Theo mẫu (nội\tdung) ,báo cáo.","Theo mẫu (nội\ndung) ,báo cáo.",
            "Theo mẫu (nội dung) ,\rbáo cáo.","Theo mẫu (nội dung) ,báo cáo /data", "Theo mẫu (nội dung) ,báo cáo a=b",
            "Theo mẫu (nội dung) ,báo cáo x+y", "Theo mẫu (nội dung) ,báo cáo user@example.com",
            "Theo mẫu (nội dung) ,báo cáo host_name", "Số: Theo mẫu (nội dung) ,báo cáo.",
            "Hà Nội, ngày 03 tháng 10 năm 2026 theo mẫu (nội dung) ,báo cáo."})
            assert(Find(text).Length==0,"Unclear, numeric, technical, function, header/date, nested/unbalanced and non-ASCII boundary contexts stay outside parenthetical punctuation repair");
        const string mixed="Kế  hoạch kế hoạch; theo mẫu ( nội dung )  ,  báo cáo; ghi chú:nội dung mẫu .   \r\a";
        const string expected="Kế hoạch; theo mẫu (nội dung), báo cáo; ghi chú: nội dung mẫu.\r\a";
        var all=Checker.Check([new(0,mixed)]).Where(i=>i.CanFix).ToArray();var punctuation=Find(mixed).Single();
        assert(Apply(mixed,[punctuation])=="Kế  hoạch kế hoạch; theo mẫu ( nội dung ), báo cáo; ghi chú:nội dung mẫu .   \r\a",
            "Selecting only outside punctuation preserves every unselected inner-parenthesis, initial-repetition, colon and sentence-space error");
        assert(Apply(mixed,all.Where(i=>i.Kind!=FixKind.PunctuationSpace))=="Kế hoạch; theo mẫu (nội dung)  ,  báo cáo; ghi chú: nội dung mẫu.\r\a" &&
            Find(Apply(mixed,all.Where(i=>i.Kind!=FixKind.PunctuationSpace))).Single().CanFix,
            "Repairing the other groups preserves outside punctuation as an actionable fresh issue");
        assert(Apply(mixed)==expected && !Checker.Check([new(0,expected)]).Any(i=>i.CanFix),
            "One mixed batch composes outside punctuation with inner parentheses, initial repetition, retained spaces, colon and both sentence edges");
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            const string source="😀 Theo mẫu (nội dung) ,báo cáo.\r";
            var issue=Checker.Check([new(107,source)],profile).Single(i=>i.Kind==FixKind.PunctuationSpace);
            assert(issue.CanFix && issue.Start==107+source.IndexOf(" ,",StringComparison.Ordinal) && issue.Original==" ,",
                "Every profile uses the real paragraph CP including an emoji before parenthetical punctuation");
            var guarded=Checker.Check([new(107,source,TextEditable:false)],profile);
            assert(guarded.All(i=>!i.CanFix) && guarded.Any(i=>i.Start==107 && i.Original==source.TrimEnd('\r')),
                "Structured paragraph evidence cannot gain a punctuation write through a closing parenthesis");
        }
        const string plain="Theo mẫu (nội dung) ,báo cáo.";var chosen=Find(plain).Single();
        assert(PunctuationSpaceRules.Check(new(0,plain,TextEditable:false)) is [{CanFix:false,Start:0}],
            "The punctuation rule itself exposes one whole-paragraph manual notice for protected content");
        foreach(var fresh in new[]{Checker.Check([new(0,plain,TextEditable:false)]),
            Checker.Check([new(0,plain.Replace("mẫu","Mẫu",StringComparison.Ordinal))]),
            Checker.Check([new(0,plain.Replace("nội dung","12 34   ",StringComparison.Ordinal))])})
        {
            bool rejected=false;try{CurrentTextPlan.Validate([chosen],fresh);}catch(InvalidOperationException){rejected=true;}
            assert(rejected,"Unchanged punctuation bytes/CP are insufficient when XML or the surrounding parenthetical proof becomes invalid");
        }
        var tooltip=IssueTooltipCatalog.Describe(chosen);var review=IssueReviewCatalog.Describe(chosen);
        assert(tooltip.Basis.Contains("Quy tắc ứng dụng") && !tooltip.Basis.Contains("Nghị định") &&
            review.Section==IssueReviewSection.ReviewSuggestion,"Parenthetical punctuation stays a reviewed application rule in the existing group");
        var report=DevelopmentReport.Build("0.1.61",DateTimeOffset.UnixEpoch);
        assert(report.Contains("Theo mẫu (nội dung) ,báo cáo.") && report.Contains("Theo mẫu (nội dung), báo cáo.") &&
            report.Contains("ngoặc cân bằng, không lồng") && DevelopmentReport.Count(CapabilityMode.Reviewable)==25,
            "The report includes an executable before/after example and parenthetical limits without adding a capability group");
        var style=new Issue(0,mixed.Length-2,mixed[..^2],mixed[..^2],"",false,FixKind.DocumentHeadingFormat,CharacterFormat:new(13,false,true));
        var styled=BatchPlan.Create(all.Append(style));
        assert(styled[0]==style && AtomicTextBatch.ExpectedStory(styled,mixed,i=>mixed[..i.Start])==expected,
            "Character formatting can share the paragraph with the new contained punctuation edit without an overlap");
        string current=mixed;int writes=0,saves=0,restores=0;
        string Read(Issue i)=>current.Substring(i.Start,i.Length);
        string Write(Issue i){writes++;current=current.Remove(i.Start,i.Length).Insert(i.Start,i.Replacement);return i.Replacement;}
        var plan=BatchPlan.Create(all);
        AtomicTextBatch.Apply(plan,Read,Write,()=>{saves++;if(current!=expected)throw new Exception("story");},()=>{restores++;current=mixed;});
        assert(current==expected && saves==1 && writes==plan.Count && restores==0,"Mixed parenthetical punctuation commits once after exact final-story verification");
        current=mixed;writes=saves=restores=0;bool failed=false;
        try{AtomicTextBatch.Apply(plan,Read,Write,()=>throw new IOException("save"),()=>{restores++;current=mixed;});}catch(IOException){failed=true;}
        assert(failed && current==mixed && restores==1 && writes==plan.Count,"A failing final save restores all selected groups including parenthetical punctuation");
    }
}
