using System.Xml.Linq;
using ChinhTa.Core;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using Wordprocessing=DocumentFormat.OpenXml.Wordprocessing;

public static class TrackedMoveBoundaryChecks
{
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private const string Before="Nội dung đầu đã  duyệt.";
    private const string Moved="Nội dung  kế hoạch kế hoạch đã duyệt.";
    private const string After="Nội dung cuối đã  duyệt.";
    private static XElement Run(string text)=>new(W+"r",new XElement(W+"t",text));
    private static XElement Row(string text)=>new(W+"p",Run(text));
    private static XElement Marker(string kind)=>new(W+kind,new XAttribute(W+"id",7),
        kind.EndsWith("RangeStart",StringComparison.Ordinal)?new object[]{new XAttribute(W+"author","Synthetic check"),
            new XAttribute(W+"date","2026-10-30T00:00:00Z"),new XAttribute(W+"name","tracked_move")}:null);
    private static XElement Tracked(string kind,string text)
    {
        if(kind is "moveTo" or "moveFrom" or "ins" or "del")
            return new(W+"p",new XElement(W+kind,new XAttribute(W+"id",7),new XAttribute(W+"author","Synthetic check"),Run(text)));
        var row=new XElement(W+"p",Marker(kind),Run(text));
        if(kind.EndsWith("RangeStart",StringComparison.Ordinal))row.Add(Marker(kind[..^5]+"End"));
        return row;
    }
    private static string Xml(params XElement[] rows)=>new XDocument(new XElement(W+"document",
        new XAttribute(XNamespace.Xmlns+"w",W),new XElement(W+"body",rows))).ToString();
    private static bool SchemaValid(string xml)
    {
        using var stream=new MemoryStream();
        using var doc=WordprocessingDocument.Create(stream,DocumentFormat.OpenXml.WordprocessingDocumentType.Document,true);
        doc.AddMainDocumentPart().Document=new Wordprocessing.Document(xml);
        return !new OpenXmlValidator().Validate(doc).Any();
    }
    private static Paragraph[] Rows()=>[new(41,Before+"\r"),new(181,Moved+"\r"),new(501,After+"\r")];
    private static WordParagraphSnapshot[] Native(Paragraph[] rows)=>rows.Select(p=>
        new WordParagraphSnapshot(p.Start,p.Start+p.Text.Length,p.Text,1,false,false)).ToArray();
    private static bool Reject(Action action)
    {try{action();return false;}catch(InvalidOperationException){return true;}}

    public static void Run(Action<bool,string> assert)
    {
        var rows=Rows();var native=Native(rows);
        string plain=Xml(Row(Before),Row(Moved),Row(After));
        var old=Checker.Check(WordTextProof.Apply(rows,WordTextProof.Read(plain,native))).Where(i=>i.CanFix).ToArray();
        assert(old.Length==4 && old.Count(i=>i.Start>=rows[1].Start && i.Start<rows[2].Start)==2,
            "The unmarked control has independent spacing and repetition choices at source-exact noncontiguous native CPs");
        foreach(string kind in new[]{"moveTo","moveFrom","moveToRangeStart","moveToRangeEnd","moveFromRangeStart","moveFromRangeEnd"})
        {
            string xml=Xml(Row(Before),Tracked(kind,Moved),Row(After));
            assert(SchemaValid(xml),
                "The tracked-move guard fixture is OpenXML schema-valid, including explicit range markers outside wrappers: "+kind);
            WordXmlMappingException? warning=null;var outlined=new Dictionary<int,OutlineParagraphInfo>();
            var format=new Dictionary<int,bool>();
            var proven=WordTextProof.Read(xml,native,e=>warning=e,(start,info)=>outlined[start]=info,(start,canEdit)=>format[start]=canEdit);
            assert(proven.SetEquals([rows[0].Start]) && warning?.DiagnosticCode.StartsWith("XML-CONTENT.P2.",StringComparison.Ordinal)==true &&
                outlined.Keys.SequenceEqual([rows[0].Start]) && format.Count==1 && format[rows[0].Start],
                "A tracked-move boundary preserves only the earlier native proof and cannot publish outline/format permission for that row or later matching text: "+kind);
            var matched=new Dictionary<int,int>();warning=null;
            var snapshot=WordXmlSnapshot.Parse(xml);
            var snapshotProof=WordTextProof.Read(snapshot,native,e=>warning=e,onParagraphMatched:(start,index)=>matched[start]=index);
            assert(snapshotProof.SetEquals(proven) && warning?.DiagnosticCode.StartsWith("XML-CONTENT.P2.",StringComparison.Ordinal)==true &&
                matched.Count==1 && matched[rows[0].Start]==0,
                "The cached-current XML reader applies the same hard boundary without guessing later UTF-16 offsets: "+kind);
            var layout=WordXmlTableMap.Read(snapshot,native,_=>{});
            assert(layout.Count==1 && layout[0].Start==rows[0].Start,
                "The header/table layout reader cannot use paragraph geometry beyond the same tracked-move boundary: "+kind);
            foreach(var profile in Enum.GetValues<DocumentProfile>())
            {
                var current=Checker.Check(WordTextProof.Apply(rows,proven,outlined,format.Where(p=>p.Value).Select(p=>p.Key).ToHashSet()),profile);
                var selected=current.Where(i=>i.CanFix).ToArray();
                assert(selected.Length==1 && selected[0].Start<rows[1].Start &&
                    current.Count(i=>!i.CanFix && (i.Original==Moved || i.Original==After))==2,
                    "Every profile leaves the moved and subsequent unproven rows manual while preserving the independent earlier repair: "+kind);
                assert(Reject(()=>CurrentTextPlan.Validate(old,current)) &&
                    Reject(()=>RepairDependencies.Expand(old,current)),
                    "Adding move history invalidates old text selections at unchanged literal bytes and native CPs before mutation: "+kind);
            }
            string story=string.Concat(rows.Select(p=>p.Text));
            var allowed=Checker.Check(WordTextProof.Apply(rows,proven)).Where(i=>i.CanFix).ToArray();
            string expected=AtomicTextBatch.ExpectedStory(BatchPlan.Create(allowed),story,i=>story[..(i.Start-rows[0].Start)]);
            assert(expected=="Nội dung đầu đã duyệt.\r"+Moved+"\r"+After+"\r",
                "Repairing the still-proven prefix preserves every moved/unproven character and paragraph terminator: "+kind);
        }
        foreach(string family in new[]{"moveTo","moveFrom"})
        {
            string xml=Xml(Row(Before),new(W+"p",Marker(family+"RangeStart"),Run(Moved)),
                new(W+"p",Run(After),Marker(family+"RangeEnd")));
            assert(SchemaValid(xml) && WordTextProof.Read(xml,native,_=>{}).SetEquals([rows[0].Start]),
                "A paired move range across paragraphs is stopped at its first marker even without a containing move run wrapper: "+family);
        }
        foreach(string kind in new[]{"ins","del"})
            assert(WordTextProof.Read(Xml(Row(Before),Tracked(kind,Moved),Row(After)),native,_=>{}).SetEquals([rows[0].Start]),
                "The pre-existing insertion/deletion mapping boundary remains unchanged: "+kind);
        var ordinary=Row(Moved);ordinary.AddFirst(new XElement(W+"proofErr",new XAttribute(W+"type","spellStart")));
        ordinary.Add(new XElement(W+"proofErr",new XAttribute(W+"type","spellEnd")));
        assert(WordTextProof.Read(Xml(Row(Before),ordinary,Row(After)),native).Count==3,
            "Ordinary spelling-proof metadata is not mistaken for tracked move history or made a new unsupported boundary");
        foreach(string kind in new[]{"moveTo","moveFrom"})
        {
            string heading=Checker.Heading.ToLowerInvariant();
            var original=new Paragraph(71,heading+"\r",Font:"Arial",Size:10,Bold:false,FormatObserved:true);
            var headingNative=Native([original]);
            var proposed=Checker.Check([original]).Where(i=>i.CanFix).ToArray();
            var marked=WordTextProof.Apply([original],WordTextProof.Read(Xml(Tracked(kind,heading)),headingNative,_=>{}),formatEditableStarts:new HashSet<int>());
            var current=Checker.Check(marked);
            assert(proposed.Length==1 && proposed[0].Format is not null && current.All(i=>!i.CanFix && i.Format is null) &&
                Reject(()=>CurrentTextPlan.Validate(proposed,current)),
                "Unchanged heading text inside tracked move history cannot retain an old text/font repair permission: "+kind);
        }
        ExternalBoundaryChecks(assert);
    }
    private static void ExternalBoundaryChecks(Action<bool,string> assert)
    {
        var rows=Rows();var native=Native(rows);
        XElement Table(params XElement[] content)=>new(W+"tbl",new XElement(W+"tblPr"),
            new XElement(W+"tblGrid",new XElement(W+"gridCol",new XAttribute(W+"w",4000))),
            new XElement(W+"tr",new XElement(W+"tc",content)));
        foreach(string family in new[]{"moveTo","moveFrom"})
        foreach(bool cell in new[]{false,true})
        {
            var currentNative=native.ToArray();
            if(cell)currentNative[1]=currentNative[1] with {Text=Moved+"\r\a",End=rows[1].Start+Moved.Length+2,InTable=true};
            XElement[] moved=[Marker(family+"RangeStart"),Row(Moved),Marker(family+"RangeEnd")];
            string xml=cell?Xml(Row(Before),Table(moved),Row(After)):Xml([Row(Before),..moved,Row(After)]);
            assert(SchemaValid(xml),"Move range markers directly under body/cell are valid OpenXML block content: "+family+"/"+cell);
            WordXmlMappingException? warning=null;
            var proof=WordTextProof.Read(xml,currentNative,e=>warning=e);
            assert(proof.SetEquals([rows[0].Start]) && warning?.DiagnosticCode.StartsWith("XML-CONTENT.P2.",StringComparison.Ordinal)==true,
                "A valid move start outside paragraphs stops the same main-story prefix before its first affected paragraph: "+family+"/"+cell);
            var snapshot=WordXmlSnapshot.Parse(xml);
            assert(WordTextProof.Read(snapshot,currentNative,_=>{}).SetEquals(proof) &&
                WordXmlTableMap.Read(snapshot,currentNative,_=>{}).Select(p=>p.Start).SequenceEqual([rows[0].Start]),
                "String/snapshot/layout reads cannot bypass an external body/cell move-history boundary: "+family+"/"+cell);
            string orphan=cell?Xml(Row(Before),Table(Row(Moved),Marker(family+"RangeEnd")),Row(After)):
                Xml(Row(Before),Row(Moved),Row(After),Marker(family+"RangeEnd"));
            warning=null;
            assert(SchemaValid(orphan) && WordTextProof.Read(orphan,currentNative,e=>warning=e).Count==0 &&
                warning?.DiagnosticCode.StartsWith("XML-CONTENT.P1.",StringComparison.Ordinal)==true,
                "An unmatched external move end cannot silently authorize the whole story even after the final paragraph: "+family+"/"+cell);
            assert(WordTextProof.Read(WordXmlSnapshot.Parse(orphan),currentNative,_=>{}).Count==0,
                "Cached-current XML also rejects an orphan external move end instead of treating a matching body as wholly proven: "+family+"/"+cell);
        }
        foreach(string family in new[]{"moveTo","moveFrom"})
        {
            string internalStart=Xml(Row(Before),new(W+"p",Marker(family+"RangeStart"),Run(Moved)),Row(After),Marker(family+"RangeEnd"));
            assert(SchemaValid(internalStart) && WordTextProof.Read(internalStart,native,_=>{}).SetEquals([rows[0].Start]),
                "An external tail end paired with an earlier in-paragraph start retains the safe prefix before that start: "+family);
            string later=Xml(Row(Before),Row(Moved),Marker(family+"RangeStart"),Row(After),Marker(family+"RangeEnd"));
            assert(SchemaValid(later) && WordTextProof.Read(later,native,_=>{}).SetEquals([rows[0].Start,rows[1].Start]),
                "A valid late external start does not revoke earlier matching paragraphs unrelated to the move range: "+family);
            var wrapper=new XElement(W+family,new XAttribute(W+"id",7),new XAttribute(W+"author","Synthetic check"),Row(Moved));
            string malformed=Xml(Row(Before),wrapper,Row(After));
            assert(!SchemaValid(malformed) && WordTextProof.Read(malformed,native,_=>{}).SetEquals([rows[0].Start]),
                "An unsupported malformed move wrapper around a paragraph fails closed at that ancestor boundary: "+family);
            string alreadyClosed=Xml(Row(Before),Tracked(family+"RangeStart",Moved),Row(After),Marker(family+"RangeEnd"));
            assert(WordTextProof.Read(alreadyClosed,native,_=>{}).Count==0,
                "An extra external tail end cannot reuse an already consumed earlier range start to appear safely paired: "+family);
        }
    }
}
