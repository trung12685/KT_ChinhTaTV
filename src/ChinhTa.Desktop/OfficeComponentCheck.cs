using System.IO;
using System.IO.Compression;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
internal static class OfficeComponentCheck
{
    public static void Run(WordSession test,string modern,string folder,Action<string> pass)
    {
        bool Component(Issue i)=>i.Kind is FixKind.AgencyFormat or FixKind.AgencyText or FixKind.RecipientFormat or FixKind.RecipientText or FixKind.DocumentHeadingFormat;
        string good=Create(modern,folder,false),bad=Create(modern,folder,true);
        var sourceHash=System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(bad));
        test.Profile=DocumentProfile.State;test.Open(good);
        if(test.Check().Any(Component))throw new InvalidOperationException("Mẫu đúng còn cảnh báo tên cơ quan/trích yếu/nơi nhận.");
        pass("Thành phần đúng: cơ quan chủ quản/ban hành, trích yếu xuống đoạn, Kính gửi căn giữa và ba gạch nơi nhận không bị báo sai");
        test.Open(bad);string original=test.ReadCheckText();var plan=test.Check().Where(i=>i.CanFix && Component(i)).ToArray();
        if(!plan.Any(i=>i.Kind==FixKind.AgencyFormat) || !plan.Any(i=>i.Kind==FixKind.RecipientFormat) || !plan.Any(i=>i.Original.Contains('\v')))
            throw new InvalidOperationException("Mẫu sai chưa tạo đủ đề xuất mới hoặc chưa cho sửa phông giữ ngắt dòng.");
        bool failed=false;
        try{test.FixTextFailureProbe(plan,3);}catch(InvalidOperationException error) when(error.InnerException is OfficeTextProbeException){failed=true;}
        if(!failed || test.ReadCheckText()!=original || test.CheckUndoCount!=0 || test.Changed)
            throw new InvalidOperationException("Rollback nhóm cơ quan/nơi nhận/trích yếu chưa về nguyên bản.");
        pass("Thành phần sai: lỗi giữa lượt phục hồi toàn bộ và không tạo Undo");
        plan=test.Check().Where(i=>i.CanFix && Component(i)).ToArray();test.FixMany(plan);
        string expected=original.Replace("cơ quan chủ quản","CƠ QUAN CHỦ QUẢN",StringComparison.Ordinal)
            .Replace("đơn vị ban hành","ĐƠN VỊ BAN HÀNH",StringComparison.Ordinal).Replace("Mục thử nghiệm.","- Mục thử nghiệm;",StringComparison.Ordinal);
        if(test.ReadCheckText()!=expected || test.Check().Any(Component) || !test.ReadCheckText().Contains('\v') || test.CheckUndoCount!=1)
            throw new InvalidOperationException("Sửa thành phần chưa đúng chữ/phông/dấu hoặc làm mất ngắt dòng.");
        string saved=Path.Combine(folder,"components-fixed.docx");test.SaveCopy(saved);test.Undo();
        if(test.ReadCheckText()!=original)throw new InvalidOperationException("Undo thành phần chưa phục hồi cả lượt.");
        test.Open(saved);
        if(test.ReadCheckText()!=expected || test.Check().Any(Component) || !System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(bad)).SequenceEqual(sourceHash))
            throw new InvalidOperationException("Lưu/mở thành phần chưa đúng hoặc nguồn mẫu bị đổi.");
        pass("Thành phần: sửa nhóm, giữ ngắt dòng, quét, một Undo và lưu/mở với nguồn nguyên vẹn");
    }
    private static string Create(string source,string folder,bool bad)
    {
        string path=Path.Combine(folder,bad?"components-bad.docx":"components-good.docx");File.Copy(source,path);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);var entry=zip.GetEntry("word/document.xml")!;
        XDocument xml;using(var s=entry.Open())xml=XDocument.Load(s,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XElement Run(string text,float size,bool bold=false,bool italic=false,bool broken=false)=>new(w+"r",new XElement(w+"rPr",
            new XElement(w+"rFonts",new XAttribute(w+"ascii",broken?"Arial":"Times New Roman"),new XAttribute(w+"hAnsi",broken?"Arial":"Times New Roman")),
            new XElement(w+"sz",new XAttribute(w+"val",size*2)),new XElement(w+"b",new XAttribute(w+"val",bold?1:0)),
            new XElement(w+"i",new XAttribute(w+"val",italic?1:0)),new XElement(w+"color",new XAttribute(w+"val",broken?"FF0000":"000000"))),new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),text));
        XElement Row(string text,float size,bool bold=false,bool italic=false,bool center=false,bool broken=false)=>new(w+"p",
            new XElement(w+"pPr",new XElement(w+"jc",new XAttribute(w+"val",center?"center":"left"))),Run(text,size,bold,italic,broken));
        foreach(var (name,bold) in new[]{("CƠ QUAN CHỦ QUẢN",false),("ĐƠN VỊ BAN HÀNH",true)})
        {
            var text=xml.Descendants(w+"t").Single(t=>t.Value==name);var p=text.Ancestors(w+"p").First();p.Elements(w+"r").Remove();
            p.Add(Run(bad?name.ToLowerInvariant():name,bad?14:13,bad?!bold:bold,false,bad));
        }
        var title=xml.Descendants(w+"p").Single(p=>string.Concat(p.Descendants(w+"t").Select(t=>t.Value))=="BÁO CÁO");
        var subject=title.ElementsAfterSelf(w+"p").First();
        var first=Row("Mang lại bình",bad?12:14,true,center:true,broken:bad);
        if(bad)first.Add(new XElement(w+"r",new XElement(w+"br")),Run("yên cho nhân dân",12,true,false,true));
        else first=Row("Mang lại bình ",14,true,center:true);
        subject.ReplaceWith(first,Row("yên cho nhân dân",14,true,center:true));
        var greeting=xml.Descendants(w+"p").Single(p=>string.Concat(p.Descendants(w+"t").Select(t=>t.Value)).StartsWith("Kính gửi:"));
        greeting.Element(w+"pPr")?.Element(w+"jc")?.SetAttributeValue(w+"val","center");
        var table=new XElement(w+"tbl",new XElement(w+"tblPr"),new XElement(w+"tblGrid",new XElement(w+"gridCol",new XAttribute(w+"w",4000)),new XElement(w+"gridCol",new XAttribute(w+"w",4000))),
            new XElement(w+"tr",new XElement(w+"tc",Row("Nơi nhận:",bad?14:12,!bad,!bad),Row("- Như trên;",11),
                Row(bad?"Mục thử nghiệm.":"- Mục thử nghiệm;",bad?14:11,broken:bad),Row("- Lưu: VT, TEST.",11)),new XElement(w+"tc",Row("TRƯỞNG ĐƠN VỊ",13,true))));
        var body=xml.Descendants(w+"body").Single();if(body.Element(w+"sectPr") is XElement section)section.AddBeforeSelf(table);else body.Add(table);
        entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);return path;
    }
}
