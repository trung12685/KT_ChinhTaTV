using System.Xml.Linq;
using ChinhTa.Core;

public static class ComponentChecks
{
    public static (Paragraph[] Rows,LayoutParagraph[] Layout) Sample(bool softBreak=false,int subjects=2,bool recipients=true,bool automatic=false,int padding=0,Action<XDocument>? changeXml=null)
    {
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XElement P(string text)=>new(w+"p",new XElement(w+"r",text.Split('\v').SelectMany((s,i)=>i==0?
            new[]{new XElement(w+"t",s)}:new[]{new XElement(w+"br"),new XElement(w+"t",s)})));
        var receiver=new XElement(w+"tc",P("Nơi nhận:"),P("- Như trên;"),P("- Đơn vị mẫu;"),P("- Lưu: VT, TEST."));
        if(automatic)receiver.Elements(w+"p").Skip(1).First().AddFirst(new XElement(w+"pPr",new XElement(w+"numPr",new XElement(w+"numId",new XAttribute(w+"val",1)))));
        var body=new XElement(w+"body",new XElement(w+"tbl",new XElement(w+"tr",
            new XElement(w+"tc",P("CƠ QUAN CHỦ QUẢN"),P("ĐƠN VỊ BAN HÀNH"),P("Số: 01/BC-TEST")),
            new XElement(w+"tc",P(Checker.Heading),P(HeadingRules.Motto)))),P("BÁO CÁO"));
        for(int i=0;i<subjects;i++)body.Add(P(i==0?softBreak?"Mang lại bình\vyên cho nhân dân":"Tổng hợp kết quả công tác ":"trong giai đoạn thử nghiệm"));
        body.Add(P(""),P("Kính gửi: đơn vị mẫu"),P("Nội dung bình thường."));
        for(int i=0;i<padding;i++)body.Add(P("Đoạn nội dung độc lập thứ "+i+"."));
        if(recipients)body.Add(new XElement(w+"tbl",new XElement(w+"tr",receiver,new XElement(w+"tc",P("TRƯỞNG ĐƠN VỊ")))));
        var xml=new XDocument(new XElement(w+"document",body));changeXml?.Invoke(xml);var ps=body.Descendants(w+"p").ToArray();int cp=0;
        var live=new List<WordParagraphSnapshot>();var rows=new List<Paragraph>();
        foreach(var p in ps)
        {
            string text=string.Concat(p.Descendants().Where(e=>e.Parent?.Name==w+"r").Select(e=>e.Name==w+"t"?e.Value:e.Name==w+"br"?"\v":""));
            bool cell=p.Ancestors(w+"tc").Any(),last=cell && !p.ElementsAfterSelf(w+"p").Any();
            string full=text+"\r"+(last?"\a":"");bool centered=p.Parent==body && text!="Nội dung bình thường." || cell && !p.Ancestors(w+"tc").Contains(receiver);
            bool bold=cp>0 && !text.StartsWith("Số:") && text!="Nội dung bình thường.";
            float size=text=="CƠ QUAN CHỦ QUẢN" || text=="ĐƠN VỊ BAN HÀNH" || text.StartsWith("Số:")?13:14;
            if(p.Parent==receiver){bool label=text=="Nơi nhận:";bold=label;size=label?12:11;}
            live.Add(new(cp,cp+full.Length,full,1,cell,centered && !text.EndsWith(' '),centered,centered));
            rows.Add(new(cp,full,Bold:bold,Size:size,Alignment:centered?1:0,InTable:cell,Page:1,FormatObserved:true,Italic:text=="Nơi nhận:"));cp+=full.Length;
        }
        var outline=new Dictionary<int,OutlineParagraphInfo>();var formats=new HashSet<int>();
        var editable=WordTextProof.Read(xml.ToString(),live,onOutline:(start,info)=>outline[start]=info,
            onFormatEditable:(start,ok)=>{if(ok)formats.Add(start);});
        return (WordTextProof.Apply(rows,editable,outline,formats).ToArray(),WordXmlTableMap.Read(xml.ToString(),live).ToArray());
    }
    public static void Run(Action<bool,string> assert)
    {
        var (rows,layout)=Sample();var targets=ComponentRules.Identify(rows,layout,DocumentProfile.State);
        assert(targets.Count==6 && targets.Count(t=>t.Role==ComponentRole.AgencyParent)==1 && targets.Count(t=>t.Role==ComponentRole.Agency)==1,
            "The header pair and the bounded recipient label/list have distinct roles");
        assert(ComponentRules.Check(rows,layout,DocumentProfile.State).Count==0,
            "Correct parent 13pt upright, issuing agency bold, recipient 12pt label and 11pt list produce no defect");
        assert(DocumentHeadingRules.Check(rows,layout,DocumentProfile.State).Count==0,
            "Separate centered subject paragraphs, a trailing space and centered Kính gửi do not create a false heading warning");
        foreach(bool soft in new[]{false,true})
        {
            var sample=Sample(soft,5);
            assert(DocumentHeadingRules.Check(sample.Rows,sample.Layout,DocumentProfile.State).Count==0,
                "A bounded valid subject may contain five paragraphs or a discretionary text line break");
            if(soft)
            {
                var broken=sample.Rows.Single(p=>p.Text.Contains('\v'));
                assert(!broken.TextEditable && broken.FormatEditable,"Text breaks gain character-format proof without opening ordinary text replacement");
                var changed=sample.Rows.Select(p=>p==broken?p with {Font="Arial",Size=12}:p).ToArray();
                var fix=DocumentHeadingRules.Check(changed,sample.Layout,DocumentProfile.State).Single(i=>i.Start==broken.Start);
                assert(fix.CanFix && fix.Original==fix.Replacement && fix.Original.Contains('\v'),"A subject font repair preserves its exact discretionary break");
                BatchPlan.Create([fix]);
            }
        }
        assert(!OutlineRules.Check(rows).Any(f=>rows.Any(p=>p.Outline?.Scope==OutlineScope.Recipient && p.Start==f.ParagraphStart)),
            "Legal recipient bullets are not body outline-order or singleton-bullet findings");
        foreach(var target in targets)
        {
            var changed=rows.Select(p=>p.Start==target.Row.Start?p with {Font="Arial",Size=10,Color=255}:p).ToArray();
            var issue=ComponentRules.Check(changed,layout,DocumentProfile.State).Single(i=>i.Start==target.Row.Start);
            assert(issue.CanFix && issue.CharacterFormat?.Role==target.Role && issue.Original==issue.Replacement && issue.Citation is not null,
                "A proven role with a real font/size/color defect offers a content-preserving repair");
            BatchPlan.Create([issue]);
            var styled=changed.Select(p=>p.Start==target.Row.Start?p with {Font="Times New Roman",Size=issue.CharacterFormat!.Size,
                Bold=issue.CharacterFormat.Bold??p.Bold,Italic=issue.CharacterFormat.Italic,Color=0}:p).ToArray();
            assert(ComponentRules.Check(styled,layout,DocumentProfile.State).Count==0,"The actual repaired component leaves no stale font warning");
        }
        var item=rows.Single(p=>p.Text.StartsWith("- Đơn vị"));
        var punctuation=rows.Select(p=>p.Start==item.Start?p with {Text="Đơn vị mẫu.\r"}:p).ToArray();
        var repairs=ComponentRules.Check(punctuation,layout,DocumentProfile.State).Where(i=>i.Kind==FixKind.RecipientText).ToArray();
        string repaired=punctuation.Single(p=>p.Start==item.Start).Text.TrimEnd('\r');
        foreach(var fix in BatchPlan.Create(repairs))repaired=repaired.Remove(fix.Start-item.Start,fix.Length).Insert(fix.Start-item.Start,fix.Replacement);
        assert(repairs.All(i=>i.CanFix) && repaired=="- Đơn vị mẫu;",
            "A proven recipient entry missing its dash and semicolon gets a specific repair");
        var changedBoth=punctuation.Select(p=>p.Start==item.Start?p with {Font="Arial",Size=14}:p).ToArray();
        assert(BatchPlan.Create(ComponentRules.Check(changedBoth,layout,DocumentProfile.State )).Count==3,
            "Recipient punctuation and character formatting may share the same selected paragraph safely");
        foreach(var profile in new[]{DocumentProfile.Party,DocumentProfile.Personal})
            assert(ComponentRules.Check(rows,layout,profile).Count==0,"State component rules do not apply to Party or personal profiles");
        var auto=Sample(automatic:true);
        assert(!auto.Rows.Any(p=>p.Outline?.Scope==OutlineScope.Recipient),"An automatic-numbering recipient block never gains literal dash insertion");
        var ordinary=new Paragraph(1000,"- Mục trong bảng\r",InTable:true,Outline:new(OutlineScope.Complex));
        assert(OutlineRules.Check([ordinary]).Count>0,"An unrelated table bullet still retains its structural warning");
        var body=new[]{new Paragraph(2000,"1. Mục đầu",Outline:new(OutlineScope.PlainBody)),new Paragraph(2020,"2. Mục tiếp",Outline:new(OutlineScope.PlainBody)),
            new Paragraph(2040,"5. Đề xuất",Outline:new(OutlineScope.PlainBody))};
        assert(OutlineRules.Check(body.Concat(rows.Where(p=>p.Outline?.Scope==OutlineScope.Recipient)).ToArray()).Any(f=>f.Issue.Replacement=="3"),
            "Excluding recipient lists does not suppress a real 1,2,5 body-order defect");
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        foreach(var element in new[]{new XElement(w+"br",new XAttribute(w+"type","page")),new XElement(w+"br",new XAttribute(w+"type","column")),
            new XElement(w+"tab"),new XElement(w+"drawing"),new XElement(w+"object")})
        {
            string visible=element.Name==w+"br"?((string?)element.Attribute(w+"type")=="page"?"\f":"\u000e"):element.Name==w+"tab"?"\t":"";
            var xml=new XDocument(new XElement(w+"document",new XElement(w+"body",new XElement(w+"p",new XElement(w+"r",new XElement(w+"t","Đầu"),element,new XElement(w+"t","cuối"))))));
            string text="Đầu"+visible+"cuối\r";var eligible=new HashSet<int>();
            WordTextProof.Read(xml.ToString(),[new(0,text.Length,text,1,false,true)],onFormatEditable:(start,ok)=>{if(ok)eligible.Add(start);});
            assert(eligible.Count==0,"Page/column breaks, tabs and embedded objects cannot gain discretionary-break format eligibility");
        }
        var longDoc=Sample(padding:50);
        var longRecipients=ComponentRules.Identify(longDoc.Rows,longDoc.Layout,DocumentProfile.State).Where(t=>t.Role is ComponentRole.RecipientLabel or ComponentRole.RecipientItem).ToArray();
        assert(longRecipients.Length==4 && longRecipients.All(t=>Array.FindIndex(longDoc.Rows,p=>p.Start==t.Row.Start)>40) &&
            ComponentRules.Check(longDoc.Rows,longDoc.Layout,DocumentProfile.State).Count==0,
            "Recipients after paragraph 40 retain full-body XML proof and their actual font-role classification");
        bool Reject(Issue issue){try{BatchPlan.Create([issue]);return false;}catch(InvalidOperationException){return true;}}
        var wrong=new Issue(0,3,"ABC","ABC","",false,FixKind.AgencyFormat,CharacterFormat:new(11,false,null,ComponentRole.RecipientItem));
        assert(Reject(wrong) && Reject(wrong with {Kind=FixKind.DocumentHeadingFormat}),"A font role cannot be transplanted onto a different repair kind");
    }
}
