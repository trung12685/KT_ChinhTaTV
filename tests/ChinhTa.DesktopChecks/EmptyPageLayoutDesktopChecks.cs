using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class EmptyPageLayoutDesktopChecks
{
    public static void Run(Action<bool,string> assert)
    {
        string folder=Path.Combine(Path.GetTempPath(),"ChinhTa-empty-page-fixture-"+Guid.NewGuid());Directory.CreateDirectory(folder);
        try
        {
            string source=OfficeEmptyHeaderFooterCheck.Create(Path.Combine("samples","BaDuongKeThieu.docx"),folder),candidate=Path.Combine(folder,"candidate.docx");
            byte[] sourceHash=SHA256.HashData(File.ReadAllBytes(source));
            XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            XDocument Read(string path,string name){using var zip=ZipFile.OpenRead(path);using var stream=zip.GetEntry(name)!.Open();return XDocument.Load(stream,LoadOptions.PreserveWhitespace);}
            var xml=Read(source,"word/document.xml");var package=PageNumberPackage.FromFile(source);
            var section=xml.Descendants(w+"body").Single().Element(w+"sectPr")!;
            assert(section.Elements().Count(e=>e.Name==w+"headerReference" || e.Name==w+"footerReference")==2 &&
                EmptyHeaderFooterScope.HasReferences(xml) && EmptyHeaderFooterScope.Proven(xml,package),
                "The actual empty-page Office fixture proves both referenced default parts genuinely empty");
            var styles=Read(source,"word/styles.xml");
            assert(styles.Descendants(w+"style").Any(e=>(string?)e.Attribute(w+"styleId")=="EmptyPageProof" &&
                (string?)e.Element(w+"basedOn")?.Attribute(w+"val")=="Normal") && styles.Root!.Element(w+"docDefaults") is not null,
                "The empty Office parts bind an explicit safe style chain and document defaults instead of a machine template");
            var observed=PageLayoutGuard.Read(xml,new(1,0,"N",0,0,0,0,0,0))!;
            assert(PageLayoutGuard.Eligible(xml,observed,package) && observed.WidthMm>210 && observed.TopMm<20 && observed.LeftMm<30,
                "The actual blank-reference fixture qualifies for the intended bad paper/top/left margin repair");
            var fix=PageLayoutFix.Propose(observed with {RepairEligible=true})!;
            DocxPageLayoutEditor.Write(source,candidate,fix);DocxPageLayoutEditor.Verify(source,candidate,fix);
            assert(sourceHash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(source))),"Writing the blank-reference C32 candidate preserves the original Office fixture bytes");
            using(var before=ZipFile.OpenRead(source))using(var after=ZipFile.OpenRead(candidate))
            {
                byte[] Bytes(ZipArchiveEntry entry){using var stream=entry.Open();using var bytes=new MemoryStream();stream.CopyTo(bytes);return bytes.ToArray();}
                assert(before.Entries.Count==after.Entries.Count && before.Entries.Where(e=>e.FullName!="word/document.xml").All(e=>
                    after.GetEntry(e.FullName) is { } other && Bytes(e).SequenceEqual(Bytes(other))),
                    "C32 changes no header/footer/style/relationship/content-type bytes in the actual Office candidate");
            }
            var corrected=Read(candidate,"word/document.xml");
            var actual=PageLayoutGuard.Read(corrected,observed)!;
            assert(PageLayoutFix.Matches(fix.After,actual) && EmptyHeaderFooterScope.Proven(corrected,PageNumberPackage.FromFile(candidate)),
                "The corrected Office candidate has the approved A4/margins and retains the proven empty references");
            using(var opened=DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(candidate,false))
            {
                var validator=new DocumentFormat.OpenXml.Validation.OpenXmlValidator();var main=opened.MainDocumentPart!;
                var roots=main.HeaderParts.Select(p=>(DocumentFormat.OpenXml.OpenXmlElement)p.Header!).Concat(main.FooterParts.Select(p=>(DocumentFormat.OpenXml.OpenXmlElement)p.Footer!))
                    .Append(main.StyleDefinitionsPart!.Styles!).Append(main.Document.Body!.Elements<DocumentFormat.OpenXml.Wordprocessing.SectionProperties>().Single());
                var errors=roots.SelectMany(root=>validator.Validate(root)).ToArray();
                assert(errors.Length==0,"The actual empty header/footer/style/section candidate satisfies OpenXML schema: "+string.Join("; ",errors.Select(e=>e.Description)));
            }
        }
        finally{Directory.Delete(folder,true);}
    }
}
