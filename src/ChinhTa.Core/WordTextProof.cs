using System.Xml.Linq;

namespace ChinhTa.Core;

// Whole-main-story text eligibility, independent of header geometry and profile.
// Starts/ends must come from live Word ranges. Unproven rows fail closed.
public static class WordTextProof
{
    public static IReadOnlySet<int> Read(string wordOpenXml,IReadOnlyList<WordParagraphSnapshot> live,
        Action<WordXmlMappingException>? onMismatch=null,Action<int,OutlineParagraphInfo>? onOutline=null,Action<int,bool>? onFormatEditable=null)
    {
        int previousEnd=-1;
        foreach(var row in live)
        {
            if(row.Start<0 || row.End<=row.Start || row.Start<previousEnd)
                throw new InvalidOperationException("Vùng đoạn Word không hợp lệ hoặc chồng lấn; chưa cho phép sửa chữ.");
            previousEnd=row.End;
        }
        var editable=new HashSet<int>();
        if(onOutline is null)
            WordXmlTableMap.Read(wordOpenXml,live,onMismatch??(_=>{}),
                onTextEditable:(start,canEdit)=>{if(canEdit)editable.Add(start);},includeLayout:false,onFormatEditable:onFormatEditable);
        else
        {
            var xml=XDocument.Parse(wordOpenXml);
            XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            var body=xml.Descendants(w+"document").Single().Element(w+"body")??throw new InvalidOperationException("Missing document body");
            var ps=body.Descendants(w+"p")
                .Where(p=>!p.Ancestors(w+"txbxContent").Any()).ToArray();
            var evidence=new WordOutlineEvidence(xml);
            var matched=new Dictionary<int,int>();
            WordXmlTableMap.Read(xml,live,onMismatch??(_=>{}),(start,index)=>matched[start]=index,
                (start,canEdit)=>
                {
                    if(canEdit)editable.Add(start);
                    onOutline(start,evidence.Describe(ps[matched[start]],canEdit));
                },includeLayout:false,onFormatEditable:onFormatEditable);
        }
        return editable;
    }
    public static IReadOnlyList<Paragraph> Apply(IEnumerable<Paragraph> paragraphs,IReadOnlySet<int> editableStarts,
        IReadOnlyDictionary<int,OutlineParagraphInfo>? outline=null,IReadOnlySet<int>? formatEditableStarts=null)=>
        paragraphs.Select(p=>p with {TextEditable=p.TextEditable && editableStarts.Contains(p.Start),
            FormatEditable=formatEditableStarts?.Contains(p.Start)??p.FormatEditable,
            Outline=outline is null?p.Outline:outline.TryGetValue(p.Start,out var info)?info:null}).ToArray();
}
