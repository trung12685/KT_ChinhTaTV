using ChinhTa.Core;
using ChinhTa.Desktop;

public static class BodyEmphasisWriterChecks
{
    public sealed class Font(float size,int bold,int italic,int underline)
    {
        private float current=size;
        public string Name{get;set;}="Arial";
        public int Color{get;set;}=255;
        public int Bold=>bold;
        public int Italic=>italic;
        public int Underline=>underline;
        public int SizeWrites{get;private set;}
        public float Size{get=>current;set{SizeWrites++;current=value;}}
    }
    public sealed class Aggregate(Font[] fonts)
    {
        public int Writes{get;private set;}
        public string Name{set{Writes++;foreach(var f in fonts)f.Name=value;}}
        public int Color{set{Writes++;foreach(var f in fonts)f.Color=value;}}
        public float Size{set=>throw new InvalidOperationException("Do not flatten visible or whitespace sizes");}
        public int Bold{get=>throw new InvalidOperationException("No aggregate emphasis read");set=>throw new InvalidOperationException("Do not normalize bold");}
        public int Italic{get=>throw new InvalidOperationException("No aggregate emphasis read");set=>throw new InvalidOperationException("Do not normalize italic");}
    }
    public sealed class Child(int start,string text,Font font)
    {public int Start=>start;public int End=>start+text.Length;public string Text=>text;public Font Font=>font;}
    public sealed class Children(Child[] items)
    {public int Count=>items.Length;public Child this[int i]=>items[i-1];}
    public sealed class Range(string text,Child[] children,Aggregate font)
    {public int Start=>100;public int End=>100+text.Length;public string Text=>text;public Aggregate Font=>font;public Children Characters=>new(children);}
    public static void Run(Action<bool,string> assert)
    {
        Font[] fonts=[new(12,-1,0,1),new(14,0,0,0),new(13,0,-1,1),new(8,0,0,0),new(14,-1,-1,1)];
        const string text="A B C";var children=text.Select((c,i)=>new Child(100+i,c.ToString(),fonts[i])).ToArray();var aggregate=new Aggregate(fonts);
        var range=new Range(text,children,aggregate);var retained=fonts.Select(f=>(f.Bold,f.Italic,f.Underline)).ToArray();
        var format=new CharacterFormat(13,false,null,ComponentRole.Body,true,true,true);
        VisibleFormatReader.ApplyCharacters(range,text,format);
        assert(aggregate.Writes==2 && fonts.All(f=>f.Name=="Times New Roman" && f.Color==0) &&
            fonts.Select(f=>f.Size).SequenceEqual(new[]{13f,14,13,8,14}) && fonts.Select(f=>f.SizeWrites).SequenceEqual(new[]{1,0,0,0,0}),
            "Body COM writer changes name/color and invalid visible sizes only, preserving valid sizes and every whitespace size");
        assert(fonts.Select(f=>(f.Bold,f.Italic,f.Underline)).SequenceEqual(retained),
            "Body COM writer never reads/writes aggregate emphasis or destroys bold/italic/underline");
        VisibleFormatReader.ApplyCharacters(range,text,format);
        assert(fonts.Sum(f=>f.SizeWrites)==1,"Already valid glyph sizes receive no extra writes on a repeated current formatting action");
        int writes=aggregate.Writes;bool rejected=false;
        try{VisibleFormatReader.ApplyCharacters(range,"Different",format);}catch(InvalidOperationException){rejected=true;}
        assert(rejected && aggregate.Writes==writes,"Stale body text is rejected before even font name/color setters run");
        foreach(var bad in new[]{format with {Role=ComponentRole.Agency},format with {Bold=false},format with {Italic=true}})
        {
            bool invalid=false;try{VisibleFormatReader.ApplyCharacters(range,text,bad);}catch(InvalidOperationException){invalid=true;}
            assert(invalid && aggregate.Writes==writes,"Invalid emphasis-retention roles cannot use the generic character writer");
        }
    }
}
