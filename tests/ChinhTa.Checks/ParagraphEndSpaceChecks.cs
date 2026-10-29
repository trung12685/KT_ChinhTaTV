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
        foreach(string text in new[]{"Đã hoàn thành.","Đã hoàn thành   ","    ","Nội dung:   ",
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
        DelimitedEndingChecks(assert);
    }
    private static void DelimitedEndingChecks(Action<bool,string> assert)
    {
        Issue[] Find(string text,int cp=0,DocumentProfile profile=DocumentProfile.State)=>Checker.Check([new(cp,text)],profile)
            .Where(i=>i.Kind==FixKind.ParagraphEndSpace).ToArray();
        string Apply(string text,IEnumerable<Issue> selected)=>AtomicTextBatch.ExpectedStory(BatchPlan.Create(selected),text,i=>text[..i.Start]);
        foreach(var sample in new (string Source,string Expected)[]
        {
            ("Đã duyệt “kế hoạch”.   \r\a","Đã duyệt “kế hoạch”.\r\a"),
            ("Đã duyệt (kế hoạch)?  \n","Đã duyệt (kế hoạch)?\n"),
            ("Theo mẫu \"nội dung\"! ","Theo mẫu \"nội dung\"!"),
            ("😀 Đã duyệt “ke\u0302\u0301 hoa\u0323ch”;  \r","😀 Đã duyệt “ke\u0302\u0301 hoa\u0323ch”;\r"),
            ("Đã duyệt “ kế hoạch ”  .   \r","Đã duyệt “ kế hoạch ”  .\r"),
            ("Đã duyệt ( kế hoạch )  ;   \r","Đã duyệt ( kế hoạch )  ;\r")
        })
        {
            string whole=sample.Source.TrimEnd('\r','\n','\a');
            var direct=ParagraphEndSpaceRules.Check(new(83,sample.Source));
            assert(direct is [{CanFix:true,BatchSafe:false,Kind:FixKind.ParagraphEndSpace,Replacement:""}] &&
                direct[0].Original.All(c=>c==' ') && sample.Source.Substring(direct[0].Start-83,direct[0].Length)==direct[0].Original,
                "One balanced final prose delimiter grants only the exact trailing ASCII gap after Unicode and before paragraph/cell markers");
            foreach(var profile in Enum.GetValues<DocumentProfile>())
            {
                var found=Find(sample.Source,profile:profile);
                CurrentTextPlan.Validate(found,Checker.Check([new(0,sample.Source)],profile));
                assert(found.Length==1 && Apply(sample.Source,found)==sample.Expected,
                    "Every profile removes the trailing gap after a proven final quote/parenthesis while retaining all unselected inner and pre-mark spaces");
                var locked=Checker.Check([new(83,sample.Source,TextEditable:false)],profile);
                assert(locked.Count==1 && !locked[0].CanFix && locked[0].Start==83 && locked[0].Original==whole && locked[0].Replacement==whole,
                    "The extended delimited-ending scope remains a whole-paragraph manual warning when current XML edit permission is absent");
            }
            assert(ParagraphEndSpaceRules.Check(new(83,sample.Source,TextEditable:false)) is [{CanFix:false,Start:83}],
                "The direct delimited-ending rule enforces text permission before proposing a deletion");
        }
        foreach(string source in new[]
        {
            "Đã duyệt “kế hoạch.   ","Đã duyệt kế hoạch”.   ","Đã duyệt (kế hoạch.   ","Đã duyệt kế hoạch).   ",
            "Đã duyệt ((kế hoạch)).   ","Đã duyệt “kế (hoạch mới)”.   ","Đã duyệt (kế “hoạch mới”).   ",
            "Đã duyệt \"kế hoạch “nội dung”\".   ","Đã duyệt \"kế hoạch\" và \"nội dung\".   ",
            "Đã duyệt “kế hoạch” và (nội dung).   ","Đã duyệt [kế hoạch].   ","Đã duyệt ‘kế hoạch’.   ",
            "Đã duyệt «kế hoạch».   ","Đã duyệt \"\".   ","Đã duyệt (    ).   ","Đã duyệt “kế hoạch”...   ",
            "Đã duyệt “kế hoạch”?!   ","Đã duyệt “kế hoạch”.\u00a0   ","Đã duyệt “kế hoạch”.\t   ",
            "Đã duyệt “kế hoạch 12”.   ","Đã duyệt (2026).   ","Đã 12 (kế hoạch).   ",
            "Đã duyệt “nội dung ts”.   ","Đã duyệt (nội dung TP).   ","Đã duyệt (nội dung tp).   ",
            "Đã duyệt “nội dung TS.”.   ","Đã duyệt “KẾ HOẠCH”.   ","Đã duyệt (kế HOẠCH).   ",
            "Đã duyệt “Kế hoạch”.   ","Đã duyệt (kế).   ","Duyệt (kế hoạch).   ","“kế hoạch”.   ",
            "Đã dùng fn(kế hoạch).   ","Đã dùng fn (kế hoạch).   ","Đã dùng parse(kế hoạch).   ",
            "print(\"kế hoạch\");   ","Console.WriteLine(\"kế hoạch\");   ","Tệp mau.docx đã duyệt “kế hoạch”.   ",
            "Số: đã duyệt (kế hoạch).   ","Hà Nội, ngày 3 tháng 1 năm 2026 đã duyệt “kế hoạch”.   "
        })
            assert(ParagraphEndSpaceRules.Check(new(0,source)).Count==0 && Find(source).Length==0,
                "Trailing gaps after ambiguous, nested, technical, numeric, abbreviated or uppercase delimiters remain outside the narrow prose extension");

        const string original="Đã duyệt “kế hoạch”.   \r";
        var trailing=Find(original).Single();
        foreach(string fresh in new[]{original.Replace("kế hoạch","KẾ HOẠCH",StringComparison.Ordinal),
            original.Replace('“','[').Replace('”',']'),original.Replace("Đã duyệt","cfg=duệt",StringComparison.Ordinal)})
        {
            bool rejected=false;
            try{CurrentTextPlan.Validate([trailing],Checker.Check([new(0,fresh)]));}catch(InvalidOperationException){rejected=true;}
            assert(rejected && fresh.Substring(trailing.Start,trailing.Length)==trailing.Original,
                "Identical trailing bytes and CPs cannot replay a stored choice after delimiter/case/technical context loses its proof");
        }
        bool permissionChanged=false;
        try{CurrentTextPlan.Validate([trailing],Checker.Check([new(0,original,TextEditable:false)]));}catch(InvalidOperationException){permissionChanged=true;}
        assert(permissionChanged,"A stored delimited-ending deletion cannot survive loss of the current XML text permission");

        foreach(var sample in new (string Source,string Expected)[]
        {
            ("Theo mẫu “  nội dung  ”;   \r\a","Theo mẫu “nội dung”;\r\a"),
            ("Theo mẫu (  nội dung  );   \r\a","Theo mẫu (nội dung);\r\a")
        })
        {
            var selected=Checker.Check([new(0,sample.Source)]).Where(i=>i.CanFix).ToArray();
            var gap=selected.Single(i=>i.Kind==FixKind.ParagraphEndSpace);
            assert(selected.Length==3 && Apply(sample.Source,selected)==sample.Expected &&
                !Checker.Check([new(0,sample.Expected)]).Any(i=>i.CanFix),
                "The final trailing-gap deletion composes without overlap with both independently selected inner delimiter-edge repairs");
            string trailingOnly=Apply(sample.Source,[gap]);
            assert(trailingOnly.Contains("  nội dung  ",StringComparison.Ordinal) &&
                Checker.Check([new(0,trailingOnly)]).Count(i=>i.CanFix)==2,
                "Selecting only the trailing gap leaves both unselected inner-edge defects and their repair choices intact");
            string current=sample.Source;int writes=0,restores=0;bool failed=false;
            var plan=BatchPlan.Create(selected);
            try
            {
                AtomicTextBatch.Apply(plan,i=>current.Substring(i.Start,i.Length),i=>
                {writes++;current=current.Remove(i.Start,i.Length).Insert(i.Start,i.Replacement);return i.Replacement;},
                    ()=>throw new IOException("synthetic delimited-ending save failure"),()=>{restores++;current=sample.Source;});
            }
            catch(IOException){failed=true;}
            assert(failed && writes==plan.Count && restores==1 && current==sample.Source,
                "A final-save failure restores the whole delimiter/trailing-space selection and all paragraph markers exactly once");
        }
    }
}
