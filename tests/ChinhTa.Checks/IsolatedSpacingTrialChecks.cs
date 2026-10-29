using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using ChinhTa.Core;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;

public static class IsolatedSpacingTrialChecks
{
    private static readonly XNamespace P="http://schemas.microsoft.com/office/2006/xmlPackage";
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly XNamespace R="http://schemas.openxmlformats.org/package/2006/relationships";
    private const string First="Nội dung dòng một.\vDòng thứ hai.\vDòng thứ ba.";
    private const string Second="Đoạn sau giữ nguyên.\vDòng tiếp theo.\vDòng cuối.";
    private static XElement Part(string name,string type,XElement content)=>new(P+"part",new XAttribute(P+"name","/"+name),new XAttribute(P+"contentType",type),new XElement(P+"xmlData",content));
    private static XElement Paragraph(string text)=>new(W+"p",new XElement(W+"pPr",
        new XElement(W+"spacing",new XAttribute(W+"before",60),new XAttribute(W+"after",100),new XAttribute(W+"line",400),new XAttribute(W+"lineRule","atLeast")),
        new XElement(W+"jc",new XAttribute(W+"val","both"))),
        new XElement(W+"r",new XElement(W+"rPr",new XElement(W+"rFonts",new XAttribute(W+"ascii","Times New Roman"),new XAttribute(W+"hAnsi","Times New Roman")),new XElement(W+"sz",new XAttribute(W+"val",26))),
            text.Split('\v').SelectMany((s,i)=>i==0?new XNode[]{new XElement(W+"t",s)}:[new XElement(W+"br"),new XElement(W+"t",s)])));
    private static XElement Fixture()=>new(P+"package",
        Part("_rels/.rels","application/vnd.openxmlformats-package.relationships+xml",new XElement(R+"Relationships",new XElement(R+"Relationship",new XAttribute("Id","main"),new XAttribute("Type","http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"),new XAttribute("Target","word/document.xml")))),
        Part("word/document.xml","application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml",new XElement(W+"document",new XElement(W+"body",Paragraph(First),Paragraph(Second),new XElement(W+"sectPr")))),
        Part("word/styles.xml","application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml",new XElement(W+"styles")),
        Part("word/_rels/document.xml.rels","application/vnd.openxmlformats-package.relationships+xml",new XElement(R+"Relationships",new XElement(R+"Relationship",new XAttribute("Id","styles"),new XAttribute("Type","http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"),new XAttribute("Target","styles.xml")))),
        Part("customXml/item1.xml","application/xml",new XElement("metadata",new XCData("Nguyên vẹn <nội dung>"))),
        new XElement(P+"part",new XAttribute(P+"name","/word/media/preserved.bin"),new XAttribute(P+"contentType","application/octet-stream"),new XElement(P+"binaryData",Convert.ToBase64String([0,17,127,128,255]))));
    private static XElement Body(XElement flat)=>flat.Descendants(W+"body").Single();
    private static XElement PPr(XElement flat)=>Body(flat).Elements(W+"p").First().Element(W+"pPr")!;
    private static string Xml(XElement flat)=>flat.ToString(SaveOptions.DisableFormatting);
    public static void Run(Action<bool,string> assert)
    {
        var fixture=Fixture();string flat=Xml(fixture);var first=IsolatedSpacingTrial.Find(flat,First);var next=IsolatedSpacingTrial.Find(flat,Second);
        assert(first.BodyIndex==0 && first.Text==First && next.BodyIndex==1 && first.SourceHash==next.SourceHash,
            "Trial anchors bind direct body positions and full soft-break text to one complete current package");
        void UnsupportedCategory(XElement source,string reason)
        {
            bool correct=false;
            try{IsolatedSpacingTrial.Find(Xml(source),First);}
            catch(SpacingDiagnosticException error) when(error.Code==SpacingDiagnosticFailureCode.UnsupportedEvidence){correct=true;}
            assert(correct,reason);
        }
        var passive=Fixture();passive.Elements(P+"part").Single(p=>(string?)p.Attribute(P+"name")=="/word/media/preserved.bin")
            .SetAttributeValue(P+"contentType","application/vnd.ms-office.vbaProject");
        UnsupportedCategory(passive,"A source passive-content restriction is unsupported scope, not a Word operation fault");
        var duplicateDefault=Fixture();duplicateDefault.Descendants(W+"styles").Single().Add(
            new XElement(W+"style",new XAttribute(W+"type","paragraph"),new XAttribute(W+"default",1),new XAttribute(W+"styleId","DefaultA")),
            new XElement(W+"style",new XAttribute(W+"type","paragraph"),new XAttribute(W+"default",1),new XAttribute(W+"styleId","DefaultB")));
        UnsupportedCategory(duplicateDefault,"Ambiguous default paragraph styles report unsupported scope without suggesting that Word needs repair");
        var verifiedTrial=IsolatedSpacingTrial.Create(flat,first,IsolatedSpacingTrialKind.Single);
        bool integrityFailure=false;
        try{IsolatedSpacingTrial.Verify(flat,verifiedTrial with {FlatXml=verifiedTrial.FlatXml.Replace("Nội dung","Đổi nội dung",StringComparison.Ordinal)});}
        catch(SpacingDiagnosticException error) when(error.Code==SpacingDiagnosticFailureCode.IntegrityFailed){integrityFailure=true;}
        assert(integrityFailure,"A tampered trial audit reports an integrity failure that optional evidence fallback cannot swallow");
        string folder=Path.Combine(Path.GetTempPath(),"ChinhTa-spacing-trials-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try
        {
            foreach(var kind in Enum.GetValues<IsolatedSpacingTrialKind>())
            {
                bool gap=kind is IsolatedSpacingTrialKind.ZeroParagraphGap or IsolatedSpacingTrialKind.SixPointParagraphGap;
                var trial=IsolatedSpacingTrial.Create(flat,first,kind,gap?next:null);
                IsolatedSpacingTrial.Verify(flat,trial);
                assert(flat==Xml(fixture),"Creating "+kind+" never modifies the source snapshot or its paragraph XML");
                var xml=XElement.Parse(trial.FlatXml);var rows=Body(xml).Elements(W+"p").ToArray();
                var before=PPr(fixture).Element(W+"spacing")!;var changed=rows[0].Element(W+"pPr")!.Element(W+"spacing")!;
                if(gap)
                    assert((string?)changed.Attribute(W+"after")== (kind==IsolatedSpacingTrialKind.SixPointParagraphGap?"120":"0") && (string?)rows[1].Element(W+"pPr")!.Element(W+"spacing")!.Attribute(W+"before")=="0" &&
                        (string?)changed.Attribute(W+"before")=="60" && (string?)changed.Attribute(W+"line")=="400" && (string?)changed.Attribute(W+"lineRule")=="atLeast" &&
                        (string?)rows[1].Element(W+"pPr")!.Element(W+"spacing")!.Attribute(W+"after")=="100",
                        kind+" changes only the two boundary contributors, retaining outer gaps and native line modes");
                else
                    assert((string?)changed.Attribute(W+"line")== (kind==IsolatedSpacingTrialKind.Single?"240":"360") && (string?)changed.Attribute(W+"lineRule")=="auto" &&
                        (string?)changed.Attribute(W+"before")== (string?)before.Attribute(W+"before") && (string?)changed.Attribute(W+"after")== (string?)before.Attribute(W+"after") &&
                        XNode.DeepEquals(rows[1],Body(XElement.Parse(flat)).Elements(W+"p").Last()),
                        kind+" sets Word-native automatic line spacing while retaining gaps and the entire unselected neighbor");
                string path=Path.Combine(folder,kind+".docx");IsolatedWordPackage.FromFlat(trial.FlatXml).WriteNew(path);
                using(var doc=WordprocessingDocument.Open(path,false))
                    assert(!new OpenXmlValidator().Validate(doc).Any(),"Disposable "+kind+" trial is schema-valid and opens as an independent DOCX");
                using(var zip=ZipFile.OpenRead(path))using(var input=zip.GetEntry("word/media/preserved.bin")!.Open())
                {
                    using var bytes=new MemoryStream();input.CopyTo(bytes);
                    assert(bytes.ToArray().SequenceEqual(new byte[]{0,17,127,128,255}),"Every "+kind+" trial preserves arbitrary binary payloads, not only visible text");
                }
                // Audit the final candidate independently rather than trusting
                // that its creator once held a source anchor.
                foreach(var tamper in new Action<XElement>[]
                {
                    x=>x.Descendants(W+"t").First().Value="Changed text",
                    x=>PPr(x).Element(W+"jc")!.SetAttributeValue(W+"val","right"),
                    x=>x.Descendants(W+"sz").First().SetAttributeValue(W+"val",28),
                    x=>x.Descendants(P+"binaryData").Single().Value=Convert.ToBase64String([0,17,127,128,254]),
                    x=>Body(x).Elements(W+"p").Last().Add(new XElement(W+"r",new XElement(W+"t","extra")))
                })
                {
                    var changedFlat=XElement.Parse(trial.FlatXml);tamper(changedFlat);bool rejected=false;
                    try{IsolatedSpacingTrial.Verify(flat,trial with {FlatXml=Xml(changedFlat)});}catch(InvalidOperationException){rejected=true;}
                    assert(rejected,"Trial audit rejects unrelated text/font/alignment/part/neighbor changes for "+kind);
                }
            }
            void Rejected(Action<XElement> change,string reason)
            {
                var bad=Fixture();change(bad);bool rejected=false;
                try{IsolatedSpacingTrial.Bind(Xml(bad),0);}catch(InvalidOperationException){rejected=true;}
                assert(rejected,reason);
            }
            foreach(string property in new[]{"beforeLines","afterLines","beforeAutospacing","afterAutospacing"})
                Rejected(x=>PPr(x).Element(W+"spacing")!.SetAttributeValue(W+property,"1"),"Inherited/automatic line-unit geometry cannot be guessed from point values: "+property);
            foreach(string flag in new[]{"contextualSpacing","keepNext","pageBreakBefore","bidi"})
                Rejected(x=>PPr(x).Add(new XElement(W+flag)),"Special paragraph context excludes a diagnostic trial: "+flag);
            foreach(string name in new[]{"framePr","sectPr","pBdr","numPr"})
                Rejected(x=>PPr(x).Add(new XElement(W+name)),"A special/structural paragraph stays outside the plain spacing trial: "+name);
            Rejected(x=>Body(x).Elements(W+"p").First().Add(new XElement(W+"fldSimple",new XAttribute(W+"instr","PAGE"))),"Even passive PAGE fields are not silently treated as ordinary paragraph text");
            Rejected(x=>Body(x).Elements(W+"p").First().Add(new XElement(W+"r",new XElement(W+"drawing"))),"Drawings cannot be stripped to make a paragraph eligible for calibration");
            Rejected(x=>Body(x).Elements(W+"p").First().Descendants(W+"br").First().SetAttributeValue(W+"type","page"),"Explicit page breaks are not treated as soft line breaks");
            Rejected(x=>Body(x).Element(W+"sectPr")!.Add(new XElement(W+"docGrid",new XAttribute(W+"type","lines"))),"A document grid blocks ordinary baseline calibration");
            Rejected(x=>PPr(x).AddFirst(new XElement(W+"pStyle",new XAttribute(W+"val","Missing"))),"A missing paragraph style cannot supply a calibration context");
            Rejected(x=>Body(x).Element(W+"sectPr")!.Add(new XElement(W+"cols",new XAttribute(W+"num",2))),"Multiple columns remain outside the one-flow calibration scope");
            Rejected(x=>Body(x).Element(W+"sectPr")!.Add(new XElement(W+"pgMar",new XAttribute(W+"gutter",144))),"Nonzero section gutter cannot silently enter ordinary calibration");
            Rejected(x=>Body(x).Element(W+"sectPr")!.Add(new XElement(W+"pgSz",new XAttribute(W+"w",16000),new XAttribute(W+"h",12000))),"Landscape dimensions are rejected even when orient is omitted");
            foreach(string name in new[]{"textDirection","bidi","rtlGutter"})
                Rejected(x=>Body(x).Element(W+"sectPr")!.Add(new XElement(W+name)),"Unsupported section direction remains outside calibration: "+name);
            Rejected(x=>
            {
                x.Descendants(P+"part").Single(p=>(string?)p.Attribute(P+"name")=="/word/styles.xml").SetAttributeValue(P+"name","/customXml/styles.xml");
                x.Descendants(R+"Relationship").Single(r=>(string?)r.Attribute("Id")=="styles").Remove();
            },
                "A custom XML styles tree cannot impersonate the Word document's actual styles part");
            foreach(bool makeCycle in new[]{false,true})
                Rejected(x=>
                {
                    PPr(x).AddFirst(new XElement(W+"pStyle",new XAttribute(W+"val","Body")));
                    var styles=x.Descendants(W+"styles").Single();styles.Add(new XElement(W+"style",new XAttribute(W+"type","paragraph"),new XAttribute(W+"styleId","Body"),
                        new XElement(W+"basedOn",new XAttribute(W+"val",makeCycle?"Body":"Missing"))));
                },"Unknown/cyclic inherited style chains cannot authorize trials");
            foreach(string setting in new[]{"afterLines","beforeAutospacing"})
                Rejected(x=>x.Descendants(W+"styles").Single().Add(new XElement(W+"docDefaults",new XElement(W+"pPrDefault",new XElement(W+"pPr",new XElement(W+"spacing",new XAttribute(W+setting,100)))))),
                    "Source defaults are checked even when direct spacing looks ordinary: "+setting);
            var duplicate=Fixture();Body(duplicate).Elements(W+"p").Last().AddAfterSelf(Paragraph(First));bool ambiguous=false;
            try{IsolatedSpacingTrial.Find(Xml(duplicate),First);}catch(InvalidOperationException){ambiguous=true;}
            assert(ambiguous,"Text-only lookup cannot choose the first of duplicate paragraph anchors");
            bool stale=false;try{IsolatedSpacingTrial.Create(flat+" ",first,IsolatedSpacingTrialKind.Single);}catch(InvalidOperationException){stale=true;}
            assert(stale,"A stale full-package hash blocks the trial even when the target paragraph text is unchanged");
            bool wrongText=false;try{IsolatedSpacingTrial.Create(flat,first with {Text="Other"},IsolatedSpacingTrialKind.Single);}catch(InvalidOperationException){wrongText=true;}
            assert(wrongText,"A forged paragraph-text anchor cannot borrow the current package hash");
            bool wrongMode=false;try{IsolatedSpacingTrial.Create(flat,first,IsolatedSpacingTrialKind.Single,next);}catch(InvalidOperationException){wrongMode=true;}
            assert(wrongMode,"A line-spacing trial cannot accidentally mutate a second paragraph");
            bool noNeighbor=false;try{IsolatedSpacingTrial.Create(flat,first,IsolatedSpacingTrialKind.ZeroParagraphGap);}catch(InvalidOperationException){noNeighbor=true;}
            assert(noNeighbor,"Zero-gap calibration requires an explicit source-bound adjacent neighbor");
            var barrier=Fixture();Body(barrier).Elements(W+"p").First().AddAfterSelf(new XElement(W+"tbl"));string blocked=Xml(barrier);bool acrossTable=false;
            try{IsolatedSpacingTrial.Create(blocked,IsolatedSpacingTrial.Find(blocked,First),IsolatedSpacingTrialKind.ZeroParagraphGap,IsolatedSpacingTrial.Find(blocked,Second));}catch(InvalidOperationException){acrossTable=true;}
            assert(acrossTable,"An intervening table is not skipped to manufacture a paragraph-gap pair");
            var absent=Fixture();PPr(absent).Element(W+"spacing")!.Remove();string noSpacing=Xml(absent);
            var added=IsolatedSpacingTrial.Create(noSpacing,IsolatedSpacingTrial.Bind(noSpacing,0),IsolatedSpacingTrialKind.Single);
            IsolatedSpacingTrial.Verify(noSpacing,added);
            string inserted=Path.Combine(folder,"inserted.docx");IsolatedWordPackage.FromFlat(added.FlatXml).WriteNew(inserted);
            using(var doc=WordprocessingDocument.Open(inserted,false))
                assert(!new OpenXmlValidator().Validate(doc).Any(),"A missing spacing element is inserted before jc in schema order without reordering unrelated properties");
            var noProperties=Fixture();PPr(noProperties).Remove();string noPPr=Xml(noProperties);
            var created=IsolatedSpacingTrial.Create(noPPr,IsolatedSpacingTrial.Bind(noPPr,0),IsolatedSpacingTrialKind.OneAndHalf);
            IsolatedSpacingTrial.Verify(noPPr,created);
            assert(XElement.Parse(created.FlatXml).Descendants(W+"body").Single().Elements(W+"p").First().Elements().First().Name==W+"pPr",
                "A newly created pPr precedes runs and its audit still proves that only spacing changed");
            XElement WithEffects(bool primaryUnsafe=false)
            {
                var document=Fixture();PPr(document).AddFirst(new XElement(W+"pStyle",new XAttribute(W+"val","Body")));
                XElement Style(int lineUnits,int automatic)=>new(W+"style",new XAttribute(W+"type","paragraph"),new XAttribute(W+"styleId","Body"),
                    new XElement(W+"pPr",new XElement(W+"spacing",new XAttribute(W+"beforeLines",lineUnits),new XAttribute(W+"afterAutospacing",automatic))));
                document.Descendants(W+"styles").Single().Add(Style(primaryUnsafe?100:0,0));
                var effects=Part("word/stylesWithEffects.xml","application/vnd.ms-word.stylesWithEffects+xml",new XElement(W+"styles",Style(primaryUnsafe?0:100,1)));
                // Put effects first: a global First(w:styles) would now select
                // the deliberately contradictory alternate tree.
                document.AddFirst(effects);
                document.Descendants(R+"Relationships").Single(r=>r.Elements(R+"Relationship").Any(e=>(string?)e.Attribute("Id")=="styles"))
                    .Add(new XElement(R+"Relationship",new XAttribute("Id","effects"),new XAttribute("Type","http://schemas.microsoft.com/office/2007/relationships/stylesWithEffects"),new XAttribute("Target","stylesWithEffects.xml")));
                return document;
            }
            var wordStyles=WithEffects();string effectsSource=Xml(wordStyles);
            var effectAnchor=IsolatedSpacingTrial.Find(effectsSource,First);
            assert(effectAnchor.BodyIndex==0,"Validated stylesWithEffects does not shadow primary styles even when its contradictory defaults appear first");
            XElement Effects(XElement x)=>x.Elements(P+"part").Single(p=>(string?)p.Attribute(P+"name")=="/word/stylesWithEffects.xml");
            foreach(var kind in Enum.GetValues<IsolatedSpacingTrialKind>())
            {
                var trial=IsolatedSpacingTrial.Create(effectsSource,effectAnchor,kind,
                    kind is IsolatedSpacingTrialKind.ZeroParagraphGap or IsolatedSpacingTrialKind.SixPointParagraphGap?IsolatedSpacingTrial.Find(effectsSource,Second):null);
                IsolatedSpacingTrial.Verify(effectsSource,trial);
                var trialXml=XElement.Parse(trial.FlatXml);var sourceXml=XElement.Parse(effectsSource);
                assert(XNode.DeepEquals(Effects(trialXml),Effects(sourceXml)) &&
                    XNode.DeepEquals(trialXml.Descendants(R+"Relationships").Single(r=>r.Elements().Any(e=>(string?)e.Attribute("Id")=="effects")),
                        sourceXml.Descendants(R+"Relationships").Single(r=>r.Elements().Any(e=>(string?)e.Attribute("Id")=="effects"))),
                    kind+" keeps the unchanged effects-style XML tree and relationships in the actual complete trial package");
            }
            bool unsafePrimary=false;try{IsolatedSpacingTrial.Bind(Xml(WithEffects(true)),0);}catch(InvalidOperationException){unsafePrimary=true;}
            assert(unsafePrimary,"Ordinary effects styles cannot hide unsafe primary style line units");
            foreach(var damage in new Action<XElement>[]
            {
                x=>Effects(x).SetAttributeValue(P+"contentType","application/xml"),
                x=>x.Descendants(R+"Relationship").Single(r=>(string?)r.Attribute("Id")=="effects").SetAttributeValue("Type","http://example.invalid/looks-like-stylesWithEffects"),
                x=>x.Descendants(R+"Relationship").Single(r=>(string?)r.Attribute("Id")=="effects").Remove(),
                x=>Effects(x).Element(P+"xmlData")!.ReplaceNodes(new XElement("styles")),
                x=>x.Add(Part("customXml/fake-styles.xml","application/xml",new XElement(W+"styles")))
            })
            {
                var bad=WithEffects();damage(bad);bool blockedEffects=false;
                try{IsolatedSpacingTrial.Bind(Xml(bad),0);}catch(InvalidOperationException){blockedEffects=true;}
                assert(blockedEffects,"Unknown/misbound effects-style content cannot be removed from evidence merely because its part name looks familiar");
            }
        }
        finally{Directory.Delete(folder,true);}
    }
}
