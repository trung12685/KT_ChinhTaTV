using System.IO.Compression;
using System.Xml.Linq;
using ChinhTa.Core;

// Optional private fixture input. No uploaded document or its content is stored in this repository.
public static class DocxLineFixture
{
    public static IReadOnlyList<LayoutParagraph> ReadThroughXmlMap(string path)
    {
        using var zip = ZipFile.OpenRead(path);
        using var docStream = zip.GetEntry("word/document.xml")!.Open();
        using var styleStream = zip.GetEntry("word/styles.xml")?.Open();
        // Test-only positions from the fixture; production always uses live Word ranges.
        var snapshots = Read(path).Select(p => new WordParagraphSnapshot(p.Start,
            p.Start + p.Text.Length + (p.InTable && p.CellEnd == p.Start + p.Text.Length + 1 ? 1 : 0),
            p.Text, p.Page, p.InTable, p.Centered, p.ParagraphCentered)).ToArray();
        var package = new XElement("fixture", XDocument.Load(docStream).Root, styleStream is null ? null : XDocument.Load(styleStream).Root);
        return WordXmlTableMap.Read(package.ToString(), snapshots);
    }
    public static IReadOnlyList<LayoutParagraph> Read(string path)
    {
        using var zip = ZipFile.OpenRead(path);
        using var stream = zip.GetEntry("word/document.xml")!.Open();
        var root = XDocument.Load(stream);
        XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var all = root.Descendants(w + "body").Descendants(w + "p").ToArray();
        var offsets = new Dictionary<XElement, (int Start, string Text)>();
        int offset = 0;
        foreach (var p in all)
        {
            string text = string.Concat(p.Descendants().Where(e => e.Parent?.Name == w + "r" && e.Ancestors(w + "p").FirstOrDefault() == p).Select(e => e.Name == w + "t" ? e.Value : e.Name == w + "tab" ? "\t" : e.Name == w + "br" ? "\n" : "")) + "\r";
            offsets[p] = (offset, text); offset += text.Length;
            if (p.Parent?.Name == w + "tc" && p == p.Parent.Elements(w + "p").Last()) offset++;
        }
        return all.Take(40).Select(p =>
        {
            var item = offsets[p]; var cell = p.Ancestors(w + "tc").FirstOrDefault();
            var cellParagraphs = cell?.Descendants(w + "p").Where(offsets.ContainsKey).ToArray();
            int start = cellParagraphs is { Length: > 0 } ? offsets[cellParagraphs[0]].Start : 0;
            int end = cellParagraphs is { Length: > 0 } ? offsets[cellParagraphs[^1]].Start + offsets[cellParagraphs[^1]].Text.Length + 1 : 0;
            bool aligned = (string?)p.Element(w + "pPr")?.Element(w + "jc")?.Attribute(w + "val") == "center";
            bool center = aligned && !item.Text.Contains('\t') && item.Text.TrimEnd('\r','\a','\n') == item.Text.TrimEnd('\r','\a','\n').Trim();
            var table = p.Ancestors(w + "tbl").FirstOrDefault();
            var firstTableParagraph = table?.Descendants(w + "p").FirstOrDefault();
            int tableStart = firstTableParagraph is not null ? offsets[firstTableParagraph].Start : -1;
            return new LayoutParagraph(item.Start, item.Text, 1, cell is not null, start, end, center, tableStart, aligned);
        }).ToArray();
    }
}
