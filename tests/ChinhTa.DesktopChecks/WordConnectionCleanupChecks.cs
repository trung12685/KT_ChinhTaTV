using System.IO;
using System.Runtime.InteropServices;
using ChinhTa.Desktop;

public static class WordConnectionCleanupChecks
{
    public sealed class Document(Action close)
    {public void Close(int saveChanges){if(saveChanges!=0)throw new Exception("Unexpected save");close();}}
    public sealed class Application(Action quit)
    {public void Quit(int saveChanges){if(saveChanges!=0)throw new Exception("Unexpected save");quit();}}
    private static Exception[] Causes(Exception? error)=>error switch
    {AggregateException aggregate=>aggregate.Flatten().InnerExceptions.ToArray(),null=>[],_=>[error]};

    public static void Run(Action<bool,string> assert)
    {
        var order=new List<string>();
        var document=new Document(()=>order.Add("close"));var application=new Application(()=>order.Add("quit"));
        var failure=WordSession.CloseOwnedConnection(document,application,item=>order.Add(ReferenceEquals(item,document)?"release-document":"release-app"));
        assert(failure is null && order.SequenceEqual(new[]{"close","release-document","quit","release-app"}),
            "Owned Word connection closes and releases each resource in order without saving");

        order.Clear();var brokenRelease=new InvalidComObjectException("synthetic document release failure");
        failure=WordSession.CloseOwnedConnection(document,application,item=>
        {order.Add(ReferenceEquals(item,document)?"release-document":"release-app");if(ReferenceEquals(item,document))throw brokenRelease;});
        assert(ReferenceEquals(failure,brokenRelease) && order.SequenceEqual(new[]{"close","release-document","quit","release-app"}),
            "A failed document RCW release cannot skip quitting or releasing the owned Word application");

        var closeFailure=new COMException("synthetic close");var quitFailure=new COMException("synthetic quit");
        var appReleaseFailure=new IOException("synthetic application release");
        document=new Document(()=>throw closeFailure);application=new Application(()=>throw quitFailure);int releases=0;
        failure=WordSession.CloseOwnedConnection(document,application,_=>{releases++;throw releases==1?brokenRelease:appReleaseFailure;});
        var causes=Causes(failure);
        assert(releases==2 && causes.Length==4 && ReferenceEquals(causes[0],closeFailure) && ReferenceEquals(causes[1],brokenRelease) &&
            ReferenceEquals(causes[2],quitFailure) && ReferenceEquals(causes[3],appReleaseFailure),
            "Strict connection cleanup retains all four independent failures in resource order");

        var observed=new List<string>();releases=0;
        failure=WordSession.CloseOwnedConnection(document,application,_=>releases++,(stage,_)=>{observed.Add(stage);return true;});
        assert(failure is null && releases==2 && observed.SequenceEqual(new[]{"Word.Close","Word.Quit"}),
            "Normal reconnect can explicitly observe and tolerate close/quit COM failures while still releasing both handles");
        releases=0;
        failure=WordSession.CloseOwnedConnection(document,application,_=>releases++,(stage,_)=>stage=="Word.Quit");
        assert(ReferenceEquals(failure,closeFailure) && releases==2,
            "A strict transition can reject one COM failure without preventing the remaining cleanup");

        var observerFailure=new InvalidOperationException("synthetic observer failure");releases=0;int callbacks=0;
        failure=WordSession.CloseOwnedConnection(document,application,_=>releases++,(_,_)=>{callbacks++;throw observerFailure;});
        assert(callbacks==2 && releases==2 && Causes(failure).Length==2 && Causes(failure).All(e=>ReferenceEquals(e,observerFailure)),
            "A diagnostic callback failure cannot skip the remaining owned resources");
        failure=WordSession.CloseOwnedConnection(null,null,_=>throw new Exception("No resource was acquired"));
        assert(failure is null,"An empty owned connection does not release a foreign or unacquired resource");
    }
}
