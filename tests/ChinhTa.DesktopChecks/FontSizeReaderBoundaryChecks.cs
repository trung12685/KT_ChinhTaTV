using System.Runtime.InteropServices;
using ChinhTa.Core;
using ChinhTa.Desktop;

// Managed observations only: the production reader must prove native range
// ownership without assuming that Word CPs and UTF-16 lengths are identical.
public static class FontSizeReaderBoundaryChecks
{
    public sealed class Font(float size,Action? onRead=null,Exception? failure=null)
    {
        private float size=size;
        public int Reads{get;private set;}
        public float Size
        {
            get{Reads++;onRead?.Invoke();if(failure is not null)throw failure;return size;}
            set=>size=value;
        }
    }
    public sealed class Range(int start,int end,string text,Font font,Range[]? children=null,Exception? fontFailure=null,int? count=null)
    {
        public int Start{get;set;}=start;
        public int End{get;set;}=end;
        public string Text{get;set;}=text;
        public Font Font{get{if(fontFailure is not null)throw fontFailure;return font;}}
        public Children Characters=>new(children??[],count);
    }
    public sealed class Children(Range[] items,int? count=null)
    {public int Count=>count??items.Length;public Range this[int index]=>items[index-1];}

    private static bool Rejected(Action read)
    {try{read();return false;}catch(InvalidOperationException){return true;}}

    public static void Run(Action<bool,string> assert)
    {
        var a=new Range(40,41,"A",new(13));var b=new Range(42,43,"B",new(14));
        var blank=new Range(41,42," ",new(8),fontFailure:new Exception("Whitespace must not read Font"));
        var ordinary=new Range(40,43,"A B",new(9999999),[a,blank,b]);
        var before=VisibleFormatReader.ReadSizes(ordinary,ordinary.Text);
        assert(before.Text=="A B" && before.Sizes.SequenceEqual(new float?[]{13,null,14}),
            "The size reader proves contiguous native character ranges and retains each visible source size without observing whitespace fonts");
        b.Font.Size=13;
        assert(!before.Matches(VisibleFormatReader.ReadSizes(ordinary,ordinary.Text)),
            "A post-save size proof rejects a retained 14pt glyph flattened to equally legal 13pt after exact range validation");

        var emoji=new Range(10,11,"😀",new(14));
        var space=new Range(11,12," ",new(8),fontFailure:new Exception("Whitespace must not read Font"));
        var accent=new Range(12,14,"e\u0301",new(13));
        var unicode=new Range(10,14,"😀 e\u0301",new(9999999),[emoji,space,accent]);
        var observed=VisibleFormatReader.ReadSizes(unicode,unicode.Text);
        assert(observed.Sizes.SequenceEqual(new float?[]{14,14,null,13,13}) && unicode.End-unicode.Start!=unicode.Text.Length,
            "One-CP emoji and multi-CP decomposed text retain full UTF-16 evidence without a guessed native CP/string-length equality");

        foreach(var second in new[]
        {
            new Range(90,91,"B",new(14)),
            new Range(11,13,"B",new(14)),
            new Range(10,12,"B",new(14)),
            new Range(12,13,"B",new(14)),
            new Range(11,11,"B",new(14)),
            new Range(11,10,"B",new(14)),
            new Range(11,12,"",new(14)),
            new Range(11,12,"X",new(14))
        })
        {
            var first=new Range(10,11,"A",new(13));
            var malformed=new Range(10,12,"AB",new(9999999),[first,second]);
            assert(Rejected(()=>VisibleFormatReader.ReadSizes(malformed,"AB")),
                "Matching joined text cannot certify outside, overlapping, gapped, empty, reversed or stale character observations");
        }
        var late=new Range(11,12,"A",new(13));
        assert(Rejected(()=>VisibleFormatReader.ReadSizes(new Range(10,12,"AB",new(9999999),[late,new(12,13,"B",new(14))]),"AB")) && late.Font.Reads==0,
            "A first child that starts outside the approved cursor is rejected before its font can become source evidence");
        assert(Rejected(()=>VisibleFormatReader.ReadSizes(new Range(10,13,"AB",new(9999999),[new(10,11,"A",new(13)),new(11,12,"B",new(14))]),"AB")),
            "A complete-looking text sequence cannot certify an unobserved native tail of the containing range");
        foreach(var bounds in new[]{(-1,1),(10,10),(10,9)})
            assert(Rejected(()=>VisibleFormatReader.ReadSizes(new Range(bounds.Item1,bounds.Item2,"AB",new(13)),"AB")),
                "Negative, empty or reversed containing bounds cannot create a retained-size fingerprint");
        foreach(int count in new[]{0,-1,5})
            assert(Rejected(()=>VisibleFormatReader.ReadSizes(new Range(10,12,"AB",new(13),count:count),"AB")),
                "An absent or impossible character cardinality fails before reading invented child observations");

        foreach(string drift in new[]{"start","end","text"})
        {
            Range? parent=null;
            var child=new Range(20,21,"A",new(13,()=>
            {
                if(drift=="start")parent!.Start=19;
                else if(drift=="end")parent!.End=22;
                else parent!.Text="X";
            }));
            parent=new Range(20,21,"A",new(13),[child]);
            assert(Rejected(()=>VisibleFormatReader.ReadSizes(parent,"A")),
                "The size reader rechecks live parent ownership and text after all child/font reads: "+drift);
        }
        foreach(float size in new[]{0,float.NaN,float.PositiveInfinity,1639,9999999})
            assert(Rejected(()=>VisibleFormatReader.ReadSizes(new Range(0,1,"A",new(13),[new(0,1,"A",new(size))]),"A")),
                "Unknown or out-of-range visible sizes remain invalid even when the native character coverage is exact");

        var disconnected=new COMException("synthetic disconnected size observation",unchecked((int)0x80010108));
        Exception? failure=null;
        try{VisibleFormatReader.ReadSizes(new Range(0,1,"A",new(13),[new(0,1,"A",new(13,failure:disconnected))]),"A");}
        catch(Exception error){failure=error;}
        assert(ReferenceEquals(failure,disconnected),
            "A disconnected size read keeps its original COM failure for the session recovery boundary instead of publishing partial evidence");
        assert(Rejected(()=>VisibleFormatReader.ReadSizes(ordinary,"Khác")),
            "An obsolete expected paragraph text is rejected before beginning a retained-size read");
    }
}
