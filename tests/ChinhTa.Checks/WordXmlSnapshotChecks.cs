using System.Xml.Linq;
using ChinhTa.Core;

public static class WordXmlSnapshotChecks
{
    private static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static XElement P(string text)=>new(W+"p",new XElement(W+"r",new XElement(W+"t",text)));
    private static string Xml(params XElement[] rows)=>new XDocument(new XElement(W+"document",new XElement(W+"body",rows))).ToString();
    private static WordParagraphSnapshot[] Live(params string[] text)
    {
        int cp=91;
        return text.Select(t=>{var row=new WordParagraphSnapshot(cp,cp+t.Length,t,1,t.EndsWith('\a'),true,true,true);cp=row.End;return row;}).ToArray();
    }
    private sealed record Proof(IReadOnlySet<int> Text,Dictionary<int,bool> Format,Dictionary<int,OutlineParagraphInfo> Outline,string[] Warnings);
    private static Proof Read(string xml,WordParagraphSnapshot[] live,bool shared)
    {
        var f=new Dictionary<int,bool>();var o=new Dictionary<int,OutlineParagraphInfo>();var warnings=new List<string>();
        Action<WordXmlMappingException> warn=e=>warnings.Add(e.DiagnosticCode);
        Action<int,OutlineParagraphInfo> outline=(s,v)=>o[s]=v;Action<int,bool> format=(s,v)=>f[s]=v;
        var text=shared?WordTextProof.Read(WordXmlSnapshot.Parse(xml),live,warn,outline,format):WordTextProof.Read(xml,live,warn,outline,format);
        return new(text,f,o,warnings.ToArray());
    }
    private static void Compare(string xml,WordParagraphSnapshot[] live,Action<bool,string> assert,string name)
    {
        var old=Read(xml,live,false);var current=Read(xml,live,true);
        assert(old.Text.SetEquals(current.Text) && old.Format.Count==current.Format.Count && old.Format.All(x=>current.Format.GetValueOrDefault(x.Key)==x.Value) &&
            old.Outline.Count==current.Outline.Count && old.Outline.All(x=>current.Outline.GetValueOrDefault(x.Key)==x.Value) && old.Warnings.SequenceEqual(current.Warnings),
            "Shared XML preserves text/format permissions, outline, recipients and warning boundaries: "+name);
        var snapshot=WordXmlSnapshot.Parse(xml);
        foreach(int prefix in new[]{1,Math.Min(live.Length,40),live.Length}.Distinct())
        {
            var a=new List<string>();var b=new List<string>();
            var legacy=WordXmlTableMap.Read(xml,live.Take(prefix).ToArray(),e=>a.Add(e.DiagnosticCode));
            var optimized=WordXmlTableMap.Read(snapshot,live.Take(prefix).ToArray(),e=>b.Add(e.DiagnosticCode));
            assert(legacy.SequenceEqual(optimized) && a.SequenceEqual(b),"Shared table geometry preserves bounded proof and mismatch handling: "+name+" prefix "+prefix);
        }
    }
    public static void Run(Action<bool,string> assert)
    {
        Compare(Xml(P("Mở đầu"),P("Nội dung đã  duyệt.")),Live("Mở đầu\r","Nội dung đã  duyệt.\r"),assert,"plain body");
        var cell=new XElement(W+"tc",P("CƠ QUAN"),P("BAN HÀNH"));
        var table=new XElement(W+"tbl",new XElement(W+"tr",cell,new XElement(W+"tc",P("Quốc hiệu"),P("Tiêu ngữ"))));
        string tableXml=Xml(table,P("BÁO CÁO"),P("Trích yếu phần một"),P("Trích yếu phần hai"));
        var tableLive=Live("CƠ QUAN\r","BAN HÀNH\r\a","Quốc hiệu\r","Tiêu ngữ\r\a","BÁO CÁO\r","Trích yếu phần một\r","Trích yếu phần hai\r");
        Compare(tableXml,tableLive,assert,"tables and multi-paragraph subject");
        assert(WordXmlSnapshot.Parse(tableXml).ParagraphCount==7,"Shared snapshot includes every cell paragraph and every subject paragraph");
        foreach(string kind in new[]{"gridSpan","vMerge","nested"})
        {
            var complex=new XElement(table);
            if(kind=="nested")complex.Descendants(W+"tc").Last().Add(new XElement(W+"tbl",new XElement(W+"tr",new XElement(W+"tc",P("Lồng")))));
            else complex.Descendants(W+"tc").First().AddFirst(new XElement(W+"tcPr",new XElement(W+kind,new XAttribute(W+"val","2"))));
            var rows=kind=="nested"?Live("CƠ QUAN\r","BAN HÀNH\r\a","Quốc hiệu\r","Tiêu ngữ\r","Lồng\r\a"):tableLive.Take(4).ToArray();
            Compare(Xml(complex),rows,assert,kind+" table");
        }
        var recipientTable=new XElement(W+"tbl",new XElement(W+"tr",new XElement(W+"tc",P("Nơi nhận:"),P("- Như trên;"),P("- PV01;"),P("- Lưu: VT, PA04."))));
        Compare(Xml(P("Nội dung"),recipientTable),Live("Nội dung\r","Nơi nhận:\r","- Như trên;\r","- PV01;\r","- Lưu: VT, PA04.\r\a"),assert,"recipient bullets in table");
        foreach(string unsafeName in new[]{"hyperlink","sdt","fldSimple","drawing","pict","object","smartTag","fldChar","instrText","ins","del","footnoteReference"})
        {
            var p=P("Giữ nguyên");p.Add(new XElement(W+"r",new XElement(W+unsafeName)));
            Compare(Xml(p,P("Đoạn sau")),Live("Giữ nguyên\r","Đoạn sau\r"),assert,unsafeName);
        }
        var textbox=P("Ngoài hộp");textbox.Add(new XElement(W+"r",new XElement(W+"drawing",new XElement(W+"txbxContent",P("Trong hộp")))));
        Compare(Xml(textbox,P("Sau hộp")),Live("Ngoài hộp\r","Sau hộp\r"),assert,"separate textbox story");
        Compare(Xml(P("Trước"),P("Sau")),Live("Trước\r","\r","Sau\r"),assert,"Word extra body blank");
        Compare(Xml(P("Đúng"),P("Sai"),P("Sau")),Live("Đúng\r","Khác\r","Sau\r"),assert,"mismatched prefix");
        Compare(Xml(P("A"),P("B")),Live("A\r"),assert,"short live prefix");
        Compare(Xml(P("A")),Live("A\r","Thừa\r"),assert,"extra live content");
        var oldSnapshot=WordXmlSnapshot.Parse(Xml(P("Bản cũ")));
        assert(WordTextProof.Read(oldSnapshot,Live("Bản cũ\r")).Count==1 && WordTextProof.Read(oldSnapshot,Live("Bản mới\r")).Count==0 &&
            WordTextProof.Read(WordXmlSnapshot.Parse(Xml(P("Bản mới"))),Live("Bản mới\r")).Count==1,
            "Each changed document needs a new snapshot; an old XML snapshot cannot authorize newly changed text");
        var runs=new XElement(W+"p",new XElement(W+"r",new XElement(W+"rPr",new XElement(W+"rStyle",new XAttribute(W+"val","Inherited"))),new XElement(W+"t","Nội dung ")),
            new XElement(W+"r",new XElement(W+"t","đậm")));
        var runSnapshot=WordXmlSnapshot.Parse(Xml(runs));var row=Live("Nội dung đậm\r")[0];
        var plan=runSnapshot.FormatRuns(0,row);
        assert(plan is not null && plan.Start==row.Start && plan.Text=="Nội dung đậm" && plan.Runs.Count==2 && plan.Runs[1].Start==row.Start+"Nội dung ".Length,
            "Run read plan preserves actual paragraph CP and run boundaries without guessing inherited font properties");
        assert(runSnapshot.FormatRuns(0,row with {End=row.End+1}) is null && runSnapshot.FormatRuns(0,row with {Text="Khác\r"}) is null &&
            runSnapshot.FormatRuns(-1,row) is null,"Run plans reject text/CP-length mismatches and invalid paragraph indices");
        foreach(string value in new[]{"A😀B","A\tB"})
            assert(WordXmlSnapshot.Parse(Xml(P(value))).FormatRuns(0,Live(value+"\r")[0]) is null,"Ambiguous Unicode/control coordinates retain the existing font reader");
        foreach(string kind in new[]{"br","noBreakHyphen","softHyphen"})
        {
            var control=new XElement(W+"p",new XElement(W+"r",new XElement(W+"t","A"),new XElement(W+kind),new XElement(W+"t","B")));
            string value=kind=="br"?"A\vB":kind=="noBreakHyphen"?"A\u001eB":"A\u001fB";
            assert(WordXmlSnapshot.Parse(Xml(control)).FormatRuns(0,Live(value+"\r")[0]) is null,"Word control mapping retains the existing fallback: "+kind);
        }
        var many=new XElement(W+"p",Enumerable.Range(0,65).Select(_=>new XElement(W+"r",new XElement(W+"t","A"))));
        assert(WordXmlSnapshot.Parse(Xml(many)).FormatRuns(0,Live(new string('A',65)+"\r")[0]) is null,
            "Highly fragmented runs do not replace the bounded legacy font fallback");
    }
}
