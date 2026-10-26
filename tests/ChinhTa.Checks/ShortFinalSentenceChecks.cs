using ChinhTa.Core;

public static class ShortFinalSentenceChecks
{
    public static void Run(Action<bool,string> assert)
    {
        bool Target(Issue i)=>i.Kind is FixKind.SentenceInnerSpace or FixKind.SentenceAfterSpace;
        Issue[] Find(string text)=>Checker.Check([new(0,text)]).Where(Target).ToArray();
        string Apply(string text,IEnumerable<Issue>? selected=null)=>AtomicTextBatch.ExpectedStory(
            BatchPlan.Create(selected??Checker.Check([new(0,text)]).Where(i=>i.CanFix)),text,i=>text[..i.Start]);
        foreach(char boundary in ".?!")foreach(char final in ".?!")
        {
            string source="Đã hoàn thành  "+boundary+"Xong"+final+"\r\a",expected="Đã hoàn thành"+boundary+" Xong"+final+"\r\a";
            var issues=Find(source);var before=issues.Single(i=>i.Kind==FixKind.SentenceInnerSpace);var after=issues.Single(i=>i.Kind==FixKind.SentenceAfterSpace);
            assert(issues.Length==2 && issues.All(i=>i.CanFix && !i.BatchSafe) && before.Original=="  " && before.Replacement=="" &&
                after.Length==1 && after.Original==boundary.ToString() && after.Replacement==boundary+" " && before.Start+before.Length==after.Start,
                "A one-word final sentence exposes two adjacent independent source spans for every supported boundary/final mark combination");
            assert(Apply(source)==expected && !Find(expected).Any(i=>i.CanFix),
                "Both sides repair in one batch and disappear after scanning the one-word final sentence");
            string afterOnly=Apply(source,[after]),beforeOnly=Apply(source,[before]);
            assert(afterOnly=="Đã hoàn thành  "+boundary+" Xong"+final+"\r\a" && Find(afterOnly) is [{Kind:FixKind.SentenceInnerSpace,CanFix:true}],
                "Selecting only the new gap preserves the original before-boundary spaces as an actionable issue");
            assert(beforeOnly=="Đã hoàn thành"+boundary+"Xong"+final+"\r\a" && Find(beforeOnly) is [{Kind:FixKind.SentenceAfterSpace,CanFix:true}],
                "Selecting only the before-boundary deletion preserves the missing after-gap for a fresh scan");
        }
        foreach(var sample in new[]{("Đã hoàn thành.Xong!","Đã hoàn thành. Xong!"),
            ("Đã hoàn thành . Xong!","Đã hoàn thành. Xong!"),
            ("Đã hoàn thành?Được.","Đã hoàn thành? Được."),
            ("Đã hoàn thành!Chưa?","Đã hoàn thành! Chưa?"),
            ("😀 Đã hoàn thành .Xong!   \r\a","😀 Đã hoàn thành. Xong!   \r\a"),
            ("Đã hoàn tha\u0300nh.Xo\u0302ng!","Đã hoàn tha\u0300nh. Xo\u0302ng!")})
            assert(Find(sample.Item1).Any(i=>i.CanFix) && Apply(sample.Item1)==sample.Item2,
                "The short-final-sentence repair preserves words, decomposed Unicode, emoji, trailing spaces and paragraph terminators");
        foreach(string text in new[]{"Đã hoàn thành. Xong!","Đã hoàn thành.Xong...","Đã hoàn thành.Xong?!",
            "Đã hoàn thành.Xong! Nội dung","Đã hoàn thành.Xong!  ?","Đã hoàn thành.Xong, báo cáo",
            "Đã hoàn thành.Xong; báo cáo","Đã hoàn thành.Xong:","Đã hoàn thành.Xong…",
            "Đã hoàn thành.xong!","Đã hoàn thành.XONG!","Đã hoàn thành.XoNg!","Đã hoàn thành.X!",
            "Đã hoàn thành.12!","Đã hoàn thành.\"Xong!\"","Đã hoàn thành.(Xong!)",
            "Đã hoàn thành.Xong!\u00a0","Đã hoàn thành.Xong!\t","Đã hoàn thành.Xong!\v",
            "Đã hoàn thành.Xong!\nNội dung","Đã hoàn thành.\u00a0Xong!",
            "Tại tp.Hồ.","Nội dung TS.Xong!","Nội dung ii.Xong!","Giá trị 12.Xong!",
            "Nội dung...Xong!","Nội dung?!Xong!","duyệt.Xong!",
            "https://host/đã hoàn thành.Xong!","Đã hoàn thành.Xong! x+y","Đã hoàn thành.Xong! a=b",
            "Đã hoàn thành.Xong! user@example.com","Số: đã hoàn thành.Xong!",
            "Hà Nội, ngày 03 tháng 10 năm 2026 đã hoàn thành.Xong!"})
            assert(Find(text).Length==0,"The new terminal boundary does not admit unclear casing, numeric/technical/header/control contexts, punctuation sequences or interior punctuation");
        foreach(string word in new[]{"Tp","Tx","Tr","Ts","Ths","Pgs","Mr","Mrs","No","Etc","Com","Docx","Pdf","Txt","Exe","Vn"})
            assert(Find("Đã hoàn thành."+word+".").Length==0 && Find("Đã hoàn thành . "+word+".").Length==0,
                "A terminal abbreviation or common file/domain suffix cannot gain before/after sentence repairs");
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            const string text="😀 Đã hoàn thành .Xong!\r";
            var issues=Checker.Check([new(109,text)],profile).Where(Target).ToArray();
            assert(issues.Length==2 && issues.Single(i=>i.Kind==FixKind.SentenceAfterSpace).Start==109+text.IndexOf('.'),
                "Short final sentences retain the real paragraph CP after emoji in every profile");
            var guarded=Checker.Check([new(109,text,TextEditable:false)],profile);
            assert(guarded.All(i=>!i.CanFix) && guarded.Any(i=>i.Start==109 && i.Original==text.TrimEnd('\r')),
                "The new right boundary cannot bypass the paragraph's XML permission gate");
        }
        const string plain="Đã hoàn thành .Xong!";var choices=Find(plain);var selected=choices.Single(i=>i.Kind==FixKind.SentenceAfterSpace);
        assert(SentenceAfterSpaceRules.Check(new(0,plain,TextEditable:false)) is [{CanFix:false,Start:0}] &&
            SentenceInnerSpaceRules.Check(new(0,plain,TextEditable:false)) is [{CanFix:false,Start:0}],
            "Both direct sentence rules keep protected short final sentences manual");
        foreach(var fresh in new[]{Checker.Check([new(0,plain,TextEditable:false)]),
            Checker.Check([new(0,plain.Replace("Xong!","XONG!",StringComparison.Ordinal))]),
            Checker.Check([new(0,plain.Replace("Xong!","Xong,",StringComparison.Ordinal))])})
        {
            bool rejected=false;try{CurrentTextPlan.Validate([selected],fresh);}catch(InvalidOperationException){rejected=true;}
            assert(rejected,"The same punctuation CP is insufficient after XML, casing or the final sentence boundary changes");
        }
        var tooltip=IssueTooltipCatalog.Describe(selected);var review=IssueReviewCatalog.Describe(selected);
        assert(tooltip.Basis.Contains("Quy tắc ứng dụng") && !tooltip.Basis.Contains("Nghị định") &&
            review.Section==IssueReviewSection.ReviewSuggestion,"Short-final-sentence spacing remains a reviewed application rule in the existing group");
        var report=DevelopmentReport.Build("0.1.61",DateTimeOffset.UnixEpoch);
        assert(report.Contains("Đã hoàn thành.Xong!") && report.Contains("Đã hoàn thành. Xong!") &&
            report.Contains("câu cuối chỉ một từ") && DevelopmentReport.Count(CapabilityMode.Reviewable)==33,
            "The report includes executable one-word final-sentence examples and limits without changing capability totals");
        const string first="Đã hoàn thành .Xong!\r";
        const string second="Kế  hoạch kế hoạch; theo mẫu ( nội dung ) ,báo cáo; ghi chú:nội dung mẫu .   \r\a";
        const string sourceMixed=first+second;
        const string expectedMixed="Đã hoàn thành. Xong!\rKế hoạch; theo mẫu (nội dung), báo cáo; ghi chú: nội dung mẫu.\r\a";
        var edits=Checker.Check([new(0,first),new(first.Length,second)]).Where(i=>i.CanFix).ToArray();var plan=BatchPlan.Create(edits);
        CurrentTextPlan.Validate(plan,Checker.Check([new(0,first),new(first.Length,second)]));
        assert(Apply(sourceMixed,edits)==expectedMixed,
            "The short-sentence before/after repairs compose across paragraphs with initial repetition, parenthetical punctuation, colon and trailing spaces");
        var style=new Issue(0,first.Length-1,first[..^1],first[..^1],"",false,FixKind.DocumentHeadingFormat,CharacterFormat:new(13,false,true));
        var styled=BatchPlan.Create(edits.Append(style));
        assert(styled[0]==style && AtomicTextBatch.ExpectedStory(styled,sourceMixed,i=>sourceMixed[..i.Start])==expectedMixed,
            "Character formatting still precedes both contained short-sentence source spans");
        string current=sourceMixed;int writes=0,saves=0,restores=0;
        string Read(Issue i)=>current.Substring(i.Start,i.Length);
        string Write(Issue i){writes++;current=current.Remove(i.Start,i.Length).Insert(i.Start,i.Replacement);return i.Replacement;}
        AtomicTextBatch.Apply(plan,Read,Write,()=>{saves++;if(current!=expectedMixed)throw new Exception("story");},()=>{restores++;current=sourceMixed;});
        assert(current==expectedMixed && writes==plan.Count && saves==1 && restores==0,"The mixed short-sentence group commits only after exact story verification");
        current=sourceMixed;writes=saves=restores=0;bool failed=false;
        try{AtomicTextBatch.Apply(plan,Read,Write,()=>throw new IOException("save"),()=>{restores++;current=sourceMixed;});}catch(IOException){failed=true;}
        assert(failed && current==sourceMixed && writes==plan.Count && restores==1,"A failed final save restores both short-sentence edits and every other selected group");
    }
}
