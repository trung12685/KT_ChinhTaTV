using System.Runtime.InteropServices;
using System.Xml.Linq;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class RunFormatReadChecks
{
    public sealed class Range(int start,string text,VisibleFormatChecks.Font font,Range[]? children=null)
    {
        public int Start { get; set; }=start;
        public int End { get; set; }=start+text.Length;
        public string Text { get; set; }=text;
        public VisibleFormatChecks.Font Font { get; }=font;
        public Collection Characters { get; }=new(children??[]);
    }
    public sealed class Collection(Range[] rows)
    {public int Count=>rows.Length;public Range this[int index]=>rows[index-1];}
    public static void Run(Action<bool,string> assert)
    {
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        string[] parts=["CƠ QUAN ","BAN HÀNH"];
        const int start=631;string text=string.Concat(parts);
        string xml=new XDocument(new XElement(w+"document",new XElement(w+"body",new XElement(w+"p",
            parts.Select(t=>new XElement(w+"r",new XElement(w+"t",t))))))).ToString();
        var plan=WordXmlSnapshot.Parse(xml).FormatRuns(0,new(start,start+text.Length+1,text+"\r",1,false,true))!;
        var glyphs=text.Select((c,i)=>new Range(start+i,c.ToString(),new VisibleFormatChecks.Font {Bold=i<parts[0].Length?0:-1})).ToArray();
        var aggregate=new VisibleFormatChecks.Font {Name="",Size=9999999,Bold=9999999};
        var whole=new Range(start,text,aggregate,glyphs);
        var children=plan.Runs.Select((p,i)=>new Range(p.Start,p.Text,new VisibleFormatChecks.Font {Bold=i==0?0:-1})).ToArray();
        object Factory(int left,int right)=>children.Single(p=>p.Start==left && p.End==right);
        var legacyMetrics=new FormatReadMeasurements();
        var legacy=VisibleFormatReader.Read(whole,text,true,measurements:legacyMetrics);
        var fastMetrics=new FormatReadMeasurements();
        var fast=VisibleFormatReader.Read(whole,text,true,plan,Factory,fastMetrics);
        assert(legacy.ToHashSet().SetEquals(fast) && fast.Count==2 && fastMetrics.PartitionSuccesses==1 && fastMetrics.PartitionReads==2 &&
            fastMetrics.CharacterReads==0 && legacyMetrics.CharacterReads==text.Length,
            "Two verified Word run reads preserve the visible mixed-font evidence while avoiding per-character COM reads");
        bool madeRange=false;
        var uniform=new Range(start,text,new());
        assert(VisibleFormatReader.Read(uniform,text,true,plan,(a,b)=>{madeRange=true;return Factory(a,b);}).Count==1 && !madeRange,
            "Uniform aggregate keeps its existing fast path before opening XML run ranges");
        foreach(string defect in new[]{"text","start","end","unknown"})
        {
            var child=children[1];var oldText=child.Text;int oldStart=child.Start,oldEnd=child.End;float oldSize=child.Font.Size;
            if(defect=="text")child.Text="Không khớp";
            if(defect=="start")child.Start++;
            if(defect=="end")child.End++;
            if(defect=="unknown")child.Font.Size=9999999;
            int calls=0;var measurements=new FormatReadMeasurements();
            var actual=VisibleFormatReader.Read(whole,text,true,plan,(a,b)=>children[calls++],measurements);
            assert(actual.SequenceEqual(legacy) && measurements.PartitionSuccesses==0 && measurements.PartitionFallbacks==1 &&
                measurements.CharacterReads==text.Length,"Incomplete/unknown run evidence falls back for the entire visible span: "+defect);
            child.Text=oldText;child.Start=oldStart;child.End=oldEnd;child.Font.Size=oldSize;
        }
        var noProof=new FormatReadMeasurements();
        assert(!VisibleFormatReader.Read(whole,text,false,plan,Factory,noProof).Single().Known && noProof.PartitionReads==0,
            "A run plan never opens format permission on an unproven span");
        whole.Start++;whole.End++;var staleCp=new FormatReadMeasurements();madeRange=false;
        assert(VisibleFormatReader.Read(whole,text,true,plan,(a,b)=>{madeRange=true;return Factory(a,b);},staleCp).SequenceEqual(legacy) &&
            !madeRange && staleCp.PartitionFallbacks==1,"Repeated identical text at a different live Word CP cannot reuse a run plan from the previous paragraph");
        whole.Start--;whole.End--;
        var changed=false;
        try{VisibleFormatReader.Read(whole,text+"X",true,plan,Factory);}catch(InvalidOperationException){changed=true;}
        assert(changed,"A stale containing range fails before any optimized child reads");
        bool disconnected=false;
        try{VisibleFormatReader.Read(whole,text,true,plan,(_,_)=>throw new COMException("RPC",unchecked((int)0x800706BE)));}
        catch(COMException){disconnected=true;}
        assert(disconnected,"Disconnected run reads propagate to Word recovery and cannot be disguised as font fallback success");
        var failing=new FormatReadMeasurements();
        assert(VisibleFormatReader.Read(whole,text,true,plan,(_,_)=>throw new COMException("Unavailable",unchecked((int)0x80004005)),failing).SequenceEqual(legacy) && failing.PartitionFallbacks==1,
            "A recoverable run-property failure retains the full legacy evidence without accepting a partial run set");
    }
}
