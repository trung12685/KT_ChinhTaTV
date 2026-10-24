using ChinhTa.Core;

public static class ColonAfterSpaceChecks
{
    public static void Run(Action<bool,string> assert)
    {
        Issue[] Find(string text,int cp=0,bool editable=true)=>Checker.Check([new(cp,text,TextEditable:editable)])
            .Where(i=>i.Kind==FixKind.ColonSpace).ToArray();
        string Apply(string text,IEnumerable<Issue>? chosen=null)=>AtomicTextBatch.ExpectedStory(
            BatchPlan.Create(chosen??Checker.Check([new(0,text)]).Where(i=>i.CanFix)),text,i=>text[..i.Start]);
        foreach(var sample in new[]{("Ghi chú:nội dung.","Ghi chú: nội dung."),
            ("Kính gửi:cơ quan.","Kính gửi: cơ quan."),("Về việc:Nội dung.","Về việc: Nội dung."),
            ("Ghi chú :nội dung.","Ghi chú: nội dung."),("Ghi chú  :nội dung.","Ghi chú: nội dung."),
            ("Ghi chú:nội dung; ví dụ:văn bản.","Ghi chú: nội dung; ví dụ: văn bản."),
            ("😀 Ghi chú:nội dung.\r\a","😀 Ghi chú: nội dung.\r\a"),
            ("Ghi chu\u0301:no\u0323\u0302i dung.","Ghi chu\u0301: no\u0323\u0302i dung.")})
        {
            var found=Find(sample.Item1);var after=found.Where(i=>i.AddsSpaceAfterColon).ToArray();
            assert(after.Length>0 && after.All(i=>i.CanFix && !i.BatchSafe && i.Length==1 && i.Original==":" &&
                i.Replacement==": " && sample.Item1.Substring(i.Start,i.Length)==":" && i.Suggestion.Contains("Thêm một dấu cách")) &&
                Apply(sample.Item1)==sample.Item2,"A missing colon gap uses an exact positive colon anchor and preserves Unicode words and paragraph terminators");
            assert(!Checker.Check([new(0,sample.Item2)]).Any(i=>i.CanFix),"A combined colon repair has no residual supported spacing fixes");
        }
        foreach(string text in new[]{"Ghi chú: nội dung.","Ghi chú : nội dung.","Ghi chú:","Ghi chú:12",
            "Giờ bắt đầu:12:30","Tỉ lệ 1:2","Tỉ lệ iv:vi","Ghi chú:NỘI DUNG","Ghi chú:x","Ghi x:nội dung",
            "Chú:nội dung","Ghi CHÚ:nội dung","Ghi Chú:nội dung","Ghi chú::nội dung","Ghi chú:;nội dung",
            "Ghi chú:\tnội dung","Ghi chú\t:nội dung","Ghi chú:\u00a0nội dung","Ghi chú\u00a0:nội dung",
            "Ghi chú\r:nội dung","Ghi chú\vnội dung:văn bản","Ghi chú\nnội dung:văn bản",
            "Ghi chú:host.com","Ghi chú:name@example.com","Ghi chú:C:\\data","Ghi chú:/data","Ghi chú:host_name",
            "Ghi chú:value=abc","Ghi chú:[value]","Ghi chú:{value}","Ghi chú:<value>","Ghi chú:nội+chữ",
            "Truy cập http:host","Truy cập https:host","Truy cập ftp:host","Chọn file:name","Gửi mailto:name",
            "Dữ liệu data:value","key:value","abc.def ghi:nội dung","fn(ghi chú:nội dung)",
            "Số:nội dung","Ninh Bình, ngày 03 tháng 10 năm 2026 ghi chú:nội dung"})
            assert(!Find(text).Any(i=>i.AddsSpaceAfterColon),"The after-colon rule excludes times, ratios, identifiers, protocols, technical text, controls and unclear boundaries");
        const string both="Ghi chú  :nội dung.";
        var pair=Find(both);var before=pair.Single(i=>!i.AddsSpaceAfterColon);var following=pair.Single(i=>i.AddsSpaceAfterColon);
        assert(Apply(both,[following])=="Ghi chú  : nội dung." && Find(Apply(both,[following])).Single().Replacement=="",
            "Choosing only the after-gap insertion preserves the unselected before-gap error for the next scan");
        assert(Apply(both,[before])=="Ghi chú:nội dung." && Find(Apply(both,[before])).Single().AddsSpaceAfterColon,
            "Choosing only the before-gap deletion preserves the unselected missing after-gap error");
        var tooltip=IssueTooltipCatalog.Describe(following);var review=IssueReviewCatalog.Describe(following);
        assert(tooltip.Text.Split('\n').Length==3 && tooltip.Basis.Contains("Quy tắc ứng dụng") &&
            !tooltip.Basis.Contains("Nghị định") && tooltip.Action.Contains("Thêm một dấu cách") &&
            review.Section==IssueReviewSection.ReviewSuggestion && review.FamilyKey=="colon" && review.Label.Contains("Thiếu khoảng trắng sau"),
            "The compact after-gap item shares the colon group but has the correct action, short tooltip and application-rule source");
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            var rows=new[]{new Paragraph(91,"😀 Ghi chú:nội dung.\r")};var result=Checker.Check(rows,profile).Single(i=>i.AddsSpaceAfterColon);
            assert(result.Start==91+rows[0].Text.IndexOf(':') && result.Length==1,"All profiles anchor the colon to the actual paragraph CP including a preceding surrogate pair");
            var locked=Checker.Check([rows[0] with {TextEditable=false}],profile);
            assert(locked.Count==1 && !locked[0].CanFix && locked[0].Start==91 && !locked[0].AddsSpaceAfterColon,
                "A structured or unverified row stays one manual paragraph notice without a writable insertion anchor");
        }
        assert(ColonSpaceRules.Check(new(91,both,TextEditable:false)) is [{CanFix:false,Start:91}],
            "The direct rule returns one manual notice when both colon edges need repair in a protected range");
        bool stale=false;try{CurrentTextPlan.Validate([following],Checker.Check([new(0,both,TextEditable:false)]));}
        catch(InvalidOperationException){stale=true;}
        assert(stale,"A stored after-gap choice loses write permission when fresh XML evidence becomes unverified");
        bool contextChanged=false;
        try{CurrentTextPlan.Validate([following],Checker.Check([new(0,"Ghi chú  :12 giờ.")]));}
        catch(InvalidOperationException){contextChanged=true;}
        assert(contextChanged,"An unchanged colon anchor is insufficient when the following word changed into a numeric field");
        const string mixed="Ghi chú  :nội dung  ( kế hoạch kế hoạch ) đã duyệt;hoàn thành  .";
        const string expected="Ghi chú: nội dung (kế hoạch) đã duyệt; hoàn thành.";
        var edits=Checker.Check([new(0,mixed)]).Where(i=>i.CanFix).ToArray();var plan=BatchPlan.Create(edits);
        assert(edits.Count(i=>i.AddsSpaceAfterColon)==1 && edits.Count(i=>i.Kind==FixKind.ColonSpace)==2 && Apply(mixed)==expected,
            "Both colon edges compose with generic spacing, parentheses, repetition and other punctuation in one nonoverlapping batch");
        var style=new Issue(0,mixed.Length,mixed,mixed,"",false,FixKind.DocumentHeadingFormat,CharacterFormat:new(13,false,true));
        var styled=BatchPlan.Create(edits.Append(style));
        assert(styled[0]==style && AtomicTextBatch.ExpectedStory(styled,mixed,i=>mixed[..i.Start])==expected,
            "Character formatting still precedes every contained colon insertion and deletion without losing the expected story");
        string current=mixed;int writes=0,restores=0,saves=0;
        string Read(Issue i)=>current.Substring(i.Start,i.Length);
        string Write(Issue i){writes++;current=current.Remove(i.Start,i.Length).Insert(i.Start,i.Replacement);return i.Replacement;}
        AtomicTextBatch.Apply(plan,Read,Write,()=>{saves++;if(current!=expected)throw new Exception("story");},()=>{restores++;current=mixed;});
        assert(current==expected && writes==plan.Count && saves==1 && restores==0,"The full selected group commits once after all expected text edits");
        current=mixed;writes=restores=saves=0;bool failed=false;
        try{AtomicTextBatch.Apply(plan,Read,Write,()=>throw new IOException("synthetic save failure"),()=>{restores++;current=mixed;});}
        catch(IOException){failed=true;}
        assert(failed && current==mixed && writes==plan.Count && restores==1,"A final save failure restores all selected punctuation and repetition edits including the new colon insertion");
    }
}
