using System.Runtime.InteropServices;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class SelectiveSizeWriterChecks
{
    public sealed class Font(float size)
    {
        private float size=size;
        public int Writes {get;private set;}
        public bool Fail {get;set;}
        public float Size {get=>size;set{Writes++;if(Fail)throw new COMException("synthetic write",unchecked((int)0x80004005));size=value;}}
    }
    public sealed class Range(int start,int end,string text,Font font,Range[]? characters=null,Exception? readError=null)
    {
        public int Start=>start;
        public int End=>end;
        public string Text=>text;
        public Font Font {get{if(readError is not null)throw readError;return font;}}
        public Children Characters=>new(characters??[]);
    }
    public sealed class Children(Range[] items)
    {public int Count=>items.Length;public Range this[int i]=>items[i-1];}
    public static void Run(Action<bool,string> assert)
    {
        var style=new CharacterFormat(13,false,true,PreserveSize:true,RepairInvalidSizes:true);
        // One emoji has one Word CP here but two UTF-16 code units. The writer
        // follows actual child bounds instead of making a range from string offsets.
        Range[] children=[new(100,101,"😀",new(12)),new(101,102," ",new(8)),new(102,103,"A",new(13)),
            new(103,104,"\v",new(8)),new(104,105,"B",new(14)),new(105,107,"e\u0301",new(16))];
        const string text="😀 A\vBe\u0301";
        var root=new Range(100,107,text,new(9999999),children);
        VisibleFormatReader.RepairSizes(root,text,style);
        assert(children.Select(c=>c.Font.Size).SequenceEqual(new[]{13f,8,13,8,14,13}) &&
            children.Select(c=>c.Font.Writes).SequenceEqual(new[]{1,0,0,0,0,1}),
            "Selective writes use actual CPs, change only invalid visible sizes and leave 13/14pt, spaces, soft breaks and emoji/combining text intact");
        VisibleFormatReader.RepairSizes(root,text,style);
        assert(children.Sum(c=>c.Font.Writes)==2,"An already normalized range receives no extra size writes");
        foreach(int bad in Enumerable.Range(0,7))
        {
            var first=new Range(10,11,"A",new(12));
            Range second=bad switch
            {
                0=>new(11,13,"B",new(14)), // Outside parent.
                1=>new(10,12,"B",new(14)), // Overlap.
                2=>new(12,12,"B",new(14)), // Gap/empty.
                3=>new(11,12,"X",new(14)), // Text mismatch.
                4=>new(11,12,"B",new(9999999)),
                5=>new(11,12,"B",new(float.NaN)),
                _=>new(11,12,"B",new(14),readError:new COMException("synthetic read",unchecked((int)0x80010108)))
            };
            var invalid=new Range(10,12,"AB",new(9999999),[first,second]);bool rejected=false;
            try{VisibleFormatReader.RepairSizes(invalid,"AB",style);}catch(Exception e) when(e is InvalidOperationException or COMException){rejected=true;}
            assert(rejected && first.Font.Writes==0 && first.Font.Size==12,
                "All character bounds/text/sizes must be proved before the first size write, including when the last child is invalid");
        }
        var incompleteFirst=new Range(10,11,"A",new(12));bool incomplete=false;
        try{VisibleFormatReader.RepairSizes(new Range(10,12,"AB",new(9999999),[incompleteFirst]),"AB",style);}
        catch(InvalidOperationException){incomplete=true;}
        assert(incomplete && incompleteFirst.Font.Writes==0,"An incomplete character sequence cannot modify its proven prefix");
        bool stale=false;
        try{VisibleFormatReader.RepairSizes(root,"Khác",style);}catch(InvalidOperationException){stale=true;}
        assert(stale,"Changed range text rejects a stale selective size write");
        foreach(var forged in new[]{style with {RepairInvalidSizes=false},style with {PreserveSize=false},style with {Size=14}})
        {
            bool rejected=false;try{VisibleFormatReader.RepairSizes(root,text,forged);}catch(InvalidOperationException){rejected=true;}
            assert(rejected,"The Word writer independently rejects an invalid or non-selective style");
        }
        var a=new Range(0,1,"A",new(12));var b=new Range(1,2,"B",new(16){Fail=true});
        var failRange=new Range(0,2,"AB",new(9999999),[a,b]);bool rolledBack=false,propagated=false;
        var issue=new Issue(0,2,"AB","AB","",false,FixKind.DocumentHeadingFormat,CharacterFormat:style);
        try
        {
            AtomicTextBatch.Apply([issue],i=>"AB",i=>{VisibleFormatReader.RepairSizes(failRange,"AB",style);return "AB";},
                ()=>throw new Exception("must not commit"),()=>{a.Font.Size=12;b.Font.Fail=false;b.Font.Size=16;rolledBack=true;});
        }
        catch(COMException){propagated=true;}
        assert(propagated && rolledBack && a.Font.Size==12 && b.Font.Size==16,
            "A size setter failure propagates into whole-transaction rollback after an earlier glyph was written");
        var line=new Issue(5,2,"AB","AB","",false,FixKind.ShapeLine,
            LineFix:new(null,20,40,10,LineRole.Subject),Prerequisites:[issue]);
        var model=new IssueReviewModel([issue,line]);
        model.Choices.Single(c=>c.Issue.LineFix is not null).IsChecked=true;
        assert(model.Choices.All(c=>c.IsChecked) && model.Choices.Count(c=>c.Issue.CharacterFormat?.RepairInvalidSizes==true)==1,
            "Selecting a Line visibly selects its selective-size prerequisite as one whole-region approval item");
        model.Choices.Single(c=>c.Issue.CharacterFormat is not null).IsChecked=false;
        assert(!model.Choices.Any(c=>c.IsChecked),"Unchecking the selective-size prerequisite also unchecks the dependent Line");
    }
}
