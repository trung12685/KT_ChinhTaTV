using ChinhTa.Core;

public static class DocumentHeadingChecks
{
    private static (Paragraph[] P,LayoutParagraph[] L) Sample(string title="BÁO CÁO",int subjectCount=2,bool body=true)
    {
        string[] text=["CƠ QUAN MẪU","Số:        /BC-ABC",Checker.Heading,HeadingRules.Motto,
            "Địa danh, ngày        tháng 10 năm 2026","",title,
            ..Enumerable.Range(0,subjectCount).Select(i=>i==0?"Tổng hợp kết quả mẫu":"đợt công tác năm 2026"),
            ..body?new[]{"Kính gửi: đơn vị mẫu.","Nội dung  giữ nguyên."}:Array.Empty<string>()];
        int start=0;
        var p=text.Select((t,i)=>{var row=new Paragraph(start,t+"\r",Bold:i<5,Size:i==3?14:i==6?16:i==7?14:13.5f,
            Font:i>=6 && i<7+subjectCount?"Arial":"Times New Roman",Italic:i is 6 or 7,Color:i>=6 && i<7+subjectCount?255:0,
            InTable:i<5,Page:1,FormatObserved:true);start+=row.Text.Length;return row;}).ToArray();
        var l=p.Select((row,i)=>new LayoutParagraph(row.Start,row.Text,1,i<5,i<2?p[0].Start:i<5?p[2].Start:0,
            i<2?p[2].Start:i<5?p[5].Start:0,i<5 || i>=6 && i<7+subjectCount,i<5?0:-1,
            i<5 || i>=6 && i<7+subjectCount)).ToArray();return (p,l);
    }
    public static void Run(Action<bool,string> assert)
    {
        var (p,l)=Sample();
        IReadOnlyList<Issue> Check(Paragraph[]? source=null,LayoutParagraph[]? layout=null,DocumentProfile profile=DocumentProfile.State)=>
            DocumentHeadingRules.Check(source??p,layout??l,profile);
        var fixes=Check().Where(i=>i.CanFix).ToArray();
        assert(fixes.Length==3 && fixes.All(i=>i.Kind==FixKind.DocumentHeadingFormat && i.Original==i.Replacement && i.CharacterFormat!.Bold==true),
            "The verified front title and two subject paragraphs offer three character-only bold upright fixes");
        assert(fixes.Select(i=>i.CharacterFormat!.Size).SequenceEqual(new[]{13f,14f,13.5f}),
            "Title/subject repairs retain valid 13–14pt sizes and choose 13pt only when size is outside the range");
        assert(fixes.All(i=>!i.Original.Contains('\r') && !i.Original.Contains('\a') && i.Suggestion.Contains("đậm")),
            "Suggestions exclude paragraph markers, preserve original text and explicitly describe bold type");
        var styled=p.Select(row=>fixes.SingleOrDefault(i=>i.Start==row.Start)?.CharacterFormat is CharacterFormat style?
            row with {Font="Times New Roman",Size=style.Size,Bold=true,Italic=false,Color=0}:row).ToArray();
        assert(Check(styled).Count==0,"A fresh check removes corrected title/subject typography instead of retaining a disabled stale warning");
        foreach(var unsafeRow in new[]{p[7] with {FormatMixed=true},p[7] with {BoldMixed=true},p[7] with {TextEditable=false},
            p[7] with {FormatObserved=false},p[7] with {Font=""},p[7] with {Size=float.NaN}})
        {
            var rows=p.ToArray();rows[7]=unsafeRow;
            var found=Check(rows);
            assert(found.Any(i=>i.Start==unsafeRow.Start && !i.CanFix) && !found.Any(i=>i.Start==unsafeRow.Start && i.CanFix),
                "Mixed, structured or unobserved subject typography remains explicitly manual without blocking independently proven rows");
        }
        foreach(var sample in new[]{Sample("Báo cáo"),Sample("BÁO CÁO",5),Sample(body:false),Sample(subjectCount:0)})
        {
            var found=DocumentHeadingRules.Check(sample.P,sample.L,DocumentProfile.State);
            if(sample.P[6].Text.StartsWith("Báo",StringComparison.Ordinal))
                assert(found.Any(i=>!i.CanFix && i.Message.Contains("viết hoa")) && found.Where(i=>i.CanFix).All(i=>i.Original==i.Replacement),
                    "A lowercase recognized document title is reviewed manually without automatic text capitalization");
            else assert(found.Count>0 && found.All(i=>!i.CanFix),"Unbounded, absent or over-limit subject blocks cannot acquire automatic formatting");
        }
        foreach(var layout in new[]{Array.Empty<LayoutParagraph>(),l.Where(r=>r.Start!=p[9].Start && r.Start!=p[10].Start).ToArray(),
            l.Select((r,i)=>i==6?r with {Text="old title\r"}:r).ToArray(),
            l.Select((r,i)=>i==6?r with {Centered=false}:r).ToArray(),
            l.Select((r,i)=>i==8?r with {Centered=false}:r).ToArray(),
            l.Select((r,i)=>i==9?r with {Centered=true,ParagraphCentered=true}:r).ToArray()})
            assert(Check(layout:layout).All(i=>!i.CanFix),"Missing XML mappings, indents or ambiguous centered body text prevent automatic subject formatting");
        var bodyBefore=p.Take(6).Concat([new Paragraph(p[6].Start-1,"Đoạn mở đầu không thuộc đầu văn bản.\r")]).Concat(p.Skip(6)).ToArray();
        assert(Check(bodyBefore).Count==0,"An exact document-type phrase after ordinary body prose does not become a front title");
        var duplicate=p.Concat([new Paragraph(p[^1].Start+100,"THÔNG BÁO\r",Page:1)]).ToArray();
        assert(Check(duplicate).All(i=>!i.CanFix),"Duplicate named-type candidates do not choose a title arbitrarily");
        foreach(var profile in new[]{DocumentProfile.Party,DocumentProfile.Personal})
            assert(Check(profile:profile).Count==0,"Title/subject rules stay within the State profile");
        assert(Check(p.Select(r=>r with {Page=2}).ToArray()).Count==0 &&
            Check(p.Where(r=>r.Start!=p[3].Start).ToArray()).Count==0,"Later pages or an absent national/motto pair do not acquire automatic front-title roles");
        var allCaps=p.ToArray();allCaps[7]=allCaps[7] with {Text=allCaps[7].Text.ToUpperInvariant()};
        var capsLayout=l.Select((r,i)=>i==7?r with {Text=allCaps[7].Text}:r).ToArray();
        assert(Check(allCaps,capsLayout).Any(i=>!i.CanFix && i.Message.Contains("toàn chữ hoa")),
            "All-caps subjects receive a manual case review instead of rewriting proper names or identifiers");
        var bodyMarker=p.ToArray();bodyMarker[7]=bodyMarker[7] with {Text="Căn cứ văn bản mẫu.\r"};
        assert(Check(bodyMarker,l.Select((r,i)=>i==7?r with {Text=bodyMarker[7].Text}:r).ToArray()).All(i=>!i.CanFix),
            "Centered legal-basis/body markers cannot be formatted as a subject block");
        var investigation=p.ToArray();investigation[7]=investigation[7] with {Text="Điều tra và tổng hợp kết quả mẫu.\r"};
        assert(Check(investigation,l.Select((r,i)=>i==7?r with {Text=investigation[7].Text}:r).ToArray()).Count(i=>i.CanFix)==3,
            "The ordinary subject phrase Điều tra is not mistaken for an Điều 1 body heading");
        assert(new CharacterFormat(14,false,true).Matches("TIMES NEW ROMAN",14,0,0,-1) &&
            !new CharacterFormat(14,false,true).Matches("Times New Roman",14,0,0,0) &&
            !new CharacterFormat(14,false,true).Matches("Times New Roman",14,0,0,9999999) &&
            new CharacterFormat(13,false).Matches("Times New Roman",13,0,0,9999999),
            "Post-save matching requires actual bold for new title styles while preserving legacy number/date bold behavior");
        bool Reject(Issue item){try{BatchPlan.Create([item]);return false;}catch(InvalidOperationException){return true;}}
        assert(Reject(fixes[0] with {CharacterFormat=null}) && Reject(fixes[0] with {CharacterFormat=new(13,false)}) &&
            Reject(fixes[0] with {CharacterFormat=new(13,true,true)}) && Reject(fixes[0] with {Replacement="changed"}) &&
            Reject(fixes[0] with {Kind=FixKind.AdministrativeFormat}),"Malformed or cross-role title-format payloads fail before mutation");
        var format=fixes[1];int offset=format.Original.IndexOf(' ');
        var textEdit=new Issue(format.Start+offset,1," ","  ","synthetic selected text edit",false);
        assert(BatchPlan.Create([format,textEdit]).First().CharacterFormat is not null,
            "An explicitly selected contained text edit can share the title-style transaction and follows formatting");
        string story=string.Concat(p.Select(r=>r.Text));int applied=0,restored=0;bool rejected=false;
        try{AtomicTextBatch.Apply(fixes,i=>i.Original,i=>{applied++;return i.Replacement;},()=>
        {
            if(!fixes[0].CharacterFormat!.Matches("Times New Roman",13,0,0,0))throw new InvalidOperationException("bold setter did not take effect");
        },()=>restored++);}catch(InvalidOperationException){rejected=true;}
        assert(rejected && applied==3 && restored==1 && AtomicTextBatch.ExpectedStory(fixes,story,i=>story[..i.Start])==story,
            "A failed actual-bold post-check rolls back the whole format batch while format-only expected text remains byte-for-byte identical");
    }
}
