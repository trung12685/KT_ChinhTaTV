using System.Xml.Linq;

namespace ChinhTa.Core;

// Positions come from live Word ranges, never from guessed XML character counts.
public record WordParagraphSnapshot(int Start, int End, string Text, int Page, bool InTable, bool Centered, bool? ParagraphCentered = null);
public sealed class WordXmlMappingException(string code, int paragraph, int xmlLength, int wordLength)
    : InvalidOperationException(code)
{
    // Only fixed codes and numbers: safe for logs without document contents.
    public string DiagnosticCode { get; } = $"{code}.P{paragraph}.XML{xmlLength}.Word{wordLength}";
}
public static class WordXmlTableMap
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly HashSet<XName> UnsafeTextNames=new(){W+"sdt",W+"fldSimple",W+"hyperlink",W+"drawing",W+"pict",W+"object",W+"smartTag",W+"tab",W+"br",W+"cr"};
    private static string Clean(string text) => text.TrimEnd('\r', '\a').Replace('\u2011', '\u001e').Replace('\u00ad', '\u001f');
    public static IReadOnlyList<LayoutParagraph> Read(string wordOpenXml, IReadOnlyList<WordParagraphSnapshot> live, Action<WordXmlMappingException>? onMismatch = null, Action<int, int>? onParagraphMatched = null, Action<int,bool>? onTextEditable = null, bool includeLayout = true)
        => Read(XDocument.Parse(wordOpenXml),live,onMismatch,onParagraphMatched,onTextEditable,includeLayout);
    internal static IReadOnlyList<LayoutParagraph> Read(XDocument xml,IReadOnlyList<WordParagraphSnapshot> live,
        Action<WordXmlMappingException>? onMismatch,Action<int,int>? onParagraphMatched,Action<int,bool>? onTextEditable,bool includeLayout)
    {
        var body = xml.Descendants(W + "document").Single().Element(W + "body")
            ?? throw new InvalidOperationException("Missing document body");
        var paragraphs = body.Descendants(W + "p").Where(p => !p.Ancestors(W + "txbxContent").Any()).ToArray();
        live = AlignBodyBlanks(paragraphs, live);
        int verified = Math.Min(paragraphs.Length, live.Count);
        var result = new List<LayoutParagraph>();
        // Validate the entire prefix before allowing any cell/shape operation.
        for (int i = 0; i < verified; i++)
        {
            var p = paragraphs[i];
            if (p.Descendants().Any(e => e.Name == W + "fldChar" || e.Name == W + "instrText" ||
                e.Name == W + "ins" || e.Name == W + "del" || e.Name == W + "footnoteReference"))
            {
                var error = new WordXmlMappingException("XML-CONTENT", i + 1, 0, live[i].Text.Length);
                if (onMismatch is null) throw error;
                onMismatch(error); verified = i; break;
            }
            string text = TextOf(p);
            if (Clean(text) != Clean(live[i].Text))
            {
                var error = new WordXmlMappingException("XML-TEXT", i + 1, Clean(text).Length, Clean(live[i].Text).Length);
                if (onMismatch is null) throw error;
                onMismatch(error); verified = i; break;
            }
            onParagraphMatched?.Invoke(live[i].Start, i);
            if(onTextEditable is not null)
            {
                bool editable=IsPlainText(p);
                onTextEditable(live[i].Start,editable);
            }
        }
        if (verified == paragraphs.Length && paragraphs.Length < live.Count)
        {
            var error = new WordXmlMappingException("XML-COUNT", verified + 1, paragraphs.Length, live.Count);
            if (onMismatch is null) throw error;
            onMismatch(error);
        }
        // Text proof for the whole body does not need table geometry or styles.
        if (!includeLayout) return result;
        var index = paragraphs.Select((p, i) => (p, i)).ToDictionary(x => x.p, x => x.i);
        var styles = xml.Descendants(W + "styles").FirstOrDefault();
        for (int i = 0; i < verified; i++)
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
                if (complex || last >= verified)
                {
                    // An unrelated later table must not disable the valid header table.
                    // Unverified cells carry no usable region or center for automatic fixes.
                    result.Add(new(item.Start, item.Text, item.Page, true, 0, 0, false));
                    continue;
                }
                cellStart = live[first].Start; cellEnd = live[last].End; tableStart = live[tableFirst].Start;
                balanced = BalancedMargins(cell, table, styles);
            }
            result.Add(new(item.Start, item.Text, item.Page, cell is not null, cellStart, cellEnd,
                item.Centered && balanced, tableStart, item.ParagraphCentered));
        }
        return result;
    }
    internal static bool IsPlainText(XElement p,bool allowTabs=false)=>!p.Ancestors(W+"sdt").Any() && !p.Descendants().Any(e=>
        UnsafeTextNames.Contains(e.Name) && (e.Name!=W+"tab" || e.Parent?.Name==W+"r" && !allowTabs) ||
        e.Name.NamespaceName=="http://schemas.openxmlformats.org/officeDocument/2006/math");
    private static string TextOf(XElement p)
    {
        return string.Concat(p.Descendants().Where(e => e.Parent?.Name == W + "r" &&
                e.Ancestors(W + "p").FirstOrDefault() == p)
                .Select(e => e.Name == W + "t" ? e.Value : e.Name == W + "tab" ? "\t" :
                    e.Name == W + "br" ? (string?)e.Attribute(W + "type") switch
                    { "page" => "\f", "column" => "\u000e", _ => "\v" } : e.Name == W + "cr" ? "\v" :
                    e.Name == W + "noBreakHyphen" ? "\u2011" : e.Name == W + "softHyphen" ? "\u00ad" : ""));
    }
    private static IReadOnlyList<WordParagraphSnapshot> AlignBodyBlanks(XElement[] paragraphs, IReadOnlyList<WordParagraphSnapshot> live)
    {
        var aligned = new List<WordParagraphSnapshot>();
        bool precedingMatched = true;
        int x = 0, y = 0;
        while (y < live.Count)
        {
            if (x >= paragraphs.Length) { aligned.AddRange(live.Skip(y)); break; }
            string expected = Clean(TextOf(paragraphs[x]));
            // Word can enumerate an additional empty body paragraph after a table.
            // Skip only after an already matched body paragraph, never inside a cell,
            // at the start of the story, or past a text mismatch. Real Word CPs stay intact.
            if (precedingMatched && x > 0 && !paragraphs[x].Ancestors(W + "tc").Any() &&
                !paragraphs[x - 1].Ancestors(W + "tc").Any() && expected.Length > 0)
                while (y < live.Count && Clean(live[y].Text).Length == 0 &&
                    !live[y].Text.EndsWith('\a') && (y == 0 || live[y].Start >= live[y - 1].End)) y++;
            if (y >= live.Count) break;
            precedingMatched &= expected == Clean(live[y].Text);
            aligned.Add(live[y++]); x++;
        }
        return aligned;
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

