using System.IO.Compression;
using System.Xml.Linq;

namespace ChinhTa.Core;

public sealed record DocxLineMarker(int Id, LineRole Role, bool IsLine);

// Identity only. Package geometry never proves where Word rendered the line.
public static class DocxLineMetadata
{
    public static IReadOnlyList<DocxLineMarker> Read(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        using var xml = (zip.GetEntry("word/document.xml") ?? throw new InvalidDataException("Missing document.xml")).Open();
        return ReadXml(XDocument.Load(xml));
    }
    public static IReadOnlyList<DocxLineMarker> ReadXml(XDocument xml)
    {
        XNamespace wp = "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing";
        XNamespace a = "http://schemas.openxmlformats.org/drawingml/2006/main";
        XNamespace wpg = "http://schemas.microsoft.com/office/word/2010/wordprocessingGroup";
        XNamespace wps = "http://schemas.microsoft.com/office/word/2010/wordprocessingShape";
        XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var result = new List<DocxLineMarker>();
        var ids = new HashSet<int>();
        foreach (var property in xml.Descendants(wp + "docPr"))
        {
            string marker = (string?)property.Attribute("descr") ?? "";
            const string prefix = "ChinhTaViet.ShapeLine.";
            if (!marker.StartsWith(prefix, StringComparison.Ordinal)) continue;
            if (!Enum.TryParse<LineRole>(marker[prefix.Length..], out var role) ||
                !Enum.IsDefined(role) || marker != prefix + role ||
                !int.TryParse((string?)property.Attribute("id"), out int id) || id <= 0 || !ids.Add(id) ||
                xml.Descendants(wp + "docPr").Count(p => (string?)p.Attribute("id") == (string?)property.Attribute("id")) != 1)
                throw new InvalidDataException("Invalid or duplicate Line identity");
            var anchor = property.Parent;
            bool line = anchor?.Name == wp + "anchor" && !property.Ancestors(w + "txbxContent").Any() &&
                !anchor.Descendants(wpg + "wgp").Any() && anchor.Descendants(wps + "wsp").Count() == 1 &&
                anchor.Descendants(a + "prstGeom").Count() == 1 &&
                (string?)anchor.Descendants(a + "prstGeom").Single().Attribute("prst") == "line";
            result.Add(new(id, role, line));
        }
        if (result.GroupBy(m => m.Role).Any(g => g.Count() != 1))
            throw new InvalidDataException("Duplicate Line role");
        return result;
    }
    public static void Verify(IReadOnlyList<DocxLineMarker> markers, IEnumerable<LineRole> roles)
    {
        foreach (var role in roles)
            if (markers.Count(m => m.Role == role && m.IsLine) != 1)
                throw new InvalidDataException("Missing or invalid Line marker: " + role);
    }
}
