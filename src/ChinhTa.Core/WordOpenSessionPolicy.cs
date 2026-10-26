namespace ChinhTa.Core;

public static class WordOpenSessionPolicy
{
    // Only the application's private Word instance is supplied by the caller.
    // Close failures other than a disconnected server must not be hidden.
    public static void Prepare(bool reusePrivateApplication,Action closeDocument,Action closeApplication)
    {
        if(!reusePrivateApplication){closeApplication();return;}
        try{closeDocument();}
        catch(Exception error) when(WordConnectionPolicy.IsDisconnected(error))
        {closeApplication();}
    }
}
