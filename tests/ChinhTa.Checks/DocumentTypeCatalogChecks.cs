using ChinhTa.Core;

public static class DocumentTypeCatalogChecks
{
    public static void Run(Action<bool,string> assert)
    {
        string[] abbreviations=["NQ","QĐ","CT","QC","QyĐ","TC","TB","HD","CTr","KH","PA","ĐA","DA","BC","BB","TTr","HĐ","CĐ","BGN","BTT","GUQ","GM","GGT","GNP","PG","PC","PB","SY","TrS","SL","BCĐX"];
        assert(DocumentTypeCatalog.Entries.Select(e=>e.Abbreviation).SequenceEqual(abbreviations),
            "The full reference preserves all 27 administrative abbreviations and 3 copy abbreviations in PDF order plus user BCĐX");
        string[] names=["Nghị quyết (cá biệt)","Quyết định (cá biệt)","Chỉ thị","Quy chế","Quy định","Thông cáo","Thông báo","Hướng dẫn","Chương trình","Kế hoạch","Phương án","Đề án","Dự án","Báo cáo","Biên bản","Tờ trình","Hợp đồng","Công điện","Bản ghi nhớ","Bản thỏa thuận","Giấy uỷ quyền","Giấy mời","Giấy giới thiệu","Giấy nghỉ phép","Phiếu gửi","Phiếu chuyển","Phiếu báo","Bản sao y","Bản trích sao","Bản sao lục","Báo cáo đề xuất"];
        assert(DocumentTypeCatalog.Entries.Select(e=>e.Name).SequenceEqual(names),
            "Each exact abbreviation stays paired with the corresponding signed-table name and user addition");
        assert(DocumentTypeCatalog.Entries.Count(e=>e.Source==DocumentTypeSource.DecreeAdministrative)==27 &&
            DocumentTypeCatalog.Entries.Count(e=>e.Source==DocumentTypeSource.DecreeCopy)==3 &&
            DocumentTypeCatalog.Entries.Count(e=>e.Source==DocumentTypeSource.UserSupplement)==1,
            "Legal administrative, legal copy and user-added entries keep distinct provenance");
        assert(DocumentTypeCatalog.Entries.Select(e=>e.Name).Distinct().Count()==31 && DocumentTypeCatalog.Entries.Select(e=>e.Abbreviation).Distinct().Count()==31,
            "All reference names and exact abbreviations are unique");
        foreach(var entry in DocumentTypeCatalog.Entries)
        {
            assert(DocumentTypeCatalog.IsKnownAbbreviation(entry.Abbreviation),"Every exact reference abbreviation is available for lookup");
            if(!entry.NamedHeading)
            {
                assert(!ShapeLineRules.IsDocumentType(entry.Heading),"Copy headings cannot acquire the original-document title/subject format rules");continue;
            }
            var source=DocumentHeadingChecks.Sample(entry.Heading.ToLowerInvariant());
            var found=DocumentHeadingRules.Check(source.P,source.L,DocumentProfile.State);
            var fix=found.Single(i=>i.Kind==FixKind.DocumentTypeText);
            assert(fix.CanFix && fix.Replacement==entry.Heading && fix.Original==entry.Heading.ToLowerInvariant(),
                "Every catalogued administrative or user name participates in the same bounded case-repair rules");
            BatchPlan.Create(found.Where(i=>i.CanFix));
            bool custom=entry.Source==DocumentTypeSource.UserSupplement;
            assert(found.All(i=>i.Citation?.Contains(DocumentTypeCatalog.UserNote,StringComparison.Ordinal)==custom),
                "The custom name remains attributed to the user in case, typography and manual issues");
            var target=ShapeLineRules.Identify(source.L,DocumentProfile.State).Single(t=>t.Role==LineRole.Subject);
            assert(target.UserDefinedType==custom && ShapeLineRules.Citation(target).Contains(DocumentTypeCatalog.UserNote,StringComparison.Ordinal)==custom,
                "Subject-Line proposals retain the custom-name provenance instead of placing BCĐX in the decree table");
            if(custom)assert(IssueTooltipCatalog.Describe(fix).Text.Contains("BCĐX là bổ sung theo yêu cầu người dùng") &&
                IssueTooltipCatalog.Describe(fix).Text.Split('\n').Length==3,"Custom-name tooltips distinguish the user extension within three concise blocks");
        }
        assert(DocumentTypeCatalog.FindHeading("Giấy uỷ quyền")==DocumentTypeCatalog.FindHeading("Giấy ủy quyền"),
            "Both uỷ/ủy spellings identify the same reference without changing the user's vowel accents");
        var alias=DocumentHeadingChecks.Sample("Giấy ủy quyền");
        assert(DocumentHeadingRules.Check(alias.P,alias.L,DocumentProfile.State).Single(i=>i.Kind==FixKind.DocumentTypeText).Replacement=="GIẤY ỦY QUYỀN",
            "A case repair preserves the source ủy spelling instead of changing it to uỷ");
        foreach(string wrong in new[]{"Công văn","CV","BCDX","Báo cáo đề xuất thêm","BẢN SAO Y"})
            assert(!ShapeLineRules.IsDocumentType(wrong),"Unknown names, arbitrary prose, Công văn and copy names do not become named original-document titles");
        foreach(string abbreviation in new[]{"QyĐ","CTr","TTr","TrS","BCĐX"})
        {
            var rows=Header("Số: 01/"+abbreviation+"-ABC");
            assert(!Checker.Check(rows).Any(i=>i.Kind==FixKind.AdministrativeNumber),
                "An exact mixed-case reference abbreviation alone cannot create a false lowercase-code warning");
            var bad=Header("Số: 01/"+abbreviation+"-Abc");
            assert(Checker.Check(bad).Any(i=>i.Kind==FixKind.AdministrativeNumber && !i.CanFix && i.Message.Contains("chữ thường")),
                "Accepting the type abbreviation never hides lowercase letters in the agency-code suffix");
        }
        assert(Checker.Check(Header("Số: 01/QuyĐ-ABC")).Any(i=>i.Kind==FixKind.AdministrativeNumber && !i.CanFix),
            "QuyĐ is not silently accepted as the exact QyĐ spelling from the signed table");
        var spaced=Header("Số: 3 / TTr – ABC");var issues=Checker.Check(spaced).Where(i=>i.Kind==FixKind.AdministrativeNumber).ToArray();
        string number=spaced[1].Text;
        foreach(var i in BatchPlan.Create(issues))number=number.Remove(i.Start-spaced[1].Start,i.Length).Insert(i.Start-spaced[1].Start,i.Replacement);
        assert(number=="Số: 03/TTr-ABC\r","Existing zero/separator repairs preserve the legal mixed-case abbreviation exactly");
        foreach(var entry in DocumentTypeCatalog.Entries)
        {
            string wrong=entry.Abbreviation==entry.Abbreviation.ToUpperInvariant()?entry.Abbreviation.ToLowerInvariant():entry.Abbreviation.ToUpperInvariant();
            var rows=Header("Số: 01/"+wrong+"-ABC-XYZ",entry.Heading);
            var fixes=Checker.Check(rows).Where(i=>i.Kind==FixKind.AdministrativeNumber).ToArray();
            if(!entry.NamedHeading)
            {
                assert(fixes.All(i=>!i.CanFix),"Copy abbreviations cannot use original-document heading recognition to rewrite a code");continue;
            }
            assert(fixes.Length==1 && fixes[0].CanFix && fixes[0].Original==wrong && fixes[0].Replacement==entry.Abbreviation &&
                fixes[0].Start==rows[1].Start+"Số: 01/".Length,
                "Case-only proposals use the exact catalog entry and exclude the number and every agency-code segment");
            var fixedRows=rows.Select(p=>p==rows[1]?p with {Text=p.Text.Remove(fixes[0].Start-p.Start,fixes[0].Length).Insert(fixes[0].Start-p.Start,fixes[0].Replacement)}:p).ToArray();
            assert(!Checker.Check(fixedRows).Any(i=>i.Kind==FixKind.AdministrativeNumber),
                "Rescanning each repaired abbreviation is idempotent, including legal mixed-case and the user addition");
            var tooltip=IssueTooltipCatalog.Describe(fixes[0]);bool custom=entry.Source==DocumentTypeSource.UserSupplement;
            assert(custom?tooltip.Basis.Contains("người dùng") && !tooltip.Basis.Contains("Nghị định"):
                tooltip.Basis.Contains("Phụ lục III, phần I"),"Abbreviation tooltips retain the exact legal or user-supplement source");
        }
        foreach(string unknown in new[]{"CV","cv","BCDX","bcdx","QuyĐ","T.Tr","ABC","BCA"})
            assert(!Checker.Check(Header("Số: 01/"+unknown+"-XYZ")).Any(i=>i.CanFix && i.Kind==FixKind.AdministrativeNumber),
                "Unknown abbreviations, missing accents and agency identifiers are never inferred from the catalog");
        var mixed=Header("số  :  3 / TTR – Abc-XYZ","TỜ TRÌNH");
        var mixedFixes=Checker.Check(mixed).Where(i=>i.Kind==FixKind.AdministrativeNumber && i.CanFix).ToArray();
        var mixedPlan=BatchPlan.Create(mixedFixes);CurrentTextPlan.Validate(mixedPlan,Checker.Check(mixed));
        string mixedNumber=mixed[1].Text;
        foreach(var i in mixedPlan)mixedNumber=mixedNumber.Remove(i.Start-mixed[1].Start,i.Length).Insert(i.Start-mixed[1].Start,i.Replacement);
        assert(mixedNumber=="Số: 03/TTr-Abc-XYZ\r","Abbreviation case repairs compose with label, zero and separators while preserving agency letters");
        var remaining=Checker.Check(mixed.Select(p=>p==mixed[1]?p with {Text=mixedNumber}:p).ToArray());
        assert(remaining.Count(i=>i.Kind==FixKind.AdministrativeNumber)==1 && remaining.Any(i=>i.Kind==FixKind.AdministrativeNumber && !i.CanFix),
            "A real lowercase agency-code warning remains after all supported number edits");
        var caseOnly=mixedPlan.Single(i=>i.Replacement=="TTr");
        var locked=mixed.Select(p=>p==mixed[1]?p with {TextEditable=false}:p).ToArray();
        bool stale=false;try{CurrentTextPlan.Validate([caseOnly],Checker.Check(locked));}catch(InvalidOperationException){stale=true;}
        assert(stale,"A saved abbreviation proposal loses edit permission when the fresh XML/text proof no longer allows writing");
        foreach(var rows in new[]{Header("Số:\t01/TTR-ABC"),Header("Số: 01/TTR-ABC").Select(p=>p with {Page=2}).ToArray(),locked})
            assert(!Checker.Check(rows).Any(i=>i.CanFix && i.Kind==FixKind.AdministrativeNumber),
                "Tabbed, second-page and unverified number rows cannot acquire abbreviation fixes");
        string mutable=mixed[1].Text,original=mutable;int writes=0,restores=0;bool failed=false;
        try{AtomicTextBatch.Apply(mixedPlan,i=>mutable.Substring(i.Start-mixed[1].Start,i.Length),i=>
            {mutable=mutable.Remove(i.Start-mixed[1].Start,i.Length).Insert(i.Start-mixed[1].Start,i.Replacement);writes++;return i.Replacement;},
            ()=>throw new IOException("synthetic save failure"),()=>{mutable=original;restores++;});}
        catch(IOException){failed=true;}
        assert(failed && writes==mixedPlan.Count && restores==1 && mutable==original,
            "A failing final save restores the entire selected batch, including case and separator edits");
        foreach(string title in new[]{"Công văn","V/v phối hợp công tác","BÁO CÁO","Nội dung thường."})
            assert(!Checker.Check(Header("Số: 01/TTR-ABC",title)).Any(i=>i.CanFix && i.Kind==FixKind.AdministrativeNumber),
                "Without the corresponding named type, a coincidentally catalogued agency prefix is never rewritten");
        var repeatedTitle=Header("Số: 01/TTR-ABC","TỜ TRÌNH").Append(new Paragraph(999,"TỜ TRÌNH\r")).ToArray();
        assert(!Checker.Check(repeatedTitle).Any(i=>i.CanFix && i.Kind==FixKind.AdministrativeNumber),
            "Two named-type candidates cannot authorize an abbreviation edit");
        var bodyTitle=Header("Số: 01/TTR-ABC","Nội dung mở đầu.").Append(new Paragraph(999,"TỜ TRÌNH\r")).ToArray();
        assert(!Checker.Check(bodyTitle).Any(i=>i.CanFix && i.Kind==FixKind.AdministrativeNumber),
            "A later named type inside prose cannot authorize a header-code edit");
        string report=DevelopmentReport.Build("test",DateTimeOffset.UnixEpoch);
        assert(DocumentTypeCatalog.Entries.All(e=>report.Contains(e.Name+" — "+e.Abbreviation)) && report.Contains(DocumentTypeCatalog.UserNote),
            "The exported report contains the complete catalog with separate provenance and no invented statutory BCĐX entry");
    }
    private static Paragraph[] Header(string number,string title="BÁO CÁO")
    {
        string[] text=["CƠ QUAN MẪU",number,Checker.Heading,HeadingRules.Motto,"Hà Nội, ngày 03 tháng 3 năm 2026",title,"Nội dung."];
        int cp=0;return text.Select((t,i)=>{var p=new Paragraph(cp,t+"\r",Bold:i<5,Size:i==3?14:13,InTable:i<5);cp+=t.Length+1;return p;}).ToArray();
    }
}
