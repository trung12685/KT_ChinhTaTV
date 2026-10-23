using ChinhTa.Core;

public static class SentenceAfterSpaceChecks
{
    public static void Run(Action<bool,string> assert)
    {
        Issue[] Find(string text)=>Checker.Check([new(0,text)]).Where(i=>i.Kind==FixKind.SentenceAfterSpace).ToArray();
        string Apply(string text,IEnumerable<Issue>? chosen=null)=>AtomicTextBatch.ExpectedStory(
            BatchPlan.Create(chosen??Checker.Check([new(0,text)]).Where(i=>i.CanFix)),text,i=>text[..i.Start]);
        foreach(var sample in new[]{("Đã hoàn thành.Tiếp tục báo cáo","Đã hoàn thành. Tiếp tục báo cáo"),
            ("Đã hoàn thành?Tiếp tục báo cáo","Đã hoàn thành? Tiếp tục báo cáo"),
            ("Xin cảm ơn!Chúng tôi tiếp tục","Xin cảm ơn! Chúng tôi tiếp tục"),
            ("Nội dung.Tiếp","Nội dung. Tiếp"),
            ("😀 Nội dung.Tiếp theo\r\a","😀 Nội dung. Tiếp theo\r\a"),
            ("Bản ke\u0302\u0301 hoa\u0323ch.Tie\u0302\u0301p theo","Bản ke\u0302\u0301 hoa\u0323ch. Tie\u0302\u0301p theo")})
        {
            var edits=Find(sample.Item1);
            assert(edits.Length==1 && edits[0].CanFix && !edits[0].BatchSafe && edits[0].Length==1 &&
                ".?!".Contains(edits[0].Original) && edits[0].Replacement==edits[0].Original+" " &&
                edits[0].Original==sample.Item1.Substring(edits[0].Start,1) && edits[0].Suggestion.Contains("giữ chữ") &&
                Apply(sample.Item1)==sample.Item2,"Missing sentence spacing uses exactly the existing punctuation range and adds one ASCII space without rewriting either word");
            assert(!Checker.Check([new(0,sample.Item2)]).Any(i=>i.CanFix),"The repaired missing-after-space item disappears on rescan");
        }
        foreach(string text in new[]{"Đã hoàn thành. Tiếp tục", "Đã hoàn thành.  Tiếp tục", "Đã hoàn thành.",
            "Đã hoàn thành.tiếp tục", "Đã hoàn thành.TIẾP tục", "Đã hoàn thành.T tiếp tục", "Đã hoàn thành.ThS tiếp tục",
            "Đã hoàn thành.1 tiếp tục", "Nội dung...Tiếp theo", "Nội dung . .Tiếp theo", "Nội dung?!Tiếp theo",
            "Nội dung…Tiếp theo", "Giá trị 1.Tiếp theo", "Giá trị 1,25.Tiếp theo", "Số thứ ii.Tiếp theo",
            "Nội dung TS.Tiếp theo", "Nội dung ThS.Tiếp theo", "Nội dung v.v.Tiếp theo", "Tại tp.Hồ Chí Minh",
            "Theo dr.Smith báo cáo", "Mở file.Docx tiếp theo", "Địa chỉ host.Com tiếp theo", "Nội dung host.com.Tiếp theo",
            "https://host/nội dung.Tiếp theo", "name@example.com nội dung.Tiếp theo", "C:\\nội dung.Tiếp theo",
            "mã_abc nội dung.Tiếp theo", "alpha = beta.Tiếp theo", "Nội dung\tđã duyệt.Tiếp theo",
            "Nội dung\vđã duyệt.Tiếp theo", "Nội dung\fđã duyệt.Tiếp theo", "Nội dung\nđã duyệt.Tiếp theo",
            "Nội dung đã duyệt\u00a0.Tiếp theo", "Nội dung đã duyệt.\u00a0Tiếp theo", "Nội dung đã duyệt.Tiếp\u00a0theo",
            "Số: đã duyệt.Tiếp theo", "Ninh Bình, ngày 03 tháng 10 năm 2026 đã duyệt.Tiếp theo", "duyệt.Tiếp theo",
            "Trước đó.duyệt.Tiếp theo", "Nội dung a\u0301.Tiếp theo", "Nội dung đã duyệt.Tp. Hồ Chí Minh",
            "Đã hoàn thành.\"Tiếp tục\"", "Nội dung đã duyệt.)Tiếp theo"})
            assert(Find(text).Length==0,"Missing-after-space scope excludes existing spaces, abbreviations/technical/numeric/control and unclear boundaries: "+text);
        foreach(char mark in ".?!")
        {
            string original="Đã hoàn thành  "+mark+"Tiếp tục báo cáo",expected="Đã hoàn thành"+mark+" Tiếp tục báo cáo";
            var all=Checker.Check([new(0,original)]).Where(i=>i.CanFix).ToArray();
            var before=all.Single(i=>i.Kind==FixKind.SentenceInnerSpace);var after=all.Single(i=>i.Kind==FixKind.SentenceAfterSpace);
            assert(all.Length==2 && before.Start+before.Length==after.Start && Apply(original)==expected,
                "Adjacent before/after spans repair both sides in one batch without overlap or a second repair cycle");
            string onlyAfter=Apply(original,[after]),onlyBefore=Apply(original,[before]);
            assert(onlyAfter=="Đã hoàn thành  "+mark+" Tiếp tục báo cáo" &&
                Checker.Check([new(0,onlyAfter)]).Single(i=>i.CanFix).Kind==FixKind.SentenceInnerSpace,
                "Selecting only after-space preserves the unselected before-space issue");
            assert(onlyBefore=="Đã hoàn thành"+mark+"Tiếp tục báo cáo" && Find(onlyBefore).Length==1,
                "Selecting only before-space preserves the unselected missing-after-space issue");
        }
        const string originalMixed="Ghi chú  :  nội dung  ( kế hoạch kế hoạch ) đã duyệt;hoàn thành  .Tiếp tục báo cáo!Chúng tôi đã xong  .";
        const string expectedMixed="Ghi chú: nội dung (kế hoạch) đã duyệt; hoàn thành. Tiếp tục báo cáo! Chúng tôi đã xong.";
        var editsAll=Checker.Check([new(0,originalMixed)]).Where(i=>i.CanFix).ToArray();
        assert(Find(originalMixed).Length==2 && Apply(originalMixed)==expectedMixed,
            "Multiple insertions share a descending plan with deletions/replacement/colon/parentheses/repetition and final punctuation");
        var style=new Issue(0,originalMixed.Length,originalMixed,originalMixed,"",false,FixKind.DocumentHeadingFormat,CharacterFormat:new(13,false,true));
        var styled=BatchPlan.Create(editsAll.Append(style));
        assert(styled[0]==style && AtomicTextBatch.ExpectedStory(styled,originalMixed,i=>originalMixed[..i.Start])==expectedMixed,
            "Full-row character formatting precedes contained positive-length punctuation replacement spans");
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            var found=Checker.Check([new(90,"Nội dung.Tiếp theo")],profile).Where(i=>i.Kind==FixKind.SentenceAfterSpace).ToArray();
            assert(found.Length==1 && found[0].Start==98,"Missing-after-space retains the paragraph's actual offset in each profile");
            var guarded=Checker.Check([new(90,"Nội dung.Tiếp theo",TextEditable:false)],profile);
            assert(guarded.Count==1 && !guarded[0].CanFix && guarded[0].Start==90 && guarded[0].Original=="Nội dung.Tiếp theo",
                "Unverified or structured text keeps one whole-paragraph manual notice");
        }
        var direct=SentenceAfterSpaceRules.Check(new(90,"Nội dung.Tiếp theo",TextEditable:false));
        assert(direct.Count==1 && !direct[0].CanFix && direct[0].Start==90,"The direct after-space rule respects XML eligibility");
        bool stale=false;
        try{CurrentTextPlan.Validate(Find("Nội dung.Tiếp theo"),Checker.Check([new(0,"Nội dung.Tiếp theo",TextEditable:false)]));}
        catch(InvalidOperationException){stale=true;}
        assert(stale,"Current eligibility invalidates a stale missing-after-space selection before writing");
        string current=originalMixed;int writes=0,saves=0,restores=0;
        var plan=BatchPlan.Create(editsAll);
        string Read(Issue issue)=>current.Substring(issue.Start,issue.Length);
        string Write(Issue issue){writes++;current=current.Remove(issue.Start,issue.Length).Insert(issue.Start,issue.Replacement);return issue.Replacement;}
        AtomicTextBatch.Apply(plan,Read,Write,()=>{saves++;if(current!=expectedMixed)throw new Exception("story");},()=>{restores++;current=originalMixed;});
        assert(writes==editsAll.Length && saves==1 && restores==0,"Mixed insertions/deletions commit once after exact whole-story verification");
        foreach(int fault in new[]{2,4,editsAll.Length})
        {
            current=originalMixed;writes=saves=restores=0;bool failed=false;
            try{AtomicTextBatch.Apply(plan,Read,issue=>{var value=Write(issue);if(writes==fault)throw new IOException("injected");return value;},()=>saves++,()=>{restores++;current=originalMixed;});}
            catch(IOException){failed=true;}
            assert(failed && current==originalMixed && writes==fault && restores==1 && saves==0,
                "Failure after insertion or adjacent deletion restores the entire mixed plan once without partial commit");
        }
        assert(DevelopmentReport.Capabilities.Single(c=>c.Id=="C19").Mode==CapabilityMode.Reviewable &&
            DevelopmentReport.Milestones.Any(m=>m.Version=="0.1.49"),"The report records C19 and the new milestone");
    }
}
