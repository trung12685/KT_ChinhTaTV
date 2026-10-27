using System.Xml.Linq;
using ChinhTa.Core;

public static class InlineSignatureImageChecks
{
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main",
        WP="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing",A="http://schemas.openxmlformats.org/drawingml/2006/main",
        PIC="http://schemas.openxmlformats.org/drawingml/2006/picture",R="http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static XElement Image()=>new(W+"drawing",new XElement(WP+"inline",
        new XElement(WP+"extent",new XAttribute("cx",635000),new XAttribute("cy",254000)),
        new XElement(WP+"docPr",new XAttribute("id",17),new XAttribute("name","synthetic.png")),
        new XElement(A+"graphic",new XElement(A+"graphicData",new XAttribute("uri",PIC.NamespaceName),
            new XElement(PIC+"pic",new XElement(PIC+"nvPicPr",new XElement(PIC+"cNvPr",new XAttribute("id",0),new XAttribute("name","synthetic.png")),new XElement(PIC+"cNvPicPr")),
                new XElement(PIC+"blipFill",new XElement(A+"blip",new XAttribute(R+"embed","rIdImage")),new XElement(A+"stretch",new XElement(A+"fillRect"))),
                new XElement(PIC+"spPr",new XElement(A+"prstGeom",new XAttribute("prst","rect"),new XElement(A+"avLst"))))))));
    // The synthetic Word observations deliberately supply object CPs independently
    // of the production XML reader, including unsupported objects in negative cases.
    private static WordParagraphSnapshot[] Live(XDocument xml)
    {
        int cp=91;
        return xml.Descendants(W+"body").Single().Descendants(W+"p").Where(p=>!p.Ancestors(W+"txbxContent").Any()).Select(p=>{
            string text=string.Concat(p.Descendants().Where(e=>e.Parent?.Name==W+"r" && e.Ancestors(W+"p").First()==p)
                .Select(e=>e.Name==W+"t"?e.Value:e.Name==W+"br"?"\v":e.Name==W+"drawing"?"\u0001":""));
            bool cell=p.Ancestors(W+"tc").Any();text+="\r"+(cell && !p.ElementsAfterSelf(W+"p").Any()?"\a":"");
            var row=new WordParagraphSnapshot(cp,cp+text.Length,text,0,false,false);cp=row.End;return row;
        }).ToArray();
    }
    private sealed record Proof(Paragraph[] Rows,Dictionary<int,bool> Formats,string[] Warnings);
    private static Proof Read(XDocument xml,WordParagraphSnapshot[] live,bool shared)
    {
        var roles=new Dictionary<int,OutlineParagraphInfo>();var formats=new Dictionary<int,bool>();var warnings=new List<string>();
        void Outline(int start,OutlineParagraphInfo info)=>roles[start]=info;
        void Format(int start,bool can)=>formats[start]=can;
        void Warn(WordXmlMappingException e)=>warnings.Add(e.DiagnosticCode);
        var editable=shared?WordTextProof.Read(WordXmlSnapshot.Parse(xml.ToString()),live,Warn,Outline,Format):WordTextProof.Read(xml.ToString(),live,Warn,Outline,Format);
        var paragraphs=live.Select(p=>new Paragraph(p.Start,p.Text,InTable:false,Page:0,FormatObserved:true,Font:"Arial",Size:12,Color:255)).ToArray();
        return new(WordTextProof.Apply(paragraphs,editable,roles,formats.Where(p=>p.Value).Select(p=>p.Key).ToHashSet()).ToArray(),formats,warnings.ToArray());
    }
    public static void Run(Action<bool,string> assert)
    {
        foreach(int padding in new[]{0,60})
        foreach(bool shared in new[]{false,true})
        foreach(bool beforeName in new[]{true,false})
        {
            var xml=SignatureChecks.Sample(padding:padding).Xml;var cell=xml.Descendants(W+"tc").Last();
            var image=new XElement(W+"p",new XElement(W+"r",Image()));
            if(beforeName)cell.Elements(W+"p").Last().AddBeforeSelf(image);else cell.Add(image);
            string original=xml.ToString();var live=Live(xml);var proof=Read(xml,live,shared);
            var targets=SignatureRules.ObservationTargets(proof.Rows,DocumentProfile.State);
            assert(targets.Count==2 && proof.Warnings.Length==0,
                $"A separate embedded inline image keeps complete current signature proof (padding={padding}, shared={shared}, beforeName={beforeName}, targets={targets.Count}, warnings={string.Join(',',proof.Warnings)})");
            var objectRow=live.Single(p=>p.Text.Contains('\u0001'));var observed=proof.Rows.Single(p=>p.Start==objectRow.Start);
            assert(!observed.TextEditable && !observed.FormatEditable && !proof.Formats[objectRow.Start] && observed.Outline?.Scope!=OutlineScope.Signature,
                "Matching an image's object CP never grants text/font edits or a signature role to the image paragraph");
            assert(SignatureRules.Check(proof.Rows,DocumentProfile.State).Count==2 && targets.All(p=>p.TextEditable || p.FormatEditable),
                "Only authority and signer receive format proposals across the proven image paragraph");
            assert(xml.ToString()==original && live.All(p=>proof.Rows.Any(r=>r.Start==p.Start && r.Text==p.Text)),
                "Inline-image proof keeps XML, actual paragraph starts and every object CP unchanged");
            foreach(string replacement in new[]{"\r","\u0001\u0001\r","\u0002\r"})
            {
                var wrong=live.Select(p=>p.Start==objectRow.Start?p with{Text=replacement}:p).ToArray();var closed=Read(xml,wrong,shared);
                assert(closed.Warnings.Length>0 && closed.Rows.Where(p=>p.Start>=objectRow.Start).All(p=>!p.TextEditable && !p.FormatEditable),
                    "Missing, extra or different actual Word object CPs stop the proof instead of guessing offsets");
                if(beforeName)assert(SignatureRules.ObservationTargets(closed.Rows,DocumentProfile.State).Count==0,
                    "A missing image/name proof cannot authorize the already matched authority");
            }
        }
        foreach(var mutation in new Action<XElement>[]
        {
            d=>d.Element(WP+"inline")!.Name=WP+"anchor",
            d=>d.Descendants(A+"graphicData").Single().SetAttributeValue("uri","urn:unverified"),
            d=>d.Descendants(A+"blip").Single().SetAttributeValue(R+"link","external"),
            d=>d.Descendants(A+"blip").Single().Attribute(R+"embed")!.Remove(),
            d=>d.Descendants(PIC+"pic").Single().Add(new XElement(A+"t","Hidden text")),
            d=>d.Element(WP+"inline")!.Add(new XElement(W+"object")),
            d=>d.Add(new XElement(d.Elements().Single())),
            d=>d.Descendants(PIC+"pic").Single().Name=XNamespace.Get("urn:wrong-picture")+"pic"
        })
        {
            var xml=SignatureChecks.Sample(padding:60).Xml;var cell=xml.Descendants(W+"tc").Last();var drawing=Image();mutation(drawing);
            cell.Elements(W+"p").Last().AddBeforeSelf(new XElement(W+"p",new XElement(W+"r",drawing)));
            foreach(bool shared in new[]{false,true})
            {
                var proof=Read(xml,Live(xml),shared);
                assert(proof.Warnings.Length>0 && SignatureRules.ObservationTargets(proof.Rows,DocumentProfile.State).Count==0,
                    "Floating, linked, text-bearing, ambiguous or wrong-namespace objects cannot bridge current signature proof");
            }
        }
        foreach(bool shared in new[]{false,true})
        {
            XElement P(string text)=>new(W+"p",new XElement(W+"r",new XElement(W+"t",text)));
            var imageParagraph=new XElement(W+"p",new XElement(W+"r",new XElement(W+"t","Giữ "),Image(),new XElement(W+"t"," nguyên.")));
            var body=new XDocument(new XElement(W+"document",new XElement(W+"body",P("Trước ảnh."),imageParagraph,P("Nội dung sau ảnh."))));
            string original=body.ToString();var live=Live(body);var proof=Read(body,live,shared);
            assert(proof.Warnings.Length==0 && proof.Rows[0].TextEditable && !proof.Rows[1].TextEditable &&
                !proof.Rows[1].FormatEditable && proof.Rows[2].TextEditable && proof.Rows[2].FormatEditable,
                "An embedded picture amid text keeps its whole paragraph closed while independently matched later prose remains eligible");
            assert(proof.Rows[1].Text=="Giữ \u0001 nguyên.\r" && proof.Rows[2].Start==live[2].Start && body.ToString()==original,
                "Object-token comparison preserves surrounding text order, XML and current later Word CPs");
            foreach(string property in new[]{"numPr","pStyle"})
            {
                var xml=SignatureChecks.Sample(padding:60).Xml;var cell=xml.Descendants(W+"tc").Last();
                var picture=new XElement(W+"p",new XElement(W+"pPr",new XElement(W+property,new XAttribute(W+"val","Unknown"))),new XElement(W+"r",Image()));
                cell.Elements(W+"p").Last().AddBeforeSelf(picture);
                assert(SignatureRules.ObservationTargets(Read(xml,Live(xml),shared).Rows,DocumentProfile.State).Count==0,
                    "Numbered or unproven styled image paragraphs cannot be silently skipped to infer a signing block");
            }
            var two=SignatureChecks.Sample().Xml;var right=two.Descendants(W+"tc").Last();
            right.Elements(W+"p").Last().AddBeforeSelf(new XElement(W+"p",new XElement(W+"r",Image(),Image())));
            var multiple=Read(two,Live(two),shared);
            assert(multiple.Warnings.Length==0 && SignatureRules.ObservationTargets(multiple.Rows,DocumentProfile.State).Count==0,
                "Two proven object CPs can map text but do not authorize the single-image signing-block exception");
            var imageGeometry=new XDocument(body);imageGeometry.Descendants(PIC+"spPr").Single().Element(A+"prstGeom")!.SetAttributeValue("prst","line");
            var inventory=DocxLineInventory.ReadXml(imageGeometry,Live(imageGeometry));
            assert(inventory.Lines.Count==0 && !inventory.HasUnmappedShapes,
                "An inline picture with a line-shaped geometry remains an image instead of being offered as a foreign Word Line");
        }
    }
}
