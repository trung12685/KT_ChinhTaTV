namespace ChinhTa.Core;

// Whole-main-story text eligibility, independent of header geometry and profile.
// Starts/ends must come from live Word ranges. Unproven rows fail closed.
public static class WordTextProof
{
    public static IReadOnlySet<int> Read(string wordOpenXml,IReadOnlyList<WordParagraphSnapshot> live,
        Action<WordXmlMappingException>? onMismatch=null)
    {
        int previousEnd=-1;
        foreach(var row in live)
        {
            if(row.Start<0 || row.End<=row.Start || row.Start<previousEnd)
                throw new InvalidOperationException("Vùng đoạn Word không hợp lệ hoặc chồng lấn; chưa cho phép sửa chữ.");
            previousEnd=row.End;
        }
        var editable=new HashSet<int>();
        WordXmlTableMap.Read(wordOpenXml,live,onMismatch??(_=>{}),
            onTextEditable:(start,canEdit)=>{if(canEdit)editable.Add(start);},includeLayout:false);
        return editable;
    }
    public static IReadOnlyList<Paragraph> Apply(IEnumerable<Paragraph> paragraphs,IReadOnlySet<int> editableStarts)=>
        paragraphs.Select(p=>p with {TextEditable=p.TextEditable && editableStarts.Contains(p.Start)}).ToArray();
}
