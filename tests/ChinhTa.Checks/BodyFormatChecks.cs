using System.Xml.Linq;
using ChinhTa.Core;

public static class BodyFormatChecks
{
    public static (Paragraph[] P,LayoutParagraph[] L) Sample(int bodies=2)
    {
        var head=DocumentHeadingChecks.Sample();
        var rows=head.P.Take(10).ToList();int start=rows[^1].Start+rows[^1].Text.Length;
        for(int i=0;i<bodies;i++)
        {
            string text=$"Nội dung  công tác công tác đã được kiểm tra đầy đủ {i}.\r";
            rows.Add(new(start,text,Alignment:3,Page:2,FormatObserved:true,Outline:new(OutlineScope.PlainBody,Alignment:3),
                FormatEvidence:new(text.TrimEnd('\r'),[new("Arial",12,0,0,255),new("Times New Roman",14,0,0,0)])));
            start+=text.Length;
        }
        rows[9]=rows[9] with {Outline=new(OutlineScope.PlainBody,Alignment:0)};
        rows.Add(new(start,"Nơi nhận:\r",Outline:new(OutlineScope.Recipient,RecipientRole:ComponentRole.RecipientLabel)));
        return (rows.ToArray(),head.L.Take(10).ToArray());
    }
    private static CharacterStyleEvidence Styles(string text)=>new(text,text.Select((c,i)=>char.IsWhiteSpace(c)?null:
        new ObservedCharacterStyle(new(i%2==0?"Arial":"Times New Roman",i%2==0?12:14,0,0,i%2==0?255:0),i%3==0?1:0)),new(3,0,0,28.35f));
    public static void Run(Action<bool,string> assert)
    {
        var sample=Sample();var p=sample.P;var l=sample.L;var body=p[10];
        IReadOnlyList<Issue> Check(Paragraph[]? rows=null,LayoutParagraph[]? layout=null,DocumentProfile profile=DocumentProfile.State)=>BodyFormatRules.Check(rows??p,layout??l,profile);
        var fixes=Check();
        assert(fixes.Count==2 && fixes.All(i=>i.CanFix && i.Kind==FixKind.BodyFormat && i.Original==i.Replacement && i.CharacterFormat is {Role:ComponentRole.Body,RepairInvalidSizes:true,PreserveSize:true}),
            "Confirmed ordinary body paragraphs offer font/color/selective-size repairs without changing their text");
        assert(fixes.All(i=>i.CharacterFormat!.SizeRange==(13f,14f) && i.CharacterFormat.Valid && !i.Original.Contains('\r')),
            "Body sizing accepts 13–14pt independently of bold and excludes the paragraph marker");
        var fixedRows=p.Select(row=>row.FormatEvidence is null?row:row with {FormatEvidence=new(row.FormatEvidence.Text,
            row.FormatEvidence.Fonts.Select(f=>f with {Name="Times New Roman",Size=f.Size is >=13 and <=14?f.Size:13,Color=0}))}).ToArray();
        assert(Check(fixedRows).Count==0,"Fresh scanning removes every repaired body-format finding instead of leaving disabled rows");
        foreach(var fonts in new ObservedFont[][]{[new("Times New Roman",13,0,0,0),new("TIMES NEW ROMAN",14,0,0,-16777216)],
            [new("Times New Roman",13.5f,0,0,0)]})
        {
            var rows=p.ToArray();rows[10]=body with {FormatEvidence=new(body.Text.TrimEnd('\r'),fonts)};
            assert(!Check(rows).Any(i=>i.Start==body.Start),"Legal mixed sizes, intermediate sizes and equivalent black encodings never become body defects");
        }
        foreach(var unsafeRow in new[]{body with {InTable=true},body with {TextEditable=false,FormatEditable=true},body with {FormatObserved=false},
            body with {FormatEvidence=null},body with {FormatEvidence=new("old",body.FormatEvidence!.Fonts)},
            body with {Outline=null},body with {Outline=new(OutlineScope.Complex)},body with {Outline=new(OutlineScope.AutomaticList)},
            body with {Outline=new(OutlineScope.Recipient)},body with {Outline=new(OutlineScope.PlainBody,HeadingLevel:1,Alignment:3)},
            body with {Outline=new(OutlineScope.PlainBody,Alignment:1)},body with {Outline=new(OutlineScope.PlainBody,Alignment:2)},
            body with {Outline=new(OutlineScope.PlainBody,Alignment:null)}})
        {
            var rows=p.ToArray();rows[10]=unsafeRow;
            assert(!Check(rows).Any(i=>i.Start==body.Start) && Check(rows).Any(i=>i.Start==p[11].Start),
                "Unknown proof, structure, numbering, title level or unsupported alignment closes only that body row without inventing a manual defect");
        }
        foreach(var font in new[]{new ObservedFont("Arial",12,-1,0,255),new("Arial",12,0,-1,255),new("",12,0,0,255),new("Arial",float.NaN,0,0,255),new("Arial",12,0,9999999,255)})
        {
            var rows=p.ToArray();rows[10]=body with {FormatEvidence=new(body.Text.TrimEnd('\r'),[font])};
            assert(!Check(rows).Any(i=>i.Start==body.Start),"Emphasis and unreadable attributes never grant body formatting or produce a false defect");
        }
        foreach(string text in new[]{"1. Nội dung công tác đã được kiểm tra đầy đủ.","a) Nội dung công tác đã được kiểm tra đầy đủ.","A) Nội dung công tác đã được kiểm tra đầy đủ.","A)Nội dung công tác đã được kiểm tra đầy đủ.","IV.Nội dung công tác đã được kiểm tra đầy đủ.","- Nội dung công tác đã được kiểm tra đầy đủ.",
            "Điều 1. Nội dung công tác đã được kiểm tra đầy đủ.","Căn cứ nội dung công tác đã được kiểm tra đầy đủ.","NỘI DUNG CÔNG TÁC ĐÃ ĐƯỢC KIỂM TRA ĐẦY ĐỦ.",
            "“Nội dung công tác đã được kiểm tra đầy đủ.”","Nội dung\t công tác đã được kiểm tra đầy đủ.","Nội dung\v công tác đã được kiểm tra đầy đủ.","Nội dung ngắn.","Nội dung công tác đã được kiểm tra đầy đủ:"})
        {
            var rows=p.ToArray();rows[10]=body with {Text=text+"\r",FormatEvidence=new(text,body.FormatEvidence!.Fonts)};
            assert(!Check(rows).Any(i=>i.Start==body.Start),"Typed labels, quotations, controls, all-caps titles and short/unbounded text stay outside ordinary prose scope");
        }
        foreach(var rows in new[]{p.Where(r=>r!=p[^1]).ToArray(),p.Where(r=>r!=p[3]).ToArray(),p.Concat([p[10]]).ToArray(),
            p.Select((r,i)=>i==9?r with {Text="Phần mở đầu không có mốc rõ.\r"}:r).ToArray(),p.Select((r,i)=>i==9?r with {TextEditable=false}:r).ToArray()})
            assert(Check(rows).Count==0,"Missing greeting/end/header boundaries and duplicate live starts never infer body roles");
        var beforeTitle=p.Take(6).Concat([new Paragraph(p[6].Start-1,"Nội dung văn xuôi ở trước tên loại.\r")]).Concat(p.Skip(6)).ToArray();
        assert(Check(beforeTitle).Count==0,"A quoted document type after prior prose cannot establish a body region");
        assert(Check(layout:[]).Count==0 && Check(layout:l.Select(r=>r.Start==p[7].Start?r with {Centered=false,UnindentedCenter=false}:r).ToArray()).Count==0,
            "Unproven front title/subject geometry does not establish an ordinary body region");
        foreach(var profile in new[]{DocumentProfile.Party,DocumentProfile.Personal})
            assert(Check(profile:profile).Count==0,"State body formatting cannot leak into Party or personal documents");
        var late=Sample(48);assert(BodyFormatRules.Check(late.P,late.L,DocumentProfile.State).Count==48,
            "Confirmed body text beyond paragraph 40 is checked without extending header geometry reads");
        var stop=p.ToArray();stop[11]=p[11] with {Text="KT. GIÁM ĐỐC\r"};
        assert(Check(stop).Count==1,"A signature boundary stops body formatting before recipients or later text");
        var issue=fixes[0];var selection=Checker.Check([body]).Where(i=>i.CanFix).Append(issue).ToArray();
        var plan=BatchPlan.Create(selection);string story=body.Text;
        string expected=AtomicTextBatch.ExpectedStory(plan,story,i=>story[..(i.Start-body.Start)]);
        assert(expected=="Nội dung công tác đã được kiểm tra đầy đủ 0.\r" && AtomicTextBatch.ExpectedStory([issue],story,i=>"")==story,
            "Body formatting composes with selected spaces/repetition while a format-only action keeps unselected text errors");
        var probe=CasePreservationPlan.Regions(plan,[new(body.Start,body.Start+body.Text.Length,body.Text,2,false,false)]).Single();
        var before=Styles(probe.Original);var after=CasePreservationPlan.Expected(probe,plan,before,i=>probe.Original[..(i.Start-body.Start)]);
        assert(after.Text==expected.TrimEnd('\r') && after.Styles.Where(s=>s is not null).All(s=>s!.Font.Name=="Times New Roman" && s.Font.Color==0 && s.Font.Size is 13 or 14) && after.Paragraph==before.Paragraph,
            "The body postcondition projects retained glyphs through text edits and requires only the approved font/color/size changes");
        var only=CasePreservationPlan.Expected(probe,[issue],before,i=>"");
        assert(only.Styles.Select(s=>s?.Underline).SequenceEqual(before.Styles.Select(s=>s?.Underline)) && only.Styles.Select(s=>s?.Font.Size).SequenceEqual(before.Styles.Select(s=>s is null?null:(float?)(s.Font.Size==12?13:s.Font.Size))),
            "Body formatting retains every underline and legal size while correcting only invalid sizes");
        foreach(var drift in new[]{only.Styles.First(s=>s is not null)! with {Underline=7},only.Styles.First(s=>s is not null)! with {Font=new("Times New Roman",13,-1,0,0)}})
        {var altered=only.Styles.ToArray();altered[0]=drift;assert(!only.Matches(new(only.Text,altered,only.Paragraph)),"Retained body underline/bold drift fails verification even with correct font and size");}
        int legal=Array.FindIndex(only.Styles.ToArray(),s=>s?.Font.Size==14);var changedSize=only.Styles.ToArray();
        changedSize[legal]=changedSize[legal]! with {Font=changedSize[legal]!.Font with {Size=13}};
        assert(!only.Matches(new(only.Text,changedSize,only.Paragraph)),"Changing a retained legal14 to legal13 fails despite both sizes matching the body rule");
        assert(!only.Matches(new(only.Text,only.Styles,new(3,1,0,28.35f))),"Unselected paragraph indentation drift fails the body postcondition");
        bool Reject(Action a){try{a();return false;}catch(InvalidOperationException){return true;}}
        assert(Reject(()=>CurrentTextPlan.Validate([issue],[])) && Reject(()=>CurrentTextPlan.Validate([issue],[issue with {CharacterFormat=new(14,false,false,ComponentRole.Body)}])),
            "A lost body role or changed current format proposal invalidates an old selection before writes");
        assert(Reject(()=>BatchPlan.Create([issue with {CharacterFormat=null}])) && Reject(()=>BatchPlan.Create([issue with {CharacterFormat=new(13,false,true,ComponentRole.Body)}])) &&
            Reject(()=>BatchPlan.Create([issue with {Kind=FixKind.DocumentHeadingFormat}])) && Reject(()=>CasePreservationPlan.Regions([issue],[])),
            "Missing body format, invalid emphasis/role and unproven tracking bounds cannot grant a write");
        assert(Reject(()=>CasePreservationPlan.Expected(probe,[issue],new(before.Text,before.Styles,new(1,0,0,0)),i=>"")),
            "Actual Word centering outside the allowed body scope fails before the first write");
        bool restored=false;int writes=0;try{AtomicTextBatch.Apply([issue],i=>i.Original,i=>{writes++;return i.Replacement;},()=>throw new InvalidOperationException("verify"),()=>restored=true);}catch(InvalidOperationException){}
        assert(restored && writes==1,"Failed body save/postverification invokes whole-transaction rollback");
        var info=IssueReviewCatalog.Describe(issue);var tooltip=IssueTooltipCatalog.Describe(issue);
        assert(info.Section==IssueReviewSection.RequiredFormat && info.FamilyKey=="body-font" && tooltip.Text.Split('\n').Length==3 && tooltip.Basis.Contains("II.6") && tooltip.Basis.Contains("điểm e"),
            "Body findings are compact required-format rows with a three-part tooltip citing the verified body rule");
        XmlEvidence(assert);
    }
    private static void XmlEvidence(Action<bool,string> assert)
    {
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        string text="Nội dung công tác đã được kiểm tra đầy đủ.";
        var live=new[]{new WordParagraphSnapshot(4000,4000+text.Length+1,text+"\r",0,false,false)};
        foreach(var (jc,wanted) in new (string?,int?)[]{(null,0),("both",3),("center",1),("right",2),("start",null)})
        {
            var xml=new XDocument(new XElement(w+"document",new XElement(w+"body",new XElement(w+"p",new XElement(w+"pPr",jc is null?null:new XElement(w+"jc",new XAttribute(w+"val",jc))),new XElement(w+"r",new XElement(w+"t",text))))));
            OutlineParagraphInfo? info=null;WordTextProof.Read(xml.ToString(),live,onOutline:(_,found)=>info=found);
            assert(info is {Scope:OutlineScope.PlainBody} && info.Alignment==wanted,"Exact live XML supplies explicit/default paragraph alignment; unsupported direction remains unknown");
        }
        XElement Row()=>new(w+"p",new XElement(w+"r",new XElement(w+"t",text)));
        var inherited=Row();inherited.AddFirst(new XElement(w+"pPr",new XElement(w+"pStyle",new XAttribute(w+"val","Body"))));
        var styles=new XElement(w+"styles",new XElement(w+"style",new XAttribute(w+"type","paragraph"),new XAttribute(w+"styleId","Body"),
            new XElement(w+"basedOn",new XAttribute(w+"val","Base"))),new XElement(w+"style",new XAttribute(w+"type","paragraph"),new XAttribute(w+"styleId","Base"),
            new XElement(w+"pPr",new XElement(w+"jc",new XAttribute(w+"val","both")))));
        var inheritedXml=new XDocument(new XElement(w+"document",styles,new XElement(w+"body",inherited)));
        OutlineParagraphInfo? derived=null;WordTextProof.Read(inheritedXml.ToString(),live,onOutline:(_,found)=>derived=found);
        assert(derived is {Scope:OutlineScope.PlainBody,Alignment:3},"Body alignment follows a unique inherited paragraph style instead of a guessed Word default");
        var table=new XElement(w+"tbl",new XElement(w+"tr",new XElement(w+"tc",Row())));
        var tableXml=new XDocument(new XElement(w+"document",new XElement(w+"body",table)));
        derived=null;WordTextProof.Read(tableXml.ToString(),live,onOutline:(_,found)=>derived=found);
        assert(derived is {Scope:OutlineScope.Complex},"XML table ancestry blocks body formatting even when a late live snapshot has no table flag");
        var numbered=Row();numbered.AddFirst(new XElement(w+"pPr",new XElement(w+"numPr",new XElement(w+"numId",new XAttribute(w+"val",1)))));
        derived=null;WordTextProof.Read(new XDocument(new XElement(w+"document",new XElement(w+"body",numbered))).ToString(),live,onOutline:(_,found)=>derived=found);
        assert(derived is {Scope:OutlineScope.AutomaticList},"Word numbering stays outside ordinary-body formatting despite plain visible words");
    }
}
