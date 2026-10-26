using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using ChinhTa.Core;

// This measures ONLY XML proof/model work, not Word, COM, XPS or total UI time.
public static class ReadPipelineBenchmark
{
    public static void Run()
    {
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var results=new List<object>();
        foreach(int count in new[]{100,1000,5000})
        {
            XElement P(int i)=>new(w+"p",new XElement(w+"pPr",new XElement(w+"pStyle",new XAttribute(w+"val","Body"))),
                Enumerable.Range(0,4).Select(r=>new XElement(w+"r",new XElement(w+"t",$"Đoạn {i} phần {r}: kiểm tra văn bản tiếng Việt. "))));
            var body=new XElement(w+"body",new XElement(w+"tbl",new XElement(w+"tr",new XElement(w+"tc",Enumerable.Range(0,12).Select(P)),
                new XElement(w+"tc",Enumerable.Range(12,12).Select(P)))),Enumerable.Range(24,count-24).Select(P));
            var xml=new XDocument(new XElement(w+"document",body),null);
            // Same embedded style part shape as the live WordOpenXML snapshot.
            xml.Root!.Add(new XElement(w+"styles",new XElement(w+"style",new XAttribute(w+"styleId","Body"),new XAttribute(w+"type","paragraph"),
                new XElement(w+"pPr",new XElement(w+"ind",new XAttribute(w+"left","0"))))));
            string source=xml.ToString(SaveOptions.DisableFormatting);int cp=73;
            var live=body.Descendants(w+"p").Select(p=>
            {
                string text=string.Concat(p.Descendants(w+"t").Select(t=>t.Value))+"\r";
                bool cell=p.Parent!.Name==w+"tc";if(cell && !p.ElementsAfterSelf(w+"p").Any())text+="\a";
                var row=new WordParagraphSnapshot(cp,cp+text.Length,text,1,cell,true);cp=row.End;return row;
            }).ToArray();
            string Signature(bool shared)
            {
                var snapshot=shared?WordXmlSnapshot.Parse(source):null;
                var outline=new Dictionary<int,OutlineParagraphInfo>();var format=new Dictionary<int,bool>();
                Action<WordXmlMappingException> fail=e=>throw e;
                var editable=snapshot is null?WordTextProof.Read(source,live,fail,(s,v)=>outline[s]=v,(s,v)=>format[s]=v):
                    WordTextProof.Read(snapshot,live,fail,(s,v)=>outline[s]=v,(s,v)=>format[s]=v);
                var layout=snapshot is null?WordXmlTableMap.Read(source,live.Take(40).ToArray(),fail):WordXmlTableMap.Read(snapshot,live.Take(40).ToArray(),fail);
                return JsonSerializer.Serialize(new {Text=editable.Order().ToArray(),Format=format,Outline=outline,Layout=layout});
            }
            string expected=Signature(false);
            if(Signature(true)!=expected)throw new InvalidOperationException("Benchmark reader outputs differ");
            for(int warm=0;warm<3;warm++){Signature(false);Signature(true);}
            var times=new[]{new List<double>(),new List<double>()};var allocations=new[]{new List<long>(),new List<long>()};
            for(int repeat=0;repeat<9;repeat++)
                foreach(int mode in repeat%2==0?new[]{0,1}:new[]{1,0})
                {
                    long bytes=GC.GetAllocatedBytesForCurrentThread();var clock=Stopwatch.StartNew();
                    string result=Signature(mode==1);double ms=clock.Elapsed.TotalMilliseconds;
                    allocations[mode].Add(GC.GetAllocatedBytesForCurrentThread()-bytes);times[mode].Add(ms);
                    if(result!=expected)throw new InvalidOperationException("Benchmark reader output drift");
                }
            double Median(List<double> v)=>v.Order().ElementAt(v.Count/2);
            results.Add(new {Paragraphs=count,Runs=count*4,XmlCharacters=source.Length,IdenticalProofAndLayout=true,
                LegacyMedianMs=Math.Round(Median(times[0]),3),SharedMedianMs=Math.Round(Median(times[1]),3),
                LegacyAllocatedBytes=allocations[0].Order().ElementAt(4),SharedAllocatedBytes=allocations[1].Order().ElementAt(4)});
        }
        Console.WriteLine(JsonSerializer.Serialize(new {Scope="XML parsing, text/format proof, outline and first-40 table map; includes signature serialization equally on both sides",
            Excludes="Word startup/open, COM font observation, pagination, XPS, editing, UI; no end-to-end speed claim",Samples=9,Warmups=3,AlternatingOrder=true,Results=results},
            new JsonSerializerOptions {WriteIndented=true}));
    }
}
