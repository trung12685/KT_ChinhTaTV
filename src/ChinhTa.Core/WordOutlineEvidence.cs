using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ChinhTa.Core;

public enum OutlineScope { PlainBody, AutomaticList, Complex, Recipient }
public sealed record OutlineParagraphInfo(OutlineScope Scope,int Indent=0,int? HeadingLevel=null,bool HasReferences=false,bool LabelThroughTabs=false,ComponentRole? RecipientRole=null,bool LastRecipient=false,int? Alignment=null);

// Resolve paragraph-list evidence from the same live XML used for text proof.
// Unknown/cyclic styles or indentation never become ordinary manual list text.
internal sealed class WordOutlineEvidence(XDocument xml)
{
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private readonly IReadOnlyDictionary<XElement,(ComponentRole Role,bool Last)> recipients=RecipientContext.Read(xml);
    private readonly XElement? styles=xml.Descendants(W+"styles").FirstOrDefault();
    private readonly Dictionary<string,XElement[]> definitions=xml.Descendants(W+"styles").FirstOrDefault()?.Elements(W+"style")
        .GroupBy(s=>(string?)s.Attribute(W+"styleId")??"",StringComparer.Ordinal).ToDictionary(g=>g.Key,g=>g.ToArray(),StringComparer.Ordinal)??new();
    private readonly bool references=xml.Descendants().Any(e=>
        (e.Name==W+"instrText" || e.Name==W+"fldSimple") &&
        Regex.IsMatch(e.Name==W+"fldSimple"?(string?)e.Attribute(W+"instr")??"":e.Value,@"\b(?:REF|PAGEREF|NOTEREF)\b",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant));
    public OutlineParagraphInfo Describe(XElement p,bool textEditable)
    {
        if(recipients.TryGetValue(p,out var recipient))return new(OutlineScope.Recipient,RecipientRole:recipient.Role,LastRecipient:recipient.Last);
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
        return new(OutlineScope.PlainBody,left.Value+first.Value-hanging.Value,level,references,labelTabs,Alignment:alignment);
    }
}
