using System.Runtime.InteropServices;
using ChinhTa.Core;

public static class WordOpenSessionChecks
{
    public static void Run(Action<bool,string> assert)
    {
        int document=0,application=0;
        WordOpenSessionPolicy.Prepare(false,()=>document++,()=>application++);
        assert(document==0 && application==1,"Legacy opening closes the private Word application as before");
        document=application=0;WordOpenSessionPolicy.Prepare(true,()=>document++,()=>application++);
        assert(document==1 && application==0,"Warm opening closes only the old document and keeps the owned Word instance");
        foreach(int code in new[]{unchecked((int)0x800706BA),unchecked((int)0x800706BE),unchecked((int)0x80010108)})
        {
            document=application=0;
            WordOpenSessionPolicy.Prepare(true,()=>{document++;throw new COMException("RPC",code);},()=>application++);
            assert(document==1 && application==1,"A disconnected owned Word document releases the old application before reopening");
        }
        foreach(Exception error in new Exception[]{new COMException("Failure",unchecked((int)0x80004005)),new IOException("Close failed")})
        {
            application=0;bool thrown=false;
            try{WordOpenSessionPolicy.Prepare(true,()=>throw error,()=>application++);}catch(Exception actual){thrown=ReferenceEquals(actual,error);}
            assert(thrown && application==0,"A non-disconnection close failure is not hidden by opening another document");
        }
    }
}
