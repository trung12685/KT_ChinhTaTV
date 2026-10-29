using System.Xml.Linq;

namespace ChinhTa.Core;

// Matching visible text proves native correspondence, not preservation of an
// annotated text range when Range.Text replaces its contents. Source ordinals
// below identify affected paragraphs only; they never become Word CPs.
internal static class WordAnnotationTextScope
{
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly IReadOnlyDictionary<XName,(string Family,bool Start)> Markers=
        new Dictionary<XName,(string,bool)>
        {
            [W+"bookmarkStart"]=("bookmark",true),[W+"bookmarkEnd"]=("bookmark",false),
            [W+"commentRangeStart"]=("comment",true),[W+"commentRangeEnd"]=("comment",false),
            [W+"permStart"]=("permission",true),[W+"permEnd"]=("permission",false)
        };
    private static readonly HashSet<XName> ContentNames=new()
    {
        W+"tab",W+"br",W+"cr",W+"noBreakHyphen",W+"softHyphen",W+"sym",W+"drawing",W+"pict",W+"object",
        W+"footnoteReference",W+"endnoteReference",W+"commentReference"
    };
    private static string ComparisonId(string raw)
    {
        // Annotation IDs have the standard decimal-number value space. Keep
        // raw XML unchanged while comparing legal lexical aliases without a
        // bounded numeric conversion. Other malformed values remain distinct.
        string value=raw.Trim(' ','\t','\r','\n');
        int digits=value.Length>0 && value[0] is '+' or '-'?1:0;
        if(digits==value.Length || !value.Skip(digits).All(char.IsAsciiDigit))return "raw:"+raw;
        string number=value[digits..].TrimStart('0');
        return number.Length==0?"number:0":"number:"+(value[0]=='-'?"-":"")+number;
    }
    internal static IReadOnlySet<XElement> Read(XElement body)
    {
        var nodes=body.Descendants().Where(e=>!e.Ancestors(W+"txbxContent").Any()).ToArray();
        var positions=nodes.Select((node,index)=>(node,index)).ToDictionary(item=>item.node,item=>item.index);
        var paragraphs=nodes.Where(e=>e.Name==W+"p").ToArray();
        var paragraphIndexes=paragraphs.Select((node,index)=>(node,index)).ToDictionary(item=>item.node,item=>item.index);
        var paragraphPositions=paragraphs.Select(p=>positions[p]).ToArray();
        var contentBefore=new int[nodes.Length+1];
        for(int i=0;i<nodes.Length;i++)
            contentBefore[i+1]=contentBefore[i]+(nodes[i].Name==W+"p" ||
                nodes[i].Name==W+"t" && nodes[i].Value.Length>0 || ContentNames.Contains(nodes[i].Name)?1:0);
        var ranges=nodes.Where(e=>Markers.ContainsKey(e.Name) && !string.IsNullOrEmpty((string?)e.Attribute(W+"id")))
            .GroupBy(e=>(Markers[e.Name].Family,Id:ComparisonId((string)e.Attribute(W+"id")!)));
        var protectedParagraphs=new HashSet<XElement>();
        foreach(var range in ranges)
        {
            var starts=range.Where(e=>Markers[e.Name].Start).ToArray();
            var ends=range.Where(e=>!Markers[e.Name].Start).ToArray();
            // Lone orphan metadata retains its existing policy. When both
            // sides exist but pairing/order is ambiguous, protect the whole
            // nonempty envelope rather than invent a supported pairing.
            if(starts.Length==0 || ends.Length==0)continue;
            var ordered=range.OrderBy(e=>positions[e]).ToArray();
            int start=positions[ordered[0]],end=positions[ordered[^1]];
            var firstHost=ordered[0].Ancestors(W+"p").FirstOrDefault();
            var lastHost=ordered[^1].Ancestors(W+"p").FirstOrDefault();
            bool nonempty=firstHost!=lastHost || contentBefore[end]>contentBefore[start+1];
            if(!nonempty)continue; // Adjacent point metadata, including _GoBack, remains usable.
            int first=firstHost is not null?paragraphIndexes[firstHost]:~Array.BinarySearch(paragraphPositions,start);
            int last=lastHost is not null?paragraphIndexes[lastHost]:(~Array.BinarySearch(paragraphPositions,end))-1;
            for(int i=first;i<=last;i++)protectedParagraphs.Add(paragraphs[i]);
        }
        return protectedParagraphs;
    }
}
