using System.Xml.Linq;

namespace ChinhTa.Core;

// Empty text alone is insufficient: paragraph/default style numbering, borders
// and run decoration can paint without text. Word's rendered body containment
// is checked separately because even an empty paragraph can reserve height.
public static class EmptyHeaderFooterScope
{
    private static readonly XNamespace W=PageNumberPackage.W;
    private static readonly XNamespace R=PageNumberPackage.R;
    public static bool HasReferences(XDocument xml)=>PageLayoutGuard.Body(xml)?.Descendants()
        .Any(e=>e.Name==W+"headerReference" || e.Name==W+"footerReference")==true;
    private static bool Ref(XElement element)=>element.Name==W+"headerReference" || element.Name==W+"footerReference";
    private static bool StructuralText(XElement element)=>element.Nodes().OfType<XText>().Any(t=>!string.IsNullOrWhiteSpace(t.Value));
    private static bool Reference(XElement element)=>!element.Elements().Any() && !StructuralText(element) &&
        element.Attributes().All(a=>a.IsNamespaceDeclaration || a.Name==W+"type" || a.Name==R+"id") &&
        (string?)element.Attribute(W+"type") is "default" or "first" or "even" && !string.IsNullOrWhiteSpace((string?)element.Attribute(R+"id"));
    public static bool Proven(XDocument xml,PageNumberPackage? package=null)
    {
        var body=PageLayoutGuard.Body(xml);var sections=body?.Descendants(W+"sectPr").ToArray();
        if(body is null || sections is not {Length:1} || sections[0]!=body.Elements().LastOrDefault())return false;
        var refs=body.Descendants().Where(Ref).ToArray();
        if(refs.Length==0)return true;
        if(refs.Any(e=>e.Parent!=sections[0] || !Reference(e)) ||
            refs.GroupBy(e=>(e.Name,(string?)e.Attribute(W+"type"))).Any(g=>g.Count()!=1))return false;
        try
        {
            package??=PageNumberPackage.FromFlat(xml.ToString(SaveOptions.DisableFormatting));
            var own=package.Section?.Elements().Where(Ref).ToArray();
            // Prefix and namespace declaration placement can differ between a
            // Flat OPC snapshot and its separately parsed document part.
            string Key(XElement e)=>e.Name+"|"+(string?)e.Attribute(W+"type")+"|"+(string?)e.Attribute(R+"id");
            if(own is null || own.Any(e=>!Reference(e)) || !refs.Select(Key).Order().SequenceEqual(own.Select(Key).Order()))return false;
            foreach(var reference in refs)
            {
                var part=package.Resolve(reference);
                string kind=reference.Name==W+"headerReference"?"hdr":"ftr";
                if(part is null || part.Value.Root.Name!=W+kind || !Empty(part.Value.Root,package))return false;
            }
            return true;
        }
        catch(Exception error) when(error is InvalidOperationException or System.Xml.XmlException or UriFormatException or ArgumentException)
        {return false;}
    }
    private static bool Empty(XElement root,PageNumberPackage package)
    {
        if(StructuralText(root) || root.Elements().Count()>1 || root.Elements().Any(e=>e.Name!=W+"p"))return false;
        var styles=package.Parts.GetValueOrDefault("word/styles.xml")?.Root;
        if(styles is not null && styles.Name!=W+"styles")return false;
        foreach(var p in root.Elements())
        {
            if(StructuralText(p) || p.Elements(W+"pPr").Count()>1 || p.Elements().Any(e=>e.Name!=W+"pPr" && e.Name!=W+"r"))return false;
            foreach(var run in p.Elements(W+"r"))
                if(StructuralText(run) || run.Elements(W+"rPr").Count()>1 ||
                    run.Elements().Any(e=>e.Name!=W+"rPr" && e.Name!=W+"t" || e.Name==W+"t" && (e.Value.Length!=0 || e.Elements().Any())) ||
                    !SafeRun(run.Element(W+"rPr")))return false;
            var sources=new List<XElement?>{p.Element(W+"pPr")};
            var chosen=sources[0]?.Elements(W+"pStyle").ToArray()??[];
            if(chosen.Length>1 || chosen.Length==1 && !StyleId(chosen[0]))return false;
            string? id=(string?)chosen.SingleOrDefault()?.Attribute(W+"val");
            var paragraphStyles=styles?.Elements(W+"style").Where(e=>(string?)e.Attribute(W+"type")=="paragraph").ToArray()??[];
            if(paragraphStyles.Any(e=>e.Attribute(W+"default") is not null &&
                (string?)e.Attribute(W+"default") is not ("1" or "true" or "on" or "0" or "false" or "off")))return false;
            var defaults=paragraphStyles.Where(e=>(string?)e.Attribute(W+"default") is "1" or "true" or "on").ToArray();
            if(defaults.Length>1 || defaults.Length==1 && string.IsNullOrWhiteSpace((string?)defaults[0].Attribute(W+"styleId")))return false;
            id??=(string?)defaults.SingleOrDefault()?.Attribute(W+"styleId");var seen=new HashSet<string>();
            while(id is not null)
            {
                var matches=styles?.Elements(W+"style").Where(e=>(string?)e.Attribute(W+"styleId")==id).ToArray();
                if(!seen.Add(id) || matches is not {Length:1} || (string?)matches[0].Attribute(W+"type")!="paragraph" ||
                    matches[0].Elements(W+"rPr").Count()>1 || matches[0].Elements(W+"pPr").Count()>1 ||
                    !SafeRun(matches[0].Element(W+"rPr")))return false;
                sources.Add(matches[0].Element(W+"pPr"));var parents=matches[0].Elements(W+"basedOn").ToArray();
                if(parents.Length>1 || parents.Length==1 && !StyleId(parents[0]))return false;
                id=(string?)parents.SingleOrDefault()?.Attribute(W+"val");
            }
            var defaultsRoot=styles?.Elements(W+"docDefaults").ToArray()??[];
            if(defaultsRoot.Length>1)return false;
            var pDefault=defaultsRoot.SingleOrDefault()?.Elements(W+"pPrDefault").ToArray()??[];
            var rDefault=defaultsRoot.SingleOrDefault()?.Elements(W+"rPrDefault").ToArray()??[];
            if(pDefault.Length>1 || rDefault.Length>1 ||
                pDefault.FirstOrDefault()?.Elements().Any(e=>e.Name!=W+"pPr")==true || rDefault.FirstOrDefault()?.Elements().Any(e=>e.Name!=W+"rPr")==true ||
                pDefault.FirstOrDefault()?.Elements(W+"pPr").Count()>1 ||
                rDefault.FirstOrDefault()?.Elements(W+"rPr").Count()>1)return false;
            sources.Add(pDefault.FirstOrDefault()?.Element(W+"pPr"));
            if(!SafeRun(rDefault.FirstOrDefault()?.Element(W+"rPr")))return false;
            foreach(var properties in sources.Where(s=>s is not null))
            {
                string[] ordinary=["pStyle","spacing","ind","jc","tabs","contextualSpacing","snapToGrid","widowControl","keepNext","keepLines"];
                if(properties!=sources[0] && properties!.Elements(W+"pStyle").Any() || StructuralText(properties!) || properties!.Elements().Any(e=>e.Name.Namespace!=W || !ordinary.Contains(e.Name.LocalName) ||
                        e.Name!=W+"tabs" && e.Elements().Any() || StructuralText(e)) ||
                    properties.Elements().GroupBy(e=>e.Name).Any(g=>g.Count()>1))return false;
                var tabs=properties.Element(W+"tabs");
                if(tabs is not null && tabs.Elements().Any(e=>e.Name!=W+"tab" || e.Elements().Any() || StructuralText(e)))return false;
            }
        }
        return true;
    }
    private static bool StyleId(XElement element)=>!string.IsNullOrWhiteSpace((string?)element.Attribute(W+"val")) &&
        !element.Elements().Any() && !StructuralText(element);
    private static bool SafeRun(XElement? properties)
    {
        string[] names=["rFonts","b","bCs","i","iCs","color","sz","szCs","lang","noProof","snapToGrid"];
        return properties is null || !StructuralText(properties) &&
            properties.Elements().All(e=>e.Name.Namespace==W && names.Contains(e.Name.LocalName) && !e.Elements().Any() && !StructuralText(e)) &&
            !properties.Elements().GroupBy(e=>e.Name).Any(g=>g.Count()>1);
    }
}
