using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;
using ChinhTa.Desktop;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;

public static class TechnicalTextDesktopChecks
{
    public static void Run(Action<bool,string> assert)
    {
        string folder=Path.Combine(Path.GetTempPath(),"ChinhTa-technical-fixture-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try
        {
            string seed=OfficeFocusedCheck.CreateSeed(Path.Combine("samples","BaDuongKeThieu.docx"),folder);
            byte[] sourceHash=SHA256.HashData(File.ReadAllBytes(seed));
            Dictionary<string,byte[]> Parts(string path)
            {
                using var zip=ZipFile.OpenRead(path);return zip.Entries.ToDictionary(e=>e.FullName,e=>
                {using var stream=e.Open();using var bytes=new MemoryStream();stream.CopyTo(bytes);return bytes.ToArray();});
            }
            var before=Parts(seed);string fixture=OfficeTechnicalTextCheck.Create(seed,folder);var after=Parts(fixture);
            using(var doc=WordprocessingDocument.Open(fixture,false))
                assert(!new OpenXmlValidator().Validate(doc).Any(),"The independent technical-text Word fixture is schema-valid without starting Word");
            assert(sourceHash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(seed))) &&
                before.Keys.Order().SequenceEqual(after.Keys.Order()) && before.Where(p=>p.Key!="word/document.xml").All(p=>after[p.Key].SequenceEqual(p.Value)),
                "Building the technical-text fixture leaves the seed bytes and every non-main package part unchanged");
            XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";XElement body;
            using(var zip=ZipFile.OpenRead(fixture))using(var stream=zip.GetEntry("word/document.xml")!.Open())body=XDocument.Load(stream).Root!.Element(w+"body")!;
            string[] rows=body.Elements(w+"p").Select(p=>string.Concat(p.Descendants(w+"t").Select(t=>t.Value))).ToArray();
            assert(rows.SequenceEqual(new[]{OfficeTechnicalTextCheck.Prose}.Concat(OfficeTechnicalTextCheck.TechnicalRows).Concat(OfficeTechnicalTextCheck.FormRows).Concat(OfficeTechnicalTextCheck.ParagraphEndRows).Append(OfficeTechnicalTextCheck.ValidTechnical)) &&
                body.Elements(w+"sectPr").Count()==1 && body.Elements().Last().Name==w+"sectPr",
                "The fixture preserves literal paths, repeated filename words and significant double spaces in separate paragraphs with a retained section");
            var paragraphs=new List<Paragraph>();int offset=200;
            foreach(string row in rows){paragraphs.Add(new(offset,row+"\r"));offset+=row.Length+1;}
            var issues=Checker.Check(paragraphs,DocumentProfile.Personal);var model=new IssueReviewModel(issues);model.All.IsChecked=true;
            var selected=model.Choices.Where(c=>c.IsChecked).Select(c=>c.Issue).ToArray();
            var expectedSelected=3+OfficeTechnicalTextCheck.ParagraphEndRows.Count;
            assert(selected.Length==expectedSelected && model.Choices.Count(c=>!c.Issue.CanFix && !c.IsChecked)==3 &&
                selected.Count(i=>i.Kind==FixKind.ParagraphEndSpace)==OfficeTechnicalTextCheck.ParagraphEndRows.Count &&
                selected.Where(i=>i.Kind!=FixKind.ParagraphEndSpace).All(i=>i.Start>=paragraphs[0].Start && (long)i.Start+i.Length<=paragraphs[0].Start+OfficeTechnicalTextCheck.Prose.Length),
                "Select all chooses only ordinary prose and proven trailing-gap repairs, leaving technical warnings and intentional form blanks untouched");
            string story=string.Concat(paragraphs.Select(p=>p.Text));int origin=paragraphs[0].Start;
            var plan=BatchPlan.Create(selected);string expected=OfficeTechnicalTextCheck.ExpectedText(story);
            assert(AtomicTextBatch.ExpectedStory(plan,story,i=>story[..(i.Start-origin)])==expected &&
                OfficeTechnicalTextCheck.TechnicalRows.Concat(OfficeTechnicalTextCheck.FormRows).All(row=>expected.Contains(row+"\r",StringComparison.Ordinal)),
                "The mixed review plan changes only prose while keeping technical literals and blank checkbox/date fields byte-for-byte in the expected story");
            assert(OfficeTechnicalTextCheck.FormRows.All(row=>!Checker.Check([new(0,row)],DocumentProfile.Personal).Any(i=>i.CanFix && !i.IsStyleOnly)),
                "The actual Word fixture keeps square checkbox width and empty slash-date components out of mechanical text repairs");
            var old=Checker.Check([new(0,"Nội dung  báo cáo.")],DocumentProfile.Personal).Where(i=>i.CanFix).ToArray();
            var current=Checker.Check([new(0,@"Nội dung  báo cáo. C:\Tep\mau.docx")],DocumentProfile.Personal);
            bool stale=false;try{CurrentTextPlan.Validate(old,current);}catch(InvalidOperationException){stale=true;}
            assert(stale && old.All(i=>i.Original=="  ") && current.All(i=>!i.CanFix),
                "An unchanged old whitespace span loses edit permission when its paragraph acquires technical context before writing");
            var properties=body.Elements(w+"p").First().Elements(w+"r").First().Element(w+"rPr")!;
            assert((string?)properties.Element(w+"b")?.Attribute(w+"val")=="1" && (string?)properties.Element(w+"u")?.Attribute(w+"val")=="single",
                "The real-Word fixture includes independent retained bold/underline text for post-edit and Undo checks");
        }
        finally{Directory.Delete(folder,true);}
    }
}
