using System.Text;
using System.Text.RegularExpressions;
using ChinhTa.Core;

public static class BirthDateWordFieldChecks
{
    private const int Cp=317;
    private static Issue[] Spaces(string text,DocumentProfile profile=DocumentProfile.Personal)=>
        Checker.Check([new(Cp,text)],profile).Where(i=>i.CanFix && i.Kind==FixKind.Text &&
            i.Replacement==" " && i.Original.Length>0 && i.Original.All(c=>c==' ')).ToArray();
    private static string Apply(string text,IReadOnlyList<Issue> edits)=>
        AtomicTextBatch.ExpectedStory(BatchPlan.Create(edits),text,i=>text[..(i.Start-Cp)]);

    public static void Run(Action<bool,string> assert)
    {
        foreach(string source in new[]{
            "Sinh ngày        tháng        năm        ",
            "Sinh ngày:    tháng    năm    (chưa điền)",
            "SINH NGÀY    THÁNG    NĂM    \r\a",
            "Sinh ngày …    tháng ____    năm ....    (chưa điền)",
            "Sinh ngày 03 tháng        năm 2026",
            "Sinh ngày    tháng 10 năm    ; ghi chú.",
            "Sinh\u00a0ngày    tháng\u202f    năm    (chưa điền)",
            "\tSinh ngày\u00a0    tháng    năm\u202f    \r\a"})
        foreach(var normalization in new[]{NormalizationForm.FormC,NormalizationForm.FormD})
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            string text=source.Normalize(normalization);
            assert(Spaces(text,profile).Length==0,
                "An anchored Sinh ngày word-date form preserves blank and placeholder widths, raw NFC/NFD, supported nonbreaking padding and Word endings in every profile");
            var fields=Regex.Matches(text,@" {2,}");
            assert(fields.All(m=>SpacingRules.IsBlankField(text,m.Index,m.Length)),
                "Each source ASCII padding span of the recognized birth-date fields has local blank-field ownership, including the last slot before annotations");
        }

        foreach(var sample in new (string Source,string Expected,int Count)[]{
            ("Sinh ngày  03 tháng    năm    (chưa điền)","Sinh ngày 03 tháng    năm    (chưa điền)",1),
            ("Sinh ngày    tháng  10 năm    ; ghi  chú.","Sinh ngày    tháng 10 năm    ; ghi chú.",2),
            ("Sinh ngày    tháng    năm  2026","Sinh ngày    tháng    năm 2026",1),
            ("Sinh ngày  03 tháng  10 năm  2026","Sinh ngày 03 tháng 10 năm 2026",3),
            ("Sinh  ngày    tháng    năm    ; ghi chú 😀  theo mẫu.\r\a",
                "Sinh ngày    tháng    năm    ; ghi chú 😀 theo mẫu.\r\a",2),
            ("Sinh ngày …    tháng ____    năm ....    ; đã  duyệt.",
                "Sinh ngày …    tháng ____    năm ....    ; đã duyệt.",1)})
        foreach(var normalization in new[]{NormalizationForm.FormC,NormalizationForm.FormD})
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            string text=sample.Source.Normalize(normalization);var edits=Spaces(text,profile);
            assert(edits.Length==sample.Count && Apply(text,edits)==sample.Expected.Normalize(normalization) &&
                edits.All(i=>i.Start>=Cp && text.Substring(i.Start-Cp,i.Length)==i.Original),
                "The label, filled date slots and independent outside prose gaps retain exact source repairs while all blank slots keep their width");
            CurrentTextPlan.Validate(edits,Checker.Check([new(Cp,text)],profile));
        }

        foreach(string source in new[]{
            "Trong sinh ngày    tháng    năm    ",
            "Ghi chú: sinh ngày    tháng    năm    ",
            "Hà Nội, sinh ngày    tháng    năm    ",
            "Sinhngày    tháng    năm    ",
            "Sinh ngay    tháng    năm    ",
            "Sinh ngày    thang    năm    ",
            "Sinh ngày    tháng tám    năm    ",
            "Sinh ngày    tháng    năm 2026x",
            "Sinh ngày    tháng    năm ____x",
            "Sinh ngày    tháng    năm ...x",
            "Sinh ngày    tháng    năm hai nghìn",
            "Sinh ngày    tháng    năm    theo mẫu"})
        foreach(var normalization in new[]{NormalizationForm.FormC,NormalizationForm.FormD})
        {
            string text=source.Normalize(normalization);
            assert(Regex.Matches(text,@" {2,}").All(m=>!SpacingRules.IsBlankField(text,m.Index,m.Length)) &&
                Spaces(text).Length>0,
                "Arbitrary prose/location prefixes, broken labels, unsupported slot values and unparsed suffixes do not gain the new birth-date exception: "+source);
        }
        foreach(char unsupported in new[]{'\v','\f','\n','\r','\a','\u2003'})
        {
            string text=$"Sinh{unsupported}ngày    tháng    năm    (chưa điền)";
            assert(Regex.Matches(text,@" {2,}").All(m=>!SpacingRules.IsBlankField(text,m.Index,m.Length)),
                "The standalone birth label accepts only its existing horizontal whitespace vocabulary, never controls or all Unicode whitespace");
        }
        foreach(string label in new[]{"Ngày","Sinh ngày"})
        {
            foreach(string ending in new[]{"...x","2026.x","____.x"})
            {
                string text=$"{label}    tháng    năm {ending}";
                assert(Regex.Matches(text,@" {2,}").All(m=>!SpacingRules.IsBlankField(text,m.Index,m.Length)),
                    "A period inside an unparsed year cannot serve as a fake terminal boundary in either the existing or added word-date label");
            }
            foreach(string ending in new[]{".",". Ghi chú.",".\u00a0Ghi chú.",".\r\a"})
            {
                string text=$"{label}    tháng    năm    {ending}";
                assert(Spaces(text).Length==0,
                    "A real terminal date period before EOF, prose whitespace or a Word ending still preserves the existing blank-slot protection");
            }
        }

        foreach(var normalization in new[]{NormalizationForm.FormC,NormalizationForm.FormD})
        foreach(var profile in Enum.GetValues<DocumentProfile>())
        {
            string old="Sinh ngày    03 tháng 10 năm 2026".Normalize(normalization);
            string current="Sinh ngày    .. tháng 10 năm 2026".Normalize(normalization);
            var pending=Spaces(old,profile);bool rejected=false;
            try{CurrentTextPlan.Validate(pending,Checker.Check([new(Cp,current)],profile));}
            catch(InvalidOperationException){rejected=true;}
            assert(pending.Length==1 && rejected && current.Substring(pending[0].Start-Cp,pending[0].Length)==pending[0].Original,
                "An unchanged old filled-day whitespace span loses its write permission after its same-width value becomes a placeholder");
        }
        assert(!Checker.Check([new(Cp,"Sinh ngày    tháng    năm    ; ghi  chú.",TextEditable:false)])
            .Any(i=>i.CanFix && i.Kind==FixKind.Text),
            "The added birth-date label never bypasses the independent XML text-edit permission");
    }
}
