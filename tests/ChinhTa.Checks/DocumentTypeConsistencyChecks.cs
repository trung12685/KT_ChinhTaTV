using ChinhTa.Core;

public static class DocumentTypeConsistencyChecks
{
    private static Paragraph[] Header(string title="KẾ HOẠCH",string number="Số: 01/BC-ABC")
    {
        string[] text=["CƠ QUAN MẪU",number,Checker.Heading,HeadingRules.Motto,
            "Hà Nội, ngày 03 tháng 3 năm 2026",title,"Nội dung thử nghiệm."];
        int cp=137;
        return text.Select((t,i)=>{var p=new Paragraph(cp,t+"\r",Bold:i is 0 or 2 or 3,Size:i==3?14:13,InTable:i<5);cp+=t.Length+3;return p;}).ToArray();
    }
    private static Issue[] Conflicts(Paragraph[] rows,DocumentProfile profile=DocumentProfile.State)=>
        Checker.Check(rows,profile).Where(i=>i.Title==AdministrativeHeaderRules.TypeMismatchTitle).ToArray();
    public static void Run(Action<bool,string> assert)
    {
        foreach(var entry in DocumentTypeCatalog.Entries.Where(e=>e.NamedHeading))
        {
            string other=entry.Abbreviation=="BC"?"KH":"BC";
            var rows=Header(entry.Heading,"Số: 01/"+other+"-ABC-XYZ");var found=Conflicts(rows);
            assert(found.Length==1 && !found[0].CanFix && !found[0].BatchSafe && found[0].Original==other &&
                found[0].Replacement==other && found[0].Start==rows[1].Start+"Số: 01/".Length &&
                found[0].Message.Contains(entry.Heading) && found[0].Message.Contains(entry.Abbreviation),
                "A proven named type and a different known abbreviation yield one precise manual warning without rewriting either side");
            var right=Header(entry.Heading,"Số: 01/"+entry.Abbreviation+"-ABC-XYZ");
            assert(Conflicts(right).Length==0,"Matching legal or user-supplement title/abbreviation pairs have no conflict warning");
            var tooltip=IssueTooltipCatalog.Describe(found[0]);
            assert(tooltip.Text.Split('\n').Length==3 && tooltip.Basis.Contains("II.3") && tooltip.Basis.Contains("Phụ lục III.I") &&
                tooltip.Action.Contains("sửa bên ghi sai") &&
                found[0].Citation!.Contains(DocumentTypeCatalog.UserNote)==(entry.Source==DocumentTypeSource.UserSupplement),
                "Conflict tooltips cite the composition rule and reference table and preserve the custom source in three short blocks");
        }
        var custom=Conflicts(Header("BÁO CÁO","Số: 01/BCĐX-ABC")).Single();
        assert(custom.Citation!.Contains(DocumentTypeCatalog.UserNote) && IssueTooltipCatalog.Describe(custom).Basis.Contains("BCĐX là bổ sung"),
            "The user source follows BCĐX even when it occurs in the conflicting code rather than the title");
        foreach(string abbreviation in new[]{"TTR","ttr","TTr"})
        {
            var rows=Header("KẾ HOẠCH","Số: 01/"+abbreviation+"-ABC");
            assert(Conflicts(rows).Length==1 && !Checker.Check(rows).Any(i=>i.CanFix && i.Kind==FixKind.AdministrativeNumber) &&
                Checker.Check(rows).Count(i=>i.Kind==FixKind.AdministrativeNumber)==1,
                "Case variants of a different named type are one conflict, never an automatic code change or duplicate lowercase warning");
        }
        foreach(string code in new[]{"XYZ","CV","BCDX","QuyĐ","SY","TrS","SL"})
            assert(Conflicts(Header(number:"Số: 01/"+code+"-ABC")).Length==0,
                "Unknown codes, missing accents and copy abbreviations do not acquire inferred original-document conflicts");
        foreach(string title in new[]{"Công văn","V/v phối hợp công tác","Nội dung thường.","BẢN SAO Y","BẢN TRÍCH SAO","BẢN SAO LỤC"})
            assert(Conflicts(Header(title)).Length==0,"Unidentified and copy titles do not acquire original-document type conflicts");
        var source=Header();
        var duplicate=source.Take(2).Append(new Paragraph(0,"Số: 02/BC-XYZ\r",InTable:true)).Concat(source.Skip(2));
        int duplicateCp=137;
        var duplicateRows=duplicate.Select(p=>{var row=p with {Start=duplicateCp};duplicateCp+=p.Text.Length+3;return row;}).ToArray();
        foreach(var rows in new[]{source.Select(p=>p with {Page=2}).ToArray(),
            source.Select((p,i)=>i==1 || i==5?p with {TextEditable=false}:p).ToArray(),
            Header(number:"Số:\t01/BC-ABC"),Header(number:"Số: 01/2026/BC-ABC"),
            source.Where((_,i)=>i!=3).ToArray(),duplicateRows,
            source.Append(new Paragraph(999,"KẾ HOẠCH\r")).ToArray()})
            assert(Conflicts(rows).Length==0,"Page, XML/control, tab, unsupported pattern, heading and duplicate guards prevent speculative conflicts");
        assert(new[]{DocumentProfile.Party,DocumentProfile.Personal}.All(p=>Conflicts(source,p).Length==0),
            "The new State format comparison does not leak into Party or Personal modes");
        var late=Header("Nội dung mở đầu.").Append(new Paragraph(999,"KẾ HOẠCH\r")).ToArray();
        assert(Conflicts(late).Length==0,"A named type appearing later in the body cannot create a header-code conflict");
        var body=Header("KẾ HOẠCH","Số: 01/KH-ABC");body[6]=body[6] with {Text="Số: 01/BC-ABC\r"};
        assert(Conflicts(body).Length==0,"A body citation is excluded from the administrative header comparison");
        var uncertainTitle=source.Select((p,i)=>i==5?p with {TextEditable=false}:p).ToArray();
        assert(Conflicts(uncertainTitle).Length==0,"An unverified named-title XML range cannot authorize a semantic format comparison");
        var spaced=Header(number:"số  :  3 / BC – ABC");var all=Checker.Check(spaced);
        var plan=BatchPlan.Create(all.Where(i=>i.CanFix && i.Kind==FixKind.AdministrativeNumber));
        CurrentTextPlan.Validate(plan,all);string original=spaced[1].Text,updated=original;
        foreach(var i in plan)updated=updated.Remove(i.Start-spaced[1].Start,i.Length).Insert(i.Start-spaced[1].Start,i.Replacement);
        assert(updated=="Số: 03/BC-ABC\r" && Conflicts(spaced).Length==1,
            "A manual type conflict coexists with selected label/zero/separator repairs and never grants a code rewrite");
        var rescanned=spaced.Select((p,i)=>i==1?p with {Text=updated}:p).ToArray();var remaining=Checker.Check(rescanned);
        assert(remaining.Count(i=>i.Kind==FixKind.AdministrativeNumber)==1 && Conflicts(rescanned).Length==1 &&
            Conflicts(rescanned)[0].Start==rescanned[1].Start+"Số: 03/".Length && !remaining.Any(i=>i.CanFix),
            "Fresh offsets keep only the real conflict after repairing other number errors; the warning is not hidden by edit history");
        var fixedCode=Header(number:"Số: 01/KH-ABC");var fixedTitle=Header("BÁO CÁO");
        assert(Conflicts(fixedCode).Length==0 && Conflicts(fixedTitle).Length==0,
            "Either verified manual correction clears the conflict on a fresh scan");
        bool manualRejected=false;try{BatchPlan.Create([Conflicts(source).Single()]);}catch(InvalidOperationException){manualRejected=true;}
        assert(manualRejected && IssueReviewCatalog.Describe(Conflicts(source).Single()) is
            {Section:IssueReviewSection.ManualWarning,FamilyKey:"number",Label:AdministrativeHeaderRules.TypeMismatchTitle},
            "The compact conflict stays in the manual number family and cannot enter a fix-selected batch");
        var old=Header("TỜ TRÌNH","Số: 01/TTR-ABC");var oldCase=Checker.Check(old).Single(i=>i.CanFix && i.Replacement=="TTr");
        var changed=old.Select((p,i)=>i==5?p with {Text="KẾ HOẠCH\r"}:p).ToArray();bool stale=false;
        try{CurrentTextPlan.Validate([oldCase],Checker.Check(changed));}catch(InvalidOperationException){stale=true;}
        assert(stale && Conflicts(changed).Length==1,"A title change invalidates the earlier case-only edit before any write");
    }
}
