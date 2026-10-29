using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;
using ChinhTa.Desktop;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;

public static class RetainedMarkFixtureChecks
{
    public static void Run(Action<bool,string> assert)
    {
        string folder=Path.Combine(Path.GetTempPath(),"ChinhTa-retained-mark-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            string seed=OfficeFocusedCheck.CreateSeed(Path.Combine("samples","BaDuongKeThieu.docx"),folder);
            byte[] digest=SHA256.HashData(File.ReadAllBytes(seed));
            Dictionary<string,byte[]> Parts(string path)
            {
                using var zip=ZipFile.OpenRead(path);
                return zip.Entries.ToDictionary(e=>e.FullName,e=>
                {using var stream=e.Open();using var bytes=new MemoryStream();stream.CopyTo(bytes);return bytes.ToArray();});
            }
            var before=Parts(seed);string fixture=OfficeRetainedMarkCheck.Create(seed,folder);var after=Parts(fixture);
            using(var doc=WordprocessingDocument.Open(fixture,false))
                assert(!new OpenXmlValidator().Validate(doc).Any(),"The mark-insertion fixture is schema-valid without launching Word");
            assert(digest.SequenceEqual(SHA256.HashData(File.ReadAllBytes(seed))) && before.Keys.Order().SequenceEqual(after.Keys.Order()) &&
                before.Where(p=>p.Key!="word/document.xml").All(p=>after[p.Key].SequenceEqual(p.Value)),
                "Creating the retained-mark fixture preserves seed bytes and every non-main package part and relationship");
            XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            using var main=new MemoryStream(after["word/document.xml"]);var body=XDocument.Load(main).Root!.Element(w+"body")!;
            var elements=body.Elements(w+"p").ToArray();
            assert(elements.Length==OfficeRetainedMarkCheck.Samples.Count && body.Elements().Last().Name==w+"sectPr",
                "Each mark-insertion sample occupies a separate plain paragraph with its retained section");
            foreach(var item in OfficeRetainedMarkCheck.Samples.Select((sample,index)=>(sample,index)))
            {
                var sample=item.sample;var row=elements[item.index];
                string text=string.Concat(row.Descendants(w+"t").Select(t=>t.Value));
                var markRun=row.Elements(w+"r").Single(run=>run.Element(w+"t")?.Value==sample.Mark.ToString());
                var markProperties=markRun.Element(w+"rPr")!;
                assert(text==sample.Source && (string?)markProperties.Element(w+"sz")?.Attribute(w+"val")=="28" &&
                    (string?)markProperties.Element(w+"b")?.Attribute(w+"val")=="1" &&
                    (string?)markProperties.Element(w+"u")?.Attribute(w+"val")=="single",
                    "The actual DOCX retains a separate 14pt bold underlined punctuation run between ordinary 13pt text");
                const int cp=357;var issues=Checker.Check([new(cp,text+"\r")],DocumentProfile.Personal);
                var model=new IssueReviewModel(issues);model.All.IsChecked=true;
                var plan=model.Choices.Where(c=>c.IsChecked).Select(c=>c.Issue).ToArray();
                assert(plan.Length==1 && plan[0].Kind==sample.Kind && plan[0].Original==sample.Mark.ToString() &&
                    plan[0].Replacement==sample.Mark+" " &&
                    AtomicTextBatch.ExpectedStory(plan,text,i=>text[..(i.Start-cp)])==sample.Expected,
                    "The real fixture yields one exact mark-anchor insertion through review selection without extra changes");
                var styles=new List<ObservedCharacterStyle?>();
                foreach(var run in row.Elements(w+"r"))
                {
                    bool decorated=ReferenceEquals(run,markRun);string value=run.Element(w+"t")!.Value;
                    styles.AddRange(value.Select(c=>char.IsWhiteSpace(c)?null:
                        new ObservedCharacterStyle(new("Times New Roman",decorated?14:13,decorated?-1:0,0,0),decorated?1:0)));
                }
                var source=new CharacterStyleEvidence(text,styles,new(0,0,0,0,new(0,0,4,18,0,0)));
                var probe=CasePreservationPlan.Regions(plan,[new(cp,cp+text.Length+1,text+"\r",1,false,true)]).Single();
                var expected=CasePreservationPlan.Expected(probe,plan,source,i=>text[..(i.Start-cp)]);
                int mark=expected.Text.IndexOf(sample.Mark);
                assert(probe.Kind==FixKind.PunctuationSpace && expected.Text==sample.Expected && expected.Paragraph==source.Paragraph &&
                    expected.Styles[mark] is {Underline:1,Font:{Size:14,Bold:-1}},
                    "The actual selected fixture plan creates a complete paragraph fingerprint that retains the source mark style");
                var drift=expected.Styles.ToArray();drift[mark]=drift[mark]! with {Underline=0,Font=drift[mark]!.Font with {Size=13,Bold=0}};
                assert(!expected.Matches(new(expected.Text,drift,source.Paragraph)),
                    "Correct inserted text cannot certify success after losing the actual fixture mark's size, bold or underline");
            }
        }
        finally{Directory.Delete(folder,true);}
    }
}
