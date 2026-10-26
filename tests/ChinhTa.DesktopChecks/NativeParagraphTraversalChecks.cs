using System.Collections;
using System.Runtime.InteropServices;
using ChinhTa.Desktop;

public static class NativeParagraphTraversalChecks
{
    public sealed record Item(int Start,int End,string Text);
    public sealed class Rows(Item[] values,string mode):IEnumerable
    {
        public int IndexReads { get; private set; }
        public object At(int index){IndexReads++;return values[index-1];}
        public IEnumerator GetEnumerator()
        {
            if(mode=="unavailable")throw new InvalidCastException();
            IEnumerable<Item> source=mode switch {"short"=>values[..^1],"extra"=>values.Append(values[^1]),
                "duplicate"=>values.Select((v,i)=>i==3?values[2]:v),_=>values};
            return Enumerate(source).GetEnumerator();
        }
        private IEnumerable<Item> Enumerate(IEnumerable<Item> source)
        {
            int count=0;
            foreach(var item in source)
            {if(mode=="late failure" && count++==4)throw new COMException("Enum",unchecked((int)0x80004005));yield return item;}
        }
    }
    public static void Run(Action<bool,string> assert)
    {
        var values=Enumerable.Range(0,12).Select(i=>new Item(i*20,i*20+10,"Đoạn "+i)).ToArray();
        bool Valid(IReadOnlyList<Item> rows)=>rows.Select((r,i)=>i==0 || r.Start>=rows[i-1].End).All(v=>v);
        foreach(string mode in new[]{"normal","unavailable","short","extra","duplicate","late failure"})
        {
            var rows=new Rows(values,mode);var metrics=new ParagraphTraversalMeasurements();int released=0;
            var actual=NativeParagraphTraversal.Read(rows,values.Length,rows.At,(value,ordinal)=>(Item)value,
                value=>{if(value is Item)released++;},true,Valid,metrics);
            assert(actual.SequenceEqual(values),"Native paragraph traversal preserves exact row text/CPs after "+mode);
            assert(mode=="normal"?metrics.Sequential && rows.IndexReads==0:metrics.FellBack && rows.IndexReads==values.Length,
                "A complete sequential traversal avoids indexed lookup; any incomplete attempt uses full legacy fallback: "+mode);
            assert(released>=values.Length,"Every observed paragraph is released, including discarded partial attempts: "+mode);
        }
        var legacy=new Rows(values,"unavailable");var legacyMetrics=new ParagraphTraversalMeasurements();
        assert(NativeParagraphTraversal.Read(legacy,values.Length,legacy.At,(v,i)=>(Item)v,_=>{},false,Valid,legacyMetrics).SequenceEqual(values) &&
            legacy.IndexReads==values.Length && !legacyMetrics.FellBack,"Disabled optimization uses only the indexed reader");
        var disconnected=new Rows(values,"normal");bool failed=false;
        try{NativeParagraphTraversal.Read<Item>(disconnected,values.Length,disconnected.At,(v,i)=>i==5?throw new COMException("RPC",unchecked((int)0x800706BE)):(Item)v,_=>{},true,Valid,new());}
        catch(COMException){failed=true;}
        assert(failed && disconnected.IndexReads==0,"Disconnected enumeration observations propagate to session recovery rather than retrying stale COM objects");
    }
}
