using System.Text;
using ChinhTa.Core;

public static class RepeatedCueReferenceChecks
{
    private static Paragraph[] Rows(params string[] texts)
    {
        int cp=193;
        return texts.Select(text=>
        {var row=new Paragraph(cp,text+"\r",Outline:new(OutlineScope.PlainBody));cp+=row.Text.Length;return row;}).ToArray();
    }
    private static Paragraph[] Flat(string reference)=>Rows("1. Nội dung đầu","2. Nội dung giữa","9. Nội dung cuối",reference);
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
        foreach(bool reverseCanonical in new[]{false,true})
        {
            string first=cue.Normalize(reverseCanonical?NormalizationForm.FormD:NormalizationForm.FormC);
            string second=cue.ToUpperInvariant().Normalize(reverseCanonical?NormalizationForm.FormC:NormalizationForm.FormD);
            string reference="😀 Xem từ "+first+" 4 "+joiner+" "+second+" 10.";
            assert(Manual(Flat(reference)),
                "An explicit interval retains its interior old label when the same known cue repeats with mixed NFC/NFD or case");
        }
        foreach(string reference in new[]{
            "Xem các mục (4) tới mục (10).",
            "Xem từ mục số 0004 đến mục số 00010.",
            "Xem mục số\u00a0(\u202f4\t)\u00a0đến\tMỤC\u202fsố\u00a0(0010).",
            "Xem mục 4 đến mục 6, 8 đến mục 10.",
            "Xem mục 10 tới mục 4.",
            "Xem mục 4 tới mục "+new string('9',600)+".",
            "Xem mục (4), mục (9)."})
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            // A direct list target or either bounded interval owns 9; the
            // repeated cue itself grants no new permission to change text.
            var rows=Flat(reference);
            assert(Manual(rows,profile),
                "Repeated same-cue ranges/lists retain exact grouping, optional number words and arbitrary-width numeric endpoints in every profile");
        }

        foreach(string reference in new[]{
            "Xem từ mục 1.4 đến mục 1.10.",
            "Xem các mục (01.004) tới MỤC số (001.010).",
            "Xem mục 1.0 tới mục 1.3.",
            "Xem mục 1.4 đến mục 1.8.1."})
            assert(Manual(Rows("1. Mục cha","1.1. Nhánh đầu","1.9. Nhánh sau",reference)),
                "A repeated-cue interval protects an old/proposed hierarchical leaf or conservatively retains its uncertain shared subtree");
        foreach(string reference in new[]{"Xem mục 1.1 đến mục 1.5.","Xem mục (8), mục (1.4)."})
        {
            var rows=Rows("1. Mục cha","1.1. Nhánh đầu","1.4. Nhánh sau","2. Mục cha tiếp",
                "2.1. Nhánh đầu","2.4. Nhánh sau",reference);
            var found=Findings(rows);var permitted=found.Where(i=>i.CanFix).ToArray();
            assert(permitted.Length==1 && permitted[0].OutlineContext?.Label=="2.4" &&
                found.Any(i=>!i.CanFix && i.Original.Contains("1.4.",StringComparison.Ordinal)),
                "A repeated-cue reference freezes only its related hierarchy while another parent keeps its independent leaf correction");
        }
        foreach(string reference in new[]{
            "Xem mục 4 và mục 10.","Xem mục (4), mục (10).",
            "Xem mục 4 đến chương 10.","Xem chương 4 tới mục 10.",
            "Xem mục 4; đến mục 10.","Xem mục 4\ntới mục 10.",
            "Xem mục đích 4 đến mục đích 10.","Xem mục 4 theo hướng dẫn đến mục 10.",
            "Xem mục 4 đến mục tiêu 10.","Xem mục IV đến mục X.",
            "Xem từ mục 10 đến mục 20.","Xem mục 4 và mục 10 đến mục 20."})
        {
            var fix=Findings(Flat(reference)).Single(i=>i.CanFix);
            assert(fix.Original=="9" && fix.Replacement=="3" && fix.Start==Flat(reference)[2].Start,
                "Different cues, separated prose/control boundaries, unrelated lists/ranges and Roman intervals do not invent numeric interval ownership");
        }
        var paragraphBoundary=Rows("1. Nội dung đầu","2. Nội dung giữa","9. Nội dung cuối","Xem từ mục 4 đến","mục 10.");
        assert(Findings(paragraphBoundary).Single(i=>i.CanFix).Replacement=="3",
            "Reference-chain recognition never joins the end of one source paragraph to the beginning of another");

        var original=Flat("");var pending=Findings(original).Single(i=>i.CanFix);
        foreach(string reference in new[]{"Xem từ mục 4 đến mục 10.","Xem mục (8), mục (9).",
            "Xem mục số 004 tới MỤC số 010.".Normalize(NormalizationForm.FormD)})
        {
            var changed=Flat(reference);var current=Checker.Check(changed);
            bool stale=false,dependencies=false;
            try{CurrentTextPlan.Validate([pending],current);}catch(InvalidOperationException){stale=true;}
            try{RepairDependencies.Expand([pending],current);}catch(InvalidOperationException){dependencies=true;}
            assert(stale && dependencies && changed[2].Start==pending.Start && changed[2].Text[..pending.Length]==pending.Original,
                "A newly added repeated-cue range/list invalidates an old selection at unchanged label bytes and Word CPs before mutation");
        }
        var positive=Flat("😀 Xem mục (4), MỤC số (10).".Normalize(NormalizationForm.FormD));
        var approved=Findings(positive).Single(i=>i.CanFix);string story=string.Concat(positive.Select(p=>p.Text));
        string expected=AtomicTextBatch.ExpectedStory([approved],story,i=>story[..(i.Start-positive[0].Start)]);
        assert(expected==story.Replace("9. Nội dung cuối","3. Nội dung cuối",StringComparison.Ordinal) &&
            expected.EndsWith(positive[^1].Text,StringComparison.Ordinal),
            "The independent approved write changes only its exact label and preserves complete raw decomposed reference text, emoji and paragraph endings");
    }
}
