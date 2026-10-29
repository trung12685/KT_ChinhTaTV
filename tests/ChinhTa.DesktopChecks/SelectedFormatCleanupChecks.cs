using System.IO;
using System.Runtime.InteropServices;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class SelectedFormatCleanupChecks
{
    private static Exception[] Causes(Exception? error)=>error switch
    {AggregateException all=>all.Flatten().InnerExceptions.ToArray(),null=>[],_=>[error]};
    public static void Run(Action<bool,string> assert)
    {
        const string text="BÁO CÁO";const int cp=610;
        var issue=new Issue(20,text.Length,text,text,"",false,FixKind.NationalHeading,Format:new(13,Alignment:1));
        var proof=new SelectedFormatProof(issue,text,"source prefix",null);
        var rows=new[]{new WordParagraphSnapshot(cp,cp+text.Length+1,text+"\r",1,false,true)};
        var primary=new IOException("synthetic font observation failure");
        var paragraphFailure=new InvalidComObjectException("synthetic paragraph release");
        var rangeFailure=new IOException("synthetic range release");
        foreach(bool failRead in new[]{false,true})
        foreach(int mask in new[]{0,1,2,3})
        {
            var range=new CharacterStyleReaderChecks.Range(cp,cp+text.Length,text,new(),error:failRead?primary:null);
            var released=new List<object>();Exception? observed=null;
            try
            {
                SelectedFormatVerification.Verify([proof],rows,_=>"source prefix",(start,end)=>
                {if(start!=cp || end!=cp+text.Length)throw new Exception("Stale range coordinates");return range;},owned=>
                {
                    if(owned is null)return;released.Add(owned);
                    if(ReferenceEquals(owned,range.ParagraphFormat) && (mask&1)!=0)throw paragraphFailure;
                    if(ReferenceEquals(owned,range) && (mask&2)!=0)throw rangeFailure;
                });
            }
            catch(Exception failure){observed=failure;}
            assert(released.Count==2 && ReferenceEquals(released[0],range.ParagraphFormat) && ReferenceEquals(released[1],range),
                "Post-save verification attempts both acquired paragraph/range releases after observation or cleanup faults: "+failRead+"/"+mask);
            var expected=new List<Exception>();if(failRead)expected.Add(primary);
            if((mask&1)!=0)expected.Add(paragraphFailure);if((mask&2)!=0)expected.Add(rangeFailure);
            assert(Causes(observed).SequenceEqual(expected) && (expected.Count!=1 || ReferenceEquals(observed,expected[0])),
                "Post-save verification preserves its primary error followed by every cleanup cause, without inventing a successful proof: "+failRead+"/"+mask);
        }
        {
            var range=new CharacterStyleReaderChecks.Range(cp,cp+text.Length,text,new());
            bool committed=false,restored=false;int rangeReleases=0;Exception? observed=null;
            try
            {
                PageLayoutBatch.Apply(()=>{},()=>{},()=>SelectedFormatVerification.Verify([proof],rows,_=>"source prefix",(_,_)=>range,owned=>
                {
                    if(ReferenceEquals(owned,range.ParagraphFormat))throw paragraphFailure;
                    if(ReferenceEquals(owned,range))rangeReleases++;
                }),()=>committed=true,()=>restored=true);
            }
            catch(Exception failure){observed=failure;}
            assert(!committed && restored && rangeReleases==1 && ReferenceEquals(observed,paragraphFailure),
                "The actual page transaction rolls back an unclean final proof while still releasing its range and preserving the cleanup cause");
        }
    }
}
