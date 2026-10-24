using ChinhTa.Core;

public static class ParagraphEndSpaceChecks
{
    public static void Run(Action<bool,string> assert)
    {
        Issue[] Find(string text,int cp=0)=>Checker.Check([new(cp,text)]).Where(i=>i.Kind==FixKind.ParagraphEndSpace).ToArray();
        string Apply(string text,IEnumerable<Issue>? chosen=null)=>AtomicTextBatch.ExpectedStory(
            BatchPlan.Create(chosen??Checker.Check([new(0,text)]).Where(i=>i.CanFix)),text,i=>text[..i.Start]);
        foreach(var sample in new[]{("Đã hoàn thành.   ","Đã hoàn thành."),("Đã hoàn thành? ","Đã hoàn thành?"),
            ("Xin cảm ơn!  ","Xin cảm ơn!"),("Đã hoàn thành .   ","Đã hoàn thành."),
            ("Đã hoàn thành.   \r\a","Đã hoàn thành.\r\a"),("Đã hoàn thành!  \n","Đã hoàn thành!\n"),
            ("😀 Nội dung đã duyệt.  \r","😀 Nội dung đã duyệt.\r"),
            ("Bản ke\u0302\u0301 hoa\u0323ch.  ","Bản ke\u0302\u0301 hoa\u0323ch.")})
        {
            var found=Find(sample.Item1);
            assert(found is [{CanFix:true,BatchSafe:false,Replacement:""}] && found[0].Original.All(c=>c==' ') &&
                found[0].Original==sample.Item1.Substring(found[0].Start,found[0].Length) && found[0].Length>0 &&
                Apply(sample.Item1)==sample.Item2,"Paragraph-end repairs remove exact ASCII gaps after a final sentence mark while retaining Unicode and every paragraph/cell terminator");
            assert(!Checker.Check([new(0,sample.Item2)]).Any(i=>i.CanFix),"A combined repair leaves no residual supported paragraph or punctuation gap");
        }
        foreach(string text in new[]{"Đã hoàn thành.","Đã hoàn thành   ","    ","Nội dung:   ","Nội dung;   ",
            "Nội dung...   ","Nội dung . .   ","Nội dung?!   ","Nội dung…   ","duyệt.   ",
            "Nội dung A.   ","Nội dung ii.   ","Nội dung TS.   ","Nội dung ts.   ","Nội dung ThS.   ",
            "Báo cáo 12.   ","Dòng số 1,25.   ","Chỉ mục a.   ","NỘI DUNG.   ","Nội dung Duyệt.   ",
            "Mở host.com.   ","Mở C:\\data.   ","Mở /data.   ","Gửi name@example.com.   ","mã_abc nội dung.   ",
            "alpha = beta.   ","alpha + beta.   ","Ghi chú [nội dung].   ","Nội dung\tđã duyệt.   ",
            "Nội dung đã duyệt.\t   ","Nội dung đã duyệt.\u00a0   ","Nội dung đã duyệt.   \u00a0",
            "Nội dung đã duyệt.\v   ","Nội dung đã duyệt.\f   ","Nội dung\nđã duyệt.   ",
            "Số:        /BC-ABC", "Số: đã duyệt.   ","Hà Nội, ngày        tháng 10 năm 2026 ghi chú.   ",
            "Trước đó. duyệt.   ","fn(nội dung).   ","Nội dung (    )   "})
            assert(Find(text).Length==0,"The trailing-gap scope excludes intentional blanks, unfinished paragraphs, identifiers, dates, abbreviations, technical text and unclear endings");
        const string both="Đã hoàn thành  .   \r";
        var trailing=Find(both).Single();var before=Checker.Check([new(0,both)]).Single(i=>i.Kind==FixKind.SentenceEndSpace);
        assert(Apply(both,[trailing])=="Đã hoàn thành  .\r" && Checker.Check([new(0,Apply(both,[trailing]))]).Any(i=>i.Kind==FixKind.SentenceEndSpace && i.CanFix),
            "Removing the trailing gap alone preserves the independently selected spaces before the final mark");
        assert(Apply(both,[before])=="Đã hoàn thành.   \r" && Find(Apply(both,[before])).Length==1,
            "Removing spaces before the mark alone preserves the unselected paragraph-end gap");
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            const string text="😀 Nội dung đã duyệt.  \r\a";
            var found=Checker.Check([new(91,text)],profile).Single(i=>i.Kind==FixKind.ParagraphEndSpace);
            assert(found.Start==91+text.IndexOf("  ",StringComparison.Ordinal) && found.Original=="  ",
                "Every profile uses the actual paragraph CP after Unicode, never the paragraph marker as a deletion span");
            var locked=Checker.Check([new(91,text,TextEditable:false)],profile);
            assert(locked.Count==1 && !locked[0].CanFix && locked[0].Start==91 && locked[0].Original==text.TrimEnd('\r','\a'),
                "A structured or unverified range stays one manual whole-paragraph notice");
        }
        assert(ParagraphEndSpaceRules.Check(new(91,both,TextEditable:false)) is [{CanFix:false,Start:91}],
            "The direct paragraph-end rule also enforces XML/text edit permission");
        bool stale=false;try{CurrentTextPlan.Validate([trailing],Checker.Check([new(0,both,TextEditable:false)]));}
        catch(InvalidOperationException){stale=true;}
        assert(stale,"A stored trailing-space choice is rejected before writing when the XML permission changes");
        var renamed=both.Replace("thành","THÀNH",StringComparison.Ordinal);bool contextChanged=false;
        try{CurrentTextPlan.Validate([trailing],Checker.Check([new(0,renamed)]));}catch(InvalidOperationException){contextChanged=true;}
        assert(contextChanged,"Identical trailing spaces and CPs are insufficient when the last word no longer meets the prose guard");
        var tooltip=IssueTooltipCatalog.Describe(trailing);var review=IssueReviewCatalog.Describe(trailing);
        assert(tooltip.Text.Split('\n').Length==3 && tooltip.Basis.Contains("Quy tắc ứng dụng") && !tooltip.Basis.Contains("Nghị định") &&
            tooltip.Action.Contains("dấu kết đoạn/ô") && review is {FamilyKey:"spacing",Section:IssueReviewSection.ReviewSuggestion} &&
            review.Label=="Thừa khoảng trắng cuối đoạn", "The new gap stays beside other whitespace errors with a concise action and no invented legal citation");
        const string mixed="Ghi chú :nội dung  ( kế hoạch kế hoạch ) đã duyệt;hoàn thành  .   \r\a";
        const string expected="Ghi chú: nội dung (kế hoạch) đã duyệt; hoàn thành.\r\a";
        var edits=Checker.Check([new(0,mixed)]).Where(i=>i.CanFix).ToArray();var plan=BatchPlan.Create(edits);
        CurrentTextPlan.Validate(plan,Checker.Check([new(0,mixed)]));
        assert(Apply(mixed)==expected && edits.Count(i=>i.Kind==FixKind.ParagraphEndSpace)==1,
            "A trailing gap composes with both colon edges, parentheses, repetition, generic spacing and final punctuation without overlapping writes");
        string whole=mixed.TrimEnd('\r','\a');var style=new Issue(0,whole.Length,whole,whole,"",false,FixKind.DocumentHeadingFormat,CharacterFormat:new(13,false,true));
        var styled=BatchPlan.Create(edits.Append(style));
        assert(styled[0]==style && AtomicTextBatch.ExpectedStory(styled,mixed,i=>mixed[..i.Start])==expected,
            "Character formatting precedes all contained text edits, including the final ASCII-space deletion");
        string current=mixed;int writes=0,saves=0,restores=0;
        string Read(Issue i)=>current.Substring(i.Start,i.Length);
        string Write(Issue i){writes++;current=current.Remove(i.Start,i.Length).Insert(i.Start,i.Replacement);return i.Replacement;}
        AtomicTextBatch.Apply(plan,Read,Write,()=>{saves++;if(current!=expected)throw new Exception("story");},()=>{restores++;current=mixed;});
        assert(current==expected && writes==plan.Count && saves==1 && restores==0,"The full selected group commits once after exact whole-story verification");
        current=mixed;writes=saves=restores=0;bool failed=false;
        try{AtomicTextBatch.Apply(plan,Read,Write,()=>throw new IOException("synthetic save failure"),()=>{restores++;current=mixed;});}
        catch(IOException){failed=true;}
        assert(failed && writes==plan.Count && restores==1 && current==mixed,"Failure at the final save restores the whole selected group and all paragraph markers once");
        foreach(var forged in new[]{trailing with {Original="\r",Length=1},trailing with {Original="\t",Length=1},
            trailing with {Replacement="x"},trailing with {Format=new(13)},trailing with {CharacterFormat=new(13,false)}})
        {
            bool blocked=false;try{BatchPlan.Create([forged]);}catch(InvalidOperationException){blocked=true;}
            assert(blocked,"A malformed trailing-space payload cannot delete controls, write words or disguise a formatting operation");
        }
    }
}
