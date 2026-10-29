using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ChinhTa.Core;

public sealed record BodyIndent(int Left,int Right,int First,int Hanging);
public enum OutlineScope { PlainBody, AutomaticList, Complex, Recipient, Signature }
public sealed record OutlineParagraphInfo(OutlineScope Scope,int Indent=0,int? HeadingLevel=null,bool HasReferences=false,bool LabelThroughTabs=false,ComponentRole? RecipientRole=null,bool LastRecipient=false,int? Alignment=null,BodySpacingContext? SpacingContext=null,BodyIndent? BodyIndent=null,ComponentRole? SignatureRole=null);

// Resolve paragraph-list evidence from the same live XML used for text proof.
// Unknown/cyclic styles or indentation never become ordinary manual list text.
internal sealed class WordOutlineEvidence(XDocument xml,IReadOnlySet<XElement>? annotationTextScope=null)
{
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private readonly IReadOnlySet<XElement> annotatedParagraphs=annotationTextScope??WordAnnotationTextScope.Read(
        xml.Descendants(W+"document").Single().Element(W+"body")??throw new InvalidOperationException("Missing document body"));
    private readonly IReadOnlyDictionary<XElement,(ComponentRole Role,bool Last)> recipients=RecipientContext.Read(xml);
    private IReadOnlyDictionary<XElement,ComponentRole>? signatures;
    private readonly XElement? styles=xml.Descendants(W+"styles").FirstOrDefault();
    private readonly Dictionary<string,XElement[]> definitions=xml.Descendants(W+"styles").FirstOrDefault()?.Elements(W+"style")
        .GroupBy(s=>(string?)s.Attribute(W+"styleId")??"",StringComparer.Ordinal).ToDictionary(g=>g.Key,g=>g.ToArray(),StringComparer.Ordinal)??new();
    private readonly bool grid=xml.Descendants(W+"docGrid").Any(e=>((string?)e.Attribute(W+"type")??"default")!="default");
    private readonly bool references=xml.Descendants().Any(e=>
        (e.Name==W+"instrText" || e.Name==W+"fldSimple") &&
        Regex.IsMatch(e.Name==W+"fldSimple"?(string?)e.Attribute(W+"instr")??"":e.Value,@"\b(?:REF|PAGEREF|NOTEREF)\b",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant));
    public OutlineParagraphInfo Describe(XElement p,bool textEditable)
    {
        if(annotatedParagraphs.Contains(p))return new(OutlineScope.Complex,HasReferences:references);
        if(recipients.TryGetValue(p,out var recipient))return new(OutlineScope.Recipient,RecipientRole:recipient.Role,LastRecipient:recipient.Last);
        signatures??=SignatureContext.Read(xml,recipients);
        if(signatures.TryGetValue(p,out var signature))return new(OutlineScope.Signature,SignatureRole:signature);
        OutlineParagraphInfo Complex()=>new(OutlineScope.Complex,HasReferences:references);
        bool labelTabs=!textEditable && p.Descendants(W+"tab").Any(e=>e.Parent?.Name==W+"r") && WordXmlTableMap.IsPlainText(p,allowTabs:true);
        if(!textEditable && !labelTabs || p.Ancestors(W+"tc").Any() || p.Descendants().Any(e=>e.Name==W+"bookmarkStart" || e.Name==W+"bookmarkEnd"))return Complex();
        var sources=new List<XElement?>{p.Element(W+"pPr")};
        string? id=(string?)sources[0]?.Element(W+"pStyle")?.Attribute(W+"val");
        id??=(string?)styles?.Elements(W+"style").FirstOrDefault(s=>(string?)s.Attribute(W+"type")=="paragraph" && (string?)s.Attribute(W+"default") is "1" or "true")?.Attribute(W+"styleId");
        var seen=new HashSet<string>();
        while(id is not null)
        {
            if(!seen.Add(id))return Complex();
            definitions.TryGetValue(id,out var matches);
            if(matches is not {Length:1} || (string?)matches[0].Attribute(W+"type")!="paragraph")return Complex();
            sources.Add(matches[0].Element(W+"pPr"));
            id=(string?)matches[0].Element(W+"basedOn")?.Attribute(W+"val");
        }
        sources.Add(styles?.Element(W+"docDefaults")?.Element(W+"pPrDefault")?.Element(W+"pPr"));
        string? Numbering()=>sources.Select(s=>(string?)s?.Element(W+"numPr")?.Element(W+"numId")?.Attribute(W+"val")).FirstOrDefault(v=>v is not null);
        string? number=Numbering();
        if(number is not null)
        {
            if(!int.TryParse(number,NumberStyles.None,CultureInfo.InvariantCulture,out int numId))return Complex();
            if(numId>0)return new(OutlineScope.AutomaticList,HasReferences:references);
        }
        else if(sources.Any(s=>s?.Element(W+"numPr") is not null))return Complex();
        int? ReadIndent(string name,string? alias=null)
        {
            string? raw=sources.Select(s=>(string?)(s?.Element(W+"ind")?.Attribute(W+(alias??name))??s?.Element(W+"ind")?.Attribute(W+name))).FirstOrDefault(v=>v is not null);
            if(raw is null)return 0;
            return int.TryParse(raw,NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out int value) && value is >=-31680 and <=31680?value:null;
        }
        if(sources.Any(s=>s?.Element(W+"ind")?.Attributes().Any(a=>a.Name.LocalName.EndsWith("Chars",StringComparison.Ordinal))==true))return Complex();
        int? left=ReadIndent("left","start"),first=ReadIndent("firstLine"),hanging=ReadIndent("hanging");
        if(left is null || first is null || hanging is null || first!=0 && hanging!=0)return Complex();
        string? heading=sources.Select(s=>(string?)s?.Element(W+"outlineLvl")?.Attribute(W+"val")).FirstOrDefault(v=>v is not null);
        int? level=null;
        if(heading is not null)
        {if(!int.TryParse(heading,out int n) || n is <0 or >9)return Complex();if(n<9)level=n;}
        string? justify=sources.Select(s=>(string?)s?.Element(W+"jc")?.Attribute(W+"val")).FirstOrDefault(v=>v is not null);
        int? alignment=justify switch {null or "left"=>0,"center"=>1,"right"=>2,"both"=>3,_=>null};
        bool? Flag(string name)
        {
            var node=sources.Select(s=>s?.Element(W+name)).FirstOrDefault(e=>e is not null);
            if(node is null)return false;
            return (string?)node.Attribute(W+"val") switch {null or "1" or "true" or "on"=>true,"0" or "false" or "off"=>false,_=>null};
        }

        var contextual=Flag("contextualSpacing");var pageBreak=Flag("pageBreakBefore");var keep=Flag("keepNext");
        // COM exposes point values, not their inherited line-unit source. A
        // non-zero beforeLines/afterLines can override later point spacing in
        // Word, so a direct zero/point value must not erase that evidence.
        bool lineUnits=sources.SelectMany(s=>s?.Elements(W+"spacing")??[]).Any(s=>
            new[]{"beforeLines","afterLines"}.Any(name=>s.Attribute(W+name) is { } a &&
                (!int.TryParse(a.Value,NumberStyles.None,CultureInfo.InvariantCulture,out int value) || value!=0)));
        bool special=p.Elements(W+"pPr").Skip(1).Any() || sources.Any(s=>s is not null &&
            (new[]{"framePr","sectPr","pBdr"}.Any(name=>s.Element(W+name) is not null) ||
             new[]{"spacing","bidi","contextualSpacing","keepNext","pageBreakBefore","textDirection","textAlignment"}
                .Any(name=>s.Elements(W+name).Skip(1).Any()) ||
             new[]{"textDirection","textAlignment"}.Any(name=>s.Element(W+name) is { } node && node.Attribute(W+"val") is null) ||
             s.Elements(W+"spacing").Any(UnknownSpacing))) ||
            Flag("bidi")!=false ||
            sources.Select(s=>(string?)s?.Element(W+"textDirection")?.Attribute(W+"val")).FirstOrDefault(v=>v is not null) is not (null or "lrTb") ||
            sources.Select(s=>(string?)s?.Element(W+"textAlignment")?.Attribute(W+"val")).FirstOrDefault(v=>v is not null) is not (null or "auto" or "baseline");
        BodySpacingContext? spacingContext=contextual is bool c && pageBreak is bool b && keep is bool k?new(c,grid,b,k,lineUnits,special):null;
        int? right=ReadIndent("right","end");
        BodyIndent? bodyIndent=right is int r && Flag("bidi")==false?new(left.Value,r,first.Value,hanging.Value):null;
        return new(OutlineScope.PlainBody,left.Value+first.Value-hanging.Value,level,references,labelTabs,Alignment:alignment,SpacingContext:spacingContext,BodyIndent:bodyIndent);
    }
    private static bool UnknownSpacing(XElement spacing)
    {
        if(spacing.HasElements || !string.IsNullOrWhiteSpace(spacing.Value))return true;
        foreach(var attribute in spacing.Attributes().Where(a=>!a.IsNamespaceDeclaration))
        {
            if(attribute.Name.Namespace!=W)return true;
            string name=attribute.Name.LocalName,value=attribute.Value;
            if(name is "beforeAutospacing" or "afterAutospacing")
            {if(value is not ("0" or "1" or "false" or "true" or "off" or "on"))return true;}
            else if(name=="lineRule")
            {if(value is not ("auto" or "exact" or "atLeast"))return true;}
            else if(name is "before" or "after" or "line" or "beforeLines" or "afterLines")
            {if(!int.TryParse(value,NumberStyles.None,CultureInfo.InvariantCulture,out _))return true;}
            else return true;
        }
        return false;
    }
}
