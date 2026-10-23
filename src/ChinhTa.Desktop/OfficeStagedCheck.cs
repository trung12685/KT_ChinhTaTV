using System.IO;
using System.IO.Compression;
using System.Xml.Linq;
using System.Windows.Xps.Packaging;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

// Synthetic Word runtime coverage; Windows CI compiles this but has no installed Word.
internal static class OfficeStagedCheck
{
    public static void Run(WordSession test,string modern,string folder,Action<string> pass)
    {
        string source=Create(modern,folder,false),full=Create(modern,folder,true);
        byte[] Digest(string path)=>System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path));
        byte[] sourceDigest=Digest(source),fullDigest=Digest(full);
        static bool Body(Issue i)=>i.CanFix && i.Kind is FixKind.OutlineOrder or FixKind.OutlineMarker;
        test.Profile=DocumentProfile.State;test.Open(source);
        string original=test.ReadCheckText();var initial=Review(test);
        if(initial.Count(Body)!=2 || initial.Count(i=>i.CanFix && i.LineFix is not null)!=3)
            throw new InvalidOperationException("Mẫu nhiều nhóm chưa nhận đúng hai lỗi mục và ba đường kẻ.");
        var selected=initial.Where(i=>Body(i) || i.LineFix?.Role==LineRole.Subject).ToArray();
        var result=test.FixSelection(selected);var after=Review(test);
        string expected=original.Replace("5. Đề xuất","3. Đề xuất",StringComparison.Ordinal)
            .Replace("- Nội dung đơn","Nội dung đơn",StringComparison.Ordinal);
        if(result.Selected!=3 || result.Applied+result.AlreadySatisfied!=3 || test.ReadCheckText()!=expected ||
            after.Any(Body) || after.Any(i=>i.LineFix?.Role==LineRole.Subject) ||
            after.Count(i=>i.CanFix && i.LineFix is not null)!=2 || !after.Any(i=>i.Kind==FixKind.Text && i.Original=="  ") || test.CheckUndoCount!=1)
            throw new InvalidOperationException("Lượt nhiều nhóm chưa đúng phạm vi chọn, số đo mới hoặc một Undo.");
        test.Undo();if(test.ReadCheckText()!=original || test.CheckUndoCount!=0 || Review(test).Count(i=>i.CanFix && i.LineFix is not null)!=3)
            throw new InvalidOperationException("Một Undo chưa phục hồi cả mục và Line.");
        pass("Nhiều nhóm: hai lỗi mục + một Line, giữ hai Line và khoảng trắng chưa chọn; một Undo");

        test.Open(full);string fullOriginal=test.ReadCheckText();var fullFound=Review(test);
        if(!fullFound.Any(i=>i.CenterFix is not null) || !fullFound.Any(i=>i.CharacterFormat is not null))
            throw new InvalidOperationException("Mẫu nhiều nhóm thiếu phông hoặc căn giữa để kiểm đủ ba bước.");
        var all=fullFound.Where(i=>i.CanFix).ToArray();
        for(int stage=1;stage<=3;stage++)
        {
            test.Open(full);bool failed=false;
            try{test.FixStagedFailureProbe(all,stage);}
            catch(InvalidOperationException error)
            {
                for(Exception? cause=error;cause is not null;cause=cause.InnerException)
                    if(cause is OfficeStagedProbeException)failed=true;
            }
            if(!failed || test.ReadCheckText()!=fullOriginal || test.CheckUndoCount!=0 || test.Changed ||
                Review(test).Count(i=>i.CanFix && i.LineFix is not null)!=3)
                throw new InvalidOperationException("Lỗi sau bước "+stage+" chưa phục hồi toàn bộ các nhóm hoặc đã tạo Undo.");
        }
        pass("Nhiều nhóm: lỗi sau chữ, sau căn giữa và sau Line đều phục hồi cả lượt, không tạo Undo");
        test.Open(full);var first=test.Check().Single(i=>i.CanFix && i.Kind==FixKind.OutlineOrder);test.Fix(first);
        string prior=test.ReadCheckText();int priorDepth=test.CheckUndoCount;bool rejected=false;
        try{test.FixStagedFailureProbe(Review(test).Where(i=>i.CanFix),3);}
        catch(InvalidOperationException){rejected=true;}
        if(!rejected || test.ReadCheckText()!=prior || test.CheckUndoCount!=priorDepth || !test.Changed)
            throw new InvalidOperationException("Rollback nhiều nhóm làm mất lượt sửa/Undo có trước.");
        test.Undo();if(test.ReadCheckText()!=fullOriginal)throw new InvalidOperationException("Undo cũ không còn dùng được sau rollback nhiều nhóm.");
        pass("Nhiều nhóm: rollback giữ lượt sửa và ngăn xếp Undo đã có");

        test.Open(full);var applied=test.FixSelection(Review(test).Where(i=>i.CanFix));string fixedText=test.ReadCheckText();
        if(applied.Selected!=applied.Applied+applied.AlreadySatisfied || Review(test).Any(i=>i.CanFix) || test.CheckUndoCount!=1)
            throw new InvalidOperationException("Sửa tất cả nhiều nhóm còn mục sửa được hoặc có hơn một Undo.");
        test.VerifyLineResult(test.VerifiedLineEdits.Select(i=>i with {Start=test.LineTargets.Single(t=>t.Role==i.LineFix!.Role).Start}).ToArray(),verifyReference:true);
        string saved=Path.Combine(folder,"staged-fixed.docx");test.SaveCopy(saved);test.Undo();
        if(test.ReadCheckText()!=fullOriginal || test.CheckUndoCount!=0)throw new InvalidOperationException("Undo tất cả nhiều nhóm chưa về nguyên bản.");
        test.Open(saved);
        if(test.ReadCheckText()!=fixedText || Review(test).Any(i=>i.CanFix) ||
            !Digest(source).SequenceEqual(sourceDigest) || !Digest(full).SequenceEqual(fullDigest))
            throw new InvalidOperationException("Lưu/mở nhiều nhóm chưa đúng hoặc nguồn mẫu bị thay đổi.");
        pass("Nhiều nhóm: chữ/phông → căn giữa → ba nét XPS, quét, một Undo, lưu/mở và nguồn nguyên vẹn");
    }
    private static IReadOnlyList<Issue> Review(WordSession test)
    {
        var found=test.Check().ToList();var anchors=new Dictionary<Issue,PreviewAnchor>();
        var probes=test.LineTargets.Select(t=>t.Probe).Concat(test.AdministrativePositionTargets.SelectMany(t=>t.References.Select(r=>r.Probe).Append(t.Probe))).ToArray();
        string path=test.ExportPreview(probes,anchors);
        try
        {
            using var xps=new XpsDocument(path,FileAccess.Read);var sequence=xps.GetFixedDocumentSequence();var preview=new PreviewHighlighter();
            found.AddRange(LineInspection.Inspect(sequence,preview,test.LineTargets,test.LineShapes,anchors,found,LinePolicy.Decree30,
                test.HasGroupedShapes,test.VerifiedLineEdits).Select(e=>e.Issue));
            found.AddRange(AdministrativePositionInspection.Inspect(sequence,preview,test.AdministrativePositionTargets,anchors).Select(e=>e.Issue));
            return found;
        }
        finally{if(File.Exists(path))File.Delete(path);}
    }
    private static string Create(string source,string folder,bool full)
    {
        string path=Path.Combine(folder,full?"staged-all-input.docx":"staged-subset-input.docx");File.Copy(source,path);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);var entry=zip.GetEntry("word/document.xml")!;
        XDocument xml;using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var title=xml.Descendants(w+"p").Single(p=>string.Concat(p.Descendants(w+"t").Select(t=>t.Value))=="BÁO CÁO");
        XElement Row(string value)=>new(w+"p",new XElement(w+"r",new XElement(w+"rPr",
            new XElement(w+"rFonts",new XAttribute(w+"ascii","Times New Roman"),new XAttribute(w+"hAnsi","Times New Roman")),
            new XElement(w+"sz",new XAttribute(w+"val",26))),new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),value)));
        title.AddBeforeSelf(new[]{Row("1. Tình hình"),Row("- Nội dung đơn"),Row("2. Kết quả công tác"),Row("5. Đề xuất"),Row("Giữ  khoảng trắng chưa chọn.")});
        if(full)
        {
            var text=xml.Descendants(w+"t").Single(t=>t.Value=="Số: 01/BC-ĐV");text.Value="Số: 3/BC-ĐV";
            var paragraph=text.Ancestors(w+"p").First();var properties=paragraph.Element(w+"pPr")!;
            properties.Element(w+"ind")?.Remove();properties.Add(new XElement(w+"ind",new XAttribute(w+"left",720),new XAttribute(w+"right",0)));
            var run=text.Parent!;var style=run.Element(w+"rPr")!;
            style.Element(w+"rFonts")!.SetAttributeValue(w+"ascii","Arial");style.Element(w+"rFonts")!.SetAttributeValue(w+"hAnsi","Arial");
            style.Element(w+"sz")!.SetAttributeValue(w+"val",24);style.Element(w+"color")?.Remove();style.Add(new XElement(w+"color",new XAttribute(w+"val","FF0000")));
        }
        entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);return path;
    }
}
