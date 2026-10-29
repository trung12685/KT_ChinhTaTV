using System.Text;
using ChinhTa.Core;

public static class OutlineReferenceVariantChecks
{
    private static Paragraph[] Rows(params string[] texts)
    {
        int cp=93;
        return texts.Select(text=>{var row=new Paragraph(cp,text+"\r",Outline:new(OutlineScope.PlainBody));cp+=row.Text.Length;return row;}).ToArray();
    }
    private static Issue[] Outline(Paragraph[] rows,DocumentProfile profile=DocumentProfile.State)=>Checker.Check(rows,profile)
        .Where(i=>i.Kind==FixKind.OutlineOrder).ToArray();
    private static Paragraph[] Flat(string reference)=>Rows("1. Nội dung đầu","2. Nội dung giữa","4. Nội dung cuối",reference);

    public static void Run(Action<bool,string> assert)
    {
        foreach(string cue in new[]{"mục","điểm","khoản","phần","chương"})
        foreach(string label in new[]{"4","3"})
        foreach(string gap in new[]{" ","\t","\u00a0","\u202f"})
        foreach(var form in new[]{NormalizationForm.FormC,NormalizationForm.FormD})
        {
            string reference=("😀 Xem "+cue+gap+"số"+gap+label+".").Normalize(form);
            var found=Outline(Flat(reference));
            assert(found.Length>0 && found.All(i=>!i.CanFix),
                "Canonical cue variants and horizontal spaces freeze renumbering when either the old or proposed label is referenced");
        }
        foreach(string reference in new[]{"Xem mục\t4.","Xem mục\u00a04.","Xem KHOẢN SỐ 4.","Xem MỤC 3.".Normalize(NormalizationForm.FormD)})
        foreach(var profile in Enum.GetValues<DocumentProfile>())
            assert(Outline(Flat(reference),profile).All(i=>!i.CanFix),
                "Bare references, optional number words and decomposed uppercase cues keep the same no-write guard in every profile");

        foreach(string connector in new[]{"và","đến","tới",",","–","-"})
        foreach(string target in new[]{"4","3"})
        foreach(var form in new[]{NormalizationForm.FormC,NormalizationForm.FormD})
        {
            // The initial 8 is outside the group, so the final capture itself
            // must be retained; recognizing only the first number is unsafe.
            string reference=("Xem mục\u00a0số\t8\t"+connector+"\u00a0"+target+".").Normalize(form);
            assert(Outline(Flat(reference)).All(i=>!i.CanFix),
                "List/range reference captures retain every relevant old/proposed label across horizontal gaps and decomposed joiners");
        }
        foreach(string reference in new[]{"Xem khoản (4).","Xem mục số (3).","Xem điểm\t(\u00a04\u00a0).".Normalize(NormalizationForm.FormD)})
            assert(Outline(Flat(reference)).All(i=>!i.CanFix),
                "A single clearly parenthesized clause label freezes the same bounded numbering group");

        foreach(string reference in new[]{"Xem mục số 1.4.","Xem khoản\u00a0số\t1.2.","Xem mục số 1.4.9.","Xem mục 1.8 đến 1.2."})
        foreach(var form in new[]{NormalizationForm.FormC,NormalizationForm.FormD})
        {
            var rows=Rows("1. Mục cha","1.1. Nhánh đầu","1.4. Nhánh sau",reference.Normalize(form));
            assert(Outline(rows).All(i=>!i.CanFix),
                "Hierarchical old/proposed/descendant references freeze the exact leaf numbering without rewriting its prefix");
        }
        foreach(string reference in new[]{"Mục đích của báo cáo là công tác năm 2026.","Điểm số của bài này là 4.",
            "Xem mục tiêu năm 2026.","Văn bản số 4.","Ghi chú: 4 người tham dự.",
            "Xem mục 2026.","Xem mục số 40.","Xem mục số 4a.","Xem khoản số (8)."})
        foreach(var form in new[]{NormalizationForm.FormC,NormalizationForm.FormD})
        {
            var rows=Flat(reference.Normalize(form));var fix=Outline(rows).Single(i=>i.CanFix);
            assert(fix.Original=="4" && fix.Replacement=="3" && fix.Start==rows[2].Start,
                "Unreferenced prose, years and unrelated labels cannot create a false structural-reference freeze or a truncated number match");
        }
        var unrelated=Rows("1. Mục cha","1.1. Nhánh đầu","1.4. Nhánh sau","Xem mục số 2.4.".Normalize(NormalizationForm.FormD));
        assert(Outline(unrelated).Single(i=>i.CanFix).Replacement=="2",
            "A different hierarchical prefix does not claim ownership of an independent leaf correction");

        var plain=Rows("1. Nội dung đầu","2. Nội dung giữa","4. Nội dung cuối");
        var pending=Outline(plain).Single(i=>i.CanFix);
        foreach(string reference in new[]{"Xem mục số 4.","Xem mục\u00a03.".Normalize(NormalizationForm.FormD),"Xem khoản (4)."})
        {
            var changed=Flat(reference);var findings=Checker.Check(changed);
            bool rejected=false,expanded=false;
            try{CurrentTextPlan.Validate([pending],findings);}catch(InvalidOperationException){rejected=true;}
            try{RepairDependencies.Expand([pending],findings);}catch(InvalidOperationException){expanded=true;}
            assert(rejected && expanded && changed[2].Start==pending.Start &&
                changed[2].Text[..pending.Length]==pending.Original,
                "A newly added reference invalidates the old write at unchanged label bytes and CPs before selection expansion or mutation");
        }
        var direct=Outline(plain).Single(i=>i.CanFix);string story=string.Concat(plain.Select(p=>p.Text));
        string expected=AtomicTextBatch.ExpectedStory([direct],story,i=>story[..(i.Start-plain[0].Start)]);
        assert(expected=="1. Nội dung đầu\r2. Nội dung giữa\r3. Nội dung cuối\r",
            "The unreferenced positive control keeps its existing exact-span correction and every paragraph terminator");
        PaddedReferenceChecks(assert);
    }
    private static void PaddedReferenceChecks(Action<bool,string> assert)
    {
        foreach(int width in new[]{2,3})
        foreach(string reference in new[]{"Xem mục 4.","Xem mục 3.","Xem mục số 004.","Xem mục số (0003).",
            "Xem mục 0004.09.","Xem mục số 003.09.001.","Xem mục số 8 đến 0003.".Normalize(NormalizationForm.FormD)})
        {
            var rows=Rows(1.ToString(new string('0',width))+". Nội dung đầu",2.ToString(new string('0',width))+". Nội dung giữa",
                4.ToString(new string('0',width))+". Nội dung cuối",reference);
            var findings=Outline(rows);
            assert(findings.Length>0 && findings.All(i=>!i.CanFix),
                "Unpadded or differently padded old/proposed/descendant references freeze a padded numbering group without rewriting the source labels");
        }
        foreach(string reference in new[]{"Xem mục 004.","Xem mục 0003.","Xem khoản số 004.02.","Xem mục 003.09."})
            assert(Outline(Flat(reference)).All(i=>!i.CanFix),
                "Padded numeric references protect the same unpadded old/proposed label values and their descendants");
        foreach(string reference in new[]{"Xem mục 01.04.","Xem mục số 001.002.","Xem mục 001.004.009.","Xem mục 001.002.009."})
        {
            var rows=Rows("1. Mục cha","1.1. Nhánh đầu","1.4. Nhánh sau",reference);
            assert(Outline(rows).All(i=>!i.CanFix),
                "Numeric reference comparison removes padding in every hierarchy component before protecting an old/proposed leaf or descendant");
        }
        foreach(int width in new[]{2,3})
        foreach(string reference in new[]{"", "Xem mục 2026.","Xem mục 0008.","Xem mục 0000.4."})
        {
            string format=new('0',width);
            var rows=Rows(1.ToString(format)+". Nội dung đầu",2.ToString(format)+". Nội dung giữa",4.ToString(format)+". Nội dung cuối",reference);
            var fix=Outline(rows).Single(i=>i.CanFix);
            assert(fix.Original==4.ToString(format) && fix.Replacement==3.ToString(format) && fix.Start==rows[2].Start,
                "An unrelated numeric reference or no reference retains the existing padded replacement width and exact source span");
        }
        var unrelated=Rows("1. Mục cha","1.1. Nhánh đầu","1.4. Nhánh sau","Xem mục 002.004.");
        assert(Outline(unrelated).Single(i=>i.CanFix).Replacement=="2",
            "Canonicalizing reference padding does not merge unrelated hierarchy prefixes");
        var roman=Rows("I. Nội dung đầu","II. Nội dung giữa","IV. Nội dung cuối","Xem mục 4.");
        assert(Outline(roman).Single(i=>i.CanFix).Replacement=="III",
            "Numeric canonicalization never guesses Arabic and Roman reference ownership from their apparent values");
        var plain=Rows("01. Nội dung đầu","02. Nội dung giữa","04. Nội dung cuối");
        var pending=Outline(plain).Single(i=>i.CanFix);
        foreach(string reference in new[]{"Xem mục 4.","Xem mục số 3.","Xem mục 003.09."})
        {
            var changed=Rows("01. Nội dung đầu","02. Nội dung giữa","04. Nội dung cuối",reference);
            bool rejected=false;
            try{CurrentTextPlan.Validate([pending],Checker.Check(changed));}catch(InvalidOperationException){rejected=true;}
            assert(rejected && changed[2].Start==pending.Start && changed[2].Text[..pending.Length]==pending.Original,
                "Adding an equivalent numeric reference invalidates an old padded-label write at identical source bytes and Word CPs");
        }
    }
}
