using System.IO;
using System.IO.Compression;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;
internal static class OfficeComponentCheck
{
    public static void Run(WordSession test,string modern,string folder,Action<string> pass)
    {
        bool Component(Issue i)=>i.Kind is FixKind.AgencyFormat or FixKind.AgencyText or FixKind.RecipientFormat or FixKind.RecipientText or FixKind.DocumentHeadingFormat or FixKind.DocumentTypeText;
        string good=Create(modern,folder,false),bad=Create(modern,folder,true);
        var sourceHash=System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(bad));
        test.Profile=DocumentProfile.State;test.Open(good);
        if(test.Check().Any(Component))throw new InvalidOperationException("Mẫu đúng còn cảnh báo tên cơ quan/trích yếu/nơi nhận.");
        pass("Thành phần đúng: cơ quan chủ quản/ban hành, trích yếu xuống đoạn, Kính gửi căn giữa và ba gạch nơi nhận không bị báo sai");
        string goodText=test.ReadCheckText();var allLines=ReadLines(test);
        if(allLines.Length!=3)throw new InvalidOperationException("Mẫu đúng chưa nhận đủ ba Line thiếu từ số đo XPS.");
        test.FixSelection(allLines);
        if(test.Check().Any(Component) || test.ReadCheckText()!=goodText || test.CheckUndoCount!=1)
            throw new InvalidOperationException("Sau ba Line, thành phần đúng bị cảnh báo giả hoặc chữ/Undo thay đổi.");
        string withLines=Path.Combine(folder,"components-good-three-lines.docx");test.SaveCopy(withLines);
        test.Undo();if(test.ReadCheckText()!=goodText)throw new InvalidOperationException("Undo ba Line thay đổi chữ mẫu đúng.");
        test.Open(withLines);
        if(test.Check().Any(Component) || test.ReadCheckText()!=goodText)
            throw new InvalidOperationException("Mở lại ba Line sinh cảnh báo giả về cơ quan/trích yếu.");
        pass("Thành phần đúng sau sửa cả ba Line: quét, lưu/mở và một Undo không sinh cảnh báo phông giả");
        test.Open(bad);string original=test.ReadCheckText();var plan=test.Check().Where(i=>i.CanFix && Component(i)).ToArray();
        if(!plan.Any(i=>i.Kind==FixKind.DocumentTypeText) || !plan.Any(i=>i.Kind==FixKind.AgencyFormat) || !plan.Any(i=>i.Kind==FixKind.RecipientFormat) || !plan.Any(i=>i.Original.Contains('\v')))
            throw new InvalidOperationException("Mẫu sai chưa tạo đủ đề xuất mới hoặc chưa cho sửa phông giữ ngắt dòng.");
        bool failed=false;
        try{test.FixTextFailureProbe(plan,3);}catch(InvalidOperationException error) when(error.InnerException is OfficeTextProbeException){failed=true;}
        if(!failed || test.ReadCheckText()!=original || test.CheckUndoCount!=0 || test.Changed)
            throw new InvalidOperationException("Rollback nhóm cơ quan/nơi nhận/trích yếu chưa về nguyên bản.");
        pass("Thành phần sai: lỗi giữa lượt phục hồi toàn bộ và không tạo Undo");
        plan=test.Check().Where(i=>i.CanFix && Component(i)).ToArray();test.FixMany(plan);
        string expected=original.Replace("Báo cáo","BÁO CÁO",StringComparison.Ordinal).Replace("cơ quan chủ quản","CƠ QUAN CHỦ QUẢN",StringComparison.Ordinal)
            .Replace("đơn vị ban hành","ĐƠN VỊ BAN HÀNH",StringComparison.Ordinal).Replace("Mục thử nghiệm.","- Mục thử nghiệm;",StringComparison.Ordinal);
        if(test.ReadCheckText()!=expected || test.Check().Any(Component) || !test.ReadCheckText().Contains('\v') || test.CheckUndoCount!=1)
            throw new InvalidOperationException("Sửa thành phần chưa đúng chữ/phông/dấu hoặc làm mất ngắt dòng.");
        string saved=Path.Combine(folder,"components-fixed.docx");test.SaveCopy(saved);test.Undo();
        if(test.ReadCheckText()!=original)throw new InvalidOperationException("Undo thành phần chưa phục hồi cả lượt.");
        test.Open(saved);
        if(test.ReadCheckText()!=expected || test.Check().Any(Component) || !System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(bad)).SequenceEqual(sourceHash))
            throw new InvalidOperationException("Lưu/mở thành phần chưa đúng hoặc nguồn mẫu bị đổi.");
        pass("Thành phần: sửa nhóm, giữ ngắt dòng, quét, một Undo và lưu/mở với nguồn nguyên vẹn");
        string longer=Create(modern,folder,true,true);test.Open(longer);var found=test.Check();
        var edits=found.Where(i=>i.CanFix && Component(i)).ToArray();
        var lines=ReadLines(test,deferComponents:true);
        if(lines.Length!=3)throw new InvalidOperationException("Mẫu phông sai chưa nhận đủ ba Line thiếu.");
        string longOriginal=test.ReadCheckText();var selected=edits.Concat(lines).ToArray();
        bool rolledBack=false;
        try{test.FixStagedFailureProbe(selected,3);}catch(InvalidOperationException error) when(error.InnerException is OfficeStagedProbeException){rolledBack=true;}
        if(!rolledBack || test.ReadCheckText()!=longOriginal || test.CheckUndoCount!=0 || test.Changed)
            throw new InvalidOperationException("Rollback phông và ba Line chưa phục hồi toàn bộ.");
        pass("Phông và ba Line: lỗi sau chèn Line phục hồi cả lượt, không tạo Undo");
        found=test.Check();edits=found.Where(i=>i.CanFix && Component(i)).ToArray();lines=ReadLines(test,deferComponents:true);
        test.FixSelection(edits.Concat(lines));
        if(test.Check().Any(Component) || test.CheckUndoCount!=1)throw new InvalidOperationException("Hậu kiểm phông cơ quan/trích yếu/nơi nhận sau ba Line chưa qua.");
        string allFixed=Path.Combine(folder,"components-all-fixed.docx");test.SaveCopy(allFixed);
        test.Undo();if(test.ReadCheckText()!=longOriginal)throw new InvalidOperationException("Undo nhóm nơi nhận dài/Line chưa về nguyên bản.");
        test.Open(allFixed);
        if(test.Check().Any(Component))throw new InvalidOperationException("Mở lại phông và ba Line còn cảnh báo thành phần.");
        pass("Phông cơ quan/trích yếu/nơi nhận sau đoạn 40: sửa cùng cả ba Line, quét, một Undo và lưu/mở");
    }
    private static Issue[] ReadLines(WordSession test,bool deferComponents=false)
    {
        var found=test.Check();var anchors=new Dictionary<Issue,PreviewAnchor>();
        string path=test.ExportPreview(test.LineTargets.Select(t=>t.Probe).ToArray(),anchors);
        try
        {
            using var xps=new System.Windows.Xps.Packaging.XpsDocument(path,FileAccess.Read);
            // Test-only deferred intent: selected component edits run before fresh
            // Line measurements in FixStaged. Do not change the production UI gate.
            var blockers=deferComponents?found.Where(i=>!i.CanFix || i.Kind is not (FixKind.AgencyFormat or FixKind.AgencyText or FixKind.RecipientFormat or FixKind.RecipientText or FixKind.DocumentHeadingFormat or FixKind.DocumentTypeText)).ToArray():found;
            return LineInspection.Inspect(xps.GetFixedDocumentSequence(),new PreviewHighlighter(),test.LineTargets,test.LineShapes,anchors,blockers,
                LinePolicy.Decree30,test.HasGroupedShapes,test.VerifiedLineEdits).Select(e=>e.Issue).Where(i=>i.CanFix && i.LineFix is not null).ToArray();
        }
        finally{if(File.Exists(path))File.Delete(path);}
    }
    private static string Create(string source,string folder,bool bad,bool longBody=false)
    {
        string path=Path.Combine(folder,longBody?"components-long.docx":bad?"components-bad.docx":"components-good.docx");File.Copy(source,path);
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
        if(bad)title.Descendants(w+"t").Single().Value="Báo cáo";
        var first=Row("Mang lại bình",bad?12:14,true,center:true,broken:bad);
        if(bad)first.Add(new XElement(w+"r",new XElement(w+"br")),Run("yên cho nhân dân",12,true,false,true));
        else first=Row("Mang lại bình ",14,true,center:true);
        subject.ReplaceWith(first,Row("yên cho nhân dân",14,true,center:true));
        var greeting=xml.Descendants(w+"p").Single(p=>string.Concat(p.Descendants(w+"t").Select(t=>t.Value)).StartsWith("Kính gửi:"));
        greeting.Element(w+"pPr")?.Element(w+"jc")?.SetAttributeValue(w+"val","center");
        var table=new XElement(w+"tbl",new XElement(w+"tblPr"),new XElement(w+"tblGrid",new XElement(w+"gridCol",new XAttribute(w+"w",4000)),new XElement(w+"gridCol",new XAttribute(w+"w",4000))),
            new XElement(w+"tr",new XElement(w+"tc",Row("Nơi nhận:",bad?14:12,!bad,!bad),Row("- Như trên;",11),
                Row(bad?"Mục thử nghiệm.":"- Mục thử nghiệm;",bad?14:11,broken:bad),Row("- Lưu: VT, TEST.",11)),new XElement(w+"tc",Row("TRƯỞNG ĐƠN VỊ",13,true))));
        var body=xml.Descendants(w+"body").Single();
        if(longBody)for(int i=0;i<45;i++){var row=Row("Nội dung bổ sung thứ "+i+".",14);if(body.Element(w+"sectPr") is XElement s)s.AddBeforeSelf(row);else body.Add(row);}
        if(body.Element(w+"sectPr") is XElement section)section.AddBeforeSelf(table);else body.Add(table);
        entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);return path;
    }
}
