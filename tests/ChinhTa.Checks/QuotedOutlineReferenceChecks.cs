using System.Text;
using System.Xml.Linq;
using ChinhTa.Core;

public static class QuotedOutlineReferenceChecks
{
    private static Paragraph[] Rows(params string[] texts)
    {
        int cp=239;
        return texts.Select(text=>{var row=new Paragraph(cp,text+"\r",Outline:new(OutlineScope.PlainBody));cp+=row.Text.Length;return row;}).ToArray();
    }
    private static Paragraph[] Numbered(string reference)=>Rows("1. Nội dung đầu","2. Nội dung giữa","4. Nội dung cuối",reference);
    private static Issue[] Orders(Paragraph[] rows,DocumentProfile profile=DocumentProfile.State)=>
        Checker.Check(rows,profile).Where(i=>i.Kind==FixKind.OutlineOrder).ToArray();
    private static bool Manual(Paragraph[] rows,DocumentProfile profile=DocumentProfile.State)
    {var orders=Orders(rows,profile);return orders.Length>0 && orders.All(i=>!i.CanFix);}

    public static void Run(Action<bool,string> assert)
    {
        foreach(var pair in new[]{("“","”"),("\"","\""),("‘","’"),("'","'"),("«","»"),("`","`"),("[","]"),("{","}"),("(",")")})
        foreach(string cue in new[]{"mục","điểm","khoản","phần","chương"})
        foreach(var form in new[]{NormalizationForm.FormC,NormalizationForm.FormD})
        {
            string reference=("😀 Xem "+pair.Item1+cue+" 4"+pair.Item2+" để đối chiếu.").Normalize(form);
            assert(Manual(Numbered(reference)),
                "A closing quote or bracket cannot make an explicit source reference disappear before automatic renumbering");
        }
        foreach(string reference in new[]{"Xem “mục 3”.","Xem [mục (3)] để đối chiếu.","Xem {mục 3}.","Xem (mục 3)."})
        foreach(var profile in Enum.GetValues<DocumentProfile>())
            assert(Manual(Numbered(reference),profile),
                "A quoted reference to the proposed label also keeps the sibling group manual in every profile");

        foreach(string reference in new[]{
            "Xem “mục 4”", "Xem \"mục 4\", để đối chiếu.","Xem ‘mục (4)’; để đối chiếu.",
            "Xem [“mục (4)”].", "Xem {mục 4.}.","Xem «mục 8, mục 4»."})
            assert(Manual(Numbered(reference)),
                "The complete citation suffix can end the paragraph, precede punctuation or contain nested terminal delimiters without changing source labels");

        foreach(string reference in new[]{
            "Xem “mục 40”.","Xem [mục 8].","Xem «mục 8, mục 9».",
            "Xem mục 4\"suffix.","Xem mục 4”suffix.","Xem mục 4]suffix.","Xem mục 4'000.",
            "Xem mục 4}000.","Xem mục 4x”.","Xem mục 4/5”.", "Xem mục “4”."})
        {
            var rows=Numbered(reference);var permitted=Orders(rows).Where(i=>i.CanFix).ToArray();
            assert(permitted is [{Original:"4",Replacement:"3"}] && permitted[0].Start==rows[2].Start,
                "Unrelated labels, incomplete tokens and unsupported opening delimiters cannot become an invented reference to a numeric prefix: "+reference);
        }

        foreach(string reference in new[]{"Xem “mục 1.3 đến mục 1.5”.","Xem [mục (8), mục (001.004)].","Xem «mục 1.2».","Xem [“mục (1.1) – (1.5)”]."})
        {
            var rows=Rows("1. Mục cha","\t1.1.\tNhánh đầu","\t1.4.\tNhánh sau","2. Mục cha sau","\t2.1.\tNhánh đầu","\t2.4.\tNhánh sau",reference);
            var found=Orders(rows);var permitted=found.Where(i=>i.CanFix).ToArray();
            assert(permitted is [{Original:"4",Replacement:"2"}] && permitted[0].OutlineContext?.Label=="2.4" &&
                found.Any(i=>!i.CanFix && i.Original.Contains("1.4.",StringComparison.Ordinal)),
                "Quoted hierarchical lists and ranges keep their affected branch manual while retaining the unrelated exact-span correction");
        }

        foreach(string reference in new[]{"Xem “mục IV đến mục X”.","Xem [mục (IV) – (X)].","Xem «mục X tới mục IV».","Xem [‘phần iv tới PHẦN x’]."})
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            var rows=Rows("I. Mục cha","1. Nhánh đầu","2. Nhánh giữa","9. Nhánh sau","II. Mục cha tiếp","IX. Mục cha cuối",reference);
            var found=Orders(rows,profile);var permitted=found.Where(i=>i.CanFix).ToArray();
            assert(permitted is [{Original:"9",Replacement:"3"}] && permitted[0].Start==rows[3].Start &&
                found.Any(i=>!i.CanFix && i.Original.StartsWith("IX.",StringComparison.Ordinal)),
                "Quoted Roman interval interiors remain Roman-only while the independent Arabic child group retains its repair in every profile");
            string story=string.Concat(rows.Select(p=>p.Text));
            assert(AtomicTextBatch.ExpectedStory(permitted,story,i=>story[..(i.Start-rows[0].Start)])==
                story.Replace("9. Nhánh sau","3. Nhánh sau",StringComparison.Ordinal),
                "The permitted write keeps quoted reference prose, Roman labels and paragraph terminators unchanged");
        }

        var pending=Orders(Numbered("")).Single(i=>i.CanFix);
        foreach(string reference in new[]{"😀 Xem “mục 4”.","Xem [mu\u0323c\u00a0số\u202f(004)]."})
        {
            var rows=Numbered(reference);var current=Checker.Check(rows);bool stale=false,dependencies=false;
            try{CurrentTextPlan.Validate([pending],current);}catch(InvalidOperationException){stale=true;}
            try{RepairDependencies.Expand([pending],current);}catch(InvalidOperationException){dependencies=true;}
            assert(stale && dependencies && pending.Start==rows[2].Start && rows[2].Text[..pending.Length]==pending.Original,
                "A new quoted reference invalidates an old selection at unchanged native label coordinates and source bytes");
        }
        ProofChecks(assert);
    }

    private static void ProofChecks(Action<bool,string> assert)
    {
        var rows=Rows("I. Mục cha","1. Nhánh đầu","2. Nhánh giữa","9. Nhánh sau","II. Mục cha tiếp","IX. Mục cha cuối",
            "😀 Xem [“mu\u0323c\u00a0số\u202f(iv) tới MỤC số (x)”].");
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
                "Quoted reference fixtures retain real native/XML correspondence rather than losing edit permission through an unrelated structure guard");
            foreach(var profile in Enum.GetValues<DocumentProfile>())
            {
                var orders=Orders(proven.ToArray(),profile);
                assert(orders.Where(i=>i.CanFix).ToArray() is [{Original:"9",Replacement:"3"}] &&
                    orders.Any(i=>!i.CanFix && i.Original.StartsWith("IX.",StringComparison.Ordinal)),
                    "String/snapshot proof paths preserve Unicode reference text and independent repair permissions in every profile");
            }
        }
    }
}
