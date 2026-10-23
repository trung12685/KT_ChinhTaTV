using ChinhTa.Core;

public static class DocumentTypeTextChecks
{
    private static (Paragraph[] P,LayoutParagraph[] L) Good(string title)
    {
        var sample=DocumentHeadingChecks.Sample(title);
        return (sample.P.Select(p=>p with {Font="Times New Roman",Size=p.Text.StartsWith("Độc lập")?14:13,Bold=true,Italic=false,Color=0}).ToArray(),sample.L);
    }
    public static void Run(Action<bool,string> assert)
    {
        string[] names=["BÁO CÁO","THÔNG BÁO","KẾ HOẠCH","TỜ TRÌNH","QUYẾT ĐỊNH","NGHỊ QUYẾT","HƯỚNG DẪN","CHỈ THỊ","CHƯƠNG TRÌNH","PHƯƠNG ÁN","ĐỀ ÁN","QUY CHẾ","QUY ĐỊNH"];
        foreach(string name in names)
        foreach(string source in new[]{name.ToLowerInvariant(),new string(name.Select((c,i)=>i%2==0?char.ToLowerInvariant(c):c).ToArray())})
        {
            var sample=Good(source);var found=DocumentHeadingRules.Check(sample.P,sample.L,DocumentProfile.State);
            var fix=found.Single(i=>i.CanFix);
            assert(found.Count==1 && fix.Kind==FixKind.DocumentTypeText && fix.Original==source && fix.Replacement==name && fix.CharacterFormat is null,
                "An exact supported front-title name gains only its bounded case correction");
            BatchPlan.Create([fix]);
            var changed=sample.P.Select(p=>p.Start==fix.Start?p with {Text=fix.Replacement+"\r"}:p).ToArray();
            var layout=sample.L.Select(p=>p.Start==fix.Start?p with {Text=fix.Replacement+"\r"}:p).ToArray();
            assert(DocumentHeadingRules.Check(changed,layout,DocumentProfile.State).Count==0,
                "Rechecking a capitalized front title clears its issue without changing the subject");
            assert(IssueReviewCatalog.Describe(fix).Section==IssueReviewSection.RequiredFormat &&
                IssueTooltipCatalog.Describe(fix).Basis.Contains("II.5, điểm b") && IssueTooltipCatalog.Describe(fix).Text.Split('\n').Length==3,
                "The title-case repair has a concise required-format label and the actual uppercase provision");
        }
        var padded=Good("  Báo cáo  ");var paddedFix=DocumentHeadingRules.Check(padded.P,padded.L,DocumentProfile.State).Single(i=>i.CanFix);
        assert(paddedFix.Start==padded.P[6].Start+2 && paddedFix.Original=="Báo cáo" && paddedFix.Replacement=="BÁO CÁO",
            "Only title letters/interword separator are replaced; surrounding layout spaces are preserved");
        var plain=Good("Báo cáo");var title=plain.P[6];
        foreach(var unsafeTitle in new[]{title with {TextEditable=false},title with {TextEditable=false,FormatEditable=true},title with {FormatMixed=true},
            title with {BoldMixed=true},title with {FormatObserved=false}})
        {
            var rows=plain.P.Select(p=>p.Start==title.Start?unsafeTitle:p).ToArray();
            assert(!DocumentHeadingRules.Check(rows,plain.L,DocumentProfile.State).Any(i=>i.CanFix && i.Kind==FixKind.DocumentTypeText),
                "Fields, drawing-only format proof, mixed or unobserved title text cannot gain capitalization writes");
        }
        foreach(string other in new[]{"Công văn","V/v kiểm tra","Báo cáo kết quả","Báo\tcáo","\u00a0Báo cáo"})
        {
            var sample=Good(other);
            assert(!DocumentHeadingRules.Check(sample.P,sample.L,DocumentProfile.State).Any(i=>i.CanFix && i.Kind==FixKind.DocumentTypeText),
                "Unknown names, prose, V/v, tabs and non-ASCII leading layout spaces are not rewritten as a known title");
        }
        foreach(var profile in new[]{DocumentProfile.Party,DocumentProfile.Personal})
            assert(DocumentHeadingRules.Check(plain.P,plain.L,profile).Count==0,"The named State title rule does not apply to Party or personal profiles");
        foreach(var bad in new[]{DocumentHeadingChecks.Sample("Báo cáo",body:false),DocumentHeadingChecks.Sample("Báo cáo",subjectCount:0)})
            assert(!DocumentHeadingRules.Check(bad.P,bad.L,DocumentProfile.State).Any(i=>i.CanFix && i.Kind==FixKind.DocumentTypeText),
                "An absent/unbounded subject cannot establish a safe front-title role");
        var renamed=plain.P.Select(p=>p==title?p with {Text="BÁO CÁO\r"}:p).ToArray();
        bool stale=false;var originalFix=DocumentHeadingRules.Check(plain.P,plain.L,DocumentProfile.State).Single();
        try{CurrentTextPlan.Validate([originalFix],DocumentHeadingRules.Check(renamed,plain.L,DocumentProfile.State));}catch(InvalidOperationException){stale=true;}
        assert(stale,"A stale title-case selection is rejected before mutation when the current role/text changes");
        var subject=plain.P[7];var subjectRows=plain.P.Select(p=>p==subject?p with {Text="UNESCO và Công ty Mẫu\r"}:p).ToArray();
        var subjectLayout=plain.L.Select(p=>p.Start==subject.Start?p with {Text="UNESCO và Công ty Mẫu\r"}:p).ToArray();
        assert(DocumentHeadingRules.Check(subjectRows,subjectLayout,DocumentProfile.State).Single().Kind==FixKind.DocumentTypeText,
            "Proper names/acronyms and paragraph breaks in the subject are untouched by title capitalization");
        var wrong=DocumentHeadingChecks.Sample("Báo cáo");var all=DocumentHeadingRules.Check(wrong.P,wrong.L,DocumentProfile.State).Where(i=>i.CanFix).ToArray();
        var plan=BatchPlan.Create(all);string story=string.Concat(wrong.P.Select(p=>p.Text));
        string expected=AtomicTextBatch.ExpectedStory(plan,story,i=>story[..i.Start]);
        assert(plan.Count==4 && plan[0].CharacterFormat is not null && expected==story.Replace("Báo cáo","BÁO CÁO",StringComparison.Ordinal),
            "Case and three title/subject font repairs share one ordered batch and preserve all other text");
        var caseOnly=all.Single(i=>i.Kind==FixKind.DocumentTypeText);
        var afterCase=wrong.P.Select(p=>p.Start==caseOnly.Start?p with {Text="BÁO CÁO\r"}:p).ToArray();
        var afterCaseLayout=wrong.L.Select(p=>p.Start==caseOnly.Start?p with {Text="BÁO CÁO\r"}:p).ToArray();
        var remaining=DocumentHeadingRules.Check(afterCase,afterCaseLayout,DocumentProfile.State);
        assert(remaining.Count==3 && remaining.All(i=>i.CanFix && i.Kind==FixKind.DocumentHeadingFormat),
            "Selecting only capitalization preserves all three unselected font repairs after the next scan");
        var fontOnly=all.Where(i=>i.CharacterFormat is not null).ToArray();
        assert(AtomicTextBatch.ExpectedStory(fontOnly,story,i=>story[..i.Start])==story,
            "Selecting only formatting never capitalizes the unselected title text");
        bool crossRole=false;
        try{BatchPlan.Create([caseOnly,new Issue(caseOnly.Start,caseOnly.Length,caseOnly.Original,caseOnly.Original,"",false,
            FixKind.AgencyFormat,CharacterFormat:new(13,false,true,ComponentRole.Agency))]);}catch(InvalidOperationException){crossRole=true;}
        assert(crossRole,"A title capitalization cannot share an agency-format payload merely because its span fits");
        string current=story;int restores=0;bool failed=false;
        try
        {
            AtomicTextBatch.Apply(plan,i=>current.Substring(i.Start,i.Length),i=>
            {
                if(i.CharacterFormat is null)current=current.Remove(i.Start,i.Length).Insert(i.Start,i.Replacement);
                return i.Replacement;
            },()=>throw new InvalidOperationException("font verification failed"),()=>{current=story;restores++;});
        }
        catch(InvalidOperationException){failed=true;}
        assert(failed && restores==1 && current==story,"Failure after selected capitalization restores the whole combined case/font batch");
        foreach(var forged in new[]{originalFix with {Original="Tên tùy ý",Length=9,Replacement="TÊN TÙY Ý"},originalFix with {Replacement="BÁO CÁO MỚI"},
            originalFix with {CharacterFormat=new(13,false,true)},originalFix with {Replacement=originalFix.Original}})
        {
            bool rejected=false;try{BatchPlan.Create([forged]);}catch(InvalidOperationException){rejected=true;}
            assert(rejected,"Forged title-case payloads cannot rewrite unknown content, change length or smuggle format operations");
        }
        var example=DevelopmentReport.Capabilities.Single(c=>c.Id=="C24").Example;
        var fixture=Good(example.Before);var exampleFix=DocumentHeadingRules.Check(fixture.P,fixture.L,DocumentProfile.State).Single();
        assert(exampleFix.Replacement==example.Outcome,"The published C24 example matches executable title-role rules");
    }
}
