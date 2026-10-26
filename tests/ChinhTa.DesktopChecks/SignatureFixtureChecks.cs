using System.IO;
using System.IO.Compression;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class SignatureFixtureChecks
{
    public static void Run(Action<bool,string> assert)
    {
        string folder=Path.Combine(Path.GetTempPath(),"ChinhTa-signature-fixture-"+Guid.NewGuid());Directory.CreateDirectory(folder);
        try
        {
            string path=OfficeSignatureFormatCheck.Create(Path.Combine("samples","BaDuongKeThieu.docx"),folder);
            OfficeSignatureFormatCheck.VerifyImage(path,path);
            using(var package=DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(path,false))
            {
                var errors=new DocumentFormat.OpenXml.Validation.OpenXmlValidator().Validate(package).ToArray();
                assert(errors.Length==0,"The signature fixture and inline image satisfy the OpenXML schema: "+string.Join("; ",errors.Select(e=>e.Description)));
            }
            using var zip=ZipFile.OpenRead(path);using var stream=zip.GetEntry("word/media/signature-format-fixture.png")!.Open();
            var decoder=new PngBitmapDecoder(stream,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);
            assert(decoder.Frames.Count==1 && decoder.Frames[0].PixelWidth==1 && decoder.Frames[0].PixelHeight==1,
                "The synthetic signature marker is a decodable PNG connected to an inline drawing with unchanged dimensions");
            using var doc=zip.GetEntry("word/document.xml")!.Open();var xml=XDocument.Load(doc);
            XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            var ps=xml.Descendants(w+"body").Single().Descendants(w+"p").ToArray();var live=new List<WordParagraphSnapshot>();int cp=0;
            foreach(var p in ps)
            {
                string text=string.Concat(p.Descendants(w+"t").Select(t=>t.Value))+"\r";
                bool cell=p.Ancestors(w+"tc").Any();if(cell && !p.ElementsAfterSelf(w+"p").Any())text+="\a";
                live.Add(new(cp,cp+text.Length,text,1,cell,true));cp+=text.Length;
            }
            var roles=new Dictionary<int,OutlineParagraphInfo>();
            WordTextProof.Read(xml.ToString(),live,onOutline:(start,info)=>roles[start]=info);
            assert(roles.Values.Count(i=>i.Scope==OutlineScope.Signature)==2,
                "The packaged Office fixture recognizes both signing roles with a separate image-only paragraph");
        }
        finally{Directory.Delete(folder,true);}
    }
}
