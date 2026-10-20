using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;

namespace ChinhTa.Core;

// Edit only a disposable candidate package; the active checkpoint and original stay untouched.
public static class DocxLineEditor
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly XNamespace V = "urn:schemas-microsoft-com:vml";
    private static readonly XNamespace O = "urn:schemas-microsoft-com:office:office";
    private static readonly XNamespace W10 = "urn:schemas-microsoft-com:office:word";
    private static readonly XNamespace WP = "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing";
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
        var shapeIds = document.Descendants().Attributes().Where(a => a.Name.LocalName is "id" or "spid")
            .Select(a => a.Value).ToHashSet(StringComparer.Ordinal);
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
                marked[0].Remove();
            }
            int number = 1025;
            while (shapeIds.Contains("_x0000_s" + number)) number++;
            string id = "_x0000_s" + number; shapeIds.Add(id);
            string Pt(double value) => value.ToString("0.###", CultureInfo.InvariantCulture) + "pt";
            // VML v:line is a native floating Word Shape, not a border or underscore text.
            var line = new XElement(V + "line",
                new XAttribute("id", id), new XAttribute(O + "spid", id), new XAttribute("alt", marker),
                new XAttribute("style", "position:absolute;z-index:251659264;mso-position-horizontal-relative:page;mso-position-vertical-relative:page"),
                new XAttribute("from", Pt(fix.Left) + "," + Pt(fix.Top)),
                new XAttribute("to", Pt(fix.Left + fix.Width) + "," + Pt(fix.Top)),
                new XAttribute(O + "allowincell", "f"), new XAttribute("strokecolor", "#000000"),
                new XAttribute("strokeweight", "0.5pt"),
                new XElement(V + "stroke", new XAttribute("endcap", "flat")),
                new XElement(W10 + "wrap", new XAttribute("type", "none"), new XAttribute("anchorx", "page"), new XAttribute("anchory", "page")),
                new XElement(W10 + "anchorlock"));
            paragraphs[index].Add(new XElement(W + "r", new XElement(W + "pict", line)));
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
}
