using ChinhTa.Core;

public static class PunctuationSpaceChecks
{
    public static void Run(Action<bool,string> assert)
    {
        Issue[] Find(string text)=>Checker.Check([new(0,text)]).Where(i=>i.Kind==FixKind.PunctuationSpace).ToArray();
        string Apply(string text,IEnumerable<Issue>? chosen=null)
        {
            var plan=BatchPlan.Create(chosen??Checker.Check([new(0,text)]).Where(i=>i.CanFix));
            return AtomicTextBatch.ExpectedStory(plan,text,i=>text[..i.Start]);
        }
        foreach(var sample in new[]{("Nội dung ,báo cáo.","Nội dung, báo cáo."),
            ("Nội dung  ,  báo cáo.","Nội dung, báo cáo."),("Nội dung,báo cáo.","Nội dung, báo cáo."),
            ("Nội dung;  báo cáo.","Nội dung; báo cáo."),("Nội dung  ;báo cáo.","Nội dung; báo cáo."),
            ("Nội dung,báo cáo,công tác;hoàn thành.","Nội dung, báo cáo, công tác; hoàn thành."),
            ("ke\u0302\u0301 hoa\u0323ch ,đa\u0303 duyệt.","ke\u0302\u0301 hoa\u0323ch, đa\u0303 duyệt."),
            ("😀 Nội dung ,báo cáo.","😀 Nội dung, báo cáo.")})
        {
            var found=Find(sample.Item1);
            assert(found.Length>0 && found.All(i=>i.CanFix && !i.BatchSafe && i.Length>0 &&
                i.Original==sample.Item1.Substring(i.Start,i.Length)) && Apply(sample.Item1)==sample.Item2,
                "Comma/semicolon edits use positive exact UTF-16 spans, include adjacent ASCII spaces and require review");
            assert(Find(sample.Item2).Length==0,"One punctuation batch removes its repaired proposals on a fresh scan");
        }
        foreach(string text in new[]{"Nội dung, báo cáo; công tác.","1,25","1 , 25","01;02","10:30","01/02/2026",
            "BC,AB","A,B","ý,ý","a\u0301,b\u0301","Nội dung\t,báo cáo","Nội dung,\tbáo cáo","Nội dung\u00a0,báo cáo",
            "Nội dung,\u00a0báo cáo","Nội dung,,báo cáo","Nội dung,;báo cáo","Nội dung ,\rbáo cáo",
            "Nội dung\r,báo cáo","foo,bar.com","foo.com , bar","name,part@example.com","https://host/a,b",
            "C:\\foo,bar","foo,bar/file","mã_AB,CD","x = alpha,beta","`alpha,beta`","[alpha,beta]",
            "Số: Nội dung,báo cáo","Ninh Bình,ngày 03 tháng 10 năm 2026","Nội dung : báo cáo.","Nội dung.báo cáo"})
            assert(Find(text).Length==0,"Conservative punctuation scope preserves valid text, numeric/code/address/header forms, controls and unsupported punctuation");
        const string mixed="Nội dung  kế hoạch kế hoạch  ,  báo cáo;công tác đã  xong.";
        var edits=Checker.Check([new(0,mixed)]).Where(i=>i.CanFix).ToArray();
        assert(edits.Count(i=>i.Kind==FixKind.PunctuationSpace)==2 && edits.Count(i=>i.Kind==FixKind.RepeatedText)==1 &&
            Apply(mixed)=="Nội dung kế hoạch, báo cáo; công tác đã xong.",
            "Repeated-phrase deletion, independent spaces and punctuation spaces share one nonoverlapping group");
        var punctuation=Find(mixed);
        assert(Apply(mixed,[punctuation[0]])=="Nội dung  kế hoạch kế hoạch, báo cáo;công tác đã  xong.",
            "A single punctuation selection preserves every unselected repetition and spacing error");
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            var found=Checker.Check([new(90,"Nội dung ,báo cáo.")],profile).Where(i=>i.Kind==FixKind.PunctuationSpace).ToArray();
            assert(found.Length==1 && found[0].Start==98 && found[0].CanFix,"All profiles retain the actual paragraph offset for punctuation spans");
            var guarded=Checker.Check([new(90,"Nội dung ,báo cáo.",TextEditable:false)],profile);
            assert(guarded.Count==1 && !guarded[0].CanFix && guarded[0].Start==90 && guarded[0].Original=="Nội dung ,báo cáo.",
                "An unverified structured row exposes one whole-paragraph manual notice instead of guessed editable CPs");
        }
        assert(PunctuationSpaceRules.Check(new(90,"Nội dung ,báo cáo.",TextEditable:false)).All(i=>!i.CanFix && i.Start==90),
            "The direct punctuation rule also respects the structured-text gate");
        bool stale=false;
        try{CurrentTextPlan.Validate(Find("Nội dung,báo cáo."),Checker.Check([new(0,"Nội dung,báo cáo.",TextEditable:false)]));}
        catch(InvalidOperationException){stale=true;}
        assert(stale,"A stale punctuation choice becomes invalid before writing when the row becomes structured");
        const string styled="Nội dung ,báo cáo.";
        var style=new Issue(0,styled.Length,styled,styled,"",false,FixKind.DocumentHeadingFormat,CharacterFormat:new(13,false,true));
        var styledPlan=BatchPlan.Create(Find(styled).Append(style));
        assert(styledPlan[0]==style && AtomicTextBatch.ExpectedStory(styledPlan,styled,i=>styled[..i.Start])=="Nội dung, báo cáo.",
            "A full-row character style and contained punctuation edit apply formatting first without overlap rejection");
        string current=mixed;int writes=0,restores=0,saves=0;
        var plan=BatchPlan.Create(edits);
        string Read(Issue i)=>current.Substring(i.Start,i.Length);
        string Write(Issue i){writes++;current=current.Remove(i.Start,i.Length).Insert(i.Start,i.Replacement);return i.Replacement;}
        AtomicTextBatch.Apply(plan,Read,Write,()=>{saves++;if(current!="Nội dung kế hoạch, báo cáo; công tác đã xong.")throw new Exception("story");},()=>{restores++;current=mixed;});
        assert(writes==edits.Length && saves==1 && restores==0,"Mixed text edits commit once after the exact whole-story result");
        current=mixed;writes=restores=saves=0;bool failed=false;
        try{AtomicTextBatch.Apply(plan,Read,i=>{string value=Write(i);if(writes==1)throw new IOException("injected");return value;},()=>saves++,()=>{restores++;current=mixed;});}
        catch(IOException){failed=true;}
        assert(failed && current==mixed && writes==1 && restores==1 && saves==0,"Failure after the first mixed edit restores the entire original group once without a save");
        var catalog=DevelopmentReport.Capabilities.Single(c=>c.Id=="C14");
        assert(catalog.Mode==CapabilityMode.Reviewable && catalog.Detail.Contains("Chưa mở rộng sang mọi dấu câu") &&
            DevelopmentReport.Milestones.Any(m=>m.Version=="0.1.42"),"The in-app report records punctuation scope, limits and the new development milestone");
    }
}
