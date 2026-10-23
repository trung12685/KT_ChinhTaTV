using ChinhTa.Core;

public static class SentenceInnerSpaceChecks
{
    public static void Run(Action<bool,string> assert)
    {
        Issue[] Find(string text)=>Checker.Check([new(0,text)]).Where(i=>i.Kind==FixKind.SentenceInnerSpace).ToArray();
        string Apply(string text,IEnumerable<Issue>? chosen=null)=>AtomicTextBatch.ExpectedStory(
            BatchPlan.Create(chosen??Checker.Check([new(0,text)]).Where(i=>i.CanFix)),text,i=>text[..i.Start]);
        foreach(var sample in new[]{("Đã hoàn thành . Tiếp tục báo cáo","Đã hoàn thành. Tiếp tục báo cáo"),
            ("Đã hoàn thành  ? Tiếp tục báo cáo","Đã hoàn thành? Tiếp tục báo cáo"),
            ("Xin cảm ơn   ! Chúng tôi tiếp tục","Xin cảm ơn! Chúng tôi tiếp tục"),
            ("Nội dung . Tiếp theo","Nội dung. Tiếp theo"),
            ("Nội dung . Tiếp","Nội dung. Tiếp"),
            ("😀 Nội dung . Tiếp theo\r\a","😀 Nội dung. Tiếp theo\r\a"),
            ("Bản ke\u0302\u0301 hoa\u0323ch . Tie\u0302\u0301p theo","Bản ke\u0302\u0301 hoa\u0323ch. Tie\u0302\u0301p theo")})
        {
            var edits=Find(sample.Item1);
            assert(edits.Length==1 && edits[0].CanFix && !edits[0].BatchSafe && edits[0].Replacement=="" &&
                edits[0].Original.All(c=>c==' ') && edits[0].Original==sample.Item1.Substring(edits[0].Start,edits[0].Length) &&
                edits[0].Suggestion.Contains("giữ chữ") && Apply(sample.Item1)==sample.Item2,
                "Internal sentence correction deletes exactly the ASCII spaces, preserving words, punctuation, Unicode and terminators");
            assert(!Checker.Check([new(0,sample.Item2)]).Any(i=>i.CanFix),"Rescan does not retain the repaired internal sentence issue");
        }
        foreach(string text in new[]{"Đã hoàn thành. Tiếp tục", "Đã hoàn thành .", "Đã hoàn thành . tiếp tục", "Đã hoàn thành . TIẾP tục",
            "Đã hoàn thành . T tiếp tục", "Đã hoàn thành . ThS tiếp tục", "Đã hoàn thành .Tiếp tục", "Đã hoàn thành . 1 tiếp tục",
            "Nội dung ... Tiếp theo", "Nội dung . . Tiếp theo", "Nội dung ? ! Tiếp theo", "Nội dung !? Tiếp theo",
            "Nội dung … Tiếp theo", "Giá trị 1 . Tiếp theo", "Giá trị 1,25 . Tiếp theo", "Số thứ ii . Tiếp theo",
            "Nội dung TS . Tiếp theo", "Nội dung ThS . Tiếp theo", "Nội dung v.v . Tiếp theo", "Nội dung v.v. . Tiếp theo",
            "Tại tp . Hồ Chí Minh", "Theo dr . Smith báo cáo", "Mở file . Docx tiếp theo", "Địa chỉ host . Com tiếp theo",
            "Nội dung host.com . Tiếp theo", "https://host/nội dung . Tiếp theo", "name@example.com nội dung . Tiếp theo",
            "C:\\nội dung . Tiếp theo", "mã_abc nội dung . Tiếp theo", "alpha = beta . Tiếp theo",
            "Nội dung\tđã duyệt . Tiếp theo", "Nội dung\vđã duyệt . Tiếp theo", "Nội dung\fđã duyệt . Tiếp theo",
            "Nội dung\nđã duyệt . Tiếp theo", "Nội dung đã duyệt\u00a0. Tiếp theo", "Nội dung đã duyệt .\u00a0Tiếp theo",
            "Nội dung đã duyệt . Tiếp\u00a0theo", "Số: đã duyệt . Tiếp theo", "Ninh Bình, ngày 03 tháng 10 năm 2026 đã duyệt . Tiếp theo",
            "duyệt . Tiếp theo", "Trước đó. duyệt . Tiếp theo", "Nội dung a\u0301 . Tiếp theo", "Nội dung đã duyệt . Tp. Hồ Chí Minh"})
            assert(Find(text).Length==0,"Internal sentence scope excludes numeric, known abbreviation/technical, control and uncertain boundaries: "+text);
        const string original="Ghi chú  :  nội dung  ( kế hoạch kế hoạch ) đã duyệt;hoàn thành  .  Tiếp tục báo cáo  ! Chúng tôi đã xong  .";
        const string expected="Ghi chú: nội dung (kế hoạch) đã duyệt; hoàn thành. Tiếp tục báo cáo! Chúng tôi đã xong.";
        var editsAll=Checker.Check([new(0,original)]).Where(i=>i.CanFix).ToArray();
        assert(Find(original).Length==2 && Apply(original)==expected,
            "Internal punctuation shares one nonoverlapping plan with colon, parentheses, repetition, semicolon, generic spaces and final punctuation");
        var first=Find(original).First();
        string only=Apply(original,[first]);
        assert(only==original.Remove(first.Start,first.Length) && only.Contains(".  Tiếp") && Find(only).Length==1 &&
            Checker.Check([new(0,only)]).Count(i=>i.CanFix)==editsAll.Length-1,
            "A single selected deletion preserves all unselected errors including spaces after the mark");
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            var found=Checker.Check([new(90,"Nội dung . Tiếp theo")],profile).Where(i=>i.Kind==FixKind.SentenceInnerSpace).ToArray();
            assert(found.Length==1 && found[0].Start==98,"The actual paragraph start is retained across all profiles");
            var guarded=Checker.Check([new(90,"Nội dung . Tiếp theo",TextEditable:false)],profile);
            assert(guarded.Count==1 && !guarded[0].CanFix && guarded[0].Start==90 && guarded[0].Original=="Nội dung . Tiếp theo",
                "A structured row produces one manual whole-row warning without guessed editable substring offsets");
        }
        var direct=SentenceInnerSpaceRules.Check(new(90,"Nội dung . Tiếp theo",TextEditable:false));
        assert(direct.Count==1 && !direct[0].CanFix && direct[0].Start==90,"The direct rule also respects XML eligibility");
        bool stale=false;
        try{CurrentTextPlan.Validate(Find("Nội dung . Tiếp theo"),Checker.Check([new(0,"Nội dung . Tiếp theo",TextEditable:false)]));}
        catch(InvalidOperationException){stale=true;}
        assert(stale,"A stale sentence choice is rejected when current XML eligibility changes");
        var format=new Issue(0,original.Length,original,original,"",false,FixKind.DocumentHeadingFormat,CharacterFormat:new(13,false,true));
        var styled=BatchPlan.Create(editsAll.Append(format));
        assert(styled[0]==format && AtomicTextBatch.ExpectedStory(styled,original,i=>original[..i.Start])==expected,
            "Full-row character formatting precedes contained internal sentence and other text edits");
        string current=original;int writes=0,saves=0,restores=0;
        var plan=BatchPlan.Create(editsAll);
        string Read(Issue issue)=>current.Substring(issue.Start,issue.Length);
        string Write(Issue issue){writes++;current=current.Remove(issue.Start,issue.Length).Insert(issue.Start,issue.Replacement);return issue.Replacement;}
        AtomicTextBatch.Apply(plan,Read,Write,()=>{saves++;if(current!=expected)throw new Exception("story");},()=>{restores++;current=original;});
        assert(writes==editsAll.Length && saves==1 && restores==0,"The full mixed plan commits once after exact story verification");
        current=original;writes=saves=restores=0;bool failed=false;
        try{AtomicTextBatch.Apply(plan,Read,issue=>{var value=Write(issue);if(writes==4)throw new IOException("injected");return value;},()=>saves++,()=>{restores++;current=original;});}
        catch(IOException){failed=true;}
        assert(failed && current==original && writes==4 && restores==1 && saves==0,
            "Failure during the mixed plan restores the whole original story once without partial commit");
        assert(DevelopmentReport.Capabilities.Single(c=>c.Id=="C18").Mode==CapabilityMode.Reviewable &&
            DevelopmentReport.Milestones.Any(m=>m.Version=="0.1.48"),"The report records C18 and its development milestone");
    }
}
