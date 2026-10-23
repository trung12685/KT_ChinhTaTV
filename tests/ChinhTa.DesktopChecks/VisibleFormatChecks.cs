using ChinhTa.Desktop;

public static class VisibleFormatChecks
{
    public sealed class Font
    {
        public string Name{get;set;}="Times New Roman";
        public float Size{get;set;}=13;
        public int Bold{get;set;}=-1;
        public int Italic{get;set;}=0;
        public int Color{get;set;}=0;
    }
    public sealed class Range(string text,Font font,Range[]? letters=null)
    {
        public string Text{get;}=text;
        public Font Font{get;}=font;
        public Characters Characters{get;}=new(letters??[]);
    }
    public sealed class Characters(Range[] items)
    {
        public int Count=>items.Length;
        public Range this[int index]=>items[index-1];
    }
    public static void Run(Action<bool,string> assert)
    {
        const string text="CƠ QUAN BAN HÀNH";
        Range[] letters=text.Select(c=>new Range(c.ToString(),new Font())).ToArray();
        var aggregate=new Font {Name="",Size=9999999,Bold=9999999};
        var range=new Range(text,aggregate,letters);
        var good=VisibleFormatReader.Read(range,text,true);
        assert(good.Count==text.Count(c=>!char.IsWhiteSpace(c)) && good.All(f=>f.Known && f.Size==13 && f.Bold==-1),
            "Mixed aggregate drawing-run style can be resolved from every actual visible character range");
        assert(!VisibleFormatReader.Read(range,text,false).Single().Known,
            "An unproven span cannot gain the character fallback just because the aggregate is mixed");
        letters[3].Font.Color=255;
        assert(VisibleFormatReader.Read(range,text,true).Any(f=>f.Color==255),"A real differently colored visible letter is never ignored by the fallback");
        letters[3].Font.Color=0;letters[4].Font.Size=12;
        assert(VisibleFormatReader.Read(range,text,true).Any(f=>f.Size==12),"A real differently sized visible letter remains observable");
        letters[4].Font.Size=13;letters[3].Font.Name="";
        assert(VisibleFormatReader.Read(range,text,true).Any(f=>!f.Known),"An unreadable visible letter keeps mixed-format handling closed");
        bool changed=false;try{VisibleFormatReader.Read(range,"Khác",true);}catch(InvalidOperationException){changed=true;}
        assert(changed,"A changed whole range fails before inspecting character formats");
        var missing=new Range(text,aggregate,letters.Skip(1).ToArray());changed=false;
        try{VisibleFormatReader.Read(missing,text,true);}catch(InvalidOperationException){changed=true;}
        assert(changed,"Missing or hidden character positions cannot be guessed from text indices");
        var uniform=new Range(text,new Font());
        assert(VisibleFormatReader.Read(uniform,text,true).Count==1,"A known aggregate uses the fast read without walking characters");
    }
}
