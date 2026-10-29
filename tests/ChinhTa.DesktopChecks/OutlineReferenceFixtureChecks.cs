using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;
using ChinhTa.Desktop;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;

public static class OutlineReferenceFixtureChecks
{
    public static void Run(Action<bool,string> assert)
    {
        string folder=Path.Combine(Path.GetTempPath(),"ChinhTa-outline-reference-fixture-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            string modern=OfficeFocusedCheck.CreateSeed(Path.Combine("samples","BaDuongKeThieu.docx"),folder);
            string seed=OfficeComponentCheck.Create(modern,folder,false);
            byte[] seedHash=SHA256.HashData(File.ReadAllBytes(seed));
            Dictionary<string,byte[]> Parts(string path)
            {
                using var zip=ZipFile.OpenRead(path);
                return zip.Entries.ToDictionary(e=>e.FullName,e=>
                {using var stream=e.Open();using var bytes=new MemoryStream();stream.CopyTo(bytes);return bytes.ToArray();});
            }
            var before=Parts(seed);
            foreach(var reference in OfficeHierarchicalOutlineCheck.ReferenceRows.Select((text,index)=>(text,index)))
            {
                string path=OfficeHierarchicalOutlineCheck.Create(seed,folder,reference.text,reference.index);
                var after=Parts(path);
                using(var doc=WordprocessingDocument.Open(path,false))
                    assert(!new OpenXmlValidator().Validate(doc).Any(),
                        "Every single/list/range reference fixture is schema-valid before a real Word run");
                assert(before.Keys.Order().SequenceEqual(after.Keys.Order()) &&
                    before.Where(p=>p.Key!="word/document.xml").All(p=>after[p.Key].SequenceEqual(p.Value)) &&
                    seedHash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(seed))),
                    "Each reference fixture preserves seed bytes and all non-main parts and relationships");
                XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
                using var mainStream=new MemoryStream(after["word/document.xml"]);
                var xml=XDocument.Load(mainStream);
                var textRows=xml.Root!.Element(w+"body")!.Elements(w+"p").Select(p=>string.Concat(p.Descendants()
                    .Where(e=>e.Name==w+"t" || e.Name==w+"tab").Select(e=>e.Name==w+"tab"?"\t":e.Value)))
                    .Where(t=>t.StartsWith("1. ",StringComparison.Ordinal) || t.StartsWith("2. ",StringComparison.Ordinal) ||
                        t.StartsWith("\t1.",StringComparison.Ordinal) || t.StartsWith("\t2.",StringComparison.Ordinal) || t==reference.text).ToArray();
                assert(textRows.Length==7 && textRows.Last()==reference.text,
                    "Actual DOCX rows retain reference punctuation, padding, Unicode and tab-delimited hierarchical labels");
                int cp=200;
                var rows=textRows.Select(text=>
                {var row=new Paragraph(cp,text+"\r",Outline:new(OutlineScope.PlainBody));cp+=row.Text.Length;return row;}).ToArray();
                var issues=Checker.Check(rows,DocumentProfile.State).Where(i=>i.Kind==FixKind.OutlineOrder).ToArray();
                var model=new IssueReviewModel(issues);model.All.IsChecked=true;
                var selected=model.Choices.Where(c=>c.IsChecked).Select(c=>c.Issue).ToArray();
                assert(selected.Length==1 && selected[0].OutlineContext?.Label=="2.4" &&
                    model.Choices.Any(c=>!c.Issue.CanFix && !c.IsChecked && c.Issue.Original.Contains("1.4.",StringComparison.Ordinal)),
                    "Select all keeps the referenced 1.4 group manual and allows only the independent 2.4 correction");
                string story=string.Concat(rows.Select(p=>p.Text));
                string expected=AtomicTextBatch.ExpectedStory(BatchPlan.Create(selected),story,i=>story[..(i.Start-rows[0].Start)]);
                assert(expected==story.Replace("2.4.","2.2.",StringComparison.Ordinal) &&
                    expected.Contains(reference.text+"\r",StringComparison.Ordinal),
                    "The selected fixture model preserves both referenced labels and the complete reference while correcting the unrelated group");
            }
        }
        finally{Directory.Delete(folder,true);}
    }
}
