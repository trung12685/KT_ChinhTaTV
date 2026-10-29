using System.Xml.Linq;
using ChinhTa.Core;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using Wordprocessing=DocumentFormat.OpenXml.Wordprocessing;

public static class AnnotatedTextRangeChecks
{
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private const string Before="Nội dung đầu đã  duyệt.";
    private const string Marked="Nội dung  kế hoạch kế hoạch đã duyệt.";
    private const string After="Nội dung cuối đã  duyệt.";
    private static XElement Run(string text)=>new(W+"r",new XElement(W+"t",new XAttribute(XNamespace.Xml+"space","preserve"),text));
    private static XElement Row(string text)=>new(W+"p",Run(text));
    private static XElement Marker(string family,bool start,string id="7",string name="kept_target")=>new(W+(family switch
        {"bookmark"=>start?"bookmarkStart":"bookmarkEnd","comment"=>start?"commentRangeStart":"commentRangeEnd",_=>start?"permStart":"permEnd"}),
        new XAttribute(W+"id",id),start && family=="bookmark"?new XAttribute(W+"name",name):
        start && family=="permission"?new XAttribute(W+"edGrp","everyone"):null);
    private static XElement Annotated(string family,string text,bool empty=false)=>new(W+"p",Marker(family,true),
        empty?null:Run(text),Marker(family,false),empty?Run(text):null);
    private static string Xml(params XElement[] nodes)=>new XDocument(new XElement(W+"document",
        new XAttribute(XNamespace.Xmlns+"w",W),new XElement(W+"body",nodes))).ToString();
    private static bool SchemaValid(string xml)
    {
        using var stream=new MemoryStream();
        using var package=WordprocessingDocument.Create(stream,DocumentFormat.OpenXml.WordprocessingDocumentType.Document,true);
        package.AddMainDocumentPart().Document=new Wordprocessing.Document(xml);
        var commentIds=XDocument.Parse(xml).Descendants(W+"commentRangeStart").Select(e=>(string)e.Attribute(W+"id")!).Distinct().ToArray();
        if(commentIds.Length>0)
            package.MainDocumentPart!.AddNewPart<WordprocessingCommentsPart>().Comments=new Wordprocessing.Comments(commentIds.Select(id=>
                new Wordprocessing.Comment(new Wordprocessing.Paragraph(new Wordprocessing.Run(new Wordprocessing.Text("Synthetic comment"))))
                {Id=id,Author="Synthetic check"}));
        return !new OpenXmlValidator().Validate(package).Any();
    }
    private static Paragraph[] Rows(params string[] texts)
    {
        int cp=91;
        return texts.Select(text=>{var row=new Paragraph(cp,text+"\r");cp+=row.Text.Length+53;return row;}).ToArray();
    }
    private static WordParagraphSnapshot[] Native(Paragraph[] rows)=>rows.Select(row=>
        new WordParagraphSnapshot(row.Start,row.Start+row.Text.Length,row.Text,1,row.InTable,false)).ToArray();
    private sealed record Observation(IReadOnlySet<int> Editable,Dictionary<int,OutlineParagraphInfo> Outline,HashSet<int> Format);
    private static Observation Observe(string xml,Paragraph[] rows,bool cached=false)
    {
        var outline=new Dictionary<int,OutlineParagraphInfo>();var format=new HashSet<int>();
        void Describe(int cp,OutlineParagraphInfo value)=>outline[cp]=value;
        void Format(int cp,bool value){if(value)format.Add(cp);}
        var editable=cached?WordTextProof.Read(WordXmlSnapshot.Parse(xml),Native(rows),onOutline:Describe,onFormatEditable:Format):
            WordTextProof.Read(xml,Native(rows),onOutline:Describe,onFormatEditable:Format);
        return new(editable,outline,format);
    }
    private static bool Reject(Action action)
    {try{action();return false;}catch(InvalidOperationException){return true;}}
    public static void Run(Action<bool,string> assert)
    {
        var rows=Rows(Before,Marked,After);string plain=Xml(rows.Select(row=>Row(row.Text.TrimEnd('\r'))).ToArray());
        var old=Checker.Check(WordTextProof.Apply(rows,WordTextProof.Read(plain,Native(rows)))).Where(i=>i.CanFix).ToArray();
        assert(old.Length==4,"The unannotated control has four independent spacing/repetition repairs at noncontiguous native starts");
        foreach(string family in new[]{"bookmark","comment","permission"})
        {
            string xml=Xml(Row(Before),Annotated(family,Marked),Row(After));
            assert(SchemaValid(xml),"The nonempty matched annotation fixture is schema-valid: "+family);
            var proof=Observe(xml,rows);var cached=Observe(xml,rows,true);
            assert(proof.Editable.SetEquals([rows[0].Start,rows[2].Start]) && cached.Editable.SetEquals(proof.Editable) &&
                proof.Outline[rows[1].Start] is {Scope:OutlineScope.Complex,LabelThroughTabs:false} &&
                proof.Format.SetEquals(rows.Select(row=>row.Start)),
                "Only the nonempty annotated paragraph loses text permission; string/snapshot correspondence, unrelated rows and format-only observations stay available: "+family);
            var matched=new Dictionary<int,int>();var mapEditable=new Dictionary<int,bool>();
            var layout=WordXmlTableMap.Read(xml,Native(rows),onParagraphMatched:(cp,index)=>matched[cp]=index,onTextEditable:(cp,value)=>mapEditable[cp]=value);
            assert(layout.Count==3 && matched.Count==3 && mapEditable.Count==3 && !mapEditable[rows[1].Start] && mapEditable[rows[2].Start],
                "Annotation exclusion does not terminate native mapping or guess later CPs to retain unrelated layout/text proof: "+family);
            foreach(var profile in Enum.GetValues<DocumentProfile>())
            {
                var current=Checker.Check(WordTextProof.Apply(rows,proof.Editable,proof.Outline,proof.Format),profile);
                var selected=current.Where(i=>i.CanFix).ToArray();
                assert(selected.Length==2 && selected.All(i=>i.Start<rows[1].Start || i.Start>=rows[2].Start) &&
                    current.Any(i=>!i.CanFix && i.Original==Marked),
                    "The actual checker leaves annotation contents manual while retaining independent neighboring repairs in every profile: "+family);
                assert(Reject(()=>CurrentTextPlan.Validate(old,current)) && Reject(()=>RepairDependencies.Expand(old,current)),
                    "A new annotation invalidates old mechanical selections without changing their source text or native positions: "+family);
            }
            string zero=Xml(Row(Before),Annotated(family,Marked,true),Row(After));
            assert(SchemaValid(zero) && Observe(zero,rows).Editable.Count==3 && Observe(zero,rows,true).Editable.Count==3,
                "Zero-length matched point metadata does not become a blanket paragraph text ban: "+family);
            CrossParagraphChecks(family,assert);
            HeadingChecks(family,assert);
            TabChecks(family,assert);
        }
        var goBack=Row(Marked);goBack.AddFirst(Marker("bookmark",true,name:"_GoBack"),Marker("bookmark",false));
        assert(Observe(Xml(Row(Before),goBack,Row(After)),rows).Editable.Count==3,
            "Word's ordinary zero-length _GoBack bookmark leaves unrelated text eligible");
        var orphan=Row(Marked);orphan.Add(new XElement(W+"bookmarkStart",new XAttribute(W+"id",1)));
        assert(Observe(Xml(Row(Before),orphan,Row(After)),rows).Editable.Count==3,
            "The pre-existing orphan bookmark metadata control keeps its existing policy; this scope does not reinterpret unmatched annotations");
        var emptyRun=Row(Marked);emptyRun.AddFirst(Marker("bookmark",true),new XElement(W+"r",new XElement(W+"rPr",new XElement(W+"b"))),Marker("bookmark",false));
        assert(Observe(Xml(Row(Before),emptyRun,Row(After)),rows).Editable.Count==3,
            "An empty run containing only formatting between point markers is not mistaken for annotated text");
        RoleChecks(assert);
        AmbiguityAndIdChecks(assert);
    }
    private static void CrossParagraphChecks(string family,Action<bool,string> assert)
    {
        var rows=Rows(Before,Marked,After);
        string cross=Xml(Row(Before),new XElement(W+"p",Marker(family,true),Run(Marked)),new XElement(W+"p",Run(After),Marker(family,false)));
        assert(SchemaValid(cross) && Observe(cross,rows).Editable.SetEquals([rows[0].Start]),
            "A matched range crossing paragraphs protects both endpoints and cannot leave the second paragraph writable: "+family);
        string boundaryOnly=Xml(Row(Before),new XElement(W+"p",Run(Marked),Marker(family,true)),new XElement(W+"p",Marker(family,false),Run(After)));
        assert(Observe(boundaryOnly,rows).Editable.SetEquals([rows[0].Start]),
            "A cross-paragraph annotation is conservatively nonempty even when only the paragraph boundary lies between markers: "+family);
        string external=Xml(Row(Before),Marker(family,true),Row(Marked),Marker(family,false),Row(After));
        assert(SchemaValid(external) && Observe(external,rows).Editable.SetEquals([rows[0].Start,rows[2].Start]),
            "Body-level annotation markers protect their actual paragraph interval and retain later matching proof: "+family);
        var table=new XElement(W+"tbl",new XElement(W+"tblPr"),new XElement(W+"tblGrid",new XElement(W+"gridCol",new XAttribute(W+"w",4000))),
            new XElement(W+"tr",new XElement(W+"tc",Marker(family,true),Row(Marked),Marker(family,false))));
        var cellRows=rows.ToArray();cellRows[1]=cellRows[1] with {Text=Marked+"\r\a",InTable=true};
        string cell=Xml(Row(Before),table,Row(After));
        assert(SchemaValid(cell) && Observe(cell,cellRows).Editable.SetEquals([rows[0].Start,rows[2].Start]) &&
            WordXmlTableMap.Read(cell,Native(cellRows)).Count==3,
            "Cell-level range markers protect text without losing the actual cell terminator or unrelated body/table correspondence: "+family);
    }
    private static void HeadingChecks(string family,Action<bool,string> assert)
    {
        string lower=Checker.Heading.ToLowerInvariant();
        var row=Rows(lower)[0] with {Font="Arial",Size=10,Bold=false,FormatObserved=true,
            FormatEvidence=new(lower,[new("Arial",10,0,0,0)])};
        var old=Checker.Check([row]).Where(i=>i.CanFix).ToArray();var proof=Observe(Xml(Annotated(family,lower)),[row]);
        var current=Checker.Check(WordTextProof.Apply([row],proof.Editable,proof.Outline,proof.Format));
        assert(old is [{Format:not null}] && proof.Format.Contains(row.Start) && current.All(i=>!i.CanFix) && Reject(()=>CurrentTextPlan.Validate(old,current)),
            "Retained format-only observations cannot authorize a mixed heading repair that replaces annotated text: "+family);
        var correct=row with {Text=Checker.Heading+"\r",FormatEvidence=new(Checker.Heading,[new("Arial",10,0,0,0)])};
        proof=Observe(Xml(Annotated(family,Checker.Heading)),[correct]);
        var onlyFormat=Checker.Check(WordTextProof.Apply([correct],proof.Editable,proof.Outline,proof.Format)).Where(i=>i.CanFix).ToArray();
        assert(onlyFormat is [{Format:not null}] && onlyFormat[0].Original==onlyFormat[0].Replacement,
            "An independently permitted format-only repair can retain already-correct annotated heading text: "+family);
    }
    private static void TabChecks(string family,Action<bool,string> assert)
    {
        var rows=Rows("1. Nội dung đầu","2. Nội dung giữa","4.\tNội dung cuối");
        string xml=Xml(Row(rows[0].Text.TrimEnd('\r')),Row(rows[1].Text.TrimEnd('\r')),
            new XElement(W+"p",Marker(family,true),Run("4."),new XElement(W+"r",new XElement(W+"tab")),Run("Nội dung cuối"),Marker(family,false)));
        var proof=Observe(xml,rows);var current=Checker.Check(WordTextProof.Apply(rows,proof.Editable,proof.Outline,proof.Format));
        assert(proof.Outline[rows[2].Start] is {Scope:OutlineScope.Complex,LabelThroughTabs:false} && !current.Any(i=>i.CanFix && i.Kind==FixKind.OutlineOrder),
            "The tab-label fallback cannot resurrect outline text permission inside an annotated range: "+family);
    }
    private static void RoleChecks(Action<bool,string> assert)
    {
        foreach(string family in new[]{"bookmark","comment","permission"})
        {
            var xml=new XDocument(new XElement(W+"document",new XElement(W+"body",new XElement(W+"tbl",new XElement(W+"tr",
                new XElement(W+"tc",Row("Nơi nhận:"),Annotated(family,"- Như trên;"),Row("- Lưu: VT, TEST.")),
                new XElement(W+"tc",Annotated(family,"TRƯỞNG PHÒNG"),Row("Nguyễn Văn An")))))));
            // Distinct IDs keep each range unambiguous within the same story.
            foreach(var marker in xml.Descendants().Where(e=>e.Name.LocalName.EndsWith("Start",StringComparison.Ordinal) || e.Name.LocalName.EndsWith("End",StringComparison.Ordinal)).Skip(2))
                marker.SetAttributeValue(W+"id",8);
            var texts=xml.Descendants(W+"p").Select(p=>string.Concat(p.Descendants(W+"t").Select(t=>t.Value))).ToArray();
            var rows=Rows(texts);var proof=Observe(xml.ToString(),rows);
            assert(proof.Outline[rows[1].Start] is {Scope:OutlineScope.Complex,RecipientRole:null} &&
                proof.Outline[rows[3].Start] is {Scope:OutlineScope.Complex,SignatureRole:null},
                "Recipient/signature semantic shortcuts cannot publish writable roles for protected annotation contents: "+family);
        }
    }
    private static void AmbiguityAndIdChecks(Action<bool,string> assert)
    {
        var rows=Rows(Before,Marked,After);
        foreach(string family in new[]{"bookmark","comment","permission"})
        {
            foreach(var (start,end) in new[]{("7","007"),("+007","7"),("-7","-007"),("0","-0"),("+0","000"),(" 007 ","+7")})
            {
                string xml=Xml(Row(Before),new XElement(W+"p",Marker(family,true,start),Run(Marked),Marker(family,false,end)),Row(After));
                string lexical=Xml(Row(Before),new XElement(W+"p",Marker(family,true,start),Run(Marked),Marker(family,false,start)),Row(After));
                assert(SchemaValid(lexical),"The annotation decimal-ID lexical control is accepted by the XML schema: "+family+"/"+start);
                if(family!="comment")assert(SchemaValid(xml),
                    "Bookmark/permission range endpoints accept equivalent decimal lexical IDs: "+family+"/"+start+"/"+end);
                var proof=Observe(xml,rows);
                assert(proof.Editable.SetEquals([rows[0].Start,rows[2].Start]) && Observe(xml,rows,true).Editable.SetEquals(proof.Editable) &&
                    XDocument.Parse(xml).Descendants().Where(e=>e.Attribute(W+"id") is not null).Select(e=>(string)e.Attribute(W+"id")!).SequenceEqual([start,end]),
                    "Comparison-only decimal ID equivalence closes a nonempty range without rewriting signs, zeros, whitespace or source/native positions: "+family);
                string zero=Xml(Row(Before),new XElement(W+"p",Marker(family,true,start),Marker(family,false,end),Run(Marked)),Row(After));
                assert(Observe(zero,rows).Editable.Count==3,
                    "Numeric lexical aliases do not convert a uniquely paired zero-length annotation into a text ban: "+family);
            }
            string distinct=Xml(Row(Before),new XElement(W+"p",Marker(family,true,"7"),Run(Marked),Marker(family,false,"8")),Row(After));
            assert(Observe(distinct,rows).Editable.Count==3,
                "Different decimal ID values are not invented as one matched range: "+family);
            string wide=new('9',400);
            string unbounded=Xml(Row(Before),new XElement(W+"p",Marker(family,true,wide),Run(Marked),Marker(family,false,"+00"+wide)),Row(After));
            assert(Observe(unbounded,rows).Editable.SetEquals([rows[0].Start,rows[2].Start]),
                "Arbitrarily wide malformed decimal metadata cannot overflow comparison or bypass the nonempty content exclusion: "+family);
            foreach(string shape in new[]{"duplicate-start","duplicate-end","reversed","repeated-pair"})
            {
                XElement[] content=shape switch
                {
                    "duplicate-start"=>[Marker(family,true),Marker(family,true,"007"),Run(Marked),Marker(family,false)],
                    "duplicate-end"=>[Marker(family,true),Run(Marked),Marker(family,false),Marker(family,false,"+7")],
                    "reversed"=>[Marker(family,false),Run(Marked),Marker(family,true)],
                    _=>[Marker(family,true),Marker(family,false),Run(Marked),Marker(family,true),Marker(family,false)]
                };
                string xml=Xml(Row(Before),new XElement(W+"p",content),Row(After));
                assert(Observe(xml,rows).Editable.SetEquals([rows[0].Start,rows[2].Start]) &&
                    Observe(xml,rows,true).Outline[rows[1].Start].Scope==OutlineScope.Complex,
                    "Ambiguous duplicate/order metadata with both sides protects its nonempty envelope instead of publishing a guessed range: "+family+"/"+shape);
            }
        }
        string mixed=Xml(Row(Before),new XElement(W+"p",Marker("bookmark",true),Run(Marked),Marker("permission",false)),Row(After));
        assert(Observe(mixed,rows).Editable.Count==3,
            "Markers from different annotation families cannot accidentally pair merely because their numeric IDs match");
    }
}
