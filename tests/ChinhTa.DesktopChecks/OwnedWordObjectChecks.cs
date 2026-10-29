using System.IO;
using System.Runtime.InteropServices;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class OwnedWordObjectChecks
{
    public sealed class Setup(string failureStage,Exception primary)
    {
        public double PageWidth=>failureStage=="geometry"?throw primary:612;
        public double PageHeight=>792;
        public double TopMargin=>72;
        public double BottomMargin=>54;
        public double LeftMargin=>45;
        public double RightMargin=>36;
        public int MirrorMargins=>0;
    }
    public sealed class TextRange(string failureStage,Exception primary)
    {
        public string Text=>failureStage=="text"?throw primary:failureStage=="empty"?"":"Section prose\r";
        public int Start=>failureStage=="start"?throw primary:17;
    }
    public sealed class Section(string failureStage,Exception primary)
    {
        public Setup Setup{get;}=new(failureStage,primary);
        public TextRange TextRange{get;}=new(failureStage,primary);
        public int SetupReads,RangeReads;
        public object PageSetup{get{SetupReads++;return failureStage=="setup-get"?throw primary:Setup;}}
        public object Range{get{RangeReads++;return failureStage=="range-get"?throw primary:TextRange;}}
    }
    private static IEnumerable<Exception> Causes(Exception? failure)
        =>failure is AggregateException aggregate?aggregate.InnerExceptions.SelectMany(Causes):failure is null?[]:[failure];

    public static void Run(Action<bool,string> assert)
    {
        foreach(string stage in new[]{"setup-get","range-get","text","start","geometry","empty","success"})
        for(int releaseMask=0;releaseMask<8;releaseMask++)
        {
            var primary=new InvalidOperationException("managed section failure: "+stage);
            var section=new Section(stage,primary);var attempted=new List<object>();
            object[] expected=stage=="setup-get"?[section]:stage=="range-get"?[section.Setup,section]:[section.TextRange,section.Setup,section];
            object[] all=[section.TextRange,section.Setup,section];
            var releaseErrors=new Exception[]{new IOException("release-range"),new IOException("release-setup"),new IOException("release-section")};
            var expectedErrors=new List<Exception>();
            if(stage is not ("empty" or "success"))expectedErrors.Add(primary);
            expectedErrors.AddRange(expected.Where(item=>(releaseMask&(1<<Array.IndexOf(all,item)))!=0)
                .Select(item=>releaseErrors[Array.IndexOf(all,item)]));
            Exception? failure=null;SectionLayout? layout=null;
            try
            {
                layout=WordSession.ReadOwnedSectionLayout(section,3,item=>
                {
                    if(item is null)throw new Exception("No missing acquisition may be released");
                    attempted.Add(item);int index=Array.IndexOf(all,item);
                    if(index<0)throw new Exception("No foreign object may be released");
                    if((releaseMask&(1<<index))!=0)throw releaseErrors[index];
                });
            }
            catch(Exception error){failure=error;}
            assert(attempted.SequenceEqual(expected) && section.SetupReads==1 && section.RangeReads==(stage=="setup-get"?0:1),
                "Actual section reader releases every successfully acquired child in reverse order after a getter/read/release failure: "+stage+"/"+releaseMask);
            bool hasPrimary=stage is not ("empty" or "success");
            var reported=hasPrimary?Causes(failure).Concat(failure is null?[]:Causes(WordOwnedObjects.CleanupFailure(failure))):
                failure is WordObjectCleanupException {InnerException:{ } cleanup}?Causes(cleanup):Causes(failure);
            assert(reported.SequenceEqual(expectedErrors) && (!hasPrimary || ReferenceEquals(failure,primary)) &&
                (hasPrimary || expectedErrors.Count==0 || failure is WordObjectCleanupException),
                "Actual section reader retains the exact primary and every attached release failure; cleanup-only errors cannot masquerade as Word operation faults: "+stage+"/"+releaseMask);
            if(expectedErrors.Count==0)
                assert(stage=="empty"?layout is null:layout is {Number:3,Start:17,Anchor:"S",MirrorMargins:false} &&
                    Math.Abs(layout.WidthMm-215.9)<.00001 && Math.Abs(layout.HeightMm-279.4)<.00001 &&
                    Math.Abs(layout.TopMm-25.4)<.00001 && Math.Abs(layout.BottomMm-19.05)<.00001 &&
                    Math.Abs(layout.LeftMm-15.875)<.00001 && Math.Abs(layout.RightMm-12.7)<.00001,
                    "Section ownership cleanup retains the original point-to-mm layout values and empty-section policy: "+stage);
        }
        {
            var shared=new object();var released=new List<object?>();int value=WordOwnedObjects.Read(own=>
            {own(shared);own(shared);return 42;},released.Add);
            assert(value==42 && released.Count==2 && released.All(item=>ReferenceEquals(item,shared)),
                "Two successful acquisitions sharing one COM identity are balanced twice instead of deduplicated or final-released");
        }
        {
            var collection=new object();var primary=new InvalidOperationException("managed range getter failure");
            var section=new Section("range-get",primary);var outerRelease=new IOException("managed collection release failure");
            var releases=new List<object?>();Exception? failure=null;
            try
            {
                WordOwnedObjects.Read(own=>
                {own(collection);return WordSession.ReadOwnedSectionLayout(section,1,releases.Add);},item=>
                {releases.Add(item);throw outerRelease;});
            }
            catch(Exception error){failure=error;}
            assert(releases.SequenceEqual(new object?[]{section.Setup,section,collection}) && ReferenceEquals(failure,primary) &&
                Causes(WordOwnedObjects.CleanupFailure(primary)).SequenceEqual(new Exception[]{outerRelease}),
                "A parent collection release cannot skip section cleanup or replace the child getter failure in nested production scopes");
        }
        {
            var primary=new COMException("managed disconnected getter",unchecked((int)0x80010108));
            var cleanup=new InvalidComObjectException("managed release failure");int attempts=0,reconnections=0;bool originalObserved=false;
            int result=WordConnectionPolicy.ReadWithRecovery(()=>
            {
                attempts++;
                try{return WordOwnedObjects.Read<int>(own=>{own(new object());if(attempts==1)throw primary;return 42;},
                    _=>{if(attempts==1)throw cleanup;});}
                catch(Exception error)
                {originalObserved=ReferenceEquals(error,primary) && ReferenceEquals(WordOwnedObjects.CleanupFailure(error),cleanup);throw;}
            },()=>reconnections++);
            assert(result==42 && attempts==2 && reconnections==1 && originalObserved,
                "An operation RPC failure retains its exact HRESULT and attached cleanup diagnosis for the existing one-retry recovery policy");
        }
        {
            var cleanup=new COMException("managed release-only disconnection",unchecked((int)0x80010108));
            int attempts=0,reconnections=0;Exception? failure=null;
            try{WordConnectionPolicy.ReadWithRecovery(()=>
                {attempts++;return WordOwnedObjects.Read(own=>{own(new object());return 42;},_=>throw cleanup);},()=>reconnections++);}
            catch(Exception error){failure=error;}
            assert(attempts==1 && reconnections==0 && failure is WordObjectCleanupException && ReferenceEquals(failure.InnerException,cleanup),
                "A successful read followed only by an RPC release failure cannot return success or replay the read through reconnect");
        }
        {
            var primary=new COMException("managed nested primary",unchecked((int)0x80010108));
            var innerRelease=new IOException("inner-release");var outerRelease=new IOException("outer-release");Exception? failure=null;
            try{WordOwnedObjects.Read<int>(outer=>
                {outer(new object());return WordOwnedObjects.Read<int>(inner=>{inner(new object());throw primary;},_=>throw innerRelease);},_=>throw outerRelease);}
            catch(Exception error){failure=error;}
            assert(failure is not null && ReferenceEquals(failure,primary) && WordConnectionPolicy.IsDisconnected(failure) &&
                Causes(WordOwnedObjects.CleanupFailure(primary)).SequenceEqual(new Exception[]{innerRelease,outerRelease}),
                "Nested ownership scopes append independent cleanup failures without replacing the original disconnected Word cause");
        }
        {
            int actions=0;bool missingRead=false,missingRelease=false;
            try{WordOwnedObjects.Read<int>(null!,_=>actions++);}catch(ArgumentNullException){missingRead=true;}
            try{WordOwnedObjects.Read(_=>++actions,null!);}catch(ArgumentNullException){missingRelease=true;}
            assert(missingRead && missingRelease && actions==0,
                "Missing scope dependencies are rejected before any acquisition or operation is attempted");
        }
    }
}
