using ChinhTa.Core;

public static class PreviewAnchorBatchChecks
{
    public static void Run(Action<bool,string> assert)
    {
        const string story="😀 Nội dung.";var word=new Issue(10,8,"Nội dung","Nội dung","",false);
        var partial=word with {Length=3,Original="Nội",Replacement="Nội"};var invalid=word with {Original="Khác nhé",Replacement="Khác nhé"};
        int reads=0,unmapped=0;
        var result=PreviewAnchorBatch.Read([word,partial,invalid],story,cp=>{reads++;return (2,"😀 ");},(_,_)=>unmapped++);
        assert(reads==1 && result.Count==2 && unmapped==1 && result[word].Offset==3 && result[partial].Length==3 && result[word].Page==2,
            "Issues at the same Word CP share one page/prefix read but individually validate their own UTF-16 text and emoji offset");
        var changed=word with {Original="Báo cáo.",Replacement="Báo cáo."};
        var next=PreviewAnchorBatch.Read([changed],"😀 Báo cáo.",cp=>{reads++;return (1,"😀 ");});
        assert(reads==2 && next[changed].Page==1,"A new export reads again after document/text/page changes; no cross-edit cache survives");
        reads=0;
        var many=Enumerable.Range(0,1200).Select(i=>new Issue(i/2,1,"x","x","",false,Title:i.ToString())).ToArray();
        var all=PreviewAnchorBatch.Read(many,new string('x',600),cp=>{reads++;return (1,new string('x',cp));});
        assert(reads==600 && all.Count==1200,"1200 independent anchors at 600 CPs need 600 position reads rather than 1200");
        bool failed=false;
        try{PreviewAnchorBatch.Read([word],story,_=>throw new IOException("read"));}catch(IOException){failed=true;}
        assert(failed,"Position read failures propagate to recovery rather than being mistaken for an unmapped text warning");
        failed=false;
        try{PreviewAnchorBatch.Read([word],story,_=>(1,"😀 "),onMapped:(_,_)=>throw new InvalidOperationException("observer"));}
        catch(InvalidOperationException){failed=true;}
        assert(failed,"A callback failure cannot silently turn a mapped anchor into apparent successful partial coverage");
    }
}
