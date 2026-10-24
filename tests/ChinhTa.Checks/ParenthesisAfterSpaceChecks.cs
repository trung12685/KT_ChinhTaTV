using ChinhTa.Core;

public static class ParenthesisAfterSpaceChecks
{
    public static void Run(Action<bool,string> assert)
    {
        Issue[] Find(string text)=>Checker.Check([new(0,text)]).Where(i=>i.Kind==FixKind.ParenthesisSpace).ToArray();
        string Apply(string text,IEnumerable<Issue>? chosen=null)=>AtomicTextBatch.ExpectedStory(
            BatchPlan.Create(chosen??Checker.Check([new(0,text)]).Where(i=>i.CanFix)),text,i=>text[..i.Start]);
        foreach(var sample in new[]{("Theo mẫu (nội dung)đã duyệt.","Theo mẫu (nội dung) đã duyệt."),
            ("Theo mẫu ( nội dung )đã duyệt.","Theo mẫu (nội dung) đã duyệt."),
            ("Theo mẫu  (  nội dung  )đã  duyệt.","Theo mẫu (nội dung) đã duyệt."),
            ("Theo mẫu (nội dung)đã duyệt; theo mẫu (báo cáo)đã xong.","Theo mẫu (nội dung) đã duyệt; theo mẫu (báo cáo) đã xong."),
            ("😀 Theo mẫu (nội dung)đã duyệt.\r\a","😀 Theo mẫu (nội dung) đã duyệt.\r\a"),
            ("Theo mẫu (ke\u0302\u0301 hoa\u0323ch)đa\u0303 duye\u0323\u0302t.","Theo mẫu (ke\u0302\u0301 hoa\u0323ch) đa\u0303 duye\u0323\u0302t.")})
        {
            var found=Find(sample.Item1).Where(i=>i.AddsSpaceAfterClosingParenthesis).ToArray();
            assert(found.Length>0 && found.All(i=>i.CanFix && !i.BatchSafe && i.Length==1 && i.Original==")" &&
                i.Replacement==") " && sample.Item1.Substring(i.Start,i.Length)==")" && i.Suggestion.Contains("Thêm một dấu cách")) &&
                Apply(sample.Item1)==sample.Item2,"A missing closing-parenthesis gap uses a positive delimiter anchor without rewriting words or Unicode/paragraph ends");
            assert(!Checker.Check([new(0,sample.Item2)]).Any(i=>i.CanFix),"One combined repair leaves no residual supported parenthesis or whitespace error");
        }
        foreach(string text in new[]{"Theo mẫu (nội dung) đã duyệt.","Theo mẫu (nội dung), đã duyệt.",
            "Theo mẫu (nội dung).","Theo mẫu (nội dung)đã", "Theo mẫu (nội dung)đã UBND",
            "Theo mẫu (nội dung)Đã duyệt.","Theo mẫu (nội dung)x duyệt.","Theo mẫu (nội dung)đã x.",
            "Theo mẫu (nội dung)đã 12.","Theo mẫu (nội dung)12 giờ.","Theo mẫu (nội dung)đã host.com",
            "(nội dung)đã duyệt.","Mẫu (nội dung)đã duyệt.","Theo Mẫu (nội dung)đã duyệt.",
            "Theo mẫu (a)đã duyệt.","Theo mẫu (ABC)đã duyệt.","Theo mẫu (12 34)đã duyệt.",
            "Theo mẫu (báo)đã duyệt.","Theo mẫu (x y)đã duyệt.","Theo mẫu (nội,dung)đã duyệt.",
            "Theo mẫu (nội dung (bổ sung))đã duyệt.","Theo mẫu (nội dung)đã duyệt (",
            "Theo mẫu (nội dung)đã duyệt)","fn(nội dung)đã duyệt.","Tính sin (alpha beta)gamma delta",
            "Tính log (alpha beta)gamma delta","Gọi function (alpha beta)gamma delta",
            "Theo mẫu (nội dung)\tđã duyệt.","Theo mẫu (nội dung)\u00a0đã duyệt.",
            "Theo mẫu (nội\tdung)đã duyệt.","Theo mẫu (nội\ndung)đã duyệt.",
            "Theo mẫu (nội dung)đã /data.","Theo mẫu (nội dung)đã host_name.",
            "Theo mẫu (nội dung)đã name@example.com","Theo mẫu (nội dung)đã value=abc",
            "Theo mẫu (nội dung)đã x+y","Theo mẫu (nội dung)đã [value]",
            "Số: (nội dung)đã duyệt.","Hà Nội, ngày 03 tháng 10 năm 2026 (nội dung)đã duyệt.",
            "Theo mẫu (    )đã duyệt."})
            assert(!Find(text).Any(i=>i.AddsSpaceAfterClosingParenthesis),"Attached function calls, formulas, codes, nested/unbalanced/empty parentheses and unclear prose stay outside the missing-gap rule");
        const string both="Theo mẫu ( nội dung )đã duyệt.";
        var all=Find(both);var after=all.Single(i=>i.AddsSpaceAfterClosingParenthesis);var inside=all.Where(i=>!i.AddsSpaceAfterClosingParenthesis).ToArray();
        assert(Apply(both,[after])=="Theo mẫu ( nội dung ) đã duyệt." && Find(Apply(both,[after])).Length==2,
            "Selecting only the new gap preserves every unselected inner-edge error");
        assert(Apply(both,inside)=="Theo mẫu (nội dung)đã duyệt." && Find(Apply(both,inside)).Single().AddsSpaceAfterClosingParenthesis,
            "Selecting only inner-edge deletions preserves the actionable missing gap outside");
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            const string text="😀 Theo mẫu (nội dung)đã duyệt.\r";
            var result=Checker.Check([new(91,text)],profile).Single(i=>i.AddsSpaceAfterClosingParenthesis);
            assert(result.Start==91+text.IndexOf(')') && result.Length==1,"Every profile uses the actual Word paragraph offset for the delimiter after an emoji");
            var guarded=Checker.Check([new(91,text,TextEditable:false)],profile);
            assert(guarded.Count==1 && !guarded[0].CanFix && guarded[0].Start==91,"A structured row stays one manual notice and cannot gain an insertion through the new rule");
        }
        assert(ParenthesisSpaceRules.Check(new(91,both,TextEditable:false)) is [{CanFix:false,Start:91}],
            "The direct rule returns one manual paragraph notice even when inner and outer gaps coexist");
        bool stale=false;try{CurrentTextPlan.Validate([after],Checker.Check([new(0,both,TextEditable:false)]));}
        catch(InvalidOperationException){stale=true;}
        assert(stale,"A stale missing-parenthesis-gap choice loses permission when fresh XML evidence becomes structured");
        string numeric=both.Replace("đã duyệt","12 giờ",StringComparison.Ordinal);bool changed=false;
        try{CurrentTextPlan.Validate([after],Checker.Check([new(0,numeric)]));}catch(InvalidOperationException){changed=true;}
        assert(changed,"The same closing-delimiter CP is insufficient when its following context becomes numeric");
        var tooltip=IssueTooltipCatalog.Describe(after);var review=IssueReviewCatalog.Describe(after);
        assert(tooltip.Text.Split('\n').Length==3 && tooltip.Basis.Contains("Quy tắc ứng dụng") && !tooltip.Basis.Contains("Nghị định") &&
            tooltip.Action.Contains("Thêm một dấu cách") && review is {FamilyKey:"parenthesis",Section:IssueReviewSection.ReviewSuggestion} && review.Label.Contains("sau dấu đóng ngoặc"),
            "The new item shares the parenthesis group with a correct short label, action and nonlegal source");
        const string mixed="Theo mẫu ( kế hoạch kế hoạch )đã duyệt; ghi chú:nội dung mẫu  .   \r\a";
        const string expected="Theo mẫu (kế hoạch) đã duyệt; ghi chú: nội dung mẫu.\r\a";
        var edits=Checker.Check([new(0,mixed)]).Where(i=>i.CanFix).ToArray();var plan=BatchPlan.Create(edits);
        CurrentTextPlan.Validate(plan,Checker.Check([new(0,mixed)]));
        assert(Apply(mixed)==expected,"Inner/outer parenthesis gaps compose with repetition, colon edges, punctuation and trailing spaces in one batch");
        var whole=mixed.TrimEnd('\r','\a');var style=new Issue(0,whole.Length,whole,whole,"",false,FixKind.DocumentHeadingFormat,CharacterFormat:new(13,false,true));
        var styled=BatchPlan.Create(edits.Append(style));
        assert(styled[0]==style && AtomicTextBatch.ExpectedStory(styled,mixed,i=>mixed[..i.Start])==expected,
            "Character-only formatting still precedes all contained mixed text edits including a closing-parenthesis insertion");
        string current=mixed;int writes=0,saves=0,restores=0;
        string Read(Issue i)=>current.Substring(i.Start,i.Length);
        string Write(Issue i){writes++;current=current.Remove(i.Start,i.Length).Insert(i.Start,i.Replacement);return i.Replacement;}
        AtomicTextBatch.Apply(plan,Read,Write,()=>{saves++;if(current!=expected)throw new Exception("story");},()=>{restores++;current=mixed;});
        assert(current==expected && writes==plan.Count && saves==1 && restores==0,"The selected mixed group commits once after exact whole-story verification");
        current=mixed;writes=saves=restores=0;bool failed=false;
        try{AtomicTextBatch.Apply(plan,Read,Write,()=>throw new IOException("synthetic save failure"),()=>{restores++;current=mixed;});}
        catch(IOException){failed=true;}
        assert(failed && current==mixed && writes==plan.Count && restores==1,"A failing final save restores the complete mixed group including words, gaps and paragraph terminators");
    }
}
