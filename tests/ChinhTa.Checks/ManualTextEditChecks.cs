using System.IO.Compression;
using System.Xml.Linq;
using ChinhTa.Core;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;

public static class ManualTextEditChecks
{
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main",P="http://schemas.microsoft.com/office/2006/xmlPackage",R="http://schemas.openxmlformats.org/package/2006/relationships";
    private const string Text="Người dùng tự sửa nội dung này.";
    private static XElement Part(string name,string type,XElement root)=>new(P+"part",new XAttribute(P+"name","/"+name),new XAttribute(P+"contentType",type),new XElement(P+"xmlData",root));
    internal static string Fixture(Action<XElement>? change=null)
    {
        var main=new XElement(W+"document",new XElement(W+"body",
            new XElement(W+"p",new XElement(W+"pPr",new XElement(W+"jc",new XAttribute(W+"val","both"))),
                new XElement(W+"r",new XElement(W+"rPr",new XElement(W+"b"),new XElement(W+"color",new XAttribute(W+"val","FF0000"))),new XElement(W+"t",Text))),
            new XElement(W+"p",new XElement(W+"r",new XElement(W+"t","Đoạn khác giữ nguyên."))),new XElement(W+"sectPr")));
        var root=new XElement(P+"package",Part("_rels/.rels","application/vnd.openxmlformats-package.relationships+xml",
            new XElement(R+"Relationships",new XElement(R+"Relationship",new XAttribute("Id","main"),new XAttribute("Type","http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"),new XAttribute("Target","word/document.xml")))),
            Part("word/document.xml","application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml",main),
            Part("customXml/item1.xml","application/xml",new XElement("untouched",new XAttribute("value","giữ nguyên"))),
            new XElement(P+"part",new XAttribute(P+"name","/customXml/data.bin"),new XAttribute(P+"contentType","application/octet-stream"),new XElement(P+"binaryData",Convert.ToBase64String([0,1,128,255]))));
        change?.Invoke(root);return root.ToString(SaveOptions.DisableFormatting);
    }
    private static WordParagraphSnapshot[] Live(string first=Text)=>[new(0,first.Length+1,first+"\r",1,false,false),new(first.Length+1,first.Length+23,"Đoạn khác giữ nguyên.\r",1,false,false)];
    private static bool Reject(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
    public static void Run(Action<bool,string> assert)
    {
        string flat=Fixture();var live=Live();live[1]=live[1] with {End=live[1].Start+live[1].Text.Length};
        ManualTextTarget Prepare(string source)=>ManualTextTarget.Prepare(source,live,6,4,"dùng");
        var target=Prepare(flat);assert(target.Original==Text && target.Start==0 && target.SourceKey.Length==64,"Manual target binds the actual XML/native paragraph and complete source, independent of an automatic suggestion");
        string folder=Path.Combine(Path.GetTempPath(),"ChinhTa-manual-"+Guid.NewGuid());Directory.CreateDirectory(folder);
        try
        {
            int count=0;
            foreach(string edited in new[]{Text.Replace("này","mới",StringComparison.Ordinal),Text.Replace("nội dung","nội dung bổ sung",StringComparison.Ordinal),Text.Replace(" tự","",StringComparison.Ordinal),"Bản sửa hoàn toàn mới."," "+Text+" "})
            {
                var plan=target.Edit(edited);string path=Path.Combine(folder,(count++)+".docx");plan.WriteNew(path);
                string actual=Fixture(root=>root.Descendants(W+"t").First().Value=edited);plan.VerifyLive(actual);
                using var zip=ZipFile.OpenRead(path);using var input=zip.GetEntry("word/document.xml")!.Open();var xml=XDocument.Load(input,LoadOptions.PreserveWhitespace);
                assert(xml.Descendants(W+"t").First().Value==edited && xml.Descendants(W+"b").Count()==1 && xml.Descendants(W+"color").Single().Attribute(W+"val")!.Value=="FF0000",
                    "User replacement/deletion/insertion preserves the edited run's exact style and unchanged paragraph structure");
                using var binary=zip.GetEntry("customXml/data.bin")!.Open();using var bytes=new MemoryStream();binary.CopyTo(bytes);
                assert(bytes.ToArray().SequenceEqual(new byte[]{0,1,128,255}),"Manual candidate preserves every binary byte outside the edit");
                using var package=WordprocessingDocument.Open(path,false);var errors=new OpenXmlValidator().Validate(package).ToArray();
                assert(errors.Length==0,"The manually authored candidate is schema-valid: "+string.Join("; ",errors.Take(3).Select(e=>e.Description)));
                assert(plan.ExpectedStory("prefix"+Text+"\rsuffix","prefix")=="prefix"+edited+"\rsuffix","Manual expected story retains the actual prefix, paragraph delimiter and suffix");
                bool existing=false;try{plan.WriteNew(path);}catch(IOException){existing=true;}
                assert(existing,"Manual candidate publication cannot overwrite an existing file");
            }
            foreach(string edited in new[]{"",Text+"\n",Text+"\r",Text+"\t",Text+"\a",Text+"😀",Text+"\u202e",new string('a',16001),Text})
                assert(Reject(()=>target.Edit(edited)),"Manual input rejects structural/control/unbounded text and unchanged submissions without writing a candidate");
            assert(Reject(()=>target.RequireCurrent(Fixture(root=>root.Descendants(W+"t").Last().Value+=" thay đổi"))),"A stale complete Word snapshot cannot authorize a manual write even when the selected text still matches");
            assert(Reject(()=>ManualTextTarget.Prepare(flat,live,6,4,"sai!")),"Manual range cannot be rebound by similar text or a stale issue CP");
            assert(Reject(()=>ManualTextTarget.Prepare(flat,live,Text.Length-1,3,".\rĐ")),"A manual target cannot span paragraph boundaries");
            string split=Fixture(root=>{var t=root.Descendants(W+"t").First();t.Value=Text[..12];t.Parent!.AddAfterSelf(new XElement(W+"r",new XElement(W+"rPr",new XElement(W+"i")),new XElement(W+"t",Text[12..])));});
            var mixed=Prepare(split);var one=mixed.Edit(Text.Replace("Người","Ngài",StringComparison.Ordinal));
            one.VerifyLive(Fixture(root=>{var t=root.Descendants(W+"t").First();t.Value=Text[..12].Replace("Người","Ngài",StringComparison.Ordinal);t.Parent!.AddAfterSelf(new XElement(W+"r",new XElement(W+"rPr",new XElement(W+"i")),new XElement(W+"t",Text[12..])));}));
            assert(true,"Editing one run preserves a differently styled neighboring run");
            assert(Reject(()=>mixed.Edit("Mới hoàn toàn.")),"An edit crossing differently styled text nodes must be split into explicit user actions");
            foreach(var mutation in new Action<XElement>[] {
                root=>root.Descendants(W+"t").Last().Value="Đoạn khác đã bị đổi.",
                root=>root.Descendants(W+"b").Remove(),
                root=>root.Descendants(W+"jc").Single().SetAttributeValue(W+"val","left"),
                root=>root.Descendants(P+"binaryData").Single().Value=Convert.ToBase64String(new byte[]{1,1,128,255}),
                root=>root.Elements(P+"part").Last().Remove()})
                assert(Reject(()=>target.Edit(Text+" thêm").VerifyLive(Fixture(root=>{root.Descendants(W+"t").First().Value=Text+" thêm";mutation(root);}))),
                    "Post-save verification rejects changed neighboring text, style, layout, binary payload or missing package parts");
            foreach(var mutation in new Action<XElement>[] {
                root=>root.Descendants(W+"p").First().AddFirst(new XElement(W+"bookmarkStart",new XAttribute(W+"id",1),new XAttribute(W+"name","keep"))),
                root=>root.Descendants(W+"r").First().Add(new XElement(W+"tab")),
                root=>root.Descendants(W+"pPr").First().Add(new XElement(W+"numPr")),
                root=>root.Descendants(W+"body").First().Add(new XElement(W+"ins")),
                root=>root.Descendants(W+"rPr").First().Add(new XElement(W+"rPrChange"))})
                assert(Reject(()=>Prepare(Fixture(mutation))),"Unproven controls, fields, numbering, annotations or revision history do not become editable through manual mode");
            var spell=Prepare(Fixture(root=>{var p=root.Descendants(W+"p").First();p.AddFirst(new XElement(W+"proofErr",new XAttribute(W+"type","spellStart")));p.Add(new XElement(W+"proofErr",new XAttribute(W+"type","spellEnd")));}));
            spell.Edit(Text+" thêm").VerifyLive(Fixture(root=>root.Descendants(W+"t").First().Value=Text+" thêm"));
            assert(true,"Word may recompute empty spelling markers without changing text or formatting during a manual edit");
            target.RequireCurrent(XDocument.Parse(flat).ToString());
            assert(true,"Serialization-only changes cannot discard a manual draft when every semantic package part still matches");
            string customSpace=Fixture(root=>root.Descendants("untouched").Single().Add(" "));
            var custom=Prepare(customSpace).Edit(Text+" thêm");
            assert(Reject(()=>custom.VerifyLive(Fixture(root=>{root.Descendants(W+"t").First().Value=Text+" thêm";}))),
                "Whitespace-only user XML data is not mistaken for ignorable Word indentation");
            string escaped=Text+" <{namespace}t>literal</{namespace}t>";
            var literal=target.Edit(escaped);literal.VerifyLive(Fixture(root=>root.Descendants(W+"t").First().Value=escaped));
            assert(Reject(()=>literal.VerifyLive(Fixture(root=>root.Descendants(W+"t").First().Value=Text))),
                "Structural XML comparison cannot collide with user text containing markup-like delimiters");
            assert(Reject(()=>target.Edit(Text+" thêm").ExpectedStory("wrong"+Text,"prefix")),"A manually edited story must match the actual whole prefix before publication");
        }
        finally{Directory.Delete(folder,true);}
        foreach(string stage in new[]{"write","verify"})
        {
            bool restored=false,committed=false;
            try{ManualEditTransaction.Apply(()=>{if(stage=="write")throw new IOException("write");},()=>throw new IOException("verify"),()=>committed=true,()=>restored=true);}catch(IOException){}
            assert(restored && !committed,"A failed manual write or late verification restores the whole edit without publishing Undo");
        }
        bool dual=false;try{ManualEditTransaction.Apply(()=>throw new IOException("primary"),()=>{},()=>{},()=>throw new IOException("restore"));}catch(AggregateException e){dual=e.InnerExceptions.Count==2;}
        assert(dual,"Manual edit recovery failure retains both causes instead of reporting successful restoration");
        var steps=new List<string>();ManualEditTransaction.Apply(()=>steps.Add("write"),()=>steps.Add("verify"),()=>steps.Add("commit"),()=>steps.Add("restore"));
        assert(steps.SequenceEqual(new[]{"write","verify","commit"}),"Manual edits enter Undo only after complete verification and never run restore after success");
    }
}
