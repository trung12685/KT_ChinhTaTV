using System.IO;
using System.Reflection;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class PunctuationSessionPreservationChecks
{
    public sealed class Font(bool marked)
    {
        public string Name=>marked?"Times New Roman":"Arial";
        public float Size=>marked?14:12;
        public int Bold=>marked?-1:0;
        public int Italic=>0;
        public int Color=>marked?0:255;
        public int Underline=>marked?1:0;
    }
    public sealed class Layout
    {
        public int Reads;
        public int Alignment=>0;
        public float LeftIndent=>0;
        public float RightIndent=>0;
        public float FirstLineIndent=>0;
        public float SpaceBefore{get{Reads++;return 2;}}
        public float SpaceAfter=>3;
        public int LineSpacingRule=>4;
        public float LineSpacing=>18;
        public int SpaceBeforeAuto=>0;
        public int SpaceAfterAuto=>0;
    }
    public sealed class Range(int start,string text,Layout layout)
    {
        public int Start=>start;
        public int End=>start+text.Length;
        public string Text=>text;
        public Font Font=>new(text==",");
        public Layout ParagraphFormat=>layout;
        public Children Characters=>new(text.Select((c,i)=>new Range(start+i,c.ToString(),layout)).ToArray());
    }
    public sealed class Children(Range[] items)
    {public int Count=>items.Length;public Range this[int i]=>items[i-1];}
    public sealed class Document(int start,string text,Layout layout)
    {
        public int Saves,Closes;
        public Range Range(int from,int to)=>new(from,text.Substring(from-start,to-from),layout);
        public void Save(){Saves++;throw new Exception("Style observation must not save or mutate Word");}
        public void Close(int saveChanges){if(saveChanges!=0)throw new Exception("Unexpected save");Closes++;}
    }
    public static void Run(Action<bool,string> assert)
    {
        string folder=Path.Combine(Path.GetTempPath(),"ctv-punctuation-session-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        using var session=new WordSession{LineCheckArtifactFolder=folder};
        try
        {
            const int cp=700;const string text="Nội dung  ,báo cáo đã xong.";
            var layout=new Layout();var document=new Document(cp,text,layout);
            void Field(string name,object value)=>typeof(WordSession).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(session,value);
            Field("document",document);Field("lastFormatLayout",new[]{new WordParagraphSnapshot(cp,cp+text.Length+1,text+"\r",1,false,true)});
            var edit=PunctuationSpaceRules.Check(new(cp,text)).Single();
            var probe=session.CaseTrackingRegions([edit]).Single();
            var expected=session.ReadCheckExpectedStyles(probe,[edit]);
            var mark=expected.Styles[expected.Text.IndexOf(',')];
            assert(probe.Kind==FixKind.PunctuationSpace && expected.Text=="Nội dung, báo cáo đã xong." &&
                mark is {Underline:1,Font:{Size:14,Bold:-1}} && layout.Reads>0,
                "The production session captures a pure C14 fingerprint through real reader calls and opts into paragraph spacing observations");
            assert(expected.Paragraph is {Spacing:{Before:2,After:3,Rule:4,Line:18}} &&
                expected.Styles.Where((_,i)=>char.IsLetter(expected.Text[i])).All(s=>s is {Font:{Name:"Arial",Size:12,Color:255}}) &&
                document.Saves==0 && !session.Changed,
                "A punctuation-only read preserves observed Exactly spacing and unselected Arial/red/12pt text without granting a font/spacing edit or saving Word");
            var changed=expected.Styles.ToArray();changed[expected.Text.IndexOf(',')]=mark! with {Underline=0};
            assert(!expected.Matches(new(expected.Text,changed,expected.Paragraph)),
                "The session-created source fingerprint rejects lost comma underline even when repaired text is exact");
            session.DisposeCheckSession();
            assert(document.Closes==1 && !session.IsOpen,"The managed session fixture releases only its owned document after the read-only observation checks");
        }
        finally{Directory.Delete(folder,true);}
    }
}
