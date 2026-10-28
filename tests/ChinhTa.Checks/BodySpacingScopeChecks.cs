using System.Xml.Linq;
using ChinhTa.Core;

public static class BodySpacingScopeChecks
{
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private const string Text="Nội dung công tác đã được kiểm tra đầy đủ.";
    private const int NativeStart=1037;
    private sealed record Proof(OutlineParagraphInfo? Outline,bool Editable,bool FormatEditable,int? MatchedStart);
    private static XElement Property(string name,string? value=null)=>new(W+name,value is null?null:new XAttribute(W+"val",value));
    private static XElement Spacing(string name,string value)=>new(W+"spacing",new XAttribute(W+name,value));
    private static XElement Style(string id,XElement? properties,string? basedOn=null,bool isDefault=false)=>new(W+"style",
        new XAttribute(W+"type","paragraph"),new XAttribute(W+"styleId",id),isDefault?new XAttribute(W+"default",1):null,
        basedOn is null?null:new XElement(W+"basedOn",new XAttribute(W+"val",basedOn)),properties);
    private static Proof Read(XElement? direct=null,XElement? styles=null,XElement? section=null)
    {
        var xml=new XDocument(new XElement(W+"document",styles,new XElement(W+"body",
            new XElement(W+"p",direct,new XElement(W+"r",new XElement(W+"t",Text))),section)));
        OutlineParagraphInfo? outline=null;bool format=false;int? matched=null;
        var snapshot=WordXmlSnapshot.Parse(xml.ToString());
        var live=new[]{new WordParagraphSnapshot(NativeStart,NativeStart+Text.Length+3,Text+"\r",2,false,false)};
        var editable=WordTextProof.Read(snapshot,live,onOutline:(_,found)=>outline=found,
            onFormatEditable:(_,canEdit)=>format=canEdit,onParagraphMatched:(start,_)=>matched=start);
        return new(outline,editable.Contains(NativeStart),format,matched);
    }
    private static XElement Properties(params XElement[] properties)=>new(W+"pPr",Property("jc","both"),properties);
    private static (Proof Proof,bool Gap,bool Line) Assess(XElement? direct=null,XElement? styles=null,XElement? section=null)
    {
        var proof=Read(direct,styles,section);var context=proof.Outline?.SpacingContext;
        var style=new ObservedParagraphStyle(3,0,0,36,new(0,0,2,24,0,0));
        var next=new BodyGapNeighbor(2000,Text,style,new(),new(2,2));
        return(proof,context is not null && new BodySpacingFix(style,context,new(2,2),next,After:6).Valid,
            context is not null && new BodySpacingFix(style,context,new(2,2),Rule:0).Valid);
    }
    public static void Run(Action<bool,string> assert)
    {
        // Native paragraph point values alone do not prove that a point setter
        // controls the result. Line-unit properties can survive in the style tree.
        foreach(string attribute in new[]{"beforeLines","afterLines"})
        foreach(string value in new[]{"1","100","-1","bad","1.5","2147483648"})
        {
            var actual=Assess(Properties(Spacing(attribute,value)));
            assert(!actual.Gap && !actual.Line,"Nonzero or malformed XML line-unit spacing cannot authorize point/line repairs: "+attribute+"="+value);
            assert(actual.Proof is {Editable:true,FormatEditable:true,MatchedStart:NativeStart,Outline.Scope:OutlineScope.PlainBody},
                "An unsupported spacing unit leaves independent text/font proof and actual native CP intact");
        }
        foreach(string attribute in new[]{"beforeLines","afterLines"})
        foreach(string location in new[]{"style","basedOn","defaults","defaultStyle"})
        foreach(bool directZero in new[]{false,true})
        {
            var direct=Properties(Spacing(attribute=="beforeLines"?"before":"after","0"));
            if(location!="defaultStyle")direct.AddFirst(Property("pStyle","Body"));
            if(directZero)direct.Element(W+"spacing")!.SetAttributeValue(W+attribute,"0");
            var inherited=Properties(Spacing(attribute,"100"));
            var styles=new XElement(W+"styles");
            if(location=="defaults")styles.Add(Style("Body",null),new XElement(W+"docDefaults",new XElement(W+"pPrDefault",inherited)));
            else if(location=="basedOn")styles.Add(Style("Body",null,"Base"),Style("Base",inherited));
            else styles.Add(Style("Body",inherited,isDefault:location=="defaultStyle"));
            var actual=Assess(direct,styles);
            assert(!actual.Gap && !actual.Line && actual.Proof.Editable,
                "An ancestor line unit is not cleared merely by a direct point value or zero line-unit marker: "+location);
        }
        foreach(var direct in new[]{Properties(),Properties(new XElement(W+"spacing",new XAttribute(W+"beforeLines",0),new XAttribute(W+"afterLines",0))),
            Properties(new XElement(W+"spacing",new XAttribute(W+"before",0),new XAttribute(W+"after",0))),
            Properties(Property("bidi","off"),Property("textDirection","lrTb"),Property("textAlignment","baseline")),
            Properties(Property("textAlignment","auto"))})
        {
            var actual=Assess(direct);
            assert(actual.Gap && actual.Line,"Absent/all-zero line units and proven ordinary left-to-right text retain existing point-spacing repairs");
        }
        var specials=new[]{new XElement(W+"framePr",new XAttribute(W+"w",300)),new XElement(W+"sectPr"),
            new XElement(W+"pBdr",new XElement(W+"bottom",new XAttribute(W+"val","single"))),Property("bidi"),Property("bidi","invalid"),
            Property("textDirection","tbRl"),Property("textDirection","invalid"),Property("textAlignment","top"),Property("textAlignment","invalid")};
        foreach(var special in specials)
        foreach(bool inherited in new[]{false,true})
        {
            var actual=inherited?Assess(Properties(Property("pStyle","Body")),new XElement(W+"styles",Style("Body",Properties(new XElement(special))))):
                Assess(Properties(new XElement(special)));
            assert(!actual.Gap && !actual.Line && actual.Proof is {Editable:true,FormatEditable:true,Outline.Scope:OutlineScope.PlainBody},
                "Special paragraph geometry excludes spacing edits while retaining separately proven text/font operations: "+special.Name.LocalName);
        }
        var leftToRight=Assess(Properties(Property("pStyle","Body"),Property("bidi","0")),
            new XElement(W+"styles",Style("Body",Properties(Property("bidi","1")))));
        assert(leftToRight.Gap && leftToRight.Line,"A direct known false bidi value overrides inherited bidi without guessing geometry");
        var finalSection=Assess(Properties(),section:new XElement(W+"sectPr"));
        assert(finalSection.Gap && finalSection.Line,"The final body section property is not mistaken for a paragraph section-break layout");
        foreach(var direct in new[]{Properties(Spacing("before","bad")),Properties(Spacing("after","-1")),Properties(Spacing("line","bad")),
            Properties(Spacing("lineRule","invalid")),Properties(Spacing("beforeAutospacing","invalid")),
            Properties(Spacing("afterAutospacing","invalid")),Properties(Spacing("after","0"),Spacing("after","0"))})
        {
            var actual=Assess(direct);
            assert(!actual.Gap && !actual.Line && actual.Proof.Editable,
                "Malformed or duplicated XML spacing does not gain repair permission from ordinary-looking native point values");
        }
        foreach(var direct in new[]{Properties(Property("textDirection")),Properties(Property("textAlignment")),
            Properties(Property("bidi","0"),Property("bidi","1")),
            Properties(Property("contextualSpacing","0"),Property("contextualSpacing","1")),
            Properties(Property("keepNext","0"),Property("keepNext","1")),
            Properties(Property("pageBreakBefore","0"),Property("pageBreakBefore","1")),
            Properties(Property("textDirection","lrTb"),Property("textDirection","tbRl")),
            Properties(Property("textAlignment","baseline"),Property("textAlignment","top")),
            Properties(new XElement(W+"spacing",new XElement(W+"unknown")))})
        {
            var actual=Assess(direct);
            assert(!actual.Gap && !actual.Line && actual.Proof.Editable,
                "Missing required layout values, conflicting duplicate flags or invalid spacing children cannot hide behind the first ordinary value");
        }
        foreach(var styles in new[]{new XElement(W+"styles",Style("Body",null,"Missing")),
            new XElement(W+"styles",Style("Body",null,"Base"),Style("Base",null,"Body")),
            new XElement(W+"styles",Style("Body",Properties()),Style("Body",Properties()))})
        {
            var actual=Assess(Properties(Property("pStyle","Body")),styles);
            assert(!actual.Gap && !actual.Line && actual.Proof.Editable,
                "Unknown, cyclic or duplicate style definitions keep spacing permissions closed independently of plain-text proof");
        }
        var ordinary=Assess(Properties());var blocked=Assess(Properties(Spacing("afterLines","100")));
        var sample=BodySpacingChecks.Sample(4);var rows=sample.P.ToArray();
        rows[10]=rows[10] with {Outline=blocked.Proof.Outline};
        var findings=BodySpacingRules.Check(rows,sample.L,DocumentProfile.State);
        assert(!findings.Any(i=>i.Start==rows[10].Start) && findings.Any(i=>i.Kind==FixKind.BodyGap && i.Start==rows[12].Start) &&
            findings.Any(i=>i.Kind==FixKind.BodyLineSpacing && i.Start==rows[12].Start),
            "A closed spacing row is not bridged, while later independent ordinary paragraph pairs remain repairable");
        assert(BodyFormatRules.Check(rows,sample.L,DocumentProfile.State).Any(i=>i.Start==rows[10].Start),
            "An unsupported spacing unit does not suppress an independently proven font/color/size proposal");
        rows=sample.P.ToArray();rows[11]=rows[11] with {Outline=blocked.Proof.Outline};
        findings=BodySpacingRules.Check(rows,sample.L,DocumentProfile.State);
        assert(!findings.Any(i=>i.Kind==FixKind.BodyGap && i.Start==rows[10].Start) && findings.Any(i=>i.Kind==FixKind.BodyLineSpacing && i.Start==rows[10].Start),
            "A neighbor with unknown effective spacing blocks the pair repair but not an independent line repair on the previous paragraph");
        var gap=BodySpacingRules.Check(sample.P,sample.L,DocumentProfile.State).Single(i=>i.Kind==FixKind.BodyGap && i.Start==sample.P[10].Start);
        var changed=gap with {SpacingFix=gap.SpacingFix! with {Context=blocked.Proof.Outline!.SpacingContext!}};
        bool stale=false;try{CurrentTextPlan.Validate([gap],[changed]);}catch(InvalidOperationException){stale=true;}
        assert(stale && !RepairDependencies.Same(gap,changed),"A changed XML spacing scope invalidates stale selected gap permissions and dependency identity");
        string story=string.Concat(sample.P.Select(p=>p.Text));var proofs=BodySpacingVerification.Build([gap],story,i=>story[..i.Start]);
        rows=sample.P.ToArray();rows[10]=rows[10] with {ParagraphStyle=rows[10].ParagraphStyle! with {Spacing=rows[10].ParagraphStyle!.Spacing! with {After=6}},
            Outline=blocked.Proof.Outline};
        bool rejected=false;try{BodySpacingVerification.Verify(proofs,rows,p=>story[..p.Start]);}catch(InvalidOperationException){rejected=true;}
        assert(rejected,"After-spacing 6 pt alone cannot pass final verification when the selected XML spacing scope becomes unsupported");
        Coverage(assert);
    }
    private static void Coverage(Action<bool,string> assert)
    {
        var sample=BodySpacingChecks.Sample(4);var targets=sample.P.Skip(10).Take(4).ToArray();
        assert(BodySpacingCoverage.Describe([]) is null && BodySpacingCoverage.Describe(targets) is null,
            "Empty or fully supported body spacing does not add a manual-coverage notice");
        var cases=new (Paragraph Row,string Reason)[]{
            (targets[0] with {Outline=targets[0].Outline! with {SpacingContext=new(LineUnits:true)}},"khoảng đoạn theo đơn vị dòng"),
            (targets[0] with {Outline=targets[0].Outline! with {SpacingContext=new(SpecialLayout:true)}},"bố cục đoạn đặc biệt"),
            (targets[0] with {Outline=targets[0].Outline! with {SpacingContext=new(Grid:true)}},"lưới dòng"),
            (targets[0] with {Outline=targets[0].Outline! with {SpacingContext=new(Contextual:true)}},"khoảng tự động/theo kiểu đoạn"),
            (targets[0] with {Outline=targets[0].Outline! with {SpacingContext=new(KeepNext:true)}},"ràng buộc ngắt/giữ trang"),
            (targets[0] with {Outline=targets[0].Outline! with {SpacingContext=new(PageBreakBefore:true)}},"ràng buộc ngắt/giữ trang"),
            (targets[0] with {ParagraphStyle=targets[0].ParagraphStyle! with {Spacing=targets[0].ParagraphStyle!.Spacing! with {Rule=3}}},"Exactly/AtLeast"),
            (targets[0] with {ParagraphStyle=targets[0].ParagraphStyle! with {Spacing=targets[0].ParagraphStyle!.Spacing! with {Rule=4}}},"Exactly/AtLeast"),
            (targets[0] with {ParagraphStyle=targets[0].ParagraphStyle! with {Spacing=targets[0].ParagraphStyle!.Spacing! with {BeforeAuto=-1}}},"khoảng tự động/theo kiểu đoạn"),
            (targets[0] with {Flow=null},"thiếu quan sát chắc chắn"),
            (targets[0] with {ParagraphStyle=null},"thiếu quan sát chắc chắn"),
            (targets[0] with {Outline=targets[0].Outline! with {SpacingContext=null}},"thiếu quan sát chắc chắn")};
        foreach(var item in cases)
        {
            var source=targets.ToArray();source[0]=item.Row;var message=BodySpacingCoverage.Describe(source);
            assert(message is not null && message.Contains("1/4") && message.Contains(item.Reason) && message.Contains("không phải kết luận"),
                "Unsupported body spacing is an explicit counted coverage limit, not a manufactured repairable defect: "+item.Reason);
        }
        var mixed=targets.ToArray();mixed[0]=targets[0] with {Outline=targets[0].Outline! with {SpacingContext=new(Grid:true,KeepNext:true,LineUnits:true)}};
        mixed[1]=targets[1] with {Outline=targets[1].Outline! with {SpacingContext=new(LineUnits:true)}};
        var combined=BodySpacingCoverage.Describe(mixed)!;
        assert(combined.Contains("2/4") && combined.Contains("khoảng đoạn theo đơn vị dòng: 2") && combined.Contains("lưới dòng: 1") &&
            combined.Contains("ràng buộc ngắt/giữ trang: 1") && BodySpacingCoverage.Describe(mixed)==combined,
            "Coverage counts each paragraph once while reporting all reasons with stable output");
        var sourceRows=sample.P.ToArray();sourceRows[10]=mixed[0];string? emitted=null;
        var issues=BodyRuleScan.Check(sourceRows,sample.L,DocumentProfile.State,m=>emitted=m);
        assert(emitted is not null && emitted.Contains("1/4") && issues.All(i=>i.Kind is not (FixKind.BodyGap or FixKind.BodyLineSpacing) || i.Start!=sourceRows[10].Start) &&
            issues.Any(i=>i.Kind==FixKind.BodyFormat && i.Start==sourceRows[10].Start),
            "The one-pass body scan reports spacing limitations alongside independent font proposals without enabling unsafe spacing writes");
    }
}
