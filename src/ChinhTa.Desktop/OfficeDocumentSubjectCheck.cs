using System.IO;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
internal static class OfficeDocumentSubjectCheck
{
    internal static string Create(string folder,string? namedType=null,bool bad=false,float size=13)
    {
        XNamespace p="http://schemas.microsoft.com/office/2006/xmlPackage",w="http://schemas.openxmlformats.org/wordprocessingml/2006/main",r="http://schemas.openxmlformats.org/package/2006/relationships";
        XElement Part(string name,string type,XElement root)=>new(p+"part",new XAttribute(p+"name","/"+name),new XAttribute(p+"contentType",type),new XElement(p+"xmlData",root));
        XElement Row(string text,float point=13,bool bold=false,bool italic=false,bool wrongFont=false,int after=0)=>new(w+"p",
            new XElement(w+"pPr",new XElement(w+"spacing",new XAttribute(w+"after",after)),new XElement(w+"jc",new XAttribute(w+"val","center"))),
            new XElement(w+"r",new XElement(w+"rPr",new XElement(w+"rFonts",new XAttribute(w+"ascii",wrongFont?"Arial":"Times New Roman"),new XAttribute(w+"hAnsi",wrongFont?"Arial":"Times New Roman")),
                new XElement(w+"b",new XAttribute(w+"val",bold?1:0)),new XElement(w+"i",new XAttribute(w+"val",italic?1:0)),new XElement(w+"sz",new XAttribute(w+"val",(int)(point*2)))),new XElement(w+"t",text)));
        var left=new List<XElement>{Row("CƠ QUAN MẪU",13,true),Row(namedType is null?"Số: 01/ABC-VP":"Số: 01/"+(DocumentTypeCatalog.FindHeading(namedType)?.Abbreviation??"BC")+"-ABC",after:namedType is null?120:0)};
        if(namedType is null)left.Add(Row("V/v tổng hợp kết quả công tác",bad?14:size,bad,bad,bad));
        XElement Cell(IEnumerable<XElement> rows)=>new(w+"tc",new XElement(w+"tcPr",new XElement(w+"tcW",new XAttribute(w+"w",4500),new XAttribute(w+"type","dxa"))),rows);
        var table=new XElement(w+"tbl",new XElement(w+"tblPr",new XElement(w+"tblW",new XAttribute(w+"w",9000),new XAttribute(w+"type","dxa"))),
            new XElement(w+"tblGrid",new XElement(w+"gridCol",new XAttribute(w+"w",4500)),new XElement(w+"gridCol",new XAttribute(w+"w",4500))),
            new XElement(w+"tr",Cell(left),Cell([Row(Checker.Heading,13,true),Row(HeadingRules.Motto,14,true),Row("Hà Nội, ngày 03 tháng 3 năm 2026",13,false,true)])));
        var body=new XElement(w+"body",table);
        if(namedType is not null){body.Add(Row(namedType,14,true));body.Add(Row("Tổng hợp kết quả công tác",14,true));}
        body.Add(new XElement(w+"p",new XElement(w+"r",new XElement(w+"t","Kính gửi: đơn vị mẫu."))));
        body.Add(new XElement(w+"p",new XElement(w+"r",new XElement(w+"t","Nội dung mẫu này phải giữ nguyên."))));
        body.Add(new XElement(w+"sectPr",new XElement(w+"pgSz",new XAttribute(w+"w",11906),new XAttribute(w+"h",16838)),new XElement(w+"pgMar",new XAttribute(w+"top",1134),new XAttribute(w+"right",850),new XAttribute(w+"bottom",1134),new XAttribute(w+"left",1701),new XAttribute(w+"header",0),new XAttribute(w+"footer",0),new XAttribute(w+"gutter",0))));
        string flat=new XElement(p+"package",Part("_rels/.rels","application/vnd.openxmlformats-package.relationships+xml",new XElement(r+"Relationships",new XElement(r+"Relationship",new XAttribute("Id","main"),new XAttribute("Type","http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"),new XAttribute("Target","word/document.xml")))),
            Part("word/document.xml","application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml",new XElement(w+"document",body))).ToString(SaveOptions.DisableFormatting);
        string file=Path.Combine(folder,"subject-"+Guid.NewGuid().ToString("N")+".docx");IsolatedWordPackage.FromFlat(flat).WriteNew(file);return file;
    }
    internal static void Run(WordSession test,string modern,string folder,Action<string> pass)
    {
        _=modern;
        foreach(float size in new[]{12f,13f})
        {
            test.Open(Create(folder,size:size));var issues=test.Check();
            if(issues.Any(i=>i.Kind==FixKind.DocumentHeadingFormat) || test.LineTargets.Count!=2 || test.LineTargets.Any(t=>t.Role==LineRole.Subject) ||
                issues.Any(i=>i.Kind==FixKind.ShapeLine && (i.Title??"").Contains("trích yếu",StringComparison.Ordinal)))
                throw new InvalidOperationException("Công văn đúng cỡ 12/13 bị nhầm thể thức hoặc yêu cầu đường kẻ trích yếu.");
        }
        pass("Công văn V/v trong ô trái dưới số: cỡ 12/13 hợp lệ, không có tên loại riêng và chỉ hai mốc đường kẻ");
        foreach(string title in new[]{"QUYẾT ĐỊNH","BÁO CÁO","KẾ HOẠCH"})
        {
            test.Open(Create(folder,title));var issues=test.Check();
            if(issues.Any(i=>i.Kind==FixKind.DocumentHeadingFormat) || test.LineTargets.Count!=3 || !test.LineTargets.Any(t=>t.Role==LineRole.Subject))
                throw new InvalidOperationException("Văn bản có tên loại chưa giữ trích yếu ngoài bảng, đậm cỡ 14 và mốc đường kẻ.");
        }
        pass("Quyết định/Báo cáo/Kế hoạch: tên loại và trích yếu ngoài bảng cỡ 14 đậm, ba mốc đường kẻ");
        string bad=Create(folder,bad:true);byte[] sourceHash=SHA256.HashData(File.ReadAllBytes(bad));test.Open(bad);string original=test.ReadCheckText();
        var fix=test.Check().Single(i=>i.CanFix && i.CharacterFormat?.Role==ComponentRole.LetterSubject);
        test.FixSelection([fix]);var after=test.Check();
        if(test.ReadCheckText()!=original || after.Any(i=>i.CharacterFormat?.Role==ComponentRole.LetterSubject) || test.CheckUndoCount!=1 || test.LineTargets.Any(t=>t.Role==LineRole.Subject))
            throw new InvalidOperationException("Sửa phông trích yếu công văn chưa qua quét lại/chữ/Undo hoặc sinh đường kẻ sai.");
        string saved=Path.Combine(folder,"letter-corrected.docx");test.SaveCopy(saved);test.Undo();
        if(test.ReadCheckText()!=original || !test.Check().Any(i=>i.CanFix && i.CharacterFormat?.Role==ComponentRole.LetterSubject))throw new InvalidOperationException("Undo chưa phục hồi định dạng trích yếu công văn.");
        test.Open(saved);if(test.Check().Any(i=>i.CharacterFormat?.Role==ComponentRole.LetterSubject) || test.ReadCheckText()!=original || !SHA256.HashData(File.ReadAllBytes(bad)).SequenceEqual(sourceHash))
            throw new InvalidOperationException("Lưu/mở trích yếu công văn chưa đúng hoặc nguồn bị đổi.");
        pass("Sửa phông riêng công văn giữ chữ, một Undo, lưu/mở, nguồn bất biến và không thêm mốc trích yếu");
    }
}
