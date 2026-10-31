using ChinhTa.WordProtocol;

internal static class NativeRangeChecks
{
    internal static void Run(Action<bool,string> assert)
    {
        NativeSelection Map(string text,int[] nativeToString,int offset,int length,int origin=0)
        {
            var row=new ParagraphDto{Index=2,Start=origin,End=origin+nativeToString.Length-1,Text=text,InTable=true};
            var finding=new FindingDto{ParagraphIndex=2,Offset=offset,Length=length,Original=text.Substring(offset,length),InTable=true};
            return NativeRangeProof.Resolve(row,finding,(a,b)=>text.Substring(nativeToString[a-origin],nativeToString[b-origin]-nativeToString[a-origin]));
        }
        bool Reject(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
        string cell="sai sai.\r\a";var native=Enumerable.Range(0,cell.Length-1).Append(cell.Length).ToArray();
        var result=Map(cell,native,4,3,100);
        assert(result.Start==104 && result.End==107,"Collapsed cell terminators do not prevent selecting the exact second word in a table");
        string emoji="😀 sai sai.\r";native=new[]{0}.Concat(Enumerable.Range(2,emoji.Length-1)).ToArray();int offset=emoji.LastIndexOf("sai",StringComparison.Ordinal);
        result=Map(emoji,native,offset,3,100);
        assert(result.Start==100+offset-1 && result.End==100+offset+2,"A supplementary character counted as one native CP cannot move selection to the wrong repeated word");
        string nfd="tiếng sai.\r";native=Enumerable.Range(0,nfd.Length+1).ToArray();offset=nfd.IndexOf("sai",StringComparison.Ordinal);result=Map(nfd,native,offset,3);
        assert(result.Start==offset && result.End==offset+3,"NFD combining characters retain their original offsets instead of being normalized before Word selection");
        result=Map("abc.\r\a",new[]{0,1,2,3,4,6},0,3,int.MaxValue-10);
        assert(result.Start==int.MaxValue-10 && result.End==int.MaxValue-7,"Native coordinate arithmetic remains valid near the Int32 boundary");
        assert(Reject(()=>Map("ABCD",new[]{0,1,3,4},1,1)),"A string boundary unavailable in Word is rejected rather than rounded to a nearby native position");
        assert(Reject(()=>Map("ABCD",new[]{0,1,1,3,4},1,2)),"Compensating zero-text and collapsed native positions cannot pass merely because total target lengths match");
        var row2=new ParagraphDto{Index=1,Start=0,End=8,Text="sai sai."};var f=new FindingDto{ParagraphIndex=1,Offset=4,Length=3,Original="sai"};
        assert(Reject(()=>NativeRangeProof.Resolve(row2,f,(a,b)=>a==0 && b==4?"xyz ":row2.Text.Substring(a,b-a))),"Matching target letters cannot bypass a different source prefix");
        assert(Reject(()=>NativeRangeProof.Resolve(row2,f,(a,b)=>a==4 && b==7?"xxx":row2.Text.Substring(a,b-a))),"Matching boundary prefixes cannot bypass a different live target range");
        f.Offset=int.MaxValue;assert(Reject(()=>NativeRangeProof.Resolve(row2,f,(_,_)=>"")),"An out-of-range finding is rejected before any native offset calculation");
        f.Offset=0;f.Length=0;f.Original="";assert(Reject(()=>NativeRangeProof.Resolve(row2,f,(_,_)=>"")),"An empty finding cannot turn into a cursor jump masquerading as a selected error");
        f.Offset=3;f.Length=1;f.Original=" ";row2.Text="sai\asai.";f.Original="\a";
        assert(Reject(()=>NativeRangeProof.Resolve(row2,f,(_,_)=>"")),"A cell/control marker can never be selected as a spelling correction");
    }
}
