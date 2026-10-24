using System.Runtime.InteropServices;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class FormatObservationChecks
{
    public sealed class Counts { public int Fonts,Words,Characters; }
    public sealed class Range(string text,VisibleFormatChecks.Font font,Counts calls,Range[]? words=null,Range[]? characters=null,Exception? wordError=null)
    {
        public string Text=>text;
        public VisibleFormatChecks.Font Font {get{calls.Fonts++;return font;}}
        public Children Words {get{calls.Words++;if(wordError is not null)throw wordError;return new(words??[]);}}
        public Children Characters {get{calls.Characters++;return new(characters??[]);}}
    }
    public sealed class Children(Range[] items)
    {public int Count=>items.Length;public Range this[int i]=>items[i-1];}
    public static void Run(Action<bool,string> assert)
    {
        VisibleFormatChecks.Font Good(float size=13,int bold=-1,int color=0)=>new(){Size=size,Bold=bold,Color=color};
        VisibleFormatChecks.Font Unknown()=>new(){Name="",Size=9999999,Bold=9999999,Italic=9999999,Color=9999999};
        const string text="CƠ QUAN BAN HÀNH";
        var counts=new Counts();var words=new[]{"CƠ ","QUAN ","BAN ","HÀNH"}.Select(s=>new Range(s,Good(),counts)).ToArray();
        var root=new Range(text,Unknown(),counts,words:words);
        assert(VisibleFormatReader.Read(root,text,true).Count==4 && counts.Fonts==5 && counts.Words==1 && counts.Characters==0,
            "An undefined whole range resolves through four real uniform word ranges with five font reads and no character walk");
        counts=new();var chars="BAN ".Select(c=>new Range(c.ToString(),Good(c=='A'?14:13),counts)).ToArray();
        words=[new("CƠ QUAN ",Good(),counts),new("BAN ",Unknown(),counts,characters:chars),new("HÀNH",Good(),counts)];
        root=new(text,Unknown(),counts,words:words);
        var observed=VisibleFormatReader.Read(root,text,true);
        assert(counts.Characters==1 && observed.Any(f=>f.Size==14) && observed.All(f=>f.Known),
            "Only an unresolved word needs actual character ranges; variation between two valid sizes stays observable");
        var row=new Paragraph(0,text,FormatObserved:true,FormatMixed:true,FormatEvidence:new(text,observed.Select(f=>new ObservedFont(f.Name,f.Size,f.Bold,f.Italic,f.Color))));
        assert(new FormatPolicy(13,14,false,true).Assess(row).State==FormatState.Correct &&
            VisibleFormatReader.Matches(root,text,new(13,false,true,PreserveSize:true),null),
            "The same pre/post reader accepts legally mixed sizes without normalizing away the distinction");
        chars[1].Font.Color=255;
        assert(!VisibleFormatReader.Matches(root,text,new(13,false,true,PreserveSize:true),null),
            "One actually red glyph still fails the exact same post-save proof even when every other glyph is valid");
        chars[1].Font.Color=0;chars[1].Font.Name="";
        assert(!VisibleFormatReader.Matches(root,text,new(13,false,true,PreserveSize:true),null),
            "An unreadable glyph cannot be hidden by a valid first word or preserve-size policy");
        counts=new();var wholeChars=text.Select(c=>new Range(c.ToString(),Good(),counts)).ToArray();
        root=new(text,Unknown(),counts,words:[new(text+"outside",Good(),counts)],characters:wholeChars);
        assert(VisibleFormatReader.Read(root,text,true).All(f=>f.Known) && counts.Characters==1,
            "Word ranges extending past the requested text fall back to a complete independently verified character sequence");
        counts=new();root=new(text,Unknown(),counts,characters:text.Select(c=>new Range(c.ToString(),Good(),counts)).ToArray(),
            wordError:new COMException("synthetic",unchecked((int)0x80004005)));
        assert(VisibleFormatReader.Read(root,text,true).All(f=>f.Known) && counts.Characters==1,
            "A non-disconnection Words failure can use the complete verified character fallback");
        counts=new();root=new(text,Unknown(),counts,wordError:new COMException("synthetic",unchecked((int)0x80010108)));bool disconnected=false;
        try{VisibleFormatReader.Read(root,text,true);}catch(COMException error) when(WordConnectionPolicy.IsDisconnected(error)){disconnected=true;}
        assert(disconnected && counts.Characters==0,"Disconnected Word errors still reach session recovery instead of becoming formatting evidence");
        counts=new();root=new(text,Unknown(),counts,words:[new("CƠ",Good(),counts)],characters:[new("CƠ",Good(),counts)]);bool mismatch=false;
        try{VisibleFormatReader.Read(root,text,true);}catch(InvalidOperationException){mismatch=true;}
        assert(mismatch,"Neither an incomplete word list nor an incomplete character list can claim complete font coverage");
        counts=new();root=new(text,Unknown(),counts,words:words);
        assert(!VisibleFormatReader.Read(root,text,false).Single().Known && counts.Words==0 && counts.Characters==0,
            "An unproven caller never enters either fallback path");
        counts=new();root=new(text,Good(),counts);
        assert(VisibleFormatReader.Read(root,text,true).Count==1 && counts.Fonts==1 && counts.Words==0 && counts.Characters==0,
            "Uniform documents retain the single aggregate fast path");
        counts=new();string longText=string.Concat(Enumerable.Repeat("báo cáo ",300));
        var longWords=Enumerable.Repeat("báo ",300).SelectMany(_=>new[]{new Range("báo ",Good(),counts),new Range("cáo ",Good(),counts)}).ToArray();
        root=new(longText,Unknown(),counts,words:longWords);
        assert(VisibleFormatReader.Read(root,longText,true).Count==600 && counts.Fonts==601 && counts.Characters==0,
            "A 2400-character mixed-aggregate fixture uses 601 font reads instead of 1801 aggregate/visible-character reads; this is a call-count fixture, not an Office timing claim");
        counts=new();const string sizedText="BÁO CÁO\v";
        var sizedChars=sizedText.Select((c,i)=>new Range(c.ToString(),Good(i<3?13:14),counts)).ToArray();
        root=new(sizedText,Unknown(),counts,characters:sizedChars);
        var sizeProof=VisibleFormatReader.ReadSizes(root,sizedText);
        assert(sizeProof.Text==sizedText && sizeProof.Sizes[0]==13 && sizeProof.Sizes[4]==14 && sizeProof.Sizes[3] is null &&
            sizeProof.Sizes[^1] is null,"Size-only Word evidence covers actual characters and ignores the styles of spaces and soft breaks");
        sizedChars[4].Font.Size=13;
        assert(!sizeProof.Matches(VisibleFormatReader.ReadSizes(root,sizedText)),"Post-save size proof detects a valid 14pt glyph flattened to equally valid 13pt");
    }
}
