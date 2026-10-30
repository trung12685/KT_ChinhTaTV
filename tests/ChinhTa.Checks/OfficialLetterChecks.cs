using ChinhTa.Core;

public static class OfficialLetterChecks
{
    public static (Paragraph[] P,LayoutParagraph[] L) Sample(float size=13,bool bold=false)
    {
        string[] text=["CƠ QUAN MẪU","Số: 01/ABC-VP","V/v tổng hợp kết quả công tác",Checker.Heading,HeadingRules.Motto,"Hà Nội, ngày 03 tháng 3 năm 2026","Kính gửi: đơn vị mẫu.","Nội dung cần giữ nguyên."];
        int start=0;var p=text.Select((t,i)=>{var row=new Paragraph(start,t+"\r",Bold:i==2?bold:true,Size:i==2?size:13,InTable:i<6,Page:1,FormatObserved:true,FormatEditable:true);start+=row.Text.Length;return row;}).ToArray();
        var l=p.Select((row,i)=>new LayoutParagraph(row.Start,row.Text,1,i<6,i<3?0:i<6?p[3].Start:0,i<3?p[3].Start:i<6?p[6].Start:0,true,i<6?0:-1,true,true)).ToArray();return(p,l);
    }
    public static void Run(Action<bool,string> assert)
    {
        var (p,l)=Sample();
        var letter=OfficialLetterRules.Identify(l,DocumentProfile.State);
        assert(letter is not null && letter.Subject.Start==p[2].Start && letter.Number.Start==p[1].Start,"Official-letter V/v subject binds to the left header cell below its own number, not to a named title");
        assert(DocumentHeadingRules.ObservationTargets(p,l,DocumentProfile.State).Single()==p[2],"The native format reader observes the actual official-letter subject even when no named heading exists");
        foreach(float size in new[]{12f,12.5f,13f})
        {
            var sample=Sample(size);
            assert(OfficialLetterRules.Check(sample.P,sample.L,DocumentProfile.State).Count==0,"A centered upright regular official-letter subject retains each valid 12–13pt size: "+size);
        }
        var defective=Sample(14,true);var found=OfficialLetterRules.Check(defective.P,defective.L,DocumentProfile.State);
        var fix=found.Single(i=>i.CanFix);
        assert(fix.CharacterFormat is {Role:ComponentRole.LetterSubject,Bold:false,Italic:false,Size:12} && fix.Original==fix.Replacement && fix.Kind==FixKind.DocumentHeadingFormat,"An official-letter font fix uses its own 12–13pt regular policy and never rewrites text or applies bold named-subject formatting");
        assert(BatchPlan.Create([fix]).Count==1 && fix.Suggestion.Contains("không đậm",StringComparison.Ordinal),"The independent official-letter style is accepted by the validated character-only writer and describes the real change");
        var correct=defective.P.ToArray();correct[2]=correct[2] with {Size=12,Bold=false};
        assert(OfficialLetterRules.Check(correct,defective.L,DocumentProfile.State).Count==0,"A rescan clears a corrected letter-subject font finding");
        var mixed=p.ToArray();mixed[2]=mixed[2] with {FormatEvidence=new(p[2].Text.TrimEnd('\r'),[new("Arial",12,0,0,255),new("Arial",13,0,0,255),new("Arial",14,0,0,255)])};
        var selective=OfficialLetterRules.Check(mixed,l,DocumentProfile.State).Single(i=>i.CanFix).CharacterFormat!;
        assert(selective is {Role:ComponentRole.LetterSubject,PreserveSize:true,RepairInvalidSizes:true,Size:12} && selective.SizeRange==(12f,13f),"Selective letter repairs preserve both valid sizes and change only out-of-range glyphs within the letter policy");
        assert(selective.Matches("Times New Roman",13,0,0,0) && !selective.Matches("Times New Roman",14,0,0,0) && !selective.Matches("Times New Roman",13,0,0,-1),"Post-save letter proof rejects named-document size 14 or bold even if other attributes match");
        foreach(var source in new[]{p.Select((x,i)=>i==2?x with {Text="stale\r"}:x).ToArray(),p.Select((x,i)=>i==2?x with {TextEditable=false,FormatEditable=false}:x).ToArray()})
            assert(!OfficialLetterRules.Check(source,l,DocumentProfile.State).Any(i=>i.CanFix),"Unbound or uneditable native letter text never acquires a font write");
        assert(OfficialLetterRules.Identify(l.Select((x,i)=>i==1?x with {Text="Số: 01/BC-ABC\r"}:x).ToArray(),DocumentProfile.State) is null &&
            !OfficialLetterRules.Check(p.Select((x,i)=>i==1?x with {Text="stale number\r"}:x).ToArray(),l,DocumentProfile.State).Any(i=>i.CanFix),"A named-type code or stale native number cannot silently acquire the letter-specific font policy");
        var offCenter=l.Select((x,i)=>i==2?x with {Centered=false,ParagraphCentered=false,UnindentedCenter=false}:x).ToArray();
        assert(OfficialLetterRules.Check(p,offCenter,DocumentProfile.State).Any(i=>!i.CanFix && i.Title=="Vị trí trích yếu công văn"),"A misplaced official-letter subject is identified independently of its faulty alignment and receives the correct left-header placement guidance");
        var targets=ShapeLineRules.Identify(l,DocumentProfile.State);
        assert(targets.Count==2 && targets.All(t=>t.Role!=LineRole.Subject) && ShapeLineRules.UnidentifiedRoles(targets,l,DocumentProfile.State).Count==0,"A recognized official letter requires only agency and motto line targets and never emits a missing subject-line marker");
        var coverage=DocumentCoverageBuilder.Build(new(DocumentProfile.State,p,l,true,true));
        assert(coverage.Rows.Single(x=>x.Id=="C10").State==CoverageState.Observed && coverage.Rows.Single(x=>x.Id=="C10").Reason.Contains("12–13",StringComparison.Ordinal) && coverage.Rows.Where(x=>x.Id is "C09" or "C24").All(x=>x.State==CoverageState.NotApplicable),"Coverage reports the letter-specific font range and marks named-heading checks inapplicable instead of missing");
        var measured=coverage.WithExternalEvidence("C12",new(CoverageState.Observed,2,2,"Hai mốc đã đo.")).WithFindings(found);
        assert(measured.Rows.Single(x=>x.Id=="C12").State==CoverageState.Observed && measured.Rows.Single(x=>x.Id=="C10").RepairableFindings==1,"Both measured letter lines complete only the letter line family and font findings remain attached to C10");
        foreach(string type in new[]{"QUYẾT ĐỊNH","BÁO CÁO","KẾ HOẠCH","THÔNG BÁO"})
        {
            var named=DocumentHeadingChecks.Sample(type);var namedTargets=ShapeLineRules.Identify(named.L,DocumentProfile.State);
            assert(OfficialLetterRules.Identify(named.L,DocumentProfile.State) is null && namedTargets.Any(t=>t.Role==LineRole.Subject) &&
                ShapeLineRules.RequiredRoles(named.L,DocumentProfile.State).Count==3 && DocumentHeadingRules.Check(named.P,named.L,DocumentProfile.State).Where(i=>i.CanFix && i.CharacterFormat is not null).All(i=>i.CharacterFormat!.Bold==true && i.CharacterFormat.SizeRange==(13f,14f)),"Named "+type+" keeps its below-table bold 13–14pt title/subject and subject line");
            var namedCoverage=DocumentCoverageBuilder.Build(new(DocumentProfile.State,named.P,named.L,true,true)).WithExternalEvidence("C12",new(CoverageState.Observed,2,2,"Chỉ hai mốc."));
            assert(namedCoverage.Rows.Single(x=>x.Id=="C12").State==CoverageState.Partial,"Two measured lines cannot falsely complete a named document's three-line family: "+type);
        }
        foreach(var layout in new[]{l.Where((_,i)=>i!=4).ToArray(),l.Concat([l[2] with {Start=999}]).ToArray(),l.Select((x,i)=>i==2?x with {CellStart=p[3].Start,CellEnd=p[6].Start}:x).ToArray(),l.Select((x,i)=>i==2?x with {Text="V/v\r"}:x).ToArray(),l.Select((x,i)=>i==2?x with {Page=2}:x).ToArray()})
            assert(OfficialLetterRules.Identify(layout,DocumentProfile.State) is null,"Incomplete, duplicate, right-cell, empty or later-page letter hints cannot choose a document policy");
        int cursor=0;var untabled=new[]{0,3,4,1,5,2,6,7}.Select(i=>{var row=l[i] with {Start=cursor,InTable=false,TableStart=-1,CellStart=0,CellEnd=0};cursor+=row.Text.Length;return row;}).ToArray();
        assert(OfficialLetterRules.Identify(untabled,DocumentProfile.State) is not null && !ShapeLineRules.RequiredRoles(untabled,DocumentProfile.State).Contains(LineRole.Subject),"NĐ30 placement does not mandate a Word table: a bounded untabled V/v header also avoids a subject-line requirement");
        foreach(var profile in new[]{DocumentProfile.Party,DocumentProfile.Personal})
            assert(OfficialLetterRules.Identify(l,profile) is null && OfficialLetterRules.Check(p,l,profile).Count==0,"Letter policy does not leak into Party or personal documents");
        bool Reject(Issue item){try{BatchPlan.Create([item]);return false;}catch(InvalidOperationException){return true;}}
        assert(Reject(fix with {CharacterFormat=fix.CharacterFormat! with {Bold=true}}) && Reject(fix with {CharacterFormat=fix.CharacterFormat! with {Size=14}}) && Reject(fix with {Kind=FixKind.AgencyFormat}) && Reject(fix with {Replacement="changed"}),"Forged letter payloads cannot bypass type, size, role or unchanged-text validation");
    }
}
