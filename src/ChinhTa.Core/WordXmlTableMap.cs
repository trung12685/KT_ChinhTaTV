using System.Xml.Linq;

namespace ChinhTa.Core;

// Positions come from live Word ranges, never from guessed XML character counts.
public record WordParagraphSnapshot(int Start, int End, string Text, int Page, bool InTable, bool Centered);
public static class WordXmlTableMap
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static string Clean(string text) => text.TrimEnd('\r', '\a');
    public static IReadOnlyList<LayoutParagraph> Read(string wordOpenXml, IReadOnlyList<WordParagraphSnapshot> live)
    {
        var xml = XDocument.Parse(wordOpenXml);
        var body = xml.Descendants(W + "document").Single().Element(W + "body")
            ?? throw new InvalidOperationException("Missing document body");
        var paragraphs = body.Descendants(W + "p").Where(p => !p.Ancestors(W + "txbxContent").Any()).ToArray();
        if (paragraphs.Length < live.Count) throw new InvalidOperationException("Paragraph count mismatch");
        var index = paragraphs.Select((p, i) => (p, i)).ToDictionary(x => x.p, x => x.i);
        var styles = xml.Descendants(W + "styles").FirstOrDefault();
        var result = new List<LayoutParagraph>();
        // Validate the entire prefix before allowing any cell/shape operation.
        for (int i = 0; i < live.Count; i++)
        {
            var p = paragraphs[i];
            if (p.Descendants().Any(e => e.Name == W + "fldChar" || e.Name == W + "instrText" ||
                e.Name == W + "ins" || e.Name == W + "del" || e.Name == W + "footnoteReference"))
                throw new InvalidOperationException("Unsupported paragraph content");
            string text = string.Concat(p.Descendants().Where(e => e.Ancestors(W + "p").FirstOrDefault() == p)
                .Select(e => e.Name == W + "t" ? e.Value : e.Name == W + "tab" ? "\t" :
                    e.Name == W + "br" || e.Name == W + "cr" ? "\v" :
                    e.Name == W + "noBreakHyphen" ? "\u2011" : e.Name == W + "softHyphen" ? "\u00ad" : ""));
            bool inTable = p.Ancestors(W + "tc").Any();
            if (text != Clean(live[i].Text) || inTable != live[i].InTable)
                throw new InvalidOperationException("Live Word/XML paragraph mismatch");
        }
        for (int i = 0; i < live.Count; i++)
        {
            var p = paragraphs[i]; var item = live[i];
            var cell = p.Ancestors(W + "tc").FirstOrDefault();
            int cellStart = 0, cellEnd = 0, tableStart = -1;
            bool balanced = true;
            if (cell is not null)
            {
                var table = cell.Ancestors(W + "tbl").First();
                // Nested tables/merged cells require separate geometry handling.
                bool complex = table.Ancestors(W + "tbl").Any() || table.Descendants(W + "tbl").Any() ||
                    table.Descendants(W + "vMerge").Any() || table.Descendants(W + "gridSpan").Any();
                var cellPs = cell.Descendants(W + "p").Where(index.ContainsKey).ToArray();
                int first = index[cellPs.First()], last = index[cellPs.Last()];
                int tableFirst = index[table.Descendants(W + "p").First()];
                if (complex || last >= live.Count)
                {
                    // An unrelated later table must not disable the valid header table.
                    // Unverified cells carry no usable region or center for automatic fixes.
                    result.Add(new(item.Start, item.Text, item.Page, true, 0, 0, false));
                    continue;
                }
                cellStart = live[first].Start; cellEnd = live[last].End; tableStart = live[tableFirst].Start;
                balanced = BalancedMargins(cell, table, styles);
            }
            result.Add(new(item.Start, item.Text, item.Page, item.InTable, cellStart, cellEnd,
                item.Centered && balanced, tableStart));
        }
        return result;
    }
    private static bool BalancedMargins(XElement cell, XElement table, XElement? styles)
    {
        var sources = new List<XElement?>
        {
            cell.Element(W + "tcPr")?.Element(W + "tcMar"),
            table.Element(W + "tblPr")?.Element(W + "tblCellMar")
        };
        string? styleId = (string?)table.Element(W + "tblPr")?.Element(W + "tblStyle")?.Attribute(W + "val");
        styleId ??= (string?)styles?.Elements(W + "style").FirstOrDefault(s =>
            (string?)s.Attribute(W + "type") == "table" &&
            (string?)s.Attribute(W + "default") is "1" or "true")?.Attribute(W + "styleId");
        var seen = new HashSet<string>();
        while (styleId is not null)
        {
            if (!seen.Add(styleId)) return false;
            var style = styles?.Elements(W + "style").SingleOrDefault(s => (string?)s.Attribute(W + "styleId") == styleId);
            if (style is null) return false;
            if (style.Elements(W + "tblStylePr").Any(s => s.Descendants().Any(e => e.Name == W + "tcMar" || e.Name == W + "tblCellMar"))) return false;
            sources.Add(style.Element(W + "tblPr")?.Element(W + "tblCellMar"));
            styleId = (string?)style.Element(W + "basedOn")?.Attribute(W + "val");
        }
        double? Side(string name, string logical)
        {
            foreach (var source in sources)
            {
                var side = source?.Element(W + logical) ?? source?.Element(W + name);
                if (side is null) continue;
                string? type = (string?)side.Attribute(W + "type");
                if (type == "nil") return 0;
                if (type != "dxa" || !double.TryParse((string?)side.Attribute(W + "w"),
                    System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out double value) || value < 0) return null;
                return value;
            }
            return 108; // Word default horizontal cell margin, in twips.
        }
        var left = Side("left", "start"); var right = Side("right", "end");
        return left is not null && right is not null && Math.Abs(left.Value - right.Value) <= 20;
    }
}
