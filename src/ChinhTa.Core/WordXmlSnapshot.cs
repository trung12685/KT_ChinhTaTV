using System.Xml.Linq;

namespace ChinhTa.Core;

// One read-only snapshot of the CURRENT WordOpenXML, scoped to one CheckCore.
// No saved-file fallback, cross-edit cache, guessed Word CPs or public XML nodes.
public sealed class WordXmlSnapshot
{
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    internal XDocument Document { get; }
    internal XElement[] Paragraphs { get; }
    private readonly string[] texts;
    private readonly Lazy<WordOutlineEvidence> outline;
    private readonly Lazy<HashSet<XElement>> ownedLines;
    private readonly Lazy<IReadOnlySet<XElement>> annotationTextScope;
    internal WordOutlineEvidence Outline=>outline.Value;
    internal IReadOnlySet<XElement> OwnedLines=>ownedLines.Value;
    internal IReadOnlySet<XElement> AnnotationTextScope=>annotationTextScope.Value;
    public int ParagraphCount=>Paragraphs.Length;
    private WordXmlSnapshot(string currentXml)
    {
        Document=XDocument.Parse(currentXml);
        var body=Document.Descendants(W+"document").Single().Element(W+"body")
            ??throw new InvalidOperationException("Missing document body");
        // Includes paragraphs in cells (also nested/merged cells); textbox stories
        // stay separate exactly as in the existing proof. Geometry guards remain.
        Paragraphs=body.Descendants(W+"p").Where(p=>!p.Ancestors(W+"txbxContent").Any()).ToArray();
        texts=Paragraphs.Select(WordXmlTableMap.TextOf).ToArray();
        annotationTextScope=new(()=>WordAnnotationTextScope.Read(body));
        outline=new(()=>new WordOutlineEvidence(Document,AnnotationTextScope));
        ownedLines=new(()=>OwnedLineFormatProof.Read(Document));
    }
    public static WordXmlSnapshot Parse(string currentXml)=>new(currentXml);
    internal string TextAt(int index)=>texts[index];
    public string? ParagraphKey(int index)
    {
        if(index<0 || index>=Paragraphs.Length)return null;
        var p=Paragraphs[index];var properties=p.Element(W+"pPr");
        if(p.Ancestors().Any(e=>e.Name==W+"tc" || e.Name==W+"sdt") ||
            properties?.Elements().Any(e=>e.Name==W+"sectPr" || e.Name==W+"framePr" || e.Name==W+"numPr")==true)return null;
        return properties?.ToString(SaveOptions.DisableFormatting)??"";
    }
    public WordFormatRunPlan? FormatRuns(int index,WordParagraphSnapshot live)
    {
        if(index<0 || index>=Paragraphs.Length)return null;
        var p=Paragraphs[index];
        return OwnedLineFormatProof.IsFormatText(p,OwnedLines)?WordFormatRunPlan.Read(p,live,OwnedLines):null;
    }
}

public sealed record WordFormatRun(int Start,string Text)
{
    public int End=>checked(Start+Text.Length);
}

// These are candidate ranges for READS ONLY. Every subrange must still match
// actual Word Start/End/Text and return a known Word font before using the plan.
public sealed class WordFormatRunPlan
{
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    public int Start { get; }
    public string Text { get; }
    public IReadOnlyList<WordFormatRun> Runs { get; }
    private WordFormatRunPlan(int start,string text,List<WordFormatRun> runs)
    {Start=start;Text=text;Runs=runs.AsReadOnly();}
    internal static WordFormatRunPlan? Read(XElement p,WordParagraphSnapshot live,IReadOnlySet<XElement> owned)
    {
        string text=live.Text.TrimEnd('\r','\a','\n');
        if(live.Start<0 || (long)live.End-live.Start!=live.Text.Length || text.Length==0 ||
            text.Any(c=>char.IsSurrogate(c) || char.IsControl(c)))return null;
        var runs=new List<WordFormatRun>();int offset=0;
        foreach(var node in p.Elements())
        {
            if(node.Name==W+"pPr")continue;
            if(node.Name!=W+"r")return null;
            // Style inheritance is resolved by Word, never guessed from rPr.
            if(node.Elements().Any(e=>e.Name!=W+"rPr" && e.Name!=W+"t" && !owned.Contains(e)))return null;
            string part=string.Concat(node.Elements(W+"t").Select(t=>t.Value));
            if(part.Length==0)continue;
            if(offset>text.Length-part.Length || !text.AsSpan(offset,part.Length).SequenceEqual(part))return null;
            if((long)live.Start+offset+part.Length>int.MaxValue)return null;
            runs.Add(new(live.Start+offset,part));offset+=part.Length;
            if(runs.Count>64)return null; // Fragmented XML should use the existing fallback.
        }
        return offset==text.Length && runs.Count>0?new(live.Start,text,runs):null;
    }
}
