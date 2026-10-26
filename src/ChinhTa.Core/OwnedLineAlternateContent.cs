using System.Globalization;
using System.Xml.Linq;

namespace ChinhTa.Core;

// Word's DrawingML/VML pair describes one object, not two independent Lines.
// This proves identity and a text-free compatibility branch only. The active
// DrawingML coordinates still require verification against the rendered XPS.
internal static class OwnedLineAlternateContent
{
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly XNamespace WP="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing";
    private static readonly XNamespace A="http://schemas.openxmlformats.org/drawingml/2006/main";
    private static readonly XNamespace WPS="http://schemas.microsoft.com/office/word/2010/wordprocessingShape";
    internal static readonly XNamespace MC="http://schemas.openxmlformats.org/markup-compatibility/2006";
    private static readonly XNamespace V="urn:schemas-microsoft-com:vml";
    private static readonly XNamespace W10="urn:schemas-microsoft-com:office:word";

    internal static Dictionary<XElement,XElement> Read(XDocument xml)
    {
        var markers=DocxLineMetadata.ReadXml(xml).Where(m=>m.IsLine).ToDictionary(m=>m.Id);
        var result=new Dictionary<XElement,XElement>();
        foreach(var wrapper in xml.Descendants(MC+"AlternateContent"))
        {
            var choices=wrapper.Elements(MC+"Choice").ToArray();
            var fallbacks=wrapper.Elements(MC+"Fallback").ToArray();
            if(wrapper.Parent?.Name!=W+"r" || wrapper.Parent.Parent?.Name!=W+"p" ||
                choices.Length!=1 || fallbacks.Length!=1 || wrapper.Elements().Count()!=2)continue;
            var choice=choices[0];var fallback=fallbacks[0];
            string[] requires=((string?)choice.Attribute("Requires")??"").Split(' ',StringSplitOptions.RemoveEmptyEntries);
            if(requires.Length!=1 || choice.GetNamespaceOfPrefix(requires[0])!=WPS ||
                wrapper.Nodes().OfType<XText>().Concat(choice.Nodes().OfType<XText>()).Any(t=>!string.IsNullOrWhiteSpace(t.Value)) ||
                choice.Elements().Count()!=1 || choice.Element(W+"drawing") is not XElement drawing ||
                drawing.Elements().Count()!=1 || drawing.Element(WP+"anchor") is not XElement anchor)continue;
            var properties=drawing.Descendants(WP+"docPr").ToArray();
            if(properties.Length!=1 || properties[0].Parent!=anchor ||
                !int.TryParse((string?)properties[0].Attribute("id"),out int id) || !markers.ContainsKey(id) ||
                drawing.Descendants(WPS+"wsp").Count()!=1 || drawing.Descendants(A+"prstGeom").Count()!=1 ||
                drawing.Descendants().Any(e=>e.Name.Namespace==W || e.Name==A+"blip" || e.Name==A+"t" ||
                    e.Name==A+"custGeom" || e.Name==WPS+"txbx" || e.Name==WPS+"linkedTxbx" ||
                    e.Name.NamespaceName=="http://schemas.openxmlformats.org/officeDocument/2006/math"))continue;
            if(fallback.Elements().Count()!=1 || fallback.Element(W+"pict") is not XElement pict ||
                pict.Elements().Count()!=1 || pict.Element(V+"line") is not XElement line ||
                !MatchesLabel((string?)line.Attribute("alt"),properties[0]) ||
                string.IsNullOrEmpty((string?)properties[0].Attribute("name")) ||
                (string?)line.Attribute("id")!=(string?)properties[0].Attribute("name") ||
                line.Elements().Any(e=>e.Name!=W10+"wrap" || e.HasElements) ||
                line.Elements(W10+"wrap").Count()>1 ||
                fallback.DescendantNodes().OfType<XText>().Any(t=>!string.IsNullOrWhiteSpace(t.Value)))continue;
            // Reject a merely marked but unrelated fallback. Compare its page
            // endpoints with the encoded DrawingML, never with an intent title.
            var h=anchor.Element(WP+"positionH");var v=anchor.Element(WP+"positionV");var extent=anchor.Element(WP+"extent");
            if((string?)anchor.Attribute("simplePos")!="0" ||
                (string?)h?.Attribute("relativeFrom")!="page" || (string?)v?.Attribute("relativeFrom")!="page" ||
                Number(h.Element(WP+"posOffset")?.Value) is not double x ||
                Number(v.Element(WP+"posOffset")?.Value) is not double y ||
                Number((string?)extent?.Attribute("cx")) is not double width || width<=0 ||
                Number((string?)extent?.Attribute("cy"))!=0 ||
                drawing.Descendants(A+"xfrm").Any(t=>Number((string?)t.Attribute("rot")??"0")!=0 ||
                    (string?)t.Attribute("flipH") is "1" or "true" || (string?)t.Attribute("flipV") is "1" or "true") ||
                !Point((string?)line.Attribute("from"),out double left,out double top) ||
                !Point((string?)line.Attribute("to"),out double right,out double bottom) ||
                Math.Abs(left-x/12700)>.1 || Math.Abs(top-y/12700)>.1 ||
                Math.Abs(right-(x+width)/12700)>.1 || Math.Abs(bottom-top)>.1)continue;
            result.Add(drawing,wrapper);
        }
        return result;
    }
    private static bool MatchesLabel(string? label,XElement property)
    {
        string? description=(string?)property.Attribute("descr");
        if(label==description)return true;
        // On save, Word combines accessibility Title and Description in VML
        // alt when the measured-correction intent occupies DrawingML's title.
        // Compare the entire observed form against this very object's valid
        // intent and marker; a substring or another object's title is no proof.
        string? title=(string?)property.Attribute("title");
        return LinePageIntent.Read(title) is not null &&
            string.Equals(label,$"Title: {title} - Description: {description}",StringComparison.Ordinal);
    }
    private static double? Number(string? value)=>double.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out double d) && double.IsFinite(d)?d:null;
    private static bool Point(string? value,out double x,out double y)
    {
        x=y=0;var pair=(value??"").Split(',');
        if(pair.Length!=2 || pair.Any(s=>!s.Trim().EndsWith("pt",StringComparison.Ordinal)))return false;
        if(Number(pair[0].Trim()[..^2]) is not double a || Number(pair[1].Trim()[..^2]) is not double b)return false;
        x=a;y=b;return true;
    }
}
