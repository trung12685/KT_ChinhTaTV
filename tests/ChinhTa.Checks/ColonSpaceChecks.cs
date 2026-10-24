using ChinhTa.Core;

public static class ColonSpaceChecks
{
    public static void Run(Action<bool,string> assert)
    {
        Issue[] Find(string text)=>Checker.Check([new(0,text)]).Where(i=>i.Kind==FixKind.ColonSpace).ToArray();
        string Apply(string text,IEnumerable<Issue>? chosen=null)
        {
            var plan=BatchPlan.Create(chosen??Checker.Check([new(0,text)]).Where(i=>i.CanFix));
            return AtomicTextBatch.ExpectedStory(plan,text,i=>text[..i.Start]);
        }
        foreach(var sample in new[]{("Ghi chú : nội dung","Ghi chú: nội dung"),("Kính gửi  : cơ quan.","Kính gửi: cơ quan."),
            ("Về việc   : Nội dung.","Về việc: Nội dung."),("Ghi chú : nội dung; ví dụ : văn bản.","Ghi chú: nội dung; ví dụ: văn bản."),
            ("Ghi chú  :  nội dung.","Ghi chú: nội dung."),("😀 Ghi chú : nội dung.\r\a","😀 Ghi chú: nội dung.\r\a"),
            ("Ghi chu\u0301 : no\u0323\u0302i dung.","Ghi chu\u0301: no\u0323\u0302i dung.")})
        {
            var found=Find(sample.Item1);
            assert(found.Length>0 && found.All(i=>i.CanFix && !i.BatchSafe && i.Length>0 && i.Replacement=="" && i.Original.All(c=>c==' ') &&
                i.Original==sample.Item1.Substring(i.Start,i.Length) && i.Suggestion.Contains("giữ chữ")) && Apply(sample.Item1)==sample.Item2,
                "Colon proposals delete exact positive ASCII space spans before the colon without rewriting the punctuation or Unicode words");
            assert(!Checker.Check([new(0,sample.Item2)]).Any(i=>i.CanFix),"A combined repair leaves no stale fixed colon or spacing proposals");
        }
        foreach(string text in new[]{"Ghi chú: nội dung","Ghi chú :","Ghi chú : 12","Ghi chú : NỘI DUNG",
            "Nội dung 12 : 30","Tỉ lệ 1 : 2","Tỉ lệ iv : vi","Ghi chú : x","Ghi x : nội dung","Ghi chú :: nội dung","Ghi chú : : nội dung",
            "Ghi chú\t: nội dung","Ghi chú :\tnội dung","Ghi chú\u00a0: nội dung","Ghi chú :\u00a0nội dung","Ghi chú\r: nội dung",
            "Ghi chú\vnội dung : văn bản","Ghi chú\nnội dung : văn bản","Ghi chú : host.com","Ghi chú : name@example.com",
            "Ghi chú : C:\\data","Ghi chú : /data","Ghi chú : host_name","Ghi chú : value=abc","Ghi chú : [value]",
            "Truy cập http : host","Truy cập https : host","Truy cập ftp : host","Chọn file : name","Dữ liệu data : value",
            "Ghi CHÚ : nội dung","Ghi Chú : nội dung","key : value","abc.def ghi : nội dung","fn(ghi chú : nội dung)",
            "Số : nội dung","Ninh Bình ( bản nháp ), ngày 03 tháng 10 năm 2026 ghi chú : nội dung"})
            assert(Find(text).Length==0,"The narrow colon rule skips unsupported numeric/time/ratio/code/header/technical/control or ambiguous forms: "+text);
        const string after="Ghi chú  :  nội dung.";
        assert(Apply(after,Find(after))=="Ghi chú:  nội dung." && Checker.Check([new(0,"Ghi chú:  nội dung.")]).Count(i=>i.CanFix && i.Kind==FixKind.Text)==1,
            "Selecting only the colon proposal preserves independent after-colon spaces and their still-actionable generic spacing proposal");
        const string mixed="Ghi chú  :  nội dung  ( kế hoạch kế hoạch ) đã duyệt;hoàn thành  .";
        const string expected="Ghi chú: nội dung (kế hoạch) đã duyệt; hoàn thành.";
        var edits=Checker.Check([new(0,mixed)]).Where(i=>i.CanFix).ToArray();
        assert(edits.Length==8 && edits.Count(i=>i.Kind==FixKind.ColonSpace)==1 && Apply(mixed)==expected,
            "Colon edge deletion merges overlapping generic spacing and shares one batch with parentheses, repetition, semicolon and sentence end");
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            var found=Checker.Check([new(90,"Ghi chú : nội dung")],profile).Where(i=>i.Kind==FixKind.ColonSpace).ToArray();
            assert(found.Length==1 && found[0].Start==97,"All profiles anchor the colon-space span at the actual paragraph offset");
            var guarded=Checker.Check([new(90,"Ghi chú : nội dung",TextEditable:false)],profile);
            assert(guarded.Count==1 && !guarded[0].CanFix && guarded[0].Start==90 && guarded[0].Original=="Ghi chú : nội dung",
                "An unverified/structured row remains one whole-paragraph manual notice instead of a guessed writable CP span");
        }
        var direct=ColonSpaceRules.Check(new(90,"Ghi chú : nội dung",TextEditable:false));
        assert(direct.Count==1 && !direct[0].CanFix && direct[0].Start==90,"The direct colon rule respects the existing structured-text gate");
        bool stale=false;
        try{CurrentTextPlan.Validate(Find("Ghi chú : nội dung"),Checker.Check([new(0,"Ghi chú : nội dung",TextEditable:false)]));}
        catch(InvalidOperationException){stale=true;}
        assert(stale,"A stale colon-space choice fails current-plan preflight when the row becomes structured");
        var style=new Issue(0,mixed.Length,mixed,mixed,"",false,FixKind.DocumentHeadingFormat,CharacterFormat:new(13,false,true));
        var styled=BatchPlan.Create(edits.Append(style));
        assert(styled[0]==style && AtomicTextBatch.ExpectedStory(styled,mixed,i=>mixed[..i.Start])==expected,
            "Full-row character formatting precedes colon edge deletion and the other contained text edits");
        string current=mixed;int writes=0,restores=0,saves=0;
        var plan=BatchPlan.Create(edits);
        string Read(Issue i)=>current.Substring(i.Start,i.Length);
        string Write(Issue i){writes++;current=current.Remove(i.Start,i.Length).Insert(i.Start,i.Replacement);return i.Replacement;}
        AtomicTextBatch.Apply(plan,Read,Write,()=>{saves++;if(current!=expected)throw new Exception("story");},()=>{restores++;current=mixed;});
        assert(writes==8 && saves==1 && restores==0,"The mixed colon group commits once after exact whole-story verification");
        current=mixed;writes=restores=saves=0;bool failed=false;
        try{AtomicTextBatch.Apply(plan,Read,i=>{var value=Write(i);if(writes==8)throw new IOException("injected");return value;},()=>saves++,()=>{restores++;current=mixed;});}
        catch(IOException){failed=true;}
        assert(failed && current==mixed && writes==8 && restores==1 && saves==0,"Failure immediately after deleting the colon gap rolls back all preceding selected edits once without partial commit");
        assert(DevelopmentReport.Capabilities.Single(c=>c.Id=="C17").Example.Before=="Ghi chú :nội dung" &&
            DevelopmentReport.Milestones.Any(m=>m.Version=="0.1.46"),"The report includes the new capability's concrete before/after example and milestone");
    }
}
