using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

// An actual-Word case, separate from synthetic XPS checks. It must fail/restore
// if Word discards a detached footer part or the measured body changes.
internal static class OfficeFooterPageMigrationCheck
{
    internal static string Create(string modern,string folder,bool multiDigit=false)
    {
        string source=OfficePageNumberCheck.Create(modern,folder),path=Path.Combine(folder,"footer-page-input.docx");File.Copy(source,path);
        using(var zip=ZipFile.Open(path,ZipArchiveMode.Update))
        {
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var footer=new XDocument(new XElement(w+"ftr",new XElement(w+"p",new XElement(w+"pPr",new XElement(w+"jc",new XAttribute(w+"val","center"))),
            new XElement(w+"fldSimple",new XAttribute(w+"instr"," PAGE "),new XElement(w+"r",new XElement(w+"rPr",
                new XElement(w+"rFonts",new XAttribute(w+"ascii","Times New Roman"),new XAttribute(w+"hAnsi","Times New Roman")),
                new XElement(w+"color",new XAttribute(w+"val","000000")),new XElement(w+"sz",new XAttribute(w+"val",28))),new XElement(w+"t","1"))))));
        if(multiDigit)
        {
            var entry=zip.GetEntry("word/document.xml")!;XDocument document;
            using(var input=entry.Open())document=XDocument.Load(input,LoadOptions.PreserveWhitespace);
            var section=document.Descendants(w+"body").Single().Element(w+"sectPr")!;
            for(int i=0;i<10;i++)section.AddBeforeSelf(new XElement(w+"p",
                new XElement(w+"pPr",new XElement(w+"pageBreakBefore")),new XElement(w+"r",new XElement(w+"t","Nội dung trang bổ sung cần giữ nguyên."))));
            entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())document.Save(output,SaveOptions.DisableFormatting);
        }
        const string part="word/footer-number-kept.xml";zip.GetEntry(part)!.Delete();using(var output=zip.CreateEntry(part).Open())footer.Save(output,SaveOptions.DisableFormatting);
        }
        OfficeFocusedCheck.OrderFixtureProperties(path);
        return path;
    }
    public static void Run(WordSession test,string modern,string folder,Action<string> pass,bool multiDigit=false)
    {
        string isolated=Path.Combine(folder,"footer-page-migration");Directory.CreateDirectory(isolated);string source=Create(modern,isolated,multiDigit);
        byte[] sourceHash=SHA256.HashData(File.ReadAllBytes(source));test.Profile=DocumentProfile.State;test.Open(source);
        Issue Approved()=>test.Check().Single(i=>i.CanFix && i.NumberFix?.FooterMigration is not null);
        var issue=Approved();
        if(multiDigit && issue.NumberFix!.FooterMigration!.Pages<10)throw new InvalidOperationException("Mẫu PAGE nhiều chữ số chưa đạt ít nhất10trang thực tế.");
        string original=test.ReadCheckText();
        bool restored=false;
        try{test.FixStagedFailureProbe([issue],2);}catch(InvalidOperationException error) when(error.InnerException is OfficeStagedProbeException){restored=true;}
        if(!restored || test.ReadCheckText()!=original || test.CheckUndoCount!=0 || test.Changed || Approved().NumberFix is null)
            throw new InvalidOperationException("Rollback chuyển PAGE cuối trang chưa phục hồi toàn bộ nguồn.");
        test.Fix(Approved());test.Check();
        if(test.Check().Any(i=>i.Kind==FixKind.PageNumber) || test.ReadCheckText()!=original || test.CheckUndoCount!=1)
            throw new InvalidOperationException("Chuyển PAGE cuối trang chưa giữ thân hoặc chưa đạt header thực tế.");
        string saved=Path.Combine(isolated,"footer-page-fixed.docx");test.SaveCopy(saved);test.Undo();
        if(test.ReadCheckText()!=original || test.CheckUndoCount!=0 || Approved().NumberFix?.FooterMigration is null)
            throw new InvalidOperationException("Một Undo chưa phục hồi PAGE cuối trang.");
        test.Open(saved);test.Check();
        if(test.Check().Any(i=>i.Kind==FixKind.PageNumber) || test.ReadCheckText()!=original ||
            !sourceHash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(source))))
            throw new InvalidOperationException("Lưu/mở chuyển PAGE cuối trang chưa giữ kết quả hoặc nguồn.");
        pass("Chuyển PAGE đơn thuần cuối trang lên header: XPS số2/ẩn số1, giữ thân và phần footer gốc, rollback muộn, một Undo, lưu/mở và nguồn nguyên vẹn");
    }
}
