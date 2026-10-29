using System.Xml.Linq;
using ChinhTa.Core;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using Wordprocessing=DocumentFormat.OpenXml.Wordprocessing;

public static class TrackedFormatHistoryChecks
{
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private const string Before="Nội dung đầu đã  duyệt.";
    private const string Marked="Nội dung  kế hoạch kế hoạch đã duyệt.";
    private const string After="Nội dung cuối đã  duyệt.";
    private static XElement Run(string text)=>new(W+"r",new XElement(W+"t",new XAttribute(XNamespace.Xml+"space","preserve"),text));
    private static XElement Row(string text)=>new(W+"p",Run(text));
    private static XElement Change(string kind)=>new(W+kind,new XAttribute(W+"id",7),
        new XAttribute(W+"author","Synthetic check"),new XAttribute(W+"date","2026-10-30T00:00:00Z"),
        new XElement(W+(kind=="rPrChange"?"rPr":"pPr"),new XElement(W+(kind=="rPrChange"?"b":"jc"),
            kind=="rPrChange"?null:new XAttribute(W+"val","left"))));
    private static XElement Mark(string kind,string text,bool paragraphMark=false)
    {
        var p=Row(text);
        if(kind=="pPrChange")p.AddFirst(new XElement(W+"pPr",Change(kind)));
        else if(paragraphMark)p.AddFirst(new XElement(W+"pPr",new XElement(W+"rPr",Change(kind))));
        else p.Element(W+"r")!.AddFirst(new XElement(W+"rPr",Change(kind)));
        return p;
    }
    private static string Xml(params XElement[] content)=>new XDocument(new XElement(W+"document",
        new XAttribute(XNamespace.Xmlns+"w",W),new XElement(W+"body",content))).ToString();
    private static bool SchemaValid(string xml)
    {
        using var stream=new MemoryStream();
        using var package=WordprocessingDocument.Create(stream,DocumentFormat.OpenXml.WordprocessingDocumentType.Document,true);
        package.AddMainDocumentPart().Document=new Wordprocessing.Document(xml);
        return !new OpenXmlValidator().Validate(package).Any();
    }
    private static Paragraph[] Rows()=>[new(41,Before+"\r"),new(181,Marked+"\r"),new(501,After+"\r")];
    private static WordParagraphSnapshot[] Native(Paragraph[] rows)=>rows.Select(p=>
        new WordParagraphSnapshot(p.Start,p.Start+p.Text.Length,p.Text,1,p.InTable,false)).ToArray();
    private static bool Reject(Action action)
    {try{action();return false;}catch(InvalidOperationException){return true;}}
    public static void Run(Action<bool,string> assert)
    {
        var rows=Rows();var native=Native(rows);
        var original=Checker.Check(WordTextProof.Apply(rows,WordTextProof.Read(Xml(Row(Before),Row(Marked),Row(After)),native)))
            .Where(i=>i.CanFix).ToArray();
        assert(original.Length==4,"Plain controls have four source-exact independent repairs before tracked formatting is introduced");
        foreach(var (kind,paragraphMark) in new[]{("rPrChange",false),("rPrChange",true),("pPrChange",false)})
        {
            string label=kind+(paragraphMark?"/paragraph-mark":"");
            string xml=Xml(Row(Before),Mark(kind,Marked,paragraphMark),Row(After));
            assert(SchemaValid(xml),"Tracked old properties are valid OpenXML without changing current text: "+label);
            WordXmlMappingException? warning=null;
            var outlines=new Dictionary<int,OutlineParagraphInfo>();var formats=new HashSet<int>();
            var proof=WordTextProof.Read(xml,native,e=>warning=e,(cp,info)=>outlines[cp]=info,(cp,editable)=>{if(editable)formats.Add(cp);});
            assert(proof.SetEquals([rows[0].Start]) && formats.SetEquals(proof) && outlines.Keys.SequenceEqual(proof) &&
                warning?.DiagnosticCode.StartsWith("XML-CONTENT.P2.",StringComparison.Ordinal)==true,
                "Tracked-format history closes text/font/outline permission at the unsupported boundary and retains the independent prefix: "+label);
            var matched=new Dictionary<int,int>();warning=null;var snapshot=WordXmlSnapshot.Parse(xml);
            assert(WordTextProof.Read(snapshot,native,e=>warning=e,onParagraphMatched:(cp,index)=>matched[cp]=index).SetEquals(proof) &&
                matched.Count==1 && matched[rows[0].Start]==0 && warning?.DiagnosticCode.StartsWith("XML-CONTENT.P2.",StringComparison.Ordinal)==true,
                "The cached-current source cannot bypass the same old-property history or invent later native offsets: "+label);
            assert(WordXmlTableMap.Read(snapshot,native,_=>{}).Select(p=>p.Start).SequenceEqual(proof),
                "The layout reader also stops before tracked-format history: "+label);
            foreach(var profile in Enum.GetValues<DocumentProfile>())
            {
                var current=Checker.Check(WordTextProof.Apply(rows,proof,outlines,formats),profile);
                var allowed=current.Where(i=>i.CanFix).ToArray();
                assert(allowed.Length==1 && allowed[0].Start<rows[1].Start &&
                    current.Count(i=>!i.CanFix && (i.Original==Marked || i.Original==After))==2,
                    "Every profile keeps the unsupported history and later unproven rows manual while retaining the prior repair: "+label);
                assert(Reject(()=>CurrentTextPlan.Validate(original,current)) && Reject(()=>RepairDependencies.Expand(original,current)),
                    "An unchanged literal selection cannot survive new tracked-format history at the same native CPs: "+label);
            }
            string story=string.Concat(rows.Select(p=>p.Text));
            var repairs=Checker.Check(WordTextProof.Apply(rows,proof)).Where(i=>i.CanFix).ToArray();
            assert(AtomicTextBatch.ExpectedStory(BatchPlan.Create(repairs),story,i=>story[..(i.Start-rows[0].Start)])==
                "Nội dung đầu đã duyệt.\r"+Marked+"\r"+After+"\r",
                "Applying the still-proven prefix preserves every unproven character and terminator: "+label);
            string first=Xml(Mark(kind,Before,paragraphMark),Row(Marked),Row(After));
            assert(WordTextProof.Read(first,native,_=>{}).Count==0,
                "History in the first paragraph never publishes a partially trusted row: "+label);
            string last=Xml(Row(Before),Row(Marked),Mark(kind,After,paragraphMark));
            assert(WordTextProof.Read(last,native,_=>{}).SetEquals([rows[0].Start,rows[1].Start]),
                "Late tracked-format history does not revoke an already proven unrelated prefix: "+label);
            HeadingCheck(kind,paragraphMark,assert);
            CellCheck(kind,paragraphMark,assert);
        }
        var ordinary=Row(Marked);ordinary.SetAttributeValue(W+"rsidRPr","00112233");
        ordinary.Element(W+"r")!.AddFirst(new XElement(W+"rPr",new XElement(W+"b",new XAttribute(W+"val",0))));
        ordinary.AddFirst(new XElement(W+"proofErr",new XAttribute(W+"type","spellStart")));
        ordinary.Add(new XElement(W+"proofErr",new XAttribute(W+"type","spellEnd")));
        string control=Xml(Row(Before),ordinary,Row(After));
        assert(SchemaValid(control) && WordTextProof.Read(control,native).Count==3,
            "Ordinary current properties, revision-session IDs and spelling metadata remain eligible; only actual old-property revision elements add this boundary");
        TableHistoryChecks(assert);
    }
    private static void HeadingCheck(string kind,bool paragraphMark,Action<bool,string> assert)
    {
        var row=new Paragraph(83,Checker.Heading+"\r",Font:"Arial",Size:10,Bold:false,FormatObserved:true,
            FormatEvidence:new(Checker.Heading,[new("Arial",10,0,0,0)]));
        var previous=Checker.Check([row]).Where(i=>i.CanFix).ToArray();var formats=new HashSet<int>();
        var proof=WordTextProof.Read(Xml(Mark(kind,Checker.Heading,paragraphMark)),Native([row]),_=>{},
            onFormatEditable:(cp,editable)=>{if(editable)formats.Add(cp);});
        var current=Checker.Check(WordTextProof.Apply([row],proof,formatEditableStarts:formats));
        assert(previous is [{Format:not null}] && proof.Count==0 && formats.Count==0 && current.All(i=>!i.CanFix) &&
            Reject(()=>CurrentTextPlan.Validate(previous,current)),
            "A format-only heading repair cannot bypass unsupported old-property history merely because its text is already correct: "+kind+"/"+paragraphMark);
    }
    private static void CellCheck(string kind,bool paragraphMark,Action<bool,string> assert)
    {
        var rows=Rows();rows[1]=rows[1] with {Text=Marked+"\r\a",InTable=true};
        var table=new XElement(W+"tbl",new XElement(W+"tblPr"),
            new XElement(W+"tblGrid",new XElement(W+"gridCol",new XAttribute(W+"w",4000))),
            new XElement(W+"tr",new XElement(W+"tc",Mark(kind,Marked,paragraphMark))));
        string xml=Xml(Row(Before),table,Row(After));
        assert(SchemaValid(xml) && WordTextProof.Read(xml,Native(rows),_=>{}).SetEquals([rows[0].Start]),
            "A table-cell paragraph with valid tracked old properties follows the same source boundary without bypassing it through cell geometry: "+kind+"/"+paragraphMark);
    }
    private static XElement Table(string kind,string text,bool history=true)
    {
        var previous=new XElement(W+kind[..^"Change".Length]);
        if(kind=="tblGridChange")previous.Add(new XElement(W+"gridCol",new XAttribute(W+"w",4000)));
        var change=new XElement(W+kind,new XAttribute(W+"id",7),
            kind=="tblGridChange"?null:new XAttribute(W+"author","Synthetic check"),previous);
        var tblPr=new XElement(W+"tblPr");
        var grid=new XElement(W+"tblGrid",new XElement(W+"gridCol",new XAttribute(W+"w",4000)));
        var trPr=new XElement(W+"trPr");var tcPr=new XElement(W+"tcPr");var tr=new XElement(W+"tr");
        if(history)switch(kind)
        {
            case "tblPrChange":tblPr.Add(change);break;
            case "tblGridChange":grid.Add(change);break;
            case "trPrChange":trPr.Add(change);break;
            case "tcPrChange":tcPr.Add(change);break;
            case "tblPrExChange":tr.Add(new XElement(W+"tblPrEx",change));break;
            default:throw new InvalidOperationException("Unknown synthetic table revision");
        }
        tr.Add(trPr,new XElement(W+"tc",tcPr,Row(text)));
        return new(W+"tbl",tblPr,grid,tr);
    }
    private static void TableHistoryChecks(Action<bool,string> assert)
    {
        var rows=Rows();rows[1]=rows[1] with {Text=Marked+"\r\a",InTable=true};var native=Native(rows);
        foreach(string kind in new[]{"tblPrChange","trPrChange","tcPrChange","tblPrExChange","tblGridChange"})
        {
            string plain=Xml(Row(Before),Table(kind,Marked,false),Row(After));
            var previous=Checker.Check(WordTextProof.Apply(rows,WordTextProof.Read(plain,native))).Where(i=>i.CanFix).ToArray();
            assert(SchemaValid(plain) && previous.Length==4 && WordXmlTableMap.Read(plain,native).Single(p=>p.InTable) is {CellStart:181,CellEnd:>181},
                "An ordinary table retains its independently proven text and usable cell bounds: "+kind);
            string xml=Xml(Row(Before),Table(kind,Marked),Row(After));
            assert(SchemaValid(xml),"The table old-property history fixture is valid block-level OpenXML: "+kind);
            WordXmlMappingException? warning=null;var outlines=new Dictionary<int,OutlineParagraphInfo>();var formats=new HashSet<int>();
            var proof=WordTextProof.Read(xml,native,e=>warning=e,(cp,info)=>outlines[cp]=info,(cp,canEdit)=>{if(canEdit)formats.Add(cp);});
            assert(proof.SetEquals([rows[0].Start]) && formats.SetEquals(proof) && outlines.Keys.SequenceEqual(proof) &&
                warning?.DiagnosticCode.StartsWith("XML-CONTENT.P2.",StringComparison.Ordinal)==true,
                "A tracked table property outside paragraphs stops source proof before the first affected cell: "+kind);
            var snapshot=WordXmlSnapshot.Parse(xml);var matched=new Dictionary<int,int>();
            assert(WordTextProof.Read(snapshot,native,_=>{},onParagraphMatched:(cp,index)=>matched[cp]=index).SetEquals(proof) && matched.Count==1 &&
                WordXmlTableMap.Read(snapshot,native,_=>{}).Select(p=>p.Start).SequenceEqual(proof),
                "String/cached/layout readers share the same table-history boundary and cannot publish affected geometry: "+kind);
            foreach(var profile in Enum.GetValues<DocumentProfile>())
            {
                var current=Checker.Check(WordTextProof.Apply(rows,proof,outlines,formats),profile);
                assert(current.Count(i=>i.CanFix)==1 && current.Single(i=>i.CanFix).Start<rows[1].Start &&
                    Reject(()=>CurrentTextPlan.Validate(previous,current)) && Reject(()=>RepairDependencies.Expand(previous,current)),
                    "Tracked table history cannot reuse a stale literal selection even when its current words and native CPs match: "+kind+"/"+profile);
            }
            var firstNative=new[]{native[1],native[2]};
            assert(WordTextProof.Read(Xml(Table(kind,Marked),Row(After)),firstNative,_=>{}).Count==0,
                "A first table with old-property revisions grants neither affected nor later proof: "+kind);
        }
    }
}
