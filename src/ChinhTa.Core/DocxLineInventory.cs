using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;

namespace ChinhTa.Core;

public sealed record PackageLine(int Id, int AnchorStart, int Page, LineGeometry? Geometry, bool AppOwned);
public sealed record PackageLineInventory(IReadOnlyList<PackageLine> Lines, bool HasUnmappedShapes);

// DOCX IDs are package identifiers, never assumed equal to COM Shape.ID.
public static class DocxLineInventory
{
    public static PackageLineInventory Read(string path, IReadOnlyList<WordParagraphSnapshot> live)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        using var xml = (zip.GetEntry("word/document.xml") ?? throw new InvalidDataException("Missing document.xml")).Open();
        return ReadXml(XDocument.Load(xml), live);
    }
    public static PackageLineInventory ReadXml(XDocument xml, IReadOnlyList<WordParagraphSnapshot> live)
    {
        XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XNamespace wp = "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing";
        XNamespace a = "http://schemas.openxmlformats.org/drawingml/2006/main";
        XNamespace v = "urn:schemas-microsoft-com:vml";
        XNamespace wpg = "http://schemas.microsoft.com/office/word/2010/wordprocessingGroup";
        var markers = DocxLineMetadata.ReadXml(xml);
        var alternateLines = OwnedLineAlternateContent.Read(xml);
        var compatibilityBranches = alternateLines.Values.SelectMany(c=>c.Elements(OwnedLineAlternateContent.MC+"Fallback")).ToHashSet();
        bool UnknownVml(XElement e) => !e.Ancestors().Any(compatibilityBranches.Contains);
        var mapped = new Dictionary<int, WordParagraphSnapshot>();
        bool uncertain = markers.Any(m => !m.IsLine) || xml.Descendants().Any(e=>e.Name.Namespace==v && UnknownVml(e)) || xml.Descendants(a + "custGeom").Any() || xml.Descendants(wpg + "wgp").Any() ||
            xml.Descendants(OwnedLineAlternateContent.MC+"AlternateContent").Any(c=>!alternateLines.ContainsValue(c));
        WordXmlTableMap.Read(xml.ToString(), live, _ => uncertain = true,
            (start,index) => mapped.Add(index, live.Single(p => p.Start == start)));
        var paragraphs = xml.Descendants(w + "body").Single().Descendants(w + "p")
            .Where(p => !p.Ancestors(w + "txbxContent").Any()).ToArray();
        var result = new List<PackageLine>();
        foreach (var drawing in xml.Descendants(w + "drawing"))
        {
            var properties = drawing.Descendants(wp + "docPr").ToArray();
            bool hasLine = drawing.Descendants(a + "prstGeom").Any(g => (string?)g.Attribute("prst") == "line");
            if (!hasLine) continue;
            if (properties.Length != 1 || !int.TryParse((string?)properties[0].Attribute("id"), out int id) || id <= 0 ||
                xml.Descendants(wp + "docPr").Count(p => (string?)p.Attribute("id") == (string?)properties[0].Attribute("id")) != 1)
            { uncertain = true; continue; }
            var paragraph = drawing.Ancestors(w + "p").FirstOrDefault();
            int index = Array.IndexOf(paragraphs, paragraph);
            if (!mapped.TryGetValue(index, out var snapshot)) { uncertain = true; continue; }
            bool owned = markers.Any(m => m.Id == id && m.IsLine) &&
                (!drawing.Ancestors(OwnedLineAlternateContent.MC+"AlternateContent").Any() || alternateLines.ContainsKey(drawing));
            var anchor = properties[0].Parent;
            LineGeometry? geometry = null;
            double? Number(string? text) => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) && double.IsFinite(value) ? value : null;
            var horizontal = anchor?.Element(wp + "positionH");
            var vertical = anchor?.Element(wp + "positionV");
            var extent = anchor?.Element(wp + "extent");
            if (owned && anchor?.Name == wp + "anchor" &&
                (string?)anchor.Attribute("simplePos") == "0" &&
                (string?)horizontal?.Attribute("relativeFrom") == "page" &&
                (string?)vertical?.Attribute("relativeFrom") == "page" &&
                Number(horizontal.Element(wp + "posOffset")?.Value) is double left &&
                Number(vertical.Element(wp + "posOffset")?.Value) is double top &&
                Number((string?)extent?.Attribute("cx")) is double width && width > 0 &&
                Number((string?)extent?.Attribute("cy")) == 0 &&
                !drawing.Descendants(a + "xfrm").Any(x => Number((string?)x.Attribute("rot") ?? "0") != 0) &&
                (string?)properties[0].Attribute("hidden") is not ("1" or "true"))
                geometry = new(left / 12700, top / 12700, width / 12700);
            if (owned && geometry is not null)
                geometry = LinePageIntent.Read((string?)properties[0].Attribute("title")) ?? geometry;
            result.Add(new(id, snapshot.Start, snapshot.Page, geometry, owned));
        }
        return new(result, uncertain);
    }
}
