using ChinhTa.Core;

public static class SentenceEndSpaceChecks
{
    public static void Run(Action<bool,string> assert)
    {
        Issue[] Find(string text)=>Checker.Check([new(0,text)]).Where(i=>i.Kind==FixKind.SentenceEndSpace).ToArray();
        string Apply(string text,IEnumerable<Issue>? chosen=null)
        {
            var plan=BatchPlan.Create(chosen??Checker.Check([new(0,text)]).Where(i=>i.CanFix));
            return AtomicTextBatch.ExpectedStory(plan,text,i=>text[..i.Start]);
        }
        foreach(var sample in new[]{("Đã hoàn thành .","Đã hoàn thành."),("Đã hoàn thành  ?","Đã hoàn thành?"),
            ("Xin cảm ơn   !  ","Xin cảm ơn!  "),("Nội dung đã duyệt .\r\a","Nội dung đã duyệt.\r\a"),
            ("😀 Nội dung đã duyệt .","😀 Nội dung đã duyệt."),
            ("Bản ke\u0302\u0301 hoa\u0323ch .","Bản ke\u0302\u0301 hoa\u0323ch."),
            ("Nội dung  ( kế hoạch kế hoạch ) đã duyệt;hoàn thành  .","Nội dung (kế hoạch) đã duyệt; hoàn thành.")})
        {
            var found=Find(sample.Item1);
            assert(found.Length==1 && found[0].CanFix && !found[0].BatchSafe && found[0].Replacement=="" &&
                found[0].Original.All(c=>c==' ') && found[0].Original==sample.Item1.Substring(found[0].Start,found[0].Length) &&
                found[0].Suggestion.Contains("giữ chữ") && Apply(sample.Item1)==sample.Item2,
                "Final punctuation repair deletes only the exact positive ASCII space span, preserving punctuation, Unicode, trailing spaces and paragraph/cell terminators");
            assert(!Checker.Check([new(0,sample.Item2)]).Any(i=>i.CanFix),"One combined repair leaves no stale repaired text/spacing proposals");
        }
        foreach(string text in new[]{"Nội dung đã duyệt.","Nội dung đã duyệt?","Nội dung đã duyệt!","Nội dung . Tiếp theo.",
            "Nội dung . tiếp theo", "Nội dung ...","Nội dung . . .","Nội dung ? !","Nội dung !?","Nội dung …",
            "Giá trị 1 .","Giá trị 1,25 .","Dòng số ii .","Ô để trống ....","Nội dung TS .","Nội dung ThS .","Nội dung Tp .",
            "Nội dung v.v .","Nội dung v.v. .","www . com .","host.com .","report . docx .","fn(nội dung) .",
            "https://host/nội dung .","name@example.com .","C:\\nội dung .","mã_abc nội dung .","alpha = beta .",
            "Nội dung\tđã duyệt .","Nội dung đã duyệt\t.","Nội dung đã duyệt\u00a0.","Nội dung đã duyệt .\u00a0",
            "Nội dung\vđã duyệt .","Nội dung\fđã duyệt .","Nội dung\nđã duyệt .","Số: đã duyệt .",
            "Ninh Bình ( bản nháp ), ngày 03 tháng 10 năm 2026 đã duyệt .","Nội dung A .","nội dung a\u0301 .",
            "duyệt .","Trước đó. duyệt .","Nội dung đã duyệt :","Nội dung đã duyệt ;"})
            assert(Find(text).Length==0,"Final-space scope skips unsupported, numeric, abbreviation, technical, repeated punctuation and ambiguous boundaries: "+text);
        const string mixed="Nội dung  ( kế hoạch kế hoạch ) đã duyệt;hoàn thành  .";
        const string expected="Nội dung (kế hoạch) đã duyệt; hoàn thành.";
        var edits=Checker.Check([new(0,mixed)]).Where(i=>i.CanFix).ToArray();
        assert(edits.Length==6 && edits.Count(i=>i.Kind==FixKind.SentenceEndSpace)==1 && Apply(mixed)==expected,
            "Final edge deletion merges overlapping generic spacing and shares one batch with parentheses, repetition and semicolon spacing");
        var final=Find(mixed).Single();
        assert(Apply(mixed,[final])=="Nội dung  ( kế hoạch kế hoạch ) đã duyệt;hoàn thành.",
            "Selecting only final punctuation preserves all unselected word, parenthesis and spacing choices");
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            var found=Checker.Check([new(90,"Nội dung .")],profile).Where(i=>i.Kind==FixKind.SentenceEndSpace).ToArray();
            assert(found.Length==1 && found[0].Start==98,"All profiles use the actual paragraph offset for the final space span");
            var guarded=Checker.Check([new(90,"Nội dung .",TextEditable:false)],profile);
            assert(guarded.Count==1 && !guarded[0].CanFix && guarded[0].Start==90 && guarded[0].Original=="Nội dung .",
                "Structured or unverified rows stay manual and anchored to the whole paragraph");
        }
        var direct=SentenceEndSpaceRules.Check(new(90,"Nội dung .",TextEditable:false));
        assert(direct.Count==1 && !direct[0].CanFix && direct[0].Start==90,"The direct final-space rule also respects TextEditable");
        bool stale=false;
        try{CurrentTextPlan.Validate(Find("Nội dung ."),Checker.Check([new(0,"Nội dung .",TextEditable:false)]));}
        catch(InvalidOperationException){stale=true;}
        assert(stale,"A stale final-space choice cannot write after the row becomes structured");
        var style=new Issue(0,mixed.Length,mixed,mixed,"",false,FixKind.DocumentHeadingFormat,CharacterFormat:new(13,false,true));
        var styled=BatchPlan.Create(edits.Append(style));
        assert(styled[0]==style && AtomicTextBatch.ExpectedStory(styled,mixed,i=>mixed[..i.Start])==expected,
            "Full-row character formatting precedes final-space deletion and the other contained text edits");
        string current=mixed;int writes=0,restores=0,saves=0;
        var plan=BatchPlan.Create(edits);
        string Read(Issue i)=>current.Substring(i.Start,i.Length);
        string Write(Issue i){writes++;current=current.Remove(i.Start,i.Length).Insert(i.Start,i.Replacement);return i.Replacement;}
        AtomicTextBatch.Apply(plan,Read,Write,()=>{saves++;if(current!=expected)throw new Exception("story");},()=>{restores++;current=mixed;});
        assert(writes==6 && saves==1 && restores==0,"The final-space mixed group commits once after exact whole-story verification");
        current=mixed;writes=restores=saves=0;bool failed=false;
        try{AtomicTextBatch.Apply(plan,Read,i=>{var value=Write(i);if(writes==1)throw new IOException("injected");return value;},()=>saves++,()=>{restores++;current=mixed;});}
        catch(IOException){failed=true;}
        assert(failed && current==mixed && writes==1 && restores==1 && saves==0,"Failure after the final-space deletion rolls back the whole selected group once without partial save");
        assert(DevelopmentReport.Capabilities.Single(c=>c.Id=="C16").Mode==CapabilityMode.Reviewable &&
            DevelopmentReport.Milestones.Any(m=>m.Version=="0.1.44"),"The in-app report records final-space scope and the new milestone");
    }
}
