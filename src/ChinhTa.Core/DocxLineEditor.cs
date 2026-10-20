using System.IO.Compression;
using System.Xml.Linq;

namespace ChinhTa.Core;

// Edit only a disposable candidate package; the active checkpoint and original stay untouched.
public static class DocxLineEditor
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly XNamespace V = "urn:schemas-microsoft-com:vml";
    private static readonly XNamespace WP = "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing";
    private static readonly XNamespace A = "http://schemas.openxmlformats.org/drawingml/2006/main";
    private static readonly XNamespace WPS = "http://schemas.microsoft.com/office/word/2010/wordprocessingShape";
    public static void Write(string source, string candidate, IReadOnlyList<WordParagraphSnapshot> live,
        IReadOnlyList<LineTarget> targets, IReadOnlyList<Issue> issues)
    {
        if (Path.GetFullPath(source) == Path.GetFullPath(candidate) || File.Exists(candidate))
            throw new InvalidOperationException("Candidate must be a new separate file");
        XDocument document, styles;
        using (var zip = ZipFile.OpenRead(source))
        {
            using var docStream = zip.GetEntry("word/document.xml")!.Open();
            document = XDocument.Load(docStream, LoadOptions.PreserveWhitespace);
            using var styleStream = zip.GetEntry("word/styles.xml")?.Open();
            styles = styleStream is null ? new XDocument(new XElement(W + "styles")) :
                XDocument.Load(styleStream, LoadOptions.PreserveWhitespace);
        }
        var body = document.Root!.Element(W + "body")!;
        var paragraphs = body.Descendants(W + "p").Where(p => !p.Ancestors(W + "txbxContent").Any()).ToArray();
        var verified = new Dictionary<int, int>();
        var wrapper = new XElement("package", new XElement(document.Root!), new XElement(styles.Root!));
        WordXmlTableMap.Read(wrapper.ToString(), live, _ => { }, (start, index) => verified.Add(start, index));
        var drawingIds = document.Descendants(WP + "docPr").Select(p => (uint?)p.Attribute("id") ?? 0).ToHashSet();
        var roles = new HashSet<LineRole>();
        foreach (var issue in issues)
        {
            var fix = issue.LineFix ?? throw new InvalidOperationException("Only Line edits are allowed");
            if (!roles.Add(fix.Role)) throw new InvalidOperationException("Duplicate Line role");
            if (!double.IsFinite(fix.Left) || !double.IsFinite(fix.Top) || !double.IsFinite(fix.Width) || fix.Width <= 0)
                throw new InvalidOperationException("Invalid Line measurements");
            if (!verified.TryGetValue(issue.Start, out int index) ||
                !live.Any(p => p.Start == issue.Start && p.Text.StartsWith(issue.Original, StringComparison.Ordinal)))
                throw new InvalidOperationException("Reference paragraph changed; rescan required");
            var target = targets.Single(t => t.Role == fix.Role && t.Start == issue.Start);
            string marker = "ChinhTaViet.ShapeLine." + fix.Role;
            var marked = body.Descendants().Where(e => e.Name == V + "line" && (string?)e.Attribute("alt") == marker ||
                e.Name == W + "drawing" && e.Descendants(WP + "docPr").Any(p => (string?)p.Attribute("descr") == marker)).ToArray();
            if (marked.Length > 1) throw new InvalidOperationException("Multiple marked Lines; manual inspection required");
            if (fix.ShapeId is not null && marked.Length == 0)
                throw new InvalidOperationException("Only application-created Lines can be updated by this editor");
            if (marked.Length == 1)
            {
                var owner = marked[0].Ancestors(W + "p").First();
                int ownerIndex = Array.IndexOf(paragraphs, owner);
                if (!verified.Any(pair => pair.Value == ownerIndex && pair.Key >= target.RegionStart && pair.Key < target.RegionEnd))
                    throw new InvalidOperationException("Marked Line anchor outside verified region");
                var run = marked[0].Ancestors(W + "r").First();
                var pict = marked[0].Parent?.Name == W + "pict" ? marked[0].Parent : null;
                marked[0].Remove();
                if (pict is not null && !pict.Elements().Any()) pict.Remove();
                if (!run.Elements().Any(e => e.Name != W + "rPr")) run.Remove();
            }
            if (marked.Length == 1)
            {
                // Older app versions left empty pict shells when replacing an owned Line.
                // Clean only empty shells in the same verified reference paragraph.
                foreach (var empty in paragraphs[index].Descendants(W + "pict")
                    .Where(p => !p.HasElements && string.IsNullOrWhiteSpace(p.Value)).ToArray())
                {
                    var run = empty.Parent?.Name == W + "r" ? empty.Parent : null;
                    empty.Remove();
                    if (run is not null && !run.HasElements && string.IsNullOrWhiteSpace(run.Value)) run.Remove();
                }
            }
            uint number = 1;
            while (drawingIds.Contains(number)) number++;
            drawingIds.Add(number);
            paragraphs[index].Add(new XElement(W + "r", CreateDrawing(fix, marker, number)));

        }
        File.Copy(source, candidate);
        try
        {
            using var zip = ZipFile.Open(candidate, ZipArchiveMode.Update);
            zip.GetEntry("word/document.xml")!.Delete();
            using var output = zip.CreateEntry("word/document.xml", CompressionLevel.Optimal).Open();
            document.Save(output, SaveOptions.DisableFormatting);
        }
        catch { File.Delete(candidate); throw; }
    }
    // A page-relative DrawingML connector, supported by Word 2010 and later.
    // No VML coordinate interpretation and no COM AddLine/LineFormat setters.
    private static XElement CreateDrawing(ShapeLineFix fix, string marker, uint id)
    {
        long Emu(double points) => checked((long)Math.Round(points * 12700));
        long width = Emu(fix.Width);
        if (width <= 0) throw new InvalidOperationException("Line too short");
        return new XElement(W + "drawing",
            new XElement(WP + "anchor",
                new XAttribute("distT", 0), new XAttribute("distB", 0),
                new XAttribute("distL", 0), new XAttribute("distR", 0),
                new XAttribute("simplePos", 0), new XAttribute("relativeHeight", 251659264),
                new XAttribute("behindDoc", 0), new XAttribute("locked", 0),
                new XAttribute("layoutInCell", 0), new XAttribute("allowOverlap", 1),
                new XElement(WP + "simplePos", new XAttribute("x", 0), new XAttribute("y", 0)),
                new XElement(WP + "positionH", new XAttribute("relativeFrom", "page"), new XElement(WP + "posOffset", Emu(fix.Left))),
                new XElement(WP + "positionV", new XAttribute("relativeFrom", "page"), new XElement(WP + "posOffset", Emu(fix.Top))),
                new XElement(WP + "extent", new XAttribute("cx", width), new XAttribute("cy", 0)),
                new XElement(WP + "effectExtent", new XAttribute("l", 0), new XAttribute("t", 0), new XAttribute("r", 0), new XAttribute("b", 0)),
                new XElement(WP + "wrapNone"),
                new XElement(WP + "docPr", new XAttribute("id", id), new XAttribute("name", "ChinhTaViet Line " + id), new XAttribute("descr", marker)),
                new XElement(WP + "cNvGraphicFramePr"),
                new XElement(A + "graphic", new XElement(A + "graphicData", new XAttribute("uri", WPS.NamespaceName),
                    new XElement(WPS + "wsp",
                        new XElement(WPS + "cNvCnPr"),
                        new XElement(WPS + "spPr",
                            new XElement(A + "xfrm",
                                new XElement(A + "off", new XAttribute("x", 0), new XAttribute("y", 0)),
                                new XElement(A + "ext", new XAttribute("cx", width), new XAttribute("cy", 0))),
                            new XElement(A + "prstGeom", new XAttribute("prst", "line"), new XElement(A + "avLst")),
                            new XElement(A + "noFill"),
                            new XElement(A + "ln", new XAttribute("w", 6350), new XAttribute("cap", "flat"),
                                new XElement(A + "solidFill", new XElement(A + "srgbClr", new XAttribute("val", "000000"))),
                                new XElement(A + "prstDash", new XAttribute("val", "solid")),
                                new XElement(A + "headEnd", new XAttribute("type", "none")),
                                new XElement(A + "tailEnd", new XAttribute("type", "none")))),
                        new XElement(WPS + "bodyPr"))))));
    }

}
