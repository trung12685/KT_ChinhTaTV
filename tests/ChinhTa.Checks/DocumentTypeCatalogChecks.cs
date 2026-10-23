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
        string report=DevelopmentReport.Build("test",DateTimeOffset.UnixEpoch);
        assert(DocumentTypeCatalog.Entries.All(e=>report.Contains(e.Name+" — "+e.Abbreviation)) && report.Contains(DocumentTypeCatalog.UserNote),
            "The exported report contains the complete catalog with separate provenance and no invented statutory BCĐX entry");
    }
    private static Paragraph[] Header(string number)
    {
        string[] text=["CƠ QUAN MẪU",number,Checker.Heading,HeadingRules.Motto,"Hà Nội, ngày 03 tháng 3 năm 2026","BÁO CÁO","Nội dung."];
        int cp=0;return text.Select((t,i)=>{var p=new Paragraph(cp,t+"\r",Bold:i<5,Size:i==3?14:13,InTable:i<5);cp+=t.Length+1;return p;}).ToArray();
    }
}
