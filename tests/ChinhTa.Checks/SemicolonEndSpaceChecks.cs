using ChinhTa.Core;

public static class SemicolonEndSpaceChecks
{
    public static void Run(Action<bool,string> assert)
    {
        Issue[] Find(string text)=>Checker.Check([new(0,text)]).Where(i=>i.Kind==FixKind.ParagraphEndSpace).ToArray();
        string Apply(string text,IEnumerable<Issue> chosen)=>AtomicTextBatch.ExpectedStory(BatchPlan.Create(chosen),text,i=>text[..i.Start]);
        foreach(var sample in new[]{("Nội dung đã duyệt;   \r","Nội dung đã duyệt;\r"),
            ("- Như trên;  \r\a","- Như trên;\r\a"),("Nội dung ;   ","Nội dung ;"),
            ("😀 Nội dung đã duyệt;  \n","😀 Nội dung đã duyệt;\n"),("Bản ke\u0302\u0301 hoa\u0323ch;   ","Bản ke\u0302\u0301 hoa\u0323ch;")})
        {
            var fix=Find(sample.Item1).Single();
            assert(fix.CanFix && fix.Original.All(c=>c==' ') && fix.Replacement=="" && Apply(sample.Item1,[fix])==sample.Item2,
                "A final semicolon gap removes only ASCII spaces, retaining the mark, Unicode, unselected pre-gap and paragraph/cell ends");
            assert(Find(sample.Item2).Length==0,"Fresh scanning clears the repaired final-semicolon gap");
        }
        foreach(string text in new[]{"Nội dung;","Nội dung;;  ","Nội dung;!  ","Nội dung:  ","duyệt;  ","Nội dung A;  ",
            "Nội dung TS;  ","Nội dung 12;  ","NỘI DUNG;  ","Nội dung\tđã duyệt;  ","Nội dung;\u00a0  ",
            "Nội dung;  \t","Số:         /BC-ABC;   ","Hà Nội, ngày       tháng 10 năm 2026;   ",
            "Gửi name@example.com;   ","Mở /data;  ","alpha = beta;   ","Nội dung [ghi chú];  ","Nội dung chưa kết thúc    "})
            assert(Find(text).Length==0,"Unsupported endings, identifiers, layouts, dates and intentional blanks never become a semicolon trailing edit");
        const string mixed="Nội dung  kế hoạch kế hoạch;   \r";
        var all=Checker.Check([new(0,mixed)]).Where(i=>i.CanFix).ToArray();var trailing=all.Single(i=>i.Kind==FixKind.ParagraphEndSpace);
        assert(Apply(mixed,[trailing])=="Nội dung  kế hoạch kế hoạch;\r" &&
            Apply(mixed,all)=="Nội dung kế hoạch;\r", "Trailing-semicolon repair composes with spaces/repetition and preserves unselected repairs");
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            var current=Checker.Check([new(400,mixed)],profile).Single(i=>i.Kind==FixKind.ParagraphEndSpace);
            assert(current.Start==400+mixed.IndexOf("   ",StringComparison.Ordinal),"Every profile keeps the paragraph's actual Word start for the final gap");
            assert(!Checker.Check([new(400,mixed,TextEditable:false)],profile).Any(i=>i.CanFix),"Lost XML permission never grants a semicolon write");
        }
        bool stale=false;
        try{CurrentTextPlan.Validate([trailing],Checker.Check([new(0,mixed.Replace("hoạch;","HOẠCH;",StringComparison.Ordinal))]));}
        catch(InvalidOperationException){stale=true;}
        assert(stale,"Changed ending context invalidates an old gap edit even when spaces and offsets remain identical");
        string value=mixed;bool restored=false,committed=false;
        try{AtomicTextBatch.Apply(all,i=>value.Substring(i.Start,i.Length),i=>{value=value.Remove(i.Start,i.Length).Insert(i.Start,i.Replacement);return i.Replacement;},
            ()=>throw new IOException("synthetic save"),()=>{value=mixed;restored=true;});committed=true;}catch(IOException){}
        assert(restored && !committed && value==mixed,"A save/verification failure restores the entire semicolon/text selection");
        var tip=IssueTooltipCatalog.Describe(trailing);
        assert(tip.Text.Split('\n').Length==3 && tip.Basis.Contains("chấm phẩy") && !tip.Basis.Contains("Nghị định"),
            "Semicolon spacing remains an application suggestion with a concise three-part tooltip");
    }
}
