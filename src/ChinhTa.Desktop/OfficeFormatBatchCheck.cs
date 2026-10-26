using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

internal static class OfficeFormatBatchCheck
{
    internal const string Text="Nội dung  cần giữ nguyên trong lượt sửa trang.";
    internal static string Create(string modern,string folder)
    {
        string seed=OfficePageLayoutCheck.Create(modern,folder),path=Path.Combine(folder,"format-batch-input.docx");File.Copy(seed,path);
        using var zip=ZipFile.Open(path,ZipArchiveMode.Update);var entry=zip.GetEntry("word/document.xml")!;XDocument xml;
        using(var s=entry.Open())xml=XDocument.Load(s,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var body=xml.Root!.Element(w+"body")!;var section=body.Element(w+"sectPr")!;
        section.Element(w+"pgMar")!.SetAttributeValue(w+"top",1134); // legal 20mm: PAGE can already be approved.
        section.Element(w+"pgMar")!.SetAttributeValue(w+"header",454);
        var agency=body.Descendants(w+"p").Single(p=>string.Concat(p.Descendants(w+"t").Select(e=>e.Value))=="ĐƠN VỊ BAN HÀNH");
        agency.Descendants(w+"color").Single().SetAttributeValue(w+"val","FF0000");
        section.AddBeforeSelf(new XElement(w+"p",new XElement(w+"r",new XElement(w+"br",new XAttribute(w+"type","page")))),
            new XElement(w+"p",new XElement(w+"r",new XElement(w+"t","Trang hai để kiểm số tự động và giữ dữ liệu."))));
        entry.Delete();using(var s=zip.CreateEntry("word/document.xml").Open())xml.Save(s,SaveOptions.DisableFormatting);return path;
    }
    public static void Run(WordSession test,string modern,string folder,Action<string> pass)
    {
        string isolated=Path.Combine(folder,"format-batch");Directory.CreateDirectory(isolated);string source=Create(modern,isolated);
        byte[] hash=SHA256.HashData(File.ReadAllBytes(source));test.Profile=DocumentProfile.State;test.Open(source);test.Check();
        string original=test.ReadCheckText();var styles=test.ReadCheckCurrentStyles(Text);
        Issue[] Selection()
        {
            var found=test.Check();int start=test.ReadCheckCurrentParagraph(Text).Start;
            var items=found.Where(i=>i.CanFix && (i.NumberFix is not null || i.PageFix is not null || i.Kind==FixKind.AgencyFormat ||
                i.Kind==FixKind.Text && i.Original=="  " && i.Start>=start && i.Start+i.Length<=start+Text.Length)).Concat(OfficeComponentCheck.ReadLines(test)).Distinct().ToArray();
            if(items.Count(i=>i.NumberFix is not null)!=1 || items.Count(i=>i.PageFix is not null)!=1 || items.Count(i=>i.LineFix is not null)!=3 ||
                items.Count(i=>i.Kind==FixKind.AgencyFormat)!=1 || items.Count(i=>i.Kind==FixKind.Text)!=1)
                throw new InvalidOperationException("Mẫu kiểm tổng thể chưa đủ bảy mục: trang/PAGE/phông/chữ/ba Line.");
            return items;
        }
        foreach(int stage in new[]{1,2,3})
        {
            bool rolledBack=false;
            try{test.FixStagedFailureProbe(Selection(),stage);}catch(InvalidOperationException e) when(e.InnerException is OfficeStagedProbeException){rolledBack=true;}
            test.Check();
            if(!rolledBack || test.ReadCheckText()!=original || test.CheckUndoCount!=0 || test.Changed || Selection().Length!=7)
                throw new InvalidOperationException("Rollback kiểm tổng thể chưa phục hồi bảy mục đã chọn.");
        }
        var selected=Selection();int originalStart=test.ReadCheckCurrentParagraph(Text).Start;
        var expectedStyles=styles.Project(selected.Where(i=>i.Kind==FixKind.Text).ToArray(),i=>Text[..(i.Start-originalStart)]);
        var updates=new List<RepairProgressUpdate>();var priorReporter=test.ProgressReporter;
        try{test.ProgressReporter=updates.Add;test.FixSelection(selected);}finally{test.ProgressReporter=priorReporter;}
        string corrected=Text.Replace("  "," ",StringComparison.Ordinal),expected=original.Replace(Text,corrected,StringComparison.Ordinal);
        var remaining=test.Check();OfficeComponentCheck.ReadLines(test,true);
        if(test.ReadCheckText()!=expected || test.CheckUndoCount!=1 || !expectedStyles.Matches(test.ReadCheckCurrentStyles(corrected)) ||
            remaining.Any(i=>i.Kind is FixKind.PageLayout or FixKind.PageNumber or FixKind.AgencyFormat) ||
            updates.Count==0 || updates.Any(u=>u.Total!=7) || updates.Last().Verified!=7)
            throw new InvalidOperationException("Lượt sửa tổng thể chưa đạt chữ/phông/trang/Line hoặc tiến độ không giữ tổng bảy mục.");
        string saved=Path.Combine(isolated,"format-batch-fixed.docx");test.SaveCopy(saved);test.Undo();test.Check();
        if(test.ReadCheckText()!=original || Selection().Length!=7)throw new InvalidOperationException("Một Undo chưa phục hồi cả lượt tổng thể.");
        test.Open(saved);remaining=test.Check();OfficeComponentCheck.ReadLines(test,true);
        if(test.ReadCheckText()!=expected || remaining.Any(i=>i.Kind is FixKind.PageLayout or FixKind.PageNumber or FixKind.AgencyFormat) ||
            !expectedStyles.Matches(test.ReadCheckCurrentStyles(corrected)) || !hash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(source))))
            throw new InvalidOperationException("Lưu/mở kiểm tổng thể chưa giữ chữ/phông/trang/Line hoặc nguồn.");
        pass("Bảy mục chung: khổ/lề, PAGE, phông cơ quan, khoảng trắng và ba Line; tiến độ giữ tổng, hậu kiểm cuối, rollback ba giai đoạn, một Undo và lưu/mở");
    }
}
