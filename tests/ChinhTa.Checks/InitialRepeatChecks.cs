using ChinhTa.Core;

public static class InitialRepeatChecks
{
    public static void Run(Action<bool,string> assert)
    {
        Issue[] Find(string text)=>Checker.Check([new(0,text)]).Where(i=>i.Kind==FixKind.RepeatedText).ToArray();
        string Apply(string text,IEnumerable<Issue> edits)=>AtomicTextBatch.ExpectedStory(BatchPlan.Create(edits),text,i=>text[..i.Start]);
        foreach(var sample in new[]{("Kế hoạch kế hoạch đã duyệt.","Kế hoạch đã duyệt."),
            ("Cơ quan cơ quan", "Cơ quan"),("Báo cáo báo cáo báo cáo.","Báo cáo."),
            ("  Kế  hoạch kế hoạch", "  Kế  hoạch"),
            ("Ke\u0302\u0301 hoa\u0323ch kế hoạch.\r\a","Ke\u0302\u0301 hoa\u0323ch.\r\a"),
            ("Kế hoạch ke\u0302\u0301 hoa\u0323ch.","Kế hoạch."),
            ("Một hai ba bốn năm sáu bảy tám một hai ba bốn năm sáu bảy tám.","Một hai ba bốn năm sáu bảy tám.")})
        {
            var edits=Find(sample.Item1);var issue=edits.Single();
            assert(issue.CanFix && !issue.BatchSafe && issue.Replacement=="" && issue.Original==sample.Item1.Substring(issue.Start,issue.Length) &&
                issue.Original.StartsWith(' ') && issue.Message.Contains("chỉ khác chữ hoa đầu tiên") && Apply(sample.Item1,edits)==sample.Item2,
                "Initial-capital repetition deletes only the exact duplicate suffix and preserves the first copy, indentation, Unicode and terminators");
            assert(!Find(sample.Item2).Any(i=>i.CanFix),"A fresh scan removes the repaired initial-capital repetition");
        }
        foreach(string text in new[]{"Kế hoạch đã duyệt.","Theo Kế hoạch kế hoạch", "Theo kế hoạch Kế hoạch", "kế hoạch Kế hoạch",
            "KẾ hoạch kế hoạch","Kế Hoạch kế hoạch","UBND tỉnh ubnd tỉnh","Đều đều","Ban ban", "Kế kế",
            "1. Kế hoạch kế hoạch","- Kế hoạch kế hoạch","😀 Kế hoạch kế hoạch","\tKế hoạch kế hoạch",
            "Kế hoạch, kế hoạch","Kế hoạch. kế hoạch","Kế hoạch; kế hoạch","Kế hoạch\tkế hoạch",
            "Kế hoạch\u00a0kế hoạch","Kế hoạch\rkế hoạch","Kế hoạch\nkế hoạch","Kế hoạch\vkế hoạch",
            "Kế hoạch\fkế hoạch","Kế hoạch khác kế hoạch","Một hai ba bốn năm sáu bảy tám chín một hai ba bốn năm sáu bảy tám chín"})
            assert(!Find(text).Any(i=>i.CanFix),"Initial-capital matching does not fold interior case, names/acronyms, single words, list prefixes or cross unsupported boundaries");
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            const string text="Kế hoạch kế hoạch.\r";
            var issue=Checker.Check([new(105,text)],profile).Single(i=>i.Kind==FixKind.RepeatedText);
            assert(issue.CanFix && issue.Start==105+"Kế hoạch".Length && issue.Original==" kế hoạch",
                "All profiles anchor the initial-capital deletion at the actual paragraph offset");
            var guarded=Checker.Check([new(105,text,TextEditable:false)],profile);
            assert(guarded.All(i=>!i.CanFix) && guarded.Any(i=>i.Start==105 && i.Original==text.TrimEnd('\r')),
                "The initial-capital variant cannot bypass a structured paragraph's manual gate");
        }
        const string source="Kế hoạch kế hoạch.";var choice=Find(source).Single();
        foreach(var fresh in new[]{Checker.Check([new(0,source,TextEditable:false)]),Checker.Check([new(0,"KẾ hoạch kế hoạch.")])})
        {
            bool rejected=false;try{CurrentTextPlan.Validate([choice],fresh);}catch(InvalidOperationException){rejected=true;}
            assert(rejected,"Stale selection is rejected when XML permission or the retained initial word changes even though the duplicate suffix is unchanged");
        }
        const string two="Kế hoạch kế hoạch; công tác công tác.";var choices=Find(two);
        assert(choices.Length==2 && Apply(two,[choices[0]])=="Kế hoạch; công tác công tác." &&
            Find(Apply(two,[choices[0]])).Single().CanFix,"Selecting the initial repetition leaves an independent unselected exact-case repetition actionable");
        var tooltip=IssueTooltipCatalog.Describe(choice);var review=IssueReviewCatalog.Describe(choice);
        assert(tooltip.Basis.Contains("Quy tắc ứng dụng") && !tooltip.Basis.Contains("Nghị định") &&
            review.Section==IssueReviewSection.ReviewSuggestion,"The initial-capital variant remains a reviewed application rule without an invented legal requirement");
        var report=DevelopmentReport.Build("0.1.61",DateTimeOffset.UnixEpoch);
        assert(report.Contains("Kế hoạch kế hoạch đã duyệt.") && report.Contains("Kế hoạch đã duyệt.") &&
            report.Contains("Không gộp hoa/thường giữa câu") && DevelopmentReport.Count(CapabilityMode.Reviewable)==33,
            "The report records the initial-capital example and limits without inventing a new capability group or a completed release");
        const string mixed="Kế  hoạch kế hoạch; ghi chú:nội dung mẫu .   \r\a";
        const string expected="Kế hoạch; ghi chú: nội dung mẫu.\r\a";
        var editsAll=Checker.Check([new(0,mixed)]).Where(i=>i.CanFix).ToArray();var plan=BatchPlan.Create(editsAll);
        CurrentTextPlan.Validate(plan,Checker.Check([new(0,mixed)]));
        assert(Apply(mixed,editsAll)==expected && !Checker.Check([new(0,expected)]).Any(i=>i.CanFix),
            "Initial repetition composes with retained-copy spacing, colon, sentence-edge and trailing-space edits in one scan");
        var style=new Issue(0,mixed.Length-2,mixed[..^2],mixed[..^2],"",false,FixKind.DocumentHeadingFormat,CharacterFormat:new(13,false,true));
        var styled=BatchPlan.Create(editsAll.Append(style));
        assert(styled[0]==style && AtomicTextBatch.ExpectedStory(styled,mixed,i=>mixed[..i.Start])==expected,
            "Contained initial-capital deletion remains compatible with character formatting before text edits");
        string current=mixed;int writes=0,saves=0,restores=0;
        string Read(Issue i)=>current.Substring(i.Start,i.Length);
        string Write(Issue i){writes++;current=current.Remove(i.Start,i.Length).Insert(i.Start,i.Replacement);return i.Replacement;}
        AtomicTextBatch.Apply(plan,Read,Write,()=>{saves++;if(current!=expected)throw new Exception("story");},()=>{restores++;current=mixed;});
        assert(current==expected && writes==plan.Count && saves==1 && restores==0,"The mixed initial-repetition batch commits once after exact story verification");
        current=mixed;writes=saves=restores=0;bool failed=false;
        try{AtomicTextBatch.Apply(plan,Read,i=>{var result=Write(i);if(writes==2)throw new IOException("injected");return result;},()=>saves++,()=>{restores++;current=mixed;});}
        catch(IOException){failed=true;}
        assert(failed && current==mixed && restores==1 && saves==0,"A failure midway through the mixed initial-repetition batch restores the entire original paragraph");
    }
}
