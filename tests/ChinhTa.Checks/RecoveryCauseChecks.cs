using System.Runtime.InteropServices;
using ChinhTa.Core;

public static class RecoveryCauseChecks
{
    public static void Run(Action<bool,string> assert)
    {
        var disconnected=new COMException("managed disconnected read",unchecked((int)0x800706BA));
        int reads=0,recoveries=0;Exception? observed=null;
        int value=WordConnectionPolicy.ReadWithRecovery(()=>
        {
            if(++reads==1)throw disconnected;
            return 7;
        },cause=>{observed=cause;recoveries++;});
        assert(value==7 && reads==2 && recoveries==1 && ReferenceEquals(observed,disconnected),
            "The read-recovery callback receives the exact disconnected failure and still retries one successful read only");
        var refused=new WordRecoveryUnavailableException("managed-working.docx",disconnected);
        reads=recoveries=0;Exception? failure=null;
        try{WordConnectionPolicy.ReadWithRecovery<int>(()=>{reads++;throw disconnected;},cause=>
        {
            recoveries++;assert(ReferenceEquals(cause,disconnected),"A refused recovery receives the actual original RPC exception");throw refused;
        });}
        catch(Exception error){failure=error;}
        assert(ReferenceEquals(failure,refused) && ReferenceEquals(failure?.InnerException,disconnected) && reads==1 && recoveries==1,
            "A refused recovery preserves its typed error and original cause without replaying the read");
        assert(!WordConnectionPolicy.IsDisconnected(refused) && refused.Message.Contains("managed-working.docx",StringComparison.Ordinal),
            "The blocked recovery signal cannot itself trigger an RPC retry and retains the owned checkpoint location");
        var ordinary=new IOException("managed non-RPC failure");reads=recoveries=0;failure=null;
        try{WordConnectionPolicy.ReadWithRecovery<int>(()=>{reads++;throw ordinary;},_=>recoveries++);}
        catch(Exception error){failure=error;}
        assert(ReferenceEquals(failure,ordinary) && reads==1 && recoveries==0,
            "A non-RPC failure cannot invoke the new recovery callback or replay a read");
        reads=recoveries=0;failure=null;
        try{WordConnectionPolicy.ReadWithRecovery<int>(()=>{reads++;throw disconnected;},_=>recoveries++);}
        catch(Exception error){failure=error;}
        assert(ReferenceEquals(failure,disconnected) && reads==2 && recoveries==1,
            "A disconnected retry failure keeps the actual RPC exception after exactly one recovery");
    }
}
