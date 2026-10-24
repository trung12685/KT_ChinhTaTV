using System.Runtime.InteropServices;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class CharacterStyleReaderChecks
{
    public sealed class Font
    {
        public string Name{get;set;}="Times New Roman";
        public float Size{get;set;}=13;
        public int Bold{get;set;}=-1;
        public int Italic{get;set;}=0;
        public int Color{get;set;}=0;
        public int Underline{get;set;}=0;
    }
    public sealed class Layout
    {
        public int Alignment{get;set;}=1;
        public float LeftIndent{get;set;}
        public float RightIndent{get;set;}
        public float FirstLineIndent{get;set;}
    }
    public sealed class Range(int start,int end,string text,Font font,Range[]? children=null,Layout? layout=null,Exception? error=null)
    {
        public int Start=>start;
        public int End=>end;
        public string Text{get;set;}=text;
        public Font Font {get{if(error is not null)throw error;return font;}}
        public Layout ParagraphFormat {get;}=layout??new();
        public Children Characters=>new(children??[]);
    }
    public sealed class Children(Range[] items)
    {public int Count=>items.Length;public Range this[int i]=>items[i-1];}
    public static void Run(Action<bool,string> assert)
    {
        const string source="Báo cáo";int cp=300;
        var glyphs=source.Select((c,i)=>new Range(cp+i,cp+i+1,c.ToString(),new Font {Size=i%2==0?13:14,Underline=i<3?1:0})).ToArray();
        var root=new Range(cp,cp+source.Length,source,new(),glyphs);var before=VisibleFormatReader.ReadStyles(root,source);
        var edit=new Issue(cp,source.Length,source,source.ToUpperInvariant(),"",false,FixKind.DocumentTypeText);
        var expected=before.Project([edit],i=>"");
        foreach(var glyph in glyphs)glyph.Text=glyph.Text.ToUpperInvariant();root.Text=source.ToUpperInvariant();
        assert(expected.Matches(VisibleFormatReader.ReadStyles(root,root.Text)),
            "Real-range style evidence accepts capitalization preserving mixed sizes, underline and paragraph alignment/indents");
        glyphs[0].Font.Underline=0;
        assert(!expected.Matches(VisibleFormatReader.ReadStyles(root,root.Text)),
            "Correct uppercase text cannot hide loss of one glyph underline");
        glyphs[0].Font.Underline=1;glyphs[1].Font.Size=13;
        assert(!expected.Matches(VisibleFormatReader.ReadStyles(root,root.Text)),
            "Case-only verification rejects a legal 14pt glyph flattened to equally legal 13pt");
        glyphs[1].Font.Size=14;root.ParagraphFormat.Alignment=0;
        assert(!expected.Matches(VisibleFormatReader.ReadStyles(root,root.Text)),
            "Case-only verification also rejects changed paragraph alignment");
        root.ParagraphFormat.Alignment=1;
        foreach(var glyph in glyphs)glyph.Font.Color=-16777216;
        assert(expected.Matches(VisibleFormatReader.ReadStyles(root,root.Text)),
            "Word's equivalent black encoding after save does not create a false preservation failure");
        var emoji=new Range(10,11,"😀",new Font {Size=14});var blank=new Range(11,12," ",new(),error:new Exception("space must not read font"));
        var accent=new Range(12,14,"e\u0301",new Font {Underline=1});
        var unicode=new Range(10,14,"😀 e\u0301",new(),[emoji,blank,accent]);
        var unicodeProof=VisibleFormatReader.ReadStyles(unicode,unicode.Text);
        assert(unicodeProof.Complete && unicodeProof.Styles.Count==unicode.Text.Length && unicodeProof.Styles[0]!.Font.Size==14 &&
            unicodeProof.Styles[1]!.Font.Size==14 && unicodeProof.Styles[2] is null && unicodeProof.Styles[^1]!.Underline==1,
            "Style reading uses actual CP ranges and exact UTF-16 coverage for emoji, combining text and ignored whitespace");
        bool Reject(Action action){try{action();return false;}catch(Exception e) when(e is InvalidOperationException or COMException){return true;}}
        foreach(var last in new[]{new Range(21,23,"B",new()),new Range(20,22,"B",new()),new Range(22,22,"B",new()),
            new Range(21,22,"X",new()),new Range(21,22,"B",new Font {Underline=9999999}),new Range(21,22,"B",new Font {Name=""})})
        {
            var invalid=new Range(20,22,"AB",new(),[new(20,21,"A",new()),last]);
            assert(Reject(()=>VisibleFormatReader.ReadStyles(invalid,"AB")),
                "Out-of-bounds, overlapping, incomplete or unknown glyph observations cannot become case style evidence");
        }
        assert(Reject(()=>VisibleFormatReader.ReadStyles(new Range(20,22,"AB",new(),[new(20,21,"A",new())]),"AB")) &&
            Reject(()=>VisibleFormatReader.ReadStyles(root,"Khác")),"Incomplete text or stale expected text fails style reading");
        var disconnected=new Range(0,1,"A",new(),[new(0,1,"A",new(),error:new COMException("synthetic",unchecked((int)0x80010108)))]);
        assert(Reject(()=>VisibleFormatReader.ReadStyles(disconnected,"A")),"Disconnected COM reads propagate to session recovery rather than inventing a style proof");
        var badLayout=new Range(0,1,"A",new(),[new(0,1,"A",new())],new Layout {LeftIndent=9999999});
        assert(Reject(()=>VisibleFormatReader.ReadStyles(badLayout,"A")),"An undefined paragraph indent cannot be treated as a known case layout");
        var type=new Issue(0,source.Length,source,source.ToUpperInvariant(),"",false,FixKind.DocumentTypeText);
        var font=type with {Kind=FixKind.DocumentHeadingFormat,Replacement=source,CharacterFormat=new(13,false,true,PreserveSize:true)};
        var model=new IssueReviewModel([type,font]);model.Choices.Single(c=>c.Issue.Kind==FixKind.DocumentTypeText).IsChecked=true;
        assert(model.Choices.Count(c=>c.IsChecked)==1 && !model.Choices.Single(c=>c.Issue.CharacterFormat is not null).IsChecked,
            "Selecting capitalization alone does not silently approve an independent font repair");
        var line=new Issue(20,"Trích yếu".Length,"Trích yếu","Trích yếu","",false,FixKind.ShapeLine,LineFix:new(null,0,50,40,LineRole.Subject),Prerequisites:[type,font]);
        model=new IssueReviewModel([type,font,line]);model.Choices.Single(c=>c.Issue.LineFix is not null).IsChecked=true;
        assert(model.Choices.All(c=>c.IsChecked),"Selecting a dependent Line visibly includes both capitalization and font approval items");
    }
}
