using System.Xml.Linq;
using ChinhTa.Core;

public static class StructuredTextChecks
{
    public static void Run(Action<bool,string> assert)
    {
        const string text="Về con đo và kết quả  thử nghiệm";
        var plain=new Paragraph(90,text+"\r",FormatObserved:true,Font:"Arial",Bold:false);
        var edits=Checker.Check([plain]).Where(i=>i.CanFix).ToArray();
        assert(edits.Length==2,"Plain subject text still offers the spacing and demonstration spelling choices");
        var unsafeRow=plain with {TextEditable=false};
        var guarded=Checker.Check([unsafeRow]);
        assert(guarded.Count==1 && !guarded[0].CanFix && guarded[0].Start==90 && guarded[0].Original==text &&
            guarded[0].Replacement==text && guarded[0].Format is null,
            "Structured subject cannot bypass a manual format gate via spacing or spelling; one whole-row manual notice uses the actual start");
        var neighbor=new Paragraph(200,"Nội dung  bình thường.\r");
        assert(Checker.Check([unsafeRow,neighbor]).Count(i=>i.CanFix)==1 && Checker.Check([unsafeRow,neighbor]).Single(i=>i.CanFix).Start>200,
            "A structured row does not prevent a separate plain neighboring row from being repaired");
        assert(Checker.Check([plain with {FormatMixed=true,BoldMixed=true}]).Count(i=>i.CanFix)==2,
            "Mixed character styles alone do not remove independently proven plain-text choices");
        foreach(var profile in Enum.GetValues<DocumentProfile>())
            assert(Checker.Check([unsafeRow],profile).All(i=>!i.CanFix),"TextEditable is respected by generic rules in every profile");
        var heading=new Paragraph(0,Checker.Heading.ToLowerInvariant()+"\r",Bold:false,Size:16,Page:1,TextEditable:false);
        assert(HeadingRules.Check(heading,DocumentProfile.State).Any(i=>!i.CanFix) &&
            Checker.Check([heading]).All(i=>!i.CanFix && i.Format is null && i.Original==i.Replacement),
            "Structured national headings cannot receive a selectable text or paragraph-format mutation");
        var blank=new Paragraph(0,"Số:        /BC-ABC\r",TextEditable:false);
        var date=new Paragraph(60,"Địa danh, ngày        tháng 10 năm 2026\r",TextEditable:false);
        assert(!Checker.Check([blank,date]).Any(i=>i.Kind==FixKind.Text || i.Title=="Kiểm tra chữ trong vùng chưa cho phép sửa"),
            "Unfilled number and date placeholders do not become generic excess-space warnings");

        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XElement Run(string value)=>new(w+"r",new XElement(w+"t",value));
        foreach(string kind in new[]{"hyperlink","sdt","fldSimple","drawing","object","pict","smartTag"})
        {
            XElement row=kind switch {
                "sdt"=>new(w+"sdt",new XElement(w+"sdtContent",new XElement(w+"p",Run(text)))),
                "drawing" or "object" or "pict"=>new(w+"p",Run(text),new XElement(w+"r",new XElement(w+kind))),
                _=>new(w+"p",new XElement(w+kind,Run(text))) };
            string xml=new XDocument(new XElement(w+"document",new XElement(w+"body",row,new XElement(w+"p",Run(neighbor.Text.TrimEnd('\r')))))).ToString();
            var editable=new HashSet<int>();
            var layout=WordXmlTableMap.Read(xml,[new(90,90+plain.Text.Length,plain.Text,1,false,true),
                new(200,200+neighbor.Text.Length,neighbor.Text,1,false,false)],onTextEditable:(start,canEdit)=>{if(canEdit)editable.Add(start);});
            var found=Checker.Check([plain with {TextEditable=editable.Contains(90)},neighbor with {TextEditable=editable.Contains(200)}]);
            assert(layout.Count==2 && !editable.Contains(90) && editable.Contains(200) &&
                !found.Any(i=>i.CanFix && i.Start>=90 && i.Start<90+plain.Text.Length) && found.Any(i=>i.CanFix && i.Start>=200),
                "XML "+kind+" retains a manual row while independently matched plain text stays editable");
        }
        string fieldXml=new XDocument(new XElement(w+"document",new XElement(w+"body",
            new XElement(w+"p",new XElement(w+"r",new XElement(w+"fldChar",new XAttribute(w+"fldCharType","begin"))),Run(text)),
            new XElement(w+"p",Run(neighbor.Text.TrimEnd('\r')))))).ToString();
        var proven=new HashSet<int>();bool mismatch=false;
        WordXmlTableMap.Read(fieldXml,[new(90,130,plain.Text,1,false,true),new(200,230,neighbor.Text,1,false,false)],
            _=>mismatch=true,onTextEditable:(start,canEdit)=>{if(canEdit)proven.Add(start);});
        assert(mismatch && proven.Count==0,"Complex fields stop prefix proof; unchanged visible text cannot establish editable CP offsets");

        bool Reject(IReadOnlyList<Issue> selected,IEnumerable<Issue> current)
        {try{CurrentTextPlan.Validate(selected,current);return false;}catch(InvalidOperationException){return true;}}
        CurrentTextPlan.Validate(edits,edits.Select(i=>i with {Title="new display label",Message="new explanation",Citation="new citation"}));
        assert(true,"Fresh plan matching ignores presentation text while retaining mutation targets");
        assert(Reject(edits,guarded),"Same visible paragraph now marked manual invalidates every old text choice");
        foreach(var changed in new[]{edits[0] with {Start=edits[0].Start+1},edits[0] with {Length=3},
            edits[0] with {Original="old"},edits[0] with {Replacement="changed"},edits[0] with {Kind=FixKind.AdministrativeNumber},
            edits[0] with {CanFix=false},edits[0] with {Format=new(13)}})
            assert(Reject([edits[0]],[changed]),"Changed position, source, replacement, kind, editability or heading payload invalidates a stale choice");
        assert(Reject([edits[0]],[]) && Reject([edits[0]],[edits[0],edits[0] with {Title="duplicate"}]),
            "Missing and ambiguous current proposals cannot authorize a write");
        var style=new Issue(90,text.Length,text,text,"format",false,FixKind.DocumentHeadingFormat,CharacterFormat:new(13,false,true));
        assert(Reject([style],[style with {CharacterFormat=new(14,false,true)}]),"A changed proposed font size rejects an old document-style choice");
        assert(Reject([edits[0]],[edits[0] with {CenterFix=new(AdministrativePositionRole.Number)}]),
            "Geometry operations cannot stand in for a fresh text proposal");
        int writes=0,saves=0,restores=0;
        bool preflightRejected=false;
        try
        {
            // The last selected choice has become manual; reject the whole batch before
            // checkpoint/apply, even though an earlier choice remains independently valid.
            CurrentTextPlan.Validate(edits,[edits[0],edits[1] with {CanFix=false}]);
            saves++;
            AtomicTextBatch.Apply(edits,i=>i.Original,i=>{writes++;return i.Replacement;},()=>saves++,()=>restores++);
        }
        catch(InvalidOperationException){preflightRejected=true;}
        assert(preflightRejected && writes==0 && saves==0 && restores==0,
            "One stale member rejects the entire selected group before writes, checkpoint, rollback or a partial success");
    }
}
