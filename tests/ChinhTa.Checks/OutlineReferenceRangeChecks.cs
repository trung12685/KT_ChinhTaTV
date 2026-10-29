using System.Text;
using ChinhTa.Core;

public static class OutlineReferenceRangeChecks
{
    private static Paragraph[] Rows(params string[] texts)
    {
        int cp=117;
        return texts.Select(text=>{var row=new Paragraph(cp,text+"\r",Outline:new(OutlineScope.PlainBody));cp+=row.Text.Length;return row;}).ToArray();
    }
    private static Issue[] Findings(Paragraph[] rows,DocumentProfile profile=DocumentProfile.State)=>Checker.Check(rows,profile)
        .Where(i=>i.Kind==FixKind.OutlineOrder).ToArray();
    private static Paragraph[] Flat(string label,string reference)=>Rows("1. Nội dung đầu","2. Nội dung giữa",label+". Nội dung cuối",reference);
    private static bool Manual(Paragraph[] rows,DocumentProfile profile=DocumentProfile.State)
    {
        var findings=Findings(rows,profile);
        return findings.Length>0 && findings.All(i=>!i.CanFix);
    }

    public static void Run(Action<bool,string> assert)
    {
        foreach(string cue in new[]{"mục","điểm","khoản","phần","chương"})
        foreach(string joiner in new[]{"đến","tới","-","–","—"})
        foreach(var form in new[]{NormalizationForm.FormC,NormalizationForm.FormD})
        {
            // Neither endpoint is an existing or proposed sibling label. The
            // interior 9 must remain protected as part of the explicit range.
            string reference=("😀 Xem "+cue+" 4 "+joiner+" 10.").Normalize(form);
            assert(Manual(Flat("9",reference)),
                "An interior old label in an explicit numeric reference range cannot be renumbered just because neither endpoint belongs to its group");
        }
        foreach(var (first,last) in new[]{("8","(4)"),("(8)","4"),("(8)","(4)")})
        foreach(string joiner in new[]{",","và","đến","tới","-","–"})
        foreach(var form in new[]{NormalizationForm.FormC,NormalizationForm.FormD})
            assert(Manual(Flat("4",("Xem mục số "+first+" "+joiner+" "+last+".").Normalize(form))),
                "Each label in a list or range can retain its own parentheses without hiding a later old label from the reference guard");

        foreach(string reference in new[]{"Xem mục 0004 đến 00010.","Xem mục (0004) đến (00010).",
            "Xem mục 10 tới 4.","Xem mục số\u00a0(\u202f4\t)\u00a0–\t(10).",
            "Xem mục 4 đến "+new string('9',600)+".","Xem mục 0004 đến "+new string('0',400)+"10."})
            assert(Manual(Flat("9",reference)),
                "Padded, reversed, Unicode-spaced and arbitrarily wide numeric range endpoints freeze interior labels without integer overflow or source normalization");
        foreach(string reference in new[]{"Xem mục 004 đến 010.","Xem khoản (0004) tới (0010)."})
        {
            var rows=Rows("01. Nội dung đầu","02. Nội dung giữa","09. Nội dung cuối",reference);
            assert(Manual(rows),"The same numeric interval protects a padded source label without changing its spelling or replacement width");
        }

        foreach(string reference in new[]{"Xem mục 1.4 đến 1.10.","Xem mục (01.004) – (001.010).",
            "Xem mục 1.0 tới 1.3.","Xem mục số (1.000) đến (1.003)."})
        {
            var rows=Rows("1. Mục cha","1.1. Nhánh đầu","1.9. Nhánh sau",reference);
            assert(Manual(rows),
                "A sibling interval protects either its old leaf or its proposed leaf even when neither endpoint is a member and only the proposed label lies inside");
        }
        var descendants=Rows("1. Mục cha","1.1. Nhánh đầu","1.9. Nhánh cuối","1.9.1. Ý đầu","1.9.4. Ý sau","Xem mục 1.4 đến 1.10.");
        assert(Findings(descendants).Any(i=>i.Start==descendants[4].Start && !i.CanFix) && Findings(descendants).All(i=>!i.CanFix),
            "An interval over parent labels also protects a descendant group belonging to an interior parent, independently of the existing parent-dependency guard");
        foreach(string reference in new[]{"Xem mục 1.4 đến 2.8.","Xem mục (1.4) tới (1.8.1).",
            "Xem mục 1.4.2 – 1.8."})
            assert(Manual(Rows("1. Mục cha","1.1. Nhánh đầu","1.9. Nhánh sau",reference)),
                "An explicit numeric range across hierarchy depths or prefixes freezes its shared numeric subtree instead of guessing interior sibling ownership");

        foreach(string reference in new[]{"Xem mục 2.4 đến 2.10.","Xem mục 1.20 tới 1.30.","Xem mục (01.020) – (001.030).",
            "Xem mục 4 đến 10."})
        {
            var rows=Rows("1. Mục cha","1.1. Nhánh đầu","1.9. Nhánh sau",reference);
            var fix=Findings(rows).Single(i=>i.CanFix);
            assert(fix.Original=="9" && fix.Replacement=="2" && fix.Start==rows[2].Start+2,
                "An unrelated numeric interval does not freeze an independent hierarchy prefix or a sibling group outside its bounds");
        }
        var outsideAmbiguous=Rows("2. Mục cha","2.1. Nhánh đầu","2.9. Nhánh sau","Xem mục 1.4 đến 1.8.1.");
        assert(Findings(outsideAmbiguous).Single(i=>i.CanFix).Replacement=="2",
            "An ambiguous range with a clear shared hierarchy prefix does not fence an unrelated numeric subtree");

        foreach(string reference in new[]{"Xem mục 4 và 10.","Xem mục 4, 10.","Xem mục (4) và (10).","Xem mục (4), (10).",
            "Xem mục 10 đến 20.","Xem mục 00010–00020."})
        {
            var rows=Flat("9",reference);var fix=Findings(rows).Single(i=>i.CanFix);
            assert(fix.Original=="9" && fix.Replacement=="3" && fix.Start==rows[2].Start,
                "A comma/and list is not expanded as an interval and an unrelated interval keeps the existing exact-span correction");
        }
        var roman=Rows("I. Nội dung đầu","II. Nội dung giữa","IX. Nội dung cuối","Xem mục 4 đến 10.");
        assert(Findings(roman).Single(i=>i.CanFix).Replacement=="III",
            "Numeric ranges do not infer equivalent Roman label values or expand the previously supported numbering-family ownership");

        foreach(var profile in Enum.GetValues<DocumentProfile>())
        foreach(string reference in new[]{"Xem mục 4 đến 10.","Xem mục số (4), (9).".Normalize(NormalizationForm.FormD)})
            assert(Manual(Flat("9",reference),profile),
                "Range and independently parenthesized-list guards are independent of the selected document profile");

        var original=Flat("9","");var pending=Findings(original).Single(i=>i.CanFix);
        foreach(string reference in new[]{"Xem mục 4 đến 10.","Xem mục (8), (9).","Xem mục 4 tới (10)."})
        {
            var changed=Flat("9",reference);var current=Checker.Check(changed);
            bool rejected=false,dependenciesRejected=false;
            try{CurrentTextPlan.Validate([pending],current);}catch(InvalidOperationException){rejected=true;}
            try{RepairDependencies.Expand([pending],current);}catch(InvalidOperationException){dependenciesRejected=true;}
            assert(rejected && dependenciesRejected && changed[2].Start==pending.Start && changed[2].Text[..pending.Length]==pending.Original,
                "A newly added interior-range or later parenthesized reference invalidates an old selection at unchanged source label bytes and Word CPs before mutation");
        }
        var positive=Flat("9","😀 Xem mục (4), (10).");var approved=Findings(positive).Single(i=>i.CanFix);
        string story=string.Concat(positive.Select(p=>p.Text));
        string expected=AtomicTextBatch.ExpectedStory([approved],story,i=>story[..(i.Start-positive[0].Start)]);
        assert(expected=="1. Nội dung đầu\r2. Nội dung giữa\r3. Nội dung cuối\r😀 Xem mục (4), (10).\r",
            "An approved correction beside an unrelated list preserves all unselected prose, parentheses, emoji and paragraph terminators exactly");
    }
}
