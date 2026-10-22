using ChinhTa.Core;

public static class HeadingPostSaveChecks
{
    public static void Run(Action<bool,string> assert)
    {
        var format=new HeadingFormat(13,1);
        assert(format.Matches("Times New Roman",13,-1,0,0,1) && format.Matches("times new roman",13,-1,0,-16777216,1),
            "Heading post-save proof accepts the requested visible font, bold upright black type and cell centering");
        foreach(var observed in new[]{
            (Font:"Arial",Size:13f,Bold:-1,Italic:0,Color:0,Align:(int?)1),
            (Font:"",Size:13f,Bold:-1,Italic:0,Color:0,Align:(int?)1),
            (Font:"Times New Roman",Size:12f,Bold:-1,Italic:0,Color:0,Align:(int?)1),
            (Font:"Times New Roman",Size:float.NaN,Bold:-1,Italic:0,Color:0,Align:(int?)1),
            (Font:"Times New Roman",Size:float.PositiveInfinity,Bold:-1,Italic:0,Color:0,Align:(int?)1),
            (Font:"Times New Roman",Size:9999999f,Bold:-1,Italic:0,Color:0,Align:(int?)1),
            (Font:"Times New Roman",Size:13f,Bold:0,Italic:0,Color:0,Align:(int?)1),
            (Font:"Times New Roman",Size:13f,Bold:9999999,Italic:0,Color:0,Align:(int?)1),
            (Font:"Times New Roman",Size:13f,Bold:-1,Italic:-1,Color:0,Align:(int?)1),
            (Font:"Times New Roman",Size:13f,Bold:-1,Italic:9999999,Color:0,Align:(int?)1),
            (Font:"Times New Roman",Size:13f,Bold:-1,Italic:0,Color:255,Align:(int?)1),
            (Font:"Times New Roman",Size:13f,Bold:-1,Italic:0,Color:9999999,Align:(int?)1),
            (Font:"Times New Roman",Size:13f,Bold:-1,Italic:0,Color:0,Align:(int?)0),
            (Font:"Times New Roman",Size:13f,Bold:-1,Italic:0,Color:0,Align:(int?)9999999),
            (Font:"Times New Roman",Size:13f,Bold:-1,Italic:0,Color:0,Align:(int?)null)})
            assert(!format.Matches(observed.Font,observed.Size,observed.Bold,observed.Italic,observed.Color,observed.Align),
                "Unchanged visible text cannot conceal missing, mixed or incorrect post-save heading properties");
        assert(new HeadingFormat(13).Matches("Times New Roman",13,-1,0,0,2) &&
            new HeadingFormat(13).Matches("Times New Roman",13,-1,0,0,null),
            "A heading without a requested paragraph alignment is verified without inventing a centering requirement");
        assert(!new HeadingFormat(float.NaN).Matches("Times New Roman",13,-1,0,0,null) &&
            !new HeadingFormat(0).Matches("Times New Roman",0,-1,0,0,null),"Invalid target sizes cannot produce verified heading formatting");
        foreach(var sample in new[]{(Text:Checker.Heading,Kind:FixKind.NationalHeading,Size:12f,Profile:DocumentProfile.State),
            (Text:HeadingRules.Motto,Kind:FixKind.StateMotto,Size:14f,Profile:DocumentProfile.State),
            (Text:HeadingRules.PartyTitle,Kind:FixKind.PartyHeading,Size:15f,Profile:DocumentProfile.Party)})
        {
            var issue=HeadingRules.Check(new(30,sample.Text,Bold:false,Size:sample.Size,Font:"Arial",Page:1),sample.Profile).Single();
            assert(issue.Kind==sample.Kind && issue.Format!.Matches("Times New Roman",sample.Size,-1,0,0,null),
                "National heading, motto and Party heading all retain their own proposed size in post-save proof");
            assert(HeadingRules.Check(new(30,issue.Replacement,Bold:true,Size:sample.Size,Page:1),sample.Profile).Count()==0,
                "Independently rescanning correctly styled heading text removes the repaired issue");
        }
        const string original="Nội dung  thử.\rđộc lập -  tự do - hạnh phúc\r";
        int headingStart=original.IndexOf('đ');
        var heading=HeadingRules.Check(new(headingStart,original[headingStart..],Bold:false,Size:16,Font:"Arial",InTable:true,Page:1),DocumentProfile.State).Single();
        var spacing=Checker.Check([new(0,original[..headingStart])],DocumentProfile.Personal).Single();
        var plan=BatchPlan.Create([heading,spacing]);
        string expected=AtomicTextBatch.ExpectedStory(plan,original,i=>original[..i.Start]);
        string current=original;int writes=0,saves=0,restores=0;bool bold=false;
        string Read(Issue i)=>current.Substring(i.Start,i.Length);
        string Apply(Issue i)
        {
            writes++;current=current.Remove(i.Start,i.Length).Insert(i.Start,i.Replacement);
            // Simulate a COM setter returning normally while Bold has not taken effect.
            return i.Replacement;
        }
        AtomicTextBatch.Apply(plan,Read,Apply,()=>{saves++;if(current!=expected)throw new Exception("story");},()=>{restores++;current=original;});
        assert(current==expected && saves==1 && restores==0 && !bold,
            "Reproduced old gap: story-only verification commits a heading whose visible text is correct but bold never took effect");
        current=original;writes=saves=restores=0;bool rejected=false;
        try
        {
            AtomicTextBatch.Apply(plan,Read,Apply,()=>
            {
                saves++;
                if(current!=expected || !heading.Format!.Matches("Times New Roman",heading.Format.Size,bold?-1:0,0,0,1))
                    throw new InvalidOperationException("post-save heading proof failed");
            },()=>{restores++;current=original;});
        }
        catch(InvalidOperationException){rejected=true;}
        assert(rejected && writes==2 && saves==1 && restores==1 && current==original,
            "Real heading-format verification rolls back the whole group, including a completed spacing edit and heading text replacement");
        current=original;writes=saves=restores=0;bold=true;
        AtomicTextBatch.Apply(plan,Read,Apply,()=>
        {
            saves++;
            if(current!=expected || !heading.Format!.Matches("Times New Roman",heading.Format.Size,-1,0,0,1))
                throw new Exception("proof");
        },()=>{restores++;current=original;});
        assert(current==expected && writes==2 && saves==1 && restores==0,
            "A corrected heading plus a separate selected spacing edit commits once only after text and actual formatting both match");
        assert(new CharacterFormat(13,false).Matches("Times New Roman",13,0,0,0) &&
            new CharacterFormat(13,false).Matches("Times New Roman",13,0,0,-1),
            "Stronger heading proof leaves the existing number/date policy of preserving bold unchanged");
    }
}
