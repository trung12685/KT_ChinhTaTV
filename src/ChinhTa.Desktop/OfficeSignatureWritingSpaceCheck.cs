using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

// Synthetic handwriting-space samples: no signature image is required.
internal static class OfficeSignatureWritingSpaceCheck
{
    public static void Run(WordSession test,string modern,string folder,Action<string> pass)
    {
        foreach(int blanks in new[]{0,2})
        {
            string isolated=Path.Combine(folder,"hand-signing-space-"+blanks);Directory.CreateDirectory(isolated);
            string source=Create(modern,isolated,blanks);var hash=SHA256.HashData(File.ReadAllBytes(source));
            test.Profile=DocumentProfile.State;test.Open(source);test.Check();
            string original=test.ReadCheckText();const string name="Đại tá Nguyễn Văn An",authority="TRƯỞNG PHÒNG";
            var nameBefore=test.ReadCheckCurrentStyles(name);var authorityBefore=test.ReadCheckCurrentStyles(authority);
            var initial=test.ReadSigningSpaceProbe();
            if(!initial.Verified || initial.Gap>=77)throw new InvalidOperationException("Mẫu chỗ ký tay chưa tạo đúng khoảng còn thiếu.");
            bool rolledBack=false;
            try{test.SigningSpaceFailureProbe();}catch(InvalidOperationException e) when(e.InnerException is OfficeSigningSpaceProbeException){rolledBack=true;}
            if(!rolledBack || test.ReadCheckText()!=original || test.CheckUndoCount!=0 || test.Changed ||
                !nameBefore.Matches(test.ReadCheckCurrentStyles(name)) || !authorityBefore.Matches(test.ReadCheckCurrentStyles(authority)))
                throw new InvalidOperationException("Lỗi hậu kiểm chỗ ký chưa phục hồi cả lượt.");
            var result=test.ReserveHandSigningSpace();var measured=test.ReadSigningSpaceProbe();
            var expectedName=new CharacterStyleEvidence(nameBefore.Text,nameBefore.Styles,nameBefore.Paragraph! with
                {Spacing=nameBefore.Paragraph!.Spacing! with{Before=result.Before,BeforeAuto=0}});
            if(!result.Changed || !measured.Verified || measured.Gap is not double gap || Math.Abs(gap-80)>3 ||
                test.ReadCheckText()!=original || test.CheckUndoCount!=1 || !expectedName.Matches(test.ReadCheckCurrentStyles(name)) ||
                !authorityBefore.Matches(test.ReadCheckCurrentStyles(authority)))
                throw new InvalidOperationException("Chỗ ký tay chưa đạt80pt hoặc chữ/phông/bố cục khác đã đổi.");
            var repeated=test.ReserveHandSigningSpace();
            if(repeated.Changed || test.CheckUndoCount!=1)throw new InvalidOperationException("Chạy lại chỗ ký đã đủ vẫn sửa hoặc tạo thêm Undo.");
            string saved=Path.Combine(isolated,"hand-signing-fixed.docx");test.SaveCopy(saved);test.Undo();test.Check();
            if(test.ReadCheckText()!=original || !nameBefore.Matches(test.ReadCheckCurrentStyles(name)))throw new InvalidOperationException("Một Undo chưa phục hồi chỗ ký tay.");
            test.Open(saved);var reopened=test.ReadSigningSpaceProbe();
            if(!reopened.Verified || reopened.Gap is not double persisted || Math.Abs(persisted-80)>3 || test.ReadCheckText()!=original ||
                !hash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(source))))throw new InvalidOperationException("Chỗ ký tay không giữ được sau lưu/mở hoặc mẫu nguồn đã đổi.");
            pass($"Chỗ ký tay80pt, {blanks} dòng Enter sẵn: đo XPS, giữ chữ/phông/Enter, rollback muộn, một Undo, chạy lại không sửa và lưu/mở");
        }
    }
    internal static string Create(string modern,string folder,int blanks)
    {
        string source=OfficeSignatureFormatCheck.Create(modern,folder,60);
        using var zip=ZipFile.Open(source,ZipArchiveMode.Update);var entry=zip.GetEntry("word/document.xml")!;
        XDocument xml;using(var input=entry.Open())xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var table=xml.Descendants(w+"body").Single().Elements(w+"tbl").Last();
        // Put the last padding paragraph on a fresh page, leaving predictable
        // room for the requested signing area rather than depending on fonts.
        table.ElementsBeforeSelf(w+"p").Last().AddFirst(new XElement(w+"pPr",new XElement(w+"pageBreakBefore")));
        var cell=table.Elements(w+"tr").Single().Elements(w+"tc").Last();
        cell.Elements(w+"p").Where(p=>p.Descendants(w+"drawing").Any()).Remove();
        if(blanks==0)cell.Elements(w+"p").Where(p=>!p.Descendants(w+"t").Any()).Remove();
        entry.Delete();using(var output=zip.CreateEntry("word/document.xml").Open())xml.Save(output);
        zip.GetEntry("word/media/signature-format-fixture.png")?.Delete();
        var relEntry=zip.GetEntry("word/_rels/document.xml.rels")!;XDocument rels;
        using(var input=relEntry.Open())rels=XDocument.Load(input);
        rels.Root!.Elements().Where(e=>(string?)e.Attribute("Id")=="rIdSignatureFormatFixture").Remove();
        relEntry.Delete();using(var output=zip.CreateEntry("word/_rels/document.xml.rels").Open())rels.Save(output);
        return source;
    }
}
