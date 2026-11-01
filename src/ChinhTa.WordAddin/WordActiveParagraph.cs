using System.Drawing;
using System.Runtime.InteropServices;
using ChinhTa.WordProtocol;
namespace ChinhTa.WordAddin;
internal sealed class WordActiveParagraph:IDisposable
{
    private readonly object document;
    internal ParagraphDto Paragraph {get;}
    internal int CaretOffset {get;}
    internal string Key {get;}
    internal string DocumentId {get;}=Guid.NewGuid().ToString();
    private WordActiveParagraph(object doc,ParagraphDto p,int caret)
    {
        document=doc;Paragraph=p;CaretOffset=caret;
        IntPtr identity=Marshal.GetIUnknownForObject(doc);
        try{Key=identity.ToInt64()+":"+p.Start+":"+p.End+":"+p.InTable+":"+p.Text;}
        finally{Marshal.Release(identity);}
    }
    internal static WordActiveParagraph Capture(object window)
    {
        dynamic w=window;object doc=w.Document;object? selection=null,selected=null,rows=null,row=null,range=null,prefix=null,fields=null;
        try
        {
            selection=w.Selection;dynamic s=selection;selected=s.Range;dynamic sr=selected;
            if((int)sr.StoryType!=1)throw new InvalidOperationException("Gạch khi gõ hiện kiểm nội dung chính; chưa kiểm đầu/cuối trang hoặc hộp chữ.");
            int caret=(int)sr.Start;rows=sr.Paragraphs;dynamic ps=rows;row=ps[1];dynamic p=row;range=p.Range;dynamic r=range;
            var dto=new ParagraphDto{Index=1,Start=(int)r.Start,End=(int)r.End,Text=(string)r.Text,InTable=(bool)r.Information[12]};
            if(dto.Text.Length>16000 || dto.Text.Any(c=>c=='\ufffc' || char.IsControl(c) && c is not ('\t' or '\r' or '\n' or '\a' or '\v' or '\f' or '\u000e')) || caret<dto.Start || caret>dto.End)
                throw new InvalidOperationException("Đoạn dài/có trường hoặc đối tượng chưa hỗ trợ gạch khi gõ; dùng bảng kiểm tra.");
            fields=r.Fields;dynamic fs=fields;if((int)fs.Count!=0)throw new InvalidOperationException("Đoạn chứa trường Word: giữ nguyên, chưa gạch khi gõ.");
            dynamic d=doc;prefix=d.Range(dto.Start,caret);dynamic pr=prefix;string before=(string)pr.Text;
            if(!dto.Text.StartsWith(before,StringComparison.Ordinal))throw new InvalidOperationException("Chưa đối chiếu được vị trí con trỏ.");
            return new(doc,dto,before.Length);
        }
        catch{ComLifetime.Release(doc);throw;}
        finally{foreach(object? value in new[]{fields,prefix,range,row,rows,selected,selection})ComLifetime.Release(value);}
    }
    internal IReadOnlyList<UnderlineMark> Measure(object window,IEnumerable<FindingDto> findings)
    {
        var result=new List<UnderlineMark>();dynamic d=document,w=window;int budget=240;
        string Read(int start,int end)
        {object value=d.Range(start,end);try{dynamic r=value;return (string)r.Text;}finally{ComLifetime.Release(value);}}
        foreach(var f in findings.Take(24))
        {
            if(f.Length>96 || f.Length>budget)continue;budget-=f.Length;
            NativeSelection proven;
            try{proven=NativeRangeProof.Resolve(Paragraph,f,Read);}catch(InvalidOperationException){continue;}
            for(int cp=proven.Start;cp<proven.End;cp++)
            {
                object? range=null;
                try
                {
                    range=d.Range(cp,cp+1);int left=0,top=0,width=0,height=0;
                    w.GetPoint(out left,out top,out width,out height,range);
                    if(width<=0 || width>400 || height<=0 || height>300)continue;
                    result.Add(new(new Rectangle(left,top,width,height),UnderlinePalette.ColorFor(f.Category)));
                }
                catch(COMException){} // Word reports invisible characters/ranges; do not guess.
                finally{ComLifetime.Release(range);}
            }
        }
        return result;
    }
    public void Dispose()=>ComLifetime.Release(document);
}
