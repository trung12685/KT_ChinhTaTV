using ChinhTa.WordAddin;
using ChinhTa.WordProtocol;
public static class FakeWordChecks
{
    public static void Run(Action<bool,string> assert)
    {
        var doc=new FakeDocument("Sai đầu. Sai cuối.\r");var window=new FakeWindow(doc);
        using(var snapshot=WordSnapshot.Capture(window))
        {
            var f=new FindingDto{ParagraphIndex=1,Start=9,Length=3,Original="Sai"};snapshot.Select(window,f);
            assert(doc.SelectedStart==9 && doc.SelectedEnd==12 && window.ActivePane.Activated && doc.Text=="Sai đầu. Sai cuối.\r","The host selects the exact second occurrence without writing document text");
            doc.Text="Đã sửa. Sai đầu. Sai cuối.\r";bool rejected=false;try{snapshot.Select(window,f);}catch(InvalidOperationException){rejected=true;}
            assert(rejected && doc.SelectedStart==9,"A changed document invalidates a stale range instead of sliding to another occurrence");
        }
        using(var snapshot=WordSnapshot.Capture(window))
        {
            window.Document=new FakeDocument(doc.Text);bool rejected=false;try{snapshot.RequireCurrent(window);}catch(InvalidOperationException){rejected=true;}
            assert(rejected,"Even identical text in another document cannot reuse a pane snapshot");
        }
    }
}
public sealed class FakeWindow(FakeDocument doc){public FakeDocument Document {get;set;}=doc;public FakePane ActivePane {get;}=new();public void Activate(){} }
public sealed class FakePane{public bool Activated {get;private set;}public void Activate(){Activated=true;}}
public sealed class FakeDocument(string text)
{
    public string Text {get;set;}=text;
    public int SelectedStart {get;set;}=-1;
    public int SelectedEnd {get;set;}=-1;
    public FakeRange Content=>new(this,0,Text.Length);
    public FakeParagraphs Paragraphs=>new(this);
    public FakeRange Range(int start,int end)=>new(this,start,end);
}
public sealed class FakeParagraphs(FakeDocument doc)
{
    public int Count=>1;
    public FakeParagraph this[int index]=>index==1?new(doc):throw new ArgumentOutOfRangeException(nameof(index));
}
public sealed class FakeParagraph(FakeDocument doc){public FakeRange Range=>doc.Content;}
public sealed class FakeRange(FakeDocument doc,int start,int end)
{
    public int Start=>start;public int End=>end;public string Text=>doc.Text.Substring(start,end-start);
    public FakeInformation Information=>new();
    public void Select(){doc.SelectedStart=start;doc.SelectedEnd=end;}
}
public sealed class FakeInformation{public bool this[int index]=>false;}
