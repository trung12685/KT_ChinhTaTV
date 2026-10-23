using System.Xml.Linq;

namespace ChinhTa.Core;

// Character-format eligibility only. A floating, text-free owned Line does not
// make the visible words unsafe to format. Ordinary text replacement stays closed.
// Identity/structure never proves where a Line rendered; XPS checks remain separate.
internal static class OwnedLineFormatProof
{
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly XNamespace A="http://schemas.openxmlformats.org/drawingml/2006/main";
    private static readonly XNamespace WPS="http://schemas.microsoft.com/office/word/2010/wordprocessingShape";
    private static readonly XNamespace WP="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing";
    private static readonly XNamespace MC="http://schemas.openxmlformats.org/markup-compatibility/2006";
    private static readonly XNamespace V="urn:schemas-microsoft-com:vml";
    internal static HashSet<XElement> Read(XDocument xml)
    {
        IReadOnlyList<DocxLineMarker> markers;
        try{markers=DocxLineMetadata.ReadXml(xml);}
        catch(InvalidDataException){return [];}
        var ids=markers.Where(m=>m.IsLine).Select(m=>m.Id).ToHashSet();
        return xml.Descendants(W+"drawing").Where(d=>
        {
            var properties=d.Descendants(WP+"docPr").ToArray();
            return properties.Length==1 && int.TryParse((string?)properties[0].Attribute("id"),out int id) && ids.Contains(id) &&
                d.Elements().Count()==1 && d.Element(WP+"anchor")==properties[0].Parent &&
                !d.Descendants().Any(e=>e.Name.Namespace==W || e.Name==A+"t" || e.Name==A+"blip" || e.Name==WPS+"txbx" || e.Name==WPS+"linkedTxbx");
        }).Select(Container).OfType<XElement>().ToHashSet();
    }
    private static XElement? Container(XElement drawing)
    {
        if(drawing.Parent?.Name==W+"r" && drawing.Parent.Parent?.Name==W+"p")return drawing;
        var choice=drawing.Parent;var wrapper=choice?.Parent;
        if(choice?.Name!=MC+"Choice" || wrapper?.Name!=MC+"AlternateContent" ||
            wrapper.Parent?.Name!=W+"r" || wrapper.Parent.Parent?.Name!=W+"p" ||
            choice.Elements().Count()!=1 || wrapper.Elements(MC+"Choice").Count()!=1 ||
            wrapper.Elements(MC+"Fallback").Count()>1 || wrapper.Elements().Any(e=>e!=choice && e.Name!=MC+"Fallback"))return null;
        string[] requires=((string?)choice.Attribute("Requires")??"").Split(' ',StringSplitOptions.RemoveEmptyEntries);
        if(requires.Length!=1 || choice.GetNamespaceOfPrefix(requires[0])!=WPS)return null;
        if(wrapper.Element(MC+"Fallback") is XElement fallback)
        {
            // Word may retain a VML compatibility branch. Accept only one
            // text-free line; never discard fallback text, images or objects.
            if(fallback.Elements().Count()!=1 || fallback.Element(W+"pict") is not XElement pict || pict.Elements().Count()!=1)return null;
            var line=pict.Elements().Single();
            if(line.Name!=V+"line" && !(line.Name==V+"shape" && (string?)line.Attribute("type")=="#_x0000_t32"))return null;
            if(line.Descendants().Any(e=>e.Name.Namespace==W || e.Name==V+"textbox" || e.Name==V+"imagedata" ||
                e.Name==A+"t" || e.Name.NamespaceName=="http://schemas.openxmlformats.org/officeDocument/2006/math"))return null;
            if(line.DescendantNodes().OfType<XText>().Any(t=>!string.IsNullOrWhiteSpace(t.Value)))return null;
        }
        return wrapper;
    }
    internal static bool IsFormatText(XElement paragraph,IReadOnlySet<XElement> ownedContainers)
    {
        if(paragraph.Ancestors(W+"sdt").Any())return false;
        var drawings=paragraph.Descendants(W+"drawing").ToArray();
        if(drawings.Length==0)return WordXmlTableMap.IsPlainText(paragraph,allowTextBreaks:true);
        if(drawings.Any(d=>!d.AncestorsAndSelf().Any(ownedContainers.Contains)))return false;
        var textOnly=new XElement(paragraph);
        // Work only on a proof copy. Word can merge a drawing into a text run:
        // remove the validated object container, never the containing text run.
        var originals=paragraph.Descendants().ToArray();var copies=textOnly.Descendants().ToArray();
        for(int i=originals.Length-1;i>=0;i--)if(ownedContainers.Contains(originals[i]))copies[i].Remove();
        return WordXmlTableMap.IsPlainText(textOnly,allowTextBreaks:true);
    }
}
