using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;
using ChinhTa.Core;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;

public static class IsolatedWordPackageChecks
{
    private static readonly XNamespace P="http://schemas.microsoft.com/office/2006/xmlPackage";
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly XNamespace R="http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace Office="http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace Ct="http://schemas.openxmlformats.org/package/2006/content-types";
    private const string RelationshipType="application/vnd.openxmlformats-package.relationships+xml";
    private static XElement Part(string name,string type,XElement root)=>new(P+"part",new XAttribute(P+"name","/"+name),new XAttribute(P+"contentType",type),new XElement(P+"xmlData",root));
    private static XElement Relationship(string id,string kind,string target)=>new(R+"Relationship",new XAttribute("Id",id),new XAttribute("Type",Office.NamespaceName+"/"+kind),new XAttribute("Target",target));
    private static XElement Fixture()
    {
        var root=new XElement(P+"package",
            Part("_rels/.rels",RelationshipType,new XElement(R+"Relationships",Relationship("main","officeDocument","word/document.xml"))),
            Part("word/document.xml","application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml",
                new XElement(W+"document",new XElement(W+"body",new XElement(W+"p",new XElement(W+"r",new XElement(W+"t","Nội dung tiếng Việt."))),new XElement(W+"sectPr")))),
            Part("word/_rels/document.xml.rels",RelationshipType,new XElement(R+"Relationships",
                Relationship("style","styles","styles.xml"),Relationship("header","header","header1.xml"),Relationship("footer","footer","footer1.xml"),Relationship("image","image","media/image.png"))),
            Part("word/styles.xml","application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml",new XElement(W+"styles")),
            Part("word/header1.xml","application/vnd.openxmlformats-officedocument.wordprocessingml.header+xml",new XElement(W+"hdr",new XElement(W+"p"))),
            Part("word/footer1.xml","application/vnd.openxmlformats-officedocument.wordprocessingml.footer+xml",new XElement(W+"ftr",new XElement(W+"p"))),
            new XElement(P+"part",new XAttribute(P+"name","/word/media/image.png"),new XAttribute(P+"contentType","image/png"),new XElement(P+"binaryData",Convert.ToBase64String([0,1,2,127,128,254,255]))),
            Part("customXml/item1.xml","application/xml",new XElement("metadata",new XAttribute("kind","unchanged"),new XCData("<dữ liệu>"))));
        return root;
    }
    private static XElement Find(XElement root,string name)=>root.Elements(P+"part").Single(p=>(string?)p.Attribute(P+"name")=="/"+name);
    public static void Run(Action<bool,string> assert)
    {
        string folder=Path.Combine(Path.GetTempPath(),"ChinhTa-copy-checks-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try
        {
            var flat=Fixture();string original=flat.ToString(SaveOptions.DisableFormatting);
            var package=IsolatedWordPackage.FromFlat(original);package.RequirePassiveCalibration();
            string copy=Path.Combine(folder,"copy.docx");package.WriteNew(copy);package.VerifyFile(copy);
            assert(package.PartNames.Count==8 && package.PartNames.Contains("word/media/image.png") && package.PartNames.Contains("customXml/item1.xml"),
                "Full isolated package includes binary/custom/header/footer parts beyond PAGE's partial XML view");
            using(var zip=ZipFile.OpenRead(copy))
            {
                using var image=new MemoryStream();using(var imageInput=zip.GetEntry("word/media/image.png")!.Open())imageInput.CopyTo(image);
                assert(image.ToArray().SequenceEqual(new byte[]{0,1,2,127,128,254,255}),"Flat OPC binary payload survives every byte, including non-UTF8 values");
                using var input=zip.GetEntry("word/_rels/document.xml.rels")!.Open();var relations=XDocument.Load(input);
                assert(relations.Root!.Elements().Count()==4 && relations.Descendants(R+"Relationship").Any(e=>(string?)e.Attribute("Target")=="media/image.png"),
                    "Isolated copy retains all relationship identifiers, types and targets");
            }
            using(var doc=WordprocessingDocument.Open(copy,false))
                assert(!new OpenXmlValidator().Validate(doc).Any(),"Full Flat OPC copy opens as a schema-valid DOCX with original stories and parts");
            byte[] hash=SHA256.HashData(File.ReadAllBytes(copy));bool overwrite=false;
            try{package.WriteNew(copy);}catch(IOException){overwrite=true;}
            assert(overwrite && hash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(copy))) && !Directory.EnumerateFiles(folder,"*.tmp").Any(),
                "Existing source/candidate is never overwritten and failed publication cleans only its own temporary file");
            string second=Path.Combine(folder,"second.docx");package.WriteNew(second);package.VerifyFile(second);
            assert(original==flat.ToString(SaveOptions.DisableFormatting),"Repeated isolated copies never mutate the in-memory source Flat OPC");
            using(var zip=ZipFile.Open(second,ZipArchiveMode.Update))
            {
                var image=zip.GetEntry("word/media/image.png")!;image.Delete();using var input=zip.CreateEntry("word/media/image.png").Open();input.Write([0,1,2,127,128,254,254]);
            }
            bool tamper=false;try{package.VerifyFile(second);}catch(SpacingDiagnosticException error) when(error.Code==SpacingDiagnosticFailureCode.IntegrityFailed){tamper=true;}
            assert(tamper,"A one-byte binary drift in a Word copy is rejected even if its part size and XML still match");
            string brokenManifest=Path.Combine(folder,"broken-content-types.docx");package.WriteNew(brokenManifest);
            using(var zip=ZipFile.Open(brokenManifest,ZipArchiveMode.Update))
            {
                zip.GetEntry("[Content_Types].xml")!.Delete();
                using var output=new StreamWriter(zip.CreateEntry("[Content_Types].xml").Open());output.Write("<Types");
            }
            SpacingDiagnosticException? manifestFailure=null;
            try{package.VerifyFile(brokenManifest);}catch(SpacingDiagnosticException error){manifestFailure=error;}
            assert(manifestFailure is {Code:SpacingDiagnosticFailureCode.IntegrityFailed,InnerException:XmlException},
                "A malformed content-type manifest is an integrity failure, not a Word operation or unsupported-evidence result");
            bool fallback=false,propagated=false;
            try{SpacingDiagnosticGuard.Optional(()=>{package.VerifyFile(brokenManifest);return true;},()=>{fallback=true;return false;});}
            catch(SpacingDiagnosticException error) when(error.Code==SpacingDiagnosticFailureCode.IntegrityFailed){propagated=true;}
            assert(propagated && !fallback,"Optional spacing evidence cannot swallow a broken DOCX content-type manifest");
            void Rejected(Action<XElement> change,string description,bool passive=false)
            {
                var fixture=Fixture();change(fixture);bool rejected=false;
                try{var parsed=IsolatedWordPackage.FromFlat(fixture.ToString());if(passive)parsed.RequirePassiveCalibration();}
                catch(Exception e) when(e is InvalidOperationException or XmlException){rejected=true;}
                assert(rejected,description);
            }
            Rejected(f=>f.Add(new XElement(f.Elements().First())),"Duplicate Flat OPC part names fail before any Word process starts");
            Rejected(f=>Find(f,"word/footer1.xml").SetAttributeValue(P+"name","/WORD/HEADER1.XML"),"Case aliases cannot create two ambiguous OPC part identities");
            foreach(string name in new[]{"/../escape.xml","/word/../escape.xml","/word/%2e%2e/escape.xml","/word\\escape.xml","//word/a.xml","/word/a.xml?x","/word/a.xml#x","word/a.xml"})
                Rejected(f=>Find(f,"customXml/item1.xml").SetAttributeValue(P+"name",name),"Unsafe/ambiguous Flat OPC name is rejected: "+name);
            Rejected(f=>Find(f,"word/header1.xml").Remove(),"Missing relationship target is unknown, not an empty story");
            Rejected(f=>Find(f,"word/_rels/document.xml.rels").Descendants(R+"Relationship").First().SetAttributeValue("Target","https://other.invalid/styles.xml"),"An internal relation cannot silently become an external URL");
            Rejected(f=>Find(f,"word/_rels/document.xml.rels").Descendants(R+"Relationship").First().SetAttributeValue("Target","../../word/styles.xml"),"An internal target cannot escape above the OPC root and normalize back into a known part");
            Rejected(f=>Find(f,"word/_rels/document.xml.rels").Descendants(R+"Relationship").First().SetAttributeValue("Id","image"),"Duplicate relationship IDs fail closed");
            Rejected(f=>Find(f,"word/media/image.png").Element(P+"binaryData")!.Value="not-base64!","Malformed binary data cannot be dropped or converted into an empty part");
            Rejected(f=>Find(f,"word/styles.xml").Element(P+"xmlData")!.Add(new XElement("extra")),"Multiple XML roots are rejected rather than silently truncated");
            Rejected(f=>Find(f,"word/document.xml").Descendants(W+"r").Single().SetAttributeValue(Office+"id","missing"),"An XML relationship reference missing from its own relationship part cannot be accepted");
            Rejected(f=>Find(f,"word/document.xml").Descendants(W+"body").Single().Remove(),"A main part without a document body is not a usable isolated Word copy");
            Rejected(f=>f.Elements().First().Remove(),"A range-sized fragment without package root relationships cannot be a full document clone");
            Rejected(f=>Find(f,"word/_rels/document.xml.rels").Descendants(R+"Relationship").First().SetAttributeValue("TargetMode","external"),"Unknown relationship target modes are not guessed");
            var external=Fixture();var link=Relationship("external","hyperlink","https://example.invalid/private");link.SetAttributeValue("TargetMode","External");
            Find(external,"word/_rels/document.xml.rels").Descendants(R+"Relationships").Single().Add(link);
            var externalPackage=IsolatedWordPackage.FromFlat(external.ToString());string externalPath=Path.Combine(folder,"external.docx");externalPackage.WriteNew(externalPath);externalPackage.VerifyFile(externalPath);
            bool externalBlocked=false;try{externalPackage.RequirePassiveCalibration();}catch(InvalidOperationException){externalBlocked=true;}
            assert(externalBlocked,"External links are preserved by the container but opening them for Word calibration remains blocked");
            foreach(string type in new[]{"application/vnd.ms-office.vbaProject","application/vnd.ms-office.activeX+xml","application/vnd.openxmlformats-officedocument.oleObject"})
                Rejected(f=>Find(f,"word/media/image.png").SetAttributeValue(P+"contentType",type),"Active binary content is kept but cannot be opened for calibration: "+type,true);
            foreach(string instruction in new[]{"DDEAUTO attacker","INCLUDETEXT file","LINK Excel.Sheet","DATE","PA","GE"})
                Rejected(f=>Find(f,"word/document.xml").Descendants(W+"r").Single().Add(new XElement(W+"instrText",instruction)),"Unknown/active/split field instruction stays outside calibration: "+instruction,true);
            bool dtd=false;try{IsolatedWordPackage.FromFlat("<!DOCTYPE package [<!ENTITY value SYSTEM 'file:///etc/passwd'>]>"+original);}catch(XmlException){dtd=true;}
            assert(dtd,"Flat OPC parsing prohibits DTD/entity expansion and external XML resolution");
            var inherited=Fixture();XNamespace mc="http://schemas.openxmlformats.org/markup-compatibility/2006";
            inherited.SetAttributeValue(XNamespace.Xmlns+"w14","http://schemas.microsoft.com/office/word/2010/wordml");
            Find(inherited,"word/document.xml").Descendants(W+"document").Single().SetAttributeValue(mc+"Ignorable","w14");
            string namespaced=Path.Combine(folder,"namespaced.docx");IsolatedWordPackage.FromFlat(inherited.ToString()).WriteNew(namespaced);
            using(var zip=ZipFile.OpenRead(namespaced))using(var input=zip.GetEntry("word/document.xml")!.Open())
                assert(XDocument.Load(input).Root!.GetNamespaceOfPrefix("w14")=="http://schemas.microsoft.com/office/word/2010/wordml",
                    "Inherited namespace declarations used only in mc:Ignorable values survive standalone XML serialization");
            foreach(string sample in new[]{"BaDuongKeThieu.docx","VanBanMau.docx","VanBanDangMau.docx","VanBanCaNhanMau.docx"})
            {
                string source=Path.Combine("samples",sample);byte[] before=SHA256.HashData(File.ReadAllBytes(source));
                XElement full=new(P+"package");
                using(var zip=ZipFile.OpenRead(source))
                {
                    using var input=zip.GetEntry("[Content_Types].xml")!.Open();var manifest=XDocument.Load(input);
                    foreach(var entry in zip.Entries.Where(e=>e.FullName!="[Content_Types].xml"))
                    {
                        string type=(string?)manifest.Descendants(Ct+"Override").SingleOrDefault(e=>(string?)e.Attribute("PartName")=="/"+entry.FullName)?.Attribute("ContentType")??
                            (string)manifest.Descendants(Ct+"Default").Single(e=>(string?)e.Attribute("Extension")==Path.GetExtension(entry.FullName)[1..]).Attribute("ContentType")!;
                        using var stream=entry.Open();full.Add(Part(entry.FullName,type,XDocument.Load(stream,LoadOptions.PreserveWhitespace).Root!));
                    }
                }
                var parsed=IsolatedWordPackage.FromFlat(full.ToString(SaveOptions.DisableFormatting));parsed.RequirePassiveCalibration();
                string target=Path.Combine(folder,sample);parsed.WriteNew(target);parsed.VerifyFile(target);
                using(var sourceDoc=WordprocessingDocument.Open(source,false))using(var doc=WordprocessingDocument.Open(target,false))
                {
                    string[] Errors(WordprocessingDocument d)=>new OpenXmlValidator().Validate(d).Select(e=>e.Id+"|"+e.Description+"|"+e.Path?.XPath).Order(StringComparer.Ordinal).ToArray();
                    assert(Errors(sourceDoc).SequenceEqual(Errors(doc)),"Full clone preserves the existing sample without introducing or concealing any source schema defects: "+sample);
                }
                assert(before.SequenceEqual(SHA256.HashData(File.ReadAllBytes(source))),"Cloning leaves the existing source sample byte-for-byte unchanged: "+sample);
            }
            // Test every generated part's declared content type independently.
            using(var zip=ZipFile.OpenRead(copy))using(var input=zip.GetEntry("[Content_Types].xml")!.Open())
                assert(XDocument.Load(input).Descendants(Ct+"Override").Count()==package.PartNames.Count,"DOCX declares a content type for every preserved XML and binary part");
        }
        finally{Directory.Delete(folder,true);}
    }
}
