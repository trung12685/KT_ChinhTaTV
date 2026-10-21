using ChinhTa.Core;

public static class AdministrativeTypographyChecks
{
    private static Paragraph[] Header(string number="số  :  3 / BC – ABC",string date="Hà Nội  Ngày3  Tháng1  Năm2026")
    {
        string[] text=["CƠ QUAN THỬ NGHIỆM",number,Checker.Heading,HeadingRules.Motto,date,"BÁO CÁO","Nội dung  giữ nguyên."];
        int start=0;
        return text.Select((value,index)=>
        {
            var p=new Paragraph(start,value+"\r",Bold:index is 0 or 2 or 3,Size:index==3?14:13,InTable:index<5);
            start+=p.Text.Length;return p;
        }).ToArray();
    }
    public static void Run(Action<bool,string> assert)
    {
        var rows=Header();rows[1]=rows[1] with {FormatObserved=true,Font="Arial",Italic=true,Size=14,Color=255};
        rows[4]=rows[4] with {FormatObserved=true,Font="Arial",Italic=false,Size=14,Color=255};
        var found=Checker.Check(rows);var formats=found.Where(i=>i.CharacterFormat is not null).ToArray();
        assert(formats.Length==2 && formats.All(i=>i.CanFix && i.Original==i.Replacement && !i.Original.Contains('\r')),
            "Observed uniform header typography has whole-row format-only suggestions without paragraph markers");
        assert(formats.Single(i=>i.Start==rows[1].Start).CharacterFormat==new CharacterFormat(13,false) &&
            formats.Single(i=>i.Start==rows[4].Start).CharacterFormat==new CharacterFormat(14,true),
            "Number fixes use upright 13pt while the date retains a valid 14pt size and becomes italic");
        foreach(float size in new[]{12f,13f,13.5f,14f,15f})
        {
            var sample=Header("Số: 03/BC-ABC","Hà Nội, ngày 03 tháng 01 năm 2026");
            sample[4]=sample[4] with {FormatObserved=true,Font="Arial",Size=size};
            var style=Checker.Check(sample).Single(i=>i.CharacterFormat is not null).CharacterFormat!;
            assert(style.Size==(size>=13 && size<=14?size:13),"Date typography preserves a valid size and otherwise proposes the lower valid bound");
        }
        assert(new CharacterFormat(14,true).Matches("Times New Roman",14,-1,0) &&
            !new CharacterFormat(14,true).Matches("Times New Roman",14,9999999,0),"Mixed italic values cannot pass the typography post-check");
        var blank=Header("Số:        /BC-ABC","Hà Nội, ngày        tháng 10 năm 2026");
        blank[1]=blank[1] with {FormatObserved=true,Font="Arial"};blank[4]=blank[4] with {FormatObserved=true,Font="Arial"};
        assert(Checker.Check(blank).Where(i=>i.CharacterFormat is not null).All(i=>i.Original==i.Replacement) &&
            !Checker.Check(blank).Any(i=>i.Kind is FixKind.AdministrativeNumber or FixKind.AdministrativeDate && i.CanFix),
            "Changing typography never fills or contracts an intentional blank number/day");
        foreach(var unsafeRow in new[]{rows[4] with {FormatMixed=true},rows[4] with {TextEditable=false},
            rows[4] with {FormatObserved=false},rows[4] with {Font=""},rows[4] with {Size=float.NaN},rows[4] with {Text="Hà Nội, ngày 31 tháng 2 năm 2026\r"}})
        {
            var sample=Header();sample[4]=unsafeRow;
            assert(!Checker.Check(sample).Any(i=>i.CharacterFormat is not null),"Unobserved, mixed, structured or ambiguous date formatting remains manual");
        }
        foreach(var profile in new[]{DocumentProfile.Party,DocumentProfile.Personal})
            assert(!Checker.Check(rows,profile).Any(i=>i.CharacterFormat is not null),"Administrative typography does not enter Party/Personal profiles");
        assert(!Checker.Check(rows.Select(p=>p with {Page=2})).Any(i=>i.CharacterFormat is not null),"Administrative typography stays on the identified first page");
        var spaced=Header("Số: 03/BC-ABC","Hà  Nội, ngày 03 tháng 01 năm 2026");spaced[4]=spaced[4] with {FormatObserved=true,Font="Arial"};
        assert(Checker.Check(spaced).Any(i=>i.Kind==FixKind.Text && i.Start>spaced[4].Start),"A whole-row format suggestion cannot hide ordinary excess spacing within that row");
        string original=string.Concat(rows.Select(p=>p.Text));string current=original;
        var edits=found.Where(i=>i.CanFix && i.Start<rows[5].Start).ToArray();
        string expected=AtomicTextBatch.ExpectedStory(edits,original,i=>original[..i.Start]);
        var order=new List<bool>();int saves=0,restores=0;
        AtomicTextBatch.Apply(edits,i=>current.Substring(i.Start,i.Length),i=>
        {
            order.Add(i.CharacterFormat is not null);
            if(i.CharacterFormat is null)current=current.Remove(i.Start,i.Length).Insert(i.Start,i.Replacement);
            return i.Replacement;
        },()=>saves++,()=>{restores++;current=original;});
        assert(current==expected && saves==1 && restores==0 && order.Take(2).All(v=>v) && order.Skip(2).All(v=>!v),
            "Combined number/date/text batch applies character formats first, then descending narrow edits in one transaction");
        assert(current.Contains("Số: 03/BC-ABC",StringComparison.Ordinal) && current.Contains("Hà Nội, ngày 03 tháng 01 năm 2026",StringComparison.Ordinal) &&
            current.EndsWith("Nội dung  giữ nguyên.\r",StringComparison.Ordinal),"Combined formatting/text correction preserves unselected body text exactly");
        foreach(bool finalMismatch in new[]{false,true})
        {
            current=original;int changedStyles=0;bool failed=false;
            try
            {
                AtomicTextBatch.Apply(edits,i=>current.Substring(i.Start,i.Length),i=>
                {
                    if(i.CharacterFormat is not null)changedStyles++;
                    else{current=current.Remove(i.Start,i.Length).Insert(i.Start,i.Replacement);if(!finalMismatch)throw new InvalidOperationException("text failed after formats");}
                    return i.Replacement;
                },()=>throw new InvalidOperationException("style post-check failed"),()=>{current=original;changedStyles=0;});
            }
            catch(InvalidOperationException){failed=true;}
            assert(failed && current==original && changedStyles==0,"A content or typography verification failure rolls back the whole combined batch");
        }
        bool Reject(params Issue[] selection)
        {
            try{BatchPlan.Create(selection);return false;}catch(InvalidOperationException){return true;}
        }
        var format=formats[0];
        assert(Reject(format,format with {CharacterFormat=new(14,true)}),"Two competing overlapping formats cannot share a batch");
        assert(Reject(format,format with {Start=format.Start-1,Length=2,Original="xx",Replacement="x",CharacterFormat=null,Kind=FixKind.Text}),
            "Text crossing a typography range boundary remains an invalid overlap");
        assert(Reject(format with {Replacement="changed"}) && Reject(format with {CharacterFormat=new(18,true)}),
            "A format payload cannot smuggle a text replacement or unsupported size through the overlap exception");
        var subset=edits.Where(i=>i.CharacterFormat is null).Take(1).Concat([format]).ToArray();
        assert(BatchPlan.Create(subset).First().CharacterFormat is not null,"A selected format can be combined with a partial text selection without expanding the action");
    }
}
