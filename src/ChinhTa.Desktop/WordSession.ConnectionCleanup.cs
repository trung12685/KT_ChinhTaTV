using System.Runtime.InteropServices;
using ChinhTa.Core;

namespace ChinhTa.Desktop;

public sealed partial class WordSession
{
    // A broken document RCW must not prevent Quit/Release of the owned Word
    // application. The caller chooses which COM failures its operation can
    // tolerate; strict opening/check paths do not have to inherit reconnect's
    // policy for a disconnected application.
    internal static Exception? CloseOwnedConnection(object? ownedDocument,object? ownedApp,
        Action<object?> release,Func<string,COMException,bool>? ignoreComFailure=null)
    {
        ArgumentNullException.ThrowIfNull(release);
        void Close(Action action,string stage)
        {
            try{action();}
            catch(COMException error)
            {if(ignoreComFailure?.Invoke(stage,error)!=true)throw;}
        }
        var actions=new List<Action>();
        if(ownedDocument is not null)
        {
            actions.Add(()=>Close(()=>{dynamic item=ownedDocument;item.Close(0);},"Word.Close"));
            actions.Add(()=>release(ownedDocument));
        }
        if(ownedApp is not null)
        {
            actions.Add(()=>Close(()=>{dynamic item=ownedApp;item.Quit(0);},"Word.Quit"));
            actions.Add(()=>release(ownedApp));
        }
        return IndependentCleanup.Complete(null,actions);
    }
}
