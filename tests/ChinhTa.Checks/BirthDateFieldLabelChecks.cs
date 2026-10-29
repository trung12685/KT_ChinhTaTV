using System.Text;
using System.Text.RegularExpressions;
using ChinhTa.Core;

public static class BirthDateFieldLabelChecks
{
    private const int Cp=281;
    private static Issue[] Spaces(string text,DocumentProfile profile=DocumentProfile.Personal)=>Checker.Check([new(Cp,text)],profile)
        .Where(i=>i.CanFix && i.Kind==FixKind.Text && i.Replacement==" " && i.Original.All(c=>c==' ')).ToArray();
    private static string Apply(string text,IReadOnlyList<Issue> edits)=>
        AtomicTextBatch.ExpectedStory(BatchPlan.Create(edits),text,i=>text[..(i.Start-Cp)]);

    public static void Run(Action<bool,string> assert)
    {
        foreach(string label in new[]{"Ngày, tháng, năm sinh:","NGÀY, THÁNG, NĂM SINH:",
            "ngày , tháng , năm sinh :","Ngày,\u00a0tháng,\u202fnăm sinh:"})
        foreach(string fields in new[]{"    /    /____","    /    /2026","..    /    /…"})
        foreach(var form in new[]{NormalizationForm.FormC,NormalizationForm.FormD})
        foreach(var profile in Enum.GetValues<DocumentProfile>())
            assert(Spaces((label+fields+"\r\a").Normalize(form),profile).Length==0,
                "The exact comma-separated birth-date label protects only recognized blank or placeholder slash slots in raw NFC/NFD and every profile");

        foreach(var sample in new[]{
            ("Ngày, tháng, năm sinh:    /  10/____; ghi  chú.","Ngày, tháng, năm sinh:    / 10/____; ghi chú.",2),
            ("Ngày, tháng, năm sinh:  03/    /2026","Ngày, tháng, năm sinh: 03/    /2026",1),
            ("Ngày, tháng, năm sinh:  03/  10/  2026","Ngày, tháng, năm sinh: 03/ 10/ 2026",3),
            ("Ngày, tháng, năm sinh:    /    /____; ghi chú 😀  theo mẫu.","Ngày, tháng, năm sinh:    /    /____; ghi chú 😀 theo mẫu.",1)})
        foreach(var form in new[]{NormalizationForm.FormC,NormalizationForm.FormD})
        {
            string text=sample.Item1.Normalize(form);var edits=Spaces(text);
            assert(edits.Length==sample.Item3 && Apply(text,edits)==sample.Item2.Normalize(form) &&
                edits.All(i=>text.Substring(i.Start-Cp,i.Length)==i.Original),
                "Filled date slots and outside prose still repair at exact source offsets while blank field widths and Unicode stay intact");
        }
        foreach(string text in new[]{"Ngày tháng năm sinh:    /    /____", "Ngày, tháng, năm sinh    /    /____",
            "Ngày, tháng, năm sinh:    /tháng/____", "Ngày, tháng, năm sinh:    /    /____x",
            "Ngày, tháng sinh:    /    /____", "Nội dung ngày, tháng, năm sinh:    /    /____"})
            assert(Regex.Matches(text,@" {2,}").All(m=>!SpacingRules.IsBlankField(text,m.Index,m.Length)),
                "Missing commas or colon, incomplete or prose labels and malformed slots gain no broader date-field exception");

        const string previous="Ngày, tháng, năm sinh:  03/10/2026",current="Ngày, tháng, năm sinh:  ../10/2026";
        var old=Spaces(previous);bool rejected=false;
        try{CurrentTextPlan.Validate(old,Checker.Check([new(Cp,current)],DocumentProfile.Personal));}
        catch(InvalidOperationException){rejected=true;}
        assert(old.Length==1 && rejected && current.Substring(old[0].Start-Cp,old[0].Length)==old[0].Original,
            "An unchanged old filled-day gap cannot replay once its same-width value becomes a placeholder under the recognized birth-date label");
        assert(!Checker.Check([new(Cp,"Ngày, tháng, năm sinh:    /    /____; ghi  chú.",TextEditable:false)]).Any(i=>i.CanFix),
            "Recognizing the extra birth-date label never bypasses independent structured-text permission");
    }
}
