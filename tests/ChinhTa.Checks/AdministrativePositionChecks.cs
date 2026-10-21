using ChinhTa.Core;

public static class AdministrativePositionChecks
{
    public static void Run(Action<bool,string> assert)
    {
        string[] text=["CƠ QUAN THỬ NGHIỆM","ĐƠN VỊ MẪU","Số:        /BC-ABC",Checker.Heading,HeadingRules.Motto,
            "Địa danh, ngày        tháng 10 năm 2026","BÁO CÁO","Nội dung."];
        int at=100;
        var p=text.Select((s,i)=>{var row=new Paragraph(at,s+"\r",Page:1,InTable:i<6);at+=s.Length+2;return row;}).ToArray();
        var layout=p.Select((r,i)=>new LayoutParagraph(r.Start,r.Text,1,i<6,i<3?p[0].Start:i<6?p[3].Start:0,
            i<3?p[3].Start:i<6?p[6].Start:0,true,i<6?p[0].Start:-1)).ToArray();
        var targets=AdministrativePositionRules.Identify(p,layout,DocumentProfile.State);
        assert(targets.Count==2 && targets.All(t=>t.References.Count==2),"Position roles bind to the two verified header cells and the complete agency/heading blocks");
        assert(targets.All(t=>!t.Probe.CanFix && t.Probe.LineFix is null && t.Probe.CharacterFormat is null),"Structural probes alone never authorize an automatic mutation");
        assert(targets.All(t=>t.ReferencesCentered),"Position targets retain verified balanced-cell reference centering as a separate repair gate");
        var uncentered=AdministrativePositionRules.Identify(p,layout.Select((row,i)=>i==0?row with {Centered=false}:row).ToArray(),DocumentProfile.State);
        assert(!uncentered.Single(t=>t.Role==AdministrativePositionRole.Number).ReferencesCentered &&
            uncentered.Single(t=>t.Role==AdministrativePositionRole.Date).ReferencesCentered,
            "An uncentered agency row blocks only its number repair while preserving independent date evidence");
        var number=targets.Single(t=>t.Role==AdministrativePositionRole.Number);
        var date=targets.Single(t=>t.Role==AdministrativePositionRole.Date);
        RenderedTextLine[] agency=[new(100,50,120,14,61),new(120,66,80,14,77)];
        RenderedTextLine[] headings=[new(300,50,200,14,61),new(320,66,160,14,77)];
        var n=new RenderedTextLine(115,90,90,14,101);var d=new RenderedTextLine(310,89,180,15,101);
        var good=AdministrativePositionRules.Assess(number,n,agency);
        assert(good.Measured && good.Warnings.Count==0 && AdministrativePositionRules.Assess(date,d,headings).Warnings.Count==0,
            "Centered rows below the complete reference blocks pass the measured position subset");
        assert(AdministrativePositionRules.SameRow(n,d) is null,"Different font box heights do not create a false same-row warning when baselines match");
        assert(AdministrativePositionRules.Assess(number,n with {Left=n.Left+3},agency).Warnings.Count==0 &&
            AdministrativePositionRules.Assess(number,n with {Left=n.Left+3.1},agency).Warnings.Count==1,
            "Center warnings use the explicit measurement tolerance without forcing exact floating-point equality");
        assert(AdministrativePositionRules.Assess(date,d with {Top=65,Baseline=76},headings).Warnings.Any(w=>w.Contains("chưa nằm dưới")),
            "Text beside or overlapping the reference block is reported independently of center alignment");
        var divergent=headings.Select((r,i)=>i==1?r with {Left=r.Left+10}:r).ToArray();
        var uncertain=AdministrativePositionRules.Assess(date,d,divergent);
        assert(!uncertain.Measured && uncertain.Warnings.Count==1,"Conflicting national/motto centers cannot silently choose one reference and report compliance");
        assert(AdministrativePositionRules.SameRow(n,d with {Top=95,Baseline=107}) is not null,"Actual baseline separation reports number/date rows at different heights");
        foreach(var absent in new RenderedTextLine?[]{null,n with {Width=double.NaN},n with {Width=0},n with {Baseline=300}})
        {
            var assessment=AdministrativePositionRules.Assess(number,absent,agency);
            assert(!assessment.Measured && assessment.Warnings.Count==1,"Missing, invalid or incomplete row measurements stay explicitly unverified");
        }
        assert(!AdministrativePositionRules.Assess(number,n,[agency[0]]).Measured &&
            !AdministrativePositionRules.Assess(number,n,[agency[0],null]).Measured,"Every structural reference must have a real measurement");
        var partial=AdministrativePositionRules.Identify(p,[],DocumentProfile.State);
        assert(partial.Count==2 && partial.All(t=>t.References.Count==0) && !AdministrativePositionRules.Assess(partial[0],n,[]).Measured,
            "Missing XML cell mapping yields unverified review targets, never an implied position pass");
        var wrongCell=layout.Select((r,i)=>i==5?r with {CellStart=999,CellEnd=1000}:r).ToArray();
        assert(AdministrativePositionRules.Identify(p,wrongCell,DocumentProfile.State).Single(t=>t.Role==AdministrativePositionRole.Date).References.Count==0,
            "A date in a different or invalid cell cannot borrow national/motto references");
        var mixed=layout.Select((r,i)=>i==0?r with {Text="Cơ quan chưa rõ\r"}:r).ToArray();
        assert(AdministrativePositionRules.Identify(p,mixed,DocumentProfile.State).Single(t=>t.Role==AdministrativePositionRole.Number).References.Count==0,
            "A partial agency block cannot be measured as if only its uppercase suffix existed");
        foreach(var profile in new[]{DocumentProfile.Party,DocumentProfile.Personal})
            assert(AdministrativePositionRules.Identify(p,layout,profile).Count==0,"Position review stays within the State profile");
        assert(AdministrativePositionRules.Identify(p.Select(r=>r with {Page=2}).ToArray(),layout,DocumentProfile.State).Count==0,
            "Later-page administrative citations do not become header position targets");
        var protectedRows=p.Select((r,i)=>i is 2 or 5?r with {TextEditable=false}:r).ToArray();
        assert(AdministrativePositionRules.Identify(protectedRows,layout,DocumentProfile.State).Count==0,
            "Structured or unverified text cannot acquire geometric review targets by text alone");
        var duplicate=p.Concat([new Paragraph(999,"Số: /BC-XYZ\r",InTable:true)]).ToArray();
        // Put the duplicate inside the header envelope before the title.
        duplicate=p.Take(6).Concat([duplicate[^1]]).Concat(p.Skip(6)).ToArray();
        assert(!AdministrativePositionRules.Identify(duplicate,layout,DocumentProfile.State).Any(t=>t.Role==AdministrativePositionRole.Number),
            "Duplicate number rows do not select an arbitrary geometric target");
    }
}
