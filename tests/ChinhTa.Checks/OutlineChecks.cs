using System.Xml.Linq;
using ChinhTa.Core;

public static class OutlineChecks
{
    private static Paragraph[] Rows(params string[] texts)
    {
        int start=0;return texts.Select(text=>{var p=new Paragraph(start,text+"\r",Outline:new(OutlineScope.PlainBody));start+=p.Text.Length;return p;}).ToArray();
    }
    private static Issue[] Issues(Paragraph[] rows)=>Checker.Check(rows).Where(i=>i.Kind is FixKind.OutlineOrder or FixKind.OutlineMarker).ToArray();
    private static string Apply(Paragraph[] rows,IEnumerable<Issue>? edits=null)
    {
        string story=string.Concat(rows.Select(p=>p.Text));
        return AtomicTextBatch.ExpectedStory(BatchPlan.Create(edits??Checker.Check(rows).Where(i=>i.CanFix)),story,i=>story[..i.Start]);
    }
    public static void Run(Action<bool,string> assert)
    {
        foreach(var sample in new[]{new[]{"1. Tình hình","2. Kết quả công tác","4. Đề xuất"},new[]{"a) Tình hình","b) Kết quả công tác","d) Đề xuất"},
            new[]{"I. Tình hình","II. Kết quả công tác","IV. Đề xuất"},new[]{"1) Tình hình","2) Kết quả công tác","4) Đề xuất"},new[]{"01. Tình hình","02. Kết quả công tác","04. Đề xuất"}})
        {
            var rows=Rows(sample);var found=Issues(rows);var issue=found.Single();
            assert(issue.CanFix && !issue.BatchSafe && issue.Kind==FixKind.OutlineOrder && issue.Start==rows[2].Start &&
                issue.Original==sample[2].Split(' ')[0].TrimEnd('.',')') && issue.Replacement is "3" or "c" or "III" or "03" &&
                issue.Suggestion.Contains("dẫn chiếu"),"Typed Arabic/letter/Roman/padded labels receive one exact reviewed marker replacement");
            var fixedText=Apply(rows);
            assert(fixedText.EndsWith(issue.Replacement+sample[2][issue.Length..]+"\r",StringComparison.Ordinal),"Renumbering preserves punctuation, title, content and paragraph terminator");
        }
        var nested=Rows("I. Tình hình","1. Nội dung thứ nhất","a) Nhánh đầu","b) Nhánh hai","d) Nhánh ba","2. Nội dung thứ hai","4. Nội dung thứ ba",
            "II. Kết quả","1. Công việc đầu","2. Công việc sau","4. Kiến nghị");
        assert(Issues(nested).Count(i=>i.CanFix)==3 && Issues(nested).All(i=>i.Original is "4" or "d"),"Nested Roman/number/letter sequences are checked separately in each parent");
        var siblings=Rows("1. Tình hình","Nội dung nằm giữa hai mục.","2. Kết quả","Nội dung tiếp theo.","4. Đề xuất");
        assert(Issues(siblings).Count(i=>i.CanFix)==1,"Ordinary intervening body paragraphs do not split sibling headings");
        var doubleGap=Rows("1. Nội dung","2. Nội dung","4. Nội dung","5. Nội dung");
        assert(Issues(doubleGap).Where(i=>i.CanFix).Select(i=>i.Replacement).SequenceEqual(new[]{"3","4"}),"All wrong subsequent labels have final-position proposals ready for one batch");
        var selected=Issues(doubleGap).Where(i=>i.CanFix).Last();
        assert(Apply(doubleGap,[selected]).Contains("4. Nội dung\r4. Nội dung",StringComparison.Ordinal),"Selecting one label changes only that marker; unselected corrections remain for a fresh scan");
        var single=Rows("1. Tình hình","- Nội dung riêng","2. Kết quả");
        var bullet=Issues(single).Single();
        assert(bullet.CanFix && bullet.Kind==FixKind.OutlineMarker && bullet.Original=="- " && bullet.Replacement=="" &&
            Apply(single)=="1. Tình hình\rNội dung riêng\r2. Kết quả\r","A singleton dash inside a complete numeric parent removes only its marker and separator");
        foreach(string mark in new[]{"-","–","—","+"})
        {
            var rows=Rows("a) Nội dung chính","  "+mark+"   Nội dung riêng","b) Nội dung khác");
            var found=Issues(rows).Single(i=>i.CanFix);
            assert(found.Start==rows[1].Start+2 && found.Original==mark+"   " && Apply(rows)=="a) Nội dung chính\r  Nội dung riêng\rb) Nội dung khác\r",
                "Singleton marker removal preserves preceding indentation and does not overlap old separator-space proposals");
        }
        var hierarchy=Rows("1. Nội dung chính","- Nhánh đầu","+ Nội dung con","- Nhánh hai","+ Nội dung khác","2. Mục khác");
        assert(Issues(hierarchy).Count(i=>i.CanFix)==2 && Issues(hierarchy).All(i=>i.Original=="+ "),"Plus counts belong to each dash parent rather than the entire numeric section");
        var keep=Rows("1. Nội dung chính","- Nhánh đầu","+ Nội dung con","+ Nội dung tiếp","- Nhánh hai","2. Mục khác");
        assert(Issues(keep).Length==0,"Two dash siblings and two plus siblings are retained");
        var both=Rows("1. Nội dung chính","- Nhánh đơn","+ Nội dung đơn","2. Mục khác");
        assert(Issues(both).Count(i=>i.CanFix)==2 && Apply(both)=="1. Nội dung chính\rNhánh đơn\rNội dung đơn\r2. Mục khác\r",
            "Singleton dash and its singleton plus can be removed together without touching the texts");
        var separate=Rows("1. Mục đầu","- Nội dung riêng","2. Mục sau","- Nội dung khác");
        assert(Issues(separate).Count(i=>i.CanFix)==2,"Dashes in different numeric parents are not counted together");
        var root=Rows("- Nội dung đơn");
        assert(Issues(root).Length==1 && !Issues(root)[0].CanFix,"A root singleton without a proven explicit parent stays manual");
        var uncertain=Rows("1. Nội dung chính","- Nội dung riêng","Đoạn chưa xác minh");uncertain[2]=uncertain[2] with {Outline=null};
        assert(Issues(uncertain).Any(i=>i.Kind==FixKind.OutlineMarker && !i.CanFix) && !Issues(uncertain).Any(i=>i.CanFix),"An unknown boundary prevents a false singleton conclusion");
        var auto=Rows("1. Nội dung","2. Nội dung","4. Nội dung");auto[1]=auto[1] with {Outline=new(OutlineScope.AutomaticList)};
        assert(!Issues(auto).Any(i=>i.CanFix) && Issues(auto).Any(i=>!i.CanFix),"Automatic Word lists break typed sibling context and produce manual coverage notices");
        var structured=Rows("1. Nội dung","2. Nội dung","4. Nội dung");structured[2]=structured[2] with {TextEditable=false};
        assert(!Issues(structured).Any(i=>i.CanFix) && Issues(structured).Any(i=>!i.CanFix),"The new rules cannot bypass a manual XML text gate");
        foreach(var sample in new[]{new[]{"2. Nội dung","4. Nội dung"},new[]{"1. Nội dung","2) Nội dung","4. Nội dung"},
            new[]{"1. Nội dung","2. Nội dung","1. Nội dung"},new[]{"I. Nội dung","IIV. Nội dung"},new[]{"1. Nội dung","02. Nội dung"}})
            assert(!Issues(Rows(sample)).Any(i=>i.CanFix),"Missing initial anchor, mixed style, restarts, invalid Roman or inconsistent padding remains manual");
        var reference=Rows("1. Nội dung","2. Nội dung","4. Nội dung","Xem mục 4 để đối chiếu.");
        assert(!Issues(reference).Any(i=>i.CanFix) && Issues(reference).Any(i=>!i.CanFix),"Literal item references prevent partial unreviewed renumbering of their sibling group");
        var alphabet=Rows("a) Nội dung","b) Nội dung","c) Nội dung","d) Nội dung","g) Nội dung");
        assert(!Issues(alphabet).Any(i=>i.CanFix) && Issues(alphabet).Any(i=>!i.CanFix),"Unresolved Latin/Vietnamese alphabet transitions stay manual");
        var latin=Rows("a) Nội dung","b) Nội dung","c) Nội dung","d) Nội dung","f) Nội dung");
        assert(Issues(latin).Single(i=>i.CanFix).Replacement=="e","A distinctive Latin letter supplies enough evidence for its alphabet");
        var vietnamese=Rows("a) Nội dung","b) Nội dung","c) Nội dung","đ) Nội dung");
        assert(Issues(vietnamese).Single(i=>i.CanFix).Replacement=="d","An explicit đ marker supplies Vietnamese alphabet evidence");
        var heading=Rows("1. Nội dung","2. Nội dung","TIÊU ĐỀ KHÁC","4. Nội dung","5. Nội dung");
        assert(!Issues(heading).Any(i=>i.CanFix),"An unnumbered uppercase heading stops an old sequence from spilling into another section");
        var indent=Rows("1. Nội dung","1. Nhánh con","2. Nhánh con","2. Nội dung","4. Nội dung");
        indent[1]=indent[1] with {Outline=new(OutlineScope.PlainBody,720)};indent[2]=indent[2] with {Outline=new(OutlineScope.PlainBody,720)};
        assert(Issues(indent).Single(i=>i.CanFix).Start==indent[4].Start,"Same-family children at a deeper observed indent do not corrupt outer numbering");
        var original=Rows("1. Nội dung","- Nội dung  ( kế hoạch kế hoạch ) đã duyệt;hoàn thành  .Tiếp tục báo cáo","2. Kết quả","4. Đề xuất");
        var all=Checker.Check(original).Where(i=>i.CanFix).ToArray();string story=string.Concat(original.Select(p=>p.Text));string expected=Apply(original);
        assert(expected=="1. Nội dung\rNội dung (kế hoạch) đã duyệt; hoàn thành. Tiếp tục báo cáo\r2. Kết quả\r3. Đề xuất\r",
            "Order, singleton removal and old text/parenthesis/repetition/punctuation repairs share one nonoverlapping mixed batch");
        var style=new Issue(original[1].Start,original[1].Text.Length-1,original[1].Text.TrimEnd('\r'),original[1].Text.TrimEnd('\r'),"",false,FixKind.DocumentHeadingFormat,CharacterFormat:new(13,false,true));
        var styled=BatchPlan.Create(all.Append(style));assert(styled[0]==style && AtomicTextBatch.ExpectedStory(styled,story,i=>story[..i.Start])==expected,"Full-row formatting can precede contained outline marker removal");
        string current=story;int writes=0,restores=0,saves=0;var plan=BatchPlan.Create(all);
        string Read(Issue i)=>current.Substring(i.Start,i.Length);
        string Write(Issue i){writes++;current=current.Remove(i.Start,i.Length).Insert(i.Start,i.Replacement);return i.Replacement;}
        AtomicTextBatch.Apply(plan,Read,Write,()=>{saves++;if(current!=expected)throw new Exception("story");},()=>{restores++;current=story;});
        assert(writes==plan.Count && saves==1 && restores==0,"The outline/text group commits once after complete story verification");
        current=story;writes=restores=saves=0;bool failed=false;
        try{AtomicTextBatch.Apply(plan,Read,i=>{var value=Write(i);if(writes==2)throw new IOException("injected");return value;},()=>saves++,()=>{restores++;current=story;});}catch(IOException){failed=true;}
        assert(failed && current==story && restores==1 && saves==0,"A failure after renumbering rolls back the entire selected group");
        var stale=Issues(single).Where(i=>i.CanFix).ToArray();var changed=Rows("1. Tình hình","- Nội dung riêng","- Nội dung thứ hai","2. Kết quả");bool rejected=false;
        try{CurrentTextPlan.Validate(stale,Checker.Check(changed));}catch(InvalidOperationException){rejected=true;}
        assert(rejected,"A newly added sibling invalidates the old singleton proposal before writing");
        foreach(var profile in Enum.GetValues<DocumentProfile>())assert(Checker.Check(Rows("1. Nội dung","2. Nội dung","4. Đề xuất"),profile).Count(i=>i.CanFix && i.Kind==FixKind.OutlineOrder)==1,"Typed structural context applies in all document profiles");
        assert(!Checker.Check([new(0,"1. Nội dung"),new(30,"3. Nội dung")]).Any(i=>i.Kind==FixKind.OutlineOrder),"Legacy callers without structural evidence do not silently grant outline editability");
        XmlChecks(assert);
    }
    private static void XmlChecks(Action<bool,string> assert)
    {
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XElement P(string label,XElement? pr=null)=>new(w+"p",pr,new XElement(w+"r",new XElement(w+"t",label)));
        var paragraphs=new[]{P("1. Nội dung"),P("2. Nội dung"),P("4. Nội dung")};
        string Xml(IEnumerable<XElement> ps,XElement? styles=null)=>new XDocument(new XElement(w+"root",new XElement(w+"document",new XElement(w+"body",ps)),styles)).ToString();
        WordParagraphSnapshot[] Live()=>Rows("1. Nội dung","2. Nội dung","4. Nội dung").Select(p=>new WordParagraphSnapshot(p.Start,p.Start+p.Text.Length,p.Text,1,false,false)).ToArray();
        var evidence=new Dictionary<int,OutlineParagraphInfo>();var live=Live();
        var editable=WordTextProof.Read(Xml(paragraphs),live,onOutline:(start,info)=>evidence[start]=info);
        assert(evidence.Count==3 && evidence.Values.All(i=>i.Scope==OutlineScope.PlainBody) && editable.Count==3,"Plain body outline evidence is attached only to exact matched live Word ranges");
        var applied=WordTextProof.Apply(Rows("1. Nội dung","2. Nội dung","4. Nội dung"),editable,evidence).ToArray();
        assert(Issues(applied).Single(i=>i.CanFix).Replacement=="3","The production XML-to-paragraph path enables a verified typed sequence");
        foreach(var construct in new[]{"num","style-num","table","bookmark","cycle","missing-style","bad-indent","char-indent","field"})
        {
            var ps=paragraphs.Select(p=>new XElement(p)).ToArray();XElement? styles=null;var pr=new XElement(w+"pPr");ps[1].AddFirst(pr);
            switch(construct)
            {
                case "num":pr.Add(new XElement(w+"numPr",new XElement(w+"numId",new XAttribute(w+"val",1))));break;
                case "style-num":pr.Add(new XElement(w+"pStyle",new XAttribute(w+"val","List")));styles=new(w+"styles",new XElement(w+"style",new XAttribute(w+"type","paragraph"),new XAttribute(w+"styleId","List"),new XElement(w+"pPr",new XElement(w+"numPr",new XElement(w+"numId",new XAttribute(w+"val",7))))));break;
                case "table":ps[1]=new(w+"tbl",new XElement(w+"tr",new XElement(w+"tc",ps[1])));break;
                case "bookmark":ps[1].Add(new XElement(w+"bookmarkStart",new XAttribute(w+"id",1)));break;
                case "cycle":case "missing-style":pr.Add(new XElement(w+"pStyle",new XAttribute(w+"val","A")));if(construct=="cycle")styles=new(w+"styles",new XElement(w+"style",new XAttribute(w+"type","paragraph"),new XAttribute(w+"styleId","A"),new XElement(w+"basedOn",new XAttribute(w+"val","A"))));break;
                case "bad-indent":pr.Add(new XElement(w+"ind",new XAttribute(w+"left","bad")));break;
                case "char-indent":pr.Add(new XElement(w+"ind",new XAttribute(w+"leftChars",100)));break;
                case "field":var run=ps[1].Element(w+"r")!;run.Remove();ps[1].Add(new XElement(w+"fldSimple",new XAttribute(w+"instr","DATE"),run));break;
            }
            evidence.Clear();var current=WordTextProof.Read(Xml(ps,styles),live,onOutline:(start,info)=>evidence[start]=info);
            assert(evidence.TryGetValue(live[1].Start,out var middle) && middle.Scope!=OutlineScope.PlainBody && !Issues(WordTextProof.Apply(Rows("1. Nội dung","2. Nội dung","4. Nội dung"),current,evidence).ToArray()).Any(i=>i.CanFix),"Unsupported numbering, style, table, field or indentation cannot join a typed sequence: "+construct);
            if(construct is "num" or "style-num" or "table" or "bookmark")assert(current.Count==3,"Outline exclusions do not revoke old plain-text eligibility for unrelated character repairs: "+construct);
        }
        var stylePr=new XElement(w+"pPr",new XElement(w+"numPr",new XElement(w+"numId",new XAttribute(w+"val",9))),new XElement(w+"ind",new XAttribute(w+"left",720)));
        var inherited=new XElement(w+"styles",new XElement(w+"style",new XAttribute(w+"type","paragraph"),new XAttribute(w+"styleId","List"),stylePr));
        var cleared=paragraphs.Select(p=>new XElement(p)).ToArray();foreach(var p in cleared)p.AddFirst(new XElement(w+"pPr",new XElement(w+"pStyle",new XAttribute(w+"val","List")),new XElement(w+"numPr",new XElement(w+"numId",new XAttribute(w+"val",0)))));
        evidence.Clear();WordTextProof.Read(Xml(cleared,inherited),live,onOutline:(start,info)=>evidence[start]=info);
        assert(evidence.Values.All(i=>i.Scope==OutlineScope.PlainBody && i.Indent==720),"Explicit numId zero overrides inherited numbering while observed indentation remains available");
        var refPs=paragraphs.Select(p=>new XElement(p)).ToArray();refPs[2].Add(new XElement(w+"fldSimple",new XAttribute(w+"instr","REF Mark")));
        evidence.Clear();WordTextProof.Read(Xml(refPs),live,onOutline:(start,info)=>evidence[start]=info);
        assert(evidence[live[0].Start].HasReferences,"Cross-reference field evidence protects earlier outline labels too");
        XElement TabP(string text)
        {
            var run=new XElement(w+"r");var parts=text.Split('\t');
            for(int i=0;i<parts.Length;i++){if(i>0)run.Add(new XElement(w+"tab"));run.Add(new XElement(w+"t",parts[i]));}
            return new(w+"p",run);
        }
        foreach(bool before in new[]{true,false})
        {
            string[] labels=before?new[]{"\t1. Nội dung","\t2. Nội dung","\t4. Nội dung"}:new[]{"1.\tNội dung","2.\tNội dung","4.\tNội dung"};
            var tabRows=Rows(labels);var tabLive=tabRows.Select(p=>new WordParagraphSnapshot(p.Start,p.Start+p.Text.Length,p.Text,1,false,false)).ToArray();
            evidence.Clear();var tabEditable=WordTextProof.Read(Xml(labels.Select(TabP)),tabLive,onOutline:(start,info)=>evidence[start]=info);
            var checkedRows=WordTextProof.Apply(tabRows,tabEditable,evidence).ToArray();
            var fix=Issues(checkedRows).Single(i=>i.CanFix);
            assert(tabEditable.Count==0 && evidence.Values.All(i=>i.LabelThroughTabs) && checkedRows.All(p=>!p.TextEditable) &&
                fix.Start==checkedRows[2].Start+(before?1:0) && fix.Original=="4" && fix.Replacement=="3",
                "Matched pure layout tabs give narrow label-only evidence without opening generic text editing");
            assert(Apply(checkedRows,[fix]).Count(c=>c=='\t')==3,"Label-only renumbering preserves every layout tab");
        }
        string[] tabBullet={"\t1. Nội dung","\t- Nội dung riêng","\t2. Nội dung"};
        var bulletRows=Rows(tabBullet);var bulletLive=bulletRows.Select(p=>new WordParagraphSnapshot(p.Start,p.Start+p.Text.Length,p.Text,1,false,false)).ToArray();
        evidence.Clear();var bulletEditable=WordTextProof.Read(Xml(tabBullet.Select(TabP)),bulletLive,onOutline:(start,info)=>evidence[start]=info);
        var tabBulletRows=WordTextProof.Apply(bulletRows,bulletEditable,evidence).ToArray();
        assert(Issues(tabBulletRows).Single(i=>i.CanFix).Original=="- " && Apply(tabBulletRows).Contains("\tNội dung riêng",StringComparison.Ordinal),
            "Singleton removal through verified tab evidence preserves the leading tab and only removes ASCII marker/separator");
        var tabGap=Rows("1. Nội dung","-\tNội dung riêng","2. Nội dung");
        assert(!Issues(tabGap).Any(i=>i.CanFix),"A tab used as the bullet separator stays manual rather than being deleted as an ASCII separator");
        var linked=TabP("\t1. Nội dung");var linkedRun=linked.Element(w+"r")!;linkedRun.Remove();linked.Add(new XElement(w+"hyperlink",linkedRun));
        var linkLive=new[]{new WordParagraphSnapshot(0,13,"\t1. Nội dung\r",1,false,false)};evidence.Clear();
        WordTextProof.Read(Xml([linked]),linkLive,onOutline:(start,info)=>evidence[start]=info);
        assert(evidence.Values.Single().Scope==OutlineScope.Complex && !evidence.Values.Single().LabelThroughTabs,"A hyperlink plus tabs cannot masquerade as the narrow tab-only label case");
        var mismatch=Live();mismatch[1]=mismatch[1] with {Text="Khác\r"};evidence.Clear();WordTextProof.Read(Xml(paragraphs),mismatch,onOutline:(start,info)=>evidence[start]=info);
        assert(evidence.Count==1,"An XML mismatch cannot produce outline evidence beyond the verified prefix");
    }
}
