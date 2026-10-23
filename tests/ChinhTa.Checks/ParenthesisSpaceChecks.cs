using ChinhTa.Core;

public static class ParenthesisSpaceChecks
{
    public static void Run(Action<bool,string> assert)
    {
        Issue[] Find(string text)=>Checker.Check([new(0,text)]).Where(i=>i.Kind==FixKind.ParenthesisSpace).ToArray();
        string Apply(string text,IEnumerable<Issue>? chosen=null)
        {
            var plan=BatchPlan.Create(chosen??Checker.Check([new(0,text)]).Where(i=>i.CanFix));
            return AtomicTextBatch.ExpectedStory(plan,text,i=>text[..i.Start]);
        }
        foreach(var sample in new[]{("Theo mẫu ( nội dung ) đã duyệt.","Theo mẫu (nội dung) đã duyệt."),
            ("(  báo cáo)","(báo cáo)"),("(báo cáo  )","(báo cáo)"),
            ("( nội dung ); ( báo cáo ).","(nội dung); (báo cáo)."),
            ("😀 ( ke\u0302\u0301 hoa\u0323ch ) đã duyệt.","😀 (ke\u0302\u0301 hoa\u0323ch) đã duyệt."),
            ("Theo mẫu (  nội dung ,báo cáo  ) đã  duyệt.","Theo mẫu (nội dung, báo cáo) đã duyệt."),
            ("( công tác công tác ) đã xong.","(công tác) đã xong."),
            ("( Công tác – báo cáo )", "(Công tác – báo cáo)")})
        {
            var found=Find(sample.Item1);
            assert(found.Length>0 && found.All(i=>i.CanFix && !i.BatchSafe && i.Length>0 && i.Replacement=="" &&
                i.Original.All(c=>c==' ') && i.Original==sample.Item1.Substring(i.Start,i.Length) && i.Suggestion.Contains("giữ chữ")) &&
                Apply(sample.Item1)==sample.Item2,"Parenthesis repairs delete only positive edge-space spans and preserve delimiters, exact Unicode and outside spacing");
            assert(!Checker.Check([new(0,sample.Item2)]).Any(i=>i.CanFix),"One combined repair leaves no stale parenthesis, punctuation, repetition or spacing proposals");
        }
        foreach(string text in new[]{"(nội dung)","()","( )","(    )","( 12 )","( 1,25 )","( ABC )","( HÀ NỘI )",
            "( a )","( a\u0301 )","( x + y )","( alpha = beta )","( tên_tệp )","( host/name )","( host\\name )",
            "( name@example.com )","( nội\tdung )","(\tnội dung )","( nội dung\u00a0 )","(\u00a0nội dung )",
            "( nội\rdung )","( nội\ndung )","( nội dung ) ( phụ ( thêm ) )","( nội dung ) ( còn thiếu",
            "( nội dung ) )","fn( nội dung )","( nội dung )tiếp","( nội dung. )","( - nội dung )","( nội dung - )",
            "[ nội dung ]","“ nội dung ”","Số: ( nội dung )","Ninh Bình ( bản nháp ), ngày 03 tháng 10 năm 2026"})
            assert(Find(text).Length==0,"The new rule leaves blank/numeric/technical/all-uppercase/short/nested/unbalanced/unsupported boundary forms outside its repair scope: "+text);
        foreach(string text in new[]{"(    )","Theo mẫu (      ) giữ trống.","(    ) và (    )"})
            assert(!Checker.Check([new(0,text)]).Any(i=>i.CanFix),"The existing generic spacing path also preserves empty parenthesis form fields during Fix All");
        const string blankMixed="Ô trống (    ), nội dung  đã duyệt.";
        assert(Apply(blankMixed)=="Ô trống (    ), nội dung đã duyệt.","A blank form field remains intact while independent old spacing is repaired");
        const string punctuation="( công,tác )";
        var parens=Find(punctuation);
        string edgesOnly=Apply(punctuation,parens);
        assert(edgesOnly=="(công,tác)" && Checker.Check([new(0,edgesOnly)]).Count(i=>i.CanFix && i.Kind==FixKind.PunctuationSpace)==1 &&
            Apply(punctuation)=="(công, tác)","Unselected comma spacing remains discoverable after edge spaces are deleted, and both kinds can commit together");
        assert(Checker.Check([new(0,"(công;tác).")]).Count(i=>i.Kind==FixKind.PunctuationSpace && i.CanFix)==1 &&
            !Checker.Check([new(0,"fn(công,tác)")]).Any(i=>i.Kind==FixKind.PunctuationSpace),
            "Comma/semicolon boundary support covers prose parentheses without treating a directly attached function call as prose");
        const string mixed="Nội dung  (  kế hoạch kế hoạch  ) đã duyệt;hoàn thành.";
        const string expected="Nội dung (kế hoạch) đã duyệt; hoàn thành.";
        var edits=Checker.Check([new(0,mixed)]).Where(i=>i.CanFix).ToArray();
        assert(edits.Length==5 && edits.Count(i=>i.Kind==FixKind.ParenthesisSpace)==2 && Apply(mixed)==expected,
            "Two edge deletions share one nonoverlapping batch with generic spaces, repetition and comma/semicolon rules");
        var first=Find(mixed).OrderBy(i=>i.Start).First();
        assert(Apply(mixed,[first])=="Nội dung  (kế hoạch kế hoạch  ) đã duyệt;hoàn thành.","A single boundary choice preserves every unselected issue");
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            var found=Checker.Check([new(90,"( nội dung )")],profile).Where(i=>i.Kind==FixKind.ParenthesisSpace).ToArray();
            assert(found.Length==2 && found[0].Start==91 && found[1].Start==100,"All profiles anchor edge spaces at actual paragraph offsets");
            var guarded=Checker.Check([new(90,"( nội dung )",TextEditable:false)],profile);
            assert(guarded.Count==1 && !guarded[0].CanFix && guarded[0].Start==90 && guarded[0].Original=="( nội dung )",
                "Unverified/structured text stays one manual whole-paragraph notice rather than selectable guessed offsets");
        }
        var direct=ParenthesisSpaceRules.Check(new(90,"( nội dung )",TextEditable:false));
        assert(direct.Count==1 && !direct[0].CanFix && direct[0].Start==90,"The direct rule enforces the structured-text gate too");
        bool stale=false;
        try{CurrentTextPlan.Validate(Find("( nội dung )"),Checker.Check([new(0,"( nội dung )",TextEditable:false)]));}
        catch(InvalidOperationException){stale=true;}
        assert(stale,"A stale edge-space choice cannot bypass a newly structured row before writing");
        var style=new Issue(0,mixed.Length,mixed,mixed,"",false,FixKind.DocumentHeadingFormat,CharacterFormat:new(13,false,true));
        var styled=BatchPlan.Create(edits.Append(style));
        assert(styled[0]==style && AtomicTextBatch.ExpectedStory(styled,mixed,i=>mixed[..i.Start])==expected,
            "Full-row formatting precedes contained edge-space deletions and other text changes");
        string current=mixed;int writes=0,restores=0,saves=0;
        var plan=BatchPlan.Create(edits);
        string Read(Issue i)=>current.Substring(i.Start,i.Length);
        string Write(Issue i){writes++;current=current.Remove(i.Start,i.Length).Insert(i.Start,i.Replacement);return i.Replacement;}
        AtomicTextBatch.Apply(plan,Read,Write,()=>{saves++;if(current!=expected)throw new Exception("story");},()=>{restores++;current=mixed;});
        assert(writes==5 && saves==1 && restores==0,"The mixed parenthesis group commits only after whole-story verification");
        current=mixed;writes=restores=saves=0;bool failed=false;
        try{AtomicTextBatch.Apply(plan,Read,i=>{var value=Write(i);if(writes==2)throw new IOException("injected");return value;},()=>saves++,()=>{restores++;current=mixed;});}
        catch(IOException){failed=true;}
        assert(failed && current==mixed && writes==2 && restores==1 && saves==0,"Failure after an edge deletion restores the entire mixed group once without committing a partial repair");
        assert(DevelopmentReport.Capabilities.Single(c=>c.Id=="C15").Mode==CapabilityMode.Reviewable &&
            DevelopmentReport.Milestones.Any(m=>m.Version=="0.1.43"),"The report records the new capability and milestone");
    }
}
