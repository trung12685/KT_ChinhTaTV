using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ChinhTa.Core;

// Full Document.WordOpenXML, not a Range export or the PAGE-specific XML view.
// Binary payloads are decoded without conversion; XML payloads retain whitespace.
public sealed class IsolatedWordPackage
{
    private static readonly XNamespace Pkg="http://schemas.microsoft.com/office/2006/xmlPackage";
    private static readonly XNamespace Types="http://schemas.openxmlformats.org/package/2006/content-types";
    private static readonly XNamespace Rel="http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private const int MaximumParts=4096;
    private const int MaximumXmlCharacters=64*1024*1024;
    private const long MaximumBytes=128*1024*1024;
    private sealed record Part(string Name,string Type,byte[] Bytes,bool Xml);
    private readonly Part[] parts;
    public IReadOnlyList<string> PartNames=>Array.AsReadOnly(parts.Select(p=>p.Name).ToArray());
    private IsolatedWordPackage(Part[] parts)=>this.parts=parts;

    private static XDocument Parse(string xml)
    {
        using var reader=XmlReader.Create(new StringReader(xml.TrimStart('\uFEFF')),new XmlReaderSettings
        {DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=MaximumXmlCharacters});
        return XDocument.Load(reader,LoadOptions.PreserveWhitespace);
    }
    private static bool SafeName(string name)=>name.Length is >0 and <=512 && !name.StartsWith('/') &&
        !name.Contains('\\') && !name.Contains('%') && !name.Contains(':') && !name.Contains('?') && !name.Contains('#') &&
        !name.Any(c=>char.IsControl(c) || char.IsWhiteSpace(c)) && name.Split('/').All(s=>s is not ("" or "." or ".."));
    private static InvalidOperationException Unknown()=>new("Gói Word cô lập chưa đủ bằng chứng toàn bộ phần/quan hệ; giữ kiểm tra thủ công.");

    public static IsolatedWordPackage FromFlat(string xml)
    {
        if(string.IsNullOrWhiteSpace(xml) || xml.Length>MaximumXmlCharacters)throw Unknown();
        var root=Parse(xml).Root;
        if(root?.Name!=Pkg+"package" || root.Elements().Any(e=>e.Name!=Pkg+"part"))throw Unknown();
        var result=new List<Part>();var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);long total=0;
        foreach(var item in root.Elements(Pkg+"part"))
        {
            string raw=(string?)item.Attribute(Pkg+"name")??"",type=(string?)item.Attribute(Pkg+"contentType")??"";
            string name=raw.StartsWith('/')?raw[1..]:"";
            if(!SafeName(name) || name.Equals("[Content_Types].xml",StringComparison.OrdinalIgnoreCase) || !names.Add(name) ||
                result.Count>=MaximumParts || type.Length==0 || type.Any(char.IsControl))throw Unknown();
            var payload=item.Elements().ToArray();
            if(payload.Length!=1 || payload[0].Name!=Pkg+"xmlData" && payload[0].Name!=Pkg+"binaryData")throw Unknown();
            bool isXml=payload[0].Name==Pkg+"xmlData";byte[] bytes;
            if(isXml)
            {
                var roots=payload[0].Elements().ToArray();
                if(roots.Length!=1 || payload[0].Nodes().OfType<XText>().Any(t=>!string.IsNullOrWhiteSpace(t.Value)))throw Unknown();
                // Namespace prefixes can appear in attribute VALUES (mc:Ignorable,
                // QName metadata), so preserve inherited declarations as well.
                var content=new XElement(roots[0]);
                var namespaces=new Dictionary<XName,string>();
                foreach(var ancestor in roots[0].AncestorsAndSelf().Reverse())
                    foreach(var attribute in ancestor.Attributes().Where(a=>a.IsNamespaceDeclaration))namespaces[attribute.Name]=attribute.Value;
                foreach(var ns in namespaces)if(content.Attribute(ns.Key) is null)content.SetAttributeValue(ns.Key,ns.Value);
                var document=new XDocument(payload[0].Nodes().Select(node=>node is XElement?content:node));
                using var memory=new MemoryStream();document.Save(memory,SaveOptions.DisableFormatting);bytes=memory.ToArray();
            }
            else
            {
                if(payload[0].HasElements)throw Unknown();
                try{bytes=Convert.FromBase64String(payload[0].Value);}catch(FormatException){throw Unknown();}
            }
            total+=bytes.LongLength;if(total>MaximumBytes)throw Unknown();
            result.Add(new(name,type,bytes,isXml));
        }
        var package=new IsolatedWordPackage(result.ToArray());package.ValidateRelationships();return package;
    }
    private XDocument Xml(Part part)
    {
        if(!part.Xml)throw Unknown();
        return Parse(Encoding.UTF8.GetString(part.Bytes));
    }
    private void ValidateRelationships()
    {
        var map=parts.ToDictionary(p=>p.Name,StringComparer.Ordinal);
        if(!map.TryGetValue("word/document.xml",out var main) || main.Type!="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml" ||
            Xml(main).Root?.Name!=W+"document" || Xml(main).Root!.Elements(W+"body").Count()!=1 || !map.TryGetValue("_rels/.rels",out var root))throw Unknown();
        bool office=false;
        foreach(var part in parts.Where(p=>p.Name.EndsWith(".rels",StringComparison.Ordinal)))
        {
            if(part.Type!="application/vnd.openxmlformats-package.relationships+xml")throw Unknown();
            string source;
            if(part.Name=="_rels/.rels")source="";
            else
            {
                int marker=part.Name.LastIndexOf("/_rels/",StringComparison.Ordinal);
                if(marker<0 && !part.Name.StartsWith("_rels/",StringComparison.Ordinal))throw Unknown();
                source=marker<0?part.Name[6..^5]:part.Name[..(marker+1)]+part.Name[(marker+7)..^5];
                if(!map.ContainsKey(source))throw Unknown();
            }
            var relations=Xml(part).Root;if(relations?.Name!=Rel+"Relationships")throw Unknown();
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var relation in relations.Elements())
            {
                string id=(string?)relation.Attribute("Id")??"",target=(string?)relation.Attribute("Target")??"",type=(string?)relation.Attribute("Type")??"";
                if(relation.Name!=Rel+"Relationship" || id.Length==0 || !ids.Add(id) || target.Length==0 || type.Length==0)throw Unknown();
                string? mode=(string?)relation.Attribute("TargetMode");
                if(mode=="External")continue; // Preserved faithfully, but never opened for calibration.
                if(mode is not (null or "Internal") || target.Contains(':') || target.Contains('\\') || target.Contains('%') || target.Contains('?') || target.Contains('#') || target.Any(c=>char.IsControl(c) || char.IsWhiteSpace(c)))throw Unknown();
                int depth=target.StartsWith('/')?0:source.Count(c=>c=='/');
                foreach(string segment in target.Split('/'))
                {
                    if(segment is "" or ".")continue;
                    if(segment==".."){if(depth==0)throw Unknown();depth--;}
                    else depth++;
                }
                var baseUri=new Uri("https://isolated.invalid/"+source);
                if(!Uri.TryCreate(baseUri,target,out var resolved) || resolved.Host!="isolated.invalid" || resolved.Scheme!="https" ||
                    !map.ContainsKey(resolved.AbsolutePath.TrimStart('/')))throw Unknown();
                if(ReferenceEquals(part,root) && type=="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument")
                {
                    if(office || resolved.AbsolutePath!="/word/document.xml")throw Unknown();
                    office=true;
                }
            }
        }
        if(!office)throw Unknown();
        XNamespace officeRel="http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        foreach(var part in parts.Where(p=>p.Xml && !p.Name.EndsWith(".rels",StringComparison.Ordinal)))
        {
            var references=Xml(part).Descendants().Attributes().Where(a=>a.Name.Namespace==officeRel).ToArray();
            if(references.Length==0)continue;
            int slash=part.Name.LastIndexOf('/');
            string relPath=(slash<0?"":part.Name[..(slash+1)])+"_rels/"+part.Name[(slash+1)..]+".rels";
            if(!map.TryGetValue(relPath,out var relationships))throw Unknown();
            var ids=Xml(relationships).Descendants(Rel+"Relationship").Select(r=>(string?)r.Attribute("Id")).ToHashSet(StringComparer.Ordinal);
            if(references.Any(a=>a.Value.Length==0 || !ids.Contains(a.Value)))throw Unknown();
        }
    }

    // A faithful container can preserve unsupported/active content. That does not
    // authorize opening it in a separate Word process with live network effects.
    public void RequirePassiveCalibration()
    {
        foreach(var part in parts)
        {
            string type=part.Type.ToLowerInvariant();
            if(type.Contains("macro") || type.Contains("vba") || type.Contains("activex") || type.Contains("oleobject") || type.Contains("digital-signature") ||
                part.Name.StartsWith("_xmlsignatures/",StringComparison.OrdinalIgnoreCase) || part.Name.StartsWith("word/embeddings/",StringComparison.OrdinalIgnoreCase))throw Unknown();
            if(!part.Xml)continue;
            var xml=Xml(part);
            if(part.Name.EndsWith(".rels",StringComparison.Ordinal) && xml.Descendants(Rel+"Relationship").Any(r=>(string?)r.Attribute("TargetMode")=="External"))throw Unknown();
            if(xml.Descendants(W+"altChunk").Any())throw Unknown();
            // Only page counters have a narrowly understood automatic update in
            // this diagnostic. Split instructions and nested fields stay manual.
            var instructions=xml.Descendants(W+"instrText").Select(e=>e.Value)
                .Concat(xml.Descendants(W+"fldSimple").Select(e=>(string?)e.Attribute(W+"instr")??""));
            if(instructions.Any(s=>s.Trim() is not ("PAGE" or "NUMPAGES")))throw Unknown();
        }
    }

    public void WriteNew(string destination)
    {
        string full=Path.GetFullPath(destination),directory=Path.GetDirectoryName(full)!;
        string temporary=Path.Combine(directory,".isolated-"+Guid.NewGuid().ToString("N")+".tmp");
        try
        {
            using(var output=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            using(var zip=new ZipArchive(output,ZipArchiveMode.Create))
            {
                var types=new XElement(Types+"Types",parts.Select(p=>new XElement(Types+"Override",new XAttribute("PartName","/"+p.Name),new XAttribute("ContentType",p.Type))));
                using(var stream=zip.CreateEntry("[Content_Types].xml").Open())new XDocument(types).Save(stream,SaveOptions.DisableFormatting);
                foreach(var part in parts){using var stream=zip.CreateEntry(part.Name).Open();stream.Write(part.Bytes);}
            }
            VerifyFile(temporary);File.Move(temporary,full); // Never overwrite an input or existing candidate.
        }
        finally{if(File.Exists(temporary))File.Delete(temporary);}
    }
    public void VerifyFile(string path)
    {
        try{VerifyFileCore(path);}
        catch(InvalidOperationException error) when(error is not SpacingDiagnosticException)
        {throw new SpacingDiagnosticException(SpacingDiagnosticFailureCode.IntegrityFailed,error);}
        catch(InvalidDataException error){throw new SpacingDiagnosticException(SpacingDiagnosticFailureCode.IntegrityFailed,error);}
    }
    private void VerifyFileCore(string path)
    {
        using var zip=ZipFile.OpenRead(path);
        if(zip.Entries.Count!=parts.Length+1 || zip.Entries.Select(e=>e.FullName).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=zip.Entries.Count)throw Unknown();
        foreach(var part in parts)
        {
            var entry=zip.GetEntry(part.Name);if(entry is null || entry.Length!=part.Bytes.Length)throw Unknown();
            using var input=entry.Open();using var bytes=new MemoryStream();input.CopyTo(bytes);
            if(!bytes.ToArray().AsSpan().SequenceEqual(part.Bytes))throw Unknown();
        }
        var content=zip.GetEntry("[Content_Types].xml");if(content is null || content.Length>MaximumXmlCharacters)throw Unknown();
        using var text=new StreamReader(content.Open());var root=Parse(text.ReadToEnd()).Root;
        if(root?.Name!=Types+"Types" || root.Elements().Count()!=parts.Length)throw Unknown();
        var overrides=root.Elements(Types+"Override").ToArray();
        if(overrides.Length!=parts.Length || parts.Any(p=>overrides.Count(e=>(string?)e.Attribute("PartName")=="/"+p.Name && (string?)e.Attribute("ContentType")==p.Type)!=1))throw Unknown();
    }
}
