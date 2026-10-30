using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ChinhTa.Core;

// User-authored text is a separate approval path. It never fabricates CanFix
// or bypasses CurrentTextPlan for an automatic rule.
public sealed class ManualTextTarget
{
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly XNamespace Pkg="http://schemas.microsoft.com/office/2006/xmlPackage";
    private readonly string source;
    private readonly int index;
    public int Start {get;}
    public string Original {get;}
    public string SourceKey {get;}
    private ManualTextTarget(string flat,int paragraphIndex,int start,string text)
    {source=flat;index=paragraphIndex;Start=start;Original=text;SourceKey=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(flat)));}
    private static InvalidOperationException Unsupported()=>new("Bản thử chỉ sửa chữ trong đoạn thuần ngoài bảng, không field/đối tượng/lịch sử sửa hoặc liên kết ngoài. Hãy sửa vùng đặc biệt trong Word rồi quét lại.");
    private static XDocument Read(string flat)
    {
        // Reuse the bounded, DTD-free, full-package validation; retain spaces.
        _=IsolatedWordPackage.FromFlat(flat);
        return XDocument.Parse(flat,LoadOptions.PreserveWhitespace);
    }
    private static XElement Body(XDocument document)=>document.Descendants(W+"document").Single().Element(W+"body")??throw Unsupported();
    private static XElement[] Paragraphs(XDocument document)=>Body(document).Descendants(W+"p").Where(p=>!p.Ancestors(W+"txbxContent").Any()).ToArray();
    private static bool ProofMarker(XElement e)=>e.Name==W+"proofErr" && !e.HasElements && e.Value.Length==0 &&
        (string?)e.Attribute(W+"type") is "spellStart" or "spellEnd" or "gramStart" or "gramEnd" &&
        e.Attributes().All(a=>a.IsNamespaceDeclaration || a.Name==W+"type");
    private static bool TextInput(string text)=>text.Length is >0 and <=16000 && !text.Any(c=>char.IsControl(c) || char.IsSurrogate(c) || char.GetUnicodeCategory(c)==System.Globalization.UnicodeCategory.Format);
    private static void Scope(XDocument doc,XElement paragraph)
    {
        if(paragraph.Parent!=Body(doc) || Body(doc).Descendants(W+"sectPr").Count()!=1 ||
            doc.Descendants().Any(e=>e.Name.Namespace==W && (e.Name.LocalName.EndsWith("Change",StringComparison.Ordinal) ||
                e.Name.LocalName is "ins" or "del" or "moveFrom" or "moveTo" or "moveFromRangeStart" or "moveFromRangeEnd" or "moveToRangeStart" or "moveToRangeEnd" ||
                e.Name.LocalName is "trackRevisions" or "documentProtection" && (string?)e.Attribute(W+"val") is not ("0" or "false" or "off"))) ||
            doc.Descendants(Pkg+"part").Any(p=>((string?)p.Attribute(Pkg+"contentType")??"").ToLowerInvariant() is var type &&
                (type.Contains("macro") || type.Contains("vba") || type.Contains("activex") || type.Contains("oleobject") || type.Contains("digital-signature"))) ||
            doc.Descendants().Any(e=>e.Name.LocalName=="Relationship" && (string?)e.Attribute("TargetMode")=="External") ||
            doc.Descendants(Pkg+"part").Any(p=>((string?)p.Attribute(Pkg+"name")??"").StartsWith("/word/embeddings/",StringComparison.OrdinalIgnoreCase) ||
                ((string?)p.Attribute(Pkg+"name")??"").StartsWith("/_xmlsignatures/",StringComparison.OrdinalIgnoreCase)) ||
            doc.Descendants(W+"altChunk").Any())throw Unsupported();
        foreach(string instruction in doc.Descendants(W+"instrText").Select(e=>e.Value).Concat(doc.Descendants(W+"fldSimple").Select(e=>(string?)e.Attribute(W+"instr")??"")))
            if(!Regex.IsMatch(instruction,@"^\s*(PAGE|NUMPAGES)(\s+\\\*\s+(MERGEFORMAT|ARABIC))?\s*$",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant))throw Unsupported();
        if(paragraph.Elements(W+"pPr").Count()>1 || paragraph.Elements().Any(e=>e.Name!=W+"pPr" && e.Name!=W+"r" && !ProofMarker(e)) ||
            paragraph.Element(W+"pPr")?.Elements().Any(e=>e.Name==W+"sectPr" || e.Name==W+"numPr" || e.Name==W+"framePr")==true)throw Unsupported();
        foreach(var run in paragraph.Elements(W+"r"))
            if(run.Elements(W+"rPr").Count()>1 || run.Elements().Any(e=>e.Name!=W+"rPr" && e.Name!=W+"t") ||
                run.Elements(W+"t").Any(e=>e.HasElements))throw Unsupported();
        if(!TextInput(string.Concat(paragraph.Elements(W+"r").Elements(W+"t").Select(e=>e.Value))))throw Unsupported();
    }
    public static ManualTextTarget Prepare(string flat,IReadOnlyList<WordParagraphSnapshot> live,int issueStart,int issueLength,string issueText)
    {
        if(issueStart<0 || issueLength<1 || issueText.Length!=issueLength)throw Unsupported();
        var doc=Read(flat);var snapshot=WordXmlSnapshot.Parse(flat);var matched=new Dictionary<int,int>();
        var editable=WordTextProof.Read(snapshot,live,onParagraphMatched:(start,i)=>matched[start]=i);
        var rows=live.Where(p=>p.Start<=issueStart && (long)issueStart+issueLength<=p.Start+p.Text.TrimEnd('\r','\a','\n').Length).ToArray();
        if(rows.Length!=1 || !editable.Contains(rows[0].Start) || !matched.TryGetValue(rows[0].Start,out int index))throw Unsupported();
        var row=rows[0];string text=row.Text.TrimEnd('\r','\a','\n');
        if((long)row.End-row.Start!=row.Text.Length || text.Substring(issueStart-row.Start,issueLength)!=issueText)throw Unsupported();
        var paragraph=Paragraphs(doc)[index];Scope(doc,paragraph);
        if(string.Concat(paragraph.Elements(W+"r").Elements(W+"t").Select(e=>e.Value))!=text)throw Unsupported();
        return new(flat,index,row.Start,text);
    }
    public void RequireCurrent(string current)
    {
        if(current==source)return;
        try{VerifyLive(source,current);}
        catch(Exception error) when(error is InvalidOperationException or System.Xml.XmlException or FormatException)
        {throw new InvalidOperationException("Tài liệu đã thay đổi sau khi mở ô sửa. Hãy đóng ô sửa và Quét lại; bản nháp chưa được ghi.",error);}
    }
    public ManualTextChange Edit(string edited)
    {
        if(!TextInput(edited))throw new InvalidOperationException("Chỉ nhập chữ trong một đoạn, tối đa16.000ký tự; không chèn ngắt đoạn, tab hoặc ký tự điều khiển trong bản thử.");
        if(edited==Original)throw new InvalidOperationException("Nội dung chưa thay đổi; chưa tạo lượt sửa hoặc Hoàn tác.");
        int prefix=0;while(prefix<Original.Length && prefix<edited.Length && Original[prefix]==edited[prefix])prefix++;
        int suffix=0;while(suffix<Original.Length-prefix && suffix<edited.Length-prefix && Original[^(suffix+1)]==edited[^(suffix+1)])suffix++;
        int removed=Original.Length-prefix-suffix;string replacement=edited.Substring(prefix,edited.Length-prefix-suffix);
        return EditRange(prefix,removed,replacement);
    }
    // Explicit selection: never slide a repeated replacement into another run.
    public ManualTextChange EditRange(int offset,int length,string replacement)
    {
        if(offset<0 || length<0 || offset>Original.Length-length)throw Unsupported();
        string edited=Original.Remove(offset,length).Insert(offset,replacement);
        if(!TextInput(edited))throw Unsupported();
        if(edited==Original)throw new InvalidOperationException("Nội dung chưa thay đổi; chưa tạo lượt sửa hoặc Hoàn tác.");
        int prefix=offset,removed=length;
        var doc=Read(source);var paragraph=Paragraphs(doc)[index];int nodeOffset=0;XElement? target=null;
        foreach(var node in paragraph.Elements(W+"r").Elements(W+"t"))
        {
            if(node.Value.Length==0)continue;
            if(prefix>=nodeOffset && prefix+removed<=nodeOffset+node.Value.Length && (removed>0 || prefix>nodeOffset || nodeOffset==0))
            {target=node;break;}
            nodeOffset+=node.Value.Length;
        }
        if(target is null)throw new InvalidOperationException("Các chỗ sửa đi qua nhiều vùng định dạng. Hãy sửa từng từ/vùng trong một lần để giữ nguyên kiểu chữ.");
        int local=prefix-nodeOffset;target.Value=target.Value.Remove(local,removed).Insert(local,replacement);
        target.SetAttributeValue(XNamespace.Xml+"space","preserve");
        return new(this,edited,doc.ToString(SaveOptions.DisableFormatting));
    }
    internal void VerifyLive(string expectedFlat,string actualFlat)
    {
        var expected=Read(expectedFlat);var actual=Read(actualFlat);
        var a=expected.Root!.Elements(Pkg+"part").ToDictionary(p=>(string)p.Attribute(Pkg+"name")!,StringComparer.Ordinal);
        var b=actual.Root!.Elements(Pkg+"part").ToDictionary(p=>(string)p.Attribute(Pkg+"name")!,StringComparer.Ordinal);
        if(!a.Keys.Order(StringComparer.Ordinal).SequenceEqual(b.Keys.Order(StringComparer.Ordinal)))throw new InvalidOperationException("Danh sách phần tài liệu đổi ngoài lượt sửa thủ công; phải phục hồi.");
        foreach(string name in a.Keys)
        {
            if((string?)a[name].Attribute(Pkg+"contentType")!=(string?)b[name].Attribute(Pkg+"contentType"))throw Unsupported();
            var left=a[name].Element(Pkg+"xmlData")?.Elements().SingleOrDefault();var right=b[name].Element(Pkg+"xmlData")?.Elements().SingleOrDefault();
            bool same=left is not null && right is not null?XNode.DeepEquals(Canonical(left,name),Canonical(right,name)):
                left is null && right is null && Convert.FromBase64String(a[name].Element(Pkg+"binaryData")!.Value).SequenceEqual(Convert.FromBase64String(b[name].Element(Pkg+"binaryData")!.Value));
            if(!same)throw new InvalidOperationException("Chữ/định dạng hoặc phần chưa chọn thay đổi sau sửa thủ công; phải phục hồi.");
        }
    }
    public void VerifyUnchangedAfterCheckpoint(string flat)=>VerifyLive(source,flat);
    private XElement Canonical(XElement root,string part)
    {
        var clone=new XElement(root);clone.Descendants(W+"lastRenderedPageBreak").Remove();
        clone.Descendants(W+"proofErr").Where(ProofMarker).Remove();
        // Proofing caches and editing-session IDs are not user text or style.
        clone.Descendants(W+"proofState").Remove();clone.Descendants(W+"rsids").Remove();
        var selected=part=="/word/document.xml"?clone.Descendants(W+"p").Where(p=>!p.Ancestors(W+"txbxContent").Any()).ElementAtOrDefault(index):null;
        foreach(var e in clone.DescendantsAndSelf())
        {
            e.Attributes().Where(a=>a.IsNamespaceDeclaration || a.Name.Namespace==W && a.Name.LocalName.StartsWith("rsid",StringComparison.Ordinal) ||
                ReferenceEquals(e,selected) && a.Name.NamespaceName=="http://schemas.microsoft.com/office/word/2010/wordml" && a.Name.LocalName=="textId" || e.Name==W+"t" && a.Name==XNamespace.Xml+"space").Remove();
            if(part=="/docProps/core.xml" && e.Name.LocalName is "modified" or "lastModifiedBy" or "revision")e.Value="#SAVE#";
            if(part=="/docProps/app.xml" && e.Name.LocalName is "TotalTime" or "Pages" or "Words" or "Characters" or "CharactersWithSpaces" or "Lines" or "Paragraphs")e.Value="#LAYOUT#";
        }
        XElement Node(XElement e)
        {
            var result=new XElement(e.Name,e.Attributes().OrderBy(a=>a.Name.ToString(),StringComparer.Ordinal).Select(a=>new XAttribute(a)));
            foreach(var node in e.Nodes())
            {
                if(node is XElement child)result.Add(Node(child));
                else if(node is XText text)
                {
                    bool structural=e.HasElements && (e.Name.Namespace==W || e.Name.Namespace==Pkg || e.Name.NamespaceName=="http://schemas.openxmlformats.org/package/2006/relationships");
                    if(!structural || !string.IsNullOrWhiteSpace(text.Value))result.Add(new XText(text.Value));
                }
                else if(node is XComment comment)result.Add(new XComment(comment.Value));
                else if(node is XProcessingInstruction instruction)result.Add(new XProcessingInstruction(instruction.Target,instruction.Data));
            }
            return result;
        }
        return Node(clone);
    }
}
public sealed class ManualTextChange
{
    private readonly ManualTextTarget target;
    private readonly string flat;
    public string Edited {get;}
    internal ManualTextChange(ManualTextTarget target,string edited,string flat){this.target=target;Edited=edited;this.flat=flat;}
    public void WriteNew(string path)=>IsolatedWordPackage.FromFlat(flat).WriteNew(path);
    public void VerifyLive(string current)=>target.VerifyLive(flat,current);
    public string ExpectedStory(string story,string prefix)
    {
        if(!story.StartsWith(prefix+target.Original,StringComparison.Ordinal))throw new InvalidOperationException("Ngữ cảnh đoạn đã thay đổi; hãy quét lại.");
        return prefix+Edited+story[(prefix.Length+target.Original.Length)..];
    }
}
public static class ManualEditTransaction
{
    public static void Apply(Action write,Action verify,Action commit,Action restore)
    {
        try{write();verify();commit();}
        catch(Exception primary)
        {
            try{restore();}catch(Exception recovery){throw new AggregateException("Sửa thủ công chưa hoàn tất và chưa xác nhận phục hồi; giữ checkpoint.",primary,recovery);}
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(primary).Throw();
        }
    }
}
