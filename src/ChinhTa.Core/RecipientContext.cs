using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ChinhTa.Core;

internal static class RecipientContext
{
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    public static bool Label(string text)=>Regex.IsMatch(text.Trim(),@"^Nơi\s+nhận\s*:?$",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
    public static bool Last(string text)=>Regex.IsMatch(text.Trim(),@"^(?:[-–—]\s*)?Lưu\s*:",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
    private static string Text(XElement p)=>string.Concat(p.Descendants(W+"t").Select(t=>t.Value)).Trim();
    public static IReadOnlyDictionary<XElement,(ComponentRole Role,bool Last)> Read(XDocument xml)
    {
        var result=new Dictionary<XElement,(ComponentRole,bool)>();
        var body=xml.Descendants(W+"document").Single().Element(W+"body");
        if(body is null)return result;
        bool Numbered(XElement p)
        {
            if(p.Element(W+"pPr")?.Element(W+"numPr") is not null)return true;
            string? id=(string?)p.Element(W+"pPr")?.Element(W+"pStyle")?.Attribute(W+"val");
            var styles=xml.Descendants(W+"styles").FirstOrDefault();
            id??=(string?)styles?.Elements(W+"style").FirstOrDefault(s=>(string?)s.Attribute(W+"type")=="paragraph" && (string?)s.Attribute(W+"default") is "1" or "true")?.Attribute(W+"styleId");
            var seen=new HashSet<string>();
            while(id is not null)
            {
                if(!seen.Add(id))return true;
                var found=styles?.Elements(W+"style").Where(s=>(string?)s.Attribute(W+"styleId")==id).ToArray();
                if(found is not {Length:1} || found[0].Element(W+"pPr")?.Element(W+"numPr") is not null)return true;
                id=(string?)found[0].Element(W+"basedOn")?.Attribute(W+"val");
            }
            return false;
        }
        foreach(var label in body.Descendants(W+"p").Where(p=>Label(Text(p))))
        {
            var container=label.Parent;
            if(container?.Name!=W+"tc" && container?.Name!=W+"body" || label.Ancestors(W+"sdt").Any() ||
                label.Ancestors(W+"tbl").Count()>1 || !WordXmlTableMap.IsPlainText(label))continue;
            var rows=label.ElementsAfterSelf(W+"p").Take(64).Where(p=>Text(p).Length>0).ToArray();
            int last=Array.FindIndex(rows,p=>Last(Text(p)));
            // Require the closing Lưu anchor, never blanket-exempt arbitrary bullets in a table.
            if(last<0 || last>24 || rows.Take(last+1).Any(p=>Numbered(p) ||p.Parent!=container || p.Descendants(W+"p").Any() ||
                p.Ancestors(W+"sdt").Any() || Label(Text(p)) || Regex.IsMatch(Text(p),@"^(?:[0-9]+[.)]|[IVXLCDM]+[.)])\s")))continue;
            result[label]=(ComponentRole.RecipientLabel,false);
            foreach(var row in rows.Take(last+1))result[row]=(ComponentRole.RecipientItem,row==rows[last]);
        }
        return result;
    }
}
