using System.Xml.Linq;
using ChinhTa.Core;

public static class HierarchicalOutlineChecks
{
    private static Paragraph[] Rows(params string[] texts)
    {
        int cp=0;return texts.Select(text=>{var p=new Paragraph(cp,text+"\r",Outline:new(OutlineScope.PlainBody));cp+=p.Text.Length;return p;}).ToArray();
    }
    private static Issue[] Issues(Paragraph[] rows)=>Checker.Check(rows).Where(i=>i.Kind is FixKind.OutlineOrder or FixKind.OutlineMarker).ToArray();
    private static string Apply(Paragraph[] rows,IEnumerable<Issue> chosen)
    {
        string text=string.Concat(rows.Select(p=>p.Text));return AtomicTextBatch.ExpectedStory(BatchPlan.Create(chosen),text,i=>text[..i.Start]);
    }
    public static void Run(Action<bool,string> assert)
    {
        foreach(string delimiter in new[]{".",")"})
        foreach(int depth in new[]{2,3,4})
        foreach(string lead in new[]{"","  ","\t"})
        {
            string parent=string.Join('.',Enumerable.Repeat("1",depth-1)),prefix=parent+".";
            var texts=Enumerable.Range(1,depth-1).Select(d=>lead+string.Join('.',Enumerable.Repeat("1",d))+delimiter+" Mục cha")
                .Concat(new[]{lead+prefix+"1"+delimiter+" Nội dung đầu",lead+prefix+"2"+delimiter+" Nội dung hai",lead+prefix+"4"+delimiter+" Đề xuất"}).ToArray();
            var rows=Rows(texts);var found=Issues(rows);var fix=found.Single(i=>i.CanFix);
            assert(found.Length==1 && fix.Start==rows[^1].Start+lead.Length+prefix.Length && fix.Original=="4" && fix.Replacement=="3" &&
                fix.Title!.Contains(prefix+"4 → "+prefix+"3"),"Two-to-four-level typed decimal headings replace only the last component under their exact proven parent");
            string expected=string.Concat(texts[..^1].Select(t=>t+"\r"))+lead+prefix+"3"+delimiter+" Đề xuất\r";
            assert(Apply(rows,[fix])==expected,"Hierarchical correction preserves all prefix digits, delimiters, indentation/tabs, content and paragraph endings");
            var repaired=rows.Select(p=>p==rows[^1]?p with {Text=lead+prefix+"3"+delimiter+" Đề xuất\r"}:p).ToArray();
            assert(!Issues(repaired).Any(),"Fresh structural scan removes a repaired decimal heading instead of retaining a disabled issue");
        }
        var ambiguous=Rows("1. Mục ngoài","1. Mục trong","1.1. Nhánh đầu","1.4. Nhánh sau");
        ambiguous[1]=ambiguous[1] with {Outline=new(OutlineScope.PlainBody,720)};
        assert(!Issues(ambiguous).Any(i=>i.CanFix && i.Kind==FixKind.OutlineOrder),
            "Duplicate open numeric parents cannot authorize a guessed compound-label scope");
        var ten=Rows(["1. Mục cha",..Enumerable.Range(1,9).Select(n=>$"1.{n}. Nhánh đúng"),"1.12. Nhánh cuối"]);
        var tenth=Issues(ten).Single(i=>i.CanFix);
        assert(tenth.Original=="12" && tenth.Replacement=="10" && Apply(ten,[tenth]).EndsWith("1.10. Nhánh cuối\r",StringComparison.Ordinal),
            "A multi-digit final component is corrected as one bounded digit span without rewriting its prefix");
        var multiple=Rows("1. Mục cha","1.1. Nhánh đầu","1.4. Nhánh hai","1.5. Nhánh ba","2. Mục cha sau","2.1. Nhánh đầu","2.4. Nhánh hai");
        var edits=Issues(multiple).Where(i=>i.CanFix).ToArray();
        assert(edits.Length==3 && edits.Select(i=>i.Replacement).SequenceEqual(new[]{"2","3","2"}),
            "Separate parents keep separate child counters and all proposed leaf labels can be applied in one batch");
        assert(Apply(multiple,edits).Contains("1.2. Nhánh hai\r1.3. Nhánh ba",StringComparison.Ordinal) &&
            Apply(multiple,[edits[^1]]).Contains("1.4. Nhánh hai\r1.5. Nhánh ba",StringComparison.Ordinal),
            "Selecting one parent group leaves every unselected parent group's original labels intact");
        assert(!Issues(Rows("1.5 triệu đồng.")).Any() &&
            !Issues(Rows("1. Mục cha","1.1. Nhánh đầu","1.5 triệu đồng.","1.2. Nhánh sau")).Any(),
            "An ordinary decimal value does not itself create an outline error or a false orphan warning in an already correct sequence");
        assert(!Issues(Rows("1. Mục cha","1.1. Nhánh đầu","1.2 Nhãn chưa rõ","1.4. Nhánh sau")).Any(i=>i.CanFix),
            "An unsupported decimal-like row freezes uncertain numbering instead of creating a duplicate label");
        var marker=Rows("1. Mục cha","1.1. Nhánh đầu","- Nội dung riêng","1.2. Nhánh sau","- Nội dung khác");
        assert(Issues(marker).Count(i=>i.CanFix && i.Kind==FixKind.OutlineMarker)==2,
            "Singleton dash counts use each explicit decimal parent rather than combining bullets across sibling subitems");
        foreach(var texts in new[]{new[]{"1.1. Nhánh đầu","1.4. Nhánh sau"},new[]{"1. Mục cha","1.1.1. Nhánh đầu","1.1.4. Nhánh sau"},
            new[]{"1. Mục cha","1.01. Nhánh đầu","1.04. Nhánh sau"},new[]{"01. Mục cha","01.1. Nhánh đầu","01.4. Nhánh sau"},
            new[]{"0. Mục cha","0.1. Nhánh đầu","0.4. Nhánh sau"},new[]{"1. Mục cha","1.2. Nhánh đầu","1.4. Nhánh sau"},
            new[]{"1. Mục cha","1.1. Nhánh đầu","1.1. Nhánh lặp","1.4. Nhánh sau"},
            new[]{"1. Mục cha","1.1. Nhánh đầu","1.4) Nhánh sau"},
            new[]{"1. Mục cha","1.1. Nhánh đầu","1.1.1.1.1. Nội dung","1.4. Nhánh sau"}})
            assert(!Issues(Rows(texts)).Any(i=>i.CanFix && i.Kind==FixKind.OutlineOrder),
                "Orphan/skipped parents, padded or zero components, missing starts, restarts, mixed delimiters and unsupported depths stay manual");
        foreach(string reference in new[]{"Xem mục 1.4.","Xem mục 1.2.","Xem mục 1.4.1.","Xem mục 1.2.9.8.7.",
            "Xem mục 1.1, 1.4.","Xem mục 1.8, 1.4.","Xem mục 1.8 và 1.4.","Xem mục 1.8 đến 1.4.",
            "Xem mục 1.8–1.4.","Xem mục 1.8 - 1.4."})
            assert(!Issues(Rows("1. Mục cha","1.1. Nhánh đầu","1.4. Nhánh hai",reference)).Any(i=>i.CanFix),
                "Old, proposed, descendant and range/list/conjunction references block partial decimal renumbering");
        assert(Issues(Rows("1. Mục cha","1.1. Nhánh đầu","1.4. Nhánh hai","Xem mục 1 và mục 2.4.")).Single(i=>i.CanFix).Replacement=="2",
            "A reference to an unchanged ancestor or an unrelated numeric prefix does not block independent leaf numbering");
        foreach(var texts in new[]{new[]{"1. Mục đầu","3. Mục sai","3.1. Nhánh đầu","5. Mục sau"},
            new[]{"1. Mục đầu","3. Mục sai","3.1.Nhánh chưa rõ","5. Mục sau"},
            new[]{"1. Mục cha","1.1. Nhánh đầu","1.4. Nhánh sai","1.4.1. Nội dung con","1.5. Nhánh sau"}})
        {
            var found=Issues(Rows(texts));
            assert(!found.Any(i=>i.CanFix && i.Kind==FixKind.OutlineOrder) && found.Any(i=>i.Title=="Đổi mục cha cần kiểm tra mục con"),
                "A group needing dependent numeric-prefix changes remains manual as a whole, including malformed child hints");
        }
        assert(!Issues(Rows("1. Mục đầu","2. Mục sau","2.1. Nhánh đầu","2.2. Nhánh sau")).Any(),
            "Correct hierarchical labels never create a dependency warning merely because they have child headings");
        assert(!Issues(Rows("1. Mục đầu","2. Nội dung","4. Nội dung","5. Nội dung","Xem mục 3.")).Any(i=>i.CanFix),
            "A reference to a proposed flat label freezes the whole sibling group instead of creating a duplicate behind a manual item");
        var recipients=Rows("1. Mục đầu","3. Mục sau","3.1. Đơn vị nhận");
        recipients[2]=recipients[2] with {Outline=new(OutlineScope.Recipient,RecipientRole:ComponentRole.RecipientItem)};
        assert(Issues(recipients).Single(i=>i.CanFix).Replacement=="2",
            "An explicitly recognized recipient container cannot invent a dependent body prefix");
        var fresh=Rows("1. Mục cha","1.1. Nhánh đầu","1.4. Nhánh hai");var pending=Issues(fresh).Single(i=>i.CanFix);
        bool Stale(Paragraph[] changed){try{CurrentTextPlan.Validate([pending],Checker.Check(changed));return false;}catch(InvalidOperationException){return true;}}
        assert(Stale([..fresh,new(fresh[^1].Start+fresh[^1].Text.Length,"1.4.1. Mục con\r",Outline:new(OutlineScope.PlainBody))]) &&
            Stale([..fresh,new(fresh[^1].Start+fresh[^1].Text.Length,"Xem mục 1.2.\r",Outline:new(OutlineScope.PlainBody))]),
            "Adding a dependent child or a reference invalidates an old leaf edit before any write");
        assert(Stale(fresh.Select(p=>p==fresh[0]?p with {Text="2. Mục cha\r"}:p).ToArray()),
            "Changing the explicit parent also invalidates the previous child proposal");
        var renamed=fresh.Select(p=>p with {Text="2."+p.Text[2..]}).ToArray();
        var newer=Issues(renamed).Single(i=>i.CanFix);
        assert(newer.Start==pending.Start && newer.Original==pending.Original && newer.Replacement==pending.Replacement && Stale(renamed),
            "A changed numeric prefix invalidates a stale proposal even when its last-digit span and replacement are identical");
        bool canonicalized=false;
        try{RepairDependencies.Expand([pending],Checker.Check(renamed));}catch(InvalidOperationException){canonicalized=true;}
        assert(canonicalized,"Selection expansion cannot silently replace an old hierarchical context with a new parent prefix");
        var mix=Rows("1. Mục cha","1.1. Nhánh đầu","1.4. Nội dung  kế hoạch kế hoạch","- Nội dung riêng","2. Mục sau");
        string story=string.Concat(mix.Select(p=>p.Text));var all=Checker.Check(mix).Where(i=>i.CanFix).ToArray();
        string desired=Apply(mix,all);
        assert(desired=="1. Mục cha\r1.1. Nhánh đầu\r1.2. Nội dung kế hoạch\rNội dung riêng\r2. Mục sau\r",
            "Leaf numbering, singleton removal, spaces and repetition compose without overlapping text writes");
        string current=story;bool restored=false,committed=false;
        try{AtomicTextBatch.Apply(all,i=>current.Substring(i.Start,i.Length),i=>{current=current.Remove(i.Start,i.Length).Insert(i.Start,i.Replacement);return i.Replacement;},
            ()=>throw new IOException("synthetic save"),()=>{current=story;restored=true;});committed=true;}catch(IOException){}
        assert(restored && !committed && current==story,"A failure after mixed hierarchical edits rolls back the entire batch");
        foreach(var profile in Enum.GetValues<DocumentProfile>())
            assert(Checker.Check(fresh,profile).Count(i=>i.CanFix && i.Kind==FixKind.OutlineOrder)==1,"Explicit typed decimal context applies in every document profile");
        var large=Rows(Enumerable.Range(1,300).SelectMany(n=>new[]{n+". Mục cha",n+".1. Nhánh đầu",n+".2. Nhánh hai",n+".4. Nhánh ba"}).ToArray());
        var largeIssues=Issues(large);
        assert(large.Length==1200 && largeIssues.Length==300 && largeIssues.All(i=>i.CanFix && i.Replacement=="3"),
            "A 1200-paragraph synthetic hierarchy keeps 300 independent parent scopes without inventing a prefix cascade");
        Xml(assert);
    }
    private static void Xml(Action<bool,string> assert)
    {
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var raw=Rows("1. Mục cha","\t1.1.\tNhánh đầu","\t1.4.\tNhánh sau");
        XElement P(string text)=>new(w+"p",new XElement(w+"r",text.Split('\t').SelectMany((s,i)=>i==0?new[]{new XElement(w+"t",s)}:
            new[]{new XElement(w+"tab"),new XElement(w+"t",s)})));
        foreach(bool automatic in new[]{false,true})
        {
            var ps=raw.Select(p=>P(p.Text.TrimEnd('\r'))).ToArray();
            if(automatic)ps[1].AddFirst(new XElement(w+"pPr",new XElement(w+"numPr",new XElement(w+"numId",new XAttribute(w+"val",1)))));
            string xml=new XDocument(new XElement(w+"document",new XElement(w+"body",ps))).ToString();
            var live=raw.Select(p=>new WordParagraphSnapshot(p.Start,p.Start+p.Text.Length,p.Text,1,false,false)).ToArray();
            var evidence=new Dictionary<int,OutlineParagraphInfo>();var editable=WordTextProof.Read(xml,live,onOutline:(start,info)=>evidence[start]=info);
            var rows=WordTextProof.Apply(raw,editable,evidence).ToArray();var issues=Issues(rows);
            assert(automatic?!issues.Any(i=>i.CanFix):issues.Single(i=>i.CanFix).Start==rows[2].Start+3,
                "Real XML tab evidence enables only the final typed digit; Word automatic numbering remains protected");
            if(!automatic)assert(Apply(rows,issues.Where(i=>i.CanFix))=="1. Mục cha\r\t1.1.\tNhánh đầu\r\t1.2.\tNhánh sau\r",
                "Tab-aware hierarchical repair retains every leading/separator tab and leaves general text permission closed");
        }
    }
}
