using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ChinhTa.Core;

// Semantic evidence only. Text/format permissions still come from the current
// Word/XML proof. Never infer a signing block from bold text or a person's name alone.
internal static class SignatureContext
{
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private const RegexOptions Options=RegexOptions.IgnoreCase|RegexOptions.CultureInvariant;
    private const string Roles="GIÁM ĐỐC|PHÓ GIÁM ĐỐC|TRƯỞNG PHÒNG|PHÓ TRƯỞNG PHÒNG|CHỦ TỊCH|PHÓ CHỦ TỊCH|BỘ TRƯỞNG|THỨ TRƯỞNG|THỦ TRƯỞNG|HIỆU TRƯỞNG|PHÓ HIỆU TRƯỞNG|CHÁNH VĂN PHÒNG|PHÓ CHÁNH VĂN PHÒNG|TRƯỞNG ĐƠN VỊ|PHÓ TRƯỞNG ĐƠN VỊ";
    private static string Text(XElement p)=>WordXmlTableMap.TextOf(p).Trim(' ');
    private static bool Authority(string text)=>text.Split('\v').All(line=>Regex.IsMatch(line.Trim(),
        @"^(?:(?:(?:KT|TL|TUQ|Q)\.\s*)?(?:"+Roles+@")|TM\.\s*(?:ỦY BAN NHÂN DÂN|HỘI ĐỒNG NHÂN DÂN))$",Options));
    private static bool Name(string text)
    {
        // Ranks are retained as supplied; neither rank nor identity is verified.
        string name=Regex.Replace(text,@"^(?:Đại tướng|Thượng tướng|Trung tướng|Thiếu tướng|Đại tá|Thượng tá|Trung tá|Thiếu tá|Đại úy|Thượng úy|Trung úy|Thiếu úy) +","",Options);
        var words=name.Split(' ');
        return words.Length is >=2 and <=7 && words.All(s=>Regex.IsMatch(s,@"^\p{Lu}[\p{L}\p{M}]*$")) &&
            !Regex.IsMatch(name,@"^(?:Người ký|Họ và tên|Họ tên|Ký tên|Chữ ký|Tên người ký)$",Options) &&
            !Authority(name);
    }
    public static IReadOnlyDictionary<XElement,ComponentRole> Read(XDocument xml,IReadOnlyDictionary<XElement,(ComponentRole Role,bool Last)> recipients)
    {
        var result=new Dictionary<XElement,ComponentRole>();
        var body=xml.Descendants(W+"document").Single().Element(W+"body");
        var table=body?.Elements(W+"tbl").LastOrDefault();
        if(table is null || table.ElementsAfterSelf().Any(e=>e.Name!=W+"sectPr" &&
            (e.Name!=W+"p" || !WordXmlTableMap.IsPlainText(e) || Text(e).Length>0)) ||
            table.Descendants(W+"tbl").Any() || table.Descendants().Any(e=>e.Name==W+"sdt" || e.Name==W+"gridSpan" || e.Name==W+"vMerge" || e.Name==W+"hMerge"))return result;
        var trs=table.Elements(W+"tr").ToArray();
        if(trs.Length!=1 || table.Elements().Any(e=>e.Name!=W+"tr" && e.Name!=W+"tblPr" && e.Name!=W+"tblGrid"))return result;
        var cells=trs[0].Elements(W+"tc").ToArray();
        if(cells.Length!=2 || trs[0].Elements().Any(e=>e.Name!=W+"tc" && e.Name!=W+"trPr") ||
            cells.Any(c=>c.Elements().Any(e=>e.Name!=W+"p" && e.Name!=W+"tcPr")))return result;
        var left=cells[0].Elements(W+"p").Where(p=>Text(p).Length>0).ToArray();
        if(left.Length<2 || !recipients.TryGetValue(left[0],out var label) || label.Role!=ComponentRole.RecipientLabel ||
            !recipients.TryGetValue(left[^1],out var last) || !last.Last || left.Skip(1).Any(p=>!recipients.ContainsKey(p)))return result;
        var paragraphs=cells[1].Elements(W+"p").ToArray();
        if(paragraphs.Length>40)return result;
        var rows=paragraphs.Where(p=>Text(p).Length>0).ToArray();
        if(rows.Length is <2 or >5 || !Name(Text(rows[^1])))return result;
        var authority=rows.Take(rows.Length-1).ToArray();
        if(authority.Length is <1 or >3 || authority.Any(p=>!Authority(Text(p))))return result;
        var styles=xml.Descendants(W+"styles").FirstOrDefault();
        bool NumberedOrUnknown(XElement p)
        {
            if(p.Element(W+"pPr")?.Element(W+"numPr") is not null)return true;
            string? id=(string?)p.Element(W+"pPr")?.Element(W+"pStyle")?.Attribute(W+"val");
            id??=(string?)styles?.Elements(W+"style").FirstOrDefault(s=>(string?)s.Attribute(W+"type")=="paragraph" && (string?)s.Attribute(W+"default") is "1" or "true")?.Attribute(W+"styleId");
            var seen=new HashSet<string>();
            while(id is not null)
            {
                if(!seen.Add(id))return true;
                var found=styles?.Elements(W+"style").Where(s=>(string?)s.Attribute(W+"styleId")==id).ToArray();
                if(found is not {Length:1} || (string?)found[0].Attribute(W+"type")!="paragraph" || found[0].Element(W+"pPr")?.Element(W+"numPr") is not null)return true;
                id=(string?)found[0].Element(W+"basedOn")?.Attribute(W+"val");
            }
            return false;
        }
        if(rows.Any(p=>!WordXmlTableMap.IsPlainText(p,allowTextBreaks:true) || NumberedOrUnknown(p) ||
            p.Descendants().Any(e=>e.Name==W+"fldChar" || e.Name==W+"instrText" || e.Name==W+"ins" || e.Name==W+"del" ||
                e.Name==W+"footnoteReference" || e.Name==W+"endnoteReference")))return result;
        foreach(var p in authority)result[p]=ComponentRole.SignatureAuthority;
        result[rows[^1]]=ComponentRole.SignatureName;
        return result;
    }
}
