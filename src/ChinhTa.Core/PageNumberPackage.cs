using System.IO.Compression;
using System.Xml.Linq;

namespace ChinhTa.Core;

// Current Flat OPC in production; saved disposable copies in the candidate editor.
// Missing referenced parts are unknown, never silently treated as empty headers.
public sealed class PageNumberPackage
{
    internal static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    internal static readonly XNamespace R="http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    internal static readonly XNamespace Rel="http://schemas.openxmlformats.org/package/2006/relationships";
    internal readonly Dictionary<string,XDocument> Parts;
    private PageNumberPackage(Dictionary<string,XDocument> parts)=>Parts=parts;
    public static PageNumberPackage FromFlat(string xml)
    {
        XNamespace pkg="http://schemas.microsoft.com/office/2006/xmlPackage";
        var doc=XDocument.Parse(xml);if(doc.Root?.Name!=pkg+"package")throw new InvalidOperationException("Không có gói Word hiện tại đầy đủ.");
        var parts=new Dictionary<string,XDocument>(StringComparer.Ordinal);
        foreach(var p in doc.Root.Elements(pkg+"part"))
        {
            var root=p.Element(pkg+"xmlData")?.Elements().SingleOrDefault();if(root is null)continue;
            string name=((string?)p.Attribute(pkg+"name")??"").TrimStart('/');
            if(name.Length==0 || !parts.TryAdd(name,new(new XElement(root))))throw new InvalidOperationException("Phần Word trùng hoặc thiếu tên.");
        }
        return new(parts);
    }
    public static PageNumberPackage FromFile(string path)
    {
        using var zip=ZipFile.OpenRead(path);var parts=new Dictionary<string,XDocument>(StringComparer.Ordinal);
        if(zip.Entries.Select(e=>e.FullName).Distinct(StringComparer.Ordinal).Count()!=zip.Entries.Count)throw new InvalidOperationException("Phần DOCX trùng tên.");
        void Load(string name)
        {
            if(parts.ContainsKey(name) || zip.GetEntry(name) is not { } entry)return;
            using var stream=entry.Open();parts.Add(name,XDocument.Load(stream,LoadOptions.PreserveWhitespace));
        }
        foreach(string name in new[]{"[Content_Types].xml","word/document.xml","word/_rels/document.xml.rels","word/styles.xml","word/settings.xml"})Load(name);
        foreach(var theme in zip.Entries.Where(e=>e.FullName.StartsWith("word/theme/",StringComparison.Ordinal) && e.FullName.EndsWith(".xml",StringComparison.Ordinal)))Load(theme.FullName);
        foreach(var relation in parts.GetValueOrDefault("word/_rels/document.xml.rels")?.Root?.Elements(Rel+"Relationship").ToArray()??[])
        {
            if((string?)relation.Attribute("Type") is not string kind || kind!=R.NamespaceName+"/header" && kind!=R.NamespaceName+"/footer" || (string?)relation.Attribute("TargetMode") is not (null or "Internal"))continue;
            if((string?)relation.Attribute("Target") is not string target || target.Contains('\\'))continue;
            var uri=new Uri(new Uri("https://docx/word/document.xml"),target);
            if(uri.Host=="docx" && uri.Query.Length==0 && uri.Fragment.Length==0)Load(Uri.UnescapeDataString(uri.AbsolutePath).TrimStart('/'));
        }
        return new(parts);
    }
    internal PageNumberPackage Clone()=>new(Parts.ToDictionary(p=>p.Key,p=>new XDocument(p.Value),StringComparer.Ordinal));
    internal XElement? Body=>Parts.GetValueOrDefault("word/document.xml")?.Root?.Element(W+"body");
    internal XElement? Section=>Body?.Elements(W+"sectPr").SingleOrDefault();
    internal (string Path,XElement Root)? Resolve(XElement reference)
    {
        string? id=(string?)reference.Attribute(R+"id");var matches=Parts.GetValueOrDefault("word/_rels/document.xml.rels")?.Root?.Elements(Rel+"Relationship")
            .Where(e=>(string?)e.Attribute("Id")==id).ToArray();
        if(id is null || matches is not {Length:1} || (string?)matches[0].Attribute("TargetMode") is not (null or "Internal"))return null;
        string? target=(string?)matches[0].Attribute("Target");if(target is null || target.Contains('\\'))return null;
        var uri=new Uri(new Uri("https://docx/word/document.xml"),target);
        if(uri.Host!="docx" || uri.Query.Length>0 || uri.Fragment.Length>0)return null;
        string path=Uri.UnescapeDataString(uri.AbsolutePath).TrimStart('/');
        if(!path.StartsWith("word/",StringComparison.Ordinal) || !Parts.TryGetValue(path,out var part) || part.Root is null)return null;
        string kind=reference.Name==W+"headerReference"?"header":"footer";
        if((string?)matches[0].Attribute("Type")!=R.NamespaceName+"/"+kind || part.Root.Name!=W+(kind=="header"?"hdr":"ftr"))return null;
        return(path,part.Root);
    }
}
