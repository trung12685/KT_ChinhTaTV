using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;
using ChinhTa.Desktop;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;

public static class AnnotatedTextFixtureChecks
{
    public static void Run(Action<bool,string> assert)
    {
        string folder=Path.Combine(Path.GetTempPath(),"ChinhTa-annotated-fixture-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try
        {
            string seed=OfficeFocusedCheck.CreateSeed(Path.Combine("samples","BaDuongKeThieu.docx"),folder);
            byte[] digest=SHA256.HashData(File.ReadAllBytes(seed));
            Dictionary<string,byte[]> Parts(string path)
            {
                using var zip=ZipFile.OpenRead(path);
                return zip.Entries.ToDictionary(e=>e.FullName,e=>
                {using var input=e.Open();using var bytes=new MemoryStream();input.CopyTo(bytes);return bytes.ToArray();});
            }
            var before=Parts(seed);string fixture=OfficeAnnotatedTextCheck.Create(seed,folder);var after=Parts(fixture);
            using(var doc=WordprocessingDocument.Open(fixture,false))
                assert(!new OpenXmlValidator().Validate(doc).Any(),"The bookmark Word fixture is schema-valid without starting Microsoft Word");
            assert(digest.SequenceEqual(SHA256.HashData(File.ReadAllBytes(seed))) && before.Keys.Order().SequenceEqual(after.Keys.Order()) &&
                before.Where(pair=>pair.Key!="word/document.xml").All(pair=>after[pair.Key].SequenceEqual(pair.Value)),
                "Creating the bookmark fixture preserves seed bytes and every unrelated package part and relationship");
            XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            using var stream=new MemoryStream(after["word/document.xml"]);var xml=XDocument.Load(stream);var body=xml.Root!.Element(w+"body")!;
            string[] rows=body.Elements(w+"p").Select(p=>string.Concat(p.Descendants(w+"t").Select(t=>t.Value))).ToArray();
            assert(rows.SequenceEqual(OfficeAnnotatedTextCheck.Rows) && body.Elements().Last().Name==w+"sectPr",
                "The real DOCX fixture has two independent ordinary controls surrounding one same-paragraph and one cross-paragraph bookmark");
            var annotations=OfficeAnnotatedTextCheck.ReadAnnotations(fixture);
            assert(annotations.Count==2 && annotations[OfficeAnnotatedTextCheck.Names[0]]==OfficeAnnotatedTextCheck.Marked &&
                annotations[OfficeAnnotatedTextCheck.Names[1]]==OfficeAnnotatedTextCheck.CrossFirst+"\r"+OfficeAnnotatedTextCheck.CrossLast,
                "Bookmark evidence captures exact names and covered text including the actual paragraph boundary");
            var paragraphs=new List<Paragraph>();int cp=211;
            foreach(string row in rows){paragraphs.Add(new(cp,row+"\r"));cp+=row.Length+61;}
            var native=paragraphs.Select(row=>new WordParagraphSnapshot(row.Start,row.Start+row.Text.Length,row.Text,1,false,true)).ToArray();
            var outline=new Dictionary<int,OutlineParagraphInfo>();var format=new HashSet<int>();
            var editable=WordTextProof.Read(xml.ToString(),native,onOutline:(start,value)=>outline[start]=value,
                onFormatEditable:(start,value)=>{if(value)format.Add(start);});
            assert(editable.SetEquals([paragraphs[0].Start,paragraphs[4].Start]) &&
                WordTextProof.Read(WordXmlSnapshot.Parse(xml.ToString()),native).SetEquals(editable),
                "Both production XML proof paths retain exact native starts and exclude only the three bookmarked paragraphs");
            foreach(var profile in Enum.GetValues<DocumentProfile>())
            {
                var current=Checker.Check(WordTextProof.Apply(paragraphs,editable,outline,format),profile);
                var model=new IssueReviewModel(current);model.All.IsChecked=true;
                var selected=model.Choices.Where(choice=>choice.IsChecked).Select(choice=>choice.Issue).ToArray();
                assert(selected.Length==2 && selected.All(issue=>issue.Kind==FixKind.Text && issue.Original=="  " && issue.Replacement==" ") &&
                    selected[0].Start>=paragraphs[0].Start && selected[0].Start<paragraphs[1].Start && selected[1].Start>=paragraphs[4].Start &&
                    new[]{1,2,3}.All(index=>current.Any(issue=>!issue.CanFix && issue.Original==rows[index])),
                    "Select-all leaves bookmarked errors manual and chooses only independent prose in every profile: "+profile);
                var old=Checker.Check(paragraphs,profile).Where(issue=>issue.CanFix).ToArray();bool rejected=false;
                try{CurrentTextPlan.Validate(old,current);}catch(InvalidOperationException){rejected=true;}
                assert(rejected && old.Length>selected.Length,
                    "A new bookmark invalidates stale plain-text repair selections before writing: "+profile);
            }
            bool Reject(XDocument value)
            {try{OfficeAnnotatedTextCheck.ReadAnnotations(value);return false;}catch(InvalidOperationException){return true;}}
            var missing=new XDocument(xml);missing.Descendants(w+"bookmarkEnd").First().Remove();
            assert(Reject(missing),"Semantic evidence refuses to certify a bookmark whose end marker was removed");
            var duplicate=new XDocument(xml);var start=duplicate.Descendants(w+"bookmarkStart").First();start.AddAfterSelf(new XElement(start));
            assert(Reject(duplicate),"Semantic evidence refuses ambiguous duplicate bookmark names or identities");
            var reversed=new XDocument(xml);var first=reversed.Descendants(w+"bookmarkStart").First();var end=reversed.Descendants(w+"bookmarkEnd").First();
            end.Remove();first.AddBeforeSelf(end);
            assert(Reject(reversed),"Semantic evidence refuses a bookmark end appearing before its start");
            var renamed=new XDocument(xml);renamed.Descendants(w+"bookmarkStart").First().SetAttributeValue(w+"name","changed_name");
            assert(Reject(renamed),"Semantic evidence refuses a renamed or missing expected bookmark");
            var reidentified=new XDocument(xml);
            foreach(var marker in reidentified.Descendants().Where(e=>e.Name==w+"bookmarkStart" || e.Name==w+"bookmarkEnd"))
                marker.SetAttributeValue(w+"id",(string?)marker.Attribute(w+"id")=="7"?"17":"18");
            assert(OfficeAnnotatedTextCheck.ReadAnnotations(reidentified).All(pair=>annotations[pair.Key]==pair.Value),
                "Semantic evidence permits consistent Word identity renumbering while requiring names and exact covered text");
            var changed=new XDocument(xml);changed.Descendants(w+"t").Single(t=>t.Value==OfficeAnnotatedTextCheck.CrossLast).Value=OfficeAnnotatedTextCheck.CrossLast.Replace("  "," ",StringComparison.Ordinal);
            assert(OfficeAnnotatedTextCheck.ReadAnnotations(changed)[OfficeAnnotatedTextCheck.Names[1]]!=annotations[OfficeAnnotatedTextCheck.Names[1]],
                "Semantic evidence detects correct-looking whitespace changes inside the protected cross-paragraph range");
        }
        finally{Directory.Delete(folder,true);}
    }
}
