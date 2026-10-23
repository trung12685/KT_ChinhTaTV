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
                !d.Descendants().Any(e=>e.Name.Namespace==W || e.Name==A+"t" || e.Name==A+"blip" || e.Name==WPS+"txbx" || e.Name==WPS+"linkedTxbx") &&
                d.Parent?.Name==W+"r" && d.Parent.Parent?.Name==W+"p" &&
                d.Parent.Elements().Count(e=>e.Name==W+"drawing")==1 &&
                d.Parent.Elements().All(e=>e.Name==W+"rPr" || e==d);
        }).ToHashSet();
    }
    internal static bool IsFormatText(XElement paragraph,IReadOnlySet<XElement> ownedLines)
    {
        var drawings=paragraph.Descendants(W+"drawing").ToArray();
        if(drawings.Length==0)return WordXmlTableMap.IsPlainText(paragraph,allowTextBreaks:true);
        if(drawings.Any(d=>!ownedLines.Contains(d)))return false;
        var textOnly=new XElement(paragraph);
        // Proof uses a detached copy; the actual object/run is never changed.
        foreach(var d in textOnly.Descendants(W+"drawing").ToArray())d.Parent!.Remove();
        return WordXmlTableMap.IsPlainText(textOnly,allowTextBreaks:true);
    }
}
