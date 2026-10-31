using ChinhTa.WordProtocol;
namespace ChinhTa.WordAddin;
internal sealed class WordSnapshot:IDisposable
{
    private readonly object document;
    internal readonly string DocumentId=Guid.NewGuid().ToString();
    internal string Hash {get;}
    internal IReadOnlyList<ParagraphDto> Paragraphs {get;}
    private WordSnapshot(object doc,string hash,List<ParagraphDto> paragraphs){document=doc;Hash=hash;Paragraphs=paragraphs;}
    internal static string Text(object doc)
    {dynamic d=doc;object content=d.Content;try{dynamic range=content;return (string)range.Text;}finally{ComLifetime.Release(content);}}
    internal static WordSnapshot Capture(object window)
    {
        dynamic w=window;object doc=w.Document;object? rows=null;
        try
        {
            string before=Text(doc);if(before.Length>500000)throw new InvalidOperationException("Bản thử giới hạn 500.000 ký tự nội dung chính.");
            dynamic d=doc;rows=d.Paragraphs;dynamic paragraphs=rows;int count=(int)paragraphs.Count;
            if(count>2000)throw new InvalidOperationException("Bản thử giới hạn 2.000 đoạn nội dung chính.");
            var result=new List<ParagraphDto>();
            for(int i=1;i<=count;i++)
            {
                object row=paragraphs[i];object? range=null;
                try
                {
                    dynamic p=row;range=p.Range;dynamic r=range;
                    bool table=(bool)r.Information[12];
                    result.Add(new(){Index=i,Start=(int)r.Start,End=(int)r.End,Text=(string)r.Text,InTable=table});
                }
                finally{ComLifetime.Release(range);ComLifetime.Release(row);}
            }
            if(Text(doc)!=before)throw new InvalidOperationException("Tài liệu đổi trong lúc đọc. Hãy Quét lại.");
            return new(doc,BridgeContract.Hash(before),result);
        }
        catch{ComLifetime.Release(doc);throw;}
        finally{ComLifetime.Release(rows);}
    }
    internal void RequireCurrent(object window)
    {
        dynamic w=window;object current=w.Document;
        try{if(!ComLifetime.Same(document,current) || Hash!=BridgeContract.Hash(Text(current)))throw new InvalidOperationException("Tài liệu đã đổi sau lượt quét. Hãy Quét lại để chọn đúng chữ.");}
        finally{ComLifetime.Release(current);}
    }
    internal void Select(object window,FindingDto finding)
    {
        RequireCurrent(window);var before=Paragraphs.Single(p=>p.Index==finding.ParagraphIndex);
        dynamic d=document;object rows=d.Paragraphs;object? row=null,range=null,target=null;
        try
        {
            dynamic ps=rows;row=ps[finding.ParagraphIndex];dynamic p=row;range=p.Range;dynamic r=range;
            var now=new ParagraphDto{Index=finding.ParagraphIndex,Start=(int)r.Start,End=(int)r.End,Text=(string)r.Text};
            if(!BridgeContract.CanSelect(before,now,finding,Hash,BridgeContract.Hash(Text(document))))throw new InvalidOperationException("Vị trí/chữ không còn khớp. Hãy Quét lại.");
            target=d.Range(finding.Start,finding.Start+finding.Length);dynamic selection=target;
            if((string)selection.Text!=finding.Original)throw new InvalidOperationException("Chưa ghép chắc vùng chữ trong Word; không chọn đoán.");
            selection.Select();dynamic owner=window;owner.Activate();
            object activePane=owner.ActivePane;try{dynamic editPane=activePane;editPane.Activate();}finally{ComLifetime.Release(activePane);}
        }
        finally{ComLifetime.Release(target);ComLifetime.Release(range);ComLifetime.Release(row);ComLifetime.Release(rows);}
    }
    public void Dispose()=>ComLifetime.Release(document);
}
