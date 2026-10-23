using ChinhTa.Core;

public static class RepeatedTextChecks
{
    public static void Run(Action<bool,string> assert)
    {
        Issue[] Repeats(string text)=>Checker.Check([new(0,text)]).Where(i=>i.Kind==FixKind.RepeatedText).ToArray();
        string Apply(string text,IEnumerable<Issue>? chosen=null)
        {
            var plan=BatchPlan.Create(chosen??Checker.Check([new(0,text)]).Where(i=>i.CanFix));
            return AtomicTextBatch.ExpectedStory(plan,text,i=>text[..i.Start]);
        }
        foreach(var sample in new[]{("Nội dung kế hoạch kế hoạch đã duyệt.","Nội dung kế hoạch đã duyệt."),
            ("công tác công tác công tác.","công tác."),("kết quả  kết quả", "kết quả"),
            ("kết  quả kết quả","kết quả"),("Kế hoạch Kế hoạch", "Kế hoạch"),
            ("Cụm một hai ba bốn năm sáu bảy tám một hai ba bốn năm sáu bảy tám.","Cụm một hai ba bốn năm sáu bảy tám.")})
        {
            var found=Repeats(sample.Item1);
            assert(found.Length==1 && found[0].CanFix && found[0].Replacement=="" && !found[0].BatchSafe &&
                found[0].Start>0 && sample.Item1[found[0].Start]==' ' && Apply(sample.Item1)==sample.Item2,
                "Repeated phrases of two to eight tokens delete only duplicate suffixes, retaining the first copy and surrounding punctuation");
            assert(!Repeats(sample.Item2).Any(i=>i.CanFix),"A fresh check removes the repaired repeated-phrase proposal in one action");
        }
        const string disjoint="kế hoạch kế hoạch; công tác công tác.";
        var two=Repeats(disjoint);
        assert(two.Length==2 && Apply(disjoint)=="kế hoạch; công tác.","Two independent repetitions form one nonoverlapping text batch");
        foreach(string text in new[]{"đều đều","nhà nhà","học học","AB CD AB CD","nhà nhà nhà nhà","Số: kế hoạch kế hoạch","Ninh Bình Ninh Bình, ngày 03 tháng 10 năm 2026"})
        {
            var found=Repeats(text);
            assert(found.Length>0 && found.All(i=>!i.CanFix && i.Original==i.Replacement),
                "Single-token repetitions and all-uppercase sequences remain manual without a semantic dictionary");
        }
        foreach(string text in new[]{"kế hoạch, kế hoạch","kế hoạch. kế hoạch","kế hoạch; kế hoạch","kế hoạch\tkế hoạch",
            "kế hoạch\rkế hoạch","kế hoạch\nkế hoạch","kế hoạch\vkế hoạch","kế hoạch\fkế hoạch",
            "kế hoạch\u00a0kế hoạch","Cơ quan cơ quan","12 12","AB1 AB1","mã_AB mã_AB","báo-cáo báo-cáo",
            "thông tin và thông tin","một hai ba bốn năm sáu bảy tám chín một hai ba bốn năm sáu bảy tám chín"})
            assert(Repeats(text).Length==0,"Repetition matching does not cross punctuation, controls, tabs or NBSP, fold case, match numbers/codes or infer long phrases");
        const string decomposed="ke\u0302\u0301 hoa\u0323ch kế hoạch";
        var canonical=Repeats(decomposed).Single();
        assert(canonical.CanFix && decomposed[..canonical.Start]=="ke\u0302\u0301 hoa\u0323ch" && Apply(decomposed)=="ke\u0302\u0301 hoa\u0323ch",
            "Canonical Unicode comparison preserves the exact decomposed first copy and original UTF-16 offsets");
        const string spaced="Nội dung  kế  hoạch kế  hoạch  kế hoạch giữ nguyên.";
        var all=Checker.Check([new(0,spaced)]).Where(i=>i.CanFix).ToArray();
        var repeat=all.Single(i=>i.Kind==FixKind.RepeatedText);
        assert(all.Length==3 && all.Where(i=>i.Kind==FixKind.Text).All(i=>(long)i.Start+i.Length<=repeat.Start) &&
            Apply(spaced)=="Nội dung kế hoạch giữ nguyên.",
            "Spacing choices inside deleted copies are suppressed while two independent retained-copy spacing choices remain repairable");
        assert(Apply(spaced,[repeat])=="Nội dung  kế  hoạch giữ nguyên.","Selecting repetition alone preserves unselected spacing in the retained text");
        const string demo="con đo con đo";
        var demoFound=Checker.Check([new(0,demo)]).Where(i=>i.CanFix).ToArray();
        assert(demoFound.Count(i=>i.Kind==FixKind.Text)==1 && demoFound.Count(i=>i.Kind==FixKind.RepeatedText)==1 && Apply(demo)=="con đò",
            "An old demo spelling proposal in the deleted copy cannot overlap a repetition deletion; the retained-copy choice remains separate");
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            var found=Checker.Check([new(100,disjoint)],profile).Where(i=>i.Kind==FixKind.RepeatedText).ToArray();
            assert(found.Length==2 && found.All(i=>i.CanFix && i.Start>=100),"All profiles expose mechanical repetitions at the actual paragraph start offset");
            var guarded=Checker.Check([new(100,disjoint,TextEditable:false)],profile);
            assert(guarded.All(i=>!i.CanFix) && guarded.Any(i=>i.Start==100 && i.Original==disjoint),
                "A structured or unverified row cannot bypass the existing manual text gate with a repetition proposal");
        }
        var direct=RepeatedTextRules.Check(new(100,disjoint,TextEditable:false));
        assert(direct.Count==1 && !direct[0].CanFix && direct[0].Start==100 && direct[0].Original==disjoint,
            "The repetition rule itself anchors an unsafe row as one whole-paragraph manual notice");
        var header=new[]{new Paragraph(0,Checker.Heading,Page:1,InTable:true),new Paragraph(100,HeadingRules.Motto,Page:1,InTable:true),
            new Paragraph(150,"Số:\t kế hoạch kế hoạch",Page:1,InTable:true)};
        assert(!Checker.Check(header).Any(i=>i.CanFix && i.Start>=150),"Administrative layout tabs keep the entire row manual, including repetitions after the tab");
        var old=Repeats(disjoint);
        bool stale=false;
        try{CurrentTextPlan.Validate(old,Checker.Check([new(0,disjoint,TextEditable:false)]));}catch(InvalidOperationException){stale=true;}
        assert(stale,"A previously selectable repetition is invalidated before writing when the same visible text becomes structured");
        const string styledText="kế hoạch kế hoạch";
        var deletion=Repeats(styledText).Single();
        var style=new Issue(0,styledText.Length,styledText,styledText,"",false,FixKind.DocumentHeadingFormat,CharacterFormat:new(13,false,true));
        var mixed=BatchPlan.Create([style,deletion]);
        assert(mixed[0].CharacterFormat is not null && AtomicTextBatch.ExpectedStory(mixed,styledText,i=>styledText[..i.Start])=="kế hoạch",
            "A full-row character style and contained repetition deletion share a validated batch with formatting first");
        string current=disjoint;int writes=0,restores=0,saves=0;
        string Read(Issue i)=>current.Substring(i.Start,i.Length);
        string Write(Issue i){writes++;current=current.Remove(i.Start,i.Length).Insert(i.Start,i.Replacement);return i.Replacement;}
        AtomicTextBatch.Apply(old,Read,Write,()=>{saves++;if(current!="kế hoạch; công tác.")throw new Exception("story");},()=>{restores++;current=disjoint;});
        assert(writes==2 && saves==1 && restores==0 && current=="kế hoạch; công tác.","Two empty replacements commit only after exact whole-story verification");
        current=disjoint;writes=restores=saves=0;bool failed=false;
        try{AtomicTextBatch.Apply(old,Read,i=>{var value=Write(i);if(writes==1)throw new IOException("injected");return value;},()=>saves++,()=>{restores++;current=disjoint;});}
        catch(IOException){failed=true;}
        assert(failed && current==disjoint && writes==1 && restores==1 && saves==0,"A failure after deleting one repeated phrase restores the entire original group once");
    }
}
