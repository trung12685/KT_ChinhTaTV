using System.Xml.Linq;
using ChinhTa.Core;

public static class WholeBodyTextProofChecks
{
    public static void Run(Action<bool,string> assert)
    {
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XElement Run(string text)=>new(w+"r",new XElement(w+"t",text));
        XElement Row(string text)=>new(w+"p",Run(text));
        const int count=65,late=45;
        var paragraphs=Enumerable.Range(0,count).Select(i=>new Paragraph(100+i*100,$"Nội dung đoạn {i} đã  duyệt.\r",Page:2)).ToArray();
        var live=paragraphs.Select(p=>new WordParagraphSnapshot(p.Start,p.Start+p.Text.Length,p.Text,0,false,false)).ToArray();
        XElement[] Plain()=>paragraphs.Select(p=>Row(p.Text.TrimEnd('\r'))).ToArray();
        string Xml(IEnumerable<XElement> rows)=>new XDocument(new XElement(w+"document",new XElement(w+"body",rows))).ToString();
        string plainXml=Xml(Plain());
        var all=WordTextProof.Read(plainXml,live);
        assert(all.Count==count && all.Contains(paragraphs[^1].Start),"Whole-body proof includes matching body paragraphs after index 40 without requesting layout observations");
        var flags=new HashSet<int>();
        var noLayout=WordXmlTableMap.Read(plainXml,live,onTextEditable:(start,canEdit)=>{if(canEdit)flags.Add(start);},includeLayout:false);
        assert(noLayout.Count==0 && flags.SetEquals(all),"Text-only map mode preserves prefix/editability callbacks while returning no geometry");
        var legacy=WordXmlTableMap.Read(plainXml,live.Take(40).ToArray());
        assert(legacy.Count==40 && legacy.All(p=>!p.Centered),"The existing 40-row geometry call retains its bounded output and supplied layout flags");
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            var found=Checker.Check(WordTextProof.Apply(paragraphs,all),profile);
            assert(found.Count(i=>i.CanFix)==count && found.Last(i=>i.CanFix).Start>paragraphs[40].Start,
                "All profiles still repair independently proven plain spacing beyond the former header limit");
            var closed=Checker.Check(WordTextProof.Apply(paragraphs,new HashSet<int>()),profile);
            assert(closed.Count==count && closed.All(i=>!i.CanFix),"An unavailable/empty proof fails closed for every paragraph in every profile");
        }
        foreach(string kind in new[]{"hyperlink","sdt","fldSimple","drawing","pict","object","smartTag"})
        {
            var rows=Plain();string text=paragraphs[late].Text.TrimEnd('\r');
            rows[late]=kind switch {
                "sdt"=>new(w+"sdt",new XElement(w+"sdtContent",Row(text))),
                "drawing" or "pict" or "object"=>new(w+"p",Run(text),new XElement(w+"r",new XElement(w+kind))),
                _=>new(w+"p",new XElement(w+kind,Run(text))) };
            var proven=WordTextProof.Read(Xml(rows),live);
            foreach(var profile in Enum.GetValues<DocumentProfile>())
            {
                var found=Checker.Check(WordTextProof.Apply(paragraphs,proven),profile);
                assert(proven.Count==count-1 && !proven.Contains(paragraphs[late].Start) && proven.Contains(paragraphs[late+1].Start) &&
                    found.Single(i=>i.Start>=paragraphs[late].Start && i.Start<paragraphs[late].Start+paragraphs[late].Text.Length).CanFix==false &&
                    found.Count(i=>i.CanFix)==count-1,
                    "A late "+kind+" stays manual while separately matched neighboring body text remains editable in "+profile);
            }
        }
        foreach(string kind in new[]{"fldChar","instrText","ins","del","footnoteReference"})
        {
            var rows=Plain();rows[late].AddFirst(new XElement(w+"r",new XElement(w+kind)));
            WordXmlMappingException? warning=null;
            var proven=WordTextProof.Read(Xml(rows),live,e=>warning=e);
            assert(warning?.DiagnosticCode.StartsWith("XML-CONTENT.P46.")==true && proven.Count==late &&
                !proven.Contains(paragraphs[late].Start) && !proven.Contains(paragraphs[^1].Start),
                "A late complex field/revision/reference stops proof without guessing later correspondence: "+kind);
            foreach(var profile in Enum.GetValues<DocumentProfile>())
                assert(Checker.Check(WordTextProof.Apply(paragraphs,proven),profile).Count(i=>i.CanFix)==late,
                    "Only the proven prefix remains writable after a complex boundary in "+profile);
        }
        var mismatchRows=Plain();mismatchRows[late]=Row("Văn bản khác");
        WordXmlMappingException? mismatch=null;
        var prefix=WordTextProof.Read(Xml(mismatchRows),live,e=>mismatch=e);
        assert(prefix.Count==late && mismatch?.DiagnosticCode.StartsWith("XML-TEXT.P46.")==true &&
            !prefix.Contains(paragraphs[late+1].Start),"A text mismatch after paragraph 40 cannot authorize later repeated or matching visible paragraphs");
        var originalChoices=Checker.Check(WordTextProof.Apply(paragraphs,all)).Where(i=>i.CanFix).ToArray();
        var now=Checker.Check(WordTextProof.Apply(paragraphs,prefix));bool stale=false;
        try{CurrentTextPlan.Validate(originalChoices,now);}catch(InvalidOperationException){stale=true;}
        assert(stale,"One newly unproven late member invalidates a previously selectable whole-body batch before writes");
        var explicitlyBlocked=paragraphs.Select((p,i)=>i==late?p with {TextEditable=false}:p).ToArray();
        assert(!WordTextProof.Apply(explicitlyBlocked,all)[late].TextEditable,"Applying new proof never re-enables a row already blocked by another gate");
        foreach(var invalid in new[]{live.Select((p,i)=>i==late?p with {Start=-1}:p).ToArray(),
            live.Select((p,i)=>i==late?p with {End=p.Start}:p).ToArray(),
            live.Select((p,i)=>i==late?p with {Start=live[late-1].Start}:p).ToArray(),
            live.Select((p,i)=>i==late?p with {Start=live[late-1].End-1}:p).ToArray()})
        {
            bool rejected=false;
            try{WordTextProof.Read(plainXml,invalid);}catch(InvalidOperationException){rejected=true;}
            assert(rejected,"Invalid, duplicate or overlapping live ranges cannot establish a text proof");
        }
        bool malformed=false;
        try{WordTextProof.Read("<broken",live);}catch(System.Xml.XmlException){malformed=true;}
        assert(malformed,"Malformed current XML is reported to the caller rather than silently enabling rows");
        WordXmlMappingException? countWarning=null;
        var missing=WordTextProof.Read(Xml(Plain().Take(late)),live,e=>countWarning=e);
        assert(missing.Count==late && countWarning?.DiagnosticCode.StartsWith("XML-COUNT.")==true,
            "Missing XML rows cannot enable unmatched live paragraphs");
        // This is a synthetic scale/offset case, not a measurement of Word COM latency.
        var large=Enumerable.Range(0,1200).Select(i=>new WordParagraphSnapshot(100+i*80,120+i*80,"Nội dung thử.\r",0,false,false)).ToArray();
        assert(WordTextProof.Read(Xml(large.Select(p=>Row(p.Text.TrimEnd('\r')))),large).Count==large.Length,
            "Text-only proof handles a long matching body without a 40-row cap or invented page geometry");
    }
}
