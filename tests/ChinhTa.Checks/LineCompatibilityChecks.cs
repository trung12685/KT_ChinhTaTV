using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using ChinhTa.Core;

static class LineCompatibilityChecks
{
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly XNamespace WP="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing";
    private static readonly XNamespace MC="http://schemas.openxmlformats.org/markup-compatibility/2006";
    private static readonly XNamespace V="urn:schemas-microsoft-com:vml";
    private static readonly XNamespace A="http://schemas.openxmlformats.org/drawingml/2006/main";
    private static readonly XNamespace WPS="http://schemas.microsoft.com/office/word/2010/wordprocessingShape";
    private static readonly XNamespace W10="urn:schemas-microsoft-com:office:word";
    public static void Run(Action<bool,string> assert)
    {
        string folder=Path.Combine(Path.GetTempPath(),"ChinhTaCompatibility-"+Guid.NewGuid());Directory.CreateDirectory(folder);
        try
        {
            string sample=Path.GetFullPath("samples/BaDuongKeThieu.docx");
            var layout=DocxLineFixture.Read(sample);
            var live=layout.Select(p=>new WordParagraphSnapshot(p.Start,p.Start+p.Text.Length+
                (p.InTable && p.CellEnd==p.Start+p.Text.Length+1?1:0),p.Text,p.Page,p.InTable,p.Centered,p.ParagraphCentered)).ToArray();
            var targets=ShapeLineRules.Identify(DocxLineFixture.ReadThroughXmlMap(sample),DocumentProfile.State);
            var plan=targets.Select(t=>t.Probe with {CanFix=true,LineFix=new(null,100+(int)t.Role*40,110+(int)t.Role*60,90,t.Role)}).ToArray();
            var agency=plan.Single(i=>i.LineFix!.Role==LineRole.Agency);
            string one=Path.Combine(folder,"one.docx");DocxLineEditor.Write(sample,one,live,targets,[agency]);
            XDocument Read(string path){using var z=ZipFile.OpenRead(path);using var s=z.GetEntry("word/document.xml")!.Open();return XDocument.Load(s,LoadOptions.PreserveWhitespace);}
            void Write(string path,XDocument xml)
            {
                File.Copy(sample,path);using var z=ZipFile.Open(path,ZipArchiveMode.Update);z.GetEntry("word/document.xml")!.Delete();
                using var s=z.CreateEntry("word/document.xml").Open();xml.Save(s,SaveOptions.DisableFormatting);
            }
            XElement Pair(XElement drawing)
            {
                var anchor=drawing.Element(WP+"anchor")!;var property=anchor.Element(WP+"docPr")!;
                double x=double.Parse(anchor.Element(WP+"positionH")!.Element(WP+"posOffset")!.Value,CultureInfo.InvariantCulture)/12700;
                double y=double.Parse(anchor.Element(WP+"positionV")!.Element(WP+"posOffset")!.Value,CultureInfo.InvariantCulture)/12700;
                double width=double.Parse(anchor.Element(WP+"extent")!.Attribute("cx")!.Value,CultureInfo.InvariantCulture)/12700;
                string Point(double h,double v)=>h.ToString("R",CultureInfo.InvariantCulture)+"pt,"+v.ToString("R",CultureInfo.InvariantCulture)+"pt";
                return new XElement(MC+"AlternateContent",new XAttribute(XNamespace.Xmlns+"shapeChoice",WPS.NamespaceName),
                    new XElement(MC+"Choice",new XAttribute("Requires","shapeChoice"),new XElement(drawing)),
                    new XElement(MC+"Fallback",new XElement(W+"pict",new XElement(V+"line",
                        new XAttribute("id",property.Attribute("name")!.Value),new XAttribute("alt",property.Attribute("descr")!.Value),
                        new XAttribute("style","position:absolute;mso-position-horizontal-relative:page;mso-position-vertical-relative:page"),
                        new XAttribute("from",Point(x,y)),new XAttribute("to",Point(x+width,y)),
                        new XAttribute("strokeweight",".5pt"),new XElement(W10+"wrap",new XAttribute("anchorx","page"),new XAttribute("anchory","page"))))));
            }
            var xml=Read(one);var original=xml.Descendants(W+"drawing").Single();original.ReplaceWith(Pair(original));
            string paired=Path.Combine(folder,"word-saved.docx");Write(paired,xml);xml=Read(paired);
            byte[] before=File.ReadAllBytes(paired);
            var inventory=DocxLineInventory.Read(paired,live);
            assert(inventory.Lines.Count==1 && !inventory.HasUnmappedShapes && inventory.Lines.Single().AppOwned,
                "Word's one DrawingML/VML compatibility pair counts as one owned Line and leaves missing regions eligible");
            assert(inventory.Lines.Single().Geometry==new LineGeometry(100,110,90),"Compatibility identity never replaces DrawingML page geometry with a VML guess");
            assert(RenderedLineVerifier.Find(inventory.Lines.Single().Geometry!,[new(185,166,90)]) is null,
                "Recognizing a compatibility pair still rejects a displaced XPS stroke");
            string added=Path.Combine(folder,"subject-added.docx");var subject=plan.Single(i=>i.LineFix!.Role==LineRole.Subject);
            DocxLineEditor.Write(paired,added,live,targets,[subject]);
            var addedXml=Read(added);
            assert(DocxLineInventory.Read(added,live).Lines.Count==2 && !DocxLineInventory.Read(added,live).HasUnmappedShapes &&
                XNode.DeepEquals(xml.Descendants(MC+"AlternateContent").Single(),addedXml.Descendants(MC+"AlternateContent").Single()),
                "Sequential Subject creation preserves the complete existing Agency compatibility pair");
            // Simulate the next Word save producing a fallback for Subject too.
            foreach(var d in addedXml.Descendants(W+"drawing").Where(d=>!d.Ancestors(MC+"AlternateContent").Any()).ToArray())d.ReplaceWith(Pair(d));
            string twice=Path.Combine(folder,"two-pairs.docx");Write(twice,addedXml);
            string third=Path.Combine(folder,"motto-added.docx");DocxLineEditor.Write(twice,third,live,targets,[plan.Single(i=>i.LineFix!.Role==LineRole.Motto)]);
            assert(DocxLineInventory.Read(third,live).Lines.Count==3 && !DocxLineInventory.Read(third,live).HasUnmappedShapes,
                "Sequential Agency, Subject and missing Motto retain three logical Lines across compatibility saves");
            string updated=Path.Combine(folder,"updated.docx");
            var update=agency with {LineFix=agency.LineFix! with {ShapeId=inventory.Lines.Single().Id,Left=104,Top=112,Width=94}};
            DocxLineEditor.Write(third,updated,live,targets,[update]);
            var updatedXml=Read(updated);var editedInventory=DocxLineInventory.Read(updated,live);
            assert(editedInventory.Lines.Count==3 && updatedXml.Descendants(V+"line").Count()==1 &&
                updatedXml.Descendants(MC+"AlternateContent").Count()==1 &&
                editedInventory.Lines.Single(l=>l.AnchorStart==agency.Start).Geometry==new LineGeometry(104,112,94),
                "Updating a paired Line replaces both branches once, without a stale fallback, duplicate or empty Choice");
            assert(XNode.DeepEquals(Read(twice).Descendants(MC+"AlternateContent").Single(c=>c.Descendants(V+"line").Single().Attribute("alt")!.Value.EndsWith("Subject")),
                updatedXml.Descendants(MC+"AlternateContent").Single()),"Updating Agency preserves Subject's complete unrelated pair");
            assert(File.ReadAllBytes(paired).SequenceEqual(before),"Pair creation and updates never alter source bytes");
            var sourceText=string.Concat(xml.Descendants(W+"t").Select(t=>t.Value));
            assert(string.Concat(updatedXml.Descendants(W+"t").Select(t=>t.Value))==sourceText,"Pair replacement preserves all body text and table cells");
            var merged=new XDocument(xml);var mergedPair=merged.Descendants(MC+"AlternateContent").Single();
            var oldRun=mergedPair.Parent!;var textRun=oldRun.Parent!.Elements(W+"r").First(r=>r.Descendants(W+"t").Any());
            mergedPair.Remove();textRun.Add(mergedPair);oldRun.Remove();
            string mergedSource=Path.Combine(folder,"merged-run.docx"),mergedCandidate=Path.Combine(folder,"merged-updated.docx");Write(mergedSource,merged);
            DocxLineEditor.Write(mergedSource,mergedCandidate,live,targets,[update]);
            assert(string.Concat(Read(mergedCandidate).Descendants(W+"t").Select(t=>t.Value))==sourceText &&
                !Read(mergedCandidate).Descendants(MC+"AlternateContent").Any(),
                "Replacing a compatibility pair merged into a text run keeps the visible text run intact");
            string shifted=Path.Combine(folder,"shifted.docx");
            DocxLineEditor.Write(sample,shifted,live,targets,[agency with {LineFix=agency.LineFix! with {Left=70,Top=90}}],[agency]);
            var shiftedXml=Read(shifted);var shiftedDrawing=shiftedXml.Descendants(W+"drawing").Single();shiftedDrawing.ReplaceWith(Pair(shiftedDrawing));
            string shiftedPair=Path.Combine(folder,"shifted-pair.docx"),shiftedUpdate=Path.Combine(folder,"shifted-update.docx");Write(shiftedPair,shiftedXml);
            assert(DocxLineInventory.Read(shiftedPair,live).Lines.Single().Geometry==new LineGeometry(100,110,90),
                "A saved compatibility pair compares fallback endpoints to encoded coordinates while retaining the separate measured intent");
            var reencoded=DocxLineEditor.Write(shiftedPair,shiftedUpdate,live,targets,[update]).Single().LineFix!;
            assert(reencoded.Left==74 && reencoded.Top==92 && DocxLineInventory.Read(shiftedUpdate,live).Lines.Single().Geometry==new LineGeometry(104,112,94),
                "Updating a compatibility pair retains its measured table translation and new intended geometry");
            using(var z=ZipFile.OpenRead(third))using(var u=ZipFile.OpenRead(updated))
            {
                bool same=z.Entries.Count==u.Entries.Count;
                foreach(var e in z.Entries.Where(e=>e.FullName!="word/document.xml"))
                {using var x=e.Open();using var y=u.GetEntry(e.FullName)!.Open();using var xb=new MemoryStream();using var yb=new MemoryStream();x.CopyTo(xb);y.CopyTo(yb);same &= xb.ToArray().SequenceEqual(yb.ToArray());}
                assert(same,"Every non-document package part is retained byte for byte when replacing a compatibility pair");
            }
            var invalid=new (string Name,Action<XElement> Change)[]
            {
                ("another Choice",c=>c.AddFirst(new XElement(c.Element(MC+"Choice")!))),
                ("unknown Requires namespace",c=>c.Element(MC+"Choice")!.SetAttributeValue("Requires","unknown")),
                ("different fallback role",c=>c.Descendants(V+"line").Single().SetAttributeValue("alt","ChinhTaViet.ShapeLine.Subject")),
                ("different fallback identity",c=>c.Descendants(V+"line").Single().SetAttributeValue("id","Other")),
                ("different fallback endpoint",c=>c.Descendants(V+"line").Single().SetAttributeValue("to","500pt,110pt")),
                ("unknown coordinate units",c=>c.Descendants(V+"line").Single().SetAttributeValue("from","100px,110px")),
                ("nonfinite endpoint",c=>c.Descendants(V+"line").Single().SetAttributeValue("from","NaNpt,110pt")),
                ("fallback textbox",c=>c.Descendants(V+"line").Single().Add(new XElement(V+"textbox",new XElement(W+"txbxContent",new XElement(W+"p",new XElement(W+"r",new XElement(W+"t","KEEP"))))))),
                ("fallback image",c=>c.Descendants(V+"line").Single().Add(new XElement(V+"imagedata"))),
                ("fallback group",c=>c.Descendants(W+"pict").Single().Add(new XElement(V+"group"))),
                ("second fallback Line",c=>c.Descendants(W+"pict").Single().Add(new XElement(c.Descendants(V+"line").Single()))),
                ("extra fallback content",c=>c.Element(MC+"Fallback")!.Add(new XElement(W+"r",new XElement(W+"t","KEEP")))),
                ("visible raw text",c=>c.Descendants(V+"line").Single().Add(new XText("KEEP"))),
                ("raw Choice text",c=>c.Element(MC+"Choice")!.Add(new XText("KEEP"))),
                ("rotated DrawingML",c=>c.Descendants(A+"xfrm").Single().SetAttributeValue("rot",60000)),
                ("flipped DrawingML",c=>c.Descendants(A+"xfrm").Single().SetAttributeValue("flipH",1)),
                ("DrawingML image",c=>c.Descendants(WPS+"wsp").Single().Add(new XElement(A+"blip"))),
                ("DrawingML text",c=>c.Descendants(WPS+"wsp").Single().Add(new XElement(A+"t","KEEP")))
            };
            int n=0;
            foreach(var (name,change) in invalid)
            {
                var bad=new XDocument(xml);change(bad.Descendants(MC+"AlternateContent").Single());
                string badPath=Path.Combine(folder,$"bad-{n}.docx"),candidate=Path.Combine(folder,$"bad-{n++}-candidate.docx");Write(badPath,bad);
                bool closed;
                try{var badInventory=DocxLineInventory.Read(badPath,live);closed=badInventory.HasUnmappedShapes && badInventory.Lines.All(l=>!l.AppOwned);}
                catch(InvalidDataException){closed=true;}
                assert(closed,"Unproven pair keeps manual review: "+name);
                var bytes=File.ReadAllBytes(badPath);bool rejected=false;
                try{DocxLineEditor.Write(badPath,candidate,live,targets,[update]);}catch(Exception error) when(error is InvalidOperationException or InvalidDataException){rejected=true;}
                assert(rejected && !File.Exists(candidate) && File.ReadAllBytes(badPath).SequenceEqual(bytes),"Unproven pair cannot be erased or normalized: "+name);
            }
            var unrelated=new XDocument(xml);unrelated.Descendants(W+"p").Last().Add(new XElement(W+"r",new XElement(W+"pict",new XElement(V+"line"))));
            assert(DocxLineInventory.ReadXml(unrelated,live).HasUnmappedShapes,"An unrelated standalone VML line still blocks automatic missing-Line creation");
        }
        finally{Directory.Delete(folder,true);}
    }
}
