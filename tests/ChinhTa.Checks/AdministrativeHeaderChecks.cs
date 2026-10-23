using ChinhTa.Core;
using System.Xml.Linq;

public static class AdministrativeHeaderChecks
{
    private static Paragraph[] Header(string number="Số: 01/BC-ABC",string date="Hà Nội, ngày 03 tháng 3 năm 2026")
    {
        string[] text=["CƠ QUAN THỬ NGHIỆM",number,Checker.Heading,HeadingRules.Motto,date,"BÁO CÁO","Nội dung thử nghiệm."];
        int start=170;
        return text.Select((value,index)=>
        {
            var p=new Paragraph(start,value+"\r",Bold:index is 0 or 2 or 3,Size:index==3?14:13,
                InTable:index<5,Italic:index==4);
            start+=value.Length+3;return p;
        }).ToArray();
    }
    private static Issue[] Found(Paragraph[] p)=>Checker.Check(p).Where(i=>i.Kind is FixKind.AdministrativeNumber or FixKind.AdministrativeDate).ToArray();
    private static string Apply(Paragraph p,IEnumerable<Issue> issues)
    {
        string text=p.Text;
        foreach(var i in BatchPlan.Create(issues.Where(i=>i.CanFix && i.Start>=p.Start && i.Start<p.Start+p.Text.Length)))
        {
            int at=i.Start-p.Start;
            if(text.Substring(at,i.Length)!=i.Original)throw new Exception("Wrong source offset");
            text=text.Remove(at,i.Length).Insert(at,i.Replacement);
        }
        return text;
    }
    public static void Run(Action<bool,string> assert)
    {
        assert(Found(Header()).Length==0,"Canonical administrative number and March date do not generate new warnings");
        var p=Header("số  :  3 / BC – ABC","Hà Nội  Ngày3  Tháng1  Năm2026");
        var issues=Found(p);
        assert(Apply(p[1],issues)=="Số: 03/BC-ABC\r","Number fixes change only the label, leading zero and separators with actual paragraph offsets");
        assert(Apply(p[4],issues)=="Hà Nội, ngày 03 tháng 01 năm 2026\r","Date fixes normalize comma, keywords, day/month zero and spaces without choosing new values");
        assert(!Checker.Check(p).Any(i=>i.Kind==FixKind.Text && issues.Any(a=>a.CanFix && i.Start<a.Start+a.Length && a.Start<i.Start+i.Length)),
            "Generic spacing does not overlap a structured administrative edit");
        var repaired=p.Select(row=>row with {Text=Apply(row,issues)}).ToArray();
        assert(Found(repaired).Length==0,"Applying both number and date edits leaves no residual administrative warning");
        // Rebuild paragraph CPs after each real text change instead of carrying old offsets
        // into the next scan. Exercise each individual edit and representative grouped choices.
        var editable=issues.Where(i=>i.CanFix).ToArray();
        var selections=editable.Select(i=>new[]{i}).Concat(new[]{editable.Where(i=>i.Kind==FixKind.AdministrativeNumber).ToArray(),
            editable.Where(i=>i.Kind==FixKind.AdministrativeDate).ToArray(),editable}).ToArray();
        foreach(var selection in selections)
        {
            int next=170;
            var current=p.Select(row=>
            {
                var updated=row with {Start=next,Text=Apply(row,selection)};
                next+=updated.Text.Length+2;return updated;
            }).ToArray();
            var remaining=Found(current);
            var final=current.Select(row=>row with {Text=Apply(row,remaining)}).ToArray();
            assert(final.Select(row=>row.Text).SequenceEqual(repaired.Select(row=>row.Text)) && Found(final).Length==0,
                "Rescan with shifted Word offsets completes the selected individual/group edit without residual warnings");
        }
        foreach(string number in new[]{"Số:        /BC-ABCD","Số:    …/BC-ABCD","Số: 01/UBND-VP","Số: 12/QĐ-ABC-XYZ","Số: 001/BC-ABC"})
            assert(Found(Header(number)).Length==0,"Valid identifier or intentional blank number is preserved: "+number);
        foreach(string date in new[]{"Ninh Bình, ngày        tháng 10 năm 2026","Hà Nội, ngày    tháng    năm    ",
            "Hà Nội, ngày … tháng … năm …","Hà Nội, ngày 29 tháng 02 năm 2024","Hà Nội, ngày 29 tháng 02 năm    ",
            "Hà Nội, ngày 03 tháng 03 năm 2026","Hà Nội, ngày 03 tháng 3 năm 2026"})
            assert(Checker.Check(Header(date:date)).All(i=>i.Kind is not (FixKind.AdministrativeDate or FixKind.Text)),
                "Valid or incomplete date preserves blank fields and does not over-pad March: "+date);
        var partial=Header(date:"Ninh Bình, ngày        tháng1 năm2026");
        assert(Apply(partial[4],Found(partial))=="Ninh Bình, ngày        tháng 01 năm 2026\r",
            "Formatting filled month/year preserves the exact intentional blank-day spacing");
        foreach(string date in new[]{"Hà Nội, ngày        tháng 10 năm 2026 (dự thảo)  ghi chú",
            "Hà Nội, ngày        tháng 10 năm 2026 bản dự thảo  ghi chú",
            "Hà Nội, ngày 03 tháng        năm 2026 (dự thảo)  ghi chú",
            "Hà Nội, ngày    tháng    năm        (dự thảo)  ghi chú"})
        {
            var rows=Header(date:date);var found=Checker.Check(rows);
            var spacing=found.Where(i=>i.CanFix && i.Kind==FixKind.Text && i.Start>=rows[4].Start &&
                i.Start<rows[4].Start+rows[4].Text.Length).ToArray();
            int note=date.LastIndexOf("  ",StringComparison.Ordinal);
            assert(Found(rows).Any(i=>!i.CanFix) && spacing.Length==1 && spacing[0].Start==rows[4].Start+note &&
                Apply(rows[4],found)==date.Remove(note,2).Insert(note," ")+"\r",
                "Annotated manual date preserves only blank fields while still checking excess spacing in the annotation");
        }
        var annotatedBody=Header();annotatedBody[6]=annotatedBody[6] with {Text="Hà Nội, ngày        tháng 10 năm 2026 (dự thảo)\r"};
        assert(Checker.Check(annotatedBody).Any(i=>i.CanFix && i.Kind==FixKind.Text && i.Start>=annotatedBody[6].Start),
            "The annotation exemption stays inside the identified header, never ordinary body text");
        var filledInvalid=Header(date:"Hà Nội, ngày 31  tháng 2 năm 2026 (dự thảo)");
        assert(Checker.Check(filledInvalid).Any(i=>i.CanFix && i.Kind==FixKind.Text && i.Start>=filledInvalid[4].Start),
            "A filled day is not mistaken for an intentional blank in a manual annotated date");
        var missingPlace=Checker.Check(Header(date:"ngày    tháng    năm    "));
        assert(missingPlace.Any(i=>i.Kind==FixKind.AdministrativeDate && !i.CanFix) && !missingPlace.Any(i=>i.CanFix),
            "Missing place remains manual without collapsing any blank day/month/year fields");
        assert(!Checker.Check(Header("Số:     .../...")).Any(i=>i.CanFix),
            "An unresolved abbreviation template keeps its intentional blank-number spacing while remaining manual");
        foreach(string number in new[]{"Số: 3/2026/QĐ-ABC","Số: 3/BC","Số: 00/BC-ABC","Số: 3 BC-ABC","Số: 3/BC-ABC ghi chú"})
        {
            var notices=Found(Header(number));
            assert(notices.Any(i=>!i.CanFix) && notices.All(i=>!i.CanFix),"Ambiguous, unsupported or zero identifiers remain manual: "+number);
        }
        var lower=Found(Header("Số: 03/bc-abc"));
        assert(lower.Count(i=>i.CanFix)==1 && lower.Single(i=>i.CanFix).Original=="bc" &&
            lower.Single(i=>i.CanFix).Replacement=="BC" && lower.Count(i=>!i.CanFix)==1,
            "Known type casing has a separate proposal while the lowercase agency code remains manual");
        foreach(string date in new[]{"Hà Nội, ngày 31 tháng 4 năm 2026","Hà Nội, ngày 29 tháng 2 năm 2025",
            "Hà Nội, ngày 00 tháng 1 năm 2026","Hà Nội, ngày 12 tháng 13 năm 2026","Hà Nội, ngày 03 tháng 1 năm 26",
            "Hà Nội, ngày 03 tháng 1 năm 0000","Hà Nội, ngày 123 tháng 1 năm 2026","Hà Nội, ngày 03 tháng 1 năm 20__",
            "ngày 03 tháng 1 năm 2026"})
        {
            var notices=Found(Header(date:date));
            assert(notices.Any(i=>!i.CanFix) && notices.All(i=>!i.CanFix),"Invalid calendar or incomplete year is manual and values are retained: "+date);
        }
        var duplicate=Header("Số: 3/BC-ABC").ToList();
        duplicate.Insert(2,new(900,"Số: 4/BC-XYZ\r",InTable:true));
        assert(Found(duplicate.ToArray()).Where(i=>i.Kind==FixKind.AdministrativeNumber).All(i=>!i.CanFix),
            "Two candidate number rows cannot select the first one for automatic correction");
        var body=Header();body[6]=body[6] with {Text="Số: 3 / BC - ABC\r"};
        assert(!Found(body).Any(i=>i.Start>=body[6].Start),"A numbered citation after the document title is outside the header scope");
        body[6]=body[6] with {Text="Hà Nội ngày 31 tháng 2 năm 2026\r"};
        assert(!Found(body).Any(i=>i.Start>=body[6].Start),"Body dates are not treated as the issue-date header");
        body[6]=body[6] with {Text="Văn bản ghi ngày    tháng 10 năm 2026\r"};
        assert(Checker.Check(body).Any(i=>i.Kind==FixKind.Text && i.Start>=body[6].Start),
            "Expanded blank-date exemptions cannot suppress ordinary body spacing after the header");
        var page2=Header("Số: 3/BC-ABC").Select(row=>row with {Page=2}).ToArray();
        assert(Found(page2).Length==0,"New administrative rules do not apply on page two");
        assert(new[]{DocumentProfile.Party,DocumentProfile.Personal}.All(profile=>
            !Checker.Check(Header("Số: 3/BC-ABC"),profile).Any(i=>i.Kind is FixKind.AdministrativeNumber or FixKind.AdministrativeDate)),
            "Party and Personal profiles retain their own rules");
        assert(Found(Header("Số: 3/BC-ABC").Where((_,i)=>i!=3).ToArray()).Length==0,
            "Without a unique recognized national-heading pair the rules cannot assume a State header");
        var format=Header();format[1]=format[1] with {FormatObserved=true,Italic=true,Size=14};
        format[4]=format[4] with {FormatObserved=true,Italic=false,Font="Arial"};
        assert(Checker.Check(format).Count(i=>i.Kind==FixKind.AdministrativeFormat && i.CanFix)==2,
            "Uniform observed number/date formatting errors offer character-only fixes");
        format=Header();format[4]=format[4] with {FormatObserved=true,FormatMixed=true};
        assert(Found(format).Single().CanFix==false,"Mixed run formatting cannot be accepted as uniformly italic");
        assert(Found(Header()).Length==0,"Unobserved default formatting is not presented as measured compliance");
        var protectedRow=Header("Số: 3 / BC - ABC");
        protectedRow[1]=protectedRow[1] with {TextEditable=false};
        assert(Found(protectedRow).Any(i=>!i.CanFix) && !Checker.Check(protectedRow).Any(i=>i.CanFix &&
            i.Start>=protectedRow[1].Start && i.Start<protectedRow[1].Start+protectedRow[1].Text.Length),
            "Unverified or structured header text cannot be changed through administrative or generic spacing edits");
        var tabbed=Header("Số:\t3 / BC - ABC");
        assert(Found(tabbed).Any(i=>!i.CanFix) && !Found(tabbed).Any(i=>i.CanFix),"Layout tabs in a header require manual review");
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        const string plain="Số: 3/BC-ABC";
        XElement Run()=>new(w+"r",new XElement(w+"t",plain));
        bool Editable(XElement bodyContent,string expected=plain)
        {
            string xml=new XDocument(new XElement(w+"document",new XElement(w+"body",bodyContent))).ToString();
            bool editable=false;
            WordXmlTableMap.Read(xml,[new(0,expected.Length+1,expected+"\r",1,false,false)],_=>{},
                onTextEditable:(_,value)=>editable=value);
            return editable;
        }
        assert(Editable(new(w+"p",Run())),"Exact plain Word XML text provides editability evidence");
        var tabStops=new XElement(w+"pPr",new XElement(w+"tabs",new XElement(w+"tab",
            new XAttribute(w+"val","center"),new XAttribute(w+"pos","1000"))));
        assert(Editable(new(w+"p",tabStops,Run())),"Formatting tab stops without tab characters do not disable plain-text edits");
        assert(!Editable(new(w+"p",new XElement(w+"pPr",tabStops.Elements()),new XElement(w+"r",new XElement(w+"tab")),Run()),"\t"+plain),
            "A real run tab still cannot be accepted as plain text even when tab-stop formatting is present");
        foreach(string wrapper in new[]{"fldSimple","hyperlink","smartTag","sdt"})
            assert(!Editable(new(w+"p",new XElement(w+wrapper,Run()))),"Structured Word run is kept manual: "+wrapper);
        assert(!Editable(new(w+"sdt",new XElement(w+"sdtContent",new XElement(w+"p",Run())))),
            "A paragraph inside a content control is never accepted as plain editable text");
        assert(!Editable(new(w+"p",new XElement(w+"r",new XElement(w+"fldChar")),Run())),
            "Complex field mapping cannot produce editability evidence");
        assert(!Editable(new(w+"p",new XElement(w+"r",new XElement(w+"t",plain+"x")))),
            "A text mismatch cannot produce editability evidence");
        foreach(string sample in new[]{"SoKyHieuNgayThang.docx","TruongSoNgayDeTrong.docx"})
        {
            string path=Path.GetFullPath(Path.Combine("samples",sample));
            byte[] original=File.ReadAllBytes(path);
            var rows=DocxLineFixture.Read(path).Select(p=>new Paragraph(p.Start,p.Text,
                Bold:HeadingRules.IsCandidate(p.Text,DocumentProfile.State),
                Size:p.Text.StartsWith("Độc",StringComparison.Ordinal)?14:13,InTable:p.InTable,Page:p.Page)).ToArray();
            var edits=Found(rows);
            if(sample.StartsWith("SoKy",StringComparison.Ordinal))
            {
                assert(edits.Any(i=>i.CanFix && i.Kind==FixKind.AdministrativeNumber) &&
                    edits.Any(i=>i.CanFix && i.Kind==FixKind.AdministrativeDate),"Public DOCX fixture exposes both new editable field types");
                assert(!Found(rows.Select(p=>p with {Text=Apply(p,edits)}).ToArray()).Any(i=>i.CanFix),
                    "Public DOCX fixture has no editable residual after the complete header batch");
            }
            else assert(edits.Length==0 && !Checker.Check(rows).Any(i=>i.Kind==FixKind.Text),
                "Public blank-field DOCX fixture retains all intentional number/day spaces");
            assert(File.ReadAllBytes(path).SequenceEqual(original),"Inspecting the public header fixture never writes over its source");
        }
    }
}
