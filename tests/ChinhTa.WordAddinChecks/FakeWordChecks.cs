using ChinhTa.WordAddin;
using ChinhTa.WordProtocol;
public static class FakeWordChecks
{
    public static void Run(Action<bool,string> assert)
    {
        var doc=new FakeDocument("Sai đầu. Sai cuối.\r");var window=new FakeWindow(doc);
        using(var snapshot=WordSnapshot.Capture(window))
        {
            var f=new FindingDto{ParagraphIndex=1,Offset=9,Length=3,Original="Sai"};snapshot.Select(window,f);
            assert(doc.SelectedStart==9 && doc.SelectedEnd==12 && window.ActivePane.Activated && doc.Text=="Sai đầu. Sai cuối.\r","The host selects the exact second occurrence without writing document text");
            doc.Text="Đã sửa. Sai đầu. Sai cuối.\r";bool rejected=false;try{snapshot.Select(window,f);}catch(InvalidOperationException){rejected=true;}
            assert(rejected && doc.SelectedStart==9,"A changed document invalidates a stale range instead of sliding to another occurrence");
        }
        using(var snapshot=WordSnapshot.Capture(window))
        {
            window.Document=new FakeDocument(doc.Text);bool rejected=false;try{snapshot.RequireCurrent(window);}catch(InvalidOperationException){rejected=true;}
            assert(rejected,"Even identical text in another document cannot reuse a pane snapshot");
        }
        string cell="😀 sai sai.\r\a";int[] map=new[]{0}.Concat(Enumerable.Range(2,cell.Length-3)).Append(cell.Length).ToArray();
        doc=new FakeDocument(cell,map,true);window=new FakeWindow(doc);
        using(var snapshot=WordSnapshot.Capture(window))
        {
            int offset=cell.LastIndexOf("sai",StringComparison.Ordinal);
            snapshot.Select(window,new(){ParagraphIndex=1,Offset=offset,Length=3,Original="sai",InTable=true});
            assert(doc.SelectedStart==offset-1 && doc.SelectedEnd==offset+2 && doc.Text==cell,"The actual host maps string offsets through live native prefixes for a table containing an emoji and a collapsed cell marker");
            doc.InTable=false;bool rejected=false;try{snapshot.Select(window,new(){ParagraphIndex=1,Offset=offset,Length=3,Original="sai",InTable=true});}catch(InvalidOperationException){rejected=true;}
            assert(rejected,"Changing paragraph table membership also invalidates the original selection evidence");
        }
        doc=new FakeDocument("ABCD",new[]{0,1,3,4},true);window=new FakeWindow(doc);
        using(var snapshot=WordSnapshot.Capture(window))
        {
            bool rejected=false;try{snapshot.Select(window,new(){ParagraphIndex=1,Offset=1,Length=1,Original="B",InTable=true});}catch(InvalidOperationException){rejected=true;}
            assert(rejected && doc.SelectedStart==-1,"The actual host never calls Select when Word cannot represent an exact string boundary");
        }
        doc=new FakeDocument("sai.\rsai.\r\a",inTable:true){ParagraphBounds=new[]{new[]{0,5},new[]{5,11}}};window=new FakeWindow(doc);
        using(var snapshot=WordSnapshot.Capture(window))
        {
            snapshot.Select(window,new(){ParagraphIndex=2,Offset=0,Length=3,Original="sai",InTable=true});
            assert(doc.SelectedStart==5 && doc.SelectedEnd==8,"The actual host distinguishes identical words in separate table paragraphs without searching the document");
        }
    }
}
public sealed class FakeWindow(FakeDocument doc){public FakeDocument Document {get;set;}=doc;public FakePane ActivePane {get;}=new();public void Activate(){} }
public sealed class FakePane{public bool Activated {get;private set;}public void Activate(){Activated=true;}}
public sealed class FakeDocument(string text,int[]? nativeMap=null,bool inTable=false)
{
    public string Text {get;set;}=text;
    public bool InTable {get;set;}=inTable;
    public int[][]? ParagraphBounds {get;set;}
    public int NativeLength=>nativeMap is null?Text.Length:nativeMap.Length-1;
    public string Read(int start,int end)=>nativeMap is null?Text.Substring(start,end-start):Text.Substring(nativeMap[start],nativeMap[end]-nativeMap[start]);
    public int SelectedStart {get;set;}=-1;
    public int SelectedEnd {get;set;}=-1;
    public FakeRange Content=>new(this,0,NativeLength);
    public FakeParagraphs Paragraphs=>new(this);
    public FakeRange Range(int start,int end)=>new(this,start,end);
}
public sealed class FakeParagraphs(FakeDocument doc)
{
    public int Count=>doc.ParagraphBounds?.Length??1;
    public FakeParagraph this[int index]=>index>0 && index<=Count?new(doc,index):throw new ArgumentOutOfRangeException(nameof(index));
}
public sealed class FakeParagraph(FakeDocument doc,int index){public FakeRange Range=>doc.ParagraphBounds is null?doc.Content:new(doc,doc.ParagraphBounds[index-1][0],doc.ParagraphBounds[index-1][1]);}
public sealed class FakeRange(FakeDocument doc,int start,int end)
{
    public int Start=>start;public int End=>end;public string Text=>doc.Read(start,end);
    public FakeInformation Information=>new(doc.InTable);
    public void Select(){doc.SelectedStart=start;doc.SelectedEnd=end;}
}
public sealed class FakeInformation(bool inTable){public bool this[int index]=>inTable;}
