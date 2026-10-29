using System.Text;
using System.Xml.Linq;
using ChinhTa.Core;

public static class RomanReferenceRangeChecks
{
    private static Paragraph[] Rows(params string[] texts)
    {
        int cp=239;
        return texts.Select(text=>{var row=new Paragraph(cp,text+"\r",Outline:new(OutlineScope.PlainBody));cp+=row.Text.Length;return row;}).ToArray();
    }
    private static Paragraph[] Roman(string reference,string last="IX")=>Rows("I. Nội dung đầu","II. Nội dung giữa",last+". Nội dung cuối",reference);
    private static Issue[] Findings(Paragraph[] rows,DocumentProfile profile=DocumentProfile.State)=>Checker.Check(rows,profile)
        .Where(i=>i.Kind==FixKind.OutlineOrder).ToArray();
    private static bool Manual(Paragraph[] rows,DocumentProfile profile=DocumentProfile.State)
    {
        var found=Findings(rows,profile);
        return found.Length>0 && found.All(i=>!i.CanFix);
    }

    public static void Run(Action<bool,string> assert)
    {
        foreach(string cue in new[]{"mục","điểm","khoản","phần","chương"})
        foreach(string joiner in new[]{"đến","tới","-","–","—"})
        foreach(var form in new[]{NormalizationForm.FormC,NormalizationForm.FormD})
        {
            string reference=("😀 Xem từ "+cue+" IV "+joiner+" "+cue.ToUpperInvariant()+" X.").Normalize(form);
            assert(Manual(Roman(reference)),
                "An interior Roman old label cannot be renumbered when neither endpoint of its explicit same-cue interval belongs to its group");
        }
        foreach(string reference in new[]{
            "Xem mục IV đến X.","Xem mục X tới IV.",
            "Xem mục (IV) – (X).","Xem mục số\u00a0(\u202fIV\t)\u00a0đến\tMỤC\u202fsố\u00a0(X).",
            "Xem mục iv tới mục x.","Xem mục Iv đến mục x.",
            "Xem mục IV đến VI, VIII đến X.","Xem mục IV đến mục VI và mục VIII tới mục X."})
        foreach(var profile in Enum.GetValues<DocumentProfile>())
            assert(Manual(Roman(reference),profile),
                "Canonical Roman reference values retain reversed bounds, parentheses, horizontal Unicode spacing, case and chained intervals in every profile");
        foreach(var sample in new[]{(Last:"XXIX",Reference:"Xem mục XX tới mục XXX."),
            (Last:"MM",Reference:"Xem mục M đến mục MMMCMXCIX.")})
            assert(Manual(Roman(sample.Reference,sample.Last)),
                "Canonical subtractive and upper-bound Roman labels are compared as Roman values without changing source spelling");

        foreach(string reference in new[]{
            "Xem mục IV và X.","Xem mục (IV), mục (X).",
            "Xem mục X tới mục XX.","Xem mục IV đến chương X.",
            "Xem mục IV; đến mục X.","Xem mục IV\nđến mục X.",
            "Xem mục IV theo hướng dẫn đến mục X.","Xem mục IV qua mục X.",
            "Xem mục 4 đến mục 10.","Xem mục IV đến mục 10.",
            "Xem mục 4 tới mục X.","Xem mục IIV tới mục VX."})
        {
            var rows=Roman(reference);var fix=Findings(rows).Single(i=>i.CanFix);
            assert(fix.Original=="IX" && fix.Replacement=="III" && fix.Start==rows[2].Start,
                "Lists, unrelated intervals, different cues, source boundaries and noncanonical/mixed endpoint families do not invent a Roman interior interval: "+reference);
        }
        var paragraphBoundary=Rows("I. Nội dung đầu","II. Nội dung giữa","IX. Nội dung cuối","Xem mục IV đến","mục X.");
        assert(Findings(paragraphBoundary).Single(i=>i.CanFix).Replacement=="III",
            "Roman interval recognition never joins separate source paragraphs");
        foreach(string reference in new[]{"Xem mục IV đến X.","Xem mục IV đến mục X.","Xem mục iv tới mục x."})
        {
            var arabic=Rows("1. Nội dung đầu","2. Nội dung giữa","9. Nội dung cuối",reference);
            var letters=Rows("a) Nội dung đầu","b) Nội dung giữa","v) Nội dung cuối",reference);
            assert(Findings(arabic).Single(i=>i.CanFix).Replacement=="3" && Findings(letters).Single(i=>i.CanFix).Replacement=="c",
                "Roman interval interiors cannot be transferred to equivalent Arabic values or Roman-looking letter labels");
        }
        var independent=Rows("I. Mục cha","1. Nhánh đầu","2. Nhánh giữa","9. Nhánh sau",
            "II. Mục cha tiếp","IX. Mục cha cuối","Xem từ mục IV đến mục X.");
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            var found=Findings(independent,profile);var permitted=found.Where(i=>i.CanFix).ToArray();
            assert(permitted is [{Original:"9",Replacement:"3"}] && permitted[0].Start==independent[3].Start &&
                found.Any(i=>!i.CanFix && i.Original.StartsWith("IX.",StringComparison.Ordinal)),
                "An affected Roman sibling group remains manual while its independent Arabic child group retains an exact-span repair");
        }

        var original=Roman("");var pending=Findings(original).Single(i=>i.CanFix);
        foreach(string reference in new[]{"Xem mục IV tới X.","Xem từ mục (IV) đến MỤC số (X).".Normalize(NormalizationForm.FormD)})
        {
            var changed=Roman(reference);var current=Checker.Check(changed);bool stale=false,dependencies=false;
            try{CurrentTextPlan.Validate([pending],current);}catch(InvalidOperationException){stale=true;}
            try{RepairDependencies.Expand([pending],current);}catch(InvalidOperationException){dependencies=true;}
            assert(stale && dependencies && changed[2].Start==pending.Start && changed[2].Text[..pending.Length]==pending.Original,
                "A new Roman interior reference invalidates a stale renumbering selection at unchanged label bytes and native positions");
        }
        string story=string.Concat(independent.Select(p=>p.Text));var approved=Findings(independent).Single(i=>i.CanFix);
        assert(AtomicTextBatch.ExpectedStory([approved],story,i=>story[..(i.Start-independent[0].Start)])==
            story.Replace("9. Nhánh sau","3. Nhánh sau",StringComparison.Ordinal),
            "The permitted independent write preserves the referenced Roman labels, reference prose and paragraph terminators exactly");
        ProofChecks(assert);
    }

    private static void ProofChecks(Action<bool,string> assert)
    {
        var rows=Rows("I. Mục cha","1. Nhánh đầu","2. Nhánh giữa","9. Nhánh sau",
            "II. Mục cha tiếp","IX. Mục cha cuối","😀 Xem từ mu\u0323c\u00a0(IV) đến MỤC số (X).");
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        string xml=new XDocument(new XElement(w+"document",new XElement(w+"body",rows.Select(row=>
            new XElement(w+"p",new XElement(w+"r",new XElement(w+"t",new XAttribute(XNamespace.Xml+"space","preserve"),row.Text.TrimEnd('\r')))))))).ToString();
        var native=rows.Select(row=>new WordParagraphSnapshot(row.Start,row.Start+row.Text.Length,row.Text,1,false,false)).ToArray();
        foreach(bool cached in new[]{false,true})
        {
            var outline=new Dictionary<int,OutlineParagraphInfo>();
            var editable=cached?WordTextProof.Read(WordXmlSnapshot.Parse(xml),native,onOutline:(cp,value)=>outline[cp]=value):
                WordTextProof.Read(xml,native,onOutline:(cp,value)=>outline[cp]=value);
            var proven=WordTextProof.Apply(rows,editable,outline);
            assert(editable.SetEquals(rows.Select(row=>row.Start)) && outline.Values.All(value=>value.Scope==OutlineScope.PlainBody),
                "The Roman interval fixture uses actual source/native XML proof and ordinary body evidence rather than a blanket complex-structure exclusion");
            foreach(var profile in Enum.GetValues<DocumentProfile>())
            {
                var current=Checker.Check(proven,profile);var selected=current.Where(i=>i.CanFix && i.Kind==FixKind.OutlineOrder).ToArray();
                assert(selected is [{Original:"9",Replacement:"3"}] && selected[0].Start==rows[3].Start &&
                    current.Any(i=>!i.CanFix && i.Kind==FixKind.OutlineOrder && i.Original.StartsWith("IX.",StringComparison.Ordinal)),
                    "Both production XML proof paths preserve raw decomposed reference text and freeze only the Roman group in every profile");
            }
        }
    }
}
