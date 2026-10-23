using System.IO.Compression;
using System.Xml.Linq;
using ChinhTa.Core;

public static class OwnedLineFormatChecks
{
    public static void Run(Action<bool,string> assert)
    {
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XNamespace wp="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing";
        XNamespace a="http://schemas.openxmlformats.org/drawingml/2006/main";
        string folder=Path.Combine(Path.GetTempPath(),"OwnedLineFormat-"+Guid.NewGuid());Directory.CreateDirectory(folder);
        try
        {
            string source=Path.GetFullPath("samples/BaDuongKeThieu.docx"),candidate=Path.Combine(folder,"three.docx");
            var layout=DocxLineFixture.Read(source);
            var live=layout.Select(p=>new WordParagraphSnapshot(p.Start,p.Start+p.Text.Length+
                (p.InTable && p.CellEnd==p.Start+p.Text.Length+1?1:0),p.Text,p.Page,p.InTable,p.Centered,p.ParagraphCentered)).ToArray();
            var targets=ShapeLineRules.Identify(DocxLineFixture.ReadThroughXmlMap(source),DocumentProfile.State);
            var plan=targets.Select(t=>t.Probe with {CanFix=true,LineFix=ShapeLineRules.Evaluate(t,new(100,100,210),null,null,LinePolicy.Decree30).Fix}).ToArray();
            DocxLineEditor.Write(source,candidate,live,targets,plan);
            XDocument created;using(var zip=ZipFile.OpenRead(candidate))using(var input=zip.GetEntry("word/document.xml")!.Open())created=XDocument.Load(input);
            var drawings=created.Descendants(w+"drawing").ToDictionary(d=>Enum.Parse<LineRole>(((string)d.Descendants(wp+"docPr").Single().Attribute("descr")!)["ChinhTaViet.ShapeLine.".Length..]),d=>new XElement(d));
            assert(drawings.Count==3,"The regression uses all three real DocxLineEditor drawing structures");
            void Attach(XDocument xml,Action<XElement>? mutate=null)
            {
                foreach(var role in Enum.GetValues<LineRole>())
                {
                    var p=xml.Descendants(w+"p").Single(p=>string.Concat(p.Descendants(w+"t").Select(t=>t.Value))==
                        (role==LineRole.Agency?"ĐƠN VỊ BAN HÀNH":role==LineRole.Motto?HeadingRules.Motto:"trong giai đoạn thử nghiệm"));
                    var drawing=new XElement(drawings[role]);if(role==LineRole.Agency)mutate?.Invoke(drawing);
                    p.Add(new XElement(w+"r",drawing));
                }
            }
            foreach(bool soft in new[]{false,true})
            {
                var sample=ComponentChecks.Sample(softBreak:soft,changeXml:xml=>Attach(xml));
                assert(ComponentRules.Check(sample.Rows,sample.Layout,DocumentProfile.State).Count==0,
                    "Adding all three owned Lines does not invent an agency/recipient format warning");
                assert(DocumentHeadingRules.Check(sample.Rows,sample.Layout,DocumentProfile.State).Count==0,
                    "After owned Lines, centered multi-paragraph subject and discretionary breaks remain valid");
                foreach(var text in new[]{"ĐƠN VỊ BAN HÀNH",HeadingRules.Motto,"trong giai đoạn thử nghiệm"})
                {
                    var row=sample.Rows.Single(p=>p.Text.TrimEnd('\r','\a')==text);
                    assert(!row.TextEditable && row.FormatEditable,"Owned floating Lines allow character formatting but never ordinary text replacement");
                }
                var bad=sample.Rows.Select(p=>p.Text.StartsWith("ĐƠN VỊ BAN HÀNH") || p.Text.StartsWith("trong giai đoạn")?
                    p with {Font="Arial",Color=255,Size=10}:p).ToArray();
                var defects=ComponentRules.Check(bad,sample.Layout,DocumentProfile.State).Concat(DocumentHeadingRules.Check(bad,sample.Layout,DocumentProfile.State)).ToArray();
                assert(defects.Length==2 && defects.All(i=>i.CanFix && i.CharacterFormat is not null && i.Original==i.Replacement),
                    "Real agency and subject font defects after a Line still offer exact content-preserving repairs");
                BatchPlan.Create(defects);
                var mixed=sample.Rows.Select(p=>p.Text.StartsWith("ĐƠN VỊ BAN HÀNH")?p with {FormatMixed=true}:p).ToArray();
                assert(ComponentRules.Check(mixed,sample.Layout,DocumentProfile.State).Single().CanFix==false,
                    "A known Line cannot conceal genuinely mixed or unobserved visible formatting");
                assert(Checker.Check(sample.Rows).All(i=>i.Kind is not (FixKind.AgencyFormat or FixKind.DocumentHeadingFormat)),
                    "The ordinary checker does not gain a new text-write path through an owned Line");
            }
            foreach(var mutate in new Action<XElement>[]
            {
                d=>d.Descendants(wp+"docPr").Single().SetAttributeValue("descr","Foreign.Line"),
                d=>d.Descendants(a+"prstGeom").Single().SetAttributeValue("prst","rect"),
                d=>d.Descendants(wp+"docPr").Single().SetAttributeValue("id",drawings[LineRole.Motto].Descendants(wp+"docPr").Single().Attribute("id")!.Value),
                d=>d.Descendants(wp+"docPr").Single().SetAttributeValue("descr","ChinhTaViet.ShapeLine.Motto"),
                d=>d.Element(wp+"anchor")!.Add(new XElement(w+"object")),
                d=>d.Element(wp+"anchor")!.Name=wp+"inline",
                d=>d.Element(wp+"anchor")!.Add(new XElement(a+"t","Hidden shape text")),
                d=>d.Element(wp+"anchor")!.Add(new XElement(XName.Get("linkedTxbx","http://schemas.microsoft.com/office/word/2010/wordprocessingShape")))
            })
            {
                var blocked=ComponentChecks.Sample(changeXml:xml=>Attach(xml,mutate));
                var row=blocked.Rows.Single(p=>p.Text.StartsWith("ĐƠN VỊ BAN HÀNH"));
                assert(!row.TextEditable && !row.FormatEditable,
                    "Foreign, non-line, duplicate-ID/role, embedded-content and inline drawings fail closed");
                assert(ComponentRules.Check(blocked.Rows,blocked.Layout,DocumentProfile.State).Any(i=>i.Start==row.Start && !i.CanFix),
                    "A blocked drawing retains the localized manual warning");
            }
            var mismatched=ComponentChecks.Sample(changeXml:xml=>Attach(xml));
            var agency=mismatched.Rows.Single(p=>p.Text.StartsWith("ĐƠN VỊ BAN HÀNH"));
            string xmlText=created.ToString();var noFormats=new HashSet<int>();
            WordTextProof.Read(xmlText,[new(agency.Start,agency.Start+5,"Khác\r",1,true,true)],onFormatEditable:(start,ok)=>{if(ok)noFormats.Add(start);});
            assert(noFormats.Count==0,"An owned marker cannot bypass the XML/live-text prefix mismatch");
        }
        finally{Directory.Delete(folder,true);}
    }
}
