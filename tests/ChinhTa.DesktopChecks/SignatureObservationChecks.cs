using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Xml.Linq;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class SignatureObservationChecks
{
    public sealed class Information(bool cell)
    {
        public int Reads {get;private set;}
        public object this[int kind]{get{Reads++;return kind switch{12=>cell,3=>1,_=>throw new InvalidOperationException()};}}
    }
    public sealed class Layout
    {
        public int Alignment=>1;
        public float LeftIndent=>0;
        public float RightIndent=>0;
        public float FirstLineIndent=>0;
    }
    public sealed class Range(int start,string text,bool cell)
    {
        public int Start=>start;
        public int End=>start+text.Length;
        public string Text=>text;
        public Information Information {get;}=new(cell);
        public Layout ParagraphFormat {get;}=new();
    }
    public sealed record NativeParagraph(Range Range);
    public sealed class Collection(NativeParagraph[] rows):IEnumerable
    {
        public object At(int ordinal)=>rows[ordinal-1];
        public IEnumerator GetEnumerator()=>rows.GetEnumerator();
    }
    public static void Run(Action<bool,string> assert)
    {
        string folder=Path.Combine(Path.GetTempPath(),"ChinhTa-signature-observation-"+Guid.NewGuid());Directory.CreateDirectory(folder);
        try
        {
            XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            foreach(int padding in new[]{0,60})
            foreach(bool beforeName in new[]{false,true})
            {
                string target=Path.Combine(folder,padding+"-"+beforeName);Directory.CreateDirectory(target);
                string source=OfficeSignatureFormatCheck.Create(Path.Combine("samples","BaDuongKeThieu.docx"),target,padding,beforeName);
                using(var package=DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(source,false))
                    assert(!new DocumentFormat.OpenXml.Validation.OpenXmlValidator().Validate(package).Any(),
                        "Short and long Office signature fixtures remain schema-valid, including the image relationship");
                OfficeSignatureFormatCheck.VerifyImage(source,source);
                using var zip=ZipFile.OpenRead(source);using var stream=zip.GetEntry("word/document.xml")!.Open();var xml=XDocument.Load(stream);
                var elements=xml.Descendants(w+"body").Single().Descendants(w+"p").Where(p=>!p.Ancestors(w+"txbxContent").Any()).ToArray();
                foreach(bool optimized in new[]{false,true})
                {
                    int cp=0;
                    var native=elements.Select(p=>{
                        bool cell=p.Ancestors(w+"tc").Any();
                        string text=string.Concat(p.Descendants().Where(e=>e.Parent?.Name==w+"r" && e.Ancestors(w+"p").First()==p)
                            .Select(e=>e.Name==w+"t"?e.Value:e.Name==w+"br"?"\v":""));
                        if(p.Descendants(w+"drawing").Any())text+="\u0001";
                        text+="\r"+(cell && !p.ElementsAfterSelf(w+"p").Any()?"\a":"");
                        var value=new NativeParagraph(new(cp,text,cell));cp+=text.Length;return value;
                    }).ToArray();
                    using var session=new WordSession{Profile=DocumentProfile.State,UseOptimizedReading=optimized};
                    var read=typeof(WordSession).GetMethod("ReadNativeParagraph",BindingFlags.Instance|BindingFlags.NonPublic)!;
                    var collection=new Collection(native);var measures=new ParagraphTraversalMeasurements();
                    (Paragraph Value,WordParagraphSnapshot Native) Observe(object paragraph,int ordinal)
                    {
                        var result=read.Invoke(session,[paragraph,ordinal])!;var type=result.GetType();
                        return((Paragraph)type.GetProperty("Value")!.GetValue(result)!,(WordParagraphSnapshot)type.GetProperty("Native")!.GetValue(result)!);
                    }
                    var observed=NativeParagraphTraversal.Read(collection,native.Length,collection.At,Observe,_=>{},optimized,
                        entries=>entries.Select((p,i)=>i==0 || p.Native.Start>=entries[i-1].Native.End).All(v=>v),measures);
                    var outline=new Dictionary<int,OutlineParagraphInfo>();var formats=new HashSet<int>();var warnings=new List<string>();
                    void Describe(int start,OutlineParagraphInfo info)=>outline[start]=info;
                    void Format(int start,bool can){if(can)formats.Add(start);}
                    var live=observed.Select(x=>x.Native).ToArray();
                    var editable=optimized?WordTextProof.Read(WordXmlSnapshot.Parse(xml.ToString()),live,e=>warnings.Add(e.DiagnosticCode),Describe,Format):
                        WordTextProof.Read(xml.ToString(),live,e=>warnings.Add(e.DiagnosticCode),Describe,Format);
                    var rows=WordTextProof.Apply(observed.Select(x=>x.Value),editable,outline,formats);
                    var signatures=SignatureRules.ObservationTargets(rows,DocumentProfile.State);
                    assert(signatures.Count==2,"The actual desktop native observer + current XML proof recognizes both signing roles in either reader mode");
                    var objectRow=live.Single(p=>p.Text.Contains('\u0001'));
                    assert(warnings.Count==0 && !editable.Contains(objectRow.Start) && !formats.Contains(objectRow.Start),
                        "A schema-valid inline image before or after the name matches actual object CPs without granting any image-paragraph edits");
                    var inventory=DocxLineInventory.ReadXml(xml,live.Take(40).ToArray());
                    assert(!inventory.HasUnmappedShapes && inventory.Lines.Count==0,
                        "A mapped embedded image does not become a Line or an unrelated mapping failure that disables the header's three Line proposals");
                    if(padding>0)
                    {
                        assert(signatures.All(p=>!p.InTable && p.Page==0 && Array.FindIndex(native,n=>n.Range.Start==p.Start)>=40),
                            "Long signing roles are independently proven despite the actual observer's unobserved table/page placeholders after40");
                        assert(native.Skip(40).All(p=>p.Range.Information.Reads==0),
                            "Signature recovery uses current XML role evidence without adding native table/page reads to every later paragraph");
                    }
                    var candidates=rows.Select(p=>signatures.Any(s=>s.Start==p.Start)?p with{Font="Arial",Color=255,Size=12,FormatObserved=true}:p).ToArray();
                    var issues=SignatureRules.Check(candidates,DocumentProfile.State);
                    assert(issues.Count==2 && issues.All(i=>i.CanFix && i.Kind==FixKind.SignatureFormat),
                        "The actual bounded observation pipeline reaches both current signature-format proposals instead of silently omitting the block");
                    var name=signatures.Single(p=>p.Outline!.SignatureRole==ComponentRole.SignatureName);
                    assert(SignatureRules.ObservationTargets(rows.Select(p=>p.Start==name.Start?p with{TextEditable=false,FormatEditable=false}:p).ToArray(),DocumentProfile.State).Count==0,
                        "An incomplete name proof still blocks both authority and signer in the actual observation pipeline");
                }
            }
        }
        finally{Directory.Delete(folder,true);}
    }
}
