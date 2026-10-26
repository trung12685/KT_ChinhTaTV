using ChinhTa.Core;
using ChinhTa.Desktop;

public static class SelectedFormatVerificationChecks
{
    public static void Run(Action<bool,string> assert)
    {
        const string text="BÁO CÁO";const int cp=700;
        var glyphs=text.Select((c,i)=>new CharacterStyleReaderChecks.Range(cp+i,cp+i+1,c.ToString(),
            new CharacterStyleReaderChecks.Font {Size=i%2==0?13:14,Underline=1})).ToArray();
        var range=new CharacterStyleReaderChecks.Range(cp,cp+text.Length,text,new(),glyphs);
        var issue=new Issue(20,text.Length,text,text,"",false,FixKind.DocumentHeadingFormat,CharacterFormat:new(13,false,true,PreserveSize:true));
        var proof=new SelectedFormatProof(issue,text,"header prefix",VisibleFormatReader.ReadSizes(range,text),VisibleFormatReader.ReadStyles(range,text));
        var rows=new[]{new WordParagraphSnapshot(cp,cp+text.Length+1,text+"\r",1,false,true)};
        int released=0,opened=0;
        object Read(int start,int end)
        {if(start!=cp || end!=cp+text.Length)throw new Exception("Old CP reused");opened++;return range;}
        void Verify(IReadOnlyList<WordParagraphSnapshot>? current=null,Func<Issue,string>? prefix=null)=>
            SelectedFormatVerification.Verify([proof],current??rows,prefix??(_=>"header prefix"),Read,o=>{if(o is not null)released++;});
        Verify();
        assert(opened==1 && released==1,"Final format proof rebinds a saved/reopened paragraph to its fresh Word CP and retains mixed legal sizes and underline");
        foreach(var mutation in new Action[]
        {
            ()=>glyphs[0].Font.Color=255,
            ()=>glyphs[0].Font.Underline=0,
            ()=>glyphs[0].Font.Size=14,
            ()=>glyphs[0].Font.Italic=-1,
            ()=>range.ParagraphFormat.Alignment=0
        })
        {
            mutation();bool rejected=false;try{Verify();}catch(InvalidOperationException){rejected=true;}
            assert(rejected,"Final post-reopen proof rejects changed color, lost underline, legal-size drift, italic or paragraph alignment even when text still matches");
            glyphs[0].Font.Color=0;glyphs[0].Font.Underline=1;glyphs[0].Font.Size=13;glyphs[0].Font.Italic=0;range.ParagraphFormat.Alignment=1;
        }
        foreach(var current in new[]{Array.Empty<WordParagraphSnapshot>(),rows.Concat(rows).ToArray()})
        {
            bool rejected=false;try{Verify(current);}catch(InvalidOperationException){rejected=true;}
            assert(rejected,"Missing or ambiguous text/prefix evidence cannot silently certify a selected format after later stages");
        }
        bool stale=false;try{Verify(prefix:_=>"different prefix");}catch(InvalidOperationException){stale=true;}
        assert(stale,"Identical paragraph text at another context cannot take the place of the approved paragraph");
        bool restored=false,committed=false,failed=false;glyphs[0].Font.Color=255;
        try{PageLayoutBatch.Apply(()=>{},()=>{},()=>Verify(),()=>committed=true,()=>{glyphs[0].Font.Color=0;restored=true;});}
        catch(InvalidOperationException){failed=true;}
        assert(failed && restored && !committed,"A late format regression forces whole-selection rollback instead of publishing successful page/PAGE completion");
    }
}
